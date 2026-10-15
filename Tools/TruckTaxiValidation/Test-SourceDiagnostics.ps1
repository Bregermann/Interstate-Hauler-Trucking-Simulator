param(
    [string]$ProjectRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [string]$ResponseDirectory = 'Library/Bee/artifacts/1900b0aE.dag'
)
$ErrorActionPreference = 'Stop'
# Roslyn diagnostics only: no Unity process, compiler executable, assembly emission, or build output.
Add-Type -TypeDefinition 'public static class TruckTaxiDiagnosticsHost {}'
$compilations = @{}
$errors = 0
foreach ($item in @(
    @{ Name='LWS.TruckTaxi.Runtime'; Folder='Scripts' },
    @{ Name='LWS.TruckTaxi.Editor'; Folder='Editor' },
    @{ Name='LWS.TruckTaxi.Tests.EditMode'; Folder='Tests/EditMode' }
)) {
    $rsp = Join-Path $ProjectRoot "$ResponseDirectory/$($item.Name).rsp"
    if (!(Test-Path -LiteralPath $rsp)) { throw "Missing cached Unity response file: $rsp" }
    $parse = [Microsoft.CodeAnalysis.CSharp.CSharpCommandLineParser].GetMethods() |
        Where-Object { $_.Name -eq 'Parse' -and $_.ReturnType.Name -eq 'CSharpCommandLineArguments' }
    $arguments = $parse.Invoke([Microsoft.CodeAnalysis.CSharp.CSharpCommandLineParser]::Default,
        [object[]]@([string[]](Get-Content -LiteralPath $rsp), $ProjectRoot, $null, $null))
    $references = [Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
    foreach ($reference in $arguments.MetadataReferences) {
        $name = [IO.Path]::GetFileNameWithoutExtension($reference.Reference)
        if ($name.EndsWith('.ref')) { $name = $name.Substring(0, $name.Length - 4) }
        if ($compilations.ContainsKey($name)) {
            $references.Add($compilations[$name].ToMetadataReference())
        } else {
            $path = $reference.Reference
            if (![IO.Path]::IsPathRooted($path)) { $path = Join-Path $ProjectRoot $path }
            if (!(Test-Path -LiteralPath $path)) { throw "Missing cached dependency: $path" }
            $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path, $reference.Properties))
        }
    }
    $trees = [Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $ProjectRoot "Assets/LWS/TruckTaxi/$($item.Folder)") -Filter '*.cs' -Recurse) {
        $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText(
            (Get-Content -LiteralPath $file.FullName -Raw), $arguments.ParseOptions, $file.FullName))
    }
    $compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create(
        $item.Name, $trees, $references, $arguments.CompilationOptions)
    $compilations[$item.Name] = $compilation
    $diagnostics = @($compilation.GetDiagnostics() | Where-Object Severity -eq 'Error')
    $errors += $diagnostics.Count
    "$($item.Name): $($trees.Count) source files, $($diagnostics.Count) errors (diagnostics only, no emission)."
    foreach ($diagnostic in $diagnostics) { $diagnostic.ToString() }
}
if ($errors -gt 0) { throw "$errors source diagnostic errors." }
