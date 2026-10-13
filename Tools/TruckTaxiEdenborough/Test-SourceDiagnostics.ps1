param([string]$ProjectRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference = 'Stop'
# The Editor asmdef now uses Splines' float3 API. Until Unity regenerates its cached
# response files, add exactly that installed reference to the existing diagnostics runner.
$runner = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Tools/TruckTaxiValidation/Test-SourceDiagnostics.ps1') -Raw
$reference = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Library/Bee/artifacts/1900b0aE.dag/LWS.TruckTaxi.Runtime.rsp') |
    Where-Object { $_ -match '^-r:.*Unity.Mathematics(\.ref)?\.dll' } | Select-Object -First 1
if (!$reference) { throw 'Installed Unity.Mathematics reference was not found in the Runtime response file.' }
$path = $reference.Substring(3).Trim('"')
if (![IO.Path]::IsPathRooted($path)) { $path = Join-Path $ProjectRoot $path }
$path = $path.Replace("'", "''")
$insert = "if (`$item.Name -eq 'LWS.TruckTaxi.Editor' -and !(`$references | Where-Object { `$_.Display -like '*Unity.Mathematics*' })) { `$references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile('$path')) }`n    "
$runner = $runner.Replace('    $trees = ', "    $insert`$trees = ")
& ([scriptblock]::Create($runner)) -ProjectRoot $ProjectRoot
