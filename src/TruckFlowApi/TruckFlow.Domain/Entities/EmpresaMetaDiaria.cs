using System.Text.Json;
using System.Text.Json.Nodes;

namespace TruckFlow.Domain.Entities
{
    public static class EmpresaMetaDiaria
    {
        private const string ChaveMetaDiariaKg = "metaDiariaKg";

        private static readonly JsonSerializerOptions SerializerOptions =
            new() { PropertyNameCaseInsensitive = true };

        private sealed class ConfiguracoesShape
        {
            public decimal? MetaDiariaKg { get; set; }
        }

        public static decimal? ResolveKg(string? configuracoesJson)
        {
            if (string.IsNullOrWhiteSpace(configuracoesJson))
                return null;

            try
            {
                var meta = JsonSerializer
                    .Deserialize<ConfiguracoesShape>(configuracoesJson, SerializerOptions)
                    ?.MetaDiariaKg;

                return meta > 0 ? meta : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string? Aplicar(string? configuracoesJson, decimal metaKg)
        {
            JsonObject configuracoes;
            try
            {
                configuracoes = string.IsNullOrWhiteSpace(configuracoesJson)
                    ? new JsonObject()
                    : JsonNode.Parse(configuracoesJson) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                configuracoes = new JsonObject();
            }

            foreach (var chave in configuracoes.Select(p => p.Key)
                .Where(k => string.Equals(k, ChaveMetaDiariaKg, StringComparison.OrdinalIgnoreCase))
                .ToList())
            {
                configuracoes.Remove(chave);
            }

            if (metaKg > 0)
                configuracoes[ChaveMetaDiariaKg] = metaKg;

            return configuracoes.Count == 0 ? null : configuracoes.ToJsonString();
        }
    }
}
