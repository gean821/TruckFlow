using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Infrastructure.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IMotoristaService
    {
        public Task<Motorista> GetById(Guid id);
    }
}
