# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Base.BattleSimulator/*" -Force -Recurse
dotnet publish "./MRDX.Base.BattleSimulator.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Base.BattleSimulator" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location