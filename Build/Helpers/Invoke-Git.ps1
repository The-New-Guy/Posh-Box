<#

    Helper function that collects git parameters and executes them.

#>

#====================================================================================================================================================

function Invoke-Git {

    <#

        .SYNOPSIS

            Safely execute a Git command.

    #>

    param([Parameter(Mandatory = $true)] [string[]]$GitParameters)

    try {
        "Executing 'git $($GitParameters -join ' ')'"
        exec { & git $GitParameters }
    } catch {
        Write-Warning -Message $_
    }

}
