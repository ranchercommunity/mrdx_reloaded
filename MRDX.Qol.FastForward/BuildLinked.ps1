# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Qol.FastForward/*" -Force -Recurse
dotnet publish "./MRDX.Qol.FastForward.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Qol.FastForward" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location