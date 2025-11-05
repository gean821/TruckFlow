using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Dto.Fornecedor;
using TruckFlow.Domain.Dto.ItensPlanejamento;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class RecebimentoFactory
    {

        private readonly IFornecedorRepositorio _repo;

        public RecebimentoFactory(
            IFornecedorRepositorio repo 
            )
        {
            _repo = repo;
        }

        public async Task<PlanejamentoRecebimento> CreateRecebimentoFromDto
                (
                    RecebimentoCreateDto dto,
                    CancellationToken token = default
                )
        {
            ArgumentNullException.ThrowIfNull(dto);

            var fornecedor = await _repo.GetById(dto.FornecedorId, token);

                var recebimento = new PlanejamentoRecebimento
                {
                    CreatedAt = DateTime.UtcNow,
                    DataInicio = dto.DataInicio,
                    Fornecedor = fornecedor,
                    FornecedorId = dto.FornecedorId,
                    StatusRecebimento = StatusRecebimento.EmAndamento,
                    ItemPlanejamentos = dto.ItensPlanejamento!.Select(x=> new ItemPlanejamento
                    {
                        PlanejamentoRecebimentoId = x.
                        ProdutoId = x.ProdutoId,
                        QuantidadeTotalPlanejada = x.QuantidadeTotalPlanejada,
                        CadenciaDiariaPlanejada = x.CadenciaDiariaPlanejada,
                    }).ToList()
                };

                return recebimento;
            }
        }
    }