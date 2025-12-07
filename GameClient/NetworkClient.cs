using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using FireboyAndWatergirl.Shared;

namespace FireboyAndWatergirl.GameClient
{
    /// <summary>
    /// 网络客户端 - 基于TcpClient
    /// </summary>
    public class NetworkClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;
        private string _playerId;
        private PlayerType _playerType;
        private string _playerName;
        private readonly object _streamLock = new object();

        public event Action<string> OnServerMessage;
        public event Action<string, string> OnChatMessage;
        public event Action<GameState> OnGameStateUpdate;
        public event Action OnGameStart;
        public event Action OnDisconnected;

        public bool IsConnected => _isConnected;
        public PlayerType PlayerType => _playerType;
        public string PlayerName => _playerName;
        public string PlayerId => _playerId;

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async Task<bool> ConnectAsync(string host, int port, string playerName, PlayerType preferredType)
        {
            try
            {
                _playerName = playerName;
                _client = new TcpClient();

                // 设置超时
                var connectTask = _client.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(GameConfig.ConnectionTimeoutMs)) != connectTask)
                {
                    throw new TimeoutException("连接超时");
                }
                await connectTask;

                _stream = _client.GetStream();

                // 发送连接请求
                var connectMsg = new ConnectMessage(playerName, preferredType);
                NetworkProtocol.SendMessage(_stream, connectMsg);

                // 等待响应
                var response = NetworkProtocol.ReceiveMessage(_stream) as ConnectResponseMessage;
                if (response == null || !response.Success)
                {
                    string errorMsg = response?.Message ?? "连接被拒绝";
                    OnServerMessage?.Invoke($"连接失败: {errorMsg}");
                    _client.Close();
                    return false;
                }

                _playerId = response.PlayerId;
                _playerType = response.AssignedType;
                _isConnected = true;

                OnServerMessage?.Invoke(response.Message);

                // 启动接收循环
                _ = Task.Run(ReceiveLoop);

                return true;
            }
            catch (Exception ex)
            {
                OnServerMessage?.Invoke($"连接错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 接收消息循环
        /// </summary>
        private async Task ReceiveLoop()
        {
            try
            {
                while (_isConnected && _client.Connected)
                {
                    if (_stream.DataAvailable)
                    {
                        NetworkMessage message;
                        lock (_streamLock)
                        {
                            message = NetworkProtocol.ReceiveMessage(_stream);
                        }
                        HandleMessage(message);
                    }
                    else
                    {
                        await Task.Delay(5);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isConnected)
                {
                    OnServerMessage?.Invoke($"连接断开: {ex.Message}");
                }
            }
            finally
            {
                _isConnected = false;
                OnDisconnected?.Invoke();
            }
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void HandleMessage(NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.GameStateUpdate:
                    var stateMsg = message as GameStateMessage;
                    if (stateMsg != null)
                    {
                        OnGameStateUpdate?.Invoke(stateMsg.State);
                    }
                    break;

                case MessageType.GameStart:
                    var startMsg = message as GameStartMessage;
                    if (startMsg != null)
                    {
                        OnGameStart?.Invoke();
                        OnGameStateUpdate?.Invoke(startMsg.InitialState);
                    }
                    break;

                case MessageType.ServerMessage:
                    var serverMsg = message as ServerMessagePacket;
                    if (serverMsg != null)
                    {
                        OnServerMessage?.Invoke(serverMsg.Content);
                    }
                    break;

                case MessageType.ChatMessage:
                    var chatMsg = message as ChatMessagePacket;
                    if (chatMsg != null)
                    {
                        OnChatMessage?.Invoke(chatMsg.SenderName, chatMsg.Content);
                    }
                    break;
            }
        }

        /// <summary>
        /// 发送玩家输入
        /// </summary>
        public void SendInput(PlayerAction actions)
        {
            if (!_isConnected) return;

            try
            {
                var inputMsg = new PlayerInputMessage(_playerType, actions);
                lock (_streamLock)
                {
                    NetworkProtocol.SendMessage(_stream, inputMsg);
                }
            }
            catch (Exception ex)
            {
                OnServerMessage?.Invoke($"发送输入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送聊天消息
        /// </summary>
        public void SendChat(string content)
        {
            if (!_isConnected) return;

            try
            {
                var chatMsg = new ChatMessagePacket(content, _playerName);
                lock (_streamLock)
                {
                    NetworkProtocol.SendMessage(_stream, chatMsg);
                }
            }
            catch (Exception ex)
            {
                OnServerMessage?.Invoke($"发送聊天失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 请求重新开始游戏
        /// </summary>
        public void RequestRestart()
        {
            if (!_isConnected) return;

            try
            {
                var restartMsg = new NetworkMessage(MessageType.GameRestart);
                lock (_streamLock)
                {
                    NetworkProtocol.SendMessage(_stream, restartMsg);
                }
            }
            catch (Exception ex)
            {
                OnServerMessage?.Invoke($"发送重启请求失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _isConnected = false;
            try
            {
                _client?.Close();
            }
            catch { }
        }
    }
}

