# 素材来源与使用说明

小G是个人学习用途的非官方白发龙娘桌面形象，与 OpenAI 等品牌方没有官方关联。

## 角色原始素材

- 作者与原项目：[fywdy / chatgpt-chibi-pets](https://github.com/fywdy/chatgpt-chibi-pets)。
- 角色目录：[chatgpt-white-dragon-chibi-v5](https://github.com/fywdy/chatgpt-chibi-pets/tree/main/chatgpt-white-dragon-chibi-v5)。
- 本项目沿用该角色参考形象和原始图集，并对透明背景、高清动作、转头、眨眼、踮脚、思考、沮丧和动画补帧进行了修改或重新生成。
- 原项目美术素材采用 [CC BY-NC 4.0（署名—非商业性使用）](https://creativecommons.org/licenses/by-nc/4.0/)，原许可与声明保存在 [third-party/fywdy](third-party/fywdy/)。原素材及其改编素材需保留原署名和使用限制。

## 目录与文件

- `xiaog-atlas.png`：角色图集和原始位置标尺。
- `artwork/`：当前程序使用的高清动作原画。
- `animation-frames.zip`：由现有桌面程序绘制的动画帧包，使用 Git LFS 管理；本地 SHA-256 为 `ca5d9b680c9737fc8beb915739e77540c7868f9e941255ac05ed986a9e6e36bc`。
- 根目录的预览图片、GIF 和图标：本地小G程序的展示资源。
- `voice/00.wav` 至 `voice/11.wav`：12段预先合成的角色配音。
- `voice/reference.wav`、`voice/voice-design.json`：合成声线样本及设计描述。
- `voice/catalog.json`：配音文本、模型来源、时长和校验信息。

## 配音

配音由 [Qwen3-TTS](https://github.com/QwenLM/Qwen3-TTS) 按角色描述预先合成；使用 VoiceDesign 设计合成样本，再以 0.6B Base 制作语录，并用 [FFmpeg atempo](https://ffmpeg.org/ffmpeg-filters.html#atempo) 将语速调整为93%。具体模型版本和制作来源保留在配音清单中。声音是为该角色生成的合成声音，不声称复刻任何现实人物。

## 获取完整素材

克隆前安装 Git LFS，在仓库目录运行 `git lfs pull` 可下载动画帧包的实际内容。
