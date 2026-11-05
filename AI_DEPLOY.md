# AI 部署手册：Minecraft 联机助手（MCLauncherApp）全流程部署

> **这份文件是写给 AI 助手看的。** 如果你是一个正在协助用户部署 Minecraft 联机环境的 AI（能读写文件、能执行命令、能操作这台 Windows 电脑），请通读全文后按阶段执行：每个阶段都有明确的操作、验证标准和失败处理。部署目标是让用户实现：**本地 Minecraft 服务端（以 1.20.1 Fabric 为例，其他版本见第 2 节的版本对照）+ SakuraFrp 内网穿透（游戏 + 语音两条隧道）+ 本启动器一键启停**，朋友用离线账号即可加入。
>
> *Abstract (EN): This is a runbook for AI assistants. It deploys, step by step with verification gates, a local Minecraft 1.20.1 Fabric server (online-mode=false) behind two SakuraFrp tunnels (TCP 25565 for the game, UDP 24454 for Simple Voice Chat), driven by the MCLauncherApp one-click launcher in this repository. The SakuraFrp access key must only ever be typed by the user into the app's Settings page — never ask the user to paste it into a chat, and never write it into any file that leaves this machine.*

---

## 0. 给 AI 的工作守则

1. **一步一验证**：每个阶段末尾都有验证标准。验证没过，不要跳到下一阶段，先按故障排查处理。
2. **访问密钥红线**：SakuraFrp 访问密钥（FrpKey）只能由**用户本人**在本程序的「设置」页输入。不要让用户把密钥发到聊天里，不要把密钥写进仓库、脚本、日志或任何会离开这台机器的文件。`config.json` 含密钥，永远不要提交到 Git（仓库的 `.gitignore` 已排除它，你也要自觉）。
3. **改文件先备份**：改 `server.properties`、`voicechat-server.properties`、服务端存档之前，先复制一份备份。涉及世界存档的操作（恢复备份、换图）必须先问用户。
4. **不擅自扩大范围**：不要顺手给用户装计划任务、开机自启或常驻服务；隧道和服务端都应该是「用时启动、关时全退」。
5. **危险操作留喊停余地**：关机、删文件夹、杀进程这类不可逆动作，执行前先告知用户并留出取消窗口。
6. **区分两类失败**：「服务器本身的问题」和「隧道/网络的问题」现象相似但修法完全不同——服务端日志健康（无 `Can't keep up`、无崩溃）而朋友掉线，多半是隧道路径问题，不要去乱改服务端。

---

## 1. 最终形态

```
朋友的电脑（PCL2 离线登录）
   │  连接公网地址，如 xxx.natfrp.com:12345
   ▼
SakuraFrp 节点（公网）
   │  游戏隧道 TCP ──► frpc ──► 127.0.0.1:25565 ──► Minecraft 服务端
   │  语音隧道 UDP ──► frpc ──► 127.0.0.1:24454 ──► Simple Voice Chat（服务端 mod）
   ▲
本机：MCLauncherApp 一键启动/关闭上面全部进程，并守护崩溃自动重连
```

| 通道 | 协议 | 本地端口 | 用途 |
|---|---|---|---|
| 游戏隧道 | TCP | 25565 | 玩家进服 |
| 语音隧道 | UDP | 24454 | Simple Voice Chat 语音 |

---

## 2. 前置检查

在用户的 Windows 电脑上逐项确认：

- [ ] Windows 10/11，.NET Framework 4.x 可用（系统自带，一般无需处理）
- [ ] Java 已安装且**版本与用户的 MC 版本匹配**：1.18–1.20.4 → **Java 17**；1.20.5 起（含 1.21.x）→ **Java 21**。用 `java -version` 核对；没有就装对应版本（Temurin / 微软 OpenJDK 均可）并记下 `java.exe` 路径。本手册以 1.20.1 + Java 17 为例——换其他版本时只有 **Java 版本、服务端 jar、mod 版本**三处不同，流程不变
- [ ] 磁盘空间足够：服务端 + 存档备份建议预留 5 GB 以上
- [ ] 用户已有（或愿意注册）SakuraFrp 账号；建隧道需要**实名认证**，这一步只能用户本人完成
- [ ] 约定目录（可按用户习惯调整，下文按此写）：
  - 服务端：`D:\MCServer`
  - 启动器：任意固定目录，如 `D:\MCLauncherApp`，内含 `MCLauncherApp.exe` 和子目录 `frpc\frpc.exe`

---

## 3. 阶段一：准备 Minecraft 服务端

1. 按用户的版本与加载器获取服务端（以 Fabric 1.20.1 为例：从 [fabricmc.net](https://fabricmc.net/use/server/) 获取，或用原版 server.jar + Fabric Loader 安装器生成；原版 / Paper / Forge / NeoForge 同理替换来源）；把服务端 jar 命名为 `server.jar` 放进服务端目录（jar 名可自定，与第 6 阶段设置一致即可）。
2. 在服务端目录手动启动一次：`java -Xms1G -Xmx2G -jar server.jar nogui`，等它生成文件并因 eula 停下。
3. 编辑 `eula.txt`：`eula=true`。
4. 编辑 `server.properties`，确认：
   - `online-mode=false` ← **最重要**。不改，离线账号的朋友会被正版验证挡在门外（报错 `Failed to verify username`）
   - `server-port=25565`
   - （可选）`view-distance`、`simulation-distance` 按机器性能调；2–4 人联机默认即可
5. 把服务端 mod 放进 `mods\`：
   - 必备：Fabric API
   - 语音：与用户版本/加载器对应的 Simple Voice Chat 服务端版（阶段五细说）
   - 注意每个 mod 的环境要求：纯服务端 mod 只放服务端；**双端 mod（如 Farmer's Delight）必须同时发给每个玩家放进他们客户端的 `mods\`**，版本要一字不差，否则对方进服会被踢
6. **验证**：再次手动启动服务端，日志出现 `Done (x.xxx s)! For help, type "help"` 且无 ERROR 级报错；在控制台输入 `stop` 正常关服。若这一步不过，先修服务端，不要继续。

---

## 4. 阶段二：SakuraFrp 账号与隧道（用户亲自操作）

以下涉及账号、实名和密钥，**由用户本人在浏览器里完成**，AI 只负责给出清单并回收非敏感信息：

请用户依次完成：

1. 注册 [SakuraFrp](https://www.natfrp.com/) 并完成实名认证；
2. 新建**游戏隧道**：协议 TCP，本地地址 `127.0.0.1`，本地端口 `25565`，节点就近选；
3. 新建**语音隧道**：协议 **UDP**，本地地址 `127.0.0.1`，本地端口 `24454`；
4. 在用户中心找到自己的**访问密钥**——**只让用户记住它在第 6 阶段亲手填，不要发给你**。

AI 需要回收并记录的只有四项（都不敏感）：**游戏隧道 ID、游戏隧道公网地址、语音隧道 ID、语音隧道公网地址**。公网地址形如 `frp-xxx.com:12345`，来自隧道详情页。

---

## 5. 阶段三：独立版 frpc

1. 从 SakuraFrp 官网下载**独立版 frpc**（与官网启动器分开提供的那个），放到启动器目录的 `frpc\frpc.exe`（或任意固定路径，第 6 阶段要填进设置）。
2. **千万不要**用 SakuraFrp 启动器安装目录（默认 `C:\Program Files\SakuraFrpLauncher\`）里自带的 frpc：那是**套装版**，只能由 SakuraFrp 的系统服务拉起，被别的程序直接运行会立刻退出。判别特征：frpc 位于 SakuraFrpLauncher 安装目录内。
   - 本启动器内置了套装版检测：真用错了，隧道日志会明确提示「检测到当前 frpc.exe 是 SakuraFrp 启动器自带的套装版……」，照提示换独立版即可，不必怀疑密钥或隧道。
3. 验证：文件存在、路径固定即可；能否真正连通在第 6 阶段由启动器的隧道日志验证。

---

## 6. 阶段四：部署启动器并配置

### 6.1 拿到程序

- 首选：到本仓库的 **Releases** 下载 `MCLauncherApp.exe`（中文界面）或 `MCLauncherApp_EN.exe`（英文界面）；
- 或自行编译：用仓库的 `Program.cs`（英文版用 `Program_EN.cs`）按 README 的「编译」一节命令构建，无第三方依赖。

把 exe 放进约定目录（如 `D:\MCLauncherApp\`）。

### 6.2 填写设置

启动程序 →「设置」，逐项对照下表填。标 ★ 的三项（访问密钥）由用户亲手粘贴：

| 设置页字段 | config.json 键 | 填什么 | 默认/示例 |
|---|---|---|---|
| 服务端目录 | `ServerDir` | 服务端文件夹 | `D:\MCServer` |
| Java 路径 | `JavaPath` | 留空=用系统 Java；或 `java.exe` 全路径 | 空 |
| 服务端 jar | `JarName` | 与服务端目录里的 jar 文件名一致 | `server.jar` |
| 内存参数 | `MemParams` | JVM 参数。2–4 人建议 `-Xms2G -Xmx2G`，或 Aikar G1GC 全套参数 | `-Xms1G -Xmx2G` |
| 服务端端口 | `Port` | 与 `server-port` 一致 | `25565` |
| frpc.exe 路径 | `FrpcPath` | 阶段三的独立版 frpc | `D:\MCLauncherApp\frpc\frpc.exe` |
| 访问密钥 ★ | `FrpKey` | 用户的 SakuraFrp 访问密钥（用户亲手填） | — |
| 隧道 ID | `TunnelId` | 游戏隧道 ID（阶段二回收） | — |
| 语音隧道 ID | `VoiceTunnelId` | 语音隧道 ID；留空则自动跳过语音隧道 | — |
| 公网地址 | `PublicAddr` | 程序自动维护，不要手填 | — |
| PCL2 路径 | `Pcl2Path` | 用户的 PCL2 exe（「打开 PCL2」按钮用） | 可空 |
| 隧道方式 | `TunnelMode` | `frpc`（命令行，首选）；`launcher` 是备用方式（打开官方启动器、需用户手动点启动） | `frpc` |
| 备份保留份数 | `BackupKeep` | 世界备份保留数量 | `10` |
| 开服前自动备份 | `BackupOnStart` | 建议开 | `false` |
| 崩溃自动重启 | `AutoRestart` | 服务端/frpc 异常退出后自动守护重连 | `true` |

注意事项：

- `config.json` 由程序自己读写（单行 JSON，改坏了程序有 `.bak` 兜底）。**优先让用户在设置页里改**；你确需直接改文件时，先关掉程序、备份原文件、保持合法 JSON 再写回。
- 程序启动时会从服务端目录的 `start.bat` / `server.properties` 自动补全留空项，所以 `start.bat` 里写对 Java 和内存也有帮助。
- 程序实际的启动方式（供你排障时核对）：服务端 = `java <MemParams> -jar <JarName> nogui`（工作目录=服务端目录）；隧道 = 对游戏、语音各起一个 `frpc.exe -f <访问密钥>:<隧道ID>` 进程，并监听其输出中的「代理启动成功」字样确认隧道建立。

### 6.3 本阶段验证

- [ ] 程序能打开，设置保存后重开程序数值不丢
- [ ] 点「一键启动联机」后：服务端日志出现 `Done`；隧道日志出现两次「代理启动成功」（游戏 + 语音；没配语音则一次 + 一条「未填写语音隧道 ID，跳过」）
- [ ] 主界面「公网地址」显示的正是游戏隧道的公网地址

> **v6.20 补充**：启动器设置页已内置「首次设置向导」，可自动完成阶段一的服务端与 Java 准备（按所选 MC 版本下载匹配的 Java 与服务端 jar 并校验哈希，写 eula 前有明确同意勾选）。AI 部署仍可按上文手动流程执行，两者结果等价；向导下载或校验失败时回退到手动流程。另外 v6.20 起，点「一键启动联机」时会先做 Java 版本校验，不匹配会弹窗拦截——联调时若被拦截，先核对 Java 版本再排查其他问题。

---

## 7. 阶段五：语音聊天（Simple Voice Chat）

1. **安装矩阵**（版本必须全员一致：同一个 MC 版本 + 同一个加载器 + 同一个 SVC 版本；本例为 1.20.1 + Fabric）：
   - 服务端 `mods\`：与用户 MC 版本和加载器对应的 Simple Voice Chat 服务端版
   - **每一个玩家**（含房主自己）的客户端 `mods\`：同版本 voicechat
   - 只装一端是没声/进不去的最常见原因
2. 服务端启动一次后，编辑 `服务端目录\config\voicechat\voicechat-server.properties`：
   - `port=24454`
   - `voice_host=<语音隧道的公网地址>`，如 `voice_host=frp-xxx.com:23456`——**填游戏隧道的地址是最常见的错误，务必核对**
3. 重启服务端生效。
4. 客户端自检：进游戏按 **V** 打开语音设置，确认麦克风、播放设备选对，音量正常；需要时改成按键说话。
5. **验证**：两名玩家进服、站到一起（语音是近距离衰减的，离太远听不见是正常现象），互相说话能听到，对方名字旁出现语音图标。

---

## 8. 阶段六：联调验收清单

逐项打勾，全部通过才算部署完成：

- [ ] 启动器一键启动后，服务端日志 `Done`，无持续报错
- [ ] 隧道日志：游戏、语音两条隧道都「代理启动成功」
- [ ] 主界面公网地址 = 游戏隧道公网地址
- [ ] 房主本机用 PCL2 离线账号、连**公网地址**能进服（先自测再喊朋友）
- [ ] 至少一位朋友从外网用公网地址成功进服
- [ ] 双人语音互听正常
- [ ] 「安全关服」后服务端存档正常退出，frpc 进程全部结束，无残留 java/frpc 进程
- [ ] （若开启）备份页能看到一份世界备份，大小合理（不是 0 KB）

给朋友的说明模板（把地址换成实际公网地址）：

> 用 PCL2 离线登录（名字随便起），进游戏 → 多人游戏 → 添加服务器，地址填 `＜公网地址＞`。语音 mod 记得装，和我同版本，进服按 V 设置麦克风。

---

## 9. 故障排查表

| 现象 | 先查什么 | 处理 |
|---|---|---|
| frpc 秒退，隧道日志提示「套装版」 | `FrpcPath` 是否指到 SakuraFrpLauncher 安装目录 | 换独立版 frpc 路径（阶段三） |
| frpc 秒退，无套装版提示 | 访问密钥/隧道 ID 是否填错；账号是否完成实名 | 让用户在设置页重新粘贴密钥、核对隧道 ID；去 SakuraFrp 确认实名状态 |
| 朋友连接超时 / refused | 游戏隧道是不是 TCP、本地端口 25565；服务端是否真的 `Done`；公网地址是否复制错 | 逐项核对；房主先用公网地址自测，区分是服务端问题还是对方网络问题 |
| 进服报 `Failed to verify username` | `server.properties` 的 `online-mode` | 改 `false` 并重启服务端 |
| 语音完全没声 | `voice_host` 是否填了**语音**隧道地址；语音隧道是否 UDP；双方客户端是否都装了 mod | 按阶段五逐项核对，这是语音三件套，缺一不可 |
| 语音有声但断断续续 / 朋友游戏卡 | 服务端日志有没有 `Can't keep up` | 没有 = 服务端没问题，是隧道节点/网络丢包：换 SakuraFrp 节点或错峰；不要去改服务端参数 |
| 启动报端口被占用 | 25565 是否被另一个服务端/程序占着 | 关掉残留 java 进程再启动 |
| 服务端内存溢出崩溃 | 机器物理内存与 `MemParams` 是否匹配 | 16 GB 机器给服务端 `-Xmx2G` 足够；整合包级别的服务端再上调，并留足系统余量，**不要把 -Xmx 设到接近物理内存** |
| Java 版本报错 | `java -version` 与设置页 Java 路径 | 按 MC 版本装对应 Java（1.18–1.20.4 → 17；1.20.5 起 → 21）并在设置里指到它 |

---

## 10. 日常运维（部署完成后告诉用户这些）

- 开服：打开启动器 → 一键启动 → 把主界面的公网地址发朋友。关服：用「安全关服」，不要直接关窗口杀进程。
- 世界备份在 `服务端目录\backups\`，启动器的「备份」页可以查看/恢复/删除；恢复备份前先关服。
- 崩溃守护默认开启：服务端或 frpc 异常退出会自动重连/重启，日志里以 `[守护]` 开头标注。
- 访问密钥若怀疑泄露：去 SakuraFrp 重置密钥，然后在设置页更新 `FrpKey` 即可，隧道 ID 不用动。
- 换电脑/重装时要带走的东西：服务端整个文件夹（含 `world` 和 `mods`）、启动器的 `config.json`（含密钥，注意保管）、独立版 frpc。

---

## 附录：相关项目与关键词

- SakuraFrp（内网穿透服务）：https://www.natfrp.com/
- frp（SakuraFrp 的底层开源项目）：https://github.com/fatedier/frp
- Simple Voice Chat（语音 mod）：https://github.com/henkelmax/simple-voice-chat
- PCL2（朋友端启动器）：https://github.com/Meloong-Git/PCL
- spark（服务端性能分析）：https://github.com/lucko/spark
- Aikar G1GC 参数：https://docs.papermc.io/paper/aikars-flags

检索关键词（中英）：Minecraft 离线联机教程、Fabric 1.20.1 服务端搭建、SakuraFrp 隧道、frpc 内网穿透、Simple Voice Chat 语音隧道、online-mode=false、PCL2 离线登录、Minecraft server behind NAT traversal、voice chat UDP tunnel。
