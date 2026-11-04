# 更新日志 / Changelog

本项目的所有显著变更都会记录在这里。从 v6.20 起执行新规：**发布附件（二进制）有任何变动，必须升版本号、在本文件留记录、并在 Release 说明中附 SHA256 校验和**，不再静默覆盖同一标签的附件。

All notable changes are documented here. From v6.20 on: any change to release binaries requires a version bump, an entry in this file, and SHA256 checksums in the release notes — no more silent asset replacement under the same tag.

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
