# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Ui.RawTechValues/*" -Force -Recurse
dotnet publish "./MRDX.Ui.RawTechValues.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Ui.RawTechValues" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location