#To build TheLauncher for windows, you should open this in the Developer PowerShell for VS

$baseLocation = Get-Location
$loginAppDir = Join-Path $baseLocation "src\HCGStudio.TheLauncherLogin"
$appDir = Join-Path $baseLocation "\src\HCGStudio.TheLauncher"
$outputDir = Join-Path $baseLocation "build"

if(Test-Path $outputDir){
    Remove-Item $outputDir -Recurse
}

mkdir $outputDir

Set-Location $loginAppDir
msbuild -t:restore
msbuild -p:Configuration=Release
$loginAppBuildResult = Join-Path $loginAppDir "bin\Release"
Remove-Item (Join-Path $loginAppBuildResult "x86") -Recurse
Remove-Item (Join-Path $loginAppBuildResult "arm64") -Recurse
Move-Item (Join-Path $loginAppBuildResult "x64" "WebView2Loader.dll") $loginAppBuildResult
Remove-Item (Join-Path $loginAppBuildResult "x64") -Recurse
#Clenup build result
Remove-Item (Join-Path $loginAppBuildResult "*.xml")
Remove-Item (Join-Path $loginAppBuildResult "*.pdb")
Remove-Item (Join-Path $loginAppBuildResult "*.config")
Copy-Item (Join-Path $loginAppBuildResult "*") $outputDir -Recurse

#Build Native
Set-Location $appDir
dotnet publish -c release
$mainAppBuildResult = "bin\release\net5.0\publish\*"
Remove-Item (Join-Path $mainAppBuildResult "*.xml")
Remove-Item (Join-Path $mainAppBuildResult "*.pdb")
Remove-Item (Join-Path $mainAppBuildResult "*.config")
Copy-Item (Join-Path $mainAppBuildResult ) $outputDir -Recurse

Set-Location $baseLocation