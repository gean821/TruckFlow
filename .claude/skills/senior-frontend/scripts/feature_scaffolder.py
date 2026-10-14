#!/usr/bin/env python3
"""
Scaffolder de feature do TruckFlowApp (Vue 3 + TanStack Vue Query + Vuetify).

Gera a cadeia padrão de uma entidade, seguindo o padrão de referência do módulo Usuario:

    src/entities/<x>.types.ts        tipos (ResponseDto, CreateDto, UpdateDto, ListQueryDto)
    src/services/<X>Service.ts       factory de chamadas axios puras (http.get/post/patch/delete)
    src/queries/<x>.queries.ts       key factory + useXxxQuery (MaybeRef, keepPreviousData, enabled getter)
    src/hooks/use<X>.ts              mutations (create/update/remove) + invalidação + toast
    src/components/Forms/<X>FormModal.vue   form burro (v-dialog + v-form), emite submit
    src/views/<X>View.vue            página: filtros em route.query, debounce, paginação, modal

Nunca sobrescreve arquivo existente (pula e avisa). Use --dry-run para só listar/visualizar.

Exemplos:
    python feature_scaffolder.py Transportadora --paged --dry-run
    python feature_scaffolder.py Transportadora --plural transportadoras --paged --repo C:/ESTUDO/TruckFlowApp
    python feature_scaffolder.py Motivo --endpoint /MotivoCancelamento   (não paginado)

Depois de gerar: ajuste os campos dos tipos ao DTO real do backend, registre a rota em
src/router/index.ts (lazy + meta.roles com RoleGroups) e rode `pnpm build` (vue-tsc).
"""
from __future__ import annotations

import argparse
import os
import re
import sys


def camel(name: str) -> str:
    return name[0].lower() + name[1:]


def kebab(name: str) -> str:
    return re.sub(r"(?<!^)(?=[A-Z])", "-", name).lower()


def default_plural(name: str) -> str:
    low = camel(name)
    if low.endswith("ao"):
        return low[:-2] + "oes"
    if low.endswith(("r", "z", "s")):
        return low + "es"
    if low.endswith("l"):
        return low[:-1] + "is"
    return low + "s"


def human(name: str) -> str:
    return re.sub(r"(?<!^)(?=[A-Z])", " ", name)


# ---------------------------------------------------------------- templates

def t_types(E: str, e: str, paged: bool) -> str:
    q = f"""
export type {E}ListQueryDto = {{
  pageNumber: number;
  pageSize: number;
  search?: string;
}};
""" if paged else ""
    return f"""// Espelha os DTOs do backend (TruckFlow.Domain/Dtos). Ajuste os campos ao contrato real.
export type {E}ResponseDto = {{
  id: string;
  nome: string;
  ativo: boolean;
  createdAt?: string | null;
  updatedAt?: string | null;
}};

export type {E}CreateDto = {{
  nome: string;
}};

export type {E}UpdateDto = {{
  nome?: string;
}};
{q}"""


def t_service(E: str, e: str, endpoint: str, paged: bool) -> str:
    imports = [f"{E}CreateDto", f"{E}ResponseDto", f"{E}UpdateDto"]
    if paged:
        imports.insert(1, f"{E}ListQueryDto")
    imp = ",\n  ".join(imports)
    paged_imp = 'import type { PaginatedResponse } from "@/entities/paginatedResponse";\n' if paged else ""
    if paged:
        list_fn = f"""  const getPaged = async (
    query: {E}ListQueryDto,
    signal?: AbortSignal
  ): Promise<PaginatedResponse<{E}ResponseDto>> => {{
    const {{ data }} = await http.get<PaginatedResponse<{E}ResponseDto>>("{endpoint}", {{ params: query, signal }});
    return data;
  }};
"""
        list_name = "getPaged"
    else:
        list_fn = f"""  const getAll = async (signal?: AbortSignal): Promise<{E}ResponseDto[]> => {{
    const {{ data }} = await http.get<{E}ResponseDto[]>("{endpoint}", {{ signal }});
    return data;
  }};
"""
        list_name = "getAll"
    return f"""import type {{
  {imp}
}} from "@/entities/{e}.types";
{paged_imp}import http from "@/http/http";

export const {E}Service = () => {{
{list_fn}
  const getById = async (id: string, signal?: AbortSignal): Promise<{E}ResponseDto> => {{
    const {{ data }} = await http.get<{E}ResponseDto>(`{endpoint}/${{id}}`, {{ signal }});
    return data;
  }};

  const create = async (payload: {E}CreateDto): Promise<{E}ResponseDto> => {{
    const {{ data }} = await http.post<{E}ResponseDto>("{endpoint}", payload);
    return data;
  }};

  const update = async (id: string, payload: {E}UpdateDto): Promise<{E}ResponseDto> => {{
    const {{ data }} = await http.patch<{E}ResponseDto>(`{endpoint}/${{id}}`, payload);
    return data;
  }};

  const remove = async (id: string): Promise<void> => {{
    await http.delete(`{endpoint}/${{id}}`);
  }};

  return {{ {list_name}, getById, create, update, remove }};
}};
"""


def t_queries(E: str, e: str, plural: str, paged: bool) -> str:
    if paged:
        list_q = f"""export function use{E}sQuery(params: MaybeRef<{E}ListQueryDto>) {{
  return useQuery({{
    queryKey: {e}Keys.list(params),
    queryFn: ({{ signal }}) => service.getPaged(unref(params), signal),
    placeholderData: keepPreviousData
  }});
}}
"""
        imp = f'import type {{ {E}ListQueryDto }} from "@/entities/{e}.types";\n'
        vq = "keepPreviousData, useQuery"
        list_key = f"list: (params: MaybeRef<{E}ListQueryDto>) => [...{e}Keys.lists(), params] as const,"
    else:
        list_q = f"""export function use{E}sQuery() {{
  return useQuery({{
    queryKey: {e}Keys.lists(),
    queryFn: ({{ signal }}) => service.getAll(signal),
    staleTime: 5 * 60 * 1000 // cadastro mestre muda pouco
  }});
}}
"""
        imp = ""
        vq = "useQuery"
        list_key = ""
    return f"""{imp}import {{ {E}Service }} from "@/services/{E}Service";
import {{ {vq} }} from "@tanstack/vue-query";
import {{ unref, type MaybeRef }} from "vue";

const service = {E}Service();

/** Raiz mantida como string simples para compatibilidade com invalidações existentes. */
export const {e}QueryKey = "{plural}";

/** Key factory hierárquica: invalidar `{e}Keys.all` cobre lista E detalhe. */
export const {e}Keys = {{
  all: [{e}QueryKey] as const,
  lists: () => [...{e}Keys.all, "list"] as const,
  {list_key}
  detail: (id: MaybeRef<string | undefined | null>) => [...{e}Keys.all, "detail", id] as const
}};

{list_q}
export function use{E}ByIdQuery(id: MaybeRef<string | undefined | null>) {{
  return useQuery({{
    queryKey: {e}Keys.detail(id),
    queryFn: ({{ signal }}) => service.getById(unref(id)!, signal),
    enabled: () => !!unref(id)
  }});
}}
"""


def t_hook(E: str, e: str, label: str) -> str:
    return f"""import type {{ {E}CreateDto, {E}UpdateDto }} from "@/entities/{e}.types";
import {{ {e}Keys }} from "@/queries/{e}.queries";
import {{ {E}Service }} from "@/services/{E}Service";
import {{ useToastStore }} from "@/stores/ToastStore";
import {{ useMutation, useQueryClient }} from "@tanstack/vue-query";
import {{ isAxiosError }} from "axios";

function backendMessage(error: unknown, fallback: string): string {{
  if (isAxiosError<{{ message?: string }}>(error)) {{
    return error.response?.data?.message ?? fallback;
  }}
  return fallback;
}}

export function use{E}() {{
  const queryClient = useQueryClient();
  const service = {E}Service();
  const toast = useToastStore();

  const invalidate = () => queryClient.invalidateQueries({{ queryKey: {e}Keys.all }});

  const createMutation = useMutation({{
    mutationFn: async (payload: {E}CreateDto) => await service.create(payload),
    onSuccess: async () => {{
      await invalidate();
      toast.notify("{label} cadastrado(a)!", "success");
    }},
    onError: (error) => {{
      toast.notify(backendMessage(error, "Erro ao cadastrar {label.lower()}."), "error");
    }}
  }});

  const updateMutation = useMutation({{
    mutationFn: async ({{ id, payload }}: {{ id: string; payload: {E}UpdateDto }}) =>
      await service.update(id, payload),
    onSuccess: async () => {{
      await invalidate();
      toast.notify("{label} atualizado(a)!", "success");
    }},
    onError: (error) => {{
      toast.notify(backendMessage(error, "Erro ao atualizar {label.lower()}."), "error");
    }}
  }});

  const removeMutation = useMutation({{
    mutationFn: async (id: string) => await service.remove(id),
    onSuccess: async () => {{
      await invalidate();
      toast.notify("{label} removido(a).", "info");
    }},
    onError: (error) => {{
      toast.notify(backendMessage(error, "Erro ao remover {label.lower()}."), "error");
    }}
  }});

  return {{
    create: (payload: {E}CreateDto) => createMutation.mutateAsync(payload),
    update: (id: string, payload: {E}UpdateDto) => updateMutation.mutateAsync({{ id, payload }}),
    remove: (id: string) => removeMutation.mutateAsync(id),
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isRemoving: removeMutation.isPending
  }};
}}
"""


def t_form(E: str, e: str, label: str) -> str:
    return f"""<template>
  <v-dialog
    :model-value="open"
    max-width="560"
    persistent
    @update:model-value="(v: boolean) => !v && emit('close')"
  >
    <v-card rounded="lg">
      <v-card-title class="text-h6 font-weight-bold pa-4">
        {{{{ isEdit ? "Editar {label.lower()}" : "Novo(a) {label.lower()}" }}}}
      </v-card-title>

      <v-card-text>
        <v-form ref="formRef" @submit.prevent="onSubmit">
          <v-text-field
            v-model="form.nome"
            label="Nome"
            variant="outlined"
            density="comfortable"
            :rules="[rules.required]"
            autofocus
          />
        </v-form>
      </v-card-text>

      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" class="text-none" @click="emit('close')">Cancelar</v-btn>
        <v-btn color="primary" variant="flat" class="text-none" :loading="loading" @click="onSubmit">
          Salvar
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import {{ computed, reactive, ref }} from "vue";
  import type {{ VForm }} from "vuetify/components";
  import type {{ {E}CreateDto, {E}ResponseDto, {E}UpdateDto }} from "@/entities/{e}.types";

  // O pai passa :key="item?.id ?? 'new'" para resetar o form entre criar/editar.
  const props = defineProps<{{
    open: boolean;
    initialData?: {E}ResponseDto;
    loading?: boolean;
  }}>();

  const emit = defineEmits<{{
    close: [];
    submit: [payload: {E}CreateDto | {E}UpdateDto];
  }}>();

  const isEdit = computed(() => !!props.initialData);
  const formRef = ref<VForm | null>(null);
  const form = reactive({{ nome: props.initialData?.nome ?? "" }});

  const rules = {{
    required: (v: string) => !!v?.trim() || "Campo obrigatório"
  }};

  async function onSubmit() {{
    const result = await formRef.value?.validate();
    if (!result?.valid) {{ return; }}
    emit("submit", {{ nome: form.nome.trim() }});
  }}
</script>
"""


def t_view(E: str, e: str, label: str, plural_label: str, paged: bool) -> str:
    if paged:
        script_list = f"""  const route = useRoute();
  const router = useRouter();

  const page = ref(Number(route.query.page) || 1);
  const pageSize = ref(12);
  const search = ref(route.query.search?.toString() ?? "");
  const searchDebounced = ref(search.value);

  // TODO: quando existir, trocar por useDebouncedRef / useListQueryState (skill vue-best-practices)
  watch(search, (val) => {{
    const t = setTimeout(() => {{ searchDebounced.value = val ?? ""; }}, 350);
    onWatcherCleanup(() => clearTimeout(t));
  }});
  watch(searchDebounced, () => {{ page.value = 1; }});
  watch([searchDebounced, page], () => {{
    router.replace({{
      query: {{
        search: searchDebounced.value || undefined,
        page: page.value !== 1 ? String(page.value) : undefined
      }}
    }});
  }});

  const params = computed<{E}ListQueryDto>(() => ({{
    pageNumber: page.value,
    pageSize: pageSize.value,
    search: searchDebounced.value || undefined
  }}));

  const {{ data, isLoading, isFetching, isError, refetch }} = use{E}sQuery(params);
  const items = computed(() => data.value?.items ?? []);
  const totalPages = computed(() => data.value?.totalPages ?? 1);
"""
        imports_extra = f'  import {{ useRoute, useRouter }} from "vue-router";\n  import type {{ {E}ListQueryDto, {E}CreateDto, {E}ResponseDto, {E}UpdateDto }} from "@/entities/{e}.types";\n'
        vue_imp = "computed, onWatcherCleanup, ref, watch"
        pagination = """
      <div v-if="totalPages > 1" class="d-flex justify-center mt-6">
        <v-pagination v-model="page" :length="totalPages" :total-visible="7" rounded="lg" />
      </div>"""
        search_field = """
    <v-row dense class="mb-4">
      <v-col cols="12" md="5">
        <v-text-field
          v-model="search"
          label="Buscar"
          prepend-inner-icon="mdi-magnify"
          variant="outlined"
          density="comfortable"
          clearable
          hide-details
        />
      </v-col>
    </v-row>"""
    else:
        script_list = f"""  const {{ data, isLoading, isFetching, isError, refetch }} = use{E}sQuery();
  const items = computed(() => data.value ?? []);
"""
        imports_extra = f'  import type {{ {E}CreateDto, {E}ResponseDto, {E}UpdateDto }} from "@/entities/{e}.types";\n'
        vue_imp = "computed, ref"
        pagination = ""
        search_field = ""
    return f"""<template>
  <v-container fluid class="pa-6">
    <div class="d-flex align-center justify-space-between mb-6">
      <div>
        <h1 class="text-h5 font-weight-bold">{plural_label}</h1>
        <p class="text-body-2 text-medium-emphasis">Gerencie os(as) {plural_label.lower()} da empresa.</p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" class="text-none" @click="openCreate">
        Novo(a) {label.lower()}
      </v-btn>
    </div>
{search_field}
    <v-progress-linear v-if="isFetching && !isLoading" indeterminate color="primary" class="mb-2" />

    <v-skeleton-loader v-if="isLoading && !data" type="card@6" />

    <v-alert v-else-if="isError" type="error" variant="tonal" class="mb-4">
      Não foi possível carregar os dados.
      <template #append>
        <v-btn variant="text" class="text-none" @click="refetch()">Tentar novamente</v-btn>
      </template>
    </v-alert>

    <div v-else-if="!items.length" class="text-center py-12 text-medium-emphasis">
      <v-icon size="64" class="mb-4">mdi-inbox-outline</v-icon>
      <p class="text-body-1">Nenhum(a) {label.lower()} encontrado(a).</p>
    </div>

    <template v-else>
      <v-row>
        <v-col v-for="item in items" :key="item.id" cols="12" sm="6" lg="4">
          <v-card rounded="lg" variant="outlined">
            <v-card-title class="text-subtitle-1 font-weight-bold">{{{{ item.nome }}}}</v-card-title>
            <v-card-actions>
              <v-spacer />
              <v-tooltip text="Editar" location="top">
                <template #activator="{{ props: tp }}">
                  <v-btn v-bind="tp" icon="mdi-pencil-outline" variant="text" size="small" aria-label="Editar" @click="openEdit(item)" />
                </template>
              </v-tooltip>
              <v-tooltip text="Remover" location="top">
                <template #activator="{{ props: tp }}">
                  <v-btn v-bind="tp" icon="mdi-trash-can-outline" variant="text" size="small" color="error" aria-label="Remover" @click="askRemove(item)" />
                </template>
              </v-tooltip>
            </v-card-actions>
          </v-card>
        </v-col>
      </v-row>{pagination}
    </template>

    <{E}FormModal
      :key="editing?.id ?? 'new'"
      :open="modalOpen"
      :initial-data="editing ?? undefined"
      :loading="isCreating || isUpdating"
      @close="closeModal"
      @submit="handleSubmit"
    />

    <ConfirmDialog
      v-model="confirmOpen"
      title="Remover {label.lower()}"
      :message="`Deseja remover ${{target?.nome ?? ''}}?`"
      @confirm="confirmRemove"
    />
  </v-container>
</template>

<script setup lang="ts">
  import {{ {vue_imp} }} from "vue";
{imports_extra}  import {{ use{E}sQuery }} from "@/queries/{e}.queries";
  import {{ use{E} }} from "@/hooks/use{E}";
  import {E}FormModal from "@/components/Forms/{E}FormModal.vue";
  import ConfirmDialog from "@/components/modals/ConfirmDialog.vue";

{script_list}
  const {{ create, update, remove, isCreating, isUpdating }} = use{E}();

  const modalOpen = ref(false);
  const editing = ref<{E}ResponseDto | null>(null);
  const confirmOpen = ref(false);
  const target = ref<{E}ResponseDto | null>(null);

  function openCreate() {{ editing.value = null; modalOpen.value = true; }}
  function openEdit(item: {E}ResponseDto) {{ editing.value = item; modalOpen.value = true; }}
  function closeModal() {{ modalOpen.value = false; editing.value = null; }}

  async function handleSubmit(payload: {E}CreateDto | {E}UpdateDto) {{
    try {{
      if (editing.value?.id) {{
        await update(editing.value.id, payload as {E}UpdateDto);
      }} else {{
        await create(payload as {E}CreateDto);
      }}
      closeModal();
    }} catch {{
      // erro já notificado pelo hook (toast); mantém o modal aberto para correção
    }}
  }}

  function askRemove(item: {E}ResponseDto) {{ target.value = item; confirmOpen.value = true; }}
  async function confirmRemove() {{
    if (!target.value) {{ return; }}
    try {{ await remove(target.value.id); }} catch {{ /* toast no hook */ }}
    confirmOpen.value = false;
    target.value = null;
  }}
</script>
"""


# ---------------------------------------------------------------- main

def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(
        description="Gera types/service/queries/hook/FormModal/View de uma entidade no TruckFlowApp.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    ap.add_argument("entity", help="Nome da entidade em PascalCase (ex.: Transportadora)")
    ap.add_argument("--plural", help="Plural em camelCase para a query key raiz (default: heurística pt-BR)")
    ap.add_argument("--endpoint", help="Path da API relativo a VITE_API_URL (default: /<Entidade>)")
    ap.add_argument("--label", help="Rótulo humano singular (default: entidade com espaços)")
    ap.add_argument("--plural-label", help="Rótulo humano plural para o título da página")
    ap.add_argument("--paged", action="store_true", help="Listagem paginada server-side (PagedResponse + ListQueryDto)")
    ap.add_argument("--repo", default="C:/ESTUDO/TruckFlowApp", help="Raiz do repo do front (default: %(default)s)")
    ap.add_argument("--dry-run", action="store_true", help="Não escreve nada; só mostra o que seria gerado")
    ap.add_argument("--show", action="store_true", help="Com --dry-run, imprime o conteúdo dos arquivos")
    args = ap.parse_args()

    E = args.entity
    if not re.fullmatch(r"[A-Z][A-Za-z0-9]+", E):
        print("erro: entidade deve ser PascalCase (ex.: Transportadora)", file=sys.stderr)
        return 2
    e = camel(E)
    plural = args.plural or default_plural(E)
    endpoint = args.endpoint or f"/{E}"
    label = args.label or human(E)
    plural_label = args.plural_label or (label + "s")

    src = os.path.join(args.repo, "src")
    if not args.dry_run and not os.path.isdir(src):
        print(f"erro: {src} não existe (use --repo)", file=sys.stderr)
        return 2

    files = {
        f"src/entities/{e}.types.ts": t_types(E, e, args.paged),
        f"src/services/{E}Service.ts": t_service(E, e, endpoint, args.paged),
        f"src/queries/{e}.queries.ts": t_queries(E, e, plural, args.paged),
        f"src/hooks/use{E}.ts": t_hook(E, e, label),
        f"src/components/Forms/{E}FormModal.vue": t_form(E, e, label),
        f"src/views/{E}View.vue": t_view(E, e, label, plural_label, args.paged),
    }

    created, skipped = 0, 0
    for rel, content in files.items():
        path = os.path.join(args.repo, rel)
        exists = os.path.exists(path)
        if exists:
            print(f"  SKIP   {rel} (já existe — não sobrescrevo)")
            skipped += 1
            continue
        if args.dry_run:
            print(f"  WOULD  {rel} ({len(content.splitlines())} linhas)")
            if args.show:
                print("-" * 72 + "\n" + content + "-" * 72)
            continue
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(content)
        print(f"  CREATE {rel}")
        created += 1

    route = f"{kebab(plural)}"
    print(
        "\nPróximos passos:\n"
        f"  1. Ajuste src/entities/{e}.types.ts ao DTO real do backend (TruckFlow.Domain/Dtos).\n"
        f"  2. Registre a rota (lazy) em src/router/index.ts:\n"
        f"       {{ path: '/{route}', name: '{route}', component: () => import('@/views/{E}View.vue'),\n"
        f"         meta: {{ requiresAuth: true, roles: RoleGroups.CanManageMasterData }} }}\n"
        "  3. Adicione o item no Navbar (respeitando usePermissions).\n"
        "  4. pnpm build  (vue-tsc precisa passar)\n"
        "  5. python pattern_auditor.py --repo <front>  (zero violações nos arquivos novos)\n"
    )
    if not args.dry_run:
        print(f"Resumo: {created} criado(s), {skipped} pulado(s).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
