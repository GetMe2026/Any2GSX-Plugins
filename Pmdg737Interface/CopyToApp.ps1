# POST
# pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)CopyToApp.ps1" $(Configuration) $(SolutionDir) $(ProjectDir) "Any2GSX"

if ($args[0] -eq "*Undefined*") { exit 0 }
if ($args[1] -eq "*Undefined*") { exit 0 }

try {
    $buildConfiguration = $args[0]
    $pathProject = $args[2]
    $destPath = Join-Path $env:APPDATA "Any2GSX\plugins\PMDG.B737"

    if (Test-Path -Path $destPath) {
        $dllPath = Join-Path $pathProject (Join-Path (Join-Path "bin" $buildConfiguration) "\net10.0-windows10.0.17763.0\win-x64\Pmdg737Interface.dll")
        $manifestPath = Join-Path $pathProject (Join-Path (Join-Path "bin" $buildConfiguration) "\net10.0-windows10.0.17763.0\win-x64\manifest.json")

        Copy-Item -Path $dllPath -Destination $destPath -Force | Out-Null
        Copy-Item -Path $manifestPath -Destination $destPath -Force | Out-Null

        Write-Host "SUCCESS: PMDG.B737 copied to Any2GSX plugin folder."
        exit 0
    }

    Write-Host "NOT copied - Any2GSX PMDG.B737 plugin folder does not exist yet."
    exit 0
}
catch {
    Write-Host "FAILED: Exception in CopyToApp.ps1!"
    exit -1
}
