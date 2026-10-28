$ErrorActionPreference='Stop'
Add-Type -TypeDefinition 'public static class FashionShowDiagnosticsHost {}'
[string]$validation=Join-Path (Get-Location) '.sports-validation/TruckSportsPublicValidation'
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
$files=@(Get-ChildItem Assets/TruckFashionShow -Filter '*.cs' -Recurse)
$trees=[Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
foreach($file in $files){$trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText((Get-Content -LiteralPath $file.FullName -Raw),$options.ParseOptions,$file.FullName))}
$comp=[Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('FashionShowSourceDiagnostics',$trees,$references,$options.CompilationOptions)
$errors=@($comp.GetDiagnostics()|Where-Object Severity -eq 'Error')
"FashionShow diagnostics: $($files.Count) sources, $($references.Count) references, $($errors.Count) errors; no emission."
$errors|ForEach-Object {$_.ToString()}
if($errors.Count -gt 0){exit 1}

