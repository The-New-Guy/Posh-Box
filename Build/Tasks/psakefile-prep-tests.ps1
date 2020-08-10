<#

    The purpose of this build file is to perform any preparations needed for the up-coming tests.

#>

#====================================================================================================================================================

# These directories will later hold our test results and coverage reports.
New-Item -Type Directory -Path $ReportsDirectory
New-Item -Type Directory -Path $TestResultsDirectory
