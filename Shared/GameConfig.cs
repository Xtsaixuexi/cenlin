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

        // 游戏物理参数
        public const float Gravity = 0.25f;        // 重力（降低，让跳跃更平滑）
        public const float MoveSpeed = 0.35f;      // 移动速度（降低）
        public const float JumpForce = -0.9f;      // 跳跃力度（负值向上）
        public const float MaxFallSpeed = 1.0f;    // 最大下落速度
        public const float Friction = 0.7f;        // 摩擦力（停止时减速）

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

