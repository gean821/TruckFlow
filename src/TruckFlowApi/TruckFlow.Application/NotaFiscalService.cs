using DFe.Utils;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFe.Classes.Informacoes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.NotaFiscais;
using TruckFlow.Application.Sefaz;
using TruckFlow.Application.Validators.NotaFiscal;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.NotaFiscal;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class NotaFiscalService : INotaFiscalService
    {
        private readonly INotaFiscalRepositorio _repo;
        private readonly IFornecedorRepositorio _fornecedorRepo;
        private readonly IProdutoFornecedorRepositorio _produtoFornecedorRepo;
        private readonly IValidator<NotaFiscalParsedDto> _parsedValidator;
        private readonly IValidator<NotaFiscalItemDto> _itemValidator;
        private readonly IProdutoRepositorio _produtoRepositorio;
        private readonly ProdutoLearningService _learningService;
        private readonly ILogger<NotaFiscalService> _logger;
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly IEmpresaContext _empresaContext;
        private readonly ISefazClient _sefazClient;
        private readonly SefazOptions _sefazOptions;


        public NotaFiscalService(
            INotaFiscalRepositorio repo,
            IFornecedorRepositorio fornecedorRepo,
            IProdutoFornecedorRepositorio produtoFornecedorRepo,
            IValidator<NotaFiscalParsedDto> parsedValidator,
            IValidator<NotaFiscalItemDto> itemValidator,
            ProdutoLearningService learningService,
            IProdutoRepositorio produtoRepositorio,
            ILogger<NotaFiscalService> logger,
            IEmpresaRepositorio empresaRepo,
            IEmpresaContext empresaContext,
            ISefazClient sefazClient,
            IOptions<SefazOptions> sefazOptions
            )
        {
            _repo = repo;
            _fornecedorRepo = fornecedorRepo;
            _produtoFornecedorRepo = produtoFornecedorRepo;
            _produtoRepositorio = produtoRepositorio;
            _parsedValidator = parsedValidator;
            _itemValidator = itemValidator;
            _learningService = learningService;
            _logger = logger;
            _empresaRepo = empresaRepo;
            _empresaContext = empresaContext;
            _sefazClient = sefazClient;
            _sefazOptions = sefazOptions.Value;
        }
        public async Task<NotaFiscalParsedDto> ParseXmlAsync(
            Stream xmlStream,
            CancellationToken token
            )
        {
            using var sr = new StreamReader(xmlStream);
            var xml = (await sr.ReadToEndAsync(token)).Trim();

            Console.WriteLine("=================================");
            if (string.IsNullOrEmpty(xml))
            {
                Console.WriteLine("❌ XML CHEGOU VAZIO");
            }
            else
            {
                Console.WriteLine($"✅ XML RECEBIDO ({xml.Length} chars)");
                Console.WriteLine(xml.Substring(0, Math.Min(200, xml.Length)));
            }
            Console.WriteLine("=================================");

            NFe.Classes.NFe nfe;
            try
            {
                nfe = NotaFiscalXmlExtractor.ParseXml(xml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao processar XML da Nota Fiscal. primeiros 500 chars: {xmlSnippet}",
                    xml.Length > 500 ? xml[..500] : xml);

                throw;
            }

            var extraida = NotaFiscalXmlExtractor.Extrair(nfe);

            if (extraida.Itens.Count == 0)
            {
                _logger.LogWarning("Nota fiscal sem itens.");
            }

            return await EnriquecerComMatchingAsync(extraida, token);
        }

        /// <summary>
        /// Busca a NF-e completa direto na SEFAZ (NFeDistribuicaoDFe) por chave de acesso,
        /// pro fluxo real de produção em que o motorista não tem o XML em mãos (só a chave,
        /// via câmera ou digitada) e a nota nunca passou pelo TruckFlow antes.
        /// Reaproveita o mesmo parser/matching do upload manual (ParseXmlAsync), a única
        /// diferença é de onde o XML vem.
        /// </summary>
        public async Task<NotaFiscalParsedDto> ParseFromSefazAsync(
            string chaveAcesso,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso) || chaveAcesso.Length != 44)
            {
                throw new BusinessException("Chave de acesso inválida (deve ter 44 dígitos).");
            }

            var resultado = await _sefazClient.ConsultarDistribuicaoAsync(chaveAcesso, token);

            if (!resultado.Disponivel)
            {
                _logger.LogWarning(
                    "Nota fiscal não disponível na SEFAZ. Chave={Chave} cStat={CStat} xMotivo={XMotivo}",
                    chaveAcesso, resultado.CStat, resultado.XMotivo);

                throw new BusinessException(
                    $"Nota fiscal não disponível na SEFAZ. Motivo: {resultado.XMotivo} (cStat={resultado.CStat})");
            }

            var nfe = NotaFiscalXmlExtractor.ParseXml(resultado.XmlNfe!);
            var extraida = NotaFiscalXmlExtractor.Extrair(nfe);

            return await EnriquecerComMatchingAsync(extraida, token);
        }

        /// <summary>
        /// Enriquecimento com dados do banco (matching fornecedor/produto), compartilhado
        /// entre ParseXmlAsync (upload manual) e ParseFromSefazAsync (busca direta na SEFAZ).
        /// A extração pura do XML já aconteceu antes disso (NotaFiscalXmlExtractor).
        /// </summary>
        private async Task<NotaFiscalParsedDto> EnriquecerComMatchingAsync(
            NotaFiscalXmlExtraidaDto extraida,
            CancellationToken token)
        {
            var cnpjDestinatario = new string(extraida.DestinatarioCpfCnpj.Where(char.IsDigit).ToArray());

            Empresa? empresaDestinataria = null;
            if (!string.IsNullOrEmpty(cnpjDestinatario))
            {
                empresaDestinataria = await _empresaRepo.GetByCnpj(cnpjDestinatario, token);
            }

            var itensDto = new List<NotaFiscalItemDto>();

            if (empresaDestinataria != null)
            {
                using var tenantScope = _empresaContext.WithTenant(empresaDestinataria.Id);

                var produtosDoSistema = await _produtoRepositorio.GetAll(token);

                Fornecedor? fornecedor = null;
                var cnpjEmitente = new string(extraida.EmitenteCnpj.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(cnpjEmitente))
                {
                    fornecedor = await _fornecedorRepo.GetByCnpj(cnpjEmitente, token);
                }

                foreach (var itemExtraido in extraida.Itens)
                {
                    var (produto, origem) = await TryMatchProdutoAsync(
                        produtosDoSistema, fornecedor, itemExtraido.Ean, itemExtraido.Codigo, token);

                    itensDto.Add(new NotaFiscalItemDto
                    {
                        Codigo = itemExtraido.Codigo,
                        Ean = itemExtraido.Ean,
                        Descricao = itemExtraido.Descricao,
                        ProdutoSistemaId = produto?.Id,
                        ProdutoSistemaNome = produto?.Nome,
                        Quantidade = itemExtraido.Quantidade,
                        Unidade = itemExtraido.Unidade,
                        ValorTotal = itemExtraido.ValorTotal,
                        ValorUnitario = itemExtraido.ValorUnitario,
                        Status = produto != null
                            ? NotaFiscalItemStatus.Matched
                            : NotaFiscalItemStatus.PendenteRevisao,
                        OrigemMatch = origem
                    });
                }
            }
            else
            {
                // Empresa destinatária não cadastrada — itens vão pendentes sem matching.
                foreach (var itemExtraido in extraida.Itens)
                {
                    itensDto.Add(new NotaFiscalItemDto
                    {
                        Codigo = itemExtraido.Codigo,
                        Ean = itemExtraido.Ean,
                        Descricao = itemExtraido.Descricao,
                        Quantidade = itemExtraido.Quantidade,
                        Unidade = itemExtraido.Unidade,
                        ValorTotal = itemExtraido.ValorTotal,
                        ValorUnitario = itemExtraido.ValorUnitario,
                        Status = NotaFiscalItemStatus.PendenteRevisao
                    });
                }
            }

            var notaFiscalDto = new NotaFiscalParsedDto
            {
                ChaveAcesso = extraida.ChaveAcesso,
                Numero = extraida.Numero,
                TipoCarga = TipoCarga.Indefinido,
                Serie = extraida.Serie,
                DataEmissao = extraida.DataEmissao,
                EmitenteNome = extraida.EmitenteNome,
                EmitenteCnpj = extraida.EmitenteCnpj,
                Fornecedor = extraida.EmitenteNome,
                DestinatarioNome = extraida.DestinatarioNome,
                DestinatarioCpfCnpj = extraida.DestinatarioCpfCnpj,
                ValorTotal = extraida.ValorTotal,
                PesoBruto = extraida.PesoBruto,
                VolumeQuantidade = extraida.VolumeQuantidade,
                PlacaVeiculo = extraida.PlacaVeiculo,
                Itens = itensDto,
                ValidationWarnings = []
            };

            Console.WriteLine("=================================");
            Console.WriteLine($"EMIT CNPJ: {extraida.EmitenteCnpj}");
            Console.WriteLine($"DEST CNPJ/CPF: {extraida.DestinatarioCpfCnpj}");
            Console.WriteLine("=================================");

            return notaFiscalDto;
        }

        /// <summary>
        /// Matching prioritário sem decisão do motorista:
        ///   Pri 1: EAN exato em produto cadastrado
        ///   Pri 2: ProdutoFornecedor mapping (FornecedorId + cProd)
        ///   Pri 3: Histórico — mesmo fornecedor já enviou esse cProd vinculado a produto X
        ///   Pri 4: nenhum → caller marca PendenteRevisao
        /// Match por "descrição contains nome do produto" foi removido (gerava falso positivo).
        /// </summary>
        private async Task<(
            Produto? produto,
            OrigemMatchProduto? origem)> TryMatchProdutoAsync(
            List<Produto> produtosDoSistema,
            Fornecedor? fornecedor,
            string? eanDaNota,
            string codigoFornecedor,
            CancellationToken token)
        {
            bool temEanValido = !string.IsNullOrWhiteSpace(eanDaNota)
                && eanDaNota!.Trim().ToUpperInvariant() != "SEM GTIN";

            if (temEanValido)
            {
                var produto = produtosDoSistema.FirstOrDefault(x => x.CodigoEan == eanDaNota);
                
                if (produto != null) {
                    return (produto, OrigemMatchProduto.EanAuto);
                } 
            }

            if (fornecedor != null && !string.IsNullOrWhiteSpace(codigoFornecedor))
            {
                var mapping = await _produtoFornecedorRepo.GetByFornecedorAndCodigo(
                    fornecedor.Id, codigoFornecedor, token);

                if (mapping != null)
                {
                    var produto = produtosDoSistema.FirstOrDefault(p => p.Id == mapping.ProdutoId)
                                  ?? mapping.Produto;
                    
                    if (produto != null) {
                        return (produto, OrigemMatchProduto.ProdFornecAuto);
                    } 
                }

                var ultimoProdutoId = await _repo.GetUltimoProdutoIdPorFornecedorECodigo(
                    fornecedor.Id, codigoFornecedor, token);

                if (ultimoProdutoId.HasValue)
                {
                    var produto = produtosDoSistema.FirstOrDefault(p => p.Id == ultimoProdutoId.Value);
                    if (produto != null) {
                        return (produto, OrigemMatchProduto.HistoricoAuto);
                    } 
                }
            }

            return (null, null);
        }

        
        private static bool NotaTemAgendamentoAtivo(NotaFiscal nota)
        {
            if (nota.AgendamentoId is null || nota.Agendamento is null) {
                return false;
            } 

            var s = nota.Agendamento.StatusAgendamento;
            return s != StatusAgendamento.Cancelado
                && s != StatusAgendamento.Expirado
                && s != StatusAgendamento.Disponivel;
        }

        /// <summary>
        /// Re-aplica matching só nos itens PendenteRevisao. Itens Matched (incluindo AdminManual)
        /// são preservados — admin pode ter classificado entre a 1ª e a 2ª chamada de /save.
        /// </summary>
        private async Task RematchPendentesAsync(
            NotaFiscal nota,
            Fornecedor? fornecedor,
            List<Produto> produtosDoSistema,
            CancellationToken token)
        {
            var nowUtc = DateTime.UtcNow;
            foreach (var item in nota.Itens.Where(i => i.Status == NotaFiscalItemStatus.PendenteRevisao))
            {
                var (produto, origem) = await TryMatchProdutoAsync(
                    produtosDoSistema, fornecedor, item.Ean, item.Codigo, token);

                if (produto != null)
                {
                    item.ProdutoId = produto.Id;
                    item.Status = NotaFiscalItemStatus.Matched;
                    item.OrigemMatch = origem;
                    item.MatchadoEm = nowUtc;
                    item.MatchadoPor = null;
                    item.Produto = produto;
                }
            }
        }

        private static NotaFiscalParsedDto MapNotaToParsedDto(
            NotaFiscal nota,
            string fornecedorNome)
        {
            return new NotaFiscalParsedDto
            {
                ChaveAcesso = nota.ChaveAcesso,
                Numero = nota.Numero,
                Serie = nota.Serie,
                DataEmissao = nota.DataEmissao,
                EmitenteNome = nota.EmitenteNome,
                EmitenteCnpj = nota.EmitenteCnpj,
                DestinatarioNome = nota.DestinatarioNome,
                DestinatarioCpfCnpj = nota.DestinatarioCpfCnpj,
                Fornecedor = fornecedorNome,
                FornecedorId = nota.FornecedorId,
                ValorTotal = nota.ValorTotal,
                PesoBruto = nota.PesoBruto,
                VolumeQuantidade = nota.VolumeQuantidade,
                PlacaVeiculo = nota.PlacaVeiculo ?? string.Empty,
                TipoCarga = nota.TipoCarga,
                Itens = nota.Itens?.Select(x => new NotaFiscalItemDto
                {
                    Codigo = x.Codigo,
                    Ean = x.Ean,
                    Descricao = x.Descricao,
                    Quantidade = x.Quantidade,
                    Unidade = x.Unidade,
                    ValorUnitario = x.ValorUnitario,
                    ValorTotal = x.ValorTotal,
                    ProdutoSistemaId = x.ProdutoId,
                    ProdutoSistemaNome = x.Produto?.Nome,
                    Status = x.Status,
                    OrigemMatch = x.OrigemMatch
                }).ToList() ?? new List<NotaFiscalItemDto>(),
                ValidationWarnings = new List<string>()
            };
        }

        public async Task<NotaFiscalParsedDto> SaveParsedNotaAsync(
            NotaFiscalParsedDto dto,
            Guid uploadedByUserId,
            CancellationToken token
            )
        {
            ValidationResult validationResult = await _parsedValidator.ValidateAsync(dto, token);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            foreach (var item in dto.Itens)
            {

                var itemResult = await _itemValidator.ValidateAsync(item, token);

                if (!itemResult.IsValid)
                {
                    throw new ValidationException(itemResult.Errors);
                }
            }

            var cnpjDestinatario = new string(dto.DestinatarioCpfCnpj
                .Where(char.IsDigit)
                .ToArray());

            _logger.LogInformation(
                "CNPJ destinatário da NF: {CnpjDestinatario}",
                cnpjDestinatario
            );

            var empresa = await _empresaRepo.GetByCnpj(cnpjDestinatario, token)
                    ??
                    throw new BusinessException(
                        $"Empresa destinatária não cadastrada. CNPJ recebido: {cnpjDestinatario}");

            using var tenantScope = _empresaContext.WithTenant(empresa.Id);

            var cnpjNota = new string(dto.EmitenteCnpj.Where(char.IsDigit).ToArray());
            Console.WriteLine($"[DEBUG] Buscando Fornecedor pelo CNPJ: '{cnpjNota}'");

            var fornecedor = await _fornecedorRepo.GetByCnpj(cnpjNota, token)
                    ?? await _fornecedorRepo.GetByNome(dto.EmitenteNome, token);

            if (fornecedor == null)
            {
                _logger.LogInformation("Novo Fornecedor identificado: {Nome} ({Cnpj})", dto.Fornecedor, cnpjNota);

                fornecedor = new Fornecedor
                {
                    Nome = dto.EmitenteNome,
                    Cnpj = cnpjNota,
                    EmpresaId = empresa.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _fornecedorRepo.CreateFornecedor(fornecedor, token);
                await _fornecedorRepo.SaveChangesAsync(token);
            }
            else
            {
                // ATUALIZAÇÃO OPCIONAL (Self-Healing)
                // Se achou o fornecedor mas ele estava sem CNPJ no banco, atualiza agora!
                if (string.IsNullOrEmpty(fornecedor.Cnpj))
                {
                    fornecedor.Cnpj = cnpjNota;
                    await _fornecedorRepo.Update(fornecedor, token);
                }
            }

            Console.WriteLine($"[DEBUG] DTO DESTINATARIO: '{dto.DestinatarioCpfCnpj}'");

            // IDEMPOTÊNCIA — se NF já existe, reusa em vez de criar duplicada.
            // Caso típico: motorista parseou + salvou + viu lista vazia → volta atrás →
            // admin cria vaga → motorista parseia de novo e chama /save. Sem idempotência,
            // o banco rejeitaria por chave duplicada e a NF ficaria "queimada".
            var produtosDoSistema = await _produtoRepositorio.GetAll(token);
            var notaExistente = await _repo.ObterPorChaveAcrossTenantsAsync(dto.ChaveAcesso, token);

            if (notaExistente is not null)
            {
                if (notaExistente.EmpresaId != empresa.Id)
                {
                    throw new BusinessException(
                        "Esta nota fiscal já está cadastrada em outra empresa.");
                }

                if (NotaTemAgendamentoAtivo(notaExistente))
                {
                    throw new BusinessException(
                        "Esta nota fiscal já está vinculada a um agendamento ativo. " +
                        "Cancele o agendamento existente para reusar a nota.");
                }

                await RematchPendentesAsync(notaExistente, fornecedor, produtosDoSistema, token);

                foreach (var item in notaExistente.Itens
                    .Where(i => i.Status == NotaFiscalItemStatus.Matched && i.ProdutoId.HasValue))
                {
                    await _learningService.TryLearnEanAsync(
                        new NotaFiscalItemDto
                        {
                            Codigo = item.Codigo,
                            Descricao = item.Descricao,
                            Ean = item.Ean,
                            ProdutoSistemaId = item.ProdutoId,
                            ValorUnitario = item.ValorUnitario,
                            ValorTotal = item.ValorTotal
                        },
                        token);
                }

                await _repo.SaveChangesAsync(token);

                _logger.LogInformation(
                    "NF reutilizada via /save idempotente. ChaveAcesso={Chave} TotalItens={Total} Pendentes={Pendentes}",
                    notaExistente.ChaveAcesso,
                    notaExistente.Itens.Count,
                    notaExistente.Itens.Count(i => i.Status == NotaFiscalItemStatus.PendenteRevisao));

                return MapNotaToParsedDto(notaExistente, fornecedor.Nome);
            }

            var nota = new NotaFiscal
            {
                ChaveAcesso = dto.ChaveAcesso,
                Numero = dto.Numero,
                EmpresaId = empresa.Id,
                Serie = dto.Serie,
                DataEmissao = DateTime.SpecifyKind(dto.DataEmissao, DateTimeKind.Utc),
                EmitenteNome = dto.EmitenteNome,
                EmitenteCnpj = dto.EmitenteCnpj,
                DestinatarioNome = dto.DestinatarioNome,
                DestinatarioCpfCnpj = dto.DestinatarioCpfCnpj,
                Fornecedor = fornecedor,
                FornecedorId = fornecedor.Id,
                ValorTotal = dto.ValorTotal,
                PesoBruto = dto.PesoBruto,
                VolumeQuantidade = dto.VolumeQuantidade,
                PlacaVeiculo = dto.PlacaVeiculo,
                TipoCarga = dto.TipoCarga,
                UploadedByUserId = uploadedByUserId,
                UploadedAt = DateTime.UtcNow
            };

            // DEFESA EM PROFUNDIDADE — recomputa matching server-side ignorando
            // ProdutoSistemaId / Status / OrigemMatch que vieram no DTO. Caso o motorista
            // tenha alterado o JSON entre /parse e /save tentando forjar vínculo a produto
            // específico, esses valores são descartados; só o resultado do matching
            // autoritativo (server-side) entra no banco.
            var itensRecomputados = new List<(NotaFiscalItemDto Dto, Produto? Produto, OrigemMatchProduto? Origem)>();
            foreach (var item in dto.Itens)
            {
                var (produto, origem) = await TryMatchProdutoAsync(
                    produtosDoSistema, fornecedor, item.Ean, item.Codigo, token);
                itensRecomputados.Add((item, produto, origem));
            }

            // Auto-learn de EAN usando o produto recomputado (não o que veio do DTO).
            foreach (var entry in itensRecomputados)
            {
                if (entry.Produto is null) continue;
                await _learningService.TryLearnEanAsync(
                    new NotaFiscalItemDto
                    {
                        Codigo = entry.Dto.Codigo,
                        Descricao = entry.Dto.Descricao,
                        Ean = entry.Dto.Ean,
                        ProdutoSistemaId = entry.Produto.Id,
                        ValorUnitario = entry.Dto.ValorUnitario,
                        ValorTotal = entry.Dto.ValorTotal
                    },
                    token);
            }

            var nowUtc = DateTime.UtcNow;
            nota.Itens = itensRecomputados.Select(entry => new NotaFiscalItem
            {
                NotaFiscal = nota,
                Codigo = entry.Dto.Codigo,
                Ean = entry.Dto.Ean,
                Descricao = entry.Dto.Descricao,
                Quantidade = entry.Dto.Quantidade,
                Unidade = entry.Dto.Unidade,
                ValorUnitario = entry.Dto.ValorUnitario,
                ValorTotal = entry.Dto.ValorTotal,
                ProdutoId = entry.Produto?.Id,
                Status = entry.Produto != null
                    ? NotaFiscalItemStatus.Matched
                    : NotaFiscalItemStatus.PendenteRevisao,
                OrigemMatch = entry.Origem,
                MatchadoEm = entry.Origem.HasValue ? nowUtc : (DateTime?)null,
                MatchadoPor = null,
                EmpresaId = empresa.Id,
                CreatedAt = nowUtc
            }).ToList();

            _logger.LogInformation(
            "Fornecedor final vinculado | ID: {Id} | Nome: {Nome} | CNPJ: {Cnpj}",
            fornecedor.Id,
            fornecedor.Nome,
            fornecedor.Cnpj
            );

            var notaSalva = await _repo.SaveParsedNotaAsync(nota, token);
            await _repo.SaveChangesAsync(token);

            return MapNotaToParsedDto(notaSalva, fornecedor.Nome);
        }
        public async Task<NotaFiscalParsedDto?> ObterPorChaveAsync
            (
                string chaveAcesso,
                CancellationToken token
            )
        {
            // Cross-tenant: motorista pode consultar a própria nota; admin é filtrado abaixo.
            var nota = await _repo.ObterPorChaveAcrossTenantsAsync(chaveAcesso, token);

            if (nota == null)
            {
                return null;
            }

            var currentTenant = _empresaContext.EmpresaIdOrNull;
            if (currentTenant.HasValue && currentTenant.Value != nota.EmpresaId)
            {
                return null;
            }

            return new NotaFiscalParsedDto
            {
                ChaveAcesso = nota.ChaveAcesso,
                Numero = nota.Numero,
                Serie = nota.Serie,
                DataEmissao = nota.DataEmissao,
                EmitenteNome = nota.EmitenteNome,
                EmitenteCnpj = nota.EmitenteCnpj,
                DestinatarioNome = nota.DestinatarioNome,
                DestinatarioCpfCnpj = nota.DestinatarioCpfCnpj,
                Fornecedor = nota.Fornecedor?.Nome ?? string.Empty,
                FornecedorId = nota.FornecedorId,
                ValorTotal = nota.ValorTotal,
                PesoBruto = nota.PesoBruto,
                VolumeQuantidade = nota.VolumeQuantidade,
                PlacaVeiculo = nota.PlacaVeiculo!,
                TipoCarga = nota.TipoCarga,
                Itens = nota.Itens?.Select(i => new NotaFiscalItemDto
                {
                    Codigo = i.Codigo,
                    Ean = i.Ean,
                    Descricao = i.Descricao,
                    Quantidade = i.Quantidade,
                    Unidade = i.Unidade,
                    ValorUnitario = i.ValorUnitario,
                    ValorTotal = i.ValorTotal,
                    ProdutoSistemaId = i.ProdutoId,
                    ProdutoSistemaNome = i.Produto?.Nome,
                    Status = i.Status,
                    OrigemMatch = i.OrigemMatch
                }).ToList() ?? new List<NotaFiscalItemDto>(),
                ValidationWarnings = new List<string>()
            };
        }

        public async Task<SefazValidacaoResultadoDto> ValidarNaSefazAsync(
            string chaveAcesso,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso) || chaveAcesso.Length != 44)
            {
                throw new BusinessException("Chave de acesso inválida (deve ter 44 dígitos).");
            }

            var uf = ChaveAcessoHelper.ExtrairUfEmitente(chaveAcesso)
                     ?? _sefazOptions.UfEmitenteFallback
                     ?? throw new BusinessException(
                         "Não foi possível determinar a UF do emitente pela chave de acesso e nenhum fallback está configurado em Sefaz:UfEmitenteFallback.");

            var resultado = await _sefazClient.ConsultarProtocoloAsync(chaveAcesso, uf, token);

            bool persistida = false;
            var notaExistente = await _repo.ObterPorChaveAcrossTenantsAsync(chaveAcesso, token);

            if (notaExistente != null)
            {
                using var tenantScope = _empresaContext.WithTenant(notaExistente.EmpresaId);

                notaExistente.StatusSefaz = resultado.CStat;
                notaExistente.UltimaValidacaoSefaz = DateTime.UtcNow;
                notaExistente.FonteValidacao = FonteValidacao.ConsultaProtocolo;

                if (resultado.Autorizada)
                {
                    notaExistente.Status = NotaFiscalStatus.Validada;
                }
                else if (resultado.Cancelada || resultado.Denegada)
                {
                    notaExistente.Status = NotaFiscalStatus.Rejeitada;
                }

                await _repo.SaveChangesAsync(token);
                persistida = true;
            }

            return new SefazValidacaoResultadoDto
            {
                ChaveAcesso = resultado.ChaveAcesso,
                CStat = resultado.CStat,
                XMotivo = resultado.XMotivo,
                Protocolo = resultado.Protocolo,
                DataAutorizacao = resultado.DataAutorizacao,
                Ambiente = resultado.Ambiente,
                Autorizada = resultado.Autorizada,
                NotaPersistidaAtualizada = persistida,
                ValidadaEm = DateTime.UtcNow
            };
        }
    }
}