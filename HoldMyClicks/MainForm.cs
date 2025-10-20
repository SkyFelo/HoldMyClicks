using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace HoldMyClicks
{
    public partial class MainForm : Form
    {
        private Button btnToggle;
        private ComboBox cbButton;
        private CheckBox chkHotkey;
        private Label lblStatus;
        private PictureBox btnEN;
        private PictureBox btnRU;
        private bool isHolding = false;

        private const int HOTKEY_ID = 0x100;
        private const uint VK_F6 = 0x75;
        private const uint MOD_NONE = 0x0000;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainForm()
        {
            InitializeComponents();
            ApplyLanguage(Properties.Settings.Default.Language);
            cbButton.SelectedIndex = Properties.Settings.Default.ButtonIndex;
        }

        private void InitializeComponents()
        {
            Text = "Mouse Hold";
            Width = 420;
            Height = 200;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            btnToggle = new Button { Left = 20, Top = 20, Width = 120 };
            btnToggle.Click += BtnToggle_Click;

            cbButton = new ComboBox { Left = 160, Top = 20, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cbButton.Items.AddRange(new[] { "Left button", "Right button" });
            cbButton.SelectedIndexChanged += (s, e) =>
            {
                Properties.Settings.Default.ButtonIndex = cbButton.SelectedIndex;
                Properties.Settings.Default.Save();
            };

            chkHotkey = new CheckBox { Left = 20, Top = 60, Width = 360 };
            chkHotkey.CheckedChanged += ChkHotkey_CheckedChanged;

            lblStatus = new Label { Left = 20, Top = 100, Width = 360, Height = 30 };

            btnEN = new PictureBox
            {
                Left = 330,
                Top = 20,
                Width = 32,
                Height = 20,
                Image = Properties.Resources.flag_en,
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            btnEN.Click += (s, e) => ChangeLanguage("en");

            btnRU = new PictureBox
            {
                Left = 370,
                Top = 20,
                Width = 32,
                Height = 20,
                Image = Properties.Resources.flag_ru,
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            btnRU.Click += (s, e) => ChangeLanguage("ru");

            Controls.Add(btnToggle);
            Controls.Add(cbButton);
            Controls.Add(chkHotkey);
            Controls.Add(lblStatus);
            Controls.Add(btnEN);
            Controls.Add(btnRU);

            FormClosing += MainForm_FormClosing;
        }

        private void ChangeLanguage(string lang)
        {
            Properties.Settings.Default.Language = lang;
            Properties.Settings.Default.Save();
            ApplyLanguage(lang);
        }

        private void ApplyLanguage(string lang)
        {
            cbButton.Items.Clear();
            if (lang == "ru")
            {
                Text = "Mouse Hold — симулятор зажатия мыши";
                btnToggle.Text = isHolding ? "Остановить (Отпустить)" : "Начать (Зажать)";
                cbButton.Items.AddRange(new[] { "Левая кнопка", "Правая кнопка" });
                chkHotkey.Text = "Включить глобальную клавишу (F6)";
                lblStatus.Text = isHolding ? "Статус: кнопка зажата" : "Статус: кнопка отпущена";
            }
            else
            {
                Text = "Mouse Hold — mouse click simulator";
                btnToggle.Text = isHolding ? "Stop (Release)" : "Start (Hold)";
                cbButton.Items.AddRange(new[] { "Left button", "Right button" });
                chkHotkey.Text = "Enable global hotkey (F6)";
                lblStatus.Text = isHolding ? "Status: button held" : "Status: button released";
            }

            if (Properties.Settings.Default.ButtonIndex < cbButton.Items.Count)
                cbButton.SelectedIndex = Properties.Settings.Default.ButtonIndex;
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {
            ToggleHold();
        }

        private void ChkHotkey_CheckedChanged(object sender, EventArgs e)
        {
            if (chkHotkey.Checked)
            {
                var ok = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_NONE, VK_F6);
                if (!ok)
                {
                    MessageBox.Show(
                        Properties.Settings.Default.Language == "ru"
                        ? "Не удалось зарегистрировать клавишу F6."
                        : "Failed to register hotkey F6.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkHotkey.Checked = false;
                }
            }
            else UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        private void ToggleHold()
        {
            if (!isHolding)
            {
                DoMouseDown();
                isHolding = true;
            }
            else
            {
                DoMouseUp();
                isHolding = false;
            }
            ApplyLanguage(Properties.Settings.Default.Language);
        }

        private void DoMouseDown()
        {
            switch (cbButton.SelectedIndex)
            {
                case 0: mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero); break;
                case 1: mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero); break;
            }
        }

        private void DoMouseUp()
        {
            switch (cbButton.SelectedIndex)
            {
                case 0: mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero); break;
                case 1: mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero); break;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isHolding)
            {
                DoMouseUp();
                isHolding = false;
            }
            try { UnregisterHotKey(this.Handle, HOTKEY_ID); } catch { }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
                ToggleHold();
            base.WndProc(ref m);
        }
    }
}
