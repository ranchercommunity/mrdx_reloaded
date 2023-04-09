
# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

$MakeDelta = true

./Publish.ps1 -ProjectPath "MRDX.Audio.VolumeConfig/MRDX.Audio.VolumeConfig.csproj" `
              -PackageName "MRDX.Audio.VolumeConfig" `
			  -ReadmePath ./MRDX.Audio.VolumeConfig/README.md `
              -PublishOutputDir "Publish/ToUpload/MRDX.Audio.VolumeConfig" `
              -MakeDelta $MakeDelta -UseGitHubDelta $MakeDelta `
              -MetadataFileName "MRDX.Audio.VolumeConfig.ReleaseMetadata.json" `
              -GitHubUserName jroweboy -GitHubRepoName mrdx_reloaded -GitHubInheritVersionFromTag false `
			  @args

./Publish.ps1 -ProjectPath "MRDX.Base.Mod/MRDX.Base.Mod.csproj" `
              -PackageName "MRDX.Base.Mod" `
			  -ReadmePath ./MRDX.Base.Mod/README.md `
              -PublishOutputDir "Publish/ToUpload/MRDX.Base.Mod" `
              -MakeDelta $MakeDelta -UseGitHubDelta $MakeDelta `
              -MetadataFileName "MRDX.Base.Mod.ReleaseMetadata.json" `
              -GitHubUserName jroweboy -GitHubRepoName mrdx_reloaded -GitHubInheritVersionFromTag false `
			  @args

./Publish.ps1 -ProjectPath "MRDX.Base.ExtractDataBin/MRDX.Base.ExtractDataBin.csproj" `
              -PackageName "MRDX.Base.ExtractDataBin" `
			  -ReadmePath ./MRDX.Base.ExtractDataBin/README.md `
              -PublishOutputDir "Publish/ToUpload/MRDX.Base.ExtractDataBin" `
              -MakeDelta $MakeDelta -UseGitHubDelta $MakeDelta `
              -MetadataFileName "MRDX.Base.ExtractDataBin.ReleaseMetadata.json" `
              -GitHubUserName jroweboy -GitHubRepoName mrdx_reloaded -GitHubInheritVersionFromTag false `
			  @args

./Publish.ps1 -ProjectPath "MRDX.Game.HardMode/MRDX.Game.HardMode.csproj" `
              -PackageName "MRDX.Game.HardMode" `
			  -ReadmePath ./MRDX.Game.HardMode/README.md `
              -PublishOutputDir "Publish/ToUpload/MRDX.Game.HardMode" `
              -MakeDelta $MakeDelta -UseGitHubDelta $MakeDelta `
              -MetadataFileName "MRDX.Game.HardMode.ReleaseMetadata.json" `
              -GitHubUserName jroweboy -GitHubRepoName mrdx_reloaded -GitHubInheritVersionFromTag false `
			  @args

./Publish.ps1 -ProjectPath "MRDX.Graphics.Widescreen/MRDX.Graphics.Widescreen.csproj" `
              -PackageName "MRDX.Graphics.Widescreen" `
			  -ReadmePath ./MRDX.Graphics.Widescreen/README.md `
              -PublishOutputDir "Publish/ToUpload/MRDX.Graphics.Widescreen" `
              -MakeDelta $MakeDelta -UseGitHubDelta $MakeDelta `
              -MetadataFileName "MRDX.Graphics.Widescreen.ReleaseMetadata.json" `
              -GitHubUserName jroweboy -GitHubRepoName mrdx_reloaded -GitHubInheritVersionFromTag false `
			  @args

./Publish.ps1 -ProjectPath "MRDX.Qol.FastForward/MRDX.Qol.FastForward.csproj" `
              -PackageName "MRDX.Qol.FastForward" `
			  -ReadmePath ./MRDX.Qol.FastForward/README.md `
              -PublishOutputDir "Publish/ToUpload/MRDX.Qol.FastForward" `
              -MakeDelta $MakeDelta -UseGitHubDelta $MakeDelta `
              -MetadataFileName "MR2DX.Qol.SkipDrillAnim.FastForward.json" `
              -GitHubUserName jroweboy -GitHubRepoName mrdx_reloaded -GitHubInheritVersionFromTag false `
			  @args

./Publish.ps1 -ProjectPath "MRDX.Qol.SkipDrillAnim/MRDX.Qol.SkipDrillAnim.csproj" `
              -PackageName "MRDX.Qol.SkipDrillAnim" `
			  -ReadmePath ./MRDX.Qol.SkipDrillAnim/README.md `
              -PublishOutputDir "Publish/ToUpload/MRDX.Qol.SkipDrillAnim" `
              -MakeDelta $MakeDelta -UseGitHubDelta $MakeDelta `
              -MetadataFileName "MR2DX.Qol.SkipDrillAnim.ReleaseMetadata.json" `
              -GitHubUserName jroweboy -GitHubRepoName mrdx_reloaded -GitHubInheritVersionFromTag false `
			  @args

Pop-Location
