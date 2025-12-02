using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Grade;
using TruckFlow.Domain.Dto.Produto;

namespace TruckFlow.Application.Interfaces
{
    public interface IGradeService
    {
        public Task<List<GradeResponse>> GetAll(CancellationToken cancellationToken = default);
        public Task<GradeResponse> GetById(Guid id, CancellationToken cancellationToken = default);
        public Task<GradeResponse> CreateGrade(GradeCreateDto grade, CancellationToken cancellationToken = default);
        public Task<GradeResponse> UpdateGrade(Guid id, GradeUpdateDto produto, CancellationToken cancellationToken = default);
        public Task DeleteGrade(Guid id, CancellationToken cancellationToken = default);
    }
}
