---
title: Variação por fábrica em Empresa.Configuracoes (jsonb) — tipada no C#, nunca if por tenant
impact: MEDIUM-HIGH
impactDescription: 21 fábricas com regras diferentes sem código condicional nem migration por variação
tags: jsonb, configuration, multi-tenant, owned-json, schema
---

## Variação por fábrica em Empresa.Configuracoes (jsonb) — tipada no C#, nunca if por tenant

ADR-0001: cada fábrica Aurora opera com processos parcialmente diferentes (escala de descarga, janelas, métodos de login). Isso mora em `Empresa.Configuracoes` (`jsonb`), não em `if (empresaId == ...)` nem em colunas novas para cada variação. Mas `jsonb` como `string?` cru espalha parse e chave mágica pelo código.

**Incorreto:**

```csharp
if (empresa.Cnpj == "83.310.441/0001-17") { janelaMinutos = 45; }          // regra hardcoded por fábrica
var cfg = JsonDocument.Parse(empresa.Configuracoes!);                        // parse ad-hoc
var metodos = cfg.RootElement.GetProperty("authMethods");                   // chave mágica
```

**Correto (tipo C# + mapeamento + default):**

```csharp
public sealed class EmpresaConfiguracoes
{
    public string[] AuthMethods { get; set; } = ["Local"];      // ["Local","EntraId"]
    public int JanelaDescargaMinutos { get; set; } = 60;
    public int ToleranciaAtrasoMinutos { get; set; } = 15;
}
// Configuração EF 8+/Npgsql: owned type como JSON
builder.OwnsOne(x => x.Configuracoes, c => c.ToJson());
// ou, mantendo string? por compatibilidade, um ValueConverter com JsonSerializer + defaults
```

```sql
-- consultar/filtrar (índice GIN se for usado em WHERE — ver advanced-jsonb-indexing)
SELECT "Id" FROM "Empresa" WHERE "Configuracoes" -> 'AuthMethods' ? 'EntraId';
-- atualização parcial sem sobrescrever o resto
UPDATE "Empresa" SET "Configuracoes" = jsonb_set("Configuracoes", '{JanelaDescargaMinutos}', '45')
WHERE "Id" = $1;
```

Regras: chave nova sempre com **default** no C# (empresas antigas não têm a chave); nada de dado relacional (lista de docas, produtos) dentro do jsonb — isso é tabela; mudança de config é auditada (`AuditSaveChangesInterceptor` já pega `Modified`).

**No TruckFlow:** `Empresa.Configuracoes` é `string?` com `HasColumnType("jsonb")` em `EmpresaConfiguracao.cs:73`; `authMethods` do Entra ID por tenant (`Docs/entra-id-integracao-backlog.md`) já depende disso.

Referência: [JSON Types](https://www.postgresql.org/docs/16/datatype-json.html) · [Npgsql — JSON mapping](https://www.npgsql.org/efcore/mapping/json.html) · [EF Core — JSON columns](https://learn.microsoft.com/ef/core/what-is-new/ef-core-7.0/whatsnew#json-columns)
