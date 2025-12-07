namespace FireboyAndWatergirl.Shared
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

        // 游戏物理参数 - 优化版
        public const float Gravity = 0.06f;        // 重力（大幅降低，让跳跃更平滑持久）
        public const float MoveSpeed = 0.25f;      // 移动速度（降低，更容易控制）
        public const float JumpForce = -0.8f;      // 跳跃力度（配合低重力，跳得更高）
        public const float MaxFallSpeed = 0.6f;    // 最大下落速度（降低，下落更柔和）
        public const float Friction = 0.85f;       // 摩擦力（提高，停止更平滑）

        // 游戏参数
        public const int TickRate = 60;  // 每秒游戏更新次数（提高到60，更丝滑）
        public const int TickIntervalMs = 1000 / TickRate;

        // 地图参数
        public const int DefaultMapWidth = 40;
        public const int DefaultMapHeight = 20;

        // 玩家大小（用于碰撞检测）
        public const float PlayerWidth = 1.0f;
        public const float PlayerHeight = 1.0f;
    }
}

