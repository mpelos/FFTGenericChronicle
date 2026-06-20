# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/fftivc.generic.chronicle.codemod/*" -Force -Recurse
dotnet publish "./fftivc.generic.chronicle.codemod.csproj" -c Release -o "$env:RELOADEDIIMODS/fftivc.generic.chronicle.codemod" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location