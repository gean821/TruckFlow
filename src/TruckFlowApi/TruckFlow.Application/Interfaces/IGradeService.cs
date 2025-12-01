using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Grade;

namespace TruckFlow.Application.Interfaces
{
    public interface IGradeService
    {
        public Task<GradeResponse> GetById(Guid id, CancellationToken token = default);
        public Task<List<GradeResponse>> GetAll(CancellationToken token = default);
        public Task<GradeResponse> CreateGrade
            (
                GradeCreateDto fornecedor,
                CancellationToken token = default
            );
        public Task<GradeResponse> UpdateGrade
            (
                Guid id,
                GradeUpdateDto grade,
                CancellationToken token = default
            );
        public Task DeleteGrade(Guid id, CancellationToken token = default);
        public Task<GradeResponse> AddProdutoToGradeAsync
           (
               Guid gradeId,
               Guid produtoId,
               CancellationToken token = default
           );
        public Task<GradeResponse> AddFornecedorToGradeAsync
           (
               Guid gradeId,
               Guid fornecedorId,
               CancellationToken token = default
           );
        public Task<GradeResponse> DeleteProdutoFromGradeAsync
            (
                Guid gradeId,
                Guid produtoId,
                CancellationToken token = default
            );
        public Task<GradeResponse> DeleteFornecedorFromGradeAsync
            (
                Guid gradeId,
                Guid fornecedorId,
                CancellationToken token = default
            );
    }
}
