using DFe.Utils;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NFe.Classes.Informacoes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.NotaFiscal;
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
        private readonly IValidator<NotaFiscalParsedDto> _parsedValidator;
        private readonly IValidator<NotaFiscalItemDto> _itemValidator;
        private readonly IProdutoRepositorio _produtoRepositorio;
        private readonly ProdutoLearningService _learningService;
        private readonly ILogger<NotaFiscalService> _logger;
        private readonly IEmpresaRepositorio _empresaRepo;


        public NotaFiscalService(
            INotaFiscalRepositorio repo,
            IFornecedorRepositorio fornecedorRepo,
            IValidator<NotaFiscalParsedDto> parsedValidator,
            IValidator<NotaFiscalItemDto> itemValidator,
            ProdutoLearningService learningService,
            IProdutoRepositorio produtoRepositorio,
            ILogger<NotaFiscalService> logger,
            IEmpresaRepositorio empresaRepo
            )
        {
            _repo = repo;
            _fornecedorRepo = fornecedorRepo;
            _produtoRepositorio = produtoRepositorio;
            _parsedValidator = parsedValidator;
            _itemValidator = itemValidator;
            _learningService = learningService;
            _logger = logger;
            _empresaRepo = empresaRepo;
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
                if (xml.Contains("<nfeProc"))
                {
                    var proc = FuncoesXml.XmlStringParaClasse<NFe.Classes.nfeProc>(xml);

                    if (proc?.NFe?.infNFe == null)
                        throw new BusinessException("XML nfeProc inválido.");

                    nfe = proc.NFe;
                }
                else if (xml.Contains("<NFe"))
                {
                    nfe = FuncoesXml.XmlStringParaClasse<NFe.Classes.NFe>(xml);

                    if (nfe?.infNFe == null)
                        throw new BusinessException("XML NFe inválido.");
                }
                else
                {
                    throw new BusinessException("XML não é uma NF-e válida.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao processar XML da Nota Fiscal. primeiros 500 chars: {xmlSnippet}",
                    xml.Length > 500 ? xml[..500] : xml);

                throw;
            }

            var infNFe = nfe.infNFe;

            if (infNFe.det == null || infNFe.det.Count == 0)
            {
                _logger.LogWarning("Nota fiscal sem itens.");
            }

            // antes de retornar Dto buscamos todos os produtos do sistema para comparar o código EAN
            //essa parte precisa ser melhorada para otimizar processo.
            var produtosDoSistema = await _produtoRepositorio.GetAll(token);
            var itensDto = new List<NotaFiscalItemDto>();

            foreach (var det in infNFe.det!)
            {

                if (det.prod == null)
                {
                    _logger.LogWarning("Item da NF-e sem bloco <prod>. Ignorado.");
                    continue;
                }

                var eanDaNota = det.prod.cEAN; //codigo de barras da nota.
                var codigoFornecedor = det.prod.cProd;
                Produto? produtoEncontrado = null;

                bool temEanValido = !string.IsNullOrWhiteSpace(eanDaNota)
                    && eanDaNota.ToUpper() != "SEM GTIN";

                //Tentativa 1 --> por codigo de barras
                if (temEanValido)
                {
                    produtoEncontrado = produtosDoSistema
                        .FirstOrDefault(x => x.CodigoEan == eanDaNota);
                }

                //tenta por nome se não encontrar o código
                if (produtoEncontrado == null)
                {
                    produtoEncontrado = produtosDoSistema.FirstOrDefault(p =>
                        !string.IsNullOrEmpty(det.prod.xProd) &&
                        det.prod.xProd.Contains(p.Nome, StringComparison.OrdinalIgnoreCase));
                }

                itensDto.Add(new NotaFiscalItemDto
                {
                    Codigo = det.prod.cProd ?? string.Empty,
                    Ean = det.prod.cEAN,
                    Descricao = det.prod.xProd ?? string.Empty,
                    ProdutoSistemaId = produtoEncontrado?.Id,
                    ProdutoSistemaNome = produtoEncontrado?.Nome,
                    Quantidade = det.prod.qCom,
                    Unidade = det.prod.uCom ?? string.Empty,
                    ValorTotal = det.prod.vProd,
                    ValorUnitario = det.prod.vUnCom
                });
            }

            DateTime dataEmissao;

            if (infNFe.ide.dhEmi != DateTimeOffset.MinValue)
            {
                dataEmissao = infNFe.ide.dhEmi.DateTime;
            }
            else if (infNFe.ide.dEmi != DateTime.MinValue)
            {
                dataEmissao = infNFe.ide.dEmi;
            }
            else
            {
                throw new ApplicationException("Data de emissão não encontrada na NF-e.");
            }

            var notaFiscalDto = new NotaFiscalParsedDto
            {
                ChaveAcesso = infNFe.Id?.Replace("NFe", "") ?? string.Empty,
                Numero = infNFe.ide.nNF,
                TipoCarga = TipoCarga.Indefinido,
                Serie = infNFe.ide.serie.ToString(),
                DataEmissao = dataEmissao,
                EmitenteNome = infNFe.emit?.xNome ?? string.Empty,
                EmitenteCnpj = infNFe.emit?.CNPJ ?? string.Empty,
                Fornecedor = infNFe.emit?.xNome ?? string.Empty,
                DestinatarioNome = infNFe.dest?.xNome ?? string.Empty,
                DestinatarioCpfCnpj = infNFe.dest?.CNPJ ?? infNFe.dest?.CPF ?? string.Empty,
                ValorTotal = infNFe.total?.ICMSTot?.vNF ?? 0,
                PesoBruto = infNFe.transp?.vol?.FirstOrDefault()?.pesoB,
                VolumeQuantidade = (int?)infNFe.transp?.vol?.FirstOrDefault()?.qVol,
                PlacaVeiculo = infNFe.transp?.veicTransp?.placa ?? string.Empty,
                Itens = itensDto,
                ValidationWarnings = []
            };

            return notaFiscalDto;
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


            foreach (var item in dto.Itens)
            {
                await _learningService.TryLearnEanAsync(item, token);
            }

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
                    await _fornecedorRepo.Update(fornecedor.Id, fornecedor, token);
                }
            }

            var cnpjDestinatario = new string(dto.DestinatarioCpfCnpj
    .Where(char.IsDigit)
    .ToArray());

            var empresa = await _empresaRepo.GetByCnpj(cnpjDestinatario, token);

            if (empresa == null)
                throw new BusinessException("Empresa destinatária não cadastrada.");

            var nota = new NotaFiscal
            {
                ChaveAcesso = dto.ChaveAcesso,
                Numero = dto.Numero,
                Empresa = empresa,
                Serie = dto.Serie,
                DataEmissao = dto.DataEmissao,
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
                TipoCarga = dto.TipoCarga
            };

            _logger.LogInformation(
            "Fornecedor final vinculado | ID: {Id} | Nome: {Nome} | CNPJ: {Cnpj}",
            fornecedor.Id,
            fornecedor.Nome,
            fornecedor.Cnpj
            );

            var notaSalva = await _repo.SaveParsedNotaAsync(nota, token);
            await _repo.SaveChangesAsync(token);

            return new NotaFiscalParsedDto
            {
                ChaveAcesso = notaSalva.ChaveAcesso,
                Numero = notaSalva.Numero,
                Serie = notaSalva.Serie,
                DataEmissao = notaSalva.DataEmissao,
                EmitenteNome = notaSalva.EmitenteNome,
                EmitenteCnpj = notaSalva.EmitenteCnpj,
                DestinatarioNome = notaSalva.DestinatarioNome,
                DestinatarioCpfCnpj = notaSalva.DestinatarioCpfCnpj,
                Fornecedor = fornecedor.Nome,
                FornecedorId = notaSalva.FornecedorId,
                ValorTotal = notaSalva.ValorTotal,
                PesoBruto = notaSalva.PesoBruto,
                VolumeQuantidade = notaSalva.VolumeQuantidade,
                PlacaVeiculo = notaSalva.PlacaVeiculo!,
                TipoCarga = notaSalva.TipoCarga,
                Itens = notaSalva.Itens.Select(x => new NotaFiscalItemDto
                {
                    Codigo = x.Codigo,
                    Descricao = x.Descricao,
                    Quantidade = x.Quantidade,
                    Unidade = x.Unidade,
                    ValorUnitario = x.ValorUnitario,
                    ValorTotal = x.ValorTotal
                }).ToList() ?? new List<NotaFiscalItemDto>()
            };
        }
        public async Task<NotaFiscalParsedDto?> ObterPorChaveAsync
            (
                string chaveAcesso,
                CancellationToken token
            )
        {
            var nota = await _repo.ObterPorChaveAsync(chaveAcesso, token);

            if (nota == null)
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
                    Descricao = i.Descricao,
                    Quantidade = i.Quantidade,
                    Unidade = i.Unidade,
                    ValorUnitario = i.ValorUnitario,
                    ValorTotal = i.ValorTotal
                }).ToList() ?? new List<NotaFiscalItemDto>(),
                ValidationWarnings = new List<string>()
            };
        }
    }
}