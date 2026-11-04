# 参与贡献 / Contributing

感谢你对 MCLauncherApp 感兴趣。这个项目是一个单文件 C# WinForms 程序，贡献方式很简单。

Thank you for your interest in MCLauncherApp. It is a single-file C# WinForms app, so contributing is straightforward.

## 项目结构 / Layout

- `Program.cs` — 中文界面版源码（唯一的主源码）
- `Program_EN.cs` — 英文界面版源码，与 `Program.cs` 功能完全一致，仅 UI 文案不同
- `app.manifest` — 编译时需要的清单文件
- `AI_DEPLOY.md` — 给 AI 助手用的整套部署手册

## 本地编译 / Building

只需要 Windows 自带的 .NET Framework 编译器，不需要 Visual Studio，也不需要联网：

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32manifest:app.manifest /out:MCLauncherApp.exe /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll Program.cs
```

英文版把 `Program.cs` 换成 `Program_EN.cs`、输出名换成 `MCLauncherApp_EN.exe` 即可。仓库里的 Release 附带图标；自行编译时不带 `/win32icon` 也能正常运行，只是用默认图标。

## 改代码时请注意 / Rules for changes

1. **中英两版必须同步**：任何功能或修复都要同时改 `Program.cs` 和 `Program_EN.cs`，只改一版的 PR 不会被合并。
2. `Program_EN.cs` 中有一处字符串**故意保留中文**：匹配 SakuraFrp frpc 输出日志的正则（它匹配的是 frpc 程序自身的中文日志），不要翻译它。
3. 不要提交 `config.json`、访问密钥、隧道 ID、公网地址等任何个人配置或凭据。
4. 提交前请至少本地编译通过一遍；推送后 GitHub Actions 也会自动编译中英两版。

## 提 Issue / Issues

- Bug 请用仓库的 Bug 模板，写清版本、Windows 版本和复现步骤；粘贴日志前请先删掉访问密钥和隧道相关信息。
- 部署中卡住，先看 `AI_DEPLOY.md`——也可以把这份文件发给你的 AI 助手，让它按手册帮你排查。
