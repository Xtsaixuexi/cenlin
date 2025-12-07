using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using FireboyAndWatergirl.Shared;

namespace FireboyAndWatergirl.GameClient
{
    /// <summary>
    /// 双缓冲面板 - 避免闪烁
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                          ControlStyles.UserPaint | 
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }
    }

    /// <summary>
    /// 游戏主窗口
    /// </summary>
    public class MainForm : Form
    {
        // 网络客户端
        private NetworkClient _client;
        
        // 游戏状态
        private GameState _gameState;
        private bool _gameStarted = false;
        private readonly object _stateLock = new object();
        
        // 渲染相关
        private GameRenderer _renderer;
        private System.Windows.Forms.Timer _renderTimer;
        private System.Threading.Timer _inputTimer;
        
        // UI控件
        private DoubleBufferedPanel _gamePanel;
        private Panel _infoPanel;
        private Label _statusLabel;
        private Label _messageLabel;
        private ListBox _chatListBox;
        private TextBox _chatTextBox;
        private Button _sendButton;
        private Button _restartButton;
        private Button _menuButton;
        
        // 输入状态 - 使用volatile确保线程安全
        private volatile bool _keyLeft = false;
        private volatile bool _keyRight = false;
        private volatile bool _keyJump = false;
        
        // 消息列表
        private List<string> _messages = new List<string>();

        // 菜单相关
        private bool _inMenu = true;
        private int _selectedLevel = 1;

        public MainForm()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeComponent()
        {
            // 窗口设置
            this.Text = "🔥 Fireboy and Watergirl 💧 - 森林冰火人网络版";
            this.Size = new Size(1280, 850);
            this.MinimumSize = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(25, 25, 35);
            this.KeyPreview = true;

            // 游戏面板
            _gamePanel = new DoubleBufferedPanel
            {
                Location = new Point(10, 10),
                Size = new Size(900, 580),
                BackColor = Color.FromArgb(15, 15, 25),
                BorderStyle = BorderStyle.FixedSingle
            };
            _gamePanel.Paint += GamePanel_Paint;

            // 信息面板
            _infoPanel = new Panel
            {
                Location = new Point(920, 10),
                Size = new Size(340, 580),
                BackColor = Color.FromArgb(35, 35, 45),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 状态标签
            _statusLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(320, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                Text = "等待连接..."
            };

            // 消息标签
            _messageLabel = new Label
            {
                Location = new Point(10, 100),
                Size = new Size(320, 45),
                ForeColor = Color.Gold,
                Font = new Font("Microsoft YaHei", 10),
                Text = ""
            };

            // 聊天列表
            var chatLabel = new Label
            {
                Location = new Point(10, 155),
                Size = new Size(320, 22),
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 9),
                Text = "📝 消息记录:"
            };

            _chatListBox = new ListBox
            {
                Location = new Point(10, 180),
                Size = new Size(320, 240),
                BackColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 聊天输入
            _chatTextBox = new TextBox
            {
                Location = new Point(10, 430),
                Size = new Size(230, 28),
                BackColor = Color.FromArgb(45, 45, 55),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            _chatTextBox.KeyPress += ChatTextBox_KeyPress;

            _sendButton = new Button
            {
                Location = new Point(245, 428),
                Size = new Size(85, 30),
                Text = "发送",
                BackColor = Color.FromArgb(60, 130, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9)
            };
            _sendButton.Click += SendButton_Click;

            _restartButton = new Button
            {
                Location = new Point(10, 475),
                Size = new Size(155, 40),
                Text = "🔄 重新开始 (R)",
                BackColor = Color.FromArgb(200, 80, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
            };
            _restartButton.Click += RestartButton_Click;

            _menuButton = new Button
            {
                Location = new Point(175, 475),
                Size = new Size(155, 40),
                Text = "📋 返回菜单 (M)",
                BackColor = Color.FromArgb(80, 80, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
            };
            _menuButton.Click += MenuButton_Click;

            // 控制说明面板
            var controlPanel = new Panel
            {
                Location = new Point(10, 600),
                Size = new Size(1250, 200),
                BackColor = Color.FromArgb(35, 35, 45),
                BorderStyle = BorderStyle.FixedSingle
            };

            var controlTitle = new Label
            {
                Location = new Point(15, 10),
                Size = new Size(200, 28),
                ForeColor = Color.Cyan,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                Text = "🎮 游戏控制"
            };

            var controlText = new Label
            {
                Location = new Point(15, 45),
                Size = new Size(400, 145),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                Text = "← / A  向左移动\n" +
                       "→ / D  向右移动\n" +
                       "↑ / W / 空格  跳跃\n" +
                       "R  重新开始\n" +
                       "M  返回菜单"
            };

            var rulesTitle = new Label
            {
                Location = new Point(450, 10),
                Size = new Size(200, 28),
                ForeColor = Color.Orange,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                Text = "📜 游戏规则"
            };

            var rulesText = new Label
            {
                Location = new Point(450, 45),
                Size = new Size(400, 145),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                Text = "💧 Watergirl (蓝): 可通过水/冰区域，怕火\n" +
                       "🔥 Fireboy (红): 可通过火区域，怕水/冰\n" +
                       "☠️ 绿色毒水: 两者都会死亡！\n" +
                       "💎 收集对应颜色的宝石\n" +
                       "🚪 两人都到达出口即可通关"
            };

            var tipsTitle = new Label
            {
                Location = new Point(880, 10),
                Size = new Size(200, 28),
                ForeColor = Color.LightGreen,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                Text = "💡 提示"
            };

            var tipsText = new Label
            {
                Location = new Point(880, 45),
                Size = new Size(350, 145),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                Text = "• 两个玩家需要配合\n" +
                       "• 可以同时按多个方向键\n" +
                       "• 共有5个关卡等你挑战\n" +
                       "• 通关后自动进入下一关"
            };

            // 添加控件
            _infoPanel.Controls.AddRange(new Control[] {
                _statusLabel, _messageLabel, chatLabel, _chatListBox,
                _chatTextBox, _sendButton, _restartButton, _menuButton
            });

            controlPanel.Controls.AddRange(new Control[] {
                controlTitle, controlText, rulesTitle, rulesText, tipsTitle, tipsText
            });

            this.Controls.AddRange(new Control[] {
                _gamePanel, _infoPanel, controlPanel
            });

            // 事件
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;
        }

        private void InitializeGame()
        {
            // 创建渲染器
            _renderer = new GameRenderer();

            // 创建网络客户端
            _client = new NetworkClient();
            _client.OnServerMessage += msg => AddMessage($"[服务器] {msg}");
            _client.OnChatMessage += (sender, msg) => AddMessage($"[{sender}] {msg}");
            _client.OnGameStart += () => 
            {
                _gameStarted = true;
                _inMenu = false;
                AddMessage("🎮 游戏开始！");
            };
            _client.OnGameStateUpdate += state =>
            {
                lock (_stateLock)
                {
                    _gameState = state;
                }
            };
            _client.OnDisconnected += () =>
            {
                _gameStarted = false;
                AddMessage("❌ 与服务器断开连接");
                UpdateStatus("已断开连接", Color.Red);
            };

            // 渲染定时器 (60 FPS)
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 16;
            _renderTimer.Tick += RenderTimer_Tick;
            _renderTimer.Start();

            // 输入发送定时器 (使用高精度定时器，60Hz)
            _inputTimer = new System.Threading.Timer(InputTimer_Callback, null, 0, 16);

            // 显示连接对话框
            ShowConnectDialog();
        }

        private void ShowConnectDialog()
        {
            _inMenu = true;
            using (var dialog = new ConnectDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ConnectToServer(dialog.Host, dialog.Port, dialog.PlayerName, dialog.PreferredType);
                }
                else
                {
                    this.Close();
                }
            }
        }

        private async void ConnectToServer(string host, int port, string playerName, PlayerType preferredType)
        {
            UpdateStatus($"正在连接 {host}:{port}...", Color.Yellow);
            
            bool success = await _client.ConnectAsync(host, port, playerName, preferredType);
            
            if (success)
            {
                string playerTypeStr = _client.PlayerType == PlayerType.Ice ? "💧 Watergirl" : "🔥 Fireboy";
                UpdateStatus($"已连接！你是 {playerTypeStr}\n等待另一位玩家...", Color.LightGreen);
                AddMessage($"✅ 连接成功！你是 {playerTypeStr}");
            }
            else
            {
                UpdateStatus("连接失败", Color.Red);
                MessageBox.Show("无法连接到服务器，请确保服务器已启动。", "连接失败", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowConnectDialog();
            }
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            // 刷新游戏画面
            _gamePanel.Invalidate();

            // 更新状态显示
            GameState state;
            lock (_stateLock)
            {
                state = _gameState;
            }

            if (state != null && _gameStarted)
            {
                string playerTypeStr = _client.PlayerType == PlayerType.Ice ? "💧 Watergirl" : "🔥 Fireboy";
                string iceStatus = state.IcePlayer?.IsAlive == true ? 
                    (state.IcePlayer.ReachedExit ? "✅到达" : "🏃") : "💀";
                string fireStatus = state.FirePlayer?.IsAlive == true ? 
                    (state.FirePlayer.ReachedExit ? "✅到达" : "🏃") : "💀";

                UpdateStatus($"你是: {playerTypeStr}\n" +
                           $"关卡: {state.CurrentLevel} / {LevelGenerator.TotalLevels}\n" +
                           $"💧 Watergirl: {iceStatus}  💎{state.IcePlayer?.GemsCollected ?? 0}\n" +
                           $"🔥 Fireboy: {fireStatus}  💎{state.FirePlayer?.GemsCollected ?? 0}",
                           Color.LightGreen);

                if (!string.IsNullOrEmpty(state.Message))
                {
                    UpdateMessage(state.Message, state.Victory ? Color.Gold : 
                        (state.GameOver ? Color.Red : Color.Yellow));
                }
            }
        }

        private void InputTimer_Callback(object state)
        {
            if (!_client.IsConnected || !_gameStarted || _inMenu) return;

            PlayerAction action = PlayerAction.None;

            // 读取按键状态
            if (_keyLeft) action |= PlayerAction.MoveLeft;
            if (_keyRight) action |= PlayerAction.MoveRight;
            if (_keyJump) action |= PlayerAction.Jump;

            // 始终发送输入（包括None，让服务器知道玩家停止了）
            _client.SendInput(action);
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_inMenu)
            {
                // 显示菜单
                _renderer.RenderMenu(e.Graphics, _gamePanel.ClientSize, _selectedLevel, _client.IsConnected);
            }
            else
            {
                GameState state;
                lock (_stateLock)
                {
                    state = _gameState;
                }

                if (state != null && _gameStarted)
                {
                    _renderer.Render(e.Graphics, state, _gamePanel.ClientSize, _client.PlayerType);
                }
                else
                {
                    _renderer.RenderWaitingScreen(e.Graphics, _gamePanel.ClientSize, 
                        _client.IsConnected ? "等待另一位玩家加入..." : "未连接");
                }
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // 更新按键状态
            switch (e.KeyCode)
            {
                case Keys.A:
                case Keys.Left:
                    _keyLeft = true;
                    break;
                case Keys.D:
                case Keys.Right:
                    _keyRight = true;
                    break;
                case Keys.W:
                case Keys.Up:
                case Keys.Space:
                    _keyJump = true;
                    break;
                case Keys.R:
                    _client.RequestRestart();
                    AddMessage("🔄 请求重新开始...");
                    break;
                case Keys.M:
                    _inMenu = !_inMenu;
                    break;
                case Keys.D1:
                case Keys.NumPad1:
                    if (_inMenu) _selectedLevel = 1;
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    if (_inMenu) _selectedLevel = 2;
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    if (_inMenu) _selectedLevel = 3;
                    break;
                case Keys.D4:
                case Keys.NumPad4:
                    if (_inMenu) _selectedLevel = 4;
                    break;
                case Keys.D5:
                case Keys.NumPad5:
                    if (_inMenu) _selectedLevel = 5;
                    break;
                case Keys.Enter:
                    if (_inMenu && _client.IsConnected)
                    {
                        // 请求开始选定的关卡
                        _client.RequestLevel(_selectedLevel);
                        _inMenu = false;
                    }
                    break;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            // 释放按键
            switch (e.KeyCode)
            {
                case Keys.A:
                case Keys.Left:
                    _keyLeft = false;
                    break;
                case Keys.D:
                case Keys.Right:
                    _keyRight = false;
                    break;
                case Keys.W:
                case Keys.Up:
                case Keys.Space:
                    _keyJump = false;
                    break;
            }
        }

        private void ChatTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                SendChatMessage();
                e.Handled = true;
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            SendChatMessage();
        }

        private void RestartButton_Click(object sender, EventArgs e)
        {
            _client.RequestRestart();
            AddMessage("🔄 请求重新开始...");
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            _inMenu = !_inMenu;
        }

        private void SendChatMessage()
        {
            string msg = _chatTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(msg))
            {
                _client.SendChat(msg);
                _chatTextBox.Clear();
            }
            // 让焦点回到主窗口以便接收按键
            this.Focus();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _renderTimer?.Stop();
            _renderTimer?.Dispose();
            _inputTimer?.Dispose();
            _client?.Disconnect();
            _renderer?.Dispose();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // 自适应布局
            int totalWidth = this.ClientSize.Width;
            int totalHeight = this.ClientSize.Height;

            // 计算游戏面板大小（保持宽高比）
            int infoPanelWidth = 340;
            int controlPanelHeight = 200;
            int padding = 10;

            int gamePanelWidth = Math.Max(500, totalWidth - infoPanelWidth - padding * 3);
            int gamePanelHeight = Math.Max(350, totalHeight - controlPanelHeight - padding * 3);

            _gamePanel.Location = new Point(padding, padding);
            _gamePanel.Size = new Size(gamePanelWidth, gamePanelHeight);

            _infoPanel.Location = new Point(gamePanelWidth + padding * 2, padding);
            _infoPanel.Size = new Size(Math.Min(infoPanelWidth, totalWidth - gamePanelWidth - padding * 3), gamePanelHeight);

            // 控制面板
            var controlPanel = this.Controls[2] as Panel;
            if (controlPanel != null)
            {
                controlPanel.Location = new Point(padding, gamePanelHeight + padding * 2);
                controlPanel.Size = new Size(totalWidth - padding * 2, Math.Min(controlPanelHeight, totalHeight - gamePanelHeight - padding * 3));
            }
        }

        private void UpdateStatus(string text, Color color)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateStatus(text, color)));
                return;
            }
            _statusLabel.Text = text;
            _statusLabel.ForeColor = color;
        }

        private void UpdateMessage(string text, Color color)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateMessage(text, color)));
                return;
            }
            _messageLabel.Text = text;
            _messageLabel.ForeColor = color;
        }

        private void AddMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => AddMessage(message)));
                return;
            }

            string timeMsg = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _messages.Add(timeMsg);
            _chatListBox.Items.Add(timeMsg);
            
            if (_chatListBox.Items.Count > 0)
                _chatListBox.TopIndex = _chatListBox.Items.Count - 1;
            
            while (_chatListBox.Items.Count > 100)
            {
                _chatListBox.Items.RemoveAt(0);
            }
        }
    }
}
