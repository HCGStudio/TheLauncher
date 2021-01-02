# TheLauncher

[中文](README-zh.md)

## Features 

- Save your disk space.
- One version / modded version, multiple instance (like MultiMC)
- Cross platfrom.
- Microsoft Login (Not fully tested)

## Run Requirements
- Any version of Java more than 1.8. 1.8 if you plan to use Forge. No upper limit for fabric, or 14 if you plan to use KubeJS with fabric. We recommend you to use [AdoptOpenJDK](https://adoptopenjdk.net/).
- For windows, please download and install [Webview Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) if you plan to use Microsoft Login.
- For linux, please install gtk3, gtkmm and webkit2gtk. This usually come with your Linux installation. If not, install them with your package manager.
- For macOS, no binary avliable at present. But you can still build from source, which is lack the function to Login by Microsoft.

## How to use

**Do not directly open the app when you have not download any game.**

First, download your required version via command line.

``` bash
HCGStudio.TheLauncher -i 1.16.4
```

Then open the app without argument.

Now, you can login. Type "AddMojangAccount" or "AddMojangAccount" for the account type you have.

If you choose Mojang, it will guide you to enter your user name and password.

If you choose Microsoft, it will pump the Oauth Windows for you to login.

By default, you have an instance name same as the installed version name for each version you installed. You can install new instance of the same version by `AddInstance version instanceName`.

For example:
``` bash
AddInstance 1.16.4 another1.16.4
```

Finally, you can start the game, simply by typing `Run accountName instanceName`. `accountName` is your **username in game**, `instanceName` is the instance you created or deault instance name same as the version.

Enjoy!

## Build Requirements
- Every system : [.Net 5 SDK](https://dotnet.microsoft.com/download) is required.
- Windows : Visual Studio, with .Net Framework SDK and C++ development installed.
- Linux : cmake, a C++ compier, and any library mentioned before.

## How to Build
- For windows, execute build.ps1 from **x64 Native Tools Command Prompt for VS**.
- For linux, execute build.sh

Build result will be in build dir.

## TODO

- [ ] Install Fabric and Forge automatically.
- [ ] GUI.
- [ ] Buid script and Microsoft Login for macOS.
- [ ] Documents.
- [ ] Modpack install.
- [ ] Modpack mods hard link.

## Known Issues

- Type exit to exit the app, or you may lose data.