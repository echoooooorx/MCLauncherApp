using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

namespace MinecraftLauncher
{
    // ---------- UI string table (v6.22 single-source, runtime language) ----------
    // Ten language tables live side by side (TxtCn / TxtTw / TxtEn /
    // TxtJa / TxtKo / TxtEs / TxtPt / TxtRu / TxtDe / TxtFr) and Txt
    // forwards every key to the active one, chosen at startup by
    // language code (Lang.Code); unknown codes fall back to TxtCn.
    // Add new UI text to ALL TEN tables; a missing entry still fails
    // the compile (CS0117) on purpose, because every Txt property
    // references every table.
    // Non-UI literals (paths, URLs, the frpc success regex matching
    // SakuraFrp's own Chinese log output, ...) stay inline in the code.
    static class Lang
    {
        // Current UI language code (one of Codes). Set once in
        // Program.InitLanguage before any UI is built; the Settings
        // page changes it for the NEXT launch (already-built UI is
        // not rebuilt in place).
        public static string Code = "cn";

        // Language codes and their native display names, in picker /
        // Settings order: Names[i] is the display name of Codes[i].
        public static readonly string[] Codes = { "cn", "tw", "en", "ja", "ko", "es", "pt", "ru", "de", "fr" };
        public static readonly string[] Names = { "简体中文", "繁體中文", "English", "日本語", "한국어", "Español", "Português", "Русский", "Deutsch", "Français" };

        public static int IndexOf(string code)
        {
            for (int i = 0; i < Codes.Length; i++) if (Codes[i] == code) return i;
            return -1;
        }

        public static bool IsKnown(string code)
        {
            return IndexOf(code) >= 0;
        }

        public static string CodeAt(int index)
        {
            return (index >= 0 && index < Codes.Length) ? Codes[index] : "cn";
        }

        // First-run default: the OS UI language when it is one of the
        // supported ones, else Simplified Chinese.
        public static string DetectDefault()
        {
            try
            {
                var ui = System.Globalization.CultureInfo.CurrentUICulture;
                if (ui.TwoLetterISOLanguageName == "zh")
                {
                    string n = ui.Name;
                    if (n.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0
                        || n == "zh-TW" || n == "zh-HK" || n == "zh-MO") return "tw";
                    return "cn";
                }
                if (IsKnown(ui.TwoLetterISOLanguageName)) return ui.TwoLetterISOLanguageName;
            }
            catch { }
            return "cn";
        }
    }

    static class TxtEn
    {
        public const string Msg = "Something went wrong";
        public const string ShowFriendlyErrorStr = ", and the program needs to close.";
        public const string ShowFriendlyErrorStr2 = ", and the program will keep running.";
        public const string ShowFriendlyErrorPart = "\n\nIf it keeps happening, just send this info to Naiwa.";
        public const string ShowFriendlyErrorShow = "Minecraft Multiplayer Helper";
        public const string YesText = "Stop the server safely before exiting";
        public const string NoText = "Cancel";
        public const string SettingsFormInitChrome = "Settings";
        public const string SettingsFormAddField = "Server folder D:\\MCServer";
        public const string SettingsFormAddField2 = "Full Java path (java.exe)";
        public const string SettingsFormAddField3 = "jar file name";
        public const string SettingsFormAddField4 = "Memory arguments";
        public const string SettingsFormAddField5 = "Port";
        public const string SettingsFormAddField6 = "frpc.exe path";
        public const string LbModeCaption = "Tunnel mode";
        public const string SettingsFormAdd = "frpc command line (preferred)";
        public const string SettingsFormAdd2 = "Open the official SakuraFrp launcher (fallback)";
        public const string LbKeyCaption = "SakuraFrp access key";
        public const string SettingsFormAddField7 = "Tunnel ID";
        public const string SettingsFormAddField8 = "Voice tunnel ID (optional)";
        public const string SettingsFormAddField9 = "Public address (optional)";
        public const string SettingsFormAddField10 = "PCL2 exe path";
        public const string BtnPropsText = "Server Properties…";
        public const string SettingsFormShow = "Set the correct server folder above first, then edit server properties.";
        public const string SettingsFormShow2 = "Server Properties";
        public const string BtnWizardText = "First-run wizard";
        public const string BtnSaveText = "Save";
        public const string ValidateSettingsMarkInvalid = "The server folder does not exist. Check the path.";
        public const string ValidateSettingsMarkInvalid2 = "The Java path does not exist. Check it.";
        public const string ValidateSettingsMarkInvalid3 = "The jar file name cannot be empty.";
        public const string ValidateSettingsMarkInvalid4 = "That jar file was not found in the server folder.";
        public const string ValidateSettingsMarkInvalid5 = "The port must be a number between 1 and 65535.";
        public const string ValidateSettingsMarkInvalid6 = "The frpc.exe path does not exist. Check it.";
        public const string ValidateSettingsMarkInvalid7 = "In frpc mode, the tunnel ID cannot be empty.";
        public const string ValidateSettingsMarkInvalid8 = "The PCL2 path does not exist. Check it.";
        public const string TitleText = "Minecraft Multiplayer Helper v6.22.5";
        public const string SubText = "Dual tunnel · Game + Voice";
        public const string ServerText = "Server: Not running";
        public const string TunnelText = "SakuraFrp tunnel: Not running";
        public const string AddrText = "Public address: Not generated";
        public const string SrvState = "Not running";
        public const string MbText = "Players";
        public const string OnPaintHeader = "Server process";
        public const string OnPaintRow = "Status";
        public const string OnPaintRow2 = "Memory usage";
        public const string OnPaintRow3 = "Player connections";
        public const string OnPaintDrawText = "Online players";
        public const string OnPaintRow4 = "Version · Ping";
        public const string OnPaintHeader2 = "Tunnel process (frpc)";
        public const string OnPaintHeader3 = "Local";
        public const string OnPaintDrawText2 = "Down";
        public const string OnPaintDrawText3 = "Up";
        public const string OnPaintRow5 = "System memory";
        public const string OnHandleCreatedIndexOf = "Error";
        public const string OnHandleCreatedStr = "Error";
        public const string ShowMsg = "OK";
        public const string PropsFormAddCombo = "Difficulty";
        public const string PropsFormStr = "Peaceful";
        public const string PropsFormStr2 = "Easy";
        public const string PropsFormStr3 = "Normal";
        public const string PropsFormStr4 = "Hard";
        public const string PropsFormAddCombo2 = "Game mode";
        public const string PropsFormStr5 = "Survival";
        public const string PropsFormStr6 = "Creative";
        public const string PropsFormStr7 = "Adventure";
        public const string PropsFormStr8 = "Spectator";
        public const string PropsFormAddNum = "Max players";
        public const string PropsFormAddNum2 = "View distance (chunks)";
        public const string PropsFormAddNum3 = "Simulation distance (chunks)";
        public const string PropsFormAddNum4 = "Spawn protection radius";
        public const string PropsFormAddText = "Server MOTD";
        public const string PropsFormAddCheck = "Enable whitelist (white-list)";
        public const string PropsFormAddCheck2 = "Allow PvP (pvp)";
        public const string PropsFormAddCheck3 = "Allow flight (allow-flight)";
        public const string LbOnCaption = "Online mode: Off (must stay off for offline multiplayer; cannot be changed here)";
        public const string PropsFormShow = "server.properties was not found in the server folder. It is created the first time the server starts successfully.";
        public const string SavePropsShow = "Max players must be a number between 1 and 9999.";
        public const string SavePropsShow2 = "View distance must be a number between 2 and 32.";
        public const string SavePropsShow3 = "Simulation distance must be a number between 2 and 32.";
        public const string SavePropsShow4 = "Spawn protection radius must be a number between 0 and 9999.";
        public const string SavePropsShow5 = "Save failed: ";
        public const string SavePropsShow6 = "Saved. The server is running, so these settings take effect on the next start.";
        public const string PlayerFormStr = "Online players (";
        public const string PlayerFormStr2 = "No players online right now";
        public const string PlayerFormAddBtn = "Teleport to me";
        public const string PlayerFormAddBtn2 = "Kick from server";
        public const string PlayerFormConfirmDo = "Kick ";
        public const string PlayerFormConfirmDo2 = " from the server?";
        public const string PlayerFormAddBtn3 = "Give OP";
        public const string PlayerFormAddBtn4 = "Remove OP";
        public const string PlayerFormAddBtn5 = "Ban";
        public const string PlayerFormConfirmDo3 = "Ban ";
        public const string PlayerFormConfirmDo4 = "? They will not be able to join (you can unban them later).";
        public const string PlayerFormAddBtn6 = "Unban";
        public const string PlayerFormStr3 = "Note: \"Teleport to me\" is unavailable while you are not online yourself.";
        public const string PlayerFormStr4 = "Actions are sent to the server as console commands, just like typing them in the command box.";
        public const string CloseText = "Close";
        public const string LbOpsCaption = "OP list: ";
        public const string RefreshListsStr = "(none)";
        public const string LbBannedCaption = "Ban list: ";
        public const string WizardFormInitChrome = "First-run setup wizard";
        public const string WizardFormAddLabel = "Minecraft version";
        public const string WizardFormAddHint = "Any release version works";
        public const string WizardFormAddLabel2 = "Server type";
        public const string WizardFormAddRadio = "Vanilla";
        public const string WizardFormAddLabel3 = "Server folder";
        public const string BtnBrowseText = "Browse...";
        public const string Description = "Pick the server folder (it does not need to exist yet - pick a parent and edit the path text)";
        public const string ChkEulaText = "I agree to the Minecraft EULA";
        public const string LnkEulaText = "Read the EULA";
        public const string WizardFormAddHint2 = "Without the tick you must set eula=true in eula.txt yourself before the first start";
        public const string LblStepCaption = "Fill in the three items above and click Start install. Java and the server are downloaded and verified automatically.";
        public const string BtnStartText = "Start install";
        public const string WizardFormWizLog = "Cancelling...";
        public const string StartInstallShow = "Enter a Minecraft version first.";
        public const string StartInstallShow2 = "That does not look like a release version number. Enter something like 1.20.1 (snapshots are not supported by the wizard).";
        public const string StartInstallShow3 = "Enter a server folder first.";
        public const string StartInstallShow4 = "The target folder already contains a server.jar. Continuing will overwrite it.\n\nYour world and mods are not touched (the wizard only replaces the jar and adds missing files). Continue?";
        public const string StartInstallShow5 = "Overwrite";
        public const string StartInstallWizLog = "-- Install started: Minecraft ";
        public const string StartInstallPart = " -> ";
        public const string RunFail = "Cannot determine which Java Minecraft ";
        public const string RunFail2 = " needs; wizard aborted (nothing was changed).";
        public const string RunWizLog = "Note: 26.x is Mojang's new year-based versioning; its official Java requirement may be higher than the Java 21 estimate used here - watch the server log after install.";
        public const string RunWizStep = "Step 1: preparing Java ";
        public const string RunWizLog2 = "The configured Java is major version ";
        public const string RunWizLog3 = " and meets the requirement; using it: ";
        public const string RunWizLog4 = "Using the previously downloaded Java next to the app: ";
        public const string RunFail3 = " needs Java ";
        public const string RunFail4 = ", but this wizard can only auto-download Java 17 / 21. Install Java ";
        public const string RunFail5 = " manually, point Settings at its java.exe, then start the server.";
        public const string RunWizLog5 = "The Adoptium assets API gave no direct link (";
        public const string RunStr = "no detail";
        public const string RunPart = "); falling back to the official binary endpoint, which publishes no checksum - this Java download will not be hash-verified.";
        public const string RunWizLog6 = "Downloading Java ";
        public const string RunWizLog7 = " (Eclipse Temurin JRE, about 40-50 MB)...";
        public const string RunFail6 = "Java download failed: ";
        public const string RunWizLog8 = "Verifying the Java zip SHA256...";
        public const string RunFail7 = "Java zip SHA256 mismatch; the download was deleted. Expected ";
        public const string RunFail8 = ", got ";
        public const string RunFail9 = ". Check your network and retry.";
        public const string RunWizLog9 = "SHA256 verified.";
        public const string RunWizLog10 = "Extracting Java to ";
        public const string RunWizLog11 = " ...";
        public const string RunFail10 = "Java extraction failed: ";
        public const string RunFail11 = "Extraction finished but no bin\\java.exe was found - unexpected zip layout; aborted (config not saved).";
        public const string RunFail12 = "The downloaded Java reports major version ";
        public const string RunFail13 = ", still below the required ";
        public const string RunFail14 = "; aborted (config not saved).";
        public const string RunWizLog12 = "Java ready: major version ";
        public const string RunWizLog13 = ", ";
        public const string RunWizStep2 = "Step 2: downloading the server jar";
        public const string RunFail15 = "Cannot fetch the Mojang version manifest: ";
        public const string RunFail16 = "Minecraft ";
        public const string RunFail17 = " is not in the version manifest (check the number; releases only).";
        public const string RunFail18 = "Cannot fetch the version metadata for ";
        public const string RunFail19 = ": ";
        public const string RunFail20 = "The version metadata has no server download.";
        public const string RunWizLog14 = "Downloading the vanilla server (official Mojang)...";
        public const string RunDownloadWithProgress = "vanilla server";
        public const string RunFail21 = "Server download failed: ";
        public const string RunWizLog15 = "Verifying the server SHA1...";
        public const string RunFail22 = "Server SHA1 mismatch; the jar was deleted. Expected ";
        public const string RunWizLog16 = "SHA1 verified.";
        public const string RunFail23 = "Cannot fetch the Fabric loader list: ";
        public const string RunFail24 = "Fabric has no stable loader for Minecraft ";
        public const string RunFail25 = " yet; pick another version.";
        public const string RunFail26 = "Cannot fetch the Fabric installer list: ";
        public const string RunFail27 = "The Fabric installer list looks wrong.";
        public const string RunWizLog17 = "Downloading the Fabric server launch jar (loader ";
        public const string RunWizLog18 = ", installer ";
        public const string RunWizLog19 = ")...";
        public const string RunWizLog20 = "Note: Fabric publishes no hash for this jar, so it is protected by HTTPS only and is not hash-verified. On first start it downloads the vanilla server and libraries itself.";
        public const string RunDownloadWithProgress2 = "Fabric server";
        public const string RunFail28 = "Fabric server download failed: ";
        public const string RunWizLog21 = "Created the mods folder. Reminder: Fabric servers usually also need the Fabric API mod in mods, and every player needs the same both-sides mods client-side.";
        public const string RunFail29 = "Cannot fetch the Paper build list (Fill API): ";
        public const string RunFail30 = ". Note: versions without Paper builds return an error here.";
        public const string RunFail31 = "No downloadable server found in the Paper build list.";
        public const string RunWizLog22 = "Downloading the Paper server (latest build)...";
        public const string RunDownloadWithProgress3 = "Paper server";
        public const string RunFail32 = "Paper server download failed: ";
        public const string RunWizLog23 = "Verifying the server SHA256...";
        public const string RunFail33 = "Server SHA256 mismatch; the jar was deleted. Expected ";
        public const string RunWizStep3 = "Step 3: writing starter files";
        public const string RunWizLog24 = "Wrote eula.txt (eula=true, as you agreed).";
        public const string RunWizLog25 = "EULA not agreed: eula.txt was not written. On first start the server will generate eula=false and exit; set it to eula=true to start.";
        public const string RunWizLog26 = "Wrote server.properties: server-port=25565, online-mode=false (required for offline multiplayer).";
        public const string RunWizLog27 = "A server.properties already exists and was left untouched. Make sure online-mode=false yourself, or offline accounts cannot join.";
        public const string RunWizStep4 = "Step 4: saving settings";
        public const string RunWizLog28 = "Done. Server folder: ";
        public const string RunWizLog29 = "; jar: server.jar; Java: ";
        public const string RunStr2 = "(previous setting kept)";
        public const string RunWizLog30 = "Close the wizard, fill in the SakuraFrp access key and tunnel IDs in Settings, then click Start Multiplayer.";
        public const string RunWizStep5 = "Install complete";
        public const string RunFail34 = "Wizard error: ";
        public const string DownloadWithProgressWizStep = "Downloading ";
        public const string DownloadWithProgressWizStep2 = "... ";
        public const string FailWizLog = "Failed: ";
        public const string FailWizStep = "Stopped";
        public const string CancelledWizLog = "Cancelled. Settings were not saved; fully downloaded files stay in the target folder, partial files were cleaned up.";
        public const string CancelledWizStep = "Cancelled";
        public const string MainFormAppendServerLog = "[Launcher] Started. Logs are also saved in the logs folder next to the program (kept for 14 days).";
        public const string TrayText = "Minecraft Multiplayer Helper · Stopped";
        public const string InitTray613ToolStripMenuItem = "Show main window";
        public const string InitTray613ToolStripMenuItem2 = "Safe exit";
        public const string BtnStartText2 = "Start Multiplayer";
        public const string AddrLblCaption = "Public address (send to friends): ";
        public const string Hint = "The public address will appear here once the tunnel starts";
        public const string BtnCopyText = "Copy address for friends";
        public const string BtnPcl2Text = "Open PCL2";
        public const string BtnStopText = "Stop server safely";
        public const string LogToggleLblCaption = "▾  Log";
        public const string BuildUiStr = "Server";
        public const string BuildUiStr2 = "Tunnel";
        public const string BuildUiStr3 = "Status";
        public const string BuildUiStr4 = "Backups";
        public const string BuildUiMakeLogBox = "No logs yet · Server output will appear here";
        public const string BuildUiMakeLogBox2 = "No logs yet · Tunnel output will appear here";
        public const string BtnJumpBottomText = "↓ Back to bottom";
        public const string Hint2 = "Search logs…";
        public const string BuildUiSetToolTip = "Previous";
        public const string BuildUiSetToolTip2 = "Next";
        public const string BuildUiSetToolTip3 = "Search the log; press Enter to jump to the next match";
        public const string ChkErrOnlyText = "Warnings/errors only";
        public const string CmdLblCaption = "Server command";
        public const string BtnCmdSendText = "Send";
        public const string BuildUiAppendState = "First run: fill in the SakuraFrp access key and tunnel ID in Settings, and check the PCL2 path.";
        public const string IsErrLineContains = "[Watchdog]";
        public const string IsErrLineContains2 = "[Backup] Failed";
        public const string LblFindCaption = "No results";
        public const string SrvState2 = "Running · ";
        public const string SampleStatsPart = " player(s)";
        public const string SampleStatsPart2 = " player(s) online (list not public)";
        public const string SampleStatsPart3 = "Ping ";
        public const string ToggleLogStr = "▸  Log";
        public const string AppendServerLogPersistLog613 = "Server";
        public const string AppendTunnelLogPersistLog613 = "Tunnel";
        public const string SendServerCommandAppendState = "The server is not running; the command cannot be sent.";
        public const string SendServerCommandAppendState2 = "Failed to send command: the server process is gone.";
        public const string SendServerCommandAppendState3 = "Failed to send command: ";
        public const string OpenPlayerManagerShow = "The server is not running; player management is available after the server starts.";
        public const string OpenPlayerManagerAppendState = "Command failed: the server process is gone.";
        public const string DoBackupAppendState = "A backup is already in progress.";
        public const string BackupCoreAppendState = "World folder not found: ";
        public const string BackupCoreAppendState2 = ", cannot back up.";
        public const string BackupCoreAppendServerLog = "[Backup] Server auto-save paused and flushed; packaging the world…";
        public const string BackupCoreAppendServerLog2 = "[Backup] Done: ";
        public const string BackupCorePart = " files";
        public const string BackupCoreStr = ", skipped ";
        public const string BackupCorePart2 = " file(s) in use";
        public const string BackupCoreAppendState3 = "World backup complete (";
        public const string BackupCoreAppendServerLog3 = "[Backup] Failed: ";
        public const string BackupCoreAppendState4 = "Backup failed: ";
        public const string BackupCoreAppendServerLog4 = "[Backup] Server auto-save resumed.";
        public const string RestoreBackupShow = "You must stop the server before restoring: swapping world files while the server runs will corrupt the save.\n\nClick \"Stop server safely\" first, then restore.";
        public const string RestoreBackupShow2 = "Restore Backup";
        public const string RestoreBackupConfirmBox = "Restore the world to backup \"";
        public const string RestoreBackupConfirmBox2 = "\"?\n\nThe current world will not be deleted: it is renamed and set aside first (world_prerestore_<time>); delete it yourself once everything checks out. If the restore was wrong, you can swap it back.";
        public const string RestoreBackupAppendState = "A backup task is already in progress.";
        public const string Result = "Restored to backup \"";
        public const string RestoreBackupPart = "\". The old world was kept at ";
        public const string RestoreBackupPart2 = "; once the new world checks out, you can delete it.";
        public const string Result2 = "Restore failed: ";
        public const string RestoreBackupPart3 = "(The old world may already have been renamed. Do not start the server yet; check the server folder.)";
        public const string RestoreBackupAppendServerLog = "[Backup] ";
        public const string BuildBackupPanelBkLabel = "World backups";
        public const string BtnBackupNowText = "Back up now";
        public const string BuildBackupPanelDoBackup = "Manual backup";
        public const string BtnRestoreBackupText = "Restore selected";
        public const string BuildBackupPanelShow = "Select a backup from the list above first.";
        public const string BtnDeleteBackupText = "Delete selected";
        public const string BuildBackupPanelShow2 = "Delete Backup";
        public const string BuildBackupPanelConfirmBox = "Delete backup \"";
        public const string BuildBackupPanelPart = "\"? It cannot be recovered after deletion.";
        public const string BuildBackupPanelShow3 = "Delete failed: ";
        public const string BuildBackupPanelBkLabel2 = "Keep the latest ";
        public const string BuildBackupPanelBkLabel3 = " backups";
        public const string BuildBackupPanelBkCheck = "Auto-restart on crash";
        public const string BuildBackupPanelBkCheck2 = "Auto-backup before server start";
        public const string BuildBackupPanelBkCheck3 = "Daily auto-backup";
        public const string BuildBackupPanelBkLabel4 = "While the app is open, back up automatically at the set time";
        public const string BuildBackupPanelBkCheck4 = "Scheduled command";
        public const string LblBackupStateCaption = "Backing up… (large worlds may take a minute or two)";
        public const string RefreshBackupListStr = "Total ";
        public const string RefreshBackupListPart = " backups · ";
        public const string RefreshBackupListStr2 = "No backups yet";
        public const string RefreshBackupListStr3 = "Latest: ";
        public const string RefreshBackupListStr4 = "Click \"Back up now\" to snapshot the world.";
        public const string CheckSchedulesAppendServerLog = "[Schedule] Daily backup time reached; starting auto-backup.";
        public const string CheckSchedulesAppendServerLog2 = "[Schedule] A scheduled command is due, but ";
        public const string CheckSchedulesAppendServerLog3 = " player(s) are still online; postponing until the server is empty.";
        public const string CheckSchedulesAppendServerLog4 = "[Schedule] Sent scheduled command: ";
        public const string ServerText2 = "Server: Running";
        public const string ServerText3 = "Server: Starting…";
        public const string TunnelText2 = "SakuraFrp tunnel: Connected";
        public const string TunnelText3 = "SakuraFrp tunnel: Starting…";
        public const string AddrText2 = "Public address: Ready";
        public const string AddrText3 = "Public address: Saved (tunnel not running)";
        public const string Tt = "Minecraft Multiplayer Helper · ";
        public const string RefreshStatusStr = "Running";
        public const string RefreshStatusStr2 = "Starting";
        public const string CopyAddrShow = "No public address yet. Start multiplayer first.";
        public const string CopyAddrShow2 = "Notice";
        public const string CopyAddrAppendState = "Public address copied to the clipboard; send it to your friends.";
        public const string BtnCopyText2 = "Copied ✓";
        public const string OpenPcl2Show = "PCL2 not found. Set PCL2's exe path in Settings.";
        public const string OpenPcl2Show2 = "Failed to open PCL2: ";
        public const string HandleServerCrashAppendServerLog = "[Watchdog] The server exited unexpectedly (exit code ";
        public const string HandleServerCrashAppendServerLog2 = "); this was not a normal shutdown.";
        public const string HandleServerCrashStr = "Possible cause: ";
        public const string HandleServerCrashAppendState = "The server exited unexpectedly";
        public const string HandleServerCrashStr2 = ", possible cause: ";
        public const string HandleServerCrashStr3 = ". Check the bottom of the server log for the reason.";
        public const string HandleServerCrashPart = "(Watchdog auto-restart is off)";
        public const string HandleServerCrashAppendServerLog3 = "[Watchdog] Crashed ";
        public const string HandleServerCrashAppendServerLog4 = " times within 10 minutes; auto-restart stopped. Most likely a save or mod problem.";
        public const string HandleServerCrashAppendState2 = "The server kept crashing; auto-restart has stopped. Check the log.";
        public const string HandleServerCrashStr4 = " Possible cause: ";
        public const string HandleServerCrashAppendState3 = "The server exited unexpectedly; the watchdog will restart it in 5 seconds…";
        public const string GuessCrash614Ret = "Port ";
        public const string GuessCrash614Part = " is already in use — probably another server instance was not shut down cleanly";
        public const string GuessCrash614Ret2 = "Out of memory, or a bad memory setting (-Xmx)";
        public const string GuessCrash614Ret3 = "Wrong Java version — this server needs Java 17";
        public const string GuessCrash614Ret4 = "The server froze for over 60 seconds and was stopped by the game's built-in watchdog";
        public const string GuessCrash614Ret5 = "A mod failed to load; find the mod named in the log errors";
        public const string GuessCrash614Ret6 = "The EULA has not been accepted";
        public const string OfferWizardIfNeededShow = "There is no usable server yet: the configured server folder does not exist, or it contains no server jar.\n\nThe first-run setup wizard can download a matching Java and server (Vanilla / Fabric / Paper) and write the EULA and the server.properties needed for offline play. All downloads are hash-verified.";
        public const string OfferWizardIfNeededShow2 = "Open wizard";
        public const string OfferWizardIfNeededShow3 = "Later";
        public const string OpenWizardAppendState = "First-run wizard finished: server and Java are ready.";
        public const string CheckJavaVersionStr = "unknown";
        public const string CheckJavaVersionAppendServerLog = "[Java check] Cannot read the server jar's version info (version.json); skipping the version check. Current Java major version: ";
        public const string CheckJavaVersionAppendServerLog2 = ".";
        public const string CheckJavaVersionAppendServerLog3 = "[Java check] Server version ";
        public const string CheckJavaVersionAppendServerLog4 = " is not in the version table; skipping the version check. Current Java major version: ";
        public const string CheckJavaVersionAppendServerLog5 = "[Java check] Server ";
        public const string CheckJavaVersionAppendServerLog6 = ", but the current Java version could not be measured; starting anyway (watch the server log).";
        public const string CheckJavaVersionAppendServerLog7 = "[Java check] Passed: server ";
        public const string CheckJavaVersionAppendServerLog8 = ", current Java is ";
        public const string CheckJavaVersionAppendServerLog9 = "[Java check] Mismatch: server ";
        public const string CheckJavaVersionAppendServerLog10 = ", current Java is only ";
        public const string CheckJavaVersionAppendServerLog11 = "; waiting for the user's decision.";
        public const string Msg2 = "The Java version is too old - the server will most likely fail to start.\n\nServer version: ";
        public const string CheckJavaVersionPart = ", which needs Java ";
        public const string CheckJavaVersionPart2 = " or newer. The Java about to be used is major version ";
        public const string CheckJavaVersionPart3 = ".\n\nSuggestion: pick a Java ";
        public const string CheckJavaVersionPart4 = " java.exe in Settings, or let the first-run setup wizard download a matching Java automatically.\nChoosing Start anyway launches with the current Java regardless; see the server log for the resulting errors.";
        public const string CheckJavaVersionShow = "Java version mismatch";
        public const string CheckJavaVersionShow2 = "Start anyway";
        public const string CheckJavaVersionShow3 = "Cancel start";
        public const string CheckJavaVersionAppendServerLog12 = "[Java check] The user chose to start anyway.";
        public const string CheckJavaVersionAppendServerLog13 = "[Java check] The user cancelled the start.";
        public const string StartAllAppendState = "The server is already running; no need to start it again.";
        public const string BtnStartText3 = "Multiplayer running";
        public const string BtnStartText4 = "Starting…";
        public const string DoStartFailState = "Server folder does not exist: ";
        public const string DoStartFailState2 = ". Check the server folder in Settings.";
        public const string DoStartFailState3 = "Server jar not found: ";
        public const string DoStartFailState4 = ". Check the jar file name in Settings.";
        public const string DoStartFailState5 = "Java (java.exe) not found. Re-select the Java path in Settings — usually under .minecraft\\runtime or in the Java install folder.";
        public const string DoStartFailState6 = "Start cancelled: the Java version is below what the server needs. Switch to a matching Java, or let the first-run setup wizard download one, then try again.";
        public const string DoStartFailState7 = " is already in use. The last server may not have shut down cleanly — click \"Stop server safely\" or restart this app; do not kill Java processes in Task Manager yourself.";
        public const string DoStartAppendState = "Auto-backing up before server start…";
        public const string DoStartAppendState2 = "Starting the Minecraft server…";
        public const string DoStartAppendServerLog = "[Process exited]";
        public const string DoStartFailState8 = "Cannot start the Java process. Check that the Java path is correct.";
        public const string DoStartAppendState3 = "Waiting for the server to be ready (watching for \"Done\")…";
        public const string DoStartFailState9 = "The server process exited before \"Done\" appeared. Most likely: 1) wrong Java path or memory arguments; 2) an error in the save/world or mods (check the bottom of the server log). Do not touch world or mods.";
        public const string DoStartFailState10 = "No \"Done\" after 90 seconds. Possibly not enough memory, an oversized world, or a stuck start. Check the bottom of the server log; do not touch world or mods.";
        public const string DoStartAppendState4 = "Ready. Click \"Copy address for friends\" and send the address to your friends.";
        public const string DoStartFailState11 = "Start failed: ";
        public const string StartTunnelAppendTunnelLog = "[Fallback] Opening the official SakuraFrp launcher — click Start Tunnel in it.";
        public const string StartTunnelAppendTunnelLog2 = "Failed to open the SakuraFrp launcher: ";
        public const string StartTunnelAppendTunnelLog3 = "frpc.exe not found: ";
        public const string StartTunnelAppendTunnelLog4 = ". Check it in Settings, or switch the tunnel mode to the official launcher.";
        public const string StartTunnelAppendTunnelLog5 = "The access key is empty. Fill in the SakuraFrp access key in Settings.";
        public const string StartTunnelAppendTunnelLog6 = "The tunnel ID is empty. Fill it in in Settings.";
        public const string StartTunnelAppendState = "Starting the SakuraFrp tunnel…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc exited]";
        public const string StartTunnelAppendTunnelLog8 = "[Watchdog] The game tunnel exited unexpectedly; reconnecting in 5 seconds.";
        public const string StartTunnelAppendTunnelLog9 = "frpc failed to start. Check the frpc.exe path.";
        public const string StartTunnelAppendTunnelLog10 = "The current frpc.exe is the bundled build that ships with the SakuraFrp launcher. That build cannot be run directly by external programs (only via the SakuraFrp Service), so it exits immediately. This has nothing to do with your access key, tunnel ID, or node. In Settings, point the frpc path at a standalone frpc, or switch the tunnel mode to the official launcher.";
        public const string StartTunnelAppendTunnelLog11 = "The frpc process exited. Possibly: 1) wrong access key or tunnel ID; 2) a node problem; 3) the account needs verification or check-in. Check the key and tunnel ID in Settings, or switch the tunnel mode to the official launcher.";
        public const string StartTunnelAppendState2 = "This frpc is the launcher's bundled build and cannot run directly. In Settings, switch to a standalone frpc or the official launcher mode.";
        public const string StartVoiceTunnelAppendTunnelLog = "[Voice] No voice tunnel ID set; skipping the voice tunnel.";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[Voice frpc exited]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[Watchdog] The voice tunnel exited unexpectedly; reconnecting in 5 seconds.";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[Voice] frpc failed to start. Check the frpc.exe path.";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[Voice] The frpc process exited. Check that the voice tunnel ID is correct.";
        public const string StartVoiceTunnelAppendState = "The voice tunnel is connected.";
        public const string VoiceFrpcOutputAppendTunnelLog = "[Voice] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[Voice][ERR] ";
        public const string TryParseFrpcAppendState = "Public address obtained automatically: ";
        public const string ServerText4 = "Server: Error";
        public const string TunnelText4 = "SakuraFrp tunnel: Error";
        public const string SafeStopAppendState = "The server is not running; the tunnel has been stopped.";
        public const string SafeStopAppendState2 = "Stopping safely: sending stop to the server and waiting for the save…";
        public const string SafeStopAppendServerLog = ">> stop sent; waiting for the save and exit (up to 60 seconds)";
        public const string SafeStopAppendState3 = "The server stopped normally (save complete). Closing the tunnel…";
        public const string SafeStopAppendServerLog2 = "[Server process exited normally]";
        public const string SafeStopShow = "The server still has not exited after 60 seconds. Force-ending it may lose the last few minutes of progress. Force end it?";
        public const string SafeStopShow2 = "Safe shutdown timed out";
        public const string SafeStopShow3 = "Force end";
        public const string SafeStopShow4 = "Wait longer";
        public const string SafeStopAppendState4 = "The server process was force-ended (recent progress may be lost).";
        public const string SafeStopAppendState5 = "Force end cancelled; the server is still running.";
        public const string SafeStopAppendState6 = "All stopped: the server and the tunnel are closed.";
        public const string OnClosingConfirmBox = "The server is still running. Closing directly may skip saving — stop it safely first?";
        public const string BtnSparkTpsText = "TPS Report";
        public const string BtnSparkProfileText = "Profile 30s";
        public const string SparkLblCaption = "Diagnostics";
        public const string SparkProfileStarted = "[Diagnostics] Started 30s profiling; keep the server running...";
        public const string SparkProfileStopped = "[Diagnostics] Profiling finished; the spark report link appears in the server log above.";
        public const string SparkProfileCancelled = "[Diagnostics] The server stopped; this profiling run was cancelled.";
        public const string LangCaption = "Language";
        public const string LangRestartHint = "Language saved. Restart the app to apply.";
        public const string LangRestartNow = "Restart now";
        public const int EulaAgreeWidth = 230;
        public const int EulaLinkDx = 232;
    }

    static class TxtCn
    {
        public const string Msg = "出了点小问题";
        public const string ShowFriendlyErrorStr = "，程序需要关闭。";
        public const string ShowFriendlyErrorStr2 = "，程序会继续运行。";
        public const string ShowFriendlyErrorPart = "\n\n如果反复出现，把这段信息告诉奶蛙就行。";
        public const string ShowFriendlyErrorShow = "Minecraft 联机助手";
        public const string YesText = "先安全关服再退出";
        public const string NoText = "取消";
        public const string SettingsFormInitChrome = "设置";
        public const string SettingsFormAddField = "服务端目录 D:\\MCServer";
        public const string SettingsFormAddField2 = "Java 完整路径 (java.exe)";
        public const string SettingsFormAddField3 = "jar 文件名";
        public const string SettingsFormAddField4 = "内存参数";
        public const string SettingsFormAddField5 = "端口";
        public const string SettingsFormAddField6 = "frpc.exe 路径";
        public const string LbModeCaption = "隧道方式";
        public const string SettingsFormAdd = "frpc 命令行（优先）";
        public const string SettingsFormAdd2 = "打开官方 SakuraFrp 启动器（备用）";
        public const string LbKeyCaption = "SakuraFrp 访问密钥";
        public const string SettingsFormAddField7 = "隧道 ID";
        public const string SettingsFormAddField8 = "语音隧道 ID（可空）";
        public const string SettingsFormAddField9 = "公网地址（可空）";
        public const string SettingsFormAddField10 = "PCL2 exe 路径";
        public const string BtnPropsText = "服务器属性…";
        public const string SettingsFormShow = "先把上面的服务端目录填对，再来改服务器属性。";
        public const string SettingsFormShow2 = "服务器属性";
        public const string BtnWizardText = "首次设置向导";
        public const string BtnSaveText = "保存";
        public const string ValidateSettingsMarkInvalid = "服务端目录不存在，请检查路径。";
        public const string ValidateSettingsMarkInvalid2 = "Java 路径不存在，请检查。";
        public const string ValidateSettingsMarkInvalid3 = "jar 文件名不能为空。";
        public const string ValidateSettingsMarkInvalid4 = "服务端目录里找不到这个 jar 文件。";
        public const string ValidateSettingsMarkInvalid5 = "端口必须是 1-65535 的数字。";
        public const string ValidateSettingsMarkInvalid6 = "frpc.exe 路径不存在，请检查。";
        public const string ValidateSettingsMarkInvalid7 = "frpc 模式下隧道 ID 不能为空。";
        public const string ValidateSettingsMarkInvalid8 = "PCL2 路径不存在，请检查。";
        public const string TitleText = "Minecraft 联机助手 v6.22.5";
        public const string SubText = "双隧道版 · 游戏 + 语音";
        public const string ServerText = "服务端：未启动";
        public const string TunnelText = "SakuraFrp 隧道：未启动";
        public const string AddrText = "公网地址：未生成";
        public const string SrvState = "未启动";
        public const string MbText = "玩家管理";
        public const string OnPaintHeader = "服务端进程";
        public const string OnPaintRow = "状态";
        public const string OnPaintRow2 = "内存占用";
        public const string OnPaintRow3 = "玩家连接";
        public const string OnPaintDrawText = "在线玩家";
        public const string OnPaintRow4 = "版本 · 延迟";
        public const string OnPaintHeader2 = "隧道进程（frpc）";
        public const string OnPaintHeader3 = "本机";
        public const string OnPaintDrawText2 = "下行";
        public const string OnPaintDrawText3 = "上行";
        public const string OnPaintRow5 = "系统内存";
        public const string OnHandleCreatedIndexOf = "错误";
        public const string OnHandleCreatedStr = "出错";
        public const string ShowMsg = "确定";
        public const string PropsFormAddCombo = "难度";
        public const string PropsFormStr = "和平";
        public const string PropsFormStr2 = "简单";
        public const string PropsFormStr3 = "普通";
        public const string PropsFormStr4 = "困难";
        public const string PropsFormAddCombo2 = "游戏模式";
        public const string PropsFormStr5 = "生存";
        public const string PropsFormStr6 = "创造";
        public const string PropsFormStr7 = "冒险";
        public const string PropsFormStr8 = "旁观";
        public const string PropsFormAddNum = "最大玩家数";
        public const string PropsFormAddNum2 = "视距（区块）";
        public const string PropsFormAddNum3 = "模拟距离（区块）";
        public const string PropsFormAddNum4 = "出生点保护半径";
        public const string PropsFormAddText = "服务器标语 MOTD";
        public const string PropsFormAddCheck = "启用白名单（white-list）";
        public const string PropsFormAddCheck2 = "允许玩家互相攻击（pvp）";
        public const string PropsFormAddCheck3 = "允许飞行（allow-flight）";
        public const string LbOnCaption = "正版验证：关闭（离线联机必须保持关闭，这里不能改）";
        public const string PropsFormShow = "在服务端目录里找不到 server.properties。服务端至少成功启动过一次才会生成这个文件。";
        public const string SavePropsShow = "最大玩家数要是 1-9999 的数字。";
        public const string SavePropsShow2 = "视距要是 2-32 的数字。";
        public const string SavePropsShow3 = "模拟距离要是 2-32 的数字。";
        public const string SavePropsShow4 = "出生点保护半径要是 0-9999 的数字。";
        public const string SavePropsShow5 = "保存失败：";
        public const string SavePropsShow6 = "已保存。服务端正在运行，这些设置会在下次开服时生效。";
        public const string PlayerFormStr = "在线玩家（";
        public const string PlayerFormStr2 = "现在没有玩家在线";
        public const string PlayerFormAddBtn = "传送到我身边";
        public const string PlayerFormAddBtn2 = "踢出服务器";
        public const string PlayerFormConfirmDo = "把 ";
        public const string PlayerFormConfirmDo2 = " 踢出服务器？";
        public const string PlayerFormAddBtn3 = "给 OP";
        public const string PlayerFormAddBtn4 = "取消 OP";
        public const string PlayerFormAddBtn5 = "封禁";
        public const string PlayerFormConfirmDo3 = "封禁 ";
        public const string PlayerFormConfirmDo4 = "？封禁后他进不了服务器（可解封）。";
        public const string PlayerFormAddBtn6 = "解封";
        public const string PlayerFormStr3 = "提示：你自己不在线时，「传送到我身边」不可用。";
        public const string PlayerFormStr4 = "动作会以控制台指令发给服务端，和在指令框输入一样。";
        public const string CloseText = "关闭";
        public const string LbOpsCaption = "OP 名单：";
        public const string RefreshListsStr = "（无）";
        public const string LbBannedCaption = "封禁名单：";
        public const string WizardFormInitChrome = "首次设置向导";
        public const string WizardFormAddLabel = "Minecraft 版本";
        public const string WizardFormAddHint = "可手填任意正式版版本号";
        public const string WizardFormAddLabel2 = "服务端类型";
        public const string WizardFormAddRadio = "原版";
        public const string WizardFormAddLabel3 = "服务端目录";
        public const string BtnBrowseText = "浏览…";
        public const string Description = "选择服务端文件夹（不存在也可以，先选一个父目录再改路径文字）";
        public const string ChkEulaText = "我同意 Minecraft EULA";
        public const string LnkEulaText = "查看 EULA 全文";
        public const string WizardFormAddHint2 = "不勾选也能装，但首次开服前要自己把 eula.txt 改成 eula=true";
        public const string LblStepCaption = "填好上面三项，点「开始安装」。Java 和服务端都会自动下好并校验。";
        public const string BtnStartText = "开始安装";
        public const string WizardFormWizLog = "正在取消…";
        public const string StartInstallShow = "先填 Minecraft 版本。";
        public const string StartInstallShow2 = "版本号格式不对，请填正式版版本号，例如 1.20.1（快照版暂不支持向导）。";
        public const string StartInstallShow3 = "先填服务端目录。";
        public const string StartInstallShow4 = "目标目录里已经有 server.jar，继续会覆盖它。\n\n存档和 mods 不受影响（向导只换 jar 并补缺失文件）。确定继续？";
        public const string StartInstallShow5 = "覆盖并继续";
        public const string StartInstallWizLog = "—— 开始安装：Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "无法判断 Minecraft ";
        public const string RunFail2 = " 需要的 Java 版本，向导中止（没有改动任何文件）。";
        public const string RunWizLog = "提示：26.x 是 Mojang 的新年份版本号，官方要求的 Java 可能高于本向导按 21 的估算，装好后请留意服务端日志。";
        public const string RunWizStep = "第 1 步：准备 Java ";
        public const string RunWizLog2 = "已配置的 Java 大版本为 ";
        public const string RunWizLog3 = "，满足要求，直接使用：";
        public const string RunWizLog4 = "使用程序旁已下载的 Java：";
        public const string RunFail3 = " 需要 Java ";
        public const string RunFail4 = "，本向导只能自动下载 Java 17 / 21。请先手动安装 Java ";
        public const string RunFail5 = "，在「设置」里指定 java.exe 路径后再开服。";
        public const string RunWizLog5 = "Adoptium 资产接口没取到直链（";
        public const string RunStr = "无详情";
        public const string RunPart = "），改用官方二进制接口下载；该接口不提供哈希，本次 Java 下载不做哈希校验。";
        public const string RunWizLog6 = "下载 Java ";
        public const string RunWizLog7 = "（Eclipse Temurin JRE，约 40–50 MB）…";
        public const string RunFail6 = "Java 下载失败：";
        public const string RunWizLog8 = "校验 Java 压缩包 SHA256…";
        public const string RunFail7 = "Java 压缩包 SHA256 校验不通过，已删除下载文件。期望 ";
        public const string RunFail8 = "，实际 ";
        public const string RunFail9 = "。请检查网络后重试。";
        public const string RunWizLog9 = "SHA256 校验通过。";
        public const string RunWizLog10 = "解压 Java 到 ";
        public const string RunWizLog11 = " …";
        public const string RunFail10 = "Java 解压失败：";
        public const string RunFail11 = "Java 解压完成但没找到 bin\\java.exe，目录结构异常，已中止（未写入配置）。";
        public const string RunFail12 = "下载到的 Java 大版本是 ";
        public const string RunFail13 = "，仍低于需要的 ";
        public const string RunFail14 = "，已中止（未写入配置）。";
        public const string RunWizLog12 = "Java 就绪：大版本 ";
        public const string RunWizLog13 = "，";
        public const string RunWizStep2 = "第 2 步：下载服务端 jar";
        public const string RunFail15 = "拿不到 Mojang 版本清单：";
        public const string RunFail16 = "版本清单里没有 Minecraft ";
        public const string RunFail17 = "（请确认版本号无误、且是正式版）。";
        public const string RunFail18 = "拿不到 ";
        public const string RunFail19 = " 的版本元数据：";
        public const string RunFail20 = "版本元数据里没有服务端下载地址。";
        public const string RunWizLog14 = "下载原版服务端（Mojang 官方）…";
        public const string RunDownloadWithProgress = "原版服务端";
        public const string RunFail21 = "服务端下载失败：";
        public const string RunWizLog15 = "校验服务端 SHA1…";
        public const string RunFail22 = "服务端 SHA1 校验不通过，已删除 jar。期望 ";
        public const string RunWizLog16 = "SHA1 校验通过。";
        public const string RunFail23 = "拿不到 Fabric Loader 列表：";
        public const string RunFail24 = "Fabric 还没有适配 Minecraft ";
        public const string RunFail25 = " 的稳定版 Loader，请换一个版本。";
        public const string RunFail26 = "拿不到 Fabric Installer 列表：";
        public const string RunFail27 = "Fabric Installer 列表异常。";
        public const string RunWizLog17 = "下载 Fabric 服务端启动 jar（Loader ";
        public const string RunWizLog18 = "，Installer ";
        public const string RunWizLog19 = "）…";
        public const string RunWizLog20 = "注意：Fabric 官方不发布该 jar 的哈希校验值，本文件只能靠 HTTPS 传输，未做哈希校验。首次开服时它还会再下载原版服务端与库文件。";
        public const string RunDownloadWithProgress2 = "Fabric 服务端";
        public const string RunFail28 = "Fabric 服务端下载失败：";
        public const string RunWizLog21 = "已建 mods 文件夹。提醒：Fabric 服务端要装 mod 还需要 Fabric API（放进 mods），语音等双端 mod 每个玩家客户端也要装。";
        public const string RunFail29 = "拿不到 Paper 构建列表（Fill API）：";
        public const string RunFail30 = "。注意：Paper 不支持的版本会返回错误，请确认该版本有 Paper。";
        public const string RunFail31 = "Paper 构建列表里没找到可下载的服务端。";
        public const string RunWizLog22 = "下载 Paper 服务端（最新构建）…";
        public const string RunDownloadWithProgress3 = "Paper 服务端";
        public const string RunFail32 = "Paper 服务端下载失败：";
        public const string RunWizLog23 = "校验服务端 SHA256…";
        public const string RunFail33 = "服务端 SHA256 校验不通过，已删除 jar。期望 ";
        public const string RunWizStep3 = "第 3 步：写入初始文件";
        public const string RunWizLog24 = "已写入 eula.txt（eula=true，按你勾选的同意）。";
        public const string RunWizLog25 = "未勾选同意 EULA：没有写 eula.txt。首次开服时服务端会生成 eula=false 并自动退出，把它改成 eula=true 后再开服。";
        public const string RunWizLog26 = "已写入 server.properties：server-port=25565，online-mode=false（离线联机必须关正版验证）。";
        public const string RunWizLog27 = "目录里已有 server.properties，未改动它。请自行确认 online-mode=false，否则离线账号进不来。";
        public const string RunWizStep4 = "第 4 步：保存设置";
        public const string RunWizLog28 = "完成。服务端目录：";
        public const string RunWizLog29 = "；jar：server.jar；Java：";
        public const string RunStr2 = "（沿用原配置）";
        public const string RunWizLog30 = "关掉向导后，先到「设置」里填 SakuraFrp 访问密钥和隧道 ID，再点「启动联机」。";
        public const string RunWizStep5 = "安装完成";
        public const string RunFail34 = "向导出错：";
        public const string DownloadWithProgressWizStep = "下载";
        public const string DownloadWithProgressWizStep2 = "…";
        public const string FailWizLog = "失败：";
        public const string FailWizStep = "已停止";
        public const string CancelledWizLog = "已取消。未写入配置；已完整下载好的文件保留在目标目录，半成品已清理。";
        public const string CancelledWizStep = "已取消";
        public const string MainFormAppendServerLog = "[启动器] 已启动，日志同步保存在程序目录的 logs 文件夹中（保留 14 天）。";
        public const string TrayText = "Minecraft 联机助手 · 已停止";
        public const string InitTray613ToolStripMenuItem = "显示主窗口";
        public const string InitTray613ToolStripMenuItem2 = "安全退出";
        public const string BtnStartText2 = "启动联机";
        public const string AddrLblCaption = "公网地址（发给朋友）：";
        public const string Hint = "隧道启动后，公网地址会显示在这里";
        public const string BtnCopyText = "复制地址发给朋友";
        public const string BtnPcl2Text = "打开 PCL2";
        public const string BtnStopText = "安全关服";
        public const string LogToggleLblCaption = "▾  日志";
        public const string BuildUiStr = "服务端日志";
        public const string BuildUiStr2 = "隧道日志";
        public const string BuildUiStr3 = "状态监控";
        public const string BuildUiStr4 = "备份";
        public const string BuildUiMakeLogBox = "暂无日志 · 服务端输出会显示在这里";
        public const string BuildUiMakeLogBox2 = "暂无日志 · 隧道输出会显示在这里";
        public const string BtnJumpBottomText = "↓ 回到底部";
        public const string Hint2 = "搜索日志…";
        public const string BuildUiSetToolTip = "上一个";
        public const string BuildUiSetToolTip2 = "下一个";
        public const string BuildUiSetToolTip3 = "在日志里搜索，回车跳到下一个";
        public const string ChkErrOnlyText = "只看警告/错误";
        public const string CmdLblCaption = "服务器指令";
        public const string BtnCmdSendText = "发送";
        public const string BuildUiAppendState = "首次使用：请在「设置」里填 SakuraFrp 访问密钥、隧道 ID，检查 PCL2 路径。";
        public const string IsErrLineContains = "[守护]";
        public const string IsErrLineContains2 = "[备份] 失败";
        public const string LblFindCaption = "无结果";
        public const string SrvState2 = "运行中 · ";
        public const string SampleStatsPart = " 人";
        public const string SampleStatsPart2 = " 人在线（名单未公开）";
        public const string SampleStatsPart3 = "延迟 ";
        public const string ToggleLogStr = "▸  日志";
        public const string AppendServerLogPersistLog613 = "服务端";
        public const string AppendTunnelLogPersistLog613 = "隧道";
        public const string SendServerCommandAppendState = "服务端没在运行，指令发不出去。";
        public const string SendServerCommandAppendState2 = "发送指令失败：服务端进程已不在。";
        public const string SendServerCommandAppendState3 = "发送指令失败：";
        public const string OpenPlayerManagerShow = "服务端没在运行，玩家管理要在开服后使用。";
        public const string OpenPlayerManagerAppendState = "指令发送失败：服务端进程已不在。";
        public const string DoBackupAppendState = "已经有一个备份正在进行中了。";
        public const string BackupCoreAppendState = "找不到世界文件夹：";
        public const string BackupCoreAppendState2 = "，无法备份。";
        public const string BackupCoreAppendServerLog = "[备份] 已暂停服务端自动保存并落盘，开始打包世界…";
        public const string BackupCoreAppendServerLog2 = "[备份] 完成：";
        public const string BackupCorePart = " 个文件";
        public const string BackupCoreStr = "，跳过 ";
        public const string BackupCorePart2 = " 个占用中的文件";
        public const string BackupCoreAppendState3 = "世界备份完成（";
        public const string BackupCoreAppendServerLog3 = "[备份] 失败：";
        public const string BackupCoreAppendState4 = "备份失败：";
        public const string BackupCoreAppendServerLog4 = "[备份] 服务端自动保存已恢复。";
        public const string RestoreBackupShow = "恢复存档前必须先关服：服务端开着的时候换世界文件会损坏存档。\n\n请先点「安全关服」，再来恢复。";
        public const string RestoreBackupShow2 = "恢复存档";
        public const string RestoreBackupConfirmBox = "把世界恢复到备份「";
        public const string RestoreBackupConfirmBox2 = "」？\n\n当前的世界不会被删：它会先被改名留在一边（world_prerestore_时间），确认没问题后你可以自己删；恢复错了也能换回来。";
        public const string RestoreBackupAppendState = "已经有一个备份任务正在进行中了。";
        public const string Result = "已恢复到备份「";
        public const string RestoreBackupPart = "」。旧世界保留在 ";
        public const string RestoreBackupPart2 = "，确认新世界没问题后可以删除它。";
        public const string Result2 = "恢复失败：";
        public const string RestoreBackupPart3 = "（旧世界可能已被改名，先别开服，检查服务端目录。）";
        public const string RestoreBackupAppendServerLog = "[备份] ";
        public const string BuildBackupPanelBkLabel = "世界备份";
        public const string BtnBackupNowText = "立即备份";
        public const string BuildBackupPanelDoBackup = "手动备份";
        public const string BtnRestoreBackupText = "恢复选中";
        public const string BuildBackupPanelShow = "先在上面的列表里选中一个备份。";
        public const string BtnDeleteBackupText = "删除选中";
        public const string BuildBackupPanelShow2 = "删除备份";
        public const string BuildBackupPanelConfirmBox = "删除备份「";
        public const string BuildBackupPanelPart = "」？删了就恢复不了了。";
        public const string BuildBackupPanelShow3 = "删除失败：";
        public const string BuildBackupPanelBkLabel2 = "保留最近";
        public const string BuildBackupPanelBkLabel3 = "份";
        public const string BuildBackupPanelBkCheck = "崩溃自动重启";
        public const string BuildBackupPanelBkCheck2 = "开服前自动备份";
        public const string BuildBackupPanelBkCheck3 = "每日自动备份";
        public const string BuildBackupPanelBkLabel4 = "应用开着时，到点自动备份一次";
        public const string BuildBackupPanelBkCheck4 = "定时指令";
        public const string LblBackupStateCaption = "正在备份…（大世界可能要一两分钟）";
        public const string RefreshBackupListStr = "共 ";
        public const string RefreshBackupListPart = " 份 · ";
        public const string RefreshBackupListStr2 = "还没有备份";
        public const string RefreshBackupListStr3 = "最近一份：";
        public const string RefreshBackupListStr4 = "点「立即备份」给世界留一份快照。";
        public const string CheckSchedulesAppendServerLog = "[定时] 到达每日备份时间，开始自动备份。";
        public const string CheckSchedulesAppendServerLog2 = "[定时] 到了定时指令时间，但还有 ";
        public const string CheckSchedulesAppendServerLog3 = " 人在线，推迟到没人时再执行。";
        public const string CheckSchedulesAppendServerLog4 = "[定时] 已发送定时指令：";
        public const string ServerText2 = "服务端：运行中";
        public const string ServerText3 = "服务端：启动中…";
        public const string TunnelText2 = "SakuraFrp 隧道：已连接";
        public const string TunnelText3 = "SakuraFrp 隧道：启动中…";
        public const string AddrText2 = "公网地址：已就绪";
        public const string AddrText3 = "公网地址：已保存（隧道未启动）";
        public const string Tt = "Minecraft 联机助手 · ";
        public const string RefreshStatusStr = "运行中";
        public const string RefreshStatusStr2 = "启动中";
        public const string CopyAddrShow = "公网地址还没生成，请先启动联机。";
        public const string CopyAddrShow2 = "提示";
        public const string CopyAddrAppendState = "公网地址已复制到剪贴板，发给朋友即可。";
        public const string BtnCopyText2 = "已复制 ✓";
        public const string OpenPcl2Show = "找不到 PCL2，请到「设置」里填写 PCL2 的 exe 路径。";
        public const string OpenPcl2Show2 = "打开 PCL2 失败：";
        public const string HandleServerCrashAppendServerLog = "[守护] 服务端异常退出（退出码 ";
        public const string HandleServerCrashAppendServerLog2 = "），这不是正常关服。";
        public const string HandleServerCrashStr = "可能原因：";
        public const string HandleServerCrashAppendState = "服务端异常退出了";
        public const string HandleServerCrashStr2 = "，可能原因：";
        public const string HandleServerCrashStr3 = "，请看服务端日志最底部找原因。";
        public const string HandleServerCrashPart = "（守护自动重启是关着的）";
        public const string HandleServerCrashAppendServerLog3 = "[守护] 10 分钟内已异常退出 ";
        public const string HandleServerCrashAppendServerLog4 = " 次，停止自动重启，多半是存档或 mod 出了问题。";
        public const string HandleServerCrashAppendState2 = "服务端连续异常退出，已停止自动重启，请看日志。";
        public const string HandleServerCrashStr4 = " 可能原因：";
        public const string HandleServerCrashAppendState3 = "服务端异常退出，守护将在 5 秒后自动重启…";
        public const string GuessCrash614Ret = "端口 ";
        public const string GuessCrash614Part = " 被占用，多半是还有一个服务端没关干净";
        public const string GuessCrash614Ret2 = "内存不足或内存参数（-Xmx）设置有问题";
        public const string GuessCrash614Ret3 = "Java 版本不对，这个服务端需要 Java 17";
        public const string GuessCrash614Ret4 = "服务器卡死超过 60 秒，被游戏自带的看门狗停掉";
        public const string GuessCrash614Ret5 = "有模组加载失败，看日志里报错的 mod 名";
        public const string GuessCrash614Ret6 = "eula 未同意";
        public const string OfferWizardIfNeededShow = "还没有可用的服务端：设置里的服务端目录不存在，或里面没有服务端 jar。\n\n「首次设置向导」可以下载匹配的 Java 和服务端（原版 / Fabric / Paper），并写好 EULA 与离线开服所需的 server.properties，下载均带哈希校验。";
        public const string OfferWizardIfNeededShow2 = "打开向导";
        public const string OfferWizardIfNeededShow3 = "以后再说";
        public const string OpenWizardAppendState = "首次设置向导已完成，服务端和 Java 都就绪了。";
        public const string CheckJavaVersionStr = "未知";
        public const string CheckJavaVersionAppendServerLog = "[Java 检查] 读不到服务端 jar 的版本信息（version.json），跳过版本校验。当前 Java 大版本：";
        public const string CheckJavaVersionAppendServerLog2 = "。";
        public const string CheckJavaVersionAppendServerLog3 = "[Java 检查] 服务端版本 ";
        public const string CheckJavaVersionAppendServerLog4 = " 不在对照表内，跳过版本校验。当前 Java 大版本：";
        public const string CheckJavaVersionAppendServerLog5 = "[Java 检查] 服务端 ";
        public const string CheckJavaVersionAppendServerLog6 = "，但没测出当前 Java 的版本，放行启动（请留意服务端日志）。";
        public const string CheckJavaVersionAppendServerLog7 = "[Java 检查] 通过：服务端 ";
        public const string CheckJavaVersionAppendServerLog8 = "，当前 Java ";
        public const string CheckJavaVersionAppendServerLog9 = "[Java 检查] 不匹配：服务端 ";
        public const string CheckJavaVersionAppendServerLog10 = "，当前 Java 只有 ";
        public const string CheckJavaVersionAppendServerLog11 = "，已弹出提示等待确认。";
        public const string Msg2 = "Java 版本不够，服务端大概率起不来。\n\n服务端版本：";
        public const string CheckJavaVersionPart = "，需要 Java ";
        public const string CheckJavaVersionPart2 = " 或更高；当前将要使用的 Java 大版本是 ";
        public const string CheckJavaVersionPart3 = "。\n\n建议：到「设置」里换一个 Java ";
        public const string CheckJavaVersionPart4 = " 的 java.exe，或用「首次设置向导」自动下载匹配的 Java。\n选「仍然启动」会按现在的 Java 硬启动，报错请看服务端日志。";
        public const string CheckJavaVersionShow = "Java 版本不匹配";
        public const string CheckJavaVersionShow2 = "仍然启动";
        public const string CheckJavaVersionShow3 = "取消启动";
        public const string CheckJavaVersionAppendServerLog12 = "[Java 检查] 用户选择仍然启动。";
        public const string CheckJavaVersionAppendServerLog13 = "[Java 检查] 用户取消了启动。";
        public const string StartAllAppendState = "服务端已在运行，无需重复启动。";
        public const string BtnStartText3 = "联机运行中";
        public const string BtnStartText4 = "启动中…";
        public const string DoStartFailState = "服务端目录不存在：";
        public const string DoStartFailState2 = "，请到「设置」里检查服务端目录。";
        public const string DoStartFailState3 = "找不到服务端 jar：";
        public const string DoStartFailState4 = "，请到「设置」里检查 jar 文件名。";
        public const string DoStartFailState5 = "找不到 Java（java.exe）。请到「设置」里重新选择 Java 路径，通常是 .minecraft\\runtime 下或安装目录里。";
        public const string DoStartFailState6 = "已取消启动：Java 版本低于服务端要求。换匹配的 Java，或用「首次设置向导」自动下载后再试。";
        public const string DoStartFailState7 = " 已被占用。可能是上次服务端没关干净，请点「安全关服」或重启本应用，不要在任务管理器里结束 Java 进程。";
        public const string DoStartAppendState = "开服前自动备份中…";
        public const string DoStartAppendState2 = "正在启动 Minecraft 服务端…";
        public const string DoStartAppendServerLog = "[进程已退出]";
        public const string DoStartFailState8 = "无法启动 Java 进程，请检查 Java 路径是否正确。";
        public const string DoStartAppendState3 = "等待服务端就绪（出现 Done）…";
        public const string DoStartFailState9 = "服务端进程在出现 Done 之前就退出了。最可能：1) Java 路径或内存参数不对；2) 存档/world 或 mods 报错（请看服务端日志最底部）。不要动 world 和 mods。";
        public const string DoStartFailState10 = "服务端 90 秒还没出现 Done。可能内存不够、世界过大、或启动卡住。请看服务端日志最底部，别动 world 和 mods。";
        public const string DoStartAppendState4 = "已就绪，点「复制地址发给朋友」把地址发给朋友即可。";
        public const string DoStartFailState11 = "启动出错：";
        public const string StartTunnelAppendTunnelLog = "[备用方案] 正在打开官方 SakuraFrp 启动器，请在启动器里启动隧道。";
        public const string StartTunnelAppendTunnelLog2 = "打开 SakuraFrp 启动器失败：";
        public const string StartTunnelAppendTunnelLog3 = "找不到 frpc.exe：";
        public const string StartTunnelAppendTunnelLog4 = "，请到「设置」里检查，或把隧道方式切到官方启动器。";
        public const string StartTunnelAppendTunnelLog5 = "还没填访问密钥，请到「设置」里填写 SakuraFrp 访问密钥。";
        public const string StartTunnelAppendTunnelLog6 = "还没填隧道 ID，请到「设置」里填写。";
        public const string StartTunnelAppendState = "正在启动 SakuraFrp 隧道…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc 已退出]";
        public const string StartTunnelAppendTunnelLog8 = "[守护] 游戏隧道异常退出，5 秒后自动重连。";
        public const string StartTunnelAppendTunnelLog9 = "frpc 启动失败，请检查 frpc.exe 路径。";
        public const string StartTunnelAppendTunnelLog10 = "检测到当前 frpc.exe 是 SakuraFrp 启动器自带的套装版，这个版本不能由外部程序直接运行（只能由 SakuraFrp Service 启动），所以直接调用会退出。这与访问密钥、隧道 ID、节点都无关。请到「设置」把 frpc 路径改成独立版 frpc，或把隧道方式切到官方启动器。";
        public const string StartTunnelAppendTunnelLog11 = "frpc 进程退出了。可能：1) 访问密钥或隧道 ID 错误；2) 节点异常；3) 账号需实名/签到。请到「设置」检查密钥和隧道 ID，或把隧道方式切到官方启动器。";
        public const string StartTunnelAppendState2 = "frpc 是启动器套装版，不能直接运行。请到「设置」改独立版 frpc 或切官方启动器。";
        public const string StartVoiceTunnelAppendTunnelLog = "[语音] 未填写语音隧道 ID，跳过语音隧道。";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[语音 frpc 已退出]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[守护] 语音隧道异常退出，5 秒后自动重连。";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[语音] frpc 启动失败，请检查 frpc.exe 路径。";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[语音] frpc 进程退出了，请检查语音隧道 ID 是否正确。";
        public const string StartVoiceTunnelAppendState = "语音隧道已连接。";
        public const string VoiceFrpcOutputAppendTunnelLog = "[语音] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[语音][ERR] ";
        public const string TryParseFrpcAppendState = "已自动获取公网地址：";
        public const string ServerText4 = "服务端：出错";
        public const string TunnelText4 = "SakuraFrp 隧道：出错";
        public const string SafeStopAppendState = "服务端未在运行，已停止隧道。";
        public const string SafeStopAppendState2 = "正在安全关服：向服务端发送 stop，等待存档保存…";
        public const string SafeStopAppendServerLog = ">> 已发送 stop，等待存档保存并退出（最多 60 秒）";
        public const string SafeStopAppendState3 = "服务端已正常停止（存档已保存）。正在关闭隧道…";
        public const string SafeStopAppendServerLog2 = "[服务端进程已正常退出]";
        public const string SafeStopShow = "服务端 60 秒还没退出。强制结束可能丢失最近几分钟的进度。是否强制结束？";
        public const string SafeStopShow2 = "安全关服超时";
        public const string SafeStopShow3 = "强制结束";
        public const string SafeStopShow4 = "再等等";
        public const string SafeStopAppendState4 = "已强制结束服务端进程（可能丢失最近进度）。";
        public const string SafeStopAppendState5 = "已取消强制结束，服务端仍在运行。";
        public const string SafeStopAppendState6 = "已全部停止：服务端和隧道均已关闭。";
        public const string OnClosingConfirmBox = "服务端还在运行，直接关可能不存档，要先安全关服吗？";
        public const string BtnSparkTpsText = "TPS 报告";
        public const string BtnSparkProfileText = "诊断 30 秒";
        public const string SparkLblCaption = "性能诊断";
        public const string SparkProfileStarted = "[诊断] 已开始 30 秒性能采样，采样期间请保持服务器运行…";
        public const string SparkProfileStopped = "[诊断] 采样结束，spark 报告链接会出现在上方服务器日志里。";
        public const string SparkProfileCancelled = "[诊断] 服务器已停止，本次诊断已取消。";
        public const string LangCaption = "界面语言";
        public const string LangRestartHint = "语言设置已保存，重启程序后生效";
        public const string LangRestartNow = "立即重启";
        public const int EulaAgreeWidth = 190;
        public const int EulaLinkDx = 190;
    }

    static class TxtTw
    {
        public const string Msg = "出了點小問題";
        public const string ShowFriendlyErrorStr = "，程式需要關閉。";
        public const string ShowFriendlyErrorStr2 = "，程式會繼續執行。";
        public const string ShowFriendlyErrorPart = "\n\n如果反覆出現，把這段資訊告訴奶蛙就行。";
        public const string ShowFriendlyErrorShow = "Minecraft 多人連線助手";
        public const string YesText = "先安全關服再結束";
        public const string NoText = "取消";
        public const string SettingsFormInitChrome = "設定";
        public const string SettingsFormAddField = "伺服器資料夾 D:\\MCServer";
        public const string SettingsFormAddField2 = "Java 完整路徑 (java.exe)";
        public const string SettingsFormAddField3 = "jar 檔案名稱";
        public const string SettingsFormAddField4 = "記憶體參數";
        public const string SettingsFormAddField5 = "連接埠";
        public const string SettingsFormAddField6 = "frpc.exe 路徑";
        public const string LbModeCaption = "隧道方式";
        public const string SettingsFormAdd = "frpc 命令列（優先）";
        public const string SettingsFormAdd2 = "開啟官方 SakuraFrp 啟動器（備用）";
        public const string LbKeyCaption = "SakuraFrp 存取金鑰";
        public const string SettingsFormAddField7 = "隧道 ID";
        public const string SettingsFormAddField8 = "語音隧道 ID（可空）";
        public const string SettingsFormAddField9 = "公開位址（可空）";
        public const string SettingsFormAddField10 = "PCL2 exe 路徑";
        public const string BtnPropsText = "伺服器屬性…";
        public const string SettingsFormShow = "先把上面的伺服器資料夾填對，再來改伺服器屬性。";
        public const string SettingsFormShow2 = "伺服器屬性";
        public const string BtnWizardText = "首次設定精靈";
        public const string BtnSaveText = "儲存";
        public const string ValidateSettingsMarkInvalid = "伺服器資料夾不存在，請檢查路徑。";
        public const string ValidateSettingsMarkInvalid2 = "Java 路徑不存在，請檢查。";
        public const string ValidateSettingsMarkInvalid3 = "jar 檔案名稱不能為空。";
        public const string ValidateSettingsMarkInvalid4 = "伺服器資料夾裡找不到這個 jar 檔案。";
        public const string ValidateSettingsMarkInvalid5 = "連接埠必須是 1-65535 的數字。";
        public const string ValidateSettingsMarkInvalid6 = "frpc.exe 路徑不存在，請檢查。";
        public const string ValidateSettingsMarkInvalid7 = "frpc 模式下隧道 ID 不能為空。";
        public const string ValidateSettingsMarkInvalid8 = "PCL2 路徑不存在，請檢查。";
        public const string TitleText = "Minecraft 多人連線助手 v6.22.5";
        public const string SubText = "雙隧道版 · 遊戲 + 語音";
        public const string ServerText = "伺服器：未啟動";
        public const string TunnelText = "SakuraFrp 隧道：未啟動";
        public const string AddrText = "公開位址：未產生";
        public const string SrvState = "未啟動";
        public const string MbText = "玩家管理";
        public const string OnPaintHeader = "伺服器程序";
        public const string OnPaintRow = "狀態";
        public const string OnPaintRow2 = "記憶體占用";
        public const string OnPaintRow3 = "玩家連線";
        public const string OnPaintDrawText = "線上玩家";
        public const string OnPaintRow4 = "版本 · 延遲";
        public const string OnPaintHeader2 = "隧道程序（frpc）";
        public const string OnPaintHeader3 = "本機";
        public const string OnPaintDrawText2 = "下行";
        public const string OnPaintDrawText3 = "上行";
        public const string OnPaintRow5 = "系統記憶體";
        public const string OnHandleCreatedIndexOf = "錯誤";
        public const string OnHandleCreatedStr = "出錯";
        public const string ShowMsg = "確定";
        public const string PropsFormAddCombo = "難度";
        public const string PropsFormStr = "和平";
        public const string PropsFormStr2 = "簡單";
        public const string PropsFormStr3 = "普通";
        public const string PropsFormStr4 = "困難";
        public const string PropsFormAddCombo2 = "遊戲模式";
        public const string PropsFormStr5 = "生存";
        public const string PropsFormStr6 = "創造";
        public const string PropsFormStr7 = "冒險";
        public const string PropsFormStr8 = "旁觀";
        public const string PropsFormAddNum = "最大玩家數";
        public const string PropsFormAddNum2 = "視距（區塊）";
        public const string PropsFormAddNum3 = "模擬距離（區塊）";
        public const string PropsFormAddNum4 = "出生點保護半徑";
        public const string PropsFormAddText = "伺服器標語 MOTD";
        public const string PropsFormAddCheck = "啟用白名單（white-list）";
        public const string PropsFormAddCheck2 = "允許玩家互相攻擊（pvp）";
        public const string PropsFormAddCheck3 = "允許飛行（allow-flight）";
        public const string LbOnCaption = "正版驗證：關閉（離線連線必須保持關閉，這裡不能改）";
        public const string PropsFormShow = "在伺服器資料夾裡找不到 server.properties。伺服器至少成功啟動過一次才會產生這個檔案。";
        public const string SavePropsShow = "最大玩家數要是 1-9999 的數字。";
        public const string SavePropsShow2 = "視距要是 2-32 的數字。";
        public const string SavePropsShow3 = "模擬距離要是 2-32 的數字。";
        public const string SavePropsShow4 = "出生點保護半徑要是 0-9999 的數字。";
        public const string SavePropsShow5 = "儲存失敗：";
        public const string SavePropsShow6 = "已儲存。伺服器正在執行，這些設定會在下次開服時生效。";
        public const string PlayerFormStr = "線上玩家（";
        public const string PlayerFormStr2 = "現在沒有玩家在線";
        public const string PlayerFormAddBtn = "傳送到我身邊";
        public const string PlayerFormAddBtn2 = "踢出伺服器";
        public const string PlayerFormConfirmDo = "把 ";
        public const string PlayerFormConfirmDo2 = " 踢出伺服器？";
        public const string PlayerFormAddBtn3 = "給 OP";
        public const string PlayerFormAddBtn4 = "取消 OP";
        public const string PlayerFormAddBtn5 = "封鎖";
        public const string PlayerFormConfirmDo3 = "封鎖 ";
        public const string PlayerFormConfirmDo4 = "？封鎖後他進不了伺服器（可解除封鎖）。";
        public const string PlayerFormAddBtn6 = "解除封鎖";
        public const string PlayerFormStr3 = "提示：你自己不在線時，「傳送到我身邊」不可用。";
        public const string PlayerFormStr4 = "動作會以主控台指令傳送給伺服器，和在指令框輸入一樣。";
        public const string CloseText = "關閉";
        public const string LbOpsCaption = "OP 名單：";
        public const string RefreshListsStr = "（無）";
        public const string LbBannedCaption = "封鎖名單：";
        public const string WizardFormInitChrome = "首次設定精靈";
        public const string WizardFormAddLabel = "Minecraft 版本";
        public const string WizardFormAddHint = "可手填任意正式版版本號";
        public const string WizardFormAddLabel2 = "伺服器類型";
        public const string WizardFormAddRadio = "原版";
        public const string WizardFormAddLabel3 = "伺服器資料夾";
        public const string BtnBrowseText = "瀏覽…";
        public const string Description = "選擇伺服器資料夾（不存在也可以，先選一個上層資料夾再改路徑文字）";
        public const string ChkEulaText = "我同意 Minecraft EULA";
        public const string LnkEulaText = "檢視 EULA 全文";
        public const string WizardFormAddHint2 = "不勾選也能裝，但首次開服前要自己把 eula.txt 改成 eula=true";
        public const string LblStepCaption = "填好上面三項，點「開始安裝」。Java 和伺服器都會自動下載並驗證。";
        public const string BtnStartText = "開始安裝";
        public const string WizardFormWizLog = "正在取消…";
        public const string StartInstallShow = "先填 Minecraft 版本。";
        public const string StartInstallShow2 = "版本號格式不對，請填正式版版本號，例如 1.20.1（快照版暫不支援精靈）。";
        public const string StartInstallShow3 = "先填伺服器資料夾。";
        public const string StartInstallShow4 = "目標資料夾裡已經有 server.jar，繼續會覆蓋它。\n\n存檔和 mods 不受影響（精靈只換 jar 並補齊缺少的檔案）。確定繼續？";
        public const string StartInstallShow5 = "覆蓋並繼續";
        public const string StartInstallWizLog = "—— 開始安裝：Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "無法判斷 Minecraft ";
        public const string RunFail2 = " 需要的 Java 版本，精靈中止（沒有改動任何檔案）。";
        public const string RunWizLog = "提示：26.x 是 Mojang 的新年份版本號，官方要求的 Java 可能高於本精靈按 21 的估算，裝好後請留意伺服器日誌。";
        public const string RunWizStep = "第 1 步：準備 Java ";
        public const string RunWizLog2 = "已設定的 Java 大版本為 ";
        public const string RunWizLog3 = "，符合要求，直接使用：";
        public const string RunWizLog4 = "使用程式旁已下載的 Java：";
        public const string RunFail3 = " 需要 Java ";
        public const string RunFail4 = "，但本精靈只能自動下載 Java 17 / 21。請先手動安裝 Java ";
        public const string RunFail5 = "，在「設定」裡指定 java.exe 路徑後再開服。";
        public const string RunWizLog5 = "Adoptium 資產接口沒取到直連（";
        public const string RunStr = "無詳情";
        public const string RunPart = "），改用官方二進位接口下載；該接口不提供雜湊，本次 Java 下載不做雜湊驗證。";
        public const string RunWizLog6 = "下載 Java ";
        public const string RunWizLog7 = "（Eclipse Temurin JRE，約 40–50 MB）…";
        public const string RunFail6 = "Java 下載失敗：";
        public const string RunWizLog8 = "驗證 Java 壓縮檔 SHA256…";
        public const string RunFail7 = "Java 壓縮檔 SHA256 驗證不通過，已刪除下載檔案。期望 ";
        public const string RunFail8 = "，實際 ";
        public const string RunFail9 = "。請檢查網路後重試。";
        public const string RunWizLog9 = "SHA256 驗證通過。";
        public const string RunWizLog10 = "解壓縮 Java 到 ";
        public const string RunWizLog11 = " …";
        public const string RunFail10 = "Java 解壓縮失敗：";
        public const string RunFail11 = "Java 解壓縮完成但沒找到 bin\\java.exe，目錄結構異常，已中止（未寫入設定）。";
        public const string RunFail12 = "下載到的 Java 大版本是 ";
        public const string RunFail13 = "，仍低於需要的 ";
        public const string RunFail14 = "，已中止（未寫入設定）。";
        public const string RunWizLog12 = "Java 就緒：大版本 ";
        public const string RunWizLog13 = "，";
        public const string RunWizStep2 = "第 2 步：下載伺服器 jar";
        public const string RunFail15 = "拿不到 Mojang 版本清單：";
        public const string RunFail16 = "版本清單裡沒有 Minecraft ";
        public const string RunFail17 = "（請確認版本號無誤、且是正式版）。";
        public const string RunFail18 = "拿不到 ";
        public const string RunFail19 = " 的版本中繼資料：";
        public const string RunFail20 = "版本中繼資料裡沒有伺服器下載位址。";
        public const string RunWizLog14 = "下載原版伺服器（Mojang 官方）…";
        public const string RunDownloadWithProgress = "原版伺服器";
        public const string RunFail21 = "伺服器下載失敗：";
        public const string RunWizLog15 = "驗證伺服器 SHA1…";
        public const string RunFail22 = "伺服器 SHA1 驗證不通過，已刪除 jar。期望 ";
        public const string RunWizLog16 = "SHA1 驗證通過。";
        public const string RunFail23 = "拿不到 Fabric Loader 清單：";
        public const string RunFail24 = "Fabric 還沒有支援 Minecraft ";
        public const string RunFail25 = " 的穩定版 Loader，請換一個版本。";
        public const string RunFail26 = "拿不到 Fabric Installer 清單：";
        public const string RunFail27 = "Fabric Installer 清單異常。";
        public const string RunWizLog17 = "下載 Fabric 伺服器啟動 jar（Loader ";
        public const string RunWizLog18 = "，Installer ";
        public const string RunWizLog19 = "）…";
        public const string RunWizLog20 = "注意：Fabric 官方不公布該 jar 的雜湊驗證值，本檔案只能靠 HTTPS 傳輸，未做雜湊驗證。首次開服時它還會再下載原版伺服器與函式庫檔案。";
        public const string RunDownloadWithProgress2 = "Fabric 伺服器";
        public const string RunFail28 = "Fabric 伺服器下載失敗：";
        public const string RunWizLog21 = "已建立 mods 資料夾。提醒：Fabric 伺服器要裝 mod 還需要 Fabric API（放進 mods），語音等雙端 mod 每個玩家用戶端也要裝。";
        public const string RunFail29 = "拿不到 Paper 構建清單（Fill API）：";
        public const string RunFail30 = "。注意：Paper 不支援的版本會傳回錯誤，請確認該版本有 Paper。";
        public const string RunFail31 = "Paper 構建清單裡沒找到可下載的伺服器。";
        public const string RunWizLog22 = "下載 Paper 伺服器（最新構建）…";
        public const string RunDownloadWithProgress3 = "Paper 伺服器";
        public const string RunFail32 = "Paper 伺服器下載失敗：";
        public const string RunWizLog23 = "驗證伺服器 SHA256…";
        public const string RunFail33 = "伺服器 SHA256 驗證不通過，已刪除 jar。期望 ";
        public const string RunWizStep3 = "第 3 步：寫入初始檔案";
        public const string RunWizLog24 = "已寫入 eula.txt（eula=true，按你勾選的同意）。";
        public const string RunWizLog25 = "未勾選同意 EULA：沒有寫 eula.txt。首次開服時伺服器會產生 eula=false 並自動結束，把它改成 eula=true 後再開服。";
        public const string RunWizLog26 = "已寫入 server.properties：server-port=25565，online-mode=false（離線連線必須關正版驗證）。";
        public const string RunWizLog27 = "資料夾裡已有 server.properties，未改動它。請自行確認 online-mode=false，否則離線帳號進不來。";
        public const string RunWizStep4 = "第 4 步：儲存設定";
        public const string RunWizLog28 = "完成。伺服器資料夾：";
        public const string RunWizLog29 = "；jar：server.jar；Java：";
        public const string RunStr2 = "（沿用原設定）";
        public const string RunWizLog30 = "關閉精靈後，先到「設定」裡填 SakuraFrp 存取金鑰和隧道 ID，再點「啟動連線」。";
        public const string RunWizStep5 = "安裝完成";
        public const string RunFail34 = "精靈出錯：";
        public const string DownloadWithProgressWizStep = "下載";
        public const string DownloadWithProgressWizStep2 = "… ";
        public const string FailWizLog = "失敗：";
        public const string FailWizStep = "已停止";
        public const string CancelledWizLog = "已取消。未寫入設定；已完整下載好的檔案保留在目標資料夾，半成品已清理。";
        public const string CancelledWizStep = "已取消";
        public const string MainFormAppendServerLog = "[啟動器] 已啟動，日誌同步儲存在程式目錄的 logs 資料夾中（保留 14 天）。";
        public const string TrayText = "Minecraft 多人連線助手 · 已停止";
        public const string InitTray613ToolStripMenuItem = "顯示主視窗";
        public const string InitTray613ToolStripMenuItem2 = "安全結束";
        public const string BtnStartText2 = "啟動連線";
        public const string AddrLblCaption = "公開位址（傳給朋友）：";
        public const string Hint = "隧道啟動後，公開位址會顯示在這裡";
        public const string BtnCopyText = "複製位址傳給朋友";
        public const string BtnPcl2Text = "開啟 PCL2";
        public const string BtnStopText = "安全關服";
        public const string LogToggleLblCaption = "▾  日誌";
        public const string BuildUiStr = "伺服器日誌";
        public const string BuildUiStr2 = "隧道日誌";
        public const string BuildUiStr3 = "狀態監控";
        public const string BuildUiStr4 = "備份";
        public const string BuildUiMakeLogBox = "暫無日誌 · 伺服器輸出會顯示在這裡";
        public const string BuildUiMakeLogBox2 = "暫無日誌 · 隧道輸出會顯示在這裡";
        public const string BtnJumpBottomText = "↓ 回到底部";
        public const string Hint2 = "搜尋日誌…";
        public const string BuildUiSetToolTip = "上一個";
        public const string BuildUiSetToolTip2 = "下一個";
        public const string BuildUiSetToolTip3 = "在日誌裡搜尋，Enter 跳到下一個";
        public const string ChkErrOnlyText = "只看警告/錯誤";
        public const string CmdLblCaption = "伺服器指令";
        public const string BtnCmdSendText = "傳送";
        public const string BuildUiAppendState = "首次使用：請在「設定」裡填 SakuraFrp 存取金鑰、隧道 ID，檢查 PCL2 路徑。";
        public const string IsErrLineContains = "[守護]";
        public const string IsErrLineContains2 = "[備份] 失敗";
        public const string LblFindCaption = "無結果";
        public const string SrvState2 = "執行中 · ";
        public const string SampleStatsPart = " 人";
        public const string SampleStatsPart2 = " 人在線（名單未公開）";
        public const string SampleStatsPart3 = "延遲 ";
        public const string ToggleLogStr = "▸  日誌";
        public const string AppendServerLogPersistLog613 = "伺服器";
        public const string AppendTunnelLogPersistLog613 = "隧道";
        public const string SendServerCommandAppendState = "伺服器沒在執行，指令傳不出去。";
        public const string SendServerCommandAppendState2 = "傳送指令失敗：伺服器程序已不在。";
        public const string SendServerCommandAppendState3 = "傳送指令失敗：";
        public const string OpenPlayerManagerShow = "伺服器沒在執行，玩家管理要在開服後使用。";
        public const string OpenPlayerManagerAppendState = "指令傳送失敗：伺服器程序已不在。";
        public const string DoBackupAppendState = "已經有一個備份正在進行中了。";
        public const string BackupCoreAppendState = "找不到世界資料夾：";
        public const string BackupCoreAppendState2 = "，無法備份。";
        public const string BackupCoreAppendServerLog = "[備份] 已暫停伺服器自動儲存並落盤，開始打包世界…";
        public const string BackupCoreAppendServerLog2 = "[備份] 完成：";
        public const string BackupCorePart = " 個檔案";
        public const string BackupCoreStr = "，跳過 ";
        public const string BackupCorePart2 = " 個占用中的檔案";
        public const string BackupCoreAppendState3 = "世界備份完成（";
        public const string BackupCoreAppendServerLog3 = "[備份] 失敗：";
        public const string BackupCoreAppendState4 = "備份失敗：";
        public const string BackupCoreAppendServerLog4 = "[備份] 伺服器自動儲存已恢復。";
        public const string RestoreBackupShow = "還原存檔前必須先關服：伺服器開著的時候換世界檔案會損壞存檔。\n\n請先點「安全關服」，再來還原。";
        public const string RestoreBackupShow2 = "還原存檔";
        public const string RestoreBackupConfirmBox = "把世界還原到備份「";
        public const string RestoreBackupConfirmBox2 = "」？\n\n目前的世界不會被刪：它會先被改名留在一邊（world_prerestore_時間），確認沒問題後你可以自己刪；還原錯了也能換回來。";
        public const string RestoreBackupAppendState = "已經有一個備份任務正在進行中了。";
        public const string Result = "已還原到備份「";
        public const string RestoreBackupPart = "」。舊世界保留在 ";
        public const string RestoreBackupPart2 = "，確認新世界沒問題後可以刪除它。";
        public const string Result2 = "還原失敗：";
        public const string RestoreBackupPart3 = "（舊世界可能已被改名，先別開服，檢查伺服器資料夾。）";
        public const string RestoreBackupAppendServerLog = "[備份] ";
        public const string BuildBackupPanelBkLabel = "世界備份";
        public const string BtnBackupNowText = "立即備份";
        public const string BuildBackupPanelDoBackup = "手動備份";
        public const string BtnRestoreBackupText = "還原選中";
        public const string BuildBackupPanelShow = "先在上面的清單裡選中一個備份。";
        public const string BtnDeleteBackupText = "刪除選中";
        public const string BuildBackupPanelShow2 = "刪除備份";
        public const string BuildBackupPanelConfirmBox = "刪除備份「";
        public const string BuildBackupPanelPart = "」？刪了就還原不了了。";
        public const string BuildBackupPanelShow3 = "刪除失敗：";
        public const string BuildBackupPanelBkLabel2 = "保留最近";
        public const string BuildBackupPanelBkLabel3 = "份";
        public const string BuildBackupPanelBkCheck = "崩潰自動重啟";
        public const string BuildBackupPanelBkCheck2 = "開服前自動備份";
        public const string BuildBackupPanelBkCheck3 = "每日自動備份";
        public const string BuildBackupPanelBkLabel4 = "應用程式開著時，到點自動備份一次";
        public const string BuildBackupPanelBkCheck4 = "定時指令";
        public const string LblBackupStateCaption = "正在備份…（大世界可能要一兩分鐘）";
        public const string RefreshBackupListStr = "共 ";
        public const string RefreshBackupListPart = " 份 · ";
        public const string RefreshBackupListStr2 = "還沒有備份";
        public const string RefreshBackupListStr3 = "最近一份：";
        public const string RefreshBackupListStr4 = "點「立即備份」給世界留一份快照。";
        public const string CheckSchedulesAppendServerLog = "[定時] 到達每日備份時間，開始自動備份。";
        public const string CheckSchedulesAppendServerLog2 = "[定時] 到了定時指令時間，但還有 ";
        public const string CheckSchedulesAppendServerLog3 = " 人在線，延後到沒人時再執行。";
        public const string CheckSchedulesAppendServerLog4 = "[定時] 已傳送定時指令：";
        public const string ServerText2 = "伺服器：執行中";
        public const string ServerText3 = "伺服器：啟動中…";
        public const string TunnelText2 = "SakuraFrp 隧道：已連線";
        public const string TunnelText3 = "SakuraFrp 隧道：啟動中…";
        public const string AddrText2 = "公開位址：已就緒";
        public const string AddrText3 = "公開位址：已儲存（隧道未啟動）";
        public const string Tt = "Minecraft 多人連線助手 · ";
        public const string RefreshStatusStr = "執行中";
        public const string RefreshStatusStr2 = "啟動中";
        public const string CopyAddrShow = "公開位址還沒產生，請先啟動連線。";
        public const string CopyAddrShow2 = "提示";
        public const string CopyAddrAppendState = "公開位址已複製到剪貼簿，傳給朋友即可。";
        public const string BtnCopyText2 = "已複製 ✓";
        public const string OpenPcl2Show = "找不到 PCL2，請到「設定」裡填寫 PCL2 的 exe 路徑。";
        public const string OpenPcl2Show2 = "開啟 PCL2 失敗：";
        public const string HandleServerCrashAppendServerLog = "[守護] 伺服器異常結束（結束碼 ";
        public const string HandleServerCrashAppendServerLog2 = "），這不是正常關服。";
        public const string HandleServerCrashStr = "可能原因：";
        public const string HandleServerCrashAppendState = "伺服器異常結束了";
        public const string HandleServerCrashStr2 = "，可能原因：";
        public const string HandleServerCrashStr3 = "。請看伺服器日誌最底部找原因。";
        public const string HandleServerCrashPart = "（守護自動重啟是關著的）";
        public const string HandleServerCrashAppendServerLog3 = "[守護] 10 分鐘內已異常結束 ";
        public const string HandleServerCrashAppendServerLog4 = " 次，停止自動重啟，多半是存檔或 mod 出了問題。";
        public const string HandleServerCrashAppendState2 = "伺服器連續異常結束，已停止自動重啟，請看日誌。";
        public const string HandleServerCrashStr4 = " 可能原因： ";
        public const string HandleServerCrashAppendState3 = "伺服器異常結束，守護將在 5 秒後自動重啟…";
        public const string GuessCrash614Ret = "連接埠 ";
        public const string GuessCrash614Part = " 被占用，多半是還有一個伺服器沒關乾淨";
        public const string GuessCrash614Ret2 = "記憶體不足或記憶體參數（-Xmx）設定有問題";
        public const string GuessCrash614Ret3 = "Java 版本不對，這個伺服器需要 Java 17";
        public const string GuessCrash614Ret4 = "伺服器卡死超過 60 秒，被遊戲內建的看門狗停掉";
        public const string GuessCrash614Ret5 = "有模組載入失敗，看日誌裡報錯的 mod 名";
        public const string GuessCrash614Ret6 = "EULA 尚未同意";
        public const string OfferWizardIfNeededShow = "還沒有可用的伺服器：設定裡的伺服器資料夾不存在，或裡面沒有伺服器 jar。\n\n「首次設定精靈」可以下載符合的 Java 和伺服器（原版 / Fabric / Paper），並寫好 EULA 與離線開服所需的 server.properties，下載均帶雜湊驗證。";
        public const string OfferWizardIfNeededShow2 = "開啟精靈";
        public const string OfferWizardIfNeededShow3 = "以後再說";
        public const string OpenWizardAppendState = "首次設定精靈已完成，伺服器和 Java 都就緒了。";
        public const string CheckJavaVersionStr = "未知";
        public const string CheckJavaVersionAppendServerLog = "[Java 檢查] 讀不到伺服器 jar 的版本資訊（version.json），略過版本檢查。目前 Java 大版本：";
        public const string CheckJavaVersionAppendServerLog2 = "。";
        public const string CheckJavaVersionAppendServerLog3 = "[Java 檢查] 伺服器版本 ";
        public const string CheckJavaVersionAppendServerLog4 = " 不在對照表內，略過版本檢查。目前 Java 大版本：";
        public const string CheckJavaVersionAppendServerLog5 = "[Java 檢查] 伺服器 ";
        public const string CheckJavaVersionAppendServerLog6 = "，但沒測出目前 Java 的版本，放行啟動（請留意伺服器日誌）。";
        public const string CheckJavaVersionAppendServerLog7 = "[Java 檢查] 通過：伺服器 ";
        public const string CheckJavaVersionAppendServerLog8 = "，目前 Java 是 ";
        public const string CheckJavaVersionAppendServerLog9 = "[Java 檢查] 不符合：伺服器 ";
        public const string CheckJavaVersionAppendServerLog10 = "，目前 Java 只有 ";
        public const string CheckJavaVersionAppendServerLog11 = "，已彈出提示等待確認。";
        public const string Msg2 = "Java 版本不夠，伺服器大概率起不來。\n\n伺服器版本：";
        public const string CheckJavaVersionPart = "，需要 Java ";
        public const string CheckJavaVersionPart2 = " 或更高；目前將要使用的 Java 大版本是 ";
        public const string CheckJavaVersionPart3 = "。\n\n建議：到「設定」裡換一個 Java ";
        public const string CheckJavaVersionPart4 = " 的 java.exe，或用「首次設定精靈」自動下載符合的 Java。\n選「仍然啟動」會按現在的 Java 硬啟動，錯誤請看伺服器日誌。";
        public const string CheckJavaVersionShow = "Java 版本不符合";
        public const string CheckJavaVersionShow2 = "仍然啟動";
        public const string CheckJavaVersionShow3 = "取消啟動";
        public const string CheckJavaVersionAppendServerLog12 = "[Java 檢查] 使用者選擇仍然啟動。";
        public const string CheckJavaVersionAppendServerLog13 = "[Java 檢查] 使用者取消了啟動。";
        public const string StartAllAppendState = "伺服器已在執行，無需重複啟動。";
        public const string BtnStartText3 = "連線執行中";
        public const string BtnStartText4 = "啟動中…";
        public const string DoStartFailState = "伺服器資料夾不存在：";
        public const string DoStartFailState2 = "，請到「設定」裡檢查伺服器資料夾。";
        public const string DoStartFailState3 = "找不到伺服器 jar：";
        public const string DoStartFailState4 = "，請到「設定」裡檢查 jar 檔案名稱。";
        public const string DoStartFailState5 = "找不到 Java（java.exe）。請到「設定」裡重新選擇 Java 路徑，通常是 .minecraft\\runtime 下或安裝目錄裡。";
        public const string DoStartFailState6 = "已取消啟動：Java 版本低於伺服器要求。換符合的 Java，或用「首次設定精靈」自動下載後再試。";
        public const string DoStartFailState7 = " 已被占用。可能是上次伺服器沒關乾淨，請點「安全關服」或重啟本應用程式，不要在工作管理員裡結束 Java 程序。";
        public const string DoStartAppendState = "開服前自動備份中…";
        public const string DoStartAppendState2 = "正在啟動 Minecraft 伺服器…";
        public const string DoStartAppendServerLog = "[程序已結束]";
        public const string DoStartFailState8 = "無法啟動 Java 程序，請檢查 Java 路徑是否正確。";
        public const string DoStartAppendState3 = "等待伺服器就緒（出現 Done）…";
        public const string DoStartFailState9 = "伺服器程序在出現 Done 之前就結束了。最可能：1) Java 路徑或記憶體參數不對；2) 存檔/world 或 mods 報錯（請看伺服器日誌最底部）。不要動 world 和 mods。";
        public const string DoStartFailState10 = "伺服器 90 秒還沒出現 Done。可能記憶體不夠、世界過大、或啟動卡住。請看伺服器日誌最底部，別動 world 和 mods。";
        public const string DoStartAppendState4 = "已就緒，點「複製位址傳給朋友」把位址傳給朋友即可。";
        public const string DoStartFailState11 = "啟動出錯：";
        public const string StartTunnelAppendTunnelLog = "[備用方案] 正在開啟官方 SakuraFrp 啟動器，請在啟動器裡啟動隧道。";
        public const string StartTunnelAppendTunnelLog2 = "開啟 SakuraFrp 啟動器失敗：";
        public const string StartTunnelAppendTunnelLog3 = "找不到 frpc.exe：";
        public const string StartTunnelAppendTunnelLog4 = "，請到「設定」裡檢查，或把隧道方式切換到官方啟動器。";
        public const string StartTunnelAppendTunnelLog5 = "還沒填存取金鑰，請到「設定」裡填寫 SakuraFrp 存取金鑰。";
        public const string StartTunnelAppendTunnelLog6 = "還沒填隧道 ID，請到「設定」裡填寫。";
        public const string StartTunnelAppendState = "正在啟動 SakuraFrp 隧道…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc 已結束]";
        public const string StartTunnelAppendTunnelLog8 = "[守護] 遊戲隧道異常結束，5 秒後自動重新連線。";
        public const string StartTunnelAppendTunnelLog9 = "frpc 啟動失敗，請檢查 frpc.exe 路徑。";
        public const string StartTunnelAppendTunnelLog10 = "偵測到目前 frpc.exe 是 SakuraFrp 啟動器內附的套裝版，這個版本不能由外部程式直接執行（只能由 SakuraFrp Service 啟動），所以直接呼叫會結束。這與存取金鑰、隧道 ID、節點都無關。請到「設定」把 frpc 路徑改成獨立版 frpc，或把隧道方式切換到官方啟動器。";
        public const string StartTunnelAppendTunnelLog11 = "frpc 程序結束了。可能：1) 存取金鑰或隧道 ID 錯誤；2) 節點異常；3) 帳號需實名/簽到。請到「設定」檢查金鑰和隧道 ID，或把隧道方式切換到官方啟動器。";
        public const string StartTunnelAppendState2 = "frpc 是啟動器套裝版，不能直接執行。請到「設定」改獨立版 frpc 或切換官方啟動器。";
        public const string StartVoiceTunnelAppendTunnelLog = "[語音] 未填寫語音隧道 ID，略過語音隧道。";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[語音 frpc 已結束]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[守護] 語音隧道異常結束，5 秒後自動重新連線。";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[語音] frpc 啟動失敗，請檢查 frpc.exe 路徑。";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[語音] frpc 程序結束了，請檢查語音隧道 ID 是否正確。";
        public const string StartVoiceTunnelAppendState = "語音隧道已連線。";
        public const string VoiceFrpcOutputAppendTunnelLog = "[語音] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[語音][ERR] ";
        public const string TryParseFrpcAppendState = "已自動取得公開位址：";
        public const string ServerText4 = "伺服器：出錯";
        public const string TunnelText4 = "SakuraFrp 隧道：出錯";
        public const string SafeStopAppendState = "伺服器未在執行，已停止隧道。";
        public const string SafeStopAppendState2 = "正在安全關服：向伺服器傳送 stop，等待存檔儲存…";
        public const string SafeStopAppendServerLog = ">> 已傳送 stop，等待儲存並結束（最多 60 秒）";
        public const string SafeStopAppendState3 = "伺服器已正常停止（存檔已儲存）。正在關閉隧道…";
        public const string SafeStopAppendServerLog2 = "[伺服器程序已正常結束]";
        public const string SafeStopShow = "伺服器 60 秒還沒結束。強制結束可能遺失最近幾分鐘的進度。是否強制結束？";
        public const string SafeStopShow2 = "安全關服逾時";
        public const string SafeStopShow3 = "強制結束";
        public const string SafeStopShow4 = "再等等";
        public const string SafeStopAppendState4 = "已強制結束伺服器程序（可能遺失最近進度）。";
        public const string SafeStopAppendState5 = "已取消強制結束，伺服器仍在執行。";
        public const string SafeStopAppendState6 = "已全部停止：伺服器和隧道均已關閉。";
        public const string OnClosingConfirmBox = "伺服器還在執行，直接關可能不存檔，要先安全關服嗎？";
        public const string BtnSparkTpsText = "TPS 報告";
        public const string BtnSparkProfileText = "診斷 30 秒";
        public const string SparkLblCaption = "性能診斷";
        public const string SparkProfileStarted = "[診斷] 已開始 30 秒性能取樣，取樣期間請保持伺服器執行…";
        public const string SparkProfileStopped = "[診斷] 取樣結束，spark 報告連結會出現在上方伺服器日誌裡。";
        public const string SparkProfileCancelled = "[診斷] 伺服器已停止，本次診斷已取消。";
        public const string LangCaption = "介面語言";
        public const string LangRestartHint = "語言設定已儲存，重新啟動程式後生效";
        public const string LangRestartNow = "立即重啟";
        public const int EulaAgreeWidth = 190;
        public const int EulaLinkDx = 190;
    }

    static class TxtJa
    {
        public const string Msg = "問題が発生しました";
        public const string ShowFriendlyErrorStr = "。プログラムを終了します。";
        public const string ShowFriendlyErrorStr2 = "。プログラムは動作を続けます。";
        public const string ShowFriendlyErrorPart = "\n\n繰り返し発生する場合は、この情報を Naiwa に送ってください。";
        public const string ShowFriendlyErrorShow = "Minecraft マルチプレイヘルパー";
        public const string YesText = "サーバーを安全に停止してから終了";
        public const string NoText = "キャンセル";
        public const string SettingsFormInitChrome = "設定";
        public const string SettingsFormAddField = "サーバーフォルダー D:\\MCServer";
        public const string SettingsFormAddField2 = "Java のフルパス (java.exe)";
        public const string SettingsFormAddField3 = "jar ファイル名";
        public const string SettingsFormAddField4 = "メモリ引数";
        public const string SettingsFormAddField5 = "ポート";
        public const string SettingsFormAddField6 = "frpc.exe のパス";
        public const string LbModeCaption = "トンネル方式";
        public const string SettingsFormAdd = "frpc コマンドライン（優先）";
        public const string SettingsFormAdd2 = "公式 SakuraFrp ランチャーを開く（予備）";
        public const string LbKeyCaption = "SakuraFrp アクセスキー";
        public const string SettingsFormAddField7 = "トンネル ID";
        public const string SettingsFormAddField8 = "音声トンネル ID（任意）";
        public const string SettingsFormAddField9 = "公開アドレス（任意）";
        public const string SettingsFormAddField10 = "PCL2 exe のパス";
        public const string BtnPropsText = "サーバープロパティ…";
        public const string SettingsFormShow = "まず上のサーバーフォルダーを正しく設定してから、サーバープロパティを編集してください。";
        public const string SettingsFormShow2 = "サーバープロパティ";
        public const string BtnWizardText = "初回セットアップウィザード";
        public const string BtnSaveText = "保存";
        public const string ValidateSettingsMarkInvalid = "サーバーフォルダーが存在しません。パスを確認してください。";
        public const string ValidateSettingsMarkInvalid2 = "Java のパスが存在しません。確認してください。";
        public const string ValidateSettingsMarkInvalid3 = "jar ファイル名は空にできません。";
        public const string ValidateSettingsMarkInvalid4 = "その jar ファイルがサーバーフォルダーに見つかりません。";
        public const string ValidateSettingsMarkInvalid5 = "ポートは 1 から 65535 の数値でなければなりません。";
        public const string ValidateSettingsMarkInvalid6 = "frpc.exe のパスが存在しません。確認してください。";
        public const string ValidateSettingsMarkInvalid7 = "frpc モードではトンネル ID は空にできません。";
        public const string ValidateSettingsMarkInvalid8 = "PCL2 のパスが存在しません。確認してください。";
        public const string TitleText = "Minecraft マルチプレイヘルパー v6.22.5";
        public const string SubText = "デュアルトンネル · ゲーム + 音声";
        public const string ServerText = "サーバー：停止中";
        public const string TunnelText = "SakuraFrp トンネル：停止中";
        public const string AddrText = "公開アドレス：未生成";
        public const string SrvState = "停止中";
        public const string MbText = "プレイヤー";
        public const string OnPaintHeader = "サーバープロセス";
        public const string OnPaintRow = "状態";
        public const string OnPaintRow2 = "メモリ使用量";
        public const string OnPaintRow3 = "プレイヤー接続";
        public const string OnPaintDrawText = "オンラインプレイヤー";
        public const string OnPaintRow4 = "バージョン · Ping";
        public const string OnPaintHeader2 = "トンネルプロセス (frpc)";
        public const string OnPaintHeader3 = "ローカル";
        public const string OnPaintDrawText2 = "下り";
        public const string OnPaintDrawText3 = "上り";
        public const string OnPaintRow5 = "システムメモリ";
        public const string OnHandleCreatedIndexOf = "エラー";
        public const string OnHandleCreatedStr = "エラー";
        public const string ShowMsg = "OK";
        public const string PropsFormAddCombo = "難易度";
        public const string PropsFormStr = "ピースフル";
        public const string PropsFormStr2 = "イージー";
        public const string PropsFormStr3 = "ノーマル";
        public const string PropsFormStr4 = "ハード";
        public const string PropsFormAddCombo2 = "ゲームモード";
        public const string PropsFormStr5 = "サバイバル";
        public const string PropsFormStr6 = "クリエイティブ";
        public const string PropsFormStr7 = "アドベンチャー";
        public const string PropsFormStr8 = "スペクテイター";
        public const string PropsFormAddNum = "最大プレイヤー数";
        public const string PropsFormAddNum2 = "描画距離（チャンク）";
        public const string PropsFormAddNum3 = "シミュレーション距離（チャンク）";
        public const string PropsFormAddNum4 = "スポーン保護の半径";
        public const string PropsFormAddText = "サーバー MOTD";
        public const string PropsFormAddCheck = "ホワイトリストを有効化（white-list）";
        public const string PropsFormAddCheck2 = "PvP を許可（pvp）";
        public const string PropsFormAddCheck3 = "飛行を許可（allow-flight）";
        public const string LbOnCaption = "オンラインモード：オフ（オフラインでのマルチプレイにはオフのまま必須。ここでは変更できません）";
        public const string PropsFormShow = "サーバーフォルダーに server.properties が見つかりません。このファイルはサーバーが初めて正常に起動したときに作成されます。";
        public const string SavePropsShow = "最大プレイヤー数は 1 から 9999 の数値でなければなりません。";
        public const string SavePropsShow2 = "描画距離は 2 から 32 の数値でなければなりません。";
        public const string SavePropsShow3 = "シミュレーション距離は 2 から 32 の数値でなければなりません。";
        public const string SavePropsShow4 = "スポーン保護の半径は 0 から 9999 の数値でなければなりません。";
        public const string SavePropsShow5 = "保存に失敗しました：";
        public const string SavePropsShow6 = "保存しました。サーバーが稼働中のため、これらの設定は次回起動時に有効になります。";
        public const string PlayerFormStr = "オンラインプレイヤー（";
        public const string PlayerFormStr2 = "現在オンラインのプレイヤーはいません";
        public const string PlayerFormAddBtn = "自分のところへテレポート";
        public const string PlayerFormAddBtn2 = "サーバーからキック";
        public const string PlayerFormConfirmDo = "";
        public const string PlayerFormConfirmDo2 = " をサーバーからキックしますか？";
        public const string PlayerFormAddBtn3 = "OP を付与";
        public const string PlayerFormAddBtn4 = "OP を解除";
        public const string PlayerFormAddBtn5 = "BAN";
        public const string PlayerFormConfirmDo3 = "";
        public const string PlayerFormConfirmDo4 = " をBANしますか？BANするとサーバーに参加できなくなります（後で解除できます）。";
        public const string PlayerFormAddBtn6 = "BAN を解除";
        public const string PlayerFormStr3 = "注意：自分がオンラインでない間は「自分のところへテレポート」は使用できません。";
        public const string PlayerFormStr4 = "アクションはコンソールコマンドとしてサーバーに送信されます。コマンド欄に入力するのと同じです。";
        public const string CloseText = "閉じる";
        public const string LbOpsCaption = "OP リスト： ";
        public const string RefreshListsStr = "（なし）";
        public const string LbBannedCaption = "BAN リスト： ";
        public const string WizardFormInitChrome = "初回セットアップウィザード";
        public const string WizardFormAddLabel = "Minecraft バージョン";
        public const string WizardFormAddHint = "リリース版であれば自由に入力できます";
        public const string WizardFormAddLabel2 = "サーバーの種類";
        public const string WizardFormAddRadio = "バニラ";
        public const string WizardFormAddLabel3 = "サーバーフォルダー";
        public const string BtnBrowseText = "参照...";
        public const string Description = "サーバーフォルダーを選択してください（まだ存在しなくてもかまいません。親フォルダーを選んでパス文字列を編集してください）";
        public const string ChkEulaText = "Minecraft EULA に同意します";
        public const string LnkEulaText = "EULA 全文を読む";
        public const string WizardFormAddHint2 = "チェックしなくてもインストールできますが、初回起動前に eula.txt を自分で eula=true に変更する必要があります";
        public const string LblStepCaption = "上の 3 項目を入力して「インストール開始」をクリックしてください。Java とサーバーは自動でダウンロードされ、検証されます。";
        public const string BtnStartText = "インストール開始";
        public const string WizardFormWizLog = "キャンセルしています...";
        public const string StartInstallShow = "まず Minecraft バージョンを入力してください。";
        public const string StartInstallShow2 = "リリース版のバージョン番号ではないようです。1.20.1 のように入力してください（スナップショットはウィザード未対応です）。";
        public const string StartInstallShow3 = "まずサーバーフォルダーを入力してください。";
        public const string StartInstallShow4 = "対象フォルダーには既に server.jar があります。続行すると上書きされます。\n\nワールドと mods には触れません（ウィザードは jar の交換と不足ファイルの追加のみを行います）。続行しますか？";
        public const string StartInstallShow5 = "上書きして続行";
        public const string StartInstallWizLog = "-- インストール開始：Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "Minecraft ";
        public const string RunFail2 = " に必要な Java を判定できません。ウィザードを中止しました（何も変更されていません）。";
        public const string RunWizLog = "注意：26.x は Mojang の新しい年ベースのバージョン表記です。公式の Java 要件は、ここで想定する Java 21 より高い可能性があります。インストール後はサーバーログを確認してください。";
        public const string RunWizStep = "ステップ 1：Java の準備 ";
        public const string RunWizLog2 = "設定済みの Java のメジャーバージョンは ";
        public const string RunWizLog3 = " で、要件を満たしています。そのまま使用します：";
        public const string RunWizLog4 = "アプリの隣にダウンロード済みの Java を使用します：";
        public const string RunFail3 = " には Java ";
        public const string RunFail4 = " が必要ですが、このウィザードが自動ダウンロードできるのは Java 17 / 21 のみです。Java ";
        public const string RunFail5 = " を手動でインストールし、設定で java.exe を指定してからサーバーを起動してください。";
        public const string RunWizLog5 = "Adoptium アセット API から直接リンクが得られませんでした（";
        public const string RunStr = "詳細なし";
        public const string RunPart = "）。公式バイナリエンドポイントに切り替えますが、そちらはチェックサムを公開していないため、この Java ダウンロードはハッシュ検証されません。";
        public const string RunWizLog6 = "Java をダウンロードしています ";
        public const string RunWizLog7 = "（Eclipse Temurin JRE、約 40-50 MB）...";
        public const string RunFail6 = "Java のダウンロードに失敗しました：";
        public const string RunWizLog8 = "Java zip の SHA256 を検証しています...";
        public const string RunFail7 = "Java zip の SHA256 が一致しません。ダウンロードは削除されました。期待値 ";
        public const string RunFail8 = "、実際 ";
        public const string RunFail9 = "。ネットワークを確認して再試行してください。";
        public const string RunWizLog9 = "SHA256 を検証しました。";
        public const string RunWizLog10 = "Java を展開しています ";
        public const string RunWizLog11 = " ...";
        public const string RunFail10 = "Java の展開に失敗しました：";
        public const string RunFail11 = "展開は終わりましたが bin\\java.exe が見つかりません。zip の構成が想定と異なるため中止しました（設定は保存されていません）。";
        public const string RunFail12 = "ダウンロードした Java のメジャーバージョンは ";
        public const string RunFail13 = " で、必要な ";
        public const string RunFail14 = " 未満です。中止しました（設定は保存されていません）。";
        public const string RunWizLog12 = "Java の準備完了：メジャーバージョン ";
        public const string RunWizLog13 = "、";
        public const string RunWizStep2 = "ステップ 2：サーバー jar のダウンロード";
        public const string RunFail15 = "Mojang のバージョンマニフェストを取得できません：";
        public const string RunFail16 = "バージョンマニフェストに Minecraft ";
        public const string RunFail17 = " がありません（番号を確認してください。リリース版のみ）。";
        public const string RunFail18 = "バージョンのメタデータを取得できません ";
        public const string RunFail19 = "： ";
        public const string RunFail20 = "バージョンのメタデータにサーバーのダウンロードがありません。";
        public const string RunWizLog14 = "バニラサーバーをダウンロードしています（Mojang 公式）...";
        public const string RunDownloadWithProgress = "バニラサーバー";
        public const string RunFail21 = "サーバーのダウンロードに失敗しました：";
        public const string RunWizLog15 = "サーバーの SHA1 を検証しています...";
        public const string RunFail22 = "サーバーの SHA1 が一致しません。jar は削除されました。期待値 ";
        public const string RunWizLog16 = "SHA1 を検証しました。";
        public const string RunFail23 = "Fabric のローダーリストを取得できません：";
        public const string RunFail24 = "Fabric には Minecraft ";
        public const string RunFail25 = " 向けの安定版ローダーがまだありません。別のバージョンを選んでください。";
        public const string RunFail26 = "Fabric インストーラーのリストを取得できません：";
        public const string RunFail27 = "Fabric インストーラーのリストが異常です。";
        public const string RunWizLog17 = "Fabric サーバーの起動 jar をダウンロードしています（ローダー ";
        public const string RunWizLog18 = "、インストーラー ";
        public const string RunWizLog19 = "）...";
        public const string RunWizLog20 = "注意：Fabric はこの jar のハッシュを公開していないため、HTTPS のみで保護され、ハッシュ検証はされません。初回起動時にバニラサーバーとライブラリを自らダウンロードします。";
        public const string RunDownloadWithProgress2 = "Fabric サーバー";
        public const string RunFail28 = "Fabric サーバーのダウンロードに失敗しました：";
        public const string RunWizLog21 = "mods フォルダーを作成しました。注意：Fabric サーバーでは通常、mods に Fabric API mod も必要です。また、両側 mod は全プレイヤーのクライアントにも必要です。";
        public const string RunFail29 = "Paper のビルドリストを取得できません（Fill API）：";
        public const string RunFail30 = "。注意：Paper ビルドのないバージョンではここでエラーになります。";
        public const string RunFail31 = "Paper のビルドリストにダウンロード可能なサーバーが見つかりません。";
        public const string RunWizLog22 = "Paper サーバーをダウンロードしています（最新ビルド）...";
        public const string RunDownloadWithProgress3 = "Paper サーバー";
        public const string RunFail32 = "Paper サーバーのダウンロードに失敗しました：";
        public const string RunWizLog23 = "サーバーの SHA256 を検証しています...";
        public const string RunFail33 = "サーバーの SHA256 が一致しません。jar は削除されました。期待値 ";
        public const string RunWizStep3 = "ステップ 3：初期ファイルの書き込み";
        public const string RunWizLog24 = "eula.txt を書き込みました（eula=true、同意済みのため）。";
        public const string RunWizLog25 = "EULA に同意していないため、eula.txt は書き込まれませんでした。初回起動時にサーバーが eula=false を生成して終了します。eula=true に変更してから起動してください。";
        public const string RunWizLog26 = "server.properties を書き込みました：server-port=25565、online-mode=false（オフラインマルチプレイに必須）。";
        public const string RunWizLog27 = "server.properties は既に存在するため、そのままにしました。online-mode=false になっているか自分で確認してください。でないとオフラインアカウントは参加できません。";
        public const string RunWizStep4 = "ステップ 4：設定の保存";
        public const string RunWizLog28 = "完了。サーバーフォルダー：";
        public const string RunWizLog29 = "；jar：server.jar；Java：";
        public const string RunStr2 = "（以前の設定を保持）";
        public const string RunWizLog30 = "ウィザードを閉じ、設定で SakuraFrp アクセスキーとトンネル ID を入力してから、「マルチプレイ開始」をクリックしてください。";
        public const string RunWizStep5 = "インストール完了";
        public const string RunFail34 = "ウィザードエラー：";
        public const string DownloadWithProgressWizStep = "ダウンロード中 ";
        public const string DownloadWithProgressWizStep2 = "... ";
        public const string FailWizLog = "失敗：";
        public const string FailWizStep = "停止";
        public const string CancelledWizLog = "キャンセルしました。設定は保存されていません。完全にダウンロード済みのファイルは対象フォルダーに残り、未完了のファイルは削除されました。";
        public const string CancelledWizStep = "キャンセル済み";
        public const string MainFormAppendServerLog = "[ランチャー] 起動しました。ログはプログラムの隣の logs フォルダーにも保存されます（14 日間保持）。";
        public const string TrayText = "Minecraft マルチプレイヘルパー · 停止中";
        public const string InitTray613ToolStripMenuItem = "メインウィンドウを表示";
        public const string InitTray613ToolStripMenuItem2 = "安全に終了";
        public const string BtnStartText2 = "マルチプレイ開始";
        public const string AddrLblCaption = "公開アドレス（友達に送ってください）： ";
        public const string Hint = "トンネルが開始すると、公開アドレスがここに表示されます";
        public const string BtnCopyText = "友達用にアドレスをコピー";
        public const string BtnPcl2Text = "PCL2 を開く";
        public const string BtnStopText = "サーバーを安全に停止";
        public const string LogToggleLblCaption = "▾  ログ";
        public const string BuildUiStr = "サーバー";
        public const string BuildUiStr2 = "トンネル";
        public const string BuildUiStr3 = "状態";
        public const string BuildUiStr4 = "バックアップ";
        public const string BuildUiMakeLogBox = "まだログはありません · サーバーの出力がここに表示されます";
        public const string BuildUiMakeLogBox2 = "まだログはありません · トンネルの出力がここに表示されます";
        public const string BtnJumpBottomText = "↓ 一番下へ戻る";
        public const string Hint2 = "ログを検索…";
        public const string BuildUiSetToolTip = "前へ";
        public const string BuildUiSetToolTip2 = "次へ";
        public const string BuildUiSetToolTip3 = "ログを検索します。Enter で次の一致へ移動します";
        public const string ChkErrOnlyText = "警告/エラーのみ";
        public const string CmdLblCaption = "サーバーコマンド";
        public const string BtnCmdSendText = "送信";
        public const string BuildUiAppendState = "初回使用時：設定で SakuraFrp アクセスキーとトンネル ID を入力し、PCL2 のパスを確認してください。";
        public const string IsErrLineContains = "[ウォッチドッグ]";
        public const string IsErrLineContains2 = "[バックアップ] 失敗";
        public const string LblFindCaption = "結果なし";
        public const string SrvState2 = "稼働中 · ";
        public const string SampleStatsPart = " 人";
        public const string SampleStatsPart2 = " 人がオンライン（リストは非公開）";
        public const string SampleStatsPart3 = "Ping ";
        public const string ToggleLogStr = "▸  ログ";
        public const string AppendServerLogPersistLog613 = "サーバー";
        public const string AppendTunnelLogPersistLog613 = "トンネル";
        public const string SendServerCommandAppendState = "サーバーが稼働していないため、コマンドを送信できません。";
        public const string SendServerCommandAppendState2 = "コマンドの送信に失敗しました：サーバープロセスが既に終了しています。";
        public const string SendServerCommandAppendState3 = "コマンドの送信に失敗しました：";
        public const string OpenPlayerManagerShow = "サーバーが稼働していません。プレイヤー管理はサーバー起動後に利用できます。";
        public const string OpenPlayerManagerAppendState = "コマンドに失敗しました：サーバープロセスが既に終了しています。";
        public const string DoBackupAppendState = "既にバックアップが進行中です。";
        public const string BackupCoreAppendState = "ワールドフォルダーが見つかりません：";
        public const string BackupCoreAppendState2 = "。バックアップできません。";
        public const string BackupCoreAppendServerLog = "[バックアップ] サーバーの自動保存を一時停止して書き出しました。ワールドを圧縮しています…";
        public const string BackupCoreAppendServerLog2 = "[バックアップ] 完了：";
        public const string BackupCorePart = " ファイル";
        public const string BackupCoreStr = "、スキップ ";
        public const string BackupCorePart2 = " 件の使用中ファイル";
        public const string BackupCoreAppendState3 = "ワールドのバックアップが完了しました（";
        public const string BackupCoreAppendServerLog3 = "[バックアップ] 失敗：";
        public const string BackupCoreAppendState4 = "バックアップに失敗しました：";
        public const string BackupCoreAppendServerLog4 = "[バックアップ] サーバーの自動保存を再開しました。";
        public const string RestoreBackupShow = "復元する前にサーバーを停止しなければなりません。サーバー稼働中にワールドファイルを入れ替えるとセーブが壊れます。\n\n先に「サーバーを安全に停止」をクリックしてから復元してください。";
        public const string RestoreBackupShow2 = "バックアップを復元";
        public const string RestoreBackupConfirmBox = "ワールドをバックアップ「";
        public const string RestoreBackupConfirmBox2 = "」に戻しますか？\n\n現在のワールドは削除されません。先に名前を変更して退避されます（world_prerestore_<time>）。問題がなければ自分で削除できます。復元が間違っていた場合は元に戻せます。";
        public const string RestoreBackupAppendState = "既にバックアップタスクが進行中です。";
        public const string Result = "バックアップ「";
        public const string RestoreBackupPart = "」に復元しました。古いワールドの保存先：";
        public const string RestoreBackupPart2 = "。新しいワールドに問題がなければ削除できます。";
        public const string Result2 = "復元に失敗しました：";
        public const string RestoreBackupPart3 = "（古いワールドは既に名前が変更されている可能性があります。サーバーを起動せず、サーバーフォルダーを確認してください。）";
        public const string RestoreBackupAppendServerLog = "[バックアップ] ";
        public const string BuildBackupPanelBkLabel = "ワールドのバックアップ";
        public const string BtnBackupNowText = "今すぐバックアップ";
        public const string BuildBackupPanelDoBackup = "手動バックアップ";
        public const string BtnRestoreBackupText = "選択したものを復元";
        public const string BuildBackupPanelShow = "まず上のリストからバックアップを選択してください。";
        public const string BtnDeleteBackupText = "選択したものを削除";
        public const string BuildBackupPanelShow2 = "バックアップを削除";
        public const string BuildBackupPanelConfirmBox = "バックアップ「";
        public const string BuildBackupPanelPart = "」を削除しますか？削除すると復元できません。";
        public const string BuildBackupPanelShow3 = "削除に失敗しました：";
        public const string BuildBackupPanelBkLabel2 = "最新のバックアップを ";
        public const string BuildBackupPanelBkLabel3 = " 件保持";
        public const string BuildBackupPanelBkCheck = "クラッシュ時に自動再起動";
        public const string BuildBackupPanelBkCheck2 = "サーバー起動前に自動バックアップ";
        public const string BuildBackupPanelBkCheck3 = "毎日自動バックアップ";
        public const string BuildBackupPanelBkLabel4 = "アプリを開いている間、設定時刻に自動でバックアップします";
        public const string BuildBackupPanelBkCheck4 = "定時コマンド";
        public const string LblBackupStateCaption = "バックアップ中…（大きなワールドでは 1〜2 分かかることがあります）";
        public const string RefreshBackupListStr = "合計 ";
        public const string RefreshBackupListPart = " 件 · ";
        public const string RefreshBackupListStr2 = "まだバックアップがありません";
        public const string RefreshBackupListStr3 = "最新： ";
        public const string RefreshBackupListStr4 = "「今すぐバックアップ」をクリックするとワールドのスナップショットを保存できます。";
        public const string CheckSchedulesAppendServerLog = "[スケジュール] 毎日のバックアップ時刻になりました。自動バックアップを開始します。";
        public const string CheckSchedulesAppendServerLog2 = "[スケジュール] 定時コマンドの時刻ですが、まだ ";
        public const string CheckSchedulesAppendServerLog3 = " 人がオンラインのため、サーバーが空になるまで延期します。";
        public const string CheckSchedulesAppendServerLog4 = "[スケジュール] 定時コマンドを送信しました：";
        public const string ServerText2 = "サーバー：稼働中";
        public const string ServerText3 = "サーバー：起動中…";
        public const string TunnelText2 = "SakuraFrp トンネル：接続済み";
        public const string TunnelText3 = "SakuraFrp トンネル：起動中…";
        public const string AddrText2 = "公開アドレス：準備完了";
        public const string AddrText3 = "公開アドレス：保存済み（トンネル停止中）";
        public const string Tt = "Minecraft マルチプレイヘルパー · ";
        public const string RefreshStatusStr = "稼働中";
        public const string RefreshStatusStr2 = "起動中";
        public const string CopyAddrShow = "公開アドレスがまだ生成されていません。先にマルチプレイを開始してください。";
        public const string CopyAddrShow2 = "通知";
        public const string CopyAddrAppendState = "公開アドレスをクリップボードにコピーしました。友達に送ってください。";
        public const string BtnCopyText2 = "コピーしました ✓";
        public const string OpenPcl2Show = "PCL2 が見つかりません。設定で PCL2 の exe パスを設定してください。";
        public const string OpenPcl2Show2 = "PCL2 を開けませんでした：";
        public const string HandleServerCrashAppendServerLog = "[ウォッチドッグ] サーバーが異常終了しました（終了コード ";
        public const string HandleServerCrashAppendServerLog2 = "）。通常の停止ではありません。";
        public const string HandleServerCrashStr = "考えられる原因：";
        public const string HandleServerCrashAppendState = "サーバーが異常終了しました";
        public const string HandleServerCrashStr2 = "。考えられる原因：";
        public const string HandleServerCrashStr3 = "。原因はサーバーログの末尾を確認してください。";
        public const string HandleServerCrashPart = "（ウォッチドッグの自動再起動はオフです）";
        public const string HandleServerCrashAppendServerLog3 = "[ウォッチドッグ] 10 分以内に ";
        public const string HandleServerCrashAppendServerLog4 = " 回異常終了したため、自動再起動を停止しました。セーブデータか mod の問題の可能性が高いです。";
        public const string HandleServerCrashAppendState2 = "サーバーの異常終了が続いたため、自動再起動を停止しました。ログを確認してください。";
        public const string HandleServerCrashStr4 = " 考えられる原因： ";
        public const string HandleServerCrashAppendState3 = "サーバーが異常終了しました。ウォッチドッグが 5 秒後に自動で再起動します…";
        public const string GuessCrash614Ret = "ポート ";
        public const string GuessCrash614Part = " は既に使用中です。別のサーバーインスタンスが正常に終了していない可能性があります";
        public const string GuessCrash614Ret2 = "メモリ不足、またはメモリ設定（-Xmx）が不適切です";
        public const string GuessCrash614Ret3 = "Java のバージョンが違います。このサーバーには Java 17 が必要です";
        public const string GuessCrash614Ret4 = "サーバーが 60 秒以上フリーズし、ゲーム内蔵のウォッチドッグに停止されました";
        public const string GuessCrash614Ret5 = "mod の読み込みに失敗しました。ログのエラーに名前のある mod を確認してください";
        public const string GuessCrash614Ret6 = "EULA に同意していません";
        public const string OfferWizardIfNeededShow = "利用可能なサーバーがまだありません。設定したサーバーフォルダーが存在しないか、サーバー jar が含まれていません。\n\n「初回セットアップウィザード」は対応する Java とサーバー（バニラ / Fabric / Paper）をダウンロードし、オフラインマルチプレイに必要な EULA と server.properties を書き込みます。ダウンロードはすべてハッシュ検証されます。";
        public const string OfferWizardIfNeededShow2 = "ウィザードを開く";
        public const string OfferWizardIfNeededShow3 = "後で";
        public const string OpenWizardAppendState = "初回セットアップウィザードが完了しました。サーバーと Java の準備ができています。";
        public const string CheckJavaVersionStr = "不明";
        public const string CheckJavaVersionAppendServerLog = "[Java チェック] サーバー jar のバージョン情報（version.json）が読めないため、バージョンチェックをスキップします。現在の Java メジャーバージョン：";
        public const string CheckJavaVersionAppendServerLog2 = "。";
        public const string CheckJavaVersionAppendServerLog3 = "[Java チェック] サーバーバージョン ";
        public const string CheckJavaVersionAppendServerLog4 = " はバージョン表にないため、バージョンチェックをスキップします。現在の Java メジャーバージョン：";
        public const string CheckJavaVersionAppendServerLog5 = "[Java チェック] サーバー ";
        public const string CheckJavaVersionAppendServerLog6 = " ですが、現在の Java バージョンを測定できませんでした。そのまま起動します（サーバーログに注意してください）。";
        public const string CheckJavaVersionAppendServerLog7 = "[Java チェック] 合格：サーバー ";
        public const string CheckJavaVersionAppendServerLog8 = "、現在の Java は ";
        public const string CheckJavaVersionAppendServerLog9 = "[Java チェック] 不一致：サーバー ";
        public const string CheckJavaVersionAppendServerLog10 = " に対し、現在の Java は ";
        public const string CheckJavaVersionAppendServerLog11 = " のみです。ユーザーの判断を待っています。";
        public const string Msg2 = "Java のバージョンが古すぎます。サーバーは起動に失敗する可能性が高いです。\n\nサーバーバージョン：";
        public const string CheckJavaVersionPart = "、必要な Java は ";
        public const string CheckJavaVersionPart2 = " 以上です。使用しようとしている Java のメジャーバージョンは ";
        public const string CheckJavaVersionPart3 = "。\n\n提案：設定で Java ";
        public const string CheckJavaVersionPart4 = " の java.exe に切り替えるか、初回セットアップウィザードに対応する Java を自動でダウンロードさせてください。\n「そのまま起動」を選ぶと現在の Java で起動します。エラーはサーバーログを確認してください。";
        public const string CheckJavaVersionShow = "Java バージョンの不一致";
        public const string CheckJavaVersionShow2 = "そのまま起動";
        public const string CheckJavaVersionShow3 = "起動をキャンセル";
        public const string CheckJavaVersionAppendServerLog12 = "[Java チェック] ユーザーがそのまま起動することを選択しました。";
        public const string CheckJavaVersionAppendServerLog13 = "[Java チェック] ユーザーが起動をキャンセルしました。";
        public const string StartAllAppendState = "サーバーは既に稼働中です。再度起動する必要はありません。";
        public const string BtnStartText3 = "マルチプレイ稼働中";
        public const string BtnStartText4 = "起動中…";
        public const string DoStartFailState = "サーバーフォルダーが存在しません：";
        public const string DoStartFailState2 = "。設定でサーバーフォルダーを確認してください。";
        public const string DoStartFailState3 = "サーバー jar が見つかりません：";
        public const string DoStartFailState4 = "。設定で jar ファイル名を確認してください。";
        public const string DoStartFailState5 = "Java（java.exe）が見つかりません。設定で Java パスを再選択してください。通常は .minecraft\\runtime か Java のインストールフォルダーにあります。";
        public const string DoStartFailState6 = "起動をキャンセルしました：Java のバージョンがサーバーの要件未満です。対応する Java に切り替えるか、初回セットアップウィザードにダウンロードさせてから再試行してください。";
        public const string DoStartFailState7 = " は既に使用中です。前回のサーバーが正常に終了していない可能性があります。「サーバーを安全に停止」をクリックするか、このアプリを再起動してください。タスクマネージャーで Java プロセスを自分で終了しないでください。";
        public const string DoStartAppendState = "サーバー起動前に自動バックアップしています…";
        public const string DoStartAppendState2 = "Minecraft サーバーを起動しています…";
        public const string DoStartAppendServerLog = "[プロセスが終了しました]";
        public const string DoStartFailState8 = "Java プロセスを起動できません。Java パスが正しいか確認してください。";
        public const string DoStartAppendState3 = "サーバーの準備完了を待っています（「Done」を監視中）…";
        public const string DoStartFailState9 = "「Done」が表示される前にサーバープロセスが終了しました。可能性が高いのは：1) Java パスかメモリ引数が間違っている。2) セーブ/ワールドか mods のエラー（サーバーログの末尾を確認）。ワールドや mods には触れないでください。";
        public const string DoStartFailState10 = "90 秒経っても「Done」が表示されません。メモリ不足、ワールドが大きすぎる、または起動が止まっている可能性があります。サーバーログの末尾を確認してください。ワールドや mods には触れないでください。";
        public const string DoStartAppendState4 = "準備完了。「友達用にアドレスをコピー」をクリックして、アドレスを友達に送ってください。";
        public const string DoStartFailState11 = "起動に失敗しました：";
        public const string StartTunnelAppendTunnelLog = "[フォールバック] 公式 SakuraFrp ランチャーを開いています。ランチャー側で「トンネル開始」をクリックしてください。";
        public const string StartTunnelAppendTunnelLog2 = "SakuraFrp ランチャーを開けませんでした：";
        public const string StartTunnelAppendTunnelLog3 = "frpc.exe が見つかりません：";
        public const string StartTunnelAppendTunnelLog4 = "。設定で確認するか、トンネル方式を公式ランチャーに切り替えてください。";
        public const string StartTunnelAppendTunnelLog5 = "アクセスキーが空です。設定で SakuraFrp アクセスキーを入力してください。";
        public const string StartTunnelAppendTunnelLog6 = "トンネル ID が空です。設定で入力してください。";
        public const string StartTunnelAppendState = "SakuraFrp トンネルを開始しています…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc が終了しました]";
        public const string StartTunnelAppendTunnelLog8 = "[ウォッチドッグ] ゲームトンネルが異常終了しました。5 秒後に再接続します。";
        public const string StartTunnelAppendTunnelLog9 = "frpc の起動に失敗しました。frpc.exe のパスを確認してください。";
        public const string StartTunnelAppendTunnelLog10 = "現在の frpc.exe は SakuraFrp ランチャー同梱ビルドです。このビルドは外部プログラムから直接実行できません（SakuraFrp Service 経由でのみ起動可能）ため、すぐに終了します。アクセスキー、トンネル ID、ノードとは無関係です。設定で frpc のパスを単体版 frpc に向けるか、トンネル方式を公式ランチャーに切り替えてください。";
        public const string StartTunnelAppendTunnelLog11 = "frpc プロセスが終了しました。可能性：1) アクセスキーかトンネル ID が間違っている。2) ノードの問題。3) アカウントに本人確認やチェックインが必要。設定でキーとトンネル ID を確認するか、トンネル方式を公式ランチャーに切り替えてください。";
        public const string StartTunnelAppendState2 = "この frpc はランチャー同梱ビルドで、直接実行できません。設定で単体版 frpc か公式ランチャー方式に切り替えてください。";
        public const string StartVoiceTunnelAppendTunnelLog = "[音声] 音声トンネル ID が未設定のため、音声トンネルをスキップします。";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[音声 frpc が終了しました]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[ウォッチドッグ] 音声トンネルが異常終了しました。5 秒後に再接続します。";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[音声] frpc の起動に失敗しました。frpc.exe のパスを確認してください。";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[音声] frpc プロセスが終了しました。音声トンネル ID が正しいか確認してください。";
        public const string StartVoiceTunnelAppendState = "音声トンネルが接続されました。";
        public const string VoiceFrpcOutputAppendTunnelLog = "[音声] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[音声][ERR] ";
        public const string TryParseFrpcAppendState = "公開アドレスを自動で取得しました：";
        public const string ServerText4 = "サーバー：エラー";
        public const string TunnelText4 = "SakuraFrp トンネル：エラー";
        public const string SafeStopAppendState = "サーバーは稼働していません。トンネルを停止しました。";
        public const string SafeStopAppendState2 = "安全に停止しています：サーバーに stop を送信し、セーブを待っています…";
        public const string SafeStopAppendServerLog = ">> stop を送信しました。セーブと終了を待っています（最大 60 秒）";
        public const string SafeStopAppendState3 = "サーバーが正常に停止しました（セーブ完了）。トンネルを閉じています…";
        public const string SafeStopAppendServerLog2 = "[サーバープロセスが正常に終了しました]";
        public const string SafeStopShow = "60 秒経ってもサーバーが終了しません。強制終了すると直近数分間の進行状況が失われる可能性があります。強制終了しますか？";
        public const string SafeStopShow2 = "安全なシャットダウンがタイムアウトしました";
        public const string SafeStopShow3 = "強制終了";
        public const string SafeStopShow4 = "もう少し待つ";
        public const string SafeStopAppendState4 = "サーバープロセスを強制終了しました（直近の進行状況が失われた可能性があります）。";
        public const string SafeStopAppendState5 = "強制終了はキャンセルされました。サーバーはまだ稼働中です。";
        public const string SafeStopAppendState6 = "すべて停止しました：サーバーとトンネルは閉じられました。";
        public const string OnClosingConfirmBox = "サーバーがまだ稼働中です。そのまま閉じるとセーブされない可能性があります。先に安全に停止しますか？";
        public const string BtnSparkTpsText = "TPS レポート";
        public const string BtnSparkProfileText = "30 秒プロファイル";
        public const string SparkLblCaption = "診断";
        public const string SparkProfileStarted = "[診断] 30 秒間のプロファイリングを開始しました。サーバーを稼働させ続けてください...";
        public const string SparkProfileStopped = "[診断] プロファイリングが終了しました。spark レポートのリンクは上のサーバーログに表示されます。";
        public const string SparkProfileCancelled = "[診断] サーバーが停止したため、今回のプロファイリングはキャンセルされました。";
        public const string LangCaption = "表示言語";
        public const string LangRestartHint = "言語を保存しました。適用するにはアプリを再起動してください。";
        public const string LangRestartNow = "今すぐ再起動";
        public const int EulaAgreeWidth = 190;
        public const int EulaLinkDx = 190;
    }

    static class TxtKo
    {
        public const string Msg = "문제가 발생했습니다";
        public const string ShowFriendlyErrorStr = ", 프로그램을 종료해야 합니다.";
        public const string ShowFriendlyErrorStr2 = ", 프로그램은 계속 실행됩니다.";
        public const string ShowFriendlyErrorPart = "\n\n계속 발생하면 이 정보를 Naiwa에게 보내 주세요.";
        public const string ShowFriendlyErrorShow = "Minecraft 멀티플레이 도우미";
        public const string YesText = "서버를 안전하게 중지한 뒤 종료";
        public const string NoText = "취소";
        public const string SettingsFormInitChrome = "설정";
        public const string SettingsFormAddField = "서버 폴더 D:\\MCServer";
        public const string SettingsFormAddField2 = "Java 전체 경로 (java.exe)";
        public const string SettingsFormAddField3 = "jar 파일 이름";
        public const string SettingsFormAddField4 = "메모리 인수";
        public const string SettingsFormAddField5 = "포트";
        public const string SettingsFormAddField6 = "frpc.exe 경로";
        public const string LbModeCaption = "터널 방식";
        public const string SettingsFormAdd = "frpc 명령줄(우선)";
        public const string SettingsFormAdd2 = "공식 SakuraFrp 런처 열기(대체)";
        public const string LbKeyCaption = "SakuraFrp 액세스 키";
        public const string SettingsFormAddField7 = "터널 ID";
        public const string SettingsFormAddField8 = "음성 터널 ID(선택)";
        public const string SettingsFormAddField9 = "공개 주소(선택)";
        public const string SettingsFormAddField10 = "PCL2 exe 경로";
        public const string BtnPropsText = "서버 속성…";
        public const string SettingsFormShow = "먼저 위의 서버 폴더를 올바르게 설정한 뒤에 서버 속성을 편집하세요.";
        public const string SettingsFormShow2 = "서버 속성";
        public const string BtnWizardText = "첫 실행 마법사";
        public const string BtnSaveText = "저장";
        public const string ValidateSettingsMarkInvalid = "서버 폴더가 존재하지 않습니다. 경로를 확인하세요.";
        public const string ValidateSettingsMarkInvalid2 = "Java 경로가 존재하지 않습니다. 확인하세요.";
        public const string ValidateSettingsMarkInvalid3 = "jar 파일 이름은 비워 둘 수 없습니다.";
        public const string ValidateSettingsMarkInvalid4 = "서버 폴더에서 해당 jar 파일을 찾을 수 없습니다.";
        public const string ValidateSettingsMarkInvalid5 = "포트는 1에서 65535 사이의 숫자여야 합니다.";
        public const string ValidateSettingsMarkInvalid6 = "frpc.exe 경로가 존재하지 않습니다. 확인하세요.";
        public const string ValidateSettingsMarkInvalid7 = "frpc 모드에서는 터널 ID를 비워 둘 수 없습니다.";
        public const string ValidateSettingsMarkInvalid8 = "PCL2 경로가 존재하지 않습니다. 확인하세요.";
        public const string TitleText = "Minecraft 멀티플레이 도우미 v6.22.5";
        public const string SubText = "듀얼 터널 · 게임 + 음성";
        public const string ServerText = "서버: 실행 중 아님";
        public const string TunnelText = "SakuraFrp 터널: 실행 중 아님";
        public const string AddrText = "공개 주소: 생성 안 됨";
        public const string SrvState = "실행 중 아님";
        public const string MbText = "플레이어";
        public const string OnPaintHeader = "서버 프로세스";
        public const string OnPaintRow = "상태";
        public const string OnPaintRow2 = "메모리 사용량";
        public const string OnPaintRow3 = "플레이어 연결";
        public const string OnPaintDrawText = "온라인 플레이어";
        public const string OnPaintRow4 = "버전 · Ping";
        public const string OnPaintHeader2 = "터널 프로세스(frpc)";
        public const string OnPaintHeader3 = "로컬";
        public const string OnPaintDrawText2 = "다운로드";
        public const string OnPaintDrawText3 = "업로드";
        public const string OnPaintRow5 = "시스템 메모리";
        public const string OnHandleCreatedIndexOf = "오류";
        public const string OnHandleCreatedStr = "오류";
        public const string ShowMsg = "확인";
        public const string PropsFormAddCombo = "난이도";
        public const string PropsFormStr = "평화로움";
        public const string PropsFormStr2 = "쉬움";
        public const string PropsFormStr3 = "보통";
        public const string PropsFormStr4 = "어려움";
        public const string PropsFormAddCombo2 = "게임 모드";
        public const string PropsFormStr5 = "서바이벌";
        public const string PropsFormStr6 = "크리에이티브";
        public const string PropsFormStr7 = "어드벤처";
        public const string PropsFormStr8 = "관전";
        public const string PropsFormAddNum = "최대 플레이어 수";
        public const string PropsFormAddNum2 = "시야 거리(청크)";
        public const string PropsFormAddNum3 = "시뮬레이션 거리(청크)";
        public const string PropsFormAddNum4 = "스폰 보호 반경";
        public const string PropsFormAddText = "서버 MOTD";
        public const string PropsFormAddCheck = "화이트리스트 사용(white-list)";
        public const string PropsFormAddCheck2 = "PvP 허용(pvp)";
        public const string PropsFormAddCheck3 = "비행 허용(allow-flight)";
        public const string LbOnCaption = "온라인 모드: 끔(오프라인 멀티플레이에서는 꺼진 상태를 유지해야 하며, 여기에서 변경할 수 없습니다)";
        public const string PropsFormShow = "서버 폴더에서 server.properties를 찾을 수 없습니다. 이 파일은 서버가 처음 정상적으로 시작될 때 만들어집니다.";
        public const string SavePropsShow = "최대 플레이어 수는 1에서 9999 사이의 숫자여야 합니다.";
        public const string SavePropsShow2 = "시야 거리는 2에서 32 사이의 숫자여야 합니다.";
        public const string SavePropsShow3 = "시뮬레이션 거리는 2에서 32 사이의 숫자여야 합니다.";
        public const string SavePropsShow4 = "스폰 보호 반경은 0에서 9999 사이의 숫자여야 합니다.";
        public const string SavePropsShow5 = "저장 실패: ";
        public const string SavePropsShow6 = "저장했습니다. 서버가 실행 중이므로 이 설정은 다음 시작 때 적용됩니다.";
        public const string PlayerFormStr = "온라인 플레이어(";
        public const string PlayerFormStr2 = "현재 온라인인 플레이어가 없습니다";
        public const string PlayerFormAddBtn = "내 위치로 텔레포트";
        public const string PlayerFormAddBtn2 = "서버에서 추방";
        public const string PlayerFormConfirmDo = "";
        public const string PlayerFormConfirmDo2 = "을(를) 서버에서 추방할까요?";
        public const string PlayerFormAddBtn3 = "OP 부여";
        public const string PlayerFormAddBtn4 = "OP 해제";
        public const string PlayerFormAddBtn5 = "차단";
        public const string PlayerFormConfirmDo3 = "";
        public const string PlayerFormConfirmDo4 = "을(를) 차단할까요? 차단하면 서버에 접속할 수 없습니다(나중에 해제할 수 있습니다).";
        public const string PlayerFormAddBtn6 = "차단 해제";
        public const string PlayerFormStr3 = "참고: 본인이 온라인이 아닐 때는 '내 위치로 텔레포트'를 사용할 수 없습니다.";
        public const string PlayerFormStr4 = "동작은 콘솔 명령으로 서버에 전송되며, 명령 상자에 입력하는 것과 같습니다.";
        public const string CloseText = "닫기";
        public const string LbOpsCaption = "OP 목록: ";
        public const string RefreshListsStr = "(없음)";
        public const string LbBannedCaption = "차단 목록: ";
        public const string WizardFormInitChrome = "첫 실행 설정 마법사";
        public const string WizardFormAddLabel = "Minecraft 버전";
        public const string WizardFormAddHint = "정식 버전이면 자유롭게 입력할 수 있습니다";
        public const string WizardFormAddLabel2 = "서버 종류";
        public const string WizardFormAddRadio = "바닐라";
        public const string WizardFormAddLabel3 = "서버 폴더";
        public const string BtnBrowseText = "찾아보기...";
        public const string Description = "서버 폴더를 선택하세요(아직 존재하지 않아도 됩니다. 상위 폴더를 고른 뒤 경로 텍스트를 편집하세요)";
        public const string ChkEulaText = "Minecraft EULA에 동의합니다";
        public const string LnkEulaText = "EULA 전문 읽기";
        public const string WizardFormAddHint2 = "체크하지 않아도 설치할 수 있지만, 처음 시작하기 전에 직접 eula.txt를 eula=true로 바꿔야 합니다";
        public const string LblStepCaption = "위 세 항목을 입력하고 설치 시작을 클릭하세요. Java와 서버는 자동으로 다운로드되고 검증됩니다.";
        public const string BtnStartText = "설치 시작";
        public const string WizardFormWizLog = "취소 중...";
        public const string StartInstallShow = "먼저 Minecraft 버전을 입력하세요.";
        public const string StartInstallShow2 = "정식 버전 번호가 아닌 것 같습니다. 1.20.1처럼 입력하세요(스냅샷은 마법사에서 지원하지 않습니다).";
        public const string StartInstallShow3 = "먼저 서버 폴더를 입력하세요.";
        public const string StartInstallShow4 = "대상 폴더에 이미 server.jar가 있습니다. 계속하면 덮어씁니다.\n\n월드와 mods는 건드리지 않습니다(마법사는 jar를 교체하고 부족한 파일만 추가합니다). 계속할까요?";
        public const string StartInstallShow5 = "덮어쓰고 계속";
        public const string StartInstallWizLog = "-- 설치 시작: Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "Minecraft ";
        public const string RunFail2 = "에 필요한 Java를 판단할 수 없습니다. 마법사를 중단했습니다(아무것도 변경되지 않았습니다).";
        public const string RunWizLog = "참고: 26.x는 Mojang의 새로운 연도 기반 버전 표기입니다. 공식 Java 요구 사항이 여기서 가정한 Java 21보다 높을 수 있으니, 설치 후 서버 로그를 확인하세요.";
        public const string RunWizStep = "1단계: Java 준비 ";
        public const string RunWizLog2 = "설정된 Java의 주 버전은 ";
        public const string RunWizLog3 = "이며 요구 사항을 충족합니다. 그대로 사용합니다: ";
        public const string RunWizLog4 = "앱 옆에 이전에 다운로드한 Java를 사용합니다: ";
        public const string RunFail3 = "에는 Java ";
        public const string RunFail4 = "가 필요하지만, 이 마법사가 자동으로 다운로드할 수 있는 것은 Java 17 / 21뿐입니다. Java ";
        public const string RunFail5 = "를 수동으로 설치하고 설정에서 java.exe를 지정한 뒤 서버를 시작하세요.";
        public const string RunWizLog5 = "Adoptium 자산 API에서 직접 링크를 얻지 못했습니다(";
        public const string RunStr = "상세 없음";
        public const string RunPart = "). 체크섬을 제공하지 않는 공식 바이너리 엔드포인트로 대체합니다. 이번 Java 다운로드는 해시 검증되지 않습니다.";
        public const string RunWizLog6 = "Java 다운로드 중 ";
        public const string RunWizLog7 = " (Eclipse Temurin JRE, 약 40-50 MB)...";
        public const string RunFail6 = "Java 다운로드 실패: ";
        public const string RunWizLog8 = "Java zip의 SHA256을 검증 중...";
        public const string RunFail7 = "Java zip의 SHA256이 일치하지 않습니다. 다운로드를 삭제했습니다. 예상 ";
        public const string RunFail8 = ", 실제 ";
        public const string RunFail9 = ". 네트워크를 확인하고 다시 시도하세요.";
        public const string RunWizLog9 = "SHA256 검증 완료.";
        public const string RunWizLog10 = "Java를 다음 위치에 압축 해제 중 ";
        public const string RunWizLog11 = " ...";
        public const string RunFail10 = "Java 압축 해제 실패: ";
        public const string RunFail11 = "압축 해제가 끝났지만 bin\\java.exe를 찾지 못했습니다. 예상과 다른 zip 구조라 중단했습니다(설정이 저장되지 않았습니다).";
        public const string RunFail12 = "다운로드한 Java의 주 버전은 ";
        public const string RunFail13 = "이며 필요한 ";
        public const string RunFail14 = "보다 낮습니다. 중단했습니다(설정이 저장되지 않았습니다).";
        public const string RunWizLog12 = "Java 준비 완료: 주 버전 ";
        public const string RunWizLog13 = ", ";
        public const string RunWizStep2 = "2단계: 서버 jar 다운로드";
        public const string RunFail15 = "Mojang 버전 매니페스트를 가져올 수 없습니다: ";
        public const string RunFail16 = "버전 매니페스트에 Minecraft ";
        public const string RunFail17 = "가 없습니다(번호를 확인하세요. 정식 버전만 해당).";
        public const string RunFail18 = "버전 메타데이터를 가져올 수 없습니다 ";
        public const string RunFail19 = ": ";
        public const string RunFail20 = "버전 메타데이터에 서버 다운로드가 없습니다.";
        public const string RunWizLog14 = "바닐라 서버를 다운로드 중입니다(Mojang 공식)...";
        public const string RunDownloadWithProgress = "바닐라 서버";
        public const string RunFail21 = "서버 다운로드 실패: ";
        public const string RunWizLog15 = "서버 SHA1을 검증 중...";
        public const string RunFail22 = "서버 SHA1이 일치하지 않습니다. jar를 삭제했습니다. 예상 ";
        public const string RunWizLog16 = "SHA1 검증 완료.";
        public const string RunFail23 = "Fabric 로더 목록을 가져올 수 없습니다: ";
        public const string RunFail24 = "Fabric에는 아직 Minecraft ";
        public const string RunFail25 = "용 안정 로더가 없습니다. 다른 버전을 선택하세요.";
        public const string RunFail26 = "Fabric 인스톨러 목록을 가져올 수 없습니다: ";
        public const string RunFail27 = "Fabric 인스톨러 목록이 올바르지 않습니다.";
        public const string RunWizLog17 = "Fabric 서버 실행 jar를 다운로드 중입니다(로더 ";
        public const string RunWizLog18 = ", 인스톨러 ";
        public const string RunWizLog19 = ")...";
        public const string RunWizLog20 = "참고: Fabric은 이 jar의 해시를 공개하지 않으므로 HTTPS로만 보호되며 해시 검증되지 않습니다. 처음 시작할 때 바닐라 서버와 라이브러리를 스스로 다운로드합니다.";
        public const string RunDownloadWithProgress2 = "Fabric 서버";
        public const string RunFail28 = "Fabric 서버 다운로드 실패: ";
        public const string RunWizLog21 = "mods 폴더를 만들었습니다. 참고: Fabric 서버에는 보통 mods에 Fabric API mod도 필요하며, 양쪽 mod는 모든 플레이어의 클라이언트에도 필요합니다.";
        public const string RunFail29 = "Paper 빌드 목록을 가져올 수 없습니다(Fill API): ";
        public const string RunFail30 = ". 참고: Paper 빌드가 없는 버전은 여기서 오류가 반환됩니다.";
        public const string RunFail31 = "Paper 빌드 목록에서 다운로드 가능한 서버를 찾지 못했습니다.";
        public const string RunWizLog22 = "Paper 서버를 다운로드 중입니다(최신 빌드)...";
        public const string RunDownloadWithProgress3 = "Paper 서버";
        public const string RunFail32 = "Paper 서버 다운로드 실패: ";
        public const string RunWizLog23 = "서버 SHA256을 검증 중...";
        public const string RunFail33 = "서버 SHA256이 일치하지 않습니다. jar를 삭제했습니다. 예상 ";
        public const string RunWizStep3 = "3단계: 초기 파일 쓰기";
        public const string RunWizLog24 = "eula.txt를 썼습니다(eula=true, 동의하신 대로).";
        public const string RunWizLog25 = "EULA에 동의하지 않아 eula.txt를 쓰지 않았습니다. 처음 시작하면 서버가 eula=false를 만들고 종료합니다. eula=true로 바꾼 뒤 시작하세요.";
        public const string RunWizLog26 = "server.properties를 썼습니다: server-port=25565, online-mode=false(오프라인 멀티플레이에 필수).";
        public const string RunWizLog27 = "server.properties가 이미 있어 그대로 두었습니다. online-mode=false인지 직접 확인하세요. 그렇지 않으면 오프라인 계정이 접속할 수 없습니다.";
        public const string RunWizStep4 = "4단계: 설정 저장";
        public const string RunWizLog28 = "완료. 서버 폴더: ";
        public const string RunWizLog29 = "; jar: server.jar; Java: ";
        public const string RunStr2 = "(이전 설정 유지)";
        public const string RunWizLog30 = "마법사를 닫고 설정에서 SakuraFrp 액세스 키와 터널 ID를 입력한 다음 멀티플레이 시작을 클릭하세요.";
        public const string RunWizStep5 = "설치 완료";
        public const string RunFail34 = "마법사 오류: ";
        public const string DownloadWithProgressWizStep = "다운로드 ";
        public const string DownloadWithProgressWizStep2 = "... ";
        public const string FailWizLog = "실패: ";
        public const string FailWizStep = "중지됨";
        public const string CancelledWizLog = "취소되었습니다. 설정이 저장되지 않았습니다. 완전히 다운로드된 파일은 대상 폴더에 남고, 부분 파일은 정리되었습니다.";
        public const string CancelledWizStep = "취소됨";
        public const string MainFormAppendServerLog = "[런처] 시작했습니다. 로그는 프로그램 옆의 logs 폴더에도 저장됩니다(14일간 보관).";
        public const string TrayText = "Minecraft 멀티플레이 도우미 · 중지됨";
        public const string InitTray613ToolStripMenuItem = "메인 창 표시";
        public const string InitTray613ToolStripMenuItem2 = "안전하게 종료";
        public const string BtnStartText2 = "멀티플레이 시작";
        public const string AddrLblCaption = "공개 주소(친구에게 보내세요): ";
        public const string Hint = "터널이 시작되면 공개 주소가 여기에 표시됩니다";
        public const string BtnCopyText = "친구에게 보낼 주소 복사";
        public const string BtnPcl2Text = "PCL2 열기";
        public const string BtnStopText = "서버 안전하게 중지";
        public const string LogToggleLblCaption = "▾  로그";
        public const string BuildUiStr = "서버";
        public const string BuildUiStr2 = "터널";
        public const string BuildUiStr3 = "상태";
        public const string BuildUiStr4 = "백업";
        public const string BuildUiMakeLogBox = "아직 로그가 없습니다 · 서버 출력이 여기에 표시됩니다";
        public const string BuildUiMakeLogBox2 = "아직 로그가 없습니다 · 터널 출력이 여기에 표시됩니다";
        public const string BtnJumpBottomText = "↓ 맨 아래로 돌아가기";
        public const string Hint2 = "로그 검색…";
        public const string BuildUiSetToolTip = "이전";
        public const string BuildUiSetToolTip2 = "다음";
        public const string BuildUiSetToolTip3 = "로그를 검색합니다. Enter를 누르면 다음 일치로 이동합니다";
        public const string ChkErrOnlyText = "경고/오류만";
        public const string CmdLblCaption = "서버 명령";
        public const string BtnCmdSendText = "전송";
        public const string BuildUiAppendState = "처음 사용 시: 설정에서 SakuraFrp 액세스 키와 터널 ID를 입력하고 PCL2 경로를 확인하세요.";
        public const string IsErrLineContains = "[워치독]";
        public const string IsErrLineContains2 = "[백업] 실패";
        public const string LblFindCaption = "결과 없음";
        public const string SrvState2 = "실행 중 · ";
        public const string SampleStatsPart = "명";
        public const string SampleStatsPart2 = "명이 온라인(목록 비공개)";
        public const string SampleStatsPart3 = "Ping ";
        public const string ToggleLogStr = "▸  로그";
        public const string AppendServerLogPersistLog613 = "서버";
        public const string AppendTunnelLogPersistLog613 = "터널";
        public const string SendServerCommandAppendState = "서버가 실행 중이 아니어서 명령을 보낼 수 없습니다.";
        public const string SendServerCommandAppendState2 = "명령 전송 실패: 서버 프로세스가 이미 종료되었습니다.";
        public const string SendServerCommandAppendState3 = "명령 전송 실패: ";
        public const string OpenPlayerManagerShow = "서버가 실행 중이 아닙니다. 플레이어 관리는 서버 시작 후 사용할 수 있습니다.";
        public const string OpenPlayerManagerAppendState = "명령 실패: 서버 프로세스가 이미 종료되었습니다.";
        public const string DoBackupAppendState = "이미 백업이 진행 중입니다.";
        public const string BackupCoreAppendState = "월드 폴더를 찾을 수 없습니다: ";
        public const string BackupCoreAppendState2 = ", 백업할 수 없습니다.";
        public const string BackupCoreAppendServerLog = "[백업] 서버 자동 저장을 일시 중지하고 디스크에 기록했습니다. 월드를 압축하는 중…";
        public const string BackupCoreAppendServerLog2 = "[백업] 완료: ";
        public const string BackupCorePart = "개 파일";
        public const string BackupCoreStr = ", 건너뜀 ";
        public const string BackupCorePart2 = "개의 사용 중인 파일";
        public const string BackupCoreAppendState3 = "월드 백업 완료(";
        public const string BackupCoreAppendServerLog3 = "[백업] 실패: ";
        public const string BackupCoreAppendState4 = "백업 실패: ";
        public const string BackupCoreAppendServerLog4 = "[백업] 서버 자동 저장을 다시 시작했습니다.";
        public const string RestoreBackupShow = "복원하기 전에 서버를 중지해야 합니다. 서버가 실행 중일 때 월드 파일을 교체하면 세이브가 손상됩니다.\n\n먼저 '서버 안전하게 중지'를 클릭한 뒤 복원하세요.";
        public const string RestoreBackupShow2 = "백업 복원";
        public const string RestoreBackupConfirmBox = "월드를 백업 「";
        public const string RestoreBackupConfirmBox2 = "」로 복원할까요?\n\n현재 월드는 삭제되지 않습니다. 먼저 이름을 바꿔 따로 보관합니다(world_prerestore_<시간>). 문제가 없으면 직접 삭제하세요. 복원이 잘못되었으면 되돌릴 수 있습니다.";
        public const string RestoreBackupAppendState = "이미 백업 작업이 진행 중입니다.";
        public const string Result = "백업 「";
        public const string RestoreBackupPart = "」로 복원했습니다. 이전 월드는 다음 위치에 보관했습니다: ";
        public const string RestoreBackupPart2 = "; 새 월드가 문제없으면 삭제할 수 있습니다.";
        public const string Result2 = "복원 실패: ";
        public const string RestoreBackupPart3 = "(이전 월드의 이름이 이미 바뀌었을 수 있습니다. 서버를 시작하지 말고 서버 폴더를 확인하세요.)";
        public const string RestoreBackupAppendServerLog = "[백업] ";
        public const string BuildBackupPanelBkLabel = "월드 백업";
        public const string BtnBackupNowText = "지금 백업";
        public const string BuildBackupPanelDoBackup = "수동 백업";
        public const string BtnRestoreBackupText = "선택 항목 복원";
        public const string BuildBackupPanelShow = "먼저 위 목록에서 백업을 선택하세요.";
        public const string BtnDeleteBackupText = "선택 항목 삭제";
        public const string BuildBackupPanelShow2 = "백업 삭제";
        public const string BuildBackupPanelConfirmBox = "백업 「";
        public const string BuildBackupPanelPart = "」을(를) 삭제할까요? 삭제하면 복구할 수 없습니다.";
        public const string BuildBackupPanelShow3 = "삭제 실패: ";
        public const string BuildBackupPanelBkLabel2 = "최근 ";
        public const string BuildBackupPanelBkLabel3 = "개 유지";
        public const string BuildBackupPanelBkCheck = "충돌 시 자동 재시작";
        public const string BuildBackupPanelBkCheck2 = "서버 시작 전 자동 백업";
        public const string BuildBackupPanelBkCheck3 = "매일 자동 백업";
        public const string BuildBackupPanelBkLabel4 = "앱이 열려 있는 동안 설정한 시각에 자동으로 한 번 백업합니다";
        public const string BuildBackupPanelBkCheck4 = "예약 명령";
        public const string LblBackupStateCaption = "백업 중…(큰 월드는 1~2분 걸릴 수 있습니다)";
        public const string RefreshBackupListStr = "총 ";
        public const string RefreshBackupListPart = "개 · ";
        public const string RefreshBackupListStr2 = "아직 백업이 없습니다";
        public const string RefreshBackupListStr3 = "최근: ";
        public const string RefreshBackupListStr4 = "'지금 백업'을 클릭하면 월드의 스냅샷을 남길 수 있습니다.";
        public const string CheckSchedulesAppendServerLog = "[예약] 매일 백업 시간이 되어 자동 백업을 시작합니다.";
        public const string CheckSchedulesAppendServerLog2 = "[예약] 예약 명령 시간이 되었지만 아직 ";
        public const string CheckSchedulesAppendServerLog3 = "명이 온라인이라 서버가 빌 때까지 연기합니다.";
        public const string CheckSchedulesAppendServerLog4 = "[예약] 예약 명령을 전송했습니다: ";
        public const string ServerText2 = "서버: 실행 중";
        public const string ServerText3 = "서버: 시작 중…";
        public const string TunnelText2 = "SakuraFrp 터널: 연결됨";
        public const string TunnelText3 = "SakuraFrp 터널: 시작 중…";
        public const string AddrText2 = "공개 주소: 준비 완료";
        public const string AddrText3 = "공개 주소: 저장됨(터널 실행 중 아님)";
        public const string Tt = "Minecraft 멀티플레이 도우미 · ";
        public const string RefreshStatusStr = "실행 중";
        public const string RefreshStatusStr2 = "시작 중";
        public const string CopyAddrShow = "공개 주소가 아직 생성되지 않았습니다. 먼저 멀티플레이를 시작하세요.";
        public const string CopyAddrShow2 = "알림";
        public const string CopyAddrAppendState = "공개 주소를 클립보드에 복사했습니다. 친구에게 보내세요.";
        public const string BtnCopyText2 = "복사됨 ✓";
        public const string OpenPcl2Show = "PCL2를 찾을 수 없습니다. 설정에서 PCL2의 exe 경로를 지정하세요.";
        public const string OpenPcl2Show2 = "PCL2 열기 실패: ";
        public const string HandleServerCrashAppendServerLog = "[워치독] 서버가 비정상 종료되었습니다(종료 코드 ";
        public const string HandleServerCrashAppendServerLog2 = "). 정상 종료가 아닙니다.";
        public const string HandleServerCrashStr = "가능한 원인: ";
        public const string HandleServerCrashAppendState = "서버가 비정상 종료되었습니다";
        public const string HandleServerCrashStr2 = ", 가능한 원인: ";
        public const string HandleServerCrashStr3 = ". 원인은 서버 로그 맨 아래를 확인하세요.";
        public const string HandleServerCrashPart = "(워치독 자동 재시작이 꺼져 있습니다)";
        public const string HandleServerCrashAppendServerLog3 = "[워치독] 10분 안에 ";
        public const string HandleServerCrashAppendServerLog4 = "회 비정상 종료되어 자동 재시작을 중단했습니다. 세이브나 mod 문제일 가능성이 큽니다.";
        public const string HandleServerCrashAppendState2 = "서버가 계속 비정상 종료되어 자동 재시작을 중단했습니다. 로그를 확인하세요.";
        public const string HandleServerCrashStr4 = " 가능한 원인: ";
        public const string HandleServerCrashAppendState3 = "서버가 비정상 종료되었습니다. 워치독이 5초 후에 자동으로 재시작합니다…";
        public const string GuessCrash614Ret = "포트 ";
        public const string GuessCrash614Part = "이(가) 이미 사용 중입니다. 다른 서버 인스턴스가 깨끗이 종료되지 않았을 가능성이 큽니다";
        public const string GuessCrash614Ret2 = "메모리가 부족하거나 메모리 설정(-Xmx)이 잘못되었습니다";
        public const string GuessCrash614Ret3 = "Java 버전이 잘못되었습니다. 이 서버에는 Java 17이 필요합니다";
        public const string GuessCrash614Ret4 = "서버가 60초 넘게 멈춰 게임 내장 워치독에 의해 중지되었습니다";
        public const string GuessCrash614Ret5 = "mod 로딩에 실패했습니다. 로그 오류에 이름이 나온 mod를 찾으세요";
        public const string GuessCrash614Ret6 = "EULA에 동의하지 않았습니다";
        public const string OfferWizardIfNeededShow = "아직 사용할 수 있는 서버가 없습니다. 설정한 서버 폴더가 존재하지 않거나 서버 jar가 없습니다.\n\n'첫 실행 설정 마법사'는 맞는 Java와 서버(바닐라 / Fabric / Paper)를 다운로드하고, 오프라인 플레이에 필요한 EULA와 server.properties를 써 줍니다. 다운로드는 모두 해시 검증됩니다.";
        public const string OfferWizardIfNeededShow2 = "마법사 열기";
        public const string OfferWizardIfNeededShow3 = "나중에";
        public const string OpenWizardAppendState = "첫 실행 설정 마법사가 완료되었습니다. 서버와 Java가 준비됐습니다.";
        public const string CheckJavaVersionStr = "알 수 없음";
        public const string CheckJavaVersionAppendServerLog = "[Java 검사] 서버 jar의 버전 정보(version.json)를 읽을 수 없어 버전 검사를 건너뜁니다. 현재 Java 주 버전: ";
        public const string CheckJavaVersionAppendServerLog2 = ".";
        public const string CheckJavaVersionAppendServerLog3 = "[Java 검사] 서버 버전 ";
        public const string CheckJavaVersionAppendServerLog4 = "이(가) 버전 표에 없어 버전 검사를 건너뜁니다. 현재 Java 주 버전: ";
        public const string CheckJavaVersionAppendServerLog5 = "[Java 검사] 서버 ";
        public const string CheckJavaVersionAppendServerLog6 = ", 현재 Java 버전을 측정하지 못했습니다. 일단 시작합니다(서버 로그를 확인하세요).";
        public const string CheckJavaVersionAppendServerLog7 = "[Java 검사] 통과: 서버 ";
        public const string CheckJavaVersionAppendServerLog8 = ", 현재 Java는 ";
        public const string CheckJavaVersionAppendServerLog9 = "[Java 검사] 불일치: 서버 ";
        public const string CheckJavaVersionAppendServerLog10 = ", 현재 Java는 ";
        public const string CheckJavaVersionAppendServerLog11 = "뿐입니다. 사용자 결정을 기다리는 중입니다.";
        public const string Msg2 = "Java 버전이 너무 낮아 서버가 시작되지 않을 가능성이 큽니다.\n\n서버 버전: ";
        public const string CheckJavaVersionPart = ", 필요한 Java는 ";
        public const string CheckJavaVersionPart2 = " 이상입니다. 사용하려는 Java의 주 버전은 ";
        public const string CheckJavaVersionPart3 = ".\n\n제안: 설정에서 Java ";
        public const string CheckJavaVersionPart4 = "의 java.exe로 바꾸거나, 첫 실행 설정 마법사가 맞는 Java를 자동으로 다운로드하게 하세요.\n'그냥 시작'을 선택하면 현재 Java로 시작합니다. 오류는 서버 로그를 확인하세요.";
        public const string CheckJavaVersionShow = "Java 버전 불일치";
        public const string CheckJavaVersionShow2 = "그냥 시작";
        public const string CheckJavaVersionShow3 = "시작 취소";
        public const string CheckJavaVersionAppendServerLog12 = "[Java 검사] 사용자가 그냥 시작하기로 했습니다.";
        public const string CheckJavaVersionAppendServerLog13 = "[Java 검사] 사용자가 시작을 취소했습니다.";
        public const string StartAllAppendState = "서버가 이미 실행 중입니다. 다시 시작할 필요가 없습니다.";
        public const string BtnStartText3 = "멀티플레이 실행 중";
        public const string BtnStartText4 = "시작 중…";
        public const string DoStartFailState = "서버 폴더가 존재하지 않습니다: ";
        public const string DoStartFailState2 = ". 설정에서 서버 폴더를 확인하세요.";
        public const string DoStartFailState3 = "서버 jar를 찾을 수 없습니다: ";
        public const string DoStartFailState4 = ". 설정에서 jar 파일 이름을 확인하세요.";
        public const string DoStartFailState5 = "Java(java.exe)를 찾을 수 없습니다. 설정에서 Java 경로를 다시 선택하세요. 보통 .minecraft\\runtime 아래나 Java 설치 폴더에 있습니다.";
        public const string DoStartFailState6 = "시작이 취소되었습니다: Java 버전이 서버 요구보다 낮습니다. 맞는 Java로 바꾸거나 첫 실행 설정 마법사가 다운로드하게 한 뒤 다시 시도하세요.";
        public const string DoStartFailState7 = "이(가) 이미 사용 중입니다. 이전 서버가 깨끗이 종료되지 않았을 수 있습니다. '서버 안전하게 중지'를 클릭하거나 이 앱을 재시작하세요. 작업 관리자에서 Java 프로세스를 직접 종료하지 마세요.";
        public const string DoStartAppendState = "서버 시작 전 자동 백업 중…";
        public const string DoStartAppendState2 = "Minecraft 서버를 시작하는 중…";
        public const string DoStartAppendServerLog = "[프로세스 종료됨]";
        public const string DoStartFailState8 = "Java 프로세스를 시작할 수 없습니다. Java 경로가 올바른지 확인하세요.";
        public const string DoStartAppendState3 = "서버가 준비되기를 기다리는 중(\"Done\" 확인 중)…";
        public const string DoStartFailState9 = "\"Done\"이 나타나기 전에 서버 프로세스가 종료되었습니다. 가능성이 큰 순서: 1) Java 경로 또는 메모리 인수가 잘못됨. 2) 세이브/월드 또는 mods 오류(서버 로그 맨 아래 확인). 월드와 mods는 건드리지 마세요.";
        public const string DoStartFailState10 = "90초가 지나도 \"Done\"이 나타나지 않았습니다. 메모리 부족, 월드가 너무 큼, 시작이 멈춤 중 하나일 수 있습니다. 서버 로그 맨 아래를 확인하세요. 월드와 mods는 건드리지 마세요.";
        public const string DoStartAppendState4 = "준비 완료. '친구에게 보낼 주소 복사'를 클릭해 친구에게 주소를 보내세요.";
        public const string DoStartFailState11 = "시작 실패: ";
        public const string StartTunnelAppendTunnelLog = "[대체] 공식 SakuraFrp 런처를 여는 중입니다. 런처에서 터널 시작을 클릭하세요.";
        public const string StartTunnelAppendTunnelLog2 = "SakuraFrp 런처 열기 실패: ";
        public const string StartTunnelAppendTunnelLog3 = "frpc.exe를 찾을 수 없습니다: ";
        public const string StartTunnelAppendTunnelLog4 = ". 설정에서 확인하거나 터널 방식을 공식 런처로 전환하세요.";
        public const string StartTunnelAppendTunnelLog5 = "액세스 키가 비어 있습니다. 설정에서 SakuraFrp 액세스 키를 입력하세요.";
        public const string StartTunnelAppendTunnelLog6 = "터널 ID가 비어 있습니다. 설정에서 입력하세요.";
        public const string StartTunnelAppendState = "SakuraFrp 터널을 시작하는 중…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc 종료됨]";
        public const string StartTunnelAppendTunnelLog8 = "[워치독] 게임 터널이 비정상 종료되었습니다. 5초 후 다시 연결합니다.";
        public const string StartTunnelAppendTunnelLog9 = "frpc 시작에 실패했습니다. frpc.exe 경로를 확인하세요.";
        public const string StartTunnelAppendTunnelLog10 = "현재 frpc.exe는 SakuraFrp 런처에 포함된 번들 빌드입니다. 이 빌드는 외부 프로그램이 직접 실행할 수 없고(SakuraFrp Service를 통해서만 실행 가능) 바로 종료됩니다. 액세스 키, 터널 ID, 노드와는 무관합니다. 설정에서 frpc 경로를 독립형 frpc로 지정하거나 터널 방식을 공식 런처로 전환하세요.";
        public const string StartTunnelAppendTunnelLog11 = "frpc 프로세스가 종료되었습니다. 가능성: 1) 액세스 키나 터널 ID가 잘못됨. 2) 노드 문제. 3) 계정에 인증이나 출석이 필요함. 설정에서 키와 터널 ID를 확인하거나 터널 방식을 공식 런처로 전환하세요.";
        public const string StartTunnelAppendState2 = "이 frpc는 런처 번들 빌드라 직접 실행할 수 없습니다. 설정에서 독립형 frpc나 공식 런처 방식으로 전환하세요.";
        public const string StartVoiceTunnelAppendTunnelLog = "[음성] 음성 터널 ID가 설정되지 않아 음성 터널을 건너뜁니다.";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[음성 frpc 종료됨]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[워치독] 음성 터널이 비정상 종료되었습니다. 5초 후 다시 연결합니다.";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[음성] frpc 시작에 실패했습니다. frpc.exe 경로를 확인하세요.";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[음성] frpc 프로세스가 종료되었습니다. 음성 터널 ID가 올바른지 확인하세요.";
        public const string StartVoiceTunnelAppendState = "음성 터널이 연결되었습니다.";
        public const string VoiceFrpcOutputAppendTunnelLog = "[음성] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[음성][ERR] ";
        public const string TryParseFrpcAppendState = "공개 주소를 자동으로 가져왔습니다: ";
        public const string ServerText4 = "서버: 오류";
        public const string TunnelText4 = "SakuraFrp 터널: 오류";
        public const string SafeStopAppendState = "서버가 실행 중이 아닙니다. 터널을 중지했습니다.";
        public const string SafeStopAppendState2 = "안전하게 중지 중: 서버에 stop을 보내고 저장을 기다리는 중…";
        public const string SafeStopAppendServerLog = ">> stop을 전송했습니다. 저장과 종료를 기다리는 중(최대 60초)";
        public const string SafeStopAppendState3 = "서버가 정상적으로 중지되었습니다(저장 완료). 터널을 닫는 중…";
        public const string SafeStopAppendServerLog2 = "[서버 프로세스가 정상 종료되었습니다]";
        public const string SafeStopShow = "60초가 지나도 서버가 종료되지 않았습니다. 강제 종료하면 최근 몇 분의 진행 상황이 사라질 수 있습니다. 강제 종료할까요?";
        public const string SafeStopShow2 = "안전 종료 시간 초과";
        public const string SafeStopShow3 = "강제 종료";
        public const string SafeStopShow4 = "더 기다리기";
        public const string SafeStopAppendState4 = "서버 프로세스를 강제 종료했습니다(최근 진행 상황이 사라졌을 수 있습니다).";
        public const string SafeStopAppendState5 = "강제 종료가 취소되었습니다. 서버는 아직 실행 중입니다.";
        public const string SafeStopAppendState6 = "모두 중지되었습니다: 서버와 터널이 닫혔습니다.";
        public const string OnClosingConfirmBox = "서버가 아직 실행 중입니다. 바로 닫으면 저장되지 않을 수 있습니다. 먼저 안전하게 중지할까요?";
        public const string BtnSparkTpsText = "TPS 보고서";
        public const string BtnSparkProfileText = "30초 프로파일";
        public const string SparkLblCaption = "진단";
        public const string SparkProfileStarted = "[진단] 30초 프로파일링을 시작했습니다. 서버를 계속 실행하세요...";
        public const string SparkProfileStopped = "[진단] 프로파일링이 끝났습니다. spark 보고서 링크는 위의 서버 로그에 나타납니다.";
        public const string SparkProfileCancelled = "[진단] 서버가 중지되어 이번 프로파일링이 취소되었습니다.";
        public const string LangCaption = "언어";
        public const string LangRestartHint = "언어가 저장되었습니다. 적용하려면 앱을 다시 시작하세요.";
        public const string LangRestartNow = "지금 다시 시작";
        public const int EulaAgreeWidth = 190;
        public const int EulaLinkDx = 190;
    }

    static class TxtEs
    {
        public const string Msg = "Algo salió mal";
        public const string ShowFriendlyErrorStr = ", y el programa debe cerrarse.";
        public const string ShowFriendlyErrorStr2 = ", y el programa seguirá funcionando.";
        public const string ShowFriendlyErrorPart = "\n\nSi sigue ocurriendo, envía esta información a Naiwa.";
        public const string ShowFriendlyErrorShow = "Asistente multijugador de Minecraft";
        public const string YesText = "Detener el servidor de forma segura antes de salir";
        public const string NoText = "Cancelar";
        public const string SettingsFormInitChrome = "Ajustes";
        public const string SettingsFormAddField = "Carpeta del servidor D:\\MCServer";
        public const string SettingsFormAddField2 = "Ruta completa de Java (java.exe)";
        public const string SettingsFormAddField3 = "Nombre del archivo jar";
        public const string SettingsFormAddField4 = "Argumentos de memoria";
        public const string SettingsFormAddField5 = "Puerto";
        public const string SettingsFormAddField6 = "Ruta de frpc.exe";
        public const string LbModeCaption = "Modo de túnel";
        public const string SettingsFormAdd = "Línea de comandos de frpc (preferido)";
        public const string SettingsFormAdd2 = "Abrir el lanzador oficial de SakuraFrp (reserva)";
        public const string LbKeyCaption = "Clave de acceso de SakuraFrp";
        public const string SettingsFormAddField7 = "ID del túnel";
        public const string SettingsFormAddField8 = "ID del túnel de voz (opcional)";
        public const string SettingsFormAddField9 = "Dirección pública (opcional)";
        public const string SettingsFormAddField10 = "Ruta del exe de PCL2";
        public const string BtnPropsText = "Propiedades del servidor…";
        public const string SettingsFormShow = "Primero define correctamente la carpeta del servidor arriba y luego edita las propiedades del servidor.";
        public const string SettingsFormShow2 = "Propiedades del servidor";
        public const string BtnWizardText = "Asistente de primera configuración";
        public const string BtnSaveText = "Guardar";
        public const string ValidateSettingsMarkInvalid = "La carpeta del servidor no existe. Revisa la ruta.";
        public const string ValidateSettingsMarkInvalid2 = "La ruta de Java no existe. Revísala.";
        public const string ValidateSettingsMarkInvalid3 = "El nombre del archivo jar no puede estar vacío.";
        public const string ValidateSettingsMarkInvalid4 = "Ese archivo jar no se encontró en la carpeta del servidor.";
        public const string ValidateSettingsMarkInvalid5 = "El puerto debe ser un número entre 1 y 65535.";
        public const string ValidateSettingsMarkInvalid6 = "La ruta de frpc.exe no existe. Revísala.";
        public const string ValidateSettingsMarkInvalid7 = "En modo frpc, el ID del túnel no puede estar vacío.";
        public const string ValidateSettingsMarkInvalid8 = "La ruta de PCL2 no existe. Revísala.";
        public const string TitleText = "Asistente multijugador de Minecraft v6.22.5";
        public const string SubText = "Túnel doble · Juego + Voz";
        public const string ServerText = "Servidor: detenido";
        public const string TunnelText = "Túnel SakuraFrp: detenido";
        public const string AddrText = "Dirección pública: no generada";
        public const string SrvState = "Detenido";
        public const string MbText = "Jugadores";
        public const string OnPaintHeader = "Proceso del servidor";
        public const string OnPaintRow = "Estado";
        public const string OnPaintRow2 = "Uso de memoria";
        public const string OnPaintRow3 = "Conexiones de jugadores";
        public const string OnPaintDrawText = "Jugadores en línea";
        public const string OnPaintRow4 = "Versión · Ping";
        public const string OnPaintHeader2 = "Proceso del túnel (frpc)";
        public const string OnPaintHeader3 = "Local";
        public const string OnPaintDrawText2 = "Bajada";
        public const string OnPaintDrawText3 = "Subida";
        public const string OnPaintRow5 = "Memoria del sistema";
        public const string OnHandleCreatedIndexOf = "Error";
        public const string OnHandleCreatedStr = "Error";
        public const string ShowMsg = "Aceptar";
        public const string PropsFormAddCombo = "Dificultad";
        public const string PropsFormStr = "Pacífico";
        public const string PropsFormStr2 = "Fácil";
        public const string PropsFormStr3 = "Normal";
        public const string PropsFormStr4 = "Difícil";
        public const string PropsFormAddCombo2 = "Modo de juego";
        public const string PropsFormStr5 = "Supervivencia";
        public const string PropsFormStr6 = "Creativo";
        public const string PropsFormStr7 = "Aventura";
        public const string PropsFormStr8 = "Espectador";
        public const string PropsFormAddNum = "Jugadores máximos";
        public const string PropsFormAddNum2 = "Distancia de visión (chunks)";
        public const string PropsFormAddNum3 = "Distancia de simulación (chunks)";
        public const string PropsFormAddNum4 = "Radio de protección del spawn";
        public const string PropsFormAddText = "MOTD del servidor";
        public const string PropsFormAddCheck = "Activar lista blanca (white-list)";
        public const string PropsFormAddCheck2 = "Permitir PvP (pvp)";
        public const string PropsFormAddCheck3 = "Permitir volar (allow-flight)";
        public const string LbOnCaption = "Modo en línea: desactivado (debe permanecer desactivado para multijugador sin conexión; no se puede cambiar aquí)";
        public const string PropsFormShow = "No se encontró server.properties en la carpeta del servidor. Se crea la primera vez que el servidor arranca correctamente.";
        public const string SavePropsShow = "Los jugadores máximos deben ser un número entre 1 y 9999.";
        public const string SavePropsShow2 = "La distancia de visión debe ser un número entre 2 y 32.";
        public const string SavePropsShow3 = "La distancia de simulación debe ser un número entre 2 y 32.";
        public const string SavePropsShow4 = "El radio de protección del spawn debe ser un número entre 0 y 9999.";
        public const string SavePropsShow5 = "Error al guardar: ";
        public const string SavePropsShow6 = "Guardado. El servidor está en marcha, así que estos ajustes se aplicarán en el próximo inicio.";
        public const string PlayerFormStr = "Jugadores en línea (";
        public const string PlayerFormStr2 = "Ahora mismo no hay jugadores en línea";
        public const string PlayerFormAddBtn = "Teletransportar hasta mí";
        public const string PlayerFormAddBtn2 = "Expulsar del servidor";
        public const string PlayerFormConfirmDo = "¿Expulsar a ";
        public const string PlayerFormConfirmDo2 = " del servidor?";
        public const string PlayerFormAddBtn3 = "Dar OP";
        public const string PlayerFormAddBtn4 = "Quitar OP";
        public const string PlayerFormAddBtn5 = "Bloquear";
        public const string PlayerFormConfirmDo3 = "¿Bloquear a ";
        public const string PlayerFormConfirmDo4 = "? No podrá entrar al servidor (puedes desbloquearlo más tarde).";
        public const string PlayerFormAddBtn6 = "Desbloquear";
        public const string PlayerFormStr3 = "Nota: «Teletransportar hasta mí» no está disponible mientras tú no estés en línea.";
        public const string PlayerFormStr4 = "Las acciones se envían al servidor como comandos de consola, igual que si los escribieras en el cuadro de comandos.";
        public const string CloseText = "Cerrar";
        public const string LbOpsCaption = "Lista de OP: ";
        public const string RefreshListsStr = "(ninguno)";
        public const string LbBannedCaption = "Lista de bloqueados: ";
        public const string WizardFormInitChrome = "Asistente de primera configuración";
        public const string WizardFormAddLabel = "Versión de Minecraft";
        public const string WizardFormAddHint = "Sirve cualquier versión oficial";
        public const string WizardFormAddLabel2 = "Tipo de servidor";
        public const string WizardFormAddRadio = "Vanilla";
        public const string WizardFormAddLabel3 = "Carpeta del servidor";
        public const string BtnBrowseText = "Examinar...";
        public const string Description = "Elige la carpeta del servidor (no hace falta que exista todavía: elige una carpeta padre y edita el texto de la ruta)";
        public const string ChkEulaText = "Acepto el EULA de Minecraft";
        public const string LnkEulaText = "Leer el EULA completo";
        public const string WizardFormAddHint2 = "Sin la marca tendrás que poner eula=true en eula.txt tú mismo antes del primer inicio";
        public const string LblStepCaption = "Rellena los tres campos de arriba y haz clic en Iniciar instalación. Java y el servidor se descargan y verifican automáticamente.";
        public const string BtnStartText = "Iniciar instalación";
        public const string WizardFormWizLog = "Cancelando...";
        public const string StartInstallShow = "Primero introduce una versión de Minecraft.";
        public const string StartInstallShow2 = "Eso no parece un número de versión oficial. Introduce algo como 1.20.1 (el asistente no admite snapshots).";
        public const string StartInstallShow3 = "Primero introduce una carpeta de servidor.";
        public const string StartInstallShow4 = "La carpeta de destino ya contiene un server.jar. Continuar lo sobrescribirá.\n\nTu mundo y tus mods no se tocan (el asistente solo sustituye el jar y añade los archivos que falten). ¿Continuar?";
        public const string StartInstallShow5 = "Sobrescribir y continuar";
        public const string StartInstallWizLog = "-- Instalación iniciada: Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "No se puede determinar qué Java necesita Minecraft ";
        public const string RunFail2 = "; asistente cancelado (no se cambió nada).";
        public const string RunWizLog = "Nota: 26.x es la nueva numeración por años de Mojang; su requisito oficial de Java puede ser mayor que la estimación de Java 21 usada aquí. Revisa el registro del servidor tras la instalación.";
        public const string RunWizStep = "Paso 1: preparando Java ";
        public const string RunWizLog2 = "El Java configurado es la versión principal ";
        public const string RunWizLog3 = " y cumple el requisito; se usará: ";
        public const string RunWizLog4 = "Usando el Java descargado anteriormente junto a la app: ";
        public const string RunFail3 = " necesita Java ";
        public const string RunFail4 = ", pero este asistente solo puede descargar automáticamente Java 17 / 21. Instala Java ";
        public const string RunFail5 = " manualmente, indica su java.exe en Ajustes y luego inicia el servidor.";
        public const string RunWizLog5 = "La API de activos de Adoptium no dio un enlace directo (";
        public const string RunStr = "sin detalle";
        public const string RunPart = "); se usará el endpoint binario oficial, que no publica suma de comprobación: esta descarga de Java no se verificará por hash.";
        public const string RunWizLog6 = "Descargando Java ";
        public const string RunWizLog7 = " (Eclipse Temurin JRE, unos 40-50 MB)...";
        public const string RunFail6 = "La descarga de Java falló: ";
        public const string RunWizLog8 = "Verificando el SHA256 del zip de Java...";
        public const string RunFail7 = "El SHA256 del zip de Java no coincide; la descarga se eliminó. Esperado ";
        public const string RunFail8 = ", obtenido ";
        public const string RunFail9 = ". Revisa tu red y reinténtalo.";
        public const string RunWizLog9 = "SHA256 verificado.";
        public const string RunWizLog10 = "Extrayendo Java en ";
        public const string RunWizLog11 = " ...";
        public const string RunFail10 = "La extracción de Java falló: ";
        public const string RunFail11 = "La extracción terminó pero no se encontró bin\\java.exe: estructura del zip inesperada; cancelado (la configuración no se guardó).";
        public const string RunFail12 = "El Java descargado informa de la versión principal ";
        public const string RunFail13 = ", aún por debajo de la requerida ";
        public const string RunFail14 = "; cancelado (la configuración no se guardó).";
        public const string RunWizLog12 = "Java listo: versión principal ";
        public const string RunWizLog13 = ", ";
        public const string RunWizStep2 = "Paso 2: descargando el jar del servidor";
        public const string RunFail15 = "No se puede obtener el manifiesto de versiones de Mojang: ";
        public const string RunFail16 = "Minecraft ";
        public const string RunFail17 = " no está en el manifiesto de versiones (revisa el número; solo versiones oficiales).";
        public const string RunFail18 = "No se pueden obtener los metadatos de la versión ";
        public const string RunFail19 = ": ";
        public const string RunFail20 = "Los metadatos de la versión no tienen descarga de servidor.";
        public const string RunWizLog14 = "Descargando el servidor vanilla (Mojang oficial)...";
        public const string RunDownloadWithProgress = "servidor vanilla";
        public const string RunFail21 = "La descarga del servidor falló: ";
        public const string RunWizLog15 = "Verificando el SHA1 del servidor...";
        public const string RunFail22 = "El SHA1 del servidor no coincide; el jar se eliminó. Esperado ";
        public const string RunWizLog16 = "SHA1 verificado.";
        public const string RunFail23 = "No se puede obtener la lista de loaders de Fabric: ";
        public const string RunFail24 = "Fabric aún no tiene un loader estable para Minecraft ";
        public const string RunFail25 = "; elige otra versión.";
        public const string RunFail26 = "No se puede obtener la lista de instaladores de Fabric: ";
        public const string RunFail27 = "La lista de instaladores de Fabric parece incorrecta.";
        public const string RunWizLog17 = "Descargando el jar de inicio del servidor Fabric (loader ";
        public const string RunWizLog18 = ", instalador ";
        public const string RunWizLog19 = ")...";
        public const string RunWizLog20 = "Nota: Fabric no publica un hash para este jar, así que solo está protegido por HTTPS y no se verifica por hash. En el primer inicio descargará él mismo el servidor vanilla y las bibliotecas.";
        public const string RunDownloadWithProgress2 = "servidor Fabric";
        public const string RunFail28 = "La descarga del servidor Fabric falló: ";
        public const string RunWizLog21 = "Se creó la carpeta mods. Recuerda: los servidores Fabric normalmente también necesitan el mod Fabric API en mods, y cada jugador necesita los mismos mods de doble lado en su cliente.";
        public const string RunFail29 = "No se puede obtener la lista de builds de Paper (Fill API): ";
        public const string RunFail30 = ". Nota: las versiones sin builds de Paper devuelven un error aquí.";
        public const string RunFail31 = "No se encontró ningún servidor descargable en la lista de builds de Paper.";
        public const string RunWizLog22 = "Descargando el servidor Paper (última build)...";
        public const string RunDownloadWithProgress3 = "servidor Paper";
        public const string RunFail32 = "La descarga del servidor Paper falló: ";
        public const string RunWizLog23 = "Verificando el SHA256 del servidor...";
        public const string RunFail33 = "El SHA256 del servidor no coincide; el jar se eliminó. Esperado ";
        public const string RunWizStep3 = "Paso 3: escribiendo los archivos iniciales";
        public const string RunWizLog24 = "Se escribió eula.txt (eula=true, como aceptaste).";
        public const string RunWizLog25 = "EULA no aceptado: no se escribió eula.txt. En el primer inicio el servidor generará eula=false y saldrá; ponlo en eula=true para iniciar.";
        public const string RunWizLog26 = "Se escribió server.properties: server-port=25565, online-mode=false (obligatorio para multijugador sin conexión).";
        public const string RunWizLog27 = "Ya existe un server.properties y no se tocó. Asegúrate tú mismo de que online-mode=false, o las cuentas sin conexión no podrán entrar.";
        public const string RunWizStep4 = "Paso 4: guardando los ajustes";
        public const string RunWizLog28 = "Listo. Carpeta del servidor: ";
        public const string RunWizLog29 = "; jar: server.jar; Java: ";
        public const string RunStr2 = "(se mantuvo el ajuste anterior)";
        public const string RunWizLog30 = "Cierra el asistente, rellena la clave de acceso de SakuraFrp y los ID de túnel en Ajustes, y luego haz clic en Iniciar multijugador.";
        public const string RunWizStep5 = "Instalación completa";
        public const string RunFail34 = "Error del asistente: ";
        public const string DownloadWithProgressWizStep = "Descargando ";
        public const string DownloadWithProgressWizStep2 = "... ";
        public const string FailWizLog = "Falló: ";
        public const string FailWizStep = "Detenido";
        public const string CancelledWizLog = "Cancelado. Los ajustes no se guardaron; los archivos descargados por completo quedan en la carpeta de destino y los parciales se limpiaron.";
        public const string CancelledWizStep = "Cancelado";
        public const string MainFormAppendServerLog = "[Lanzador] Iniciado. Los registros también se guardan en la carpeta logs junto al programa (se conservan 14 días).";
        public const string TrayText = "Asistente multijugador de Minecraft · Detenido";
        public const string InitTray613ToolStripMenuItem = "Mostrar ventana principal";
        public const string InitTray613ToolStripMenuItem2 = "Salida segura";
        public const string BtnStartText2 = "Iniciar multijugador";
        public const string AddrLblCaption = "Dirección pública (envíala a tus amigos): ";
        public const string Hint = "La dirección pública aparecerá aquí cuando se inicie el túnel";
        public const string BtnCopyText = "Copiar la dirección para amigos";
        public const string BtnPcl2Text = "Abrir PCL2";
        public const string BtnStopText = "Detener el servidor de forma segura";
        public const string LogToggleLblCaption = "▾  Registro";
        public const string BuildUiStr = "Servidor";
        public const string BuildUiStr2 = "Túnel";
        public const string BuildUiStr3 = "Estado";
        public const string BuildUiStr4 = "Copias";
        public const string BuildUiMakeLogBox = "Aún no hay registros · La salida del servidor aparecerá aquí";
        public const string BuildUiMakeLogBox2 = "Aún no hay registros · La salida del túnel aparecerá aquí";
        public const string BtnJumpBottomText = "↓ Volver abajo";
        public const string Hint2 = "Buscar en los registros…";
        public const string BuildUiSetToolTip = "Anterior";
        public const string BuildUiSetToolTip2 = "Siguiente";
        public const string BuildUiSetToolTip3 = "Busca en el registro; pulsa Intro para saltar a la siguiente coincidencia";
        public const string ChkErrOnlyText = "Solo advertencias/errores";
        public const string CmdLblCaption = "Comando del servidor";
        public const string BtnCmdSendText = "Enviar";
        public const string BuildUiAppendState = "Primer uso: rellena la clave de acceso de SakuraFrp y el ID del túnel en Ajustes, y revisa la ruta de PCL2.";
        public const string IsErrLineContains = "[Watchdog]";
        public const string IsErrLineContains2 = "[Copia] Error";
        public const string LblFindCaption = "Sin resultados";
        public const string SrvState2 = "En ejecución · ";
        public const string SampleStatsPart = " jugador(es)";
        public const string SampleStatsPart2 = " jugador(es) en línea (lista no pública)";
        public const string SampleStatsPart3 = "Ping ";
        public const string ToggleLogStr = "▸  Registro";
        public const string AppendServerLogPersistLog613 = "Servidor";
        public const string AppendTunnelLogPersistLog613 = "Túnel";
        public const string SendServerCommandAppendState = "El servidor no está en marcha; no se puede enviar el comando.";
        public const string SendServerCommandAppendState2 = "No se pudo enviar el comando: el proceso del servidor ya no existe.";
        public const string SendServerCommandAppendState3 = "No se pudo enviar el comando: ";
        public const string OpenPlayerManagerShow = "El servidor no está en marcha; la gestión de jugadores está disponible cuando el servidor arranca.";
        public const string OpenPlayerManagerAppendState = "El comando falló: el proceso del servidor ya no existe.";
        public const string DoBackupAppendState = "Ya hay una copia de seguridad en curso.";
        public const string BackupCoreAppendState = "Carpeta del mundo no encontrada: ";
        public const string BackupCoreAppendState2 = ", no se puede hacer la copia.";
        public const string BackupCoreAppendServerLog = "[Copia] Guardado automático del servidor pausado y volcado; empaquetando el mundo…";
        public const string BackupCoreAppendServerLog2 = "[Copia] Listo: ";
        public const string BackupCorePart = " archivos";
        public const string BackupCoreStr = ", omitidos ";
        public const string BackupCorePart2 = " archivo(s) en uso";
        public const string BackupCoreAppendState3 = "Copia del mundo completa (";
        public const string BackupCoreAppendServerLog3 = "[Copia] Error: ";
        public const string BackupCoreAppendState4 = "La copia falló: ";
        public const string BackupCoreAppendServerLog4 = "[Copia] Guardado automático del servidor reanudado.";
        public const string RestoreBackupShow = "Debes detener el servidor antes de restaurar: cambiar los archivos del mundo con el servidor en marcha dañará la partida.\n\nHaz clic primero en «Detener el servidor de forma segura» y luego restaura.";
        public const string RestoreBackupShow2 = "Restaurar copia";
        public const string RestoreBackupConfirmBox = "Restaurar el mundo a la copia \"";
        public const string RestoreBackupConfirmBox2 = "\"?\n\nEl mundo actual no se eliminará: primero se le cambia el nombre y se aparta (world_prerestore_<hora>); elimínalo tú cuando todo esté comprobado. Si la restauración fue incorrecta, puedes volver atrás.";
        public const string RestoreBackupAppendState = "Ya hay una tarea de copia en curso.";
        public const string Result = "Restaurado a la copia \"";
        public const string RestoreBackupPart = "\". El mundo anterior se guardó en ";
        public const string RestoreBackupPart2 = "; cuando el mundo nuevo esté comprobado, puedes eliminarlo.";
        public const string Result2 = "La restauración falló: ";
        public const string RestoreBackupPart3 = "(Es posible que el mundo anterior ya haya sido renombrado. No inicies el servidor todavía; revisa la carpeta del servidor.)";
        public const string RestoreBackupAppendServerLog = "[Copia] ";
        public const string BuildBackupPanelBkLabel = "Copias del mundo";
        public const string BtnBackupNowText = "Hacer copia ahora";
        public const string BuildBackupPanelDoBackup = "Copia manual";
        public const string BtnRestoreBackupText = "Restaurar la seleccionada";
        public const string BuildBackupPanelShow = "Primero selecciona una copia de la lista de arriba.";
        public const string BtnDeleteBackupText = "Eliminar la seleccionada";
        public const string BuildBackupPanelShow2 = "Eliminar copia";
        public const string BuildBackupPanelConfirmBox = "Eliminar la copia \"";
        public const string BuildBackupPanelPart = "\"? No se podrá recuperar tras eliminarla.";
        public const string BuildBackupPanelShow3 = "La eliminación falló: ";
        public const string BuildBackupPanelBkLabel2 = "Conservar las últimas ";
        public const string BuildBackupPanelBkLabel3 = " copias";
        public const string BuildBackupPanelBkCheck = "Reinicio automático si se cuelga";
        public const string BuildBackupPanelBkCheck2 = "Copia automática antes de iniciar el servidor";
        public const string BuildBackupPanelBkCheck3 = "Copia automática diaria";
        public const string BuildBackupPanelBkLabel4 = "Mientras la app esté abierta, hace una copia automática a la hora fijada";
        public const string BuildBackupPanelBkCheck4 = "Comando programado";
        public const string LblBackupStateCaption = "Haciendo copia… (los mundos grandes pueden tardar un minuto o dos)";
        public const string RefreshBackupListStr = "Total ";
        public const string RefreshBackupListPart = " copias · ";
        public const string RefreshBackupListStr2 = "Aún no hay copias";
        public const string RefreshBackupListStr3 = "Última: ";
        public const string RefreshBackupListStr4 = "Haz clic en «Hacer copia ahora» para guardar una instantánea del mundo.";
        public const string CheckSchedulesAppendServerLog = "[Programado] Hora de la copia diaria alcanzada; iniciando la copia automática.";
        public const string CheckSchedulesAppendServerLog2 = "[Programado] Hay un comando programado pendiente, pero ";
        public const string CheckSchedulesAppendServerLog3 = " jugador(es) siguen en línea; se pospone hasta que el servidor quede vacío.";
        public const string CheckSchedulesAppendServerLog4 = "[Programado] Comando programado enviado: ";
        public const string ServerText2 = "Servidor: en marcha";
        public const string ServerText3 = "Servidor: iniciando…";
        public const string TunnelText2 = "Túnel SakuraFrp: conectado";
        public const string TunnelText3 = "Túnel SakuraFrp: iniciando…";
        public const string AddrText2 = "Dirección pública: lista";
        public const string AddrText3 = "Dirección pública: guardada (túnel detenido)";
        public const string Tt = "Asistente multijugador de Minecraft · ";
        public const string RefreshStatusStr = "En ejecución";
        public const string RefreshStatusStr2 = "Iniciando";
        public const string CopyAddrShow = "Aún no hay dirección pública. Inicia el multijugador primero.";
        public const string CopyAddrShow2 = "Aviso";
        public const string CopyAddrAppendState = "Dirección pública copiada al portapapeles; envíasela a tus amigos.";
        public const string BtnCopyText2 = "Copiado ✓";
        public const string OpenPcl2Show = "No se encontró PCL2. Define la ruta del exe de PCL2 en Ajustes.";
        public const string OpenPcl2Show2 = "No se pudo abrir PCL2: ";
        public const string HandleServerCrashAppendServerLog = "[Watchdog] El servidor salió inesperadamente (código de salida ";
        public const string HandleServerCrashAppendServerLog2 = "); no fue una parada normal.";
        public const string HandleServerCrashStr = "Posible causa: ";
        public const string HandleServerCrashAppendState = "El servidor salió inesperadamente";
        public const string HandleServerCrashStr2 = ", posible causa: ";
        public const string HandleServerCrashStr3 = ". Revisa el final del registro del servidor para encontrar la causa.";
        public const string HandleServerCrashPart = "(El reinicio automático del watchdog está desactivado)";
        public const string HandleServerCrashAppendServerLog3 = "[Watchdog] Se colgó ";
        public const string HandleServerCrashAppendServerLog4 = " veces en 10 minutos; reinicio automático detenido. Lo más probable es un problema de la partida o de un mod.";
        public const string HandleServerCrashAppendState2 = "El servidor no dejaba de colgarse; el reinicio automático se detuvo. Revisa el registro.";
        public const string HandleServerCrashStr4 = " Posible causa: ";
        public const string HandleServerCrashAppendState3 = "El servidor salió inesperadamente; el watchdog lo reiniciará en 5 segundos…";
        public const string GuessCrash614Ret = "El puerto ";
        public const string GuessCrash614Part = " ya está en uso. Probablemente otra instancia del servidor no se cerró limpiamente";
        public const string GuessCrash614Ret2 = "Memoria insuficiente, o un ajuste de memoria incorrecto (-Xmx)";
        public const string GuessCrash614Ret3 = "Versión de Java incorrecta: este servidor necesita Java 17";
        public const string GuessCrash614Ret4 = "El servidor se congeló durante más de 60 segundos y lo detuvo el watchdog integrado del juego";
        public const string GuessCrash614Ret5 = "Un mod no se pudo cargar; busca el mod nombrado en los errores del registro";
        public const string GuessCrash614Ret6 = "El EULA no ha sido aceptado";
        public const string OfferWizardIfNeededShow = "Aún no hay un servidor utilizable: la carpeta del servidor configurada no existe, o no contiene ningún jar de servidor.\n\nEl asistente de primera configuración puede descargar un Java y un servidor adecuados (Vanilla / Fabric / Paper) y escribir el EULA y el server.properties necesarios para jugar sin conexión. Todas las descargas se verifican por hash.";
        public const string OfferWizardIfNeededShow2 = "Abrir el asistente";
        public const string OfferWizardIfNeededShow3 = "Más tarde";
        public const string OpenWizardAppendState = "Asistente de primera configuración terminado: el servidor y Java están listos.";
        public const string CheckJavaVersionStr = "desconocida";
        public const string CheckJavaVersionAppendServerLog = "[Java] No se puede leer la información de versión del jar del servidor (version.json); se omite la comprobación de versión. Versión principal actual de Java: ";
        public const string CheckJavaVersionAppendServerLog2 = ".";
        public const string CheckJavaVersionAppendServerLog3 = "[Java] Versión del servidor ";
        public const string CheckJavaVersionAppendServerLog4 = " no está en la tabla de versiones; se omite la comprobación de versión. Versión principal actual de Java: ";
        public const string CheckJavaVersionAppendServerLog5 = "[Java] Servidor ";
        public const string CheckJavaVersionAppendServerLog6 = ", pero no se pudo medir la versión actual de Java; se inicia de todos modos (vigila el registro del servidor).";
        public const string CheckJavaVersionAppendServerLog7 = "[Java] Superada: servidor ";
        public const string CheckJavaVersionAppendServerLog8 = ", el Java actual es ";
        public const string CheckJavaVersionAppendServerLog9 = "[Java] No coincide: servidor ";
        public const string CheckJavaVersionAppendServerLog10 = ", el Java actual es solo ";
        public const string CheckJavaVersionAppendServerLog11 = "; esperando la decisión del usuario.";
        public const string Msg2 = "La versión de Java es demasiado antigua: lo más probable es que el servidor no arranque.\n\nVersión del servidor: ";
        public const string CheckJavaVersionPart = ", que necesita Java ";
        public const string CheckJavaVersionPart2 = " o más nuevo. El Java que se va a usar es la versión principal ";
        public const string CheckJavaVersionPart3 = ".\n\nSugerencia: elige un java.exe de Java ";
        public const string CheckJavaVersionPart4 = " en Ajustes, o deja que el asistente de primera configuración descargue automáticamente un Java adecuado.\nElegir Iniciar de todos modos arranca con el Java actual igualmente; consulta el registro del servidor para ver los errores resultantes.";
        public const string CheckJavaVersionShow = "Versión de Java no coincidente";
        public const string CheckJavaVersionShow2 = "Iniciar de todos modos";
        public const string CheckJavaVersionShow3 = "Cancelar el inicio";
        public const string CheckJavaVersionAppendServerLog12 = "[Java] El usuario eligió iniciar de todos modos.";
        public const string CheckJavaVersionAppendServerLog13 = "[Java] El usuario canceló el inicio.";
        public const string StartAllAppendState = "El servidor ya está en marcha; no hace falta iniciarlo de nuevo.";
        public const string BtnStartText3 = "Multijugador en marcha";
        public const string BtnStartText4 = "Iniciando…";
        public const string DoStartFailState = "La carpeta del servidor no existe: ";
        public const string DoStartFailState2 = ". Revisa la carpeta del servidor en Ajustes.";
        public const string DoStartFailState3 = "Jar del servidor no encontrado: ";
        public const string DoStartFailState4 = ". Revisa el nombre del archivo jar en Ajustes.";
        public const string DoStartFailState5 = "Java (java.exe) no encontrado. Vuelve a elegir la ruta de Java en Ajustes: normalmente está en .minecraft\\runtime o en la carpeta de instalación de Java.";
        public const string DoStartFailState6 = "Inicio cancelado: la versión de Java está por debajo de lo que el servidor necesita. Cambia a un Java adecuado, o deja que el asistente de primera configuración descargue uno, y reinténtalo.";
        public const string DoStartFailState7 = " ya está en uso. Es posible que el último servidor no se cerrara limpiamente: haz clic en «Detener el servidor de forma segura» o reinicia esta app; no mates procesos de Java en el Administrador de tareas tú mismo.";
        public const string DoStartAppendState = "Haciendo copia automática antes de iniciar el servidor…";
        public const string DoStartAppendState2 = "Iniciando el servidor de Minecraft…";
        public const string DoStartAppendServerLog = "[El proceso terminó]";
        public const string DoStartFailState8 = "No se puede iniciar el proceso de Java. Comprueba que la ruta de Java sea correcta.";
        public const string DoStartAppendState3 = "Esperando a que el servidor esté listo (vigilando el «Done»)…";
        public const string DoStartFailState9 = "El proceso del servidor terminó antes de que apareciera «Done». Lo más probable: 1) ruta de Java o argumentos de memoria incorrectos; 2) un error en la partida/mundo o en los mods (revisa el final del registro del servidor). No toques el mundo ni los mods.";
        public const string DoStartFailState10 = "Sin «Done» tras 90 segundos. Posiblemente memoria insuficiente, un mundo demasiado grande, o un inicio atascado. Revisa el final del registro del servidor; no toques el mundo ni los mods.";
        public const string DoStartAppendState4 = "Listo. Haz clic en «Copiar la dirección para amigos» y envía la dirección a tus amigos.";
        public const string DoStartFailState11 = "El inicio falló: ";
        public const string StartTunnelAppendTunnelLog = "[Alternativa] Abriendo el lanzador oficial de SakuraFrp: haz clic en Iniciar túnel allí.";
        public const string StartTunnelAppendTunnelLog2 = "No se pudo abrir el lanzador de SakuraFrp: ";
        public const string StartTunnelAppendTunnelLog3 = "frpc.exe no encontrado: ";
        public const string StartTunnelAppendTunnelLog4 = ". Revísalo en Ajustes, o cambia el modo de túnel al lanzador oficial.";
        public const string StartTunnelAppendTunnelLog5 = "La clave de acceso está vacía. Rellena la clave de acceso de SakuraFrp en Ajustes.";
        public const string StartTunnelAppendTunnelLog6 = "El ID del túnel está vacío. Rellénalo en Ajustes.";
        public const string StartTunnelAppendState = "Iniciando el túnel SakuraFrp…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc terminó]";
        public const string StartTunnelAppendTunnelLog8 = "[Watchdog] El túnel de juego salió inesperadamente; reconectando en 5 segundos.";
        public const string StartTunnelAppendTunnelLog9 = "frpc no se pudo iniciar. Revisa la ruta de frpc.exe.";
        public const string StartTunnelAppendTunnelLog10 = "El frpc.exe actual es la build incluida con el lanzador de SakuraFrp. Esa build no puede ser ejecutada directamente por programas externos (solo mediante el SakuraFrp Service), así que sale de inmediato. Esto no tiene nada que ver con tu clave de acceso, tu ID de túnel o el nodo. En Ajustes, apunta la ruta de frpc a un frpc independiente, o cambia el modo de túnel al lanzador oficial.";
        public const string StartTunnelAppendTunnelLog11 = "El proceso frpc terminó. Posiblemente: 1) clave de acceso o ID de túnel incorrectos; 2) un problema del nodo; 3) la cuenta necesita verificación o registro diario. Revisa la clave y el ID de túnel en Ajustes, o cambia el modo de túnel al lanzador oficial.";
        public const string StartTunnelAppendState2 = "Este frpc es la build incluida del lanzador y no puede ejecutarse directamente. En Ajustes, cambia a un frpc independiente o al modo de lanzador oficial.";
        public const string StartVoiceTunnelAppendTunnelLog = "[Voz] No hay ID de túnel de voz definido; se omite el túnel de voz.";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[Voz frpc terminó]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[Watchdog] El túnel de voz salió inesperadamente; reconectando en 5 segundos.";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[Voz] frpc no se pudo iniciar. Revisa la ruta de frpc.exe.";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[Voz] El proceso frpc terminó. Comprueba que el ID del túnel de voz sea correcto.";
        public const string StartVoiceTunnelAppendState = "El túnel de voz está conectado.";
        public const string VoiceFrpcOutputAppendTunnelLog = "[Voz] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[Voz][ERR] ";
        public const string TryParseFrpcAppendState = "Dirección pública obtenida automáticamente: ";
        public const string ServerText4 = "Servidor: error";
        public const string TunnelText4 = "Túnel SakuraFrp: error";
        public const string SafeStopAppendState = "El servidor no está en marcha; el túnel se ha detenido.";
        public const string SafeStopAppendState2 = "Deteniendo de forma segura: enviando stop al servidor y esperando el guardado…";
        public const string SafeStopAppendServerLog = ">> stop enviado; esperando el guardado y la salida (hasta 60 segundos)";
        public const string SafeStopAppendState3 = "El servidor se detuvo normalmente (guardado completo). Cerrando el túnel…";
        public const string SafeStopAppendServerLog2 = "[El proceso del servidor terminó normalmente]";
        public const string SafeStopShow = "El servidor aún no ha salido tras 60 segundos. Forzar su cierre puede perder los últimos minutos de progreso. ¿Forzar el cierre?";
        public const string SafeStopShow2 = "La parada segura agotó el tiempo";
        public const string SafeStopShow3 = "Forzar el cierre";
        public const string SafeStopShow4 = "Esperar más";
        public const string SafeStopAppendState4 = "El proceso del servidor fue cerrado a la fuerza (puede haberse perdido progreso reciente).";
        public const string SafeStopAppendState5 = "Cierre forzado cancelado; el servidor sigue en marcha.";
        public const string SafeStopAppendState6 = "Todo detenido: el servidor y el túnel están cerrados.";
        public const string OnClosingConfirmBox = "El servidor sigue en marcha. Cerrar directamente puede saltarse el guardado: ¿detenerlo de forma segura primero?";
        public const string BtnSparkTpsText = "Informe TPS";
        public const string BtnSparkProfileText = "Diagnosticar 30 s";
        public const string SparkLblCaption = "Diagnóstico";
        public const string SparkProfileStarted = "[Diagnóstico] Diagnóstico de 30 s iniciado; mantén el servidor en marcha...";
        public const string SparkProfileStopped = "[Diagnóstico] Diagnóstico terminado; el enlace del informe de spark aparece en el registro del servidor de arriba.";
        public const string SparkProfileCancelled = "[Diagnóstico] El servidor se detuvo; esta sesión de diagnóstico se canceló.";
        public const string LangCaption = "Idioma";
        public const string LangRestartHint = "Idioma guardado. Reinicia la app para aplicarlo.";
        public const string LangRestartNow = "Reiniciar ahora";
        public const int EulaAgreeWidth = 230;
        public const int EulaLinkDx = 232;
    }

    static class TxtPt
    {
        public const string Msg = "Algo correu mal";
        public const string ShowFriendlyErrorStr = ", e o programa tem de fechar.";
        public const string ShowFriendlyErrorStr2 = ", e o programa continuará a funcionar.";
        public const string ShowFriendlyErrorPart = "\n\nSe continuar a acontecer, envia esta informação ao Naiwa.";
        public const string ShowFriendlyErrorShow = "Assistente multijogador do Minecraft";
        public const string YesText = "Parar o servidor em segurança antes de sair";
        public const string NoText = "Cancelar";
        public const string SettingsFormInitChrome = "Definições";
        public const string SettingsFormAddField = "Pasta do servidor D:\\MCServer";
        public const string SettingsFormAddField2 = "Caminho completo do Java (java.exe)";
        public const string SettingsFormAddField3 = "Nome do ficheiro jar";
        public const string SettingsFormAddField4 = "Argumentos de memória";
        public const string SettingsFormAddField5 = "Porta";
        public const string SettingsFormAddField6 = "Caminho do frpc.exe";
        public const string LbModeCaption = "Modo de túnel";
        public const string SettingsFormAdd = "Linha de comandos do frpc (preferido)";
        public const string SettingsFormAdd2 = "Abrir o lançador oficial SakuraFrp (alternativa)";
        public const string LbKeyCaption = "Chave de acesso SakuraFrp";
        public const string SettingsFormAddField7 = "ID do túnel";
        public const string SettingsFormAddField8 = "ID do túnel de voz (opcional)";
        public const string SettingsFormAddField9 = "Endereço público (opcional)";
        public const string SettingsFormAddField10 = "Caminho do exe do PCL2";
        public const string BtnPropsText = "Propriedades do servidor…";
        public const string SettingsFormShow = "Primeiro define corretamente a pasta do servidor acima e depois edita as propriedades do servidor.";
        public const string SettingsFormShow2 = "Propriedades do servidor";
        public const string BtnWizardText = "Assistente de primeira configuração";
        public const string BtnSaveText = "Salvar";
        public const string ValidateSettingsMarkInvalid = "A pasta do servidor não existe. Verifica o caminho.";
        public const string ValidateSettingsMarkInvalid2 = "O caminho do Java não existe. Verifica-o.";
        public const string ValidateSettingsMarkInvalid3 = "O nome do ficheiro jar não pode estar vazio.";
        public const string ValidateSettingsMarkInvalid4 = "Esse ficheiro jar não foi encontrado na pasta do servidor.";
        public const string ValidateSettingsMarkInvalid5 = "A porta tem de ser um número entre 1 e 65535.";
        public const string ValidateSettingsMarkInvalid6 = "O caminho do frpc.exe não existe. Verifica-o.";
        public const string ValidateSettingsMarkInvalid7 = "No modo frpc, o ID do túnel não pode estar vazio.";
        public const string ValidateSettingsMarkInvalid8 = "O caminho do PCL2 não existe. Verifica-o.";
        public const string TitleText = "Assistente multijogador do Minecraft v6.22.5";
        public const string SubText = "Túnel duplo · Jogo + Voz";
        public const string ServerText = "Servidor: parado";
        public const string TunnelText = "Túnel SakuraFrp: parado";
        public const string AddrText = "Endereço público: não gerado";
        public const string SrvState = "Parado";
        public const string MbText = "Jogadores";
        public const string OnPaintHeader = "Processo do servidor";
        public const string OnPaintRow = "Estado";
        public const string OnPaintRow2 = "Uso de memória";
        public const string OnPaintRow3 = "Ligações de jogadores";
        public const string OnPaintDrawText = "Jogadores online";
        public const string OnPaintRow4 = "Versão · Ping";
        public const string OnPaintHeader2 = "Processo do túnel (frpc)";
        public const string OnPaintHeader3 = "Local";
        public const string OnPaintDrawText2 = "Descida";
        public const string OnPaintDrawText3 = "Subida";
        public const string OnPaintRow5 = "Memória do sistema";
        public const string OnHandleCreatedIndexOf = "Erro";
        public const string OnHandleCreatedStr = "Erro";
        public const string ShowMsg = "OK";
        public const string PropsFormAddCombo = "Dificuldade";
        public const string PropsFormStr = "Pacífico";
        public const string PropsFormStr2 = "Fácil";
        public const string PropsFormStr3 = "Normal";
        public const string PropsFormStr4 = "Difícil";
        public const string PropsFormAddCombo2 = "Modo de jogo";
        public const string PropsFormStr5 = "Sobrevivência";
        public const string PropsFormStr6 = "Criativo";
        public const string PropsFormStr7 = "Aventura";
        public const string PropsFormStr8 = "Espectador";
        public const string PropsFormAddNum = "Máximo de jogadores";
        public const string PropsFormAddNum2 = "Distância de visão (chunks)";
        public const string PropsFormAddNum3 = "Distância de simulação (chunks)";
        public const string PropsFormAddNum4 = "Raio de proteção do spawn";
        public const string PropsFormAddText = "MOTD do servidor";
        public const string PropsFormAddCheck = "Ativar lista branca (white-list)";
        public const string PropsFormAddCheck2 = "Permitir PvP (pvp)";
        public const string PropsFormAddCheck3 = "Permitir voar (allow-flight)";
        public const string LbOnCaption = "Modo online: desligado (tem de permanecer desligado para multijogador offline; não pode ser alterado aqui)";
        public const string PropsFormShow = "Não foi encontrado server.properties na pasta do servidor. É criado na primeira vez que o servidor arranca com sucesso.";
        public const string SavePropsShow = "O máximo de jogadores tem de ser um número entre 1 e 9999.";
        public const string SavePropsShow2 = "A distância de visão tem de ser um número entre 2 e 32.";
        public const string SavePropsShow3 = "A distância de simulação tem de ser um número entre 2 e 32.";
        public const string SavePropsShow4 = "O raio de proteção do spawn tem de ser um número entre 0 e 9999.";
        public const string SavePropsShow5 = "Falha ao salvar: ";
        public const string SavePropsShow6 = "Salvo. O servidor está a funcionar, por isso estas definições entram em vigor no próximo arranque.";
        public const string PlayerFormStr = "Jogadores online (";
        public const string PlayerFormStr2 = "Não há jogadores online neste momento";
        public const string PlayerFormAddBtn = "Teleportar até mim";
        public const string PlayerFormAddBtn2 = "Expulsar do servidor";
        public const string PlayerFormConfirmDo = "Expulsar ";
        public const string PlayerFormConfirmDo2 = " do servidor?";
        public const string PlayerFormAddBtn3 = "Dar OP";
        public const string PlayerFormAddBtn4 = "Remover OP";
        public const string PlayerFormAddBtn5 = "Bloquear";
        public const string PlayerFormConfirmDo3 = "Bloquear ";
        public const string PlayerFormConfirmDo4 = "? Não poderá entrar no servidor (podes desbloqueá-lo mais tarde).";
        public const string PlayerFormAddBtn6 = "Desbloquear";
        public const string PlayerFormStr3 = "Nota: «Teleportar até mim» não está disponível enquanto não estiveres online.";
        public const string PlayerFormStr4 = "As ações são enviadas ao servidor como comandos de consola, tal como escrevê-las na caixa de comandos.";
        public const string CloseText = "Fechar";
        public const string LbOpsCaption = "Lista de OP: ";
        public const string RefreshListsStr = "(nenhum)";
        public const string LbBannedCaption = "Lista de bloqueados: ";
        public const string WizardFormInitChrome = "Assistente de primeira configuração";
        public const string WizardFormAddLabel = "Versão do Minecraft";
        public const string WizardFormAddHint = "Qualquer versão oficial serve";
        public const string WizardFormAddLabel2 = "Tipo de servidor";
        public const string WizardFormAddRadio = "Vanilla";
        public const string WizardFormAddLabel3 = "Pasta do servidor";
        public const string BtnBrowseText = "Procurar...";
        public const string Description = "Escolhe a pasta do servidor (não precisa de existir ainda — escolhe uma pasta mãe e edita o texto do caminho)";
        public const string ChkEulaText = "Aceito o EULA do Minecraft";
        public const string LnkEulaText = "Ler o EULA completo";
        public const string WizardFormAddHint2 = "Sem a marca, terás de definir eula=true no eula.txt tu mesmo antes do primeiro arranque";
        public const string LblStepCaption = "Preenche os três itens acima e clica em Iniciar instalação. O Java e o servidor são transferidos e verificados automaticamente.";
        public const string BtnStartText = "Iniciar instalação";
        public const string WizardFormWizLog = "A cancelar...";
        public const string StartInstallShow = "Primeiro introduz uma versão do Minecraft.";
        public const string StartInstallShow2 = "Isso não parece um número de versão oficial. Introduz algo como 1.20.1 (snapshots não são suportadas pelo assistente).";
        public const string StartInstallShow3 = "Primeiro introduz uma pasta de servidor.";
        public const string StartInstallShow4 = "A pasta de destino já contém um server.jar. Continuar irá substituí-lo.\n\nO teu mundo e os teus mods não são tocados (o assistente só substitui o jar e adiciona os ficheiros em falta). Continuar?";
        public const string StartInstallShow5 = "Substituir e continuar";
        public const string StartInstallWizLog = "-- Instalação iniciada: Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "Não é possível determinar que Java o Minecraft ";
        public const string RunFail2 = " necessita; assistente abortado (nada foi alterado).";
        public const string RunWizLog = "Nota: 26.x é a nova numeração por anos da Mojang; o requisito oficial de Java pode ser superior à estimativa de Java 21 usada aqui — verifica o registo do servidor após a instalação.";
        public const string RunWizStep = "Passo 1: a preparar o Java ";
        public const string RunWizLog2 = "O Java configurado é a versão principal ";
        public const string RunWizLog3 = " e cumpre o requisito; a utilizá-lo: ";
        public const string RunWizLog4 = "A utilizar o Java transferido anteriormente junto à app: ";
        public const string RunFail3 = " necessita do Java ";
        public const string RunFail4 = ", mas este assistente só pode transferir automaticamente Java 17 / 21. Instala o Java ";
        public const string RunFail5 = " manualmente, aponta as Definições para o seu java.exe e depois arranca o servidor.";
        public const string RunWizLog5 = "A API de ativos da Adoptium não deu uma ligação direta (";
        public const string RunStr = "sem detalhe";
        public const string RunPart = "); a recorrer ao endpoint binário oficial, que não publica soma de verificação — esta transferência de Java não será verificada por hash.";
        public const string RunWizLog6 = "A transferir o Java ";
        public const string RunWizLog7 = " (Eclipse Temurin JRE, cerca de 40-50 MB)...";
        public const string RunFail6 = "A transferência do Java falhou: ";
        public const string RunWizLog8 = "A verificar o SHA256 do zip do Java...";
        public const string RunFail7 = "O SHA256 do zip do Java não coincide; a transferência foi eliminada. Esperado ";
        public const string RunFail8 = ", obtido ";
        public const string RunFail9 = ". Verifica a tua rede e tenta de novo.";
        public const string RunWizLog9 = "SHA256 verificado.";
        public const string RunWizLog10 = "A extrair o Java para ";
        public const string RunWizLog11 = " ...";
        public const string RunFail10 = "A extração do Java falhou: ";
        public const string RunFail11 = "A extração terminou mas não foi encontrado bin\\java.exe — estrutura do zip inesperada; abortado (as definições não foram salvas).";
        public const string RunFail12 = "O Java transferido indica a versão principal ";
        public const string RunFail13 = ", ainda abaixo da necessária ";
        public const string RunFail14 = "; abortado (as definições não foram salvas).";
        public const string RunWizLog12 = "Java pronto: versão principal ";
        public const string RunWizLog13 = ", ";
        public const string RunWizStep2 = "Passo 2: a transferir o jar do servidor";
        public const string RunFail15 = "Não é possível obter o manifesto de versões da Mojang: ";
        public const string RunFail16 = "O Minecraft ";
        public const string RunFail17 = " não está no manifesto de versões (verifica o número; apenas versões oficiais).";
        public const string RunFail18 = "Não é possível obter os metadados da versão ";
        public const string RunFail19 = ": ";
        public const string RunFail20 = "Os metadados da versão não têm transferência de servidor.";
        public const string RunWizLog14 = "A transferir o servidor vanilla (Mojang oficial)...";
        public const string RunDownloadWithProgress = "servidor vanilla";
        public const string RunFail21 = "A transferência do servidor falhou: ";
        public const string RunWizLog15 = "A verificar o SHA1 do servidor...";
        public const string RunFail22 = "O SHA1 do servidor não coincide; o jar foi eliminado. Esperado ";
        public const string RunWizLog16 = "SHA1 verificado.";
        public const string RunFail23 = "Não é possível obter a lista de loaders do Fabric: ";
        public const string RunFail24 = "O Fabric ainda não tem um loader estável para o Minecraft ";
        public const string RunFail25 = "; escolhe outra versão.";
        public const string RunFail26 = "Não é possível obter a lista de instaladores do Fabric: ";
        public const string RunFail27 = "A lista de instaladores do Fabric parece errada.";
        public const string RunWizLog17 = "A transferir o jar de arranque do servidor Fabric (loader ";
        public const string RunWizLog18 = ", instalador ";
        public const string RunWizLog19 = ")...";
        public const string RunWizLog20 = "Nota: o Fabric não publica um hash para este jar, por isso está protegido apenas por HTTPS e não é verificado por hash. No primeiro arranque, ele próprio transfere o servidor vanilla e as bibliotecas.";
        public const string RunDownloadWithProgress2 = "servidor Fabric";
        public const string RunFail28 = "A transferência do servidor Fabric falhou: ";
        public const string RunWizLog21 = "Pasta mods criada. Lembra-te: os servidores Fabric normalmente também precisam do mod Fabric API em mods, e cada jogador precisa dos mesmos mods de dois lados no seu cliente.";
        public const string RunFail29 = "Não é possível obter a lista de builds do Paper (Fill API): ";
        public const string RunFail30 = ". Nota: versões sem builds do Paper devolvem um erro aqui.";
        public const string RunFail31 = "Nenhum servidor transferível encontrado na lista de builds do Paper.";
        public const string RunWizLog22 = "A transferir o servidor Paper (build mais recente)...";
        public const string RunDownloadWithProgress3 = "servidor Paper";
        public const string RunFail32 = "A transferência do servidor Paper falhou: ";
        public const string RunWizLog23 = "A verificar o SHA256 do servidor...";
        public const string RunFail33 = "O SHA256 do servidor não coincide; o jar foi eliminado. Esperado ";
        public const string RunWizStep3 = "Passo 3: a escrever os ficheiros iniciais";
        public const string RunWizLog24 = "eula.txt escrito (eula=true, como aceitaste).";
        public const string RunWizLog25 = "EULA não aceite: o eula.txt não foi escrito. No primeiro arranque, o servidor irá gerar eula=false e sair; define-o como eula=true para arrancar.";
        public const string RunWizLog26 = "server.properties escrito: server-port=25565, online-mode=false (obrigatório para multijogador offline).";
        public const string RunWizLog27 = "Já existe um server.properties e ficou intocado. Certifica-te tu mesmo de que online-mode=false, ou as contas offline não conseguem entrar.";
        public const string RunWizStep4 = "Passo 4: a salvar as definições";
        public const string RunWizLog28 = "Concluído. Pasta do servidor: ";
        public const string RunWizLog29 = "; jar: server.jar; Java: ";
        public const string RunStr2 = "(definição anterior mantida)";
        public const string RunWizLog30 = "Fecha o assistente, preenche a chave de acesso SakuraFrp e os IDs de túnel nas Definições e depois clica em Iniciar multijogador.";
        public const string RunWizStep5 = "Instalação concluída";
        public const string RunFail34 = "Erro do assistente: ";
        public const string DownloadWithProgressWizStep = "A transferir ";
        public const string DownloadWithProgressWizStep2 = "... ";
        public const string FailWizLog = "Falhou: ";
        public const string FailWizStep = "Parado";
        public const string CancelledWizLog = "Cancelado. As definições não foram salvas; os ficheiros totalmente transferidos ficam na pasta de destino e os parciais foram limpos.";
        public const string CancelledWizStep = "Cancelado";
        public const string MainFormAppendServerLog = "[Lançador] Iniciado. Os registos também são salvos na pasta logs junto ao programa (mantidos durante 14 dias).";
        public const string TrayText = "Assistente multijogador do Minecraft · Parado";
        public const string InitTray613ToolStripMenuItem = "Mostrar janela principal";
        public const string InitTray613ToolStripMenuItem2 = "Saída segura";
        public const string BtnStartText2 = "Iniciar multijogador";
        public const string AddrLblCaption = "Endereço público (envia-o aos teus amigos): ";
        public const string Hint = "O endereço público aparecerá aqui quando o túnel arrancar";
        public const string BtnCopyText = "Copiar endereço para amigos";
        public const string BtnPcl2Text = "Abrir PCL2";
        public const string BtnStopText = "Parar o servidor em segurança";
        public const string LogToggleLblCaption = "▾  Registo";
        public const string BuildUiStr = "Servidor";
        public const string BuildUiStr2 = "Túnel";
        public const string BuildUiStr3 = "Estado";
        public const string BuildUiStr4 = "Cópias";
        public const string BuildUiMakeLogBox = "Ainda sem registos · A saída do servidor aparecerá aqui";
        public const string BuildUiMakeLogBox2 = "Ainda sem registos · A saída do túnel aparecerá aqui";
        public const string BtnJumpBottomText = "↓ Voltar ao fundo";
        public const string Hint2 = "Pesquisar registos…";
        public const string BuildUiSetToolTip = "Anterior";
        public const string BuildUiSetToolTip2 = "Seguinte";
        public const string BuildUiSetToolTip3 = "Pesquisa no registo; prime Enter para saltar para a correspondência seguinte";
        public const string ChkErrOnlyText = "Apenas avisos/erros";
        public const string CmdLblCaption = "Comando do servidor";
        public const string BtnCmdSendText = "Enviar";
        public const string BuildUiAppendState = "Primeira utilização: preenche a chave de acesso SakuraFrp e o ID do túnel nas Definições e verifica o caminho do PCL2.";
        public const string IsErrLineContains = "[Watchdog]";
        public const string IsErrLineContains2 = "[Cópia] Erro";
        public const string LblFindCaption = "Sem resultados";
        public const string SrvState2 = "A funcionar · ";
        public const string SampleStatsPart = " jogador(es)";
        public const string SampleStatsPart2 = " jogador(es) online (lista não pública)";
        public const string SampleStatsPart3 = "Ping ";
        public const string ToggleLogStr = "▸  Registo";
        public const string AppendServerLogPersistLog613 = "Servidor";
        public const string AppendTunnelLogPersistLog613 = "Túnel";
        public const string SendServerCommandAppendState = "O servidor não está a funcionar; o comando não pode ser enviado.";
        public const string SendServerCommandAppendState2 = "Falha ao enviar o comando: o processo do servidor desapareceu.";
        public const string SendServerCommandAppendState3 = "Falha ao enviar o comando: ";
        public const string OpenPlayerManagerShow = "O servidor não está a funcionar; a gestão de jogadores fica disponível depois de o servidor arrancar.";
        public const string OpenPlayerManagerAppendState = "O comando falhou: o processo do servidor desapareceu.";
        public const string DoBackupAppendState = "Já está uma cópia de segurança em curso.";
        public const string BackupCoreAppendState = "Pasta do mundo não encontrada: ";
        public const string BackupCoreAppendState2 = ", não é possível fazer a cópia.";
        public const string BackupCoreAppendServerLog = "[Cópia] Guardado automático do servidor pausado e gravado; a empacotar o mundo…";
        public const string BackupCoreAppendServerLog2 = "[Cópia] Concluído: ";
        public const string BackupCorePart = " ficheiros";
        public const string BackupCoreStr = ", ignorados ";
        public const string BackupCorePart2 = " ficheiro(s) em uso";
        public const string BackupCoreAppendState3 = "Cópia do mundo concluída (";
        public const string BackupCoreAppendServerLog3 = "[Cópia] Erro: ";
        public const string BackupCoreAppendState4 = "A cópia falhou: ";
        public const string BackupCoreAppendServerLog4 = "[Cópia] Guardado automático do servidor retomado.";
        public const string RestoreBackupShow = "Tens de parar o servidor antes de restaurar: trocar os ficheiros do mundo com o servidor a funcionar irá corromper o save.\n\nClica primeiro em «Parar o servidor em segurança» e depois restaura.";
        public const string RestoreBackupShow2 = "Restaurar cópia";
        public const string RestoreBackupConfirmBox = "Restaurar o mundo para a cópia \"";
        public const string RestoreBackupConfirmBox2 = "\"?\n\nO mundo atual não será eliminado: primeiro muda-se-lhe o nome e põe-se de parte (world_prerestore_<hora>); elimina-o tu quando tudo estiver verificado. Se a restauração foi errada, podes voltar atrás.";
        public const string RestoreBackupAppendState = "Já está uma tarefa de cópia em curso.";
        public const string Result = "Restaurado para a cópia \"";
        public const string RestoreBackupPart = "\". O mundo anterior foi guardado em ";
        public const string RestoreBackupPart2 = "; quando o mundo novo estiver verificado, podes eliminá-lo.";
        public const string Result2 = "A restauração falhou: ";
        public const string RestoreBackupPart3 = "(O mundo anterior pode já ter sido renomeado. Ainda não arranques o servidor; verifica a pasta do servidor.)";
        public const string RestoreBackupAppendServerLog = "[Cópia] ";
        public const string BuildBackupPanelBkLabel = "Cópias do mundo";
        public const string BtnBackupNowText = "Fazer cópia agora";
        public const string BuildBackupPanelDoBackup = "Cópia manual";
        public const string BtnRestoreBackupText = "Restaurar a selecionada";
        public const string BuildBackupPanelShow = "Primeiro seleciona uma cópia da lista acima.";
        public const string BtnDeleteBackupText = "Eliminar a selecionada";
        public const string BuildBackupPanelShow2 = "Eliminar cópia";
        public const string BuildBackupPanelConfirmBox = "Eliminar a cópia \"";
        public const string BuildBackupPanelPart = "\"? Não poderá ser recuperada após a eliminação.";
        public const string BuildBackupPanelShow3 = "A eliminação falhou: ";
        public const string BuildBackupPanelBkLabel2 = "Manter as últimas ";
        public const string BuildBackupPanelBkLabel3 = " cópias";
        public const string BuildBackupPanelBkCheck = "Reinício automático em caso de falha";
        public const string BuildBackupPanelBkCheck2 = "Cópia automática antes do arranque do servidor";
        public const string BuildBackupPanelBkCheck3 = "Cópia automática diária";
        public const string BuildBackupPanelBkLabel4 = "Enquanto a app estiver aberta, faz uma cópia automática à hora definida";
        public const string BuildBackupPanelBkCheck4 = "Comando agendado";
        public const string LblBackupStateCaption = "A fazer cópia… (mundos grandes podem demorar um ou dois minutos)";
        public const string RefreshBackupListStr = "Total ";
        public const string RefreshBackupListPart = " cópias · ";
        public const string RefreshBackupListStr2 = "Ainda não há cópias";
        public const string RefreshBackupListStr3 = "Mais recente: ";
        public const string RefreshBackupListStr4 = "Clica em «Fazer cópia agora» para guardar um instantâneo do mundo.";
        public const string CheckSchedulesAppendServerLog = "[Agendado] Hora da cópia diária atingida; a iniciar a cópia automática.";
        public const string CheckSchedulesAppendServerLog2 = "[Agendado] Um comando agendado está devido, mas ";
        public const string CheckSchedulesAppendServerLog3 = " jogador(es) ainda estão online; a adiar até o servidor ficar vazio.";
        public const string CheckSchedulesAppendServerLog4 = "[Agendado] Comando agendado enviado: ";
        public const string ServerText2 = "Servidor: a funcionar";
        public const string ServerText3 = "Servidor: a arrancar…";
        public const string TunnelText2 = "Túnel SakuraFrp: ligado";
        public const string TunnelText3 = "Túnel SakuraFrp: a arrancar…";
        public const string AddrText2 = "Endereço público: pronto";
        public const string AddrText3 = "Endereço público: salvo (túnel parado)";
        public const string Tt = "Assistente multijogador do Minecraft · ";
        public const string RefreshStatusStr = "A funcionar";
        public const string RefreshStatusStr2 = "A arrancar";
        public const string CopyAddrShow = "Ainda não há endereço público. Inicia o multijogador primeiro.";
        public const string CopyAddrShow2 = "Aviso";
        public const string CopyAddrAppendState = "Endereço público copiado para a área de transferência; envia-o aos teus amigos.";
        public const string BtnCopyText2 = "Copiado ✓";
        public const string OpenPcl2Show = "PCL2 não encontrado. Define o caminho do exe do PCL2 nas Definições.";
        public const string OpenPcl2Show2 = "Falha ao abrir o PCL2: ";
        public const string HandleServerCrashAppendServerLog = "[Watchdog] O servidor saiu inesperadamente (código de saída ";
        public const string HandleServerCrashAppendServerLog2 = "); não foi uma paragem normal.";
        public const string HandleServerCrashStr = "Causa possível: ";
        public const string HandleServerCrashAppendState = "O servidor saiu inesperadamente";
        public const string HandleServerCrashStr2 = ", causa possível: ";
        public const string HandleServerCrashStr3 = ". Verifica o fundo do registo do servidor para saberes a causa.";
        public const string HandleServerCrashPart = "(O reinício automático do watchdog está desligado)";
        public const string HandleServerCrashAppendServerLog3 = "[Watchdog] Falhou ";
        public const string HandleServerCrashAppendServerLog4 = " vezes em 10 minutos; reinício automático parado. O mais provável é um problema do save ou de um mod.";
        public const string HandleServerCrashAppendState2 = "O servidor não parava de falhar; o reinício automático parou. Verifica o registo.";
        public const string HandleServerCrashStr4 = " Causa possível: ";
        public const string HandleServerCrashAppendState3 = "O servidor saiu inesperadamente; o watchdog irá reiniciá-lo dentro de 5 segundos…";
        public const string GuessCrash614Ret = "A porta ";
        public const string GuessCrash614Part = " já está em uso — provavelmente outra instância do servidor não foi encerrada de forma limpa";
        public const string GuessCrash614Ret2 = "Memória insuficiente, ou uma definição de memória errada (-Xmx)";
        public const string GuessCrash614Ret3 = "Versão do Java errada — este servidor precisa do Java 17";
        public const string GuessCrash614Ret4 = "O servidor congelou durante mais de 60 segundos e foi parado pelo watchdog integrado do jogo";
        public const string GuessCrash614Ret5 = "Um mod falhou ao carregar; procura o mod nomeado nos erros do registo";
        public const string GuessCrash614Ret6 = "O EULA não foi aceite";
        public const string OfferWizardIfNeededShow = "Ainda não há um servidor utilizável: a pasta do servidor configurada não existe, ou não contém nenhum jar de servidor.\n\nO assistente de primeira configuração pode transferir um Java e um servidor adequados (Vanilla / Fabric / Paper) e escrever o EULA e o server.properties necessários para jogar offline. Todas as transferências são verificadas por hash.";
        public const string OfferWizardIfNeededShow2 = "Abrir o assistente";
        public const string OfferWizardIfNeededShow3 = "Mais tarde";
        public const string OpenWizardAppendState = "Assistente de primeira configuração concluído: o servidor e o Java estão prontos.";
        public const string CheckJavaVersionStr = "desconhecida";
        public const string CheckJavaVersionAppendServerLog = "[Java] Não é possível ler a informação de versão do jar do servidor (version.json); a saltar a verificação de versão. Versão principal atual do Java: ";
        public const string CheckJavaVersionAppendServerLog2 = ".";
        public const string CheckJavaVersionAppendServerLog3 = "[Java] Versão do servidor ";
        public const string CheckJavaVersionAppendServerLog4 = " não está na tabela de versões; a saltar a verificação de versão. Versão principal atual do Java: ";
        public const string CheckJavaVersionAppendServerLog5 = "[Java] Servidor ";
        public const string CheckJavaVersionAppendServerLog6 = ", mas a versão atual do Java não pôde ser medida; a arrancar na mesma (fica atento ao registo do servidor).";
        public const string CheckJavaVersionAppendServerLog7 = "[Java] Aprovado: servidor ";
        public const string CheckJavaVersionAppendServerLog8 = ", o Java atual é ";
        public const string CheckJavaVersionAppendServerLog9 = "[Java] Incompatível: servidor ";
        public const string CheckJavaVersionAppendServerLog10 = ", o Java atual é apenas ";
        public const string CheckJavaVersionAppendServerLog11 = "; a aguardar a decisão do utilizador.";
        public const string Msg2 = "A versão do Java é demasiado antiga — o servidor muito provavelmente não arrancará.\n\nVersão do servidor: ";
        public const string CheckJavaVersionPart = ", que precisa do Java ";
        public const string CheckJavaVersionPart2 = " ou mais recente. O Java prestes a ser usado é a versão principal ";
        public const string CheckJavaVersionPart3 = ".\n\nSugestão: escolhe um java.exe do Java ";
        public const string CheckJavaVersionPart4 = " nas Definições, ou deixa o assistente de primeira configuração transferir automaticamente um Java adequado.\nEscolher Arrancar na mesma arranca com o Java atual de qualquer forma; vê o registo do servidor para os erros resultantes.";
        public const string CheckJavaVersionShow = "Versão do Java incompatível";
        public const string CheckJavaVersionShow2 = "Arrancar na mesma";
        public const string CheckJavaVersionShow3 = "Cancelar o arranque";
        public const string CheckJavaVersionAppendServerLog12 = "[Java] O utilizador escolheu arrancar na mesma.";
        public const string CheckJavaVersionAppendServerLog13 = "[Java] O utilizador cancelou o arranque.";
        public const string StartAllAppendState = "O servidor já está a funcionar; não é preciso arrancá-lo outra vez.";
        public const string BtnStartText3 = "Multijogador a funcionar";
        public const string BtnStartText4 = "A arrancar…";
        public const string DoStartFailState = "A pasta do servidor não existe: ";
        public const string DoStartFailState2 = ". Verifica a pasta do servidor nas Definições.";
        public const string DoStartFailState3 = "Jar do servidor não encontrado: ";
        public const string DoStartFailState4 = ". Verifica o nome do ficheiro jar nas Definições.";
        public const string DoStartFailState5 = "Java (java.exe) não encontrado. Volta a selecionar o caminho do Java nas Definições — normalmente está em .minecraft\\runtime ou na pasta de instalação do Java.";
        public const string DoStartFailState6 = "Arranque cancelado: a versão do Java está abaixo do que o servidor precisa. Muda para um Java adequado, ou deixa o assistente de primeira configuração transferir um, e tenta de novo.";
        public const string DoStartFailState7 = " já está em uso. O último servidor pode não ter encerrado de forma limpa — clica em «Parar o servidor em segurança» ou reinicia esta app; não mates processos Java no Gestor de Tarefas tu mesmo.";
        public const string DoStartAppendState = "A fazer cópia automática antes do arranque do servidor…";
        public const string DoStartAppendState2 = "A iniciar o servidor de Minecraft…";
        public const string DoStartAppendServerLog = "[Processo terminou]";
        public const string DoStartFailState8 = "Não é possível iniciar o processo Java. Verifica se o caminho do Java está correto.";
        public const string DoStartAppendState3 = "A aguardar que o servidor fique pronto (a vigiar o «Done»)…";
        public const string DoStartFailState9 = "O processo do servidor terminou antes de aparecer «Done». O mais provável: 1) caminho do Java ou argumentos de memória errados; 2) um erro no save/mundo ou nos mods (verifica o fundo do registo do servidor). Não mexas no mundo nem nos mods.";
        public const string DoStartFailState10 = "Sem «Done» após 90 segundos. Possivelmente memória insuficiente, um mundo demasiado grande, ou um arranque bloqueado. Verifica o fundo do registo do servidor; não mexas no mundo nem nos mods.";
        public const string DoStartAppendState4 = "Pronto. Clica em «Copiar endereço para amigos» e envia o endereço aos teus amigos.";
        public const string DoStartFailState11 = "O arranque falhou: ";
        public const string StartTunnelAppendTunnelLog = "[Alternativa] A abrir o lançador oficial SakuraFrp — clica em Iniciar túnel nele.";
        public const string StartTunnelAppendTunnelLog2 = "Falha ao abrir o lançador SakuraFrp: ";
        public const string StartTunnelAppendTunnelLog3 = "frpc.exe não encontrado: ";
        public const string StartTunnelAppendTunnelLog4 = ". Verifica-o nas Definições, ou muda o modo de túnel para o lançador oficial.";
        public const string StartTunnelAppendTunnelLog5 = "A chave de acesso está vazia. Preenche a chave de acesso SakuraFrp nas Definições.";
        public const string StartTunnelAppendTunnelLog6 = "O ID do túnel está vazio. Preenche-o nas Definições.";
        public const string StartTunnelAppendState = "A iniciar o túnel SakuraFrp…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc terminou]";
        public const string StartTunnelAppendTunnelLog8 = "[Watchdog] O túnel de jogo saiu inesperadamente; a religar dentro de 5 segundos.";
        public const string StartTunnelAppendTunnelLog9 = "O frpc não conseguiu arrancar. Verifica o caminho do frpc.exe.";
        public const string StartTunnelAppendTunnelLog10 = "O frpc.exe atual é a build incluída com o lançador SakuraFrp. Essa build não pode ser executada diretamente por programas externos (apenas através do SakuraFrp Service), por isso sai imediatamente. Isto não tem nada a ver com a tua chave de acesso, o ID do túnel ou o nó. Nas Definições, aponta o caminho do frpc para um frpc autónomo, ou muda o modo de túnel para o lançador oficial.";
        public const string StartTunnelAppendTunnelLog11 = "O processo frpc terminou. Possivelmente: 1) chave de acesso ou ID do túnel errados; 2) um problema do nó; 3) a conta precisa de verificação ou check-in. Verifica a chave e o ID do túnel nas Definições, ou muda o modo de túnel para o lançador oficial.";
        public const string StartTunnelAppendState2 = "Este frpc é a build incluída do lançador e não pode ser executado diretamente. Nas Definições, muda para um frpc autónomo ou para o modo de lançador oficial.";
        public const string StartVoiceTunnelAppendTunnelLog = "[Voz] Nenhum ID de túnel de voz definido; a ignorar o túnel de voz.";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[Voz frpc terminou]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[Watchdog] O túnel de voz saiu inesperadamente; a religar dentro de 5 segundos.";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[Voz] O frpc não conseguiu arrancar. Verifica o caminho do frpc.exe.";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[Voz] O processo frpc terminou. Verifica se o ID do túnel de voz está correto.";
        public const string StartVoiceTunnelAppendState = "O túnel de voz está ligado.";
        public const string VoiceFrpcOutputAppendTunnelLog = "[Voz] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[Voz][ERR] ";
        public const string TryParseFrpcAppendState = "Endereço público obtido automaticamente: ";
        public const string ServerText4 = "Servidor: erro";
        public const string TunnelText4 = "Túnel SakuraFrp: erro";
        public const string SafeStopAppendState = "O servidor não está a funcionar; o túnel foi parado.";
        public const string SafeStopAppendState2 = "A parar em segurança: a enviar stop ao servidor e a aguardar o save…";
        public const string SafeStopAppendServerLog = ">> stop enviado; a aguardar o save e a saída (até 60 segundos)";
        public const string SafeStopAppendState3 = "O servidor parou normalmente (save concluído). A fechar o túnel…";
        public const string SafeStopAppendServerLog2 = "[O processo do servidor terminou normalmente]";
        public const string SafeStopShow = "O servidor ainda não saiu após 60 segundos. Forçar o fim pode perder os últimos minutos de progresso. Forçar o fim?";
        public const string SafeStopShow2 = "A paragem segura excedeu o tempo";
        public const string SafeStopShow3 = "Forçar o fim";
        public const string SafeStopShow4 = "Esperar mais";
        public const string SafeStopAppendState4 = "O processo do servidor foi forçado a terminar (o progresso recente pode ter-se perdido).";
        public const string SafeStopAppendState5 = "Fim forçado cancelado; o servidor ainda está a funcionar.";
        public const string SafeStopAppendState6 = "Tudo parado: o servidor e o túnel estão fechados.";
        public const string OnClosingConfirmBox = "O servidor ainda está a funcionar. Fechar diretamente pode saltar o save — parar em segurança primeiro?";
        public const string BtnSparkTpsText = "Relatório TPS";
        public const string BtnSparkProfileText = "Diagnosticar 30 s";
        public const string SparkLblCaption = "Diagnóstico";
        public const string SparkProfileStarted = "[Diagnóstico] Diagnóstico de 30 s iniciado; mantém o servidor a funcionar...";
        public const string SparkProfileStopped = "[Diagnóstico] Diagnóstico terminado; a ligação do relatório spark aparece no registo do servidor acima.";
        public const string SparkProfileCancelled = "[Diagnóstico] O servidor parou; esta sessão de diagnóstico foi cancelada.";
        public const string LangCaption = "Idioma";
        public const string LangRestartHint = "Idioma salvo. Reinicia a app para o aplicar.";
        public const string LangRestartNow = "Reiniciar agora";
        public const int EulaAgreeWidth = 230;
        public const int EulaLinkDx = 232;
    }

    static class TxtRu
    {
        public const string Msg = "Что-то пошло не так";
        public const string ShowFriendlyErrorStr = ", и программа должна закрыться.";
        public const string ShowFriendlyErrorStr2 = ", и программа продолжит работу.";
        public const string ShowFriendlyErrorPart = "\n\nЕсли это повторяется, отправьте эту информацию Naiwa.";
        public const string ShowFriendlyErrorShow = "Помощник для сетевой игры Minecraft";
        public const string YesText = "Сначала безопасно остановить сервер, затем выйти";
        public const string NoText = "Отмена";
        public const string SettingsFormInitChrome = "Настройки";
        public const string SettingsFormAddField = "Папка сервера D:\\MCServer";
        public const string SettingsFormAddField2 = "Полный путь к Java (java.exe)";
        public const string SettingsFormAddField3 = "Имя файла jar";
        public const string SettingsFormAddField4 = "Параметры памяти";
        public const string SettingsFormAddField5 = "Порт";
        public const string SettingsFormAddField6 = "Путь к frpc.exe";
        public const string LbModeCaption = "Способ туннеля";
        public const string SettingsFormAdd = "Командная строка frpc (предпочтительно)";
        public const string SettingsFormAdd2 = "Открыть официальный лаунчер SakuraFrp (запасной вариант)";
        public const string LbKeyCaption = "Ключ доступа SakuraFrp";
        public const string SettingsFormAddField7 = "ID туннеля";
        public const string SettingsFormAddField8 = "ID голосового туннеля (необязательно)";
        public const string SettingsFormAddField9 = "Публичный адрес (необязательно)";
        public const string SettingsFormAddField10 = "Путь к exe PCL2";
        public const string BtnPropsText = "Свойства сервера…";
        public const string SettingsFormShow = "Сначала укажите выше правильную папку сервера, а затем редактируйте свойства сервера.";
        public const string SettingsFormShow2 = "Свойства сервера";
        public const string BtnWizardText = "Мастер первой настройки";
        public const string BtnSaveText = "Сохранить";
        public const string ValidateSettingsMarkInvalid = "Папка сервера не существует. Проверьте путь.";
        public const string ValidateSettingsMarkInvalid2 = "Путь к Java не существует. Проверьте его.";
        public const string ValidateSettingsMarkInvalid3 = "Имя файла jar не может быть пустым.";
        public const string ValidateSettingsMarkInvalid4 = "Этот файл jar не найден в папке сервера.";
        public const string ValidateSettingsMarkInvalid5 = "Порт должен быть числом от 1 до 65535.";
        public const string ValidateSettingsMarkInvalid6 = "Путь к frpc.exe не существует. Проверьте его.";
        public const string ValidateSettingsMarkInvalid7 = "В режиме frpc ID туннеля не может быть пустым.";
        public const string ValidateSettingsMarkInvalid8 = "Путь к PCL2 не существует. Проверьте его.";
        public const string TitleText = "Помощник для сетевой игры Minecraft v6.22.5";
        public const string SubText = "Двойной туннель · Игра + Голос";
        public const string ServerText = "Сервер: остановлен";
        public const string TunnelText = "Туннель SakuraFrp: остановлен";
        public const string AddrText = "Публичный адрес: не создан";
        public const string SrvState = "Остановлен";
        public const string MbText = "Игроки";
        public const string OnPaintHeader = "Процесс сервера";
        public const string OnPaintRow = "Состояние";
        public const string OnPaintRow2 = "Использование памяти";
        public const string OnPaintRow3 = "Подключения игроков";
        public const string OnPaintDrawText = "Игроки онлайн";
        public const string OnPaintRow4 = "Версия · Ping";
        public const string OnPaintHeader2 = "Процесс туннеля (frpc)";
        public const string OnPaintHeader3 = "Локальный";
        public const string OnPaintDrawText2 = "Входящий";
        public const string OnPaintDrawText3 = "Исходящий";
        public const string OnPaintRow5 = "Системная память";
        public const string OnHandleCreatedIndexOf = "Ошибка";
        public const string OnHandleCreatedStr = "Ошибка";
        public const string ShowMsg = "ОК";
        public const string PropsFormAddCombo = "Сложность";
        public const string PropsFormStr = "Мирная";
        public const string PropsFormStr2 = "Лёгкая";
        public const string PropsFormStr3 = "Нормальная";
        public const string PropsFormStr4 = "Сложная";
        public const string PropsFormAddCombo2 = "Режим игры";
        public const string PropsFormStr5 = "Выживание";
        public const string PropsFormStr6 = "Творчество";
        public const string PropsFormStr7 = "Приключение";
        public const string PropsFormStr8 = "Наблюдение";
        public const string PropsFormAddNum = "Максимум игроков";
        public const string PropsFormAddNum2 = "Дальность прорисовки (чанки)";
        public const string PropsFormAddNum3 = "Дальность симуляции (чанки)";
        public const string PropsFormAddNum4 = "Радиус защиты спауна";
        public const string PropsFormAddText = "MOTD сервера";
        public const string PropsFormAddCheck = "Включить белый список (white-list)";
        public const string PropsFormAddCheck2 = "Разрешить PvP (pvp)";
        public const string PropsFormAddCheck3 = "Разрешить полёт (allow-flight)";
        public const string LbOnCaption = "Сетевой режим: выкл (для офлайн-игры должен оставаться выключенным; здесь его изменить нельзя)";
        public const string PropsFormShow = "Файл server.properties не найден в папке сервера. Он создаётся при первом успешном запуске сервера.";
        public const string SavePropsShow = "Максимум игроков должен быть числом от 1 до 9999.";
        public const string SavePropsShow2 = "Дальность прорисовки должна быть числом от 2 до 32.";
        public const string SavePropsShow3 = "Дальность симуляции должна быть числом от 2 до 32.";
        public const string SavePropsShow4 = "Радиус защиты спауна должен быть числом от 0 до 9999.";
        public const string SavePropsShow5 = "Не удалось сохранить: ";
        public const string SavePropsShow6 = "Сохранено. Сервер работает, поэтому настройки вступят в силу при следующем запуске.";
        public const string PlayerFormStr = "Игроки онлайн (";
        public const string PlayerFormStr2 = "Сейчас игроков онлайн нет";
        public const string PlayerFormAddBtn = "Телепортировать к себе";
        public const string PlayerFormAddBtn2 = "Выгнать с сервера";
        public const string PlayerFormConfirmDo = "Выгнать ";
        public const string PlayerFormConfirmDo2 = " с сервера?";
        public const string PlayerFormAddBtn3 = "Дать OP";
        public const string PlayerFormAddBtn4 = "Снять OP";
        public const string PlayerFormAddBtn5 = "Забанить";
        public const string PlayerFormConfirmDo3 = "Забанить ";
        public const string PlayerFormConfirmDo4 = "? Он не сможет заходить на сервер (бан можно снять позже).";
        public const string PlayerFormAddBtn6 = "Разбанить";
        public const string PlayerFormStr3 = "Примечание: «Телепортировать к себе» недоступно, пока вы сами не в сети.";
        public const string PlayerFormStr4 = "Действия отправляются серверу как консольные команды, так же как при вводе в поле команд.";
        public const string CloseText = "Закрыть";
        public const string LbOpsCaption = "Список OP: ";
        public const string RefreshListsStr = "(нет)";
        public const string LbBannedCaption = "Список банов: ";
        public const string WizardFormInitChrome = "Мастер первой настройки";
        public const string WizardFormAddLabel = "Версия Minecraft";
        public const string WizardFormAddHint = "Подходит любая релизная версия";
        public const string WizardFormAddLabel2 = "Тип сервера";
        public const string WizardFormAddRadio = "Ванильный";
        public const string WizardFormAddLabel3 = "Папка сервера";
        public const string BtnBrowseText = "Обзор...";
        public const string Description = "Выберите папку сервера (она не обязана существовать — выберите родительскую папку и измените текст пути)";
        public const string ChkEulaText = "Я принимаю Minecraft EULA";
        public const string LnkEulaText = "Читать EULA полностью";
        public const string WizardFormAddHint2 = "Без отметки вам придётся самому установить eula=true в eula.txt перед первым запуском";
        public const string LblStepCaption = "Заполните три пункта выше и нажмите «Начать установку». Java и сервер будут скачаны и проверены автоматически.";
        public const string BtnStartText = "Начать установку";
        public const string WizardFormWizLog = "Отмена...";
        public const string StartInstallShow = "Сначала введите версию Minecraft.";
        public const string StartInstallShow2 = "Это не похоже на номер релизной версии. Введите что-то вроде 1.20.1 (снапшоты мастером не поддерживаются).";
        public const string StartInstallShow3 = "Сначала введите папку сервера.";
        public const string StartInstallShow4 = "В папке назначения уже есть server.jar. Продолжение перезапишет его.\n\nВаш мир и моды не затрагиваются (мастер только заменяет jar и добавляет недостающие файлы). Продолжить?";
        public const string StartInstallShow5 = "Перезаписать и продолжить";
        public const string StartInstallWizLog = "-- Установка начата: Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "Невозможно определить, какая Java нужна Minecraft ";
        public const string RunFail2 = "; мастер прерван (ничего не было изменено).";
        public const string RunWizLog = "Примечание: 26.x — новая годовая нумерация версий Mojang; официальное требование к Java может быть выше использованной здесь оценки Java 21 — следите за логом сервера после установки.";
        public const string RunWizStep = "Шаг 1: подготовка Java ";
        public const string RunWizLog2 = "Настроенная Java имеет основную версию ";
        public const string RunWizLog3 = " и соответствует требованию; используется она: ";
        public const string RunWizLog4 = "Используется ранее скачанная Java рядом с приложением: ";
        public const string RunFail3 = " нужна Java ";
        public const string RunFail4 = ", но этот мастер может автоматически скачать только Java 17 / 21. Установите Java ";
        public const string RunFail5 = " вручную, укажите её java.exe в Настройках, а затем запустите сервер.";
        public const string RunWizLog5 = "API ресурсов Adoptium не дала прямой ссылки (";
        public const string RunStr = "без подробностей";
        public const string RunPart = "); используется официальный бинарный endpoint, который не публикует контрольную сумму — эта загрузка Java не будет проверена по хешу.";
        public const string RunWizLog6 = "Скачивание Java ";
        public const string RunWizLog7 = " (Eclipse Temurin JRE, около 40-50 МБ)...";
        public const string RunFail6 = "Скачивание Java не удалось: ";
        public const string RunWizLog8 = "Проверка SHA256 архива Java...";
        public const string RunFail7 = "SHA256 архива Java не совпал; загрузка удалена. Ожидалось ";
        public const string RunFail8 = ", получено ";
        public const string RunFail9 = ". Проверьте сеть и повторите попытку.";
        public const string RunWizLog9 = "SHA256 подтверждён.";
        public const string RunWizLog10 = "Распаковка Java в ";
        public const string RunWizLog11 = " ...";
        public const string RunFail10 = "Распаковка Java не удалась: ";
        public const string RunFail11 = "Распаковка завершена, но bin\\java.exe не найден — неожиданная структура архива; прервано (настройки не сохранены).";
        public const string RunFail12 = "Скачанная Java сообщает основную версию ";
        public const string RunFail13 = ", что всё ещё ниже требуемой ";
        public const string RunFail14 = "; прервано (настройки не сохранены).";
        public const string RunWizLog12 = "Java готова: основная версия ";
        public const string RunWizLog13 = ", ";
        public const string RunWizStep2 = "Шаг 2: скачивание jar сервера";
        public const string RunFail15 = "Не удаётся получить манифест версий Mojang: ";
        public const string RunFail16 = "Minecraft ";
        public const string RunFail17 = " отсутствует в манифесте версий (проверьте номер; только релизы).";
        public const string RunFail18 = "Не удаётся получить метаданные версии ";
        public const string RunFail19 = ": ";
        public const string RunFail20 = "В метаданных версии нет загрузки сервера.";
        public const string RunWizLog14 = "Скачивание ванильного сервера (официальный Mojang)...";
        public const string RunDownloadWithProgress = "ванильный сервер";
        public const string RunFail21 = "Скачивание сервера не удалось: ";
        public const string RunWizLog15 = "Проверка SHA1 сервера...";
        public const string RunFail22 = "SHA1 сервера не совпал; jar удалён. Ожидалось ";
        public const string RunWizLog16 = "SHA1 подтверждён.";
        public const string RunFail23 = "Не удаётся получить список загрузчиков Fabric: ";
        public const string RunFail24 = "У Fabric пока нет стабильного загрузчика для Minecraft ";
        public const string RunFail25 = "; выберите другую версию.";
        public const string RunFail26 = "Не удаётся получить список установщиков Fabric: ";
        public const string RunFail27 = "Список установщиков Fabric выглядит неверным.";
        public const string RunWizLog17 = "Скачивание стартового jar сервера Fabric (загрузчик ";
        public const string RunWizLog18 = ", установщик ";
        public const string RunWizLog19 = ")...";
        public const string RunWizLog20 = "Примечание: Fabric не публикует хеш для этого jar, поэтому он защищён только HTTPS и не проверяется по хешу. При первом запуске он сам скачает ванильный сервер и библиотеки.";
        public const string RunDownloadWithProgress2 = "сервер Fabric";
        public const string RunFail28 = "Скачивание сервера Fabric не удалось: ";
        public const string RunWizLog21 = "Папка mods создана. Напоминание: серверам Fabric обычно также нужен мод Fabric API в папке mods, а каждому игроку нужны те же двусторонние моды в клиенте.";
        public const string RunFail29 = "Не удаётся получить список сборок Paper (Fill API): ";
        public const string RunFail30 = ". Примечание: версии без сборок Paper возвращают здесь ошибку.";
        public const string RunFail31 = "В списке сборок Paper не найден сервер для скачивания.";
        public const string RunWizLog22 = "Скачивание сервера Paper (последняя сборка)...";
        public const string RunDownloadWithProgress3 = "сервер Paper";
        public const string RunFail32 = "Скачивание сервера Paper не удалось: ";
        public const string RunWizLog23 = "Проверка SHA256 сервера...";
        public const string RunFail33 = "SHA256 сервера не совпал; jar удалён. Ожидалось ";
        public const string RunWizStep3 = "Шаг 3: запись начальных файлов";
        public const string RunWizLog24 = "Записан eula.txt (eula=true, как вы согласились).";
        public const string RunWizLog25 = "EULA не принята: eula.txt не записан. При первом запуске сервер создаст eula=false и завершится; измените на eula=true для запуска.";
        public const string RunWizLog26 = "Записан server.properties: server-port=25565, online-mode=false (обязательно для офлайн-игры).";
        public const string RunWizLog27 = "Файл server.properties уже существует и оставлен без изменений. Убедитесь сами, что online-mode=false, иначе офлайн-аккаунты не смогут войти.";
        public const string RunWizStep4 = "Шаг 4: сохранение настроек";
        public const string RunWizLog28 = "Готово. Папка сервера: ";
        public const string RunWizLog29 = "; jar: server.jar; Java: ";
        public const string RunStr2 = "(сохранена прежняя настройка)";
        public const string RunWizLog30 = "Закройте мастер, заполните ключ доступа SakuraFrp и ID туннелей в Настройках, а затем нажмите «Запуск сетевой игры».";
        public const string RunWizStep5 = "Установка завершена";
        public const string RunFail34 = "Ошибка мастера: ";
        public const string DownloadWithProgressWizStep = "Скачивание ";
        public const string DownloadWithProgressWizStep2 = "... ";
        public const string FailWizLog = "Не удалось: ";
        public const string FailWizStep = "Остановлено";
        public const string CancelledWizLog = "Отменено. Настройки не были сохранены; полностью скачанные файлы остались в папке назначения, частичные файлы были удалены.";
        public const string CancelledWizStep = "Отменено";
        public const string MainFormAppendServerLog = "[Лаунчер] Запущено. Логи также сохраняются в папку logs рядом с программой (хранятся 14 дней).";
        public const string TrayText = "Помощник для сетевой игры Minecraft · Остановлено";
        public const string InitTray613ToolStripMenuItem = "Показать главное окно";
        public const string InitTray613ToolStripMenuItem2 = "Безопасный выход";
        public const string BtnStartText2 = "Запуск сетевой игры";
        public const string AddrLblCaption = "Публичный адрес (отправьте друзьям): ";
        public const string Hint = "Публичный адрес появится здесь, когда туннель запустится";
        public const string BtnCopyText = "Копировать адрес для друзей";
        public const string BtnPcl2Text = "Открыть PCL2";
        public const string BtnStopText = "Безопасно остановить сервер";
        public const string LogToggleLblCaption = "▾  Лог";
        public const string BuildUiStr = "Сервер";
        public const string BuildUiStr2 = "Туннель";
        public const string BuildUiStr3 = "Статус";
        public const string BuildUiStr4 = "Копии";
        public const string BuildUiMakeLogBox = "Логов пока нет · Вывод сервера появится здесь";
        public const string BuildUiMakeLogBox2 = "Логов пока нет · Вывод туннеля появится здесь";
        public const string BtnJumpBottomText = "↓ Вернуться вниз";
        public const string Hint2 = "Поиск по логам…";
        public const string BuildUiSetToolTip = "Назад";
        public const string BuildUiSetToolTip2 = "Вперёд";
        public const string BuildUiSetToolTip3 = "Поиск по логу; Enter — переход к следующему совпадению";
        public const string ChkErrOnlyText = "Только ошибки";
        public const string CmdLblCaption = "Команда сервера";
        public const string BtnCmdSendText = "Отправить";
        public const string BuildUiAppendState = "Первое использование: заполните ключ доступа SakuraFrp и ID туннеля в Настройках и проверьте путь к PCL2.";
        public const string IsErrLineContains = "[Watchdog]";
        public const string IsErrLineContains2 = "[Копия] Ошибка";
        public const string LblFindCaption = "Нет результатов";
        public const string SrvState2 = "Работает · ";
        public const string SampleStatsPart = " игрок(а)";
        public const string SampleStatsPart2 = " игрок(а) онлайн (список не публикуется)";
        public const string SampleStatsPart3 = "Ping ";
        public const string ToggleLogStr = "▸  Лог";
        public const string AppendServerLogPersistLog613 = "Сервер";
        public const string AppendTunnelLogPersistLog613 = "Туннель";
        public const string SendServerCommandAppendState = "Сервер не работает; команду нельзя отправить.";
        public const string SendServerCommandAppendState2 = "Не удалось отправить команду: процесс сервера уже завершён.";
        public const string SendServerCommandAppendState3 = "Не удалось отправить команду: ";
        public const string OpenPlayerManagerShow = "Сервер не работает; управление игроками доступно после запуска сервера.";
        public const string OpenPlayerManagerAppendState = "Команда не удалась: процесс сервера уже завершён.";
        public const string DoBackupAppendState = "Резервное копирование уже выполняется.";
        public const string BackupCoreAppendState = "Папка мира не найдена: ";
        public const string BackupCoreAppendState2 = ", копирование невозможно.";
        public const string BackupCoreAppendServerLog = "[Копия] Автосохранение сервера приостановлено и сброшено на диск; упаковка мира…";
        public const string BackupCoreAppendServerLog2 = "[Копия] Готово: ";
        public const string BackupCorePart = " файлов";
        public const string BackupCoreStr = ", пропущено ";
        public const string BackupCorePart2 = " файл(ов) используется";
        public const string BackupCoreAppendState3 = "Резервная копия мира завершена (";
        public const string BackupCoreAppendServerLog3 = "[Копия] Ошибка: ";
        public const string BackupCoreAppendState4 = "Резервное копирование не удалось: ";
        public const string BackupCoreAppendServerLog4 = "[Копия] Автосохранение сервера возобновлено.";
        public const string RestoreBackupShow = "Перед восстановлением нужно остановить сервер: замена файлов мира при работающем сервере повредит сохранение.\n\nСначала нажмите «Безопасно остановить сервер», а затем восстанавливайте.";
        public const string RestoreBackupShow2 = "Восстановить копию";
        public const string RestoreBackupConfirmBox = "Восстановить мир из резервной копии «";
        public const string RestoreBackupConfirmBox2 = "»?\n\nТекущий мир не будет удалён: сначала он переименовывается и откладывается в сторону (world_prerestore_<время>); удалите его сами, когда всё проверите. Если восстановление оказалось неверным, можно вернуть его обратно.";
        public const string RestoreBackupAppendState = "Задача резервного копирования уже выполняется.";
        public const string Result = "Восстановлено из копии «";
        public const string RestoreBackupPart = "». Старый мир сохранён в ";
        public const string RestoreBackupPart2 = "; когда новый мир будет проверен, его можно удалить.";
        public const string Result2 = "Восстановление не удалось: ";
        public const string RestoreBackupPart3 = "(Старый мир, возможно, уже переименован. Пока не запускайте сервер; проверьте папку сервера.)";
        public const string RestoreBackupAppendServerLog = "[Копия] ";
        public const string BuildBackupPanelBkLabel = "Резервные копии мира";
        public const string BtnBackupNowText = "Сделать копию сейчас";
        public const string BuildBackupPanelDoBackup = "Ручная копия";
        public const string BtnRestoreBackupText = "Восстановить выбранную";
        public const string BuildBackupPanelShow = "Сначала выберите копию из списка выше.";
        public const string BtnDeleteBackupText = "Удалить выбранную";
        public const string BuildBackupPanelShow2 = "Удалить копию";
        public const string BuildBackupPanelConfirmBox = "Удалить резервную копию «";
        public const string BuildBackupPanelPart = "»? После удаления её нельзя будет восстановить.";
        public const string BuildBackupPanelShow3 = "Удаление не удалось: ";
        public const string BuildBackupPanelBkLabel2 = "Хранить последние ";
        public const string BuildBackupPanelBkLabel3 = " копий";
        public const string BuildBackupPanelBkCheck = "Автоперезапуск при сбое";
        public const string BuildBackupPanelBkCheck2 = "Автокопия перед запуском сервера";
        public const string BuildBackupPanelBkCheck3 = "Ежедневная автокопия";
        public const string BuildBackupPanelBkLabel4 = "Пока приложение открыто, делать копию автоматически в заданное время";
        public const string BuildBackupPanelBkCheck4 = "Команда по расписанию";
        public const string LblBackupStateCaption = "Копирование… (большие миры могут занять минуту-две)";
        public const string RefreshBackupListStr = "Всего ";
        public const string RefreshBackupListPart = " копий · ";
        public const string RefreshBackupListStr2 = "Копий пока нет";
        public const string RefreshBackupListStr3 = "Последняя: ";
        public const string RefreshBackupListStr4 = "Нажмите «Сделать копию сейчас», чтобы сохранить снимок мира.";
        public const string CheckSchedulesAppendServerLog = "[Расписание] Наступило время ежедневной копии; запуск автоматического копирования.";
        public const string CheckSchedulesAppendServerLog2 = "[Расписание] Команда по расписанию должна выполниться, но ";
        public const string CheckSchedulesAppendServerLog3 = " игрок(а) всё ещё онлайн; откладывается, пока сервер не опустеет.";
        public const string CheckSchedulesAppendServerLog4 = "[Расписание] Отправлена команда по расписанию: ";
        public const string ServerText2 = "Сервер: работает";
        public const string ServerText3 = "Сервер: запускается…";
        public const string TunnelText2 = "Туннель SakuraFrp: подключён";
        public const string TunnelText3 = "Туннель SakuraFrp: запускается…";
        public const string AddrText2 = "Публичный адрес: готов";
        public const string AddrText3 = "Публичный адрес: сохранён (туннель не работает)";
        public const string Tt = "Помощник для сетевой игры Minecraft · ";
        public const string RefreshStatusStr = "Работает";
        public const string RefreshStatusStr2 = "Запускается";
        public const string CopyAddrShow = "Публичного адреса пока нет. Сначала запустите сетевую игру.";
        public const string CopyAddrShow2 = "Уведомление";
        public const string CopyAddrAppendState = "Публичный адрес скопирован в буфер обмена; отправьте его друзьям.";
        public const string BtnCopyText2 = "Скопировано ✓";
        public const string OpenPcl2Show = "PCL2 не найден. Укажите путь к exe PCL2 в Настройках.";
        public const string OpenPcl2Show2 = "Не удалось открыть PCL2: ";
        public const string HandleServerCrashAppendServerLog = "[Watchdog] Сервер неожиданно завершился (код выхода ";
        public const string HandleServerCrashAppendServerLog2 = "); это не было обычной остановкой.";
        public const string HandleServerCrashStr = "Возможная причина: ";
        public const string HandleServerCrashAppendState = "Сервер неожиданно завершился";
        public const string HandleServerCrashStr2 = ", возможная причина: ";
        public const string HandleServerCrashStr3 = ". Ищите причину в конце лога сервера.";
        public const string HandleServerCrashPart = "(Автоперезапуск watchdog отключён)";
        public const string HandleServerCrashAppendServerLog3 = "[Watchdog] Сбоев ";
        public const string HandleServerCrashAppendServerLog4 = " за 10 минут; автоперезапуск остановлен. Скорее всего, проблема с сохранением или модом.";
        public const string HandleServerCrashAppendState2 = "Сервер продолжал сбоить; автоперезапуск остановлен. Проверьте лог.";
        public const string HandleServerCrashStr4 = " Возможная причина: ";
        public const string HandleServerCrashAppendState3 = "Сервер неожиданно завершился; watchdog перезапустит его через 5 секунд…";
        public const string GuessCrash614Ret = "Порт ";
        public const string GuessCrash614Part = " уже используется — вероятно, другой экземпляр сервера не был закрыт корректно";
        public const string GuessCrash614Ret2 = "Нехватка памяти или неверная настройка памяти (-Xmx)";
        public const string GuessCrash614Ret3 = "Неверная версия Java — этому серверу нужна Java 17";
        public const string GuessCrash614Ret4 = "Сервер завис более чем на 60 секунд и был остановлен встроенным watchdog игры";
        public const string GuessCrash614Ret5 = "Мод не загрузился; найдите мод, названный в ошибках лога";
        public const string GuessCrash614Ret6 = "EULA не принята";
        public const string OfferWizardIfNeededShow = "Используемого сервера пока нет: настроенная папка сервера не существует или не содержит jar сервера.\n\nМастер первой настройки может скачать подходящие Java и сервер (ванильный / Fabric / Paper) и записать EULA и server.properties, нужные для офлайн-игры. Все загрузки проверяются по хешу.";
        public const string OfferWizardIfNeededShow2 = "Открыть мастер";
        public const string OfferWizardIfNeededShow3 = "Позже";
        public const string OpenWizardAppendState = "Мастер первой настройки завершён: сервер и Java готовы.";
        public const string CheckJavaVersionStr = "неизвестна";
        public const string CheckJavaVersionAppendServerLog = "[Проверка Java] Невозможно прочитать сведения о версии jar сервера (version.json); проверка версии пропущена. Текущая основная версия Java: ";
        public const string CheckJavaVersionAppendServerLog2 = ".";
        public const string CheckJavaVersionAppendServerLog3 = "[Проверка Java] Версия сервера ";
        public const string CheckJavaVersionAppendServerLog4 = " отсутствует в таблице версий; проверка версии пропущена. Текущая основная версия Java: ";
        public const string CheckJavaVersionAppendServerLog5 = "[Проверка Java] Сервер ";
        public const string CheckJavaVersionAppendServerLog6 = ", но текущую версию Java измерить не удалось; запуск продолжается (следите за логом сервера).";
        public const string CheckJavaVersionAppendServerLog7 = "[Проверка Java] Пройдена: сервер ";
        public const string CheckJavaVersionAppendServerLog8 = ", текущая Java — ";
        public const string CheckJavaVersionAppendServerLog9 = "[Проверка Java] Несоответствие: сервер ";
        public const string CheckJavaVersionAppendServerLog10 = ", текущая Java — только ";
        public const string CheckJavaVersionAppendServerLog11 = "; ожидание решения пользователя.";
        public const string Msg2 = "Версия Java слишком старая — сервер, скорее всего, не запустится.\n\nВерсия сервера: ";
        public const string CheckJavaVersionPart = ", которой нужна Java ";
        public const string CheckJavaVersionPart2 = " или новее. Используемая Java имеет основную версию ";
        public const string CheckJavaVersionPart3 = ".\n\nСовет: укажите в Настройках java.exe от Java ";
        public const string CheckJavaVersionPart4 = " или позвольте мастеру первой настройки автоматически скачать подходящую Java.\nВыбор «Запустить всё равно» запустит сервер с текущей Java в любом случае; ошибки смотрите в логе сервера.";
        public const string CheckJavaVersionShow = "Несоответствие версии Java";
        public const string CheckJavaVersionShow2 = "Запустить всё равно";
        public const string CheckJavaVersionShow3 = "Отменить запуск";
        public const string CheckJavaVersionAppendServerLog12 = "[Проверка Java] Пользователь решил запустить всё равно.";
        public const string CheckJavaVersionAppendServerLog13 = "[Проверка Java] Пользователь отменил запуск.";
        public const string StartAllAppendState = "Сервер уже работает; запускать его снова не нужно.";
        public const string BtnStartText3 = "Сетевая игра работает";
        public const string BtnStartText4 = "Запуск…";
        public const string DoStartFailState = "Папка сервера не существует: ";
        public const string DoStartFailState2 = ". Проверьте папку сервера в Настройках.";
        public const string DoStartFailState3 = "Jar сервера не найден: ";
        public const string DoStartFailState4 = ". Проверьте имя файла jar в Настройках.";
        public const string DoStartFailState5 = "Java (java.exe) не найдена. Заново выберите путь к Java в Настройках — обычно это .minecraft\\runtime или папка установки Java.";
        public const string DoStartFailState6 = "Запуск отменён: версия Java ниже, чем нужно серверу. Переключитесь на подходящую Java или позвольте мастеру первой настройки скачать её, затем повторите попытку.";
        public const string DoStartFailState7 = " уже используется. Возможно, прошлый сервер не закрылся корректно — нажмите «Безопасно остановить сервер» или перезапустите это приложение; не завершайте процессы Java в Диспетчере задач сами.";
        public const string DoStartAppendState = "Автокопия перед запуском сервера…";
        public const string DoStartAppendState2 = "Запуск сервера Minecraft…";
        public const string DoStartAppendServerLog = "[Процесс завершён]";
        public const string DoStartFailState8 = "Не удаётся запустить процесс Java. Проверьте правильность пути к Java.";
        public const string DoStartAppendState3 = "Ожидание готовности сервера (отслеживается «Done»)…";
        public const string DoStartFailState9 = "Процесс сервера завершился до появления «Done». Вероятнее всего: 1) неверный путь к Java или параметры памяти; 2) ошибка в сохранении/мире или модах (смотрите конец лога сервера). Не трогайте мир и моды.";
        public const string DoStartFailState10 = "Нет «Done» за 90 секунд. Возможно, не хватает памяти, мир слишком большой или запуск завис. Смотрите конец лога сервера; не трогайте мир и моды.";
        public const string DoStartAppendState4 = "Готово. Нажмите «Копировать адрес для друзей» и отправьте адрес друзьям.";
        public const string DoStartFailState11 = "Запуск не удался: ";
        public const string StartTunnelAppendTunnelLog = "[Запасной вариант] Открытие официального лаунчера SakuraFrp — нажмите в нём «Запуск туннеля».";
        public const string StartTunnelAppendTunnelLog2 = "Не удалось открыть лаунчер SakuraFrp: ";
        public const string StartTunnelAppendTunnelLog3 = "frpc.exe не найден: ";
        public const string StartTunnelAppendTunnelLog4 = ". Проверьте его в Настройках или переключите способ туннеля на официальный лаунчер.";
        public const string StartTunnelAppendTunnelLog5 = "Ключ доступа пуст. Заполните ключ доступа SakuraFrp в Настройках.";
        public const string StartTunnelAppendTunnelLog6 = "ID туннеля пуст. Заполните его в Настройках.";
        public const string StartTunnelAppendState = "Запуск туннеля SakuraFrp…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc завершён]";
        public const string StartTunnelAppendTunnelLog8 = "[Watchdog] Игровой туннель неожиданно завершился; переподключение через 5 секунд.";
        public const string StartTunnelAppendTunnelLog9 = "frpc не запустился. Проверьте путь к frpc.exe.";
        public const string StartTunnelAppendTunnelLog10 = "Текущий frpc.exe — это сборка, поставляемая с лаунчером SakuraFrp. Эту сборку нельзя запускать напрямую внешними программами (только через SakuraFrp Service), поэтому она сразу завершается. Это не связано с вашим ключом доступа, ID туннеля или узлом. В Настройках укажите путь к автономному frpc или переключите способ туннеля на официальный лаунчер.";
        public const string StartTunnelAppendTunnelLog11 = "Процесс frpc завершился. Возможно: 1) неверный ключ доступа или ID туннеля; 2) проблема узла; 3) аккаунту нужна верификация или отметка. Проверьте ключ и ID туннеля в Настройках или переключите способ туннеля на официальный лаунчер.";
        public const string StartTunnelAppendState2 = "Этот frpc — встроенная сборка лаунчера и не может работать напрямую. В Настройках переключитесь на автономный frpc или режим официального лаунчера.";
        public const string StartVoiceTunnelAppendTunnelLog = "[Голос] ID голосового туннеля не задан; голосовой туннель пропущен.";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[Голос frpc завершён]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[Watchdog] Голосовой туннель неожиданно завершился; переподключение через 5 секунд.";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[Голос] frpc не запустился. Проверьте путь к frpc.exe.";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[Голос] Процесс frpc завершился. Проверьте правильность ID голосового туннеля.";
        public const string StartVoiceTunnelAppendState = "Голосовой туннель подключён.";
        public const string VoiceFrpcOutputAppendTunnelLog = "[Голос] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[Голос][ERR] ";
        public const string TryParseFrpcAppendState = "Публичный адрес получен автоматически: ";
        public const string ServerText4 = "Сервер: ошибка";
        public const string TunnelText4 = "Туннель SakuraFrp: ошибка";
        public const string SafeStopAppendState = "Сервер не работает; туннель остановлен.";
        public const string SafeStopAppendState2 = "Безопасная остановка: отправка stop серверу и ожидание сохранения…";
        public const string SafeStopAppendServerLog = ">> stop отправлен; ожидание сохранения и выхода (до 60 секунд)";
        public const string SafeStopAppendState3 = "Сервер остановился нормально (сохранение завершено). Закрытие туннеля…";
        public const string SafeStopAppendServerLog2 = "[Процесс сервера завершён нормально]";
        public const string SafeStopShow = "Сервер всё ещё не завершился за 60 секунд. Принудительное завершение может потерять последние несколько минут прогресса. Завершить принудительно?";
        public const string SafeStopShow2 = "Превышено время безопасной остановки";
        public const string SafeStopShow3 = "Завершить принудительно";
        public const string SafeStopShow4 = "Подождать ещё";
        public const string SafeStopAppendState4 = "Процесс сервера был завершён принудительно (последний прогресс может быть потерян).";
        public const string SafeStopAppendState5 = "Принудительное завершение отменено; сервер всё ещё работает.";
        public const string SafeStopAppendState6 = "Всё остановлено: сервер и туннель закрыты.";
        public const string OnClosingConfirmBox = "Сервер всё ещё работает. Прямое закрытие может пропустить сохранение — сначала остановить безопасно?";
        public const string BtnSparkTpsText = "Отчёт TPS";
        public const string BtnSparkProfileText = "Диагностика 30 с";
        public const string SparkLblCaption = "Диагностика";
        public const string SparkProfileStarted = "[Диагностика] Начата 30-секундная диагностика; оставьте сервер включённым...";
        public const string SparkProfileStopped = "[Диагностика] Диагностика завершена; ссылка на отчёт spark появится в логе сервера выше.";
        public const string SparkProfileCancelled = "[Диагностика] Сервер остановился; этот сеанс диагностики отменён.";
        public const string LangCaption = "Язык";
        public const string LangRestartHint = "Настройки языка сохранены. Перезапустите приложение, чтобы применить изменения.";
        public const string LangRestartNow = "Перезапустить сейчас";
        public const int EulaAgreeWidth = 230;
        public const int EulaLinkDx = 232;
    }

    static class TxtDe
    {
        public const string Msg = "Etwas ist schiefgelaufen";
        public const string ShowFriendlyErrorStr = ", und das Programm muss geschlossen werden.";
        public const string ShowFriendlyErrorStr2 = ", und das Programm läuft weiter.";
        public const string ShowFriendlyErrorPart = "\n\nWenn es immer wieder passiert, sende diese Informationen einfach an Naiwa.";
        public const string ShowFriendlyErrorShow = "Minecraft Mehrspieler-Helfer";
        public const string YesText = "Server vor dem Beenden sicher stoppen";
        public const string NoText = "Abbrechen";
        public const string SettingsFormInitChrome = "Einstellungen";
        public const string SettingsFormAddField = "Serverordner D:\\MCServer";
        public const string SettingsFormAddField2 = "Vollständiger Java-Pfad (java.exe)";
        public const string SettingsFormAddField3 = "Name der jar-Datei";
        public const string SettingsFormAddField4 = "Speicherargumente";
        public const string SettingsFormAddField5 = "Port";
        public const string SettingsFormAddField6 = "Pfad zu frpc.exe";
        public const string LbModeCaption = "Tunnelmodus";
        public const string SettingsFormAdd = "frpc-Befehlszeile (bevorzugt)";
        public const string SettingsFormAdd2 = "Offiziellen SakuraFrp Launcher öffnen (Ersatz)";
        public const string LbKeyCaption = "SakuraFrp Zugriffsschlüssel";
        public const string SettingsFormAddField7 = "Tunnel-ID";
        public const string SettingsFormAddField8 = "Sprachtunnel-ID (optional)";
        public const string SettingsFormAddField9 = "Öffentliche Adresse (optional)";
        public const string SettingsFormAddField10 = "Pfad zur PCL2 exe";
        public const string BtnPropsText = "Servereigenschaften…";
        public const string SettingsFormShow = "Lege zuerst oben den richtigen Serverordner fest und bearbeite dann die Servereigenschaften.";
        public const string SettingsFormShow2 = "Servereigenschaften";
        public const string BtnWizardText = "Assistent für die Ersteinrichtung";
        public const string BtnSaveText = "Speichern";
        public const string ValidateSettingsMarkInvalid = "Der Serverordner existiert nicht. Prüfe den Pfad.";
        public const string ValidateSettingsMarkInvalid2 = "Der Java-Pfad existiert nicht. Prüfe ihn.";
        public const string ValidateSettingsMarkInvalid3 = "Der Name der jar-Datei darf nicht leer sein.";
        public const string ValidateSettingsMarkInvalid4 = "Diese jar-Datei wurde im Serverordner nicht gefunden.";
        public const string ValidateSettingsMarkInvalid5 = "Der Port muss eine Zahl zwischen 1 und 65535 sein.";
        public const string ValidateSettingsMarkInvalid6 = "Der Pfad zu frpc.exe existiert nicht. Prüfe ihn.";
        public const string ValidateSettingsMarkInvalid7 = "Im frpc-Modus darf die Tunnel-ID nicht leer sein.";
        public const string ValidateSettingsMarkInvalid8 = "Der PCL2-Pfad existiert nicht. Prüfe ihn.";
        public const string TitleText = "Minecraft Mehrspieler-Helfer v6.22.5";
        public const string SubText = "Doppeltunnel · Spiel + Sprache";
        public const string ServerText = "Server: Gestoppt";
        public const string TunnelText = "SakuraFrp Tunnel: Gestoppt";
        public const string AddrText = "Öffentliche Adresse: Nicht erstellt";
        public const string SrvState = "Gestoppt";
        public const string MbText = "Spieler";
        public const string OnPaintHeader = "Serverprozess";
        public const string OnPaintRow = "Status";
        public const string OnPaintRow2 = "Speicherverbrauch";
        public const string OnPaintRow3 = "Spielerverbindungen";
        public const string OnPaintDrawText = "Spieler online";
        public const string OnPaintRow4 = "Version · Ping";
        public const string OnPaintHeader2 = "Tunnelprozess (frpc)";
        public const string OnPaintHeader3 = "Lokal";
        public const string OnPaintDrawText2 = "Download";
        public const string OnPaintDrawText3 = "Upload";
        public const string OnPaintRow5 = "Systemspeicher";
        public const string OnHandleCreatedIndexOf = "Fehler";
        public const string OnHandleCreatedStr = "Fehler";
        public const string ShowMsg = "OK";
        public const string PropsFormAddCombo = "Schwierigkeit";
        public const string PropsFormStr = "Friedlich";
        public const string PropsFormStr2 = "Einfach";
        public const string PropsFormStr3 = "Normal";
        public const string PropsFormStr4 = "Schwer";
        public const string PropsFormAddCombo2 = "Spielmodus";
        public const string PropsFormStr5 = "Überleben";
        public const string PropsFormStr6 = "Kreativ";
        public const string PropsFormStr7 = "Abenteuer";
        public const string PropsFormStr8 = "Zuschauer";
        public const string PropsFormAddNum = "Maximale Spielerzahl";
        public const string PropsFormAddNum2 = "Sichtweite (Chunks)";
        public const string PropsFormAddNum3 = "Simulationsdistanz (Chunks)";
        public const string PropsFormAddNum4 = "Radius des Spawn-Schutzes";
        public const string PropsFormAddText = "Server-MOTD";
        public const string PropsFormAddCheck = "Whitelist aktivieren (white-list)";
        public const string PropsFormAddCheck2 = "PvP erlauben (pvp)";
        public const string PropsFormAddCheck3 = "Fliegen erlauben (allow-flight)";
        public const string LbOnCaption = "Online-Modus: Aus (muss für Offline-Mehrspieler aus bleiben; kann hier nicht geändert werden)";
        public const string PropsFormShow = "server.properties wurde im Serverordner nicht gefunden. Die Datei wird erstellt, wenn der Server zum ersten Mal erfolgreich startet.";
        public const string SavePropsShow = "Die maximale Spielerzahl muss eine Zahl zwischen 1 und 9999 sein.";
        public const string SavePropsShow2 = "Die Sichtweite muss eine Zahl zwischen 2 und 32 sein.";
        public const string SavePropsShow3 = "Die Simulationsdistanz muss eine Zahl zwischen 2 und 32 sein.";
        public const string SavePropsShow4 = "Der Radius des Spawn-Schutzes muss eine Zahl zwischen 0 und 9999 sein.";
        public const string SavePropsShow5 = "Speichern fehlgeschlagen: ";
        public const string SavePropsShow6 = "Gespeichert. Der Server läuft, daher werden diese Einstellungen beim nächsten Start wirksam.";
        public const string PlayerFormStr = "Spieler online (";
        public const string PlayerFormStr2 = "Gerade sind keine Spieler online";
        public const string PlayerFormAddBtn = "Zu mir teleportieren";
        public const string PlayerFormAddBtn2 = "Vom Server werfen";
        public const string PlayerFormConfirmDo = "Spieler ";
        public const string PlayerFormConfirmDo2 = " vom Server werfen?";
        public const string PlayerFormAddBtn3 = "OP geben";
        public const string PlayerFormAddBtn4 = "OP entziehen";
        public const string PlayerFormAddBtn5 = "Bannen";
        public const string PlayerFormConfirmDo3 = "Spieler ";
        public const string PlayerFormConfirmDo4 = " bannen? Er kann dem Server dann nicht mehr beitreten (du kannst ihn später entbannen).";
        public const string PlayerFormAddBtn6 = "Entbannen";
        public const string PlayerFormStr3 = "Hinweis: „Zu mir teleportieren“ ist nicht verfügbar, solange du selbst nicht online bist.";
        public const string PlayerFormStr4 = "Aktionen werden als Konsolenbefehle an den Server gesendet, genau wie beim Eintippen in das Befehlsfeld.";
        public const string CloseText = "Schließen";
        public const string LbOpsCaption = "OP-Liste: ";
        public const string RefreshListsStr = "(keine)";
        public const string LbBannedCaption = "Bannliste: ";
        public const string WizardFormInitChrome = "Assistent für die Ersteinrichtung";
        public const string WizardFormAddLabel = "Minecraft-Version";
        public const string WizardFormAddHint = "Jede Release-Version ist möglich";
        public const string WizardFormAddLabel2 = "Servertyp";
        public const string WizardFormAddRadio = "Vanilla";
        public const string WizardFormAddLabel3 = "Serverordner";
        public const string BtnBrowseText = "Durchsuchen...";
        public const string Description = "Wähle den Serverordner (er muss noch nicht existieren – wähle einen übergeordneten Ordner und bearbeite den Pfadtext)";
        public const string ChkEulaText = "Ich stimme der Minecraft EULA zu";
        public const string LnkEulaText = "EULA vollständig lesen";
        public const string WizardFormAddHint2 = "Ohne das Häkchen musst du vor dem ersten Start selbst eula=true in der eula.txt setzen";
        public const string LblStepCaption = "Fülle die drei Punkte oben aus und klicke auf „Installation starten“. Java und der Server werden automatisch heruntergeladen und geprüft.";
        public const string BtnStartText = "Installation starten";
        public const string WizardFormWizLog = "Wird abgebrochen...";
        public const string StartInstallShow = "Gib zuerst eine Minecraft-Version ein.";
        public const string StartInstallShow2 = "Das sieht nicht nach einer Release-Versionsnummer aus. Gib etwas wie 1.20.1 ein (Snapshots werden vom Assistenten nicht unterstützt).";
        public const string StartInstallShow3 = "Gib zuerst einen Serverordner ein.";
        public const string StartInstallShow4 = "Der Zielordner enthält bereits eine server.jar. Beim Fortfahren wird sie überschrieben.\n\nDeine Welt und deine Mods bleiben unberührt (der Assistent ersetzt nur die jar und fügt fehlende Dateien hinzu). Fortfahren?";
        public const string StartInstallShow5 = "Überschreiben und fortfahren";
        public const string StartInstallWizLog = "-- Installation gestartet: Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "Es kann nicht bestimmt werden, welches Java Minecraft ";
        public const string RunFail2 = " benötigt; Assistent abgebrochen (es wurde nichts geändert).";
        public const string RunWizLog = "Hinweis: 26.x ist Mojangs neue jahresbasierte Versionierung; die offizielle Java-Anforderung kann höher sein als die hier verwendete Schätzung mit Java 21 – behalte nach der Installation das Serverprotokoll im Auge.";
        public const string RunWizStep = "Schritt 1: Java vorbereiten ";
        public const string RunWizLog2 = "Das konfigurierte Java hat die Hauptversion ";
        public const string RunWizLog3 = " und erfüllt die Anforderung; es wird verwendet: ";
        public const string RunWizLog4 = "Das zuvor heruntergeladene Java neben der App wird verwendet: ";
        public const string RunFail3 = " benötigt Java ";
        public const string RunFail4 = ", aber dieser Assistent kann nur Java 17 / 21 automatisch herunterladen. Installiere Java ";
        public const string RunFail5 = " manuell, zeige in den Einstellungen auf seine java.exe und starte dann den Server.";
        public const string RunWizLog5 = "Die Adoptium-Asset-API gab keinen direkten Link (";
        public const string RunStr = "kein Detail";
        public const string RunPart = "); es wird auf den offiziellen Binär-Endpunkt ausgewichen, der keine Prüfsumme veröffentlicht – dieser Java-Download wird nicht per Hash geprüft.";
        public const string RunWizLog6 = "Java wird heruntergeladen ";
        public const string RunWizLog7 = " (Eclipse Temurin JRE, etwa 40-50 MB)...";
        public const string RunFail6 = "Java-Download fehlgeschlagen: ";
        public const string RunWizLog8 = "SHA256 der Java-Zip wird geprüft...";
        public const string RunFail7 = "SHA256 der Java-Zip stimmt nicht überein; der Download wurde gelöscht. Erwartet ";
        public const string RunFail8 = ", erhalten ";
        public const string RunFail9 = ". Prüfe dein Netzwerk und versuche es erneut.";
        public const string RunWizLog9 = "SHA256 geprüft.";
        public const string RunWizLog10 = "Java wird entpackt nach ";
        public const string RunWizLog11 = " ...";
        public const string RunFail10 = "Entpacken von Java fehlgeschlagen: ";
        public const string RunFail11 = "Das Entpacken ist fertig, aber es wurde keine bin\\java.exe gefunden – unerwartete Zip-Struktur; abgebrochen (Einstellungen nicht gespeichert).";
        public const string RunFail12 = "Das heruntergeladene Java meldet die Hauptversion ";
        public const string RunFail13 = ", immer noch unter der benötigten ";
        public const string RunFail14 = "; abgebrochen (Einstellungen nicht gespeichert).";
        public const string RunWizLog12 = "Java bereit: Hauptversion ";
        public const string RunWizLog13 = ", ";
        public const string RunWizStep2 = "Schritt 2: Server-jar herunterladen";
        public const string RunFail15 = "Das Versionsmanifest von Mojang kann nicht abgerufen werden: ";
        public const string RunFail16 = "Minecraft ";
        public const string RunFail17 = " ist nicht im Versionsmanifest (prüfe die Nummer; nur Releases).";
        public const string RunFail18 = "Die Versionsmetadaten für ";
        public const string RunFail19 = " können nicht abgerufen werden: ";
        public const string RunFail20 = "Die Versionsmetadaten enthalten keinen Server-Download.";
        public const string RunWizLog14 = "Vanilla-Server wird heruntergeladen (offiziell von Mojang)...";
        public const string RunDownloadWithProgress = "Vanilla-Server";
        public const string RunFail21 = "Server-Download fehlgeschlagen: ";
        public const string RunWizLog15 = "SHA1 des Servers wird geprüft...";
        public const string RunFail22 = "SHA1 des Servers stimmt nicht überein; die jar wurde gelöscht. Erwartet ";
        public const string RunWizLog16 = "SHA1 geprüft.";
        public const string RunFail23 = "Die Fabric-Loader-Liste kann nicht abgerufen werden: ";
        public const string RunFail24 = "Fabric hat noch keinen stabilen Loader für Minecraft ";
        public const string RunFail25 = "; wähle eine andere Version.";
        public const string RunFail26 = "Die Fabric-Installer-Liste kann nicht abgerufen werden: ";
        public const string RunFail27 = "Die Fabric-Installer-Liste sieht falsch aus.";
        public const string RunWizLog17 = "Start-jar des Fabric-Servers wird heruntergeladen (Loader ";
        public const string RunWizLog18 = ", Installer ";
        public const string RunWizLog19 = ")...";
        public const string RunWizLog20 = "Hinweis: Fabric veröffentlicht keinen Hash für diese jar, daher ist sie nur durch HTTPS geschützt und wird nicht per Hash geprüft. Beim ersten Start lädt sie den Vanilla-Server und die Bibliotheken selbst herunter.";
        public const string RunDownloadWithProgress2 = "Fabric-Server";
        public const string RunFail28 = "Fabric-Server-Download fehlgeschlagen: ";
        public const string RunWizLog21 = "Der Ordner mods wurde erstellt. Hinweis: Fabric-Server brauchen normalerweise auch die Fabric-API-Mod in mods, und jeder Spieler braucht dieselben beidseitigen Mods in seinem Client.";
        public const string RunFail29 = "Die Paper-Build-Liste kann nicht abgerufen werden (Fill API): ";
        public const string RunFail30 = ". Hinweis: Versionen ohne Paper-Builds geben hier einen Fehler zurück.";
        public const string RunFail31 = "Kein herunterladbarer Server in der Paper-Build-Liste gefunden.";
        public const string RunWizLog22 = "Paper-Server wird heruntergeladen (neuester Build)...";
        public const string RunDownloadWithProgress3 = "Paper-Server";
        public const string RunFail32 = "Paper-Server-Download fehlgeschlagen: ";
        public const string RunWizLog23 = "SHA256 des Servers wird geprüft...";
        public const string RunFail33 = "SHA256 des Servers stimmt nicht überein; die jar wurde gelöscht. Erwartet ";
        public const string RunWizStep3 = "Schritt 3: Startdateien schreiben";
        public const string RunWizLog24 = "eula.txt geschrieben (eula=true, wie du zugestimmt hast).";
        public const string RunWizLog25 = "EULA nicht zugestimmt: eula.txt wurde nicht geschrieben. Beim ersten Start erzeugt der Server eula=false und beendet sich; stelle es auf eula=true, um zu starten.";
        public const string RunWizLog26 = "server.properties geschrieben: server-port=25565, online-mode=false (für Offline-Mehrspieler erforderlich).";
        public const string RunWizLog27 = "Eine server.properties existiert bereits und wurde nicht angetastet. Stelle selbst sicher, dass online-mode=false gesetzt ist, sonst können Offline-Konten nicht beitreten.";
        public const string RunWizStep4 = "Schritt 4: Einstellungen speichern";
        public const string RunWizLog28 = "Fertig. Serverordner: ";
        public const string RunWizLog29 = "; jar: server.jar; Java: ";
        public const string RunStr2 = "(vorige Einstellung beibehalten)";
        public const string RunWizLog30 = "Schließe den Assistenten, trage den SakuraFrp Zugriffsschlüssel und die Tunnel-IDs in den Einstellungen ein und klicke dann auf „Mehrspieler starten“.";
        public const string RunWizStep5 = "Installation abgeschlossen";
        public const string RunFail34 = "Assistentenfehler: ";
        public const string DownloadWithProgressWizStep = "Wird heruntergeladen ";
        public const string DownloadWithProgressWizStep2 = "... ";
        public const string FailWizLog = "Fehlgeschlagen: ";
        public const string FailWizStep = "Gestoppt";
        public const string CancelledWizLog = "Abgebrochen. Die Einstellungen wurden nicht gespeichert; vollständig heruntergeladene Dateien bleiben im Zielordner, teilweise Dateien wurden aufgeräumt.";
        public const string CancelledWizStep = "Abgebrochen";
        public const string MainFormAppendServerLog = "[Launcher] Gestartet. Protokolle werden auch im Ordner logs neben dem Programm gespeichert (14 Tage aufbewahrt).";
        public const string TrayText = "Minecraft Mehrspieler-Helfer · Gestoppt";
        public const string InitTray613ToolStripMenuItem = "Hauptfenster anzeigen";
        public const string InitTray613ToolStripMenuItem2 = "Sicher beenden";
        public const string BtnStartText2 = "Mehrspieler starten";
        public const string AddrLblCaption = "Öffentliche Adresse (an Freunde senden): ";
        public const string Hint = "Die öffentliche Adresse erscheint hier, sobald der Tunnel startet";
        public const string BtnCopyText = "Adresse für Freunde kopieren";
        public const string BtnPcl2Text = "PCL2 öffnen";
        public const string BtnStopText = "Server sicher stoppen";
        public const string LogToggleLblCaption = "▾  Protokoll";
        public const string BuildUiStr = "Server";
        public const string BuildUiStr2 = "Tunnel";
        public const string BuildUiStr3 = "Status";
        public const string BuildUiStr4 = "Sicherungen";
        public const string BuildUiMakeLogBox = "Noch keine Protokolle · Die Ausgabe des Servers erscheint hier";
        public const string BuildUiMakeLogBox2 = "Noch keine Protokolle · Die Ausgabe des Tunnels erscheint hier";
        public const string BtnJumpBottomText = "↓ Zurück nach unten";
        public const string Hint2 = "Protokolle durchsuchen…";
        public const string BuildUiSetToolTip = "Zurück";
        public const string BuildUiSetToolTip2 = "Weiter";
        public const string BuildUiSetToolTip3 = "Durchsucht das Protokoll; Eingabetaste springt zum nächsten Treffer";
        public const string ChkErrOnlyText = "Nur Warnungen/Fehler";
        public const string CmdLblCaption = "Serverbefehl";
        public const string BtnCmdSendText = "Senden";
        public const string BuildUiAppendState = "Erste Verwendung: Trage den SakuraFrp Zugriffsschlüssel und die Tunnel-ID in den Einstellungen ein und prüfe den PCL2-Pfad.";
        public const string IsErrLineContains = "[Watchdog]";
        public const string IsErrLineContains2 = "[Sicherung] Fehlgeschlagen";
        public const string LblFindCaption = "Keine Treffer";
        public const string SrvState2 = "Läuft · ";
        public const string SampleStatsPart = " Spieler";
        public const string SampleStatsPart2 = " Spieler online (Liste nicht öffentlich)";
        public const string SampleStatsPart3 = "Ping ";
        public const string ToggleLogStr = "▸  Protokoll";
        public const string AppendServerLogPersistLog613 = "Server";
        public const string AppendTunnelLogPersistLog613 = "Tunnel";
        public const string SendServerCommandAppendState = "Der Server läuft nicht; der Befehl kann nicht gesendet werden.";
        public const string SendServerCommandAppendState2 = "Befehl konnte nicht gesendet werden: Der Serverprozess ist nicht mehr da.";
        public const string SendServerCommandAppendState3 = "Befehl konnte nicht gesendet werden: ";
        public const string OpenPlayerManagerShow = "Der Server läuft nicht; die Spielerverwaltung ist verfügbar, nachdem der Server gestartet ist.";
        public const string OpenPlayerManagerAppendState = "Befehl fehlgeschlagen: Der Serverprozess ist nicht mehr da.";
        public const string DoBackupAppendState = "Eine Sicherung läuft bereits.";
        public const string BackupCoreAppendState = "Weltordner nicht gefunden: ";
        public const string BackupCoreAppendState2 = ", Sicherung nicht möglich.";
        public const string BackupCoreAppendServerLog = "[Sicherung] Automatisches Speichern des Servers pausiert und geschrieben; die Welt wird gepackt…";
        public const string BackupCoreAppendServerLog2 = "[Sicherung] Fertig: ";
        public const string BackupCorePart = " Dateien";
        public const string BackupCoreStr = ", übersprungen ";
        public const string BackupCorePart2 = " Datei(en) in Benutzung";
        public const string BackupCoreAppendState3 = "Weltsicherung abgeschlossen (";
        public const string BackupCoreAppendServerLog3 = "[Sicherung] Fehlgeschlagen: ";
        public const string BackupCoreAppendState4 = "Sicherung fehlgeschlagen: ";
        public const string BackupCoreAppendServerLog4 = "[Sicherung] Automatisches Speichern des Servers fortgesetzt.";
        public const string RestoreBackupShow = "Du musst den Server vor dem Wiederherstellen stoppen: Weltdateien bei laufendem Server zu tauschen, beschädigt den Spielstand.\n\nKlicke zuerst auf „Server sicher stoppen“ und stelle dann wieder her.";
        public const string RestoreBackupShow2 = "Sicherung wiederherstellen";
        public const string RestoreBackupConfirmBox = "Die Welt auf die Sicherung „";
        public const string RestoreBackupConfirmBox2 = "“ zurücksetzen?\n\nDie aktuelle Welt wird nicht gelöscht: Sie wird zuerst umbenannt und beiseitegelegt (world_prerestore_<Zeit>); lösche sie selbst, sobald alles geprüft ist. Wenn die Wiederherstellung falsch war, kannst du sie zurücktauschen.";
        public const string RestoreBackupAppendState = "Eine Sicherungsaufgabe läuft bereits.";
        public const string Result = "Auf die Sicherung „";
        public const string RestoreBackupPart = "“ zurückgesetzt. Die alte Welt wurde behalten unter ";
        public const string RestoreBackupPart2 = "; sobald die neue Welt geprüft ist, kannst du sie löschen.";
        public const string Result2 = "Wiederherstellung fehlgeschlagen: ";
        public const string RestoreBackupPart3 = "(Die alte Welt wurde möglicherweise bereits umbenannt. Starte den Server noch nicht; prüfe den Serverordner.)";
        public const string RestoreBackupAppendServerLog = "[Sicherung] ";
        public const string BuildBackupPanelBkLabel = "Weltsicherungen";
        public const string BtnBackupNowText = "Jetzt sichern";
        public const string BuildBackupPanelDoBackup = "Manuelle Sicherung";
        public const string BtnRestoreBackupText = "Ausgewählte wiederherstellen";
        public const string BuildBackupPanelShow = "Wähle zuerst eine Sicherung aus der Liste oben aus.";
        public const string BtnDeleteBackupText = "Ausgewählte löschen";
        public const string BuildBackupPanelShow2 = "Sicherung löschen";
        public const string BuildBackupPanelConfirmBox = "Sicherung „";
        public const string BuildBackupPanelPart = "“ löschen? Nach dem Löschen kann sie nicht wiederhergestellt werden.";
        public const string BuildBackupPanelShow3 = "Löschen fehlgeschlagen: ";
        public const string BuildBackupPanelBkLabel2 = "Die neuesten ";
        public const string BuildBackupPanelBkLabel3 = " Sicherungen behalten";
        public const string BuildBackupPanelBkCheck = "Bei Absturz automatisch neu starten";
        public const string BuildBackupPanelBkCheck2 = "Vor dem Serverstart automatisch sichern";
        public const string BuildBackupPanelBkCheck3 = "Täglich automatisch sichern";
        public const string BuildBackupPanelBkLabel4 = "Solange die App geöffnet ist, zur eingestellten Zeit einmal automatisch sichern";
        public const string BuildBackupPanelBkCheck4 = "Zeitgesteuerter Befehl";
        public const string LblBackupStateCaption = "Sicherung läuft… (große Welten können ein oder zwei Minuten dauern)";
        public const string RefreshBackupListStr = "Insgesamt ";
        public const string RefreshBackupListPart = " Sicherungen · ";
        public const string RefreshBackupListStr2 = "Noch keine Sicherungen";
        public const string RefreshBackupListStr3 = "Neueste: ";
        public const string RefreshBackupListStr4 = "Klicke auf „Jetzt sichern“, um einen Schnappschuss der Welt zu speichern.";
        public const string CheckSchedulesAppendServerLog = "[Zeitplan] Zeit für die tägliche Sicherung erreicht; automatische Sicherung wird gestartet.";
        public const string CheckSchedulesAppendServerLog2 = "[Zeitplan] Ein zeitgesteuerter Befehl ist fällig, aber ";
        public const string CheckSchedulesAppendServerLog3 = " Spieler sind noch online; wird verschoben, bis der Server leer ist.";
        public const string CheckSchedulesAppendServerLog4 = "[Zeitplan] Zeitgesteuerter Befehl gesendet: ";
        public const string ServerText2 = "Server: Läuft";
        public const string ServerText3 = "Server: Startet…";
        public const string TunnelText2 = "SakuraFrp Tunnel: Verbunden";
        public const string TunnelText3 = "SakuraFrp Tunnel: Startet…";
        public const string AddrText2 = "Öffentliche Adresse: Bereit";
        public const string AddrText3 = "Öffentliche Adresse: Gespeichert (Tunnel läuft nicht)";
        public const string Tt = "Minecraft Mehrspieler-Helfer · ";
        public const string RefreshStatusStr = "Läuft";
        public const string RefreshStatusStr2 = "Startet";
        public const string CopyAddrShow = "Noch keine öffentliche Adresse. Starte zuerst den Mehrspielermodus.";
        public const string CopyAddrShow2 = "Hinweis";
        public const string CopyAddrAppendState = "Öffentliche Adresse in die Zwischenablage kopiert; sende sie an deine Freunde.";
        public const string BtnCopyText2 = "Kopiert ✓";
        public const string OpenPcl2Show = "PCL2 nicht gefunden. Lege den exe-Pfad von PCL2 in den Einstellungen fest.";
        public const string OpenPcl2Show2 = "PCL2 konnte nicht geöffnet werden: ";
        public const string HandleServerCrashAppendServerLog = "[Watchdog] Der Server ist unerwartet beendet worden (Exit-Code ";
        public const string HandleServerCrashAppendServerLog2 = "); das war kein normales Herunterfahren.";
        public const string HandleServerCrashStr = "Mögliche Ursache: ";
        public const string HandleServerCrashAppendState = "Der Server ist unerwartet beendet worden";
        public const string HandleServerCrashStr2 = ", mögliche Ursache: ";
        public const string HandleServerCrashStr3 = ". Sieh dir das Ende des Serverprotokolls für den Grund an.";
        public const string HandleServerCrashPart = "(Automatischer Neustart des Watchdog ist aus)";
        public const string HandleServerCrashAppendServerLog3 = "[Watchdog] ";
        public const string HandleServerCrashAppendServerLog4 = " Abstürze innerhalb von 10 Minuten; automatischer Neustart gestoppt. Höchstwahrscheinlich ein Problem mit dem Spielstand oder einer Mod.";
        public const string HandleServerCrashAppendState2 = "Der Server ist immer wieder abgestürzt; der automatische Neustart wurde gestoppt. Prüfe das Protokoll.";
        public const string HandleServerCrashStr4 = " Mögliche Ursache: ";
        public const string HandleServerCrashAppendState3 = "Der Server ist unerwartet beendet worden; der Watchdog startet ihn in 5 Sekunden neu…";
        public const string GuessCrash614Ret = "Port ";
        public const string GuessCrash614Part = " wird bereits verwendet – wahrscheinlich wurde eine andere Serverinstanz nicht sauber geschlossen";
        public const string GuessCrash614Ret2 = "Nicht genügend Speicher oder eine falsche Speichereinstellung (-Xmx)";
        public const string GuessCrash614Ret3 = "Falsche Java-Version – dieser Server braucht Java 17";
        public const string GuessCrash614Ret4 = "Der Server ist über 60 Sekunden eingefroren und wurde vom eingebauten Watchdog des Spiels gestoppt";
        public const string GuessCrash614Ret5 = "Eine Mod konnte nicht geladen werden; suche die in den Protokollfehlern genannte Mod";
        public const string GuessCrash614Ret6 = "Die EULA wurde nicht akzeptiert";
        public const string OfferWizardIfNeededShow = "Es gibt noch keinen verwendbaren Server: Der konfigurierte Serverordner existiert nicht oder enthält keine Server-jar.\n\nDer Assistent für die Ersteinrichtung kann ein passendes Java und einen Server (Vanilla / Fabric / Paper) herunterladen und die EULA sowie die für Offline-Spiel benötigte server.properties schreiben. Alle Downloads werden per Hash geprüft.";
        public const string OfferWizardIfNeededShow2 = "Assistent öffnen";
        public const string OfferWizardIfNeededShow3 = "Später";
        public const string OpenWizardAppendState = "Assistent für die Ersteinrichtung abgeschlossen: Server und Java sind bereit.";
        public const string CheckJavaVersionStr = "unbekannt";
        public const string CheckJavaVersionAppendServerLog = "[Java-Prüfung] Die Versionsinformationen der Server-jar (version.json) können nicht gelesen werden; die Versionsprüfung wird übersprungen. Aktuelle Java-Hauptversion: ";
        public const string CheckJavaVersionAppendServerLog2 = ".";
        public const string CheckJavaVersionAppendServerLog3 = "[Java-Prüfung] Serverversion ";
        public const string CheckJavaVersionAppendServerLog4 = " ist nicht in der Versionstabelle; die Versionsprüfung wird übersprungen. Aktuelle Java-Hauptversion: ";
        public const string CheckJavaVersionAppendServerLog5 = "[Java-Prüfung] Server ";
        public const string CheckJavaVersionAppendServerLog6 = ", aber die aktuelle Java-Version konnte nicht gemessen werden; es wird trotzdem gestartet (behalte das Serverprotokoll im Auge).";
        public const string CheckJavaVersionAppendServerLog7 = "[Java-Prüfung] Bestanden: Server ";
        public const string CheckJavaVersionAppendServerLog8 = ", das aktuelle Java ist ";
        public const string CheckJavaVersionAppendServerLog9 = "[Java-Prüfung] Nicht übereinstimmend: Server ";
        public const string CheckJavaVersionAppendServerLog10 = ", das aktuelle Java ist nur ";
        public const string CheckJavaVersionAppendServerLog11 = "; es wird auf die Entscheidung des Benutzers gewartet.";
        public const string Msg2 = "Die Java-Version ist zu alt – der Server wird höchstwahrscheinlich nicht starten.\n\nServerversion: ";
        public const string CheckJavaVersionPart = ", die Java ";
        public const string CheckJavaVersionPart2 = " oder neuer benötigt. Das zu verwendende Java hat die Hauptversion ";
        public const string CheckJavaVersionPart3 = ".\n\nVorschlag: Wähle in den Einstellungen die java.exe eines Java ";
        public const string CheckJavaVersionPart4 = " aus, oder lasse den Assistenten für die Ersteinrichtung automatisch ein passendes Java herunterladen.\n„Trotzdem starten“ startet in jedem Fall mit dem aktuellen Java; die entstehenden Fehler stehen im Serverprotokoll.";
        public const string CheckJavaVersionShow = "Java-Version stimmt nicht überein";
        public const string CheckJavaVersionShow2 = "Trotzdem starten";
        public const string CheckJavaVersionShow3 = "Start abbrechen";
        public const string CheckJavaVersionAppendServerLog12 = "[Java-Prüfung] Der Benutzer hat sich entschieden, trotzdem zu starten.";
        public const string CheckJavaVersionAppendServerLog13 = "[Java-Prüfung] Der Benutzer hat den Start abgebrochen.";
        public const string StartAllAppendState = "Der Server läuft bereits; er muss nicht erneut gestartet werden.";
        public const string BtnStartText3 = "Mehrspieler läuft";
        public const string BtnStartText4 = "Startet…";
        public const string DoStartFailState = "Der Serverordner existiert nicht: ";
        public const string DoStartFailState2 = ". Prüfe den Serverordner in den Einstellungen.";
        public const string DoStartFailState3 = "Server-jar nicht gefunden: ";
        public const string DoStartFailState4 = ". Prüfe den Namen der jar-Datei in den Einstellungen.";
        public const string DoStartFailState5 = "Java (java.exe) nicht gefunden. Wähle den Java-Pfad in den Einstellungen erneut aus – er liegt normalerweise unter .minecraft\\runtime oder im Java-Installationsordner.";
        public const string DoStartFailState6 = "Start abgebrochen: Die Java-Version liegt unter dem, was der Server braucht. Wechsle zu einem passenden Java oder lasse den Assistenten für die Ersteinrichtung eines herunterladen und versuche es dann erneut.";
        public const string DoStartFailState7 = " wird bereits verwendet. Der letzte Server wurde möglicherweise nicht sauber geschlossen — klicke auf „Server sicher stoppen“ oder starte diese App neu; beende Java-Prozesse im Task-Manager nicht selbst.";
        public const string DoStartAppendState = "Automatische Sicherung vor dem Serverstart…";
        public const string DoStartAppendState2 = "Der Minecraft-Server wird gestartet…";
        public const string DoStartAppendServerLog = "[Prozess beendet]";
        public const string DoStartFailState8 = "Der Java-Prozess kann nicht gestartet werden. Prüfe, ob der Java-Pfad korrekt ist.";
        public const string DoStartAppendState3 = "Es wird gewartet, bis der Server bereit ist (es wird auf „Done“ geachtet)…";
        public const string DoStartFailState9 = "Der Serverprozess ist beendet worden, bevor „Done“ erschien. Am wahrscheinlichsten: 1) falscher Java-Pfad oder falsche Speicherargumente; 2) ein Fehler im Spielstand/in der Welt oder in Mods (sieh dir das Ende des Serverprotokolls an). Fasse Welt und Mods nicht an.";
        public const string DoStartFailState10 = "Kein „Done“ nach 90 Sekunden. Möglicherweise nicht genug Speicher, eine übergroße Welt oder ein hängender Start. Sieh dir das Ende des Serverprotokolls an; fasse Welt und Mods nicht an.";
        public const string DoStartAppendState4 = "Bereit. Klicke auf „Adresse für Freunde kopieren“ und sende die Adresse an deine Freunde.";
        public const string DoStartFailState11 = "Start fehlgeschlagen: ";
        public const string StartTunnelAppendTunnelLog = "[Fallback] Der offizielle SakuraFrp Launcher wird geöffnet — klicke dort auf „Tunnel starten“.";
        public const string StartTunnelAppendTunnelLog2 = "Der SakuraFrp Launcher konnte nicht geöffnet werden: ";
        public const string StartTunnelAppendTunnelLog3 = "frpc.exe nicht gefunden: ";
        public const string StartTunnelAppendTunnelLog4 = ". Prüfe es in den Einstellungen oder wechsle den Tunnelmodus zum offiziellen Launcher.";
        public const string StartTunnelAppendTunnelLog5 = "Der Zugriffsschlüssel ist leer. Trage den SakuraFrp Zugriffsschlüssel in den Einstellungen ein.";
        public const string StartTunnelAppendTunnelLog6 = "Die Tunnel-ID ist leer. Trage sie in den Einstellungen ein.";
        public const string StartTunnelAppendState = "Der SakuraFrp Tunnel wird gestartet…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc beendet]";
        public const string StartTunnelAppendTunnelLog8 = "[Watchdog] Der Spieltunnel ist unerwartet beendet worden; erneute Verbindung in 5 Sekunden.";
        public const string StartTunnelAppendTunnelLog9 = "frpc konnte nicht gestartet werden. Prüfe den Pfad zu frpc.exe.";
        public const string StartTunnelAppendTunnelLog10 = "Die aktuelle frpc.exe ist der Build, der mit dem SakuraFrp Launcher ausgeliefert wird. Dieser Build kann von externen Programmen nicht direkt ausgeführt werden (nur über den SakuraFrp Service) und beendet sich daher sofort. Das hat nichts mit deinem Zugriffsschlüssel, deiner Tunnel-ID oder dem Knoten zu tun. Zeige in den Einstellungen mit dem frpc-Pfad auf eine eigenständige frpc oder wechsle den Tunnelmodus zum offiziellen Launcher.";
        public const string StartTunnelAppendTunnelLog11 = "Der frpc-Prozess ist beendet worden. Möglicherweise: 1) falscher Zugriffsschlüssel oder falsche Tunnel-ID; 2) ein Problem mit dem Knoten; 3) das Konto braucht eine Verifizierung oder ein Check-in. Prüfe Schlüssel und Tunnel-ID in den Einstellungen oder wechsle den Tunnelmodus zum offiziellen Launcher.";
        public const string StartTunnelAppendState2 = "Diese frpc ist der mitgelieferte Build des Launchers und kann nicht direkt laufen. Wechsle in den Einstellungen zu einer eigenständigen frpc oder zum offiziellen Launcher-Modus.";
        public const string StartVoiceTunnelAppendTunnelLog = "[Sprache] Keine Sprachtunnel-ID gesetzt; der Sprachtunnel wird übersprungen.";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[Sprache frpc beendet]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[Watchdog] Der Sprachtunnel ist unerwartet beendet worden; erneute Verbindung in 5 Sekunden.";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[Sprache] frpc konnte nicht gestartet werden. Prüfe den Pfad zu frpc.exe.";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[Sprache] Der frpc-Prozess ist beendet worden. Prüfe, ob die Sprachtunnel-ID korrekt ist.";
        public const string StartVoiceTunnelAppendState = "Der Sprachtunnel ist verbunden.";
        public const string VoiceFrpcOutputAppendTunnelLog = "[Sprache] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[Sprache][ERR] ";
        public const string TryParseFrpcAppendState = "Öffentliche Adresse automatisch ermittelt: ";
        public const string ServerText4 = "Server: Fehler";
        public const string TunnelText4 = "SakuraFrp Tunnel: Fehler";
        public const string SafeStopAppendState = "Der Server läuft nicht; der Tunnel wurde gestoppt.";
        public const string SafeStopAppendState2 = "Sicheres Stoppen: stop wird an den Server gesendet und auf das Speichern gewartet…";
        public const string SafeStopAppendServerLog = ">> stop gesendet; es wird auf das Speichern und Beenden gewartet (bis zu 60 Sekunden)";
        public const string SafeStopAppendState3 = "Der Server ist normal gestoppt (Speichern abgeschlossen). Der Tunnel wird geschlossen…";
        public const string SafeStopAppendServerLog2 = "[Serverprozess normal beendet]";
        public const string SafeStopShow = "Der Server ist nach 60 Sekunden immer noch nicht beendet. Erzwingen des Beendens kann die letzten Minuten Fortschritt kosten. Beenden erzwingen?";
        public const string SafeStopShow2 = "Zeitüberschreitung beim sicheren Herunterfahren";
        public const string SafeStopShow3 = "Beenden erzwingen";
        public const string SafeStopShow4 = "Länger warten";
        public const string SafeStopAppendState4 = "Der Serverprozess wurde zwangsweise beendet (der letzte Fortschritt kann verloren sein).";
        public const string SafeStopAppendState5 = "Erzwungenes Beenden abgebrochen; der Server läuft noch.";
        public const string SafeStopAppendState6 = "Alles gestoppt: Server und Tunnel sind geschlossen.";
        public const string OnClosingConfirmBox = "Der Server läuft noch. Direktes Schließen kann das Speichern überspringen – zuerst sicher stoppen?";
        public const string BtnSparkTpsText = "TPS-Bericht";
        public const string BtnSparkProfileText = "30 s diagnostizieren";
        public const string SparkLblCaption = "Diagnose";
        public const string SparkProfileStarted = "[Diagnose] 30-sekündige Diagnose gestartet; lasse den Server laufen...";
        public const string SparkProfileStopped = "[Diagnose] Diagnose beendet; der Link zum spark-Bericht erscheint im Serverprotokoll oben.";
        public const string SparkProfileCancelled = "[Diagnose] Der Server ist gestoppt; diese Diagnosesitzung wurde abgebrochen.";
        public const string LangCaption = "Sprache";
        public const string LangRestartHint = "Sprache gespeichert. Starte die App neu, um sie anzuwenden.";
        public const string LangRestartNow = "Jetzt neu starten";
        public const int EulaAgreeWidth = 230;
        public const int EulaLinkDx = 232;
    }

    static class TxtFr
    {
        public const string Msg = "Un problème est survenu";
        public const string ShowFriendlyErrorStr = ", et le programme doit se fermer.";
        public const string ShowFriendlyErrorStr2 = ", et le programme continuera de fonctionner.";
        public const string ShowFriendlyErrorPart = "\n\nSi cela continue, envoie simplement ces informations à Naiwa.";
        public const string ShowFriendlyErrorShow = "Assistant multijoueur Minecraft";
        public const string YesText = "Arrêter le serveur en sécurité avant de quitter";
        public const string NoText = "Annuler";
        public const string SettingsFormInitChrome = "Paramètres";
        public const string SettingsFormAddField = "Dossier du serveur D:\\MCServer";
        public const string SettingsFormAddField2 = "Chemin complet de Java (java.exe)";
        public const string SettingsFormAddField3 = "Nom du fichier jar";
        public const string SettingsFormAddField4 = "Arguments mémoire";
        public const string SettingsFormAddField5 = "Port";
        public const string SettingsFormAddField6 = "Chemin de frpc.exe";
        public const string LbModeCaption = "Mode de tunnel";
        public const string SettingsFormAdd = "Ligne de commande frpc (préféré)";
        public const string SettingsFormAdd2 = "Ouvrir le lanceur officiel SakuraFrp (secours)";
        public const string LbKeyCaption = "Clé d'accès SakuraFrp";
        public const string SettingsFormAddField7 = "ID du tunnel";
        public const string SettingsFormAddField8 = "ID du tunnel vocal (facultatif)";
        public const string SettingsFormAddField9 = "Adresse publique (facultatif)";
        public const string SettingsFormAddField10 = "Chemin de l'exe PCL2";
        public const string BtnPropsText = "Propriétés du serveur…";
        public const string SettingsFormShow = "Définis d'abord correctement le dossier du serveur ci-dessus, puis modifie les propriétés du serveur.";
        public const string SettingsFormShow2 = "Propriétés du serveur";
        public const string BtnWizardText = "Assistant de première configuration";
        public const string BtnSaveText = "Enregistrer";
        public const string ValidateSettingsMarkInvalid = "Le dossier du serveur n'existe pas. Vérifie le chemin.";
        public const string ValidateSettingsMarkInvalid2 = "Le chemin de Java n'existe pas. Vérifie-le.";
        public const string ValidateSettingsMarkInvalid3 = "Le nom du fichier jar ne peut pas être vide.";
        public const string ValidateSettingsMarkInvalid4 = "Ce fichier jar est introuvable dans le dossier du serveur.";
        public const string ValidateSettingsMarkInvalid5 = "Le port doit être un nombre entre 1 et 65535.";
        public const string ValidateSettingsMarkInvalid6 = "Le chemin de frpc.exe n'existe pas. Vérifie-le.";
        public const string ValidateSettingsMarkInvalid7 = "En mode frpc, l'ID du tunnel ne peut pas être vide.";
        public const string ValidateSettingsMarkInvalid8 = "Le chemin de PCL2 n'existe pas. Vérifie-le.";
        public const string TitleText = "Assistant multijoueur Minecraft v6.22.5";
        public const string SubText = "Double tunnel · Jeu + Voix";
        public const string ServerText = "Serveur : arrêté";
        public const string TunnelText = "Tunnel SakuraFrp : arrêté";
        public const string AddrText = "Adresse publique : non générée";
        public const string SrvState = "Arrêté";
        public const string MbText = "Joueurs";
        public const string OnPaintHeader = "Processus du serveur";
        public const string OnPaintRow = "État";
        public const string OnPaintRow2 = "Utilisation mémoire";
        public const string OnPaintRow3 = "Connexions des joueurs";
        public const string OnPaintDrawText = "Joueurs en ligne";
        public const string OnPaintRow4 = "Version · Ping";
        public const string OnPaintHeader2 = "Processus du tunnel (frpc)";
        public const string OnPaintHeader3 = "Local";
        public const string OnPaintDrawText2 = "Descendant";
        public const string OnPaintDrawText3 = "Montant";
        public const string OnPaintRow5 = "Mémoire système";
        public const string OnHandleCreatedIndexOf = "Erreur";
        public const string OnHandleCreatedStr = "Erreur";
        public const string ShowMsg = "OK";
        public const string PropsFormAddCombo = "Difficulté";
        public const string PropsFormStr = "Paisible";
        public const string PropsFormStr2 = "Facile";
        public const string PropsFormStr3 = "Normal";
        public const string PropsFormStr4 = "Difficile";
        public const string PropsFormAddCombo2 = "Mode de jeu";
        public const string PropsFormStr5 = "Survie";
        public const string PropsFormStr6 = "Créatif";
        public const string PropsFormStr7 = "Aventure";
        public const string PropsFormStr8 = "Spectateur";
        public const string PropsFormAddNum = "Joueurs maximum";
        public const string PropsFormAddNum2 = "Distance d'affichage (chunks)";
        public const string PropsFormAddNum3 = "Distance de simulation (chunks)";
        public const string PropsFormAddNum4 = "Rayon de protection du spawn";
        public const string PropsFormAddText = "MOTD du serveur";
        public const string PropsFormAddCheck = "Activer la liste blanche (white-list)";
        public const string PropsFormAddCheck2 = "Autoriser le PvP (pvp)";
        public const string PropsFormAddCheck3 = "Autoriser le vol (allow-flight)";
        public const string LbOnCaption = "Mode en ligne : désactivé (doit rester désactivé pour le multijoueur hors ligne ; ne peut pas être modifié ici)";
        public const string PropsFormShow = "server.properties est introuvable dans le dossier du serveur. Il est créé la première fois que le serveur démarre correctement.";
        public const string SavePropsShow = "Le nombre maximum de joueurs doit être un nombre entre 1 et 9999.";
        public const string SavePropsShow2 = "La distance d'affichage doit être un nombre entre 2 et 32.";
        public const string SavePropsShow3 = "La distance de simulation doit être un nombre entre 2 et 32.";
        public const string SavePropsShow4 = "Le rayon de protection du spawn doit être un nombre entre 0 et 9999.";
        public const string SavePropsShow5 = "Échec de l'enregistrement : ";
        public const string SavePropsShow6 = "Enregistré. Le serveur est en cours d'exécution, donc ces paramètres prendront effet au prochain démarrage.";
        public const string PlayerFormStr = "Joueurs en ligne (";
        public const string PlayerFormStr2 = "Aucun joueur en ligne pour le moment";
        public const string PlayerFormAddBtn = "Téléporter jusqu'à moi";
        public const string PlayerFormAddBtn2 = "Expulser du serveur";
        public const string PlayerFormConfirmDo = "Expulser ";
        public const string PlayerFormConfirmDo2 = " du serveur ?";
        public const string PlayerFormAddBtn3 = "Donner OP";
        public const string PlayerFormAddBtn4 = "Retirer OP";
        public const string PlayerFormAddBtn5 = "Bannir";
        public const string PlayerFormConfirmDo3 = "Bannir ";
        public const string PlayerFormConfirmDo4 = " ? Il ne pourra plus rejoindre le serveur (tu pourras le débannir plus tard).";
        public const string PlayerFormAddBtn6 = "Débannir";
        public const string PlayerFormStr3 = "Remarque : « Téléporter jusqu'à moi » est indisponible tant que tu n'es pas en ligne toi-même.";
        public const string PlayerFormStr4 = "Les actions sont envoyées au serveur comme des commandes de console, exactement comme si tu les saisissais dans la zone de commande.";
        public const string CloseText = "Fermer";
        public const string LbOpsCaption = "Liste des OP : ";
        public const string RefreshListsStr = "(aucun)";
        public const string LbBannedCaption = "Liste des bannis : ";
        public const string WizardFormInitChrome = "Assistant de première configuration";
        public const string WizardFormAddLabel = "Version de Minecraft";
        public const string WizardFormAddHint = "Toute version officielle convient";
        public const string WizardFormAddLabel2 = "Type de serveur";
        public const string WizardFormAddRadio = "Vanilla";
        public const string WizardFormAddLabel3 = "Dossier du serveur";
        public const string BtnBrowseText = "Parcourir...";
        public const string Description = "Choisis le dossier du serveur (il n'a pas besoin d'exister encore — choisis un dossier parent et modifie le texte du chemin)";
        public const string ChkEulaText = "J'accepte l'EULA de Minecraft";
        public const string LnkEulaText = "Lire l'EULA complet";
        public const string WizardFormAddHint2 = "Sans la coche, tu devras mettre toi-même eula=true dans eula.txt avant le premier démarrage";
        public const string LblStepCaption = "Remplis les trois éléments ci-dessus et clique sur Démarrer l'installation. Java et le serveur seront téléchargés et vérifiés automatiquement.";
        public const string BtnStartText = "Démarrer l'installation";
        public const string WizardFormWizLog = "Annulation...";
        public const string StartInstallShow = "Saisis d'abord une version de Minecraft.";
        public const string StartInstallShow2 = "Cela ne ressemble pas à un numéro de version officielle. Saisis quelque chose comme 1.20.1 (les snapshots ne sont pas pris en charge par l'assistant).";
        public const string StartInstallShow3 = "Saisis d'abord un dossier de serveur.";
        public const string StartInstallShow4 = "Le dossier de destination contient déjà un server.jar. Continuer l'écrasera.\n\nTon monde et tes mods ne sont pas touchés (l'assistant ne fait que remplacer le jar et ajouter les fichiers manquants). Continuer ?";
        public const string StartInstallShow5 = "Écraser et continuer";
        public const string StartInstallWizLog = "-- Installation commencée : Minecraft ";
        public const string StartInstallPart = " → ";
        public const string RunFail = "Impossible de déterminer quel Java Minecraft ";
        public const string RunFail2 = " requiert ; assistant interrompu (rien n'a été modifié).";
        public const string RunWizLog = "Remarque : 26.x est la nouvelle numérotation annuelle de Mojang ; son exigence Java officielle peut être supérieure à l'estimation Java 21 utilisée ici — surveille le journal du serveur après l'installation.";
        public const string RunWizStep = "Étape 1 : préparation de Java ";
        public const string RunWizLog2 = "Le Java configuré est en version majeure ";
        public const string RunWizLog3 = " et satisfait l'exigence ; il sera utilisé : ";
        public const string RunWizLog4 = "Utilisation du Java précédemment téléchargé à côté de l'application : ";
        public const string RunFail3 = " requiert Java ";
        public const string RunFail4 = ", mais cet assistant ne peut télécharger automatiquement que Java 17 / 21. Installe Java ";
        public const string RunFail5 = " manuellement, fais pointer les Paramètres vers son java.exe, puis démarre le serveur.";
        public const string RunWizLog5 = "L'API d'actifs Adoptium n'a donné aucun lien direct (";
        public const string RunStr = "aucun détail";
        public const string RunPart = ") ; bascule vers le point de terminaison binaire officiel, qui ne publie pas de somme de contrôle — ce téléchargement de Java ne sera pas vérifié par hachage.";
        public const string RunWizLog6 = "Téléchargement de Java ";
        public const string RunWizLog7 = " (Eclipse Temurin JRE, environ 40-50 Mo)...";
        public const string RunFail6 = "Échec du téléchargement de Java : ";
        public const string RunWizLog8 = "Vérification du SHA256 du zip Java...";
        public const string RunFail7 = "Le SHA256 du zip Java ne correspond pas ; le téléchargement a été supprimé. Attendu ";
        public const string RunFail8 = ", obtenu ";
        public const string RunFail9 = ". Vérifie ton réseau et réessaie.";
        public const string RunWizLog9 = "SHA256 vérifié.";
        public const string RunWizLog10 = "Extraction de Java vers ";
        public const string RunWizLog11 = " ...";
        public const string RunFail10 = "Échec de l'extraction de Java : ";
        public const string RunFail11 = "Extraction terminée mais aucun bin\\java.exe trouvé — structure du zip inattendue ; abandon (paramètres non enregistrés).";
        public const string RunFail12 = "Le Java téléchargé signale la version majeure ";
        public const string RunFail13 = ", toujours inférieure à la version requise ";
        public const string RunFail14 = " ; abandon (paramètres non enregistrés).";
        public const string RunWizLog12 = "Java prêt : version majeure ";
        public const string RunWizLog13 = ", ";
        public const string RunWizStep2 = "Étape 2 : téléchargement du jar du serveur";
        public const string RunFail15 = "Impossible d'obtenir le manifeste des versions Mojang : ";
        public const string RunFail16 = "Minecraft ";
        public const string RunFail17 = " n'est pas dans le manifeste des versions (vérifie le numéro ; versions officielles uniquement).";
        public const string RunFail18 = "Impossible d'obtenir les métadonnées de la version ";
        public const string RunFail19 = " : ";
        public const string RunFail20 = "Les métadonnées de la version ne contiennent pas de téléchargement de serveur.";
        public const string RunWizLog14 = "Téléchargement du serveur vanilla (Mojang officiel)...";
        public const string RunDownloadWithProgress = "serveur vanilla";
        public const string RunFail21 = "Échec du téléchargement du serveur : ";
        public const string RunWizLog15 = "Vérification du SHA1 du serveur...";
        public const string RunFail22 = "Le SHA1 du serveur ne correspond pas ; le jar a été supprimé. Attendu ";
        public const string RunWizLog16 = "SHA1 vérifié.";
        public const string RunFail23 = "Impossible d'obtenir la liste des loaders Fabric : ";
        public const string RunFail24 = "Fabric n'a pas encore de loader stable pour Minecraft ";
        public const string RunFail25 = " ; choisis une autre version.";
        public const string RunFail26 = "Impossible d'obtenir la liste des installateurs Fabric : ";
        public const string RunFail27 = "La liste des installateurs Fabric semble incorrecte.";
        public const string RunWizLog17 = "Téléchargement du jar de lancement du serveur Fabric (loader ";
        public const string RunWizLog18 = ", installateur ";
        public const string RunWizLog19 = ")...";
        public const string RunWizLog20 = "Remarque : Fabric ne publie pas de hachage pour ce jar, il est donc protégé uniquement par HTTPS et n'est pas vérifié par hachage. Au premier démarrage, il téléchargera lui-même le serveur vanilla et les bibliothèques.";
        public const string RunDownloadWithProgress2 = "serveur Fabric";
        public const string RunFail28 = "Échec du téléchargement du serveur Fabric : ";
        public const string RunWizLog21 = "Dossier mods créé. Rappel : les serveurs Fabric ont généralement aussi besoin du mod Fabric API dans mods, et chaque joueur a besoin des mêmes mods double face dans son client.";
        public const string RunFail29 = "Impossible d'obtenir la liste des builds Paper (Fill API) : ";
        public const string RunFail30 = ". Remarque : les versions sans builds Paper renvoient une erreur ici.";
        public const string RunFail31 = "Aucun serveur téléchargeable trouvé dans la liste des builds Paper.";
        public const string RunWizLog22 = "Téléchargement du serveur Paper (dernier build)...";
        public const string RunDownloadWithProgress3 = "serveur Paper";
        public const string RunFail32 = "Échec du téléchargement du serveur Paper : ";
        public const string RunWizLog23 = "Vérification du SHA256 du serveur...";
        public const string RunFail33 = "Le SHA256 du serveur ne correspond pas ; le jar a été supprimé. Attendu ";
        public const string RunWizStep3 = "Étape 3 : écriture des fichiers de départ";
        public const string RunWizLog24 = "eula.txt écrit (eula=true, comme tu l'as accepté).";
        public const string RunWizLog25 = "EULA non accepté : eula.txt n'a pas été écrit. Au premier démarrage, le serveur générera eula=false et quittera ; mets-le à eula=true pour démarrer.";
        public const string RunWizLog26 = "server.properties écrit : server-port=25565, online-mode=false (obligatoire pour le multijoueur hors ligne).";
        public const string RunWizLog27 = "Un server.properties existe déjà et est resté intact. Assure-toi toi-même que online-mode=false, sinon les comptes hors ligne ne pourront pas rejoindre.";
        public const string RunWizStep4 = "Étape 4 : enregistrement des paramètres";
        public const string RunWizLog28 = "Terminé. Dossier du serveur : ";
        public const string RunWizLog29 = "; jar : server.jar; Java : ";
        public const string RunStr2 = "(paramètre précédent conservé)";
        public const string RunWizLog30 = "Ferme l'assistant, remplis la clé d'accès SakuraFrp et les ID de tunnel dans les Paramètres, puis clique sur Démarrer le multijoueur.";
        public const string RunWizStep5 = "Installation terminée";
        public const string RunFail34 = "Erreur de l'assistant : ";
        public const string DownloadWithProgressWizStep = "Téléchargement ";
        public const string DownloadWithProgressWizStep2 = "... ";
        public const string FailWizLog = "Échec : ";
        public const string FailWizStep = "Arrêté";
        public const string CancelledWizLog = "Annulé. Les paramètres n'ont pas été enregistrés ; les fichiers entièrement téléchargés restent dans le dossier de destination, les fichiers partiels ont été nettoyés.";
        public const string CancelledWizStep = "Annulé";
        public const string MainFormAppendServerLog = "[Lanceur] Démarré. Les journaux sont aussi enregistrés dans le dossier logs à côté du programme (conservés 14 jours).";
        public const string TrayText = "Assistant multijoueur Minecraft · Arrêté";
        public const string InitTray613ToolStripMenuItem = "Afficher la fenêtre principale";
        public const string InitTray613ToolStripMenuItem2 = "Quitter en sécurité";
        public const string BtnStartText2 = "Démarrer le multijoueur";
        public const string AddrLblCaption = "Adresse publique (à envoyer à tes amis) : ";
        public const string Hint = "L'adresse publique apparaîtra ici une fois le tunnel démarré";
        public const string BtnCopyText = "Copier l'adresse pour les amis";
        public const string BtnPcl2Text = "Ouvrir PCL2";
        public const string BtnStopText = "Arrêter le serveur en sécurité";
        public const string LogToggleLblCaption = "▾  Journal";
        public const string BuildUiStr = "Serveur";
        public const string BuildUiStr2 = "Tunnel";
        public const string BuildUiStr3 = "État";
        public const string BuildUiStr4 = "Sauvegardes";
        public const string BuildUiMakeLogBox = "Aucun journal pour l'instant · La sortie du serveur apparaîtra ici";
        public const string BuildUiMakeLogBox2 = "Aucun journal pour l'instant · La sortie du tunnel apparaîtra ici";
        public const string BtnJumpBottomText = "↓ Revenir en bas";
        public const string Hint2 = "Rechercher dans les journaux…";
        public const string BuildUiSetToolTip = "Précédent";
        public const string BuildUiSetToolTip2 = "Suivant";
        public const string BuildUiSetToolTip3 = "Recherche dans le journal ; appuie sur Entrée pour aller à la correspondance suivante";
        public const string ChkErrOnlyText = "Avertissements/erreurs seulement";
        public const string CmdLblCaption = "Commande du serveur";
        public const string BtnCmdSendText = "Envoyer";
        public const string BuildUiAppendState = "Première utilisation : remplis la clé d'accès SakuraFrp et l'ID du tunnel dans les Paramètres, et vérifie le chemin de PCL2.";
        public const string IsErrLineContains = "[Watchdog]";
        public const string IsErrLineContains2 = "[Sauvegarde] Échec";
        public const string LblFindCaption = "Aucun résultat";
        public const string SrvState2 = "En cours · ";
        public const string SampleStatsPart = " joueur(s)";
        public const string SampleStatsPart2 = " joueur(s) en ligne (liste non publique)";
        public const string SampleStatsPart3 = "Ping ";
        public const string ToggleLogStr = "▸  Journal";
        public const string AppendServerLogPersistLog613 = "Serveur";
        public const string AppendTunnelLogPersistLog613 = "Tunnel";
        public const string SendServerCommandAppendState = "Le serveur n'est pas en cours d'exécution ; la commande ne peut pas être envoyée.";
        public const string SendServerCommandAppendState2 = "Échec de l'envoi de la commande : le processus du serveur n'existe plus.";
        public const string SendServerCommandAppendState3 = "Échec de l'envoi de la commande : ";
        public const string OpenPlayerManagerShow = "Le serveur n'est pas en cours d'exécution ; la gestion des joueurs est disponible après le démarrage du serveur.";
        public const string OpenPlayerManagerAppendState = "La commande a échoué : le processus du serveur n'existe plus.";
        public const string DoBackupAppendState = "Une sauvegarde est déjà en cours.";
        public const string BackupCoreAppendState = "Dossier du monde introuvable : ";
        public const string BackupCoreAppendState2 = ", sauvegarde impossible.";
        public const string BackupCoreAppendServerLog = "[Sauvegarde] Sauvegarde automatique du serveur suspendue et vidée ; empaquetage du monde…";
        public const string BackupCoreAppendServerLog2 = "[Sauvegarde] Terminé : ";
        public const string BackupCorePart = " fichiers";
        public const string BackupCoreStr = ", ignorés ";
        public const string BackupCorePart2 = " fichier(s) en cours d'utilisation";
        public const string BackupCoreAppendState3 = "Sauvegarde du monde terminée (";
        public const string BackupCoreAppendServerLog3 = "[Sauvegarde] Échec : ";
        public const string BackupCoreAppendState4 = "Échec de la sauvegarde : ";
        public const string BackupCoreAppendServerLog4 = "[Sauvegarde] Sauvegarde automatique du serveur reprise.";
        public const string RestoreBackupShow = "Tu dois arrêter le serveur avant de restaurer : échanger les fichiers du monde pendant que le serveur tourne corrompra la sauvegarde.\n\nClique d'abord sur « Arrêter le serveur en sécurité », puis restaure.";
        public const string RestoreBackupShow2 = "Restaurer la sauvegarde";
        public const string RestoreBackupConfirmBox = "Restaurer le monde vers la sauvegarde « ";
        public const string RestoreBackupConfirmBox2 = " » ?\n\nLe monde actuel ne sera pas supprimé : il est d'abord renommé et mis de côté (world_prerestore_<heure>) ; supprime-le toi-même une fois tout vérifié. Si la restauration était erronée, tu pourras revenir en arrière.";
        public const string RestoreBackupAppendState = "Une tâche de sauvegarde est déjà en cours.";
        public const string Result = "Restauré vers la sauvegarde « ";
        public const string RestoreBackupPart = " ». L'ancien monde a été conservé dans ";
        public const string RestoreBackupPart2 = " ; une fois le nouveau monde vérifié, tu pourras le supprimer.";
        public const string Result2 = "Échec de la restauration : ";
        public const string RestoreBackupPart3 = "(L'ancien monde a peut-être déjà été renommé. Ne démarre pas encore le serveur ; vérifie le dossier du serveur.)";
        public const string RestoreBackupAppendServerLog = "[Sauvegarde] ";
        public const string BuildBackupPanelBkLabel = "Sauvegardes du monde";
        public const string BtnBackupNowText = "Sauvegarder maintenant";
        public const string BuildBackupPanelDoBackup = "Sauvegarde manuelle";
        public const string BtnRestoreBackupText = "Restaurer la sélectionnée";
        public const string BuildBackupPanelShow = "Sélectionne d'abord une sauvegarde dans la liste ci-dessus.";
        public const string BtnDeleteBackupText = "Supprimer la sélectionnée";
        public const string BuildBackupPanelShow2 = "Supprimer la sauvegarde";
        public const string BuildBackupPanelConfirmBox = "Supprimer la sauvegarde « ";
        public const string BuildBackupPanelPart = " » ? Elle ne pourra pas être récupérée après suppression.";
        public const string BuildBackupPanelShow3 = "Échec de la suppression : ";
        public const string BuildBackupPanelBkLabel2 = "Conserver les ";
        public const string BuildBackupPanelBkLabel3 = " dernières sauvegardes";
        public const string BuildBackupPanelBkCheck = "Redémarrage automatique en cas de plantage";
        public const string BuildBackupPanelBkCheck2 = "Sauvegarde automatique avant le démarrage du serveur";
        public const string BuildBackupPanelBkCheck3 = "Sauvegarde automatique quotidienne";
        public const string BuildBackupPanelBkLabel4 = "Pendant que l'application est ouverte, sauvegarde automatique à l'heure définie";
        public const string BuildBackupPanelBkCheck4 = "Commande planifiée";
        public const string LblBackupStateCaption = "Sauvegarde en cours… (les grands mondes peuvent prendre une ou deux minutes)";
        public const string RefreshBackupListStr = "Total ";
        public const string RefreshBackupListPart = " sauvegardes · ";
        public const string RefreshBackupListStr2 = "Aucune sauvegarde pour l'instant";
        public const string RefreshBackupListStr3 = "Dernière : ";
        public const string RefreshBackupListStr4 = "Clique sur « Sauvegarder maintenant » pour enregistrer un instantané du monde.";
        public const string CheckSchedulesAppendServerLog = "[Planifié] Heure de la sauvegarde quotidienne atteinte ; démarrage de la sauvegarde automatique.";
        public const string CheckSchedulesAppendServerLog2 = "[Planifié] Une commande planifiée est due, mais ";
        public const string CheckSchedulesAppendServerLog3 = " joueur(s) sont encore en ligne ; report jusqu'à ce que le serveur soit vide.";
        public const string CheckSchedulesAppendServerLog4 = "[Planifié] Commande planifiée envoyée : ";
        public const string ServerText2 = "Serveur : en cours";
        public const string ServerText3 = "Serveur : démarrage…";
        public const string TunnelText2 = "Tunnel SakuraFrp : connecté";
        public const string TunnelText3 = "Tunnel SakuraFrp : démarrage…";
        public const string AddrText2 = "Adresse publique : prête";
        public const string AddrText3 = "Adresse publique : enregistrée (tunnel arrêté)";
        public const string Tt = "Assistant multijoueur Minecraft · ";
        public const string RefreshStatusStr = "En cours";
        public const string RefreshStatusStr2 = "Démarrage";
        public const string CopyAddrShow = "Pas encore d'adresse publique. Démarre d'abord le multijoueur.";
        public const string CopyAddrShow2 = "Avis";
        public const string CopyAddrAppendState = "Adresse publique copiée dans le presse-papiers ; envoie-la à tes amis.";
        public const string BtnCopyText2 = "Copié ✓";
        public const string OpenPcl2Show = "PCL2 introuvable. Définis le chemin de l'exe PCL2 dans les Paramètres.";
        public const string OpenPcl2Show2 = "Échec de l'ouverture de PCL2 : ";
        public const string HandleServerCrashAppendServerLog = "[Watchdog] Le serveur s'est fermé de façon inattendue (code de sortie ";
        public const string HandleServerCrashAppendServerLog2 = ") ; ce n'était pas un arrêt normal.";
        public const string HandleServerCrashStr = "Cause possible : ";
        public const string HandleServerCrashAppendState = "Le serveur s'est fermé de façon inattendue";
        public const string HandleServerCrashStr2 = ", cause possible : ";
        public const string HandleServerCrashStr3 = ". Consulte la fin du journal du serveur pour en trouver la cause.";
        public const string HandleServerCrashPart = "(Le redémarrage automatique du watchdog est désactivé)";
        public const string HandleServerCrashAppendServerLog3 = "[Watchdog] Plantages : ";
        public const string HandleServerCrashAppendServerLog4 = " en 10 minutes ; redémarrage automatique arrêté. Il s'agit très probablement d'un problème de sauvegarde ou de mod.";
        public const string HandleServerCrashAppendState2 = "Le serveur n'arrêtait pas de planter ; le redémarrage automatique s'est arrêté. Consulte le journal.";
        public const string HandleServerCrashStr4 = " Cause possible : ";
        public const string HandleServerCrashAppendState3 = "Le serveur s'est fermé de façon inattendue ; le watchdog le redémarrera dans 5 secondes…";
        public const string GuessCrash614Ret = "Le port ";
        public const string GuessCrash614Part = " est déjà utilisé — probablement une autre instance du serveur qui ne s'est pas fermée proprement";
        public const string GuessCrash614Ret2 = "Mémoire insuffisante, ou un mauvais réglage mémoire (-Xmx)";
        public const string GuessCrash614Ret3 = "Mauvaise version de Java — ce serveur a besoin de Java 17";
        public const string GuessCrash614Ret4 = "Le serveur s'est figé pendant plus de 60 secondes et a été arrêté par le watchdog intégré du jeu";
        public const string GuessCrash614Ret5 = "Un mod n'a pas pu se charger ; trouve le mod nommé dans les erreurs du journal";
        public const string GuessCrash614Ret6 = "L'EULA n'a pas été accepté";
        public const string OfferWizardIfNeededShow = "Il n'y a pas encore de serveur utilisable : le dossier du serveur configuré n'existe pas, ou ne contient aucun jar de serveur.\n\nL'assistant de première configuration peut télécharger un Java et un serveur adaptés (Vanilla / Fabric / Paper) et écrire l'EULA et le server.properties requis pour le jeu hors ligne. Tous les téléchargements sont vérifiés par hachage.";
        public const string OfferWizardIfNeededShow2 = "Ouvrir l'assistant";
        public const string OfferWizardIfNeededShow3 = "Plus tard";
        public const string OpenWizardAppendState = "Assistant de première configuration terminé : le serveur et Java sont prêts.";
        public const string CheckJavaVersionStr = "inconnue";
        public const string CheckJavaVersionAppendServerLog = "[Contrôle Java] Impossible de lire les informations de version du jar du serveur (version.json) ; contrôle de version ignoré. Version majeure actuelle de Java : ";
        public const string CheckJavaVersionAppendServerLog2 = ".";
        public const string CheckJavaVersionAppendServerLog3 = "[Contrôle Java] Version du serveur ";
        public const string CheckJavaVersionAppendServerLog4 = " n'est pas dans le tableau des versions ; contrôle de version ignoré. Version majeure actuelle de Java : ";
        public const string CheckJavaVersionAppendServerLog5 = "[Contrôle Java] Serveur ";
        public const string CheckJavaVersionAppendServerLog6 = ", mais la version actuelle de Java n'a pas pu être mesurée ; démarrage quand même (surveille le journal du serveur).";
        public const string CheckJavaVersionAppendServerLog7 = "[Contrôle Java] Réussi : serveur ";
        public const string CheckJavaVersionAppendServerLog8 = ", le Java actuel est ";
        public const string CheckJavaVersionAppendServerLog9 = "[Contrôle Java] Incompatibilité : serveur ";
        public const string CheckJavaVersionAppendServerLog10 = ", le Java actuel est seulement ";
        public const string CheckJavaVersionAppendServerLog11 = " ; en attente de la décision de l'utilisateur.";
        public const string Msg2 = "La version de Java est trop ancienne — le serveur échouera très probablement à démarrer.\n\nVersion du serveur : ";
        public const string CheckJavaVersionPart = ", qui requiert Java ";
        public const string CheckJavaVersionPart2 = " ou plus récent. Le Java sur le point d'être utilisé est en version majeure ";
        public const string CheckJavaVersionPart3 = ".\n\nSuggestion : choisis le java.exe d'un Java ";
        public const string CheckJavaVersionPart4 = " dans les Paramètres, ou laisse l'assistant de première configuration télécharger automatiquement un Java adapté.\nChoisir Démarrer quand même lance avec le Java actuel dans tous les cas ; consulte le journal du serveur pour les erreurs qui en résultent.";
        public const string CheckJavaVersionShow = "Version de Java incompatible";
        public const string CheckJavaVersionShow2 = "Démarrer quand même";
        public const string CheckJavaVersionShow3 = "Annuler le démarrage";
        public const string CheckJavaVersionAppendServerLog12 = "[Contrôle Java] L'utilisateur a choisi de démarrer quand même.";
        public const string CheckJavaVersionAppendServerLog13 = "[Contrôle Java] L'utilisateur a annulé le démarrage.";
        public const string StartAllAppendState = "Le serveur est déjà en cours d'exécution ; pas besoin de le démarrer à nouveau.";
        public const string BtnStartText3 = "Multijoueur en cours";
        public const string BtnStartText4 = "Démarrage…";
        public const string DoStartFailState = "Le dossier du serveur n'existe pas : ";
        public const string DoStartFailState2 = ". Vérifie le dossier du serveur dans les Paramètres.";
        public const string DoStartFailState3 = "Jar du serveur introuvable : ";
        public const string DoStartFailState4 = ". Vérifie le nom du fichier jar dans les Paramètres.";
        public const string DoStartFailState5 = "Java (java.exe) introuvable. Sélectionne à nouveau le chemin de Java dans les Paramètres — habituellement sous .minecraft\\runtime ou dans le dossier d'installation de Java.";
        public const string DoStartFailState6 = "Démarrage annulé : la version de Java est inférieure à ce que le serveur requiert. Passe à un Java adapté, ou laisse l'assistant de première configuration en télécharger un, puis réessaie.";
        public const string DoStartFailState7 = " est déjà utilisé. Le dernier serveur ne s'est peut-être pas fermé proprement — clique sur « Arrêter le serveur en sécurité » ou redémarre cette application ; ne tue pas toi-même les processus Java dans le Gestionnaire des tâches.";
        public const string DoStartAppendState = "Sauvegarde automatique avant le démarrage du serveur…";
        public const string DoStartAppendState2 = "Démarrage du serveur Minecraft…";
        public const string DoStartAppendServerLog = "[Processus terminé]";
        public const string DoStartFailState8 = "Impossible de démarrer le processus Java. Vérifie que le chemin de Java est correct.";
        public const string DoStartAppendState3 = "En attente que le serveur soit prêt (surveillance du « Done »)…";
        public const string DoStartFailState9 = "Le processus du serveur s'est terminé avant l'apparition de « Done ». Le plus probable : 1) mauvais chemin Java ou mauvais arguments mémoire ; 2) une erreur dans la sauvegarde/le monde ou les mods (consulte la fin du journal du serveur). Ne touche ni au monde ni aux mods.";
        public const string DoStartFailState10 = "Aucun « Done » après 90 secondes. Possiblement une mémoire insuffisante, un monde trop grand, ou un démarrage bloqué. Consulte la fin du journal du serveur ; ne touche ni au monde ni aux mods.";
        public const string DoStartAppendState4 = "Prêt. Clique sur « Copier l'adresse pour les amis » et envoie l'adresse à tes amis.";
        public const string DoStartFailState11 = "Échec du démarrage : ";
        public const string StartTunnelAppendTunnelLog = "[Secours] Ouverture du lanceur officiel SakuraFrp — clique sur Démarrer le tunnel dedans.";
        public const string StartTunnelAppendTunnelLog2 = "Échec de l'ouverture du lanceur SakuraFrp : ";
        public const string StartTunnelAppendTunnelLog3 = "frpc.exe introuvable : ";
        public const string StartTunnelAppendTunnelLog4 = ". Vérifie-le dans les Paramètres, ou bascule le mode de tunnel vers le lanceur officiel.";
        public const string StartTunnelAppendTunnelLog5 = "La clé d'accès est vide. Remplis la clé d'accès SakuraFrp dans les Paramètres.";
        public const string StartTunnelAppendTunnelLog6 = "L'ID du tunnel est vide. Remplis-le dans les Paramètres.";
        public const string StartTunnelAppendState = "Démarrage du tunnel SakuraFrp…";
        public const string StartTunnelAppendTunnelLog7 = "[frpc terminé]";
        public const string StartTunnelAppendTunnelLog8 = "[Watchdog] Le tunnel de jeu s'est fermé de façon inattendue ; reconnexion dans 5 secondes.";
        public const string StartTunnelAppendTunnelLog9 = "frpc n'a pas pu démarrer. Vérifie le chemin de frpc.exe.";
        public const string StartTunnelAppendTunnelLog10 = "Le frpc.exe actuel est le build fourni avec le lanceur SakuraFrp. Ce build ne peut pas être exécuté directement par des programmes externes (uniquement via le SakuraFrp Service), il se ferme donc immédiatement. Cela n'a rien à voir avec ta clé d'accès, ton ID de tunnel ou le nœud. Dans les Paramètres, fais pointer le chemin de frpc vers un frpc autonome, ou bascule le mode de tunnel vers le lanceur officiel.";
        public const string StartTunnelAppendTunnelLog11 = "Le processus frpc s'est terminé. Possiblement : 1) mauvaise clé d'accès ou mauvais ID de tunnel ; 2) un problème de nœud ; 3) le compte a besoin d'une vérification ou d'un check-in. Vérifie la clé et l'ID de tunnel dans les Paramètres, ou bascule le mode de tunnel vers le lanceur officiel.";
        public const string StartTunnelAppendState2 = "Ce frpc est le build fourni avec le lanceur et ne peut pas s'exécuter directement. Dans les Paramètres, passe à un frpc autonome ou au mode lanceur officiel.";
        public const string StartVoiceTunnelAppendTunnelLog = "[Voix] Aucun ID de tunnel vocal défini ; le tunnel vocal est ignoré.";
        public const string StartVoiceTunnelAppendTunnelLog2 = "[Voix frpc terminé]";
        public const string StartVoiceTunnelAppendTunnelLog3 = "[Watchdog] Le tunnel vocal s'est fermé de façon inattendue ; reconnexion dans 5 secondes.";
        public const string StartVoiceTunnelAppendTunnelLog4 = "[Voix] frpc n'a pas pu démarrer. Vérifie le chemin de frpc.exe.";
        public const string StartVoiceTunnelAppendTunnelLog5 = "[Voix] Le processus frpc s'est terminé. Vérifie que l'ID du tunnel vocal est correct.";
        public const string StartVoiceTunnelAppendState = "Le tunnel vocal est connecté.";
        public const string VoiceFrpcOutputAppendTunnelLog = "[Voix] ";
        public const string VoiceFrpcErrorAppendTunnelLog = "[Voix][ERR] ";
        public const string TryParseFrpcAppendState = "Adresse publique obtenue automatiquement : ";
        public const string ServerText4 = "Serveur : erreur";
        public const string TunnelText4 = "Tunnel SakuraFrp : erreur";
        public const string SafeStopAppendState = "Le serveur n'est pas en cours d'exécution ; le tunnel a été arrêté.";
        public const string SafeStopAppendState2 = "Arrêt en sécurité : envoi de stop au serveur et attente de la sauvegarde…";
        public const string SafeStopAppendServerLog = ">> stop envoyé ; en attente de la sauvegarde et de la sortie (jusqu'à 60 secondes)";
        public const string SafeStopAppendState3 = "Le serveur s'est arrêté normalement (sauvegarde terminée). Fermeture du tunnel…";
        public const string SafeStopAppendServerLog2 = "[Le processus du serveur s'est terminé normalement]";
        public const string SafeStopShow = "Le serveur ne s'est toujours pas fermé après 60 secondes. Forcer sa fermeture peut faire perdre les dernières minutes de progression. Forcer la fermeture ?";
        public const string SafeStopShow2 = "Délai d'arrêt en sécurité dépassé";
        public const string SafeStopShow3 = "Forcer la fermeture";
        public const string SafeStopShow4 = "Attendre encore";
        public const string SafeStopAppendState4 = "Le processus du serveur a été fermé de force (la progression récente peut être perdue).";
        public const string SafeStopAppendState5 = "Fermeture forcée annulée ; le serveur est toujours en cours d'exécution.";
        public const string SafeStopAppendState6 = "Tout est arrêté : le serveur et le tunnel sont fermés.";
        public const string OnClosingConfirmBox = "Le serveur est toujours en cours d'exécution. Fermer directement peut sauter la sauvegarde — l'arrêter en sécurité d'abord ?";
        public const string BtnSparkTpsText = "Rapport TPS";
        public const string BtnSparkProfileText = "Diagnostiquer 30 s";
        public const string SparkLblCaption = "Diagnostic";
        public const string SparkProfileStarted = "[Diagnostic] Diagnostic de 30 s commencé ; garde le serveur en cours d'exécution...";
        public const string SparkProfileStopped = "[Diagnostic] Diagnostic terminé ; le lien du rapport spark apparaît dans le journal du serveur ci-dessus.";
        public const string SparkProfileCancelled = "[Diagnostic] Le serveur s'est arrêté ; cette session de diagnostic a été annulée.";
        public const string LangCaption = "Langue";
        public const string LangRestartHint = "Langue enregistrée. Redémarre l'application pour l'appliquer.";
        public const string LangRestartNow = "Redémarrer maintenant";
        public const int EulaAgreeWidth = 230;
        public const int EulaLinkDx = 232;
    }


    static class Txt
    {
        public static string Msg { get { switch (Lang.Code) { case "tw": return TxtTw.Msg; case "en": return TxtEn.Msg; case "ja": return TxtJa.Msg; case "ko": return TxtKo.Msg; case "es": return TxtEs.Msg; case "pt": return TxtPt.Msg; case "ru": return TxtRu.Msg; case "de": return TxtDe.Msg; case "fr": return TxtFr.Msg; default: return TxtCn.Msg; } } }
        public static string ShowFriendlyErrorStr { get { switch (Lang.Code) { case "tw": return TxtTw.ShowFriendlyErrorStr; case "en": return TxtEn.ShowFriendlyErrorStr; case "ja": return TxtJa.ShowFriendlyErrorStr; case "ko": return TxtKo.ShowFriendlyErrorStr; case "es": return TxtEs.ShowFriendlyErrorStr; case "pt": return TxtPt.ShowFriendlyErrorStr; case "ru": return TxtRu.ShowFriendlyErrorStr; case "de": return TxtDe.ShowFriendlyErrorStr; case "fr": return TxtFr.ShowFriendlyErrorStr; default: return TxtCn.ShowFriendlyErrorStr; } } }
        public static string ShowFriendlyErrorStr2 { get { switch (Lang.Code) { case "tw": return TxtTw.ShowFriendlyErrorStr2; case "en": return TxtEn.ShowFriendlyErrorStr2; case "ja": return TxtJa.ShowFriendlyErrorStr2; case "ko": return TxtKo.ShowFriendlyErrorStr2; case "es": return TxtEs.ShowFriendlyErrorStr2; case "pt": return TxtPt.ShowFriendlyErrorStr2; case "ru": return TxtRu.ShowFriendlyErrorStr2; case "de": return TxtDe.ShowFriendlyErrorStr2; case "fr": return TxtFr.ShowFriendlyErrorStr2; default: return TxtCn.ShowFriendlyErrorStr2; } } }
        public static string ShowFriendlyErrorPart { get { switch (Lang.Code) { case "tw": return TxtTw.ShowFriendlyErrorPart; case "en": return TxtEn.ShowFriendlyErrorPart; case "ja": return TxtJa.ShowFriendlyErrorPart; case "ko": return TxtKo.ShowFriendlyErrorPart; case "es": return TxtEs.ShowFriendlyErrorPart; case "pt": return TxtPt.ShowFriendlyErrorPart; case "ru": return TxtRu.ShowFriendlyErrorPart; case "de": return TxtDe.ShowFriendlyErrorPart; case "fr": return TxtFr.ShowFriendlyErrorPart; default: return TxtCn.ShowFriendlyErrorPart; } } }
        public static string ShowFriendlyErrorShow { get { switch (Lang.Code) { case "tw": return TxtTw.ShowFriendlyErrorShow; case "en": return TxtEn.ShowFriendlyErrorShow; case "ja": return TxtJa.ShowFriendlyErrorShow; case "ko": return TxtKo.ShowFriendlyErrorShow; case "es": return TxtEs.ShowFriendlyErrorShow; case "pt": return TxtPt.ShowFriendlyErrorShow; case "ru": return TxtRu.ShowFriendlyErrorShow; case "de": return TxtDe.ShowFriendlyErrorShow; case "fr": return TxtFr.ShowFriendlyErrorShow; default: return TxtCn.ShowFriendlyErrorShow; } } }
        public static string YesText { get { switch (Lang.Code) { case "tw": return TxtTw.YesText; case "en": return TxtEn.YesText; case "ja": return TxtJa.YesText; case "ko": return TxtKo.YesText; case "es": return TxtEs.YesText; case "pt": return TxtPt.YesText; case "ru": return TxtRu.YesText; case "de": return TxtDe.YesText; case "fr": return TxtFr.YesText; default: return TxtCn.YesText; } } }
        public static string NoText { get { switch (Lang.Code) { case "tw": return TxtTw.NoText; case "en": return TxtEn.NoText; case "ja": return TxtJa.NoText; case "ko": return TxtKo.NoText; case "es": return TxtEs.NoText; case "pt": return TxtPt.NoText; case "ru": return TxtRu.NoText; case "de": return TxtDe.NoText; case "fr": return TxtFr.NoText; default: return TxtCn.NoText; } } }
        public static string SettingsFormInitChrome { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormInitChrome; case "en": return TxtEn.SettingsFormInitChrome; case "ja": return TxtJa.SettingsFormInitChrome; case "ko": return TxtKo.SettingsFormInitChrome; case "es": return TxtEs.SettingsFormInitChrome; case "pt": return TxtPt.SettingsFormInitChrome; case "ru": return TxtRu.SettingsFormInitChrome; case "de": return TxtDe.SettingsFormInitChrome; case "fr": return TxtFr.SettingsFormInitChrome; default: return TxtCn.SettingsFormInitChrome; } } }
        public static string SettingsFormAddField { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField; case "en": return TxtEn.SettingsFormAddField; case "ja": return TxtJa.SettingsFormAddField; case "ko": return TxtKo.SettingsFormAddField; case "es": return TxtEs.SettingsFormAddField; case "pt": return TxtPt.SettingsFormAddField; case "ru": return TxtRu.SettingsFormAddField; case "de": return TxtDe.SettingsFormAddField; case "fr": return TxtFr.SettingsFormAddField; default: return TxtCn.SettingsFormAddField; } } }
        public static string SettingsFormAddField2 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField2; case "en": return TxtEn.SettingsFormAddField2; case "ja": return TxtJa.SettingsFormAddField2; case "ko": return TxtKo.SettingsFormAddField2; case "es": return TxtEs.SettingsFormAddField2; case "pt": return TxtPt.SettingsFormAddField2; case "ru": return TxtRu.SettingsFormAddField2; case "de": return TxtDe.SettingsFormAddField2; case "fr": return TxtFr.SettingsFormAddField2; default: return TxtCn.SettingsFormAddField2; } } }
        public static string SettingsFormAddField3 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField3; case "en": return TxtEn.SettingsFormAddField3; case "ja": return TxtJa.SettingsFormAddField3; case "ko": return TxtKo.SettingsFormAddField3; case "es": return TxtEs.SettingsFormAddField3; case "pt": return TxtPt.SettingsFormAddField3; case "ru": return TxtRu.SettingsFormAddField3; case "de": return TxtDe.SettingsFormAddField3; case "fr": return TxtFr.SettingsFormAddField3; default: return TxtCn.SettingsFormAddField3; } } }
        public static string SettingsFormAddField4 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField4; case "en": return TxtEn.SettingsFormAddField4; case "ja": return TxtJa.SettingsFormAddField4; case "ko": return TxtKo.SettingsFormAddField4; case "es": return TxtEs.SettingsFormAddField4; case "pt": return TxtPt.SettingsFormAddField4; case "ru": return TxtRu.SettingsFormAddField4; case "de": return TxtDe.SettingsFormAddField4; case "fr": return TxtFr.SettingsFormAddField4; default: return TxtCn.SettingsFormAddField4; } } }
        public static string SettingsFormAddField5 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField5; case "en": return TxtEn.SettingsFormAddField5; case "ja": return TxtJa.SettingsFormAddField5; case "ko": return TxtKo.SettingsFormAddField5; case "es": return TxtEs.SettingsFormAddField5; case "pt": return TxtPt.SettingsFormAddField5; case "ru": return TxtRu.SettingsFormAddField5; case "de": return TxtDe.SettingsFormAddField5; case "fr": return TxtFr.SettingsFormAddField5; default: return TxtCn.SettingsFormAddField5; } } }
        public static string SettingsFormAddField6 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField6; case "en": return TxtEn.SettingsFormAddField6; case "ja": return TxtJa.SettingsFormAddField6; case "ko": return TxtKo.SettingsFormAddField6; case "es": return TxtEs.SettingsFormAddField6; case "pt": return TxtPt.SettingsFormAddField6; case "ru": return TxtRu.SettingsFormAddField6; case "de": return TxtDe.SettingsFormAddField6; case "fr": return TxtFr.SettingsFormAddField6; default: return TxtCn.SettingsFormAddField6; } } }
        public static string LbModeCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LbModeCaption; case "en": return TxtEn.LbModeCaption; case "ja": return TxtJa.LbModeCaption; case "ko": return TxtKo.LbModeCaption; case "es": return TxtEs.LbModeCaption; case "pt": return TxtPt.LbModeCaption; case "ru": return TxtRu.LbModeCaption; case "de": return TxtDe.LbModeCaption; case "fr": return TxtFr.LbModeCaption; default: return TxtCn.LbModeCaption; } } }
        public static string SettingsFormAdd { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAdd; case "en": return TxtEn.SettingsFormAdd; case "ja": return TxtJa.SettingsFormAdd; case "ko": return TxtKo.SettingsFormAdd; case "es": return TxtEs.SettingsFormAdd; case "pt": return TxtPt.SettingsFormAdd; case "ru": return TxtRu.SettingsFormAdd; case "de": return TxtDe.SettingsFormAdd; case "fr": return TxtFr.SettingsFormAdd; default: return TxtCn.SettingsFormAdd; } } }
        public static string SettingsFormAdd2 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAdd2; case "en": return TxtEn.SettingsFormAdd2; case "ja": return TxtJa.SettingsFormAdd2; case "ko": return TxtKo.SettingsFormAdd2; case "es": return TxtEs.SettingsFormAdd2; case "pt": return TxtPt.SettingsFormAdd2; case "ru": return TxtRu.SettingsFormAdd2; case "de": return TxtDe.SettingsFormAdd2; case "fr": return TxtFr.SettingsFormAdd2; default: return TxtCn.SettingsFormAdd2; } } }
        public static string LbKeyCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LbKeyCaption; case "en": return TxtEn.LbKeyCaption; case "ja": return TxtJa.LbKeyCaption; case "ko": return TxtKo.LbKeyCaption; case "es": return TxtEs.LbKeyCaption; case "pt": return TxtPt.LbKeyCaption; case "ru": return TxtRu.LbKeyCaption; case "de": return TxtDe.LbKeyCaption; case "fr": return TxtFr.LbKeyCaption; default: return TxtCn.LbKeyCaption; } } }
        public static string SettingsFormAddField7 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField7; case "en": return TxtEn.SettingsFormAddField7; case "ja": return TxtJa.SettingsFormAddField7; case "ko": return TxtKo.SettingsFormAddField7; case "es": return TxtEs.SettingsFormAddField7; case "pt": return TxtPt.SettingsFormAddField7; case "ru": return TxtRu.SettingsFormAddField7; case "de": return TxtDe.SettingsFormAddField7; case "fr": return TxtFr.SettingsFormAddField7; default: return TxtCn.SettingsFormAddField7; } } }
        public static string SettingsFormAddField8 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField8; case "en": return TxtEn.SettingsFormAddField8; case "ja": return TxtJa.SettingsFormAddField8; case "ko": return TxtKo.SettingsFormAddField8; case "es": return TxtEs.SettingsFormAddField8; case "pt": return TxtPt.SettingsFormAddField8; case "ru": return TxtRu.SettingsFormAddField8; case "de": return TxtDe.SettingsFormAddField8; case "fr": return TxtFr.SettingsFormAddField8; default: return TxtCn.SettingsFormAddField8; } } }
        public static string SettingsFormAddField9 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField9; case "en": return TxtEn.SettingsFormAddField9; case "ja": return TxtJa.SettingsFormAddField9; case "ko": return TxtKo.SettingsFormAddField9; case "es": return TxtEs.SettingsFormAddField9; case "pt": return TxtPt.SettingsFormAddField9; case "ru": return TxtRu.SettingsFormAddField9; case "de": return TxtDe.SettingsFormAddField9; case "fr": return TxtFr.SettingsFormAddField9; default: return TxtCn.SettingsFormAddField9; } } }
        public static string SettingsFormAddField10 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormAddField10; case "en": return TxtEn.SettingsFormAddField10; case "ja": return TxtJa.SettingsFormAddField10; case "ko": return TxtKo.SettingsFormAddField10; case "es": return TxtEs.SettingsFormAddField10; case "pt": return TxtPt.SettingsFormAddField10; case "ru": return TxtRu.SettingsFormAddField10; case "de": return TxtDe.SettingsFormAddField10; case "fr": return TxtFr.SettingsFormAddField10; default: return TxtCn.SettingsFormAddField10; } } }
        public static string BtnPropsText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnPropsText; case "en": return TxtEn.BtnPropsText; case "ja": return TxtJa.BtnPropsText; case "ko": return TxtKo.BtnPropsText; case "es": return TxtEs.BtnPropsText; case "pt": return TxtPt.BtnPropsText; case "ru": return TxtRu.BtnPropsText; case "de": return TxtDe.BtnPropsText; case "fr": return TxtFr.BtnPropsText; default: return TxtCn.BtnPropsText; } } }
        public static string SettingsFormShow { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormShow; case "en": return TxtEn.SettingsFormShow; case "ja": return TxtJa.SettingsFormShow; case "ko": return TxtKo.SettingsFormShow; case "es": return TxtEs.SettingsFormShow; case "pt": return TxtPt.SettingsFormShow; case "ru": return TxtRu.SettingsFormShow; case "de": return TxtDe.SettingsFormShow; case "fr": return TxtFr.SettingsFormShow; default: return TxtCn.SettingsFormShow; } } }
        public static string SettingsFormShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.SettingsFormShow2; case "en": return TxtEn.SettingsFormShow2; case "ja": return TxtJa.SettingsFormShow2; case "ko": return TxtKo.SettingsFormShow2; case "es": return TxtEs.SettingsFormShow2; case "pt": return TxtPt.SettingsFormShow2; case "ru": return TxtRu.SettingsFormShow2; case "de": return TxtDe.SettingsFormShow2; case "fr": return TxtFr.SettingsFormShow2; default: return TxtCn.SettingsFormShow2; } } }
        public static string BtnWizardText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnWizardText; case "en": return TxtEn.BtnWizardText; case "ja": return TxtJa.BtnWizardText; case "ko": return TxtKo.BtnWizardText; case "es": return TxtEs.BtnWizardText; case "pt": return TxtPt.BtnWizardText; case "ru": return TxtRu.BtnWizardText; case "de": return TxtDe.BtnWizardText; case "fr": return TxtFr.BtnWizardText; default: return TxtCn.BtnWizardText; } } }
        public static string BtnSaveText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnSaveText; case "en": return TxtEn.BtnSaveText; case "ja": return TxtJa.BtnSaveText; case "ko": return TxtKo.BtnSaveText; case "es": return TxtEs.BtnSaveText; case "pt": return TxtPt.BtnSaveText; case "ru": return TxtRu.BtnSaveText; case "de": return TxtDe.BtnSaveText; case "fr": return TxtFr.BtnSaveText; default: return TxtCn.BtnSaveText; } } }
        public static string ValidateSettingsMarkInvalid { get { switch (Lang.Code) { case "tw": return TxtTw.ValidateSettingsMarkInvalid; case "en": return TxtEn.ValidateSettingsMarkInvalid; case "ja": return TxtJa.ValidateSettingsMarkInvalid; case "ko": return TxtKo.ValidateSettingsMarkInvalid; case "es": return TxtEs.ValidateSettingsMarkInvalid; case "pt": return TxtPt.ValidateSettingsMarkInvalid; case "ru": return TxtRu.ValidateSettingsMarkInvalid; case "de": return TxtDe.ValidateSettingsMarkInvalid; case "fr": return TxtFr.ValidateSettingsMarkInvalid; default: return TxtCn.ValidateSettingsMarkInvalid; } } }
        public static string ValidateSettingsMarkInvalid2 { get { switch (Lang.Code) { case "tw": return TxtTw.ValidateSettingsMarkInvalid2; case "en": return TxtEn.ValidateSettingsMarkInvalid2; case "ja": return TxtJa.ValidateSettingsMarkInvalid2; case "ko": return TxtKo.ValidateSettingsMarkInvalid2; case "es": return TxtEs.ValidateSettingsMarkInvalid2; case "pt": return TxtPt.ValidateSettingsMarkInvalid2; case "ru": return TxtRu.ValidateSettingsMarkInvalid2; case "de": return TxtDe.ValidateSettingsMarkInvalid2; case "fr": return TxtFr.ValidateSettingsMarkInvalid2; default: return TxtCn.ValidateSettingsMarkInvalid2; } } }
        public static string ValidateSettingsMarkInvalid3 { get { switch (Lang.Code) { case "tw": return TxtTw.ValidateSettingsMarkInvalid3; case "en": return TxtEn.ValidateSettingsMarkInvalid3; case "ja": return TxtJa.ValidateSettingsMarkInvalid3; case "ko": return TxtKo.ValidateSettingsMarkInvalid3; case "es": return TxtEs.ValidateSettingsMarkInvalid3; case "pt": return TxtPt.ValidateSettingsMarkInvalid3; case "ru": return TxtRu.ValidateSettingsMarkInvalid3; case "de": return TxtDe.ValidateSettingsMarkInvalid3; case "fr": return TxtFr.ValidateSettingsMarkInvalid3; default: return TxtCn.ValidateSettingsMarkInvalid3; } } }
        public static string ValidateSettingsMarkInvalid4 { get { switch (Lang.Code) { case "tw": return TxtTw.ValidateSettingsMarkInvalid4; case "en": return TxtEn.ValidateSettingsMarkInvalid4; case "ja": return TxtJa.ValidateSettingsMarkInvalid4; case "ko": return TxtKo.ValidateSettingsMarkInvalid4; case "es": return TxtEs.ValidateSettingsMarkInvalid4; case "pt": return TxtPt.ValidateSettingsMarkInvalid4; case "ru": return TxtRu.ValidateSettingsMarkInvalid4; case "de": return TxtDe.ValidateSettingsMarkInvalid4; case "fr": return TxtFr.ValidateSettingsMarkInvalid4; default: return TxtCn.ValidateSettingsMarkInvalid4; } } }
        public static string ValidateSettingsMarkInvalid5 { get { switch (Lang.Code) { case "tw": return TxtTw.ValidateSettingsMarkInvalid5; case "en": return TxtEn.ValidateSettingsMarkInvalid5; case "ja": return TxtJa.ValidateSettingsMarkInvalid5; case "ko": return TxtKo.ValidateSettingsMarkInvalid5; case "es": return TxtEs.ValidateSettingsMarkInvalid5; case "pt": return TxtPt.ValidateSettingsMarkInvalid5; case "ru": return TxtRu.ValidateSettingsMarkInvalid5; case "de": return TxtDe.ValidateSettingsMarkInvalid5; case "fr": return TxtFr.ValidateSettingsMarkInvalid5; default: return TxtCn.ValidateSettingsMarkInvalid5; } } }
        public static string ValidateSettingsMarkInvalid6 { get { switch (Lang.Code) { case "tw": return TxtTw.ValidateSettingsMarkInvalid6; case "en": return TxtEn.ValidateSettingsMarkInvalid6; case "ja": return TxtJa.ValidateSettingsMarkInvalid6; case "ko": return TxtKo.ValidateSettingsMarkInvalid6; case "es": return TxtEs.ValidateSettingsMarkInvalid6; case "pt": return TxtPt.ValidateSettingsMarkInvalid6; case "ru": return TxtRu.ValidateSettingsMarkInvalid6; case "de": return TxtDe.ValidateSettingsMarkInvalid6; case "fr": return TxtFr.ValidateSettingsMarkInvalid6; default: return TxtCn.ValidateSettingsMarkInvalid6; } } }
        public static string ValidateSettingsMarkInvalid7 { get { switch (Lang.Code) { case "tw": return TxtTw.ValidateSettingsMarkInvalid7; case "en": return TxtEn.ValidateSettingsMarkInvalid7; case "ja": return TxtJa.ValidateSettingsMarkInvalid7; case "ko": return TxtKo.ValidateSettingsMarkInvalid7; case "es": return TxtEs.ValidateSettingsMarkInvalid7; case "pt": return TxtPt.ValidateSettingsMarkInvalid7; case "ru": return TxtRu.ValidateSettingsMarkInvalid7; case "de": return TxtDe.ValidateSettingsMarkInvalid7; case "fr": return TxtFr.ValidateSettingsMarkInvalid7; default: return TxtCn.ValidateSettingsMarkInvalid7; } } }
        public static string ValidateSettingsMarkInvalid8 { get { switch (Lang.Code) { case "tw": return TxtTw.ValidateSettingsMarkInvalid8; case "en": return TxtEn.ValidateSettingsMarkInvalid8; case "ja": return TxtJa.ValidateSettingsMarkInvalid8; case "ko": return TxtKo.ValidateSettingsMarkInvalid8; case "es": return TxtEs.ValidateSettingsMarkInvalid8; case "pt": return TxtPt.ValidateSettingsMarkInvalid8; case "ru": return TxtRu.ValidateSettingsMarkInvalid8; case "de": return TxtDe.ValidateSettingsMarkInvalid8; case "fr": return TxtFr.ValidateSettingsMarkInvalid8; default: return TxtCn.ValidateSettingsMarkInvalid8; } } }
        public static string TitleText { get { switch (Lang.Code) { case "tw": return TxtTw.TitleText; case "en": return TxtEn.TitleText; case "ja": return TxtJa.TitleText; case "ko": return TxtKo.TitleText; case "es": return TxtEs.TitleText; case "pt": return TxtPt.TitleText; case "ru": return TxtRu.TitleText; case "de": return TxtDe.TitleText; case "fr": return TxtFr.TitleText; default: return TxtCn.TitleText; } } }
        public static string SubText { get { switch (Lang.Code) { case "tw": return TxtTw.SubText; case "en": return TxtEn.SubText; case "ja": return TxtJa.SubText; case "ko": return TxtKo.SubText; case "es": return TxtEs.SubText; case "pt": return TxtPt.SubText; case "ru": return TxtRu.SubText; case "de": return TxtDe.SubText; case "fr": return TxtFr.SubText; default: return TxtCn.SubText; } } }
        public static string ServerText { get { switch (Lang.Code) { case "tw": return TxtTw.ServerText; case "en": return TxtEn.ServerText; case "ja": return TxtJa.ServerText; case "ko": return TxtKo.ServerText; case "es": return TxtEs.ServerText; case "pt": return TxtPt.ServerText; case "ru": return TxtRu.ServerText; case "de": return TxtDe.ServerText; case "fr": return TxtFr.ServerText; default: return TxtCn.ServerText; } } }
        public static string TunnelText { get { switch (Lang.Code) { case "tw": return TxtTw.TunnelText; case "en": return TxtEn.TunnelText; case "ja": return TxtJa.TunnelText; case "ko": return TxtKo.TunnelText; case "es": return TxtEs.TunnelText; case "pt": return TxtPt.TunnelText; case "ru": return TxtRu.TunnelText; case "de": return TxtDe.TunnelText; case "fr": return TxtFr.TunnelText; default: return TxtCn.TunnelText; } } }
        public static string AddrText { get { switch (Lang.Code) { case "tw": return TxtTw.AddrText; case "en": return TxtEn.AddrText; case "ja": return TxtJa.AddrText; case "ko": return TxtKo.AddrText; case "es": return TxtEs.AddrText; case "pt": return TxtPt.AddrText; case "ru": return TxtRu.AddrText; case "de": return TxtDe.AddrText; case "fr": return TxtFr.AddrText; default: return TxtCn.AddrText; } } }
        public static string SrvState { get { switch (Lang.Code) { case "tw": return TxtTw.SrvState; case "en": return TxtEn.SrvState; case "ja": return TxtJa.SrvState; case "ko": return TxtKo.SrvState; case "es": return TxtEs.SrvState; case "pt": return TxtPt.SrvState; case "ru": return TxtRu.SrvState; case "de": return TxtDe.SrvState; case "fr": return TxtFr.SrvState; default: return TxtCn.SrvState; } } }
        public static string MbText { get { switch (Lang.Code) { case "tw": return TxtTw.MbText; case "en": return TxtEn.MbText; case "ja": return TxtJa.MbText; case "ko": return TxtKo.MbText; case "es": return TxtEs.MbText; case "pt": return TxtPt.MbText; case "ru": return TxtRu.MbText; case "de": return TxtDe.MbText; case "fr": return TxtFr.MbText; default: return TxtCn.MbText; } } }
        public static string OnPaintHeader { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintHeader; case "en": return TxtEn.OnPaintHeader; case "ja": return TxtJa.OnPaintHeader; case "ko": return TxtKo.OnPaintHeader; case "es": return TxtEs.OnPaintHeader; case "pt": return TxtPt.OnPaintHeader; case "ru": return TxtRu.OnPaintHeader; case "de": return TxtDe.OnPaintHeader; case "fr": return TxtFr.OnPaintHeader; default: return TxtCn.OnPaintHeader; } } }
        public static string OnPaintRow { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintRow; case "en": return TxtEn.OnPaintRow; case "ja": return TxtJa.OnPaintRow; case "ko": return TxtKo.OnPaintRow; case "es": return TxtEs.OnPaintRow; case "pt": return TxtPt.OnPaintRow; case "ru": return TxtRu.OnPaintRow; case "de": return TxtDe.OnPaintRow; case "fr": return TxtFr.OnPaintRow; default: return TxtCn.OnPaintRow; } } }
        public static string OnPaintRow2 { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintRow2; case "en": return TxtEn.OnPaintRow2; case "ja": return TxtJa.OnPaintRow2; case "ko": return TxtKo.OnPaintRow2; case "es": return TxtEs.OnPaintRow2; case "pt": return TxtPt.OnPaintRow2; case "ru": return TxtRu.OnPaintRow2; case "de": return TxtDe.OnPaintRow2; case "fr": return TxtFr.OnPaintRow2; default: return TxtCn.OnPaintRow2; } } }
        public static string OnPaintRow3 { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintRow3; case "en": return TxtEn.OnPaintRow3; case "ja": return TxtJa.OnPaintRow3; case "ko": return TxtKo.OnPaintRow3; case "es": return TxtEs.OnPaintRow3; case "pt": return TxtPt.OnPaintRow3; case "ru": return TxtRu.OnPaintRow3; case "de": return TxtDe.OnPaintRow3; case "fr": return TxtFr.OnPaintRow3; default: return TxtCn.OnPaintRow3; } } }
        public static string OnPaintDrawText { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintDrawText; case "en": return TxtEn.OnPaintDrawText; case "ja": return TxtJa.OnPaintDrawText; case "ko": return TxtKo.OnPaintDrawText; case "es": return TxtEs.OnPaintDrawText; case "pt": return TxtPt.OnPaintDrawText; case "ru": return TxtRu.OnPaintDrawText; case "de": return TxtDe.OnPaintDrawText; case "fr": return TxtFr.OnPaintDrawText; default: return TxtCn.OnPaintDrawText; } } }
        public static string OnPaintRow4 { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintRow4; case "en": return TxtEn.OnPaintRow4; case "ja": return TxtJa.OnPaintRow4; case "ko": return TxtKo.OnPaintRow4; case "es": return TxtEs.OnPaintRow4; case "pt": return TxtPt.OnPaintRow4; case "ru": return TxtRu.OnPaintRow4; case "de": return TxtDe.OnPaintRow4; case "fr": return TxtFr.OnPaintRow4; default: return TxtCn.OnPaintRow4; } } }
        public static string OnPaintHeader2 { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintHeader2; case "en": return TxtEn.OnPaintHeader2; case "ja": return TxtJa.OnPaintHeader2; case "ko": return TxtKo.OnPaintHeader2; case "es": return TxtEs.OnPaintHeader2; case "pt": return TxtPt.OnPaintHeader2; case "ru": return TxtRu.OnPaintHeader2; case "de": return TxtDe.OnPaintHeader2; case "fr": return TxtFr.OnPaintHeader2; default: return TxtCn.OnPaintHeader2; } } }
        public static string OnPaintHeader3 { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintHeader3; case "en": return TxtEn.OnPaintHeader3; case "ja": return TxtJa.OnPaintHeader3; case "ko": return TxtKo.OnPaintHeader3; case "es": return TxtEs.OnPaintHeader3; case "pt": return TxtPt.OnPaintHeader3; case "ru": return TxtRu.OnPaintHeader3; case "de": return TxtDe.OnPaintHeader3; case "fr": return TxtFr.OnPaintHeader3; default: return TxtCn.OnPaintHeader3; } } }
        public static string OnPaintDrawText2 { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintDrawText2; case "en": return TxtEn.OnPaintDrawText2; case "ja": return TxtJa.OnPaintDrawText2; case "ko": return TxtKo.OnPaintDrawText2; case "es": return TxtEs.OnPaintDrawText2; case "pt": return TxtPt.OnPaintDrawText2; case "ru": return TxtRu.OnPaintDrawText2; case "de": return TxtDe.OnPaintDrawText2; case "fr": return TxtFr.OnPaintDrawText2; default: return TxtCn.OnPaintDrawText2; } } }
        public static string OnPaintDrawText3 { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintDrawText3; case "en": return TxtEn.OnPaintDrawText3; case "ja": return TxtJa.OnPaintDrawText3; case "ko": return TxtKo.OnPaintDrawText3; case "es": return TxtEs.OnPaintDrawText3; case "pt": return TxtPt.OnPaintDrawText3; case "ru": return TxtRu.OnPaintDrawText3; case "de": return TxtDe.OnPaintDrawText3; case "fr": return TxtFr.OnPaintDrawText3; default: return TxtCn.OnPaintDrawText3; } } }
        public static string OnPaintRow5 { get { switch (Lang.Code) { case "tw": return TxtTw.OnPaintRow5; case "en": return TxtEn.OnPaintRow5; case "ja": return TxtJa.OnPaintRow5; case "ko": return TxtKo.OnPaintRow5; case "es": return TxtEs.OnPaintRow5; case "pt": return TxtPt.OnPaintRow5; case "ru": return TxtRu.OnPaintRow5; case "de": return TxtDe.OnPaintRow5; case "fr": return TxtFr.OnPaintRow5; default: return TxtCn.OnPaintRow5; } } }
        public static string OnHandleCreatedIndexOf { get { switch (Lang.Code) { case "tw": return TxtTw.OnHandleCreatedIndexOf; case "en": return TxtEn.OnHandleCreatedIndexOf; case "ja": return TxtJa.OnHandleCreatedIndexOf; case "ko": return TxtKo.OnHandleCreatedIndexOf; case "es": return TxtEs.OnHandleCreatedIndexOf; case "pt": return TxtPt.OnHandleCreatedIndexOf; case "ru": return TxtRu.OnHandleCreatedIndexOf; case "de": return TxtDe.OnHandleCreatedIndexOf; case "fr": return TxtFr.OnHandleCreatedIndexOf; default: return TxtCn.OnHandleCreatedIndexOf; } } }
        public static string OnHandleCreatedStr { get { switch (Lang.Code) { case "tw": return TxtTw.OnHandleCreatedStr; case "en": return TxtEn.OnHandleCreatedStr; case "ja": return TxtJa.OnHandleCreatedStr; case "ko": return TxtKo.OnHandleCreatedStr; case "es": return TxtEs.OnHandleCreatedStr; case "pt": return TxtPt.OnHandleCreatedStr; case "ru": return TxtRu.OnHandleCreatedStr; case "de": return TxtDe.OnHandleCreatedStr; case "fr": return TxtFr.OnHandleCreatedStr; default: return TxtCn.OnHandleCreatedStr; } } }
        public static string ShowMsg { get { switch (Lang.Code) { case "tw": return TxtTw.ShowMsg; case "en": return TxtEn.ShowMsg; case "ja": return TxtJa.ShowMsg; case "ko": return TxtKo.ShowMsg; case "es": return TxtEs.ShowMsg; case "pt": return TxtPt.ShowMsg; case "ru": return TxtRu.ShowMsg; case "de": return TxtDe.ShowMsg; case "fr": return TxtFr.ShowMsg; default: return TxtCn.ShowMsg; } } }
        public static string PropsFormAddCombo { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddCombo; case "en": return TxtEn.PropsFormAddCombo; case "ja": return TxtJa.PropsFormAddCombo; case "ko": return TxtKo.PropsFormAddCombo; case "es": return TxtEs.PropsFormAddCombo; case "pt": return TxtPt.PropsFormAddCombo; case "ru": return TxtRu.PropsFormAddCombo; case "de": return TxtDe.PropsFormAddCombo; case "fr": return TxtFr.PropsFormAddCombo; default: return TxtCn.PropsFormAddCombo; } } }
        public static string PropsFormStr { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormStr; case "en": return TxtEn.PropsFormStr; case "ja": return TxtJa.PropsFormStr; case "ko": return TxtKo.PropsFormStr; case "es": return TxtEs.PropsFormStr; case "pt": return TxtPt.PropsFormStr; case "ru": return TxtRu.PropsFormStr; case "de": return TxtDe.PropsFormStr; case "fr": return TxtFr.PropsFormStr; default: return TxtCn.PropsFormStr; } } }
        public static string PropsFormStr2 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormStr2; case "en": return TxtEn.PropsFormStr2; case "ja": return TxtJa.PropsFormStr2; case "ko": return TxtKo.PropsFormStr2; case "es": return TxtEs.PropsFormStr2; case "pt": return TxtPt.PropsFormStr2; case "ru": return TxtRu.PropsFormStr2; case "de": return TxtDe.PropsFormStr2; case "fr": return TxtFr.PropsFormStr2; default: return TxtCn.PropsFormStr2; } } }
        public static string PropsFormStr3 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormStr3; case "en": return TxtEn.PropsFormStr3; case "ja": return TxtJa.PropsFormStr3; case "ko": return TxtKo.PropsFormStr3; case "es": return TxtEs.PropsFormStr3; case "pt": return TxtPt.PropsFormStr3; case "ru": return TxtRu.PropsFormStr3; case "de": return TxtDe.PropsFormStr3; case "fr": return TxtFr.PropsFormStr3; default: return TxtCn.PropsFormStr3; } } }
        public static string PropsFormStr4 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormStr4; case "en": return TxtEn.PropsFormStr4; case "ja": return TxtJa.PropsFormStr4; case "ko": return TxtKo.PropsFormStr4; case "es": return TxtEs.PropsFormStr4; case "pt": return TxtPt.PropsFormStr4; case "ru": return TxtRu.PropsFormStr4; case "de": return TxtDe.PropsFormStr4; case "fr": return TxtFr.PropsFormStr4; default: return TxtCn.PropsFormStr4; } } }
        public static string PropsFormAddCombo2 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddCombo2; case "en": return TxtEn.PropsFormAddCombo2; case "ja": return TxtJa.PropsFormAddCombo2; case "ko": return TxtKo.PropsFormAddCombo2; case "es": return TxtEs.PropsFormAddCombo2; case "pt": return TxtPt.PropsFormAddCombo2; case "ru": return TxtRu.PropsFormAddCombo2; case "de": return TxtDe.PropsFormAddCombo2; case "fr": return TxtFr.PropsFormAddCombo2; default: return TxtCn.PropsFormAddCombo2; } } }
        public static string PropsFormStr5 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormStr5; case "en": return TxtEn.PropsFormStr5; case "ja": return TxtJa.PropsFormStr5; case "ko": return TxtKo.PropsFormStr5; case "es": return TxtEs.PropsFormStr5; case "pt": return TxtPt.PropsFormStr5; case "ru": return TxtRu.PropsFormStr5; case "de": return TxtDe.PropsFormStr5; case "fr": return TxtFr.PropsFormStr5; default: return TxtCn.PropsFormStr5; } } }
        public static string PropsFormStr6 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormStr6; case "en": return TxtEn.PropsFormStr6; case "ja": return TxtJa.PropsFormStr6; case "ko": return TxtKo.PropsFormStr6; case "es": return TxtEs.PropsFormStr6; case "pt": return TxtPt.PropsFormStr6; case "ru": return TxtRu.PropsFormStr6; case "de": return TxtDe.PropsFormStr6; case "fr": return TxtFr.PropsFormStr6; default: return TxtCn.PropsFormStr6; } } }
        public static string PropsFormStr7 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormStr7; case "en": return TxtEn.PropsFormStr7; case "ja": return TxtJa.PropsFormStr7; case "ko": return TxtKo.PropsFormStr7; case "es": return TxtEs.PropsFormStr7; case "pt": return TxtPt.PropsFormStr7; case "ru": return TxtRu.PropsFormStr7; case "de": return TxtDe.PropsFormStr7; case "fr": return TxtFr.PropsFormStr7; default: return TxtCn.PropsFormStr7; } } }
        public static string PropsFormStr8 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormStr8; case "en": return TxtEn.PropsFormStr8; case "ja": return TxtJa.PropsFormStr8; case "ko": return TxtKo.PropsFormStr8; case "es": return TxtEs.PropsFormStr8; case "pt": return TxtPt.PropsFormStr8; case "ru": return TxtRu.PropsFormStr8; case "de": return TxtDe.PropsFormStr8; case "fr": return TxtFr.PropsFormStr8; default: return TxtCn.PropsFormStr8; } } }
        public static string PropsFormAddNum { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddNum; case "en": return TxtEn.PropsFormAddNum; case "ja": return TxtJa.PropsFormAddNum; case "ko": return TxtKo.PropsFormAddNum; case "es": return TxtEs.PropsFormAddNum; case "pt": return TxtPt.PropsFormAddNum; case "ru": return TxtRu.PropsFormAddNum; case "de": return TxtDe.PropsFormAddNum; case "fr": return TxtFr.PropsFormAddNum; default: return TxtCn.PropsFormAddNum; } } }
        public static string PropsFormAddNum2 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddNum2; case "en": return TxtEn.PropsFormAddNum2; case "ja": return TxtJa.PropsFormAddNum2; case "ko": return TxtKo.PropsFormAddNum2; case "es": return TxtEs.PropsFormAddNum2; case "pt": return TxtPt.PropsFormAddNum2; case "ru": return TxtRu.PropsFormAddNum2; case "de": return TxtDe.PropsFormAddNum2; case "fr": return TxtFr.PropsFormAddNum2; default: return TxtCn.PropsFormAddNum2; } } }
        public static string PropsFormAddNum3 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddNum3; case "en": return TxtEn.PropsFormAddNum3; case "ja": return TxtJa.PropsFormAddNum3; case "ko": return TxtKo.PropsFormAddNum3; case "es": return TxtEs.PropsFormAddNum3; case "pt": return TxtPt.PropsFormAddNum3; case "ru": return TxtRu.PropsFormAddNum3; case "de": return TxtDe.PropsFormAddNum3; case "fr": return TxtFr.PropsFormAddNum3; default: return TxtCn.PropsFormAddNum3; } } }
        public static string PropsFormAddNum4 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddNum4; case "en": return TxtEn.PropsFormAddNum4; case "ja": return TxtJa.PropsFormAddNum4; case "ko": return TxtKo.PropsFormAddNum4; case "es": return TxtEs.PropsFormAddNum4; case "pt": return TxtPt.PropsFormAddNum4; case "ru": return TxtRu.PropsFormAddNum4; case "de": return TxtDe.PropsFormAddNum4; case "fr": return TxtFr.PropsFormAddNum4; default: return TxtCn.PropsFormAddNum4; } } }
        public static string PropsFormAddText { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddText; case "en": return TxtEn.PropsFormAddText; case "ja": return TxtJa.PropsFormAddText; case "ko": return TxtKo.PropsFormAddText; case "es": return TxtEs.PropsFormAddText; case "pt": return TxtPt.PropsFormAddText; case "ru": return TxtRu.PropsFormAddText; case "de": return TxtDe.PropsFormAddText; case "fr": return TxtFr.PropsFormAddText; default: return TxtCn.PropsFormAddText; } } }
        public static string PropsFormAddCheck { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddCheck; case "en": return TxtEn.PropsFormAddCheck; case "ja": return TxtJa.PropsFormAddCheck; case "ko": return TxtKo.PropsFormAddCheck; case "es": return TxtEs.PropsFormAddCheck; case "pt": return TxtPt.PropsFormAddCheck; case "ru": return TxtRu.PropsFormAddCheck; case "de": return TxtDe.PropsFormAddCheck; case "fr": return TxtFr.PropsFormAddCheck; default: return TxtCn.PropsFormAddCheck; } } }
        public static string PropsFormAddCheck2 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddCheck2; case "en": return TxtEn.PropsFormAddCheck2; case "ja": return TxtJa.PropsFormAddCheck2; case "ko": return TxtKo.PropsFormAddCheck2; case "es": return TxtEs.PropsFormAddCheck2; case "pt": return TxtPt.PropsFormAddCheck2; case "ru": return TxtRu.PropsFormAddCheck2; case "de": return TxtDe.PropsFormAddCheck2; case "fr": return TxtFr.PropsFormAddCheck2; default: return TxtCn.PropsFormAddCheck2; } } }
        public static string PropsFormAddCheck3 { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormAddCheck3; case "en": return TxtEn.PropsFormAddCheck3; case "ja": return TxtJa.PropsFormAddCheck3; case "ko": return TxtKo.PropsFormAddCheck3; case "es": return TxtEs.PropsFormAddCheck3; case "pt": return TxtPt.PropsFormAddCheck3; case "ru": return TxtRu.PropsFormAddCheck3; case "de": return TxtDe.PropsFormAddCheck3; case "fr": return TxtFr.PropsFormAddCheck3; default: return TxtCn.PropsFormAddCheck3; } } }
        public static string LbOnCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LbOnCaption; case "en": return TxtEn.LbOnCaption; case "ja": return TxtJa.LbOnCaption; case "ko": return TxtKo.LbOnCaption; case "es": return TxtEs.LbOnCaption; case "pt": return TxtPt.LbOnCaption; case "ru": return TxtRu.LbOnCaption; case "de": return TxtDe.LbOnCaption; case "fr": return TxtFr.LbOnCaption; default: return TxtCn.LbOnCaption; } } }
        public static string PropsFormShow { get { switch (Lang.Code) { case "tw": return TxtTw.PropsFormShow; case "en": return TxtEn.PropsFormShow; case "ja": return TxtJa.PropsFormShow; case "ko": return TxtKo.PropsFormShow; case "es": return TxtEs.PropsFormShow; case "pt": return TxtPt.PropsFormShow; case "ru": return TxtRu.PropsFormShow; case "de": return TxtDe.PropsFormShow; case "fr": return TxtFr.PropsFormShow; default: return TxtCn.PropsFormShow; } } }
        public static string SavePropsShow { get { switch (Lang.Code) { case "tw": return TxtTw.SavePropsShow; case "en": return TxtEn.SavePropsShow; case "ja": return TxtJa.SavePropsShow; case "ko": return TxtKo.SavePropsShow; case "es": return TxtEs.SavePropsShow; case "pt": return TxtPt.SavePropsShow; case "ru": return TxtRu.SavePropsShow; case "de": return TxtDe.SavePropsShow; case "fr": return TxtFr.SavePropsShow; default: return TxtCn.SavePropsShow; } } }
        public static string SavePropsShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.SavePropsShow2; case "en": return TxtEn.SavePropsShow2; case "ja": return TxtJa.SavePropsShow2; case "ko": return TxtKo.SavePropsShow2; case "es": return TxtEs.SavePropsShow2; case "pt": return TxtPt.SavePropsShow2; case "ru": return TxtRu.SavePropsShow2; case "de": return TxtDe.SavePropsShow2; case "fr": return TxtFr.SavePropsShow2; default: return TxtCn.SavePropsShow2; } } }
        public static string SavePropsShow3 { get { switch (Lang.Code) { case "tw": return TxtTw.SavePropsShow3; case "en": return TxtEn.SavePropsShow3; case "ja": return TxtJa.SavePropsShow3; case "ko": return TxtKo.SavePropsShow3; case "es": return TxtEs.SavePropsShow3; case "pt": return TxtPt.SavePropsShow3; case "ru": return TxtRu.SavePropsShow3; case "de": return TxtDe.SavePropsShow3; case "fr": return TxtFr.SavePropsShow3; default: return TxtCn.SavePropsShow3; } } }
        public static string SavePropsShow4 { get { switch (Lang.Code) { case "tw": return TxtTw.SavePropsShow4; case "en": return TxtEn.SavePropsShow4; case "ja": return TxtJa.SavePropsShow4; case "ko": return TxtKo.SavePropsShow4; case "es": return TxtEs.SavePropsShow4; case "pt": return TxtPt.SavePropsShow4; case "ru": return TxtRu.SavePropsShow4; case "de": return TxtDe.SavePropsShow4; case "fr": return TxtFr.SavePropsShow4; default: return TxtCn.SavePropsShow4; } } }
        public static string SavePropsShow5 { get { switch (Lang.Code) { case "tw": return TxtTw.SavePropsShow5; case "en": return TxtEn.SavePropsShow5; case "ja": return TxtJa.SavePropsShow5; case "ko": return TxtKo.SavePropsShow5; case "es": return TxtEs.SavePropsShow5; case "pt": return TxtPt.SavePropsShow5; case "ru": return TxtRu.SavePropsShow5; case "de": return TxtDe.SavePropsShow5; case "fr": return TxtFr.SavePropsShow5; default: return TxtCn.SavePropsShow5; } } }
        public static string SavePropsShow6 { get { switch (Lang.Code) { case "tw": return TxtTw.SavePropsShow6; case "en": return TxtEn.SavePropsShow6; case "ja": return TxtJa.SavePropsShow6; case "ko": return TxtKo.SavePropsShow6; case "es": return TxtEs.SavePropsShow6; case "pt": return TxtPt.SavePropsShow6; case "ru": return TxtRu.SavePropsShow6; case "de": return TxtDe.SavePropsShow6; case "fr": return TxtFr.SavePropsShow6; default: return TxtCn.SavePropsShow6; } } }
        public static string PlayerFormStr { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormStr; case "en": return TxtEn.PlayerFormStr; case "ja": return TxtJa.PlayerFormStr; case "ko": return TxtKo.PlayerFormStr; case "es": return TxtEs.PlayerFormStr; case "pt": return TxtPt.PlayerFormStr; case "ru": return TxtRu.PlayerFormStr; case "de": return TxtDe.PlayerFormStr; case "fr": return TxtFr.PlayerFormStr; default: return TxtCn.PlayerFormStr; } } }
        public static string PlayerFormStr2 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormStr2; case "en": return TxtEn.PlayerFormStr2; case "ja": return TxtJa.PlayerFormStr2; case "ko": return TxtKo.PlayerFormStr2; case "es": return TxtEs.PlayerFormStr2; case "pt": return TxtPt.PlayerFormStr2; case "ru": return TxtRu.PlayerFormStr2; case "de": return TxtDe.PlayerFormStr2; case "fr": return TxtFr.PlayerFormStr2; default: return TxtCn.PlayerFormStr2; } } }
        public static string PlayerFormAddBtn { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormAddBtn; case "en": return TxtEn.PlayerFormAddBtn; case "ja": return TxtJa.PlayerFormAddBtn; case "ko": return TxtKo.PlayerFormAddBtn; case "es": return TxtEs.PlayerFormAddBtn; case "pt": return TxtPt.PlayerFormAddBtn; case "ru": return TxtRu.PlayerFormAddBtn; case "de": return TxtDe.PlayerFormAddBtn; case "fr": return TxtFr.PlayerFormAddBtn; default: return TxtCn.PlayerFormAddBtn; } } }
        public static string PlayerFormAddBtn2 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormAddBtn2; case "en": return TxtEn.PlayerFormAddBtn2; case "ja": return TxtJa.PlayerFormAddBtn2; case "ko": return TxtKo.PlayerFormAddBtn2; case "es": return TxtEs.PlayerFormAddBtn2; case "pt": return TxtPt.PlayerFormAddBtn2; case "ru": return TxtRu.PlayerFormAddBtn2; case "de": return TxtDe.PlayerFormAddBtn2; case "fr": return TxtFr.PlayerFormAddBtn2; default: return TxtCn.PlayerFormAddBtn2; } } }
        public static string PlayerFormConfirmDo { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormConfirmDo; case "en": return TxtEn.PlayerFormConfirmDo; case "ja": return TxtJa.PlayerFormConfirmDo; case "ko": return TxtKo.PlayerFormConfirmDo; case "es": return TxtEs.PlayerFormConfirmDo; case "pt": return TxtPt.PlayerFormConfirmDo; case "ru": return TxtRu.PlayerFormConfirmDo; case "de": return TxtDe.PlayerFormConfirmDo; case "fr": return TxtFr.PlayerFormConfirmDo; default: return TxtCn.PlayerFormConfirmDo; } } }
        public static string PlayerFormConfirmDo2 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormConfirmDo2; case "en": return TxtEn.PlayerFormConfirmDo2; case "ja": return TxtJa.PlayerFormConfirmDo2; case "ko": return TxtKo.PlayerFormConfirmDo2; case "es": return TxtEs.PlayerFormConfirmDo2; case "pt": return TxtPt.PlayerFormConfirmDo2; case "ru": return TxtRu.PlayerFormConfirmDo2; case "de": return TxtDe.PlayerFormConfirmDo2; case "fr": return TxtFr.PlayerFormConfirmDo2; default: return TxtCn.PlayerFormConfirmDo2; } } }
        public static string PlayerFormAddBtn3 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormAddBtn3; case "en": return TxtEn.PlayerFormAddBtn3; case "ja": return TxtJa.PlayerFormAddBtn3; case "ko": return TxtKo.PlayerFormAddBtn3; case "es": return TxtEs.PlayerFormAddBtn3; case "pt": return TxtPt.PlayerFormAddBtn3; case "ru": return TxtRu.PlayerFormAddBtn3; case "de": return TxtDe.PlayerFormAddBtn3; case "fr": return TxtFr.PlayerFormAddBtn3; default: return TxtCn.PlayerFormAddBtn3; } } }
        public static string PlayerFormAddBtn4 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormAddBtn4; case "en": return TxtEn.PlayerFormAddBtn4; case "ja": return TxtJa.PlayerFormAddBtn4; case "ko": return TxtKo.PlayerFormAddBtn4; case "es": return TxtEs.PlayerFormAddBtn4; case "pt": return TxtPt.PlayerFormAddBtn4; case "ru": return TxtRu.PlayerFormAddBtn4; case "de": return TxtDe.PlayerFormAddBtn4; case "fr": return TxtFr.PlayerFormAddBtn4; default: return TxtCn.PlayerFormAddBtn4; } } }
        public static string PlayerFormAddBtn5 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormAddBtn5; case "en": return TxtEn.PlayerFormAddBtn5; case "ja": return TxtJa.PlayerFormAddBtn5; case "ko": return TxtKo.PlayerFormAddBtn5; case "es": return TxtEs.PlayerFormAddBtn5; case "pt": return TxtPt.PlayerFormAddBtn5; case "ru": return TxtRu.PlayerFormAddBtn5; case "de": return TxtDe.PlayerFormAddBtn5; case "fr": return TxtFr.PlayerFormAddBtn5; default: return TxtCn.PlayerFormAddBtn5; } } }
        public static string PlayerFormConfirmDo3 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormConfirmDo3; case "en": return TxtEn.PlayerFormConfirmDo3; case "ja": return TxtJa.PlayerFormConfirmDo3; case "ko": return TxtKo.PlayerFormConfirmDo3; case "es": return TxtEs.PlayerFormConfirmDo3; case "pt": return TxtPt.PlayerFormConfirmDo3; case "ru": return TxtRu.PlayerFormConfirmDo3; case "de": return TxtDe.PlayerFormConfirmDo3; case "fr": return TxtFr.PlayerFormConfirmDo3; default: return TxtCn.PlayerFormConfirmDo3; } } }
        public static string PlayerFormConfirmDo4 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormConfirmDo4; case "en": return TxtEn.PlayerFormConfirmDo4; case "ja": return TxtJa.PlayerFormConfirmDo4; case "ko": return TxtKo.PlayerFormConfirmDo4; case "es": return TxtEs.PlayerFormConfirmDo4; case "pt": return TxtPt.PlayerFormConfirmDo4; case "ru": return TxtRu.PlayerFormConfirmDo4; case "de": return TxtDe.PlayerFormConfirmDo4; case "fr": return TxtFr.PlayerFormConfirmDo4; default: return TxtCn.PlayerFormConfirmDo4; } } }
        public static string PlayerFormAddBtn6 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormAddBtn6; case "en": return TxtEn.PlayerFormAddBtn6; case "ja": return TxtJa.PlayerFormAddBtn6; case "ko": return TxtKo.PlayerFormAddBtn6; case "es": return TxtEs.PlayerFormAddBtn6; case "pt": return TxtPt.PlayerFormAddBtn6; case "ru": return TxtRu.PlayerFormAddBtn6; case "de": return TxtDe.PlayerFormAddBtn6; case "fr": return TxtFr.PlayerFormAddBtn6; default: return TxtCn.PlayerFormAddBtn6; } } }
        public static string PlayerFormStr3 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormStr3; case "en": return TxtEn.PlayerFormStr3; case "ja": return TxtJa.PlayerFormStr3; case "ko": return TxtKo.PlayerFormStr3; case "es": return TxtEs.PlayerFormStr3; case "pt": return TxtPt.PlayerFormStr3; case "ru": return TxtRu.PlayerFormStr3; case "de": return TxtDe.PlayerFormStr3; case "fr": return TxtFr.PlayerFormStr3; default: return TxtCn.PlayerFormStr3; } } }
        public static string PlayerFormStr4 { get { switch (Lang.Code) { case "tw": return TxtTw.PlayerFormStr4; case "en": return TxtEn.PlayerFormStr4; case "ja": return TxtJa.PlayerFormStr4; case "ko": return TxtKo.PlayerFormStr4; case "es": return TxtEs.PlayerFormStr4; case "pt": return TxtPt.PlayerFormStr4; case "ru": return TxtRu.PlayerFormStr4; case "de": return TxtDe.PlayerFormStr4; case "fr": return TxtFr.PlayerFormStr4; default: return TxtCn.PlayerFormStr4; } } }
        public static string CloseText { get { switch (Lang.Code) { case "tw": return TxtTw.CloseText; case "en": return TxtEn.CloseText; case "ja": return TxtJa.CloseText; case "ko": return TxtKo.CloseText; case "es": return TxtEs.CloseText; case "pt": return TxtPt.CloseText; case "ru": return TxtRu.CloseText; case "de": return TxtDe.CloseText; case "fr": return TxtFr.CloseText; default: return TxtCn.CloseText; } } }
        public static string LbOpsCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LbOpsCaption; case "en": return TxtEn.LbOpsCaption; case "ja": return TxtJa.LbOpsCaption; case "ko": return TxtKo.LbOpsCaption; case "es": return TxtEs.LbOpsCaption; case "pt": return TxtPt.LbOpsCaption; case "ru": return TxtRu.LbOpsCaption; case "de": return TxtDe.LbOpsCaption; case "fr": return TxtFr.LbOpsCaption; default: return TxtCn.LbOpsCaption; } } }
        public static string RefreshListsStr { get { switch (Lang.Code) { case "tw": return TxtTw.RefreshListsStr; case "en": return TxtEn.RefreshListsStr; case "ja": return TxtJa.RefreshListsStr; case "ko": return TxtKo.RefreshListsStr; case "es": return TxtEs.RefreshListsStr; case "pt": return TxtPt.RefreshListsStr; case "ru": return TxtRu.RefreshListsStr; case "de": return TxtDe.RefreshListsStr; case "fr": return TxtFr.RefreshListsStr; default: return TxtCn.RefreshListsStr; } } }
        public static string LbBannedCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LbBannedCaption; case "en": return TxtEn.LbBannedCaption; case "ja": return TxtJa.LbBannedCaption; case "ko": return TxtKo.LbBannedCaption; case "es": return TxtEs.LbBannedCaption; case "pt": return TxtPt.LbBannedCaption; case "ru": return TxtRu.LbBannedCaption; case "de": return TxtDe.LbBannedCaption; case "fr": return TxtFr.LbBannedCaption; default: return TxtCn.LbBannedCaption; } } }
        public static string WizardFormInitChrome { get { switch (Lang.Code) { case "tw": return TxtTw.WizardFormInitChrome; case "en": return TxtEn.WizardFormInitChrome; case "ja": return TxtJa.WizardFormInitChrome; case "ko": return TxtKo.WizardFormInitChrome; case "es": return TxtEs.WizardFormInitChrome; case "pt": return TxtPt.WizardFormInitChrome; case "ru": return TxtRu.WizardFormInitChrome; case "de": return TxtDe.WizardFormInitChrome; case "fr": return TxtFr.WizardFormInitChrome; default: return TxtCn.WizardFormInitChrome; } } }
        public static string WizardFormAddLabel { get { switch (Lang.Code) { case "tw": return TxtTw.WizardFormAddLabel; case "en": return TxtEn.WizardFormAddLabel; case "ja": return TxtJa.WizardFormAddLabel; case "ko": return TxtKo.WizardFormAddLabel; case "es": return TxtEs.WizardFormAddLabel; case "pt": return TxtPt.WizardFormAddLabel; case "ru": return TxtRu.WizardFormAddLabel; case "de": return TxtDe.WizardFormAddLabel; case "fr": return TxtFr.WizardFormAddLabel; default: return TxtCn.WizardFormAddLabel; } } }
        public static string WizardFormAddHint { get { switch (Lang.Code) { case "tw": return TxtTw.WizardFormAddHint; case "en": return TxtEn.WizardFormAddHint; case "ja": return TxtJa.WizardFormAddHint; case "ko": return TxtKo.WizardFormAddHint; case "es": return TxtEs.WizardFormAddHint; case "pt": return TxtPt.WizardFormAddHint; case "ru": return TxtRu.WizardFormAddHint; case "de": return TxtDe.WizardFormAddHint; case "fr": return TxtFr.WizardFormAddHint; default: return TxtCn.WizardFormAddHint; } } }
        public static string WizardFormAddLabel2 { get { switch (Lang.Code) { case "tw": return TxtTw.WizardFormAddLabel2; case "en": return TxtEn.WizardFormAddLabel2; case "ja": return TxtJa.WizardFormAddLabel2; case "ko": return TxtKo.WizardFormAddLabel2; case "es": return TxtEs.WizardFormAddLabel2; case "pt": return TxtPt.WizardFormAddLabel2; case "ru": return TxtRu.WizardFormAddLabel2; case "de": return TxtDe.WizardFormAddLabel2; case "fr": return TxtFr.WizardFormAddLabel2; default: return TxtCn.WizardFormAddLabel2; } } }
        public static string WizardFormAddRadio { get { switch (Lang.Code) { case "tw": return TxtTw.WizardFormAddRadio; case "en": return TxtEn.WizardFormAddRadio; case "ja": return TxtJa.WizardFormAddRadio; case "ko": return TxtKo.WizardFormAddRadio; case "es": return TxtEs.WizardFormAddRadio; case "pt": return TxtPt.WizardFormAddRadio; case "ru": return TxtRu.WizardFormAddRadio; case "de": return TxtDe.WizardFormAddRadio; case "fr": return TxtFr.WizardFormAddRadio; default: return TxtCn.WizardFormAddRadio; } } }
        public static string WizardFormAddLabel3 { get { switch (Lang.Code) { case "tw": return TxtTw.WizardFormAddLabel3; case "en": return TxtEn.WizardFormAddLabel3; case "ja": return TxtJa.WizardFormAddLabel3; case "ko": return TxtKo.WizardFormAddLabel3; case "es": return TxtEs.WizardFormAddLabel3; case "pt": return TxtPt.WizardFormAddLabel3; case "ru": return TxtRu.WizardFormAddLabel3; case "de": return TxtDe.WizardFormAddLabel3; case "fr": return TxtFr.WizardFormAddLabel3; default: return TxtCn.WizardFormAddLabel3; } } }
        public static string BtnBrowseText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnBrowseText; case "en": return TxtEn.BtnBrowseText; case "ja": return TxtJa.BtnBrowseText; case "ko": return TxtKo.BtnBrowseText; case "es": return TxtEs.BtnBrowseText; case "pt": return TxtPt.BtnBrowseText; case "ru": return TxtRu.BtnBrowseText; case "de": return TxtDe.BtnBrowseText; case "fr": return TxtFr.BtnBrowseText; default: return TxtCn.BtnBrowseText; } } }
        public static string Description { get { switch (Lang.Code) { case "tw": return TxtTw.Description; case "en": return TxtEn.Description; case "ja": return TxtJa.Description; case "ko": return TxtKo.Description; case "es": return TxtEs.Description; case "pt": return TxtPt.Description; case "ru": return TxtRu.Description; case "de": return TxtDe.Description; case "fr": return TxtFr.Description; default: return TxtCn.Description; } } }
        public static string ChkEulaText { get { switch (Lang.Code) { case "tw": return TxtTw.ChkEulaText; case "en": return TxtEn.ChkEulaText; case "ja": return TxtJa.ChkEulaText; case "ko": return TxtKo.ChkEulaText; case "es": return TxtEs.ChkEulaText; case "pt": return TxtPt.ChkEulaText; case "ru": return TxtRu.ChkEulaText; case "de": return TxtDe.ChkEulaText; case "fr": return TxtFr.ChkEulaText; default: return TxtCn.ChkEulaText; } } }
        public static string LnkEulaText { get { switch (Lang.Code) { case "tw": return TxtTw.LnkEulaText; case "en": return TxtEn.LnkEulaText; case "ja": return TxtJa.LnkEulaText; case "ko": return TxtKo.LnkEulaText; case "es": return TxtEs.LnkEulaText; case "pt": return TxtPt.LnkEulaText; case "ru": return TxtRu.LnkEulaText; case "de": return TxtDe.LnkEulaText; case "fr": return TxtFr.LnkEulaText; default: return TxtCn.LnkEulaText; } } }
        public static string WizardFormAddHint2 { get { switch (Lang.Code) { case "tw": return TxtTw.WizardFormAddHint2; case "en": return TxtEn.WizardFormAddHint2; case "ja": return TxtJa.WizardFormAddHint2; case "ko": return TxtKo.WizardFormAddHint2; case "es": return TxtEs.WizardFormAddHint2; case "pt": return TxtPt.WizardFormAddHint2; case "ru": return TxtRu.WizardFormAddHint2; case "de": return TxtDe.WizardFormAddHint2; case "fr": return TxtFr.WizardFormAddHint2; default: return TxtCn.WizardFormAddHint2; } } }
        public static string LblStepCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LblStepCaption; case "en": return TxtEn.LblStepCaption; case "ja": return TxtJa.LblStepCaption; case "ko": return TxtKo.LblStepCaption; case "es": return TxtEs.LblStepCaption; case "pt": return TxtPt.LblStepCaption; case "ru": return TxtRu.LblStepCaption; case "de": return TxtDe.LblStepCaption; case "fr": return TxtFr.LblStepCaption; default: return TxtCn.LblStepCaption; } } }
        public static string BtnStartText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnStartText; case "en": return TxtEn.BtnStartText; case "ja": return TxtJa.BtnStartText; case "ko": return TxtKo.BtnStartText; case "es": return TxtEs.BtnStartText; case "pt": return TxtPt.BtnStartText; case "ru": return TxtRu.BtnStartText; case "de": return TxtDe.BtnStartText; case "fr": return TxtFr.BtnStartText; default: return TxtCn.BtnStartText; } } }
        public static string WizardFormWizLog { get { switch (Lang.Code) { case "tw": return TxtTw.WizardFormWizLog; case "en": return TxtEn.WizardFormWizLog; case "ja": return TxtJa.WizardFormWizLog; case "ko": return TxtKo.WizardFormWizLog; case "es": return TxtEs.WizardFormWizLog; case "pt": return TxtPt.WizardFormWizLog; case "ru": return TxtRu.WizardFormWizLog; case "de": return TxtDe.WizardFormWizLog; case "fr": return TxtFr.WizardFormWizLog; default: return TxtCn.WizardFormWizLog; } } }
        public static string StartInstallShow { get { switch (Lang.Code) { case "tw": return TxtTw.StartInstallShow; case "en": return TxtEn.StartInstallShow; case "ja": return TxtJa.StartInstallShow; case "ko": return TxtKo.StartInstallShow; case "es": return TxtEs.StartInstallShow; case "pt": return TxtPt.StartInstallShow; case "ru": return TxtRu.StartInstallShow; case "de": return TxtDe.StartInstallShow; case "fr": return TxtFr.StartInstallShow; default: return TxtCn.StartInstallShow; } } }
        public static string StartInstallShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.StartInstallShow2; case "en": return TxtEn.StartInstallShow2; case "ja": return TxtJa.StartInstallShow2; case "ko": return TxtKo.StartInstallShow2; case "es": return TxtEs.StartInstallShow2; case "pt": return TxtPt.StartInstallShow2; case "ru": return TxtRu.StartInstallShow2; case "de": return TxtDe.StartInstallShow2; case "fr": return TxtFr.StartInstallShow2; default: return TxtCn.StartInstallShow2; } } }
        public static string StartInstallShow3 { get { switch (Lang.Code) { case "tw": return TxtTw.StartInstallShow3; case "en": return TxtEn.StartInstallShow3; case "ja": return TxtJa.StartInstallShow3; case "ko": return TxtKo.StartInstallShow3; case "es": return TxtEs.StartInstallShow3; case "pt": return TxtPt.StartInstallShow3; case "ru": return TxtRu.StartInstallShow3; case "de": return TxtDe.StartInstallShow3; case "fr": return TxtFr.StartInstallShow3; default: return TxtCn.StartInstallShow3; } } }
        public static string StartInstallShow4 { get { switch (Lang.Code) { case "tw": return TxtTw.StartInstallShow4; case "en": return TxtEn.StartInstallShow4; case "ja": return TxtJa.StartInstallShow4; case "ko": return TxtKo.StartInstallShow4; case "es": return TxtEs.StartInstallShow4; case "pt": return TxtPt.StartInstallShow4; case "ru": return TxtRu.StartInstallShow4; case "de": return TxtDe.StartInstallShow4; case "fr": return TxtFr.StartInstallShow4; default: return TxtCn.StartInstallShow4; } } }
        public static string StartInstallShow5 { get { switch (Lang.Code) { case "tw": return TxtTw.StartInstallShow5; case "en": return TxtEn.StartInstallShow5; case "ja": return TxtJa.StartInstallShow5; case "ko": return TxtKo.StartInstallShow5; case "es": return TxtEs.StartInstallShow5; case "pt": return TxtPt.StartInstallShow5; case "ru": return TxtRu.StartInstallShow5; case "de": return TxtDe.StartInstallShow5; case "fr": return TxtFr.StartInstallShow5; default: return TxtCn.StartInstallShow5; } } }
        public static string StartInstallWizLog { get { switch (Lang.Code) { case "tw": return TxtTw.StartInstallWizLog; case "en": return TxtEn.StartInstallWizLog; case "ja": return TxtJa.StartInstallWizLog; case "ko": return TxtKo.StartInstallWizLog; case "es": return TxtEs.StartInstallWizLog; case "pt": return TxtPt.StartInstallWizLog; case "ru": return TxtRu.StartInstallWizLog; case "de": return TxtDe.StartInstallWizLog; case "fr": return TxtFr.StartInstallWizLog; default: return TxtCn.StartInstallWizLog; } } }
        public static string StartInstallPart { get { switch (Lang.Code) { case "tw": return TxtTw.StartInstallPart; case "en": return TxtEn.StartInstallPart; case "ja": return TxtJa.StartInstallPart; case "ko": return TxtKo.StartInstallPart; case "es": return TxtEs.StartInstallPart; case "pt": return TxtPt.StartInstallPart; case "ru": return TxtRu.StartInstallPart; case "de": return TxtDe.StartInstallPart; case "fr": return TxtFr.StartInstallPart; default: return TxtCn.StartInstallPart; } } }
        public static string RunFail { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail; case "en": return TxtEn.RunFail; case "ja": return TxtJa.RunFail; case "ko": return TxtKo.RunFail; case "es": return TxtEs.RunFail; case "pt": return TxtPt.RunFail; case "ru": return TxtRu.RunFail; case "de": return TxtDe.RunFail; case "fr": return TxtFr.RunFail; default: return TxtCn.RunFail; } } }
        public static string RunFail2 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail2; case "en": return TxtEn.RunFail2; case "ja": return TxtJa.RunFail2; case "ko": return TxtKo.RunFail2; case "es": return TxtEs.RunFail2; case "pt": return TxtPt.RunFail2; case "ru": return TxtRu.RunFail2; case "de": return TxtDe.RunFail2; case "fr": return TxtFr.RunFail2; default: return TxtCn.RunFail2; } } }
        public static string RunWizLog { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog; case "en": return TxtEn.RunWizLog; case "ja": return TxtJa.RunWizLog; case "ko": return TxtKo.RunWizLog; case "es": return TxtEs.RunWizLog; case "pt": return TxtPt.RunWizLog; case "ru": return TxtRu.RunWizLog; case "de": return TxtDe.RunWizLog; case "fr": return TxtFr.RunWizLog; default: return TxtCn.RunWizLog; } } }
        public static string RunWizStep { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizStep; case "en": return TxtEn.RunWizStep; case "ja": return TxtJa.RunWizStep; case "ko": return TxtKo.RunWizStep; case "es": return TxtEs.RunWizStep; case "pt": return TxtPt.RunWizStep; case "ru": return TxtRu.RunWizStep; case "de": return TxtDe.RunWizStep; case "fr": return TxtFr.RunWizStep; default: return TxtCn.RunWizStep; } } }
        public static string RunWizLog2 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog2; case "en": return TxtEn.RunWizLog2; case "ja": return TxtJa.RunWizLog2; case "ko": return TxtKo.RunWizLog2; case "es": return TxtEs.RunWizLog2; case "pt": return TxtPt.RunWizLog2; case "ru": return TxtRu.RunWizLog2; case "de": return TxtDe.RunWizLog2; case "fr": return TxtFr.RunWizLog2; default: return TxtCn.RunWizLog2; } } }
        public static string RunWizLog3 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog3; case "en": return TxtEn.RunWizLog3; case "ja": return TxtJa.RunWizLog3; case "ko": return TxtKo.RunWizLog3; case "es": return TxtEs.RunWizLog3; case "pt": return TxtPt.RunWizLog3; case "ru": return TxtRu.RunWizLog3; case "de": return TxtDe.RunWizLog3; case "fr": return TxtFr.RunWizLog3; default: return TxtCn.RunWizLog3; } } }
        public static string RunWizLog4 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog4; case "en": return TxtEn.RunWizLog4; case "ja": return TxtJa.RunWizLog4; case "ko": return TxtKo.RunWizLog4; case "es": return TxtEs.RunWizLog4; case "pt": return TxtPt.RunWizLog4; case "ru": return TxtRu.RunWizLog4; case "de": return TxtDe.RunWizLog4; case "fr": return TxtFr.RunWizLog4; default: return TxtCn.RunWizLog4; } } }
        public static string RunFail3 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail3; case "en": return TxtEn.RunFail3; case "ja": return TxtJa.RunFail3; case "ko": return TxtKo.RunFail3; case "es": return TxtEs.RunFail3; case "pt": return TxtPt.RunFail3; case "ru": return TxtRu.RunFail3; case "de": return TxtDe.RunFail3; case "fr": return TxtFr.RunFail3; default: return TxtCn.RunFail3; } } }
        public static string RunFail4 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail4; case "en": return TxtEn.RunFail4; case "ja": return TxtJa.RunFail4; case "ko": return TxtKo.RunFail4; case "es": return TxtEs.RunFail4; case "pt": return TxtPt.RunFail4; case "ru": return TxtRu.RunFail4; case "de": return TxtDe.RunFail4; case "fr": return TxtFr.RunFail4; default: return TxtCn.RunFail4; } } }
        public static string RunFail5 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail5; case "en": return TxtEn.RunFail5; case "ja": return TxtJa.RunFail5; case "ko": return TxtKo.RunFail5; case "es": return TxtEs.RunFail5; case "pt": return TxtPt.RunFail5; case "ru": return TxtRu.RunFail5; case "de": return TxtDe.RunFail5; case "fr": return TxtFr.RunFail5; default: return TxtCn.RunFail5; } } }
        public static string RunWizLog5 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog5; case "en": return TxtEn.RunWizLog5; case "ja": return TxtJa.RunWizLog5; case "ko": return TxtKo.RunWizLog5; case "es": return TxtEs.RunWizLog5; case "pt": return TxtPt.RunWizLog5; case "ru": return TxtRu.RunWizLog5; case "de": return TxtDe.RunWizLog5; case "fr": return TxtFr.RunWizLog5; default: return TxtCn.RunWizLog5; } } }
        public static string RunStr { get { switch (Lang.Code) { case "tw": return TxtTw.RunStr; case "en": return TxtEn.RunStr; case "ja": return TxtJa.RunStr; case "ko": return TxtKo.RunStr; case "es": return TxtEs.RunStr; case "pt": return TxtPt.RunStr; case "ru": return TxtRu.RunStr; case "de": return TxtDe.RunStr; case "fr": return TxtFr.RunStr; default: return TxtCn.RunStr; } } }
        public static string RunPart { get { switch (Lang.Code) { case "tw": return TxtTw.RunPart; case "en": return TxtEn.RunPart; case "ja": return TxtJa.RunPart; case "ko": return TxtKo.RunPart; case "es": return TxtEs.RunPart; case "pt": return TxtPt.RunPart; case "ru": return TxtRu.RunPart; case "de": return TxtDe.RunPart; case "fr": return TxtFr.RunPart; default: return TxtCn.RunPart; } } }
        public static string RunWizLog6 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog6; case "en": return TxtEn.RunWizLog6; case "ja": return TxtJa.RunWizLog6; case "ko": return TxtKo.RunWizLog6; case "es": return TxtEs.RunWizLog6; case "pt": return TxtPt.RunWizLog6; case "ru": return TxtRu.RunWizLog6; case "de": return TxtDe.RunWizLog6; case "fr": return TxtFr.RunWizLog6; default: return TxtCn.RunWizLog6; } } }
        public static string RunWizLog7 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog7; case "en": return TxtEn.RunWizLog7; case "ja": return TxtJa.RunWizLog7; case "ko": return TxtKo.RunWizLog7; case "es": return TxtEs.RunWizLog7; case "pt": return TxtPt.RunWizLog7; case "ru": return TxtRu.RunWizLog7; case "de": return TxtDe.RunWizLog7; case "fr": return TxtFr.RunWizLog7; default: return TxtCn.RunWizLog7; } } }
        public static string RunFail6 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail6; case "en": return TxtEn.RunFail6; case "ja": return TxtJa.RunFail6; case "ko": return TxtKo.RunFail6; case "es": return TxtEs.RunFail6; case "pt": return TxtPt.RunFail6; case "ru": return TxtRu.RunFail6; case "de": return TxtDe.RunFail6; case "fr": return TxtFr.RunFail6; default: return TxtCn.RunFail6; } } }
        public static string RunWizLog8 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog8; case "en": return TxtEn.RunWizLog8; case "ja": return TxtJa.RunWizLog8; case "ko": return TxtKo.RunWizLog8; case "es": return TxtEs.RunWizLog8; case "pt": return TxtPt.RunWizLog8; case "ru": return TxtRu.RunWizLog8; case "de": return TxtDe.RunWizLog8; case "fr": return TxtFr.RunWizLog8; default: return TxtCn.RunWizLog8; } } }
        public static string RunFail7 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail7; case "en": return TxtEn.RunFail7; case "ja": return TxtJa.RunFail7; case "ko": return TxtKo.RunFail7; case "es": return TxtEs.RunFail7; case "pt": return TxtPt.RunFail7; case "ru": return TxtRu.RunFail7; case "de": return TxtDe.RunFail7; case "fr": return TxtFr.RunFail7; default: return TxtCn.RunFail7; } } }
        public static string RunFail8 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail8; case "en": return TxtEn.RunFail8; case "ja": return TxtJa.RunFail8; case "ko": return TxtKo.RunFail8; case "es": return TxtEs.RunFail8; case "pt": return TxtPt.RunFail8; case "ru": return TxtRu.RunFail8; case "de": return TxtDe.RunFail8; case "fr": return TxtFr.RunFail8; default: return TxtCn.RunFail8; } } }
        public static string RunFail9 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail9; case "en": return TxtEn.RunFail9; case "ja": return TxtJa.RunFail9; case "ko": return TxtKo.RunFail9; case "es": return TxtEs.RunFail9; case "pt": return TxtPt.RunFail9; case "ru": return TxtRu.RunFail9; case "de": return TxtDe.RunFail9; case "fr": return TxtFr.RunFail9; default: return TxtCn.RunFail9; } } }
        public static string RunWizLog9 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog9; case "en": return TxtEn.RunWizLog9; case "ja": return TxtJa.RunWizLog9; case "ko": return TxtKo.RunWizLog9; case "es": return TxtEs.RunWizLog9; case "pt": return TxtPt.RunWizLog9; case "ru": return TxtRu.RunWizLog9; case "de": return TxtDe.RunWizLog9; case "fr": return TxtFr.RunWizLog9; default: return TxtCn.RunWizLog9; } } }
        public static string RunWizLog10 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog10; case "en": return TxtEn.RunWizLog10; case "ja": return TxtJa.RunWizLog10; case "ko": return TxtKo.RunWizLog10; case "es": return TxtEs.RunWizLog10; case "pt": return TxtPt.RunWizLog10; case "ru": return TxtRu.RunWizLog10; case "de": return TxtDe.RunWizLog10; case "fr": return TxtFr.RunWizLog10; default: return TxtCn.RunWizLog10; } } }
        public static string RunWizLog11 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog11; case "en": return TxtEn.RunWizLog11; case "ja": return TxtJa.RunWizLog11; case "ko": return TxtKo.RunWizLog11; case "es": return TxtEs.RunWizLog11; case "pt": return TxtPt.RunWizLog11; case "ru": return TxtRu.RunWizLog11; case "de": return TxtDe.RunWizLog11; case "fr": return TxtFr.RunWizLog11; default: return TxtCn.RunWizLog11; } } }
        public static string RunFail10 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail10; case "en": return TxtEn.RunFail10; case "ja": return TxtJa.RunFail10; case "ko": return TxtKo.RunFail10; case "es": return TxtEs.RunFail10; case "pt": return TxtPt.RunFail10; case "ru": return TxtRu.RunFail10; case "de": return TxtDe.RunFail10; case "fr": return TxtFr.RunFail10; default: return TxtCn.RunFail10; } } }
        public static string RunFail11 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail11; case "en": return TxtEn.RunFail11; case "ja": return TxtJa.RunFail11; case "ko": return TxtKo.RunFail11; case "es": return TxtEs.RunFail11; case "pt": return TxtPt.RunFail11; case "ru": return TxtRu.RunFail11; case "de": return TxtDe.RunFail11; case "fr": return TxtFr.RunFail11; default: return TxtCn.RunFail11; } } }
        public static string RunFail12 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail12; case "en": return TxtEn.RunFail12; case "ja": return TxtJa.RunFail12; case "ko": return TxtKo.RunFail12; case "es": return TxtEs.RunFail12; case "pt": return TxtPt.RunFail12; case "ru": return TxtRu.RunFail12; case "de": return TxtDe.RunFail12; case "fr": return TxtFr.RunFail12; default: return TxtCn.RunFail12; } } }
        public static string RunFail13 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail13; case "en": return TxtEn.RunFail13; case "ja": return TxtJa.RunFail13; case "ko": return TxtKo.RunFail13; case "es": return TxtEs.RunFail13; case "pt": return TxtPt.RunFail13; case "ru": return TxtRu.RunFail13; case "de": return TxtDe.RunFail13; case "fr": return TxtFr.RunFail13; default: return TxtCn.RunFail13; } } }
        public static string RunFail14 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail14; case "en": return TxtEn.RunFail14; case "ja": return TxtJa.RunFail14; case "ko": return TxtKo.RunFail14; case "es": return TxtEs.RunFail14; case "pt": return TxtPt.RunFail14; case "ru": return TxtRu.RunFail14; case "de": return TxtDe.RunFail14; case "fr": return TxtFr.RunFail14; default: return TxtCn.RunFail14; } } }
        public static string RunWizLog12 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog12; case "en": return TxtEn.RunWizLog12; case "ja": return TxtJa.RunWizLog12; case "ko": return TxtKo.RunWizLog12; case "es": return TxtEs.RunWizLog12; case "pt": return TxtPt.RunWizLog12; case "ru": return TxtRu.RunWizLog12; case "de": return TxtDe.RunWizLog12; case "fr": return TxtFr.RunWizLog12; default: return TxtCn.RunWizLog12; } } }
        public static string RunWizLog13 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog13; case "en": return TxtEn.RunWizLog13; case "ja": return TxtJa.RunWizLog13; case "ko": return TxtKo.RunWizLog13; case "es": return TxtEs.RunWizLog13; case "pt": return TxtPt.RunWizLog13; case "ru": return TxtRu.RunWizLog13; case "de": return TxtDe.RunWizLog13; case "fr": return TxtFr.RunWizLog13; default: return TxtCn.RunWizLog13; } } }
        public static string RunWizStep2 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizStep2; case "en": return TxtEn.RunWizStep2; case "ja": return TxtJa.RunWizStep2; case "ko": return TxtKo.RunWizStep2; case "es": return TxtEs.RunWizStep2; case "pt": return TxtPt.RunWizStep2; case "ru": return TxtRu.RunWizStep2; case "de": return TxtDe.RunWizStep2; case "fr": return TxtFr.RunWizStep2; default: return TxtCn.RunWizStep2; } } }
        public static string RunFail15 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail15; case "en": return TxtEn.RunFail15; case "ja": return TxtJa.RunFail15; case "ko": return TxtKo.RunFail15; case "es": return TxtEs.RunFail15; case "pt": return TxtPt.RunFail15; case "ru": return TxtRu.RunFail15; case "de": return TxtDe.RunFail15; case "fr": return TxtFr.RunFail15; default: return TxtCn.RunFail15; } } }
        public static string RunFail16 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail16; case "en": return TxtEn.RunFail16; case "ja": return TxtJa.RunFail16; case "ko": return TxtKo.RunFail16; case "es": return TxtEs.RunFail16; case "pt": return TxtPt.RunFail16; case "ru": return TxtRu.RunFail16; case "de": return TxtDe.RunFail16; case "fr": return TxtFr.RunFail16; default: return TxtCn.RunFail16; } } }
        public static string RunFail17 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail17; case "en": return TxtEn.RunFail17; case "ja": return TxtJa.RunFail17; case "ko": return TxtKo.RunFail17; case "es": return TxtEs.RunFail17; case "pt": return TxtPt.RunFail17; case "ru": return TxtRu.RunFail17; case "de": return TxtDe.RunFail17; case "fr": return TxtFr.RunFail17; default: return TxtCn.RunFail17; } } }
        public static string RunFail18 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail18; case "en": return TxtEn.RunFail18; case "ja": return TxtJa.RunFail18; case "ko": return TxtKo.RunFail18; case "es": return TxtEs.RunFail18; case "pt": return TxtPt.RunFail18; case "ru": return TxtRu.RunFail18; case "de": return TxtDe.RunFail18; case "fr": return TxtFr.RunFail18; default: return TxtCn.RunFail18; } } }
        public static string RunFail19 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail19; case "en": return TxtEn.RunFail19; case "ja": return TxtJa.RunFail19; case "ko": return TxtKo.RunFail19; case "es": return TxtEs.RunFail19; case "pt": return TxtPt.RunFail19; case "ru": return TxtRu.RunFail19; case "de": return TxtDe.RunFail19; case "fr": return TxtFr.RunFail19; default: return TxtCn.RunFail19; } } }
        public static string RunFail20 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail20; case "en": return TxtEn.RunFail20; case "ja": return TxtJa.RunFail20; case "ko": return TxtKo.RunFail20; case "es": return TxtEs.RunFail20; case "pt": return TxtPt.RunFail20; case "ru": return TxtRu.RunFail20; case "de": return TxtDe.RunFail20; case "fr": return TxtFr.RunFail20; default: return TxtCn.RunFail20; } } }
        public static string RunWizLog14 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog14; case "en": return TxtEn.RunWizLog14; case "ja": return TxtJa.RunWizLog14; case "ko": return TxtKo.RunWizLog14; case "es": return TxtEs.RunWizLog14; case "pt": return TxtPt.RunWizLog14; case "ru": return TxtRu.RunWizLog14; case "de": return TxtDe.RunWizLog14; case "fr": return TxtFr.RunWizLog14; default: return TxtCn.RunWizLog14; } } }
        public static string RunDownloadWithProgress { get { switch (Lang.Code) { case "tw": return TxtTw.RunDownloadWithProgress; case "en": return TxtEn.RunDownloadWithProgress; case "ja": return TxtJa.RunDownloadWithProgress; case "ko": return TxtKo.RunDownloadWithProgress; case "es": return TxtEs.RunDownloadWithProgress; case "pt": return TxtPt.RunDownloadWithProgress; case "ru": return TxtRu.RunDownloadWithProgress; case "de": return TxtDe.RunDownloadWithProgress; case "fr": return TxtFr.RunDownloadWithProgress; default: return TxtCn.RunDownloadWithProgress; } } }
        public static string RunFail21 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail21; case "en": return TxtEn.RunFail21; case "ja": return TxtJa.RunFail21; case "ko": return TxtKo.RunFail21; case "es": return TxtEs.RunFail21; case "pt": return TxtPt.RunFail21; case "ru": return TxtRu.RunFail21; case "de": return TxtDe.RunFail21; case "fr": return TxtFr.RunFail21; default: return TxtCn.RunFail21; } } }
        public static string RunWizLog15 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog15; case "en": return TxtEn.RunWizLog15; case "ja": return TxtJa.RunWizLog15; case "ko": return TxtKo.RunWizLog15; case "es": return TxtEs.RunWizLog15; case "pt": return TxtPt.RunWizLog15; case "ru": return TxtRu.RunWizLog15; case "de": return TxtDe.RunWizLog15; case "fr": return TxtFr.RunWizLog15; default: return TxtCn.RunWizLog15; } } }
        public static string RunFail22 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail22; case "en": return TxtEn.RunFail22; case "ja": return TxtJa.RunFail22; case "ko": return TxtKo.RunFail22; case "es": return TxtEs.RunFail22; case "pt": return TxtPt.RunFail22; case "ru": return TxtRu.RunFail22; case "de": return TxtDe.RunFail22; case "fr": return TxtFr.RunFail22; default: return TxtCn.RunFail22; } } }
        public static string RunWizLog16 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog16; case "en": return TxtEn.RunWizLog16; case "ja": return TxtJa.RunWizLog16; case "ko": return TxtKo.RunWizLog16; case "es": return TxtEs.RunWizLog16; case "pt": return TxtPt.RunWizLog16; case "ru": return TxtRu.RunWizLog16; case "de": return TxtDe.RunWizLog16; case "fr": return TxtFr.RunWizLog16; default: return TxtCn.RunWizLog16; } } }
        public static string RunFail23 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail23; case "en": return TxtEn.RunFail23; case "ja": return TxtJa.RunFail23; case "ko": return TxtKo.RunFail23; case "es": return TxtEs.RunFail23; case "pt": return TxtPt.RunFail23; case "ru": return TxtRu.RunFail23; case "de": return TxtDe.RunFail23; case "fr": return TxtFr.RunFail23; default: return TxtCn.RunFail23; } } }
        public static string RunFail24 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail24; case "en": return TxtEn.RunFail24; case "ja": return TxtJa.RunFail24; case "ko": return TxtKo.RunFail24; case "es": return TxtEs.RunFail24; case "pt": return TxtPt.RunFail24; case "ru": return TxtRu.RunFail24; case "de": return TxtDe.RunFail24; case "fr": return TxtFr.RunFail24; default: return TxtCn.RunFail24; } } }
        public static string RunFail25 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail25; case "en": return TxtEn.RunFail25; case "ja": return TxtJa.RunFail25; case "ko": return TxtKo.RunFail25; case "es": return TxtEs.RunFail25; case "pt": return TxtPt.RunFail25; case "ru": return TxtRu.RunFail25; case "de": return TxtDe.RunFail25; case "fr": return TxtFr.RunFail25; default: return TxtCn.RunFail25; } } }
        public static string RunFail26 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail26; case "en": return TxtEn.RunFail26; case "ja": return TxtJa.RunFail26; case "ko": return TxtKo.RunFail26; case "es": return TxtEs.RunFail26; case "pt": return TxtPt.RunFail26; case "ru": return TxtRu.RunFail26; case "de": return TxtDe.RunFail26; case "fr": return TxtFr.RunFail26; default: return TxtCn.RunFail26; } } }
        public static string RunFail27 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail27; case "en": return TxtEn.RunFail27; case "ja": return TxtJa.RunFail27; case "ko": return TxtKo.RunFail27; case "es": return TxtEs.RunFail27; case "pt": return TxtPt.RunFail27; case "ru": return TxtRu.RunFail27; case "de": return TxtDe.RunFail27; case "fr": return TxtFr.RunFail27; default: return TxtCn.RunFail27; } } }
        public static string RunWizLog17 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog17; case "en": return TxtEn.RunWizLog17; case "ja": return TxtJa.RunWizLog17; case "ko": return TxtKo.RunWizLog17; case "es": return TxtEs.RunWizLog17; case "pt": return TxtPt.RunWizLog17; case "ru": return TxtRu.RunWizLog17; case "de": return TxtDe.RunWizLog17; case "fr": return TxtFr.RunWizLog17; default: return TxtCn.RunWizLog17; } } }
        public static string RunWizLog18 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog18; case "en": return TxtEn.RunWizLog18; case "ja": return TxtJa.RunWizLog18; case "ko": return TxtKo.RunWizLog18; case "es": return TxtEs.RunWizLog18; case "pt": return TxtPt.RunWizLog18; case "ru": return TxtRu.RunWizLog18; case "de": return TxtDe.RunWizLog18; case "fr": return TxtFr.RunWizLog18; default: return TxtCn.RunWizLog18; } } }
        public static string RunWizLog19 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog19; case "en": return TxtEn.RunWizLog19; case "ja": return TxtJa.RunWizLog19; case "ko": return TxtKo.RunWizLog19; case "es": return TxtEs.RunWizLog19; case "pt": return TxtPt.RunWizLog19; case "ru": return TxtRu.RunWizLog19; case "de": return TxtDe.RunWizLog19; case "fr": return TxtFr.RunWizLog19; default: return TxtCn.RunWizLog19; } } }
        public static string RunWizLog20 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog20; case "en": return TxtEn.RunWizLog20; case "ja": return TxtJa.RunWizLog20; case "ko": return TxtKo.RunWizLog20; case "es": return TxtEs.RunWizLog20; case "pt": return TxtPt.RunWizLog20; case "ru": return TxtRu.RunWizLog20; case "de": return TxtDe.RunWizLog20; case "fr": return TxtFr.RunWizLog20; default: return TxtCn.RunWizLog20; } } }
        public static string RunDownloadWithProgress2 { get { switch (Lang.Code) { case "tw": return TxtTw.RunDownloadWithProgress2; case "en": return TxtEn.RunDownloadWithProgress2; case "ja": return TxtJa.RunDownloadWithProgress2; case "ko": return TxtKo.RunDownloadWithProgress2; case "es": return TxtEs.RunDownloadWithProgress2; case "pt": return TxtPt.RunDownloadWithProgress2; case "ru": return TxtRu.RunDownloadWithProgress2; case "de": return TxtDe.RunDownloadWithProgress2; case "fr": return TxtFr.RunDownloadWithProgress2; default: return TxtCn.RunDownloadWithProgress2; } } }
        public static string RunFail28 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail28; case "en": return TxtEn.RunFail28; case "ja": return TxtJa.RunFail28; case "ko": return TxtKo.RunFail28; case "es": return TxtEs.RunFail28; case "pt": return TxtPt.RunFail28; case "ru": return TxtRu.RunFail28; case "de": return TxtDe.RunFail28; case "fr": return TxtFr.RunFail28; default: return TxtCn.RunFail28; } } }
        public static string RunWizLog21 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog21; case "en": return TxtEn.RunWizLog21; case "ja": return TxtJa.RunWizLog21; case "ko": return TxtKo.RunWizLog21; case "es": return TxtEs.RunWizLog21; case "pt": return TxtPt.RunWizLog21; case "ru": return TxtRu.RunWizLog21; case "de": return TxtDe.RunWizLog21; case "fr": return TxtFr.RunWizLog21; default: return TxtCn.RunWizLog21; } } }
        public static string RunFail29 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail29; case "en": return TxtEn.RunFail29; case "ja": return TxtJa.RunFail29; case "ko": return TxtKo.RunFail29; case "es": return TxtEs.RunFail29; case "pt": return TxtPt.RunFail29; case "ru": return TxtRu.RunFail29; case "de": return TxtDe.RunFail29; case "fr": return TxtFr.RunFail29; default: return TxtCn.RunFail29; } } }
        public static string RunFail30 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail30; case "en": return TxtEn.RunFail30; case "ja": return TxtJa.RunFail30; case "ko": return TxtKo.RunFail30; case "es": return TxtEs.RunFail30; case "pt": return TxtPt.RunFail30; case "ru": return TxtRu.RunFail30; case "de": return TxtDe.RunFail30; case "fr": return TxtFr.RunFail30; default: return TxtCn.RunFail30; } } }
        public static string RunFail31 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail31; case "en": return TxtEn.RunFail31; case "ja": return TxtJa.RunFail31; case "ko": return TxtKo.RunFail31; case "es": return TxtEs.RunFail31; case "pt": return TxtPt.RunFail31; case "ru": return TxtRu.RunFail31; case "de": return TxtDe.RunFail31; case "fr": return TxtFr.RunFail31; default: return TxtCn.RunFail31; } } }
        public static string RunWizLog22 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog22; case "en": return TxtEn.RunWizLog22; case "ja": return TxtJa.RunWizLog22; case "ko": return TxtKo.RunWizLog22; case "es": return TxtEs.RunWizLog22; case "pt": return TxtPt.RunWizLog22; case "ru": return TxtRu.RunWizLog22; case "de": return TxtDe.RunWizLog22; case "fr": return TxtFr.RunWizLog22; default: return TxtCn.RunWizLog22; } } }
        public static string RunDownloadWithProgress3 { get { switch (Lang.Code) { case "tw": return TxtTw.RunDownloadWithProgress3; case "en": return TxtEn.RunDownloadWithProgress3; case "ja": return TxtJa.RunDownloadWithProgress3; case "ko": return TxtKo.RunDownloadWithProgress3; case "es": return TxtEs.RunDownloadWithProgress3; case "pt": return TxtPt.RunDownloadWithProgress3; case "ru": return TxtRu.RunDownloadWithProgress3; case "de": return TxtDe.RunDownloadWithProgress3; case "fr": return TxtFr.RunDownloadWithProgress3; default: return TxtCn.RunDownloadWithProgress3; } } }
        public static string RunFail32 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail32; case "en": return TxtEn.RunFail32; case "ja": return TxtJa.RunFail32; case "ko": return TxtKo.RunFail32; case "es": return TxtEs.RunFail32; case "pt": return TxtPt.RunFail32; case "ru": return TxtRu.RunFail32; case "de": return TxtDe.RunFail32; case "fr": return TxtFr.RunFail32; default: return TxtCn.RunFail32; } } }
        public static string RunWizLog23 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog23; case "en": return TxtEn.RunWizLog23; case "ja": return TxtJa.RunWizLog23; case "ko": return TxtKo.RunWizLog23; case "es": return TxtEs.RunWizLog23; case "pt": return TxtPt.RunWizLog23; case "ru": return TxtRu.RunWizLog23; case "de": return TxtDe.RunWizLog23; case "fr": return TxtFr.RunWizLog23; default: return TxtCn.RunWizLog23; } } }
        public static string RunFail33 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail33; case "en": return TxtEn.RunFail33; case "ja": return TxtJa.RunFail33; case "ko": return TxtKo.RunFail33; case "es": return TxtEs.RunFail33; case "pt": return TxtPt.RunFail33; case "ru": return TxtRu.RunFail33; case "de": return TxtDe.RunFail33; case "fr": return TxtFr.RunFail33; default: return TxtCn.RunFail33; } } }
        public static string RunWizStep3 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizStep3; case "en": return TxtEn.RunWizStep3; case "ja": return TxtJa.RunWizStep3; case "ko": return TxtKo.RunWizStep3; case "es": return TxtEs.RunWizStep3; case "pt": return TxtPt.RunWizStep3; case "ru": return TxtRu.RunWizStep3; case "de": return TxtDe.RunWizStep3; case "fr": return TxtFr.RunWizStep3; default: return TxtCn.RunWizStep3; } } }
        public static string RunWizLog24 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog24; case "en": return TxtEn.RunWizLog24; case "ja": return TxtJa.RunWizLog24; case "ko": return TxtKo.RunWizLog24; case "es": return TxtEs.RunWizLog24; case "pt": return TxtPt.RunWizLog24; case "ru": return TxtRu.RunWizLog24; case "de": return TxtDe.RunWizLog24; case "fr": return TxtFr.RunWizLog24; default: return TxtCn.RunWizLog24; } } }
        public static string RunWizLog25 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog25; case "en": return TxtEn.RunWizLog25; case "ja": return TxtJa.RunWizLog25; case "ko": return TxtKo.RunWizLog25; case "es": return TxtEs.RunWizLog25; case "pt": return TxtPt.RunWizLog25; case "ru": return TxtRu.RunWizLog25; case "de": return TxtDe.RunWizLog25; case "fr": return TxtFr.RunWizLog25; default: return TxtCn.RunWizLog25; } } }
        public static string RunWizLog26 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog26; case "en": return TxtEn.RunWizLog26; case "ja": return TxtJa.RunWizLog26; case "ko": return TxtKo.RunWizLog26; case "es": return TxtEs.RunWizLog26; case "pt": return TxtPt.RunWizLog26; case "ru": return TxtRu.RunWizLog26; case "de": return TxtDe.RunWizLog26; case "fr": return TxtFr.RunWizLog26; default: return TxtCn.RunWizLog26; } } }
        public static string RunWizLog27 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog27; case "en": return TxtEn.RunWizLog27; case "ja": return TxtJa.RunWizLog27; case "ko": return TxtKo.RunWizLog27; case "es": return TxtEs.RunWizLog27; case "pt": return TxtPt.RunWizLog27; case "ru": return TxtRu.RunWizLog27; case "de": return TxtDe.RunWizLog27; case "fr": return TxtFr.RunWizLog27; default: return TxtCn.RunWizLog27; } } }
        public static string RunWizStep4 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizStep4; case "en": return TxtEn.RunWizStep4; case "ja": return TxtJa.RunWizStep4; case "ko": return TxtKo.RunWizStep4; case "es": return TxtEs.RunWizStep4; case "pt": return TxtPt.RunWizStep4; case "ru": return TxtRu.RunWizStep4; case "de": return TxtDe.RunWizStep4; case "fr": return TxtFr.RunWizStep4; default: return TxtCn.RunWizStep4; } } }
        public static string RunWizLog28 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog28; case "en": return TxtEn.RunWizLog28; case "ja": return TxtJa.RunWizLog28; case "ko": return TxtKo.RunWizLog28; case "es": return TxtEs.RunWizLog28; case "pt": return TxtPt.RunWizLog28; case "ru": return TxtRu.RunWizLog28; case "de": return TxtDe.RunWizLog28; case "fr": return TxtFr.RunWizLog28; default: return TxtCn.RunWizLog28; } } }
        public static string RunWizLog29 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog29; case "en": return TxtEn.RunWizLog29; case "ja": return TxtJa.RunWizLog29; case "ko": return TxtKo.RunWizLog29; case "es": return TxtEs.RunWizLog29; case "pt": return TxtPt.RunWizLog29; case "ru": return TxtRu.RunWizLog29; case "de": return TxtDe.RunWizLog29; case "fr": return TxtFr.RunWizLog29; default: return TxtCn.RunWizLog29; } } }
        public static string RunStr2 { get { switch (Lang.Code) { case "tw": return TxtTw.RunStr2; case "en": return TxtEn.RunStr2; case "ja": return TxtJa.RunStr2; case "ko": return TxtKo.RunStr2; case "es": return TxtEs.RunStr2; case "pt": return TxtPt.RunStr2; case "ru": return TxtRu.RunStr2; case "de": return TxtDe.RunStr2; case "fr": return TxtFr.RunStr2; default: return TxtCn.RunStr2; } } }
        public static string RunWizLog30 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizLog30; case "en": return TxtEn.RunWizLog30; case "ja": return TxtJa.RunWizLog30; case "ko": return TxtKo.RunWizLog30; case "es": return TxtEs.RunWizLog30; case "pt": return TxtPt.RunWizLog30; case "ru": return TxtRu.RunWizLog30; case "de": return TxtDe.RunWizLog30; case "fr": return TxtFr.RunWizLog30; default: return TxtCn.RunWizLog30; } } }
        public static string RunWizStep5 { get { switch (Lang.Code) { case "tw": return TxtTw.RunWizStep5; case "en": return TxtEn.RunWizStep5; case "ja": return TxtJa.RunWizStep5; case "ko": return TxtKo.RunWizStep5; case "es": return TxtEs.RunWizStep5; case "pt": return TxtPt.RunWizStep5; case "ru": return TxtRu.RunWizStep5; case "de": return TxtDe.RunWizStep5; case "fr": return TxtFr.RunWizStep5; default: return TxtCn.RunWizStep5; } } }
        public static string RunFail34 { get { switch (Lang.Code) { case "tw": return TxtTw.RunFail34; case "en": return TxtEn.RunFail34; case "ja": return TxtJa.RunFail34; case "ko": return TxtKo.RunFail34; case "es": return TxtEs.RunFail34; case "pt": return TxtPt.RunFail34; case "ru": return TxtRu.RunFail34; case "de": return TxtDe.RunFail34; case "fr": return TxtFr.RunFail34; default: return TxtCn.RunFail34; } } }
        public static string DownloadWithProgressWizStep { get { switch (Lang.Code) { case "tw": return TxtTw.DownloadWithProgressWizStep; case "en": return TxtEn.DownloadWithProgressWizStep; case "ja": return TxtJa.DownloadWithProgressWizStep; case "ko": return TxtKo.DownloadWithProgressWizStep; case "es": return TxtEs.DownloadWithProgressWizStep; case "pt": return TxtPt.DownloadWithProgressWizStep; case "ru": return TxtRu.DownloadWithProgressWizStep; case "de": return TxtDe.DownloadWithProgressWizStep; case "fr": return TxtFr.DownloadWithProgressWizStep; default: return TxtCn.DownloadWithProgressWizStep; } } }
        public static string DownloadWithProgressWizStep2 { get { switch (Lang.Code) { case "tw": return TxtTw.DownloadWithProgressWizStep2; case "en": return TxtEn.DownloadWithProgressWizStep2; case "ja": return TxtJa.DownloadWithProgressWizStep2; case "ko": return TxtKo.DownloadWithProgressWizStep2; case "es": return TxtEs.DownloadWithProgressWizStep2; case "pt": return TxtPt.DownloadWithProgressWizStep2; case "ru": return TxtRu.DownloadWithProgressWizStep2; case "de": return TxtDe.DownloadWithProgressWizStep2; case "fr": return TxtFr.DownloadWithProgressWizStep2; default: return TxtCn.DownloadWithProgressWizStep2; } } }
        public static string FailWizLog { get { switch (Lang.Code) { case "tw": return TxtTw.FailWizLog; case "en": return TxtEn.FailWizLog; case "ja": return TxtJa.FailWizLog; case "ko": return TxtKo.FailWizLog; case "es": return TxtEs.FailWizLog; case "pt": return TxtPt.FailWizLog; case "ru": return TxtRu.FailWizLog; case "de": return TxtDe.FailWizLog; case "fr": return TxtFr.FailWizLog; default: return TxtCn.FailWizLog; } } }
        public static string FailWizStep { get { switch (Lang.Code) { case "tw": return TxtTw.FailWizStep; case "en": return TxtEn.FailWizStep; case "ja": return TxtJa.FailWizStep; case "ko": return TxtKo.FailWizStep; case "es": return TxtEs.FailWizStep; case "pt": return TxtPt.FailWizStep; case "ru": return TxtRu.FailWizStep; case "de": return TxtDe.FailWizStep; case "fr": return TxtFr.FailWizStep; default: return TxtCn.FailWizStep; } } }
        public static string CancelledWizLog { get { switch (Lang.Code) { case "tw": return TxtTw.CancelledWizLog; case "en": return TxtEn.CancelledWizLog; case "ja": return TxtJa.CancelledWizLog; case "ko": return TxtKo.CancelledWizLog; case "es": return TxtEs.CancelledWizLog; case "pt": return TxtPt.CancelledWizLog; case "ru": return TxtRu.CancelledWizLog; case "de": return TxtDe.CancelledWizLog; case "fr": return TxtFr.CancelledWizLog; default: return TxtCn.CancelledWizLog; } } }
        public static string CancelledWizStep { get { switch (Lang.Code) { case "tw": return TxtTw.CancelledWizStep; case "en": return TxtEn.CancelledWizStep; case "ja": return TxtJa.CancelledWizStep; case "ko": return TxtKo.CancelledWizStep; case "es": return TxtEs.CancelledWizStep; case "pt": return TxtPt.CancelledWizStep; case "ru": return TxtRu.CancelledWizStep; case "de": return TxtDe.CancelledWizStep; case "fr": return TxtFr.CancelledWizStep; default: return TxtCn.CancelledWizStep; } } }
        public static string MainFormAppendServerLog { get { switch (Lang.Code) { case "tw": return TxtTw.MainFormAppendServerLog; case "en": return TxtEn.MainFormAppendServerLog; case "ja": return TxtJa.MainFormAppendServerLog; case "ko": return TxtKo.MainFormAppendServerLog; case "es": return TxtEs.MainFormAppendServerLog; case "pt": return TxtPt.MainFormAppendServerLog; case "ru": return TxtRu.MainFormAppendServerLog; case "de": return TxtDe.MainFormAppendServerLog; case "fr": return TxtFr.MainFormAppendServerLog; default: return TxtCn.MainFormAppendServerLog; } } }
        public static string TrayText { get { switch (Lang.Code) { case "tw": return TxtTw.TrayText; case "en": return TxtEn.TrayText; case "ja": return TxtJa.TrayText; case "ko": return TxtKo.TrayText; case "es": return TxtEs.TrayText; case "pt": return TxtPt.TrayText; case "ru": return TxtRu.TrayText; case "de": return TxtDe.TrayText; case "fr": return TxtFr.TrayText; default: return TxtCn.TrayText; } } }
        public static string InitTray613ToolStripMenuItem { get { switch (Lang.Code) { case "tw": return TxtTw.InitTray613ToolStripMenuItem; case "en": return TxtEn.InitTray613ToolStripMenuItem; case "ja": return TxtJa.InitTray613ToolStripMenuItem; case "ko": return TxtKo.InitTray613ToolStripMenuItem; case "es": return TxtEs.InitTray613ToolStripMenuItem; case "pt": return TxtPt.InitTray613ToolStripMenuItem; case "ru": return TxtRu.InitTray613ToolStripMenuItem; case "de": return TxtDe.InitTray613ToolStripMenuItem; case "fr": return TxtFr.InitTray613ToolStripMenuItem; default: return TxtCn.InitTray613ToolStripMenuItem; } } }
        public static string InitTray613ToolStripMenuItem2 { get { switch (Lang.Code) { case "tw": return TxtTw.InitTray613ToolStripMenuItem2; case "en": return TxtEn.InitTray613ToolStripMenuItem2; case "ja": return TxtJa.InitTray613ToolStripMenuItem2; case "ko": return TxtKo.InitTray613ToolStripMenuItem2; case "es": return TxtEs.InitTray613ToolStripMenuItem2; case "pt": return TxtPt.InitTray613ToolStripMenuItem2; case "ru": return TxtRu.InitTray613ToolStripMenuItem2; case "de": return TxtDe.InitTray613ToolStripMenuItem2; case "fr": return TxtFr.InitTray613ToolStripMenuItem2; default: return TxtCn.InitTray613ToolStripMenuItem2; } } }
        public static string BtnStartText2 { get { switch (Lang.Code) { case "tw": return TxtTw.BtnStartText2; case "en": return TxtEn.BtnStartText2; case "ja": return TxtJa.BtnStartText2; case "ko": return TxtKo.BtnStartText2; case "es": return TxtEs.BtnStartText2; case "pt": return TxtPt.BtnStartText2; case "ru": return TxtRu.BtnStartText2; case "de": return TxtDe.BtnStartText2; case "fr": return TxtFr.BtnStartText2; default: return TxtCn.BtnStartText2; } } }
        public static string AddrLblCaption { get { switch (Lang.Code) { case "tw": return TxtTw.AddrLblCaption; case "en": return TxtEn.AddrLblCaption; case "ja": return TxtJa.AddrLblCaption; case "ko": return TxtKo.AddrLblCaption; case "es": return TxtEs.AddrLblCaption; case "pt": return TxtPt.AddrLblCaption; case "ru": return TxtRu.AddrLblCaption; case "de": return TxtDe.AddrLblCaption; case "fr": return TxtFr.AddrLblCaption; default: return TxtCn.AddrLblCaption; } } }
        public static string Hint { get { switch (Lang.Code) { case "tw": return TxtTw.Hint; case "en": return TxtEn.Hint; case "ja": return TxtJa.Hint; case "ko": return TxtKo.Hint; case "es": return TxtEs.Hint; case "pt": return TxtPt.Hint; case "ru": return TxtRu.Hint; case "de": return TxtDe.Hint; case "fr": return TxtFr.Hint; default: return TxtCn.Hint; } } }
        public static string BtnCopyText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnCopyText; case "en": return TxtEn.BtnCopyText; case "ja": return TxtJa.BtnCopyText; case "ko": return TxtKo.BtnCopyText; case "es": return TxtEs.BtnCopyText; case "pt": return TxtPt.BtnCopyText; case "ru": return TxtRu.BtnCopyText; case "de": return TxtDe.BtnCopyText; case "fr": return TxtFr.BtnCopyText; default: return TxtCn.BtnCopyText; } } }
        public static string BtnPcl2Text { get { switch (Lang.Code) { case "tw": return TxtTw.BtnPcl2Text; case "en": return TxtEn.BtnPcl2Text; case "ja": return TxtJa.BtnPcl2Text; case "ko": return TxtKo.BtnPcl2Text; case "es": return TxtEs.BtnPcl2Text; case "pt": return TxtPt.BtnPcl2Text; case "ru": return TxtRu.BtnPcl2Text; case "de": return TxtDe.BtnPcl2Text; case "fr": return TxtFr.BtnPcl2Text; default: return TxtCn.BtnPcl2Text; } } }
        public static string BtnStopText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnStopText; case "en": return TxtEn.BtnStopText; case "ja": return TxtJa.BtnStopText; case "ko": return TxtKo.BtnStopText; case "es": return TxtEs.BtnStopText; case "pt": return TxtPt.BtnStopText; case "ru": return TxtRu.BtnStopText; case "de": return TxtDe.BtnStopText; case "fr": return TxtFr.BtnStopText; default: return TxtCn.BtnStopText; } } }
        public static string LogToggleLblCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LogToggleLblCaption; case "en": return TxtEn.LogToggleLblCaption; case "ja": return TxtJa.LogToggleLblCaption; case "ko": return TxtKo.LogToggleLblCaption; case "es": return TxtEs.LogToggleLblCaption; case "pt": return TxtPt.LogToggleLblCaption; case "ru": return TxtRu.LogToggleLblCaption; case "de": return TxtDe.LogToggleLblCaption; case "fr": return TxtFr.LogToggleLblCaption; default: return TxtCn.LogToggleLblCaption; } } }
        public static string BuildUiStr { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiStr; case "en": return TxtEn.BuildUiStr; case "ja": return TxtJa.BuildUiStr; case "ko": return TxtKo.BuildUiStr; case "es": return TxtEs.BuildUiStr; case "pt": return TxtPt.BuildUiStr; case "ru": return TxtRu.BuildUiStr; case "de": return TxtDe.BuildUiStr; case "fr": return TxtFr.BuildUiStr; default: return TxtCn.BuildUiStr; } } }
        public static string BuildUiStr2 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiStr2; case "en": return TxtEn.BuildUiStr2; case "ja": return TxtJa.BuildUiStr2; case "ko": return TxtKo.BuildUiStr2; case "es": return TxtEs.BuildUiStr2; case "pt": return TxtPt.BuildUiStr2; case "ru": return TxtRu.BuildUiStr2; case "de": return TxtDe.BuildUiStr2; case "fr": return TxtFr.BuildUiStr2; default: return TxtCn.BuildUiStr2; } } }
        public static string BuildUiStr3 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiStr3; case "en": return TxtEn.BuildUiStr3; case "ja": return TxtJa.BuildUiStr3; case "ko": return TxtKo.BuildUiStr3; case "es": return TxtEs.BuildUiStr3; case "pt": return TxtPt.BuildUiStr3; case "ru": return TxtRu.BuildUiStr3; case "de": return TxtDe.BuildUiStr3; case "fr": return TxtFr.BuildUiStr3; default: return TxtCn.BuildUiStr3; } } }
        public static string BuildUiStr4 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiStr4; case "en": return TxtEn.BuildUiStr4; case "ja": return TxtJa.BuildUiStr4; case "ko": return TxtKo.BuildUiStr4; case "es": return TxtEs.BuildUiStr4; case "pt": return TxtPt.BuildUiStr4; case "ru": return TxtRu.BuildUiStr4; case "de": return TxtDe.BuildUiStr4; case "fr": return TxtFr.BuildUiStr4; default: return TxtCn.BuildUiStr4; } } }
        public static string BuildUiMakeLogBox { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiMakeLogBox; case "en": return TxtEn.BuildUiMakeLogBox; case "ja": return TxtJa.BuildUiMakeLogBox; case "ko": return TxtKo.BuildUiMakeLogBox; case "es": return TxtEs.BuildUiMakeLogBox; case "pt": return TxtPt.BuildUiMakeLogBox; case "ru": return TxtRu.BuildUiMakeLogBox; case "de": return TxtDe.BuildUiMakeLogBox; case "fr": return TxtFr.BuildUiMakeLogBox; default: return TxtCn.BuildUiMakeLogBox; } } }
        public static string BuildUiMakeLogBox2 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiMakeLogBox2; case "en": return TxtEn.BuildUiMakeLogBox2; case "ja": return TxtJa.BuildUiMakeLogBox2; case "ko": return TxtKo.BuildUiMakeLogBox2; case "es": return TxtEs.BuildUiMakeLogBox2; case "pt": return TxtPt.BuildUiMakeLogBox2; case "ru": return TxtRu.BuildUiMakeLogBox2; case "de": return TxtDe.BuildUiMakeLogBox2; case "fr": return TxtFr.BuildUiMakeLogBox2; default: return TxtCn.BuildUiMakeLogBox2; } } }
        public static string BtnJumpBottomText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnJumpBottomText; case "en": return TxtEn.BtnJumpBottomText; case "ja": return TxtJa.BtnJumpBottomText; case "ko": return TxtKo.BtnJumpBottomText; case "es": return TxtEs.BtnJumpBottomText; case "pt": return TxtPt.BtnJumpBottomText; case "ru": return TxtRu.BtnJumpBottomText; case "de": return TxtDe.BtnJumpBottomText; case "fr": return TxtFr.BtnJumpBottomText; default: return TxtCn.BtnJumpBottomText; } } }
        public static string Hint2 { get { switch (Lang.Code) { case "tw": return TxtTw.Hint2; case "en": return TxtEn.Hint2; case "ja": return TxtJa.Hint2; case "ko": return TxtKo.Hint2; case "es": return TxtEs.Hint2; case "pt": return TxtPt.Hint2; case "ru": return TxtRu.Hint2; case "de": return TxtDe.Hint2; case "fr": return TxtFr.Hint2; default: return TxtCn.Hint2; } } }
        public static string BuildUiSetToolTip { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiSetToolTip; case "en": return TxtEn.BuildUiSetToolTip; case "ja": return TxtJa.BuildUiSetToolTip; case "ko": return TxtKo.BuildUiSetToolTip; case "es": return TxtEs.BuildUiSetToolTip; case "pt": return TxtPt.BuildUiSetToolTip; case "ru": return TxtRu.BuildUiSetToolTip; case "de": return TxtDe.BuildUiSetToolTip; case "fr": return TxtFr.BuildUiSetToolTip; default: return TxtCn.BuildUiSetToolTip; } } }
        public static string BuildUiSetToolTip2 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiSetToolTip2; case "en": return TxtEn.BuildUiSetToolTip2; case "ja": return TxtJa.BuildUiSetToolTip2; case "ko": return TxtKo.BuildUiSetToolTip2; case "es": return TxtEs.BuildUiSetToolTip2; case "pt": return TxtPt.BuildUiSetToolTip2; case "ru": return TxtRu.BuildUiSetToolTip2; case "de": return TxtDe.BuildUiSetToolTip2; case "fr": return TxtFr.BuildUiSetToolTip2; default: return TxtCn.BuildUiSetToolTip2; } } }
        public static string BuildUiSetToolTip3 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiSetToolTip3; case "en": return TxtEn.BuildUiSetToolTip3; case "ja": return TxtJa.BuildUiSetToolTip3; case "ko": return TxtKo.BuildUiSetToolTip3; case "es": return TxtEs.BuildUiSetToolTip3; case "pt": return TxtPt.BuildUiSetToolTip3; case "ru": return TxtRu.BuildUiSetToolTip3; case "de": return TxtDe.BuildUiSetToolTip3; case "fr": return TxtFr.BuildUiSetToolTip3; default: return TxtCn.BuildUiSetToolTip3; } } }
        public static string ChkErrOnlyText { get { switch (Lang.Code) { case "tw": return TxtTw.ChkErrOnlyText; case "en": return TxtEn.ChkErrOnlyText; case "ja": return TxtJa.ChkErrOnlyText; case "ko": return TxtKo.ChkErrOnlyText; case "es": return TxtEs.ChkErrOnlyText; case "pt": return TxtPt.ChkErrOnlyText; case "ru": return TxtRu.ChkErrOnlyText; case "de": return TxtDe.ChkErrOnlyText; case "fr": return TxtFr.ChkErrOnlyText; default: return TxtCn.ChkErrOnlyText; } } }
        public static string CmdLblCaption { get { switch (Lang.Code) { case "tw": return TxtTw.CmdLblCaption; case "en": return TxtEn.CmdLblCaption; case "ja": return TxtJa.CmdLblCaption; case "ko": return TxtKo.CmdLblCaption; case "es": return TxtEs.CmdLblCaption; case "pt": return TxtPt.CmdLblCaption; case "ru": return TxtRu.CmdLblCaption; case "de": return TxtDe.CmdLblCaption; case "fr": return TxtFr.CmdLblCaption; default: return TxtCn.CmdLblCaption; } } }
        public static string BtnCmdSendText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnCmdSendText; case "en": return TxtEn.BtnCmdSendText; case "ja": return TxtJa.BtnCmdSendText; case "ko": return TxtKo.BtnCmdSendText; case "es": return TxtEs.BtnCmdSendText; case "pt": return TxtPt.BtnCmdSendText; case "ru": return TxtRu.BtnCmdSendText; case "de": return TxtDe.BtnCmdSendText; case "fr": return TxtFr.BtnCmdSendText; default: return TxtCn.BtnCmdSendText; } } }
        public static string BuildUiAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.BuildUiAppendState; case "en": return TxtEn.BuildUiAppendState; case "ja": return TxtJa.BuildUiAppendState; case "ko": return TxtKo.BuildUiAppendState; case "es": return TxtEs.BuildUiAppendState; case "pt": return TxtPt.BuildUiAppendState; case "ru": return TxtRu.BuildUiAppendState; case "de": return TxtDe.BuildUiAppendState; case "fr": return TxtFr.BuildUiAppendState; default: return TxtCn.BuildUiAppendState; } } }
        public static string IsErrLineContains { get { switch (Lang.Code) { case "tw": return TxtTw.IsErrLineContains; case "en": return TxtEn.IsErrLineContains; case "ja": return TxtJa.IsErrLineContains; case "ko": return TxtKo.IsErrLineContains; case "es": return TxtEs.IsErrLineContains; case "pt": return TxtPt.IsErrLineContains; case "ru": return TxtRu.IsErrLineContains; case "de": return TxtDe.IsErrLineContains; case "fr": return TxtFr.IsErrLineContains; default: return TxtCn.IsErrLineContains; } } }
        public static string IsErrLineContains2 { get { switch (Lang.Code) { case "tw": return TxtTw.IsErrLineContains2; case "en": return TxtEn.IsErrLineContains2; case "ja": return TxtJa.IsErrLineContains2; case "ko": return TxtKo.IsErrLineContains2; case "es": return TxtEs.IsErrLineContains2; case "pt": return TxtPt.IsErrLineContains2; case "ru": return TxtRu.IsErrLineContains2; case "de": return TxtDe.IsErrLineContains2; case "fr": return TxtFr.IsErrLineContains2; default: return TxtCn.IsErrLineContains2; } } }
        public static string LblFindCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LblFindCaption; case "en": return TxtEn.LblFindCaption; case "ja": return TxtJa.LblFindCaption; case "ko": return TxtKo.LblFindCaption; case "es": return TxtEs.LblFindCaption; case "pt": return TxtPt.LblFindCaption; case "ru": return TxtRu.LblFindCaption; case "de": return TxtDe.LblFindCaption; case "fr": return TxtFr.LblFindCaption; default: return TxtCn.LblFindCaption; } } }
        public static string SrvState2 { get { switch (Lang.Code) { case "tw": return TxtTw.SrvState2; case "en": return TxtEn.SrvState2; case "ja": return TxtJa.SrvState2; case "ko": return TxtKo.SrvState2; case "es": return TxtEs.SrvState2; case "pt": return TxtPt.SrvState2; case "ru": return TxtRu.SrvState2; case "de": return TxtDe.SrvState2; case "fr": return TxtFr.SrvState2; default: return TxtCn.SrvState2; } } }
        public static string SampleStatsPart { get { switch (Lang.Code) { case "tw": return TxtTw.SampleStatsPart; case "en": return TxtEn.SampleStatsPart; case "ja": return TxtJa.SampleStatsPart; case "ko": return TxtKo.SampleStatsPart; case "es": return TxtEs.SampleStatsPart; case "pt": return TxtPt.SampleStatsPart; case "ru": return TxtRu.SampleStatsPart; case "de": return TxtDe.SampleStatsPart; case "fr": return TxtFr.SampleStatsPart; default: return TxtCn.SampleStatsPart; } } }
        public static string SampleStatsPart2 { get { switch (Lang.Code) { case "tw": return TxtTw.SampleStatsPart2; case "en": return TxtEn.SampleStatsPart2; case "ja": return TxtJa.SampleStatsPart2; case "ko": return TxtKo.SampleStatsPart2; case "es": return TxtEs.SampleStatsPart2; case "pt": return TxtPt.SampleStatsPart2; case "ru": return TxtRu.SampleStatsPart2; case "de": return TxtDe.SampleStatsPart2; case "fr": return TxtFr.SampleStatsPart2; default: return TxtCn.SampleStatsPart2; } } }
        public static string SampleStatsPart3 { get { switch (Lang.Code) { case "tw": return TxtTw.SampleStatsPart3; case "en": return TxtEn.SampleStatsPart3; case "ja": return TxtJa.SampleStatsPart3; case "ko": return TxtKo.SampleStatsPart3; case "es": return TxtEs.SampleStatsPart3; case "pt": return TxtPt.SampleStatsPart3; case "ru": return TxtRu.SampleStatsPart3; case "de": return TxtDe.SampleStatsPart3; case "fr": return TxtFr.SampleStatsPart3; default: return TxtCn.SampleStatsPart3; } } }
        public static string ToggleLogStr { get { switch (Lang.Code) { case "tw": return TxtTw.ToggleLogStr; case "en": return TxtEn.ToggleLogStr; case "ja": return TxtJa.ToggleLogStr; case "ko": return TxtKo.ToggleLogStr; case "es": return TxtEs.ToggleLogStr; case "pt": return TxtPt.ToggleLogStr; case "ru": return TxtRu.ToggleLogStr; case "de": return TxtDe.ToggleLogStr; case "fr": return TxtFr.ToggleLogStr; default: return TxtCn.ToggleLogStr; } } }
        public static string AppendServerLogPersistLog613 { get { switch (Lang.Code) { case "tw": return TxtTw.AppendServerLogPersistLog613; case "en": return TxtEn.AppendServerLogPersistLog613; case "ja": return TxtJa.AppendServerLogPersistLog613; case "ko": return TxtKo.AppendServerLogPersistLog613; case "es": return TxtEs.AppendServerLogPersistLog613; case "pt": return TxtPt.AppendServerLogPersistLog613; case "ru": return TxtRu.AppendServerLogPersistLog613; case "de": return TxtDe.AppendServerLogPersistLog613; case "fr": return TxtFr.AppendServerLogPersistLog613; default: return TxtCn.AppendServerLogPersistLog613; } } }
        public static string AppendTunnelLogPersistLog613 { get { switch (Lang.Code) { case "tw": return TxtTw.AppendTunnelLogPersistLog613; case "en": return TxtEn.AppendTunnelLogPersistLog613; case "ja": return TxtJa.AppendTunnelLogPersistLog613; case "ko": return TxtKo.AppendTunnelLogPersistLog613; case "es": return TxtEs.AppendTunnelLogPersistLog613; case "pt": return TxtPt.AppendTunnelLogPersistLog613; case "ru": return TxtRu.AppendTunnelLogPersistLog613; case "de": return TxtDe.AppendTunnelLogPersistLog613; case "fr": return TxtFr.AppendTunnelLogPersistLog613; default: return TxtCn.AppendTunnelLogPersistLog613; } } }
        public static string SendServerCommandAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.SendServerCommandAppendState; case "en": return TxtEn.SendServerCommandAppendState; case "ja": return TxtJa.SendServerCommandAppendState; case "ko": return TxtKo.SendServerCommandAppendState; case "es": return TxtEs.SendServerCommandAppendState; case "pt": return TxtPt.SendServerCommandAppendState; case "ru": return TxtRu.SendServerCommandAppendState; case "de": return TxtDe.SendServerCommandAppendState; case "fr": return TxtFr.SendServerCommandAppendState; default: return TxtCn.SendServerCommandAppendState; } } }
        public static string SendServerCommandAppendState2 { get { switch (Lang.Code) { case "tw": return TxtTw.SendServerCommandAppendState2; case "en": return TxtEn.SendServerCommandAppendState2; case "ja": return TxtJa.SendServerCommandAppendState2; case "ko": return TxtKo.SendServerCommandAppendState2; case "es": return TxtEs.SendServerCommandAppendState2; case "pt": return TxtPt.SendServerCommandAppendState2; case "ru": return TxtRu.SendServerCommandAppendState2; case "de": return TxtDe.SendServerCommandAppendState2; case "fr": return TxtFr.SendServerCommandAppendState2; default: return TxtCn.SendServerCommandAppendState2; } } }
        public static string SendServerCommandAppendState3 { get { switch (Lang.Code) { case "tw": return TxtTw.SendServerCommandAppendState3; case "en": return TxtEn.SendServerCommandAppendState3; case "ja": return TxtJa.SendServerCommandAppendState3; case "ko": return TxtKo.SendServerCommandAppendState3; case "es": return TxtEs.SendServerCommandAppendState3; case "pt": return TxtPt.SendServerCommandAppendState3; case "ru": return TxtRu.SendServerCommandAppendState3; case "de": return TxtDe.SendServerCommandAppendState3; case "fr": return TxtFr.SendServerCommandAppendState3; default: return TxtCn.SendServerCommandAppendState3; } } }
        public static string OpenPlayerManagerShow { get { switch (Lang.Code) { case "tw": return TxtTw.OpenPlayerManagerShow; case "en": return TxtEn.OpenPlayerManagerShow; case "ja": return TxtJa.OpenPlayerManagerShow; case "ko": return TxtKo.OpenPlayerManagerShow; case "es": return TxtEs.OpenPlayerManagerShow; case "pt": return TxtPt.OpenPlayerManagerShow; case "ru": return TxtRu.OpenPlayerManagerShow; case "de": return TxtDe.OpenPlayerManagerShow; case "fr": return TxtFr.OpenPlayerManagerShow; default: return TxtCn.OpenPlayerManagerShow; } } }
        public static string OpenPlayerManagerAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.OpenPlayerManagerAppendState; case "en": return TxtEn.OpenPlayerManagerAppendState; case "ja": return TxtJa.OpenPlayerManagerAppendState; case "ko": return TxtKo.OpenPlayerManagerAppendState; case "es": return TxtEs.OpenPlayerManagerAppendState; case "pt": return TxtPt.OpenPlayerManagerAppendState; case "ru": return TxtRu.OpenPlayerManagerAppendState; case "de": return TxtDe.OpenPlayerManagerAppendState; case "fr": return TxtFr.OpenPlayerManagerAppendState; default: return TxtCn.OpenPlayerManagerAppendState; } } }
        public static string DoBackupAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.DoBackupAppendState; case "en": return TxtEn.DoBackupAppendState; case "ja": return TxtJa.DoBackupAppendState; case "ko": return TxtKo.DoBackupAppendState; case "es": return TxtEs.DoBackupAppendState; case "pt": return TxtPt.DoBackupAppendState; case "ru": return TxtRu.DoBackupAppendState; case "de": return TxtDe.DoBackupAppendState; case "fr": return TxtFr.DoBackupAppendState; default: return TxtCn.DoBackupAppendState; } } }
        public static string BackupCoreAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreAppendState; case "en": return TxtEn.BackupCoreAppendState; case "ja": return TxtJa.BackupCoreAppendState; case "ko": return TxtKo.BackupCoreAppendState; case "es": return TxtEs.BackupCoreAppendState; case "pt": return TxtPt.BackupCoreAppendState; case "ru": return TxtRu.BackupCoreAppendState; case "de": return TxtDe.BackupCoreAppendState; case "fr": return TxtFr.BackupCoreAppendState; default: return TxtCn.BackupCoreAppendState; } } }
        public static string BackupCoreAppendState2 { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreAppendState2; case "en": return TxtEn.BackupCoreAppendState2; case "ja": return TxtJa.BackupCoreAppendState2; case "ko": return TxtKo.BackupCoreAppendState2; case "es": return TxtEs.BackupCoreAppendState2; case "pt": return TxtPt.BackupCoreAppendState2; case "ru": return TxtRu.BackupCoreAppendState2; case "de": return TxtDe.BackupCoreAppendState2; case "fr": return TxtFr.BackupCoreAppendState2; default: return TxtCn.BackupCoreAppendState2; } } }
        public static string BackupCoreAppendServerLog { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreAppendServerLog; case "en": return TxtEn.BackupCoreAppendServerLog; case "ja": return TxtJa.BackupCoreAppendServerLog; case "ko": return TxtKo.BackupCoreAppendServerLog; case "es": return TxtEs.BackupCoreAppendServerLog; case "pt": return TxtPt.BackupCoreAppendServerLog; case "ru": return TxtRu.BackupCoreAppendServerLog; case "de": return TxtDe.BackupCoreAppendServerLog; case "fr": return TxtFr.BackupCoreAppendServerLog; default: return TxtCn.BackupCoreAppendServerLog; } } }
        public static string BackupCoreAppendServerLog2 { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreAppendServerLog2; case "en": return TxtEn.BackupCoreAppendServerLog2; case "ja": return TxtJa.BackupCoreAppendServerLog2; case "ko": return TxtKo.BackupCoreAppendServerLog2; case "es": return TxtEs.BackupCoreAppendServerLog2; case "pt": return TxtPt.BackupCoreAppendServerLog2; case "ru": return TxtRu.BackupCoreAppendServerLog2; case "de": return TxtDe.BackupCoreAppendServerLog2; case "fr": return TxtFr.BackupCoreAppendServerLog2; default: return TxtCn.BackupCoreAppendServerLog2; } } }
        public static string BackupCorePart { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCorePart; case "en": return TxtEn.BackupCorePart; case "ja": return TxtJa.BackupCorePart; case "ko": return TxtKo.BackupCorePart; case "es": return TxtEs.BackupCorePart; case "pt": return TxtPt.BackupCorePart; case "ru": return TxtRu.BackupCorePart; case "de": return TxtDe.BackupCorePart; case "fr": return TxtFr.BackupCorePart; default: return TxtCn.BackupCorePart; } } }
        public static string BackupCoreStr { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreStr; case "en": return TxtEn.BackupCoreStr; case "ja": return TxtJa.BackupCoreStr; case "ko": return TxtKo.BackupCoreStr; case "es": return TxtEs.BackupCoreStr; case "pt": return TxtPt.BackupCoreStr; case "ru": return TxtRu.BackupCoreStr; case "de": return TxtDe.BackupCoreStr; case "fr": return TxtFr.BackupCoreStr; default: return TxtCn.BackupCoreStr; } } }
        public static string BackupCorePart2 { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCorePart2; case "en": return TxtEn.BackupCorePart2; case "ja": return TxtJa.BackupCorePart2; case "ko": return TxtKo.BackupCorePart2; case "es": return TxtEs.BackupCorePart2; case "pt": return TxtPt.BackupCorePart2; case "ru": return TxtRu.BackupCorePart2; case "de": return TxtDe.BackupCorePart2; case "fr": return TxtFr.BackupCorePart2; default: return TxtCn.BackupCorePart2; } } }
        public static string BackupCoreAppendState3 { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreAppendState3; case "en": return TxtEn.BackupCoreAppendState3; case "ja": return TxtJa.BackupCoreAppendState3; case "ko": return TxtKo.BackupCoreAppendState3; case "es": return TxtEs.BackupCoreAppendState3; case "pt": return TxtPt.BackupCoreAppendState3; case "ru": return TxtRu.BackupCoreAppendState3; case "de": return TxtDe.BackupCoreAppendState3; case "fr": return TxtFr.BackupCoreAppendState3; default: return TxtCn.BackupCoreAppendState3; } } }
        public static string BackupCoreAppendServerLog3 { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreAppendServerLog3; case "en": return TxtEn.BackupCoreAppendServerLog3; case "ja": return TxtJa.BackupCoreAppendServerLog3; case "ko": return TxtKo.BackupCoreAppendServerLog3; case "es": return TxtEs.BackupCoreAppendServerLog3; case "pt": return TxtPt.BackupCoreAppendServerLog3; case "ru": return TxtRu.BackupCoreAppendServerLog3; case "de": return TxtDe.BackupCoreAppendServerLog3; case "fr": return TxtFr.BackupCoreAppendServerLog3; default: return TxtCn.BackupCoreAppendServerLog3; } } }
        public static string BackupCoreAppendState4 { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreAppendState4; case "en": return TxtEn.BackupCoreAppendState4; case "ja": return TxtJa.BackupCoreAppendState4; case "ko": return TxtKo.BackupCoreAppendState4; case "es": return TxtEs.BackupCoreAppendState4; case "pt": return TxtPt.BackupCoreAppendState4; case "ru": return TxtRu.BackupCoreAppendState4; case "de": return TxtDe.BackupCoreAppendState4; case "fr": return TxtFr.BackupCoreAppendState4; default: return TxtCn.BackupCoreAppendState4; } } }
        public static string BackupCoreAppendServerLog4 { get { switch (Lang.Code) { case "tw": return TxtTw.BackupCoreAppendServerLog4; case "en": return TxtEn.BackupCoreAppendServerLog4; case "ja": return TxtJa.BackupCoreAppendServerLog4; case "ko": return TxtKo.BackupCoreAppendServerLog4; case "es": return TxtEs.BackupCoreAppendServerLog4; case "pt": return TxtPt.BackupCoreAppendServerLog4; case "ru": return TxtRu.BackupCoreAppendServerLog4; case "de": return TxtDe.BackupCoreAppendServerLog4; case "fr": return TxtFr.BackupCoreAppendServerLog4; default: return TxtCn.BackupCoreAppendServerLog4; } } }
        public static string RestoreBackupShow { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupShow; case "en": return TxtEn.RestoreBackupShow; case "ja": return TxtJa.RestoreBackupShow; case "ko": return TxtKo.RestoreBackupShow; case "es": return TxtEs.RestoreBackupShow; case "pt": return TxtPt.RestoreBackupShow; case "ru": return TxtRu.RestoreBackupShow; case "de": return TxtDe.RestoreBackupShow; case "fr": return TxtFr.RestoreBackupShow; default: return TxtCn.RestoreBackupShow; } } }
        public static string RestoreBackupShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupShow2; case "en": return TxtEn.RestoreBackupShow2; case "ja": return TxtJa.RestoreBackupShow2; case "ko": return TxtKo.RestoreBackupShow2; case "es": return TxtEs.RestoreBackupShow2; case "pt": return TxtPt.RestoreBackupShow2; case "ru": return TxtRu.RestoreBackupShow2; case "de": return TxtDe.RestoreBackupShow2; case "fr": return TxtFr.RestoreBackupShow2; default: return TxtCn.RestoreBackupShow2; } } }
        public static string RestoreBackupConfirmBox { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupConfirmBox; case "en": return TxtEn.RestoreBackupConfirmBox; case "ja": return TxtJa.RestoreBackupConfirmBox; case "ko": return TxtKo.RestoreBackupConfirmBox; case "es": return TxtEs.RestoreBackupConfirmBox; case "pt": return TxtPt.RestoreBackupConfirmBox; case "ru": return TxtRu.RestoreBackupConfirmBox; case "de": return TxtDe.RestoreBackupConfirmBox; case "fr": return TxtFr.RestoreBackupConfirmBox; default: return TxtCn.RestoreBackupConfirmBox; } } }
        public static string RestoreBackupConfirmBox2 { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupConfirmBox2; case "en": return TxtEn.RestoreBackupConfirmBox2; case "ja": return TxtJa.RestoreBackupConfirmBox2; case "ko": return TxtKo.RestoreBackupConfirmBox2; case "es": return TxtEs.RestoreBackupConfirmBox2; case "pt": return TxtPt.RestoreBackupConfirmBox2; case "ru": return TxtRu.RestoreBackupConfirmBox2; case "de": return TxtDe.RestoreBackupConfirmBox2; case "fr": return TxtFr.RestoreBackupConfirmBox2; default: return TxtCn.RestoreBackupConfirmBox2; } } }
        public static string RestoreBackupAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupAppendState; case "en": return TxtEn.RestoreBackupAppendState; case "ja": return TxtJa.RestoreBackupAppendState; case "ko": return TxtKo.RestoreBackupAppendState; case "es": return TxtEs.RestoreBackupAppendState; case "pt": return TxtPt.RestoreBackupAppendState; case "ru": return TxtRu.RestoreBackupAppendState; case "de": return TxtDe.RestoreBackupAppendState; case "fr": return TxtFr.RestoreBackupAppendState; default: return TxtCn.RestoreBackupAppendState; } } }
        public static string Result { get { switch (Lang.Code) { case "tw": return TxtTw.Result; case "en": return TxtEn.Result; case "ja": return TxtJa.Result; case "ko": return TxtKo.Result; case "es": return TxtEs.Result; case "pt": return TxtPt.Result; case "ru": return TxtRu.Result; case "de": return TxtDe.Result; case "fr": return TxtFr.Result; default: return TxtCn.Result; } } }
        public static string RestoreBackupPart { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupPart; case "en": return TxtEn.RestoreBackupPart; case "ja": return TxtJa.RestoreBackupPart; case "ko": return TxtKo.RestoreBackupPart; case "es": return TxtEs.RestoreBackupPart; case "pt": return TxtPt.RestoreBackupPart; case "ru": return TxtRu.RestoreBackupPart; case "de": return TxtDe.RestoreBackupPart; case "fr": return TxtFr.RestoreBackupPart; default: return TxtCn.RestoreBackupPart; } } }
        public static string RestoreBackupPart2 { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupPart2; case "en": return TxtEn.RestoreBackupPart2; case "ja": return TxtJa.RestoreBackupPart2; case "ko": return TxtKo.RestoreBackupPart2; case "es": return TxtEs.RestoreBackupPart2; case "pt": return TxtPt.RestoreBackupPart2; case "ru": return TxtRu.RestoreBackupPart2; case "de": return TxtDe.RestoreBackupPart2; case "fr": return TxtFr.RestoreBackupPart2; default: return TxtCn.RestoreBackupPart2; } } }
        public static string Result2 { get { switch (Lang.Code) { case "tw": return TxtTw.Result2; case "en": return TxtEn.Result2; case "ja": return TxtJa.Result2; case "ko": return TxtKo.Result2; case "es": return TxtEs.Result2; case "pt": return TxtPt.Result2; case "ru": return TxtRu.Result2; case "de": return TxtDe.Result2; case "fr": return TxtFr.Result2; default: return TxtCn.Result2; } } }
        public static string RestoreBackupPart3 { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupPart3; case "en": return TxtEn.RestoreBackupPart3; case "ja": return TxtJa.RestoreBackupPart3; case "ko": return TxtKo.RestoreBackupPart3; case "es": return TxtEs.RestoreBackupPart3; case "pt": return TxtPt.RestoreBackupPart3; case "ru": return TxtRu.RestoreBackupPart3; case "de": return TxtDe.RestoreBackupPart3; case "fr": return TxtFr.RestoreBackupPart3; default: return TxtCn.RestoreBackupPart3; } } }
        public static string RestoreBackupAppendServerLog { get { switch (Lang.Code) { case "tw": return TxtTw.RestoreBackupAppendServerLog; case "en": return TxtEn.RestoreBackupAppendServerLog; case "ja": return TxtJa.RestoreBackupAppendServerLog; case "ko": return TxtKo.RestoreBackupAppendServerLog; case "es": return TxtEs.RestoreBackupAppendServerLog; case "pt": return TxtPt.RestoreBackupAppendServerLog; case "ru": return TxtRu.RestoreBackupAppendServerLog; case "de": return TxtDe.RestoreBackupAppendServerLog; case "fr": return TxtFr.RestoreBackupAppendServerLog; default: return TxtCn.RestoreBackupAppendServerLog; } } }
        public static string BuildBackupPanelBkLabel { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelBkLabel; case "en": return TxtEn.BuildBackupPanelBkLabel; case "ja": return TxtJa.BuildBackupPanelBkLabel; case "ko": return TxtKo.BuildBackupPanelBkLabel; case "es": return TxtEs.BuildBackupPanelBkLabel; case "pt": return TxtPt.BuildBackupPanelBkLabel; case "ru": return TxtRu.BuildBackupPanelBkLabel; case "de": return TxtDe.BuildBackupPanelBkLabel; case "fr": return TxtFr.BuildBackupPanelBkLabel; default: return TxtCn.BuildBackupPanelBkLabel; } } }
        public static string BtnBackupNowText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnBackupNowText; case "en": return TxtEn.BtnBackupNowText; case "ja": return TxtJa.BtnBackupNowText; case "ko": return TxtKo.BtnBackupNowText; case "es": return TxtEs.BtnBackupNowText; case "pt": return TxtPt.BtnBackupNowText; case "ru": return TxtRu.BtnBackupNowText; case "de": return TxtDe.BtnBackupNowText; case "fr": return TxtFr.BtnBackupNowText; default: return TxtCn.BtnBackupNowText; } } }
        public static string BuildBackupPanelDoBackup { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelDoBackup; case "en": return TxtEn.BuildBackupPanelDoBackup; case "ja": return TxtJa.BuildBackupPanelDoBackup; case "ko": return TxtKo.BuildBackupPanelDoBackup; case "es": return TxtEs.BuildBackupPanelDoBackup; case "pt": return TxtPt.BuildBackupPanelDoBackup; case "ru": return TxtRu.BuildBackupPanelDoBackup; case "de": return TxtDe.BuildBackupPanelDoBackup; case "fr": return TxtFr.BuildBackupPanelDoBackup; default: return TxtCn.BuildBackupPanelDoBackup; } } }
        public static string BtnRestoreBackupText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnRestoreBackupText; case "en": return TxtEn.BtnRestoreBackupText; case "ja": return TxtJa.BtnRestoreBackupText; case "ko": return TxtKo.BtnRestoreBackupText; case "es": return TxtEs.BtnRestoreBackupText; case "pt": return TxtPt.BtnRestoreBackupText; case "ru": return TxtRu.BtnRestoreBackupText; case "de": return TxtDe.BtnRestoreBackupText; case "fr": return TxtFr.BtnRestoreBackupText; default: return TxtCn.BtnRestoreBackupText; } } }
        public static string BuildBackupPanelShow { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelShow; case "en": return TxtEn.BuildBackupPanelShow; case "ja": return TxtJa.BuildBackupPanelShow; case "ko": return TxtKo.BuildBackupPanelShow; case "es": return TxtEs.BuildBackupPanelShow; case "pt": return TxtPt.BuildBackupPanelShow; case "ru": return TxtRu.BuildBackupPanelShow; case "de": return TxtDe.BuildBackupPanelShow; case "fr": return TxtFr.BuildBackupPanelShow; default: return TxtCn.BuildBackupPanelShow; } } }
        public static string BtnDeleteBackupText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnDeleteBackupText; case "en": return TxtEn.BtnDeleteBackupText; case "ja": return TxtJa.BtnDeleteBackupText; case "ko": return TxtKo.BtnDeleteBackupText; case "es": return TxtEs.BtnDeleteBackupText; case "pt": return TxtPt.BtnDeleteBackupText; case "ru": return TxtRu.BtnDeleteBackupText; case "de": return TxtDe.BtnDeleteBackupText; case "fr": return TxtFr.BtnDeleteBackupText; default: return TxtCn.BtnDeleteBackupText; } } }
        public static string BuildBackupPanelShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelShow2; case "en": return TxtEn.BuildBackupPanelShow2; case "ja": return TxtJa.BuildBackupPanelShow2; case "ko": return TxtKo.BuildBackupPanelShow2; case "es": return TxtEs.BuildBackupPanelShow2; case "pt": return TxtPt.BuildBackupPanelShow2; case "ru": return TxtRu.BuildBackupPanelShow2; case "de": return TxtDe.BuildBackupPanelShow2; case "fr": return TxtFr.BuildBackupPanelShow2; default: return TxtCn.BuildBackupPanelShow2; } } }
        public static string BuildBackupPanelConfirmBox { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelConfirmBox; case "en": return TxtEn.BuildBackupPanelConfirmBox; case "ja": return TxtJa.BuildBackupPanelConfirmBox; case "ko": return TxtKo.BuildBackupPanelConfirmBox; case "es": return TxtEs.BuildBackupPanelConfirmBox; case "pt": return TxtPt.BuildBackupPanelConfirmBox; case "ru": return TxtRu.BuildBackupPanelConfirmBox; case "de": return TxtDe.BuildBackupPanelConfirmBox; case "fr": return TxtFr.BuildBackupPanelConfirmBox; default: return TxtCn.BuildBackupPanelConfirmBox; } } }
        public static string BuildBackupPanelPart { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelPart; case "en": return TxtEn.BuildBackupPanelPart; case "ja": return TxtJa.BuildBackupPanelPart; case "ko": return TxtKo.BuildBackupPanelPart; case "es": return TxtEs.BuildBackupPanelPart; case "pt": return TxtPt.BuildBackupPanelPart; case "ru": return TxtRu.BuildBackupPanelPart; case "de": return TxtDe.BuildBackupPanelPart; case "fr": return TxtFr.BuildBackupPanelPart; default: return TxtCn.BuildBackupPanelPart; } } }
        public static string BuildBackupPanelShow3 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelShow3; case "en": return TxtEn.BuildBackupPanelShow3; case "ja": return TxtJa.BuildBackupPanelShow3; case "ko": return TxtKo.BuildBackupPanelShow3; case "es": return TxtEs.BuildBackupPanelShow3; case "pt": return TxtPt.BuildBackupPanelShow3; case "ru": return TxtRu.BuildBackupPanelShow3; case "de": return TxtDe.BuildBackupPanelShow3; case "fr": return TxtFr.BuildBackupPanelShow3; default: return TxtCn.BuildBackupPanelShow3; } } }
        public static string BuildBackupPanelBkLabel2 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelBkLabel2; case "en": return TxtEn.BuildBackupPanelBkLabel2; case "ja": return TxtJa.BuildBackupPanelBkLabel2; case "ko": return TxtKo.BuildBackupPanelBkLabel2; case "es": return TxtEs.BuildBackupPanelBkLabel2; case "pt": return TxtPt.BuildBackupPanelBkLabel2; case "ru": return TxtRu.BuildBackupPanelBkLabel2; case "de": return TxtDe.BuildBackupPanelBkLabel2; case "fr": return TxtFr.BuildBackupPanelBkLabel2; default: return TxtCn.BuildBackupPanelBkLabel2; } } }
        public static string BuildBackupPanelBkLabel3 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelBkLabel3; case "en": return TxtEn.BuildBackupPanelBkLabel3; case "ja": return TxtJa.BuildBackupPanelBkLabel3; case "ko": return TxtKo.BuildBackupPanelBkLabel3; case "es": return TxtEs.BuildBackupPanelBkLabel3; case "pt": return TxtPt.BuildBackupPanelBkLabel3; case "ru": return TxtRu.BuildBackupPanelBkLabel3; case "de": return TxtDe.BuildBackupPanelBkLabel3; case "fr": return TxtFr.BuildBackupPanelBkLabel3; default: return TxtCn.BuildBackupPanelBkLabel3; } } }
        public static string BuildBackupPanelBkCheck { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelBkCheck; case "en": return TxtEn.BuildBackupPanelBkCheck; case "ja": return TxtJa.BuildBackupPanelBkCheck; case "ko": return TxtKo.BuildBackupPanelBkCheck; case "es": return TxtEs.BuildBackupPanelBkCheck; case "pt": return TxtPt.BuildBackupPanelBkCheck; case "ru": return TxtRu.BuildBackupPanelBkCheck; case "de": return TxtDe.BuildBackupPanelBkCheck; case "fr": return TxtFr.BuildBackupPanelBkCheck; default: return TxtCn.BuildBackupPanelBkCheck; } } }
        public static string BuildBackupPanelBkCheck2 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelBkCheck2; case "en": return TxtEn.BuildBackupPanelBkCheck2; case "ja": return TxtJa.BuildBackupPanelBkCheck2; case "ko": return TxtKo.BuildBackupPanelBkCheck2; case "es": return TxtEs.BuildBackupPanelBkCheck2; case "pt": return TxtPt.BuildBackupPanelBkCheck2; case "ru": return TxtRu.BuildBackupPanelBkCheck2; case "de": return TxtDe.BuildBackupPanelBkCheck2; case "fr": return TxtFr.BuildBackupPanelBkCheck2; default: return TxtCn.BuildBackupPanelBkCheck2; } } }
        public static string BuildBackupPanelBkCheck3 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelBkCheck3; case "en": return TxtEn.BuildBackupPanelBkCheck3; case "ja": return TxtJa.BuildBackupPanelBkCheck3; case "ko": return TxtKo.BuildBackupPanelBkCheck3; case "es": return TxtEs.BuildBackupPanelBkCheck3; case "pt": return TxtPt.BuildBackupPanelBkCheck3; case "ru": return TxtRu.BuildBackupPanelBkCheck3; case "de": return TxtDe.BuildBackupPanelBkCheck3; case "fr": return TxtFr.BuildBackupPanelBkCheck3; default: return TxtCn.BuildBackupPanelBkCheck3; } } }
        public static string BuildBackupPanelBkLabel4 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelBkLabel4; case "en": return TxtEn.BuildBackupPanelBkLabel4; case "ja": return TxtJa.BuildBackupPanelBkLabel4; case "ko": return TxtKo.BuildBackupPanelBkLabel4; case "es": return TxtEs.BuildBackupPanelBkLabel4; case "pt": return TxtPt.BuildBackupPanelBkLabel4; case "ru": return TxtRu.BuildBackupPanelBkLabel4; case "de": return TxtDe.BuildBackupPanelBkLabel4; case "fr": return TxtFr.BuildBackupPanelBkLabel4; default: return TxtCn.BuildBackupPanelBkLabel4; } } }
        public static string BuildBackupPanelBkCheck4 { get { switch (Lang.Code) { case "tw": return TxtTw.BuildBackupPanelBkCheck4; case "en": return TxtEn.BuildBackupPanelBkCheck4; case "ja": return TxtJa.BuildBackupPanelBkCheck4; case "ko": return TxtKo.BuildBackupPanelBkCheck4; case "es": return TxtEs.BuildBackupPanelBkCheck4; case "pt": return TxtPt.BuildBackupPanelBkCheck4; case "ru": return TxtRu.BuildBackupPanelBkCheck4; case "de": return TxtDe.BuildBackupPanelBkCheck4; case "fr": return TxtFr.BuildBackupPanelBkCheck4; default: return TxtCn.BuildBackupPanelBkCheck4; } } }
        public static string LblBackupStateCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LblBackupStateCaption; case "en": return TxtEn.LblBackupStateCaption; case "ja": return TxtJa.LblBackupStateCaption; case "ko": return TxtKo.LblBackupStateCaption; case "es": return TxtEs.LblBackupStateCaption; case "pt": return TxtPt.LblBackupStateCaption; case "ru": return TxtRu.LblBackupStateCaption; case "de": return TxtDe.LblBackupStateCaption; case "fr": return TxtFr.LblBackupStateCaption; default: return TxtCn.LblBackupStateCaption; } } }
        public static string RefreshBackupListStr { get { switch (Lang.Code) { case "tw": return TxtTw.RefreshBackupListStr; case "en": return TxtEn.RefreshBackupListStr; case "ja": return TxtJa.RefreshBackupListStr; case "ko": return TxtKo.RefreshBackupListStr; case "es": return TxtEs.RefreshBackupListStr; case "pt": return TxtPt.RefreshBackupListStr; case "ru": return TxtRu.RefreshBackupListStr; case "de": return TxtDe.RefreshBackupListStr; case "fr": return TxtFr.RefreshBackupListStr; default: return TxtCn.RefreshBackupListStr; } } }
        public static string RefreshBackupListPart { get { switch (Lang.Code) { case "tw": return TxtTw.RefreshBackupListPart; case "en": return TxtEn.RefreshBackupListPart; case "ja": return TxtJa.RefreshBackupListPart; case "ko": return TxtKo.RefreshBackupListPart; case "es": return TxtEs.RefreshBackupListPart; case "pt": return TxtPt.RefreshBackupListPart; case "ru": return TxtRu.RefreshBackupListPart; case "de": return TxtDe.RefreshBackupListPart; case "fr": return TxtFr.RefreshBackupListPart; default: return TxtCn.RefreshBackupListPart; } } }
        public static string RefreshBackupListStr2 { get { switch (Lang.Code) { case "tw": return TxtTw.RefreshBackupListStr2; case "en": return TxtEn.RefreshBackupListStr2; case "ja": return TxtJa.RefreshBackupListStr2; case "ko": return TxtKo.RefreshBackupListStr2; case "es": return TxtEs.RefreshBackupListStr2; case "pt": return TxtPt.RefreshBackupListStr2; case "ru": return TxtRu.RefreshBackupListStr2; case "de": return TxtDe.RefreshBackupListStr2; case "fr": return TxtFr.RefreshBackupListStr2; default: return TxtCn.RefreshBackupListStr2; } } }
        public static string RefreshBackupListStr3 { get { switch (Lang.Code) { case "tw": return TxtTw.RefreshBackupListStr3; case "en": return TxtEn.RefreshBackupListStr3; case "ja": return TxtJa.RefreshBackupListStr3; case "ko": return TxtKo.RefreshBackupListStr3; case "es": return TxtEs.RefreshBackupListStr3; case "pt": return TxtPt.RefreshBackupListStr3; case "ru": return TxtRu.RefreshBackupListStr3; case "de": return TxtDe.RefreshBackupListStr3; case "fr": return TxtFr.RefreshBackupListStr3; default: return TxtCn.RefreshBackupListStr3; } } }
        public static string RefreshBackupListStr4 { get { switch (Lang.Code) { case "tw": return TxtTw.RefreshBackupListStr4; case "en": return TxtEn.RefreshBackupListStr4; case "ja": return TxtJa.RefreshBackupListStr4; case "ko": return TxtKo.RefreshBackupListStr4; case "es": return TxtEs.RefreshBackupListStr4; case "pt": return TxtPt.RefreshBackupListStr4; case "ru": return TxtRu.RefreshBackupListStr4; case "de": return TxtDe.RefreshBackupListStr4; case "fr": return TxtFr.RefreshBackupListStr4; default: return TxtCn.RefreshBackupListStr4; } } }
        public static string CheckSchedulesAppendServerLog { get { switch (Lang.Code) { case "tw": return TxtTw.CheckSchedulesAppendServerLog; case "en": return TxtEn.CheckSchedulesAppendServerLog; case "ja": return TxtJa.CheckSchedulesAppendServerLog; case "ko": return TxtKo.CheckSchedulesAppendServerLog; case "es": return TxtEs.CheckSchedulesAppendServerLog; case "pt": return TxtPt.CheckSchedulesAppendServerLog; case "ru": return TxtRu.CheckSchedulesAppendServerLog; case "de": return TxtDe.CheckSchedulesAppendServerLog; case "fr": return TxtFr.CheckSchedulesAppendServerLog; default: return TxtCn.CheckSchedulesAppendServerLog; } } }
        public static string CheckSchedulesAppendServerLog2 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckSchedulesAppendServerLog2; case "en": return TxtEn.CheckSchedulesAppendServerLog2; case "ja": return TxtJa.CheckSchedulesAppendServerLog2; case "ko": return TxtKo.CheckSchedulesAppendServerLog2; case "es": return TxtEs.CheckSchedulesAppendServerLog2; case "pt": return TxtPt.CheckSchedulesAppendServerLog2; case "ru": return TxtRu.CheckSchedulesAppendServerLog2; case "de": return TxtDe.CheckSchedulesAppendServerLog2; case "fr": return TxtFr.CheckSchedulesAppendServerLog2; default: return TxtCn.CheckSchedulesAppendServerLog2; } } }
        public static string CheckSchedulesAppendServerLog3 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckSchedulesAppendServerLog3; case "en": return TxtEn.CheckSchedulesAppendServerLog3; case "ja": return TxtJa.CheckSchedulesAppendServerLog3; case "ko": return TxtKo.CheckSchedulesAppendServerLog3; case "es": return TxtEs.CheckSchedulesAppendServerLog3; case "pt": return TxtPt.CheckSchedulesAppendServerLog3; case "ru": return TxtRu.CheckSchedulesAppendServerLog3; case "de": return TxtDe.CheckSchedulesAppendServerLog3; case "fr": return TxtFr.CheckSchedulesAppendServerLog3; default: return TxtCn.CheckSchedulesAppendServerLog3; } } }
        public static string CheckSchedulesAppendServerLog4 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckSchedulesAppendServerLog4; case "en": return TxtEn.CheckSchedulesAppendServerLog4; case "ja": return TxtJa.CheckSchedulesAppendServerLog4; case "ko": return TxtKo.CheckSchedulesAppendServerLog4; case "es": return TxtEs.CheckSchedulesAppendServerLog4; case "pt": return TxtPt.CheckSchedulesAppendServerLog4; case "ru": return TxtRu.CheckSchedulesAppendServerLog4; case "de": return TxtDe.CheckSchedulesAppendServerLog4; case "fr": return TxtFr.CheckSchedulesAppendServerLog4; default: return TxtCn.CheckSchedulesAppendServerLog4; } } }
        public static string ServerText2 { get { switch (Lang.Code) { case "tw": return TxtTw.ServerText2; case "en": return TxtEn.ServerText2; case "ja": return TxtJa.ServerText2; case "ko": return TxtKo.ServerText2; case "es": return TxtEs.ServerText2; case "pt": return TxtPt.ServerText2; case "ru": return TxtRu.ServerText2; case "de": return TxtDe.ServerText2; case "fr": return TxtFr.ServerText2; default: return TxtCn.ServerText2; } } }
        public static string ServerText3 { get { switch (Lang.Code) { case "tw": return TxtTw.ServerText3; case "en": return TxtEn.ServerText3; case "ja": return TxtJa.ServerText3; case "ko": return TxtKo.ServerText3; case "es": return TxtEs.ServerText3; case "pt": return TxtPt.ServerText3; case "ru": return TxtRu.ServerText3; case "de": return TxtDe.ServerText3; case "fr": return TxtFr.ServerText3; default: return TxtCn.ServerText3; } } }
        public static string TunnelText2 { get { switch (Lang.Code) { case "tw": return TxtTw.TunnelText2; case "en": return TxtEn.TunnelText2; case "ja": return TxtJa.TunnelText2; case "ko": return TxtKo.TunnelText2; case "es": return TxtEs.TunnelText2; case "pt": return TxtPt.TunnelText2; case "ru": return TxtRu.TunnelText2; case "de": return TxtDe.TunnelText2; case "fr": return TxtFr.TunnelText2; default: return TxtCn.TunnelText2; } } }
        public static string TunnelText3 { get { switch (Lang.Code) { case "tw": return TxtTw.TunnelText3; case "en": return TxtEn.TunnelText3; case "ja": return TxtJa.TunnelText3; case "ko": return TxtKo.TunnelText3; case "es": return TxtEs.TunnelText3; case "pt": return TxtPt.TunnelText3; case "ru": return TxtRu.TunnelText3; case "de": return TxtDe.TunnelText3; case "fr": return TxtFr.TunnelText3; default: return TxtCn.TunnelText3; } } }
        public static string AddrText2 { get { switch (Lang.Code) { case "tw": return TxtTw.AddrText2; case "en": return TxtEn.AddrText2; case "ja": return TxtJa.AddrText2; case "ko": return TxtKo.AddrText2; case "es": return TxtEs.AddrText2; case "pt": return TxtPt.AddrText2; case "ru": return TxtRu.AddrText2; case "de": return TxtDe.AddrText2; case "fr": return TxtFr.AddrText2; default: return TxtCn.AddrText2; } } }
        public static string AddrText3 { get { switch (Lang.Code) { case "tw": return TxtTw.AddrText3; case "en": return TxtEn.AddrText3; case "ja": return TxtJa.AddrText3; case "ko": return TxtKo.AddrText3; case "es": return TxtEs.AddrText3; case "pt": return TxtPt.AddrText3; case "ru": return TxtRu.AddrText3; case "de": return TxtDe.AddrText3; case "fr": return TxtFr.AddrText3; default: return TxtCn.AddrText3; } } }
        public static string Tt { get { switch (Lang.Code) { case "tw": return TxtTw.Tt; case "en": return TxtEn.Tt; case "ja": return TxtJa.Tt; case "ko": return TxtKo.Tt; case "es": return TxtEs.Tt; case "pt": return TxtPt.Tt; case "ru": return TxtRu.Tt; case "de": return TxtDe.Tt; case "fr": return TxtFr.Tt; default: return TxtCn.Tt; } } }
        public static string RefreshStatusStr { get { switch (Lang.Code) { case "tw": return TxtTw.RefreshStatusStr; case "en": return TxtEn.RefreshStatusStr; case "ja": return TxtJa.RefreshStatusStr; case "ko": return TxtKo.RefreshStatusStr; case "es": return TxtEs.RefreshStatusStr; case "pt": return TxtPt.RefreshStatusStr; case "ru": return TxtRu.RefreshStatusStr; case "de": return TxtDe.RefreshStatusStr; case "fr": return TxtFr.RefreshStatusStr; default: return TxtCn.RefreshStatusStr; } } }
        public static string RefreshStatusStr2 { get { switch (Lang.Code) { case "tw": return TxtTw.RefreshStatusStr2; case "en": return TxtEn.RefreshStatusStr2; case "ja": return TxtJa.RefreshStatusStr2; case "ko": return TxtKo.RefreshStatusStr2; case "es": return TxtEs.RefreshStatusStr2; case "pt": return TxtPt.RefreshStatusStr2; case "ru": return TxtRu.RefreshStatusStr2; case "de": return TxtDe.RefreshStatusStr2; case "fr": return TxtFr.RefreshStatusStr2; default: return TxtCn.RefreshStatusStr2; } } }
        public static string CopyAddrShow { get { switch (Lang.Code) { case "tw": return TxtTw.CopyAddrShow; case "en": return TxtEn.CopyAddrShow; case "ja": return TxtJa.CopyAddrShow; case "ko": return TxtKo.CopyAddrShow; case "es": return TxtEs.CopyAddrShow; case "pt": return TxtPt.CopyAddrShow; case "ru": return TxtRu.CopyAddrShow; case "de": return TxtDe.CopyAddrShow; case "fr": return TxtFr.CopyAddrShow; default: return TxtCn.CopyAddrShow; } } }
        public static string CopyAddrShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.CopyAddrShow2; case "en": return TxtEn.CopyAddrShow2; case "ja": return TxtJa.CopyAddrShow2; case "ko": return TxtKo.CopyAddrShow2; case "es": return TxtEs.CopyAddrShow2; case "pt": return TxtPt.CopyAddrShow2; case "ru": return TxtRu.CopyAddrShow2; case "de": return TxtDe.CopyAddrShow2; case "fr": return TxtFr.CopyAddrShow2; default: return TxtCn.CopyAddrShow2; } } }
        public static string CopyAddrAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.CopyAddrAppendState; case "en": return TxtEn.CopyAddrAppendState; case "ja": return TxtJa.CopyAddrAppendState; case "ko": return TxtKo.CopyAddrAppendState; case "es": return TxtEs.CopyAddrAppendState; case "pt": return TxtPt.CopyAddrAppendState; case "ru": return TxtRu.CopyAddrAppendState; case "de": return TxtDe.CopyAddrAppendState; case "fr": return TxtFr.CopyAddrAppendState; default: return TxtCn.CopyAddrAppendState; } } }
        public static string BtnCopyText2 { get { switch (Lang.Code) { case "tw": return TxtTw.BtnCopyText2; case "en": return TxtEn.BtnCopyText2; case "ja": return TxtJa.BtnCopyText2; case "ko": return TxtKo.BtnCopyText2; case "es": return TxtEs.BtnCopyText2; case "pt": return TxtPt.BtnCopyText2; case "ru": return TxtRu.BtnCopyText2; case "de": return TxtDe.BtnCopyText2; case "fr": return TxtFr.BtnCopyText2; default: return TxtCn.BtnCopyText2; } } }
        public static string OpenPcl2Show { get { switch (Lang.Code) { case "tw": return TxtTw.OpenPcl2Show; case "en": return TxtEn.OpenPcl2Show; case "ja": return TxtJa.OpenPcl2Show; case "ko": return TxtKo.OpenPcl2Show; case "es": return TxtEs.OpenPcl2Show; case "pt": return TxtPt.OpenPcl2Show; case "ru": return TxtRu.OpenPcl2Show; case "de": return TxtDe.OpenPcl2Show; case "fr": return TxtFr.OpenPcl2Show; default: return TxtCn.OpenPcl2Show; } } }
        public static string OpenPcl2Show2 { get { switch (Lang.Code) { case "tw": return TxtTw.OpenPcl2Show2; case "en": return TxtEn.OpenPcl2Show2; case "ja": return TxtJa.OpenPcl2Show2; case "ko": return TxtKo.OpenPcl2Show2; case "es": return TxtEs.OpenPcl2Show2; case "pt": return TxtPt.OpenPcl2Show2; case "ru": return TxtRu.OpenPcl2Show2; case "de": return TxtDe.OpenPcl2Show2; case "fr": return TxtFr.OpenPcl2Show2; default: return TxtCn.OpenPcl2Show2; } } }
        public static string HandleServerCrashAppendServerLog { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashAppendServerLog; case "en": return TxtEn.HandleServerCrashAppendServerLog; case "ja": return TxtJa.HandleServerCrashAppendServerLog; case "ko": return TxtKo.HandleServerCrashAppendServerLog; case "es": return TxtEs.HandleServerCrashAppendServerLog; case "pt": return TxtPt.HandleServerCrashAppendServerLog; case "ru": return TxtRu.HandleServerCrashAppendServerLog; case "de": return TxtDe.HandleServerCrashAppendServerLog; case "fr": return TxtFr.HandleServerCrashAppendServerLog; default: return TxtCn.HandleServerCrashAppendServerLog; } } }
        public static string HandleServerCrashAppendServerLog2 { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashAppendServerLog2; case "en": return TxtEn.HandleServerCrashAppendServerLog2; case "ja": return TxtJa.HandleServerCrashAppendServerLog2; case "ko": return TxtKo.HandleServerCrashAppendServerLog2; case "es": return TxtEs.HandleServerCrashAppendServerLog2; case "pt": return TxtPt.HandleServerCrashAppendServerLog2; case "ru": return TxtRu.HandleServerCrashAppendServerLog2; case "de": return TxtDe.HandleServerCrashAppendServerLog2; case "fr": return TxtFr.HandleServerCrashAppendServerLog2; default: return TxtCn.HandleServerCrashAppendServerLog2; } } }
        public static string HandleServerCrashStr { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashStr; case "en": return TxtEn.HandleServerCrashStr; case "ja": return TxtJa.HandleServerCrashStr; case "ko": return TxtKo.HandleServerCrashStr; case "es": return TxtEs.HandleServerCrashStr; case "pt": return TxtPt.HandleServerCrashStr; case "ru": return TxtRu.HandleServerCrashStr; case "de": return TxtDe.HandleServerCrashStr; case "fr": return TxtFr.HandleServerCrashStr; default: return TxtCn.HandleServerCrashStr; } } }
        public static string HandleServerCrashAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashAppendState; case "en": return TxtEn.HandleServerCrashAppendState; case "ja": return TxtJa.HandleServerCrashAppendState; case "ko": return TxtKo.HandleServerCrashAppendState; case "es": return TxtEs.HandleServerCrashAppendState; case "pt": return TxtPt.HandleServerCrashAppendState; case "ru": return TxtRu.HandleServerCrashAppendState; case "de": return TxtDe.HandleServerCrashAppendState; case "fr": return TxtFr.HandleServerCrashAppendState; default: return TxtCn.HandleServerCrashAppendState; } } }
        public static string HandleServerCrashStr2 { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashStr2; case "en": return TxtEn.HandleServerCrashStr2; case "ja": return TxtJa.HandleServerCrashStr2; case "ko": return TxtKo.HandleServerCrashStr2; case "es": return TxtEs.HandleServerCrashStr2; case "pt": return TxtPt.HandleServerCrashStr2; case "ru": return TxtRu.HandleServerCrashStr2; case "de": return TxtDe.HandleServerCrashStr2; case "fr": return TxtFr.HandleServerCrashStr2; default: return TxtCn.HandleServerCrashStr2; } } }
        public static string HandleServerCrashStr3 { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashStr3; case "en": return TxtEn.HandleServerCrashStr3; case "ja": return TxtJa.HandleServerCrashStr3; case "ko": return TxtKo.HandleServerCrashStr3; case "es": return TxtEs.HandleServerCrashStr3; case "pt": return TxtPt.HandleServerCrashStr3; case "ru": return TxtRu.HandleServerCrashStr3; case "de": return TxtDe.HandleServerCrashStr3; case "fr": return TxtFr.HandleServerCrashStr3; default: return TxtCn.HandleServerCrashStr3; } } }
        public static string HandleServerCrashPart { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashPart; case "en": return TxtEn.HandleServerCrashPart; case "ja": return TxtJa.HandleServerCrashPart; case "ko": return TxtKo.HandleServerCrashPart; case "es": return TxtEs.HandleServerCrashPart; case "pt": return TxtPt.HandleServerCrashPart; case "ru": return TxtRu.HandleServerCrashPart; case "de": return TxtDe.HandleServerCrashPart; case "fr": return TxtFr.HandleServerCrashPart; default: return TxtCn.HandleServerCrashPart; } } }
        public static string HandleServerCrashAppendServerLog3 { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashAppendServerLog3; case "en": return TxtEn.HandleServerCrashAppendServerLog3; case "ja": return TxtJa.HandleServerCrashAppendServerLog3; case "ko": return TxtKo.HandleServerCrashAppendServerLog3; case "es": return TxtEs.HandleServerCrashAppendServerLog3; case "pt": return TxtPt.HandleServerCrashAppendServerLog3; case "ru": return TxtRu.HandleServerCrashAppendServerLog3; case "de": return TxtDe.HandleServerCrashAppendServerLog3; case "fr": return TxtFr.HandleServerCrashAppendServerLog3; default: return TxtCn.HandleServerCrashAppendServerLog3; } } }
        public static string HandleServerCrashAppendServerLog4 { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashAppendServerLog4; case "en": return TxtEn.HandleServerCrashAppendServerLog4; case "ja": return TxtJa.HandleServerCrashAppendServerLog4; case "ko": return TxtKo.HandleServerCrashAppendServerLog4; case "es": return TxtEs.HandleServerCrashAppendServerLog4; case "pt": return TxtPt.HandleServerCrashAppendServerLog4; case "ru": return TxtRu.HandleServerCrashAppendServerLog4; case "de": return TxtDe.HandleServerCrashAppendServerLog4; case "fr": return TxtFr.HandleServerCrashAppendServerLog4; default: return TxtCn.HandleServerCrashAppendServerLog4; } } }
        public static string HandleServerCrashAppendState2 { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashAppendState2; case "en": return TxtEn.HandleServerCrashAppendState2; case "ja": return TxtJa.HandleServerCrashAppendState2; case "ko": return TxtKo.HandleServerCrashAppendState2; case "es": return TxtEs.HandleServerCrashAppendState2; case "pt": return TxtPt.HandleServerCrashAppendState2; case "ru": return TxtRu.HandleServerCrashAppendState2; case "de": return TxtDe.HandleServerCrashAppendState2; case "fr": return TxtFr.HandleServerCrashAppendState2; default: return TxtCn.HandleServerCrashAppendState2; } } }
        public static string HandleServerCrashStr4 { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashStr4; case "en": return TxtEn.HandleServerCrashStr4; case "ja": return TxtJa.HandleServerCrashStr4; case "ko": return TxtKo.HandleServerCrashStr4; case "es": return TxtEs.HandleServerCrashStr4; case "pt": return TxtPt.HandleServerCrashStr4; case "ru": return TxtRu.HandleServerCrashStr4; case "de": return TxtDe.HandleServerCrashStr4; case "fr": return TxtFr.HandleServerCrashStr4; default: return TxtCn.HandleServerCrashStr4; } } }
        public static string HandleServerCrashAppendState3 { get { switch (Lang.Code) { case "tw": return TxtTw.HandleServerCrashAppendState3; case "en": return TxtEn.HandleServerCrashAppendState3; case "ja": return TxtJa.HandleServerCrashAppendState3; case "ko": return TxtKo.HandleServerCrashAppendState3; case "es": return TxtEs.HandleServerCrashAppendState3; case "pt": return TxtPt.HandleServerCrashAppendState3; case "ru": return TxtRu.HandleServerCrashAppendState3; case "de": return TxtDe.HandleServerCrashAppendState3; case "fr": return TxtFr.HandleServerCrashAppendState3; default: return TxtCn.HandleServerCrashAppendState3; } } }
        public static string GuessCrash614Ret { get { switch (Lang.Code) { case "tw": return TxtTw.GuessCrash614Ret; case "en": return TxtEn.GuessCrash614Ret; case "ja": return TxtJa.GuessCrash614Ret; case "ko": return TxtKo.GuessCrash614Ret; case "es": return TxtEs.GuessCrash614Ret; case "pt": return TxtPt.GuessCrash614Ret; case "ru": return TxtRu.GuessCrash614Ret; case "de": return TxtDe.GuessCrash614Ret; case "fr": return TxtFr.GuessCrash614Ret; default: return TxtCn.GuessCrash614Ret; } } }
        public static string GuessCrash614Part { get { switch (Lang.Code) { case "tw": return TxtTw.GuessCrash614Part; case "en": return TxtEn.GuessCrash614Part; case "ja": return TxtJa.GuessCrash614Part; case "ko": return TxtKo.GuessCrash614Part; case "es": return TxtEs.GuessCrash614Part; case "pt": return TxtPt.GuessCrash614Part; case "ru": return TxtRu.GuessCrash614Part; case "de": return TxtDe.GuessCrash614Part; case "fr": return TxtFr.GuessCrash614Part; default: return TxtCn.GuessCrash614Part; } } }
        public static string GuessCrash614Ret2 { get { switch (Lang.Code) { case "tw": return TxtTw.GuessCrash614Ret2; case "en": return TxtEn.GuessCrash614Ret2; case "ja": return TxtJa.GuessCrash614Ret2; case "ko": return TxtKo.GuessCrash614Ret2; case "es": return TxtEs.GuessCrash614Ret2; case "pt": return TxtPt.GuessCrash614Ret2; case "ru": return TxtRu.GuessCrash614Ret2; case "de": return TxtDe.GuessCrash614Ret2; case "fr": return TxtFr.GuessCrash614Ret2; default: return TxtCn.GuessCrash614Ret2; } } }
        public static string GuessCrash614Ret3 { get { switch (Lang.Code) { case "tw": return TxtTw.GuessCrash614Ret3; case "en": return TxtEn.GuessCrash614Ret3; case "ja": return TxtJa.GuessCrash614Ret3; case "ko": return TxtKo.GuessCrash614Ret3; case "es": return TxtEs.GuessCrash614Ret3; case "pt": return TxtPt.GuessCrash614Ret3; case "ru": return TxtRu.GuessCrash614Ret3; case "de": return TxtDe.GuessCrash614Ret3; case "fr": return TxtFr.GuessCrash614Ret3; default: return TxtCn.GuessCrash614Ret3; } } }
        public static string GuessCrash614Ret4 { get { switch (Lang.Code) { case "tw": return TxtTw.GuessCrash614Ret4; case "en": return TxtEn.GuessCrash614Ret4; case "ja": return TxtJa.GuessCrash614Ret4; case "ko": return TxtKo.GuessCrash614Ret4; case "es": return TxtEs.GuessCrash614Ret4; case "pt": return TxtPt.GuessCrash614Ret4; case "ru": return TxtRu.GuessCrash614Ret4; case "de": return TxtDe.GuessCrash614Ret4; case "fr": return TxtFr.GuessCrash614Ret4; default: return TxtCn.GuessCrash614Ret4; } } }
        public static string GuessCrash614Ret5 { get { switch (Lang.Code) { case "tw": return TxtTw.GuessCrash614Ret5; case "en": return TxtEn.GuessCrash614Ret5; case "ja": return TxtJa.GuessCrash614Ret5; case "ko": return TxtKo.GuessCrash614Ret5; case "es": return TxtEs.GuessCrash614Ret5; case "pt": return TxtPt.GuessCrash614Ret5; case "ru": return TxtRu.GuessCrash614Ret5; case "de": return TxtDe.GuessCrash614Ret5; case "fr": return TxtFr.GuessCrash614Ret5; default: return TxtCn.GuessCrash614Ret5; } } }
        public static string GuessCrash614Ret6 { get { switch (Lang.Code) { case "tw": return TxtTw.GuessCrash614Ret6; case "en": return TxtEn.GuessCrash614Ret6; case "ja": return TxtJa.GuessCrash614Ret6; case "ko": return TxtKo.GuessCrash614Ret6; case "es": return TxtEs.GuessCrash614Ret6; case "pt": return TxtPt.GuessCrash614Ret6; case "ru": return TxtRu.GuessCrash614Ret6; case "de": return TxtDe.GuessCrash614Ret6; case "fr": return TxtFr.GuessCrash614Ret6; default: return TxtCn.GuessCrash614Ret6; } } }
        public static string OfferWizardIfNeededShow { get { switch (Lang.Code) { case "tw": return TxtTw.OfferWizardIfNeededShow; case "en": return TxtEn.OfferWizardIfNeededShow; case "ja": return TxtJa.OfferWizardIfNeededShow; case "ko": return TxtKo.OfferWizardIfNeededShow; case "es": return TxtEs.OfferWizardIfNeededShow; case "pt": return TxtPt.OfferWizardIfNeededShow; case "ru": return TxtRu.OfferWizardIfNeededShow; case "de": return TxtDe.OfferWizardIfNeededShow; case "fr": return TxtFr.OfferWizardIfNeededShow; default: return TxtCn.OfferWizardIfNeededShow; } } }
        public static string OfferWizardIfNeededShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.OfferWizardIfNeededShow2; case "en": return TxtEn.OfferWizardIfNeededShow2; case "ja": return TxtJa.OfferWizardIfNeededShow2; case "ko": return TxtKo.OfferWizardIfNeededShow2; case "es": return TxtEs.OfferWizardIfNeededShow2; case "pt": return TxtPt.OfferWizardIfNeededShow2; case "ru": return TxtRu.OfferWizardIfNeededShow2; case "de": return TxtDe.OfferWizardIfNeededShow2; case "fr": return TxtFr.OfferWizardIfNeededShow2; default: return TxtCn.OfferWizardIfNeededShow2; } } }
        public static string OfferWizardIfNeededShow3 { get { switch (Lang.Code) { case "tw": return TxtTw.OfferWizardIfNeededShow3; case "en": return TxtEn.OfferWizardIfNeededShow3; case "ja": return TxtJa.OfferWizardIfNeededShow3; case "ko": return TxtKo.OfferWizardIfNeededShow3; case "es": return TxtEs.OfferWizardIfNeededShow3; case "pt": return TxtPt.OfferWizardIfNeededShow3; case "ru": return TxtRu.OfferWizardIfNeededShow3; case "de": return TxtDe.OfferWizardIfNeededShow3; case "fr": return TxtFr.OfferWizardIfNeededShow3; default: return TxtCn.OfferWizardIfNeededShow3; } } }
        public static string OpenWizardAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.OpenWizardAppendState; case "en": return TxtEn.OpenWizardAppendState; case "ja": return TxtJa.OpenWizardAppendState; case "ko": return TxtKo.OpenWizardAppendState; case "es": return TxtEs.OpenWizardAppendState; case "pt": return TxtPt.OpenWizardAppendState; case "ru": return TxtRu.OpenWizardAppendState; case "de": return TxtDe.OpenWizardAppendState; case "fr": return TxtFr.OpenWizardAppendState; default: return TxtCn.OpenWizardAppendState; } } }
        public static string CheckJavaVersionStr { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionStr; case "en": return TxtEn.CheckJavaVersionStr; case "ja": return TxtJa.CheckJavaVersionStr; case "ko": return TxtKo.CheckJavaVersionStr; case "es": return TxtEs.CheckJavaVersionStr; case "pt": return TxtPt.CheckJavaVersionStr; case "ru": return TxtRu.CheckJavaVersionStr; case "de": return TxtDe.CheckJavaVersionStr; case "fr": return TxtFr.CheckJavaVersionStr; default: return TxtCn.CheckJavaVersionStr; } } }
        public static string CheckJavaVersionAppendServerLog { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog; case "en": return TxtEn.CheckJavaVersionAppendServerLog; case "ja": return TxtJa.CheckJavaVersionAppendServerLog; case "ko": return TxtKo.CheckJavaVersionAppendServerLog; case "es": return TxtEs.CheckJavaVersionAppendServerLog; case "pt": return TxtPt.CheckJavaVersionAppendServerLog; case "ru": return TxtRu.CheckJavaVersionAppendServerLog; case "de": return TxtDe.CheckJavaVersionAppendServerLog; case "fr": return TxtFr.CheckJavaVersionAppendServerLog; default: return TxtCn.CheckJavaVersionAppendServerLog; } } }
        public static string CheckJavaVersionAppendServerLog2 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog2; case "en": return TxtEn.CheckJavaVersionAppendServerLog2; case "ja": return TxtJa.CheckJavaVersionAppendServerLog2; case "ko": return TxtKo.CheckJavaVersionAppendServerLog2; case "es": return TxtEs.CheckJavaVersionAppendServerLog2; case "pt": return TxtPt.CheckJavaVersionAppendServerLog2; case "ru": return TxtRu.CheckJavaVersionAppendServerLog2; case "de": return TxtDe.CheckJavaVersionAppendServerLog2; case "fr": return TxtFr.CheckJavaVersionAppendServerLog2; default: return TxtCn.CheckJavaVersionAppendServerLog2; } } }
        public static string CheckJavaVersionAppendServerLog3 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog3; case "en": return TxtEn.CheckJavaVersionAppendServerLog3; case "ja": return TxtJa.CheckJavaVersionAppendServerLog3; case "ko": return TxtKo.CheckJavaVersionAppendServerLog3; case "es": return TxtEs.CheckJavaVersionAppendServerLog3; case "pt": return TxtPt.CheckJavaVersionAppendServerLog3; case "ru": return TxtRu.CheckJavaVersionAppendServerLog3; case "de": return TxtDe.CheckJavaVersionAppendServerLog3; case "fr": return TxtFr.CheckJavaVersionAppendServerLog3; default: return TxtCn.CheckJavaVersionAppendServerLog3; } } }
        public static string CheckJavaVersionAppendServerLog4 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog4; case "en": return TxtEn.CheckJavaVersionAppendServerLog4; case "ja": return TxtJa.CheckJavaVersionAppendServerLog4; case "ko": return TxtKo.CheckJavaVersionAppendServerLog4; case "es": return TxtEs.CheckJavaVersionAppendServerLog4; case "pt": return TxtPt.CheckJavaVersionAppendServerLog4; case "ru": return TxtRu.CheckJavaVersionAppendServerLog4; case "de": return TxtDe.CheckJavaVersionAppendServerLog4; case "fr": return TxtFr.CheckJavaVersionAppendServerLog4; default: return TxtCn.CheckJavaVersionAppendServerLog4; } } }
        public static string CheckJavaVersionAppendServerLog5 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog5; case "en": return TxtEn.CheckJavaVersionAppendServerLog5; case "ja": return TxtJa.CheckJavaVersionAppendServerLog5; case "ko": return TxtKo.CheckJavaVersionAppendServerLog5; case "es": return TxtEs.CheckJavaVersionAppendServerLog5; case "pt": return TxtPt.CheckJavaVersionAppendServerLog5; case "ru": return TxtRu.CheckJavaVersionAppendServerLog5; case "de": return TxtDe.CheckJavaVersionAppendServerLog5; case "fr": return TxtFr.CheckJavaVersionAppendServerLog5; default: return TxtCn.CheckJavaVersionAppendServerLog5; } } }
        public static string CheckJavaVersionAppendServerLog6 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog6; case "en": return TxtEn.CheckJavaVersionAppendServerLog6; case "ja": return TxtJa.CheckJavaVersionAppendServerLog6; case "ko": return TxtKo.CheckJavaVersionAppendServerLog6; case "es": return TxtEs.CheckJavaVersionAppendServerLog6; case "pt": return TxtPt.CheckJavaVersionAppendServerLog6; case "ru": return TxtRu.CheckJavaVersionAppendServerLog6; case "de": return TxtDe.CheckJavaVersionAppendServerLog6; case "fr": return TxtFr.CheckJavaVersionAppendServerLog6; default: return TxtCn.CheckJavaVersionAppendServerLog6; } } }
        public static string CheckJavaVersionAppendServerLog7 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog7; case "en": return TxtEn.CheckJavaVersionAppendServerLog7; case "ja": return TxtJa.CheckJavaVersionAppendServerLog7; case "ko": return TxtKo.CheckJavaVersionAppendServerLog7; case "es": return TxtEs.CheckJavaVersionAppendServerLog7; case "pt": return TxtPt.CheckJavaVersionAppendServerLog7; case "ru": return TxtRu.CheckJavaVersionAppendServerLog7; case "de": return TxtDe.CheckJavaVersionAppendServerLog7; case "fr": return TxtFr.CheckJavaVersionAppendServerLog7; default: return TxtCn.CheckJavaVersionAppendServerLog7; } } }
        public static string CheckJavaVersionAppendServerLog8 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog8; case "en": return TxtEn.CheckJavaVersionAppendServerLog8; case "ja": return TxtJa.CheckJavaVersionAppendServerLog8; case "ko": return TxtKo.CheckJavaVersionAppendServerLog8; case "es": return TxtEs.CheckJavaVersionAppendServerLog8; case "pt": return TxtPt.CheckJavaVersionAppendServerLog8; case "ru": return TxtRu.CheckJavaVersionAppendServerLog8; case "de": return TxtDe.CheckJavaVersionAppendServerLog8; case "fr": return TxtFr.CheckJavaVersionAppendServerLog8; default: return TxtCn.CheckJavaVersionAppendServerLog8; } } }
        public static string CheckJavaVersionAppendServerLog9 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog9; case "en": return TxtEn.CheckJavaVersionAppendServerLog9; case "ja": return TxtJa.CheckJavaVersionAppendServerLog9; case "ko": return TxtKo.CheckJavaVersionAppendServerLog9; case "es": return TxtEs.CheckJavaVersionAppendServerLog9; case "pt": return TxtPt.CheckJavaVersionAppendServerLog9; case "ru": return TxtRu.CheckJavaVersionAppendServerLog9; case "de": return TxtDe.CheckJavaVersionAppendServerLog9; case "fr": return TxtFr.CheckJavaVersionAppendServerLog9; default: return TxtCn.CheckJavaVersionAppendServerLog9; } } }
        public static string CheckJavaVersionAppendServerLog10 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog10; case "en": return TxtEn.CheckJavaVersionAppendServerLog10; case "ja": return TxtJa.CheckJavaVersionAppendServerLog10; case "ko": return TxtKo.CheckJavaVersionAppendServerLog10; case "es": return TxtEs.CheckJavaVersionAppendServerLog10; case "pt": return TxtPt.CheckJavaVersionAppendServerLog10; case "ru": return TxtRu.CheckJavaVersionAppendServerLog10; case "de": return TxtDe.CheckJavaVersionAppendServerLog10; case "fr": return TxtFr.CheckJavaVersionAppendServerLog10; default: return TxtCn.CheckJavaVersionAppendServerLog10; } } }
        public static string CheckJavaVersionAppendServerLog11 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog11; case "en": return TxtEn.CheckJavaVersionAppendServerLog11; case "ja": return TxtJa.CheckJavaVersionAppendServerLog11; case "ko": return TxtKo.CheckJavaVersionAppendServerLog11; case "es": return TxtEs.CheckJavaVersionAppendServerLog11; case "pt": return TxtPt.CheckJavaVersionAppendServerLog11; case "ru": return TxtRu.CheckJavaVersionAppendServerLog11; case "de": return TxtDe.CheckJavaVersionAppendServerLog11; case "fr": return TxtFr.CheckJavaVersionAppendServerLog11; default: return TxtCn.CheckJavaVersionAppendServerLog11; } } }
        public static string Msg2 { get { switch (Lang.Code) { case "tw": return TxtTw.Msg2; case "en": return TxtEn.Msg2; case "ja": return TxtJa.Msg2; case "ko": return TxtKo.Msg2; case "es": return TxtEs.Msg2; case "pt": return TxtPt.Msg2; case "ru": return TxtRu.Msg2; case "de": return TxtDe.Msg2; case "fr": return TxtFr.Msg2; default: return TxtCn.Msg2; } } }
        public static string CheckJavaVersionPart { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionPart; case "en": return TxtEn.CheckJavaVersionPart; case "ja": return TxtJa.CheckJavaVersionPart; case "ko": return TxtKo.CheckJavaVersionPart; case "es": return TxtEs.CheckJavaVersionPart; case "pt": return TxtPt.CheckJavaVersionPart; case "ru": return TxtRu.CheckJavaVersionPart; case "de": return TxtDe.CheckJavaVersionPart; case "fr": return TxtFr.CheckJavaVersionPart; default: return TxtCn.CheckJavaVersionPart; } } }
        public static string CheckJavaVersionPart2 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionPart2; case "en": return TxtEn.CheckJavaVersionPart2; case "ja": return TxtJa.CheckJavaVersionPart2; case "ko": return TxtKo.CheckJavaVersionPart2; case "es": return TxtEs.CheckJavaVersionPart2; case "pt": return TxtPt.CheckJavaVersionPart2; case "ru": return TxtRu.CheckJavaVersionPart2; case "de": return TxtDe.CheckJavaVersionPart2; case "fr": return TxtFr.CheckJavaVersionPart2; default: return TxtCn.CheckJavaVersionPart2; } } }
        public static string CheckJavaVersionPart3 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionPart3; case "en": return TxtEn.CheckJavaVersionPart3; case "ja": return TxtJa.CheckJavaVersionPart3; case "ko": return TxtKo.CheckJavaVersionPart3; case "es": return TxtEs.CheckJavaVersionPart3; case "pt": return TxtPt.CheckJavaVersionPart3; case "ru": return TxtRu.CheckJavaVersionPart3; case "de": return TxtDe.CheckJavaVersionPart3; case "fr": return TxtFr.CheckJavaVersionPart3; default: return TxtCn.CheckJavaVersionPart3; } } }
        public static string CheckJavaVersionPart4 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionPart4; case "en": return TxtEn.CheckJavaVersionPart4; case "ja": return TxtJa.CheckJavaVersionPart4; case "ko": return TxtKo.CheckJavaVersionPart4; case "es": return TxtEs.CheckJavaVersionPart4; case "pt": return TxtPt.CheckJavaVersionPart4; case "ru": return TxtRu.CheckJavaVersionPart4; case "de": return TxtDe.CheckJavaVersionPart4; case "fr": return TxtFr.CheckJavaVersionPart4; default: return TxtCn.CheckJavaVersionPart4; } } }
        public static string CheckJavaVersionShow { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionShow; case "en": return TxtEn.CheckJavaVersionShow; case "ja": return TxtJa.CheckJavaVersionShow; case "ko": return TxtKo.CheckJavaVersionShow; case "es": return TxtEs.CheckJavaVersionShow; case "pt": return TxtPt.CheckJavaVersionShow; case "ru": return TxtRu.CheckJavaVersionShow; case "de": return TxtDe.CheckJavaVersionShow; case "fr": return TxtFr.CheckJavaVersionShow; default: return TxtCn.CheckJavaVersionShow; } } }
        public static string CheckJavaVersionShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionShow2; case "en": return TxtEn.CheckJavaVersionShow2; case "ja": return TxtJa.CheckJavaVersionShow2; case "ko": return TxtKo.CheckJavaVersionShow2; case "es": return TxtEs.CheckJavaVersionShow2; case "pt": return TxtPt.CheckJavaVersionShow2; case "ru": return TxtRu.CheckJavaVersionShow2; case "de": return TxtDe.CheckJavaVersionShow2; case "fr": return TxtFr.CheckJavaVersionShow2; default: return TxtCn.CheckJavaVersionShow2; } } }
        public static string CheckJavaVersionShow3 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionShow3; case "en": return TxtEn.CheckJavaVersionShow3; case "ja": return TxtJa.CheckJavaVersionShow3; case "ko": return TxtKo.CheckJavaVersionShow3; case "es": return TxtEs.CheckJavaVersionShow3; case "pt": return TxtPt.CheckJavaVersionShow3; case "ru": return TxtRu.CheckJavaVersionShow3; case "de": return TxtDe.CheckJavaVersionShow3; case "fr": return TxtFr.CheckJavaVersionShow3; default: return TxtCn.CheckJavaVersionShow3; } } }
        public static string CheckJavaVersionAppendServerLog12 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog12; case "en": return TxtEn.CheckJavaVersionAppendServerLog12; case "ja": return TxtJa.CheckJavaVersionAppendServerLog12; case "ko": return TxtKo.CheckJavaVersionAppendServerLog12; case "es": return TxtEs.CheckJavaVersionAppendServerLog12; case "pt": return TxtPt.CheckJavaVersionAppendServerLog12; case "ru": return TxtRu.CheckJavaVersionAppendServerLog12; case "de": return TxtDe.CheckJavaVersionAppendServerLog12; case "fr": return TxtFr.CheckJavaVersionAppendServerLog12; default: return TxtCn.CheckJavaVersionAppendServerLog12; } } }
        public static string CheckJavaVersionAppendServerLog13 { get { switch (Lang.Code) { case "tw": return TxtTw.CheckJavaVersionAppendServerLog13; case "en": return TxtEn.CheckJavaVersionAppendServerLog13; case "ja": return TxtJa.CheckJavaVersionAppendServerLog13; case "ko": return TxtKo.CheckJavaVersionAppendServerLog13; case "es": return TxtEs.CheckJavaVersionAppendServerLog13; case "pt": return TxtPt.CheckJavaVersionAppendServerLog13; case "ru": return TxtRu.CheckJavaVersionAppendServerLog13; case "de": return TxtDe.CheckJavaVersionAppendServerLog13; case "fr": return TxtFr.CheckJavaVersionAppendServerLog13; default: return TxtCn.CheckJavaVersionAppendServerLog13; } } }
        public static string StartAllAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.StartAllAppendState; case "en": return TxtEn.StartAllAppendState; case "ja": return TxtJa.StartAllAppendState; case "ko": return TxtKo.StartAllAppendState; case "es": return TxtEs.StartAllAppendState; case "pt": return TxtPt.StartAllAppendState; case "ru": return TxtRu.StartAllAppendState; case "de": return TxtDe.StartAllAppendState; case "fr": return TxtFr.StartAllAppendState; default: return TxtCn.StartAllAppendState; } } }
        public static string BtnStartText3 { get { switch (Lang.Code) { case "tw": return TxtTw.BtnStartText3; case "en": return TxtEn.BtnStartText3; case "ja": return TxtJa.BtnStartText3; case "ko": return TxtKo.BtnStartText3; case "es": return TxtEs.BtnStartText3; case "pt": return TxtPt.BtnStartText3; case "ru": return TxtRu.BtnStartText3; case "de": return TxtDe.BtnStartText3; case "fr": return TxtFr.BtnStartText3; default: return TxtCn.BtnStartText3; } } }
        public static string BtnStartText4 { get { switch (Lang.Code) { case "tw": return TxtTw.BtnStartText4; case "en": return TxtEn.BtnStartText4; case "ja": return TxtJa.BtnStartText4; case "ko": return TxtKo.BtnStartText4; case "es": return TxtEs.BtnStartText4; case "pt": return TxtPt.BtnStartText4; case "ru": return TxtRu.BtnStartText4; case "de": return TxtDe.BtnStartText4; case "fr": return TxtFr.BtnStartText4; default: return TxtCn.BtnStartText4; } } }
        public static string DoStartFailState { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState; case "en": return TxtEn.DoStartFailState; case "ja": return TxtJa.DoStartFailState; case "ko": return TxtKo.DoStartFailState; case "es": return TxtEs.DoStartFailState; case "pt": return TxtPt.DoStartFailState; case "ru": return TxtRu.DoStartFailState; case "de": return TxtDe.DoStartFailState; case "fr": return TxtFr.DoStartFailState; default: return TxtCn.DoStartFailState; } } }
        public static string DoStartFailState2 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState2; case "en": return TxtEn.DoStartFailState2; case "ja": return TxtJa.DoStartFailState2; case "ko": return TxtKo.DoStartFailState2; case "es": return TxtEs.DoStartFailState2; case "pt": return TxtPt.DoStartFailState2; case "ru": return TxtRu.DoStartFailState2; case "de": return TxtDe.DoStartFailState2; case "fr": return TxtFr.DoStartFailState2; default: return TxtCn.DoStartFailState2; } } }
        public static string DoStartFailState3 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState3; case "en": return TxtEn.DoStartFailState3; case "ja": return TxtJa.DoStartFailState3; case "ko": return TxtKo.DoStartFailState3; case "es": return TxtEs.DoStartFailState3; case "pt": return TxtPt.DoStartFailState3; case "ru": return TxtRu.DoStartFailState3; case "de": return TxtDe.DoStartFailState3; case "fr": return TxtFr.DoStartFailState3; default: return TxtCn.DoStartFailState3; } } }
        public static string DoStartFailState4 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState4; case "en": return TxtEn.DoStartFailState4; case "ja": return TxtJa.DoStartFailState4; case "ko": return TxtKo.DoStartFailState4; case "es": return TxtEs.DoStartFailState4; case "pt": return TxtPt.DoStartFailState4; case "ru": return TxtRu.DoStartFailState4; case "de": return TxtDe.DoStartFailState4; case "fr": return TxtFr.DoStartFailState4; default: return TxtCn.DoStartFailState4; } } }
        public static string DoStartFailState5 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState5; case "en": return TxtEn.DoStartFailState5; case "ja": return TxtJa.DoStartFailState5; case "ko": return TxtKo.DoStartFailState5; case "es": return TxtEs.DoStartFailState5; case "pt": return TxtPt.DoStartFailState5; case "ru": return TxtRu.DoStartFailState5; case "de": return TxtDe.DoStartFailState5; case "fr": return TxtFr.DoStartFailState5; default: return TxtCn.DoStartFailState5; } } }
        public static string DoStartFailState6 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState6; case "en": return TxtEn.DoStartFailState6; case "ja": return TxtJa.DoStartFailState6; case "ko": return TxtKo.DoStartFailState6; case "es": return TxtEs.DoStartFailState6; case "pt": return TxtPt.DoStartFailState6; case "ru": return TxtRu.DoStartFailState6; case "de": return TxtDe.DoStartFailState6; case "fr": return TxtFr.DoStartFailState6; default: return TxtCn.DoStartFailState6; } } }
        public static string DoStartFailState7 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState7; case "en": return TxtEn.DoStartFailState7; case "ja": return TxtJa.DoStartFailState7; case "ko": return TxtKo.DoStartFailState7; case "es": return TxtEs.DoStartFailState7; case "pt": return TxtPt.DoStartFailState7; case "ru": return TxtRu.DoStartFailState7; case "de": return TxtDe.DoStartFailState7; case "fr": return TxtFr.DoStartFailState7; default: return TxtCn.DoStartFailState7; } } }
        public static string DoStartAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartAppendState; case "en": return TxtEn.DoStartAppendState; case "ja": return TxtJa.DoStartAppendState; case "ko": return TxtKo.DoStartAppendState; case "es": return TxtEs.DoStartAppendState; case "pt": return TxtPt.DoStartAppendState; case "ru": return TxtRu.DoStartAppendState; case "de": return TxtDe.DoStartAppendState; case "fr": return TxtFr.DoStartAppendState; default: return TxtCn.DoStartAppendState; } } }
        public static string DoStartAppendState2 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartAppendState2; case "en": return TxtEn.DoStartAppendState2; case "ja": return TxtJa.DoStartAppendState2; case "ko": return TxtKo.DoStartAppendState2; case "es": return TxtEs.DoStartAppendState2; case "pt": return TxtPt.DoStartAppendState2; case "ru": return TxtRu.DoStartAppendState2; case "de": return TxtDe.DoStartAppendState2; case "fr": return TxtFr.DoStartAppendState2; default: return TxtCn.DoStartAppendState2; } } }
        public static string DoStartAppendServerLog { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartAppendServerLog; case "en": return TxtEn.DoStartAppendServerLog; case "ja": return TxtJa.DoStartAppendServerLog; case "ko": return TxtKo.DoStartAppendServerLog; case "es": return TxtEs.DoStartAppendServerLog; case "pt": return TxtPt.DoStartAppendServerLog; case "ru": return TxtRu.DoStartAppendServerLog; case "de": return TxtDe.DoStartAppendServerLog; case "fr": return TxtFr.DoStartAppendServerLog; default: return TxtCn.DoStartAppendServerLog; } } }
        public static string DoStartFailState8 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState8; case "en": return TxtEn.DoStartFailState8; case "ja": return TxtJa.DoStartFailState8; case "ko": return TxtKo.DoStartFailState8; case "es": return TxtEs.DoStartFailState8; case "pt": return TxtPt.DoStartFailState8; case "ru": return TxtRu.DoStartFailState8; case "de": return TxtDe.DoStartFailState8; case "fr": return TxtFr.DoStartFailState8; default: return TxtCn.DoStartFailState8; } } }
        public static string DoStartAppendState3 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartAppendState3; case "en": return TxtEn.DoStartAppendState3; case "ja": return TxtJa.DoStartAppendState3; case "ko": return TxtKo.DoStartAppendState3; case "es": return TxtEs.DoStartAppendState3; case "pt": return TxtPt.DoStartAppendState3; case "ru": return TxtRu.DoStartAppendState3; case "de": return TxtDe.DoStartAppendState3; case "fr": return TxtFr.DoStartAppendState3; default: return TxtCn.DoStartAppendState3; } } }
        public static string DoStartFailState9 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState9; case "en": return TxtEn.DoStartFailState9; case "ja": return TxtJa.DoStartFailState9; case "ko": return TxtKo.DoStartFailState9; case "es": return TxtEs.DoStartFailState9; case "pt": return TxtPt.DoStartFailState9; case "ru": return TxtRu.DoStartFailState9; case "de": return TxtDe.DoStartFailState9; case "fr": return TxtFr.DoStartFailState9; default: return TxtCn.DoStartFailState9; } } }
        public static string DoStartFailState10 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState10; case "en": return TxtEn.DoStartFailState10; case "ja": return TxtJa.DoStartFailState10; case "ko": return TxtKo.DoStartFailState10; case "es": return TxtEs.DoStartFailState10; case "pt": return TxtPt.DoStartFailState10; case "ru": return TxtRu.DoStartFailState10; case "de": return TxtDe.DoStartFailState10; case "fr": return TxtFr.DoStartFailState10; default: return TxtCn.DoStartFailState10; } } }
        public static string DoStartAppendState4 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartAppendState4; case "en": return TxtEn.DoStartAppendState4; case "ja": return TxtJa.DoStartAppendState4; case "ko": return TxtKo.DoStartAppendState4; case "es": return TxtEs.DoStartAppendState4; case "pt": return TxtPt.DoStartAppendState4; case "ru": return TxtRu.DoStartAppendState4; case "de": return TxtDe.DoStartAppendState4; case "fr": return TxtFr.DoStartAppendState4; default: return TxtCn.DoStartAppendState4; } } }
        public static string DoStartFailState11 { get { switch (Lang.Code) { case "tw": return TxtTw.DoStartFailState11; case "en": return TxtEn.DoStartFailState11; case "ja": return TxtJa.DoStartFailState11; case "ko": return TxtKo.DoStartFailState11; case "es": return TxtEs.DoStartFailState11; case "pt": return TxtPt.DoStartFailState11; case "ru": return TxtRu.DoStartFailState11; case "de": return TxtDe.DoStartFailState11; case "fr": return TxtFr.DoStartFailState11; default: return TxtCn.DoStartFailState11; } } }
        public static string StartTunnelAppendTunnelLog { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog; case "en": return TxtEn.StartTunnelAppendTunnelLog; case "ja": return TxtJa.StartTunnelAppendTunnelLog; case "ko": return TxtKo.StartTunnelAppendTunnelLog; case "es": return TxtEs.StartTunnelAppendTunnelLog; case "pt": return TxtPt.StartTunnelAppendTunnelLog; case "ru": return TxtRu.StartTunnelAppendTunnelLog; case "de": return TxtDe.StartTunnelAppendTunnelLog; case "fr": return TxtFr.StartTunnelAppendTunnelLog; default: return TxtCn.StartTunnelAppendTunnelLog; } } }
        public static string StartTunnelAppendTunnelLog2 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog2; case "en": return TxtEn.StartTunnelAppendTunnelLog2; case "ja": return TxtJa.StartTunnelAppendTunnelLog2; case "ko": return TxtKo.StartTunnelAppendTunnelLog2; case "es": return TxtEs.StartTunnelAppendTunnelLog2; case "pt": return TxtPt.StartTunnelAppendTunnelLog2; case "ru": return TxtRu.StartTunnelAppendTunnelLog2; case "de": return TxtDe.StartTunnelAppendTunnelLog2; case "fr": return TxtFr.StartTunnelAppendTunnelLog2; default: return TxtCn.StartTunnelAppendTunnelLog2; } } }
        public static string StartTunnelAppendTunnelLog3 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog3; case "en": return TxtEn.StartTunnelAppendTunnelLog3; case "ja": return TxtJa.StartTunnelAppendTunnelLog3; case "ko": return TxtKo.StartTunnelAppendTunnelLog3; case "es": return TxtEs.StartTunnelAppendTunnelLog3; case "pt": return TxtPt.StartTunnelAppendTunnelLog3; case "ru": return TxtRu.StartTunnelAppendTunnelLog3; case "de": return TxtDe.StartTunnelAppendTunnelLog3; case "fr": return TxtFr.StartTunnelAppendTunnelLog3; default: return TxtCn.StartTunnelAppendTunnelLog3; } } }
        public static string StartTunnelAppendTunnelLog4 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog4; case "en": return TxtEn.StartTunnelAppendTunnelLog4; case "ja": return TxtJa.StartTunnelAppendTunnelLog4; case "ko": return TxtKo.StartTunnelAppendTunnelLog4; case "es": return TxtEs.StartTunnelAppendTunnelLog4; case "pt": return TxtPt.StartTunnelAppendTunnelLog4; case "ru": return TxtRu.StartTunnelAppendTunnelLog4; case "de": return TxtDe.StartTunnelAppendTunnelLog4; case "fr": return TxtFr.StartTunnelAppendTunnelLog4; default: return TxtCn.StartTunnelAppendTunnelLog4; } } }
        public static string StartTunnelAppendTunnelLog5 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog5; case "en": return TxtEn.StartTunnelAppendTunnelLog5; case "ja": return TxtJa.StartTunnelAppendTunnelLog5; case "ko": return TxtKo.StartTunnelAppendTunnelLog5; case "es": return TxtEs.StartTunnelAppendTunnelLog5; case "pt": return TxtPt.StartTunnelAppendTunnelLog5; case "ru": return TxtRu.StartTunnelAppendTunnelLog5; case "de": return TxtDe.StartTunnelAppendTunnelLog5; case "fr": return TxtFr.StartTunnelAppendTunnelLog5; default: return TxtCn.StartTunnelAppendTunnelLog5; } } }
        public static string StartTunnelAppendTunnelLog6 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog6; case "en": return TxtEn.StartTunnelAppendTunnelLog6; case "ja": return TxtJa.StartTunnelAppendTunnelLog6; case "ko": return TxtKo.StartTunnelAppendTunnelLog6; case "es": return TxtEs.StartTunnelAppendTunnelLog6; case "pt": return TxtPt.StartTunnelAppendTunnelLog6; case "ru": return TxtRu.StartTunnelAppendTunnelLog6; case "de": return TxtDe.StartTunnelAppendTunnelLog6; case "fr": return TxtFr.StartTunnelAppendTunnelLog6; default: return TxtCn.StartTunnelAppendTunnelLog6; } } }
        public static string StartTunnelAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendState; case "en": return TxtEn.StartTunnelAppendState; case "ja": return TxtJa.StartTunnelAppendState; case "ko": return TxtKo.StartTunnelAppendState; case "es": return TxtEs.StartTunnelAppendState; case "pt": return TxtPt.StartTunnelAppendState; case "ru": return TxtRu.StartTunnelAppendState; case "de": return TxtDe.StartTunnelAppendState; case "fr": return TxtFr.StartTunnelAppendState; default: return TxtCn.StartTunnelAppendState; } } }
        public static string StartTunnelAppendTunnelLog7 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog7; case "en": return TxtEn.StartTunnelAppendTunnelLog7; case "ja": return TxtJa.StartTunnelAppendTunnelLog7; case "ko": return TxtKo.StartTunnelAppendTunnelLog7; case "es": return TxtEs.StartTunnelAppendTunnelLog7; case "pt": return TxtPt.StartTunnelAppendTunnelLog7; case "ru": return TxtRu.StartTunnelAppendTunnelLog7; case "de": return TxtDe.StartTunnelAppendTunnelLog7; case "fr": return TxtFr.StartTunnelAppendTunnelLog7; default: return TxtCn.StartTunnelAppendTunnelLog7; } } }
        public static string StartTunnelAppendTunnelLog8 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog8; case "en": return TxtEn.StartTunnelAppendTunnelLog8; case "ja": return TxtJa.StartTunnelAppendTunnelLog8; case "ko": return TxtKo.StartTunnelAppendTunnelLog8; case "es": return TxtEs.StartTunnelAppendTunnelLog8; case "pt": return TxtPt.StartTunnelAppendTunnelLog8; case "ru": return TxtRu.StartTunnelAppendTunnelLog8; case "de": return TxtDe.StartTunnelAppendTunnelLog8; case "fr": return TxtFr.StartTunnelAppendTunnelLog8; default: return TxtCn.StartTunnelAppendTunnelLog8; } } }
        public static string StartTunnelAppendTunnelLog9 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog9; case "en": return TxtEn.StartTunnelAppendTunnelLog9; case "ja": return TxtJa.StartTunnelAppendTunnelLog9; case "ko": return TxtKo.StartTunnelAppendTunnelLog9; case "es": return TxtEs.StartTunnelAppendTunnelLog9; case "pt": return TxtPt.StartTunnelAppendTunnelLog9; case "ru": return TxtRu.StartTunnelAppendTunnelLog9; case "de": return TxtDe.StartTunnelAppendTunnelLog9; case "fr": return TxtFr.StartTunnelAppendTunnelLog9; default: return TxtCn.StartTunnelAppendTunnelLog9; } } }
        public static string StartTunnelAppendTunnelLog10 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog10; case "en": return TxtEn.StartTunnelAppendTunnelLog10; case "ja": return TxtJa.StartTunnelAppendTunnelLog10; case "ko": return TxtKo.StartTunnelAppendTunnelLog10; case "es": return TxtEs.StartTunnelAppendTunnelLog10; case "pt": return TxtPt.StartTunnelAppendTunnelLog10; case "ru": return TxtRu.StartTunnelAppendTunnelLog10; case "de": return TxtDe.StartTunnelAppendTunnelLog10; case "fr": return TxtFr.StartTunnelAppendTunnelLog10; default: return TxtCn.StartTunnelAppendTunnelLog10; } } }
        public static string StartTunnelAppendTunnelLog11 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendTunnelLog11; case "en": return TxtEn.StartTunnelAppendTunnelLog11; case "ja": return TxtJa.StartTunnelAppendTunnelLog11; case "ko": return TxtKo.StartTunnelAppendTunnelLog11; case "es": return TxtEs.StartTunnelAppendTunnelLog11; case "pt": return TxtPt.StartTunnelAppendTunnelLog11; case "ru": return TxtRu.StartTunnelAppendTunnelLog11; case "de": return TxtDe.StartTunnelAppendTunnelLog11; case "fr": return TxtFr.StartTunnelAppendTunnelLog11; default: return TxtCn.StartTunnelAppendTunnelLog11; } } }
        public static string StartTunnelAppendState2 { get { switch (Lang.Code) { case "tw": return TxtTw.StartTunnelAppendState2; case "en": return TxtEn.StartTunnelAppendState2; case "ja": return TxtJa.StartTunnelAppendState2; case "ko": return TxtKo.StartTunnelAppendState2; case "es": return TxtEs.StartTunnelAppendState2; case "pt": return TxtPt.StartTunnelAppendState2; case "ru": return TxtRu.StartTunnelAppendState2; case "de": return TxtDe.StartTunnelAppendState2; case "fr": return TxtFr.StartTunnelAppendState2; default: return TxtCn.StartTunnelAppendState2; } } }
        public static string StartVoiceTunnelAppendTunnelLog { get { switch (Lang.Code) { case "tw": return TxtTw.StartVoiceTunnelAppendTunnelLog; case "en": return TxtEn.StartVoiceTunnelAppendTunnelLog; case "ja": return TxtJa.StartVoiceTunnelAppendTunnelLog; case "ko": return TxtKo.StartVoiceTunnelAppendTunnelLog; case "es": return TxtEs.StartVoiceTunnelAppendTunnelLog; case "pt": return TxtPt.StartVoiceTunnelAppendTunnelLog; case "ru": return TxtRu.StartVoiceTunnelAppendTunnelLog; case "de": return TxtDe.StartVoiceTunnelAppendTunnelLog; case "fr": return TxtFr.StartVoiceTunnelAppendTunnelLog; default: return TxtCn.StartVoiceTunnelAppendTunnelLog; } } }
        public static string StartVoiceTunnelAppendTunnelLog2 { get { switch (Lang.Code) { case "tw": return TxtTw.StartVoiceTunnelAppendTunnelLog2; case "en": return TxtEn.StartVoiceTunnelAppendTunnelLog2; case "ja": return TxtJa.StartVoiceTunnelAppendTunnelLog2; case "ko": return TxtKo.StartVoiceTunnelAppendTunnelLog2; case "es": return TxtEs.StartVoiceTunnelAppendTunnelLog2; case "pt": return TxtPt.StartVoiceTunnelAppendTunnelLog2; case "ru": return TxtRu.StartVoiceTunnelAppendTunnelLog2; case "de": return TxtDe.StartVoiceTunnelAppendTunnelLog2; case "fr": return TxtFr.StartVoiceTunnelAppendTunnelLog2; default: return TxtCn.StartVoiceTunnelAppendTunnelLog2; } } }
        public static string StartVoiceTunnelAppendTunnelLog3 { get { switch (Lang.Code) { case "tw": return TxtTw.StartVoiceTunnelAppendTunnelLog3; case "en": return TxtEn.StartVoiceTunnelAppendTunnelLog3; case "ja": return TxtJa.StartVoiceTunnelAppendTunnelLog3; case "ko": return TxtKo.StartVoiceTunnelAppendTunnelLog3; case "es": return TxtEs.StartVoiceTunnelAppendTunnelLog3; case "pt": return TxtPt.StartVoiceTunnelAppendTunnelLog3; case "ru": return TxtRu.StartVoiceTunnelAppendTunnelLog3; case "de": return TxtDe.StartVoiceTunnelAppendTunnelLog3; case "fr": return TxtFr.StartVoiceTunnelAppendTunnelLog3; default: return TxtCn.StartVoiceTunnelAppendTunnelLog3; } } }
        public static string StartVoiceTunnelAppendTunnelLog4 { get { switch (Lang.Code) { case "tw": return TxtTw.StartVoiceTunnelAppendTunnelLog4; case "en": return TxtEn.StartVoiceTunnelAppendTunnelLog4; case "ja": return TxtJa.StartVoiceTunnelAppendTunnelLog4; case "ko": return TxtKo.StartVoiceTunnelAppendTunnelLog4; case "es": return TxtEs.StartVoiceTunnelAppendTunnelLog4; case "pt": return TxtPt.StartVoiceTunnelAppendTunnelLog4; case "ru": return TxtRu.StartVoiceTunnelAppendTunnelLog4; case "de": return TxtDe.StartVoiceTunnelAppendTunnelLog4; case "fr": return TxtFr.StartVoiceTunnelAppendTunnelLog4; default: return TxtCn.StartVoiceTunnelAppendTunnelLog4; } } }
        public static string StartVoiceTunnelAppendTunnelLog5 { get { switch (Lang.Code) { case "tw": return TxtTw.StartVoiceTunnelAppendTunnelLog5; case "en": return TxtEn.StartVoiceTunnelAppendTunnelLog5; case "ja": return TxtJa.StartVoiceTunnelAppendTunnelLog5; case "ko": return TxtKo.StartVoiceTunnelAppendTunnelLog5; case "es": return TxtEs.StartVoiceTunnelAppendTunnelLog5; case "pt": return TxtPt.StartVoiceTunnelAppendTunnelLog5; case "ru": return TxtRu.StartVoiceTunnelAppendTunnelLog5; case "de": return TxtDe.StartVoiceTunnelAppendTunnelLog5; case "fr": return TxtFr.StartVoiceTunnelAppendTunnelLog5; default: return TxtCn.StartVoiceTunnelAppendTunnelLog5; } } }
        public static string StartVoiceTunnelAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.StartVoiceTunnelAppendState; case "en": return TxtEn.StartVoiceTunnelAppendState; case "ja": return TxtJa.StartVoiceTunnelAppendState; case "ko": return TxtKo.StartVoiceTunnelAppendState; case "es": return TxtEs.StartVoiceTunnelAppendState; case "pt": return TxtPt.StartVoiceTunnelAppendState; case "ru": return TxtRu.StartVoiceTunnelAppendState; case "de": return TxtDe.StartVoiceTunnelAppendState; case "fr": return TxtFr.StartVoiceTunnelAppendState; default: return TxtCn.StartVoiceTunnelAppendState; } } }
        public static string VoiceFrpcOutputAppendTunnelLog { get { switch (Lang.Code) { case "tw": return TxtTw.VoiceFrpcOutputAppendTunnelLog; case "en": return TxtEn.VoiceFrpcOutputAppendTunnelLog; case "ja": return TxtJa.VoiceFrpcOutputAppendTunnelLog; case "ko": return TxtKo.VoiceFrpcOutputAppendTunnelLog; case "es": return TxtEs.VoiceFrpcOutputAppendTunnelLog; case "pt": return TxtPt.VoiceFrpcOutputAppendTunnelLog; case "ru": return TxtRu.VoiceFrpcOutputAppendTunnelLog; case "de": return TxtDe.VoiceFrpcOutputAppendTunnelLog; case "fr": return TxtFr.VoiceFrpcOutputAppendTunnelLog; default: return TxtCn.VoiceFrpcOutputAppendTunnelLog; } } }
        public static string VoiceFrpcErrorAppendTunnelLog { get { switch (Lang.Code) { case "tw": return TxtTw.VoiceFrpcErrorAppendTunnelLog; case "en": return TxtEn.VoiceFrpcErrorAppendTunnelLog; case "ja": return TxtJa.VoiceFrpcErrorAppendTunnelLog; case "ko": return TxtKo.VoiceFrpcErrorAppendTunnelLog; case "es": return TxtEs.VoiceFrpcErrorAppendTunnelLog; case "pt": return TxtPt.VoiceFrpcErrorAppendTunnelLog; case "ru": return TxtRu.VoiceFrpcErrorAppendTunnelLog; case "de": return TxtDe.VoiceFrpcErrorAppendTunnelLog; case "fr": return TxtFr.VoiceFrpcErrorAppendTunnelLog; default: return TxtCn.VoiceFrpcErrorAppendTunnelLog; } } }
        public static string TryParseFrpcAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.TryParseFrpcAppendState; case "en": return TxtEn.TryParseFrpcAppendState; case "ja": return TxtJa.TryParseFrpcAppendState; case "ko": return TxtKo.TryParseFrpcAppendState; case "es": return TxtEs.TryParseFrpcAppendState; case "pt": return TxtPt.TryParseFrpcAppendState; case "ru": return TxtRu.TryParseFrpcAppendState; case "de": return TxtDe.TryParseFrpcAppendState; case "fr": return TxtFr.TryParseFrpcAppendState; default: return TxtCn.TryParseFrpcAppendState; } } }
        public static string ServerText4 { get { switch (Lang.Code) { case "tw": return TxtTw.ServerText4; case "en": return TxtEn.ServerText4; case "ja": return TxtJa.ServerText4; case "ko": return TxtKo.ServerText4; case "es": return TxtEs.ServerText4; case "pt": return TxtPt.ServerText4; case "ru": return TxtRu.ServerText4; case "de": return TxtDe.ServerText4; case "fr": return TxtFr.ServerText4; default: return TxtCn.ServerText4; } } }
        public static string TunnelText4 { get { switch (Lang.Code) { case "tw": return TxtTw.TunnelText4; case "en": return TxtEn.TunnelText4; case "ja": return TxtJa.TunnelText4; case "ko": return TxtKo.TunnelText4; case "es": return TxtEs.TunnelText4; case "pt": return TxtPt.TunnelText4; case "ru": return TxtRu.TunnelText4; case "de": return TxtDe.TunnelText4; case "fr": return TxtFr.TunnelText4; default: return TxtCn.TunnelText4; } } }
        public static string SafeStopAppendState { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopAppendState; case "en": return TxtEn.SafeStopAppendState; case "ja": return TxtJa.SafeStopAppendState; case "ko": return TxtKo.SafeStopAppendState; case "es": return TxtEs.SafeStopAppendState; case "pt": return TxtPt.SafeStopAppendState; case "ru": return TxtRu.SafeStopAppendState; case "de": return TxtDe.SafeStopAppendState; case "fr": return TxtFr.SafeStopAppendState; default: return TxtCn.SafeStopAppendState; } } }
        public static string SafeStopAppendState2 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopAppendState2; case "en": return TxtEn.SafeStopAppendState2; case "ja": return TxtJa.SafeStopAppendState2; case "ko": return TxtKo.SafeStopAppendState2; case "es": return TxtEs.SafeStopAppendState2; case "pt": return TxtPt.SafeStopAppendState2; case "ru": return TxtRu.SafeStopAppendState2; case "de": return TxtDe.SafeStopAppendState2; case "fr": return TxtFr.SafeStopAppendState2; default: return TxtCn.SafeStopAppendState2; } } }
        public static string SafeStopAppendServerLog { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopAppendServerLog; case "en": return TxtEn.SafeStopAppendServerLog; case "ja": return TxtJa.SafeStopAppendServerLog; case "ko": return TxtKo.SafeStopAppendServerLog; case "es": return TxtEs.SafeStopAppendServerLog; case "pt": return TxtPt.SafeStopAppendServerLog; case "ru": return TxtRu.SafeStopAppendServerLog; case "de": return TxtDe.SafeStopAppendServerLog; case "fr": return TxtFr.SafeStopAppendServerLog; default: return TxtCn.SafeStopAppendServerLog; } } }
        public static string SafeStopAppendState3 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopAppendState3; case "en": return TxtEn.SafeStopAppendState3; case "ja": return TxtJa.SafeStopAppendState3; case "ko": return TxtKo.SafeStopAppendState3; case "es": return TxtEs.SafeStopAppendState3; case "pt": return TxtPt.SafeStopAppendState3; case "ru": return TxtRu.SafeStopAppendState3; case "de": return TxtDe.SafeStopAppendState3; case "fr": return TxtFr.SafeStopAppendState3; default: return TxtCn.SafeStopAppendState3; } } }
        public static string SafeStopAppendServerLog2 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopAppendServerLog2; case "en": return TxtEn.SafeStopAppendServerLog2; case "ja": return TxtJa.SafeStopAppendServerLog2; case "ko": return TxtKo.SafeStopAppendServerLog2; case "es": return TxtEs.SafeStopAppendServerLog2; case "pt": return TxtPt.SafeStopAppendServerLog2; case "ru": return TxtRu.SafeStopAppendServerLog2; case "de": return TxtDe.SafeStopAppendServerLog2; case "fr": return TxtFr.SafeStopAppendServerLog2; default: return TxtCn.SafeStopAppendServerLog2; } } }
        public static string SafeStopShow { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopShow; case "en": return TxtEn.SafeStopShow; case "ja": return TxtJa.SafeStopShow; case "ko": return TxtKo.SafeStopShow; case "es": return TxtEs.SafeStopShow; case "pt": return TxtPt.SafeStopShow; case "ru": return TxtRu.SafeStopShow; case "de": return TxtDe.SafeStopShow; case "fr": return TxtFr.SafeStopShow; default: return TxtCn.SafeStopShow; } } }
        public static string SafeStopShow2 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopShow2; case "en": return TxtEn.SafeStopShow2; case "ja": return TxtJa.SafeStopShow2; case "ko": return TxtKo.SafeStopShow2; case "es": return TxtEs.SafeStopShow2; case "pt": return TxtPt.SafeStopShow2; case "ru": return TxtRu.SafeStopShow2; case "de": return TxtDe.SafeStopShow2; case "fr": return TxtFr.SafeStopShow2; default: return TxtCn.SafeStopShow2; } } }
        public static string SafeStopShow3 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopShow3; case "en": return TxtEn.SafeStopShow3; case "ja": return TxtJa.SafeStopShow3; case "ko": return TxtKo.SafeStopShow3; case "es": return TxtEs.SafeStopShow3; case "pt": return TxtPt.SafeStopShow3; case "ru": return TxtRu.SafeStopShow3; case "de": return TxtDe.SafeStopShow3; case "fr": return TxtFr.SafeStopShow3; default: return TxtCn.SafeStopShow3; } } }
        public static string SafeStopShow4 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopShow4; case "en": return TxtEn.SafeStopShow4; case "ja": return TxtJa.SafeStopShow4; case "ko": return TxtKo.SafeStopShow4; case "es": return TxtEs.SafeStopShow4; case "pt": return TxtPt.SafeStopShow4; case "ru": return TxtRu.SafeStopShow4; case "de": return TxtDe.SafeStopShow4; case "fr": return TxtFr.SafeStopShow4; default: return TxtCn.SafeStopShow4; } } }
        public static string SafeStopAppendState4 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopAppendState4; case "en": return TxtEn.SafeStopAppendState4; case "ja": return TxtJa.SafeStopAppendState4; case "ko": return TxtKo.SafeStopAppendState4; case "es": return TxtEs.SafeStopAppendState4; case "pt": return TxtPt.SafeStopAppendState4; case "ru": return TxtRu.SafeStopAppendState4; case "de": return TxtDe.SafeStopAppendState4; case "fr": return TxtFr.SafeStopAppendState4; default: return TxtCn.SafeStopAppendState4; } } }
        public static string SafeStopAppendState5 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopAppendState5; case "en": return TxtEn.SafeStopAppendState5; case "ja": return TxtJa.SafeStopAppendState5; case "ko": return TxtKo.SafeStopAppendState5; case "es": return TxtEs.SafeStopAppendState5; case "pt": return TxtPt.SafeStopAppendState5; case "ru": return TxtRu.SafeStopAppendState5; case "de": return TxtDe.SafeStopAppendState5; case "fr": return TxtFr.SafeStopAppendState5; default: return TxtCn.SafeStopAppendState5; } } }
        public static string SafeStopAppendState6 { get { switch (Lang.Code) { case "tw": return TxtTw.SafeStopAppendState6; case "en": return TxtEn.SafeStopAppendState6; case "ja": return TxtJa.SafeStopAppendState6; case "ko": return TxtKo.SafeStopAppendState6; case "es": return TxtEs.SafeStopAppendState6; case "pt": return TxtPt.SafeStopAppendState6; case "ru": return TxtRu.SafeStopAppendState6; case "de": return TxtDe.SafeStopAppendState6; case "fr": return TxtFr.SafeStopAppendState6; default: return TxtCn.SafeStopAppendState6; } } }
        public static string OnClosingConfirmBox { get { switch (Lang.Code) { case "tw": return TxtTw.OnClosingConfirmBox; case "en": return TxtEn.OnClosingConfirmBox; case "ja": return TxtJa.OnClosingConfirmBox; case "ko": return TxtKo.OnClosingConfirmBox; case "es": return TxtEs.OnClosingConfirmBox; case "pt": return TxtPt.OnClosingConfirmBox; case "ru": return TxtRu.OnClosingConfirmBox; case "de": return TxtDe.OnClosingConfirmBox; case "fr": return TxtFr.OnClosingConfirmBox; default: return TxtCn.OnClosingConfirmBox; } } }
        public static string BtnSparkTpsText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnSparkTpsText; case "en": return TxtEn.BtnSparkTpsText; case "ja": return TxtJa.BtnSparkTpsText; case "ko": return TxtKo.BtnSparkTpsText; case "es": return TxtEs.BtnSparkTpsText; case "pt": return TxtPt.BtnSparkTpsText; case "ru": return TxtRu.BtnSparkTpsText; case "de": return TxtDe.BtnSparkTpsText; case "fr": return TxtFr.BtnSparkTpsText; default: return TxtCn.BtnSparkTpsText; } } }
        public static string BtnSparkProfileText { get { switch (Lang.Code) { case "tw": return TxtTw.BtnSparkProfileText; case "en": return TxtEn.BtnSparkProfileText; case "ja": return TxtJa.BtnSparkProfileText; case "ko": return TxtKo.BtnSparkProfileText; case "es": return TxtEs.BtnSparkProfileText; case "pt": return TxtPt.BtnSparkProfileText; case "ru": return TxtRu.BtnSparkProfileText; case "de": return TxtDe.BtnSparkProfileText; case "fr": return TxtFr.BtnSparkProfileText; default: return TxtCn.BtnSparkProfileText; } } }
        public static string SparkLblCaption { get { switch (Lang.Code) { case "tw": return TxtTw.SparkLblCaption; case "en": return TxtEn.SparkLblCaption; case "ja": return TxtJa.SparkLblCaption; case "ko": return TxtKo.SparkLblCaption; case "es": return TxtEs.SparkLblCaption; case "pt": return TxtPt.SparkLblCaption; case "ru": return TxtRu.SparkLblCaption; case "de": return TxtDe.SparkLblCaption; case "fr": return TxtFr.SparkLblCaption; default: return TxtCn.SparkLblCaption; } } }
        public static string SparkProfileStarted { get { switch (Lang.Code) { case "tw": return TxtTw.SparkProfileStarted; case "en": return TxtEn.SparkProfileStarted; case "ja": return TxtJa.SparkProfileStarted; case "ko": return TxtKo.SparkProfileStarted; case "es": return TxtEs.SparkProfileStarted; case "pt": return TxtPt.SparkProfileStarted; case "ru": return TxtRu.SparkProfileStarted; case "de": return TxtDe.SparkProfileStarted; case "fr": return TxtFr.SparkProfileStarted; default: return TxtCn.SparkProfileStarted; } } }
        public static string SparkProfileStopped { get { switch (Lang.Code) { case "tw": return TxtTw.SparkProfileStopped; case "en": return TxtEn.SparkProfileStopped; case "ja": return TxtJa.SparkProfileStopped; case "ko": return TxtKo.SparkProfileStopped; case "es": return TxtEs.SparkProfileStopped; case "pt": return TxtPt.SparkProfileStopped; case "ru": return TxtRu.SparkProfileStopped; case "de": return TxtDe.SparkProfileStopped; case "fr": return TxtFr.SparkProfileStopped; default: return TxtCn.SparkProfileStopped; } } }
        public static string SparkProfileCancelled { get { switch (Lang.Code) { case "tw": return TxtTw.SparkProfileCancelled; case "en": return TxtEn.SparkProfileCancelled; case "ja": return TxtJa.SparkProfileCancelled; case "ko": return TxtKo.SparkProfileCancelled; case "es": return TxtEs.SparkProfileCancelled; case "pt": return TxtPt.SparkProfileCancelled; case "ru": return TxtRu.SparkProfileCancelled; case "de": return TxtDe.SparkProfileCancelled; case "fr": return TxtFr.SparkProfileCancelled; default: return TxtCn.SparkProfileCancelled; } } }
        public static string LangCaption { get { switch (Lang.Code) { case "tw": return TxtTw.LangCaption; case "en": return TxtEn.LangCaption; case "ja": return TxtJa.LangCaption; case "ko": return TxtKo.LangCaption; case "es": return TxtEs.LangCaption; case "pt": return TxtPt.LangCaption; case "ru": return TxtRu.LangCaption; case "de": return TxtDe.LangCaption; case "fr": return TxtFr.LangCaption; default: return TxtCn.LangCaption; } } }
        public static string LangRestartHint { get { switch (Lang.Code) { case "tw": return TxtTw.LangRestartHint; case "en": return TxtEn.LangRestartHint; case "ja": return TxtJa.LangRestartHint; case "ko": return TxtKo.LangRestartHint; case "es": return TxtEs.LangRestartHint; case "pt": return TxtPt.LangRestartHint; case "ru": return TxtRu.LangRestartHint; case "de": return TxtDe.LangRestartHint; case "fr": return TxtFr.LangRestartHint; default: return TxtCn.LangRestartHint; } } }
        public static string LangRestartNow { get { switch (Lang.Code) { case "tw": return TxtTw.LangRestartNow; case "en": return TxtEn.LangRestartNow; case "ja": return TxtJa.LangRestartNow; case "ko": return TxtKo.LangRestartNow; case "es": return TxtEs.LangRestartNow; case "pt": return TxtPt.LangRestartNow; case "ru": return TxtRu.LangRestartNow; case "de": return TxtDe.LangRestartNow; case "fr": return TxtFr.LangRestartNow; default: return TxtCn.LangRestartNow; } } }
        public static int EulaAgreeWidth { get { switch (Lang.Code) { case "tw": return TxtTw.EulaAgreeWidth; case "en": return TxtEn.EulaAgreeWidth; case "ja": return TxtJa.EulaAgreeWidth; case "ko": return TxtKo.EulaAgreeWidth; case "es": return TxtEs.EulaAgreeWidth; case "pt": return TxtPt.EulaAgreeWidth; case "ru": return TxtRu.EulaAgreeWidth; case "de": return TxtDe.EulaAgreeWidth; case "fr": return TxtFr.EulaAgreeWidth; default: return TxtCn.EulaAgreeWidth; } } }
        public static int EulaLinkDx { get { switch (Lang.Code) { case "tw": return TxtTw.EulaLinkDx; case "en": return TxtEn.EulaLinkDx; case "ja": return TxtJa.EulaLinkDx; case "ko": return TxtKo.EulaLinkDx; case "es": return TxtEs.EulaLinkDx; case "pt": return TxtPt.EulaLinkDx; case "ru": return TxtRu.EulaLinkDx; case "de": return TxtDe.EulaLinkDx; case "fr": return TxtFr.EulaLinkDx; default: return TxtCn.EulaLinkDx; } } }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ShowFriendlyError(e.Exception, false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowFriendlyError(e.ExceptionObject as Exception, true);
            InitLanguage();
            Application.Run(new MainForm());
        }

        // v6.22: one exe for all languages. The choice is persisted
        // as config.json "Language" (a code from Lang.Codes); when it
        // has never been set, ask once with the language picker
        // before any UI exists. Codes stored by earlier builds
        // ("cn" / "en") are members of Lang.Codes and keep working.
        static void InitLanguage()
        {
            try
            {
                Config cfg = Config.Load();
                if (Lang.IsKnown(cfg.Language)) { Lang.Code = cfg.Language; return; }
                string code;
                using (var picker = new LangPickerForm())
                {
                    picker.ShowDialog();
                    code = picker.ChosenCode;
                }
                Lang.Code = code;
                cfg.Language = code;
                cfg.Save();
            }
            catch { }
        }
        static void ShowFriendlyError(Exception ex, bool terminating)
        {
            try
            {
                string msg = Txt.Msg + (terminating ? Txt.ShowFriendlyErrorStr : Txt.ShowFriendlyErrorStr2)
                    + "\n\n" + (ex != null ? ex.Message : "")
                    + Txt.ShowFriendlyErrorPart;
                InfoBox.Show(msg, Txt.ShowFriendlyErrorShow);
            }
            catch { }
        }
    }

    // ---------- 首次运行语言选择（v6.22） ----------
    // Runs before a language has been chosen, so it must not touch Txt:
    // its entries are the languages' own names (Lang.Names) and the
    // confirm caption is hardcoded bilingually. The list starts
    // pre-selected with the OS UI language when supported (else
    // Simplified Chinese); confirming with the button, a double-click
    // or Enter chooses the selected language, and closing the form
    // without confirming (the X button) counts as choosing the
    // pre-selected one. The choice can be changed later in Settings.
    class LangPickerForm : Form
    {
        public string ChosenCode;
        private ListBox _list;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Cosmetic only: dark title bar so the frame matches the dark body.
            try
            {
                int v = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref v, 4) != 0)
                    DwmSetWindowAttribute(Handle, 19, ref v, 4);
            }
            catch { }
        }

        public LangPickerForm()
        {
            ChosenCode = Lang.DetectDefault();
            Text = "Minecraft Launcher";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 600);
            BackColor = Pal.Bg;
            ForeColor = Pal.Text;
            Font = new Font("Microsoft YaHei UI", 14f);

            var lb = new Label();
            lb.Text = "选择语言 / Select language";
            lb.SetBounds(24, 28, 472, 44);
            lb.Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold);
            lb.ForeColor = Pal.Text;
            lb.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(lb);

            _list = new ListBox();
            for (int i = 0; i < Lang.Names.Length; i++) _list.Items.Add(Lang.Names[i]);
            _list.SetBounds(36, 96, 448, 380);
            _list.Font = new Font("Microsoft YaHei UI", 14f);
            _list.BackColor = Pal.Surface;
            _list.ForeColor = Color.White;
            _list.BorderStyle = BorderStyle.FixedSingle;
            _list.IntegralHeight = false;
            _list.SelectedIndex = Lang.IndexOf(ChosenCode);
            _list.DoubleClick += (s, e) => Confirm();
            Controls.Add(_list);

            var btnOk = new Button();
            btnOk.Text = "确定 / OK";
            btnOk.SetBounds(160, 508, 200, 56);
            btnOk.Font = new Font("Microsoft YaHei UI", 14f);
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.BackColor = Pal.Surface;
            btnOk.ForeColor = Color.White;
            btnOk.Click += (s, e) => Confirm();
            Controls.Add(btnOk);
            AcceptButton = btnOk;
        }

        private void Confirm()
        {
            if (_list.SelectedIndex >= 0) ChosenCode = Lang.CodeAt(_list.SelectedIndex);
            DialogResult = DialogResult.OK;
        }
    }

    // ---------- 配置 ----------
    class Config
    {
        public string ServerDir = "D:\\MCServer";
        public string JavaPath = "";
        public string JarName = "server.jar";
        public string MemParams = "-Xms1G -Xmx2G";
        public int Port = 25565;
        public string FrpcPath = "frpc\\frpc.exe";
        public string FrpKey = "";
        public string TunnelId = "";
        public string VoiceTunnelId = "";
        public string PublicAddr = "";
        public string Pcl2Path = "";
        public string TunnelMode = "frpc"; // frpc / launcher
        public int WinX = -1;
        public int WinY = -1;
        // v6.11: backup / watchdog / schedule settings
        public int BackupKeep = 10;
        public bool BackupOnStart = false;
        public bool AutoRestart = true;
        public bool DailyBackup = false;
        public string DailyBackupTime = "03:00";
        public bool TimedCmdEnabled = false;
        public string TimedCmdTime = "23:00";
        public string TimedCmdText = "";
        public string LastDailyBackup = "";
        public string LastTimedCmd = "";
        public int LastTab = 0; // v6.13: remembered log tab
        public string Language = ""; // v6.22: "" = never chosen; otherwise a code from Lang.Codes

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"); }
        }

        static Dictionary<string, string> ReadDict(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path, Encoding.UTF8);
                var d = new Dictionary<string, string>();
                var ms = Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");
                foreach (Match m in ms) { d[m.Groups[1].Value] = JsonUnesc(m.Groups[2].Value); }
                return d.Count > 0 ? d : null;
            }
            catch { return null; }
        }

        public static Config Load()
        {
            Config c = new Config();
            try
            {
                // v6.10: if the main file is missing/truncated (an interrupted
                // write used to silently reset everything, FrpKey included),
                // fall back to the .bak the atomic Save keeps.
                var d = ReadDict(FilePath);
                var bak = ReadDict(FilePath + ".bak");
                if (bak != null && (d == null || bak.Count > d.Count)) d = bak;
                if (d != null)
                {
                    if (d.ContainsKey("ServerDir")) c.ServerDir = d["ServerDir"];
                    if (d.ContainsKey("JavaPath")) c.JavaPath = d["JavaPath"];
                    if (d.ContainsKey("JarName")) c.JarName = d["JarName"];
                    if (d.ContainsKey("MemParams")) c.MemParams = d["MemParams"];
                    if (d.ContainsKey("Port")) { int p; if (int.TryParse(d["Port"], out p)) c.Port = p; }
                    if (d.ContainsKey("FrpcPath")) c.FrpcPath = d["FrpcPath"];
                    if (d.ContainsKey("FrpKey")) c.FrpKey = d["FrpKey"];
                    if (d.ContainsKey("TunnelId")) c.TunnelId = d["TunnelId"];
                    if (d.ContainsKey("VoiceTunnelId")) c.VoiceTunnelId = d["VoiceTunnelId"];
                    if (d.ContainsKey("PublicAddr")) c.PublicAddr = d["PublicAddr"];
                    if (d.ContainsKey("Pcl2Path")) c.Pcl2Path = d["Pcl2Path"];
                    if (d.ContainsKey("TunnelMode")) c.TunnelMode = d["TunnelMode"];
                    if (d.ContainsKey("WinX")) { int wx; if (int.TryParse(d["WinX"], out wx)) c.WinX = wx; }
                    if (d.ContainsKey("WinY")) { int wy; if (int.TryParse(d["WinY"], out wy)) c.WinY = wy; }
                    if (d.ContainsKey("BackupKeep")) { int bk; if (int.TryParse(d["BackupKeep"], out bk) && bk >= 1 && bk <= 99) c.BackupKeep = bk; }
                    if (d.ContainsKey("BackupOnStart")) c.BackupOnStart = d["BackupOnStart"] == "true";
                    if (d.ContainsKey("AutoRestart")) c.AutoRestart = d["AutoRestart"] != "false";
                    if (d.ContainsKey("DailyBackup")) c.DailyBackup = d["DailyBackup"] == "true";
                    if (d.ContainsKey("DailyBackupTime")) c.DailyBackupTime = d["DailyBackupTime"];
                    if (d.ContainsKey("TimedCmdEnabled")) c.TimedCmdEnabled = d["TimedCmdEnabled"] == "true";
                    if (d.ContainsKey("TimedCmdTime")) c.TimedCmdTime = d["TimedCmdTime"];
                    if (d.ContainsKey("TimedCmdText")) c.TimedCmdText = d["TimedCmdText"];
                    if (d.ContainsKey("LastDailyBackup")) c.LastDailyBackup = d["LastDailyBackup"];
                    if (d.ContainsKey("LastTimedCmd")) c.LastTimedCmd = d["LastTimedCmd"];
                    if (d.ContainsKey("LastTab")) { int lt; if (int.TryParse(d["LastTab"], out lt) && lt >= 0 && lt <= 3) c.LastTab = lt; }
                    if (d.ContainsKey("Language")) c.Language = d["Language"];
                }
            }
            catch { }
            AutoFill(c);
            return c;
        }

        public void Save()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"ServerDir\":\"" + JsonEsc(ServerDir) + "\",");
            sb.Append("\"JavaPath\":\"" + JsonEsc(JavaPath) + "\",");
            sb.Append("\"JarName\":\"" + JsonEsc(JarName) + "\",");
            sb.Append("\"MemParams\":\"" + JsonEsc(MemParams) + "\",");
            sb.Append("\"Port\":\"" + Port + "\",");
            sb.Append("\"FrpcPath\":\"" + JsonEsc(FrpcPath) + "\",");
            sb.Append("\"FrpKey\":\"" + JsonEsc(FrpKey) + "\",");
            sb.Append("\"TunnelId\":\"" + JsonEsc(TunnelId) + "\",");
            sb.Append("\"VoiceTunnelId\":\"" + JsonEsc(VoiceTunnelId) + "\",");
            sb.Append("\"PublicAddr\":\"" + JsonEsc(PublicAddr) + "\",");
            sb.Append("\"Pcl2Path\":\"" + JsonEsc(Pcl2Path) + "\",");
            sb.Append("\"TunnelMode\":\"" + JsonEsc(TunnelMode) + "\",");
            sb.Append("\"WinX\":\"" + WinX + "\",");
            sb.Append("\"WinY\":\"" + WinY + "\",");
            sb.Append("\"BackupKeep\":\"" + BackupKeep + "\",");
            sb.Append("\"BackupOnStart\":\"" + (BackupOnStart ? "true" : "false") + "\",");
            sb.Append("\"AutoRestart\":\"" + (AutoRestart ? "true" : "false") + "\",");
            sb.Append("\"DailyBackup\":\"" + (DailyBackup ? "true" : "false") + "\",");
            sb.Append("\"DailyBackupTime\":\"" + JsonEsc(DailyBackupTime) + "\",");
            sb.Append("\"TimedCmdEnabled\":\"" + (TimedCmdEnabled ? "true" : "false") + "\",");
            sb.Append("\"TimedCmdTime\":\"" + JsonEsc(TimedCmdTime) + "\",");
            sb.Append("\"TimedCmdText\":\"" + JsonEsc(TimedCmdText) + "\",");
            sb.Append("\"LastDailyBackup\":\"" + JsonEsc(LastDailyBackup) + "\",");
            sb.Append("\"LastTab\":\"" + LastTab + "\",");
            sb.Append("\"Language\":\"" + JsonEsc(Language) + "\",");
            sb.Append("\"LastTimedCmd\":\"" + JsonEsc(LastTimedCmd) + "\"");
            sb.Append("}");
            // v6.10: write to a temp file, then swap atomically, keeping the
            // previous good file as .bak - a crash mid-write can no longer
            // truncate the only copy of the config.
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), Encoding.UTF8);
                if (File.Exists(FilePath)) File.Replace(tmp, FilePath, FilePath + ".bak");
                else File.Move(tmp, FilePath);
            }
            catch { }
        }

        static string JsonEsc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }

        static string JsonUnesc(string s) { if (s == null) return s; return s.Replace("\\\"", "\u0001").Replace("\\\\", "\\").Replace("\u0001", "\""); }

        // 从 start.bat / server.properties 自动补全空项
        static void AutoFill(Config c)
        {
            try
            {
                string bat = Path.Combine(c.ServerDir, "start.bat");
                if (File.Exists(bat) && string.IsNullOrEmpty(c.JavaPath))
                {
                    string txt = File.ReadAllText(bat, Encoding.UTF8);
                    var mj = Regex.Match(txt, "\"([^\"]+java\\.exe)\"");
                    if (mj.Success) c.JavaPath = mj.Groups[1].Value;
                    var mx = Regex.Match(txt, "-Xmx([0-9A-Za-z]+)");
                    var ms = Regex.Match(txt, "-Xms([0-9A-Za-z]+)");
                    if (mx.Success && ms.Success)
                        c.MemParams = "-Xms" + ms.Groups[1].Value + " -Xmx" + mx.Groups[1].Value;
                    var mj2 = Regex.Match(txt, "-jar\\s+([^\\s\"]+)\\.jar");
                    if (mj2.Success) c.JarName = mj2.Groups[1].Value + ".jar";
                }
                string props = Path.Combine(c.ServerDir, "server.properties");
                if (File.Exists(props))
                {
                    foreach (string line in File.ReadAllLines(props))
                    {
                        var mp = Regex.Match(line, "^server-port=(\\d+)$");
                        if (mp.Success) { int p; if (int.TryParse(mp.Groups[1].Value, out p)) c.Port = p; }
                    }
                }
            }
            catch { }
        }
    }

    // ---------- 自定义确认框 ----------
    // v6.9: same treatment as InfoBox - self-painted message, height fitted
    // to the measured text, amber status dot (this box always asks for a
    // real choice). The old fixed 440x150 TextLabel box read as a different
    // generation next to the rebuilt InfoBox.
    class ConfirmBox : DarkDialog
    {
        public bool ChoiceYes = false;
        readonly string _message;
        RoundButton _yes, _no;
        Font _msgFont;
        Rectangle _msgRect, _dotRect;
        int _baseW = 440; // v6.22.3: 按钮译文更长时对话框按需加宽

        public ConfirmBox(string message)
        {
            _message = message ?? "";
            // v6.22.3: 确认按钮按当前语言实测宽度，对话框不足时加宽
            int yesW, noW;
            using (var bf = new Font("Microsoft YaHei UI", 9f))
            {
                yesW = Math.Max(180, TextFit.Measure(Txt.YesText, bf) + 28);
                noW = Math.Max(100, TextFit.Measure(Txt.NoText, bf) + 28);
            }
            _baseW = Math.Max(440, yesW + noW + 56);
            InitChrome(Txt.ShowFriendlyErrorShow, _baseW, 190);
            _yes = new RoundButton();
            _yes.Primary = true;
            _yes.Text = Txt.YesText;
            _yes.BackColor = Pal.Green;
            _yes.ForeColor = Color.White;
            _yes.BackdropColor = Pal.Bg;
            int yesX = Math.Max(20, (_baseW - (yesW + noW + 16)) / 2);
            _yes.SetBounds(yesX, 95, yesW, 34);
            _yes.Click += (s, e) => { ChoiceYes = true; DialogResult = DialogResult.OK; };
            Controls.Add(_yes);
            _no = new RoundButton();
            _no.Text = Txt.NoText;
            _no.BackColor = Pal.Surface;
            _no.ForeColor = Color.White;
            _no.BackdropColor = Pal.Bg;
            _no.SetBounds(yesX + yesW + 16, 95, noW, 34);
            _no.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(_no);
            AcceptButton = _yes;
            CancelButton = _no;
            FinishChrome();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                float sf = ClientSize.Width / (float)_baseW;
                _msgFont = new Font(Font.FontFamily, Font.Size + 1f, FontStyle.Regular);
                int textX = (int)Math.Round(42 * sf);
                int textW = ClientSize.Width - textX - (int)Math.Round(24 * sf);
                int textTop = (int)Math.Round((40 + 18) * sf);
                var flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
                Size sz;
                using (var g = CreateGraphics())
                    sz = TextRenderer.MeasureText(g, _message, _msgFont, new Size(textW, int.MaxValue), flags);
                _msgRect = new Rectangle(textX, textTop, textW, sz.Height);
                int dotD = Math.Max(6, (int)Math.Round(8 * sf));
                _dotRect = new Rectangle((int)Math.Round(24 * sf), textTop + Math.Max(2, (int)Math.Round(4 * sf)), dotD, dotD);
                int btnTop = textTop + sz.Height + (int)Math.Round(20 * sf);
                int bottom = btnTop + _yes.Height + (int)Math.Round(18 * sf);
                _yes.Top = btnTop;
                _no.Top = btnTop;
                Size = new Size(Width, bottom);
                var wa = Screen.FromControl(this).WorkingArea;
                Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
                Invalidate();
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(Pal.Amber)) g.FillEllipse(b, _dotRect);
            if (_msgFont != null)
                TextRenderer.DrawText(g, _message, _msgFont, _msgRect, Pal.Text,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { try { if (_msgFont != null) _msgFont.Dispose(); } catch { } }
            base.Dispose(disposing);
        }
    }

    // ---------- 设置页 ----------
    class SettingsForm : DarkDialog
    {
        private TextBox tbServerDir, tbJava, tbJar, tbMem, tbPort, tbFrpc, tbKey, tbTunnelId, tbVoiceTunnelId, tbAddr, tbPcl2;
        private ComboBox cbTunnelMode;
        private ComboBox cbLanguage;
        private Config _cfg;
        private TextLabel _errLbl;
        private int _labelW = 140;
        public bool ServerRunning = false;
        public bool RestartRequested = false;

        public SettingsForm(Config cfg)
        {
            _cfg = cfg;
            // v6.22.3: 标签列按当前语言实测（下限 140 / 上限 210），窗口随需加宽，
            // 输入框 x 与宽度随之联动，避免德/俄/西长标签压到输入框上。
            using (var lf = new Font("Microsoft YaHei UI", 9f))
            {
                string[] labels = {
                    Txt.SettingsFormAddField, Txt.SettingsFormAddField2, Txt.SettingsFormAddField3,
                    Txt.SettingsFormAddField4, Txt.SettingsFormAddField5, Txt.SettingsFormAddField6,
                    Txt.LbModeCaption, Txt.LbKeyCaption, Txt.SettingsFormAddField7,
                    Txt.SettingsFormAddField8, Txt.SettingsFormAddField9, Txt.SettingsFormAddField10,
                    Txt.LangCaption };
                for (int i = 0; i < labels.Length; i++) _labelW = Math.Max(_labelW, TextFit.Measure(labels[i], lf) + 6);
            }
            if (_labelW > 210) _labelW = 210;
            int lx = 20, tx = lx + _labelW + 10;
            int formW = Math.Max(620, tx + 420 + 30);
            int tw = formW - tx - 30;
            InitChrome(Txt.SettingsFormInitChrome, formW, 620);
            AutoScroll = true;

            int y = 14;

            AddField(Txt.SettingsFormAddField, "tbServerDir", cfg.ServerDir, ref y, lx, tx, tw, out tbServerDir);
            AddField(Txt.SettingsFormAddField2, "tbJava", cfg.JavaPath, ref y, lx, tx, tw, out tbJava);
            AddField(Txt.SettingsFormAddField3, "tbJar", cfg.JarName, ref y, lx, tx, tw, out tbJar);
            AddField(Txt.SettingsFormAddField4, "tbMem", cfg.MemParams, ref y, lx, tx, tw, out tbMem);
            AddField(Txt.SettingsFormAddField5, "tbPort", cfg.Port.ToString(), ref y, lx, tx, tw, out tbPort);
            AddField(Txt.SettingsFormAddField6, "tbFrpc", cfg.FrpcPath, ref y, lx, tx, tw, out tbFrpc);

            var lbMode = new TextLabel();
            lbMode.TopAlign = true;
            lbMode.Caption = Txt.LbModeCaption;
            lbMode.TextShiftX = 2f; lbMode.TextShiftY = -0.67f;
            lbMode.SetBounds(lx, y, _labelW, 26);
            lbMode.BackColor = Color.Transparent; lbMode.BackdropColor = Pal.Bg; lbMode.ForeColor = Pal.Sub;
            Controls.Add(lbMode);
            cbTunnelMode = new ComboBox();
            cbTunnelMode.Items.Add(Txt.SettingsFormAdd);
            cbTunnelMode.Items.Add(Txt.SettingsFormAdd2);
            cbTunnelMode.SelectedIndex = (cfg.TunnelMode == "launcher") ? 1 : 0;
            cbTunnelMode.SetBounds(tx, y, tw, 26);
            cbTunnelMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTunnelMode.FlatStyle = FlatStyle.Flat;
            cbTunnelMode.BackColor = Pal.Surface;
            cbTunnelMode.ForeColor = Color.White;
            Controls.Add(cbTunnelMode);
            ThemedCombo.Attach(cbTunnelMode);
            y += 36;

            var lbKey = new TextLabel();
            lbKey.TopAlign = true;
            lbKey.Caption = Txt.LbKeyCaption;
            lbKey.TextShiftX = 2f; lbKey.TextShiftY = -0.67f;
            lbKey.SetBounds(lx, y, _labelW, 26);
            lbKey.BackColor = Color.Transparent; lbKey.BackdropColor = Pal.Bg; lbKey.ForeColor = Pal.Sub;
            Controls.Add(lbKey);
            tbKey = new TextBox();
            tbKey.SetBounds(tx, y, tw, 26);
            tbKey.BackColor = Pal.Surface;
            tbKey.ForeColor = Color.White;
            tbKey.BorderStyle = BorderStyle.FixedSingle;
            tbKey.UseSystemPasswordChar = true;
            tbKey.Text = cfg.FrpKey;
            Controls.Add(tbKey);
            y += 36;

            AddField(Txt.SettingsFormAddField7, "tbTunnelId", cfg.TunnelId, ref y, lx, tx, tw, out tbTunnelId);
            AddField(Txt.SettingsFormAddField8, "tbVoiceTunnelId", cfg.VoiceTunnelId, ref y, lx, tx, tw, out tbVoiceTunnelId);
            AddField(Txt.SettingsFormAddField9, "tbAddr", cfg.PublicAddr, ref y, lx, tx, tw, out tbAddr);
            AddField(Txt.SettingsFormAddField10, "tbPcl2", cfg.Pcl2Path, ref y, lx, tx, tw, out tbPcl2);

            var lbLang = new TextLabel();
            lbLang.TopAlign = true;
            lbLang.Caption = Txt.LangCaption;
            lbLang.TextShiftX = 2f; lbLang.TextShiftY = -0.67f;
            lbLang.SetBounds(lx, y, _labelW, 26);
            lbLang.BackColor = Color.Transparent; lbLang.BackdropColor = Pal.Bg; lbLang.ForeColor = Pal.Sub;
            Controls.Add(lbLang);
            cbLanguage = new ComboBox();
            for (int i = 0; i < Lang.Names.Length; i++) cbLanguage.Items.Add(Lang.Names[i]);
            int langIdx = Lang.IndexOf(_cfg.Language);
            cbLanguage.SelectedIndex = langIdx >= 0 ? langIdx : Lang.IndexOf("cn");
            cbLanguage.SetBounds(tx, y, tw, 26);
            cbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLanguage.FlatStyle = FlatStyle.Flat;
            cbLanguage.BackColor = Pal.Surface;
            cbLanguage.ForeColor = Color.White;
            Controls.Add(cbLanguage);
            ThemedCombo.Attach(cbLanguage);
            y += 36;

            _errLbl = new TextLabel();
            _errLbl.TopAlign = true;
            _errLbl.WordWrap = true; // v6.9: a longer error may take two lines
            _errLbl.TextShiftX = 2f; _errLbl.TextShiftY = -0.67f;
            _errLbl.SetBounds(tx, y + 2, tw, 40);
            _errLbl.ForeColor = Pal.DangerText; // v6.14
            _errLbl.BackColor = Color.Transparent; _errLbl.BackdropColor = Pal.Bg;
            Controls.Add(_errLbl);

            // v6.22.3: 底部四个按钮按译文比例分配（原固定 180/150/100/100）
            int[] bw;
            using (var bf = new Font("Microsoft YaHei UI", 9f))
                bw = TextFit.Distribute(new string[] { Txt.BtnPropsText, Txt.BtnWizardText, Txt.BtnSaveText, Txt.NoText },
                    bf, formW - 40, 12, 24, 84, true);
            int bx0 = 20, bx1 = bx0 + bw[0] + 12, bx2 = bx1 + bw[1] + 12, bx3 = bx2 + bw[2] + 12;

            var btnProps = new RoundButton();
            btnProps.Text = Txt.BtnPropsText;
            btnProps.BackColor = Pal.Surface;
            btnProps.ForeColor = Color.White;
            btnProps.SetBounds(bx0, y + 52, bw[0], 38);
            btnProps.Click += (s, e) =>
            {
                string dir = tbServerDir.Text.Trim();
                if (dir.Length == 0 || !System.IO.Directory.Exists(dir)) { InfoBox.Show(Txt.SettingsFormShow, Txt.SettingsFormShow2); return; }
                using (var pf = new PropsForm(dir, ServerRunning)) pf.ShowDialog(this);
            };
            Controls.Add(btnProps);

            var btnWizard = new RoundButton();
            btnWizard.Text = Txt.BtnWizardText;
            btnWizard.BackColor = Pal.Surface;
            btnWizard.ForeColor = Color.White;
            btnWizard.SetBounds(bx1, y + 52, bw[1], 38);
            btnWizard.Click += (s, e) =>
            {
                using (var wf = new WizardForm(_cfg, tbServerDir.Text.Trim()))
                {
                    if (wf.ShowDialog(this) == DialogResult.OK)
                    {
                        tbServerDir.Text = _cfg.ServerDir;
                        tbJava.Text = _cfg.JavaPath;
                        tbJar.Text = _cfg.JarName;
                        _errLbl.Caption = "";
                    }
                }
            };
            Controls.Add(btnWizard);

            var btnSave = new RoundButton();
            btnSave.Primary = true;
            btnSave.Text = Txt.BtnSaveText;
            btnSave.BackColor = Pal.Green;
            btnSave.ForeColor = Color.White;
            btnSave.SetBounds(bx2, y + 52, bw[2], 38);
            btnSave.Click += (s, e) => { if (Save()) DialogResult = DialogResult.OK; };
            Controls.Add(btnSave);

            var btnCancel = new RoundButton();
            btnCancel.Text = Txt.NoText;
            btnCancel.BackColor = Pal.Surface;
            btnCancel.ForeColor = Color.White;
            btnCancel.SetBounds(bx3, y + 52, bw[3], 38);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(btnCancel);

            ClientSize = new Size(formW, Math.Min(y + 130, 710) + 40);
            AcceptButton = btnSave;
            FinishChrome();
        }

        void AddField(string label, string tag, string value, ref int y, int lx, int tx, int tw, out TextBox tb)
        {
            var lb = new TextLabel();
            lb.TopAlign = true;
            lb.Caption = label;
            lb.TextShiftX = 2f; lb.TextShiftY = -0.67f;
            lb.SetBounds(lx, y, _labelW, 26);
            lb.BackColor = Color.Transparent; lb.BackdropColor = Pal.Bg;
            lb.ForeColor = Pal.Sub;
            Controls.Add(lb);
            tb = new TextBox();
            tb.SetBounds(tx, y, tw, 26);
            tb.BackColor = Pal.Surface;
            tb.ForeColor = Color.White;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Text = value ?? "";
            var tbLocal = tb;
            tbLocal.TextChanged += (s, e) => { tbLocal.BackColor = Pal.Surface; };
            Controls.Add(tb);
            y += 36;
        }

        bool MarkInvalid(TextBox tb, string msg)
        {
            tb.BackColor = Gfx.Mix(Pal.Surface, Pal.Danger, 0.30);
            if (_errLbl != null && string.IsNullOrEmpty(_errLbl.Caption)) _errLbl.Caption = msg;
            return false;
        }

        bool ValidateSettings()
        {
            if (_errLbl != null) _errLbl.Caption = "";
            bool ok = true;
            string dir = tbServerDir.Text.Trim();
            if (dir.Length == 0 || !System.IO.Directory.Exists(dir)) ok = MarkInvalid(tbServerDir, Txt.ValidateSettingsMarkInvalid);
            string java = tbJava.Text.Trim();
            if (java.Length > 0 && !System.IO.File.Exists(java)) ok = MarkInvalid(tbJava, Txt.ValidateSettingsMarkInvalid2);
            string jar = tbJar.Text.Trim();
            if (jar.Length == 0) ok = MarkInvalid(tbJar, Txt.ValidateSettingsMarkInvalid3);
            else if (dir.Length > 0 && System.IO.Directory.Exists(dir) && !System.IO.File.Exists(System.IO.Path.Combine(dir, jar))) ok = MarkInvalid(tbJar, Txt.ValidateSettingsMarkInvalid4);
            int p;
            if (!int.TryParse(tbPort.Text.Trim(), out p) || p < 1 || p > 65535) ok = MarkInvalid(tbPort, Txt.ValidateSettingsMarkInvalid5);
            string frpc = tbFrpc.Text.Trim();
            if (frpc.Length > 0 && !System.IO.File.Exists(frpc)) ok = MarkInvalid(tbFrpc, Txt.ValidateSettingsMarkInvalid6);
            if (cbTunnelMode.SelectedIndex == 0 && tbTunnelId.Text.Trim().Length == 0) ok = MarkInvalid(tbTunnelId, Txt.ValidateSettingsMarkInvalid7);
            string pcl = tbPcl2.Text.Trim();
            if (pcl.Length > 0 && !System.IO.File.Exists(pcl)) ok = MarkInvalid(tbPcl2, Txt.ValidateSettingsMarkInvalid8);
            return ok;
        }

        bool Save()
        {
            if (!ValidateSettings()) return false;
            _cfg.ServerDir = tbServerDir.Text.Trim();
            _cfg.JavaPath = tbJava.Text.Trim();
            _cfg.JarName = tbJar.Text.Trim();
            _cfg.MemParams = tbMem.Text.Trim();
            int p; if (int.TryParse(tbPort.Text.Trim(), out p)) _cfg.Port = p;
            _cfg.FrpcPath = tbFrpc.Text.Trim();
            _cfg.FrpKey = tbKey.Text.Trim();
            _cfg.TunnelId = tbTunnelId.Text.Trim();
            _cfg.VoiceTunnelId = tbVoiceTunnelId.Text.Trim();
            _cfg.PublicAddr = tbAddr.Text.Trim();
            _cfg.Pcl2Path = tbPcl2.Text.Trim();
            _cfg.TunnelMode = (cbTunnelMode.SelectedIndex == 1) ? "launcher" : "frpc";
            string newLang = Lang.CodeAt(cbLanguage.SelectedIndex);
            bool langChanged = newLang != _cfg.Language;
            if (langChanged) _cfg.Language = newLang;
            _cfg.Save();
            if (langChanged)
            {
                if (ServerRunning)
                {
                    InfoBox.Show(Txt.LangRestartHint, Txt.SettingsFormInitChrome);
                }
                else
                {
                    var rr = InfoBox.Show(Txt.LangRestartHint, Txt.SettingsFormInitChrome, Txt.LangRestartNow, Txt.NoText);
                    if (rr == DialogResult.OK) RestartRequested = true;
                }
            }
            return true;
        }
    }

    // ---------- 设计系统（v5 美化） ----------
    static class Pal
    {
        public static readonly Color Bg = Color.FromArgb(23, 26, 31);
        public static readonly Color TitleBar = Color.FromArgb(27, 31, 38);
        public static readonly Color Card = Color.FromArgb(32, 36, 44);
        public static readonly Color Inset = Color.FromArgb(20, 23, 28);
        public static readonly Color InsetBorder = Color.FromArgb(35, 39, 47);
        public static readonly Color Surface = Color.FromArgb(41, 46, 55);
        public static readonly Color Border = Color.FromArgb(44, 49, 58);
        public static readonly Color Text = Color.FromArgb(238, 241, 245);
        public static readonly Color Sub = Color.FromArgb(154, 165, 177);
        public static readonly Color Mute = Color.FromArgb(110, 118, 130); // v6.14: lifted (was 94,103,115, only 2.8:1)
        public static readonly Color Green = Color.FromArgb(59, 165, 93);
        public static readonly Color Amber = Color.FromArgb(250, 168, 26);
        public static readonly Color Danger = Color.FromArgb(237, 66, 69);
        public static readonly Color DangerText = Color.FromArgb(248, 113, 113); // v6.14: small-text danger (>=4.5:1 on card)
        public static readonly Color AddrGreen = Color.FromArgb(86, 216, 143);
        public static readonly Color InfoBlue = Color.FromArgb(72, 144, 245);
        public static readonly Color LogText = Color.FromArgb(203, 210, 219);
    }

    static class Gfx
    {
        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return p;
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        public static Color Mix(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
        public static Color Lighten(Color c, double t) { return Mix(c, Color.White, t); }
        public static Color Darken(Color c, double t) { return Mix(c, Color.Black, t); }
        // Two-layer focus ring (v6.4): light outer band + dark separator,
        // drawn inside the control bounds along its rounded path.
        public static void DrawFocusRing(Graphics g, Rectangle rect, int radius, float scale)
        {
            int ow = Math.Max(2, (int)Math.Round(2 * scale));
            int inset1 = Math.Max(1, ow / 2);
            var r1 = new Rectangle(rect.X + inset1, rect.Y + inset1, rect.Width - 2 * inset1, rect.Height - 2 * inset1);
            if (r1.Width <= 4 || r1.Height <= 4) return;
            using (var p = RoundRect(r1, Math.Max(1, radius - inset1)))
            using (var pen = new Pen(Pal.Text, ow)) g.DrawPath(pen, p);
            int iw = Math.Max(1, (int)Math.Round(1 * scale));
            int inset2 = inset1 + ow / 2 + iw / 2 + 1;
            var r2 = new Rectangle(rect.X + inset2, rect.Y + inset2, rect.Width - 2 * inset2, rect.Height - 2 * inset2);
            if (r2.Width <= 4 || r2.Height <= 4) return;
            using (var p = RoundRect(r2, Math.Max(1, radius - inset2)))
            using (var pen = new Pen(Pal.Bg, iw)) g.DrawPath(pen, p);
        }
    }

    // ---------- shared micro-animation clock (v6.4) ----------
    // One static 8 ms timer (with timeBeginPeriod(1)) drives every AnimFloat;
    // values are pure functions of wall-clock time, so a late tick jumps to
    // the correct value instead of accumulating error. MainForm mirrors the
    // system animation setting (SPI_GETCLIENTAREAANIMATION) into Enabled;
    // when off, every tween snaps straight to its end state.
    static class UiAnim
    {
        public static bool Enabled = true;
        static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();
        static readonly System.Collections.Generic.List<AnimFloat> Active = new System.Collections.Generic.List<AnimFloat>();
        static System.Windows.Forms.Timer _timer;
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint uPeriod);
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern uint timeEndPeriod(uint uPeriod);
        public static long Now { get { return Clock.ElapsedMilliseconds; } }
        public static double EaseOut(double p)
        {
            // cubic-bezier(0, 0, 0, 1) - the Fluent fast-out curve
            double t = p;
            for (int i = 0; i < 6; i++)
            {
                double u = 1 - t;
                double cx = 3 * u * t * t + t * t * t - p;
                double dx = 6 * u * t + 3 * t * t;
                if (Math.Abs(cx) < 1e-6 || Math.Abs(dx) < 1e-6) break;
                t -= cx / dx; if (t < 0) t = 0; if (t > 1) t = 1;
            }
            double v = 1 - t;
            return 3 * v * t * t + t * t * t;
        }
        internal static void Register(AnimFloat f)
        {
            if (!Active.Contains(f)) Active.Add(f);
            if (_timer == null)
            {
                _timer = new System.Windows.Forms.Timer();
                _timer.Interval = 8;
                _timer.Tick += (s, e) => TickAll();
            }
            if (!_timer.Enabled) { try { timeBeginPeriod(1); } catch { } _timer.Start(); }
        }
        internal static void Unregister(AnimFloat f)
        {
            Active.Remove(f);
            if (Active.Count == 0 && _timer != null && _timer.Enabled) { _timer.Stop(); try { timeEndPeriod(1); } catch { } }
        }
        static void TickAll()
        {
            var snapshot = Active.ToArray();
            foreach (var f in snapshot)
            {
                if (!Active.Contains(f)) continue;
                bool alive = false;
                try { alive = f.Step(); } catch { alive = false; }
                if (!alive) Active.Remove(f);
            }
            if (Active.Count == 0 && _timer != null && _timer.Enabled) { _timer.Stop(); try { timeEndPeriod(1); } catch { } }
        }
    }

    class AnimFloat
    {
        public double Value;
        public Action Changed;
        double _from, _to; long _t0; int _dur;
        bool _pulse;
        public AnimFloat(double v) { Value = v; _to = v; }
        public bool IsPulsing { get { return _pulse; } }
        public void Set(double target, int durMs)
        {
            _pulse = false;
            if (!UiAnim.Enabled || durMs <= 0) { Value = target; _to = target; UiAnim.Unregister(this); Fire(); return; }
            _from = Value; _to = target; _dur = durMs; _t0 = UiAnim.Now;
            UiAnim.Register(this);
        }
        // Press feedback: cover most of the distance on this very frame, then settle.
        public void JumpTo(double target, double fraction, int settleMs)
        {
            _pulse = false;
            Value = Value + (target - Value) * fraction;
            Fire();
            Set(target, settleMs);
        }
        public void StartPulse()
        {
            if (!UiAnim.Enabled) { Value = 1; return; }
            _pulse = true; _t0 = UiAnim.Now;
            UiAnim.Register(this);
        }
        public void StopPulse()
        {
            if (!_pulse && Value == 1) return;
            _pulse = false; Value = 1; UiAnim.Unregister(this); Fire();
        }
        internal bool Step()
        {
            if (_pulse)
            {
                double ph = (UiAnim.Now - _t0) % 1800 / 1800.0;
                Value = 0.775 + 0.225 * Math.Cos(ph * 2 * Math.PI);
                Fire();
                return true;
            }
            double p = _dur > 0 ? (UiAnim.Now - _t0) / (double)_dur : 1;
            if (p >= 1) { Value = _to; Fire(); return false; }
            Value = _from + (_to - _from) * UiAnim.EaseOut(p);
            Fire();
            return true;
        }
        void Fire() { if (Changed != null) { try { Changed(); } catch { } } }
    }

    static class DpiScale
    {
        public static float Measure(Control c)
        {
            try { using (var g = c.CreateGraphics()) return g.DpiX / 96f; }
            catch { return 1f; }
        }
        public static void ScaleChildren(Control parent, float s)
        {
            foreach (Control c in parent.Controls)
            {
                c.Bounds = new Rectangle((int)Math.Round(c.Left * s), (int)Math.Round(c.Top * s), (int)Math.Round(c.Width * s), (int)Math.Round(c.Height * s));
                ScaleChildren(c, s);
            }
        }
        public static void ScaleForm(Form f, int designW, int designH)
        {
            try
            {
                float s = Measure(f);
                if (s <= 0 || Math.Abs(s - 1f) < 0.01f) return;
                ScaleChildren(f, s);
                f.ClientSize = new Size((int)(designW * s), (int)(designH * s));
            }
            catch { }
        }
    }

    static class Fonts
    {
        public static Font Mono(float size, FontStyle style)
        {
            string[] names = { "Cascadia Mono", "Cascadia Code", "Consolas" };
            try
            {
                var installed = new System.Drawing.Text.InstalledFontCollection();
                foreach (string n in names)
                {
                    foreach (var f in installed.Families) { if (f.Name == n) return new Font(n, size, style); }
                }
            }
            catch { }
            return new Font("Consolas", size, style);
        }
    }

    // ---------- v6.22.3: 多语言文本自适应 ----------
    // 所有固定像素布局都是按中文/英文长度定的，法/德/俄/西/葡译文更长，
    // 9pt 固定字号下会被裁掉或互相压叠。这里集中提供“按当前设备字体实测 +
    // 逐级缩字号（下限 7pt）+ 必要时省略号”的能力，由各控件在绘制/排版时调用。
    static class TextFit
    {
        public const float MinSize = 7f;
        // 单行文本像素宽（不含内边距）
        public static int Measure(string s, Font f)
        {
            if (string.IsNullOrEmpty(s) || f == null) return 0;
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                    return TextRenderer.MeasureText(g, s, f, new Size(int.MaxValue, int.MaxValue),
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            }
            catch { return 0; }
        }
        // 返回在 avail 宽内放得下的最大字号；都放不下时返回 MinSize
        public static float FitSize(string s, Font baseFont, int avail, float minSize)
        {
            if (baseFont == null || string.IsNullOrEmpty(s) || avail <= 0) return baseFont == null ? minSize : baseFont.Size;
            float size = baseFont.Size;
            while (size > minSize)
            {
                using (var f = new Font(baseFont.FontFamily, size, baseFont.Style))
                {
                    if (Measure(s, f) <= avail) return size;
                }
                size -= 0.5f;
            }
            return minSize;
        }
        // 按文本比例把一组控件的宽度分配到 total 内（不足时等比压缩）
        public static int[] Distribute(string[] texts, Font f, int total, int gap, int pad, int minW, bool fill = false)
        {
            int n = texts == null ? 0 : texts.Length;
            int[] w = new int[n];
            if (n == 0) return w;
            int[] want = new int[n];
            int sum = 0;
            for (int i = 0; i < n; i++)
            {
                want[i] = Math.Max(minW, Measure(texts[i], f) + pad);
                sum += want[i];
            }
            int avail = total - gap * (n - 1);
            if (avail < n) avail = n;
            if (sum <= avail && !fill)
            {
                for (int i = 0; i < n; i++) w[i] = want[i];
                return w;
            }
            double k = (double)avail / sum;
            int used = 0;
            for (int i = 0; i < n; i++)
            {
                w[i] = Math.Max(1, (int)Math.Floor(want[i] * k));
                used += w[i];
            }
            w[n - 1] += avail - used; // 余数给最后一项，保证总宽一致
            if (w[n - 1] < 1) w[n - 1] = 1;
            return w;
        }
    }

    class CardPanel : Panel
    {
        public int Radius = 12;
        public Color BackdropColor = Pal.Bg;
        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var back = new SolidBrush(BackdropColor)) e.Graphics.FillRectangle(back, ClientRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundRect(rect, Radius))
            {
                using (var brush = new SolidBrush(Pal.Card)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(Pal.Border, 1f)) e.Graphics.DrawPath(pen, path);
            }
        }
        protected override void OnPaint(PaintEventArgs e) { }
    }

    class InsetPanel : Panel
    {
        public int Radius = 8;
        public bool FocusGlow = false;
        public Color BackdropColor = Pal.Bg;
        public InsetPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var back = new SolidBrush(BackdropColor)) e.Graphics.FillRectangle(back, ClientRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            bool lit = FocusGlow && ContainsFocus;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundRect(rect, Radius))
            {
                using (var brush = new SolidBrush(Pal.Inset)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(lit ? Pal.Green : Pal.InsetBorder, 1f)) e.Graphics.DrawPath(pen, path);
            }
            if (lit)
            {
                var inner = new Rectangle(2, 2, Width - 5, Height - 5);
                using (var path2 = Gfx.RoundRect(inner, Radius - 2))
                using (var pen2 = new Pen(Color.FromArgb(45, Pal.Green), 1f)) e.Graphics.DrawPath(pen2, path2);
            }
        }
        protected override void OnPaint(PaintEventArgs e) { }
    }

    class RoundButton : Button
    {
        public bool Primary = false;
        public bool DangerHover = false;
        public int Radius = 8;
        public Color BackdropColor = Pal.Bg;
        bool _hover = false, _down = false;
        // v6.22.3: 译文自适应缓存（文本/宽度/字号不变时复用测量结果）
        string _fitText = null;
        int _fitWidth = -1;
        float _fitSize = 0f;
        Font _fitFont = null;
        // _shade: 0 = normal, +1 = full hover lighten, -1 = full press darken
        readonly AnimFloat _shade = new AnimFloat(0);
        public RoundButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            _shade.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; _shade.Set(1, 120); Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; _shade.Set(0, 80); Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _down = true; _shade.JumpTo(-1, 0.7, 70); Invalidate(); } base.OnMouseDown(e); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; if (_hover) _shade.Set(1, 110); else _shade.Set(0, 80); Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { if (!Enabled) _shade.Set(0, 0); base.OnEnabledChanged(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var back = new SolidBrush(BackdropColor)) g.FillRectangle(back, ClientRectangle);
            Color baseCol = BackColor;
            if (!Enabled) baseCol = Gfx.Mix(baseCol, Pal.Mute, 0.55);
            Color fill = baseCol;
            double hAmt = 0;
            if (Enabled)
            {
                double sh = _shade.Value;
                hAmt = sh > 0 ? sh : 0;
                if (sh > 0.001) fill = Gfx.Lighten(baseCol, 0.09 * sh);
                else if (sh < -0.001) fill = Gfx.Darken(baseCol, 0.10 * -sh);
            }
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundRect(rect, Radius))
            {
                Color top = Gfx.Lighten(fill, Primary ? 0.13 : 0.05);
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), top, fill, LinearGradientMode.Vertical))
                    g.FillPath(br, path);
                if (Primary)
                {
                    using (var pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
                        g.DrawLine(pen, Radius + 2, 1, Width - Radius - 2, 1);
                }
                else
                {
                    Color bc = DangerHover ? Gfx.Mix(Gfx.Lighten(fill, 0.16), Pal.Danger, hAmt) : Gfx.Lighten(fill, 0.16);
                    using (var pen = new Pen(bc, 1f)) g.DrawPath(pen, path);
                }
            }
            Color tc = ForeColor;
            if (!Enabled) tc = Gfx.Mix(tc, Pal.Mute, 0.4);
            if (DangerHover && Enabled && hAmt > 0) tc = Gfx.Mix(tc, Pal.Danger, hAmt);
            if (Enabled && Focused && ShowFocusCues) Gfx.DrawFocusRing(g, new Rectangle(0, 0, Width, Height), Radius, g.DpiX / 96f);
            // v6.22.3: 按译文实测宽度缩字号（下限 7pt），仍放不下才用省略号。
            // 旧代码固定 9pt + EndEllipsis，法/西/俄等长译文会被直接截成“...”。
            int avail = Math.Max(1, Width - 16);
            if (_fitText != Text || _fitWidth != Width)
            {
                _fitText = Text;
                _fitWidth = Width;
                _fitSize = TextFit.FitSize(Text, Font, avail, TextFit.MinSize);
                if (_fitFont != null) { try { _fitFont.Dispose(); } catch { } }
                _fitFont = Math.Abs(_fitSize - Font.Size) > 0.01f
                    ? new Font(Font.FontFamily, _fitSize, Font.Style) : null;
            }
            Font df = _fitFont != null ? _fitFont : Font;
            var tflags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            if (TextFit.Measure(Text, df) > avail) tflags |= TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(g, Text, df, rect, tc, tflags);
        }
    }

    class TitleBarPanel : Panel
    {
        public event EventHandler CloseClicked;
        public event EventHandler MinClicked;
        public event EventHandler DragRequested;
        // v6.6: reusable on dialogs — zones are width-relative (identical
        // geometry on the 560-wide main form), title/subtitle/min are settable.
        public string TitleText = Txt.TitleText;
        public string SubText = Txt.SubText;
        public bool ShowMin = true;
        Image _icon;
        int _hover = 0, _down = 0;
        readonly AnimFloat _minAmt = new AnimFloat(0);
        readonly AnimFloat _closeAmt = new AnimFloat(0);
        Font _titleFont = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold);
        Font _verFont = new Font("Microsoft YaHei UI", 8f);
        public TitleBarPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Pal.TitleBar;
            TabStop = false;
            try { string ipng = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.png"); if (File.Exists(ipng)) _icon = Image.FromFile(ipng); } catch { }
            _minAmt.Changed = () => { if (!IsDisposed) Invalidate(); };
            _closeAmt.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        void RetargetZones()
        {
            double minT = _down == 1 ? 1.0 : (_hover == 1 ? 0.6 : 0.0);
            double closeT = (_down == 2 || _hover == 2) ? 1.0 : 0.0;
            _minAmt.Set(minT, minT > 0 ? 100 : 80);
            _closeAmt.Set(closeT, closeT > 0 ? 100 : 80);
        }
        float S { get { return Height / 40f; } }
        int ZoneAt(int x)
        {
            float s = S;
            if (x >= Width - 46 * s) return 2;
            if (ShowMin && x >= Width - 92 * s) return 1;
            return 0;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int z = ZoneAt(e.X);
            if (z != _hover) { _hover = z; RetargetZones(); Invalidate(); }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { if (_hover != 0 || _down != 0) { _hover = 0; _down = 0; Invalidate(); } RetargetZones(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int z = ZoneAt(e.X);
                if (z > 0) { _down = z; if (z == 2) _closeAmt.JumpTo(1, 0.7, 70); else _minAmt.JumpTo(1, 0.7, 70); RetargetZones(); Invalidate(); }
                else if (DragRequested != null) DragRequested(this, EventArgs.Empty);
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _down > 0)
            {
                int z = ZoneAt(e.X);
                int d = _down; _down = 0; RetargetZones(); Invalidate();
                if (z == d)
                {
                    if (d == 2 && CloseClicked != null) CloseClicked(this, EventArgs.Empty);
                    if (d == 1 && MinClicked != null) MinClicked(this, EventArgs.Empty);
                }
            }
            base.OnMouseUp(e);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = S;
            using (var back = new SolidBrush(Pal.TitleBar)) g.FillRectangle(back, ClientRectangle);
            double ca = _closeAmt.Value;
            if (ca > 0.003)
            {
                using (var b = new SolidBrush(Gfx.Mix(Pal.TitleBar, Color.FromArgb(232, 17, 35), ca))) g.FillRectangle(b, Width - 46 * s, 0, 46 * s, Height);
            }
            double ma = _minAmt.Value;
            if (ShowMin && ma > 0.003)
            {
                Color fill = ma <= 0.6 ? Gfx.Mix(Pal.TitleBar, Color.FromArgb(44, 49, 58), ma / 0.6)
                                       : Gfx.Mix(Color.FromArgb(44, 49, 58), Color.FromArgb(52, 58, 68), (ma - 0.6) / 0.4);
                using (var b = new SolidBrush(fill)) g.FillRectangle(b, Width - 92 * s, 0, 46 * s, Height);
            }
            if (_icon != null) g.DrawImage(_icon, new RectangleF(12 * s, 11 * s, 18 * s, 18 * s));
            int rightEdge = (int)(Width - (ShowMin ? 100 : 54) * s);
            int titleX = (int)(38 * s);
            int avail = Math.Max(0, rightEdge - titleX);
            Font tFont = _titleFont;
            bool ownFont = false;
            int tW = TextRenderer.MeasureText(g, TitleText, tFont, new Size(int.MaxValue, (int)Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            float[] shrinkSizes = { 11f, 10f, 9.5f };
            int si2 = 0;
            while (tW > avail && si2 < shrinkSizes.Length)
            {
                var nf = new Font("Microsoft YaHei UI", shrinkSizes[si2], FontStyle.Bold);
                int w2 = TextRenderer.MeasureText(g, TitleText, nf, new Size(int.MaxValue, (int)Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
                if (ownFont) tFont.Dispose();
                tFont = nf; ownFont = true; tW = w2; si2++;
            }
            var tFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            if (tW > avail) tFlags |= TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(g, TitleText, tFont, new Rectangle(titleX, 0, Math.Min(tW + 6, avail), (int)Height), Pal.Text, tFlags);
            if (ownFont) tFont.Dispose();
            if (!string.IsNullOrEmpty(SubText))
            {
                int subX = titleX + Math.Min(tW, avail) + (int)(12 * s);
                int subAvail = rightEdge - subX;
                int subNeed = TextRenderer.MeasureText(g, SubText, _verFont, new Size(int.MaxValue, (int)Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
                if (subNeed > 0 && subAvail >= subNeed)
                    TextRenderer.DrawText(g, SubText, _verFont, new Rectangle(subX, 0, subAvail, (int)Height), Pal.Mute, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
            int half = Math.Max(4, Height / 10);
            if (ShowMin) DrawGlyph(g, (int)(Width - 69 * s), Height / 2, false, _minAmt.Value, half);
            DrawGlyph(g, (int)(Width - 23 * s), Height / 2, true, _closeAmt.Value, half);
        }
        void DrawGlyph(Graphics g, int cx, int cy, bool isClose, double amt, int half)
        {
            Color gc = Gfx.Mix(Pal.Sub, Color.White, amt);
            using (var pen = new Pen(gc, 1.6f))
            {
                if (isClose)
                {
                    g.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
                    g.DrawLine(pen, cx + half, cy - half, cx - half, cy + half);
                }
                else g.DrawLine(pen, cx - half, cy, cx + half, cy);
            }
        }
    }

    class StatusCard : CardPanel
    {
        public string ServerText = Txt.ServerText;
        public Color ServerColor = Color.FromArgb(94, 103, 115);
        public string TunnelText = Txt.TunnelText;
        public Color TunnelColor = Color.FromArgb(94, 103, 115);
        public string AddrText = Txt.AddrText;
        public Color AddrColor = Color.FromArgb(94, 103, 115);
        public string StateText = "";
        Image _art;
        Font _stateFont = new Font("Microsoft YaHei UI", 9f);
        readonly AnimFloat _pulseSrv = new AnimFloat(1);
        readonly AnimFloat _pulseTun = new AnimFloat(1);
        public StatusCard()
        {
            Font = new Font("Microsoft YaHei UI", 9f);
            try { string png = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.png"); if (File.Exists(png)) _art = Image.FromFile(png); } catch { }
            _pulseSrv.Changed = () => { if (!IsDisposed) Invalidate(); };
            _pulseTun.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        // row 0 = server, 1 = tunnel: pulse while that row is in a
        // transitional ("starting") state, static dot otherwise (v6.4)
        public void SetPulse(int row, bool on)
        {
            var f = row == 0 ? _pulseSrv : _pulseTun;
            if (on) { if (!f.IsPulsing) f.StartPulse(); }
            else f.StopPulse();
        }
        float Cs { get { return Height / 148f; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Cs;
            DrawRow(g, s, 14, ServerText, ServerColor, _pulseSrv.Value);
            DrawRow(g, s, 46, TunnelText, TunnelColor, _pulseTun.Value);
            DrawRow(g, s, 78, AddrText, AddrColor, 1.0);
            if (!string.IsNullOrEmpty(StateText))
                TextRenderer.DrawText(g, StateText, _stateFont, new Rectangle((int)(18 * s), (int)(114 * s), (int)(484 * s), (int)(24 * s)), Pal.Sub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            if (_art != null)
            {
                float boxX = 428 * s, boxY = 40 * s, boxW = 68 * s, boxH = 68 * s;
                float sc = Math.Min(boxW / _art.Width, boxH / _art.Height);
                float w = _art.Width * sc, h = _art.Height * sc;
                g.DrawImage(_art, boxX + (boxW - w) / 2f, boxY + (boxH - h) / 2f, w, h);
            }
        }
        void DrawRow(Graphics g, float s, int yLogical, string text, Color c, double pulse)
        {
            if (c == Color.Red) c = Pal.Danger;
            float rowX = 10 * s, rowY = yLogical * s, rowH = 28 * s;
            float cy = rowY + rowH / 2f;
            Color dc = pulse >= 0.999 ? c : Gfx.Mix(c, Pal.Card, 1 - pulse);
            using (var halo = new SolidBrush(Color.FromArgb((int)(58 * pulse), c))) g.FillEllipse(halo, rowX + 4, cy - 9, 18, 18);
            using (var dot = new SolidBrush(dc)) g.FillEllipse(dot, rowX + 9, cy - 4, 8, 8);
            var rect = new Rectangle((int)(rowX + 30), (int)rowY, (int)(330 * s - 30), (int)rowH);
            TextRenderer.DrawText(g, text ?? "", Font, rect, Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    class SegControl : Control
    {
        public string[] Items = new string[0];
        public int SelectedIndex = 0;
        public event EventHandler Changed;
        double _fade = 1.0;
        Font _boldFont;
        int _hoverIdx = -1;
        readonly AnimFloat _hoverAmt = new AnimFloat(0);
        readonly AnimFloat _selPos = new AnimFloat(0);
        public SegControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            _hoverAmt.Changed = () => { if (!IsDisposed) Invalidate(); };
            _selPos.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        // 1 = fully shown; 0 = fully blended into the card behind (v6.3 collapse fade)
        public double Fade
        {
            get { return _fade; }
            set { if (Math.Abs(value - _fade) > 0.004) { _fade = value; Invalidate(); } }
        }
        Color BlendC(Color c) { return _fade >= 0.999 ? c : Gfx.Mix(c, Pal.Card, 1.0 - _fade); }
        Font BoldFont()
        {
            if (_boldFont == null || _boldFont.Size != Font.Size || _boldFont.FontFamily.Name != Font.FontFamily.Name)
            {
                if (_boldFont != null) _boldFont.Dispose();
                _boldFont = new Font(Font, FontStyle.Bold);
            }
            return _boldFont;
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            using (var back = new SolidBrush(Pal.Card)) g.FillRectangle(back, ClientRectangle);
            if (_fade <= 0.004 || Items == null || Items.Length == 0) return;
            // Device-px geometry snapped to the pixel grid (the corner-dot fix):
            // logical tokens at this control's 32px reference height are
            // track radius 8, inset 2, segment radius 6 (concentric rule),
            // i.e. 12 / 3 / 9 device px at 150% scaling - all integers.
            double s = Height / 32.0; if (s <= 0) s = 1;
            int inset = Math.Max(1, (int)Math.Round(2 * s));
            int trackR = Math.Max(2, (int)Math.Round(8 * s));
            int segR = Math.Max(1, trackR - inset);
            var track = new Rectangle(0, 0, Width, Height);
            using (var p = Gfx.RoundRect(track, trackR))
            using (var b = new SolidBrush(BlendC(Pal.Inset))) g.FillPath(b, p);
            int n = Items.Length;
            int[] segB = SegBounds();
            float segSize = SegFontSize();
            Font segFont = Font;
            bool ownSegFont = false;
            if (Math.Abs(segSize - Font.Size) > 0.01f) { segFont = new Font(Font.FontFamily, segSize, Font.Style); ownSegFont = true; }
            double pos = _selPos.Value;
            if (pos < 0) pos = 0; if (pos > n - 1) pos = n - 1;
            // v6.5 filled bar: the segments partition the track exactly and
            // share one device-snapped boundary, so no dark gap can open
            // between them; the track path used as clip rounds the outer
            // corners. The selection is a filled thumb that SLIDES with
            // _selPos (190ms ease-out) instead of jumping between segments.
            var clipState = g.Save();
            using (var clip = Gfx.RoundRect(track, trackR)) g.SetClip(clip);
            if (_hoverIdx >= 0 && _hoverIdx < n && _hoverAmt.Value > 0.003 && Math.Abs(pos - _hoverIdx) > 0.6)
            {
                int hx0 = segB[_hoverIdx], hx1 = segB[_hoverIdx + 1];
                using (var b = new SolidBrush(BlendC(Gfx.Mix(Pal.Inset, Color.White, 0.08 * _hoverAmt.Value)))) g.FillRectangle(b, hx0, 0, hx1 - hx0, Height);
            }
            int tx0 = SegX(segB, pos), tx1 = SegX(segB, pos + 1.0);
            using (var b = new SolidBrush(BlendC(Color.FromArgb(52, 58, 68)))) g.FillRectangle(b, tx0, 0, tx1 - tx0, Height);
            g.Restore(clipState);
            using (var pen = new Pen(BlendC(Pal.Border), 1f))
            using (var p = Gfx.RoundRect(track, trackR)) g.DrawPath(pen, p);
            Font segBold = null;
            for (int i = 0; i < n; i++)
            {
                var sr = new Rectangle(segB[i], 0, Math.Max(1, segB[i + 1] - segB[i]), Height);
                double emph = 1.0 - Math.Abs(pos - i); if (emph < 0) emph = 0;
                Color c = Gfx.Mix(Pal.Sub, Pal.Text, emph);
                if (i == _hoverIdx && emph < 0.5) c = Gfx.Mix(c, Pal.Text, _hoverAmt.Value * (1.0 - emph));
                // v6.22.3: 段宽按译文实测分配（不再等分），每段独立裁剪 +
                // 必要时省略号，法/西/俄长标题不会侵入相邻段。
                Font useFont = segFont;
                if (emph > 0.5)
                {
                    if (segBold == null) segBold = new Font(Font.FontFamily, segSize, FontStyle.Bold);
                    useFont = segBold;
                }
                var segFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
                if (TextFit.Measure(Items[i], useFont) > sr.Width - 4) segFlags |= TextFormatFlags.EndEllipsis;
                var segClip = g.Save();
                g.SetClip(sr);
                TextRenderer.DrawText(g, Items[i], useFont, sr, BlendC(c), segFlags);
                g.Restore(segClip);
            }
            if (segBold != null) { try { segBold.Dispose(); } catch { } }
            if (ownSegFont) { try { segFont.Dispose(); } catch { } }
            if (Focused && ShowFocusCues) Gfx.DrawFocusRing(g, new Rectangle(0, 0, Width, Height), trackR, g.DpiX / 96f);
        }
        // v6.22.3: 段宽按当前语言实测文本分配；总宽不够时整体降字号（下限 7pt）
        int[] SegBounds()
        {
            int n = Items == null ? 0 : Items.Length;
            int[] b = new int[n + 1];
            if (n == 0) return b;
            float size = SegFontSize();
            int[] need = new int[n];
            int sum = 0;
            using (var mf = new Font(Font.FontFamily, size, Font.Style))
            {
                for (int i = 0; i < n; i++) { need[i] = TextFit.Measure(Items[i], mf) + 12; sum += need[i]; }
            }
            int total = Math.Max(1, Width);
            int acc = 0;
            for (int i = 0; i < n; i++)
            {
                int wi = sum >= total ? (int)Math.Floor((double)need[i] * total / sum) : need[i] + (total - sum) / n;
                if (wi < 1) wi = 1;
                b[i] = acc;
                acc += wi;
            }
            b[n] = total;
            return b;
        }
        float SegFontSize()
        {
            int n = Items == null ? 0 : Items.Length;
            if (n == 0) return Font.Size;
            float size = Font.Size;
            while (size > TextFit.MinSize)
            {
                int need = 0;
                using (var mf = new Font(Font.FontFamily, size, Font.Style))
                    for (int i = 0; i < n; i++) need += TextFit.Measure(Items[i], mf) + 12;
                if (need <= Width) break;
                size -= 0.5f;
            }
            return size;
        }
        static int SegX(int[] b, double p)
        {
            int n = b.Length - 1;
            if (n <= 0) return 0;
            if (p <= 0) return b[0];
            if (p >= n) return b[n];
            int i = (int)Math.Floor(p);
            double t = p - i;
            return (int)Math.Round(b[i] + t * (b[i + 1] - b[i]));
        }
        int SegIndexAt(int x)
        {
            if (Items == null || Items.Length == 0) return 0;
            int[] b = SegBounds();
            for (int i = 0; i < Items.Length; i++) if (x < b[i + 1]) return i;
            return Items.Length - 1;
        }
        void Select(int idx)
        {
            if (idx == SelectedIndex) return;
            int fromIdx = SelectedIndex;
            SelectedIndex = idx;
            // v6.15: swap panes BEFORE starting the slide clock - the swap
            // is synchronous UI work and used to eat the head of the ease,
            // which read as a stutter on short (adjacent) moves.
            if (Changed != null) Changed(this, EventArgs.Empty);
            int steps = Math.Abs(idx - fromIdx); if (steps < 1) steps = 1;
            int dur = 140 + 40 * steps; if (dur > 260) dur = 260;
            _selPos.Set(idx, dur);
            _hoverAmt.Set(0, 80);
            Invalidate();
        }
        // v6.13: restore the remembered tab at startup via the click path
        public void Restore613(int idx) { if (idx > 0 && idx < Items.Length) Select(idx); }
        // v6.16: programmatic page switch through the normal click path
        public void SelectPublic(int idx) { if (idx >= 0 && idx < Items.Length) Select(idx); }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Items == null || Items.Length == 0) return;
            int idx = SegIndexAt(e.X);
            if (idx < 0) idx = 0;
            if (idx >= Items.Length) idx = Items.Length - 1;
            if (idx != _hoverIdx)
            {
                _hoverIdx = idx;
                _hoverAmt.Set(idx != SelectedIndex ? 1 : 0, 120);
                Invalidate();
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIdx = -1;
            _hoverAmt.Set(0, 80);
            Invalidate();
            base.OnMouseLeave(e);
        }
        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right) return true;
            return base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (Items == null || Items.Length == 0) return;
            int idx = SelectedIndex;
            if (e.KeyCode == Keys.Left) idx = Math.Max(0, SelectedIndex - 1);
            else if (e.KeyCode == Keys.Right) idx = Math.Min(Items.Length - 1, SelectedIndex + 1);
            else return;
            if (idx != SelectedIndex)
            {
                Select(idx);
                e.Handled = true;
            }
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Items == null || Items.Length == 0) return;
            int idx = SegIndexAt(e.X);
            if (idx < 0) idx = 0;
            if (idx >= Items.Length) idx = Items.Length - 1;
            Select(idx);
        }
    }

    // Owner-drawn label (v6.5.2): on the user's machine, double-clicking a
    // plain WinForms Label copies its Text to the clipboard (verified with a
    // clipboard watcher: owner = this app, content = the label's text, for
    // both main-form labels). This control paints its caption itself and
    // keeps the string in a private field instead of Control.Text, so there
    // is no native label text for anything to copy.
    class TextLabel : Control
    {
        string _caption = "";
        public bool TopAlign = false;
        public bool WordWrap = false; // v6.6: multi-line wrapped text (dialog messages)
        public Color BackdropColor = Pal.Card;
        public float TextShiftX = 0, TextShiftY = 0; // logical px, tuned so the ink lands where the old Label put it
        public string Caption
        {
            get { return _caption; }
            set { string v = value ?? ""; if (v != _caption) { _caption = v; Invalidate(); } }
        }
        public TextLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
        }
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set { if (base.ForeColor != value) { base.ForeColor = value; Invalidate(); } }
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            Color bg = BackColor;
            if (bg.A == 0) bg = BackdropColor;
            using (var b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, ClientRectangle);
            if (_caption.Length > 0)
            {
                TextFormatFlags flags;
                if (WordWrap) flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
                else flags = TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | (TopAlign ? TextFormatFlags.Top : TextFormatFlags.VerticalCenter);
                float sc = e.Graphics.DpiX / 96f;
                var rc = ClientRectangle;
                rc.Offset((int)Math.Round(TextShiftX * sc), (int)Math.Round(TextShiftY * sc));
                // v6.22.3: 单行标签按可用宽度缩字号（下限 7pt），再省略；
                // 换行标签保持原字号，由高度容纳多行。
                Font useFont = Font;
                Font tmpFont = null;
                try
                {
                    if (!WordWrap)
                    {
                        float fs = TextFit.FitSize(_caption, Font, Math.Max(1, rc.Width), TextFit.MinSize);
                        if (Math.Abs(fs - Font.Size) > 0.01f) { tmpFont = new Font(Font.FontFamily, fs, Font.Style); useFont = tmpFont; }
                    }
                    TextRenderer.DrawText(e.Graphics, _caption, useFont, rc, ForeColor, flags);
                }
                finally { if (tmpFont != null) tmpFont.Dispose(); }
            }
        }
    }

    // ---------- 日志文本框（v6.6） ----------
    // Multiline EDIT with hidden native scrollbars: draws its own empty-state
    // placeholder after the base paint, scrolls itself on mouse wheel (the
    // EDIT ignores the wheel without bar styles), and reports scroll activity
    // so the slim overlay bars + jump-to-bottom chip can stay in sync.
    // ---------- v6.12 visual controls ----------
    // A single-line input with a painted hint (themed placeholder), drawn
    // the same way LogTextBox draws its empty-state text.
    class HintTextBox : TextBox
    {
        public string Hint = "";
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x000F && TextLength == 0 && !Focused && Hint.Length > 0)
            {
                // v6.22.4: 占位文字按可用宽度缩字号（下限 7pt），仍放不下才省略；
                // 法/西/德/俄的长占位句不再被词中硬裁。
                try
                {
                    using (var g = Graphics.FromHwnd(Handle))
                    {
                        int avail = Math.Max(1, ClientSize.Width - 8);
                        float fs = TextFit.FitSize(Hint, Font, avail, TextFit.MinSize);
                        Font hf = Font;
                        bool own = false;
                        if (Math.Abs(fs - Font.Size) > 0.01f) { hf = new Font(Font.FontFamily, fs, Font.Style); own = true; }
                        try
                        {
                            var flags = TextFormatFlags.Left | TextFormatFlags.SingleLine;
                            if (TextFit.Measure(Hint, hf) > avail) flags |= TextFormatFlags.EndEllipsis;
                            TextRenderer.DrawText(g, Hint, hf, new Rectangle(4, 3, avail, Math.Max(1, Font.Height + 2)), Pal.Mute, flags);
                        }
                        finally { if (own) hf.Dispose(); }
                    }
                }
                catch { }
            }
        }
    }

    // Owner-drawn checkbox in the app palette: the native checkbox paints
    // a bright white box that fights the dark theme everywhere it appears.
    class DarkCheckBox : Control
    {
        bool _checked;
        bool _hover;
        readonly AnimFloat _chkAmt614 = new AnimFloat(0); // v6.14: state-color lerp
        readonly AnimFloat _hovAmt614 = new AnimFloat(0);
        public event EventHandler CheckedChanged;
        public bool Checked
        {
            get { return _checked; }
            set { if (_checked != value) { _checked = value; _chkAmt614.Set(value ? 1 : 0, 120); Invalidate(); var h = CheckedChanged; if (h != null) h(this, EventArgs.Empty); } }
        }
        public DarkCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            _chkAmt614.Changed = () => { if (!IsDisposed) Invalidate(); };
            _hovAmt614.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        float S { get { return Height / 24f; } }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; _hovAmt614.Set(1, 120); Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _hovAmt614.Set(0, 120); Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location)) Checked = !Checked;
            base.OnMouseUp(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) { Checked = !Checked; e.Handled = true; }
            base.OnKeyDown(e);
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float sc = S;
            using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, ClientRectangle);
            float bs = 14 * sc;
            float by = (Height - bs) / 2f;
            var box = new RectangleF(1, by, bs, bs);
            Color border = Gfx.Mix(Gfx.Mix(Pal.Border, Pal.Sub, _hovAmt614.Value), Pal.InfoBlue, _chkAmt614.Value); // v6.14
            Color fill = Gfx.Mix(Color.FromArgb(17, 20, 24), Pal.InfoBlue, _chkAmt614.Value); // v6.14
            if (!Enabled) { border = Pal.Mute; fill = Gfx.Mix(fill, Pal.Mute, 0.4); }
            using (var path = Gfx.RoundRect(Rectangle.Round(box), Math.Max(2, (int)(3 * sc))))
            {
                using (var fb = new SolidBrush(fill)) g.FillPath(fb, path);
                using (var pen = new Pen(border, Math.Max(1f, sc))) g.DrawPath(pen, path);
            }
            if (_chkAmt614.Value > 0.02)
            {
                var pts = new PointF[]
                {
                    new PointF(box.X + 3.2f * sc, box.Y + 7.2f * sc),
                    new PointF(box.X + 6.0f * sc, box.Y + 10.2f * sc),
                    new PointF(box.X + 10.8f * sc, box.Y + 3.6f * sc)
                };
                using (var pen = new Pen(Color.FromArgb((int)(255 * _chkAmt614.Value), Color.White), Math.Max(1.2f, 1.7f * sc)))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLines(pen, pts);
                }
            }
            if (Focused && ShowFocusCues) Gfx.DrawFocusRing(g, new Rectangle(0, 0, Width, Height), 4, sc);
            Color tc = Enabled ? ForeColor : Pal.Mute;
            int tx = (int)(bs + 8 * sc);
            // v6.22.3: 复选框文字按可用宽度缩字号（下限 7pt），仍放不下才省略，
            // 德语/法语的长选项不再被硬裁。
            float csz = TextFit.FitSize(Text, Font, Math.Max(1, Width - tx - 2), TextFit.MinSize);
            Font cf = Font;
            bool ownCf = false;
            if (Math.Abs(csz - Font.Size) > 0.01f) { cf = new Font(Font.FontFamily, csz, Font.Style); ownCf = true; }
            try
            {
                TextRenderer.DrawText(g, Text, cf, new Rectangle(tx, 0, Width - tx, Height), tc, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
            finally { if (ownCf) cf.Dispose(); }
        }
    }

    // Themed owner-draw for list boxes: the native selection is a solid
    // system-blue bar; ours is a rounded, low-tint accent surface.
    static class ThemedList
    {
        public static void Attach(ListBox lb)
        {
            lb.DrawMode = DrawMode.OwnerDrawFixed;
            lb.HandleCreated += (snd, ev) => { try { lb.ItemHeight = Math.Max(16, lb.Font.Height + 8); } catch { } };
            lb.DrawItem += (snd, e) =>
            {
                if (e.Index < 0) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(lb.BackColor)) g.FillRectangle(b, e.Bounds);
                bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                var rc = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 1, e.Bounds.Width - 4, e.Bounds.Height - 2);
                if (sel)
                {
                    using (var path = Gfx.RoundRect(rc, 5))
                    using (var b2 = new SolidBrush(Gfx.Mix(lb.BackColor, Pal.InfoBlue, 0.34))) g.FillPath(b2, path);
                }
                string txt = lb.Items[e.Index] != null ? lb.Items[e.Index].ToString() : "";
                TextRenderer.DrawText(g, txt, lb.Font, new Rectangle(rc.X + 8, rc.Y, rc.Width - 12, rc.Height), Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            };
        }
    }

    // Themed owner-draw for combo boxes: a focused native combo paints
    // its whole field system blue; ours stays on-palette in every state.
    static class ThemedCombo
    {
        public static void Attach(ComboBox cb)
        {
            cb.DrawMode = DrawMode.OwnerDrawFixed;
            cb.DrawItem += (snd, e) =>
            {
                var g = e.Graphics;
                bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bg = sel ? Gfx.Mix(cb.BackColor, Pal.InfoBlue, 0.38) : cb.BackColor;
                using (var b = new SolidBrush(bg)) g.FillRectangle(b, e.Bounds);
                if (e.Index >= 0 && e.Index < cb.Items.Count)
                {
                    string txt = cb.Items[e.Index] != null ? cb.Items[e.Index].ToString() : "";
                    TextRenderer.DrawText(g, txt, cb.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height), cb.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                }
            };
        }
    }

    class LogTextBox : TextBox
    {
        public string Placeholder = "";
        public event EventHandler Scrolled;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr wp, IntPtr lp);

        public int LineCount() { return Send(0xBA, 0, 0); }          // EM_GETLINECOUNT
        public int FirstVisible() { return Send(0xCE, 0, 0); }       // EM_GETFIRSTVISIBLELINE
        public int VisibleLines() { return Math.Max(1, ClientSize.Height / Math.Max(1, Font.Height)); }
        public void ScrollLines(int dy) { if (dy != 0) Send(0xB6, 0, dy); }   // EM_LINESCROLL vertical
        public void ScrollChars(int dx) { if (dx != 0) Send(0xB6, dx, 0); }   // EM_LINESCROLL horizontal (chars)
        // Horizontal scroll offset in pixels. GetScrollPos reports nothing
        // without a native bar style, but EM_POSFROMCHAR on the first visible
        // line's first character gives its (negative) x directly (QA-verified:
        // EM_LINESCROLL does scroll horizontally; only the API readout is dead).
        public int HOffsetPx()
        {
            if (!IsHandleCreated) return 0;
            int idx = Send(0xBB, FirstVisible(), 0); // EM_LINEINDEX
            if (idx < 0) idx = 0;
            int packed = Send(0xD6, idx, 0);          // EM_POSFROMCHAR
            int x = (short)(packed & 0xFFFF);
            return x < 0 ? -x : 0;
        }
        int Send(int msg, int wp, int lp)
        {
            if (!IsHandleCreated) return 0;
            try { return SendMessage(Handle, msg, (IntPtr)wp, (IntPtr)lp).ToInt32(); } catch { return 0; }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x020A) // WM_MOUSEWHEEL: no bar styles -> the EDIT won't scroll; do it manually
            {
                int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                int notches = delta / 120; if (notches == 0) notches = delta > 0 ? 1 : -1;
                int lines = Math.Max(1, SystemInformation.MouseWheelScrollLines);
                ScrollLines(-notches * lines);
                if (Scrolled != null) Scrolled(this, EventArgs.Empty);
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == 0x020E) // WM_MOUSEHWHEEL (tilt wheel)
            {
                int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                ScrollChars(delta > 0 ? -3 : 3);
                if (Scrolled != null) Scrolled(this, EventArgs.Empty);
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
            if (m.Msg == 0x000F) // WM_PAINT: empty-state placeholder over the base paint
            {
                if (TextLength == 0 && Placeholder.Length > 0)
                {
                    // v6.22.4: 占位文字按可用宽度缩字号（下限 7pt），仍放不下才省略
                    try
                    {
                        using (var g = Graphics.FromHwnd(Handle))
                        {
                            int avail = Math.Max(1, ClientSize.Width - 8);
                            float fs = TextFit.FitSize(Placeholder, Font, avail, TextFit.MinSize);
                            Font pf = Font;
                            bool own = false;
                            if (Math.Abs(fs - Font.Size) > 0.01f) { pf = new Font(Font.FontFamily, fs, Font.Style); own = true; }
                            try
                            {
                                var flags = TextFormatFlags.Left | TextFormatFlags.SingleLine;
                                if (TextFit.Measure(Placeholder, pf) > avail) flags |= TextFormatFlags.EndEllipsis;
                                TextRenderer.DrawText(g, Placeholder, pf, new Rectangle(4, 3, avail, Math.Max(1, Font.Height + 2)), Pal.Mute, flags);
                            }
                            finally { if (own) pf.Dispose(); }
                        }
                    }
                    catch { }
                }
            }
            else if (m.Msg == 0x0115 || m.Msg == 0x0114) // WM_VSCROLL / WM_HSCROLL
            {
                if (Scrolled != null) Scrolled(this, EventArgs.Empty);
            }
        }
    }

    // ---------- 自绘细滚动条（v6.6） ----------
    // Slim dark overlay bar driving a LogTextBox whose native bars are hidden.
    // Vertical mode works in lines; horizontal mode works in characters and
    // reads the pixel offset via GetScrollPos (the EDIT still maintains it
    // with its bars hidden), converting through a measured character width.
    class SlimScrollBar : Control
    {
        public bool Horizontal = false;
        public LogTextBox Target;
        public Func<int> MaxChars; // horizontal only: longest line so far, in chars
        bool _hover = false, _drag = false;
        readonly AnimFloat _hovAmt614 = new AnimFloat(0); // v6.14: state-color lerp
        int _dragStartPos = 0, _dragStartFirst = 0;
        float _charW = 0;

        public SlimScrollBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = false;
            _hovAmt614.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        public bool Needed
        {
            get
            {
                if (Target == null || !Target.IsHandleCreated) return false;
                if (!Horizontal) return Target.LineCount() > Target.VisibleLines();
                return (MaxChars != null ? MaxChars() : 0) > VisibleChars();
            }
        }
        public void Sync()
        {
            bool need = false;
            try { need = Needed; } catch { }
            if (Visible != need) Visible = need;
            if (need) Invalidate();
        }
        float CharWidth()
        {
            if (_charW > 0) return _charW;
            try { _charW = TextRenderer.MeasureText("MMMMMMMMMM", Target.Font).Width / 10f; } catch { }
            if (_charW <= 0) _charW = 7f;
            return _charW;
        }
        int VisibleChars()
        {
            if (Target == null) return 1;
            return Math.Max(1, (int)(Target.ClientSize.Width / CharWidth()));
        }
        int HFirst()
        {
            try { return (int)(Target.HOffsetPx() / CharWidth()); } catch { return 0; }
        }
        Rectangle ThumbRect()
        {
            var r = ClientRectangle;
            if (Target == null || !Target.IsHandleCreated) return new Rectangle(1, 1, Math.Max(1, r.Width - 2), Math.Max(1, r.Height - 2));
            if (!Horizontal)
            {
                int total = Target.LineCount(), vis = Target.VisibleLines();
                if (total <= vis) return new Rectangle(1, 0, Math.Max(1, r.Width - 2), r.Height);
                int th = Math.Max(28, (int)((double)r.Height * vis / total));
                int range = Math.Max(1, total - vis);
                int first = Math.Min(Target.FirstVisible(), range);
                int ty = r.Height > th ? (int)((double)(r.Height - th) * first / range) : 0;
                return new Rectangle(1, ty, Math.Max(1, r.Width - 2), th);
            }
            else
            {
                int vis = VisibleChars();
                int total = Math.Max(vis + 1, MaxChars != null ? MaxChars() : vis + 1);
                int tw = Math.Max(28, (int)((double)r.Width * vis / total));
                int range = Math.Max(1, total - vis);
                int first = Math.Min(HFirst(), range);
                int tx = r.Width > tw ? (int)((double)(r.Width - tw) * first / range) : 0;
                return new Rectangle(tx, 1, tw, Math.Max(1, r.Height - 2));
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var b = new SolidBrush(Pal.Inset)) e.Graphics.FillRectangle(b, ClientRectangle);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var t = ThumbRect();
            Color c = _drag ? Color.FromArgb(96, 106, 120) : Gfx.Mix(Color.FromArgb(58, 65, 76), Color.FromArgb(78, 86, 99), _hovAmt614.Value); // v6.14
            int rad = Math.Max(1, Math.Min(4, Math.Min(t.Width, t.Height) / 2));
            using (var path = Gfx.RoundRect(t, rad))
            using (var b = new SolidBrush(c)) e.Graphics.FillPath(b, path);
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; _hovAmt614.Set(1, 150); Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _hovAmt614.Set(0, 150); Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Target != null)
            {
                if (Horizontal) Target.ScrollChars(e.Delta > 0 ? -3 : 3);
                else Target.ScrollLines(e.Delta > 0 ? -3 : 3);
                Sync();
            }
            base.OnMouseWheel(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Target != null)
            {
                var t = ThumbRect();
                int pos = Horizontal ? e.X : e.Y;
                int tPos = Horizontal ? t.X : t.Y;
                int tLen = Horizontal ? t.Width : t.Height;
                if (pos >= tPos && pos < tPos + tLen)
                {
                    _drag = true; _dragStartPos = pos;
                    _dragStartFirst = Horizontal ? HFirst() : Target.FirstVisible();
                    Capture = true; Invalidate();
                }
                else
                {
                    int page = Horizontal ? VisibleChars() : Target.VisibleLines();
                    int dir = pos < tPos ? -1 : 1;
                    if (Horizontal) Target.ScrollChars(dir * page); else Target.ScrollLines(dir * page);
                    Sync();
                }
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_drag && Target != null)
            {
                var t = ThumbRect();
                if (!Horizontal)
                {
                    int total = Target.LineCount(), vis = Target.VisibleLines();
                    int track = Math.Max(1, ClientSize.Height - t.Height);
                    int want = _dragStartFirst + (int)Math.Round((double)(e.Y - _dragStartPos) / track * Math.Max(1, total - vis));
                    Target.ScrollLines(want - Target.FirstVisible());
                }
                else
                {
                    int vis = VisibleChars();
                    int total = Math.Max(vis + 1, MaxChars != null ? MaxChars() : vis + 1);
                    int track = Math.Max(1, ClientSize.Width - t.Width);
                    int want = _dragStartFirst + (int)Math.Round((double)(e.X - _dragStartPos) / track * Math.Max(1, total - vis));
                    Target.ScrollChars(want - HFirst());
                }
                Sync();
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_drag) { _drag = false; Capture = false; Invalidate(); }
            base.OnMouseUp(e);
        }
    }

    // ---------- 监控页（v6.7） ----------
    // Third tab of the log panel: server process state/memory/CPU/player
    // connections, tunnel (frpc) memory/CPU, whole-machine up/down throughput
    // and system memory. MainForm samples once a second into the text fields;
    // this panel only paints, in the same inset style as the log boxes.
    // Coordinates scale by Height/290 like StatusCard's Height/148 rule.
    class StatsPanel : Panel
    {
        public string SrvState = Txt.SrvState;
        public Color SrvStateColor = Pal.Mute;
        public string SrvMem = "—", SrvMemCap = "", SrvCpu = "—", SrvConn = "—";
        public string SrvPlayers = "—";
        public string SrvVerPing = "—";
        public event EventHandler ManageClicked;
        public double SrvMemFrac = -1;
        public double[] SrvMemHist = new double[0]; // v6.9: last ~60 samples of SrvMemFrac
        public string TunMem = "—", TunCpu = "—";
        public string NetDown = "—", NetUp = "—";
        public string SysMem = "—";
        Font _hdrFont, _valFont;
        public StatsPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = new Font("Microsoft YaHei UI", 9f);
            TabStop = false;
            // v6.11: player management entry, sits on the players row
            var mb = new RoundButton();
            mb.Text = Txt.MbText;
            // v6.22.3: “玩家管理”按钮按当前语言实测宽度，右对齐到面板内缘
            int mbW = Math.Max(80, TextFit.Measure(Txt.MbText, this.Font) + 26);
            mb.SetBounds(472 - mbW, 120, mbW, 24);
            mb.BackColor = Pal.Surface;
            mb.ForeColor = Pal.Text;
            mb.Font = new Font("Microsoft YaHei UI", 9f);
            mb.Click += (snd, ev) => { var h = ManageClicked; if (h != null) h(this, EventArgs.Empty); };
            Controls.Add(mb);
        }
        float S { get { return Height / 290f; } }
        int X(double v) { return (int)Math.Round(v * S); }
        Font HdrFont()
        {
            if (_hdrFont == null || _hdrFont.Size != Font.Size - 0.5f || _hdrFont.FontFamily.Name != Font.FontFamily.Name)
            {
                if (_hdrFont != null) _hdrFont.Dispose();
                _hdrFont = new Font(Font.FontFamily, Font.Size - 0.5f, FontStyle.Bold);
            }
            return _hdrFont;
        }
        Font ValFont()
        {
            if (_valFont == null || _valFont.Size != Font.Size)
            {
                if (_valFont != null) _valFont.Dispose();
                _valFont = Fonts.Mono(Font.Size, FontStyle.Regular);
            }
            return _valFont;
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var back = new SolidBrush(Pal.Card)) e.Graphics.FillRectangle(back, ClientRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundRect(rect, Math.Max(2, X(8))))
            {
                using (var brush = new SolidBrush(Pal.Inset)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(Pal.InsetBorder, 1f)) e.Graphics.DrawPath(pen, path);
            }
        }
        void Header(Graphics g, int y, string text)
        {
            TextRenderer.DrawText(g, text, HdrFont(), new Rectangle(X(16), X(y), X(220), X(20)), Pal.Mute, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
        // v6.22.4: 标签列按当前语言实测宽度（下限 92 设计像素，上限 186），
        // 法/西/葡/俄的长标签（“Connexions des joueurs”等）不再在词中被截断。
        int LabelW()
        {
            int lw = X(92);
            string[] labels = { Txt.OnPaintRow, Txt.OnPaintRow2, Txt.OnPaintRow3, Txt.OnPaintDrawText, Txt.OnPaintRow4, Txt.OnPaintRow5, "CPU" };
            for (int i = 0; i < labels.Length; i++)
            {
                int w = TextFit.Measure(labels[i], Font) + X(6);
                if (w > lw) lw = w;
            }
            int max = X(186);
            return lw > max ? max : lw;
        }
        // 放不下时先降字号（下限 7pt），仍放不下才省略
        void FitLabel(Graphics g, Rectangle rc, string text)
        {
            Font lf = Font;
            Font tmp = null;
            try
            {
                if (TextFit.Measure(text, Font) > rc.Width)
                {
                    float fs = TextFit.FitSize(text, Font, rc.Width, TextFit.MinSize);
                    if (Math.Abs(fs - Font.Size) > 0.01f) { tmp = new Font(Font.FontFamily, fs, Font.Style); lf = tmp; }
                }
                var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
                if (TextFit.Measure(text, lf) > rc.Width) flags |= TextFormatFlags.EndEllipsis;
                TextRenderer.DrawText(g, text, lf, rc, Pal.Sub, flags);
            }
            finally { if (tmp != null) tmp.Dispose(); }
        }
        void Row(Graphics g, int y, int lw, int vx, string label, string value, Color vc)
        {
            FitLabel(g, new Rectangle(X(16), X(y), lw, X(21)), label);
            TextRenderer.DrawText(g, value, ValFont(), new Rectangle(vx, X(y), Math.Max(1, Width - vx - X(16)), X(21)), vc, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
        int TrackW(int vx)
        {
            int w = X(304) - vx;
            if (w > X(180)) w = X(180);
            if (w < X(60)) w = X(60);
            return w;
        }
        void Bar(Graphics g, int y, int vx, double frac, string cap)
        {
            var track = new Rectangle(vx, X(y), TrackW(vx), Math.Max(2, X(6)));
            using (var p = Gfx.RoundRect(track, Math.Max(1, X(3))))
            using (var b = new SolidBrush(Color.FromArgb(41, 46, 55))) g.FillPath(b, p);
            if (frac > 0)
            {
                int w = Math.Max(X(6), (int)(track.Width * Math.Min(1.0, frac)));
                var fill = new Rectangle(track.X, track.Y, Math.Min(w, track.Width), track.Height);
                using (var p = Gfx.RoundRect(fill, Math.Max(1, X(3))))
                using (var b = new SolidBrush(Pal.Green)) g.FillPath(b, p);
            }
            if (!string.IsNullOrEmpty(cap))
                TextRenderer.DrawText(g, cap, Font, new Rectangle(X(304), X(y) - X(6), Math.Max(X(40), Width - X(320)), X(18)), Pal.Mute, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
        // v6.9: sparkline of the server memory fraction (same 0..cap scale as
        // the bar above it), drawn in the strip under the bar.
        void Spark(Graphics g, int y, int vx)
        {
            var rc = new Rectangle(vx, X(y), TrackW(vx), X(10));
            using (var b = new SolidBrush(Color.FromArgb(41, 46, 55)))
                g.FillRectangle(b, new Rectangle(rc.X, rc.Bottom - 1, rc.Width, 1));
            var h = SrvMemHist;
            if (h == null || h.Length < 2) return;
            var pts = new PointF[h.Length];
            for (int i = 0; i < h.Length; i++)
            {
                // right-anchored: the newest sample sits at the right edge
                // and the line grows leftwards as history fills the minute
                float fx = rc.Right - (float)(h.Length - 1 - i) / 59f * rc.Width;
                double f = h[i]; if (f < 0) f = 0; if (f > 1) f = 1;
                float fy = rc.Bottom - 1 - (float)(f * (rc.Height - 2));
                pts[i] = new PointF(fx, fy);
            }
            using (var p = new Pen(Color.FromArgb(72, 144, 245), Math.Max(1f, S * 0.9f)))
                g.DrawLines(p, pts);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // v6.22.4: 所有行标签共用一列实测宽度，值列随之右移
            int lw = LabelW();
            int vx = X(16) + lw + X(4);
            Header(g, 4, Txt.OnPaintHeader);
            Row(g, 22, lw, vx, Txt.OnPaintRow, SrvState, SrvStateColor);
            Row(g, 40, lw, vx, Txt.OnPaintRow2, SrvMem, Pal.Text);
            Bar(g, 60, vx, SrvMemFrac, SrvMemCap);
            Spark(g, 71, vx);
            Row(g, 86, lw, vx, "CPU", SrvCpu, Pal.Text);
            Row(g, 104, lw, vx, Txt.OnPaintRow3, SrvConn, Pal.Text);
            // online player names (from the server log's join/leave lines)
            FitLabel(g, new Rectangle(X(16), X(122), lw, X(21)), Txt.OnPaintDrawText);
            TextRenderer.DrawText(g, SrvPlayers, Font, new Rectangle(vx, X(122), Math.Max(1, X(388) - vx), X(21)), Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            Row(g, 140, lw, vx, Txt.OnPaintRow4, SrvVerPing, Pal.Text);
            Header(g, 162, Txt.OnPaintHeader2);
            Row(g, 180, lw, vx, Txt.OnPaintRow2, TunMem, Pal.Text);
            Row(g, 198, lw, vx, "CPU", TunCpu, Pal.Text);
            Header(g, 220, Txt.OnPaintHeader3);
            // net row: down/up side by side (labels measured, values reflowed)
            int ld = Math.Max(X(40), TextFit.Measure(Txt.OnPaintDrawText2, Font) + X(2));
            int dvx = X(16) + ld + X(2);
            FitLabel(g, new Rectangle(X(16), X(238), ld, X(21)), Txt.OnPaintDrawText2);
            TextRenderer.DrawText(g, NetDown, ValFont(), new Rectangle(dvx, X(238), Math.Max(1, X(256) - X(8) - dvx), X(21)), Pal.AddrGreen, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            int lu = Math.Max(X(40), TextFit.Measure(Txt.OnPaintDrawText3, Font) + X(2));
            FitLabel(g, new Rectangle(X(256), X(238), lu, X(21)), Txt.OnPaintDrawText3);
            int uvx = X(256) + lu + X(2);
            TextRenderer.DrawText(g, NetUp, ValFont(), new Rectangle(uvx, X(238), Math.Max(1, Width - uvx - X(16)), X(21)), Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            Row(g, 256, lw, vx, Txt.OnPaintRow5, SysMem, Pal.Text);
        }
    }

    // ---------- 深色对话框基类（v6.6） ----------
    // Same invisible-frame trick as MainForm: a real caption/thick-frame
    // window with a zeroed non-client area, so the TitleBarPanel is the only
    // chrome and DWM still supplies rounded corners + shadow.
    class DarkDialog : Form
    {
        protected TitleBarPanel Bar;
        Size _wantClient;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr wp, IntPtr lp);
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);

        protected void InitChrome(string title, int contentW, int contentH)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Pal.Bg;
            ForeColor = Pal.Text;
            Font = new Font("Microsoft YaHei UI", 9f);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(contentW, contentH + 40);
            Bar = new TitleBarPanel();
            Bar.TitleText = title;
            Bar.SubText = "";
            Bar.ShowMin = false;
            Bar.SetBounds(0, 0, contentW, 40);
            Bar.DragRequested += (s, e) => { try { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero); } catch { } };
            Bar.CloseClicked += (s, e) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(Bar);
            try { string ico = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.ico"); if (File.Exists(ico)) Icon = new Icon(ico); } catch { }
        }
        // Shift every non-titlebar control below the bar, scale, and keep the
        // bar frontmost. Call once at the end of the derived constructor,
        // after ClientSize has its final value.
        protected void FinishChrome()
        {
            foreach (Control c in Controls) if (c != Bar) c.Top += 40;
            Bar.BringToFront();
            DpiScale.ScaleForm(this, ClientSize.Width, ClientSize.Height);
            _wantClient = ClientSize;
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // A ClientSize assigned before the handle exists is converted
            // through the (invisible) frame metrics and inflates the window,
            // leaving dead background strips right/bottom. With the NC area
            // zeroed the window size IS the client size, so pin it here.
            try { if (_wantClient.Width > 0 && Size != _wantClient) Size = _wantClient; } catch { }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x00C00000 | 0x00040000 | 0x00080000; // CAPTION | THICKFRAME | SYSMENU
                return cp;
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0083) // WM_NCCALCSIZE: zero the non-client area
            {
                if (m.WParam != IntPtr.Zero) { m.Result = IntPtr.Zero; return; }
                base.WndProc(ref m);
                return;
            }
            if (m.Msg == 0x0086) { m.Result = (IntPtr)1; return; } // WM_NCACTIVATE: always active
            base.WndProc(ref m);
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int v = 2; DwmSetWindowAttribute(Handle, 33, ref v, 4); } catch { }
        }
    }

    // ---------- 信息弹窗（v6.8 重做排版） ----------
    // The old box was one fixed 440x168 template for everything: a short
    // message floated at the top over a void, and a long error text simply
    // clipped. Now the message is laid out in OnLoad with the real device
    // fonts and width it will be painted with: the dialog takes the height
    // its content needs, the first paragraph reads as a headline (bigger,
    // full-bright) and the rest as body (a step smaller, softer), and a
    // small status dot by the headline gives the box its one accent -
    // red for errors, amber for a real choice, blue for plain info.
    class InfoBox : DarkDialog
    {
        readonly string[] _paras;
        readonly Color _dotColor;
        readonly bool _twoBtn;
        RoundButton _ok, _no;
        Font _headFont, _bodyFont;
        readonly List<Rectangle> _rects = new List<Rectangle>();
        readonly List<bool> _isHead = new List<bool>();
        Rectangle _dotRect;
        static readonly Color SoftText = Color.FromArgb(198, 206, 216);
        static readonly Color InfoBlue = Color.FromArgb(72, 144, 245);

        InfoBox(string message, string title, string okText, string cancelText)
        {
            string norm = (message ?? "").Replace("\r\n", "\n");
            var parts = new List<string>();
            foreach (var p in norm.Split(new string[] { "\n\n" }, StringSplitOptions.None))
            {
                string t = p.Trim('\n');
                if (t.Length > 0) parts.Add(t);
            }
            if (parts.Count == 0) parts.Add("");
            _paras = parts.ToArray();
            _twoBtn = cancelText != null;
            if (title.IndexOf(Txt.OnHandleCreatedIndexOf) >= 0 || title == Txt.OnHandleCreatedStr || norm.StartsWith(Txt.Msg)) _dotColor = Pal.Danger;
            else if (_twoBtn) _dotColor = Pal.Amber;
            else _dotColor = InfoBlue;
            InitChrome(title, 440, 200);
            // v6.22.3: 按钮宽度按当前语言实测（俄/法“立即重启”在 140px 下会被截）
            int okW = Math.Max(140, TextFit.Measure(okText, this.Font) + 28);
            int noW = Math.Max(110, TextFit.Measure(cancelText == null ? "" : cancelText, this.Font) + 28);
            if (okW > 260) okW = 260;
            _ok = new RoundButton();
            _ok.Primary = true;
            _ok.Text = okText;
            _ok.BackColor = Pal.Green;
            _ok.ForeColor = Color.White;
            _ok.BackdropColor = Pal.Bg;
            _ok.Click += (s, e) => { DialogResult = DialogResult.OK; };
            if (!_twoBtn)
            {
                _ok.SetBounds((440 - okW) / 2, 124, okW, 34);
                Controls.Add(_ok);
                AcceptButton = _ok;
                CancelButton = _ok;
            }
            else
            {
                _ok.SetBounds(Math.Max(20, (440 - (okW + noW + 30)) / 2), 124, okW, 34);
                Controls.Add(_ok);
                _no = new RoundButton();
                _no.Text = cancelText;
                _no.BackColor = Pal.Surface;
                _no.ForeColor = Color.White;
                _no.BackdropColor = Pal.Bg;
                _no.SetBounds(_ok.Left + okW + 30, 124, noW, 34);
                _no.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
                Controls.Add(_no);
                AcceptButton = _ok;
                CancelButton = _no;
            }
            FinishChrome();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e); // DarkDialog pins the nominal size first
            try { LayoutMessage(); } catch { }
        }

        // Measure with the exact fonts/width used at paint time, then take
        // only the height the content needs and recenter (the nominal size
        // was centered before we knew the real one).
        void LayoutMessage()
        {
            float sf = ClientSize.Width / 440f;
            var fam = Font.FontFamily;
            bool multi = _paras.Length > 1;
            _headFont = new Font(fam, Font.Size + (multi ? 2f : 1f), FontStyle.Regular);
            _bodyFont = new Font(fam, Font.Size + 0.5f, FontStyle.Regular);
            int textX = (int)Math.Round(42 * sf);
            int textW = ClientSize.Width - textX - (int)Math.Round(24 * sf);
            int textTop = (int)Math.Round((40 + 18) * sf);
            int gap = (int)Math.Round(10 * sf);
            var flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
            _rects.Clear(); _isHead.Clear();
            int y = textTop;
            using (var g = CreateGraphics())
            {
                for (int i = 0; i < _paras.Length; i++)
                {
                    bool head = i == 0;
                    var f = (multi && !head) ? _bodyFont : _headFont;
                    Size sz = TextRenderer.MeasureText(g, _paras[i], f, new Size(textW, int.MaxValue), flags);
                    _rects.Add(new Rectangle(textX, y, textW, sz.Height));
                    _isHead.Add(head);
                    y += sz.Height + gap;
                }
            }
            int textH = (y - gap) - textTop;
            var headRc = _rects[0];
            int dotD = Math.Max(6, (int)Math.Round(8 * sf));
            int dotY = headRc.Top + Math.Max(2, (int)Math.Round(4 * sf));
            _dotRect = new Rectangle((int)Math.Round(24 * sf), dotY, dotD, dotD);
            int btnTop = textTop + textH + (int)Math.Round(20 * sf);
            int bottom = btnTop + _ok.Height + (int)Math.Round(18 * sf);
            var wa = Screen.FromControl(this).WorkingArea;
            int maxH = (int)(wa.Height * 0.92);
            if (bottom > maxH) bottom = maxH;
            _ok.Top = btnTop;
            if (_no != null) _no.Top = btnTop;
            // Assign Size, not ClientSize: a ClientSize set after the handle
            // exists converts through the frame metrics and inflates the
            // window (+22x+56 of dead area). With the NC area zeroed the
            // window size IS the client size, so set it directly.
            Size = new Size(Width, bottom);
            Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(_dotColor)) g.FillEllipse(b, _dotRect);
            var flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
            for (int i = 0; i < _paras.Length && i < _rects.Count; i++)
            {
                bool head = _isHead[i];
                var f = (_paras.Length > 1 && !head) ? _bodyFont : _headFont;
                TextRenderer.DrawText(g, _paras[i], f, _rects[i], head ? Pal.Text : SoftText, flags);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { if (_headFont != null) _headFont.Dispose(); } catch { }
                try { if (_bodyFont != null) _bodyFont.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }

        public static DialogResult Show(string message) { return Show(message, Txt.ShowFriendlyErrorShow, Txt.ShowMsg, null); }
        public static DialogResult Show(string message, string title) { return Show(message, title, Txt.ShowMsg, null); }
        public static DialogResult Show(string message, string title, string okText, string cancelText)
        {
            using (var f = new InfoBox(message, title, okText, cancelText)) return f.ShowDialog();
        }
    }

    // ---------- 主窗体 ----------
    // ---------- server.properties 图形编辑（v6.11） ----------
    // Edits only the handful of keys a home host actually touches, and
    // rewrites the file line by line so comments, unknown keys and their
    // order all survive. online-mode is shown but never written here:
    // this crew plays on offline accounts and it must stay false.
    class PropsForm : DarkDialog
    {
        string _dir;
        bool _running;
        System.Collections.Generic.List<string> _lines = new System.Collections.Generic.List<string>();
        System.Collections.Generic.Dictionary<string, string> _kv = new System.Collections.Generic.Dictionary<string, string>();
        ComboBox _cbDiff, _cbMode;
        int _labelW = 140;
        TextBox _tbMax, _tbView, _tbSim, _tbSpawn, _tbMotd;
        DarkCheckBox _ckWhite, _ckPvp, _ckFly;

        public PropsForm(string serverDir, bool serverRunning)
        {
            _dir = serverDir; _running = serverRunning;
            InitChrome(Txt.SettingsFormShow2, 480, 462);
            // v6.22.3: 标签列按当前语言实测，输入控件右移并压缩宽度
            using (var lf = new Font("Microsoft YaHei UI", 9f))
            {
                string[] labs = { Txt.PropsFormAddCombo, Txt.PropsFormAddCombo2, Txt.PropsFormAddNum,
                    Txt.PropsFormAddNum2, Txt.PropsFormAddNum3, Txt.PropsFormAddNum4, Txt.PropsFormAddText };
                for (int i = 0; i < labs.Length; i++) _labelW = Math.Max(_labelW, TextFit.Measure(labs[i], lf) + 6);
            }
            if (_labelW > 210) _labelW = 210;
            string pf = System.IO.Path.Combine(_dir, "server.properties");
            bool have = System.IO.File.Exists(pf);
            if (have)
            {
                try
                {
                    foreach (var raw in System.IO.File.ReadAllLines(pf, Encoding.UTF8))
                    {
                        _lines.Add(raw);
                        var t = raw.Trim();
                        if (t.StartsWith("#") || t.StartsWith("!")) continue;
                        int eq = raw.IndexOf('=');
                        if (eq > 0) _kv[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 1).Trim();
                    }
                }
                catch { }
            }
            int y = 14;
            _cbDiff = AddCombo(Txt.PropsFormAddCombo, y, new string[] { Txt.PropsFormStr, Txt.PropsFormStr2, Txt.PropsFormStr3, Txt.PropsFormStr4 }, new string[] { "peaceful", "easy", "normal", "hard" }, Get("difficulty", "easy")); y += 36;
            _cbMode = AddCombo(Txt.PropsFormAddCombo2, y, new string[] { Txt.PropsFormStr5, Txt.PropsFormStr6, Txt.PropsFormStr7, Txt.PropsFormStr8 }, new string[] { "survival", "creative", "adventure", "spectator" }, Get("gamemode", "survival")); y += 36;
            _tbMax = AddNum(Txt.PropsFormAddNum, y, Get("max-players", "20")); y += 36;
            _tbView = AddNum(Txt.PropsFormAddNum2, y, Get("view-distance", "10")); y += 36;
            _tbSim = AddNum(Txt.PropsFormAddNum3, y, Get("simulation-distance", "10")); y += 36;
            _tbSpawn = AddNum(Txt.PropsFormAddNum4, y, Get("spawn-protection", "16")); y += 36;
            _tbMotd = AddText(Txt.PropsFormAddText, y, Get("motd", "A Minecraft Server")); y += 36;
            _ckWhite = AddCheck(Txt.PropsFormAddCheck, y, Get("white-list", "false") == "true"); y += 32;
            _ckPvp = AddCheck(Txt.PropsFormAddCheck2, y, Get("pvp", "true") == "true"); y += 32;
            _ckFly = AddCheck(Txt.PropsFormAddCheck3, y, Get("allow-flight", "false") == "true"); y += 36;
            var lbOn = new TextLabel();
            lbOn.WordWrap = true;
            lbOn.Caption = Txt.LbOnCaption;
            lbOn.SetBounds(20, y, 440, 40);
            lbOn.ForeColor = Pal.Mute; lbOn.BackColor = Pal.Bg; lbOn.Font = new Font("Microsoft YaHei UI", 9f);
            Controls.Add(lbOn); y += 48;
            var btnSave = new RoundButton();
            btnSave.Primary = true;
            btnSave.Text = Txt.BtnSaveText;
            btnSave.BackColor = Pal.Green; btnSave.ForeColor = Color.White; btnSave.BackdropColor = Pal.Bg;
            btnSave.SetBounds(240, y, 100, 34);
            btnSave.Click += (s2, e2) => { if (SaveProps()) DialogResult = DialogResult.OK; };
            Controls.Add(btnSave);
            var btnCancel = new RoundButton();
            btnCancel.Text = Txt.NoText;
            btnCancel.BackColor = Pal.Surface; btnCancel.ForeColor = Color.White; btnCancel.BackdropColor = Pal.Bg;
            btnCancel.SetBounds(352, y, 100, 34);
            btnCancel.Click += (s2, e2) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(btnCancel);
            if (!have)
            {
                btnSave.Enabled = false;
                InfoBox.Show(Txt.PropsFormShow, Txt.SettingsFormShow2);
            }
            FinishChrome();
        }
        string Get(string k, string def) { string v; return _kv.TryGetValue(k, out v) ? v : def; }
        TextLabel AddLbl(string text, int y)
        {
            var l = new TextLabel();
            l.Caption = text;
            l.SetBounds(20, y, _labelW, 26);
            l.ForeColor = Pal.Sub; l.BackColor = Pal.Bg; l.Font = new Font("Microsoft YaHei UI", 9f);
            Controls.Add(l);
            return l;
        }
        ComboBox AddCombo(string label, int y, string[] items, string[] values, string cur)
        {
            AddLbl(label, y);
            var cb = new ComboBox();
            foreach (var it in items) cb.Items.Add(it);
            cb.SetBounds(30 + _labelW, y, Math.Max(120, 430 - _labelW), 26);
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.FlatStyle = FlatStyle.Flat;
            cb.BackColor = Pal.Surface; cb.ForeColor = Color.White;
            cb.Font = new Font("Microsoft YaHei UI", 9f);
            cb.Tag = values;
            int idx = Array.IndexOf(values, cur); cb.SelectedIndex = idx >= 0 ? idx : 0;
            Controls.Add(cb);
            ThemedCombo.Attach(cb);
            return cb;
        }
        TextBox AddBox(int y, string val, int w)
        {
            var t = new TextBox();
            t.SetBounds(30 + _labelW, y, Math.Min(w, Math.Max(80, 430 - _labelW)), 26);
            t.BackColor = Color.FromArgb(17, 20, 24);
            t.ForeColor = Pal.Text;
            t.BorderStyle = BorderStyle.None;
            t.Font = new Font("Microsoft YaHei UI", 9f);
            t.Text = val;
            Controls.Add(t);
            return t;
        }
        TextBox AddNum(string label, int y, string val) { AddLbl(label, y); return AddBox(y, val, 120); }
        TextBox AddText(string label, int y, string val) { AddLbl(label, y); return AddBox(y, val, 280); }
        DarkCheckBox AddCheck(string label, int y, bool val)
        {
            var c = new DarkCheckBox();
            c.Text = label;
            c.SetBounds(20, y, Math.Min(456, Math.Max(320, TextFit.Measure(label, this.Font) + 34)), 24);
            c.ForeColor = Pal.Text; c.BackColor = Pal.Bg;
            c.Font = new Font("Microsoft YaHei UI", 9f);
            c.Checked = val;
            Controls.Add(c);
            return c;
        }
        static bool IntIn(string txt, int lo, int hi, out int v)
        {
            return int.TryParse((txt ?? "").Trim(), out v) && v >= lo && v <= hi;
        }
        bool SaveProps()
        {
            int maxp, view, sim, spawn;
            if (!IntIn(_tbMax.Text, 1, 9999, out maxp)) { InfoBox.Show(Txt.SavePropsShow, Txt.SettingsFormShow2); return false; }
            if (!IntIn(_tbView.Text, 2, 32, out view)) { InfoBox.Show(Txt.SavePropsShow2, Txt.SettingsFormShow2); return false; }
            if (!IntIn(_tbSim.Text, 2, 32, out sim)) { InfoBox.Show(Txt.SavePropsShow3, Txt.SettingsFormShow2); return false; }
            if (!IntIn(_tbSpawn.Text, 0, 9999, out spawn)) { InfoBox.Show(Txt.SavePropsShow4, Txt.SettingsFormShow2); return false; }
            var set = new System.Collections.Generic.Dictionary<string, string>();
            set["difficulty"] = ((string[])_cbDiff.Tag)[_cbDiff.SelectedIndex];
            set["gamemode"] = ((string[])_cbMode.Tag)[_cbMode.SelectedIndex];
            set["max-players"] = maxp.ToString();
            set["view-distance"] = view.ToString();
            set["simulation-distance"] = sim.ToString();
            set["spawn-protection"] = spawn.ToString();
            set["motd"] = _tbMotd.Text.Trim();
            set["white-list"] = _ckWhite.Checked ? "true" : "false";
            set["pvp"] = _ckPvp.Checked ? "true" : "false";
            set["allow-flight"] = _ckFly.Checked ? "true" : "false";
            var seen = new System.Collections.Generic.HashSet<string>();
            var outLines = new System.Collections.Generic.List<string>();
            foreach (var raw in _lines)
            {
                var t = raw.Trim();
                int eq = raw.IndexOf('=');
                if (eq > 0 && !t.StartsWith("#") && !t.StartsWith("!"))
                {
                    string k = raw.Substring(0, eq).Trim();
                    if (set.ContainsKey(k)) { outLines.Add(k + "=" + set[k]); seen.Add(k); continue; }
                }
                outLines.Add(raw);
            }
            foreach (var kv in set) if (!seen.Contains(kv.Key)) outLines.Add(kv.Key + "=" + kv.Value);
            try
            {
                string pf = System.IO.Path.Combine(_dir, "server.properties");
                try { System.IO.File.Copy(pf, pf + ".bak", true); } catch { }
                System.IO.File.WriteAllLines(pf, outLines.ToArray(), new UTF8Encoding(false));
            }
            catch (Exception ex) { InfoBox.Show(Txt.SavePropsShow5 + ex.Message, Txt.SettingsFormShow2); return false; }
            if (_running) InfoBox.Show(Txt.SavePropsShow6, Txt.SettingsFormShow2);
            return true;
        }
    }

    // ---------- 玩家管理（v6.11） ----------
    // Every action is just a console command through the launcher's
    // existing stdin channel - the same thing the host could type into
    // the command box, without the typing.
    class PlayerForm : DarkDialog
    {
        ListBox _lb;
        System.Collections.Generic.List<string> _ops, _banned;
        string _me;
        Action<string> _exec;
        TextLabel _lbOps, _lbBanned;
        RoundButton _btnTp;

        public PlayerForm(System.Collections.Generic.List<string> online, System.Collections.Generic.List<string> ops,
            System.Collections.Generic.List<string> banned, string me, Action<string> exec)
        {
            _ops = ops; _banned = banned; _me = me; _exec = exec;
            InitChrome(Txt.MbText, 460, 404);
            var head = new TextLabel();
            head.Caption = online.Count > 0 ? (Txt.PlayerFormStr + online.Count + "）") : Txt.PlayerFormStr2;
            head.SetBounds(20, 12, 420, 22);
            head.ForeColor = Pal.Sub; head.BackColor = Pal.Bg;
            head.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            Controls.Add(head);
            _lb = new ListBox();
            _lb.SetBounds(20, 38, 196, 196);
            _lb.BackColor = Color.FromArgb(17, 20, 24);
            _lb.ForeColor = Pal.Text;
            _lb.BorderStyle = BorderStyle.None;
            _lb.Font = new Font("Microsoft YaHei UI", 9f);
            _lb.IntegralHeight = false;
            ThemedList.Attach(_lb);
            foreach (var n in online) _lb.Items.Add(n);
            if (online.Count > 0) _lb.SelectedIndex = 0;
            Controls.Add(_lb);
            int bx = 232, by = 38;
            _btnTp = AddBtn(bx, by, Txt.PlayerFormAddBtn, (s2, e2) => { string n = Sel(); if (n != null && _me != null) _exec("tp " + n + " " + _me); }); by += 38;
            AddBtn(bx, by, Txt.PlayerFormAddBtn2, (s2, e2) => { string n = Sel(); if (n != null && ConfirmDo(Txt.PlayerFormConfirmDo + n + Txt.PlayerFormConfirmDo2)) _exec("kick " + n); }); by += 38;
            AddBtn(bx, by, Txt.PlayerFormAddBtn3, (s2, e2) => { string n = Sel(); if (n != null) { _exec("op " + n); if (!_ops.Contains(n)) _ops.Add(n); RefreshLists(); } }); by += 38;
            AddBtn(bx, by, Txt.PlayerFormAddBtn4, (s2, e2) => { string n = Sel(); if (n != null) { _exec("deop " + n); _ops.Remove(n); RefreshLists(); } }); by += 38;
            AddBtn(bx, by, Txt.PlayerFormAddBtn5, (s2, e2) => { string n = Sel(); if (n != null && ConfirmDo(Txt.PlayerFormConfirmDo3 + n + Txt.PlayerFormConfirmDo4)) { _exec("ban " + n); if (!_banned.Contains(n)) _banned.Add(n); RefreshLists(); } }); by += 38;
            AddBtn(bx, by, Txt.PlayerFormAddBtn6, (s2, e2) => { string n = Sel(); if (n != null) { _exec("pardon " + n); _banned.Remove(n); RefreshLists(); } });
            if (_me == null && _btnTp != null) { _btnTp.Enabled = false; }
            _lbOps = new TextLabel(); _lbOps.SetBounds(20, 244, 420, 20); _lbOps.ForeColor = Pal.Sub; _lbOps.BackColor = Pal.Bg; _lbOps.Font = new Font("Microsoft YaHei UI", 9f); Controls.Add(_lbOps);
            _lbBanned = new TextLabel(); _lbBanned.SetBounds(20, 268, 420, 20); _lbBanned.ForeColor = Pal.Sub; _lbBanned.BackColor = Pal.Bg; _lbBanned.Font = new Font("Microsoft YaHei UI", 9f); Controls.Add(_lbBanned);
            var hint = new TextLabel();
            hint.WordWrap = true;
            hint.Caption = _me == null ? Txt.PlayerFormStr3 : Txt.PlayerFormStr4;
            hint.SetBounds(20, 292, 420, 40); hint.ForeColor = Pal.Mute; hint.BackColor = Pal.Bg; hint.Font = new Font("Microsoft YaHei UI", 9f);
            Controls.Add(hint);
            var close = new RoundButton();
            close.Text = Txt.CloseText;
            int closeW = Math.Max(100, TextFit.Measure(Txt.CloseText, this.Font) + 26);
            close.SetBounds(440 - closeW, 356, closeW, 32);
            close.BackColor = Pal.Surface; close.ForeColor = Color.White; close.BackdropColor = Pal.Bg;
            close.Font = new Font("Microsoft YaHei UI", 9f);
            close.Click += (s2, e2) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(close);
            CancelButton = close;
            RefreshLists();
            FinishChrome();
        }
        RoundButton AddBtn(int x, int y, string text, EventHandler onClick)
        {
            var b = new RoundButton();
            b.Text = text;
            b.SetBounds(x, y, 208, 30);
            b.BackColor = Pal.Surface; b.ForeColor = Pal.Text; b.BackdropColor = Pal.Bg;
            b.Font = new Font("Microsoft YaHei UI", 9f);
            b.Click += onClick;
            Controls.Add(b);
            return b;
        }
        string Sel() { return _lb != null && _lb.SelectedItem != null ? _lb.SelectedItem.ToString() : null; }
        bool ConfirmDo(string msg)
        {
            using (var c = new ConfirmBox(msg)) { var r = c.ShowDialog(this); return r == DialogResult.OK && c.ChoiceYes; }
        }
        void RefreshLists()
        {
            if (_lbOps != null) _lbOps.Caption = Txt.LbOpsCaption + (_ops.Count > 0 ? string.Join("、", _ops.ToArray()) : Txt.RefreshListsStr);
            if (_lbBanned != null) _lbBanned.Caption = Txt.LbBannedCaption + (_banned.Count > 0 ? string.Join("、", _banned.ToArray()) : Txt.RefreshListsStr);
        }
    }

    // ---------- Minecraft status protocol (SLP) query (v6.11) ----------
    // The Server List Ping from the protocol documentation: handshake with
    // next-state 1, a status request, a JSON response, then ping/pong for
    // latency. Lets the monitor ask the server itself for its version,
    // MOTD, player count and player names instead of guessing from the
    // outside.
    class SlpResult
    {
        public bool Ok;
        public string Version = "";
        public string Motd = "";
        public int Online = -1;
        public int Max = -1;
        public System.Collections.Generic.List<string> Names = new System.Collections.Generic.List<string>();
        public long PingMs = -1;
    }

    static class Slp
    {
        static void WriteVarInt(Stream st, int v)
        {
            uint uv = (uint)v;
            while (true) { byte b = (byte)(uv & 0x7F); uv >>= 7; if (uv != 0) { st.WriteByte((byte)(b | 0x80)); } else { st.WriteByte(b); break; } }
        }
        static int ReadVarInt(Stream st)
        {
            int result = 0, shift = 0, b;
            do { b = st.ReadByte(); if (b < 0) throw new EndOfStreamException(); result |= (b & 0x7F) << shift; shift += 7; if (shift > 35) throw new InvalidDataException(); } while ((b & 0x80) != 0);
            return result;
        }
        static byte[] Packet(byte[] payload)
        {
            using (var ms = new MemoryStream()) { WriteVarInt(ms, payload.Length); ms.Write(payload, 0, payload.Length); return ms.ToArray(); }
        }
        static byte[] ReadPacket(Stream st, out int id)
        {
            int len = ReadVarInt(st);
            if (len <= 0 || len > (1 << 21)) throw new InvalidDataException();
            byte[] buf = new byte[len];
            int got = 0;
            while (got < len) { int r = st.Read(buf, got, len - got); if (r <= 0) throw new EndOfStreamException(); got += r; }
            using (var ms = new MemoryStream(buf)) { id = ReadVarInt(ms); byte[] rest = new byte[(int)(len - ms.Position)]; ms.Read(rest, 0, rest.Length); return rest; }
        }
        public static SlpResult Query(string host, int port, int timeoutMs)
        {
            var res = new SlpResult();
            try
            {
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    var ar = tcp.BeginConnect(host, port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(timeoutMs)) return res;
                    tcp.EndConnect(ar);
                    var st = tcp.GetStream();
                    st.ReadTimeout = timeoutMs; st.WriteTimeout = timeoutMs;
                    byte[] hs;
                    using (var ms = new MemoryStream())
                    {
                        ms.WriteByte(0);
                        WriteVarInt(ms, -1);
                        byte[] addr = Encoding.UTF8.GetBytes(host);
                        WriteVarInt(ms, addr.Length); ms.Write(addr, 0, addr.Length);
                        ms.WriteByte((byte)(port >> 8)); ms.WriteByte((byte)(port & 0xFF));
                        WriteVarInt(ms, 1);
                        hs = ms.ToArray();
                    }
                    byte[] p1 = Packet(hs); st.Write(p1, 0, p1.Length);
                    byte[] req = Packet(new byte[] { 0 }); st.Write(req, 0, req.Length);
                    int id; byte[] body = ReadPacket(st, out id);
                    if (id != 0) return res;
                    string json = Encoding.UTF8.GetString(body);
                    var mv = Regex.Match(json, "\"version\"\\s*:\\s*\\{[^}]*\"name\"\\s*:\\s*\"([^\"]*)\"");
                    if (mv.Success) res.Version = mv.Groups[1].Value;
                    var mo = Regex.Match(json, "\"online\"\\s*:\\s*(\\d+)"); if (mo.Success) res.Online = int.Parse(mo.Groups[1].Value);
                    var mm = Regex.Match(json, "\"max\"\\s*:\\s*(\\d+)"); if (mm.Success) res.Max = int.Parse(mm.Groups[1].Value);
                    var md = Regex.Match(json, "\"description\"\\s*:\\s*\"([^\"]*)\"");
                    if (md.Success) res.Motd = md.Groups[1].Value;
                    else { var mt = Regex.Match(json, "\"description\"\\s*:\\s*\\{[^{}]*\"text\"\\s*:\\s*\"([^\"]*)\""); if (mt.Success) res.Motd = mt.Groups[1].Value; }
                    var msamp = Regex.Match(json, "\"sample\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                    if (msamp.Success) { foreach (Match mn in Regex.Matches(msamp.Groups[1].Value, "\"name\"\\s*:\\s*\"([^\"]*)\"")) res.Names.Add(mn.Groups[1].Value); }
                    res.Motd = Regex.Replace(res.Motd ?? "", "\u00a7.", "");
                    try
                    {
                        long payload = DateTime.Now.Ticks;
                        byte[] pb; using (var ms = new MemoryStream()) { ms.WriteByte(1); for (int i = 7; i >= 0; i--) ms.WriteByte((byte)(payload >> (8 * i))); pb = ms.ToArray(); }
                        byte[] pp = Packet(pb);
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        st.Write(pp, 0, pp.Length);
                        int pid; ReadPacket(st, out pid);
                        sw.Stop();
                        if (pid == 1) res.PingMs = sw.ElapsedMilliseconds;
                    }
                    catch { }
                    res.Ok = res.Online >= 0;
                }
            }
            catch { }
            return res;
        }
    }

    // ---------- v6.20: MC / Java version helpers ----------
    static class McJava
    {
        // The java.exe a check should look at: the configured one first,
        // then PATH. Returns null when nothing is found. Never throws.
        public static string ResolveJavaExe(Config cfg)
        {
            try
            {
                if (cfg != null && !string.IsNullOrEmpty(cfg.JavaPath) && File.Exists(cfg.JavaPath)) return cfg.JavaPath;
                string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (string dir in pathEnv.Split(';'))
                {
                    string d = dir.Trim().Trim('"');
                    if (d.Length == 0) continue;
                    string cand = Path.Combine(d, "java.exe");
                    if (File.Exists(cand)) return cand;
                }
            }
            catch { }
            return null;
        }

        // Run `java -version` and parse the major version (17, 21; a
        // "1.8.0_x" banner means 8). 0 = unknown. 5 s timeout, never throws.
        public static int GetJavaMajor(string javaExe)
        {
            if (string.IsNullOrEmpty(javaExe)) return 0;
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = javaExe;
                psi.Arguments = "-version";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                using (var p = Process.Start(psi))
                {
                    if (p == null) return 0;
                    // The banner goes to stderr; drain stdout on a side
                    // thread so a chatty JVM can never fill the pipe.
                    var drain = new Thread(() => { try { p.StandardOutput.ReadToEnd(); } catch { } });
                    drain.IsBackground = true;
                    drain.Start();
                    string err = "";
                    var reader = new Thread(() => { try { err = p.StandardError.ReadToEnd(); } catch { } });
                    reader.IsBackground = true;
                    reader.Start();
                    if (!p.WaitForExit(5000)) { try { p.Kill(); } catch { } return 0; }
                    reader.Join(1500);
                    return ParseJavaVersionText(err);
                }
            }
            catch { return 0; }
        }

        public static int ParseJavaVersionText(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var m = Regex.Match(text, "version\\s+\"(\\d+)(?:\\.(\\d+))?");
            if (!m.Success) return 0;
            int a, b = 0;
            if (!int.TryParse(m.Groups[1].Value, out a)) return 0;
            if (m.Groups[2].Success) int.TryParse(m.Groups[2].Value, out b);
            return a == 1 ? b : a; // 1.8.0_xxx is Java 8
        }

        // Minecraft version from a server jar's version.json ("name").
        // Vanilla / Paper / Fabric launch jars all carry one. Null when
        // unreadable. Never throws, never blocks on a corrupt jar.
        public static string ReadMcVersion(string jarPath)
        {
            try
            {
                if (string.IsNullOrEmpty(jarPath) || !File.Exists(jarPath)) return null;
                using (var fs = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    var entry = za.GetEntry("version.json");
                    if (entry == null) return null;
                    using (var es = entry.Open())
                    using (var sr = new StreamReader(es, Encoding.UTF8))
                    {
                        string json = sr.ReadToEnd();
                        var m = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                        if (m.Success) return m.Groups[1].Value.Trim();
                        var m2 = Regex.Match(json, "\"id\"\\s*:\\s*\"([^\"]+)\"");
                        if (m2.Success) return m2.Groups[1].Value.Trim();
                    }
                }
            }
            catch { }
            return null;
        }

        // Required Java major for an MC version name; 0 = unknown.
        public static int RequiredJava(string mcVersion)
        {
            int[] v = ParseMcVersion(mcVersion);
            if (v == null) return 0;
            if (v[0] >= 26) return 21; // year-based versions: best known estimate, see DESIGN_v620.md
            if (v[0] != 1) return 0;
            if (v[1] > 20) return 21;
            if (v[1] == 20) return v[2] >= 5 ? 21 : 17;
            if (v[1] >= 18) return 17;
            if (v[1] == 17) return 16;
            return 8;
        }

        // "1.20.1" -> [1,20,1]; snapshots / junk -> null.
        public static int[] ParseMcVersion(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var m = Regex.Match(s.Trim(), "^(\\d+)\\.(\\d+)(?:\\.(\\d+))?$");
            if (!m.Success) return null;
            int a, b, c = 0;
            if (!int.TryParse(m.Groups[1].Value, out a)) return null;
            if (!int.TryParse(m.Groups[2].Value, out b)) return null;
            if (m.Groups[3].Success) int.TryParse(m.Groups[3].Value, out c);
            return new int[] { a, b, c };
        }
    }

    // ---------- v6.20: download / hash / zip helpers ----------
    static class DlUtil
    {
        public static WebClient NewClient()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            var wc = new WebClient();
            wc.Headers[HttpRequestHeader.UserAgent] = "MCLauncherApp/6.22.5";
            wc.Encoding = Encoding.UTF8;
            return wc;
        }

        // Small JSON GET with a hard timeout (WebClient has none of its
        // own for string downloads, so race it against a timer).
        public static string DownloadStringTimed(string url, int timeoutSec, out string err)
        {
            err = null;
            string result = null;
            string errLocal = null;
            var done = new ManualResetEvent(false);
            using (var wc = NewClient())
            {
                wc.DownloadStringCompleted += (s, e) =>
                {
                    if (e.Error != null) errLocal = e.Error.Message;
                    else if (!e.Cancelled) result = e.Result;
                    done.Set();
                };
                try { wc.DownloadStringAsync(new Uri(url)); }
                catch (Exception ex) { err = ex.Message; return null; }
                if (!done.WaitOne(timeoutSec * 1000))
                {
                    try { wc.CancelAsync(); } catch { }
                    err = "request timed out after " + timeoutSec + " s: " + url;
                    return null;
                }
            }
            err = errLocal;
            return result;
        }

        // Big-file download with progress. Lands in "<dest>.part" first and
        // is only renamed to destPath after a clean finish - the "safe
        // promotion" rule; partial files are deleted on error/cancel.
        // Returns null on success, "cancelled" on cancel, else the error.
        public static string DownloadFile(WebClient wc, string url, string destPath, Action<int> progress, Func<bool> isCancelled)
        {
            string part = destPath + ".part";
            TryDelete(part);
            var done = new ManualResetEvent(false);
            Exception error = null;
            bool cancelled = false;
            DownloadProgressChangedEventHandler ph = (s, e) => { if (progress != null) { try { progress(e.ProgressPercentage); } catch { } } };
            System.ComponentModel.AsyncCompletedEventHandler ch = (s, e) => { error = e.Error; cancelled = e.Cancelled; done.Set(); };
            wc.DownloadProgressChanged += ph;
            wc.DownloadFileCompleted += ch;
            try
            {
                wc.DownloadFileAsync(new Uri(url), part);
                while (!done.WaitOne(200))
                {
                    if (isCancelled != null && isCancelled()) { try { wc.CancelAsync(); } catch { } }
                }
            }
            catch (Exception ex) { TryDelete(part); return ex.Message; }
            finally
            {
                wc.DownloadProgressChanged -= ph;
                wc.DownloadFileCompleted -= ch;
            }
            if (cancelled || (isCancelled != null && isCancelled())) { TryDelete(part); return "cancelled"; }
            if (error != null) { TryDelete(part); return error.Message; }
            try
            {
                if (File.Exists(destPath)) File.Delete(destPath);
                File.Move(part, destPath);
            }
            catch (Exception ex) { TryDelete(part); return ex.Message; }
            return null;
        }

        public static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        public static string Sha256File(string path) { return HashFile(path, SHA256.Create()); }
        public static string Sha1File(string path) { return HashFile(path, SHA1.Create()); }

        static string HashFile(string path, HashAlgorithm alg)
        {
            using (alg)
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] h = alg.ComputeHash(fs);
                var sb = new StringBuilder();
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // Value of "key" (a quoted string) appearing after startIndex in a
        // JSON text. Hand-rolled on purpose: no JSON library may be added.
        public static string JsonStrAfter(string json, int startIndex, string key)
        {
            if (json == null || startIndex < 0) return null;
            var m = Regex.Match(json.Substring(startIndex), "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        public static int JsonIndexOf(string json, string pattern)
        {
            if (json == null) return -1;
            var m = Regex.Match(json, pattern);
            return m.Success ? m.Index : -1;
        }

        // Fabric meta lists put "version" right before "stable" in each
        // entry, newest first: the first "stable": true match is the
        // newest stable build (loader list / installer list alike).
        public static string FirstStableVersion(string json)
        {
            if (json == null) return null;
            var m = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"stable\"\\s*:\\s*true");
            return m.Success ? m.Groups[1].Value : null;
        }

        // Zip extraction with a path-traversal guard. Returns error or null.
        public static string ExtractZip(string zipPath, string destDir)
        {
            try
            {
                string fullDest = Path.GetFullPath(destDir);
                using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    foreach (var entry in za.Entries)
                    {
                        string rel = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        string target = Path.GetFullPath(Path.Combine(fullDest, rel));
                        if (!target.Equals(fullDest, StringComparison.OrdinalIgnoreCase) &&
                            !target.StartsWith(fullDest + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                        if (entry.FullName.EndsWith("/")) { Directory.CreateDirectory(target); continue; }
                        string parent = Path.GetDirectoryName(target);
                        if (parent != null && parent.Length > 0) Directory.CreateDirectory(parent);
                        using (var es = entry.Open())
                        using (var os = new FileStream(target, FileMode.Create, FileAccess.Write))
                        {
                            es.CopyTo(os);
                        }
                    }
                }
                return null;
            }
            catch (Exception ex) { return ex.Message; }
        }

        // Find bin\java.exe under root (depth-limited). Null when absent.
        public static string FindJavaExe(string root)
        {
            try
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return null;
                string direct = Path.Combine(root, "bin\\java.exe");
                if (File.Exists(direct)) return direct;
                foreach (string sub in Directory.GetDirectories(root))
                {
                    string cand = Path.Combine(sub, "bin\\java.exe");
                    if (File.Exists(cand)) return cand;
                    foreach (string sub2 in Directory.GetDirectories(sub))
                    {
                        string cand2 = Path.Combine(sub2, "bin\\java.exe");
                        if (File.Exists(cand2)) return cand2;
                    }
                }
            }
            catch { }
            return null;
        }
    }

    // ---------- v6.20: first-run setup wizard ----------
    // Downloads a matching Java runtime (Adoptium) and a server jar
    // (Vanilla / Fabric / Paper), verifies hashes where upstream publishes
    // them, writes eula.txt only with explicit consent, and saves the
    // resulting paths back into the shared Config.
    class WizardForm : DarkDialog
    {
        readonly Config _cfg;
        ComboBox cbVersion;
        RadioButton rbVanilla, rbFabric, rbPaper;
        TextBox tbDir, tbLog;
        CheckBox chkEula;
        ProgressBar pb;
        TextLabel lblStep;
        RoundButton btnStart, btnCancelDl, btnClose;
        volatile bool _cancel;
        WebClient _wc;
        bool _running;
        public bool InstalledOk = false;

        // captured on the UI thread when Start is clicked
        string _ver, _dir;
        int _type; // 0 vanilla, 1 fabric, 2 paper
        bool _eulaAgreed;

        public WizardForm(Config cfg, string initialDir)
        {
            _cfg = cfg;
            InitChrome(Txt.WizardFormInitChrome, 660, 646);

            int lx = 20, tx = 170;
            AddLabel(Txt.WizardFormAddLabel, lx, 12, 140);
            cbVersion = new ComboBox();
            cbVersion.DropDownStyle = ComboBoxStyle.DropDown;
            cbVersion.Items.AddRange(new object[] { "1.20.1", "1.21.1", "1.21.11", "1.21.4", "1.20.4", "1.20.6", "1.19.4", "1.18.2" });
            cbVersion.Text = "1.20.1";
            cbVersion.SetBounds(tx, 8, 300, 26);
            cbVersion.FlatStyle = FlatStyle.Flat;
            cbVersion.BackColor = Pal.Surface;
            cbVersion.ForeColor = Color.White;
            Controls.Add(cbVersion);
            ThemedCombo.Attach(cbVersion);
            // v6.22.3: 提示改为整行换行，不再挤在版本框右侧的 300px 单行里
            AddHint(Txt.WizardFormAddHint, lx, 42, 620, 34);

            AddLabel(Txt.WizardFormAddLabel2, lx, 84, 140);
            rbVanilla = AddRadio(Txt.WizardFormAddRadio, tx, 82);
            rbFabric = AddRadio("Fabric", tx + 90, 82);
            rbPaper = AddRadio("Paper", tx + 180, 82);
            rbFabric.Checked = true;

            AddLabel(Txt.WizardFormAddLabel3, lx, 122, 140);
            tbDir = new TextBox();
            tbDir.SetBounds(tx, 118, 340, 26);
            tbDir.BackColor = Pal.Surface;
            tbDir.ForeColor = Color.White;
            tbDir.BorderStyle = BorderStyle.FixedSingle;
            tbDir.Text = string.IsNullOrEmpty(initialDir) ? "D:\\MCServer" : initialDir;
            Controls.Add(tbDir);
            var btnBrowse = new RoundButton();
            btnBrowse.Text = Txt.BtnBrowseText;
            btnBrowse.BackColor = Pal.Surface;
            btnBrowse.ForeColor = Color.White;
            btnBrowse.BackdropColor = Pal.Bg;
            btnBrowse.SetBounds(520, 116, 110, 30);
            btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = Txt.Description;
                    if (Directory.Exists(tbDir.Text.Trim())) fbd.SelectedPath = tbDir.Text.Trim();
                    if (fbd.ShowDialog(this) == DialogResult.OK) tbDir.Text = fbd.SelectedPath;
                }
            };
            Controls.Add(btnBrowse);

            chkEula = new CheckBox();
            chkEula.Text = Txt.ChkEulaText;
            chkEula.ForeColor = Color.White;
            chkEula.BackColor = Color.Transparent;
            // v6.22.3: 宽度按当前语言实测（原来只有 190/230 两档常量）
            int eulaW = TextFit.Measure(Txt.ChkEulaText, Font) + 30;
            if (eulaW < 190) eulaW = 190;
            if (eulaW > 400) eulaW = 400;
            chkEula.SetBounds(tx, 156, eulaW, 24);
            Controls.Add(chkEula);
            var lnkEula = new LinkLabel();
            lnkEula.Text = Txt.LnkEulaText;
            lnkEula.LinkColor = Color.FromArgb(120, 180, 255);
            lnkEula.ActiveLinkColor = Color.FromArgb(160, 200, 255);
            lnkEula.BackColor = Color.Transparent;
            int lnkW = Math.Max(120, TextFit.Measure(Txt.LnkEulaText, Font) + 6);
            lnkEula.SetBounds(tx + eulaW + 8, 158, lnkW, 24);
            lnkEula.LinkClicked += (s, e) => { try { Process.Start("https://www.minecraft.net/en-us/eula"); } catch { } };
            Controls.Add(lnkEula);
            AddHint(Txt.WizardFormAddHint2, lx, 186, 620, 34);

            lblStep = new TextLabel();
            lblStep.TopAlign = true;
            lblStep.WordWrap = true;
            lblStep.Caption = Txt.LblStepCaption;
            lblStep.TextShiftX = 2f; lblStep.TextShiftY = -0.67f;
            lblStep.SetBounds(20, 226, 620, 34);
            lblStep.BackColor = Color.Transparent; lblStep.BackdropColor = Pal.Bg; lblStep.ForeColor = Pal.Sub;
            Controls.Add(lblStep);

            pb = new ProgressBar();
            pb.SetBounds(20, 266, 620, 16);
            pb.Minimum = 0; pb.Maximum = 100; pb.Value = 0;
            Controls.Add(pb);

            tbLog = new TextBox();
            tbLog.Multiline = true;
            tbLog.ReadOnly = true;
            tbLog.ScrollBars = ScrollBars.Vertical;
            tbLog.SetBounds(20, 292, 620, 280);
            tbLog.BackColor = Pal.Surface;
            tbLog.ForeColor = Color.White;
            tbLog.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(tbLog);

            btnStart = new RoundButton();
            btnStart.Primary = true;
            btnStart.Text = Txt.BtnStartText;
            btnStart.BackColor = Pal.Green;
            btnStart.ForeColor = Color.White;
            btnStart.BackdropColor = Pal.Bg;
            btnStart.SetBounds(20, 588, 140, 36);
            btnStart.Click += (s, e) => StartInstall();
            Controls.Add(btnStart);

            btnCancelDl = new RoundButton();
            btnCancelDl.Text = Txt.NoText;
            btnCancelDl.BackColor = Pal.Surface;
            btnCancelDl.ForeColor = Color.White;
            btnCancelDl.BackdropColor = Pal.Bg;
            btnCancelDl.SetBounds(172, 588, 100, 36);
            btnCancelDl.Enabled = false;
            btnCancelDl.Click += (s, e) =>
            {
                _cancel = true;
                try { if (_wc != null) _wc.CancelAsync(); } catch { }
                WizLog(Txt.WizardFormWizLog);
            };
            Controls.Add(btnCancelDl);

            btnClose = new RoundButton();
            btnClose.Text = Txt.CloseText;
            btnClose.BackColor = Pal.Surface;
            btnClose.ForeColor = Color.White;
            btnClose.BackdropColor = Pal.Bg;
            btnClose.SetBounds(520, 588, 120, 36);
            btnClose.Click += (s, e) => { DialogResult = InstalledOk ? DialogResult.OK : DialogResult.Cancel; };
            Controls.Add(btnClose);

            FinishChrome();
        }

        void AddLabel(string text, int x, int y, int w)
        {
            var lb = new TextLabel();
            lb.TopAlign = true;
            lb.Caption = text;
            lb.TextShiftX = 2f; lb.TextShiftY = -0.67f;
            lb.SetBounds(x, y, w, 26);
            lb.BackColor = Color.Transparent; lb.BackdropColor = Pal.Bg; lb.ForeColor = Pal.Sub;
            Controls.Add(lb);
        }

        // v6.22.3: 提示行整行换行显示（原来固定 300px 单行，长译文必然被裁）
        void AddHint(string text, int x, int y, int w, int h)
        {
            var lb = new TextLabel();
            lb.TopAlign = true;
            lb.WordWrap = true;
            lb.Caption = text;
            lb.TextShiftX = 2f; lb.TextShiftY = -0.67f;
            lb.SetBounds(x, y, w, h);
            lb.BackColor = Color.Transparent; lb.BackdropColor = Pal.Bg; lb.ForeColor = Pal.Sub;
            Controls.Add(lb);
        }

        RadioButton AddRadio(string text, int x, int y)
        {
            var rb = new RadioButton();
            rb.Text = text;
            rb.ForeColor = Color.White;
            rb.BackColor = Color.Transparent;
            rb.SetBounds(x, y, 84, 26);
            Controls.Add(rb);
            return rb;
        }

        void WizUi(Action a)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) BeginInvoke(a); else a(); } catch { }
        }

        void WizLog(string s)
        {
            WizUi(() =>
            {
                tbLog.AppendText(s + "\r\n");
                tbLog.SelectionStart = tbLog.Text.Length;
                tbLog.ScrollToCaret();
            });
        }

        void WizStep(string s) { WizUi(() => { lblStep.Caption = s; }); }

        void WizProgress(int pct)
        {
            WizUi(() =>
            {
                if (pct >= 0) { pb.Style = ProgressBarStyle.Continuous; pb.Value = Math.Min(100, pct); }
                else pb.Style = ProgressBarStyle.Marquee;
            });
        }

        void SetRunningUi(bool running)
        {
            WizUi(() =>
            {
                _running = running;
                btnStart.Enabled = !running;
                btnCancelDl.Enabled = running;
                cbVersion.Enabled = !running;
                tbDir.Enabled = !running;
                rbVanilla.Enabled = !running;
                rbFabric.Enabled = !running;
                rbPaper.Enabled = !running;
                chkEula.Enabled = !running;
                if (!running) { pb.Style = ProgressBarStyle.Continuous; }
            });
        }

        void StartInstall()
        {
            if (_running) return;
            _ver = cbVersion.Text.Trim();
            _dir = tbDir.Text.Trim();
            _type = rbPaper.Checked ? 2 : (rbFabric.Checked ? 1 : 0);
            _eulaAgreed = chkEula.Checked;
            if (_ver.Length == 0) { InfoBox.Show(Txt.StartInstallShow, Txt.WizardFormInitChrome); return; }
            if (McJava.ParseMcVersion(_ver) == null) { InfoBox.Show(Txt.StartInstallShow2, Txt.WizardFormInitChrome); return; }
            if (_dir.Length == 0) { InfoBox.Show(Txt.StartInstallShow3, Txt.WizardFormInitChrome); return; }
            string existingJar = Path.Combine(_dir, "server.jar");
            if (File.Exists(existingJar))
            {
                var r = InfoBox.Show(Txt.StartInstallShow4, Txt.WizardFormInitChrome, Txt.StartInstallShow5, Txt.NoText);
                if (r != DialogResult.OK) return;
            }
            _cancel = false;
            SetRunningUi(true);
            WizLog(Txt.StartInstallWizLog + _ver + " / " + (_type == 2 ? "Paper" : (_type == 1 ? "Fabric" : Txt.WizardFormAddRadio)) + Txt.StartInstallPart + _dir);
            var t = new Thread(Run);
            t.IsBackground = true;
            t.Start();
        }

        void Run()
        {
            try
            {
                int[] vp = McJava.ParseMcVersion(_ver);
                int required = McJava.RequiredJava(_ver);
                if (required <= 0) { Fail(Txt.RunFail + _ver + Txt.RunFail2); return; }
                if (vp != null && vp[0] >= 26) WizLog(Txt.RunWizLog);

                // ---- Step 1: a suitable Java ----
                WizStep(Txt.RunWizStep + required);
                string javaExe = null;
                int cfgMajor = 0;
                if (!string.IsNullOrEmpty(_cfg.JavaPath) && File.Exists(_cfg.JavaPath)) cfgMajor = McJava.GetJavaMajor(_cfg.JavaPath);
                if (cfgMajor >= required)
                {
                    javaExe = _cfg.JavaPath;
                    WizLog(Txt.RunWizLog2 + cfgMajor + Txt.RunWizLog3 + javaExe);
                }
                if (javaExe == null)
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string localRoot = Path.Combine(baseDir, "java\\" + required);
                    string found = DlUtil.FindJavaExe(localRoot);
                    if (found != null && McJava.GetJavaMajor(found) >= required)
                    {
                        javaExe = found;
                        WizLog(Txt.RunWizLog4 + javaExe);
                    }
                }
                if (javaExe == null)
                {
                    if (_cancel) { Cancelled(); return; }
                    if (required != 17 && required != 21)
                    {
                        Fail("Minecraft " + _ver + Txt.RunFail3 + required + Txt.RunFail4 + required + Txt.RunFail5);
                        return;
                    }
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string javaRoot = Path.Combine(baseDir, "java");
                    Directory.CreateDirectory(javaRoot);
                    string zipPath = Path.Combine(javaRoot, "temurin" + required + ".zip");
                    string link = null, checksum = null, metaErr;
                    string meta = DlUtil.DownloadStringTimed("https://api.adoptium.net/v3/assets/latest/" + required + "/hotspot?os=windows&architecture=x64&image_type=jre&vendor=eclipse", 30, out metaErr);
                    if (meta != null)
                    {
                        int pkg = DlUtil.JsonIndexOf(meta, "\"package\"\\s*:");
                        if (pkg >= 0)
                        {
                            link = DlUtil.JsonStrAfter(meta, pkg, "link");
                            checksum = DlUtil.JsonStrAfter(meta, pkg, "checksum");
                        }
                    }
                    if (link == null)
                    {
                        link = "https://api.adoptium.net/v3/binary/latest/" + required + "/ga/windows/x64/jre/hotspot/normal/eclipse";
                        checksum = null;
                        WizLog(Txt.RunWizLog5 + (metaErr ?? Txt.RunStr) + Txt.RunPart);
                    }
                    WizLog(Txt.RunWizLog6 + required + Txt.RunWizLog7);
                    string dlErr = DownloadWithProgress(link, zipPath, "Java " + required);
                    if (dlErr == "cancelled") { Cancelled(); return; }
                    if (dlErr != null) { Fail(Txt.RunFail6 + dlErr); return; }
                    if (checksum != null)
                    {
                        WizLog(Txt.RunWizLog8);
                        string actual = DlUtil.Sha256File(zipPath);
                        if (!string.Equals(actual, checksum, StringComparison.OrdinalIgnoreCase))
                        {
                            DlUtil.TryDelete(zipPath);
                            Fail(Txt.RunFail7 + checksum + Txt.RunFail8 + actual + Txt.RunFail9);
                            return;
                        }
                        WizLog(Txt.RunWizLog9);
                    }
                    string destDir = Path.Combine(javaRoot, required.ToString());
                    WizLog(Txt.RunWizLog10 + destDir + Txt.RunWizLog11);
                    string exErr = DlUtil.ExtractZip(zipPath, destDir);
                    DlUtil.TryDelete(zipPath);
                    if (exErr != null) { Fail(Txt.RunFail10 + exErr); return; }
                    javaExe = DlUtil.FindJavaExe(destDir);
                    if (javaExe == null) { Fail(Txt.RunFail11); return; }
                    int got = McJava.GetJavaMajor(javaExe);
                    if (got < required) { Fail(Txt.RunFail12 + got + Txt.RunFail13 + required + Txt.RunFail14); return; }
                    WizLog(Txt.RunWizLog12 + got + Txt.RunWizLog13 + javaExe);
                }
                if (_cancel) { Cancelled(); return; }

                // ---- Step 2: the server jar ----
                WizStep(Txt.RunWizStep2);
                Directory.CreateDirectory(_dir);
                string jarDest = Path.Combine(_dir, "server.jar");
                if (_type == 0)
                {
                    string err;
                    string manifest = DlUtil.DownloadStringTimed("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json", 30, out err);
                    if (manifest == null) { Fail(Txt.RunFail15 + err); return; }
                    int vi = DlUtil.JsonIndexOf(manifest, "\"id\"\\s*:\\s*\"" + Regex.Escape(_ver) + "\"");
                    string verUrl = vi >= 0 ? DlUtil.JsonStrAfter(manifest, vi, "url") : null;
                    if (verUrl == null) { Fail(Txt.RunFail16 + _ver + Txt.RunFail17); return; }
                    string verJson = DlUtil.DownloadStringTimed(verUrl, 30, out err);
                    if (verJson == null) { Fail(Txt.RunFail18 + _ver + Txt.RunFail19 + err); return; }
                    int si = DlUtil.JsonIndexOf(verJson, "\"server\"\\s*:\\s*\\{");
                    string jarUrl = si >= 0 ? DlUtil.JsonStrAfter(verJson, si, "url") : null;
                    string sha1 = si >= 0 ? DlUtil.JsonStrAfter(verJson, si, "sha1") : null;
                    if (jarUrl == null) { Fail(Txt.RunFail20); return; }
                    WizLog(Txt.RunWizLog14);
                    string dlErr = DownloadWithProgress(jarUrl, jarDest, Txt.RunDownloadWithProgress);
                    if (dlErr == "cancelled") { Cancelled(); return; }
                    if (dlErr != null) { Fail(Txt.RunFail21 + dlErr); return; }
                    if (sha1 != null)
                    {
                        WizLog(Txt.RunWizLog15);
                        string actual = DlUtil.Sha1File(jarDest);
                        if (!string.Equals(actual, sha1, StringComparison.OrdinalIgnoreCase))
                        {
                            DlUtil.TryDelete(jarDest);
                            Fail(Txt.RunFail22 + sha1 + Txt.RunFail8 + actual + Txt.RunFail9);
                            return;
                        }
                        WizLog(Txt.RunWizLog16);
                    }
                }
                else if (_type == 1)
                {
                    string err;
                    string loaders = DlUtil.DownloadStringTimed("https://meta.fabricmc.net/v2/versions/loader/" + _ver, 30, out err);
                    if (loaders == null) { Fail(Txt.RunFail23 + err); return; }
                    string loader = DlUtil.FirstStableVersion(loaders);
                    if (loader == null) { Fail(Txt.RunFail24 + _ver + Txt.RunFail25); return; }
                    string installers = DlUtil.DownloadStringTimed("https://meta.fabricmc.net/v2/versions/installer", 30, out err);
                    if (installers == null) { Fail(Txt.RunFail26 + err); return; }
                    string installer = DlUtil.FirstStableVersion(installers);
                    if (installer == null) { Fail(Txt.RunFail27); return; }
                    string jarUrl = "https://meta.fabricmc.net/v2/versions/loader/" + _ver + "/" + loader + "/" + installer + "/server/jar";
                    WizLog(Txt.RunWizLog17 + loader + Txt.RunWizLog18 + installer + Txt.RunWizLog19);
                    WizLog(Txt.RunWizLog20);
                    string dlErr = DownloadWithProgress(jarUrl, jarDest, Txt.RunDownloadWithProgress2);
                    if (dlErr == "cancelled") { Cancelled(); return; }
                    if (dlErr != null) { Fail(Txt.RunFail28 + dlErr); return; }
                    try { Directory.CreateDirectory(Path.Combine(_dir, "mods")); } catch { }
                    WizLog(Txt.RunWizLog21);
                }
                else
                {
                    string err;
                    string builds = DlUtil.DownloadStringTimed("https://fill.papermc.io/v3/projects/paper/versions/" + _ver + "/builds", 30, out err);
                    if (builds == null) { Fail(Txt.RunFail29 + err + Txt.RunFail30); return; }
                    int si = DlUtil.JsonIndexOf(builds, "\"server:default\"");
                    string jarUrl = si >= 0 ? DlUtil.JsonStrAfter(builds, si, "url") : null;
                    string sha256 = si >= 0 ? DlUtil.JsonStrAfter(builds, si, "sha256") : null;
                    if (jarUrl == null) { Fail(Txt.RunFail31); return; }
                    WizLog(Txt.RunWizLog22);
                    string dlErr = DownloadWithProgress(jarUrl, jarDest, Txt.RunDownloadWithProgress3);
                    if (dlErr == "cancelled") { Cancelled(); return; }
                    if (dlErr != null) { Fail(Txt.RunFail32 + dlErr); return; }
                    if (sha256 != null)
                    {
                        WizLog(Txt.RunWizLog23);
                        string actual = DlUtil.Sha256File(jarDest);
                        if (!string.Equals(actual, sha256, StringComparison.OrdinalIgnoreCase))
                        {
                            DlUtil.TryDelete(jarDest);
                            Fail(Txt.RunFail33 + sha256 + Txt.RunFail8 + actual + Txt.RunFail9);
                            return;
                        }
                        WizLog(Txt.RunWizLog9);
                    }
                }
                if (_cancel) { Cancelled(); return; }

                // ---- Step 3: starter files ----
                WizStep(Txt.RunWizStep3);
                string eulaPath = Path.Combine(_dir, "eula.txt");
                if (_eulaAgreed)
                {
                    File.WriteAllText(eulaPath,
                        "#By changing the setting below to TRUE you are indicating your agreement to our EULA (https://aka.ms/MinecraftEULA).\r\n" +
                        "#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" +
                        "eula=true\r\n", Encoding.UTF8);
                    WizLog(Txt.RunWizLog24);
                }
                else
                {
                    WizLog(Txt.RunWizLog25);
                }
                string propsPath = Path.Combine(_dir, "server.properties");
                if (!File.Exists(propsPath))
                {
                    File.WriteAllText(propsPath,
                        "#Minecraft server properties\r\n" +
                        "#generated by MCLauncherApp first-run wizard\r\n" +
                        "server-port=25565\r\n" +
                        "online-mode=false\r\n", Encoding.UTF8);
                    WizLog(Txt.RunWizLog26);
                }
                else
                {
                    WizLog(Txt.RunWizLog27);
                }

                // ---- Step 4: save config ----
                WizStep(Txt.RunWizStep4);
                _cfg.ServerDir = _dir;
                _cfg.JarName = "server.jar";
                if (javaExe != null) _cfg.JavaPath = javaExe;
                _cfg.Save();
                InstalledOk = true;
                WizLog(Txt.RunWizLog28 + _dir + Txt.RunWizLog29 + (javaExe ?? Txt.RunStr2));
                WizLog(Txt.RunWizLog30);
                WizStep(Txt.RunWizStep5);
                SetRunningUi(false);
            }
            catch (Exception ex)
            {
                Fail(Txt.RunFail34 + ex.Message);
            }
        }

        string DownloadWithProgress(string url, string destPath, string label)
        {
            _wc = DlUtil.NewClient();
            try
            {
                int lastPct = -2;
                string err = DlUtil.DownloadFile(_wc, url, destPath,
                    pct =>
                    {
                        if (pct != lastPct)
                        {
                            lastPct = pct;
                            WizProgress(pct);
                            if (pct >= 0) WizStep(Txt.DownloadWithProgressWizStep + label + Txt.DownloadWithProgressWizStep2 + pct + "%");
                        }
                    },
                    () => _cancel);
                WizProgress(100);
                return err;
            }
            finally { try { _wc.Dispose(); } catch { } _wc = null; }
        }

        void Fail(string msg)
        {
            WizLog(Txt.FailWizLog + msg);
            WizStep(Txt.FailWizStep);
            SetRunningUi(false);
        }

        void Cancelled()
        {
            WizLog(Txt.CancelledWizLog);
            WizStep(Txt.CancelledWizStep);
            SetRunningUi(false);
        }
    }

    class MainForm : Form
    {
        private Config _cfg;
        private StatusCard statusCard;
        private RoundButton btnStart, btnPcl2, btnStop, btnSettings, btnCopy;
        private RoundButton btnPrevSearch, btnNextSearch;
        private TextLabel addrLbl;
        private HintTextBox tbAddr;
        private NotifyIcon _tray;
        private SegControl segLog;
        private LogTextBox tbServerLog, tbTunnelLog;
        private readonly AnimFloat _logFade = new AnimFloat(1);
        private TextBox tbCmd;
        private RoundButton btnCmdSend;
        // v6.21: spark 性能诊断入口（TPS 报告 + 30 秒 profiler）
        private TextLabel sparkLbl;
        private RoundButton btnSparkTps, btnSparkProfile;
        private System.Windows.Forms.Timer _sparkTimer;
        private bool _profiling = false;
        // v6.6: slim overlay scrollbars + jump-to-bottom chip + command history
        private SlimScrollBar sbSrvV, sbSrvH, sbTunV, sbTunH;
        private RoundButton btnJumpBottom;
        // v6.11: log search row + error filter (server log only)
        private Panel searchRow;
        private HintTextBox tbSearch;
        private ToolTip _tips;
        private TextLabel lblFind;
        private DarkCheckBox chkErrOnly;
        private readonly System.Collections.Generic.List<string> _srvLines = new System.Collections.Generic.List<string>();
        private readonly object _logLock613 = new object(); // v6.13: on-disk log mirror
        private bool _timedDeferred614 = false; // v6.14: timed-command deferral notice shown
        private bool _srvFilterErr = false;
        private System.Windows.Forms.Timer _logSyncTimer;
        private int _maxCharsSrv = 0, _maxCharsTun = 0;
        private readonly System.Collections.Generic.List<string> _cmdHist = new System.Collections.Generic.List<string>();
        private int _histIdx = -1;
        private string _histDraft = "";
        // v6.7: stats tab sampling state
        private StatsPanel statsPanel;
        private System.Windows.Forms.Timer _statsTimer;
        private long _lastNetRx = 0, _lastNetTx = 0;
        private DateTime _lastNetT = DateTime.MinValue;
        private TimeSpan _lastSrvCpu = TimeSpan.Zero; private DateTime _lastSrvCpuT = DateTime.MinValue;
        private TimeSpan _lastTunCpu = TimeSpan.Zero; private DateTime _lastTunCpuT = DateTime.MinValue;
        private string _xmxKey = null; private int _xmxMb = -1;
        // v6.9: online player names (server log join/leave) + memory history
        private readonly System.Collections.Generic.List<string> _onlineNames = new System.Collections.Generic.List<string>();
        private readonly System.Collections.Generic.List<double> _memHist = new System.Collections.Generic.List<double>();
        private SlpResult _slp;
        private volatile bool _stoppingIntentional = false;
        private volatile bool _stopping = false;
        private readonly System.Collections.Generic.List<DateTime> _crashTimes = new System.Collections.Generic.List<DateTime>();
        private DateTime _slpAt = DateTime.MinValue;
        private volatile bool _slpBusy = false;
        private System.Windows.Forms.Timer _copyTimer;
        private CardPanel logPanel;
        private InsetPanel logInset, cmdInset, addrInset;
        private TextLabel logToggleLbl, cmdLbl;
        private int _goodW = 0, _goodH = 0;
        private float _uiScale = 1f;
        private bool logCollapsed = false;
        // v6.3 animation engine: 8ms timer paced by timeBeginPeriod(1), Stopwatch time-based easing
        private bool _animActive = false;
        private bool _animExpanding = false;
        private double _animFromH = 0, _animToH = 0, _animFadeFrom = 1.0;
        private int _animDurMs = 0;
        private int _animLastH = -1;
        private double _animLastFade = -1;
        private readonly System.Diagnostics.Stopwatch _animClock = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer _animTimer;
        private bool _sysClientAnim = true;
        private readonly AnimFloat _lblAmt = new AnimFloat(0);

        // 进程
        private Process _server;
        // v6.11: backup tab controls + schedule timer
        private InsetPanel backupPanel;
        private RoundButton btnBackupNow, btnRestoreBackup, btnDeleteBackup;
        private ListBox backupList;
        private TextLabel lblBackupState, lblBackupInfo;
        // v6.22.4: 备份页实测排版所需的标签引用
        private TextLabel lblBackupHead, lblBkKeepPrefix, lblBkKeepSuffix, lblBkHint;
        private TextBox tbKeep, tbDailyTime, tbTimedTime, tbTimedCmd;
        private DarkCheckBox chkAutoRestart, chkBackupOnStart, chkDailyBackup, chkTimedCmd;
        private readonly System.Collections.Generic.List<string> _backupFiles = new System.Collections.Generic.List<string>();
        private bool _bkInit = false;
        private System.Windows.Forms.Timer _schedTimer;
        private Process _frpc;
        private Process _frpcVoice;
        private volatile bool _voiceUp = false;
        private volatile bool _serverDone = false;
        private volatile bool _tunnelUp = false;
        private volatile bool _frpcBundled = false; // 检测到 SakuraFrp 启动器套装版 frpc（不能直接运行）
        private volatile bool _starting = false;

        public MainForm()
        {
            _cfg = Config.Load();
            BuildUi();
            _uiScale = DpiScale.Measure(this);
            DpiScale.ScaleForm(this, 560, 800);
            RefreshStatus();
            // v6.13: tray icon, log-mirror housekeeping, remembered tab
            InitTray613();
            CleanupOldLogs613();
            if (_cfg.LastTab > 0 && segLog != null) segLog.Restore613(_cfg.LastTab);
            AppendServerLog(Txt.MainFormAppendServerLog);
        }

        // v6.13: tray icon - tooltip mirrors server state; double-click or
        // the menu restores the window; the exit item goes through the
        // normal close path (OnClosing asks + safe-stops first).
        void InitTray613()
        {
            try
            {
                _tray = new NotifyIcon();
                try
                {
                    string ico2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.ico");
                    if (File.Exists(ico2)) _tray.Icon = new Icon(ico2);
                    else if (Icon != null) _tray.Icon = Icon;
                }
                catch { }
                if (_tray.Icon == null) _tray.Icon = SystemIcons.Application;
                _tray.Text = Txt.TrayText;
                var menu = new ContextMenuStrip();
                var miShow = new ToolStripMenuItem(Txt.InitTray613ToolStripMenuItem);
                miShow.Click += (s2, e2) => { try { Show(); WindowState = FormWindowState.Normal; Activate(); } catch { } };
                var miExit = new ToolStripMenuItem(Txt.InitTray613ToolStripMenuItem2);
                miExit.Click += (s2, e2) => { try { Close(); } catch { } };
                menu.Items.Add(miShow);
                menu.Items.Add(miExit);
                _tray.ContextMenuStrip = menu;
                _tray.DoubleClick += (s2, e2) => { try { Show(); WindowState = FormWindowState.Normal; Activate(); } catch { } };
                _tray.Visible = true;
                FormClosed += (s2, e2) => { try { if (_tray != null) { _tray.Visible = false; _tray.Dispose(); } } catch { } };
                Activated += (s2, e2) => { QueueHealPaint(); }; // v6.20.1: taskbar/tray restore lands here
            }
            catch { }
        }

        // ---------- UI（v5 美化） ----------
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hWnd, int attr, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, [System.Runtime.InteropServices.In] ref Rectangle lprcUpdate, IntPtr hrgnUpdate, uint flags);
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        static extern uint timeBeginPeriod(uint uPeriod);
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        static extern uint timeEndPeriod(uint uPeriod);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SystemParametersInfo(uint action, uint param, ref bool value, uint winini);
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        void DragMove()
        {
            try { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } catch { }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var br = new SolidBrush(Pal.Bg)) e.Graphics.FillRectangle(br, e.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var pen = new Pen(Pal.Border, 1f)) e.Graphics.DrawRectangle(pen, rect);
        }

        void OnFormLoad()
        {
            TryDwmRound();
            try
            {
                if (_cfg.WinX >= 0 && _cfg.WinY >= 0)
                {
                    var probe = new Rectangle(_cfg.WinX, _cfg.WinY, 80, 80);
                    bool onScreen = false;
                    foreach (var sc in Screen.AllScreens) { if (sc.WorkingArea.IntersectsWith(probe)) { onScreen = true; break; } }
                    if (onScreen) { StartPosition = FormStartPosition.Manual; Location = new Point(_cfg.WinX, _cfg.WinY); }
                }
            }
            catch { }
            try
            {
                _goodW = Size.Width; _goodH = Size.Height;
                _uiScale = ClientSize.Width / 560f;
                if (_uiScale <= 0) _uiScale = 1f;
                MinimumSize = new Size(ClientSize.Width, (int)(494 * _uiScale));
            }
            catch { }
            // v6.22.3: 按当前语言实测宽度重排固定像素行
            ApplyLangLayout();
            // v6.22.4: 备份页同样按当前语言实测重排
            ApplyBackupLayout();
            // v6.6: command history + a slow sync for the overlay scrollbars
            LoadCmdHistory();
            try
            {
                _logSyncTimer = new System.Windows.Forms.Timer();
                _logSyncTimer.Interval = 250;
                _logSyncTimer.Tick += (s, e) => { if (logInset != null && logInset.Visible) SyncLogBars(); };
                _logSyncTimer.Start();
            }
            catch { }
            // v6.7: stats sampling at 1 Hz, only while the tab is showing
            try
            {
                _statsTimer = new System.Windows.Forms.Timer();
                _statsTimer.Interval = 1000;
                _statsTimer.Tick += (s, e) => { if (statsPanel != null && statsPanel.Visible) SampleStats(); };
                _statsTimer.Start();
            }
            catch { }
            // v6.11: schedule checks (daily backup / timed command)
            try
            {
                _schedTimer = new System.Windows.Forms.Timer();
                _schedTimer.Interval = 20000;
                _schedTimer.Tick += (s, e) => CheckSchedules();
                _schedTimer.Start();
            }
            catch { }
        }

        void TryDwmRound()
        {
            try { int v = 2; DwmSetWindowAttribute(Handle, 33, ref v, 4); } catch { }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            try { Invalidate(true); } catch { }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            try { Invalidate(true); } catch { }
        }

        // Invisible frame: keep the standard frame styles so DWM treats this
        // as a normal app window (and animates minimize/restore), while
        // WM_NCCALCSIZE zeroes the frame so nothing visible changes.
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x00C00000 | 0x00040000 | 0x00020000 | 0x00080000; // WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_SYSMENU
                // v6.19: WS_EX_COMPOSITED - present the whole client area
                // atomically from an off-screen buffer. The slowed-tween
                // localization (2026-11-05) showed mid-expand frames with
                // strips of unpainted background: the resize was presented
                // before the panel and the native log TextBoxes painted
                // the newly exposed strip. Compositing makes a
                // half-painted frame undisplayable instead of racing it.
                // v6.20.2: WS_EX_COMPOSITED removed - QaProbe49/50 proved it strands
                // the DWM surface on Hide->Show (visible transparent flash until
                // the +400ms heal); without it every capture is clean from +30ms.
                // cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                // v6.19: CS_VREDRAW | CS_HREDRAW - invalidate the whole
                // client area on every resize step so no strip is left
                // waiting for a late paint (live class style measured
                // 0x00000008 before this change: neither style was set).
                cp.ClassStyle |= 0x0001 | 0x0002; // CS_VREDRAW | CS_HREDRAW
                // v6.5: CS_VREDRAW/CS_HREDRAW removed - redrawing the whole
                // client area on every height change made the native log
                // TextBoxes strobe during the log height tween. Resize now
                // bit-copies the static content and paints only the newly
                // exposed strip; FinishLogAnim still lands with a full repaint.
                return cp;
            }
        }

        protected override void SetClientSizeCore(int x, int y)
        {
            if (IsHandleCreated)
            {
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, x, y, 0x0002 | 0x0004 | 0x0010); // NOMOVE | NOZORDER | NOACTIVATE
                return;
            }
            base.SetClientSizeCore(x, y);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0083) // WM_NCCALCSIZE: client area = whole window rect
            {
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == 0x0086) // WM_NCACTIVATE: suppress frame flash on activation
            {
                m.LParam = (IntPtr)(-1);
            }
            if (m.Msg == 0x001A) // WM_SETTINGCHANGE: user may have toggled system animations
            {
                try { bool b = true; if (SystemParametersInfo(0x1042, 0, ref b, 0)) _sysClientAnim = b; } catch { } UiAnim.Enabled = _sysClientAnim;
            }
            if (m.Msg == 0x0005 && m.WParam == (IntPtr)1 && _animActive) // minimized mid-animation: land on the target
            {
                FinishLogAnim(_animToH, _animExpanding);
            }
            base.WndProc(ref m);
            if (m.Msg == 0x0005 || m.Msg == 0x0047) // WM_SIZE / WM_WINDOWPOSCHANGED: heal any phantom frame-inflation after the burst settles
            {
                try { BeginInvoke(new Action(HealSize)); } catch { }
                QueueHealPaint(); // v6.20.1
            }
            if (m.Msg == 0x0018 && m.WParam != IntPtr.Zero) // v6.20.1: WM_SHOWWINDOW (shown) - same stranded-paint family
            {
                QueueHealPaint();
            }
        }

        // v6.20.1: QaProbe45 reproduced the "ghost window" on the shipped
        // v6.20 build: after a hide -> show transition, horizontal bands of
        // the client area stay unpainted under WS_EX_COMPOSITED and the
        // desktop shows through (worst band = the log panel). Size, opacity
        // and data are all fine; one forced full repaint cures it. Queue a
        // single coalesced repaint after any show/restore/activate burst.
        // NOT gated on _animActive or size equality (unlike HealSize):
        // stranded paint happens exactly when the size never changed.
        private bool _healPaintQueued = false;
        private System.Windows.Forms.Timer _healPaintTimer = null;
        void QueueHealPaint()
        {
            // Delayed second pass (v6.20.1, QaProbe48): the stranded paint
            // lands AFTER the transition's first message burst - a heal
            // invoked at +0 ms does nothing, the same heal later cures
            // 100%. (Re)arm a one-shot 400 ms timer on every trigger so
            // the repaint lands after the burst settles.
            try
            {
                if (_healPaintTimer == null)
                {
                    _healPaintTimer = new System.Windows.Forms.Timer();
                    _healPaintTimer.Interval = 400;
                    _healPaintTimer.Tick += (s, e) =>
                    {
                        _healPaintTimer.Stop();
                        if (!Visible) return;
                        if (WindowState != FormWindowState.Normal) return;
                        try { Invalidate(true); Update(); } catch { }
                    };
                }
                _healPaintTimer.Stop();
                _healPaintTimer.Start();
            }
            catch { }
            if (_healPaintQueued) return;
            if (!IsHandleCreated) return;
            _healPaintQueued = true;
            try
            {
                BeginInvoke(new Action(() =>
                {
                    _healPaintQueued = false;
                    if (!Visible) return;
                    if (WindowState != FormWindowState.Normal) return;
                    try { Invalidate(true); Update(); } catch { }
                }));
            }
            catch { _healPaintQueued = false; }
        }

        // Restoring from minimized makes the framework re-apply the client size
        // inflated by frame metrics this form does not actually have (NCCALCSIZE
        // zeroes them). Snap back to the last known-good window size.
        void HealSize()
        {
            if (_goodW <= 0) return;
            if (WindowState != FormWindowState.Normal) return;
            if (_animActive) return;
            if (Size.Width == _goodW && Size.Height == _goodH) return;
            try { SetWindowPos(Handle, IntPtr.Zero, 0, 0, _goodW, _goodH, 0x0002 | 0x0004 | 0x0010); } catch { }
        }

        void BuildUi()
        {
            Text = Txt.TitleText;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Pal.Bg;
            ForeColor = Pal.Text;
            Font = new Font("Microsoft YaHei UI", 9f);
            Size = new Size(560, 800);
            MinimumSize = new Size(560, 494);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            DoubleBuffered = true;
            try { if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.ico"))) Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.ico")); } catch { }
            Load += (s, e) => { OnFormLoad(); };

            // ---- 自定义标题栏 ----
            var titleBar = new TitleBarPanel();
            titleBar.SetBounds(0, 0, 560, 40);
            titleBar.DragRequested += (s, e) => { DragMove(); };
            titleBar.CloseClicked += (s, e) => { Close(); };
            titleBar.MinClicked += (s, e) => { WindowState = FormWindowState.Minimized; };
            Controls.Add(titleBar);

            // ---- 状态卡片 ----
            statusCard = new StatusCard();
            statusCard.SetBounds(20, 56, 520, 148);
            Controls.Add(statusCard);

            // ---- 主按钮 ----
            btnStart = new RoundButton();
            btnStart.Primary = true;
            btnStart.Radius = 8;
            btnStart.Text = Txt.BtnStartText2;
            btnStart.SetBounds(20, 220, 520, 52);
            btnStart.BackColor = Pal.Green;
            btnStart.ForeColor = Color.White;
            btnStart.Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold);
            btnStart.Click += (s, e) => StartAll();
            Controls.Add(btnStart);

            // ---- 公网地址 ----
            var addrLblLocal = new TextLabel();
            addrLblLocal.BackdropColor = Pal.Bg;
            addrLblLocal.TopAlign = true;
            addrLblLocal.Caption = Txt.AddrLblCaption;
            addrLblLocal.TextShiftX = 1.33f; addrLblLocal.TextShiftY = -0.67f;
            addrLblLocal.SetBounds(20, 290, 500, 18);
            addrLblLocal.BackColor = Color.Transparent;
            addrLblLocal.ForeColor = Pal.Sub;
            addrLblLocal.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            Controls.Add(addrLblLocal);
            addrLbl = addrLblLocal;

            var addrInsetLocal = new InsetPanel();
            addrInsetLocal.SetBounds(20, 314, 392, 38);
            Controls.Add(addrInsetLocal);
            addrInset = addrInsetLocal;

            tbAddr = new HintTextBox();
            tbAddr.Hint = Txt.Hint;
            tbAddr.SetBounds(12, 10, 368, 22);
            tbAddr.BackColor = Pal.Inset;
            tbAddr.ForeColor = Pal.AddrGreen;
            tbAddr.BorderStyle = BorderStyle.None;
            tbAddr.Font = Fonts.Mono(11f, FontStyle.Bold);
            tbAddr.ReadOnly = true;
            tbAddr.TabStop = false;
            tbAddr.DoubleClick += (s, e) => { try { tbAddr.SelectAll(); } catch { } }; // v6.6: double-click selects the whole address
            addrInset.Controls.Add(tbAddr);

            btnCopy = new RoundButton();
            btnCopy.Text = Txt.BtnCopyText;
            btnCopy.SetBounds(420, 314, 120, 38);
            btnCopy.BackColor = Pal.Surface;
            btnCopy.ForeColor = Color.White;
            btnCopy.Font = new Font("Microsoft YaHei UI", 9f);
            btnCopy.Click += (s, e) => CopyAddr();
            Controls.Add(btnCopy);

            // ---- 次要按钮（等宽三列） ----
            btnPcl2 = new RoundButton();
            btnPcl2.Text = Txt.BtnPcl2Text;
            btnPcl2.SetBounds(20, 368, 168, 38);
            btnPcl2.BackColor = Pal.Surface;
            btnPcl2.ForeColor = Color.White;
            btnPcl2.Click += (s, e) => OpenPcl2();
            Controls.Add(btnPcl2);

            btnStop = new RoundButton();
            btnStop.DangerHover = true;
            btnStop.Text = Txt.BtnStopText;
            btnStop.SetBounds(196, 368, 168, 38);
            btnStop.BackColor = Pal.Surface;
            btnStop.ForeColor = Color.White;
            btnStop.Click += (s, e) =>
            {
                if (_stopping) return;
                _stopping = true;
                Thread tStop = new Thread(() => { try { SafeStop(); } finally { _stopping = false; } });
                tStop.IsBackground = true;
                tStop.Start();
            };
            Controls.Add(btnStop);

            btnSettings = new RoundButton();
            btnSettings.Text = Txt.SettingsFormInitChrome;
            btnSettings.SetBounds(372, 368, 168, 38);
            btnSettings.BackColor = Pal.Surface;
            btnSettings.ForeColor = Color.White;
            btnSettings.Click += (s, e) => OpenSettings();
            Controls.Add(btnSettings);

            // ---- 日志卡片 ----
            logPanel = new CardPanel();
            logPanel.SetBounds(20, 422, 520, 358);
            Controls.Add(logPanel);

            logToggleLbl = new TextLabel();
            logToggleLbl.Caption = Txt.LogToggleLblCaption;
            logToggleLbl.TextShiftX = -0.67f;
            logToggleLbl.SetBounds(16, 10, 120, 32);
            logToggleLbl.BackColor = Pal.Card;
            logToggleLbl.ForeColor = Pal.Sub;
            logToggleLbl.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            logToggleLbl.Cursor = Cursors.Hand;
            logToggleLbl.Click += (s, e) => ToggleLog();
            logPanel.Controls.Add(logToggleLbl);
            _lblAmt.Changed = () => { if (!IsDisposed && logToggleLbl != null) logToggleLbl.ForeColor = Gfx.Mix(Pal.Sub, Pal.Text, _lblAmt.Value); };
            logToggleLbl.MouseEnter += (s, e) => _lblAmt.Set(1, 120);
            logToggleLbl.MouseLeave += (s, e) => _lblAmt.Set(0, 80);

            segLog = new SegControl();
            segLog.SetBounds(216, 10, 288, 32);
            segLog.Font = new Font("Microsoft YaHei UI", 9f);
            segLog.Items = new string[] { Txt.BuildUiStr, Txt.BuildUiStr2, Txt.BuildUiStr3, Txt.BuildUiStr4 };
            segLog.Changed += (s, e) => ShowLogPage();
            logPanel.Controls.Add(segLog);

            logInset = new InsetPanel();
            logInset.SetBounds(16, 86, 488, 166);
            logInset.BackdropColor = Pal.Card;
            logPanel.Controls.Add(logInset);

            tbServerLog = MakeLogBox(Txt.BuildUiMakeLogBox);
            tbServerLog.SetBounds(8, 6, 472, 154);
            logInset.Controls.Add(tbServerLog);

            tbTunnelLog = MakeLogBox(Txt.BuildUiMakeLogBox2);
            tbTunnelLog.SetBounds(8, 6, 472, 154);
            tbTunnelLog.Visible = false;
            logInset.Controls.Add(tbTunnelLog);

            // v6.6: slim overlay scrollbars (vertical overlays the box's right
            // edge; horizontal along its bottom) + jump-to-bottom chip
            sbSrvV = new SlimScrollBar(); sbSrvV.Target = tbServerLog; sbSrvV.SetBounds(472, 6, 8, 146);
            sbSrvH = new SlimScrollBar(); sbSrvH.Horizontal = true; sbSrvH.Target = tbServerLog; sbSrvH.MaxChars = () => _maxCharsSrv; sbSrvH.SetBounds(8, 152, 464, 8);
            sbTunV = new SlimScrollBar(); sbTunV.Target = tbTunnelLog; sbTunV.SetBounds(472, 6, 8, 146);
            sbTunH = new SlimScrollBar(); sbTunH.Horizontal = true; sbTunH.Target = tbTunnelLog; sbTunH.MaxChars = () => _maxCharsTun; sbTunH.SetBounds(8, 152, 464, 8);
            logInset.Controls.Add(sbSrvV); logInset.Controls.Add(sbSrvH);
            logInset.Controls.Add(sbTunV); logInset.Controls.Add(sbTunH);
            btnJumpBottom = new RoundButton();
            btnJumpBottom.Text = Txt.BtnJumpBottomText;
            btnJumpBottom.BackColor = Pal.Surface;
            btnJumpBottom.ForeColor = Pal.Text;
            btnJumpBottom.BackdropColor = Pal.Inset;
            btnJumpBottom.Radius = 8;
            btnJumpBottom.SetBounds(378, 120, 96, 26);
            btnJumpBottom.Visible = false;
            btnJumpBottom.Click += (s, e) => JumpToBottom();
            logInset.Controls.Add(btnJumpBottom);
            // v6.11: search row above the server log (page 0 only)
            searchRow = new Panel();
            searchRow.SetBounds(16, 52, 488, 28);
            searchRow.BackColor = Pal.Card;
            logPanel.Controls.Add(searchRow);
            tbSearch = new HintTextBox();
            tbSearch.Hint = Txt.Hint2;
            tbSearch.SetBounds(0, 2, 218, 24);
            tbSearch.BackColor = Color.FromArgb(17, 20, 24);
            tbSearch.ForeColor = Pal.Text;
            tbSearch.BorderStyle = BorderStyle.None;
            tbSearch.Font = new Font("Microsoft YaHei UI", 9f);
            tbSearch.KeyDown += (s2, e2) => { if (e2.KeyCode == Keys.Enter) { e2.SuppressKeyPress = true; FindInLog(!e2.Shift); } };
            tbSearch.TextChanged += (s2, e2) => { if (lblFind != null) lblFind.Caption = ""; };
            searchRow.Controls.Add(tbSearch);
            btnPrevSearch = new RoundButton();
            btnPrevSearch.Text = "↑";
            btnPrevSearch.SetBounds(224, 0, 44, 28);
            btnPrevSearch.BackColor = Pal.Surface; btnPrevSearch.ForeColor = Pal.Text; btnPrevSearch.BackdropColor = Pal.Card;
            btnPrevSearch.Font = new Font("Microsoft YaHei UI", 9f);
            btnPrevSearch.Click += (s2, e2) => FindInLog(false);
            searchRow.Controls.Add(btnPrevSearch);
            btnNextSearch = new RoundButton();
            btnNextSearch.Text = "↓";
            btnNextSearch.SetBounds(272, 0, 44, 28);
            btnNextSearch.BackColor = Pal.Surface; btnNextSearch.ForeColor = Pal.Text; btnNextSearch.BackdropColor = Pal.Card;
            btnNextSearch.Font = new Font("Microsoft YaHei UI", 9f);
            btnNextSearch.Click += (s2, e2) => FindInLog(true);
            searchRow.Controls.Add(btnNextSearch);
            _tips = new ToolTip();
            _tips.SetToolTip(btnPrevSearch, Txt.BuildUiSetToolTip);
            _tips.SetToolTip(btnNextSearch, Txt.BuildUiSetToolTip2);
            _tips.SetToolTip(tbSearch, Txt.BuildUiSetToolTip3);
            lblFind = new TextLabel();
            lblFind.Caption = "";
            lblFind.SetBounds(322, 2, 60, 24);
            lblFind.ForeColor = Pal.Sub; lblFind.BackColor = Pal.Card;
            lblFind.Font = new Font("Microsoft YaHei UI", 9f);
            searchRow.Controls.Add(lblFind);
            chkErrOnly = new DarkCheckBox();
            chkErrOnly.Text = Txt.ChkErrOnlyText;
            chkErrOnly.SetBounds(386, 2, 102, 24);
            chkErrOnly.ForeColor = Pal.Text; chkErrOnly.BackColor = Pal.Card;
            chkErrOnly.Font = new Font("Microsoft YaHei UI", 8f);
            chkErrOnly.CheckedChanged += (s2, e2) => SetSrvFilter(chkErrOnly.Checked);
            searchRow.Controls.Add(chkErrOnly);
            logInset.Controls.SetChildIndex(sbSrvV, 0);
            logInset.Controls.SetChildIndex(sbSrvH, 0);
            logInset.Controls.SetChildIndex(sbTunV, 0);
            logInset.Controls.SetChildIndex(sbTunH, 0);
            logInset.Controls.SetChildIndex(btnJumpBottom, 0);

            // v6.7: stats tab page, same footprint as log inset + command row
            statsPanel = new StatsPanel();
            statsPanel.ManageClicked += (s, e) => OpenPlayerManager();
            statsPanel.SetBounds(16, 52, 488, 290);
            statsPanel.Visible = false;
            logPanel.Controls.Add(statsPanel);

            // v6.11: backup tab (world backups + schedules + watchdog toggle)
            // v6.22.4: 290 → 302，为底部整句提示留出两行高度
            backupPanel = new InsetPanel();
            backupPanel.SetBounds(16, 52, 488, 302);
            backupPanel.BackdropColor = Pal.Card;
            backupPanel.Visible = false;
            logPanel.Controls.Add(backupPanel);
            BuildBackupPanel();

            cmdLbl = new TextLabel();
            cmdLbl.TopAlign = true;
            cmdLbl.Caption = Txt.CmdLblCaption;
            cmdLbl.TextShiftX = 2f; cmdLbl.TextShiftY = -0.67f;
            cmdLbl.SetBounds(16, 312, 72, 26);
            cmdLbl.BackColor = Pal.Card;
            cmdLbl.ForeColor = Pal.Sub;
            cmdLbl.Font = new Font("Microsoft YaHei UI", 9f);
            logPanel.Controls.Add(cmdLbl);

            cmdInset = new InsetPanel();
            cmdInset.FocusGlow = true;
            cmdInset.SetBounds(92, 308, 320, 34);
            cmdInset.BackdropColor = Pal.Card;
            logPanel.Controls.Add(cmdInset);

            tbCmd = new TextBox();
            tbCmd.SetBounds(10, 7, 300, 22);
            tbCmd.BackColor = Pal.Inset;
            tbCmd.ForeColor = Pal.Text;
            tbCmd.BorderStyle = BorderStyle.None;
            tbCmd.Font = Fonts.Mono(9f, FontStyle.Regular);
            tbCmd.KeyDown += CmdKeyDown;
            tbCmd.GotFocus += (s, e) => { cmdInset.Invalidate(); };
            tbCmd.LostFocus += (s, e) => { cmdInset.Invalidate(); };
            cmdInset.Controls.Add(tbCmd);

            btnCmdSend = new RoundButton();
            btnCmdSend.Text = Txt.BtnCmdSendText;
            btnCmdSend.SetBounds(420, 308, 84, 34);
            btnCmdSend.BackColor = Pal.Surface;
            btnCmdSend.BackdropColor = Pal.Card;
            btnCmdSend.ForeColor = Color.White;
            btnCmdSend.Click += (s, e) => SendServerCommand();
            logPanel.Controls.Add(btnCmdSend);

            // v6.21: spark 性能诊断行（日志框下方、指令行上方；与指令行同显隐）
            sparkLbl = new TextLabel();
            sparkLbl.TopAlign = true;
            sparkLbl.Caption = Txt.SparkLblCaption;
            sparkLbl.TextShiftX = 2f; sparkLbl.TextShiftY = -0.67f;
            sparkLbl.SetBounds(16, 262, 72, 26);
            sparkLbl.BackColor = Pal.Card;
            sparkLbl.ForeColor = Pal.Sub;
            sparkLbl.Font = new Font("Microsoft YaHei UI", 9f);
            logPanel.Controls.Add(sparkLbl);

            btnSparkTps = new RoundButton();
            btnSparkTps.Text = Txt.BtnSparkTpsText;
            btnSparkTps.SetBounds(92, 258, 130, 34);
            btnSparkTps.BackColor = Pal.Surface;
            btnSparkTps.BackdropColor = Pal.Card;
            btnSparkTps.ForeColor = Color.White;
            btnSparkTps.Enabled = false;
            btnSparkTps.Click += (s, e) => SparkTps();
            logPanel.Controls.Add(btnSparkTps);

            btnSparkProfile = new RoundButton();
            btnSparkProfile.Text = Txt.BtnSparkProfileText;
            btnSparkProfile.SetBounds(230, 258, 150, 34);
            btnSparkProfile.BackColor = Pal.Surface;
            btnSparkProfile.BackdropColor = Pal.Card;
            btnSparkProfile.ForeColor = Color.White;
            btnSparkProfile.Enabled = false;
            btnSparkProfile.Click += (s, e) => SparkProfileStart();
            logPanel.Controls.Add(btnSparkProfile);

            try { bool b = true; if (SystemParametersInfo(0x1042, 0, ref b, 0)) _sysClientAnim = b; } catch { } UiAnim.Enabled = _sysClientAnim;
            _animTimer = new System.Windows.Forms.Timer();
            _animTimer.Interval = 8;
            _animTimer.Tick += (s, e) => AnimFrame();

            KeyPreview = true;
            Shown += (s, e) => { if (string.IsNullOrEmpty(_cfg.FrpKey)) { AppendState(Txt.BuildUiAppendState); } };
            Shown += (s, e) => { BeginInvoke(new Action(() => { try { OfferWizardIfNeeded(); } catch { } })); };
            // v6.6 trial: one full repaint right after the first frame settles,
            // to normalize any first-paint edge rasterization (QA measures it).
            Shown += (s, e) => { BeginInvoke(new Action(() => { try { Invalidate(true); Update(); } catch { } })); };
            FormClosing += OnClosing;
        }

        // v6.22.3: 主窗口固定像素行的多语言重排。
        // 设计坐标按中/英译文确定，法/西/葡/俄更长时会截断或压叠；
        // 这里在 DPI 缩放之后用真实字体实测，把宽度让给更长的译文，
        // 中/英布局与原来完全一致（实测宽度未超过设计宽度时保持原值）。
        void ApplyLangLayout()
        {
            if (btnCopy == null || addrInset == null || segLog == null) return;
            float sc = _uiScale > 0 ? _uiScale : 1f;
            Func<double, int> D = v => (int)Math.Round(v * sc);
            Font f9 = null, f8 = null;
            try
            {
                f9 = new Font("Microsoft YaHei UI", 9f);
                f8 = new Font("Microsoft YaHei UI", 8f);
                // 1) 公网地址行：复制按钮按译文加宽，地址框让位
                int copyW = Math.Min(D(240), Math.Max(D(120), TextFit.Measure(Txt.BtnCopyText, f9) + D(26)));
                int copyX = D(540) - copyW;
                btnCopy.SetBounds(copyX, D(314), copyW, D(38));
                int insetW = Math.Max(D(180), copyX - D(8) - D(20));
                addrInset.SetBounds(D(20), D(314), insetW, D(38));
                tbAddr.SetBounds(D(12), D(10), Math.Max(D(80), insetW - D(24)), D(22));
                // 2) 三个次要按钮：520px 内按译文比例分配
                int[] w3 = TextFit.Distribute(
                    new string[] { Txt.BtnPcl2Text, Txt.BtnStopText, Txt.SettingsFormInitChrome },
                    f9, D(520), D(8), D(24), D(96), true);
                int bx = D(20);
                btnPcl2.SetBounds(bx, D(368), w3[0], D(38)); bx += w3[0] + D(8);
                btnStop.SetBounds(bx, D(368), w3[1], D(38)); bx += w3[1] + D(8);
                btnSettings.SetBounds(bx, D(368), w3[2], D(38));
                // 3) 日志区：折叠标签与页签条（288 → 312，段宽由 SegControl 自算）
                logToggleLbl.SetBounds(D(16), D(10),
                    Math.Min(D(168), Math.Max(D(120), TextFit.Measure(Txt.LogToggleLblCaption, f9) + D(12))), D(32));
                segLog.SetBounds(D(196), D(10), D(312), D(32));
                // 4) 搜索行：错误过滤复选框按译文加宽，其余控件左移让位
                // v6.22.4: 上限 204 → 236，长译文（俄/德）不再被上限截断，宽度从 tbSearch 让出
                int chkW = Math.Min(D(236), Math.Max(D(102), TextFit.Measure(Txt.ChkErrOnlyText, f8) + D(46)));
                int chkX = D(488) - chkW;
                chkErrOnly.SetBounds(chkX, D(2), chkW, D(24));
                int findX = chkX - D(56);
                lblFind.SetBounds(findX, D(2), D(52), D(24));
                int nextX = findX - D(48);
                btnNextSearch.SetBounds(nextX, D(0), D(44), D(28));
                int prevX = nextX - D(48);
                btnPrevSearch.SetBounds(prevX, D(0), D(44), D(28));
                tbSearch.SetBounds(D(0), D(2), Math.Max(D(90), prevX - D(6)), D(24));
                // 5) 指令行 / 性能诊断行：左侧标签加宽，输入区右移
                int cmdW = Math.Min(D(124), Math.Max(D(72), TextFit.Measure(Txt.CmdLblCaption, f9) + D(8)));
                cmdLbl.SetBounds(D(16), D(312), cmdW, D(26));
                int cmdX = D(16) + cmdW + D(4);
                cmdInset.SetBounds(cmdX, D(308), Math.Max(D(140), D(412) - cmdX), D(34));
                tbCmd.SetBounds(D(10), D(7), Math.Max(D(60), cmdInset.Width - D(20)), D(22));
                int sparkW = Math.Min(D(124), Math.Max(D(72), TextFit.Measure(Txt.SparkLblCaption, f9) + D(8)));
                sparkLbl.SetBounds(D(16), D(262), sparkW, D(26));
                int spX = D(16) + sparkW + D(4);
                int tpsW = Math.Min(D(190), Math.Max(D(130), TextFit.Measure(Txt.BtnSparkTpsText, f9) + D(26)));
                btnSparkTps.SetBounds(spX, D(258), tpsW, D(34));
                int profW = Math.Min(D(220), Math.Max(D(150), TextFit.Measure(Txt.BtnSparkProfileText, f9) + D(26)));
                if (spX + tpsW + D(8) + profW > D(504)) profW = Math.Max(D(110), D(504) - spX - tpsW - D(8));
                btnSparkProfile.SetBounds(spX + tpsW + D(8), D(258), profW, D(34));
                // 6) 地址标签与“回到最底部”按钮
                addrLbl.SetBounds(D(20), D(290), D(500), D(18));
                int jumpW = Math.Max(D(96), TextFit.Measure(Txt.BtnJumpBottomText, f9) + D(26));
                btnJumpBottom.SetBounds(D(474) - jumpW, D(120), jumpW, D(26));
            }
            catch { }
            finally
            {
                if (f9 != null) f9.Dispose();
                if (f8 != null) f8.Dispose();
            }
        }

        // v6.22.4: 备份页按当前语言实测重排——三个按钮按译文分配、四个复选框按译文加宽、
        // 底部整句提示改为整行换行；法/德/西/葡/俄的长译文不再被裁。
        void ApplyBackupLayout()
        {
            if (backupPanel == null || btnBackupNow == null || backupList == null || lblBkHint == null) return;
            float sc = _uiScale > 0 ? _uiScale : 1f;
            Func<double, int> D = v => (int)Math.Round(v * sc);
            Font f9 = null;
            try
            {
                f9 = new Font("Microsoft YaHei UI", 9f);
                // 标题 + 右上角“立即备份”
                int hw = Math.Min(D(260), Math.Max(D(180), TextFit.Measure(Txt.BuildBackupPanelBkLabel, f9) + D(12)));
                lblBackupHead.SetBounds(D(16), D(10), hw, D(24));
                int bw = Math.Min(D(240), Math.Max(D(116), TextFit.Measure(Txt.BtnBackupNowText, f9) + D(26)));
                btnBackupNow.SetBounds(D(472) - bw, D(8), bw, D(28));
                lblBackupState.SetBounds(D(16), D(38), D(456), D(24));
                backupList.SetBounds(D(16), D(62), D(456), D(74));
                // 还原 / 删除：456px 内按译文比例分配，信息标签接在右侧
                int[] w2 = TextFit.Distribute(
                    new string[] { Txt.BtnRestoreBackupText, Txt.BtnDeleteBackupText },
                    f9, D(456), D(8), D(24), D(104));
                int bx = D(16);
                btnRestoreBackup.SetBounds(bx, D(140), w2[0], D(28)); bx += w2[0] + D(8);
                btnDeleteBackup.SetBounds(bx, D(140), w2[1], D(28));
                int infoX = bx + w2[1] + D(8);
                lblBackupInfo.SetBounds(infoX, D(142), Math.Max(D(40), D(472) - infoX), D(24));
                // 保留份数行 + 右侧“崩溃自动重启”
                int pfxW = Math.Max(D(40), TextFit.Measure(Txt.BuildBackupPanelBkLabel2, f9) + D(4));
                lblBkKeepPrefix.SetBounds(D(16), D(170), pfxW, D(24));
                int keepX = D(16) + pfxW + D(4);
                tbKeep.SetBounds(keepX, D(170), D(44), D(24));
                int sfxX = keepX + D(44) + D(4);
                int sfxW = Math.Max(D(20), TextFit.Measure(Txt.BuildBackupPanelBkLabel3, f9) + D(4));
                lblBkKeepSuffix.SetBounds(sfxX, D(170), sfxW, D(24));
                int cbW = Math.Min(D(340), Math.Max(D(128), TextFit.Measure(Txt.BuildBackupPanelBkCheck, f9) + D(30)));
                int cbX = Math.Max(sfxX + sfxW + D(10), D(472) - cbW);
                cbW = Math.Max(D(90), D(472) - cbX);
                chkAutoRestart.SetBounds(cbX, D(170), cbW, D(24));
                // 其余三个复选框各自按译文加宽（本版各语言单行均放得下）
                int wA = Math.Min(D(456), Math.Max(D(140), TextFit.Measure(Txt.BuildBackupPanelBkCheck2, f9) + D(30)));
                chkBackupOnStart.SetBounds(D(16), D(196), wA, D(24));
                int wB = Math.Min(D(456), Math.Max(D(118), TextFit.Measure(Txt.BuildBackupPanelBkCheck3, f9) + D(30)));
                chkDailyBackup.SetBounds(D(16), D(222), wB, D(24));
                tbDailyTime.SetBounds(D(16) + wB + D(8), D(222), D(56), D(24));
                int wC = Math.Min(D(456), Math.Max(D(118), TextFit.Measure(Txt.BuildBackupPanelBkCheck4, f9) + D(30)));
                chkTimedCmd.SetBounds(D(16), D(248), wC, D(24));
                int ttx = D(16) + wC + D(8);
                tbTimedTime.SetBounds(ttx, D(248), D(56), D(24));
                int tcx = ttx + D(56) + D(8);
                tbTimedCmd.SetBounds(tcx, D(248), Math.Max(D(120), D(472) - tcx), D(24));
                // 底部整句提示：整行换行，两行高度
                lblBkHint.SetBounds(D(16), D(274), D(456), Math.Max(D(24), D(302) - D(274)));
            }
            catch { }
            finally { if (f9 != null) f9.Dispose(); }
        }

        // v6.11: page 0 carries the search row, so the log inset sits
        // lower and shorter there; the other log page keeps the full area.
        void LayoutLogChrome(int page)
        {
            if (logInset == null) return;
            // Runtime bounds are DEVICE pixels in this app (the build-time
            // logical values were scaled once by the DPI pass), so every
            // number here goes through the scale factor.
            float sc = _uiScale > 0 ? _uiScale : 1f;
            Func<double, int> D = v => (int)Math.Round(v * sc);
            bool srv = page == 0;
            if (srv)
            {
                logInset.SetBounds(D(16), D(86), D(488), D(166));
                tbServerLog.SetBounds(D(8), D(6), D(472), D(154));
                tbTunnelLog.SetBounds(D(8), D(6), D(472), D(154));
                sbSrvV.SetBounds(D(472), D(6), D(8), D(146)); sbSrvH.SetBounds(D(8), D(152), D(464), D(8));
                sbTunV.SetBounds(D(472), D(6), D(8), D(146)); sbTunH.SetBounds(D(8), D(152), D(464), D(8));
                if (btnJumpBottom != null) btnJumpBottom.SetBounds(D(474) - btnJumpBottom.Width, D(120), btnJumpBottom.Width, D(26));
            }
            else
            {
                logInset.SetBounds(D(16), D(52), D(488), D(200));
                tbServerLog.SetBounds(D(8), D(6), D(472), D(188));
                tbTunnelLog.SetBounds(D(8), D(6), D(472), D(188));
                sbSrvV.SetBounds(D(472), D(6), D(8), D(180)); sbSrvH.SetBounds(D(8), D(186), D(464), D(8));
                sbTunV.SetBounds(D(472), D(6), D(8), D(180)); sbTunH.SetBounds(D(8), D(186), D(464), D(8));
                if (btnJumpBottom != null) btnJumpBottom.SetBounds(D(474) - btnJumpBottom.Width, D(154), btnJumpBottom.Width, D(26));
            }
        }

        static bool IsErrLine(string ln)
        {
            return ln != null && (ln.Contains("/WARN") || ln.Contains("/ERROR") || ln.Contains("/FATAL") || ln.Contains("Exception") || ln.Contains(Txt.IsErrLineContains) || ln.Contains(Txt.IsErrLineContains2));
        }

        void SetSrvFilter(bool on)
        {
            _srvFilterErr = on;
            try
            {
                var keep = on ? _srvLines.FindAll(IsErrLine) : _srvLines;
                tbServerLog.Text = keep.Count > 0 ? string.Join(Environment.NewLine, keep.ToArray()) + Environment.NewLine : "";
                tbServerLog.SelectionStart = tbServerLog.TextLength;
                tbServerLog.ScrollToCaret();
                SyncLogBars();
            }
            catch { }
        }

        void FindInLog(bool next)
        {
            if (tbSearch == null || tbServerLog == null) return;
            string q = tbSearch.Text;
            if (q.Length == 0 || tbServerLog.TextLength == 0) { if (lblFind != null) lblFind.Caption = ""; return; }
            string text = tbServerLog.Text;
            var hits = new System.Collections.Generic.List<int>();
            int scan = 0;
            while (scan <= text.Length - q.Length)
            {
                int h = text.IndexOf(q, scan, StringComparison.OrdinalIgnoreCase);
                if (h < 0) break;
                hits.Add(h);
                scan = h + Math.Max(1, q.Length);
            }
            if (hits.Count == 0) { lblFind.Caption = Txt.LblFindCaption; return; }
            int idx;
            if (next)
            {
                int pos = tbServerLog.SelectionStart + tbServerLog.SelectionLength;
                idx = hits.FindIndex(h => h >= pos);
                if (idx < 0) idx = 0;
            }
            else
            {
                idx = -1;
                for (int i = hits.Count - 1; i >= 0; i--) { if (hits[i] < tbServerLog.SelectionStart) { idx = i; break; } }
                if (idx < 0) idx = hits.Count - 1;
            }
            tbServerLog.Focus();
            tbServerLog.SelectionStart = hits[idx];
            tbServerLog.SelectionLength = q.Length;
            tbServerLog.ScrollToCaret();
            lblFind.Caption = (idx + 1) + "/" + hits.Count;
        }

        void ShowLogPage()
        {
            int page = segLog == null ? 0 : segLog.SelectedIndex;
            if (_cfg != null && _cfg.LastTab != page) { _cfg.LastTab = page; try { _cfg.Save(); } catch { } } // v6.13
            bool stats = page == 2;
            bool backup = page == 3;
            if (tbServerLog != null) tbServerLog.Visible = page == 0;
            if (tbTunnelLog != null) tbTunnelLog.Visible = page == 1;
            // v6.7: the stats tab replaces the log inset AND the command row;
            // v6.11: the backup tab does the same with its own panel
            if (logInset != null) logInset.Visible = !stats && !backup;
            if (statsPanel != null) statsPanel.Visible = stats;
            if (backupPanel != null) backupPanel.Visible = backup;
            if (searchRow != null) searchRow.Visible = page == 0;
            if (cmdLbl != null) cmdLbl.Visible = !stats && !backup;
            if (cmdInset != null) cmdInset.Visible = !stats && !backup;
            if (btnCmdSend != null) btnCmdSend.Visible = !stats && !backup;
            if (sparkLbl != null) sparkLbl.Visible = !stats && !backup;
            if (btnSparkTps != null) btnSparkTps.Visible = !stats && !backup;
            if (btnSparkProfile != null) btnSparkProfile.Visible = !stats && !backup;
            if (backup) RefreshBackupList();
            LayoutLogChrome(page);
            // v6.5: the incoming log fades in over ~180ms instead of popping
            // (text color ramps from the inset background color to LogText);
            // the outgoing page vanishing is masked by the sliding tab thumb.
            if (_logFade.Changed == null)
            {
                _logFade.Changed = () =>
                {
                    if (IsDisposed) return;
                    var c = Gfx.Mix(Pal.Inset, Pal.LogText, _logFade.Value);
                    try { if (tbServerLog != null) tbServerLog.ForeColor = c; if (tbTunnelLog != null) tbTunnelLog.ForeColor = c; } catch { }
                };
            }
            if (!stats && _sysClientAnim && UiAnim.Enabled) { _logFade.Set(0.12, 0); _logFade.Set(1, 180); }
            if (stats) SampleStats();
            SyncLogBars();
        }

        // ---------- 状态监控采样（v6.7） ----------
        [System.Runtime.InteropServices.DllImport("iphlpapi.dll", SetLastError = true)]
        static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwSize, bool bOrder, int ulAf, int TableClass, uint Reserved);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] MEMORYSTATUSEX ms);
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        class MEMORYSTATUSEX
        {
            public uint dwLength = 64;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        static string FmtBytes(double b)
        {
            if (b < 1024) return ((long)b) + " B";
            double kb = b / 1024.0;
            if (kb < 1024) return kb.ToString("0.0") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("0.0") + " MB";
            return (mb / 1024.0).ToString("0.00") + " GB";
        }
        static string FmtDur(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            return ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00");
        }
        static double CpuRate(TimeSpan cpu, DateTime t, ref TimeSpan lastCpu, ref DateTime lastT)
        {
            double pct = -1;
            if (lastT != DateTime.MinValue)
            {
                double wall = (t - lastT).TotalSeconds;
                if (wall > 0.2) pct = (cpu - lastCpu).TotalSeconds / wall / Environment.ProcessorCount * 100.0;
                if (pct < 0) pct = 0;
            }
            lastCpu = cpu; lastT = t;
            return pct;
        }
        int XmxMb()
        {
            string mp = _cfg.MemParams ?? "";
            if (mp == _xmxKey) return _xmxMb;
            _xmxKey = mp; _xmxMb = -1;
            var m = Regex.Match(mp, @"-Xmx(\d+)\s*([gGmMkK]?)");
            if (m.Success)
            {
                long v = long.Parse(m.Groups[1].Value);
                string u = m.Groups[2].Value.ToLower();
                if (u == "g") _xmxMb = (int)(v * 1024);
                else if (u == "k") _xmxMb = (int)(v / 1024);
                else if (u == "m") _xmxMb = (int)v;
                else _xmxMb = (int)(v / 1048576); // bare number = bytes
            }
            return _xmxMb;
        }
        int CountConns(int pid, int port)
        {
            IntPtr buf = IntPtr.Zero;
            try
            {
                int size = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 5, 0);
                if (size <= 0) return 0;
                buf = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
                if (GetExtendedTcpTable(buf, ref size, false, 2, 5, 0) != 0) return 0;
                int n = System.Runtime.InteropServices.Marshal.ReadInt32(buf);
                int count = 0;
                long basePtr = buf.ToInt64() + 4;
                for (int i = 0; i < n; i++)
                {
                    long row = basePtr + (long)i * 24; // MIB_TCPROW_OWNER_PID
                    int state = System.Runtime.InteropServices.Marshal.ReadInt32((IntPtr)(row + 0));
                    int lportRaw = System.Runtime.InteropServices.Marshal.ReadInt32((IntPtr)(row + 8));
                    int opid = System.Runtime.InteropServices.Marshal.ReadInt32((IntPtr)(row + 20));
                    int lport = ((lportRaw & 0xFF) << 8) | ((lportRaw >> 8) & 0xFF);
                    if (opid == pid && lport == port && state == 5) count++; // ESTABLISHED
                }
                return count;
            }
            catch { return 0; }
            finally { if (buf != IntPtr.Zero) System.Runtime.InteropServices.Marshal.FreeHGlobal(buf); }
        }

        void SampleStats()
        {
            if (statsPanel == null) return;
            try
            {
                DateTime now = DateTime.Now;
                // ---- server process ----
                if (ProcAlive(_server))
                {
                    try
                    {
                        _server.Refresh();
                        statsPanel.SrvState = Txt.SrvState2 + FmtDur(now - _server.StartTime);
                        statsPanel.SrvStateColor = Color.FromArgb(64, 199, 116);
                        long ws = _server.WorkingSet64;
                        statsPanel.SrvMem = FmtBytes(ws);
                        int xmx = XmxMb();
                        if (xmx > 0)
                        {
                            statsPanel.SrvMemFrac = ws / (xmx * 1048576.0);
                            statsPanel.SrvMemCap = "";
                        }
                        else { statsPanel.SrvMemFrac = -1; statsPanel.SrvMemCap = ""; }
                        double pc = CpuRate(_server.TotalProcessorTime, now, ref _lastSrvCpu, ref _lastSrvCpuT);
                        statsPanel.SrvCpu = pc < 0 ? "—" : pc.ToString("0.0") + " %";
                        statsPanel.SrvConn = CountConns(_server.Id, _cfg.Port) + Txt.SampleStatsPart;
                        // v6.9: memory history for the sparkline + player names
                        if (statsPanel.SrvMemFrac >= 0)
                        {
                            _memHist.Add(statsPanel.SrvMemFrac);
                            while (_memHist.Count > 60) _memHist.RemoveAt(0);
                            statsPanel.SrvMemHist = _memHist.ToArray();
                        }
                        statsPanel.SrvPlayers = _onlineNames.Count > 0 ? string.Join("、", _onlineNames.ToArray()) : "—";
                        // v6.11: poll the server's own status protocol every
                        // few seconds; its answer outranks the log-derived
                        // names (and knows version / max / latency too)
                        if (!_slpBusy && (now - _slpAt).TotalSeconds >= 4)
                        {
                            _slpBusy = true; _slpAt = now;
                            int sport = _cfg.Port;
                            System.Threading.ThreadPool.QueueUserWorkItem(_q =>
                            {
                                var r = Slp.Query("127.0.0.1", sport, 1500);
                                _slp = r; _slpBusy = false;
                            });
                        }
                        var slpNow = _slp;
                        if (slpNow != null && slpNow.Ok)
                        {
                            if (slpNow.Names.Count > 0) statsPanel.SrvPlayers = string.Join("、", slpNow.Names.ToArray());
                            else if (slpNow.Online > 0) statsPanel.SrvPlayers = slpNow.Online + Txt.SampleStatsPart2;
                            string vp = slpNow.Version ?? "";
                            if (slpNow.Max > 0) vp += (vp.Length > 0 ? " · " : "") + slpNow.Online + "/" + slpNow.Max + Txt.SampleStatsPart;
                            if (slpNow.PingMs >= 0) vp += (vp.Length > 0 ? " · " : "") + Txt.SampleStatsPart3 + slpNow.PingMs + " ms";
                            if (vp.Length > 0) statsPanel.SrvVerPing = vp;
                        }
                    }
                    catch { }
                }
                else
                {
                    statsPanel.SrvState = Txt.SrvState; statsPanel.SrvStateColor = Pal.Mute;
                    statsPanel.SrvMem = "—"; statsPanel.SrvMemFrac = -1; statsPanel.SrvMemCap = "";
                    statsPanel.SrvCpu = "—"; statsPanel.SrvConn = "—";
                    statsPanel.SrvPlayers = "—"; statsPanel.SrvVerPing = "—"; _slp = null;
                    if (_memHist.Count > 0) { _memHist.Clear(); statsPanel.SrvMemHist = new double[0]; }
                    if (_onlineNames.Count > 0) _onlineNames.Clear();
                    _lastSrvCpuT = DateTime.MinValue;
                }
                // ---- tunnel processes ----
                long tws = 0; TimeSpan tcpu = TimeSpan.Zero; bool anyTun = false;
                foreach (var p in new Process[] { _frpc, _frpcVoice })
                {
                    if (ProcAlive(p)) { try { p.Refresh(); tws += p.WorkingSet64; tcpu += p.TotalProcessorTime; anyTun = true; } catch { } }
                }
                if (anyTun)
                {
                    statsPanel.TunMem = FmtBytes(tws);
                    double pc = CpuRate(tcpu, now, ref _lastTunCpu, ref _lastTunCpuT);
                    statsPanel.TunCpu = pc < 0 ? "—" : pc.ToString("0.0") + " %";
                }
                else { statsPanel.TunMem = "—"; statsPanel.TunCpu = "—"; _lastTunCpuT = DateTime.MinValue; }
                // ---- whole-machine throughput (per-process bytes are not
                // exposed by the framework; this is the honest machine total)
                long rx = 0, tx = 0;
                try
                {
                    foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                        if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;
                        try { var s = ni.GetIPv4Statistics(); rx += s.BytesReceived; tx += s.BytesSent; } catch { }
                    }
                }
                catch { }
                if (_lastNetT != DateTime.MinValue)
                {
                    double dt = (now - _lastNetT).TotalSeconds;
                    if (dt > 0.2)
                    {
                        statsPanel.NetDown = FmtBytes((rx - _lastNetRx) / dt) + "/s";
                        statsPanel.NetUp = FmtBytes((tx - _lastNetTx) / dt) + "/s";
                    }
                }
                _lastNetRx = rx; _lastNetTx = tx; _lastNetT = now;
                // ---- system memory ----
                try
                {
                    var ms = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(ms))
                    {
                        double tot = ms.ullTotalPhys, used = tot - ms.ullAvailPhys;
                        statsPanel.SysMem = FmtBytes(used) + " / " + FmtBytes(tot) + " · " + ms.dwMemoryLoad + " %";
                    }
                }
                catch { }
                statsPanel.Invalidate();
            }
            catch { }
        }

        // ---------- v6.3 animation engine ----------
        // Time-based tween on an 8ms pacing timer (timeBeginPeriod(1) makes it real):
        // position is a pure function of wall-clock time, so a late frame jumps
        // to the correct place instead of drifting or crawling at the tail.
        static double CubicBezier(double x1, double y1, double x2, double y2, double x)
        {
            double t = x;
            for (int i = 0; i < 8; i++)
            {
                double u = 1 - t;
                double cx = 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t - x;
                double dx = 3 * u * u * x1 + 6 * u * t * (x2 - x1) + 3 * t * t * (1 - x2);
                if (Math.Abs(cx) < 1e-6 || Math.Abs(dx) < 1e-6) break;
                t -= cx / dx;
                if (t < 0) t = 0; if (t > 1) t = 1;
            }
            double v = 1 - t;
            return 3 * v * v * t * y1 + 3 * v * t * t * y2 + t * t * t;
        }

        void StartLogAnim(bool expanding)
        {
            double toH = (expanding ? 800 : 494) * _uiScale;
            double fromH = Height;
            double full = Math.Abs(800 - 494) * _uiScale;
            double frac = full > 0 ? Math.Abs(toH - fromH) / full : 0;
            if (!_sysClientAnim || frac < 0.01) { FinishLogAnim(toH, expanding); return; }
            _animDurMs = Math.Max(60, (int)((expanding ? 300 : 200) * frac));
            _animFromH = fromH;
            _animToH = toH;
            _animExpanding = expanding;
            _animFadeFrom = segLog.Fade;
            _animLastH = -1;
            _animLastFade = -1;
            _animActive = true;
            SuspendLayout();
            try { timeBeginPeriod(1); } catch { }
            _animClock.Restart();
            _animTimer.Start();
            AnimFrame();
        }

        void AnimFrame()
        {
            if (!_animActive) return;
            double t = _animClock.Elapsed.TotalMilliseconds;
            double p = _animDurMs > 0 ? t / _animDurMs : 1;
            if (p >= 1) { FinishLogAnim(_animToH, _animExpanding); return; }
            double e = _animExpanding ? CubicBezier(0.2, 0, 0, 1, p) : CubicBezier(0.4, 0, 1, 1, p);
            double hd = _animFromH + (_animToH - _animFromH) * e;
            if (p > 0.8 && Math.Abs(hd - _animToH) <= 2.0) { FinishLogAnim(_animToH, _animExpanding); return; }
            int h = (int)Math.Round(hd);
            // Segmented tabs fade with the panel: out over the first 60% of a
            // collapse, in over 20%-80% of an expand; seeded from the current
            // fade so a mid-flight reversal never jumps.
            double fp = _animExpanding ? (p - 0.2) / 0.6 : p / 0.6;
            if (fp < 0) fp = 0; if (fp > 1) fp = 1;
            double f = _animExpanding ? _animFadeFrom + (1.0 - _animFadeFrom) * fp
                                      : _animFadeFrom * (1.0 - fp);
            if (Math.Abs(f - _animLastFade) > 0.004)
            {
                _animLastFade = f;
                segLog.Fade = f;
                if (!_animExpanding && f <= 0.004 && segLog.Visible) segLog.Visible = false;
            }
            if (h != _animLastH)
            {
                _animLastH = h;
                ApplyAnimHeight(h);
            }
        }

        void ApplyAnimHeight(int h) { ApplyAnimHeight(h, false); }

        void ApplyAnimHeight(int h, bool landing)
        {
            int oldPanelH = logPanel.Height;
            logPanel.Height = h - (int)(442 * _uiScale);
            // v6.14: mid-flight stripe fix - when the panel grows, force
            // the newly uncovered strip (and the child controls inside it)
            // to repaint in this frame instead of trusting the bit-copy.
            // v6.17: the strip repaint moved to after the form SetWindowPos
            // below (see the RedrawWindow call there).
            // v6.5 flicker fix: animation frames no longer force a full
            // repaint (no SWP_NOCOPYBITS, no Invalidate(true)) - the native
            // log TextBoxes were erasing and redrawing their whole text on
            // every frame, which reads as strobe flicker while the panel
            // opens/closes. Plain bit-copy resize leaves the static content
            // untouched and only the newly exposed strip paints. The landing
            // frame still gets one full repaint so the settled image is exact.
            try { SetWindowPos(Handle, IntPtr.Zero, 0, 0, Width, h, 0x0002 | 0x0004 | 0x0010); } catch { }
            if (!landing && logPanel.Height > oldPanelH && logPanel.Visible)
            {
                // v6.17: panel.Update() (the v6.15 approach) repaints only the
                // panel itself; the child windows inside the newly exposed
                // strip (inset panel, native log TextBox, scrollbars) still
                // painted a frame late - the stripe on expand. RedrawWindow
                // with RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW makes
                // every child in the strip paint synchronously in this frame,
                // and running it after SetWindowPos means the clip geometry
                // is already final when they paint.
                try
                {
                    var strip = new Rectangle(0, oldPanelH, logPanel.Width, logPanel.Height - oldPanelH);
                    RedrawWindow(logPanel.Handle, ref strip, IntPtr.Zero, 0x0001 | 0x0080 | 0x0100);
                }
                catch { }
            }
            if (landing) { Invalidate(true); Update(); }
        }

        void FinishLogAnim(double toH, bool expanding)
        {
            bool wasActive = _animActive;
            _animActive = false;
            if (wasActive)
            {
                try { timeEndPeriod(1); } catch { }
                _animClock.Stop();
                _animTimer.Stop();
                ResumeLayout(false);
            }
            int h = (int)Math.Round(toH);
            ApplyAnimHeight(h, true);
            _goodW = Width; _goodH = h;
            segLog.Fade = expanding ? 1.0 : 0.0;
            segLog.Visible = expanding;
            segLog.Enabled = expanding;
            // v6.7: the stats tab swaps the log chrome for the stats panel -
            // restore whichever the current tab uses, not always the log set
            bool statsLand = segLog.SelectedIndex == 2;
            bool backupLand = segLog.SelectedIndex == 3;
            logInset.Visible = expanding && !statsLand && !backupLand;
            cmdLbl.Visible = expanding && !statsLand && !backupLand;
            cmdInset.Visible = expanding && !statsLand && !backupLand;
            btnCmdSend.Visible = expanding && !statsLand && !backupLand;
            if (statsPanel != null) statsPanel.Visible = expanding && statsLand;
            if (backupPanel != null) backupPanel.Visible = expanding && backupLand;
            if (searchRow != null) searchRow.Visible = expanding && segLog.SelectedIndex == 0;
            // v6.14: landing hardening (stripe reported 2026-10-28 on the
            // live session; five probe variants could not reproduce it, so
            // this is defense-in-depth, not a claimed root-cause fix):
            // rebuild the settled frame from current bounds and repaint the
            // whole log chrome recursively, whatever the mid-flight frames
            // under load may have skipped.
            if (expanding)
            {
                LayoutLogChrome(segLog.SelectedIndex);
                if (logInset != null) logInset.Invalidate(true);
            }
            Invalidate(true);
            Update();
        }

        LogTextBox MakeLogBox(string placeholder)
        {
            var tb = new LogTextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.ScrollBars = ScrollBars.None; // v6.6: native bars hidden; SlimScrollBar overlays drive it
            tb.WordWrap = false;
            tb.BackColor = Pal.Inset;
            tb.ForeColor = Pal.LogText;
            tb.BorderStyle = BorderStyle.None;
            tb.Font = Fonts.Mono(9f, FontStyle.Regular);
            tb.Placeholder = placeholder;
            tb.Scrolled += (s, e) => SyncLogBars();
            return tb;
        }

        // v6.6: keep the overlay bars + jump chip in sync with the visible log
        void SyncLogBars()
        {
            try
            {
                if (sbSrvV == null || tbServerLog == null) return;
                if (logInset != null && !logInset.Visible)
                {
                    // stats tab: the whole inset (boxes, bars, chip) is hidden
                    if (btnJumpBottom != null) btnJumpBottom.Visible = false;
                    return;
                }
                bool tun = tbTunnelLog != null && tbTunnelLog.Visible;
                if (tun) { sbSrvV.Visible = false; sbSrvH.Visible = false; sbTunV.Sync(); sbTunH.Sync(); }
                else { sbTunV.Visible = false; sbTunH.Visible = false; sbSrvV.Sync(); sbSrvH.Sync(); }
                if (btnJumpBottom != null)
                {
                    LogTextBox tb = tun ? tbTunnelLog : tbServerLog;
                    bool up = false;
                    if (tb != null && tb.IsHandleCreated)
                    {
                        int total = tb.LineCount(), vis = tb.VisibleLines(), first = tb.FirstVisible();
                        up = total > vis && first + vis < total - 1;
                    }
                    if (btnJumpBottom.Visible != up) btnJumpBottom.Visible = up;
                }
            }
            catch { }
        }

        void JumpToBottom()
        {
            LogTextBox tb = (tbTunnelLog != null && tbTunnelLog.Visible) ? tbTunnelLog : tbServerLog;
            if (tb == null) return;
            try { tb.SelectionStart = tb.TextLength; tb.ScrollToCaret(); } catch { }
            SyncLogBars();
        }

        void ToggleLog()
        {
            logCollapsed = !logCollapsed;
            logToggleLbl.Caption = logCollapsed ? Txt.ToggleLogStr : Txt.LogToggleLblCaption;
            bool expanding = !logCollapsed;
            if (expanding)
            {
                // Reveal the content up front: the growing window uncovers the
                // log box and command row from the bottom edge, and the tabs
                // fade in from the card color - nothing pops.
                // v6.7: on the stats tab the panel is what gets uncovered.
                bool statsPage = segLog.SelectedIndex == 2;
                bool backupPage = segLog.SelectedIndex == 3;
                logInset.Visible = !statsPage && !backupPage;
                cmdLbl.Visible = !statsPage && !backupPage;
                cmdInset.Visible = !statsPage && !backupPage;
                btnCmdSend.Visible = !statsPage && !backupPage;
                if (statsPanel != null) statsPanel.Visible = statsPage;
                if (backupPanel != null) backupPanel.Visible = backupPage;
                if (searchRow != null) searchRow.Visible = segLog.SelectedIndex == 0;
                segLog.Visible = true;
                if (!_animActive && _sysClientAnim) segLog.Fade = 0.0;
            }
            segLog.Enabled = false;
            StartLogAnim(expanding);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.F)
            {
                e.SuppressKeyPress = true;
                try
                {
                    if (segLog != null && segLog.SelectedIndex != 0) segLog.SelectPublic(0);
                    if (logCollapsed) ToggleLog();
                    if (tbSearch != null) { tbSearch.Focus(); tbSearch.SelectAll(); }
                }
                catch { }
                return;
            }
            if (e.Control && e.KeyCode == Keys.L) { ToggleLog(); e.SuppressKeyPress = true; }
        }

        // ---------- 工具 ----------
        void Ui(Action a)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) Invoke(a); else a(); } catch { }
        }

        int SendTb(TextBox tb, int msg, int wp, int lp)
        {
            try { return SendMessage(tb.Handle, msg, wp, lp).ToInt32(); } catch { return 0; }
        }

        // Append without yanking the view: follow the tail only when already at
        // the bottom; if the user scrolled up to read, keep their place.
        void AppendLogLine(TextBox tb, string s)
        {
            // v6.19: one repaint per appended line - the native EDIT
            // repaints on append, on scroll and on caret moves, which
            // strobes while the server log streams. WM_SETREDRAW off
            // batches the whole mutation; the matching re-enable and
            // Invalidate are at the end of this method.
            SendTb(tb, 0x000B, 0, 0);        // WM_SETREDRAW off
            int total = SendTb(tb, 0xBA, 0, 0);        // EM_GETLINECOUNT
            int first = SendTb(tb, 0xCE, 0, 0);        // EM_GETFIRSTVISIBLELINE
            int visible = Math.Max(1, tb.ClientSize.Height / Math.Max(1, tb.Font.Height));
            bool atBottom = total <= visible || first + visible >= total - 1;
            tb.AppendText(s + Environment.NewLine);
            total++;
            // v6.10: the 2000-line cap must run even when the user is scrolled
            // up reading history - it used to sit inside the atBottom branch,
            // so a long session read mid-game grew the box without bound
            // (soak: 6,301 lines / 607 KB retained). Rebuilding Lines resets
            // the view to the top, so afterwards scroll back to where the
            // reader was, shifted by the number of dropped lines.
            bool trimmed = false;
            if (total > 2100)
            {
                try
                {
                    string[] all = tb.Lines;
                    if (all.Length > 2000)
                    {
                        int drop = all.Length - 2000;
                        string[] keep = new string[2000];
                        Array.Copy(all, drop, keep, 0, 2000);
                        tb.Lines = keep;
                        trimmed = true;
                        if (!atBottom)
                        {
                            int target = Math.Max(0, first - drop);
                            if (target > 0) SendTb(tb, 0xB6, 0, target);   // EM_LINESCROLL down from the reset top
                        }
                    }
                }
                catch { }
            }
            if (atBottom)
            {
                tb.SelectionStart = tb.TextLength;
                tb.ScrollToCaret();
            }
            else if (!trimmed)
            {
                int first2 = SendTb(tb, 0xCE, 0, 0);
                int delta = first2 - first;
                if (delta != 0) SendTb(tb, 0xB6, 0, -delta);   // EM_LINESCROLL back
            }
            SendTb(tb, 0x000B, 1, 0);        // v6.19: WM_SETREDRAW on
            try { tb.Invalidate(); } catch { }
        }

        // v6.13: mirror both log panes to logs\launcher-yyyyMMdd.log so a
        // launcher crash/restart no longer loses the tunnel diagnostics.
        void PersistLog613(string tag, string s)
        {
            try
            {
                lock (_logLock613)
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    string path = Path.Combine(dir, "launcher-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                    File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss") + " [" + tag + "] " + s + "\r\n", Encoding.UTF8);
                }
            }
            catch { }
        }

        void CleanupOldLogs613()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(dir)) return;
                var cutoff = DateTime.Now.AddDays(-14);
                foreach (string f in Directory.GetFiles(dir, "launcher-*.log"))
                {
                    try { if (File.GetLastWriteTime(f) < cutoff) File.Delete(f); } catch { }
                }
            }
            catch { }
        }

        void AppendServerLog(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            PersistLog613(Txt.AppendServerLogPersistLog613, s);
            if (s.Length > _maxCharsSrv) _maxCharsSrv = s.Length;
            Ui(() =>
            {
                // v6.11: keep a bounded in-memory copy of the server log so
                // the error filter can rebuild the view at any time
                _srvLines.Add(s);
                while (_srvLines.Count > 2000) _srvLines.RemoveAt(0);
                if (_srvFilterErr && !IsErrLine(s)) return;
                AppendLogLine(tbServerLog, s);
            });
        }

        void AppendTunnelLog(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            PersistLog613(Txt.AppendTunnelLogPersistLog613, s);
            if (s.Length > _maxCharsTun) _maxCharsTun = s.Length;
            Ui(() => { AppendLogLine(tbTunnelLog, s); });
        }

        void AppendState(string s)
        {
            Ui(() => { if (statusCard != null) { statusCard.StateText = s; statusCard.Invalidate(); } });
        }

        // v6.11: the one place that writes to the server's stdin; the
        // command box, backups, schedules and player actions all go
        // through here.
        bool SendCmd(string cmd)
        {
            if (!ProcAlive(_server)) return false;
            try { _server.StandardInput.WriteLine(cmd); _server.StandardInput.Flush(); return true; }
            catch { return false; }
        }

        void SendServerCommand()
        {
            if (tbCmd == null) return;
            string cmd = tbCmd.Text.Trim();
            if (cmd.Length == 0) return;
            if (!ProcAlive(_server)) { AppendState(Txt.SendServerCommandAppendState); return; }
            try
            {
                if (!SendCmd(cmd)) { AppendState(Txt.SendServerCommandAppendState2); return; }
                AppendServerLog("> " + cmd);
                RecordCmd(cmd);
                tbCmd.Clear();
                tbCmd.Focus();
            }
            catch (Exception ex) { AppendState(Txt.SendServerCommandAppendState3 + ex.Message); }
        }

        // ---------- spark 性能诊断（v6.21） ----------
        // 命令走与指令框相同的 SendCmd 通道（控制台 stdin，无斜杠）；
        // spark 的输出（含报告链接）由服务端控制台流回日志区，不另做查看器。
        void SparkTps()
        {
            if (!ProcAlive(_server)) { AppendState(Txt.SendServerCommandAppendState); return; }
            if (SendCmd("spark tps")) AppendServerLog("> spark tps");
            else AppendState(Txt.SendServerCommandAppendState2);
        }

        void SparkProfileStart()
        {
            if (_profiling) return;
            if (!ProcAlive(_server)) { AppendState(Txt.SendServerCommandAppendState); return; }
            if (!SendCmd("spark profiler start")) { AppendState(Txt.SendServerCommandAppendState2); return; }
            AppendServerLog("> spark profiler start");
            AppendServerLog(Txt.SparkProfileStarted);
            _profiling = true;
            if (btnSparkProfile != null) btnSparkProfile.Enabled = false;
            if (_sparkTimer == null)
            {
                _sparkTimer = new System.Windows.Forms.Timer();
                _sparkTimer.Interval = 30000;
                _sparkTimer.Tick += (s, e) => SparkProfileStop();
            }
            _sparkTimer.Stop();
            _sparkTimer.Start();
        }

        // 计时器到期（UI 线程）或关服取消后走到这里：复位并按需发送 stop
        void SparkProfileStop()
        {
            if (_sparkTimer != null) _sparkTimer.Stop();
            if (!_profiling) return;
            _profiling = false;
            if (btnSparkProfile != null) btnSparkProfile.Enabled = ProcAlive(_server);
            if (!ProcAlive(_server)) { AppendServerLog(Txt.SparkProfileCancelled); return; }
            if (SendCmd("spark profiler stop"))
            {
                AppendServerLog("> spark profiler stop");
                AppendServerLog(Txt.SparkProfileStopped);
            }
            else AppendState(Txt.SendServerCommandAppendState2);
        }

        // 服务端在采样途中退出时由 Exited 处理调用：停表、复位并记一行取消日志
        void CancelSparkProfile()
        {
            if (!_profiling) return;
            if (_sparkTimer != null) _sparkTimer.Stop();
            _profiling = false;
            AppendServerLog(Txt.SparkProfileCancelled);
        }

        // ---------- 玩家管理入口（v6.11） ----------
        System.Collections.Generic.List<string> ReadNameList(string file)
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                string p = Path.Combine(_cfg.ServerDir, file);
                if (File.Exists(p))
                {
                    string txt = File.ReadAllText(p, Encoding.UTF8);
                    foreach (Match m in Regex.Matches(txt, "\"name\"\\s*:\\s*\"([^\"]+)\""))
                        if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
                }
            }
            catch { }
            return list;
        }
        void OpenPlayerManager()
        {
            if (!ProcAlive(_server)) { InfoBox.Show(Txt.OpenPlayerManagerShow, Txt.MbText); return; }
            var online = new System.Collections.Generic.List<string>();
            var slp = _slp;
            if (slp != null && slp.Ok && slp.Names.Count > 0) online.AddRange(slp.Names);
            else online.AddRange(_onlineNames);
            string me = null;
            foreach (var n in online) { if (n.StartsWith("oihg")) { me = n; break; } }
            var ops = ReadNameList("ops.json");
            var banned = ReadNameList("banned-players.json");
            using (var f = new PlayerForm(online, ops, banned, me, cmd =>
            {
                if (SendCmd(cmd)) AppendServerLog("> " + cmd);
                else AppendState(Txt.OpenPlayerManagerAppendState);
            }))
                f.ShowDialog(this);
        }

        // ---------- 世界备份（v6.11） ----------
        // The pattern the mature panels use: never copy a world the server
        // is actively writing. When the server is up we first ask it to
        // flush and pause saving (save-off / save-all flush), copy, then
        // always save-on again in a finally.
        string LevelName()
        {
            try
            {
                string pf = Path.Combine(_cfg.ServerDir, "server.properties");
                if (File.Exists(pf))
                {
                    foreach (var line in File.ReadAllLines(pf))
                    {
                        var t = line.Trim();
                        if (t.StartsWith("level-name=")) { string v = t.Substring("level-name=".Length).Trim(); if (v.Length > 0) return v; }
                    }
                }
            }
            catch { }
            return "world";
        }
        string WorldPath() { return Path.Combine(_cfg.ServerDir, LevelName()); }
        string BackupDirPath() { return Path.Combine(_cfg.ServerDir, "backups"); }

        private volatile bool _backupBusy = false;
        public bool BackupBusy { get { return _backupBusy; } }

        void DoBackup(string reason) { DoBackup(reason, null); }

        // Runs the zip on a worker thread; doneMsg (nullable) runs on the UI
        // thread afterwards. Synchronous core is BackupCore so the
        // start-of-server path can run it inline on its own thread.
        void DoBackup(string reason, Action doneUi)
        {
            if (_backupBusy) { AppendState(Txt.DoBackupAppendState); return; }
            _backupBusy = true;
            SetBackupUi();
            var th = new Thread(() => { BackupCore(reason); Ui(() => { _backupBusy = false; SetBackupUi(); RefreshBackupList(); if (doneUi != null) doneUi(); }); });
            th.IsBackground = true;
            th.Start();
        }

        void BackupCore(string reason)
        {
            string world = WorldPath();
            bool running = ProcAlive(_server) && _serverDone;
            try
            {
                if (!Directory.Exists(world)) { Ui(() => AppendState(Txt.BackupCoreAppendState + world + Txt.BackupCoreAppendState2)); return; }
                string bdir = BackupDirPath();
                Directory.CreateDirectory(bdir);
                if (running)
                {
                    SendCmd("save-off");
                    SendCmd("save-all flush");
                    Ui(() => AppendServerLog(Txt.BackupCoreAppendServerLog));
                    Thread.Sleep(2000);
                }
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string zip = Path.Combine(bdir, "world_" + stamp + ".zip");
                int files = 0, skipped = 0;
                long bytes = 0;
                using (var fs = new FileStream(zip, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var za = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
                {
                    foreach (var f in Directory.GetFiles(world, "*", SearchOption.AllDirectories))
                    {
                        string rel = f.Substring(world.Length).TrimStart('\\', '/').Replace('\\', '/');
                        try
                        {
                            var entry = za.CreateEntry(rel, System.IO.Compression.CompressionLevel.Optimal);
                            using (var es = entry.Open())
                            using (var src = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            {
                                src.CopyTo(es);
                                bytes += src.Length;
                            }
                            files++;
                        }
                        catch { skipped++; }
                    }
                }
                Ui(() => AppendServerLog(Txt.BackupCoreAppendServerLog2 + Path.GetFileName(zip) + "（" + files + Txt.BackupCorePart + (skipped > 0 ? Txt.BackupCoreStr + skipped + Txt.BackupCorePart2 : "") + "）"));
                Ui(() => AppendState(Txt.BackupCoreAppendState3 + reason + "）。"));
                PruneBackups();
            }
            catch (Exception ex)
            {
                Ui(() => { AppendServerLog(Txt.BackupCoreAppendServerLog3 + ex.Message); AppendState(Txt.BackupCoreAppendState4 + ex.Message); });
            }
            finally
            {
                if (running) { SendCmd("save-on"); Ui(() => AppendServerLog(Txt.BackupCoreAppendServerLog4)); }
            }
        }

        void PruneBackups()
        {
            try
            {
                var zips = new DirectoryInfo(BackupDirPath()).GetFiles("world_*.zip");
                Array.Sort(zips, (a, b) => string.Compare(b.Name, a.Name, StringComparison.Ordinal));
                for (int i = _cfg.BackupKeep; i < zips.Length; i++) { try { zips[i].Delete(); } catch { } }
            }
            catch { }
        }

        void RestoreBackup(string zipPath)
        {
            if (ProcAlive(_server)) { InfoBox.Show(Txt.RestoreBackupShow, Txt.RestoreBackupShow2); return; }
            string name = Path.GetFileName(zipPath);
            bool yes = false;
            using (var c = new ConfirmBox(Txt.RestoreBackupConfirmBox + name + Txt.RestoreBackupConfirmBox2))
            {
                var r = c.ShowDialog(this);
                yes = (r == DialogResult.OK && c.ChoiceYes);
            }
            if (!yes) return;
            if (_backupBusy) { AppendState(Txt.RestoreBackupAppendState); return; }
            _backupBusy = true; SetBackupUi();
            var th = new Thread(() =>
            {
                string world = WorldPath();
                string result;
                try
                {
                    string aside = world + "_prerestore_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    if (Directory.Exists(world)) Directory.Move(world, aside);
                    Directory.CreateDirectory(world);
                    using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var za = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read))
                    {
                        foreach (var entry in za.Entries)
                        {
                            if (entry.FullName.EndsWith("/")) continue;
                            string rel = entry.FullName.Replace('/', '\\');
                            string fullWorld = Path.GetFullPath(world);
                            string dest = Path.GetFullPath(Path.Combine(fullWorld, rel));
                            if (!dest.StartsWith(fullWorld + "\\", StringComparison.OrdinalIgnoreCase)) continue;
                            Directory.CreateDirectory(Path.GetDirectoryName(dest));
                            using (var es = entry.Open())
                            using (var ds = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None))
                                es.CopyTo(ds);
                        }
                    }
                    result = Txt.Result + name + Txt.RestoreBackupPart + Path.GetFileName(aside) + Txt.RestoreBackupPart2;
                }
                catch (Exception ex) { result = Txt.Result2 + ex.Message + Txt.RestoreBackupPart3; }
                Ui(() => { _backupBusy = false; SetBackupUi(); RefreshBackupList(); InfoBox.Show(result, Txt.RestoreBackupShow2); AppendServerLog(Txt.RestoreBackupAppendServerLog + result); });
            });
            th.IsBackground = true;
            th.Start();
        }

        // ---------- 备份页签 UI（v6.11） ----------
        TextBox BkBox(int x, int y, int w)
        {
            var t = new TextBox();
            t.SetBounds(x, y, w, 24);
            t.BackColor = Color.FromArgb(17, 20, 24);
            t.ForeColor = Pal.Text;
            t.BorderStyle = BorderStyle.None;
            t.Font = new Font("Microsoft YaHei UI", 9f);
            return t;
        }
        DarkCheckBox BkCheck(int x, int y, int w, string text)
        {
            var c = new DarkCheckBox();
            c.Text = text;
            c.SetBounds(x, y, w, 24);
            c.ForeColor = Pal.Text;
            c.BackColor = Pal.Inset;
            c.Font = new Font("Microsoft YaHei UI", 9f);
            return c;
        }
        TextLabel BkLabel(int x, int y, int w, string text, Color col)
        {
            var l = new TextLabel();
            l.Caption = text;
            l.SetBounds(x, y, w, 24);
            l.ForeColor = col;
            l.BackColor = Pal.Inset;
            l.Font = new Font("Microsoft YaHei UI", 9f);
            return l;
        }

        void BuildBackupPanel()
        {
            _bkInit = true;
            lblBackupHead = BkLabel(16, 10, 220, Txt.BuildBackupPanelBkLabel, Pal.Text);
            lblBackupHead.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            backupPanel.Controls.Add(lblBackupHead);
            btnBackupNow = new RoundButton();
            btnBackupNow.Text = Txt.BtnBackupNowText;
            btnBackupNow.SetBounds(356, 8, 116, 28);
            btnBackupNow.BackColor = Pal.Surface; btnBackupNow.ForeColor = Pal.Text; btnBackupNow.BackdropColor = Pal.Inset;
            btnBackupNow.Font = new Font("Microsoft YaHei UI", 9f);
            btnBackupNow.Click += (s, e) => DoBackup(Txt.BuildBackupPanelDoBackup);
            backupPanel.Controls.Add(btnBackupNow);
            lblBackupState = BkLabel(16, 38, 456, "—", Pal.Sub);
            backupPanel.Controls.Add(lblBackupState);
            backupList = new ListBox();
            backupList.SetBounds(16, 62, 456, 74);
            backupList.BackColor = Color.FromArgb(17, 20, 24);
            backupList.ForeColor = Pal.Text;
            backupList.BorderStyle = BorderStyle.None;
            backupList.Font = new Font("Microsoft YaHei UI", 9f);
            backupList.IntegralHeight = false;
            ThemedList.Attach(backupList);
            backupList.SelectedIndexChanged += (s, e) => SetBackupUi();
            backupList.DoubleClick += (s, e) => { int i = backupList.SelectedIndex; if (i >= 0 && i < _backupFiles.Count) RestoreBackup(_backupFiles[i]); };
            backupPanel.Controls.Add(backupList);
            btnRestoreBackup = new RoundButton();
            btnRestoreBackup.Text = Txt.BtnRestoreBackupText;
            btnRestoreBackup.SetBounds(16, 140, 104, 28);
            btnRestoreBackup.BackColor = Pal.Surface; btnRestoreBackup.ForeColor = Pal.Text; btnRestoreBackup.BackdropColor = Pal.Inset;
            btnRestoreBackup.Font = new Font("Microsoft YaHei UI", 9f);
            btnRestoreBackup.Click += (s, e) => { int i = backupList.SelectedIndex; if (i >= 0 && i < _backupFiles.Count) RestoreBackup(_backupFiles[i]); else InfoBox.Show(Txt.BuildBackupPanelShow, Txt.RestoreBackupShow2); };
            backupPanel.Controls.Add(btnRestoreBackup);
            btnDeleteBackup = new RoundButton();
            btnDeleteBackup.Text = Txt.BtnDeleteBackupText;
            btnDeleteBackup.SetBounds(128, 140, 104, 28);
            btnDeleteBackup.BackColor = Pal.Surface; btnDeleteBackup.ForeColor = Pal.Text; btnDeleteBackup.BackdropColor = Pal.Inset;
            btnDeleteBackup.Font = new Font("Microsoft YaHei UI", 9f);
            btnDeleteBackup.Click += (s, e) =>
            {
                int i = backupList.SelectedIndex;
                if (i < 0 || i >= _backupFiles.Count) { InfoBox.Show(Txt.BuildBackupPanelShow, Txt.BuildBackupPanelShow2); return; }
                bool yes = false;
                using (var c = new ConfirmBox(Txt.BuildBackupPanelConfirmBox + Path.GetFileName(_backupFiles[i]) + Txt.BuildBackupPanelPart))
                { var r = c.ShowDialog(this); yes = (r == DialogResult.OK && c.ChoiceYes); }
                if (!yes) return;
                try { File.Delete(_backupFiles[i]); } catch (Exception ex) { InfoBox.Show(Txt.BuildBackupPanelShow3 + ex.Message, Txt.BuildBackupPanelShow2); }
                RefreshBackupList();
            };
            backupPanel.Controls.Add(btnDeleteBackup);
            lblBackupInfo = BkLabel(244, 142, 228, "", Pal.Sub);
            backupPanel.Controls.Add(lblBackupInfo);
            lblBkKeepPrefix = BkLabel(16, 170, 62, Txt.BuildBackupPanelBkLabel2, Pal.Sub);
            backupPanel.Controls.Add(lblBkKeepPrefix);
            tbKeep = BkBox(142, 170, 44); tbKeep.Text = _cfg.BackupKeep.ToString(); tbKeep.TextAlign = HorizontalAlignment.Center;
            tbKeep.Leave += (s, e) => { int v; if (int.TryParse(tbKeep.Text.Trim(), out v) && v >= 1 && v <= 99) { _cfg.BackupKeep = v; _cfg.Save(); PruneBackups(); RefreshBackupList(); } tbKeep.Text = _cfg.BackupKeep.ToString(); };
            backupPanel.Controls.Add(tbKeep);
            lblBkKeepSuffix = BkLabel(192, 170, 20, Txt.BuildBackupPanelBkLabel3, Pal.Sub);
            backupPanel.Controls.Add(lblBkKeepSuffix);
            chkAutoRestart = BkCheck(218, 170, 128, Txt.BuildBackupPanelBkCheck);
            chkAutoRestart.Checked = _cfg.AutoRestart;
            chkAutoRestart.CheckedChanged += (s, e) => { if (_bkInit) return; _cfg.AutoRestart = chkAutoRestart.Checked; _cfg.Save(); };
            backupPanel.Controls.Add(chkAutoRestart);
            chkBackupOnStart = BkCheck(352, 196, 120, Txt.BuildBackupPanelBkCheck2);
            chkBackupOnStart.Checked = _cfg.BackupOnStart;
            chkBackupOnStart.CheckedChanged += (s, e) => { if (_bkInit) return; _cfg.BackupOnStart = chkBackupOnStart.Checked; _cfg.Save(); };
            backupPanel.Controls.Add(chkBackupOnStart);
            chkDailyBackup = BkCheck(16, 222, 118, Txt.BuildBackupPanelBkCheck3);
            chkDailyBackup.Checked = _cfg.DailyBackup;
            chkDailyBackup.CheckedChanged += (s, e) => { if (_bkInit) return; _cfg.DailyBackup = chkDailyBackup.Checked; _cfg.Save(); };
            backupPanel.Controls.Add(chkDailyBackup);
            tbDailyTime = BkBox(142, 222, 56); tbDailyTime.Text = _cfg.DailyBackupTime; tbDailyTime.TextAlign = HorizontalAlignment.Center;
            tbDailyTime.Leave += (s, e) => { TimeSpan ts; if (TimeSpan.TryParse(tbDailyTime.Text.Trim(), out ts)) { _cfg.DailyBackupTime = tbDailyTime.Text.Trim(); _cfg.Save(); } tbDailyTime.Text = _cfg.DailyBackupTime; };
            backupPanel.Controls.Add(tbDailyTime);
            // v6.22.4: 整句提示改为整行换行（宽度 456、8pt、两行高度），不再挤在 266px 单行里
            lblBkHint = BkLabel(16, 274, 456, Txt.BuildBackupPanelBkLabel4, Pal.Sub);
            lblBkHint.Font = new Font("Microsoft YaHei UI", 8f);
            lblBkHint.WordWrap = true;
            backupPanel.Controls.Add(lblBkHint); // v6.14: meaningful text
            chkTimedCmd = BkCheck(16, 248, 118, Txt.BuildBackupPanelBkCheck4);
            chkTimedCmd.Checked = _cfg.TimedCmdEnabled;
            chkTimedCmd.CheckedChanged += (s, e) => { if (_bkInit) return; _cfg.TimedCmdEnabled = chkTimedCmd.Checked; _cfg.Save(); };
            backupPanel.Controls.Add(chkTimedCmd);
            tbTimedTime = BkBox(142, 248, 56); tbTimedTime.Text = _cfg.TimedCmdTime; tbTimedTime.TextAlign = HorizontalAlignment.Center;
            tbTimedTime.Leave += (s, e) => { TimeSpan ts; if (TimeSpan.TryParse(tbTimedTime.Text.Trim(), out ts)) { _cfg.TimedCmdTime = tbTimedTime.Text.Trim(); _cfg.Save(); } tbTimedTime.Text = _cfg.TimedCmdTime; };
            backupPanel.Controls.Add(tbTimedTime);
            tbTimedCmd = BkBox(206, 248, 266); tbTimedCmd.Text = _cfg.TimedCmdText;
            tbTimedCmd.Leave += (s, e) => { if (_bkInit) return; _cfg.TimedCmdText = tbTimedCmd.Text; _cfg.Save(); };
            backupPanel.Controls.Add(tbTimedCmd);
            _bkInit = false;
            RefreshBackupList();
        }

        void SetBackupUi()
        {
            if (btnBackupNow == null) return;
            bool en = !_backupBusy;
            btnBackupNow.Enabled = en;
            if (btnRestoreBackup != null) btnRestoreBackup.Enabled = en;
            if (btnDeleteBackup != null) btnDeleteBackup.Enabled = en;
            if (lblBackupState != null && _backupBusy) lblBackupState.Caption = Txt.LblBackupStateCaption;
        }

        void RefreshBackupList()
        {
            if (backupList == null) return;
            _backupFiles.Clear();
            backupList.Items.Clear();
            long total = 0;
            try
            {
                string bdir = BackupDirPath();
                if (Directory.Exists(bdir))
                {
                    var zips = new DirectoryInfo(bdir).GetFiles("world_*.zip");
                    Array.Sort(zips, (a, b) => string.Compare(b.Name, a.Name, StringComparison.Ordinal));
                    foreach (var f in zips)
                    {
                        _backupFiles.Add(f.FullName);
                        total += f.Length;
                        string when = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                        var m = Regex.Match(f.Name, @"world_(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})\.zip");
                        if (m.Success) when = m.Groups[1].Value + "-" + m.Groups[2].Value + "-" + m.Groups[3].Value + " " + m.Groups[4].Value + ":" + m.Groups[5].Value;
                        backupList.Items.Add(when + " · " + FmtBytes(f.Length));
                    }
                }
            }
            catch { }
            if (lblBackupInfo != null) lblBackupInfo.Caption = _backupFiles.Count > 0 ? (Txt.RefreshBackupListStr + _backupFiles.Count + Txt.RefreshBackupListPart + FmtBytes(total)) : Txt.RefreshBackupListStr2;
            if (lblBackupState != null && !_backupBusy)
                lblBackupState.Caption = _backupFiles.Count > 0 ? Txt.RefreshBackupListStr3 + backupList.Items[0].ToString() : Txt.RefreshBackupListStr4;
            SetBackupUi();
        }

        // ---------- 计划任务（v6.11 简化版） ----------
        static bool TimeReached(string hhmm)
        {
            TimeSpan t;
            if (!TimeSpan.TryParse(hhmm, out t)) return false;
            return DateTime.Now.TimeOfDay >= t;
        }
        void CheckSchedules()
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (_cfg.DailyBackup && _cfg.LastDailyBackup != today && TimeReached(_cfg.DailyBackupTime) && !_backupBusy)
                {
                    _cfg.LastDailyBackup = today; _cfg.Save();
                    AppendServerLog(Txt.CheckSchedulesAppendServerLog);
                    DoBackup(Txt.BuildBackupPanelBkCheck3);
                }
                if (_cfg.TimedCmdEnabled && _cfg.LastTimedCmd != today && TimeReached(_cfg.TimedCmdTime)
                    && ProcAlive(_server) && _serverDone && !string.IsNullOrEmpty(_cfg.TimedCmdText))
                {
                    string c = _cfg.TimedCmdText.Trim();
                    // v6.14: a stop/restart command waits until nobody is
                    // online instead of kicking players mid-session
                    bool destructive = c.StartsWith("stop") || c.StartsWith("restart");
                    if (destructive && _onlineNames != null && _onlineNames.Count > 0)
                    {
                        if (!_timedDeferred614)
                        {
                            _timedDeferred614 = true;
                            AppendServerLog(Txt.CheckSchedulesAppendServerLog2 + _onlineNames.Count + Txt.CheckSchedulesAppendServerLog3);
                        }
                    }
                    else
                    {
                        _timedDeferred614 = false;
                        _cfg.LastTimedCmd = today; _cfg.Save();
                        if (c.Length > 0 && SendCmd(c)) AppendServerLog(Txt.CheckSchedulesAppendServerLog4 + c);
                    }
                }
            }
            catch { }
        }

        // ---------- 指令历史（v6.6） ----------
        void CmdKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SendServerCommand(); return; }
            if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                if (_cmdHist.Count == 0) return;
                if (_histIdx < 0) { _histDraft = tbCmd.Text; _histIdx = _cmdHist.Count - 1; }
                else if (_histIdx > 0) _histIdx--;
                tbCmd.Text = _cmdHist[_histIdx];
                tbCmd.SelectionStart = tbCmd.TextLength;
                return;
            }
            if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                if (_histIdx < 0) return;
                _histIdx++;
                if (_histIdx >= _cmdHist.Count) { _histIdx = -1; tbCmd.Text = _histDraft; }
                else tbCmd.Text = _cmdHist[_histIdx];
                tbCmd.SelectionStart = tbCmd.TextLength;
            }
        }

        string CmdHistPath() { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cmdhistory.txt"); }

        void LoadCmdHistory()
        {
            try
            {
                if (!File.Exists(CmdHistPath())) return;
                foreach (string line in File.ReadAllLines(CmdHistPath(), System.Text.Encoding.UTF8))
                {
                    string t = line.Trim();
                    if (t.Length > 0 && (_cmdHist.Count == 0 || _cmdHist[_cmdHist.Count - 1] != t)) _cmdHist.Add(t);
                }
                while (_cmdHist.Count > 50) _cmdHist.RemoveAt(0);
            }
            catch { }
        }

        void RecordCmd(string cmd)
        {
            if (_cmdHist.Count == 0 || _cmdHist[_cmdHist.Count - 1] != cmd) _cmdHist.Add(cmd);
            while (_cmdHist.Count > 50) _cmdHist.RemoveAt(0);
            _histIdx = -1; _histDraft = "";
            try { File.WriteAllLines(CmdHistPath(), _cmdHist.ToArray(), System.Text.Encoding.UTF8); } catch { }
        }

        void RefreshStatus()
        {
            bool sRun = ProcAlive(_server);
            bool sDone = sRun && _serverDone;
            // 服务端灯
            if (statusCard != null)
            {
                if (sRun && sDone) { statusCard.ServerText = Txt.ServerText2; statusCard.ServerColor = Color.FromArgb(64, 199, 116); }
                else if (sRun) { statusCard.ServerText = Txt.ServerText3; statusCard.ServerColor = Color.FromArgb(250, 168, 26); }
                else { statusCard.ServerText = Txt.ServerText; statusCard.ServerColor = Color.FromArgb(94, 103, 115); }
            }

            bool tRun = ProcAlive(_frpc) || ProcAlive(_frpcVoice);
            if (statusCard != null)
            {
                if (tRun && _tunnelUp) { statusCard.TunnelText = Txt.TunnelText2; statusCard.TunnelColor = Color.FromArgb(64, 199, 116); }
                else if (tRun) { statusCard.TunnelText = Txt.TunnelText3; statusCard.TunnelColor = Color.FromArgb(250, 168, 26); }
                else { statusCard.TunnelText = Txt.TunnelText; statusCard.TunnelColor = Color.FromArgb(94, 103, 115); }
            }

            bool gameUp = ProcAlive(_frpc) && _tunnelUp;
            if (statusCard != null)
            {
                if (string.IsNullOrEmpty(_cfg.PublicAddr)) { statusCard.AddrText = Txt.AddrText; statusCard.AddrColor = Color.FromArgb(94, 103, 115); }
                else if (gameUp) { statusCard.AddrText = Txt.AddrText2; statusCard.AddrColor = Color.FromArgb(64, 199, 116); }
                else { statusCard.AddrText = Txt.AddrText3; statusCard.AddrColor = Color.FromArgb(154, 165, 177); }
                statusCard.SetPulse(0, sRun && !sDone);
                statusCard.SetPulse(1, tRun && !_tunnelUp);
                statusCard.Invalidate();
            }

            if (_tray != null) { string tt = Txt.Tt + (sDone ? Txt.RefreshStatusStr : ((sRun || tRun) ? Txt.RefreshStatusStr2 : Txt.FailWizStep)); if (_tray.Text != tt) _tray.Text = tt; } // v6.13
            string pa = _cfg.PublicAddr ?? "";
            if (tbAddr.Text != pa) tbAddr.Text = pa; // v6.6: don't clobber a selection on every refresh
            btnCopy.Enabled = !string.IsNullOrEmpty(_cfg.PublicAddr);
            btnStop.Enabled = sRun || tRun;
            if (tbCmd != null) { tbCmd.Enabled = sRun; btnCmdSend.Enabled = sRun; }
            if (btnSparkTps != null) { btnSparkTps.Enabled = sRun; btnSparkProfile.Enabled = sRun && !_profiling; }
        }

        void CopyAddr()
        {
            if (string.IsNullOrEmpty(_cfg.PublicAddr)) { InfoBox.Show(Txt.CopyAddrShow, Txt.CopyAddrShow2); return; }
            try
            {
                Clipboard.SetText(_cfg.PublicAddr);
                AppendState(Txt.CopyAddrAppendState);
                // v6.9: the button itself confirms for a moment - the state
                // line alone was easy to miss
                btnCopy.Text = Txt.BtnCopyText2;
                if (_copyTimer == null)
                {
                    _copyTimer = new System.Windows.Forms.Timer();
                    _copyTimer.Interval = 1200;
                    _copyTimer.Tick += (s, e) => { _copyTimer.Stop(); btnCopy.Text = Txt.BtnCopyText; };
                }
                _copyTimer.Stop();
                _copyTimer.Start();
            }
            catch { }
        }

        void OpenPcl2()
        {
            if (string.IsNullOrEmpty(_cfg.Pcl2Path) || !File.Exists(_cfg.Pcl2Path)) { InfoBox.Show(Txt.OpenPcl2Show, Txt.CopyAddrShow2); OpenSettings(); return; }
            try { Process.Start(new ProcessStartInfo(_cfg.Pcl2Path) { WorkingDirectory = Path.GetDirectoryName(_cfg.Pcl2Path) }); }
            catch (Exception ex) { InfoBox.Show(Txt.OpenPcl2Show2 + ex.Message, Txt.OnHandleCreatedIndexOf); }
        }

        void OpenSettings()
        {
            bool restart = false;
            using (var f = new SettingsForm(_cfg) { ServerRunning = ProcAlive(_server) })
            {
                f.ShowDialog(this);
                restart = f.RestartRequested;
            }
            RefreshStatus();
            if (restart && !ProcAlive(_server) && !ProcAlive(_frpc) && !ProcAlive(_frpcVoice))
            {
                try { Application.Restart(); } catch { }
            }
        }

        // v6.11: run an action once after a delay, on the UI thread
        void Delayed(int ms, Action a)
        {
            var t = new System.Windows.Forms.Timer();
            t.Interval = ms;
            t.Tick += (s2, e2) => { t.Stop(); t.Dispose(); try { a(); } catch { } };
            t.Start();
        }

        // v6.11 crash watchdog: the server reached Done and then died
        // without anyone asking it to stop. Report it plainly, and (when
        // enabled) restart it - but never forever: 3 crashes inside 10
        // minutes means something is broken, not unlucky.
        void HandleServerCrash(int exitCode)
        {
            var now = DateTime.Now;
            _crashTimes.RemoveAll(t => (now - t).TotalMinutes > 10);
            _crashTimes.Add(now);
            bool again = _cfg.AutoRestart && _crashTimes.Count <= 3;
            string guess614 = GuessCrash614();
            AppendServerLog(Txt.HandleServerCrashAppendServerLog + exitCode + Txt.HandleServerCrashAppendServerLog2 + (guess614.Length > 0 ? Txt.HandleServerCrashStr + guess614 + "。" : ""));
            if (!_cfg.AutoRestart)
            {
                AppendState(Txt.HandleServerCrashAppendState + (guess614.Length > 0 ? Txt.HandleServerCrashStr2 + guess614 + "。" : Txt.HandleServerCrashStr3) + Txt.HandleServerCrashPart);
                return;
            }
            if (!again)
            {
                AppendServerLog(Txt.HandleServerCrashAppendServerLog3 + _crashTimes.Count + Txt.HandleServerCrashAppendServerLog4);
                AppendState(Txt.HandleServerCrashAppendState2 + (guess614.Length > 0 ? Txt.HandleServerCrashStr4 + guess614 + "。" : ""));
                return;
            }
            AppendState(Txt.HandleServerCrashAppendState3);
            Delayed(5000, () => { if (!ProcAlive(_server) && !_stoppingIntentional) StartAll(); });
        }

        // v6.14: guess the crash cause from the recent log tail, in
        // plain words (borrowed from MC Server Manager's crash diagnosis)
        string GuessCrash614()
        {
            try
            {
                int n = _srvLines.Count;
                var tail = new System.Text.StringBuilder();
                for (int i = Math.Max(0, n - 40); i < n; i++) tail.Append(_srvLines[i]).Append('\n');
                string t = tail.ToString();
                if (t.Contains("Failed to bind to port") || t.Contains("Address already in use")) return Txt.GuessCrash614Ret + _cfg.Port + Txt.GuessCrash614Part;
                if (t.Contains("OutOfMemoryError") || t.Contains("Could not create the Java Virtual Machine") || t.Contains("insufficient memory")) return Txt.GuessCrash614Ret2;
                if (t.Contains("UnsupportedClassVersionError")) return Txt.GuessCrash614Ret3;
                if (t.Contains("single server tick")) return Txt.GuessCrash614Ret4;
                if (t.Contains("ModLoadingException") || t.Contains("Failed to load mod") || t.Contains("requires fabric") || t.Contains("Missing or unsupported mandatory dependencies")) return Txt.GuessCrash614Ret5;
                if (t.Contains("Failed to load eula")) return Txt.GuessCrash614Ret6;
            }
            catch { }
            return "";
        }

        // ---------- 端口检查 ----------
        bool PortInUse(int port)
        {
            try
            {
                var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                l.Start();
                l.Stop();
                return false;
            }
            catch { return true; }
        }

        // ---------- 一键启动 ----------
        // ---------- v6.20: 首次设置向导入口 ----------
        bool _wizardOffered = false;

        void OfferWizardIfNeeded()
        {
            if (_wizardOffered) return;
            _wizardOffered = true;
            bool need = !Directory.Exists(_cfg.ServerDir) || !File.Exists(Path.Combine(_cfg.ServerDir, _cfg.JarName));
            if (!need) return;
            var r = InfoBox.Show(Txt.OfferWizardIfNeededShow, Txt.WizardFormInitChrome, Txt.OfferWizardIfNeededShow2, Txt.OfferWizardIfNeededShow3);
            if (r == DialogResult.OK) OpenWizard();
        }

        void OpenWizard()
        {
            using (var wf = new WizardForm(_cfg, _cfg.ServerDir))
            {
                if (wf.ShowDialog(this) == DialogResult.OK)
                {
                    AppendState(Txt.OpenWizardAppendState);
                    RefreshStatus();
                }
            }
        }

        // ---------- v6.20: 启动前 Java 版本校验 ----------
        // Returns false only when the user cancels at the mismatch dialog.
        // Every "unknown" outcome logs its reason and lets the start
        // proceed - the check must never become a new way to break a
        // working setup, and it must never crash on a bad jar.
        bool CheckJavaVersion(string jarPath)
        {
            string mcVer = McJava.ReadMcVersion(jarPath);
            int required = mcVer != null ? McJava.RequiredJava(mcVer) : 0;
            string javaExe = McJava.ResolveJavaExe(_cfg);
            int have = javaExe != null ? McJava.GetJavaMajor(javaExe) : 0;
            string haveText = have > 0 ? have.ToString() : Txt.CheckJavaVersionStr;
            if (mcVer == null)
            {
                AppendServerLog(Txt.CheckJavaVersionAppendServerLog + haveText + Txt.CheckJavaVersionAppendServerLog2);
                return true;
            }
            if (required <= 0)
            {
                AppendServerLog(Txt.CheckJavaVersionAppendServerLog3 + mcVer + Txt.CheckJavaVersionAppendServerLog4 + haveText + Txt.CheckJavaVersionAppendServerLog2);
                return true;
            }
            if (have <= 0)
            {
                AppendServerLog(Txt.CheckJavaVersionAppendServerLog5 + mcVer + Txt.RunFail3 + required + Txt.CheckJavaVersionAppendServerLog6);
                return true;
            }
            if (have >= required)
            {
                AppendServerLog(Txt.CheckJavaVersionAppendServerLog7 + mcVer + Txt.RunFail3 + required + Txt.CheckJavaVersionAppendServerLog8 + have + Txt.CheckJavaVersionAppendServerLog2);
                return true;
            }
            AppendServerLog(Txt.CheckJavaVersionAppendServerLog9 + mcVer + Txt.RunFail3 + required + Txt.CheckJavaVersionAppendServerLog10 + have + Txt.CheckJavaVersionAppendServerLog11);
            bool proceed = false;
            Ui(() =>
            {
                string msg = Txt.Msg2 + mcVer + Txt.CheckJavaVersionPart + required + Txt.CheckJavaVersionPart2 + have + Txt.CheckJavaVersionPart3 + required + Txt.CheckJavaVersionPart4;
                proceed = InfoBox.Show(msg, Txt.CheckJavaVersionShow, Txt.CheckJavaVersionShow2, Txt.CheckJavaVersionShow3) == DialogResult.OK;
            });
            AppendServerLog(proceed ? Txt.CheckJavaVersionAppendServerLog12 : Txt.CheckJavaVersionAppendServerLog13);
            return proceed;
        }

        void StartAll()
        {
            if (_starting) return;
            if (ProcAlive(_server) && _serverDone) { AppendState(Txt.StartAllAppendState); btnStart.Text = Txt.BtnStartText3; btnStart.BackColor = Color.FromArgb(71, 77, 88); RefreshStatus(); return; }

            _starting = true;
            btnStart.Enabled = false;
            btnStart.Text = Txt.BtnStartText4;
            btnStart.BackColor = Color.FromArgb(72, 144, 245);
            Thread t = new Thread(() => DoStart());
            t.IsBackground = true;
            t.Start();
        }

        void DoStart()
        {
            try
            {
                // 1. 检查目录和文件
                if (!Directory.Exists(_cfg.ServerDir)) { FailState(Txt.DoStartFailState + _cfg.ServerDir + Txt.DoStartFailState2); return; }
                if (!File.Exists(Path.Combine(_cfg.ServerDir, _cfg.JarName))) { FailState(Txt.DoStartFailState3 + _cfg.JarName + Txt.DoStartFailState4); return; }
                if (string.IsNullOrEmpty(_cfg.JavaPath) || !File.Exists(_cfg.JavaPath)) { FailState(Txt.DoStartFailState5); return; }

                // v6.20: Java 版本校验（版本过低会弹窗，用户可坚持启动）
                if (!CheckJavaVersion(Path.Combine(_cfg.ServerDir, _cfg.JarName))) { FailState(Txt.DoStartFailState6); return; }

                // 2. 端口检查
                if (!ProcAlive(_server))
                {
                    if (PortInUse(_cfg.Port))
                    {
                        FailState(Txt.GuessCrash614Ret + _cfg.Port + Txt.DoStartFailState7);
                        return;
                    }
                }

                // v6.11: optional pre-start backup, inline on this worker
                // thread (the server is not up yet, so the world is quiet)
                if (_cfg.BackupOnStart && !_backupBusy)
                {
                    _backupBusy = true;
                    try { Ui(() => AppendState(Txt.DoStartAppendState)); BackupCore(Txt.BuildBackupPanelBkCheck2); }
                    catch { }
                    _backupBusy = false;
                    Ui(() => SetBackupUi());
                }

                // 3. 启动服务端
                AppendState(Txt.DoStartAppendState2);
                _serverDone = false;
                _stoppingIntentional = false;
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = _cfg.JavaPath;
                psi.Arguments = _cfg.MemParams + " -jar " + _cfg.JarName + " nogui";
                psi.WorkingDirectory = _cfg.ServerDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardInput = true;
                psi.StandardErrorEncoding = Encoding.UTF8;
                try { psi.StandardOutputEncoding = Encoding.UTF8; } catch { }

                _server = new Process();
                _server.StartInfo = psi;
                _server.OutputDataReceived += ServerOutput;
                _server.ErrorDataReceived += ServerError;
                _server.Exited += (s, e) =>
                {
                    int code = -999; try { code = ((Process)s).ExitCode; } catch { }
                    bool wasDone = _serverDone;
                    Ui(() =>
                    {
                        AppendServerLog(Txt.DoStartAppendServerLog);
                        CancelSparkProfile();
                        if (!_stoppingIntentional && wasDone) HandleServerCrash(code);
                        RefreshStatus();
                    });
                };
                _server.EnableRaisingEvents = true;
                if (!_server.Start()) { FailState(Txt.DoStartFailState8); return; }
                _server.BeginOutputReadLine();
                _server.BeginErrorReadLine();

                // 等待 Done，最多 90 秒
                AppendState(Txt.DoStartAppendState3);
                int waited = 0;
                while (!_serverDone && waited <= 90)
                {
                    if (!ProcAlive(_server)) { FailState(Txt.DoStartFailState9); return; }
                    Thread.Sleep(1000);
                    waited++;
                }
                if (!_serverDone) { FailState(Txt.DoStartFailState10); return; }

                Ui(() => { btnStart.BackColor = Color.FromArgb(59, 165, 93); btnStart.Text = Txt.BtnStartText3; RefreshStatus(); });

                // 4. 启动隧道
                StartTunnel();

                // 5. 全成功
                if (_tunnelUp && !string.IsNullOrEmpty(_cfg.PublicAddr))
                {
                    Ui(() => { btnStart.BackColor = Color.FromArgb(71, 77, 88); AppendState(Txt.DoStartAppendState4); });
                }
                else
                {
                    Ui(() => { btnStart.Text = Txt.BtnStartText2; btnStart.BackColor = Color.FromArgb(59, 165, 93); });
                }
            }
            catch (Exception ex)
            {
                FailState(Txt.DoStartFailState11 + ex.Message);
            }
            finally
            {
                _starting = false;
                Ui(() => { btnStart.Enabled = true; if (btnStart.Text == Txt.BtnStartText4) btnStart.Text = Txt.BtnStartText2; RefreshStatus(); });
            }
        }

        void StartTunnel()
        {
            if (_cfg.TunnelMode == "launcher")
            {
                Ui(() => AppendTunnelLog(Txt.StartTunnelAppendTunnelLog));
                try { Process.Start(new ProcessStartInfo(_cfg.FrpcPath) { WorkingDirectory = Path.GetDirectoryName(_cfg.FrpcPath) }); }
                catch (Exception ex) { Ui(() => AppendTunnelLog(Txt.StartTunnelAppendTunnelLog2 + ex.Message)); }
                return;
            }

            // frpc 命令行
            if (!File.Exists(_cfg.FrpcPath)) { Ui(() => AppendTunnelLog(Txt.StartTunnelAppendTunnelLog3 + _cfg.FrpcPath + Txt.StartTunnelAppendTunnelLog4)); return; }
            if (string.IsNullOrEmpty(_cfg.FrpKey)) { Ui(() => AppendTunnelLog(Txt.StartTunnelAppendTunnelLog5)); return; }
            if (string.IsNullOrEmpty(_cfg.TunnelId)) { Ui(() => AppendTunnelLog(Txt.StartTunnelAppendTunnelLog6)); return; }

            if (ProcAlive(_frpc)) { StartVoiceTunnel(); return; }
            AppendState(Txt.StartTunnelAppendState);
            _tunnelUp = false;
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = _cfg.FrpcPath;
            psi.Arguments = "-f " + _cfg.FrpKey + ":" + _cfg.TunnelId;
            psi.WorkingDirectory = Path.GetDirectoryName(_cfg.FrpcPath);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardErrorEncoding = Encoding.UTF8;
            try { psi.StandardOutputEncoding = Encoding.UTF8; } catch { }

            _frpc = new Process();
            _frpc.StartInfo = psi;
            _frpc.OutputDataReceived += FrpcOutput;
            _frpc.ErrorDataReceived += FrpcError;
            _frpc.EnableRaisingEvents = true;
            _frpc.Exited += (s, e) => { Ui(() =>
            {
                AppendTunnelLog(Txt.StartTunnelAppendTunnelLog7);
                if (!_stoppingIntentional && _serverDone && ProcAlive(_server) && _cfg.TunnelMode == "frpc")
                {
                    AppendTunnelLog(Txt.StartTunnelAppendTunnelLog8);
                    Delayed(5000, () => { if (!_stoppingIntentional && ProcAlive(_server)) StartTunnel(); });
                }
                RefreshStatus();
            }); };
            if (!_frpc.Start()) { Ui(() => AppendTunnelLog(Txt.StartTunnelAppendTunnelLog9)); return; }
            _frpc.BeginOutputReadLine();
            _frpc.BeginErrorReadLine();

            int waited = 0;
            while (!_tunnelUp && waited <= 40)
            {
                if (!ProcAlive(_frpc))
                {
                    if (_frpcBundled)
                    {
                        Ui(() => AppendTunnelLog(Txt.StartTunnelAppendTunnelLog10));
                    }
                    else
                    {
                        Ui(() => AppendTunnelLog(Txt.StartTunnelAppendTunnelLog11));
                    }
                    break;
                }
                Thread.Sleep(1000);
                waited++;
            }
            if (_frpcBundled && !_tunnelUp)
            {
                Ui(() => { AppendState(Txt.StartTunnelAppendState2); });
            }

            StartVoiceTunnel();
        }

        void StartVoiceTunnel()
        {
            if (string.IsNullOrEmpty(_cfg.VoiceTunnelId)) { Ui(() => AppendTunnelLog(Txt.StartVoiceTunnelAppendTunnelLog)); return; }
            if (ProcAlive(_frpcVoice)) return;
            _voiceUp = false;
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = _cfg.FrpcPath;
            psi.Arguments = "-f " + _cfg.FrpKey + ":" + _cfg.VoiceTunnelId;
            psi.WorkingDirectory = Path.GetDirectoryName(_cfg.FrpcPath);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardErrorEncoding = Encoding.UTF8;
            try { psi.StandardOutputEncoding = Encoding.UTF8; } catch { }

            _frpcVoice = new Process();
            _frpcVoice.StartInfo = psi;
            _frpcVoice.OutputDataReceived += VoiceFrpcOutput;
            _frpcVoice.ErrorDataReceived += VoiceFrpcError;
            _frpcVoice.EnableRaisingEvents = true;
            _frpcVoice.Exited += (s, e) => { Ui(() =>
            {
                AppendTunnelLog(Txt.StartVoiceTunnelAppendTunnelLog2);
                if (!_stoppingIntentional && _serverDone && ProcAlive(_server))
                {
                    AppendTunnelLog(Txt.StartVoiceTunnelAppendTunnelLog3);
                    Delayed(5000, () => { if (!_stoppingIntentional && ProcAlive(_server)) StartVoiceTunnel(); });
                }
                RefreshStatus();
            }); };
            if (!_frpcVoice.Start()) { Ui(() => AppendTunnelLog(Txt.StartVoiceTunnelAppendTunnelLog4)); return; }
            _frpcVoice.BeginOutputReadLine();
            _frpcVoice.BeginErrorReadLine();

            int waited = 0;
            while (!_voiceUp && waited <= 20)
            {
                if (!ProcAlive(_frpcVoice)) { Ui(() => AppendTunnelLog(Txt.StartVoiceTunnelAppendTunnelLog5)); break; }
                Thread.Sleep(1000);
                waited++;
            }
            if (_voiceUp) Ui(() => AppendState(Txt.StartVoiceTunnelAppendState));
        }

        void VoiceFrpcOutput(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendTunnelLog(Txt.VoiceFrpcOutputAppendTunnelLog + e.Data);
            TryParseVoice(e.Data);
        }
        void VoiceFrpcError(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendTunnelLog(Txt.VoiceFrpcErrorAppendTunnelLog + e.Data);
            TryParseVoice(e.Data);
        }
        void TryParseVoice(string line)
        {
            if (Regex.IsMatch(line, @"代理启动成功|start proxy success|login success|隧道.*启动成功|\[.*\]\s*tunnel.*success"))
                _voiceUp = true;
        }

        // v6.9: keep the online-player list from the server log's own
        // join/leave lines (the count itself still comes from the TCP table)
        void TrackPlayerLine(string line)
        {
            try
            {
                var m = Regex.Match(line, @"([A-Za-z0-9_]{3,16}) joined the game");
                if (m.Success)
                {
                    string n = m.Groups[1].Value;
                    Ui(() => { if (!_onlineNames.Contains(n)) _onlineNames.Add(n); });
                    return;
                }
                m = Regex.Match(line, @"([A-Za-z0-9_]{3,16}) left the game");
                if (m.Success)
                {
                    string n = m.Groups[1].Value;
                    Ui(() => { _onlineNames.Remove(n); });
                }
            }
            catch { }
        }

        void ServerOutput(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendServerLog(e.Data);
            TrackPlayerLine(e.Data);
            if (Regex.IsMatch(e.Data, "Done \\(") && e.Data.Contains("For help"))
                _serverDone = true;
        }
        void ServerError(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendServerLog("[ERR] " + e.Data);
            if (Regex.IsMatch(e.Data, "Done \\(") && e.Data.Contains("For help"))
                _serverDone = true;
        }
        void FrpcOutput(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendTunnelLog(e.Data);
            TryParseFrpc(e.Data);
        }
        void FrpcError(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendTunnelLog("[ERR] " + e.Data);
            TryParseFrpc(e.Data);
        }

        void TryParseFrpc(string line)
        {
            // 检测 SakuraFrp 启动器套装版 frpc（不能由外部程序直接调用）
            if (line.Contains("not intended to be run directly") || line.Contains("other than SakuraFrp Service") || line.Contains("part of SakuraFrp Launcher"))
            {
                _frpcBundled = true;
            }
            // 成功字样
            if (Regex.IsMatch(line, @"代理启动成功|start proxy success|login success|隧道.*启动成功|\[.*\]\s*tunnel.*success"))
                _tunnelUp = true;

            // 解析公网地址：形如 xxxx.natfrp.cloud:port 或 域名:port
            if (string.IsNullOrEmpty(_cfg.PublicAddr))
            {
                var m = Regex.Match(line, "\\b[a-z0-9][a-z0-9-]*\\.(?:natfrp\\.cloud|frp-[a-z0-9]+\\.com|gofrp\\.org|nathub\\.app|frp-a\\.com|\\w+\\.frp-\\w+\\.com):\\d+\\b");
                if (!m.Success) m = Regex.Match(line, "\\b[a-z0-9][a-z0-9-]*\\.\\w+\\.\\w{2,4}:\\d{4,5}\\b");
                if (m.Success && !m.Value.StartsWith("127.0.0.1"))
                {
                    string addr = m.Value;
                    _cfg.PublicAddr = addr;
                    _cfg.Save();
                    Ui(() => { tbAddr.Text = addr; AppendState(Txt.TryParseFrpcAppendState + addr); RefreshStatus(); });
                }
            }
        }

        void FailState(string msg)
        {
            Ui(() => {
                AppendState(msg);
                InfoBox.Show(msg, Txt.OnHandleCreatedStr);
                if (statusCard != null) { statusCard.ServerText = Txt.ServerText4; statusCard.ServerColor = Color.Red; statusCard.TunnelText = Txt.TunnelText4; statusCard.TunnelColor = Color.Red; statusCard.SetPulse(0, false); statusCard.SetPulse(1, false); statusCard.Invalidate(); }
                btnStart.Text = Txt.BtnStartText2; btnStart.BackColor = Color.FromArgb(59, 165, 93);
            });
        }

        // ---------- 安全关服 ----------
        void SafeStop()
        {
            _stoppingIntentional = true;
            if (!ProcAlive(_server))
            {
                KillFrpc();
                Ui(() => { AppendState(Txt.SafeStopAppendState); RefreshStatus(); });
                return;
            }

            Ui(() => { btnStop.Enabled = false; });
            AppendState(Txt.SafeStopAppendState2);
            try { _server.StandardInput.WriteLine("stop"); _server.StandardInput.Flush(); }
            catch { }

            AppendServerLog(Txt.SafeStopAppendServerLog);

            // 最多等 60 秒
            int waited = 0;
            bool exited = false;
            while (waited <= 60)
            {
                if (!ProcAlive(_server)) { exited = true; break; }
                Thread.Sleep(1000);
                waited++;
            }

            if (exited)
            {
                AppendState(Txt.SafeStopAppendState3);
                Ui(() => AppendServerLog(Txt.SafeStopAppendServerLog2));
            }
            else
            {
                DialogResult res = DialogResult.Cancel;
                Ui(() => { res = InfoBox.Show(Txt.SafeStopShow, Txt.SafeStopShow2, Txt.SafeStopShow3, Txt.SafeStopShow4); });
                if (res == DialogResult.OK)
                {
                    try { _server.Kill(); } catch { }
                    AppendState(Txt.SafeStopAppendState4);
                }
                else
                {
                    AppendState(Txt.SafeStopAppendState5);
                }
            }

            KillFrpc();
            bool serverGone = !ProcAlive(_server);
            Ui(() => { btnStart.Text = Txt.BtnStartText2; btnStart.BackColor = Color.FromArgb(59, 165, 93); btnStop.Enabled = true; RefreshStatus(); if (serverGone) AppendState(Txt.SafeStopAppendState6); });
        }

        // HasExited throws on a Process that was constructed but whose
        // Start() failed (no associated process). Never read it raw on
        // the frpc processes: a blocked start must not crash stop/status.
        static bool ProcAlive(Process p)
        {
            if (p == null) return false;
            try { return !p.HasExited; } catch { return false; }
        }

        void KillFrpc()
        {
            if (ProcAlive(_frpc)) { try { _frpc.Kill(); } catch { } }
            _frpc = null;
            _tunnelUp = false;
            if (ProcAlive(_frpcVoice)) { try { _frpcVoice.Kill(); } catch { } }
            _frpcVoice = null;
            _voiceUp = false;
        }

        // ---------- 关闭拦截 ----------
        void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (_animActive)
            {
                _animActive = false;
                if (_animTimer != null) _animTimer.Stop();
                try { timeEndPeriod(1); } catch { }
                _animClock.Stop();
            }
            try { if (WindowState == FormWindowState.Normal) { _cfg.WinX = Location.X; _cfg.WinY = Location.Y; _cfg.Save(); } } catch { }
            bool serverRunning = ProcAlive(_server);
            bool frpcRunning = ProcAlive(_frpc) || ProcAlive(_frpcVoice);
            if (serverRunning || frpcRunning)
            {
                using (var c = new ConfirmBox(Txt.OnClosingConfirmBox))
                {
                    var r = c.ShowDialog(this);
                    if (r == DialogResult.OK && c.ChoiceYes)
                    {
                        _stoppingIntentional = true;
                        // 安全关服
                        try { if (ProcAlive(_server)) { _server.StandardInput.WriteLine("stop"); _server.StandardInput.Flush(); } } catch { }
                        int waited = 0;
                        while (ProcAlive(_server) && waited <= 60) { Thread.Sleep(1000); waited++; }
                        KillFrpc();
                        e.Cancel = false;
                    }
                    else
                    {
                        e.Cancel = true; // 取消关闭
                    }
                }
            }
        }
    }
}
