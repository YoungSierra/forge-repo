param(
    [string]$ProjectRoot = (Get-Location).Path,
    [switch]$WriteReport,
    [switch]$FailOnRed
)

$ErrorActionPreference = "Stop"
$ProjectRoot = (Resolve-Path $ProjectRoot).Path

function Get-SpecYamlFiles {
    param([string]$Root)
    $patterns = @(
        (Join-Path $Root "V57/specs/<slug>/features/*.yaml"),
        (Join-Path $Root "V57/specs/<slug>/systems/*.yaml")
    )
    $files = @()
    foreach ($pattern in $patterns) {
        if (Test-Path (Split-Path $pattern -Parent)) {
            $files += Get-ChildItem -Path $pattern -File -ErrorAction SilentlyContinue
        }
    }
    return $files | Where-Object { $_.FullName -notmatch '[\\/]template[\\/]' }
}

function Test-SpecHasSpecId {
    param([string]$Content)
    if ($Content -match '(?m)^specId:\s*\S+') { return $true }
    return $false
}

function Test-SpecHasUsefulTouches {
    param([string]$Content)
    if ($Content -notmatch '(?m)^touches:') { return $false }
    $listPatterns = @(
        '(?m)^\s+scripts:\s*\[(.+)\]',
        '(?m)^\s+prefabs:\s*\[(.+)\]',
        '(?m)^\s+scriptable_objects:\s*\[(.+)\]',
        '(?m)^\s+scenes:\s*\[(.+)\]',
        '(?m)^\s+tests:\s*\[(.+)\]'
    )
    foreach ($pattern in $listPatterns) {
        if ($Content -match $pattern) {
            $inner = $Matches[1].Trim()
            if ($inner.Length -gt 0) { return $true }
        }
    }
    $blockKeys = @('scripts', 'prefabs', 'scriptable_objects', 'scenes', 'tests')
    foreach ($key in $blockKeys) {
        if ($Content -match "(?m)^\s+${key}:\s*\r?\n\s+-\s+\S") { return $true }
    }
    return $false
}

$assetsRoot = Join-Path $ProjectRoot "Assets"
$scriptsRoot = Join-Path $assetsRoot "_Game/Scripts"
if (-not (Test-Path $scriptsRoot)) { $scriptsRoot = Join-Path $assetsRoot "Scripts" }
$contextPath = Join-Path $ProjectRoot "Docs/V57/CONTEXT.md"
if (-not (Test-Path $contextPath)) { $contextPath = Join-Path $ProjectRoot "CONTEXT.md" }
$indexPath = Join-Path $ProjectRoot "project-index.json"
$unityAssetsPath = Join-Path $ProjectRoot "v57-unity-assets.json"
$exporterPath = Join-Path $ProjectRoot "Assets/Editor/V57/V57IndexExporter.cs"

$scriptCount = 0
if (Test-Path $scriptsRoot) {
    $scriptCount = (Get-ChildItem -Path $scriptsRoot -Filter "*.cs" -Recurse -File -ErrorAction SilentlyContinue).Count
}

$specFiles = Get-SpecYamlFiles -Root $ProjectRoot
$specCount = $specFiles.Count
$specsWithoutSpecId = 0
$specsWithoutTouches = 0

foreach ($spec in $specFiles) {
    $content = Get-Content -Path $spec.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }
    if (-not (Test-SpecHasSpecId -Content $content)) { $specsWithoutSpecId++ }
    if (-not (Test-SpecHasUsefulTouches -Content $content)) { $specsWithoutTouches++ }
}

$hasIndex = Test-Path $indexPath
$indexAgeDays = $null
if ($hasIndex) {
    $indexAgeDays = [math]::Round(((Get-Date) - (Get-Item $indexPath).LastWriteTime).TotalDays, 1)
}

$hasUnityAssets = Test-Path $unityAssetsPath
$hasExporter = Test-Path $exporterPath
$isConsumerProject = Test-Path $assetsRoot

$contextLines = 0
if (Test-Path $contextPath) {
    $contextLines = (Get-Content -Path $contextPath -ErrorAction SilentlyContinue).Count
}

$score = 0
$scoreNotes = @()

if ($scriptCount -ge 150) {
    $score += 3
    $scoreNotes += "scripts >= 150 (+3)"
}
elseif ($scriptCount -ge 50) {
    $score += 1
    $scoreNotes += "scripts >= 50 (+1)"
}

if ($specCount -ge 15) {
    $score += 2
    $scoreNotes += "specs >= 15 (+2)"
}
elseif ($specCount -ge 8) {
    $score += 1
    $scoreNotes += "specs >= 8 (+1)"
}

if ($specsWithoutTouches -ge 5) {
    $score += 2
    $scoreNotes += "specs without touches >= 5 (+2)"
}

if ($specsWithoutSpecId -ge 3) {
    $score += 1
    $scoreNotes += "specs without specId >= 3 (+1)"
}

if (-not $hasIndex -and $scriptCount -ge 30) {
    $score += 1
    $scoreNotes += "no project-index.json with scripts >= 30 (+1)"
}

if ($contextLines -gt 300) {
    $score += 1
    $scoreNotes += "CONTEXT.md > 300 lines (+1)"
}

$status = "Green"
if ($score -ge 5 -or $scriptCount -ge 150 -or ($specCount -ge 15 -and $scriptCount -ge 80)) {
    $status = "Red"
}
elseif ($score -ge 1) {
    $status = "Yellow"
}

$recommendation = "Phase 1 is sufficient. No Context Intelligence action required."
$nextStep = "Continue with CONTEXT.md + specs + touches on new features."

if ($status -eq "Yellow") {
    $recommendation = "Consider Phase 2 lightly: complete specId/touches on active specs; run /context-drift after milestones."
    $nextStep = "/context-drift (optional). Run /context-index when code grows or before cross-module refactors."
}
if ($status -eq "Red") {
    $recommendation = "Phase 2 recommended: index the project and keep specs aligned with disk."
    if (-not $hasExporter) {
        $recommendation += " Copy V57/tools/unity-export/Editor/V57IndexExporter.cs to Assets/Editor/V57/."
    }
    $nextStep = "Unity: V57 > Export Unity Assets Index, then /context-index (or ./V57/tools/index-project.ps1 -ProposeContext -Drift)."
}

$projectKind = if ($isConsumerProject) { "Consumer Unity project" } else { "Template / pack-only (no Assets/)" }

$report = @"
## V57 Context Readiness

| Field | Value |
|-------|-------|
| **Status** | $status |
| **Score** | $score |
| **Project** | $projectKind |
| **Root** | ``$ProjectRoot`` |

### Metrics

| Metric | Value | Threshold (reference) |
|--------|-------|------------------------|
| Scripts (.cs in Assets/_Game/Scripts) | $scriptCount | Phase 2 strong: 150+ |
| Active specs (features/systems) | $specCount | Phase 2: 15+ |
| Specs without specId | $specsWithoutSpecId | aim: 0 |
| Specs without useful touches | $specsWithoutTouches | aim: 0 |
| project-index.json | $(if ($hasIndex) { "present ($indexAgeDays days old)" } else { "missing" }) | generated by /context-index |
| v57-unity-assets.json | $(if ($hasUnityAssets) { "present" } else { "missing" }) | Unity export recommended |
| V57IndexExporter in Assets/Editor | $(if ($hasExporter) { "present" } else { "missing" }) | copy for Phase 2 |
| CONTEXT.md lines | $contextLines | large CONTEXT: 300+ |

### Score breakdown

$(if ($scoreNotes.Count -eq 0) { "_No weighted signals (score 0)._`n" } else { ($scoreNotes | ForEach-Object { "- $_" }) -join "`n" })

### Recommendation

$recommendation

### Suggested next step

$nextStep

_Guide: V57/docs/context/CONTEXT_INDEX.md_
"@

Write-Output $report

if ($WriteReport) {
    $reportPath = Join-Path $ProjectRoot "Docs/V57/reports/context-readiness-report.md"
    $reportDir = Split-Path $reportPath -Parent
    if (-not (Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    Set-Content -Path $reportPath -Value $report -Encoding UTF8
    Write-Host "Report written: $reportPath" -ForegroundColor Green
}

if ($FailOnRed -and $status -eq "Red") { exit 1 }
exit 0
