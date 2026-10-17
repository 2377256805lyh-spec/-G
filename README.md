# 小G

一个陪你学习的白发龙娘 Q 版 Windows 桌面挂件，支持动作互动、鼠标注视和本地角色配音。

## 功能

- 单击跳跃；双击依次播放踮脚、思考、摇头、沮丧、等待和检查等动作。
- 拖动时朝移动方向奔跑，鼠标靠近头部时转头注视。
- 互动时随机显示短句，并播放对应的 12 段预制配音。
- 右键菜单可调整大小、置顶、音量和随 Codex 启动等设置。

日常显示与配音播放在本机完成，不调用聊天模型。窗口位置、大小等个人设置保存在程序旁的 `settings.json`，该文件不提交到仓库。

## 下载与编译

使用 64 位 Windows，需要 Git、Git LFS（Git Large File Storage，Git 大文件存储），以及系统中的 .NET Framework C# 编译器与 WPF（Windows Presentation Foundation，Windows 界面绘制框架）。

在 PowerShell 中执行：

```powershell
git clone https://github.com/2377256805lyh-spec/chatG.git
Set-Location chatG
git lfs install --local
git lfs pull
.\build.ps1
.\小G.exe
```

如果 PowerShell 阻止运行构建脚本，可仅对本次构建使用：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

构建会校验语录、配音顺序与文件哈希，然后将图片、动画和声音嵌入根目录的 `小G.exe`。已有素材可直接用于编译，无需下载语音模型。动画帧包约 287 MB，通过 Git LFS 保存；若本地文件只有几行文字，请先运行 `git lfs pull`。

## 使用

运行编译得到的 `小G.exe`；人物和通知区域图标的右键菜单均提供设置与退出入口。双击通知区域图标可将小G移回主屏幕。

开启“随桌面 Codex 启动”后，程序会在当前用户的 Windows 启动文件夹创建快捷方式，用隐藏等待进程检测桌面 Codex 窗口。此功能可在同一菜单关闭。

更新正在使用的版本时，先退出旧程序，再替换可执行文件，并保留已有 `settings.json`。

## 项目结构

| 路径 | 内容 |
|---|---|
| `src/` | 9 个 C# 源文件与应用清单 |
| `assets/` | 原画、角色图集、图标、动画帧包和配音 |
| `scripts/make-voice.py` | 可选的角色配音制作脚本 |
| `docs/` | 素材来源、第三方许可和动态预览 |
| `build.ps1` | Windows 构建入口 |

重新制作配音才需要 Qwen3-TTS、兼容的模型环境与 FFmpeg。制作脚本读取 `assets/voice/` 的声线记录和 `src/Main.cs` 的语录；具体依赖与参数见脚本开头说明。生成新配音后重新构建即可。

## 预览

[动作动态预览](docs/previews/小G-动作动态预览.gif) · [鼠标转头动态预览](docs/previews/小G-转头动态预览.gif)

## 素材来源

角色原始参考来自 [fywdy/chatgpt-chibi-pets](https://github.com/fywdy/chatgpt-chibi-pets)。本项目是非官方同人创作，与 OpenAI 等品牌方没有官方关联。

原美术素材及其改编需遵守署名与非商业使用条件。完整来源、修改说明和许可见 [素材说明](docs/ASSET-SOURCES.md) 与 [原项目许可](docs/licenses/fywdy/)。配音为基于角色描述生成的合成声音，模型与生成记录保留在配音清单中。
