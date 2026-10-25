namespace TruckFlow.Domain.Dto.AppMobile
{
    public sealed class VersaoMinimaResponseDto
    {
        public required string Android { get; init; }
        public required string Ios { get; init; }
        public string? UrlLojaAndroid { get; init; }
        public string? UrlLojaIos { get; init; }
    }
}
