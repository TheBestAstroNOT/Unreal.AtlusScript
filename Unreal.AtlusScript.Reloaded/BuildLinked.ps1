# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/Unreal.AtlusScript.Reloaded/*" -Force -Recurse
dotnet publish "./Unreal.AtlusScript.Reloaded.csproj" -c Release -o "$env:RELOADEDIIMODS/Unreal.AtlusScript.Reloaded" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location