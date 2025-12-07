using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FireboyAndWatergirl.Shared
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        // 连接相关
        Connect = 0,
        ConnectResponse = 1,
        Disconnect = 2,
        Heartbeat = 3,

        // 游戏控制
        PlayerInput = 10,
        GameStateUpdate = 11,
        GameStart = 12,
        GameOver = 13,
        GameRestart = 14,
        LevelSelect = 15,
        PlayerReady = 16,

        // 聊天
        ChatMessage = 20,

        // 系统消息
        ServerMessage = 30,
        Error = 31
    }

    /// <summary>
    /// 玩家输入动作
    /// </summary>
    [Flags]
    public enum PlayerAction
    {
        None = 0,
        MoveLeft = 1,
        MoveRight = 2,
        Jump = 4,
        Action = 8  // 特殊动作
    }

    /// <summary>
    /// 网络消息基类
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public long Timestamp { get; set; }
        public string SenderId { get; set; }

        public NetworkMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public NetworkMessage(MessageType type) : this()
        {
            Type = type;
        }
    }

    /// <summary>
    /// 连接请求消息
    /// </summary>
    [Serializable]
    public class ConnectMessage : NetworkMessage
    {
        public string PlayerName { get; set; }
        public PlayerType PreferredType { get; set; }

        public ConnectMessage() : base(MessageType.Connect) { }

        public ConnectMessage(string name, PlayerType preferred) : this()
        {
            PlayerName = name;
            PreferredType = preferred;
        }
    }

    /// <summary>
    /// 连接响应消息
    /// </summary>
    [Serializable]
    public class ConnectResponseMessage : NetworkMessage
    {
        public bool Success { get; set; }
        public PlayerType AssignedType { get; set; }
        public string PlayerId { get; set; }
        public string Message { get; set; }
        public int PlayersConnected { get; set; }

        public ConnectResponseMessage() : base(MessageType.ConnectResponse) { }
    }

    /// <summary>
    /// 玩家输入消息
    /// </summary>
    [Serializable]
    public class PlayerInputMessage : NetworkMessage
    {
        public PlayerAction Actions { get; set; }
        public PlayerType PlayerType { get; set; }

        public PlayerInputMessage() : base(MessageType.PlayerInput) { }

        public PlayerInputMessage(PlayerType type, PlayerAction actions) : this()
        {
            PlayerType = type;
            Actions = actions;
        }
    }

    /// <summary>
    /// 游戏状态更新消息
    /// </summary>
    [Serializable]
    public class GameStateMessage : NetworkMessage
    {
        public GameState State { get; set; }

        public GameStateMessage() : base(MessageType.GameStateUpdate) { }

        public GameStateMessage(GameState state) : this()
        {
            State = state;
        }
    }

    /// <summary>
    /// 游戏开始消息
    /// </summary>
    [Serializable]
    public class GameStartMessage : NetworkMessage
    {
        public GameState InitialState { get; set; }

        public GameStartMessage() : base(MessageType.GameStart) { }
    }

    /// <summary>
    /// 聊天消息
    /// </summary>
    [Serializable]
    public class ChatMessagePacket : NetworkMessage
    {
        public string Content { get; set; }
        public string SenderName { get; set; }

        public ChatMessagePacket() : base(MessageType.ChatMessage) { }

        public ChatMessagePacket(string content, string senderName) : this()
        {
            Content = content;
            SenderName = senderName;
        }
    }

    /// <summary>
    /// 服务器消息
    /// </summary>
    [Serializable]
    public class ServerMessagePacket : NetworkMessage
    {
        public string Content { get; set; }

        public ServerMessagePacket() : base(MessageType.ServerMessage) { }

        public ServerMessagePacket(string content) : this()
        {
            Content = content;
        }
    }

    /// <summary>
    /// 关卡选择消息
    /// </summary>
    [Serializable]
    public class LevelSelectMessage : NetworkMessage
    {
        public int Level { get; set; }

        public LevelSelectMessage() : base(MessageType.LevelSelect) { }

        public LevelSelectMessage(int level) : this()
        {
            Level = level;
        }
    }

    /// <summary>
    /// 玩家准备消息
    /// </summary>
    [Serializable]
    public class PlayerReadyMessage : NetworkMessage
    {
        public bool IsReady { get; set; }

        public PlayerReadyMessage() : base(MessageType.PlayerReady) { }

        public PlayerReadyMessage(bool isReady) : this()
        {
            IsReady = isReady;
        }
    }

    /// <summary>
    /// 网络协议帮助类
    /// </summary>
    public static class NetworkProtocol
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// 将消息序列化为字节数组（带长度前缀）
        /// </summary>
        public static byte[] Serialize(NetworkMessage message)
        {
            // 获取具体类型名
            string typeName = message.GetType().Name;
            string json = JsonSerializer.Serialize(message, message.GetType(), JsonOptions);

            // 格式: [4字节类型名长度][类型名][4字节JSON长度][JSON数据]
            byte[] typeNameBytes = Encoding.UTF8.GetBytes(typeName);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(typeNameBytes.Length);
            writer.Write(typeNameBytes);
            writer.Write(jsonBytes.Length);
            writer.Write(jsonBytes);

            return ms.ToArray();
        }

        /// <summary>
        /// 从字节数组反序列化消息
        /// </summary>
        public static NetworkMessage Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            int typeNameLength = reader.ReadInt32();
            byte[] typeNameBytes = reader.ReadBytes(typeNameLength);
            string typeName = Encoding.UTF8.GetString(typeNameBytes);

            int jsonLength = reader.ReadInt32();
            byte[] jsonBytes = reader.ReadBytes(jsonLength);
            string json = Encoding.UTF8.GetString(jsonBytes);

            // 根据类型名反序列化
            Type messageType = typeName switch
            {
                nameof(ConnectMessage) => typeof(ConnectMessage),
                nameof(ConnectResponseMessage) => typeof(ConnectResponseMessage),
                nameof(PlayerInputMessage) => typeof(PlayerInputMessage),
                nameof(GameStateMessage) => typeof(GameStateMessage),
                nameof(GameStartMessage) => typeof(GameStartMessage),
                nameof(ChatMessagePacket) => typeof(ChatMessagePacket),
                nameof(ServerMessagePacket) => typeof(ServerMessagePacket),
                nameof(LevelSelectMessage) => typeof(LevelSelectMessage),
                nameof(PlayerReadyMessage) => typeof(PlayerReadyMessage),
                _ => typeof(NetworkMessage)
            };

            return (NetworkMessage)JsonSerializer.Deserialize(json, messageType, JsonOptions);
        }

        /// <summary>
        /// 发送消息到流（带长度前缀）
        /// </summary>
        public static void SendMessage(Stream stream, NetworkMessage message)
        {
            byte[] data = Serialize(message);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            stream.Write(lengthPrefix, 0, 4);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        /// <summary>
        /// 从流接收消息
        /// </summary>
        public static NetworkMessage ReceiveMessage(Stream stream)
        {
            // 读取长度前缀
            byte[] lengthBuffer = new byte[4];
            int bytesRead = 0;
            while (bytesRead < 4)
            {
                int read = stream.Read(lengthBuffer, bytesRead, 4 - bytesRead);
                if (read == 0) throw new IOException("连接已关闭");
                bytesRead += read;
            }

            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (dataLength <= 0 || dataLength > 1024 * 1024) // 最大1MB
                throw new IOException($"无效的消息长度: {dataLength}");

            // 读取消息数据
            byte[] dataBuffer = new byte[dataLength];
            bytesRead = 0;
            while (bytesRead < dataLength)
            {
                int read = stream.Read(dataBuffer, bytesRead, dataLength - bytesRead);
                if (read == 0) throw new IOException("连接已关闭");
                bytesRead += read;
            }

            return Deserialize(dataBuffer);
        }
    }
}

