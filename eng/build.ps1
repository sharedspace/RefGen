function Get-ScriptDirectory {
  $Invocation = (Get-Variable MyInvocation -Scope 1).Value
  $Invocation.PSScriptRoot
}

function Get-VsWhere {

    $scriptDir = Get-ScriptDirectory
    $repoRoot = [System.IO.Directory]::GetParent($scriptDir)

    $artifactsDir = join-path $repoRoot  'artifacts'
    $toolsDir = join-path $artifactsDir 'tools'
    $vswhere = join-path $toolsDir 'vswhere.exe'

    if (-not (Test-Path $vswhere)) {
        if (-not (Test-Path -PathType Container $toolsDir)) {
            New-Item -ItemType Directory $toolsDir
        }

        Invoke-WebRequest 'https://github.com/Microsoft/vswhere/releases/download/2.5.2/vswhere.exe' -OutFile $vswhere
    }

    $vswhere
}

function Get-MsBuild {
 $vswhere = Get-VsWhere
 $msBuild = 'msBuild.exe'
 $msBuildDir = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
 if ($msBuildDir) {
    $path = join-path $msBuildDir 'MSBuild\15.0\Bin\MSBuild.exe'
    if (Test-Path $path) {
      $msbuild = $path
    }
 }

 $msBuild
}

Get-MsBuild