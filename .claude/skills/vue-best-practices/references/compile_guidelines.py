#!/usr/bin/env python3
"""Recompila references/vue-performance-guidelines.md a partir de references/rules/*.md.

Uso: python compile_guidelines.py   (rodar de dentro de references/ ou passar --dir)
"""
import argparse
import os
import re

SECTIONS = [
    ("query", "1. Estado de servidor com TanStack Vue Query", "CRITICAL"),
    ("async", "2. Eliminando waterfalls", "CRITICAL"),
    ("bundle", "3. Tamanho de bundle", "CRITICAL"),
    ("reactivity", "4. Reatividade", "HIGH"),
    ("rendering", "5. Renderização", "MEDIUM"),
    ("component", "6. API de componentes", "MEDIUM"),
    ("composable", "7. Composables", "MEDIUM"),
    ("js", "8. Performance de JavaScript", "LOW-MEDIUM"),
]
ORDER = {"CRITICAL": 0, "HIGH": 1, "MEDIUM-HIGH": 2, "MEDIUM": 3, "LOW-MEDIUM": 4, "LOW": 5}


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--dir", default=os.path.dirname(os.path.abspath(__file__)))
    args = ap.parse_args()
    rules = os.path.join(args.dir, "rules")
    toc = [
        "# Vue Best Practices — Guia completo (TruckFlowApp)\n",
        "> Compilado automaticamente a partir de `rules/*.md` por `compile_guidelines.py`. Não edite aqui: edite a regra e recompile.\n",
        "> Stack: Vue 3.5 · TanStack Vue Query 5 · Vuetify 3 · Vite 7 · Pinia 3 · SPA (sem SSR).\n",
        "## Sumário\n",
    ]
    body = []
    for prefix, title, impact in SECTIONS:
        num = title.split(".")[0]
        items = []
        for f in sorted(os.listdir(rules)):
            if not f.startswith(prefix + "-"):
                continue
            text = open(os.path.join(rules, f), encoding="utf-8").read()
            m = re.match(r"---\n(.*?)\n---\n(.*)", text, re.S)
            if not m:
                raise SystemExit(f"frontmatter inválido em {f}")
            meta = dict(line.split(": ", 1) for line in m.group(1).splitlines() if ": " in line)
            items.append((ORDER.get(meta.get("impact", ""), 9), meta["title"], meta.get("impact", "?"), m.group(2).strip()))
        items.sort(key=lambda x: x[0])
        toc.append(f"- **{title}** ({impact})")
        body.append(f"\n---\n\n# {title}\n\n**Impacto da seção:** {impact}\n")
        for i, (_, t, imp, b) in enumerate(items, 1):
            toc.append(f"  - {num}.{i} {t} — `{imp}`")
            body.append(re.sub(r"^## ", f"## {num}.{i} ", b, count=1, flags=re.M) + "\n")
    out = os.path.join(args.dir, "vue-performance-guidelines.md")
    with open(out, "w", encoding="utf-8") as fh:
        fh.write("\n".join(toc) + "\n" + "\n".join(body))
    print(f"ok: {out}")


if __name__ == "__main__":
    main()
