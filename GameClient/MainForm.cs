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
            this.Text = "Fireboy and Watergirl - 森林冰火人网络版";
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(1000, 700);
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
                AutoScroll = false
            };

            // 状态标签
            _statusLabel = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Text = "连接中...",
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft
            };

            // 游戏规则标签
            _rulesLabel = new Label
            {
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 9),
                Text = "========= 游戏规则 =========\n" +
                       "W/上/空格: 跳跃\n" +
                       "A/左: 左移    D/右: 右移\n" +
                       "冰人(蓝): 躲避火焰\n" +
                       "火人(红): 躲避水池\n" +
                       "收集宝石后到达出口\n" +
                       "R: 重新开始  Esc: 返回\n" +
                       "F1: 作弊-一键通关\n" +
                       "============================",
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft
            };

            // 准备按钮
            _readyButton = new Button
            {
                Text = "准备",
                BackColor = Color.FromArgb(60, 160, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Visible = false
            };
            _readyButton.FlatAppearance.BorderSize = 0;
            _readyButton.Click += ReadyButton_Click;

            // 开始按钮
            _startButton = new Button
            {
                Text = "开始游戏",
                BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Visible = false,
                Enabled = false
            };
            _startButton.FlatAppearance.BorderSize = 0;
            _startButton.Click += StartButton_Click;

            // 聊天标签
            _chatLabel = new Label
            {
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 9),
                Text = "消息记录:",
                AutoSize = false
            };

            // 聊天列表
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

            // 事件
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

            // 固定侧边面板宽度为300
            int sidePanelWidth = 300;
            int gamePanelWidth = this.ClientSize.Width - sidePanelWidth;
            int height = this.ClientSize.Height;

            // 游戏面板
            _gamePanel.SetBounds(0, 0, gamePanelWidth, height);
            
            // 侧边面板
            _sidePanel.SetBounds(gamePanelWidth, 0, sidePanelWidth, height);

            // 侧边面板内部布局 - 使用固定值确保显示完整
            int padding = 10;
            int controlWidth = sidePanelWidth - padding * 2;
            int y = padding;

            // 状态标签 - 固定高度
            _statusLabel.SetBounds(padding, y, controlWidth, 100);
            y += 105;

            // 游戏规则 - 固定高度
            _rulesLabel.SetBounds(padding, y, controlWidth, 155);
            y += 160;

            // 按钮区域 - 两个按钮各占一半宽度
            int buttonWidth = (controlWidth - 10) / 2;
            _readyButton.SetBounds(padding, y, buttonWidth, 38);
            _startButton.SetBounds(padding + buttonWidth + 10, y, buttonWidth, 38);
            y += 46;

            // 聊天标签
            _chatLabel.SetBounds(padding, y, controlWidth, 20);
            y += 22;

            // 聊天列表 - 自适应剩余高度
            int remainingHeight = height - y - 50;
            int chatListHeight = Math.Max(80, remainingHeight);
            _chatListBox.SetBounds(padding, y, controlWidth, chatListHeight);
            y += chatListHeight + 4;

            // 聊天输入区
            int inputWidth = controlWidth - 55;
            _chatTextBox.SetBounds(padding, y, inputWidth, 28);
            _sendButton.SetBounds(padding + inputWidth + 5, y, 50, 28);
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
                _myReady = false;  // 游戏开始后重置准备状态
                AddMessage("游戏开始！");
                UpdateUI();
            };
            _client.OnGameStateUpdate += state => {
                lock (_stateLock) { _gameState = state; }
                
                if (state.GameOver)
                {
                    _currentScreen = GameScreen.GameOver;
                    UpdateUI();
                }
            };
            _client.OnDisconnected += () => {
                _currentScreen = GameScreen.Connecting;
                _lobbyPlayers.Clear();
                _playerCount = 0;
                _myReady = false;
                AddMessage("与服务器断开连接");
                UpdateUI();
            };
            _client.OnLobbyStatus += lobbyStatus => {
                _lobbyPlayers = lobbyStatus.Players ?? new List<LobbyPlayerInfo>();
                _playerCount = lobbyStatus.PlayerCount;
                
                // 同步自己的准备状态（从服务器获取）
                string myId = _client?.PlayerId;
                if (!string.IsNullOrEmpty(myId))
                {
                    var myInfo = _lobbyPlayers.FirstOrDefault(p => p.Id == myId);
                    if (myInfo != null)
                    {
                        _myReady = myInfo.IsReady;
                    }
                }
                
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
                _myReady = false;
                string playerTypeStr = _client.PlayerType == PlayerType.Ice ? "冰人(Watergirl)" : "火人(Fireboy)";
                AddMessage($"连接成功！你是 {playerTypeStr}");
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
        /// 检查对方是否准备
        /// </summary>
        private bool IsOtherPlayerReady()
        {
            if (_lobbyPlayers == null || _lobbyPlayers.Count < 2) 
                return false;
            
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
                (_client.PlayerType == PlayerType.Ice ? "冰人(蓝)" : "火人(红)") : "";

            bool otherReady = IsOtherPlayerReady();
            bool canStart = _myReady && otherReady && _playerCount >= 2;

            switch (_currentScreen)
            {
                case GameScreen.Connecting:
                    _readyButton.Visible = false;
                    _startButton.Visible = false;
                    _rulesLabel.Visible = true;
                    UpdateStatus("未连接\n请连接服务器", Color.Red);
                    break;

                case GameScreen.Lobby:
                    _readyButton.Visible = true;
                    _startButton.Visible = true;
                    _rulesLabel.Visible = true;
                    
                    // 准备按钮
                    _readyButton.Text = _myReady ? "取消准备" : "准备";
                    _readyButton.BackColor = _myReady ? 
                        Color.FromArgb(180, 60, 60) : Color.FromArgb(60, 160, 60);
                    
                    // 开始按钮
                    _startButton.Text = "开始游戏";
                    _startButton.Enabled = canStart;
                    _startButton.BackColor = canStart ? 
                        Color.FromArgb(60, 120, 200) : Color.FromArgb(80, 80, 100);
                    
                    // 状态文本
                    string status = $"你的角色: {playerType}\n";
                    status += $"房间人数: {_playerCount}/2\n\n";
                    status += $"你: {(_myReady ? "[已准备]" : "[未准备]")}\n";
                    
                    if (_playerCount >= 2)
                    {
                        string otherName = GetOtherPlayerName();
                        string displayName = string.IsNullOrEmpty(otherName) ? "对方" : otherName;
                        status += $"{displayName}: {(otherReady ? "[已准备]" : "[未准备]")}";
                    }
                    else
                    {
                        status += "等待其他玩家加入...";
                    }
                    
                    UpdateStatus(status, Color.LightGreen);
                    break;

                case GameScreen.LevelSelect:
                    _readyButton.Visible = true;
                    _readyButton.Text = "返回";
                    _readyButton.BackColor = Color.FromArgb(100, 100, 120);
                    _startButton.Visible = true;
                    _startButton.Enabled = true;
                    _startButton.Text = "开始";
                    _startButton.BackColor = Color.FromArgb(60, 160, 60);
                    _rulesLabel.Visible = false;
                    UpdateStatus($"你的角色: {playerType}\n\n选择关卡: 按1-5\n确认: 按Enter或点开始\n返回: 按Esc", Color.Cyan);
                    break;

                case GameScreen.Playing:
                    _readyButton.Visible = false;
                    _startButton.Visible = false;
                    _rulesLabel.Visible = true;
                    GameState state;
                    lock (_stateLock) { state = _gameState; }
                    if (state != null)
                    {
                        string iceStatus = state.IcePlayer?.IsAlive == true ? 
                            (state.IcePlayer.ReachedExit ? "[到达出口]" : "[游戏中]") : "[死亡]";
                        string fireStatus = state.FirePlayer?.IsAlive == true ? 
                            (state.FirePlayer.ReachedExit ? "[到达出口]" : "[游戏中]") : "[死亡]";
                        
                        UpdateStatus($"你的角色: {playerType}\n" +
                            $"当前关卡: 第{state.CurrentLevel}关\n\n" +
                            $"冰人(蓝): {iceStatus}\n" +
                            $"火人(红): {fireStatus}",
                            Color.LightGreen);
                    }
                    break;

                case GameScreen.GameOver:
                    _readyButton.Visible = true;
                    _readyButton.Text = "返回大厅";
                    _readyButton.BackColor = Color.FromArgb(200, 120, 60);
                    _startButton.Visible = true;
                    _startButton.Text = "重新开始";
                    _startButton.Enabled = true;
                    _startButton.BackColor = Color.FromArgb(60, 160, 60);
                    _rulesLabel.Visible = true;
                    
                    GameState endState;
                    lock (_stateLock) { endState = _gameState; }
                    if (endState != null)
                    {
                        string result = endState.Victory ? "通关成功！" : "游戏失败！";
                        UpdateStatus($"=== {result} ===\n\n按R或点击重新开始\n按Esc返回大厅", 
                            endState.Victory ? Color.Gold : Color.Red);
                    }
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
                return;
            }

            bool handled = false;

            // F1作弊键 - 在任何游戏状态下都可用
            if (e.KeyCode == Keys.F1 && _currentScreen == GameScreen.Playing)
            {
                _client.CheatWin();
                AddMessage("[作弊] 一键通关！");
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

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
                        AddMessage("请求重新开始...");
                        break;
                    case Keys.Escape:
                        _currentScreen = GameScreen.Lobby;
                        _myReady = false;
                        _client.SendReady(false);
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
                        AddMessage($"请求开始第{_selectedLevel}关...");
                        break;
                    case Keys.Escape:
                        _currentScreen = GameScreen.Lobby;
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
                        AddMessage("请求重新开始...");
                        break;
                    case Keys.Escape:
                        _currentScreen = GameScreen.Lobby;
                        _myReady = false;
                        _client.SendReady(false);
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
                            AddMessage($"请求开始第{_selectedLevel}关...");
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
                _client.SendReady(false);
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

            // Lobby界面 - 切换准备状态
            _myReady = !_myReady;
            _client.SendReady(_myReady);
            AddMessage(_myReady ? "你已准备" : "取消准备");
            UpdateUI();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            // 游戏结束界面 - 重新开始
            if (_currentScreen == GameScreen.GameOver)
            {
                _client.RequestRestart();
                AddMessage("请求重新开始...");
                return;
            }

            // 关卡选择界面 - 开始游戏
            if (_currentScreen == GameScreen.LevelSelect)
            {
                _client.RequestLevel(_selectedLevel);
                AddMessage($"请求开始第{_selectedLevel}关...");
                return;
            }

            // 大厅界面 - 进入关卡选择
            bool otherReady = IsOtherPlayerReady();
            
            if (!_myReady)
            {
                AddMessage("你还没有准备！");
                return;
            }
            
            if (!otherReady)
            {
                AddMessage("对方还没有准备！");
                return;
            }
            
            if (_playerCount < 2)
            {
                AddMessage("需要2名玩家才能开始！");
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
