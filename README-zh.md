# TheLauncher

## 功能 

- 节省磁盘空间
- 每个版本或者装了Mod Loader的版本，可以有很多个实例，其中的数据单独储存，互不干扰（与MultiMC类似）
- 跨平台
- 支持微软账户登录（未充分测试）

## 运行需求

- 高于1.8版本的Java安装。如果你用Forge，请一定要使用1.8版本的Java。如果你用Fabric，没有最高版本限制，但是注意KubeJS最高支持Java14。推荐使用[AdoptOpenJDK](https://adoptopenjdk.net/)（若下载速度慢，请使用[清华源](https://mirrors.tuna.tsinghua.edu.cn/help/adoptopenjdk/)。
额外需求：仅在微软账户登陆时需要：
- Windows用户：请安装[Webview Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)。
- Linux用户：请安装gtk3, gtkmm and webkit2gtk。一般来说系统会自带这些库，如果没有，请使用包管理器安装。
- macOS用户：目前没有点击即用的版本，但是你可以尝试从源代码编译，macOS目前不支持微软账户登录。

## 怎么使用

**交互式界面没有下载游戏的选项，请在双击交互界面前先使用命令行下载游戏。**

首先下载版本，这个需要通过命令行调用。比如，我要下载1.16.4，就输入：

``` bash
HCGStudio.TheLauncher -i 1.16.4
```

然后双击打开应用。

根据你的账号类型输入"AddMojangAccount"或者"AddMojangAccount"并回车，程序会引导你登录。

在你下载一个版本后，会默认创建一个与版本号名称相同的实例。你也可以通过输入`AddInstance version instanceName`的方式手动创建实例。版本相同的实例的游戏版本是完全相同的，而存档、mod之类是分开储存，互不干扰。

比如：
``` bash
AddInstance 1.16.4 另一个1.16.4
```

最后，输入`Run accountName instanceName`并回车，游戏就会启动。 `accountName`是你的**游戏名**，`instanceName`是实例名称。比如我要启动1.16.4，就输入`Run mahoshojoHCG 1.16.4`，我要启动另一个1.16.4就输入`Run mahoshojoHCG 另一个1.16.4`。

享受吧！

## 怎么安装Forge/Fabric/Optifine

~~你可能会发现这一块没有英文，这是因为我懒得写了~~

安装器对本启动器的文件没有作用，在启动器自动安装支持前，你可以尝试自动安装。如果你不能理解什么是游戏JSON文件，建议等待启动器更新。

将要安装的Forge/Fabric/Optifine的JSON文件复制到启动器的versions文件夹中。如果你使用Foege或者仅安装Optifine，还请手动拷贝libraries文件。

然后以JSON名称为版本，创建游戏实例。

游戏文件夹在“games/实例名”里，安装Mods、光影就往里面丢。

## 怎么编译

请看英文部分，不想翻译了。

## TODO

- [ ] 自动安装Forge或者Fabric
- [ ] 图形界面
- [ ] 更好的macOS支持
- [ ] 文档
- [ ] 整合包导入
- [ ] 整合包mod硬链接

## 已知问题

- 请在交互界面输入Exit关闭程序，而不是点击右上角的×或者按Ctrl+C，否则可能会丢失数据。