using Microsoft.Win32; // Registry�N���X�̂��߂ɕK�v
using System;
using System.Diagnostics; // Process�N���X�̂��߂ɕK�v
using System.Windows.Forms;
using System.Drawing; // Icon�N���X�ASystemIcons�̂��߂ɕK�v

namespace AutoLidModeSwitcher
{
    public partial class Form1 : Form
    {
        // NotifyIcon�ɐݒ肷��R���e�L�X�g���j���[
        private ContextMenuStrip contextMenuStrip1 = default!;
        private ToolStripMenuItem menuItemCurrentMode = default!;
        private ToolStripMenuItem menuItemReturnToAuto = default!;
        private ToolStripSeparator menuSeparatorActions = default!;
        private ToolStripMenuItem menuItemSetAcDoNothing = default!; // AC�d���ݒ�p�u�������Ȃ��v
        private ToolStripMenuItem menuItemSetAcSleep = default!;   // AC�d���ݒ�p�u�X���[�v�v
        private ToolStripSeparator menuSeparatorStartup = default!;
        private ToolStripMenuItem menuItemAutoStart = default!;
        private ToolStripSeparator menuSeparatorExit = default!;
        private ToolStripMenuItem menuItemExit = default!;

        private bool isManualModeActive = false;
        private const string BASE_TOOLTIP_TEXT = "�N�����V�F�����[�h�����ؑ�";
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
                Console.WriteLine($"�A�C�R���̓ǂݍ��݂Ɏ��s���܂���: {ex.Message}");
                iconAutoMode = SystemIcons.Application;
                iconManualMode = SystemIcons.Warning;
            }
        }

        private void InitializeContextMenu()
        {
            contextMenuStrip1 = new ContextMenuStrip();
            menuItemCurrentMode = new ToolStripMenuItem("���[�h: �����ݒ�");
            menuItemCurrentMode.Enabled = false;
            menuItemReturnToAuto = new ToolStripMenuItem("�����ݒ�ɖ߂� (&A)");
            menuSeparatorActions = new ToolStripSeparator();
            menuItemSetAcDoNothing = new ToolStripMenuItem("AC�d����: �������Ȃ� (&N)");
            menuItemSetAcSleep = new ToolStripMenuItem("AC�d����: �X���[�v (&S)");
            menuSeparatorStartup = new ToolStripSeparator();
            menuItemAutoStart = new ToolStripMenuItem("Windows�N�����Ɏ����N�� (&T)");
            menuItemAutoStart.CheckOnClick = true;
            menuSeparatorExit = new ToolStripSeparator();
            menuItemExit = new ToolStripMenuItem("�I�� (&X)");

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
            menuItemSetAcDoNothing.Click += MenuItemSetAcDoNothing_Click; // �C�x���g�n���h����R�t��
            menuItemSetAcSleep.Click += MenuItemSetAcSleep_Click;       // �C�x���g�n���h����R�t��
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
            string modeText = isManualModeActive ? "�蓮�ݒ蒆" : "�����ݒ�";
            if (menuItemCurrentMode != null)
            {
                menuItemCurrentMode.Text = $"���[�h: {modeText}";
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
            UpdateLidActionBasedOnPowerState(); // ���ꂪAC�ݒ�݂̂�ύX����
            if (notifyIcon1 != null)
            {
                notifyIcon1.ShowBalloonTip(1000, "���[�h�ύX", "�����ݒ胂�[�h�ɖ߂�܂����B", ToolTipIcon.Info);
            }
        }

        private void MenuItemSetAcDoNothing_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("�蓮�ݒ�: AC�d�����u�������Ȃ��v");
            SetLidActionForAc(false, true); // �蓮�ݒ�Ƃ���AC�d���ݒ�݂̂�ύX
            isManualModeActive = true;
            UpdateModeStatusDisplay();
        }

        private void MenuItemSetAcSleep_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("�蓮�ݒ�: AC�d�����u�X���[�v�v");
            SetLidActionForAc(true, true); // �蓮�ݒ�Ƃ���AC�d���ݒ�݂̂�ύX
            isManualModeActive = true;
            UpdateModeStatusDisplay();
        }

        private void MenuItemAutoStart_CheckedChanged(object? sender, EventArgs e)
        {
            if (menuItemAutoStart != null) // null�`�F�b�N��ǉ�
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
                        notifyIcon1.ShowBalloonTip(1000, "���[�h�ύX", "�d����Ԃ��ύX���ꂽ���߁A�����ݒ胂�[�h�ɖ߂�܂����B", ToolTipIcon.Info);
                    }
                }
                isManualModeActive = false;
                UpdateModeStatusDisplay();
                UpdateLidActionBasedOnPowerState(); // ���ꂪAC�ݒ�݂̂�ύX����
            }
        }

        // ���̃��\�b�h�́A���݂�PC�̓d����Ԃɉ����āuAC�d���ڑ����̊W���ݒ�v���ǂ����邩�����肷��
        private void UpdateLidActionBasedOnPowerState()
        {
            if (isManualModeActive) return; // �蓮���[�h�̏ꍇ�͎����ύX���Ȃ�

            PowerLineStatus currentPcPowerStatus = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus;
            bool setAcLidActionToSleep; // AC�d���ݒ���X���[�v�ɂ��邩�ǂ����̃t���O

            // �����ݒ胍�W�b�N:
            // PC��AC�ڑ����́AAC�d���ݒ���u�������Ȃ��v(�N�����V�F�����[�h) �ɂ���
            // PC���o�b�e���[���́AAC�d���ݒ���u�X���[�v�v�ɂ���
            if (currentPcPowerStatus == PowerLineStatus.Online)
            {
                Console.WriteLine("PC��AC�d���ɐڑ�����Ă��܂��BAC�d�����̊W��������u�������Ȃ��v�ɐݒ肵�܂��B");
                setAcLidActionToSleep = false;
            }
            else if (currentPcPowerStatus == PowerLineStatus.Offline)
            {
                Console.WriteLine("PC�̓o�b�e���[�쓮�ł��BAC�d�����̊W��������u�X���[�v�v�ɐݒ肵�܂��B");
                setAcLidActionToSleep = true;
            }
            else
            {
                Console.WriteLine("PC�̓d����Ԃ��s���ł��B�ݒ�͕ύX���܂���B");
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(2000, "�d����ԕs��", "���݂̓d����Ԃ����ł��܂���ł����B", ToolTipIcon.Warning);
                }
                return;
            }
            // �����ݒ�Ƃ��āAAC�d���ݒ�݂̂�ύX
            SetLidActionForAc(setAcLidActionToSleep, false);
        }

        private void CheckInitialPowerState()
        {
            Console.WriteLine("���݂̓d����Ԃ��m�F���ď����ݒ���s���܂��B");
            isManualModeActive = false; // �N�����͕K���������[�h
            UpdateLidActionBasedOnPowerState(); // ���ꂪAC�ݒ�݂̂�ύX����
        }

        // ���̃��\�b�h�͏��AC�d���ݒ�݂̂�ύX����
        // setAcToSleepValue: AC�d���ݒ���X���[�v�ɂ���ꍇ��true�A�������Ȃ��ɂ���ꍇ��false
        // isManualOperation: �蓮����ɂ��Ăяo�����ǂ��� (��ɒʒm���b�Z�[�W�p)
        private void SetLidActionForAc(bool setAcToSleepValue, bool isManualOperation = false)
        {
            string actionDescription = setAcToSleepValue ? "�X���[�v" : "�������Ȃ�";
            string powercfgArgumentsAC;
            string baseMessage;

            // ���AC�d���ݒ�݂̂�ύX����R�}���h�𐶐�
            powercfgArgumentsAC = $"/SETACVALUEINDEX SCHEME_CURRENT SUB_BUTTONS LIDACTION {(setAcToSleepValue ? 1 : 0)}";

            if (isManualOperation)
            {
                baseMessage = $"�蓮��AC�d�����̊W��������u{actionDescription}�v�ɐݒ肵�܂����B";
            }
            else // �����ݒ�̏ꍇ
            {
                PowerLineStatus currentPcPowerStatus = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus;
                string powerContextMessage = currentPcPowerStatus == PowerLineStatus.Online ? "PC��AC�d���ɐڑ����ꂽ����" : "PC���o�b�e���[�쓮�ɂȂ�������";
                baseMessage = $"{powerContextMessage}�AAC�d�����̊W��������u{actionDescription}�v�ɐݒ肵�܂����B";
            }

            try
            {
                // AC�d���ݒ�̕ύX�R�}���h�̂ݎ��s
                ProcessStartInfo procStartInfoAC = new ProcessStartInfo("powercfg.exe", powercfgArgumentsAC)
                { UseShellExecute = true, CreateNoWindow = true };
                Process.Start(procStartInfoAC)?.WaitForExit();

                // �d���v�������A�N�e�B�u�ɂ��� (�ݒ�𑦎����f�����邽��)
                ProcessStartInfo setActiveProcInfo = new ProcessStartInfo("powercfg.exe", "/SETACTIVE SCHEME_CURRENT")
                { UseShellExecute = true, CreateNoWindow = true };
                Process.Start(setActiveProcInfo)?.WaitForExit();

                Console.WriteLine(baseMessage);
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(2000, "�ݒ芮��", baseMessage, ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"�W������̐ݒ�ύX�Ɏ��s���܂����B\n�G���[: {ex.GetType().Name}";
                Console.WriteLine($"powercfg�̎��s�Ɏ��s���܂���: {ex.Message}");
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(3000, "�ݒ�G���[", errorMessage, ToolTipIcon.Error);
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
                    Console.WriteLine("�����N���ݒ�̓ǂݍ��݃G���[: �A�v���P�[�V�����p�X���擾�ł��܂���B");
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
                Console.WriteLine($"�����N���ݒ�̓ǂݍ��݂Ɏ��s: {ex.Message}");
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
                    Console.WriteLine("�����N���ݒ�̃G���[: �A�v���P�[�V�����p�X���擾�ł��܂���B");
                    if (notifyIcon1 != null)
                    {
                        notifyIcon1.ShowBalloonTip(2000, "�ݒ�G���[", "�����N���ݒ�̕ύX�Ɏ��s���܂����B\n(�A�v���P�[�V�����p�X�s��)", ToolTipIcon.Error);
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
                        Console.WriteLine("���W�X�g���L�[ 'Run' ��������܂���B");
                        if (notifyIcon1 != null)
                        {
                            notifyIcon1.ShowBalloonTip(2000, "�ݒ�G���[", "�����N���ݒ�̕ύX�Ɏ��s���܂����B\n(���W�X�g���L�[��������܂���)", ToolTipIcon.Error);
                        }
                        if (menuItemAutoStart != null) menuItemAutoStart.Checked = !autoStartEnabled;
                        return;
                    }

                    if (autoStartEnabled)
                    {
                        rk.SetValue(APP_NAME_FOR_REGISTRY, executablePath);
                        Console.WriteLine("�����N����L���ɂ��܂����B");
                    }
                    else
                    {
                        rk.DeleteValue(APP_NAME_FOR_REGISTRY, false);
                        Console.WriteLine("�����N���𖳌��ɂ��܂����B");
                    }
                }
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(1000, "�����N���ݒ�", autoStartEnabled ? "Windows�N�����̎����N����L���ɂ��܂����B" : "Windows�N�����̎����N���𖳌��ɂ��܂����B", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"�����N���ݒ�̏������݂Ɏ��s: {ex.Message}");
                if (notifyIcon1 != null)
                {
                    notifyIcon1.ShowBalloonTip(3000, "�ݒ�G���[", $"�����N���ݒ�̕ύX�Ɏ��s���܂����B\n�G���[: {ex.GetType().Name}", ToolTipIcon.Error);
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
