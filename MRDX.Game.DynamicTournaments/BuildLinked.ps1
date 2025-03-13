# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Game.ABD_Tournaments/*" -Force -Recurse
dotnet publish "./MRDX.Game.ABD_Tournaments.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Game.ABD_Tournaments" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location