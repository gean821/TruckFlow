# Rastreamento de motorista sem TimescaleDB/PostGIS — revisão e backlog de tasks

> Origem: revisão da decisão de infraestrutura do `Docs/adr/0003-design-tracking-motorista.md`, motivada por dois fatos novos que não existiam quando o ADR-0003 foi escrito:
> 1. A Aurora exige explicitamente hospedagem de dados no Brasil (levantado na reunião de alinhamento de TI de 2026-09-14).
> 2. O rastreamento de motorista é um **plus da plataforma — nunca foi pedido formalmente pela Aurora**, não é item contratual.
>
> **Não faz sentido deixar uma feature bônus, ainda não construída, restringir a escolha de provedor de nuvem pra um requisito que esse sim é inegociável.**

---

## 0. O que muda em relação ao ADR-0003 original

| Peça | ADR-0003 original | Revisão (este documento) |
|---|---|---|
| Extensão de banco | TimescaleDB (hypertable + compressão + retenção automática) | **Nenhuma** — Postgres puro |
| Cálculo de geofencing (raio de 200m) | Não especificado explicitamente, implícito no TimescaleDB/PostGIS | **Fórmula de Haversine** em SQL comum ou C#, sem índice espacial |
| Retenção de 90 dias | `add_retention_policy` do TimescaleDB (2 linhas) | **Particionamento nativo por mês** (`PARTITION BY RANGE`) + job agendado dropando partição antiga |
| Compressão de dados antigos | `add_compression_policy` do TimescaleDB | **Não necessária** — volume em regime estável (~90 dias de retenção) fica em torno de 100M linhas / 5-10GB, não justifica a complexidade adicional |
| Provedores de nuvem compatíveis no Brasil | Restrito a quem suporta a extensão (Aiven, Supabase, Timescale Cloud/Tiger Cloud) | **Qualquer um** — AWS RDS, Azure, GCP, Aiven, Supabase, todos suportam Postgres puro sem exigir extensão nenhuma |

**Por que isso é seguro:** o volume real (ADR-0001: ~400 motoristas × 2880 pings/dia, retenção 90 dias) é modesto pro que Postgres puro aguenta com particionamento nativo. TimescaleDB resolve um problema de escala que este produto não tem ainda. Se um dia precisar de geofencing complexo (polígonos, múltiplas fábricas próximas, busca por proximidade entre muitos pontos), aí sim vale reavaliar PostGIS — que é universalmente suportado em qualquer provedor brasileiro, então adiar essa decisão não fecha porta nenhuma.

**Nota de escopo:** nenhuma das tabelas abaixo existe hoje (`MotoristaPosicaoAtual`/`MotoristaPosicaoHistorico`) — confirmado que não há migration criada ainda. O rastreamento de motorista, como um todo, ainda é só design, não implementação. Este backlog assume que a implementação vai come çar do zero seguindo já o modelo revisado (sem retrabalho de migração).

---

## 1. Modelo de dados

### `TRACK-01` — Tabela `MotoristaPosicaoAtual` (última posição, upsert)
- **Prioridade:** P2 (plus, não bloqueia piloto) · **Repo:** backend
- **Onde:** nova entidade `TruckFlow.Domain/Entities/MotoristaPosicaoAtual.cs` + migration em `TruckFlowApi.Infra/Migrations/`
- **Como implementar:** tabela simples, 1 linha por motorista, sem particionamento (é sempre pequena — no máximo 1 linha por motorista ativo):
  ```csharp
  public class MotoristaPosicaoAtual
  {
      public Guid MotoristaId { get; set; }   // PK
      public Guid AgendamentoId { get; set; }
      public Guid EmpresaId { get; set; }     // multi-tenant scope
      public double Latitude { get; set; }
      public double Longitude { get; set; }
      public float Accuracy { get; set; }
      public float? Velocidade { get; set; }
      public float? Heading { get; set; }
      public DateTime CapturadoEm { get; set; }
      public DateTime RecebidoEm { get; set; }
  }
  ```
  Upsert via `INSERT ... ON CONFLICT (MotoristaId) DO UPDATE` (raw SQL — hot path, sem EF tracking, conforme já indicado no ADR-0003 original).
  Índice em `(EmpresaId, AgendamentoId)`.
- **Critério de aceite:** batch de posições do endpoint `/v1/motorista/posicao` atualiza essa tabela em uma única operação atômica.
- **Esforço:** 0,5 dia.

### `TRACK-02` — Tabela `MotoristaPosicaoHistorico` com particionamento nativo por mês
- **Prioridade:** P2 · **Repo:** backend
- **Onde:** nova entidade + migration com DDL de particionamento (particionamento declarativo não é totalmente mapeável via EF Core Fluent API — parte da migration precisa de SQL raw)
- **Como implementar:**
  ```sql
  CREATE TABLE "MotoristaPosicaoHistorico" (
      "MotoristaId" uuid NOT NULL,
      "AgendamentoId" uuid NOT NULL,
      "EmpresaId" uuid NOT NULL,
      "Latitude" double precision NOT NULL,
      "Longitude" double precision NOT NULL,
      "Accuracy" real NOT NULL,
      "Velocidade" real,
      "CapturadoEm" timestamptz NOT NULL
  ) PARTITION BY RANGE ("CapturadoEm");

  -- uma partição por mês, criada com antecedência (ver TRACK-04)
  CREATE TABLE "MotoristaPosicaoHistorico_2026_09"
      PARTITION OF "MotoristaPosicaoHistorico"
      FOR VALUES FROM ('2026-09-01') TO ('2026-10-01');
  ```
  Índice em `(EmpresaId, CapturadoEm DESC)` em cada partição (ou índice criado na tabela-mãe, que o Postgres propaga automaticamente pras partições filhas).
  Inserção em lote via `COPY` ou `INSERT` em batch (não EF tracking), igual já indicado no ADR-0003 original.
- **Critério de aceite:** inserção de posições cai automaticamente na partição correta pelo `CapturadoEm`; consulta filtrando por período recente usa só a(s) partição(ões) relevante(s) (confirmar via `EXPLAIN`).
- **Esforço:** 1 dia (a parte de DDL raw SQL na migration exige mais atenção que uma migration EF comum).

### `TRACK-03` — Job de manutenção de partições (criar novas, dropar antigas)
- **Prioridade:** P2 · **Repo:** backend
- **Onde:** novo `IHostedService`, mesmo padrão já usado pelo `AgendamentoExpirationService`
- **Como implementar:** job mensal (ou diário, de baixo custo) que:
  1. Garante que a partição do mês seguinte já existe (`CREATE TABLE ... PARTITION OF ...` para o próximo mês, se ainda não criada).
  2. Dropa partições cujo intervalo de datas é mais antigo que 90 dias (`DROP TABLE "MotoristaPosicaoHistorico_AAAA_MM"`) — operação instantânea no Postgres, muito mais barata que `DELETE` em massa.
- **Critério de aceite:** rodar o job manualmente em ambiente de teste cria a partição do próximo mês e remove partições vencidas, sem downtime nem lock longo na tabela-mãe.
- **Esforço:** 1 dia.

---

## 2. Cálculo de geofencing sem PostGIS

### `TRACK-04` — Distância via Haversine (raio de 200m da fábrica)
- **Prioridade:** P2 · **Repo:** backend
- **Onde:** `TruckFlow.Application` — service que processa o batch de posições recebido em `/v1/motorista/posicao`
- **Como implementar:** fórmula de Haversine direto em C# (não precisa de SQL nem de índice espacial — é um cálculo O(1) entre dois pontos conhecidos, não uma busca espacial):
  ```csharp
  static double DistanciaMetros(double lat1, double lon1, double lat2, double lon2)
  {
      const double R = 6371000; // raio da Terra em metros
      var dLat = (lat2 - lat1) * Math.PI / 180;
      var dLon = (lon2 - lon1) * Math.PI / 180;
      var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
              Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
              Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
      return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
  }
  ```
  Compara contra `Empresa.Latitude`/`Empresa.Longitude` (já existentes no domínio). Se `DistanciaMetros(...) <= 200`, aplica a regra de geofencing do ADR-0003 (parar de processar/armazenar pings, conforme já especificado).
- **Critério de aceite:** motorista simulado a 199m do ponto da fábrica é tratado como "dentro do raio"; a 201m, fora. Comparar resultado com uma ferramenta de referência (ex.: calculadora de distância geográfica online) pra validar a margem de erro (Haversine tem erro desprezível em distâncias curtas como essa).
- **Esforço:** 0,5 dia (é lógica simples, o esforço maior é escrever os testes de borda).

---

## 3. Desbloqueio da decisão de hospedagem

### `TRACK-05` — Remover TimescaleDB/PostGIS como critério de escolha de provedor
- **Prioridade:** P1 (destrava outras decisões pendentes) · **Repo:** infra/decisão
- **O que fazer:** com este backlog, a escolha de provedor de nuvem para hospedagem no Brasil deixa de precisar filtrar por suporte a extensão de banco. Qualquer provedor com região no Brasil (AWS `sa-east-1`, Azure Brazil South, GCP `southamerica-east1`, Aiven, Supabase, etc.) atende.
- **Esforço:** nenhum esforço de engenharia — é só remover essa restrição da lista de critérios já em avaliação.

---

## 4. Documentação

### `TRACK-06` — Atualizar `Docs/adr/0003-design-tracking-motorista.md`
- **Prioridade:** P2 · **Repo:** backend
- **O que fazer:** registrar esta revisão no ADR-0003 (seção "Alternativas consideradas" já tinha rejeitado "Postgres puro com particionamento manual" como A2 — atualizar pra refletir que a decisão foi revertida, com o motivo: requisito de hospedagem Brasil da Aurora + volume real não justifica a complexidade do TimescaleDB pra uma feature que é plus, não contratual). Manter o ADR original como histórico (não reescrever, adicionar uma nota de revisão com data).
- **Esforço:** 0,5 dia.

---

## 5. Ordem sugerida

1. `TRACK-05` primeiro (é grátis, destrava a decisão de provedor imediatamente).
2. `TRACK-01` + `TRACK-02` (modelo de dados).
3. `TRACK-04` (cálculo de geofencing, pode rodar em paralelo aos itens de dados).
4. `TRACK-03` (job de manutenção de partições).
5. `TRACK-06` (documentação, a qualquer momento).

Estimativa total: ~3,5 dias úteis — bem mais barato do que seria integrar e operar TimescaleDB, e sem amarrar a escolha de provedor.

---

*Prioridade geral: P2/P3 dentro do roadmap — é plus, não é pré-requisito de piloto. Não compete com os itens P0 do `Docs/sad-aurora-backlog.md` (segredos, backup, stack trace) nem com o `Docs/entra-id-integracao-backlog.md` (pedido explícito da Aurora).*
