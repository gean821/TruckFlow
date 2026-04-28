namespace TruckFlow.Application.Sefaz
{
    public static class ChaveAcessoHelper
    {
        /// <summary>
        /// Extrai a sigla da UF a partir dos 2 primeiros dígitos (cUF) da chave de acesso da NF-e.
        /// </summary>
        /// <returns>Sigla (ex: "SP") ou null se cUF inválido.</returns>
        public static string? ExtrairUfEmitente(string chaveAcesso)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso) || chaveAcesso.Length < 2)
                return null;

            if (!int.TryParse(chaveAcesso.AsSpan(0, 2), out var cUF))
                return null;

            return cUF switch
            {
                11 => "RO", 12 => "AC", 13 => "AM", 14 => "RR", 15 => "PA",
                16 => "AP", 17 => "TO",
                21 => "MA", 22 => "PI", 23 => "CE", 24 => "RN", 25 => "PB",
                26 => "PE", 27 => "AL", 28 => "SE", 29 => "BA",
                31 => "MG", 32 => "ES", 33 => "RJ", 35 => "SP",
                41 => "PR", 42 => "SC", 43 => "RS",
                50 => "MS", 51 => "MT", 52 => "GO", 53 => "DF",
                _ => null
            };
        }
    }
}
