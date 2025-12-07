namespace IceFireMan.Shared
{
    /// <summary>
    /// 游戏配置常量
    /// </summary>
    public static class GameConfig
    {
        // 网络配置
        public const int DefaultPort = 9527;
        public const string DefaultHost = "127.0.0.1";
        public const int HeartbeatIntervalMs = 5000;
        public const int ConnectionTimeoutMs = 10000;

        // 游戏物理参数
        public const float Gravity = 0.5f;
        public const float MoveSpeed = 0.8f;
        public const float JumpForce = -1.2f;
        public const float MaxFallSpeed = 2.0f;
        public const float Friction = 0.8f;

        // 游戏参数
        public const int TickRate = 30;  // 每秒游戏更新次数
        public const int TickIntervalMs = 1000 / TickRate;

        // 地图参数
        public const int DefaultMapWidth = 40;
        public const int DefaultMapHeight = 20;

        // 玩家大小（用于碰撞检测）
        public const float PlayerWidth = 1.0f;
        public const float PlayerHeight = 1.0f;
    }
}

