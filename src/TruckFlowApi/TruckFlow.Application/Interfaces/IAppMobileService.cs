using TruckFlow.Domain.Dto.AppMobile;

namespace TruckFlow.Application.Interfaces
{
    public interface IAppMobileService
    {
        VersaoMinimaResponseDto GetVersaoMinima();
    }
}
