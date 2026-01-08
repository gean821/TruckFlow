using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.User.Motorista;
using TruckFlow.Domain.Dto.Veiculo;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Interfaces;

public interface IMotoristaService
{
    public Task<UserMotoristaResponseDto> GetMe(Guid usuarioId, CancellationToken token = default);
    public Task<List<VeiculoResponse>> GetMeusVeiculos(Guid usuarioId, CancellationToken token = default);
}
