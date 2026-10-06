#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Basic structural validation for V57 feature/system spec YAML files.

.PARAMETER SpecPath
  Path to a .yaml spec file.

.PARAMETER SchemaPath
  Optional JSON schema path (default: V57/tools/schema/feature-spec.schema.json).

.EXAMPLE
  ./V57/tools/validate-spec.ps1 -SpecPath V57/specs/<slug>/features/player_movement.yaml
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SpecPath,

    [string]$SchemaPath = (Join-Path $PSScriptRoot 'schema/feature-spec.schema.json')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $SpecPath)) {
    Write-Error "Spec file not found: $SpecPath"
    exit 1
}

$requiredKeys = @(
    'specVersion', 'name', 'type', 'dependencies', 'components',
    'acceptanceCriteria', 'validationGates'
)

$yaml = Get-Content -LiteralPath $SpecPath -Raw -Encoding UTF8
$missing = @()

foreach ($key in $requiredKeys) {
    if ($yaml -notmatch "(?m)^$([regex]::Escape($key)):\s*") {
        $missing += $key
    }
}

if ($missing.Count -gt 0) {
    Write-Error "Missing required keys: $($missing -join ', ')"
    exit 1
}

if ($yaml -match '(?m)^type:\s*(\S+)') {
    $type = $Matches[1].Trim('"', "'")
    if ($type -notin @('feature', 'system', 'mechanic')) {
        Write-Error "Invalid type: $type (expected feature|system|mechanic)"
        exit 1
    }
}

if ($yaml -notmatch '(?m)^acceptanceCriteria:\s*') {
    Write-Error 'acceptanceCriteria must be present and non-empty'
    exit 1
}

if (-not (Test-Path -LiteralPath $SchemaPath)) {
    Write-Warning "Schema not found at $SchemaPath — structural key check only."
}
else {
    Write-Host "Schema reference: $SchemaPath (full JSON Schema validation optional — install a YAML validator to enforce)."
}

Write-Host "PASS: $SpecPath"
exit 0
