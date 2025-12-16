using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Validators.EanValidators;
using TruckFlow.Domain.Dto.NotaFiscal;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class ProdutoLearningService(IProdutoRepositorio repo)
    {
        private readonly IProdutoRepositorio _repo = repo;

        public async Task TryLearnEanAsync(NotaFiscalItemDto item, CancellationToken token)
        {
            if (item.ProdutoSistemaId == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Ean))
            {
                return;
            }

            var ean = item.Ean.Trim().ToUpper();

            if (ean == "SEM GTIN" || !EanValidator.IsValid(ean))
            {
                return;
            }

            var produto = await _repo.GetById(item.ProdutoSistemaId.Value, token);
            
            if (produto == null)
            {
                return;
            }

            // já tem EAN → não aprender novamente
            if (!string.IsNullOrEmpty(produto.CodigoEan))
            { 
                return;
            }

            produto.CodigoEan = ean;

            await _repo.UpdateProduto(produto.Id, produto, token);
        }
    }
}

