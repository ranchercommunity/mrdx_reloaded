# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Qol.BattleTimer/*" -Force -Recurse
dotnet publish "./MRDX.Qol.BattleTimer.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Qol.BattleTimer" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location