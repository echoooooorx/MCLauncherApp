# 参与贡献 / Contributing

MCLauncherApp 是一个单文件 C# WinForms 程序，改起来不复杂。

MCLauncherApp is a single-file C# WinForms app, so the contribution flow is short.

## 项目结构 / Layout

- `Program.cs` — 唯一的主源码，中英文界面共用；界面文案存放在文件内的双语字符串表中，编译时按语言选取
- `app.manifest` — 编译时需要的清单文件
- `AI_DEPLOY.md` — 给 AI 助手用的整套部署手册

## 本地编译 / Building

只需要 Windows 自带的 .NET Framework 编译器，不需要 Visual Studio，也不需要联网：

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32manifest:app.manifest /out:MCLauncherApp.exe /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll Program.cs
```

英文版用同一条命令：加上 `/define:EN`、输出名换成 `MCLauncherApp_EN.exe` 即可。仓库里的 Release 附带图标；自行编译时不带 `/win32icon` 也能正常运行，只是用默认图标。

## 改代码时请注意 / Rules for changes

1. **文案表双列必须同步**：改任何界面文案时，源码内双语字符串表的中英两列要一起改，只改一列的 PR 不会被合并。
2. 源码中有一处字符串**故意保留中文**：匹配 SakuraFrp frpc 输出日志的正则（它匹配的是 frpc 程序自身的中文日志），不要翻译它。
3. 不要提交 `config.json`、访问密钥、隧道 ID、公网地址等任何个人配置或凭据。
4. 提交前请至少本地编译通过一遍；推送后 GitHub Actions 也会自动编译中英两版。

## 提 Issue / Issues

- Bug 请用仓库的 Bug 模板，写清版本、Windows 版本和复现步骤；粘贴日志前请先删掉访问密钥和隧道相关信息。
- 部署中卡住，先看 `AI_DEPLOY.md`——也可以把这份文件发给你的 AI 助手，让它按手册帮你排查。
