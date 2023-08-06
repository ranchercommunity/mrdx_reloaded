# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Game.Randomizer/*" -Force -Recurse
dotnet publish "./MRDX.Game.Randomizer.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Game.Randomizer" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location