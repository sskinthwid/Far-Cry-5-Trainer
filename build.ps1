param([string]$OutputDirectory='build')
$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
$outputPath=Join-Path $projectRoot $OutputDirectory
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$references=@('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll') | ForEach-Object { '/r:'+(Join-Path $wpf $_) }
$sourceFiles=Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -Recurse | ForEach-Object FullName
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /r:System.Xaml.dll /r:System.Web.Extensions.dll /r:System.Xml.Linq.dll $references "/out:$outputPath\HopeControl.exe" "/resource:$projectRoot\src\catalog.json,catalog.json" $sourceFiles
if($LASTEXITCODE -ne 0){throw 'C# build failed.'}
Write-Output "Built $outputPath\HopeControl.exe"
