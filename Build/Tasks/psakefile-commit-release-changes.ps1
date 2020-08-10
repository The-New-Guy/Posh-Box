<#

    The purpose of this build file is to commit any changes made during the build process of a release branch to the repository.

#>

# Only do this if running as part of a build system.

## Azure DevOps/VSTS ##

if ($env:BUILD_DEFINITIONNAME) {

    'Updating source repository for production version numbers'
    $Separator[1..57] -join ''

    "`n"

    "Updating Branch : '$($env:BUILD_SOURCEBRANCH.Replace('refs/heads/',''))'"

    $commitMessage = $env:BUILD_SOURCEVERSIONMESSAGE.TrimEnd()
    "Commit Message : '$commitMessage'"

    "`n"

    if ($commitMessage -match '^Azure DevOps Build updating Version Number to .*') {
        "Skipping update to '$($env:BUILD_SOURCEBRANCH.Replace('refs/heads/',''))' branch with build changes because this build was triggered by Azure DevOps Updating the Version Number"
    } else {

        Set-Location -Path $SourceRoot
        Invoke-Git -GitParameters @('config', '--global', 'user.name', 'AzureDevopsBuildService')
        Invoke-Git -GitParameters @('config', '--global', 'user.email', 'UITS-CCI@users.noreply.github.iu.edu')

        Invoke-Git -GitParameters @('checkout', $env:BUILD_SOURCEBRANCH.Replace('refs/heads/',''))

        "Pushing build changes to '$($env:BUILD_SOURCEBRANCH.Replace('refs/heads/',''))'"
        $Separator
        Invoke-Git -GitParameters @('add', '.')
        Invoke-Git -GitParameters @('commit', '-m', "Azure DevOps Build updating Version Number to $GitVersionMajorMinorPatch")
        Invoke-Git -GitParameters @('status')
        Invoke-Git -GitParameters @('push', 'origin', $env:BUILD_SOURCEBRANCH)

    }

} else {
    "Skipping update to repository with build changes because build system is not supported"
}
