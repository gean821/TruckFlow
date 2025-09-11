namespace TruckFlow.Infrastructure.Enums
{
    public enum TipoCarga
    {
        // Grãos
        Milho = 1,
        SojaCrua = 2,
        FareloDeSoja = 3,
        Trigo = 4,
        Arroz = 5,
        Sorgo = 6,

        // Fertilizantes e minerais
        Fosfato = 10,
        Calcário = 11,
        Potássio = 12,
        Ureia = 13,
        NPK = 14,

        // Rações e suplementos
        Sal = 20,
        SalMineral = 21,
        RacaoBovina = 22,
        RacaoSuina = 23,
        RacaoAves = 24,

        // Outros produtos
        Açucar = 30,
        Algodao = 31,
        Café = 32,
        CanaDeAcucar = 33,

        // Categoria genérica
        Outro = 99
    }
}
