Write-Output "build: Build started"

Push-Location $PSScriptRoot

if(Test-Path .\artifacts) {
    Write-Output "build: Cleaning .\artifacts"
    Remove-Item .\artifacts -Force -Recurse
}

$branch = @{ $true = $env:APPVEYOR_REPO_BRANCH; $false = $(git symbolic-ref --short -q HEAD) }[$env:APPVEYOR_REPO_BRANCH -ne $NULL];
$revision = @{ $true = "{0:00000}" -f [convert]::ToInt32("0" + $env:APPVEYOR_BUILD_NUMBER, 10); $false = "local" }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
$suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)))-$revision"}[$branch -eq "main" -and $revision -ne "local"]
$commitHash = $(git rev-parse --short HEAD)
$buildSuffix = @{ $true = "$($suffix)-$($commitHash)"; $false = "$($branch)-$($commitHash)" }[$suffix -ne ""]

Write-Output "build: Package version suffix is $suffix"
Write-Output "build: Build version suffix is $buildSuffix"

& dotnet build --configuration Release --version-suffix=$buildSuffix /p:ContinuousIntegrationBuild=true

if($LASTEXITCODE -ne 0) { throw 'build failed' }

if($suffix) {
    & dotnet pack src\Serilog.Settings.Configuration --configuration Release --no-build --no-restore -o artifacts --version-suffix=$suffix
} else {
    & dotnet pack src\Serilog.Settings.Configuration --configuration Release --no-build --no-restore -o artifacts
}

if($LASTEXITCODE -ne 0) { throw 'pack failed' }

Write-Output "build: Testing"

# Dotnet test doesn't run separate TargetFrameworks in parallel: https://github.com/dotnet/sdk/issues/19147
# Workaround: use `dotnet test` on dlls directly in order to pass the `--parallel` option to vstest.
# The _reported_ runtime is wrong but the _actual_ used runtime is correct, see https://github.com/microsoft/vstest/issues/2037#issuecomment-720549173
& dotnet test test\Serilog.Settings.Configuration.Tests\bin\Release\*\Serilog.Settings.Configuration.Tests.dll --parallel

if($LASTEXITCODE -ne 0) { throw 'unit tests failed' }