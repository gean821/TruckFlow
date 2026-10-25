using Microsoft.Extensions.Options;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.AppMobile;

namespace TruckFlow.Application.AppMobile
{
    public class AppMobileService(IOptions<AppMobileOptions> options) : IAppMobileService
    {
        private readonly AppMobileOptions _options = options.Value;

        public VersaoMinimaResponseDto GetVersaoMinima()
        {
            return new VersaoMinimaResponseDto
            {
                Android = _options.VersaoMinimaAndroid,
                Ios = _options.VersaoMinimaIos,
                UrlLojaAndroid = string.IsNullOrWhiteSpace(_options.UrlLojaAndroid) ? null : _options.UrlLojaAndroid,
                UrlLojaIos = string.IsNullOrWhiteSpace(_options.UrlLojaIos) ? null : _options.UrlLojaIos,
            };
        }
    }
}
