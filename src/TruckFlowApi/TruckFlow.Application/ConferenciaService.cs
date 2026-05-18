using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Conferencia;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class ConferenciaService : IConferenciaService
    {
        private const int TopSugestoes = 3;
        private const int MinTokenLength = 3;

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "de", "da", "do", "das", "dos", "para", "pra", "com", "sem", "em",
            "no", "na", "nos", "nas", "kg", "und", "un", "lt", "ml", "gr", "g",
            "tipo", "ref", "cod"
        };

        private readonly IAgendamentoRepositorio _agendamentoRepo;
        private readonly INotaFiscalItemRepositorio _itemRepo;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly IProdutoFornecedorRepositorio _produtoFornecedorRepo;
        private readonly IEmpresaContext _empresaContext;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ConferenciaService> _logger;

        public ConferenciaService(
            IAgendamentoRepositorio agendamentoRepo,
            INotaFiscalItemRepositorio itemRepo,
            IProdutoRepositorio produtoRepo,
            IProdutoFornecedorRepositorio produtoFornecedorRepo,
            IEmpresaContext empresaContext,
            ICurrentUserService currentUser,
            ILogger<ConferenciaService> logger)
        {
            _agendamentoRepo = agendamentoRepo;
            _itemRepo = itemRepo;
            _produtoRepo = produtoRepo;
            _produtoFornecedorRepo = produtoFornecedorRepo;
            _empresaContext = empresaContext;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<ConferenciaResponseDto> GetByAgendamentoIdAsync(
            Guid agendamentoId,
            CancellationToken token = default)
        {
            var agendamento = await _agendamentoRepo.GetById(agendamentoId, token)
                ?? throw new NotFoundException("Agendamento não encontrado.");

            var notaFiscal = agendamento.NotaFiscal;

            if (notaFiscal is null)
            {
                return new ConferenciaResponseDto
                {
                    AgendamentoId = agendamentoId,
                    Itens = new List<ConferenciaItemDto>()
                };
            }

            var itens = notaFiscal.Itens?.ToList() ?? new List<NotaFiscalItem>();

            var produtosCatalogo = await _produtoRepo.GetAll(token);

            var itensDto = itens.Select(item =>
            {
                var dto = new ConferenciaItemDto
                {
                    Id = item.Id,
                    Codigo = item.Codigo,
                    Ean = item.Ean,
                    Descricao = item.Descricao,
                    Quantidade = item.Quantidade,
                    Unidade = item.Unidade,
                    Status = item.Status,
                    OrigemMatch = item.OrigemMatch,
                    ProdutoId = item.ProdutoId,
                    ProdutoNome = item.Produto?.Nome,
                    MatchadoEm = item.MatchadoEm,
                    MatchadoPor = item.MatchadoPor
                };

                if (item.Status == NotaFiscalItemStatus.PendenteRevisao)
                {
                    dto.Sugestoes = CalcularSugestoes(item.Descricao, produtosCatalogo);
                }

                return dto;
            }).ToList();

            return new ConferenciaResponseDto
            {
                AgendamentoId = agendamentoId,
                NotaFiscalId = notaFiscal.Id,
                ChaveAcesso = notaFiscal.ChaveAcesso,
                FornecedorId = notaFiscal.FornecedorId,
                FornecedorNome = notaFiscal.Fornecedor?.Nome,
                Itens = itensDto,
                PendentesCount = itensDto.Count(x => x.Status == NotaFiscalItemStatus.PendenteRevisao),
                MatchedCount = itensDto.Count(x => x.Status == NotaFiscalItemStatus.Matched)
            };
        }

        public async Task<ConferenciaItemDto> MatchItemAsync(
            Guid itemId,
            Guid produtoId,
            CancellationToken token = default)
        {
            var item = await _itemRepo.GetByIdAsync(itemId, token)
                ?? throw new NotFoundException("Item da nota fiscal não encontrado.");

            var produto = await _produtoRepo.GetById(produtoId, token)
                ?? throw new NotFoundException("Produto não encontrado no catálogo.");

            if (produto.EmpresaId != item.EmpresaId)
            {
                throw new BusinessException(
                    "Produto pertence a outra empresa. Match cross-tenant não permitido.");
            }

            var fornecedorId = item.NotaFiscal?.FornecedorId
                ?? throw new BusinessException(
                    "Nota fiscal do item não tem fornecedor identificado. Impossível matchar.");

            var nowUtc = DateTime.UtcNow;

            item.ProdutoId = produto.Id;
            item.Status = NotaFiscalItemStatus.Matched;
            item.OrigemMatch = OrigemMatchProduto.AdminManual;
            item.MatchadoEm = nowUtc;
            item.MatchadoPor = _currentUser.UserIdOrNull;
            item.Produto = produto;

            await _produtoFornecedorRepo.UpsertMapping(
                empresaId: item.EmpresaId,
                fornecedorId: fornecedorId,
                produtoId: produto.Id,
                codigoFornecedor: item.Codigo,
                eanFornecedor: item.Ean,
                token);

            await _itemRepo.SaveChangesAsync(token);

            _logger.LogInformation(
                "Item da NF matchado manualmente. ItemId={ItemId} ProdutoId={ProdutoId} FornecedorId={FornecedorId} UserId={UserId}",
                item.Id, produto.Id, fornecedorId, _currentUser.UserIdOrNull);

            return new ConferenciaItemDto
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Ean = item.Ean,
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                Unidade = item.Unidade,
                Status = item.Status,
                OrigemMatch = item.OrigemMatch,
                ProdutoId = item.ProdutoId,
                ProdutoNome = produto.Nome,
                MatchadoEm = item.MatchadoEm,
                MatchadoPor = item.MatchadoPor
            };
        }

        private static List<ProdutoSugestaoDto> CalcularSugestoes(
            string descricaoNota,
            List<Produto> produtos)
        {
            var tokensNota = Tokenize(descricaoNota);
            if (tokensNota.Count == 0 || produtos.Count == 0)
            {
                return new List<ProdutoSugestaoDto>();
            }

            return produtos
                .Select(p => new
                {
                    Produto = p,
                    Score = JaccardScore(tokensNota, Tokenize(p.Nome))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(TopSugestoes)
                .Select(x => new ProdutoSugestaoDto(x.Produto.Id, x.Produto.Nome, Math.Round(x.Score, 3)))
                .ToList();
        }

        private static HashSet<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new HashSet<string>();
            }

            var normalized = RemoveDiacritics(text).ToLowerInvariant();
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in normalized.Split(
                new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                if (raw.Length < MinTokenLength) {
                    continue;
                }

                if (StopWords.Contains(raw)) {
                    continue;
                } 
                tokens.Add(raw);
            }

            return tokens;
        }

        private static double JaccardScore(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 || b.Count == 0) return 0;
            var intersection = a.Intersect(b).Count();
            if (intersection == 0) return 0;
            var union = a.Count + b.Count - intersection;
            return (double)intersection / union;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}