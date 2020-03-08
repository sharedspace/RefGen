[CmdletBinding(PositionalBinding=$false)]
param (
    [ValidateScript({$_ -ne $null})]
    [string[]]$BuildProcesses = @('msbuild', 'dotnet', 'vbcscompiler', 'mspdbsrv', 'git'),

    [ValidateScript({$_ -ne $null})]
    [string[]]$AdditionalBuildProcesses = @(), 

    [switch]$KillBuildProcesses, 

    [switch]$DryRun
)

Function Kill-ChildProcesses {
    [CmdletBinding(PositionalBinding=$false)]
    param(
        [int]$ProcessId,
        [string]$Tabs="",
        [switch]$DryRun
    )

    $processName = (Get-Process -Id $ProcessId).Name
    Write-Host "$Tabs[$processName] $ProcessId"

    if (-not $DryRun) {
        Get-Process -Id $processId -ErrorAction SilentlyContinue | % {
            try {
                $_.Kill($true)
            } catch {
                Write-Warning "Could not terminate $processName [$ProcessId] - likely part of current process tree"
                
            }
        }
    }
}

Function Kill-ChildProcessesByName {
    [CmdletBinding(PositionalBinding=$false)]
    param(
        [string]$ProcessName,
        [switch]$DryRun
    )

    $p = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue 
    if (-not $p) {
        Write-Verbose "Kill-ChildProcessesByName: Process $ProcessName not found - skipping"
    }
    else {
        $p | % {
            Kill-ChildProcesses -ProcessId $_.Id -DryRun:$DryRun
        }
    }
}

Function Get-PowershellCore {
    [CmdletBinding(PositionalBinding=$false)]
    param(
        [string]$Version
    )

    if ((Get-Variable -Name PowerShellCorePath -Scope Script -ErrorAction SilentlyContinue -ValueOnly) -and 
        (Get-Command -Name $script.PowerShellCorePath -ErrorAction SilentlyContinue)) {
        $script:PowerShellCorePath
    }

    $pwsh = Get-Command -Name pwsh -ErrorAction SilentlyContinue
    
    if ($Version -and $pwsh) {
        $pwsh = $pwsh | ? { 
            $_.Version -ge (New-Object System.Version $Version) 
        }
    }

    if ($pwsh) {
        $script:PowerShellCorePath = $pwsh.Source
        $script:PowerShellCorePath
        return
    }

    $tempPath = [System.IO.Path]::GetTempFileName()
    Remove-Item -Force $tempPath | Out-Null
    New-Item -Path $tempPath -ItemType Directory | Out-Null
    # Schedule the temp folder to be deleted upon script completion
    $appDomain = [System.AppDomain]::CurrentDomain
    Register-ObjectEvent  -EventName DomainUnload -InputObject $appDomain  -Action{
        Write-Verbose "Deleting $tempPath..."
        Remove-Item -Path $tempPath -Force -Recurse -ErrorAction SilentlyContinue
    } | Out-Null

    $pwshInstallCommand = "dotnet tool install --tool-path $tempPath PowerShell"
    if ($VerbosePreference -ieq 'SilentlyContinue') {
        $pwshInstallCommand += " --verbosity minimal"
    }

    if ($Version) {
        $pwshInstallCommand += " --version $Version"
    }

    Write-Verbose "`t$pwshInstallCommand"
    $pwshInstallOutput = Invoke-Expression -Command $pwshInstallCommand | Out-String
    Write-Verbose $pwshInstallOutput 
    if ($VerbosePreference -ine 'SilentlyContinue') {
        Write-Host $pwshInstallCommand
    }



    $pwsh = Get-Command -Name (Join-Path $tempPath 'pwsh.exe') 

    $script:PowerShellCorePath = $pwsh.Source
    $script:PowerShellCorePath
}

Function Ensure-PowershellCore7 {
    if ($PSVersionTable.PSVersion -lt "7.0" -or $PSVersionTable.PSEdition -ine 'Core') {
        Write-Verbose "Getting Powershell 7.0"
        $pwsh = Get-PowershellCore -Version "7.0"


        [string[]]$arguments = @()
        $arguments += @('-ExecutionPolicy', 'ByPass', '-NoProfile', '-command')
        $arguments += $script:MyInvocation.Line

        Write-Verbose "& $pwsh $arguments"
        & $pwsh $arguments
        exit
    }
}

Ensure-PowershellCore7

<#
Set GIT_ASK_YESNO=false to prevent interactive
Q&A from git like "Unlink of file '.vs/foo' failed. Should I try again? (y/n)"

GIT_ASK_YESNO is defined at 
https://github.com/git/git/blob/d62dad7a7dca3f6a65162bf0e52cdf6927958e78/compat/mingw.c#L188
No documentation as far as I can tell

Call to setlocal earlier would ensure that this setting is local to 
the lifetime of this batch-file's execution
#>
$env:GIT_ASK_YESNO = 'false'

if ($KillBuildProcesses) {
    ($BuildProcesses + $AdditionalBuildProcesses) | % {
        Kill-ChildProcessesByName -ProcessName $_ -DryRun:$DryRun
    }
}

$cleanCommand = "git clean -xdf"

if ($DryRun) {
    $cleanCommand += " --dry-run"
}

Write-Host "Cleaning git enlistment: $cleanCommand ..."

$gitOutput = Invoke-Expression -Command $cleanCommand | Out-String 
$gitOutput -split '\r?\n' | % {
    Write-Host "`t" -NoNewline
    Write-Host $_
}