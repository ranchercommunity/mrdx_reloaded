# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Qol.MagicBananaStatic/*" -Force -Recurse
dotnet publish "./MRDX.Qol.MagicBananaStatic.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Qol.MagicBananaStatic" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location