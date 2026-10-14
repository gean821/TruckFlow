#!/usr/bin/env python3
"""
Auditor de padrões do TruckFlowApp.

Varre src/ do front e reporta violações das convenções do projeto:

  SVC-IN-UI       view/componente importa *Service (deveria consumir hook/query)
  HTTP-OUTSIDE    `http` (axios) usado fora de src/services e src/http
  MUTATION-IN-UI  useMutation declarado dentro de .vue (deveria estar em src/hooks)
  STATIC-ENABLED  `enabled: !!id` estático em query (use getter `() => !!unref(id)`)
  ANY             uso de `: any` / `as any` / `<any>`
  UNTYPED-EMITS   defineEmits([...]) com array de strings (use tupla tipada)
  TOAST-MS        toast.notify(..., N) com N < 100 (duração é em milissegundos)
  NO-KEEPPREV     query paginada (getPaged/getByFilters) sem placeholderData: keepPreviousData
  NO-SIGNAL       queryFn que não repassa o AbortSignal (info)

Uso:
    python pattern_auditor.py                          # repo padrão C:/ESTUDO/TruckFlowApp
    python pattern_auditor.py --repo ../TruckFlowApp --only ANY,SVC-IN-UI
    python pattern_auditor.py --files src/views/XView.vue src/hooks/useX.ts   # só arquivos do PR
    python pattern_auditor.py --json > report.json

Exit code 1 se houver violação de severidade "error" (útil em pre-commit/CI).
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
from dataclasses import asdict, dataclass

ERROR, WARN, INFO = "error", "warn", "info"


@dataclass
class Finding:
    rule: str
    severity: str
    file: str
    line: int
    text: str


RE_SERVICE_IMPORT = re.compile(r"""import\s+.*?from\s+["']@/services/\w+["']""")
RE_HTTP_IMPORT = re.compile(r"""import\s+http\b.*?from\s+["']@/http/http["']""")
RE_USE_MUTATION = re.compile(r"\buseMutation\s*\(")
RE_STATIC_ENABLED = re.compile(r"enabled\s*:\s*!!\s*[A-Za-z_][\w.]*\s*[,}\n]")
RE_ANY = re.compile(r"(:\s*any\b|\bas\s+any\b|<any>|any\[\])")
RE_UNTYPED_EMITS = re.compile(r"defineEmits\s*\(\s*\[")
RE_TOAST = re.compile(r"\.notify\s*\((?P<args>[^)]*)\)")
RE_PAGED_CALL = re.compile(r"\b(getPaged|getByFilters)\s*\(")
RE_USE_QUERY = re.compile(r"\buseQuery\s*\(\s*\{")
RE_QUERYFN_NO_SIGNAL = re.compile(r"queryFn\s*:\s*async\s*\(\s*\)\s*=>")


def rel(repo: str, path: str) -> str:
    return os.path.relpath(path, repo).replace("\\", "/")


def line_of(text: str, idx: int) -> int:
    return text.count("\n", 0, idx) + 1


def query_blocks(text: str):
    """Yield (start_idx, block_text) para cada useQuery({ ... }) com chaves balanceadas."""
    for m in RE_USE_QUERY.finditer(text):
        i = m.end() - 1
        depth = 0
        for j in range(i, len(text)):
            if text[j] == "{":
                depth += 1
            elif text[j] == "}":
                depth -= 1
                if depth == 0:
                    yield m.start(), text[m.start(): j + 1]
                    break


def audit_file(repo: str, path: str) -> list[Finding]:
    r = rel(repo, path)
    try:
        text = open(path, encoding="utf-8").read()
    except UnicodeDecodeError:
        return []
    out: list[Finding] = []
    is_vue = path.endswith(".vue")
    in_ui = r.startswith("src/views/") or r.startswith("src/components/")
    in_services = r.startswith("src/services/") or r.startswith("src/shared/services/")
    in_http = r.startswith("src/http/")
    lines = text.splitlines()

    def add(rule: str, sev: str, idx: int):
        ln = line_of(text, idx)
        out.append(Finding(rule, sev, r, ln, lines[ln - 1].strip()[:140] if ln - 1 < len(lines) else ""))

    if in_ui:
        for m in RE_SERVICE_IMPORT.finditer(text):
            add("SVC-IN-UI", ERROR, m.start())
    if not (in_services or in_http):
        for m in RE_HTTP_IMPORT.finditer(text):
            sev = WARN if r.startswith("src/stores/") else ERROR  # AuthStore usa http para refresh — aceitável
            add("HTTP-OUTSIDE", sev, m.start())
    if is_vue:
        for m in RE_USE_MUTATION.finditer(text):
            add("MUTATION-IN-UI", ERROR, m.start())
    for m in RE_STATIC_ENABLED.finditer(text):
        add("STATIC-ENABLED", WARN, m.start())
    for m in RE_ANY.finditer(text):
        add("ANY", WARN, m.start())
    if is_vue:
        for m in RE_UNTYPED_EMITS.finditer(text):
            add("UNTYPED-EMITS", WARN, m.start())
    for m in RE_TOAST.finditer(text):
        nums = re.findall(r",\s*(\d+)\s*$", m.group("args"))
        if nums and int(nums[0]) < 100:
            add("TOAST-MS", ERROR, m.start())
    if r.startswith("src/queries/") or r.startswith("src/hooks/"):
        for start, block in query_blocks(text):
            if RE_PAGED_CALL.search(block) and "keepPreviousData" not in block:
                add("NO-KEEPPREV", WARN, start)
            if RE_QUERYFN_NO_SIGNAL.search(block):
                add("NO-SIGNAL", INFO, start)
    return out


def iter_files(repo: str, only_files: list[str] | None):
    if only_files:
        for f in only_files:
            p = f if os.path.isabs(f) else os.path.join(repo, f)
            if os.path.isfile(p):
                yield p
        return
    src = os.path.join(repo, "src")
    for root, dirs, files in os.walk(src):
        dirs[:] = [d for d in dirs if d not in ("node_modules", "dist", "__tests__")]
        for f in files:
            if f.endswith((".ts", ".vue")) and not f.endswith(".d.ts"):
                yield os.path.join(root, f)


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(
        description="Audita convenções do TruckFlowApp (hooks/queries/services, tipagem, toasts).",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    ap.add_argument("--repo", default="C:/ESTUDO/TruckFlowApp")
    ap.add_argument("--only", help="Lista de regras separadas por vírgula")
    ap.add_argument("--files", nargs="*", help="Auditar só estes arquivos (relativos ao repo)")
    ap.add_argument("--json", action="store_true", help="Saída JSON")
    ap.add_argument("--no-info", action="store_true", help="Oculta achados de severidade info")
    args = ap.parse_args()

    if not os.path.isdir(os.path.join(args.repo, "src")):
        print(f"erro: {args.repo}/src não encontrado", file=sys.stderr)
        return 2

    only = {x.strip() for x in args.only.split(",")} if args.only else None
    findings: list[Finding] = []
    for p in iter_files(args.repo, args.files):
        for f in audit_file(args.repo, p):
            if only and f.rule not in only:
                continue
            if args.no_info and f.severity == INFO:
                continue
            findings.append(f)

    findings.sort(key=lambda f: ({ERROR: 0, WARN: 1, INFO: 2}[f.severity], f.rule, f.file, f.line))
    if args.json:
        print(json.dumps([asdict(f) for f in findings], ensure_ascii=False, indent=2))
    else:
        cur = None
        for f in findings:
            if f.rule != cur:
                cur = f.rule
                print(f"\n[{f.severity.upper()}] {f.rule}")
            print(f"  {f.file}:{f.line}  {f.text}")
        summary: dict[str, int] = {}
        for f in findings:
            summary[f.rule] = summary.get(f.rule, 0) + 1
        print("\nResumo:", ", ".join(f"{k}={v}" for k, v in sorted(summary.items())) or "nenhuma violação")
    return 1 if any(f.severity == ERROR for f in findings) else 0


if __name__ == "__main__":
    sys.exit(main())
