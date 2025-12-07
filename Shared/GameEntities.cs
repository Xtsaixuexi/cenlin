using System;
using System.Collections.Generic;

namespace FireboyAndWatergirl.Shared
{
    /// <summary>
    /// 玩家类型枚举
    /// </summary>
    public enum PlayerType
    {
        Ice = 0,    // 冰人 - 蓝色，可以通过冰区域，怕火
        Fire = 1    // 火人 - 红色，可以通过火区域，怕冰
    }

    /// <summary>
    /// 方块类型枚举
    /// </summary>
    public enum TileType
    {
        Empty = 0,      // 空气
        Wall = 1,       // 墙壁
        Ice = 2,        // 冰区域 - 只有冰人能通过
        Fire = 3,       // 火区域 - 只有火人能通过
        Water = 4,      // 水 - 两者都会死
        IceGem = 5,     // 冰宝石 - 冰人收集
        FireGem = 6,    // 火宝石 - 火人收集
        IceDoor = 7,    // 冰人出口
        FireDoor = 8,   // 火人出口
        Platform = 9,   // 平台
        Button = 10,    // 按钮
        MovingPlatform = 11  // 移动平台
    }

    /// <summary>
    /// 玩家状态
    /// </summary>
    [Serializable]
    public class Player
    {
        public PlayerType Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public bool IsOnGround { get; set; }
        public bool IsAlive { get; set; } = true;
        public bool ReachedExit { get; set; } = false;
        public int GemsCollected { get; set; } = 0;
        public string ConnectionId { get; set; }

        public Player() { }

        public Player(PlayerType type, float x, float y)
        {
            Type = type;
            X = x;
            Y = y;
            VelocityX = 0;
            VelocityY = 0;
            IsOnGround = false;
            IsAlive = true;
        }

        public char GetDisplayChar()
        {
            if (!IsAlive) return 'X';
            return Type == PlayerType.Ice ? 'I' : 'F';
        }

        public ConsoleColor GetColor()
        {
            return Type == PlayerType.Ice ? ConsoleColor.Cyan : ConsoleColor.Red;
        }
    }

    /// <summary>
    /// 游戏地图
    /// </summary>
    [Serializable]
    public class GameMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        
        // 使用一维数组以支持JSON序列化（二维数组无法直接序列化）
        public int[] TilesData { get; set; }
        
        public string Name { get; set; }

        // 宝石位置列表
        public List<GemPosition> Gems { get; set; } = new List<GemPosition>();

        public GameMap() { }

        public GameMap(int width, int height, string name = "Level 1")
        {
            Width = width;
            Height = height;
            Name = name;
            TilesData = new int[height * width];
        }

        public TileType GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return TileType.Wall;
            return (TileType)TilesData[y * Width + x];
        }

        public void SetTile(int x, int y, TileType type)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                TilesData[y * Width + x] = (int)type;
        }

        public char GetTileChar(int x, int y)
        {
            var tile = GetTile(x, y);
            return tile switch
            {
                TileType.Empty => ' ',
                TileType.Wall => '█',
                TileType.Ice => '~',
                TileType.Fire => '^',
                TileType.Water => '≈',
                TileType.IceGem => '◆',
                TileType.FireGem => '◇',
                TileType.IceDoor => '▣',
                TileType.FireDoor => '▢',
                TileType.Platform => '═',
                TileType.Button => '○',
                TileType.MovingPlatform => '▬',
                _ => '?'
            };
        }

        public ConsoleColor GetTileColor(int x, int y)
        {
            var tile = GetTile(x, y);
            return tile switch
            {
                TileType.Ice => ConsoleColor.Cyan,
                TileType.Fire => ConsoleColor.Red,
                TileType.Water => ConsoleColor.DarkBlue,
                TileType.IceGem => ConsoleColor.Blue,
                TileType.FireGem => ConsoleColor.DarkRed,
                TileType.IceDoor => ConsoleColor.Cyan,
                TileType.FireDoor => ConsoleColor.Red,
                TileType.Wall => ConsoleColor.Gray,
                TileType.Platform => ConsoleColor.DarkYellow,
                TileType.Button => ConsoleColor.Yellow,
                TileType.MovingPlatform => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
        }
    }

    /// <summary>
    /// 宝石位置
    /// </summary>
    [Serializable]
    public class GemPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public PlayerType ForPlayer { get; set; }
        public bool Collected { get; set; } = false;

        public GemPosition() { }

        public GemPosition(int x, int y, PlayerType forPlayer)
        {
            X = x;
            Y = y;
            ForPlayer = forPlayer;
        }
    }

    /// <summary>
    /// 完整的游戏状态
    /// </summary>
    [Serializable]
    public class GameState
    {
        public Player IcePlayer { get; set; }
        public Player FirePlayer { get; set; }
        public GameMap Map { get; set; }
        public bool GameOver { get; set; } = false;
        public bool Victory { get; set; } = false;
        public string Message { get; set; } = "";
        public int CurrentLevel { get; set; } = 1;
        public long GameTick { get; set; } = 0;

        public GameState() { }

        public bool BothPlayersReachedExit()
        {
            return IcePlayer?.ReachedExit == true && FirePlayer?.ReachedExit == true;
        }

        public bool AnyPlayerDead()
        {
            return IcePlayer?.IsAlive == false || FirePlayer?.IsAlive == false;
        }
    }
}

