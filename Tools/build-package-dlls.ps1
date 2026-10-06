# Build precompiled DLLs and stage a consumer UPM package under Dist/com.v57.unity-game-forge
# (DLLs + UI only — no Editor/Runtime .cs).
#
# Usage:
#   powershell -File Tools/build-package-dlls.ps1
#   powershell -File Tools/build-package-dlls.ps1 -SkipUnity   # copy already-compiled ScriptAssemblies only
#
# Dev iteration keeps sources under Packages/com.v57.unity-game-forge/.
# Consumers / release installs should use Dist/com.v57.unity-game-forge.

param(
  [string]$UnityEditor = "",
  [string]$ProjectPath = "",
  [switch]$SkipUnity
)

$ErrorActionPreference = "Stop"

if (-not $ProjectPath) {
  $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Find-UnityEditor {
  param([string]$Preferred)
  if ($Preferred -and (Test-Path $Preferred)) { return $Preferred }

  $hub = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor"
  if (-not (Test-Path $hub)) { return $null }

  $candidates = @(
    "6000.5.9f1",
    "6000.5.5f1",
    "6000.3.17f1"
  )
  foreach ($v in $candidates) {
    $exe = Join-Path $hub "$v\Editor\Unity.exe"
    if (Test-Path $exe) { return $exe }
  }

  $latest = Get-ChildItem $hub -Directory | Sort-Object Name -Descending | Select-Object -First 1
  if ($latest) {
    $exe = Join-Path $latest.FullName "Editor\Unity.exe"
    if (Test-Path $exe) { return $exe }
  }
  return $null
}

$UnityEditor = Find-UnityEditor -Preferred $UnityEditor
$dist = Join-Path $ProjectPath "Dist\com.v57.unity-game-forge"
$log = Join-Path $ProjectPath "Logs\gameforge-dll-export.log"

Write-Host "GameForge DLL build"
Write-Host "  Project: $ProjectPath"
Write-Host "  Dist:    $dist"

if (-not $SkipUnity) {
  if (-not $UnityEditor) {
    Write-Error "Unity Editor not found. Pass -UnityEditor path or install Unity 6000.5.x, or use -SkipUnity after a manual compile."
  }
  Write-Host "  Unity:   $UnityEditor"
  New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
  if (Test-Path $log) { Remove-Item $log -Force }

  $args = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", "V57.GameForge.Editor.GameForgePackageDllBuilder.ExportForReleaseBatch",
    "-logFile", $log
  )

  Write-Host "Running Unity batchmode export…"
  $p = Start-Process -FilePath $UnityEditor -ArgumentList $args -Wait -PassThru
  if ($p.ExitCode -ne 0) {
    Write-Host "--- Unity log (tail) ---"
    if (Test-Path $log) { Get-Content $log -Tail 80 }
    Write-Error "Unity export failed with exit code $($p.ExitCode). See $log"
  }
}
else {
  Write-Host "SkipUnity: invoking export via already-open Editor is preferred; falling back to file copy…"
  $runtimeSrc = Join-Path $ProjectPath "Library\ScriptAssemblies\V57.GameForge.Runtime.dll"
  $editorSrc = Join-Path $ProjectPath "Library\ScriptAssemblies\V57.GameForge.Editor.dll"
  if (-not (Test-Path $runtimeSrc) -or -not (Test-Path $editorSrc)) {
    Write-Error "ScriptAssemblies missing. Compile in Unity first, or omit -SkipUnity."
  }

  # Mirror ExportForRelease without Unity (meta + package copy)
  if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
  New-Item -ItemType Directory -Force -Path $dist | Out-Null
  $pkg = Join-Path $ProjectPath "Packages\com.v57.unity-game-forge"
  Copy-Item (Join-Path $pkg "package.json") (Join-Path $dist "package.json")
  $uiSrc = Join-Path $pkg "UI"
  if (Test-Path $uiSrc) {
    Copy-Item $uiSrc (Join-Path $dist "UI") -Recurse
    Get-ChildItem (Join-Path $dist "UI") -Recurse -File |
      Where-Object { $_.Extension -notin ".uss", ".uxml", ".meta", ".json" } |
      Remove-Item -Force
  }
  $rtPlugins = Join-Path $dist "Runtime\Plugins"
  $edPlugins = Join-Path $dist "Editor\Plugins"
  New-Item -ItemType Directory -Force -Path $rtPlugins, $edPlugins | Out-Null
  Copy-Item $runtimeSrc (Join-Path $rtPlugins "V57.GameForge.Runtime.dll")
  Copy-Item $editorSrc (Join-Path $edPlugins "V57.GameForge.Editor.dll")

  @"
fileFormatVersion: 2
guid: a7f31c2e4b5d6a7890abcdef12345601
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 1
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Set-Content -Path (Join-Path $rtPlugins "V57.GameForge.Runtime.dll.meta") -Encoding utf8

  @"
fileFormatVersion: 2
guid: b8e42d3f5c6e7b8901bcdef234567802
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        DefaultValueInitialized: true
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Set-Content -Path (Join-Path $edPlugins "V57.GameForge.Editor.dll.meta") -Encoding utf8

  @"
# V57 Unity GameForge (release package)

Precompiled DLLs + UI. No C# sources.
Rebuild: Tools/build-package-dlls.ps1
"@ | Set-Content -Path (Join-Path $dist "README.md") -Encoding utf8

  "V57.GameForge precompiled UPM package`n$(Get-Date -Format o)" |
    Set-Content -Path (Join-Path $dist ".gameforge-dll-package") -Encoding utf8
}

# Validate Dist has no .cs of GameForge logic
$cs = @()
if (Test-Path $dist) {
  $cs = @(Get-ChildItem $dist -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue)
}
if ($cs.Count -gt 0) {
  Write-Error ("Dist still contains .cs files:`n" + ($cs.FullName -join "`n"))
}

$rt = Join-Path $dist "Runtime\Plugins\V57.GameForge.Runtime.dll"
$ed = Join-Path $dist "Editor\Plugins\V57.GameForge.Editor.dll"
if (-not (Test-Path $rt) -or -not (Test-Path $ed)) {
  Write-Error "Dist missing DLLs. Export may have failed."
}

Write-Host "OK - release package ready:"
Write-Host "  $dist"
Write-Host "  Runtime DLL: $((Get-Item $rt).Length) bytes"
Write-Host "  Editor DLL:  $((Get-Item $ed).Length) bytes"
Write-Host "Install: Package Manager -> Add package from disk -> Dist/com.v57.unity-game-forge"
