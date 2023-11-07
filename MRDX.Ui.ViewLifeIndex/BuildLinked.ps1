# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Ui.ViewLifeIndex/*" -Force -Recurse
dotnet publish "./MRDX.Ui.ViewLifeIndex.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Ui.ViewLifeIndex" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location