# BioShell Lab

帮助没有计算机基础的生物学研究者，通过中文讲解与循序渐进的练习学习 Linux 命令。

## Windows 使用

下载 [`downloads/BioShellLab.exe`](downloads/BioShellLab.exe)，双击启动。

环境要求：Windows 10/11 x64、.NET Framework 4.7.2 或更高版本，以及 [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)。应用不需要 Python、Node.js 或 WSL。

课程和练习可离线使用；外部资料需要联网。

## 功能

- 18 个递进关卡：目录导航、查看生物数据、整理文件、组合命令、综合小实验。
- 虚构的 FASTQ 与样本表，围绕具体生物科研任务学习。
- 模拟终端、自动判题、章节解锁、任务提示、环境重置和自由练习。
- 本地保存完成进度；支持命令历史和路径补全。
- 内置 Data Carpentry、Software Carpentry 与 GNU 官方资料链接。

## 练习范围

支持 `pwd`、`ls`、`ll`、`cd`、`cat`、`head`、`tail`、`wc -l`、`grep`、`mkdir`、`touch`、`cp`、`mv`、`rm`、`find`、`echo`、管道和重定向等常用命令子集。

这是教学模拟器，**不是真实 Linux 或完整 Bash**。`ll` 在此预设为 `ls -l` 的别名。练习不会读取或修改真实研究文件。它不支持软件安装、网络命令、变量展开、Shell 脚本或真实生信工具；文件属性也是教学示例。

## 构建

在 Windows PowerShell 中进入源码目录，运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\build-desktop.ps1
```

默认生成 `BioShellLab-Studio.exe`。SDK 组件位于 `vendor/`，无需在线下载构建依赖。

## 检查

```powershell
.\downloads\BioShellLab.exe --self-test
```

成功时退出码为 0，并在 exe 所在目录生成 `self-test-result.txt`。目前通过 79 项断言，覆盖全部 18 关、命令解析、管道、重定向、文件操作及常见错误。

已在桌面验证启动、命令输入、自动判题、关卡切换、资料弹窗及重启恢复进度。测试不保证所有设备与操作组合都无问题。

## 项目文件

| 文件 | 作用 |
| --- | --- |
| `Engine.cs` | 模拟文件系统、命令引擎、课程与判题 |
| `Desktop.cs` | Windows 窗口、WebView2、界面通信与进度保存 |
| `studio.html` | 学习工作台界面与交互 |
| `Tests.cs` | 自动检查 |
| `build-desktop.ps1` | 打包单文件 exe |
| `downloads/BioShellLab.exe` | 已构建的 Windows x64 程序 |
| `使用说明.txt` | 详细使用说明与实现边界 |

学习记录位于 `%LOCALAPPDATA%\BioShellLab\progress-v1.txt`。程序同时在该目录解出页面、加载器与浏览器缓存；这些运行数据不纳入仓库。

## 第三方组件

使用 Microsoft.Web.WebView2 1.0.4314.35，第三方组件许可参见 [`vendor/LICENSE.txt`](vendor/LICENSE.txt)。程序未内置 WebView2 浏览器运行时。

## 后续方向

可编辑课程包、章节测验、错题复习和教师自定义任务；在设计环境隔离与重置机制后，再扩展真实 Linux 练习。
