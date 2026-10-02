$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
[xml]$versions = Get-Content (Join-Path $repo 'version.props')
$version = $versions.Project.PropertyGroup.PackageVersion
$root = Join-Path $repo 'artifacts/consumer'
if (Test-Path $root) {
    Remove-Item $root -Recurse -Force
}
$feed = Join-Path $root 'feed'
$packages = Join-Path $root 'packages'
New-Item $feed -ItemType Directory -Force | Out-Null
$package = Join-Path $repo "artifacts/packages/Release/AaronSalisbury.DotNetFrameworkToolkit.$version.nupkg"
Copy-Item $package $feed
# A local feed guarantees that this build's toolkit package is installed, even when
# the same version already exists on nuget.org. Dependencies come from the restore.
Get-ChildItem (Join-Path $repo 'src/packages') -Recurse -Filter '*.nupkg' | Copy-Item -Destination $feed
& nuget install AaronSalisbury.DotNetFrameworkToolkit -Version $version -Framework net20 -Source $feed -OutputDirectory $packages -NonInteractive -NoCache -DirectDownload
if ($LASTEXITCODE -ne 0) {
    throw 'Consumer runtime package installation failed.'
}
& nuget install Microsoft.NETFramework.ReferenceAssemblies.net20 -Version 1.0.3 -Source $feed -OutputDirectory $packages -NonInteractive -NoCache -DirectDownload
if ($LASTEXITCODE -ne 0) {
    throw 'Consumer reference assembly installation failed.'
}
$installedPackage = Join-Path $packages "AaronSalisbury.DotNetFrameworkToolkit.$version/AaronSalisbury.DotNetFrameworkToolkit.$version.nupkg"
if ((Get-FileHash $package).Hash -ne (Get-FileHash $installedPackage).Hash) {
    throw 'NuGet installed a different toolkit package.'
}
$results = @()
foreach ($architecture in @('x86', 'x64')) {
    & msbuild (Join-Path $repo 'samples/ConsumerSmoke/ConsumerSmoke.csproj') /t:Rebuild /p:Configuration=Release "/p:ConsumerRoot=$root" "/p:PlatformTarget=$architecture" /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Consumer compilation failed for $architecture."
    }
    $output = Join-Path $root $architecture
    $installedDll = Join-Path $packages "AaronSalisbury.DotNetFrameworkToolkit.$version/lib/net20/DotNetFrameworkToolkit.dll"
    if ((Get-FileHash $installedDll).Hash -ne (Get-FileHash (Join-Path $output 'DotNetFrameworkToolkit.dll')).Hash) {
        throw 'Consumer does not use the installed package binary.'
    }
    $pointerSize = if ($architecture -eq 'x86') { 4 } else { 8 }
    $log = & (Join-Path $output 'ConsumerSmoke.exe') $pointerSize 2>&1
    $exitCode = $LASTEXITCODE
    $log | Tee-Object -FilePath (Join-Path $root "$architecture.log") | Write-Host
    if ($exitCode -ne 0) {
        throw "CLR 2.0 consumer execution failed for $architecture."
    }
    $results += [ordered]@{ Architecture = $architecture; Runtime = 'CLR 2.0'; Passed = $true }
}
[ordered]@{
    PackageVersion = $version
    PackageSha256 = (Get-FileHash $package).Hash
    OperatingSystem = [Environment]::OSVersion.VersionString
    Host = 'Windows with .NET Framework 3.5 (CLR 2.0) enabled'
    Consumers = $results
    Unverified = @('Framework 2.0-only installations', 'Historical Windows platforms')
} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $root 'validation.json')
