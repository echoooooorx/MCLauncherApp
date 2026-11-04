# Minecraft 联机助手（MCLauncherApp）

一个 Windows 桌面小工具：把「启动 Minecraft 服务端 + 打开 SakuraFrp 内网穿透隧道 + 语音隧道」这套每次联机都要重复的操作，收进一个窗口、一键完成。

为 1.20.1 Fabric 离线联机场景做的：服务端 `online-mode=false`，朋友用离线账号直连隧道地址即可进服。

> 说明：本项目在 AI 辅助下开发——需求、测试与验收由作者本人完成，代码由 AI 编写并经作者实机验证。

## 功能

- **一键启停**：同时拉起服务端进程与 SakuraFrp 游戏隧道、语音隧道（Simple Voice Chat），停止时一并干净关闭
- **双日志面板**：服务端日志与隧道日志分开显示，可搜索（Ctrl+F）、可发送指令到服务端控制台（支持 Shift+Enter 多行）
- **展开/收起动画**：日志区可收起成紧凑窗口，展开/收起带缓动动画
- **开服前自动备份**世界存档；另有手动备份管理页（查看/恢复/删除备份）
- **状态页**：服务器运行状态、在线情况、spark 友好的诊断信息
- **设置页**：SakuraFrp 访问密钥、游戏/语音隧道 ID、服务端目录、PCL2 路径、服务端内存参数（-Xms/-Xmx 与 Aikar G1GC 参数）都存在本地 `config.json`

## 运行要求

- Windows 10/11，.NET Framework 4.x（系统自带即可）
- 一个 Minecraft Java 版服务端文件夹（默认 `D:\MCServer`，设置里可改）
- [SakuraFrp](https://www.natfrp.com/) 账号与隧道（游戏隧道走 TCP，语音隧道走 UDP），以及独立的 `frpc.exe`
- 首次使用：打开设置页，填访问密钥和两个隧道 ID，程序会在日志区提示

## 编译

单文件 WinForms 程序，无第三方依赖。用系统自带的 C# 编译器即可：

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe ^
  /win32icon:assets\icon.ico /win32manifest:app.manifest /out:MCLauncherApp.exe ^
  /reference:System.dll /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll ^
  Program.cs
```

（图标 `assets\icon.ico` 来自作者本地部署，未包含在本仓库；去掉 `/win32icon` 参数即可无图标编译。）

## 配置

程序同目录的 `config.json` 保存全部设置，由程序在设置页里读写。**这个文件里有你的 SakuraFrp 访问密钥，绝对不要提交到任何仓库**——本仓库的 `.gitignore` 已经排除它。

## 已知问题

- v6.19 已修复：日志区展开时闪过一条横纹、日志滚动刷新闪烁（见下方版本说明）。这个条纹在 v6.14–v6.17 修了三轮都没根除，v6.19 通过慢速动画逐帧定位后确认根源是窗口类缺少 CS_VREDRAW/CS_HREDRAW 重绘样式——每次改高只有新露出的窄条等下一帧绘制。修复方式：补上两个重绘样式让每步整窗重绘，再加 WS_EX_COMPOSITED 让整帧原子呈现；日志追加改用 WM_SETREDRAW 批量成一次重绘。

## 版本

当前版本 **v6.19**。单文件源码 `Program.cs` 即完整程序。
