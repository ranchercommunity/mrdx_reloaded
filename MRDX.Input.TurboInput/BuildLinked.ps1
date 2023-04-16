# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/MRDX.Input.TurboInput/*" -Force -Recurse
dotnet publish "./MRDX.Input.TurboInput.csproj" -c Release -o "$env:RELOADEDIIMODS/MRDX.Input.TurboInput" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location