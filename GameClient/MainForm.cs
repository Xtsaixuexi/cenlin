using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading.Tasks;
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
                          ControlStyles.OptimizedDoubleBuffer, true);
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
        private System.Windows.Forms.Timer _gameTimer;
        private System.Windows.Forms.Timer _inputTimer;
        
        // UI控件
        private Panel _gamePanel;
        private Panel _infoPanel;
        private Label _statusLabel;
        private Label _messageLabel;
        private ListBox _chatListBox;
        private TextBox _chatTextBox;
        private Button _sendButton;
        private Button _restartButton;
        
        // 输入状态
        private HashSet<Keys> _pressedKeys = new HashSet<Keys>();
        
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
            this.Text = "❄ 森林冰火人网络版 🔥 - Ice and Fire Man";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(30, 30, 40);
            this.KeyPreview = true;

            // 游戏面板 - 使用双缓冲避免闪烁
            _gamePanel = new DoubleBufferedPanel
            {
                Location = new Point(10, 10),
                Size = new Size(850, 550),
                BackColor = Color.FromArgb(20, 20, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            _gamePanel.Paint += GamePanel_Paint;

            // 信息面板
            _infoPanel = new Panel
            {
                Location = new Point(870, 10),
                Size = new Size(300, 550),
                BackColor = Color.FromArgb(40, 40, 50),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 状态标签
            _statusLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(280, 60),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Text = "等待连接..."
            };

            // 消息标签
            _messageLabel = new Label
            {
                Location = new Point(10, 80),
                Size = new Size(280, 40),
                ForeColor = Color.Yellow,
                Font = new Font("Microsoft YaHei", 9),
                Text = ""
            };

            // 聊天列表
            var chatLabel = new Label
            {
                Location = new Point(10, 130),
                Size = new Size(280, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Microsoft YaHei", 9),
                Text = "消息记录:"
            };

            _chatListBox = new ListBox
            {
                Location = new Point(10, 155),
                Size = new Size(280, 280),
                BackColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 聊天输入
            _chatTextBox = new TextBox
            {
                Location = new Point(10, 445),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            _chatTextBox.KeyPress += ChatTextBox_KeyPress;

            _sendButton = new Button
            {
                Location = new Point(215, 443),
                Size = new Size(75, 27),
                Text = "发送",
                BackColor = Color.FromArgb(60, 120, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _sendButton.Click += SendButton_Click;

            _restartButton = new Button
            {
                Location = new Point(10, 485),
                Size = new Size(280, 35),
                Text = "🔄 重新开始 (R)",
                BackColor = Color.FromArgb(180, 80, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
            };
            _restartButton.Click += RestartButton_Click;

            // 控制说明面板
            var controlPanel = new Panel
            {
                Location = new Point(10, 570),
                Size = new Size(1150, 180),
                BackColor = Color.FromArgb(40, 40, 50),
                BorderStyle = BorderStyle.FixedSingle
            };

            var controlTitle = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(200, 25),
                ForeColor = Color.Cyan,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                Text = "🎮 游戏控制"
            };

            var controlText = new Label
            {
                Location = new Point(10, 40),
                Size = new Size(550, 130),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                Text = "移动: A/D 或 ←/→ 方向键\n" +
                       "跳跃: W 或 ↑ 或 空格键\n" +
                       "重新开始: R键\n" +
                       "发送消息: Enter键"
            };

            var rulesTitle = new Label
            {
                Location = new Point(580, 10),
                Size = new Size(200, 25),
                ForeColor = Color.Orange,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                Text = "📜 游戏规则"
            };

            var rulesText = new Label
            {
                Location = new Point(580, 40),
                Size = new Size(550, 130),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                Text = "❄ 冰人(蓝色): 可以通过冰区域，但怕火\n" +
                       "🔥 火人(红色): 可以通过火区域，但怕冰\n" +
                       "💀 水域: 两者都会死亡！\n" +
                       "🎯 目标: 收集宝石并到达各自的出口门"
            };

            // 添加控件
            _infoPanel.Controls.AddRange(new Control[] {
                _statusLabel, _messageLabel, chatLabel, _chatListBox,
                _chatTextBox, _sendButton, _restartButton
            });

            controlPanel.Controls.AddRange(new Control[] {
                controlTitle, controlText, rulesTitle, rulesText
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
                AddMessage("游戏开始！");
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
                AddMessage("与服务器断开连接");
                UpdateStatus("已断开连接", Color.Red);
            };

            // 游戏刷新定时器 (60 FPS)
            _gameTimer = new System.Windows.Forms.Timer();
            _gameTimer.Interval = 16;
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();

            // 输入发送定时器 (30 Hz)
            _inputTimer = new System.Windows.Forms.Timer();
            _inputTimer.Interval = 33;
            _inputTimer.Tick += InputTimer_Tick;
            _inputTimer.Start();

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
                string playerTypeStr = _client.PlayerType == PlayerType.Ice ? "❄ 冰人" : "🔥 火人";
                UpdateStatus($"已连接！你是 {playerTypeStr}\n等待另一位玩家...", Color.LightGreen);
                AddMessage($"连接成功！你是 {playerTypeStr}");
            }
            else
            {
                UpdateStatus("连接失败", Color.Red);
                MessageBox.Show("无法连接到服务器，请确保服务器已启动。", "连接失败", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowConnectDialog();
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
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
                string playerTypeStr = _client.PlayerType == PlayerType.Ice ? "❄ 冰人" : "🔥 火人";
                string iceStatus = state.IcePlayer?.IsAlive == true ? 
                    (state.IcePlayer.ReachedExit ? "✅到达" : "正常") : "💀";
                string fireStatus = state.FirePlayer?.IsAlive == true ? 
                    (state.FirePlayer.ReachedExit ? "✅到达" : "正常") : "💀";

                UpdateStatus($"你是: {playerTypeStr}\n" +
                           $"关卡: {state.CurrentLevel}\n" +
                           $"冰人: {iceStatus} 宝石:{state.IcePlayer?.GemsCollected ?? 0}\n" +
                           $"火人: {fireStatus} 宝石:{state.FirePlayer?.GemsCollected ?? 0}",
                           Color.LightGreen);

                if (!string.IsNullOrEmpty(state.Message))
                {
                    UpdateMessage(state.Message, state.Victory ? Color.Gold : 
                        (state.GameOver ? Color.Red : Color.Yellow));
                }
            }
        }

        private void InputTimer_Tick(object sender, EventArgs e)
        {
            if (!_client.IsConnected || !_gameStarted) return;

            PlayerAction action = PlayerAction.None;

            lock (_pressedKeys)
            {
                if (_pressedKeys.Contains(Keys.A) || _pressedKeys.Contains(Keys.Left))
                    action |= PlayerAction.MoveLeft;
                if (_pressedKeys.Contains(Keys.D) || _pressedKeys.Contains(Keys.Right))
                    action |= PlayerAction.MoveRight;
                if (_pressedKeys.Contains(Keys.W) || _pressedKeys.Contains(Keys.Up) || _pressedKeys.Contains(Keys.Space))
                    action |= PlayerAction.Jump;
            }

            if (action != PlayerAction.None)
            {
                _client.SendInput(action);
            }
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
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
                // 显示等待画面
                _renderer.RenderWaitingScreen(e.Graphics, _gamePanel.ClientSize, 
                    _client.IsConnected ? "等待另一位玩家加入..." : "未连接");
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            lock (_pressedKeys)
            {
                _pressedKeys.Add(e.KeyCode);
            }

            // 快捷键
            if (e.KeyCode == Keys.R)
            {
                _client.RequestRestart();
                AddMessage("请求重新开始...");
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            lock (_pressedKeys)
            {
                _pressedKeys.Remove(e.KeyCode);
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
            AddMessage("请求重新开始...");
        }

        private void SendChatMessage()
        {
            string msg = _chatTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(msg))
            {
                _client.SendChat(msg);
                _chatTextBox.Clear();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 停止定时器
            _gameTimer?.Stop();
            _gameTimer?.Dispose();
            _inputTimer?.Stop();
            _inputTimer?.Dispose();
            
            // 断开网络连接
            _client?.Disconnect();
            
            // 释放渲染器资源
            _renderer?.Dispose();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // 响应式布局
            int panelWidth = Math.Max(400, this.ClientSize.Width - 330);
            int panelHeight = Math.Max(300, this.ClientSize.Height - 210);
            
            _gamePanel.Size = new Size(panelWidth, panelHeight);
            _infoPanel.Location = new Point(panelWidth + 20, 10);
        }

        private void UpdateStatus(string text, Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(text, color)));
                return;
            }
            _statusLabel.Text = text;
            _statusLabel.ForeColor = color;
        }

        private void UpdateMessage(string text, Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateMessage(text, color)));
                return;
            }
            _messageLabel.Text = text;
            _messageLabel.ForeColor = color;
        }

        private void AddMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AddMessage(message)));
                return;
            }

            string timeMsg = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _messages.Add(timeMsg);
            _chatListBox.Items.Add(timeMsg);
            
            // 滚动到底部
            if (_chatListBox.Items.Count > 0)
                _chatListBox.TopIndex = _chatListBox.Items.Count - 1;
            
            // 限制消息数量
            while (_chatListBox.Items.Count > 100)
            {
                _chatListBox.Items.RemoveAt(0);
            }
        }
    }
}

