<#

    The purpose of this build file is to version the PowerShell module.

#>

#====================================================================================================================================================

"`n"
'Proposed version number information'
$Separator[1..35] -join ''

# Output version info to build log.
'$GitVersionMajorMinorPatch : ' + $GitVersionMajorMinorPatch
'$GitVersionNuGetPreReleaseTagV2 : ' + $GitVersionNuGetPreReleaseTagV2
"`n"

##########################
# Update Version Numbers #
##########################

# Set the new version number in the staged Module Manifest.
"Updating module manifest : $ModuleManifest"
$Separator[1..24] -join ''
$manifestContent = Get-Content -Path $ModuleManifest -Raw
$manifestContent = $manifestContent -replace "(?<=ModuleVersion\s+=\s+')(?<ModVer>.*)(?=')", $GitVersionMajorMinorPatch
if ($GitVersionNuGetPreReleaseTagV2.Length -gt 0) {
    $manifestContent = $manifestContent -replace "(# Prerelease = 'alpha')","Prerelease = '-$GitVersionNuGetPreReleaseTagV2'"
} else {
    $manifestContent = $manifestContent -replace "(  Prerelease = '.*?')","  # Prerelease = 'alpha'"
}
Set-Content -Path $ModuleManifest -Value $manifestContent -NoNewLine -Force -Verbose:$IsVerbose

"`n"

# Set the new version number in the staged project file.
"Updating project file : $ProjectFile"
$Separator[1..24] -join ''
$projectFileContent = Get-Content -Path $ProjectFile -Raw
$projectFileContent = $projectFileContent -replace "(?<=<Version>)(?<ModVer>.*)(?=<)", $GitVersionMajorMinorPatch
$projectFileContent = $projectFileContent -replace "(?<=<AssemblyVersion>)(?<ModVer>.*)(?=<)", $GitVersionAssemblySemVer
$projectFileContent = $projectFileContent -replace "(?<=<FileVersion>)(?<ModVer>.*)(?=<)", $GitVersionAssemblySemVer
Set-Content -Path $ProjectFile -Value $projectFileContent -NoNewLine -Force -Verbose:$IsVerbose

"`n"
