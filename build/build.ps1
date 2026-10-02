$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'Build/Build.csproj'
dotnet run --project $project -- @args
exit $LASTEXITCODE
