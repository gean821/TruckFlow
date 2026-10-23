# Padrões Vue do TruckFlowApp

Guia de referência da arquitetura por entidade. O exemplo é uma entidade nova, **Transportadora**, com listagem paginada server-side — exatamente o que `scripts/feature_scaffolder.py Transportadora --paged` gera.

> Referência real no repo: módulo **Usuario** (`usuario.types.ts` → `UsuarioService.ts` → `usuario.queries.ts` → `useUsuario.ts` → `UsuarioFormModal.vue` → `ManageUserView.vue`).

---

## 1. Visão da cadeia

```
View (página)                ── lê: useTransportadorasQuery(params) / useTransportadoraByIdQuery(id)
  │                          ── escreve: useTransportadora().create/update/remove
  ├── FormModal (burro)      ── recebe initialData, emite submit(payload tipado)
  └── ConfirmDialog
          │
hooks/useTransportadora.ts   ── useMutation + invalidateQueries(transportadoraKeys.all) + toast
queries/transportadora.queries.ts ── key factory + useQuery (MaybeRef, keepPreviousData, enabled getter)
services/TransportadoraService.ts ── factory com http.get/post/patch/delete — nada de Vue aqui
entities/transportadora.types.ts  ── tipos espelhando os DTOs do backend
http/http.ts                 ── axios: baseURL VITE_API_URL, Bearer, refresh single-flight
```

Cada camada só conhece a de baixo. A view nunca toca `http` nem o service.

---

## 2. Tipos — `src/entities/transportadora.types.ts`

Espelham os DTOs do backend (`TruckFlow.Domain/Dtos`, sufixo `Dto`). Campos opcionais do backend (`string?`, `Guid?`) viram `?: T | null`. Datas chegam como string ISO — não tipar como `Date`.

```ts
export type TransportadoraResponseDto = {
  id: string;
  nome: string;
  cnpj: string;
  ativo: boolean;
  createdAt?: string | null;
  updatedAt?: string | null;
};

export type TransportadoraCreateDto = {
  nome: string;
  cnpj: string; // só dígitos — o form limpa a máscara antes de emitir
};

export type TransportadoraUpdateDto = Partial<TransportadoraCreateDto>;

export type TransportadoraListQueryDto = {
  pageNumber: number;
  pageSize: number;
  search?: string;
  ativo?: boolean;
};
```

Convenção de casing: **camelCase** (o ASP.NET serializa assim). Não copie o `GradeListQueryDto` com `LocalDescargaId` em PascalCase.

Onde colocar: tipos novos em `src/entities/<x>.types.ts`. `src/Dtos/` é legado — não crie arquivos novos lá (e não mova os antigos sem pedido).

---

## 3. Service — `src/services/TransportadoraService.ts`

Factory, funções `async`, retorno tipado via generic do `http`, `signal` opcional nas leituras.

```ts
import type {
  TransportadoraCreateDto,
  TransportadoraListQueryDto,
  TransportadoraResponseDto,
  TransportadoraUpdateDto
} from "@/entities/transportadora.types";
import type { PaginatedResponse } from "@/entities/paginatedResponse";
import http from "@/http/http";

export const TransportadoraService = () => {
  const getPaged = async (
    query: TransportadoraListQueryDto,
    signal?: AbortSignal
  ): Promise<PaginatedResponse<TransportadoraResponseDto>> => {
    const { data } = await http.get<PaginatedResponse<TransportadoraResponseDto>>("/Transportadora", { params: query, signal });
    return data;
  };

  const getById = async (id: string, signal?: AbortSignal): Promise<TransportadoraResponseDto> => {
    const { data } = await http.get<TransportadoraResponseDto>(`/Transportadora/${id}`, { signal });
    return data;
  };

  const create = async (payload: TransportadoraCreateDto): Promise<TransportadoraResponseDto> => {
    const { data } = await http.post<TransportadoraResponseDto>("/Transportadora", payload);
    return data;
  };

  const update = async (id: string, payload: TransportadoraUpdateDto): Promise<TransportadoraResponseDto> => {
    const { data } = await http.patch<TransportadoraResponseDto>(`/Transportadora/${id}`, payload);
    return data;
  };

  const remove = async (id: string): Promise<void> => {
    await http.delete(`/Transportadora/${id}`);
  };

  return { getPaged, getById, create, update, remove };
};
```

**Não copie:** classes estáticas com métodos PascalCase (`ItemPlanejamentoService.GetAll`), object literal (`ConferenciaService`), default + named export juntos (`RecebimentoService`), `http.get` sem generic. Paths começam com `/` e são relativos a `VITE_API_URL` (que já tem `/v1/`).

**Nunca** mande `empresaId` no body/params para "filtrar" — o backend tira o tenant do claim `EmpresaId` do JWT. Mandar do front só cria a ilusão de filtro (e um vetor de ataque se o backend confiar nele).

---

## 4. Queries — `src/queries/transportadora.queries.ts`

```ts
import type { TransportadoraListQueryDto } from "@/entities/transportadora.types";
import { TransportadoraService } from "@/services/TransportadoraService";
import { keepPreviousData, useQuery } from "@tanstack/vue-query";
import { unref, type MaybeRef } from "vue";

const service = TransportadoraService();

/** Raiz como string simples: compatível com invalidações já existentes no projeto. */
export const transportadoraQueryKey = "transportadoras";

/** Key factory hierárquica (TkDodo): invalidar `.all` cobre lista E detalhe. */
export const transportadoraKeys = {
  all: [transportadoraQueryKey] as const,
  lists: () => [...transportadoraKeys.all, "list"] as const,
  list: (params: MaybeRef<TransportadoraListQueryDto>) => [...transportadoraKeys.lists(), params] as const,
  detail: (id: MaybeRef<string | undefined | null>) => [...transportadoraKeys.all, "detail", id] as const
};

export function useTransportadorasQuery(params: MaybeRef<TransportadoraListQueryDto>) {
  return useQuery({
    queryKey: transportadoraKeys.list(params),
    queryFn: ({ signal }) => service.getPaged(unref(params), signal),
    placeholderData: keepPreviousData
  });
}

export function useTransportadoraByIdQuery(id: MaybeRef<string | undefined | null>) {
  return useQuery({
    queryKey: transportadoraKeys.detail(id),
    queryFn: ({ signal }) => service.getById(unref(id)!, signal),
    enabled: () => !!unref(id)
  });
}
```

Checklist da query:
- [ ] parâmetro é `MaybeRef<T>` e vai **inteiro** na key (não `.value`)
- [ ] `unref` só dentro do `queryFn`
- [ ] lista paginada tem `placeholderData: keepPreviousData`
- [ ] detalhe tem `enabled` getter
- [ ] cadastro mestre pouco mutável tem `staleTime` (ex.: 5 min)
- [ ] `signal` repassado

### Por que key factory

Hoje o projeto mistura `["agendamentos", filtros]` com `["agendamento", id]`, e `useAgendamento` invalida só a lista → o detalhe fica velho. Com a factory, `transportadoraKeys.all` é prefixo de tudo. Ao migrar um módulo existente, mantenha a raiz (`'agendamentos'`), porque outras telas invalidam por ela.

---

## 5. Hook de mutations — `src/hooks/useTransportadora.ts`

```ts
import type { TransportadoraCreateDto, TransportadoraUpdateDto } from "@/entities/transportadora.types";
import { transportadoraKeys } from "@/queries/transportadora.queries";
import { TransportadoraService } from "@/services/TransportadoraService";
import { useToastStore } from "@/stores/ToastStore";
import { useMutation, useQueryClient } from "@tanstack/vue-query";
import { getErrorMessage } from "@/http/errors"; // helper (ver frontend_best_practices.md §2)

export function useTransportadora() {
  const queryClient = useQueryClient();
  const service = TransportadoraService();
  const toast = useToastStore();

  const invalidate = () => queryClient.invalidateQueries({ queryKey: transportadoraKeys.all });

  const createMutation = useMutation({
    mutationFn: async (payload: TransportadoraCreateDto) => await service.create(payload),
    onSuccess: async () => {
      await invalidate();
      toast.notify("Transportadora cadastrada!", "success");
    },
    onError: (error) => toast.notify(getErrorMessage(error, "Erro ao cadastrar transportadora."), "error")
  });

  const updateMutation = useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: TransportadoraUpdateDto }) =>
      await service.update(id, payload),
    onSuccess: async () => {
      await invalidate();
      toast.notify("Transportadora atualizada!", "success");
    },
    onError: (error) => toast.notify(getErrorMessage(error, "Erro ao atualizar transportadora."), "error")
  });

  const removeMutation = useMutation({
    mutationFn: async (id: string) => await service.remove(id),
    onSuccess: async () => {
      await invalidate();
      toast.notify("Transportadora removida.", "info");
    },
    onError: (error) => toast.notify(getErrorMessage(error, "Erro ao remover transportadora."), "error")
  });

  return {
    create: (payload: TransportadoraCreateDto) => createMutation.mutateAsync(payload),
    update: (id: string, payload: TransportadoraUpdateDto) => updateMutation.mutateAsync({ id, payload }),
    remove: (id: string) => removeMutation.mutateAsync(id),
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isRemoving: removeMutation.isPending
  };
}
```

Contrato do hook (padrão `useUsuario`/`useFornecedor`): funções embrulhadas com assinatura natural (`update(id, payload)`) + flags `isXxx`. **Não** exponha o `mutateAsync` cru (`useAgendamento`/`useGrade` fazem; obriga o chamador a montar `{ id, payload }`).

Se a mutation afeta outras entidades (check-in → agendamento + dashboard + grade), invalide todas — ver `usePlanejamento.ts`.

O hook mostra o toast; por isso a view faz `try { await create(...) } catch {}` só para **não fechar o modal** em caso de erro — sem toast duplicado.

---

## 6. Form modal — `src/components/Forms/TransportadoraFormModal.vue`

Form "burro": não chama API, não conhece hook. Recebe `initialData`, valida, emite `submit`.

```vue
<template>
  <v-dialog :model-value="open" max-width="560" persistent @update:model-value="(v: boolean) => !v && emit('close')">
    <v-card rounded="lg">
      <v-card-title class="text-h6 font-weight-bold pa-4">
        {{ isEdit ? "Editar transportadora" : "Nova transportadora" }}
      </v-card-title>
      <v-card-text>
        <v-form ref="formRef" @submit.prevent="onSubmit">
          <v-text-field v-model="form.nome" label="Razão social" :rules="[rules.required]" autofocus />
          <v-text-field v-model="form.cnpj" label="CNPJ" :rules="[rules.required, rules.cnpj]" inputmode="numeric" />
        </v-form>
      </v-card-text>
      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" @click="emit('close')">Cancelar</v-btn>
        <v-btn color="primary" variant="flat" :loading="loading" @click="onSubmit">Salvar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { computed, reactive, ref } from "vue";
  import type { VForm } from "vuetify/components";
  import type { TransportadoraCreateDto, TransportadoraResponseDto, TransportadoraUpdateDto } from "@/entities/transportadora.types";

  const props = defineProps<{ open: boolean; initialData?: TransportadoraResponseDto; loading?: boolean }>();
  const emit = defineEmits<{ close: []; submit: [payload: TransportadoraCreateDto | TransportadoraUpdateDto] }>();

  const isEdit = computed(() => !!props.initialData);
  const formRef = ref<VForm | null>(null);
  // O pai usa :key="editing?.id ?? 'new'" → o form nasce de novo a cada abertura; dispensa watch(initialData).
  const form = reactive({ nome: props.initialData?.nome ?? "", cnpj: props.initialData?.cnpj ?? "" });

  const rules = {
    required: (v: string) => !!v?.trim() || "Campo obrigatório",
    cnpj: (v: string) => v?.replace(/\D/g, "").length === 14 || "CNPJ deve ter 14 dígitos"
  };

  async function onSubmit() {
    const result = await formRef.value?.validate();
    if (!result?.valid) { return; }
    emit("submit", { nome: form.nome.trim(), cnpj: form.cnpj.replace(/\D/g, "") });
  }
</script>
```

Validação: Vuetify `rules` (funções `v => true | "mensagem"`), mensagens em pt-BR e específicas. Não há vee-validate/zod no projeto — não adicione sem decisão do time. Máscara: `v-maska` aparece no código mas o pacote `maska` **não está instalado** (a máscara não funciona); para campo novo, formate manualmente (como `formatPhone` no `UsuarioFormModal`) ou proponha instalar e registrar o `maska` num PR próprio.

---

## 7. View — `src/views/TransportadoraView.vue` (trecho do script)

```ts
const route = useRoute();
const router = useRouter();

const page = ref(Number(route.query.page) || 1);
const pageSize = ref(12);
const search = ref(route.query.search?.toString() ?? "");
const searchDebounced = useDebouncedRef(search, 350); // composable (vue-best-practices: composable-debounced-ref)

watch(searchDebounced, () => { page.value = 1; });
watch([searchDebounced, page], () => {
  router.replace({ query: {
    search: searchDebounced.value || undefined,
    page: page.value !== 1 ? String(page.value) : undefined
  }});
});

const params = computed<TransportadoraListQueryDto>(() => ({
  pageNumber: page.value,
  pageSize: pageSize.value,
  search: searchDebounced.value || undefined
}));

const { data, isLoading, isFetching, isError, refetch } = useTransportadorasQuery(params);
const { create, update, remove, isCreating, isUpdating } = useTransportadora();

const modalOpen = ref(false);
const editing = ref<TransportadoraResponseDto | null>(null);

async function handleSubmit(payload: TransportadoraCreateDto | TransportadoraUpdateDto) {
  try {
    if (editing.value?.id) { await update(editing.value.id, payload as TransportadoraUpdateDto); }
    else { await create(payload as TransportadoraCreateDto); }
    modalOpen.value = false;
  } catch {
    // toast já foi mostrado pelo hook; modal continua aberto para correção
  }
}
```

Template: cabeçalho (título + ação primária), filtros, `v-progress-linear` em `isFetching`, `v-skeleton-loader` em `isLoading && !data`, `v-alert` de erro com "Tentar novamente", estado vazio com ícone, grid/tabela com `:key="item.id"`, `v-pagination` com `data.totalPages`, `<TransportadoraFormModal :key="editing?.id ?? 'new'" ...>`, `ConfirmDialog` para remover.

---

## 8. Permissões na UI

```ts
const { canManageMasterData } = usePermissions();
```

```vue
<v-btn v-if="canManageMasterData" color="primary" @click="openCreate">Nova transportadora</v-btn>
```

Rota: `meta: { requiresAuth: true, roles: RoleGroups.CanManageMasterData }`. Se precisar de grupo novo, adicione em `shared/auth/roles.ts` (`RoleGroups`) e em `usePermissions.ts` — nunca array de strings solto. Esconder no front é UX; o backend precisa do `[Authorize(Roles = ...)]` equivalente.

Papéis reais: `Admin`, `Gerente`, `Monitor`, `Portaria` (web) e `Motorista` (só mobile). Usuários `isEntraId` (SSO) não editam e-mail/senha localmente — esconda essas ações como `ManageUserView` faz.

---

## 9. Realtime (SSE)

Um único `useRealtimeNotifications()` montado no `App.vue`. Ao receber evento, **invalide queries** — não injete dado do evento direto no cache. Para nova entidade "ao vivo": o backend publica o evento, o composable mapeia o tipo do evento → `invalidateQueries({ queryKey: xKeys.all })`.

---

## 10. Quando quebrar o padrão

- Endpoint utilitário curto (enum, dropdown) pode retornar lista bruta — query com `staleTime` longo, sem paginação.
- Export CSV de "tudo": função no hook usando `queryClient.fetchQuery` ou chamada do service **dentro do hook**, nunca na view.
- Upload multipart: o service sobrescreve `Content-Type` só naquela chamada (`headers: { "Content-Type": "multipart/form-data" }`) — e manda o arquivo real, não `URL.createObjectURL` (bug atual do `UsuarioFormModal`).

Fontes: [Vue — Composables](https://vuejs.org/guide/reusability/composables.html) · [TanStack Vue Query](https://tanstack.com/query/latest/docs/framework/vue/overview) · [TkDodo — Effective Query Keys](https://tkdodo.eu/blog/effective-react-query-keys) · [TkDodo — Mastering Mutations](https://tkdodo.eu/blog/mastering-mutations-in-react-query) · [Michael Thiessen — Clean Components](https://michaelnthiessen.com/) · [Vuetify — Forms](https://vuetifyjs.com/en/components/forms/)
