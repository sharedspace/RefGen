class msbuild_finder {
  [string]$RepoRoot = [IO.Directory]::GetParent($PSScriptRoot)
  [string]$VsWhere
  [string]$MsBuild

  msbuild_finder(){
   # Find VsWhere 
   Install-Package -Name VsWhere -ProviderName Chocolatey -ForceBootstrap -Force
   $this.VsWhere = join-path (join-path $env:ChocolateyPath bin) vswhere.bat

   # Set-up environment
   $installationPath = & $this.VsWhere -prerelease -latest -property installationPath
   #if ($installationPath -and (test-path "$installationPath\Common7\Tools\vsdevcmd.bat")) {
   #  & "${env:COMSPEC}" /s /c "`"$installationPath\Common7\Tools\vsdevcmd.bat`" -no_logo && set" | foreach-object {
   #  $name, $value = $_ -split '=', 2
   #  set-content env:\"$name" $value
   #  }
   #}

   if ($installationPath){
     $vsDevCmd = Join-Path $installationPath "Common7\Tools\vsdevcmd.bat"
     if (Test-Path "$vsDevCmd") {
      $vsDevCmd = '"' + $vsDevCmd + '"'
      $process = New-Object System.Diagnostics.Process
      $process.StartInfo.FileName = "${env:COMSPEC}"
      $process.StartInfo.Arguments = "/s /c $vsDevCmd -no_logo && set"
      $process.StartInfo.RedirectStandardOutput = $true
      $process.StartInfo.UseShellExecute = $false
      $process.StartInfo.CreateNoWindow = $false
      $process.Start()
      $process.WaitForExit()

      $output = $process.StandardOutput.ReadToEnd() 
      $output | ForEach-Object {
       $name, $value = $_ -split '=', 2
       set-content env:\"$name" $value
      }
     }
   }

   # Find MsBuild
   $process = New-Object System.Diagnostics.Process
   $process.StartInfo.FileName = $this.VsWhere
   $process.StartInfo.Arguments = "-prerelease -products * -requires Microsoft.Component.MSBuild -property installationPath"
   $process.StartInfo.RedirectStandardOutput = $true
   $process.StartInfo.UseShellExecute = $false
   $process.Start()
   $process.WaitForExit()
  
   # Try MSBuild versions 'Current' first, then look for version '15.0'
   $versions = @('Current', '15.0')

   if ($process.ExitCode -eq 0) {
     $msBuildDir = $process.StandardOutput.ReadLine()
     if ($msBuildDir) {
       foreach ($version in $versions) {
           $path = join-path $msBuildDir "MSBuild\$version\Bin\MSBuild.exe"
           if (Test-Path -PathType Leaf "$path") {
             $this.MsBuild = $path
             break
           }
       }
     }
   }

   if ($this.MsBuild -eq $null) {
    throw "MsBuild.exe could not be found"
   }
  }

  #[string]Download_And_Get_VsWhere(){
  # Install-Package -Name VsWhere -ProviderName Chocolatey -ForceBootstrap -Force
  # $vswhere = join-path (join-path $env:ChocolateyPath bin) vswhere.bat
  # return $vswhere
  #}

  #[void]setup_environment(){
  # $vswhere = $this.Download_And_Get_VsWhere()

  # $installationPath = & $vswhere -prerelease -latest -property installationPath
  # if ($installationPath -and (test-path "$installationPath\Common7\Tools\vsdevcmd.bat")) {
  #   & "${env:COMSPEC}" /s /c "`"$installationPath\Common7\Tools\vsdevcmd.bat`" -no_logo && set" | foreach-object {
  #   $name, $value = $_ -split '=', 2
  #   set-content env:\"$name" $value
  #   }
  # }
  #}

  #[string]Get_MsBuild(){
  #  # Try MSBuild versions 'Current' first, then look for version '15.0'
  #  $versions = @('Current', '15.0')

  #  $vswhere = $this.Download_And_Get_VsWhere()
  #  $this.setup_environment()
  #  $msBuild = $null
 
  #  $process = New-Object System.Diagnostics.Process
  #  $process.StartInfo.FileName = $vswhere
  #  $process.StartInfo.Arguments = "-prerelease -products * -requires Microsoft.Component.MSBuild -property installationPath"
  #  $process.StartInfo.RedirectStandardOutput = $true
  #  $process.StartInfo.UseShellExecute = $false
  #  $process.Start()
  #  $process.WaitForExit()
  
  #  if ($process.ExitCode -eq 0) {
  #    $msBuildDir = $process.StandardOutput.ReadLine()
  #    if ($msBuildDir) {
  #      foreach ($version in $versions) {
  #          $path = join-path $msBuildDir "MSBuild\$version\Bin\MSBuild.exe"
  #          if (Test-Path -PathType Leaf "$path") {
  #            $msbuild = $path
  #            break
  #          }
  #      }
  #    }
  #  }

  #  if ($msBuild -eq $null) {
  #   throw "MsBuild.exe could not be found"
  #  }

  #  return $msBuild
  # }

  [void] build(){
    $solution = Join-Path $this.RepoRoot RefGen.Sln
    & $this.MsBuild $solution /t:restore /t:rebuild | Out-Default
  }
}

$finder = [msbuild_finder]::new()
#$finder.build()