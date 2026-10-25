namespace TruckFlow.Application.AppMobile
{
    public sealed class AppMobileOptions
    {
        public const string SectionName = "AppMobile";

        public string VersaoMinimaAndroid { get; set; } = "1.0.0";

        public string VersaoMinimaIos { get; set; } = "1.0.0";

        public string? UrlLojaAndroid { get; set; }

        public string? UrlLojaIos { get; set; }
    }
}
