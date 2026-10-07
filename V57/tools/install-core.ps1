<#
.SYNOPSIS
  Installs the V57 GameForge core into a provider game repo (run from the V57 core repo checkout).

.DESCRIPTION
  Copies the core next to the provider delivery so any agent can run /vertical-slice or /game-setup there:
  V57/, AGENTS.md, .cursor/, .claude/, Tools/, Packages/, ProjectSettings/, Assets/Settings + URP global assets,
  the V57-owned Assets/_Game folders (with .meta), gameforge.env.example, .gitignore and .gitattributes (merged).
  Provider content (Docs/, Source/, Assets/_Game/Art, Assets/_Game/Audio, the provider README.md) is never overwritten.
  The core README is copied to V57/README.md. node_modules and Library are never copied.

.EXAMPLE
  pwsh V57/tools/install-core.ps1 -Target E:\Repositorio\MyGame
#>
param(
    [Parameter(Mandatory = $true)][string]$Target,
    [string]$Source = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path (Join-Path $Target '.git'))) { throw "Target '$Target' is not a git repository (clone the provider repo first)." }
if (-not (Test-Path (Join-Path $Source 'V57\SYSTEM_PROMPT.md'))) { throw "Source '$Source' is not the V57 core repo." }

function Copy-Tree([string]$Relative) {
    $from = Join-Path $Source $Relative
    if (-not (Test-Path $from)) { return }
    $to = Join-Path $Target $Relative
    New-Item -ItemType Directory -Force (Split-Path $to -Parent) | Out-Null
    robocopy $from $to /E /XD node_modules Library Temp obj /XF *.log /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $Relative ($LASTEXITCODE)" }
    $global:LASTEXITCODE = 0
    Write-Host "  core  $Relative"
}

function Copy-File([string]$Relative, [string]$Destination = $Relative, [switch]$KeepExisting) {
    $from = Join-Path $Source $Relative
    $to = Join-Path $Target $Destination
    if (-not (Test-Path $from)) { return }
    if ($KeepExisting -and (Test-Path $to)) { Write-Host "  keep  $Destination (provider file)"; return }
    New-Item -ItemType Directory -Force (Split-Path $to -Parent) | Out-Null
    Copy-Item $from $to -Force
    Write-Host "  core  $Destination"
}

function Merge-Lines([string]$Relative) {
    $from = Join-Path $Source $Relative
    $to = Join-Path $Target $Relative
    $core = Get-Content $from
    if (-not (Test-Path $to)) { Set-Content $to $core; Write-Host "  core  $Relative"; return }
    $existing = Get-Content $to
    # Provider templates ignore Unity files (*.meta, ProjectSettings/, Packages/); a V57 repo needs them tracked.
    $blocked = @('*.meta', 'ProjectSettings/', 'Packages/', '/ProjectSettings/', '/Packages/')
    $kept = $existing | Where-Object { $blocked -notcontains $_.Trim() }
    $extra = $core | Where-Object { $_.Trim() -and -not $_.Trim().StartsWith('#') -and ($kept -notcontains $_) }
    Set-Content $to ($kept + '' + '# ---- V57 core ----' + $extra)
    Write-Host "  merge $Relative (+$($extra.Count) lines)"
}

Write-Host "Installing V57 core from $Source into $Target"
foreach ($dir in 'V57', '.cursor', '.claude', 'Tools', 'Packages', 'ProjectSettings', 'Assets\Settings') { Copy-Tree $dir }
foreach ($file in 'AGENTS.md', 'gameforge.env.example', 'Assets\Settings.meta', 'Assets\_Game.meta',
                  'Assets\UniversalRenderPipelineGlobalSettings.asset', 'Assets\UniversalRenderPipelineGlobalSettings.asset.meta',
                  'Assets\DefaultVolumeProfile.asset', 'Assets\DefaultVolumeProfile.asset.meta') { Copy-File $file }
Copy-File 'README.md' 'V57\README.md'

# V57-owned Assets/_Game skeleton (+ .meta of every folder); provider files are never replaced.
$gameRoot = Join-Path $Source 'Assets\_Game'
Get-ChildItem $gameRoot -Recurse -Force | Where-Object { $_.Name -eq '.gitkeep' -or $_.Extension -eq '.meta' } | ForEach-Object {
    $rel = $_.FullName.Substring($Source.Length + 1)
    Copy-File $rel $rel -KeepExisting
} | Out-Null

Merge-Lines '.gitignore'
Merge-Lines '.gitattributes'

Write-Host ''
Write-Host 'Done. Next: commit on a branch (e.g. git switch -c v57/core; git add -A; git commit -m "V57 core"),'
Write-Host 'open the project once in Unity 6000.6.2f1, then run /vertical-slice from your agent (see V57/agents/RUN.md).'
