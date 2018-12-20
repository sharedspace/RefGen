class msbuild_finder {
  [string]Get_ScriptDirectory(){
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    return $Invocation.PSScriptRoot
  }

  [string]Get_RepoRoot(){
    $scriptDir = $this.Get_ScriptDirectory()
   return [IO.Directory]::GetParent($scriptDir)
  }

  [string]Download_And_Get_VsWhere(){
   Install-Package -Name VsWhere -ProviderName Chocolatey -ForceBootstrap -Force
   $vswhere = join-path (join-path $env:ChocolateyPath bin) vswhere.bat
   return $vswhere
  }

  [void]setup_environment(){
   $vswhere = $this.Download_And_Get_VsWhere()

   $installationPath = & $vswhere -prerelease -latest -property installationPath
   if ($installationPath -and (test-path "$installationPath\Common7\Tools\vsdevcmd.bat")) {
     & "${env:COMSPEC}" /s /c "`"$installationPath\Common7\Tools\vsdevcmd.bat`" -no_logo && set" | foreach-object {
     $name, $value = $_ -split '=', 2
     set-content env:\"$name" $value
     }
   }
  }

  [string]Get_MsBuild(){
    # Try MSBuild versions 'Current' first, then look for version '15.0'
    $versions = @('Current', '15.0')

    $vswhere = $this.Download_And_Get_VsWhere()
    $this.setup_environment()
    $msBuild = $null
 
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = $vswhere
    $process.StartInfo.Arguments = "-prerelease -products * -requires Microsoft.Component.MSBuild -property installationPath"
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.UseShellExecute = $false
    $process.Start()
    $process.WaitForExit()
  
    if ($process.ExitCode -eq 0) {
      $msBuildDir = $process.StandardOutput.ReadLine()
      if ($msBuildDir) {
        foreach ($version in $versions) {
            $path = join-path $msBuildDir "MSBuild\$version\Bin\MSBuild.exe"
            if (Test-Path -PathType Leaf "$path") {
              $msbuild = $path
              break
            }
        }
      }
    }

    if ($msBuild -eq $null) {
     throw "MsBuild.exe could not be found"
    }

    return $msBuild
   }

   [void] build(){
     $repoRoot = $this.Get_RepoRoot()
     $msBuild = $this.Get_MsBuild()

     & $msBuild $repoRoot\RefGen.Sln /t:restore /t:rebuild | Out-Default
   }
}

$finder = [msbuild_finder]::new()
$finder.build()