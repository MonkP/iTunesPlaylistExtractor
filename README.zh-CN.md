# iTunes 播放列表提取工具
### 核心功能已完成，MTP 同步仍在规划中。
## 说明
我的 PC 上有大约 6,000 首本地音乐的 iTunes 资料库，之前用来把 iTunes 音乐同步到安卓手机的 app（double twists）最近越来越不好用了。
我找了一圈替代品，没找到合适的。而且我也不喜欢 double twists 在手机上组织音乐文件的方式。所以我打算自己做一个工具把这个活干了。

## 功能与使用说明

1. 第一步是选择本地的 `iTunes Music Library.xml` 文件，然后就会开始解析。资料库在后台解析，即使资料库很大，界面也不会卡顿。
2. 播放列表以列表视图展示，显示名称、曲目数量和父子/兄弟层级关系。播放列表文件夹的嵌套层级以每级两个空格的缩进表示，子项紧跟在父文件夹下方。iTunes 主资料库和内置系统播放列表（音乐、影片、播客等）会被隐藏。文件夹的曲目数量是其内部所有播放列表中曲目的去重总数。
3. 选择音乐库的根路径。
4. 在列表视图中勾选要提取的播放列表。勾选播放列表文件夹时，会将其作为一整个播放列表提取，包含内部所有播放列表的去重曲目集合。
5. 选择要提取到的目标路径。
6. 点击"提取"按钮后，所选播放列表中的音乐文件将被提取到目标路径，保持与所选根路径完全相同的相对路径结构。
    例如，根路径为 `F:/musics`，其中一个提取文件为 `F:/music/周杰伦/叶惠美/晴天.mp3`，目标路径为 `G:/PickedMusic`。那么该文件会被复制到 `G:/PickedMusic/周杰伦/叶惠美/晴天.mp3`。
    所有来自所选播放列表的文件在复制前会被收集和去重。目标路径中已存在的音乐文件是否覆盖由 **OverWrite** 复选框决定：勾选时覆盖已有音乐文件；不勾选时保留已有文件不再复制。`.m3u` 文件始终重写。单个文件的复制失败不会中断整个提取过程，仅会被记录到日志并跳过。
7. 播放列表本身会被提取为 `.m3u` 文件，创建在目标路径下。每个 `.m3u` 文件包含其原始播放列表中的音乐条目，但文件路径为相对路径。
    `.m3u` 文件的命名方式为：从顶层文件夹到勾选项的完整路径，以 `-` 连接。每一级父文件夹名称压缩为最多前 6 个字符，勾选项保留完整名称，文件名中的非法字符替换为 `_`。例如，在播放列表文件夹 `周杰伦` 内勾选播放列表 `周杰伦全集`，会生成 `周杰伦-周杰伦全集.m3u`。
8. 提取完成后，会检查目标路径中是否存在本次所选播放列表未包含的文件，例如本次未勾选的播放列表留下的旧 `.m3u`，或勾选列表未包含的音乐文件。若存在，会弹窗询问 "Target Path contains files that not included in playlists choosen this time, would you like to delete them?"：选择 Yes 自动删除（删除数量会追加到汇总信息），选择 No 则保留。
9. 现在可以将整个目标路径的内容移动到设备上，然后把 `m3u` 播放列表导入你喜欢的播放器 app。
10. 自动记忆之前的选择。资料库文件、根路径、目标路径、OverWrite 选项以及上次提取时勾选的播放列表会在点击"提取"时保存到应用目录下的 `preference.json` 中，下次启动时自动恢复。
11. 进度和摘要显示在状态栏中，例如提取时显示 `Copying (5/30)` 和 `Creating m3u (1/5)`。提取完成后会弹出摘要提示（所选/已创建的播放列表数量和曲目数量）。如果出现问题，关闭提示后会自动打开当天的日志文件。
12. 日志写入应用目录下的 `log/yyyy-MM-dd.log`。最低记录级别（Fatal/Error/Warn/Info/Debug）由 `preference.json` 中的 `LogLevel` 字段设置，默认为 `Error`。

## AI 编程声明

* 创意——我
* 可行性研究——我 + DeepSeek 对话模式
* 项目初始化和主界面布局——我
* 功能描述和关键实现决策——我
* 所有脏活累活——QoderCN（Quest 模式）
* 代码审查——我 + 另一个 QoderCN 会话
* 手动测试和调试——我
* 文档——我 + QoderCN（Quest 模式）

## 后续计划

1. 增加 MTP 功能，以便可以选择安卓设备上的路径作为目标路径。
2. 借助 MTP，检查目标路径中已有的 `.m3u` 和音乐文件，然后执行真正的同步而非仅添加。

## 致谢

本项目使用了 [iTunesLibraryParser](https://github.com/asciamanna/iTunesLibraryParser)，Copyright (c) 2018 Anthony Sciamanna，基于 [MIT 许可证](iTunesLibraryParser-master/LICENSE) 授权：

> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
