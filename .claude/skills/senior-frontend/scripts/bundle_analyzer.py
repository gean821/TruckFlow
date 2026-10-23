#!/usr/bin/env python3
"""
Analisador de bundle do TruckFlowApp (Vite 7).

1. (opcional) roda o build:  pnpm build-only   (--build)
2. lê dist/assets e lista JS/CSS por tamanho (bruto e gzip estimado)
3. sinaliza chunks acima do limite (default 250 KB brutos)
4. checa anti-padrões de bundle no código-fonte:
     - import * as components/directives from 'vuetify/...'  (sem tree-shaking)
     - app.use(VueApexCharts) global no main.ts                (ApexCharts no bundle inicial)
     - import estático de views em src/router                  (rota não-lazy)
     - @mdi/font importado                                     (font com todos os ícones)

Uso:
    python bundle_analyzer.py                         # analisa dist/ existente
    python bundle_analyzer.py --build                 # roda pnpm build-only antes
    python bundle_analyzer.py --repo C:/ESTUDO/TruckFlowApp --limit 300
    python bundle_analyzer.py --source-only           # só checa o código, sem dist/

Exit code 1 se algum chunk passar do limite ou houver anti-padrão "error".
"""
from __future__ import annotations

import argparse
import gzip
import os
import re
import shutil
import subprocess
import sys


def kb(n: int) -> str:
    return f"{n / 1024:8.1f} KB"


def run_build(repo: str) -> bool:
    pm = "pnpm" if shutil.which("pnpm") else "npm"
    cmd = [pm, "run", "build-only"]
    print(f"$ {' '.join(cmd)}  (cwd={repo})")
    try:
        res = subprocess.run(cmd, cwd=repo, shell=(os.name == "nt"))
    except FileNotFoundError:
        print("erro: gerenciador de pacotes não encontrado", file=sys.stderr)
        return False
    return res.returncode == 0


def analyze_dist(repo: str, limit_kb: int) -> int:
    assets = os.path.join(repo, "dist", "assets")
    if not os.path.isdir(assets):
        print(f"(sem {assets} — rode com --build ou `pnpm build` antes)")
        return 0
    rows = []
    for f in os.listdir(assets):
        if not f.endswith((".js", ".css")):
            continue
        p = os.path.join(assets, f)
        raw = open(p, "rb").read()
        rows.append((f, len(raw), len(gzip.compress(raw, 6))))
    rows.sort(key=lambda r: r[1], reverse=True)
    total = sum(r[1] for r in rows)
    total_gz = sum(r[2] for r in rows)
    over = 0
    print(f"\n{'arquivo':60} {'bruto':>11} {'gzip':>11}")
    for name, size, gz in rows:
        flag = ""
        if size > limit_kb * 1024 and name.endswith(".js"):
            flag = f"  <-- acima de {limit_kb} KB"
            over += 1
        print(f"{name[:60]:60} {kb(size)} {kb(gz)}{flag}")
    print(f"{'TOTAL':60} {kb(total)} {kb(total_gz)}")
    entry = [r for r in rows if r[0].startswith("index-") and r[0].endswith(".js")]
    if entry:
        print(f"\nChunk de entrada: {entry[0][0]} = {kb(entry[0][1]).strip()} ({kb(entry[0][2]).strip()} gzip)")
    return over


SOURCE_CHECKS = [
    ("error", r"""import\s+\*\s+as\s+\w+\s+from\s+['"]vuetify/(components|directives)['"]""",
     "Vuetify importado inteiro — use vite-plugin-vuetify({ autoImport: true })"),
    ("warn", r"""app\.use\(\s*VueApexCharts\s*\)""",
     "ApexCharts registrado globalmente — carregue com defineAsyncComponent nas telas de gráfico"),
    ("warn", r"""import\s+['"]@mdi/font""",
     "@mdi/font carrega todos os ícones — considere @mdi/js + iconset mdi-svg (decisão de projeto)"),
]
RE_STATIC_VIEW = re.compile(r"""^import\s+(\w+)\s+from\s+['"]@/views/(\w+)\.vue['"]""", re.M)
EAGER_OK = {"LoginView", "HomePage"}


def analyze_source(repo: str) -> int:
    src = os.path.join(repo, "src")
    errors = 0
    print("\nChecagem de código-fonte:")
    found_any = False
    for root, dirs, files in os.walk(src):
        dirs[:] = [d for d in dirs if d != "node_modules"]
        for f in files:
            if not f.endswith((".ts", ".vue")):
                continue
            p = os.path.join(root, f)
            text = open(p, encoding="utf-8", errors="ignore").read()
            r = os.path.relpath(p, repo).replace("\\", "/")
            for sev, pat, msg in SOURCE_CHECKS:
                for m in re.finditer(pat, text):
                    ln = text.count("\n", 0, m.start()) + 1
                    print(f"  [{sev}] {r}:{ln}  {msg}")
                    found_any = True
                    errors += sev == "error"
            if r.startswith("src/router/"):
                for m in RE_STATIC_VIEW.finditer(text):
                    if m.group(2) in EAGER_OK:
                        continue
                    ln = text.count("\n", 0, m.start()) + 1
                    print(f"  [warn] {r}:{ln}  view '{m.group(2)}' importada estaticamente — use () => import(...)")
                    found_any = True
    if not found_any:
        print("  nenhum anti-padrão encontrado")
    return errors


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(
        description="Analisa o bundle do TruckFlowApp e anti-padrões de importação.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    ap.add_argument("--repo", default="C:/ESTUDO/TruckFlowApp")
    ap.add_argument("--limit", type=int, default=250, help="Limite por chunk JS em KB brutos (default: 250)")
    ap.add_argument("--build", action="store_true", help="Roda `pnpm build-only` antes de analisar")
    ap.add_argument("--source-only", action="store_true", help="Só checa o código-fonte")
    args = ap.parse_args()

    if not os.path.isdir(os.path.join(args.repo, "src")):
        print(f"erro: {args.repo}/src não encontrado", file=sys.stderr)
        return 2
    if args.build and not run_build(args.repo):
        print("build falhou", file=sys.stderr)
        return 1
    over = 0 if args.source_only else analyze_dist(args.repo, args.limit)
    errs = analyze_source(args.repo)
    return 1 if (over or errs) else 0


if __name__ == "__main__":
    sys.exit(main())
