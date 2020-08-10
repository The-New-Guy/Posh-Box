<#

    The purpose of this build file is to fully initialize the build process of this PowerShell project. Here you would load any prerequisites needed
    to build the project.

#>

#====================================================================================================================================================

Set-Location -Path $SourceRoot

#~~~~~~~~~~DO INIT STUFF HERE~~~~~~~~~~#

# Init complete. Output some details about the environment to the build log.

"`n"

'Environment Variables:'
$Separator
Get-ChildItem -Path env:

"`n"

'PowerShell Details:'
$Separator
$PSVersionTable

"`n"

'PowerShell Registered Repositories'
$Separator
Get-PSRepository

"`n"
