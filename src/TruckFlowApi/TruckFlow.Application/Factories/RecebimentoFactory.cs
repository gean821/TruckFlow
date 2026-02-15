
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class RecebimentoFactory
    {
        private readonly IFornecedorRepositorio _fornecedorRepo;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly IEmpresaRepositorio _empresaRepo;

        public RecebimentoFactory(
            IFornecedorRepositorio fornecedorRepo,
            IProdutoRepositorio produtoRepo,
            IEmpresaRepositorio empresaRepo
            )
        {
            _fornecedorRepo = fornecedorRepo;
            _produtoRepo = produtoRepo;
            _empresaRepo = empresaRepo;
        }

        public async Task<PlanejamentoRecebimento> CreateRecebimentoFromDto(
            RecebimentoCreateDto dto,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var fornecedor = await _fornecedorRepo.GetById(dto.FornecedorId, token)
                ?? throw new NotFoundException("Fornecedor não encontrado.");

            var produtoIds = dto.ItensPlanejamento!.Select(x => x.ProdutoId).ToList();
            var produtos = await _produtoRepo.GetByIdsAsync(produtoIds, token);

            if (produtos.Count != produtoIds.Count)
                throw new NotFoundException("Um ou mais produtos informados não foram encontrados.");

            var produtoDicionario = produtos.ToDictionary(p => p.Id);

            var empresa = await _empresaRepo.GetById(dto.EmpresaId, token)
                ?? throw new NotFoundException("Empresa não encontrada.");

            var recebimento = new PlanejamentoRecebimento
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                DataInicio = dto.DataInicio,
                Fornecedor = fornecedor,
                FornecedorId = fornecedor.Id,
                StatusRecebimento = StatusRecebimento.Planejado,
                Empresa = empresa
            };

            recebimento.ItemPlanejamentos = dto.ItensPlanejamento!.Select(itemDto => new ItemPlanejamento
            {
                Id = Guid.NewGuid(),
                PlanejamentoRecebimento = recebimento,
                PlanejamentoRecebimentoId = recebimento.Id,
                ProdutoId = itemDto.ProdutoId,
                Produto = produtoDicionario[itemDto.ProdutoId],
                QuantidadeTotalPlanejada = itemDto.QuantidadeTotalPlanejada,
                CadenciaDiariaPlanejada = itemDto.CadenciaDiariaPlanejada,
                QuantidadeTotalRecebida = 0,
                CreatedAt = DateTime.UtcNow,
                Empresa = empresa
            }).ToList();

            return recebimento;
        }
    }
}
