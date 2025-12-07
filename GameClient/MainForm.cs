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
    /// 游戏界面状态
    /// </summary>
    public enum GameScreen
    {
        Connecting,     // 连接中
        Lobby,          // 等待大厅
        LevelSelect,    // 选择关卡
        Playing,        // 游戏中
        GameOver        // 游戏结束
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
        private GameScreen _currentScreen = GameScreen.Connecting;
        private readonly object _stateLock = new object();
        
        // 大厅状态
        private bool _isReady = false;
        private bool _otherPlayerReady = false;
        private string _otherPlayerName = "";
        private int _playerCount = 0;
        
        // 渲染相关
        private GameRenderer _renderer;
        private System.Windows.Forms.Timer _renderTimer;
        private System.Threading.Timer _inputTimer;
        
        // UI控件
        private DoubleBufferedPanel _gamePanel;
        private Panel _sidePanel;
        private Label _statusLabel;
        private Label _messageLabel;
        private ListBox _chatListBox;
        private TextBox _chatTextBox;
        private Button _readyButton;
        private Button _startButton;
        
        // 输入状态
        private volatile bool _keyLeft = false;
        private volatile bool _keyRight = false;
        private volatile bool _keyJump = false;
        
        // 关卡选择
        private int _selectedLevel = 1;
        
        // 消息列表
        private List<string> _messages = new List<string>();

        public MainForm()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeComponent()
        {
            // 窗口设置
            this.Text = "🔥 Fireboy and Watergirl 💧 - 森林冰火人网络版";
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(25, 25, 35);
            this.KeyPreview = true;

            // 游戏面板
            _gamePanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 25),
            };
            _gamePanel.Paint += GamePanel_Paint;

            // 侧边面板
            _sidePanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = Color.FromArgb(35, 35, 45),
                Padding = new Padding(10)
            };

            // 状态标签
            _statusLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(295, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                Text = "连接中..."
            };

            // 消息标签
            _messageLabel = new Label
            {
                Location = new Point(10, 120),
                Size = new Size(295, 50),
                ForeColor = Color.Gold,
                Font = new Font("Microsoft YaHei", 10),
                Text = ""
            };

            // 准备按钮
            _readyButton = new Button
            {
                Location = new Point(10, 180),
                Size = new Size(140, 45),
                Text = "✋ 准备",
                BackColor = Color.FromArgb(60, 160, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                Visible = false
            };
            _readyButton.Click += ReadyButton_Click;

            // 开始按钮
            _startButton = new Button
            {
                Location = new Point(160, 180),
                Size = new Size(140, 45),
                Text = "🎮 开始游戏",
                BackColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                Visible = false,
                Enabled = false
            };
            _startButton.Click += StartButton_Click;

            // 聊天列表
            var chatLabel = new Label
            {
                Location = new Point(10, 240),
                Size = new Size(295, 22),
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 9),
                Text = "📝 消息:"
            };

            _chatListBox = new ListBox
            {
                Location = new Point(10, 265),
                Size = new Size(295, 420),
                BackColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 聊天输入
            _chatTextBox = new TextBox
            {
                Location = new Point(10, 695),
                Size = new Size(210, 28),
                BackColor = Color.FromArgb(45, 45, 55),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            _chatTextBox.KeyPress += ChatTextBox_KeyPress;

            var sendButton = new Button
            {
                Location = new Point(225, 693),
                Size = new Size(80, 30),
                Text = "发送",
                BackColor = Color.FromArgb(60, 130, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            sendButton.Click += (s, e) => SendChatMessage();

            // 添加控件到侧边面板
            _sidePanel.Controls.AddRange(new Control[] {
                _statusLabel, _messageLabel, _readyButton, _startButton,
                chatLabel, _chatListBox, _chatTextBox, sendButton
            });

            // 添加到窗口
            this.Controls.Add(_gamePanel);
            this.Controls.Add(_sidePanel);

            // 事件
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeGame()
        {
            _renderer = new GameRenderer();
            _client = new NetworkClient();
            
            // 网络事件
            _client.OnServerMessage += msg => {
                AddMessage($"[服务器] {msg}");
                ParseServerMessage(msg);
            };
            _client.OnChatMessage += (sender, msg) => AddMessage($"[{sender}] {msg}");
            _client.OnGameStart += () => {
                _currentScreen = GameScreen.Playing;
                AddMessage("🎮 游戏开始！");
            };
            _client.OnGameStateUpdate += state => {
                lock (_stateLock) { _gameState = state; }
                
                // 检查游戏结束
                if (state.GameOver)
                {
                    _currentScreen = GameScreen.GameOver;
                }
            };
            _client.OnDisconnected += () => {
                _currentScreen = GameScreen.Connecting;
                AddMessage("❌ 与服务器断开连接");
                UpdateUI();
            };
            _client.OnPlayerCountChanged += count => {
                _playerCount = count;
                UpdateUI();
            };

            // 渲染定时器 (60 FPS)
            _renderTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _renderTimer.Tick += (s, e) => _gamePanel.Invalidate();
            _renderTimer.Start();

            // 输入定时器 (60Hz)
            _inputTimer = new System.Threading.Timer(InputTimer_Callback, null, 0, 16);

            // 显示连接对话框
            ShowConnectDialog();
        }

        private void ParseServerMessage(string msg)
        {
            // 解析服务器消息更新状态
            if (msg.Contains("加入了游戏"))
            {
                _playerCount++;
                if (msg.Contains("(") && !msg.Contains(_client.PlayerName))
                {
                    int start = msg.IndexOf("玩家 ") + 3;
                    int end = msg.IndexOf(" (");
                    if (start > 2 && end > start)
                        _otherPlayerName = msg.Substring(start, end - start);
                }
            }
            else if (msg.Contains("离开了游戏"))
            {
                _playerCount = Math.Max(1, _playerCount - 1);
                _otherPlayerReady = false;
                _otherPlayerName = "";
            }
            else if (msg.Contains("已准备"))
            {
                if (!msg.Contains(_client.PlayerName))
                    _otherPlayerReady = true;
            }
            else if (msg.Contains("取消准备"))
            {
                if (!msg.Contains(_client.PlayerName))
                    _otherPlayerReady = false;
            }
            
            UpdateUI();
        }

        private void ShowConnectDialog()
        {
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
                _currentScreen = GameScreen.Lobby;
                _playerCount = 1;
                string playerTypeStr = _client.PlayerType == PlayerType.Ice ? "💧 Watergirl" : "🔥 Fireboy";
                AddMessage($"✅ 连接成功！你是 {playerTypeStr}");
                UpdateUI();
            }
            else
            {
                UpdateStatus("连接失败", Color.Red);
                MessageBox.Show("无法连接到服务器，请确保服务器已启动。", "连接失败", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowConnectDialog();
            }
        }

        private void UpdateUI()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateUI));
                return;
            }

            string playerType = _client.IsConnected ? 
                (_client.PlayerType == PlayerType.Ice ? "💧 Watergirl" : "🔥 Fireboy") : "";

            switch (_currentScreen)
            {
                case GameScreen.Connecting:
                    _readyButton.Visible = false;
                    _startButton.Visible = false;
                    UpdateStatus("未连接\n请连接服务器", Color.Red);
                    break;

                case GameScreen.Lobby:
                    _readyButton.Visible = true;
                    _startButton.Visible = true;
                    _readyButton.Text = _isReady ? "❌ 取消准备" : "✋ 准备";
                    _readyButton.BackColor = _isReady ? Color.FromArgb(180, 60, 60) : Color.FromArgb(60, 160, 60);
                    _startButton.Enabled = _isReady && _otherPlayerReady && _playerCount >= 2;
                    
                    string status = $"你是: {playerType}\n";
                    status += $"房间人数: {_playerCount}/2\n\n";
                    status += $"你: {(_isReady ? "✅ 已准备" : "⏳ 未准备")}\n";
                    if (_playerCount >= 2)
                        status += $"对方: {(_otherPlayerReady ? "✅ 已准备" : "⏳ 未准备")}";
                    else
                        status += "等待另一位玩家加入...";
                    
                    UpdateStatus(status, Color.LightGreen);
                    break;

                case GameScreen.LevelSelect:
                    _readyButton.Visible = false;
                    _startButton.Visible = false;
                    UpdateStatus($"你是: {playerType}\n选择关卡: 按1-5\n按Enter确认", Color.Cyan);
                    break;

                case GameScreen.Playing:
                    _readyButton.Visible = false;
                    _startButton.Visible = false;
                    GameState state;
                    lock (_stateLock) { state = _gameState; }
                    if (state != null)
                    {
                        string iceStatus = state.IcePlayer?.IsAlive == true ? 
                            (state.IcePlayer.ReachedExit ? "✅" : "🏃") : "💀";
                        string fireStatus = state.FirePlayer?.IsAlive == true ? 
                            (state.FirePlayer.ReachedExit ? "✅" : "🏃") : "💀";
                        
                        UpdateStatus($"你是: {playerType}\n" +
                            $"关卡: {state.CurrentLevel}/{LevelGenerator.TotalLevels}\n\n" +
                            $"💧 Watergirl: {iceStatus} 💎{state.IcePlayer?.GemsCollected ?? 0}\n" +
                            $"🔥 Fireboy: {fireStatus} 💎{state.FirePlayer?.GemsCollected ?? 0}",
                            Color.LightGreen);
                    }
                    break;

                case GameScreen.GameOver:
                    _readyButton.Visible = true;
                    _readyButton.Text = "🔄 再来一局";
                    _readyButton.BackColor = Color.FromArgb(200, 120, 60);
                    _startButton.Visible = false;
                    break;
            }
        }

        private void InputTimer_Callback(object state)
        {
            if (!_client.IsConnected || _currentScreen != GameScreen.Playing) return;

            PlayerAction action = PlayerAction.None;
            if (_keyLeft) action |= PlayerAction.MoveLeft;
            if (_keyRight) action |= PlayerAction.MoveRight;
            if (_keyJump) action |= PlayerAction.Jump;

            _client.SendInput(action);
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var size = _gamePanel.ClientSize;

            switch (_currentScreen)
            {
                case GameScreen.Connecting:
                    _renderer.RenderWaitingScreen(e.Graphics, size, "正在连接服务器...");
                    break;

                case GameScreen.Lobby:
                    _renderer.RenderLobby(e.Graphics, size, _playerCount, _isReady, _otherPlayerReady, 
                        _client.PlayerType, _otherPlayerName);
                    break;

                case GameScreen.LevelSelect:
                    _renderer.RenderMenu(e.Graphics, size, _selectedLevel, true);
                    break;

                case GameScreen.Playing:
                case GameScreen.GameOver:
                    GameState state;
                    lock (_stateLock) { state = _gameState; }
                    if (state != null)
                    {
                        _renderer.Render(e.Graphics, state, size, _client.PlayerType);
                    }
                    break;
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // 游戏中的移动控制
            if (_currentScreen == GameScreen.Playing)
            {
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
                        // 重新开始
                        _client.RequestRestart();
                        AddMessage("🔄 请求重新开始...");
                        break;
                    case Keys.Escape:
                        // 返回大厅
                        _currentScreen = GameScreen.Lobby;
                        _isReady = false;
                        _otherPlayerReady = false;
                        UpdateUI();
                        break;
                }
            }
            // 关卡选择
            else if (_currentScreen == GameScreen.LevelSelect)
            {
                switch (e.KeyCode)
                {
                    case Keys.D1: case Keys.NumPad1: _selectedLevel = 1; break;
                    case Keys.D2: case Keys.NumPad2: _selectedLevel = 2; break;
                    case Keys.D3: case Keys.NumPad3: _selectedLevel = 3; break;
                    case Keys.D4: case Keys.NumPad4: _selectedLevel = 4; break;
                    case Keys.D5: case Keys.NumPad5: _selectedLevel = 5; break;
                    case Keys.Enter:
                        _client.RequestLevel(_selectedLevel);
                        break;
                    case Keys.Escape:
                        _currentScreen = GameScreen.Lobby;
                        UpdateUI();
                        break;
                }
            }
            // 游戏结束
            else if (_currentScreen == GameScreen.GameOver)
            {
                if (e.KeyCode == Keys.R || e.KeyCode == Keys.Enter)
                {
                    _client.RequestRestart();
                    _currentScreen = GameScreen.Playing;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    _currentScreen = GameScreen.Lobby;
                    _isReady = false;
                    UpdateUI();
                }
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
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

        private void ReadyButton_Click(object sender, EventArgs e)
        {
            if (_currentScreen == GameScreen.GameOver)
            {
                // 再来一局
                _currentScreen = GameScreen.Lobby;
                _isReady = false;
                _otherPlayerReady = false;
                UpdateUI();
                return;
            }

            _isReady = !_isReady;
            _client.SendReady(_isReady);
            AddMessage(_isReady ? "✅ 你已准备" : "❌ 取消准备");
            UpdateUI();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (_isReady && _otherPlayerReady && _playerCount >= 2)
            {
                _currentScreen = GameScreen.LevelSelect;
                UpdateUI();
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

        private void SendChatMessage()
        {
            string msg = _chatTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(msg))
            {
                _client.SendChat(msg);
                _chatTextBox.Clear();
            }
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
                _chatListBox.Items.RemoveAt(0);
        }
    }
}
