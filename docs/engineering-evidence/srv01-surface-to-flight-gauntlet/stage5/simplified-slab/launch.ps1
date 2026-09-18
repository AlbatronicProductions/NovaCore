param([ValidateSet('Manual','Launcher')][string]$Route='Manual')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'E:\NovaCore'
foreach($seal in Get-Content (Join-Path $PSScriptRoot 'manual-binary-seals.json') -Raw | ConvertFrom-Json){
    if(!(Test-Path -LiteralPath $seal.path) -or (Get-FileHash -LiteralPath $seal.path).Hash -ne $seal.sha256){throw "Prepared binary identity differs: $($seal.path)"}
}
Remove-Item Env:VK_INSTANCE_LAYERS -ErrorAction SilentlyContinue
if($Route -eq 'Launcher'){
    $exe='E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe'
    if(!(Test-Path -LiteralPath $exe)){throw 'Run build.ps1 Release then prepare-launcher.ps1 first'}
    & $exe
}else{
    $exe='E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe'
    if(!(Test-Path -LiteralPath $exe)){throw 'Run build.ps1 Release then prepare-launcher.ps1 first'}
    & $exe '--scene=srv01-florida-support' '--log=renderer,vulkan'
}
