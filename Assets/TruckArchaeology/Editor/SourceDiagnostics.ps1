$ErrorActionPreference='Stop'
Add-Type -TypeDefinition 'public static class ArchaeologyActualVendorDiagnosticsHost {}'
[string]$validation=Join-Path (Get-Location) '.sports-validation/TruckSportsPublicValidation'
if(!(Test-Path -LiteralPath (Join-Path $validation 'Library/ScriptAssemblies/Assembly-CSharp.dll'))){throw 'Actual cached Sports runtime assembly is temporarily unavailable while lead Editor recompiles; retry after that operation. No reference stubs are permitted.'}
$parse=[Microsoft.CodeAnalysis.CSharp.CSharpCommandLineParser].GetMethods()|Where-Object { $_.Name -eq 'Parse' -and $_.ReturnType.Name -eq 'CSharpCommandLineArguments' }
$references=[Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$seen=@{}
foreach($rspName in @('Assembly-CSharp-Editor','LWS.TruckSports.GoddessPlayModeTests','LWS.TruckSports.PresentationEditModeTests')){
 $rsp=Join-Path $validation "Library/Bee/artifacts/1900b0aE.dag/$rspName.rsp"
 $arguments=$parse.Invoke([Microsoft.CodeAnalysis.CSharp.CSharpCommandLineParser]::Default,[object[]]@([string[]](Get-Content -LiteralPath $rsp),$validation,$null,$null))
 if($rspName -eq 'Assembly-CSharp-Editor'){$options=$arguments}
 foreach($reference in $arguments.MetadataReferences){$path=$reference.Reference;if(![IO.Path]::IsPathRooted($path)){$path=Join-Path $validation $path};if((Test-Path -LiteralPath $path) -and !$seen.ContainsKey($path)){$seen[$path]=$true;$references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path,$reference.Properties))}}
}
$cachedEditor=Join-Path $validation 'Library/ScriptAssemblies/Assembly-CSharp-Editor.dll'
if(!$seen.ContainsKey($cachedEditor)){$references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($cachedEditor))}
$vendorFiles=@(Get-ChildItem 'Logs/TruckSports/Impossible07/ArchaeologyDiggerIntake/UnmodifiedPackage/Assets/Digger/Modules' -Filter '*.cs' -Recurse)
$ownedFiles=@(Get-ChildItem 'Assets/TruckArchaeology' -Filter '*.cs' -Recurse)
$files=$vendorFiles+$ownedFiles+@(Get-Item Assets/TruckJenga/Scripts/JengaCrane.cs,Assets/TruckSports/Scripts/ISportsWorkshopState.cs)
$symbols=@($options.ParseOptions.PreprocessorSymbolNames)+@('USING_URP','USING_URP_14_OR_ABOVE','__DIGGER__','__DIGGER_RUNTIME__')
$parseOptions=$options.ParseOptions.WithPreprocessorSymbols([string[]]$symbols)
$trees=[Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
foreach($file in $files){$trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText((Get-Content -LiteralPath $file.FullName -Raw),$parseOptions,$file.FullName))}
$comp=[Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('ArchaeologyActualVendorSourceDiagnostics',$trees,$references,$options.CompilationOptions.WithAllowUnsafe($true))
$errors=@($comp.GetDiagnostics()|Where-Object Severity -eq 'Error')
"ArchaeologyActualVendor diagnostics: $($files.Count) sources ($($ownedFiles.Count) owned + $($vendorFiles.Count) real vendor +2 existing dependency), $($references.Count) references, $($errors.Count) errors; no emission."
$errors|ForEach-Object {$_.ToString()}
if($errors.Count -gt 0){exit 1}

