$projects =  Get-ChildItem -Path ..\*\*.csproj -Recurse -Force;
Write-Host Generating Packages;
$ticks=(Get-Date).Ticks;
$projects | Foreach-Object { 
 dotnet pack $_.FullName  --output "../packages" --version-suffix "beta.$($ticks)" --include-source --configuration "Debug"
}