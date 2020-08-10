<#

    This is the main psake build file that should be invoked. All other psake build files are merely called from this one and cannot be invoked alone.

#>

#=====================================================================================================================================================

## Build Properties ##

# When Invoke-psake is called, the properties below will be available to each of the task scriptblocks. Any parameters provided to the Invoke-psake
# command via the -Parameters argument will also be available to all scriptblocks, including the Properties scriptblock below. Many of the properties
# below will first check to see if a parameter by the same name has been given, if not, set it to a build system property or default value.

Properties {

    ######################################
    ## Parameter Based Build Properties ##
    ######################################

    Write-Output "Build Properties"
    Write-Output "----------------"

    ##----------

    if (-not $ProjectName) {
        if ($env:CUSTOM_PROJECTNAME) { $ProjectName = $env:CUSTOM_PROJECTNAME }
        elseif ($env:SYSTEM_TEAMPROJECT) { $ProjectName = $env:SYSTEM_TEAMPROJECT }
        else { $ProjectName = 'Posh-Box' }
    }

    Write-Output "ProjectName : $ProjectName"

    ##----------

    if (-not $SourceRoot) {
        if ($env:CUSTOM_SOURCEROOT) { $SourceRoot = $env:CUSTOM_SOURCEROOT }
        elseif ($env:BUILD_SOURCESDIRECTORY) { $SourceRoot = $env:BUILD_SOURCESDIRECTORY }
        else { $SourceRoot = Resolve-Path -Path "$PSScriptRoot\.." }
    }

    Write-Output "SourceRoot : $SourceRoot"

    ##----------

    if (-not $ArtifactsRoot) {
        if ($env:CUSTOM_ARTIFACTSROOT) { $ArtifactsRoot = $env:CUSTOM_ARTIFACTSROOT }
        elseif ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) { $ArtifactsRoot = $env:BUILD_ARTIFACTSTAGINGDIRECTORY }
        else { $ArtifactsRoot = Resolve-Path -Path "$PSScriptRoot\.." }
    }

    Write-Output "ArtifactsRoot : $ArtifactsRoot"

    ##----------

    if (-not $BuildScriptDirectory) {
        $BuildScriptDirectory = Join-Path -Path $SourceRoot -ChildPath 'Build'
    }

    Write-Output "BuildScriptDirectory : $BuildScriptDirectory"

    ##----------

    if ($null -eq $IsVerbose) { $IsVerbose = if ($VerbosePreference -ne 'SilentlyContinue') { $true } else { $false } }
    else { $IsVerbose = [bool]$IsVerbose }  # Make sure it is a bool.

    Write-Output "IsVerbose : $IsVerbose"

    ##----------

    if (-not $GitVersionMajorMinorPatch) {
        if ($env:GITVERSION_MAJORMINORPATCH) { $GitVersionMajorMinorPatch = $env:GITVERSION_MAJORMINORPATCH }
        elseif (Get-Command -Name 'GitVersion.exe' -ErrorAction SilentlyContinue) { $GitVersionMajorMinorPatch = (GitVersion.exe | ConvertFrom-Json).MajorMinorPatch }
        else { $GitVersionMajorMinorPatch = '0.1.0' }
    }

    Write-Output "GitVersionMajorMinorPatch : $GitVersionMajorMinorPatch"

    ##----------

    if (-not $GitVersionFullSemVer) {
        if ($env:GITVERSION_FULLSEMVER) { $GitVersionFullSemVer = $env:GITVERSION_FULLSEMVER }
        elseif (Get-Command -Name 'GitVersion.exe' -ErrorAction SilentlyContinue) { $GitVersionFullSemVer = (GitVersion.exe | ConvertFrom-Json).FullSemVer }
        else { $GitVersionFullSemVer = '0.1.0-wtf.1' }
    }

    Write-Output "GitVersionFullSemVer : $GitVersionFullSemVer"

    ##----------

    if (-not $GitVersionNuGetPreReleaseTagV2) {
        if ($env:GITVERSION_NUGETPRERELEASETAGV2) { $GitVersionNuGetPreReleaseTagV2 = $env:GITVERSION_NUGETPRERELEASETAGV2 }
        elseif ($env:GITVERSION_BRANCHNAME -eq 'master') { $GitVersionNuGetPreReleaseTagV2 = '' }
        elseif (Get-Command -Name 'GitVersion.exe' -ErrorAction SilentlyContinue) { $GitVersionNuGetPreReleaseTagV2 = (GitVersion.exe | ConvertFrom-Json).NuGetPreReleaseTagV2 }
        else { $GitVersionNuGetPreReleaseTagV2 = '-wtf0001' }
    }
    if ($ExcludePreReleaseTag) { $GitVersionNuGetPreReleaseTagV2 = '' }

    Write-Output "GitVersionNuGetPreReleaseTagV2 : $GitVersionNuGetPreReleaseTagV2 (ExcludePreReleaseTag = $ExcludePreReleaseTag)"

    ##----------

    if (-not $ModuleManifest) {
        $ModuleManifest = Join-Path -Path $ArtifactsRoot -ChildPath (Join-Path -Path $GitVersionFullSemVer -ChildPath (Join-Path -Path 'src' -ChildPath "$ProjectName.psd1"))
    }

    Write-Output "ModuleManifest : $ModuleManifest"

    ##----------

    if (-not $ProjectFile) {
        $ProjectFile = Join-Path -Path $ArtifactsRoot -ChildPath (Join-Path -Path $GitVersionFullSemVer -ChildPath (Join-Path -Path 'src' -ChildPath "$ProjectName.csproj"))
    }

    Write-Output "ProjectFile : $ProjectFile"

    ##----------

    if (-not $ReportsDirectory) { $ReportsDirectory = Join-Path -Path $ArtifactsRoot -ChildPath 'Reports' }

    Write-Output "ReportsDirectory : $ReportsDirectory"

    ##----------

    if (-not $TestResultsDirectory) { $TestResultsDirectory = Join-Path -Path $ArtifactsRoot -ChildPath 'Test-Results' }

    Write-Output "TestResultsDirectory : $TestResultsDirectory"

    ##----------

    if (-not $ModuleRoot) {
        $ModuleRoot = Join-Path -Path $ArtifactsRoot -ChildPath (Join-Path -Path $GitVersionFullSemVer -ChildPath 'src')
    }

    Write-Output "ModuleRoot : $ModuleRoot"

    ####################################
    ## Internal Only Build Properties ##
    ####################################

    $Timestamp = Get-Date -uformat "%Y%m%d-%H%M%S"
    $PSVersion = $PSVersionTable.PSVersion.Major
    $Separator = "$('-' * 80)"

    Write-Output "`n"

}

#=====================================================================================================================================================

## Tasks ##

# Format each task name as such.
FormatTaskName ("`n" + ('-' * 30) + ' Task : {0} ' + ('-' * 30) + "`n")

Task Default -Depends Init

Task Init {

    . (Join-Path -Path $BuildScriptDirectory -ChildPath (Join-Path -Path 'Tasks' -ChildPath 'psakefile-init.ps1'))

}

Task VersionPSModule -Depends Init {

    . (Join-Path -Path $BuildScriptDirectory -ChildPath (Join-Path -Path 'Tasks' -ChildPath 'psakefile-version-ps-module.ps1'))

}

Task PrepTests -Depends Init {

    . (Join-Path -Path $BuildScriptDirectory -ChildPath (Join-Path -Path 'Tasks' -ChildPath 'psakefile-prep-tests.ps1'))

}

Task CommitReleaseChanges -Depends Init {

    . (Join-Path -Path $BuildScriptDirectory -ChildPath (Join-Path -Path 'Helpers' -ChildPath 'Invoke-Git.ps1'))
    . (Join-Path -Path $BuildScriptDirectory -ChildPath (Join-Path -Path 'Tasks' -ChildPath 'psakefile-commit-release-changes.ps1'))

}
