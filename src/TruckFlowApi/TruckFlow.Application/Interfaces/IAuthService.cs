using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IAuthService
    {
        public Task<string> GenerateTokenAsync(Usuario usuario, CancellationToken token = default);
    }
}
