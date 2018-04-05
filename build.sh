#!/bin/bash

set -e 
dotnet --info
dotnet restore

for path in src/**/*.csproj; do
    dotnet build -f netstandard1.6 -c Release ${path}
done

for path in test/*.Tests/*.csproj; do
    dotnet test -f netcoreapp2.0  -c Release ${path}
done

cd sample/Sample/
dotnet build -f netcoreapp2.0 -c Release 
dotnet bin/Release/netcoreapp2.0/Sample.dll --run-once
