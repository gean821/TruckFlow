using System.Text.Json;

namespace TruckFlow.Domain.Entities
{
    /// <summary>
    /// Leitura tipada do campo Empresa.Configuracoes (jsonb) para a chave "authMethods".
    /// Decide quais métodos de login uma Empresa aceita (Local, EntraId, ambos).
    ///
    /// Default para qualquer Empresa que nunca configurou nada: só "Local" — garante
    /// retrocompatibilidade total com clientes que não usam Entra ID
    /// </summary>
    public static class EmpresaAuthMethods
    {
        public const string Local = "Local";
        public const string EntraId = "EntraId";

        private static readonly IReadOnlyCollection<string> Default = [Local];

        private static readonly JsonSerializerOptions SerializerOptions =
            new() { PropertyNameCaseInsensitive = true };

        private sealed class ConfiguracoesShape
        {
            public string[]? AuthMethods { get; set; }
        }

        public static IReadOnlyCollection<string> Resolve(string? configuracoesJson)
        {
            if (string.IsNullOrWhiteSpace(configuracoesJson))
                return Default;

            try
            {
                var shape = JsonSerializer.Deserialize<ConfiguracoesShape>(
                    configuracoesJson,
                    SerializerOptions);

                if (shape?.AuthMethods is null || shape.AuthMethods.Length == 0)
                    return Default;

                return shape.AuthMethods;
            }
            catch (JsonException)
            {
                return Default;
            }
        }

        public static bool Permite(string? configuracoesJson, string metodo) =>
            Resolve(configuracoesJson).Contains(metodo, StringComparer.OrdinalIgnoreCase);
    }
}
