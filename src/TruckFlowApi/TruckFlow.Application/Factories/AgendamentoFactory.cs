using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public sealed class AgendamentoFactory
    {
        private readonly IFornecedorRepositorio _repo;
        //private readonly IUsuarioRepositorio _userRepo; //implementar após o repo de usuario..
        //private readonly INotaFiscalRepositorio _notaRepo //aqui verificar como vamos utilizar a notaFiscal no projeto.
        public AgendamentoFactory(IFornecedorRepositorio repo)
        {
            _repo = repo;
        }

       // public async Task<Agendamento> CreateAgendamentoFromDto(
         //   AgendamentoCreateDto dto,
           // CancellationToken token = default
            //)
        //{
            //var fornecedorEncontrado = await _repo.GetByNome(dto.Fornecedor, token);
            
            //return new Agendamento
            //{
               // VolumeCarga = dto.VolumeCarga,
              // TipoCarga = dto.TipoCarga,
               // Fornecedor = fornecedorEncontrado,
                //FornecedorId = fornecedorEncontrado.Id,
                //NotaFiscal = 
                //Usuario = 
                //Usuarioid = 
                //UnidadeEntrega..
            //};
        } 
    }
//}
