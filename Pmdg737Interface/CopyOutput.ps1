# POST
# pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)CopyOutput.ps1" $(Configuration) $(SolutionDir) $(ProjectDir) "Any2GSX"

if ($args[0] -eq "*Undefined*") { exit 0 }
if ($args[1] -eq "*Undefined*") { exit 0 }

try {
    $buildConfiguration = $args[0]
    $pathProject = $args[2]

    $dllPath = Join-Path $pathProject (Join-Path (Join-Path "bin" $buildConfiguration) "\net10.0-windows10.0.17763.0\win-x64\Pmdg737Interface.dll")
    $manifestPath = Join-Path $pathProject (Join-Path (Join-Path "bin" $buildConfiguration) "\net10.0-windows10.0.17763.0\win-x64\manifest.json")
    $destPath = Join-Path $pathProject "publish"

    New-Item -ItemType Directory -Force -Path $destPath | Out-Null
    Copy-Item -Path $dllPath -Destination $destPath -Force | Out-Null
    Copy-Item -Path $manifestPath -Destination $destPath -Force | Out-Null
    Copy-Item -Path "Channel\PMDG.737.json" -Destination $destPath -Force | Out-Null

    Write-Host "SUCCESS: PMDG.B737 publish folder updated."
    exit 0
}
catch {
    Write-Host "FAILED: Exception in CopyOutput.ps1!"
    exit -1
}
