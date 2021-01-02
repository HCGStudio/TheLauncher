baseLocation=`pwd`
loginAppDir="$baseLocation/src/HCGStudio.TheLauncherLoginGtk"
appDir="$baseLocation/src/HCGStudio.TheLauncher"
outputDir="$baseLocation/build"

rm -r $outputDir
mkdir $outputDir

#build loginApp with cmake
cd $loginAppDir
mkdir build
cd build
cmake ..
make
cp HCGStudio.TheLauncherLogin $outputDir

#build app
cd $appDir
dotnet publish -r linux-x64 -c release
cp "$appDir/bin/release/net5.0/linux-x64/publish/" HCGStudio.TheLauncher $outputDir

cd $baseLocation