using Microsoft.Extensions.Options;
using TruckFlow.Application.AppMobile;

namespace TruckFlow.Test.Application;

public class AppMobileServiceTests
{
    [Fact]
    public void GetVersaoMinima_DevolveVersoesEUrlsDaConfig()
    {
        var service = new AppMobileService(Options.Create(new AppMobileOptions
        {
            VersaoMinimaAndroid = "1.2.0",
            VersaoMinimaIos = "1.1.0",
            UrlLojaAndroid = "https://play.google.com/store/apps/details?id=x",
            UrlLojaIos = "  ",
        }));

        var dto = service.GetVersaoMinima();

        Assert.Equal("1.2.0", dto.Android);
        Assert.Equal("1.1.0", dto.Ios);
        Assert.Equal("https://play.google.com/store/apps/details?id=x", dto.UrlLojaAndroid);
        Assert.Null(dto.UrlLojaIos);
    }

    [Fact]
    public void GetVersaoMinima_SemConfig_UsaPadrao100()
    {
        var dto = new AppMobileService(Options.Create(new AppMobileOptions())).GetVersaoMinima();

        Assert.Equal("1.0.0", dto.Android);
        Assert.Equal("1.0.0", dto.Ios);
        Assert.Null(dto.UrlLojaAndroid);
    }
}
