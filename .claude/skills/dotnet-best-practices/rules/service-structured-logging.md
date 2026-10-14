---
title: Log estruturado com template fixo, contexto de tenant e zero PII/segredo
impact: MEDIUM-HIGH
impactDescription: logs consultáveis por EmpresaId no Loki/Seq e sem achado LGPD no pen-test
tags: logging, serilog, lgpd, observability
---

## Log estruturado com template fixo, contexto de tenant e zero PII/segredo

Injete `ILogger<T>` (abstração Microsoft; Serilog é o provider). Use **message template** com placeholders PascalCase — nunca interpolação `$"..."`, que destrói a estrutura e impede busca por propriedade. Inclua `EmpresaId` e o ID de domínio em eventos relevantes: com 21 fábricas, "qual fábrica?" é sempre a primeira pergunta do incidente (ADR-0004 itens 9-10).

Níveis: `Information` = fato de negócio (agendamento criado, check-in); `Warning` = falha esperada/recuperável (validação, concorrência, SEFAZ fora); `Error` = falha inesperada com exceção. Request/duração já são logados por `RequestLoggingMiddleware` e `ActionLoggingFilter` — não duplique.

**Nunca logue:** senha, token/refresh token, `Authorization`, certificado/senha do A1, CPF completo, telefone, e-mail completo, localização precisa do motorista. Chave de acesso NF-e: só os últimos dígitos se necessário.

**Incorreto:**

```csharp
_logger.LogInformation($"Agendamento {vaga.Id} reservado por {motorista.Cpf} tel {motorista.Telefone}");
_logger.LogInformation("Login: {@Dto}", loginDto);    // destructuring loga a senha
```

**Correto:**

```csharp
_logger.LogInformation(
    "Agendamento avulso criado: {AgendamentoId} (empresa {EmpresaId}, status {Status})",
    vaga.Id, empresaId, vaga.StatusAgendamento);

_logger.LogWarning(ex, "Concorrência ao reservar {AgendamentoId} pelo motorista {MotoristaId}",
    vaga.Id, motoristaId);
```

Para hot paths, `LoggerMessage` source generator (`[LoggerMessage(...)]`) evita alocação. Ao implementar o `CorrelationIdMiddleware` (hoje vazio), use `ILogger.BeginScope` / `LogContext.PushProperty` com `TraceId`, `EmpresaId`, `UserId`.

**No TruckFlow:** templates em `AgendamentoAdminService` e workers seguem o padrão. Config Serilog em `TruckFlow/Configuration/SerilogConfiguration.cs` (sem enrichers ainda; Console listado 2x no appsettings).

Referência: [Serilog — Writing log events / message templates](https://github.com/serilog/serilog/wiki/Writing-Log-Events) · [Nicholas Blumhardt — structured logging](https://nblumhardt.com/) · [High-performance logging (LoggerMessage)](https://learn.microsoft.com/dotnet/core/extensions/high-performance-logging) · [LGPD — Lei 13.709/2018, art. 6º (minimização)](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm)
