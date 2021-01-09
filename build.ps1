$baseLocation = Get-Location
$loginAppDir = Join-Path $baseLocation "src\HCGStudio.TheLauncherLogin"
$appDir = Join-Path $baseLocation "\src\HCGStudio.TheLauncher"
$outputDir = Join-Path $baseLocation "build"

if(Test-Path $outputDir){
    Remove-Item $outputDir -Recurse
}

mkdir $outputDir

Set-Location $loginAppDir
dotnet publish -c release -r win-x64 -p:PublishSingleFile=true
$loginAppBuildResult = Join-Path $loginAppDir "bin\release\net5.0-windows\win-x64\publish"
Remove-Item (Join-Path $loginAppBuildResult "x86") -Recurse
Remove-Item (Join-Path $loginAppBuildResult "arm64") -Recurse
Remove-Item (Join-Path $loginAppBuildResult "x64") -Recurse
#Clenup build result
Remove-Item (Join-Path $loginAppBuildResult "*.xml")
Remove-Item (Join-Path $loginAppBuildResult "*.pdb")
Remove-Item (Join-Path $loginAppBuildResult "*.config")
Copy-Item (Join-Path $loginAppBuildResult "*") $outputDir -Recurse

#Build Main App
Set-Location $appDir
dotnet publish -c release  -r win-x64 -p:PublishSingleFile=true
$mainAppBuildResult = "bin\release\net5.0\win-64\publish"
#Clenup build result
Remove-Item (Join-Path $mainAppBuildResult "*.xml")
Remove-Item (Join-Path $mainAppBuildResult "*.pdb")
Remove-Item (Join-Path $mainAppBuildResult "*.config")
Copy-Item (Join-Path $mainAppBuildResult "*") $outputDir -Recurse

Set-Location $baseLocation
