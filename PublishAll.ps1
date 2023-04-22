
# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD


# Name of interface mod, version
# These are kinda hard coded into the publish script for now.
$InterfaceVersions = @{
    BaseExtractDataBinInterface = "1.0.1";
    BaseModInterfaces = "1.0.2"
}

$AllProjects = @(
# Name of mod, version
    @("MRDX.Audio.VolumeConfig", "1.1.1"),
    @("MRDX.Base.ExtractDataBin", "1.0.1"),
    @("MRDX.Base.Mod", "1.0.1"),
    @("MRDX.Game.HardMode", "1.0.1"),
#    Not ready for release yet, so leave it commented out for now
#    @("MRDX.Game.MonsterEditor", "1.0.0"),
    @("MRDX.Graphics.Widescreen", "1.1.1"),
    @("MRDX.Qol.FastForward", "1.3.0"),
    @("MRDX.Qol.SkipDrillAnim", "1.2.1"),
    @("MRDX.Qol.TurboInput", "1.0.0"),
    @("MRDX.Ui.RawTechValues", "1.0.0")
)

Write-Output "Starting Publish All"

foreach ($Project in $AllProjects)
{
    $ProjectName = $Project[0]
    $ProjectVersion = $Project[1]
    (Get-Content "$ProjectName/ModConfig.template.json").replace("{{ MOD_VERSION }}", "$ProjectVersion") | Set-Content "$ProjectName/ModConfig.json"
    Write-Output "Publishing $ProjectName"
    ./Publish.ps1 -ProjectPath "$ProjectName/$ProjectName.csproj" `
          -PackageName "$ProjectName" `
          -ReadmePath "./$ProjectName/README.md" `
          -Version "$ProjectVersion" `
          @InterfaceVersions `
          -PublishOutputDir "Publish/ToUpload/$ProjectName" `
          -MakeDelta true -UseGitHubDelta true `
          -MetadataFileName "$ProjectName.ReleaseMetadata.json" `
          -GitHubUserName jroweboy -GitHubRepoName mrdx_reloaded `
          -GitHubFallbackPattern mod.zip -GitHubInheritVersionFromTag false `
          @args
}

Write-Output "Publishing Complete"

Pop-Location
