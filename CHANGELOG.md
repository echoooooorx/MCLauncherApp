# 更新日志 / Changelog

本项目的所有显著变更都会记录在这里。从 v6.20 起执行新规：**发布附件（二进制）有任何变动，必须升版本号、在本文件留记录、并在 Release 说明中附 SHA256 校验和**，不再静默覆盖同一标签的附件。

All notable changes are documented here. From v6.20 on: any change to release binaries requires a version bump, an entry in this file, and SHA256 checksums in the release notes — no more silent asset replacement under the same tag.

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
