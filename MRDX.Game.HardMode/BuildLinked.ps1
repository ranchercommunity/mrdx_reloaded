# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Base.HardMode/*" -Force -Recurse
dotnet publish "./MRDX.Base.HardMode.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Base.HardMode" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location