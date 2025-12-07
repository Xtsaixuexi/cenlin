using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FireboyAndWatergirl.Shared;

namespace FireboyAndWatergirl.Server
{
    /// <summary>
    /// 连接的玩家信息
    /// </summary>
    public class ConnectedPlayer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PlayerType Type { get; set; }
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public bool IsConnected { get; set; } = true;
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 游戏服务器 - 基于TcpListener
    /// </summary>
    public class GameServer
    {
        private TcpListener _listener;
        private readonly int _port;
        private bool _isRunning;
        private readonly ConcurrentDictionary<string, ConnectedPlayer> _players = new();
        private readonly object _gameLock = new();
        
        private GameState _gameState;
        private readonly GameLogic _gameLogic;
        private bool _gameStarted = false;

        // 玩家输入缓冲
        private PlayerAction _icePlayerInput = PlayerAction.None;
        private PlayerAction _firePlayerInput = PlayerAction.None;

        public event Action<string> OnLog;

        public GameServer(int port = GameConfig.DefaultPort)
        {
            _port = port;
            _gameLogic = new GameLogic();
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;

            Log($"🎮 森林冰火人服务器启动在端口 {_port}");
            Log("等待玩家连接...");

            // 启动游戏循环
            _ = Task.Run(() => GameLoopAsync(cancellationToken), cancellationToken);

            // 接受客户端连接
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_listener.Pending())
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"接受连接时出错: {ex.Message}");
                }
            }

            Stop();
        }

        /// <summary>
        /// 处理客户端连接
        /// </summary>
        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            ConnectedPlayer player = null;
            try
            {
                var stream = client.GetStream();
                
                // 接收连接消息
                var connectMsg = NetworkProtocol.ReceiveMessage(stream) as ConnectMessage;
                if (connectMsg == null)
                {
                    client.Close();
                    return;
                }

                // 分配玩家类型
                PlayerType assignedType;
                lock (_gameLock)
                {
                    if (_players.Count >= 2)
                    {
                        // 已满
                        var response = new ConnectResponseMessage
                        {
                            Success = false,
                            Message = "服务器已满，请稍后再试"
                        };
                        NetworkProtocol.SendMessage(stream, response);
                        client.Close();
                        return;
                    }

                    // 分配类型
                    bool icePlayerExists = false;
                    bool firePlayerExists = false;
                    foreach (var p in _players.Values)
                    {
                        if (p.Type == PlayerType.Ice) icePlayerExists = true;
                        if (p.Type == PlayerType.Fire) firePlayerExists = true;
                    }

                    if (connectMsg.PreferredType == PlayerType.Ice && !icePlayerExists)
                        assignedType = PlayerType.Ice;
                    else if (connectMsg.PreferredType == PlayerType.Fire && !firePlayerExists)
                        assignedType = PlayerType.Fire;
                    else if (!icePlayerExists)
                        assignedType = PlayerType.Ice;
                    else
                        assignedType = PlayerType.Fire;

                    // 创建玩家
                    player = new ConnectedPlayer
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = connectMsg.PlayerName ?? $"Player{_players.Count + 1}",
                        Type = assignedType,
                        Client = client,
                        Stream = stream
                    };

                    _players[player.Id] = player;
                }

                Log($"✅ 玩家 [{player.Name}] 已连接，分配为 {(assignedType == PlayerType.Ice ? "❄冰人" : "🔥火人")}");

                // 发送连接响应
                var successResponse = new ConnectResponseMessage
                {
                    Success = true,
                    AssignedType = assignedType,
                    PlayerId = player.Id,
                    Message = $"欢迎 {player.Name}！你是{(assignedType == PlayerType.Ice ? "冰人❄" : "火人🔥")}",
                    PlayersConnected = _players.Count
                };
                NetworkProtocol.SendMessage(stream, successResponse);

                // 广播消息
                await BroadcastServerMessage($"玩家 {player.Name} ({(assignedType == PlayerType.Ice ? "冰人" : "火人")}) 加入了游戏！");

                // 检查是否可以开始游戏
                CheckAndStartGame();

                // 接收玩家输入
                while (_isRunning && player.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (stream.DataAvailable)
                        {
                            var message = NetworkProtocol.ReceiveMessage(stream);
                            await HandlePlayerMessage(player, message);
                        }
                        else
                        {
                            await Task.Delay(10, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"处理玩家消息时出错: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"客户端处理出错: {ex.Message}");
            }
            finally
            {
                if (player != null)
                {
                    _players.TryRemove(player.Id, out _);
                    Log($"❌ 玩家 [{player.Name}] 已断开连接");
                    await BroadcastServerMessage($"玩家 {player.Name} 离开了游戏");
                    
                    // 如果游戏正在进行，暂停游戏
                    if (_gameStarted)
                    {
                        _gameStarted = false;
                        await BroadcastServerMessage("等待玩家重新连接...");
                    }
                }
                client.Close();
            }
        }

        /// <summary>
        /// 处理玩家消息
        /// </summary>
        private async Task HandlePlayerMessage(ConnectedPlayer player, NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.PlayerInput:
                    var inputMsg = message as PlayerInputMessage;
                    if (inputMsg != null)
                    {
                        lock (_gameLock)
                        {
                            if (player.Type == PlayerType.Ice)
                                _icePlayerInput = inputMsg.Actions;
                            else
                                _firePlayerInput = inputMsg.Actions;
                        }
                    }
                    break;

                case MessageType.ChatMessage:
                    var chatMsg = message as ChatMessagePacket;
                    if (chatMsg != null)
                    {
                        chatMsg.SenderName = player.Name;
                        await BroadcastMessage(chatMsg);
                    }
                    break;

                case MessageType.GameRestart:
                    await RestartGame();
                    break;

                case MessageType.Heartbeat:
                    player.LastHeartbeat = DateTime.UtcNow;
                    break;
            }
        }

        /// <summary>
        /// 检查并开始游戏
        /// </summary>
        private void CheckAndStartGame()
        {
            lock (_gameLock)
            {
                if (_players.Count == 2 && !_gameStarted)
                {
                    StartNewGame();
                }
            }
        }

        /// <summary>
        /// 开始新游戏
        /// </summary>
        private void StartNewGame()
        {
            _gameState = LevelGenerator.CreateLevel(1);

            // 分配玩家连接ID
            foreach (var player in _players.Values)
            {
                if (player.Type == PlayerType.Ice)
                    _gameState.IcePlayer.ConnectionId = player.Id;
                else
                    _gameState.FirePlayer.ConnectionId = player.Id;
            }

            _gameStarted = true;
            Log("🎮 游戏开始！");

            // 发送游戏开始消息
            var startMsg = new GameStartMessage
            {
                InitialState = _gameState
            };
            BroadcastMessageSync(startMsg);
        }

        /// <summary>
        /// 重启游戏
        /// </summary>
        private async Task RestartGame()
        {
            // 确保有两个玩家才能重启
            if (_players.Count < 2)
            {
                await BroadcastServerMessage("需要两名玩家才能开始游戏！");
                return;
            }

            lock (_gameLock)
            {
                int currentLevel;
                if (_gameState?.Victory == true)
                {
                    // 通关后进入下一关，超过最大关卡则返回第1关
                    currentLevel = _gameState.CurrentLevel + 1;
                    if (currentLevel > LevelGenerator.TotalLevels)
                        currentLevel = 1;
                }
                else
                {
                    // 失败则重玩当前关
                    currentLevel = _gameState?.CurrentLevel ?? 1;
                }
                
                _gameState = LevelGenerator.CreateLevel(currentLevel);
                
                foreach (var player in _players.Values)
                {
                    if (player.Type == PlayerType.Ice)
                        _gameState.IcePlayer.ConnectionId = player.Id;
                    else
                        _gameState.FirePlayer.ConnectionId = player.Id;
                }

                _icePlayerInput = PlayerAction.None;
                _firePlayerInput = PlayerAction.None;
                _gameStarted = true;  // 修复：重启后设置游戏开始状态
            }

            Log("🔄 游戏重新开始");
            await BroadcastServerMessage("游戏重新开始！");

            var startMsg = new GameStartMessage
            {
                InitialState = _gameState
            };
            await BroadcastMessage(startMsg);
        }

        /// <summary>
        /// 游戏主循环
        /// </summary>
        private async Task GameLoopAsync(CancellationToken cancellationToken)
        {
            var lastTick = DateTime.UtcNow;

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var elapsed = (now - lastTick).TotalMilliseconds;

                    if (elapsed >= GameConfig.TickIntervalMs)
                    {
                        lastTick = now;

                        if (_gameStarted && _gameState != null && !_gameState.GameOver)
                        {
                            lock (_gameLock)
                            {
                                // 更新游戏状态
                                _gameLogic.Update(_gameState, _icePlayerInput, _firePlayerInput);
                                _gameState.GameTick++;

                                // 重置输入
                                _icePlayerInput = PlayerAction.None;
                                _firePlayerInput = PlayerAction.None;
                            }

                            // 广播游戏状态
                            var stateMsg = new GameStateMessage(_gameState);
                            BroadcastMessageSync(stateMsg);

                            // 检查游戏结束
                            if (_gameState.GameOver)
                            {
                                if (_gameState.Victory)
                                {
                                    Log("🎉 玩家胜利！");
                                    await BroadcastServerMessage("恭喜！双方都到达了出口！按R重新开始下一关");
                                }
                                else
                                {
                                    Log("💀 游戏结束");
                                    await BroadcastServerMessage("有玩家死亡！按R重新开始");
                                }
                            }
                        }
                    }

                    await Task.Delay(5, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"游戏循环错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 广播消息给所有玩家
        /// </summary>
        private async Task BroadcastMessage(NetworkMessage message)
        {
            var tasks = new List<Task>();
            foreach (var player in _players.Values)
            {
                if (player.IsConnected)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            lock (player.Stream)
                            {
                                NetworkProtocol.SendMessage(player.Stream, message);
                            }
                        }
                        catch
                        {
                            player.IsConnected = false;
                        }
                    }));
                }
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 同步广播消息
        /// </summary>
        private void BroadcastMessageSync(NetworkMessage message)
        {
            foreach (var player in _players.Values)
            {
                if (player.IsConnected)
                {
                    try
                    {
                        lock (player.Stream)
                        {
                            NetworkProtocol.SendMessage(player.Stream, message);
                        }
                    }
                    catch
                    {
                        player.IsConnected = false;
                    }
                }
            }
        }

        /// <summary>
        /// 广播服务器消息
        /// </summary>
        private async Task BroadcastServerMessage(string content)
        {
            var msg = new ServerMessagePacket(content);
            await BroadcastMessage(msg);
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            foreach (var player in _players.Values)
            {
                try
                {
                    player.Client?.Close();
                }
                catch { }
            }
            _players.Clear();
            _listener?.Stop();
            Log("服务器已停止");
        }

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}

