# Disponibiliza os agentes e skills do TruckFlow (versionados neste repo) em qualquer pasta,
# incluindo os repos do admin (TruckFlowApp) e do mobile (tf-mobile).
#
# Cria junções (atalhos de pasta do Windows, não precisam de admin) em ~/.claude apontando para
# este repo. Não copia nada: um `git pull` aqui atualiza os 3 repos de uma vez.
#
# Uso (de qualquer pasta):  powershell -ExecutionPolicy Bypass -File <repo-api>\.claude\setup-agents.ps1
# Pode rodar de novo sem problema: o que já está certo é mantido.

$ErrorActionPreference = 'Stop'

$repoClaude = $PSScriptRoot
$userClaude = Join-Path $env:USERPROFILE '.claude'

function New-TruckFlowJunction([string] $link, [string] $target) {
    if (Test-Path -LiteralPath $link) {
        $item = Get-Item -LiteralPath $link -Force
        if ($item.LinkType -eq 'Junction' -and ($item.Target | Select-Object -First 1) -eq $target) {
            Write-Host "ok        $link"
            return
        }
        Write-Warning "ignorado  $link já existe e não aponta para $target (remova manualmente se quiser substituir)"
        return
    }
    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    Write-Host "criado    $link -> $target"
}

New-Item -ItemType Directory -Force -Path $userClaude | Out-Null

# Agentes: a pasta inteira. Se já existir uma ~/.claude/agents própria, cai no aviso acima.
New-TruckFlowJunction (Join-Path $userClaude 'agents') (Join-Path $repoClaude 'agents')

# Skills: uma junção por skill, porque ~/.claude/skills costuma já existir com skills de outros projetos.
$userSkills = Join-Path $userClaude 'skills'
New-Item -ItemType Directory -Force -Path $userSkills | Out-Null
Get-ChildItem -LiteralPath (Join-Path $repoClaude 'skills') -Directory | ForEach-Object {
    New-TruckFlowJunction (Join-Path $userSkills $_.Name) $_.FullName
}

Write-Host ''
Write-Host 'Pronto. Reinicie as sessões do Claude Code abertas para carregar os agentes.'
