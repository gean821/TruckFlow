using System.Text.Json;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Audit;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class AuditLogService : IAuditLogService
    {
        private static readonly Dictionary<string, string> IdFieldToEntity = new(StringComparer.Ordinal)
        {
            { "EmpresaId", "Empresa" },
            { "UnidadeEntregaId", "UnidadeEntrega" },
            { "LocalDescargaId", "LocalDescarga" },
            { "ProdutoId", "Produto" },
            { "FornecedorId", "Fornecedor" },
            { "UsuarioId", "Usuario" },
            { "MotoristaId", "Usuario" },
            { "CreatedBy", "Usuario" },
            { "UpdatedBy", "Usuario" }
        };

        private readonly IAuditLogRepositorio _repo;

        public AuditLogService(IAuditLogRepositorio repo)
        {
            _repo = repo;
        }

        public async Task<PagedResponse<AuditLogResponseDto>> GetPaged(
            AuditLogListQueryDto query,
            CancellationToken token = default)
        {
            var paged = await _repo.GetPagedAsync(query, token);

            var authorIds = paged.Items
                .Where(x => x.UserId.HasValue)
                .Select(x => x.UserId!.Value)
                .Distinct()
                .ToList();

            var userDisplayNames = await _repo.GetUserDisplayNamesAsync(authorIds, token);

            var parsedChanges = new Dictionary<Guid, JsonElement?>();
            var idsByEntity = new Dictionary<string, HashSet<Guid>>(StringComparer.Ordinal);

            foreach (var entry in paged.Items)
            {
                var parsed = ParseChanges(entry.ChangesJson);
                parsedChanges[entry.Id] = parsed;

                if (parsed.HasValue)
                {
                    CollectReferencedIds(parsed.Value, idsByEntity);
                }
            }

            var labelsByEntity = new Dictionary<string, IReadOnlyDictionary<Guid, string>>(StringComparer.Ordinal);
            foreach (var (entityName, ids) in idsByEntity)
            {
                labelsByEntity[entityName] = await _repo.GetEntityLabelsAsync(entityName, ids, token);
            }

            return new PagedResponse<AuditLogResponseDto>
            {
                Items = paged.Items
                    .Select(x => MapToResponse(x, userDisplayNames, parsedChanges, labelsByEntity))
                    .ToList(),
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                TotalPages = paged.TotalPages
            };
        }

        private static AuditLogResponseDto MapToResponse(
            AuditLog entry,
            IReadOnlyDictionary<Guid, string> userDisplayNames,
            Dictionary<Guid, JsonElement?> parsedChanges,
            Dictionary<string, IReadOnlyDictionary<Guid, string>> labelsByEntity)
        {
            parsedChanges.TryGetValue(entry.Id, out var changesElement);

            Dictionary<string, Dictionary<string, string>>? labelsForEvent = null;
            if (changesElement.HasValue)
            {
                labelsForEvent = BuildLabelsForEvent(changesElement.Value, labelsByEntity);
            }

            return new AuditLogResponseDto
            {
                Id = entry.Id,
                EntityName = entry.EntityName,
                EntityId = entry.EntityId,
                Action = entry.Action.ToString(),
                UserId = entry.UserId,
                UserName = entry.UserId.HasValue && userDisplayNames.TryGetValue(entry.UserId.Value, out var name)
                    ? name
                    : null,
                Timestamp = entry.Timestamp,
                Changes = changesElement,
                Labels = labelsForEvent,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent
            };
        }

        private static JsonElement? ParseChanges(string? changesJson)
        {
            if (string.IsNullOrWhiteSpace(changesJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<JsonElement>(changesJson);
            }
            catch
            {
                return null;
            }
        }

        private static void CollectReferencedIds(
            JsonElement changes,
            Dictionary<string, HashSet<Guid>> idsByEntity)
        {
            if (changes.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var prop in changes.EnumerateObject())
            {
                if (!IdFieldToEntity.TryGetValue(prop.Name, out var targetEntity))
                {
                    continue;
                }

                if (prop.Value.ValueKind == JsonValueKind.Object &&
                    prop.Value.TryGetProperty("from", out var from) &&
                    prop.Value.TryGetProperty("to", out var to))
                {
                    TryAddGuid(from, targetEntity, idsByEntity);
                    TryAddGuid(to, targetEntity, idsByEntity);
                }
                else
                {
                    TryAddGuid(prop.Value, targetEntity, idsByEntity);
                }
            }
        }

        private static void TryAddGuid(
            JsonElement el,
            string targetEntity,
            Dictionary<string, HashSet<Guid>> idsByEntity)
        {
            if (el.ValueKind != JsonValueKind.String)
            {
                return;
            }

            if (!Guid.TryParse(el.GetString(), out var id))
            {
                return;
            }

            if (!idsByEntity.TryGetValue(targetEntity, out var set))
            {
                set = new HashSet<Guid>();
                idsByEntity[targetEntity] = set;
            }

            set.Add(id);
        }

        private static Dictionary<string, Dictionary<string, string>>? BuildLabelsForEvent(
            JsonElement changes,
            Dictionary<string, IReadOnlyDictionary<Guid, string>> labelsByEntity)
        {
            if (changes.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            foreach (var prop in changes.EnumerateObject())
            {
                if (!IdFieldToEntity.TryGetValue(prop.Name, out var targetEntity))
                {
                    continue;
                }

                if (!labelsByEntity.TryGetValue(targetEntity, out var entityLabels))
                {
                    continue;
                }

                var fieldDict = new Dictionary<string, string>(StringComparer.Ordinal);

                if (prop.Value.ValueKind == JsonValueKind.Object &&
                    prop.Value.TryGetProperty("from", out var from) &&
                    prop.Value.TryGetProperty("to", out var to))
                {
                    TryAddLabel(from, entityLabels, fieldDict);
                    TryAddLabel(to, entityLabels, fieldDict);
                }
                else
                {
                    TryAddLabel(prop.Value, entityLabels, fieldDict);
                }

                if (fieldDict.Count > 0)
                {
                    result[prop.Name] = fieldDict;
                }
            }

            return result.Count > 0 ? result : null;
        }

        private static void TryAddLabel(
            JsonElement el,
            IReadOnlyDictionary<Guid, string> entityLabels,
            Dictionary<string, string> fieldDict)
        {
            if (el.ValueKind != JsonValueKind.String)
            {
                return;
            }

            var raw = el.GetString();
            if (raw is null)
            {
                return;
            }

            if (!Guid.TryParse(raw, out var id))
            {
                return;
            }

            if (entityLabels.TryGetValue(id, out var label))
            {
                fieldDict[raw] = label;
            }
        }
    }
}
