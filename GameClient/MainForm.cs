using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
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
        
        // 大厅状态 - 从服务器同步
        private List<LobbyPlayerInfo> _lobbyPlayers = new List<LobbyPlayerInfo>();
        private int _playerCount = 0;
        
        // 渲染相关
        private GameRenderer _renderer;
        private System.Windows.Forms.Timer _renderTimer;
        private System.Threading.Timer _inputTimer;
        
        // UI控件
        private DoubleBufferedPanel _gamePanel;
        private Panel _sidePanel;
        private Label _statusLabel;
        private ListBox _chatListBox;
        private TextBox _chatTextBox;
        private Button _readyButton;
        private Button _startButton;
        private Button _sendButton;
        private Label _chatLabel;
        private Label _rulesLabel;
        
        // 输入状态
        private volatile bool _keyLeft = false;
        private volatile bool _keyRight = false;
        private volatile bool _keyJump = false;
        
        // 关卡选择
        private int _selectedLevel = 1;
        
        // 消息列表
        private List<string> _messages = new List<string>();

        // 自己的准备状态 - 本地维护，不受服务器影响
        private bool _myReady = false;

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
            this.MinimumSize = new Size(950, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(25, 25, 35);
            this.KeyPreview = true;

            // 游戏面板
            _gamePanel = new DoubleBufferedPanel
            {
                BackColor = Color.FromArgb(15, 15, 25),
            };
            _gamePanel.Paint += GamePanel_Paint;
            _gamePanel.MouseClick += GamePanel_MouseClick;

            // 侧边面板
            _sidePanel = new Panel
            {
                BackColor = Color.FromArgb(35, 35, 45),
            };

            // 状态标签
            _statusLabel = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Text = "连接中...",
                AutoSize = false
            };

            // 游戏规则标签
            _rulesLabel = new Label
            {
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 9),
                Text = "📖 游戏规则:\n" +
                       "• WASD/方向键 移动跳跃\n" +
                       "• 💧冰人躲避火焰\n" +
                       "• 🔥火人躲避水池\n" +
                       "• 收集宝石到达出口",
                AutoSize = false
            };

            // 准备按钮
            _readyButton = new Button
            {
                Text = "✋ 准备",
                BackColor = Color.FromArgb(60, 160, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                Visible = false
            };
            _readyButton.FlatAppearance.BorderSize = 0;
            _readyButton.Click += ReadyButton_Click;

            // 开始按钮
            _startButton = new Button
            {
                Text = "🎮 开始游戏",
                BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                Visible = false,
                Enabled = false
            };
            _startButton.FlatAppearance.BorderSize = 0;
            _startButton.Click += StartButton_Click;

            // 聊天列表
            _chatLabel = new Label
            {
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 9),
                Text = "📝 消息:",
                AutoSize = false
            };

            _chatListBox = new ListBox
            {
                BackColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };

            // 聊天输入
            _chatTextBox = new TextBox
            {
                BackColor = Color.FromArgb(45, 45, 55),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            _chatTextBox.KeyPress += ChatTextBox_KeyPress;
            _chatTextBox.Enter += (s, e) => { /* 获得焦点时不做特殊处理 */ };

            _sendButton = new Button
            {
                Text = "发送",
                BackColor = Color.FromArgb(60, 130, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9)
            };
            _sendButton.FlatAppearance.BorderSize = 0;
            _sendButton.Click += (s, e) => SendChatMessage();

            // 添加控件到侧边面板
            _sidePanel.Controls.AddRange(new Control[] {
                _statusLabel, _rulesLabel, _readyButton, _startButton,
                _chatLabel, _chatListBox, _chatTextBox, _sendButton
            });

            // 添加到窗口
            this.Controls.Add(_gamePanel);
            this.Controls.Add(_sidePanel);

            // 事件 - 注意：只处理游戏面板上的按键
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            if (this.ClientSize.Width < 100 || this.ClientSize.Height < 100) return;

            int sidePanelWidth = Math.Max(260, Math.Min(320, this.ClientSize.Width / 4));
            int gamePanelWidth = this.ClientSize.Width - sidePanelWidth;
            int height = this.ClientSize.Height;

            // 游戏面板
            _gamePanel.SetBounds(0, 0, gamePanelWidth, height);
            
            // 侧边面板
            _sidePanel.SetBounds(gamePanelWidth, 0, sidePanelWidth, height);

            // 侧边面板内部布局
            int padding = 8;
            int controlWidth = sidePanelWidth - padding * 2;
            int y = padding;

            // 状态标签
            _statusLabel.SetBounds(padding, y, controlWidth, 90);
            y += 95;

            // 游戏规则
            _rulesLabel.SetBounds(padding, y, controlWidth, 85);
            y += 90;

            // 按钮区域
            int buttonWidth = (controlWidth - 8) / 2;
            _readyButton.SetBounds(padding, y, buttonWidth, 40);
            _startButton.SetBounds(padding + buttonWidth + 8, y, buttonWidth, 40);
            y += 48;

            // 聊天标签
            _chatLabel.SetBounds(padding, y, controlWidth, 20);
            y += 22;

            // 聊天列表 - 自适应剩余高度
            int chatListHeight = height - y - 50;
            _chatListBox.SetBounds(padding, y, controlWidth, Math.Max(80, chatListHeight));
            y += Math.Max(80, chatListHeight) + 4;

            // 聊天输入区
            int inputWidth = controlWidth - 55;
            _chatTextBox.SetBounds(padding, y, inputWidth, 26);
            _sendButton.SetBounds(padding + inputWidth + 4, y - 1, 50, 28);
        }

        private void InitializeGame()
        {
            _renderer = new GameRenderer();
            _client = new NetworkClient();
            
            // 网络事件
            _client.OnServerMessage += msg => {
                AddMessage($"[服务器] {msg}");
            };
            _client.OnChatMessage += (sender, msg) => AddMessage($"[{sender}] {msg}");
            _client.OnGameStart += () => {
                _currentScreen = GameScreen.Playing;
                AddMessage("🎮 游戏开始！");
                // 游戏开始后，重置准备状态
                _myReady = false;
            };
            _client.OnGameStateUpdate += state => {
                lock (_stateLock) { _gameState = state; }
                
                if (state.GameOver)
                {
                    _currentScreen = GameScreen.GameOver;
                }
            };
            _client.OnDisconnected += () => {
                _currentScreen = GameScreen.Connecting;
                _lobbyPlayers.Clear();
                _playerCount = 0;
                _myReady = false;
                AddMessage("❌ 与服务器断开连接");
                UpdateUI();
            };
            _client.OnLobbyStatus += lobbyStatus => {
                // 只更新玩家列表，不覆盖本地的准备状态
                _lobbyPlayers = lobbyStatus.Players ?? new List<LobbyPlayerInfo>();
                _playerCount = lobbyStatus.PlayerCount;
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
                _myReady = false; // 确保初始状态为未准备
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

        /// <summary>
        /// 检查对方是否准备 - 从服务器同步的数据中获取
        /// </summary>
        private bool IsOtherPlayerReady()
        {
            if (_lobbyPlayers == null || _lobbyPlayers.Count < 2) 
                return false;
            
            // 找到不是自己的玩家
            string myId = _client?.PlayerId;
            if (string.IsNullOrEmpty(myId)) 
                return false;

            var otherPlayer = _lobbyPlayers.FirstOrDefault(p => 
                !string.IsNullOrEmpty(p.Id) && p.Id != myId);
            
            return otherPlayer?.IsReady ?? false;
        }

        /// <summary>
        /// 获取对方玩家名称
        /// </summary>
        private string GetOtherPlayerName()
        {
            if (_lobbyPlayers == null || _lobbyPlayers.Count < 2) 
                return "";
            
            string myId = _client?.PlayerId;
            if (string.IsNullOrEmpty(myId)) 
                return "";

            var otherPlayer = _lobbyPlayers.FirstOrDefault(p => 
                !string.IsNullOrEmpty(p.Id) && p.Id != myId);
            
            return otherPlayer?.Name ?? "";
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

            bool otherReady = IsOtherPlayerReady();
            bool canStart = _myReady && otherReady && _playerCount >= 2;

            switch (_currentScreen)
            {
                case GameScreen.Connecting:
                    _readyButton.Visible = false;
                    _startButton.Visible = false;
                    _rulesLabel.Visible = false;
                    UpdateStatus("未连接\n请连接服务器", Color.Red);
                    break;

                case GameScreen.Lobby:
                    _readyButton.Visible = true;
                    _startButton.Visible = true;
                    _rulesLabel.Visible = true;
                    
                    // 更新准备按钮
                    _readyButton.Text = _myReady ? "❌ 取消准备" : "✋ 准备";
                    _readyButton.BackColor = _myReady ? 
                        Color.FromArgb(180, 60, 60) : Color.FromArgb(60, 160, 60);
                    
                    // 更新开始按钮
                    _startButton.Enabled = canStart;
                    _startButton.BackColor = canStart ? 
                        Color.FromArgb(60, 120, 200) : Color.FromArgb(80, 80, 100);
                    
                    // 状态文本
                    string status = $"角色: {playerType}\n";
                    status += $"房间: {_playerCount}/2 人\n\n";
                    status += $"你: {(_myReady ? "✅已准备" : "⏳未准备")}\n";
                    
                    if (_playerCount >= 2)
                    {
                        string otherName = GetOtherPlayerName();
                        string displayName = string.IsNullOrEmpty(otherName) ? "对方" : otherName;
                        status += $"{displayName}: {(otherReady ? "✅已准备" : "⏳未准备")}";
                    }
                    else
                    {
                        status += "等待玩家加入...";
                    }
                    
                    UpdateStatus(status, Color.LightGreen);
                    break;

                case GameScreen.LevelSelect:
                    _readyButton.Visible = true;
                    _readyButton.Text = "⬅ 返回";
                    _readyButton.BackColor = Color.FromArgb(100, 100, 120);
                    _startButton.Visible = true;
                    _startButton.Enabled = true;
                    _startButton.Text = "▶ 开始";
                    _startButton.BackColor = Color.FromArgb(60, 160, 60);
                    _rulesLabel.Visible = false;
                    UpdateStatus($"角色: {playerType}\n\n点击或按1-5选择\n双击或点开始", Color.Cyan);
                    break;

                case GameScreen.Playing:
                    _readyButton.Visible = false;
                    _startButton.Visible = false;
                    _rulesLabel.Visible = false;
                    GameState state;
                    lock (_stateLock) { state = _gameState; }
                    if (state != null)
                    {
                        string iceStatus = state.IcePlayer?.IsAlive == true ? 
                            (state.IcePlayer.ReachedExit ? "✅" : "🏃") : "💀";
                        string fireStatus = state.FirePlayer?.IsAlive == true ? 
                            (state.FirePlayer.ReachedExit ? "✅" : "🏃") : "💀";
                        
                        UpdateStatus($"角色: {playerType}\n" +
                            $"关卡: {state.CurrentLevel}\n\n" +
                            $"💧冰人: {iceStatus}\n" +
                            $"🔥火人: {fireStatus}",
                            Color.LightGreen);
                    }
                    break;

                case GameScreen.GameOver:
                    _readyButton.Visible = true;
                    _readyButton.Text = "🔄 返回大厅";
                    _readyButton.BackColor = Color.FromArgb(200, 120, 60);
                    _startButton.Visible = false;
                    _rulesLabel.Visible = false;
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
                    bool otherReady = IsOtherPlayerReady();
                    string otherName = GetOtherPlayerName();
                    _renderer.RenderLobby(e.Graphics, size, _playerCount, _myReady, otherReady, 
                        _client.PlayerType, otherName);
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
            // 如果聊天框有焦点，不处理游戏按键
            if (_chatTextBox.Focused)
            {
                return; // 让TextBox正常处理按键（包括退格、删除等）
            }

            bool handled = false;

            // 游戏中的移动控制
            if (_currentScreen == GameScreen.Playing)
            {
                handled = true;
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
                    case Keys.Escape:
                        _currentScreen = GameScreen.Lobby;
                        _myReady = false;
                        UpdateUI();
                        break;
                    default:
                        handled = false;
                        break;
                }
            }
            // 关卡选择
            else if (_currentScreen == GameScreen.LevelSelect)
            {
                handled = true;
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
                        _myReady = false;
                        UpdateUI();
                        break;
                    default:
                        handled = false;
                        break;
                }
            }
            // 游戏结束
            else if (_currentScreen == GameScreen.GameOver)
            {
                handled = true;
                switch (e.KeyCode)
                {
                    case Keys.R:
                    case Keys.Enter:
                        _client.RequestRestart();
                        _currentScreen = GameScreen.Playing;
                        break;
                    case Keys.Escape:
                        _currentScreen = GameScreen.Lobby;
                        _myReady = false;
                        UpdateUI();
                        break;
                    default:
                        handled = false;
                        break;
                }
            }

            if (handled)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            // 如果聊天框有焦点，不处理
            if (_chatTextBox.Focused) return;

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

        /// <summary>
        /// 鼠标点击处理 - 用于关卡选择
        /// </summary>
        private void GamePanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (_currentScreen == GameScreen.LevelSelect)
            {
                // 计算关卡按钮位置
                var size = _gamePanel.ClientSize;
                float buttonWidth = 280;
                float buttonHeight = 50;
                float buttonStartY = size.Height * 0.25f;
                float buttonSpacing = 60;
                float buttonX = (size.Width - buttonWidth) / 2;

                for (int i = 0; i < 5; i++)
                {
                    float buttonY = buttonStartY + i * buttonSpacing;
                    var buttonRect = new RectangleF(buttonX, buttonY, buttonWidth, buttonHeight);
                    
                    if (buttonRect.Contains(e.Location))
                    {
                        _selectedLevel = i + 1;
                        
                        // 双击直接开始
                        if (e.Clicks == 2)
                        {
                            _client.RequestLevel(_selectedLevel);
                        }
                        break;
                    }
                }
                _gamePanel.Invalidate();
            }
        }

        private void ReadyButton_Click(object sender, EventArgs e)
        {
            if (_currentScreen == GameScreen.GameOver)
            {
                // 返回大厅
                _currentScreen = GameScreen.Lobby;
                _myReady = false;
                UpdateUI();
                return;
            }

            if (_currentScreen == GameScreen.LevelSelect)
            {
                // 返回大厅
                _currentScreen = GameScreen.Lobby;
                UpdateUI();
                return;
            }

            // 切换准备状态 (Lobby界面)
            _myReady = !_myReady;
            _client.SendReady(_myReady);
            AddMessage(_myReady ? "✅ 你已准备" : "❌ 取消准备");
            UpdateUI();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            // 关卡选择界面 - 开始游戏
            if (_currentScreen == GameScreen.LevelSelect)
            {
                _client.RequestLevel(_selectedLevel);
                AddMessage($"🎮 请求开始第 {_selectedLevel} 关...");
                return;
            }

            // 大厅界面 - 进入关卡选择
            bool otherReady = IsOtherPlayerReady();
            
            // 双重检查：必须双方都准备且有2人
            if (!_myReady || !otherReady || _playerCount < 2)
            {
                AddMessage("⚠️ 需要双方都准备才能开始！");
                return;
            }

            _currentScreen = GameScreen.LevelSelect;
            UpdateUI();
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
            // 让游戏面板获得焦点以响应游戏按键
            _gamePanel.Focus();
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
