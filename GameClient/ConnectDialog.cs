using System;
using System.Drawing;
using System.Windows.Forms;
using FireboyAndWatergirl.Shared;

namespace FireboyAndWatergirl.GameClient
{
    /// <summary>
    /// 连接对话框
    /// </summary>
    public class ConnectDialog : Form
    {
        private TextBox _hostTextBox;
        private TextBox _portTextBox;
        private TextBox _nameTextBox;
        private RadioButton _iceRadio;
        private RadioButton _fireRadio;
        private Button _connectButton;
        private Button _cancelButton;

        public string Host => _hostTextBox.Text.Trim();
        public int Port => int.TryParse(_portTextBox.Text, out int p) ? p : GameConfig.DefaultPort;
        public string PlayerName => _nameTextBox.Text.Trim();
        public PlayerType PreferredType => _iceRadio.Checked ? PlayerType.Ice : PlayerType.Fire;

        public ConnectDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "连接到服务器";
            this.Size = new Size(400, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 55);

            // 标题
            var titleLabel = new Label
            {
                Text = "❄ 森林冰火人 🔥",
                Location = new Point(20, 20),
                Size = new Size(360, 35),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 服务器地址
            var hostLabel = new Label
            {
                Text = "服务器地址:",
                Location = new Point(30, 75),
                Size = new Size(100, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 10)
            };

            _hostTextBox = new TextBox
            {
                Text = GameConfig.DefaultHost,
                Location = new Point(140, 72),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                Font = new Font("Consolas", 11),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 端口
            var portLabel = new Label
            {
                Text = "端口:",
                Location = new Point(30, 115),
                Size = new Size(100, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 10)
            };

            _portTextBox = new TextBox
            {
                Text = GameConfig.DefaultPort.ToString(),
                Location = new Point(140, 112),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                Font = new Font("Consolas", 11),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 玩家名称
            var nameLabel = new Label
            {
                Text = "你的名字:",
                Location = new Point(30, 155),
                Size = new Size(100, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 10)
            };

            _nameTextBox = new TextBox
            {
                Text = $"Player{new Random().Next(1000, 9999)}",
                Location = new Point(140, 152),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 角色选择
            var roleLabel = new Label
            {
                Text = "选择角色:",
                Location = new Point(30, 195),
                Size = new Size(100, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 10)
            };

            _iceRadio = new RadioButton
            {
                Text = "❄ 冰人 (Ice)",
                Location = new Point(140, 195),
                Size = new Size(120, 30),
                ForeColor = Color.Cyan,
                Font = new Font("Microsoft YaHei", 10),
                Checked = true
            };

            _fireRadio = new RadioButton
            {
                Text = "🔥 火人 (Fire)",
                Location = new Point(270, 195),
                Size = new Size(120, 30),
                ForeColor = Color.OrangeRed,
                Font = new Font("Microsoft YaHei", 10)
            };

            // 提示信息
            var hintLabel = new Label
            {
                Text = "提示: 先启动服务器，再运行客户端连接",
                Location = new Point(30, 240),
                Size = new Size(340, 25),
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei", 9)
            };

            // 按钮
            _connectButton = new Button
            {
                Text = "连接",
                Location = new Point(80, 285),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(60, 140, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };

            _cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(200, 285),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(100, 100, 110),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 11),
                DialogResult = DialogResult.Cancel
            };

            // 添加控件
            this.Controls.AddRange(new Control[]
            {
                titleLabel, hostLabel, _hostTextBox,
                portLabel, _portTextBox,
                nameLabel, _nameTextBox,
                roleLabel, _iceRadio, _fireRadio,
                hintLabel, _connectButton, _cancelButton
            });

            this.AcceptButton = _connectButton;
            this.CancelButton = _cancelButton;
        }
    }
}

