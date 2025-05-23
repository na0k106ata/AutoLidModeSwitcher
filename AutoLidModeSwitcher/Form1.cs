using Microsoft.Win32; // Registryクラスのために必要
using System;
using System.Diagnostics; // Processクラスのために必要
using System.Windows.Forms;
using System.Drawing; // Iconクラス、SystemIconsのために必要

namespace AutoLidModeSwitcher
{
    public partial class Form1 : Form
    {
        // NotifyIconに設定するコンテキストメニュー
        private ContextMenuStrip contextMenuStrip1 = default!;
        private ToolStripMenuItem menuItemCurrentMode = default!;
        private ToolStripMenuItem menuItemReturnToAuto = default!;
        private ToolStripSeparator menuSeparatorActions = default!;
        private ToolStripMenuItem menuItemSetAcDoNothing = default!; // AC電源設定用「何もしない」
        private ToolStripMenuItem menuItemSetAcSleep = default!;   // AC電源設定用「スリープ」
        private ToolStripSeparator menuSeparatorStartup = default!;
        private ToolStripMenuItem menuItemAutoStart = default!;
        private ToolStripSeparator menuSeparatorExit = default!;
        private ToolStripMenuItem menuItemExit = default!;

        private bool isManualModeActive = false;
        private const string BASE_TOOLTIP_TEXT = "クラムシェルモード自動切替";
        private const string APP_NAME_FOR_REGISTRY = "AutoLidModeSwitcher";

        private Icon iconAutoMode = default!;
        private Icon iconManualMode = default!;

        public Form1()
        {
            InitializeComponent();
            LoadIcons();
            InitializeContextMenu();

            if (this.notifyIcon1 != null && iconAutoMode != null)
            {
                this.notifyIcon1.Icon = iconAutoMode;
            }
        }

        private void LoadIcons()
        {
            try
            {
                iconAutoMode = Properties.Resources.icon_auto;
                iconManualMode = Properties.Resources.icon_manual;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"アイコンの読み込みに失敗しました: {ex.Message}");
                iconAutoMode = SystemIcons.Application;
                iconManualMode = SystemIcons.Warning;
            }
        }

        private void InitializeContextMenu()
        {
            contextMenuStrip1 = new ContextMenuStrip();
            menuItemCurrentMode = new ToolStripMenuItem("モード: 自動設定");
            menuItemCurrentMode.Enabled = false;
            menuItemReturnToAuto = new ToolStripMenuItem("自動設定に戻す (&A)");
            menuSeparatorActions = new ToolStripSeparator();
            menuItemSetAcDoNothing = new ToolStripMenuItem("AC電源時: 何もしない (&N)");
            menuItemSetAcSleep = new ToolStripMenuItem("AC電源時: スリープ (&S)");
            menuSeparatorStartup = new ToolStripSeparator();
            menuItemAutoStart = new ToolStripMenuItem("Windows起動時に自動起動 (&T)");
            menuItemAutoStart.CheckOnClick = true;
            menuSeparatorExit = new ToolStripSeparator();
            menuItemExit = new ToolStripMenuItem("終了 (&X)");

            contextMenuStrip1.Items.AddRange(new ToolStripItem[] {
                menuItemCurrentMode,
                menuItemReturnToAuto,
                menuSeparatorActions,
                menuItemSetAcDoNothing,
                menuItemSetAcSleep,
                menuSeparatorStartup,
                menuItemAutoStart,
                menuSeparatorExit,
                menuItemExit
            });

            contextMenuStrip1.Opening += ContextMenuStrip1_Opening_Simplified;
            menuItemReturnToAuto.Click += MenuItemReturnToAuto_Click;
            menuItemSetAcDoNothing.Click += MenuItemSetAcDoNothing_Click; // イベントハンドラを紐付け
            menuItemSetAcSleep.Click += MenuItemSetAcSleep_Click;       // イベントハンドラを紐付け
            menuItemAutoStart.CheckedChanged += MenuItemAutoStart_CheckedChanged;
            menuItemExit.Click += MenuItemExit_Click;

            if (this.notifyIcon1 != null)
            {
                this.notifyIcon1.ContextMenuStrip = contextMenuStrip1;
                this.notifyIcon1.Text = BASE_TOOLTIP_TEXT;
            }
        }

        private void ContextMenuStrip1_Opening_Simplified(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateModeStatusDisplay();
            LoadAutoStartSetting();
        }

        private void UpdateModeStatusDisplay()
        {
            string modeText = isManualModeActive ? "手動設定中" : "自動設定";
            if (menuItemCurrentMode != null)
            {
                menuItemCurrentMode.Text = $"モード: {modeText}";
            }
            if (menuItemReturnToAuto != null)
            {
                menuItemReturnToAuto.Enabled = isManualModeActive;
            }

            if (notifyIcon1 != null)
            {
                notifyIcon1.Text = $"{BASE_TOOLTIP_TEXT} ({modeText})";
                notifyIcon1.Icon = isManualModeActive ? iconManualMode : iconAutoMode;
            }
        }

        private void MenuItemReturnToAuto_Click(object? sender, EventArgs e)
        {
            isManualModeActive = false;
            UpdateModeStatusDisplay();
            UpdateLidActionBasedOnPowerState(); // これがAC設定のみを変更する
            if (notifyIcon1 != null)
            {
                notifyIcon1.ShowBalloonTip(1000, "モード変更", "自動設定モードに戻りました。", ToolTipIcon.Info);
            }
        }

        private void MenuItemSetAcDoNothing_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("手動設定: AC電源時「何もしない」");
            SetLidActionForAc(false, true); // 手動設定としてAC電源設定のみを変更
            isManualModeActive = true;
            UpdateModeStatusDisplay();
        }

        private void MenuItemSetAcSleep_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("手動設定: AC電源時「スリープ」");
            SetLidActionForAc(true, true); // 手動設定としてAC電源設定のみを変更
            isManualModeActive = true;
            UpdateModeStatusDisplay();
        }

        private void MenuItemAutoStart_CheckedChanged(object? sender, EventArgs e)
        {
            if (menuItemAutoStart != null) // nullチェックを追加
            {
                SetAutoStart(menuItemAutoStart.Checked);
            }
        }

        private void MenuItemExit_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            LoadAutoStartSetting();

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            CheckInitialPowerState();
            UpdateModeStatusDisplay();
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            if (notifyIcon1 != null)
            {
                notifyIcon1.Visible = false;
                notifyIcon1.Dispose();
            }
        }

        private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.StatusChange)
            {
                if (isManualModeActive)
                {
                    if (notifyIcon1 != null)
                    {
                        notifyIcon1.ShowBalloonTip(1000, "モード変更", "電源状態が変更されたため、自動設定モードに戻りました。", ToolTipIcon.Info);
                    }
                }
                isManualModeActive = false;
                UpdateModeStatusDisplay();
                UpdateLidActionBasedOnPowerState(); // これがAC設定のみを変更する
            }
        }

        // このメソッドは、現在のPCの電源状態に応じて「AC電源接続時の蓋閉じ設定」をどうするかを決定する
        private void UpdateLidActionBasedOnPowerState()
        {
            if (isManualModeActive) return; // 手動モードの場合は自動変更しない

            PowerLineStatus currentPcPowerStatus = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus;
            bool setAcLidActionToSleep; // AC電源設定をスリープにするかどうかのフラグ

            // 自動設定ロジック:
            // PCがAC接続時は、AC電源設定を「何もしない」(クラムシェルモード) にする
            // PCがバッテリー時は、AC電源設定を「スリープ」にする
            if (currentPcPowerStatus == PowerLineStatus.Online)
            {
                Console.WriteLine("PCはAC電源に接続されています。AC電源時の蓋閉じ動作を「何もしない」に設定します。");
                setAcLidActionToSleep = false;
            }
            else if (currentPcPowerStatus == PowerLineStatus.Offline)
            {
                Console.WriteLine("PCはバッテリー駆動です。AC電源時の蓋閉じ動作を「スリープ」に設定します。");
                setAcLidActionToSleep = true;
            }
            else
            {
                Console.WriteLine("PCの電源状態が不明です。設定は変更しません。");
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(2000, "電源状態不明", "現在の電源状態を特定できませんでした。", ToolTipIcon.Warning);
                }
                return;
            }
            // 自動設定として、AC電源設定のみを変更
            SetLidActionForAc(setAcLidActionToSleep, false);
        }

        private void CheckInitialPowerState()
        {
            Console.WriteLine("現在の電源状態を確認して初期設定を行います。");
            isManualModeActive = false; // 起動時は必ず自動モード
            UpdateLidActionBasedOnPowerState(); // これがAC設定のみを変更する
        }

        // このメソッドは常にAC電源設定のみを変更する
        // setAcToSleepValue: AC電源設定をスリープにする場合はtrue、何もしないにする場合はfalse
        // isManualOperation: 手動操作による呼び出しかどうか (主に通知メッセージ用)
        private void SetLidActionForAc(bool setAcToSleepValue, bool isManualOperation = false)
        {
            string actionDescription = setAcToSleepValue ? "スリープ" : "何もしない";
            string powercfgArgumentsAC;
            string baseMessage;

            // 常にAC電源設定のみを変更するコマンドを生成
            powercfgArgumentsAC = $"/SETACVALUEINDEX SCHEME_CURRENT SUB_BUTTONS LIDACTION {(setAcToSleepValue ? 1 : 0)}";

            if (isManualOperation)
            {
                baseMessage = $"手動でAC電源時の蓋閉じ動作を「{actionDescription}」に設定しました。";
            }
            else // 自動設定の場合
            {
                PowerLineStatus currentPcPowerStatus = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus;
                string powerContextMessage = currentPcPowerStatus == PowerLineStatus.Online ? "PCがAC電源に接続されたため" : "PCがバッテリー駆動になったため";
                baseMessage = $"{powerContextMessage}、AC電源時の蓋閉じ動作を「{actionDescription}」に設定しました。";
            }

            try
            {
                // AC電源設定の変更コマンドのみ実行
                ProcessStartInfo procStartInfoAC = new ProcessStartInfo("powercfg.exe", powercfgArgumentsAC)
                { UseShellExecute = true, CreateNoWindow = true };
                Process.Start(procStartInfoAC)?.WaitForExit();

                // 電源プランをアクティブにする (設定を即時反映させるため)
                ProcessStartInfo setActiveProcInfo = new ProcessStartInfo("powercfg.exe", "/SETACTIVE SCHEME_CURRENT")
                { UseShellExecute = true, CreateNoWindow = true };
                Process.Start(setActiveProcInfo)?.WaitForExit();

                Console.WriteLine(baseMessage);
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(2000, "設定完了", baseMessage, ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"蓋閉じ動作の設定変更に失敗しました。\nエラー: {ex.GetType().Name}";
                Console.WriteLine($"powercfgの実行に失敗しました: {ex.Message}");
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(3000, "設定エラー", errorMessage, ToolTipIcon.Error);
                }
            }
        }

        private void LoadAutoStartSetting()
        {
            try
            {
                string? executablePath = Application.ExecutablePath;
                if (string.IsNullOrEmpty(executablePath))
                {
                    Console.WriteLine("自動起動設定の読み込みエラー: アプリケーションパスが取得できません。");
                    if (menuItemAutoStart != null) menuItemAutoStart.Checked = false;
                    return;
                }

                using (RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    if (rk == null)
                    {
                        if (menuItemAutoStart != null) menuItemAutoStart.Checked = false;
                        return;
                    }
                    string? appPath = rk.GetValue(APP_NAME_FOR_REGISTRY) as string;
                    if (menuItemAutoStart != null)
                    {
                        menuItemAutoStart.Checked = !string.IsNullOrEmpty(appPath) && appPath.Equals(executablePath, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"自動起動設定の読み込みに失敗: {ex.Message}");
                if (menuItemAutoStart != null)
                {
                    menuItemAutoStart.Checked = false;
                }
            }
        }

        private void SetAutoStart(bool autoStartEnabled)
        {
            try
            {
                string? executablePath = Application.ExecutablePath;
                if (string.IsNullOrEmpty(executablePath))
                {
                    Console.WriteLine("自動起動設定のエラー: アプリケーションパスが取得できません。");
                    if (notifyIcon1 != null)
                    {
                        notifyIcon1.ShowBalloonTip(2000, "設定エラー", "自動起動設定の変更に失敗しました。\n(アプリケーションパス不明)", ToolTipIcon.Error);
                    }
                    if (menuItemAutoStart != null && menuItemAutoStart.Checked == autoStartEnabled)
                    {
                        menuItemAutoStart.CheckedChanged -= MenuItemAutoStart_CheckedChanged;
                        menuItemAutoStart.Checked = !autoStartEnabled;
                        menuItemAutoStart.CheckedChanged += MenuItemAutoStart_CheckedChanged;
                    }
                    return;
                }

                using (RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (rk == null)
                    {
                        Console.WriteLine("レジストリキー 'Run' が見つかりません。");
                        if (notifyIcon1 != null)
                        {
                            notifyIcon1.ShowBalloonTip(2000, "設定エラー", "自動起動設定の変更に失敗しました。\n(レジストリキーが見つかりません)", ToolTipIcon.Error);
                        }
                        if (menuItemAutoStart != null) menuItemAutoStart.Checked = !autoStartEnabled;
                        return;
                    }

                    if (autoStartEnabled)
                    {
                        rk.SetValue(APP_NAME_FOR_REGISTRY, executablePath);
                        Console.WriteLine("自動起動を有効にしました。");
                    }
                    else
                    {
                        rk.DeleteValue(APP_NAME_FOR_REGISTRY, false);
                        Console.WriteLine("自動起動を無効にしました。");
                    }
                }
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(1000, "自動起動設定", autoStartEnabled ? "Windows起動時の自動起動を有効にしました。" : "Windows起動時の自動起動を無効にしました。", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"自動起動設定の書き込みに失敗: {ex.Message}");
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(3000, "設定エラー", $"自動起動設定の変更に失敗しました。\nエラー: {ex.GetType().Name}", ToolTipIcon.Error);
                }
                if (menuItemAutoStart != null)
                {
                    try
                    {
                        menuItemAutoStart.CheckedChanged -= MenuItemAutoStart_CheckedChanged;
                        menuItemAutoStart.Checked = !autoStartEnabled;
                    }
                    finally
                    {
                        menuItemAutoStart.CheckedChanged += MenuItemAutoStart_CheckedChanged;
                    }
                }
            }
        }
    }
}
