# 更新日志 / Changelog

本项目的所有显著变更都会记录在这里。从 v6.20 起执行新规：**发布附件（二进制）有任何变动，必须升版本号、在本文件留记录、并在 Release 说明中附 SHA256 校验和**，不再静默覆盖同一标签的附件。

All notable changes are documented here. From v6.20 on: any change to release binaries requires a version bump, an entry in this file, and SHA256 checksums in the release notes — no more silent asset replacement under the same tag.

## [v6.22.4] - 2026-11-12

### 修复 / Fixed

- **长语言下多处文字截断**：v6.22.2 之后在法语界面实测发现，主窗口「复制地址」「停止服务器」按钮、日志分段标签在法语、西班牙语、葡萄牙语、俄语、德语等较长语言下仍有截断；现引入按文字实际宽度自适应字号的绘制方式，分段标签改用短文案，并按各标签的实际宽度分配比例，不再平均切分造成重叠或省略。
  Text clipping in longer languages: hands-on testing of the French UI after v6.22.2 showed the main window's Copy Address and Stop Server buttons and the log segment tabs still clipped in French, Spanish, Portuguese, Russian and German. Controls now fit their font size to the measured text width; the segment tabs use shorter labels and are laid out in proportion to each label's measured width instead of an even split, so they no longer overlap or get ellipsized.

- **状态页标签列截断**：状态页的标签列宽度原为固定值，内存占用、玩家连接等标签在所有语言下都被截断；现按当前语言下最长标签的实际宽度计算列宽，并相应右移数值列与网络行标签。
  Status panel labels clipped: the label column had a fixed width, and labels such as memory usage and player connections were clipped in every language. The column width is now computed from the widest label in the current language, and the value column and the network row labels shift to match.

- **备份页布局重排**：「立即备份」「恢复所选」「删除所选」按钮改为按文字实际宽度排版，先前被截断的按钮文字现可完整显示；保留份数一行、总数标签与底部提示一并重新排版，提示改为整行换行显示。
  Backups view layout rework: the Back Up Now, Restore Selected and Delete Selected buttons are laid out by measured text width, so the previously clipped button labels now display in full; the keep-count row, the total label and the bottom hint were rearranged with it, and the hint now wraps across the full width.

- **俄语「仅错误」标签与搜索框占位文字**：俄语「仅显示错误」勾选标签过长被截断，已改短并放宽其宽度上限；搜索框占位文字改为按框宽自适应绘制，不再在词中间断开。
  Russian "errors only" label and search placeholder: the Russian errors-only checkbox label was too long and clipped; it has been shortened and its width cap raised. The search box placeholder now fits itself to the box width instead of breaking off mid-word.

### 附件 / Assets

- `MCLauncherApp.exe` SHA256: `4C5BAF7D8D56EF3D7649AEFCDAD1DF291CF2EBA23FC87C65F26B99E585B42701`

## [v6.22.2] - 2026-11-12

### 修复 / Fixed

- **标题栏标题截断**：主窗口与首次启动窗口的标题原固定绘制在 260px 宽区域内，英文、西班牙语、俄语等较长标题显示不全；现改为按文字实际宽度自适应排版，空间不足时自动缩小字号、右侧副标题让位，10 种语言的标题均可完整显示。
  Title bar titles are no longer clipped: the fixed 260px drawing area is replaced with width-aware layout that scales the font down and yields the right-side subtitle when space runs short, so titles fit in all 10 languages.

- **切换语言后的重启提示**：切换界面语言并保存后，原提示仅有一个「确定」按钮；现改为「立即重启」与「取消」，点「立即重启」直接重启应用生效，但服务端或隧道正在运行时仍显示原提示、不执行重启，以免中断正在运行的服务。
  The restart prompt shown after changing the interface language now offers Restart Now and Cancel instead of a single OK button, so the new language can be applied without a manual restart; when a server or tunnel is running, the original prompt is still shown and no restart is triggered, so the running service is not interrupted.

### 附件 / Assets

- `MCLauncherApp.exe` SHA256: `6A5C749970EE71DDB0EFE56315F924BBC18DD99D30B7D99FA790DCE79E946678`

## [v6.22.1] - 2026-11-12

### 修复 / Fixed

- **首次启动语言选择窗口放大并统一外观**：窗口尺寸由 360×400 增大至 520×600，标题、语言列表与确认按钮统一改用 14pt 微软雅黑 UI 字体，解决高分辨率屏幕下文字过小、难以辨认的问题；窗口标题栏改为与主界面一致的深色，不再是系统默认的白色标题栏。
  First-launch language picker enlarged and restyled: the window grows from 360×400 to 520×600, and the heading, the language list and the confirm button now use 14 pt Microsoft YaHei UI, fixing text that was too small to read on high-resolution displays. The title bar is now dark, matching the main window, instead of the default white system title bar.

### 附件 / Assets

- `MCLauncherApp.exe` SHA256: `42D84E2896EB6E949CFCDF94BB8D2A35A3D0CC55464C2DE695CDE5DED620C48A`

## [v6.22] - 2026-11-12

### 新增 / Added

- **界面语言增至 10 种**：简体中文、繁體中文、English、日本語、한국어、Español、Português、Русский、Deutsch、Français。首次启动时先弹出语言选择窗口，选定后写入配置。
  Ten UI languages: Simplified Chinese, Traditional Chinese, English, Japanese, Korean, Spanish, Portuguese, Russian, German and French. A language picker opens on first launch and stores the choice in the configuration.
- **设置中可更改语言**：设置页新增界面语言选项，改选后重启程序生效。
  The language can be changed later in Settings; the new language applies after a restart.

### 变更 / Changed

- **中英文版合并为单一可执行文件**：原先分开的中文版 `MCLauncherApp.exe` 与英文版 `MCLauncherApp_EN.exe` 合并为一个文件，英文版文件停止单独发布。界面语言在运行时读取，不再按语言分别编译。
  The separate Chinese and English executables are merged into a single `MCLauncherApp.exe`; `MCLauncherApp_EN.exe` is no longer published separately. The UI language is read at runtime instead of being fixed at compile time.

### 附件 / Assets

- `MCLauncherApp.exe` SHA256: `EBDF7B0673BFF3572552A2DD8BB9E1263490139BFD3CEBD7B7B65820E5735660`

## [v6.21] - 2026-11-12

### 新增 / Added

- **日志页 spark 性能诊断入口**：日志页在日志框与指令行之间新增「性能诊断」一行，提供「TPS 报告」与「诊断 30 秒」两个按钮，经既有的服务端指令通道调用服务端已安装的 spark（`spark tps`、30 秒 profiler 采样），输出直接回显在日志中，不另开窗口。
  Spark diagnostics on the log page: a new "Performance diagnostics" row between the log view and the command row adds "TPS report" and "Diagnose 30 s" buttons. They drive the spark mod already installed on the server through the existing server-command channel (`spark tps`, a 30-second profiler run), with output shown in the log — no separate window.

### 变更 / Changed

- **应用图标重新制作**：图标的圆角方形底板改为透明背景重新抠制，边缘平滑无毛刺；`.ico` 包含 16 / 24 / 32 / 48 / 64 / 128 / 256 px 全套尺寸。
  Application icon redone: the rounded-square plate is now cut out on a transparent background with a clean, smooth edge; the `.ico` carries the full 16 / 24 / 32 / 48 / 64 / 128 / 256 px size set.
- **文案修订**：应用内文案与仓库文档统一修订，去除冗余与口号式表述（如主按钮「一键启动联机」改为「启动联机」），改为平实表述；功能与布局不变。
  Copy pass: in-app text and the repository documents were revised to remove redundant and slogan-style wording (e.g. the main button now reads "启动联机" / "Start Multiplayer"), in a plainer tone; features and layout are unchanged.
- **内部：中英文版合并为单一源码**：原先的 `Program.cs` 与 `Program_EN.cs` 两份源码合并为一份，全部界面文案收进文件内的双语字符串表；中文版照常编译，英文版在同一条命令上加 `/define:EN`。构建方式变更，功能与行为不变。
  Internals: the Chinese and English sources are merged into a single file — the former `Program.cs` / `Program_EN.cs` pair is now one `Program.cs` with all interface text in an in-file bilingual string table; the English build is the same command plus `/define:EN`. A build change only; features and behavior are unchanged.

### 附件 / Assets

- `MCLauncherApp.exe` SHA256: `a0a5d3b837b2f5661eab1b3b0c98046777343018f502c55c6f4b1e11e21ee4a8`
- `MCLauncherApp_EN.exe` SHA256: `a1095d1067525f9df64a8508396a6aea9d5f7209e3483a28d0135c21a690da72`

## [v6.20.3] - 2026-11-07

### 修复 / Fixed

- **「服务器属性」对话框底部按钮被切掉一半**：对话框内容实际需要 432 px 高，窗口只给了 428 px，保存/取消按钮沉到窗口下边缘之外被裁。高度修正为 444 px（按钮回框内并留 12 px 余量）。同时用探针逐个打开审计了全部六个对话框（设置、玩家管理、首次设置向导、提示框、确认框）的控件底边，其余五个均有余量，仅此一处超高。
  "Server Properties" dialog buttons clipped at the bottom: the dialog's content needs 432 px but the window was only given 428 px, sinking the Save/Cancel buttons past the lower edge. Height corrected to 444 px (buttons back inside with 12 px to spare). All six dialogs (Settings, Player Management, First-run Wizard, info and confirm boxes) were opened and measured with a UI probe; the other five have slack — this was the only overflow.

### 附件 / Assets

- `MCLauncherApp.exe` SHA256: `fd2c4d9f0d52b4437584930e9303dd23221b56cb40ceffd3d526efd587b4e2d3`
- `MCLauncherApp_EN.exe` SHA256: `ebb2c73912c44a491435900897dfc8825bc74f74b1d1ebc34972f558f1a5c39ca2`

## [v6.20.2] - 2026-11-06

### 修复 / Fixed

- **窗口打开时先透明闪一下（v6.20.1 未除根）**：v6.20.1 的延迟补刷能把鬼影最终刷掉，但真机上每次拉起窗口仍会先透出桌面约 0.4 秒再恢复。根源在 v6.19 引入的 WS_EX_COMPOSITED：隐藏→显示切换时合成层保留了旧表面，日志面板顶边一带的新画面迟迟不落定。现撤除该标志（治条纹真正依赖的 CS_VREDRAW/CS_HREDRAW 与日志批量刷新保留，400 ms 补刷保留作保险）。用用户的真实配置 + 模拟实时日志流量验证（QaProbe49–51）：隐藏→显示后 +30 ms 起逐帧残留 0.00%；最小化/恢复、日志收起/展开动画中途帧均无条纹复发。
  Transparent flash on open (root cause left by v6.20.1): the delayed heal removed the ghost eventually, but on a real machine the window still showed the desktop through for ~0.4 s on every restore. The culprit was WS_EX_COMPOSITED (added in v6.19): on hide -> show the composited layer keeps a stale surface and bands near the log panel settle late. The flag is removed (the stripe fix rests on CS_VREDRAW/CS_HREDRAW plus batched log writes, both kept; the 400 ms heal stays as a backstop). Verified with the user's real config and simulated live log traffic (QaProbe49-51): 0.00% residual from +30 ms after show; no stripe regression in minimize/restore or the log expand/collapse animation.

### 附件 / Assets

- `MCLauncherApp.exe` SHA256: `386c8378592045519c9d8eaa33c7cc7c788c657468ceb8ec31740173e27006fb`
- `MCLauncherApp_EN.exe` SHA256: `da5944fcebaf6013b2cdb6f55b966e89bdd983409f84fb82aa476e4cea259fc9`

## [v6.20.1] - 2026-11-06

### 修复 / Fixed

- **窗口“鬼影”透明**：窗口被隐藏再显示（如从托盘拉起、锁屏唤醒）后，部分横带不重绘、透出桌面壁纸。定位结论：残留未绘区域在切换的消息风暴之后才落定，即时重绘无效；现改为每次触发后合并一次即时重绘 + 延迟 400 ms 的第二次全量重绘。经桌面探针（QaProbe45–48）在出厂构建上复现并验证：完整切换序列（最小化/恢复 ×2 → 隐藏/显示 ×2）的残留像素从 43.15% 降至 0.00%。
  Ghost window: after a hide -> show transition (tray restore, session unlock), horizontal bands stayed unpainted and the desktop showed through. The stranded paint settles only after the transition's message burst, so an immediate repaint does not help; the heal now runs as a coalesced immediate pass plus a second full repaint 400 ms later. Reproduced and verified with desktop probes (QaProbe45-48): residual pixels across the full transition sequence dropped from 43.15% to 0.00%.

### 附件 / Assets

- `MCLauncherApp.exe` SHA256: `73c317aeb2dc13ec66d7a6c767d9d45423c83fcdb8e373555d9515b3ea58824e`
- `MCLauncherApp_EN.exe` SHA256: `58f0f6eeafc2643f0888b850dbed1605cad6c432c27f8925940a306ec7d39b64`

## [v6.20] - 2026-11-05

### 新增 / Added

- **启动前 Java 版本校验**：开服前核对服务端 jar 所需 Java 版本与实际 Java 版本，不匹配时弹窗说明并可选择仍然启动，不再让服务端直接崩溃。
  Pre-start Java check: verifies the Java in use against the version the server jar requires; on mismatch it explains and offers a start-anyway override.
- **首次设置向导**（设置页内置）：从零准备服务端——按所选 MC 版本自动下载匹配的 Java 与服务端 jar（原版 / Fabric / Paper；原版与 Paper 验哈希，Fabric 官方无哈希、向导会明示），写 eula 需手动勾选同意，并生成 `online-mode=false` 配置。
  First-run wizard (Settings page): prepares a server from scratch — downloads the matching Java and server jar (Vanilla / Fabric / Paper, hash-verified where upstream publishes hashes) and writes the eula (explicit consent checkbox) plus `online-mode=false` defaults.

### 修复 / Fixed

- 安全关服改走后台线程：服务端迟迟不退出时窗口不再假死 60 秒。
  Safe stop now runs on a background thread; the window no longer freezes for up to 60 s.
- 备份恢复的 zip 解压补齐完整路径穿越校验（与向导解压一致）。
  Backup-restore extraction gained the same full path-traversal guard as the wizard.
- 配置默认值清理：隧道 ID 默认值为空、frpc 路径默认为相对路径 `frpc\frpc.exe`（同 v6.19 的 347763d 修复）。
  Config defaults scrubbed: empty tunnel ID, relative frpc path.
- 状态页内存条旁去掉「上限 x GB」小字（应作者要求）。
  Removed the heap-cap caption next to the status page memory bar.

### 附件 / Assets

发布时先行上传 CI 构建（暂无自定义图标），2026-11-05 当日已替换为作者本地构建（带自定义图标；源码与功能完全一致）/ Initially CI builds (no custom icon); replaced on 2026-11-05 with the author's local builds (custom icon included; identical source and behavior):

- `MCLauncherApp.exe`：265,216 字节，SHA256 `f421658244d189e184ef77cd4636cdd0e89ffc1230f45e60ce5b3fed5ff3e9d4`
- `MCLauncherApp_EN.exe`：284,160 字节，SHA256 `4479a31920de2970022f0b55c3b6117c1d8c8434c4fdfb1200cf086e0216dd0a`

## [v6.19] - 2026-11-05

首个公开发布版本（本地迭代至 v6.19 后一次性公开）。

First public release (developed locally up to v6.19).

- 一键启动：Minecraft 服务端 + SakuraFrp 游戏隧道（TCP）+ 语音隧道（UDP）
- 世界自动备份与恢复、崩溃自动重启与中文崩溃诊断
- 修复日志区展开条纹与日志闪烁（窗口类补 CS_VREDRAW/CS_HREDRAW + WS_EX_COMPOSITED）

### 附件更新 / Asset refresh — 2026-11-05 05:52

源码未变（HEAD 仍为 347763d），Release 附件按修复后源码重新编译并替换：

The source did not change (HEAD remains 347763d); the release assets were rebuilt from the fixed source and replaced:

- 修复 / Fixed：源码内置默认值中含作者已作废的旧隧道 ID、且 frpc 路径默认指向不可直接运行的套装版路径（提交 347763d）。新附件中两者均已清除，`frpc` 路径默认值为相对路径 `frpc\frpc.exe`。
- `MCLauncherApp.exe`：237,568 → **141,312 字节**，SHA256 `d4086402541197f1c340ddda1d570cdf8ef846918d8ea0d27c015542a5c90086`
- `MCLauncherApp_EN.exe`：249,856 → **153,600 字节**，SHA256 `84c8548c0bb55841d5bb948efeea9c6b752af92c8312fd65a2d59600cada3058`
- 新附件为 CI 构建，暂无自定义图标，功能与原附件一致。

> 说明：本次以"同标签覆盖附件"的方式发布，是为了让已流出的旧附件尽快被修复版取代；该做法不利于区分新旧版本（只能靠文件大小/校验和），故自 v6.20 起改为上述升版规矩。
