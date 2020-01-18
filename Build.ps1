Write-Output "build: Build started"

Push-Location $PSScriptRoot

if(Test-Path .\artifacts) {
	Write-Output "build: Cleaning .\artifacts"
	Remove-Item .\artifacts -Force -Recurse
}

& dotnet restore --no-cache

$branch = @{ $true = $env:APPVEYOR_REPO_BRANCH; $false = $(git symbolic-ref --short -q HEAD) }[$null -ne $env:APPVEYOR_REPO_BRANCH ];
$revision = @{ $true = "{0:00000}" -f [convert]::ToInt32("0" + $env:APPVEYOR_BUILD_NUMBER, 10); $false = "local" }[$null -ne $env:APPVEYOR_BUILD_NUMBER];
$suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)))-$revision"}[$branch -eq "master" -and $revision -ne "local"]

Write-Output "build: Version suffix is $suffix"

foreach ($src in Get-ChildItem "src/*") {
    Push-Location $src

	Write-Output "build: Packaging project in $src"

    & dotnet pack -c Release -o ..\..\artifacts --version-suffix=$suffix --include-source
    if($LASTEXITCODE -ne 0) { exit 1 }

    Pop-Location
}

foreach ($test in Get-ChildItem "test/*.PerformanceTests") {
    Push-Location $test

	Write-Output "build: Building performance test project in $test"

    & dotnet build -c Release
    if($LASTEXITCODE -ne 0) { exit 2 }

    Pop-Location
}

foreach ($test in Get-ChildItem "test/*.Tests") {
    Push-Location $test

	Write-Output "build: Testing project in $test"

    & dotnet test -c Release
    if($LASTEXITCODE -ne 0) { exit 3 }

    Pop-Location
}

Pop-Location
