using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IGradeRepositorio
    {
        public Task<Grade> GetById(Guid id, CancellationToken token = default);
        public Task<List<Grade>> GetAll(CancellationToken token = default);
        public Task<Grade> CreateGrade(Grade grade, CancellationToken token = default);
        public Task<Grade> Update(Grade grade, CancellationToken token = default);
        public Task Delete(Grade grade, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
        public Task<bool>? CheckAppointmentExistence(Guid id, CancellationToken token = default);
    }
}
