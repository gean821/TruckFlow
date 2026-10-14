#!/usr/bin/env python3
"""Compila rules/*.md em AGENTS.md (documento único para agentes).

Uso:
    python .claude/skills/postgres-best-practices/build_agents.py

Ordem: seções de rules/_sections.md (pelo prefixo do arquivo); dentro de cada
seção, por impacto (CRITICAL → LOW) e depois nome. Arquivos "_*.md" são ignorados.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
RULES = ROOT / "rules"


def parse_sections(text: str):
    pattern = re.compile(
        r"^## (\d+)\. (.+?) \((\w+)\)\s*\n\*\*Impact:\*\* (.+?)\s*\n\*\*Description:\*\* (.+?)\s*$",
        re.MULTILINE,
    )
    return [
        {"num": int(m[1]), "title": m[2], "prefix": m[3], "impact": m[4], "desc": m[5]}
        for m in pattern.finditer(text)
    ]


def parse_rule(path: Path):
    raw = path.read_text(encoding="utf-8")
    meta, body = {}, raw
    if raw.startswith("---"):
        _, fm, body = raw.split("---", 2)
        for line in fm.strip().splitlines():
            if ":" in line:
                k, v = line.split(":", 1)
                meta[k.strip()] = v.strip()
    body = body.strip()
    # remove o "## Título" do corpo (vira ### numerado no compilado)
    body = re.sub(r"^## .+\n", "", body, count=1).strip()
    return meta, body


def anchor(text: str) -> str:
    text = text.lower()
    text = re.sub(r"[^\w\s-]", "", text, flags=re.UNICODE)
    return re.sub(r"\s+", "-", text.strip())


def main() -> int:
    sections = parse_sections((RULES / "_sections.md").read_text(encoding="utf-8"))
    meta = json.loads((ROOT / "metadata.json").read_text(encoding="utf-8"))
    files = sorted(p for p in RULES.glob("*.md") if not p.name.startswith("_"))

    grouped = {s["prefix"]: [] for s in sections}
    orphans = []
    for f in files:
        prefix = f.stem.split("-", 1)[0]
        (grouped[prefix] if prefix in grouped else orphans).append(f)
    if orphans:
        print("Regras sem seção (prefixo desconhecido):", ", ".join(o.name for o in orphans), file=sys.stderr)
        return 1
    rank = {k: i for i, k in enumerate(["CRITICAL", "HIGH", "MEDIUM-HIGH", "MEDIUM", "LOW-MEDIUM", "LOW"])}
    for prefix in grouped:
        grouped[prefix].sort(key=lambda f: (rank.get(parse_rule(f)[0].get("impact", "LOW"), 9), f.name))

    out = [
        "# PostgreSQL Best Practices — TruckFlow",
        "",
        f"**Versão {meta['version']}**  ",
        f"{meta['organization']}  ",
        f"{meta['date']}",
        "",
        "> Documento gerado por `build_agents.py` a partir de `rules/*.md`. Não edite à mão.",
        "",
        "---",
        "",
        "## Resumo",
        "",
        meta["abstract"],
        "",
        "---",
        "",
        "## Sumário",
        "",
    ]
    for s in sections:
        out.append(f"{s['num']}. [{s['title']}](#{anchor(s['title'])}) - **{s['impact']}**")
        for i, f in enumerate(grouped[s["prefix"]], 1):
            title = parse_rule(f)[0].get("title", f.stem)
            num = s["num"]
            out.append(f"   - {num}.{i} [{title}](#{anchor(f'{num}{i} {title}')})")
    out += ["", "---", ""]

    for s in sections:
        out += [f"## {s['title']}", "", f"**Impacto:** {s['impact']}  ", s["desc"], ""]
        for i, f in enumerate(grouped[s["prefix"]], 1):
            m, body = parse_rule(f)
            out += [
                f"### {s['num']}.{i} {m.get('title', f.stem)}",
                "",
                f"**Impacto: {m.get('impact', '?')} ({m.get('impactDescription', '')})**  ",
                f"Tags: {m.get('tags', '')}  ",
                f"Arquivo: `rules/{f.name}`",
                "",
                body,
                "",
                "---",
                "",
            ]

    out += ["## Referências", ""] + [f"- {r}" for r in meta["references"]] + [""]
    (ROOT / "AGENTS.md").write_text("\n".join(out), encoding="utf-8")
    print(f"AGENTS.md gerado: {len(files)} regras em {len(sections)} seções.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
