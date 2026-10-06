param(
    [string]$ProjectRoot = (Get-Location).Path,
    [switch]$SkipIndex,
    [switch]$NoOpen,
    [switch]$Drift,
    [switch]$ProposeContext,
    [switch]$SkipUnityCheck
)

$ErrorActionPreference = "Stop"
$ProjectRoot = (Resolve-Path $ProjectRoot).Path

$indexScript = Join-Path $ProjectRoot "V57/tools/index-project.ps1"
$templatePath = Join-Path $ProjectRoot "V57/tools/viewer/context-graph-template.html"
$indexPath = Join-Path $ProjectRoot "project-index.json"
$outDir = Join-Path $ProjectRoot "Docs/V57/reports/context-graph-viewer"
$outHtml = Join-Path $outDir "index.html"
$outIndexCopy = Join-Path $outDir "project-index.json"

if (-not (Test-Path $templatePath)) {
    Write-Error "Viewer template not found: $templatePath"
    exit 1
}

if (-not $SkipIndex) {
    if (-not (Test-Path $indexScript)) {
        Write-Error "Indexer script not found: $indexScript"
        exit 1
    }
    $indexArgs = @{ ProjectRoot = $ProjectRoot }
    if ($Drift) { $indexArgs.Drift = $true }
    if ($ProposeContext) { $indexArgs.ProposeContext = $true }
    if ($SkipUnityCheck) { $indexArgs.SkipUnityCheck = $true }
    Write-Host "Regenerating project-index.json..." -ForegroundColor Cyan
    & $indexScript @indexArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not (Test-Path $indexPath)) {
    Write-Error "Missing $indexPath. Run without -SkipIndex, or run ./V57/tools/index-project.ps1 first."
    exit 1
}

$indexJson = Get-Content $indexPath -Raw -Encoding UTF8
try {
    $parsed = $indexJson | ConvertFrom-Json
} catch {
    Write-Error "Invalid project-index.json: $_"
    exit 1
}
if (-not $parsed.modules) {
    Write-Error "project-index.json has no modules[]."
    exit 1
}

$projectName = Split-Path $ProjectRoot -Leaf
$template = Get-Content $templatePath -Raw -Encoding UTF8
$html = $template.Replace("__V57_PROJECT_NAME__", $projectName).Replace("__V57_INDEX_JSON__", $indexJson)

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
[System.IO.File]::WriteAllText($outHtml, $html, [System.Text.UTF8Encoding]::new($false))
Copy-Item $indexPath $outIndexCopy -Force

Write-Host "Wrote $outHtml" -ForegroundColor Green
Write-Host "Copied $outIndexCopy" -ForegroundColor Green
Write-Host ("Modules={0} Interfaces={1} Events={2} generatedAt={3}" -f `
    @($parsed.modules).Count, `
    @($parsed.interfaces).Count, `
    @($parsed.events).Count, `
    $parsed.generatedAt)

if (-not $NoOpen) {
    Start-Process $outHtml
    Write-Host "Opened in default browser." -ForegroundColor Green
}
