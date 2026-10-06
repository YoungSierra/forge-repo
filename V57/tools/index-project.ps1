param(
    [string]$ProjectRoot = (Get-Location).Path,
    [switch]$ProposeContext,
    [switch]$Drift,
    [switch]$SkipUnityCheck
)

$ErrorActionPreference = "Stop"
$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$ToolProject = Join-Path $ProjectRoot "V57/tools/roslyn-index/V57.RoslynIndex.csproj"

if (-not (Test-Path $ToolProject)) {
    Write-Error "Roslyn indexer not found: $ToolProject"
    exit 1
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK not found. Install .NET SDK 8.0+ (see V57/docs/context/CONTEXT_INDEX.md)."
    exit 1
}

$UnityAssets = Join-Path $ProjectRoot "v57-unity-assets.json"
if (-not $SkipUnityCheck -and -not (Test-Path $UnityAssets)) {
    Write-Host "Note: v57-unity-assets.json not found. In Unity: V57 > Export Unity Assets Index" -ForegroundColor Yellow
}

$indexOutput = Join-Path $ProjectRoot "project-index.json"
$dotnetArgs = @(
    "run",
    "--project", $ToolProject,
    "--configuration", "Release",
    "--",
    "--root", $ProjectRoot,
    "--output", $indexOutput
)

if ($ProposeContext) { $dotnetArgs += "--propose-context" }
if ($Drift) { $dotnetArgs += "--drift" }

Write-Host "Running v57-index..." -ForegroundColor Cyan
dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. Output: $indexOutput" -ForegroundColor Green
if ($ProposeContext) {
    Write-Host "CONTEXT patch proposal: Docs/V57/reports/context-auto-patch.md (confirm-gated before merge)" -ForegroundColor Green
}
if ($Drift) {
    Write-Host "Drift report: Docs/V57/reports/drift-report.md" -ForegroundColor Green
}
