using System.Collections.Generic;

namespace FireboyAndWatergirl.Shared
{
    /// <summary>
    /// 关卡生成器 - 包含5个精心设计的关卡
    /// 注意：平台间距控制在3-4格，确保玩家能跳过去
    /// </summary>
    public static class LevelGenerator
    {
        public const int TotalLevels = 5;

        /// <summary>
        /// 第1关 - 新手教学
        /// 简单的平台布局，让玩家熟悉操作
        /// </summary>
        public static GameState CreateLevel1()
        {
            var map = new GameMap(40, 22, "第1关 - 新手教学");

            // 边界墙壁
            CreateBorder(map);

            // 地面平台
            for (int x = 1; x < map.Width - 1; x++)
            {
                map.SetTile(x, map.Height - 2, TileType.Platform);
            }

            // 第一层平台 (高度差4格，容易跳)
            for (int x = 3; x < 14; x++)
                map.SetTile(x, 17, TileType.Platform);
            
            for (int x = 26; x < 37; x++)
                map.SetTile(x, 17, TileType.Platform);

            // 中间连接平台
            for (int x = 16; x < 24; x++)
                map.SetTile(x, 15, TileType.Platform);

            // 第二层平台 (高度差3格)
            for (int x = 5; x < 16; x++)
                map.SetTile(x, 12, TileType.Platform);
            
            for (int x = 24; x < 35; x++)
                map.SetTile(x, 12, TileType.Platform);

            // 顶层平台 - 出口 (高度差4格)
            for (int x = 14; x < 26; x++)
                map.SetTile(x, 8, TileType.Platform);

            // 宝石 - 放在容易够到的位置
            map.Gems = new List<GemPosition>
            {
                new GemPosition(6, 16, PlayerType.Ice),
                new GemPosition(10, 16, PlayerType.Fire),
                new GemPosition(30, 16, PlayerType.Ice),
                new GemPosition(34, 16, PlayerType.Fire),
                new GemPosition(8, 11, PlayerType.Ice),
                new GemPosition(12, 11, PlayerType.Fire),
                new GemPosition(27, 11, PlayerType.Ice),
                new GemPosition(31, 11, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口门
            map.SetTile(17, 7, TileType.IceDoor);
            map.SetTile(22, 7, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 6, map.Height - 3),
                CurrentLevel = 1,
                Message = "第1关：收集宝石并到达出口！"
            };
        }

        /// <summary>
        /// 第2关 - 危险区域
        /// 引入冰火区域，需要分工合作
        /// </summary>
        public static GameState CreateLevel2()
        {
            var map = new GameMap(42, 22, "第2关 - 危险区域");
            CreateBorder(map);

            // 地面 - 分为三段，中间有危险区域
            for (int x = 1; x < 14; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);
            
            // 冰区域 - 只有Watergirl能过
            for (int x = 14; x < 18; x++)
                map.SetTile(x, map.Height - 2, TileType.Ice);
            
            for (int x = 18; x < 24; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);
            
            // 火区域 - 只有Fireboy能过
            for (int x = 24; x < 28; x++)
                map.SetTile(x, map.Height - 2, TileType.Fire);
            
            for (int x = 28; x < map.Width - 1; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);

            // 第一层平台 (高度差4格)
            for (int x = 3; x < 12; x++)
                map.SetTile(x, 16, TileType.Platform);
            
            for (int x = 15; x < 27; x++)
                map.SetTile(x, 16, TileType.Platform);
            
            for (int x = 30; x < 39; x++)
                map.SetTile(x, 16, TileType.Platform);

            // 第二层平台 (高度差4格)
            for (int x = 8; x < 18; x++)
                map.SetTile(x, 12, TileType.Platform);
            
            for (int x = 24; x < 34; x++)
                map.SetTile(x, 12, TileType.Platform);

            // 顶层平台 (高度差4格)
            for (int x = 14; x < 28; x++)
                map.SetTile(x, 8, TileType.Platform);

            // 宝石
            map.Gems = new List<GemPosition>
            {
                new GemPosition(5, 15, PlayerType.Ice),
                new GemPosition(8, 15, PlayerType.Fire),
                new GemPosition(33, 15, PlayerType.Ice),
                new GemPosition(36, 15, PlayerType.Fire),
                new GemPosition(11, 11, PlayerType.Ice),
                new GemPosition(14, 11, PlayerType.Fire),
                new GemPosition(27, 11, PlayerType.Ice),
                new GemPosition(30, 11, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口
            map.SetTile(17, 7, TileType.IceDoor);
            map.SetTile(24, 7, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 37, map.Height - 3),
                CurrentLevel = 2,
                Message = "第2关：小心危险区域！冰人走冰路，火人走火路"
            };
        }

        /// <summary>
        /// 第3关 - 合作挑战
        /// 需要两人配合通过
        /// </summary>
        public static GameState CreateLevel3()
        {
            var map = new GameMap(42, 22, "第3关 - 合作挑战");
            CreateBorder(map);

            // 地面
            for (int x = 1; x < map.Width - 1; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);

            // 第一层平台 (高度差4格)
            for (int x = 3; x < 14; x++)
                map.SetTile(x, 16, TileType.Platform);
            
            for (int x = 28; x < 39; x++)
                map.SetTile(x, 16, TileType.Platform);

            // 中间宽平台
            for (int x = 16; x < 26; x++)
                map.SetTile(x, 14, TileType.Platform);

            // 第二层平台 (高度差4格)
            for (int x = 6; x < 16; x++)
                map.SetTile(x, 10, TileType.Platform);
            for (int x = 26; x < 36; x++)
                map.SetTile(x, 10, TileType.Platform);

            // 顶层平台 - 出口
            for (int x = 14; x < 28; x++)
                map.SetTile(x, 6, TileType.Platform);

            // 简单的危险区域
            map.SetTile(19, 14, TileType.Ice);
            map.SetTile(22, 14, TileType.Fire);

            // 宝石
            map.Gems = new List<GemPosition>
            {
                new GemPosition(6, 15, PlayerType.Ice),
                new GemPosition(33, 15, PlayerType.Fire),
                new GemPosition(18, 13, PlayerType.Ice),
                new GemPosition(23, 13, PlayerType.Fire),
                new GemPosition(9, 9, PlayerType.Ice),
                new GemPosition(32, 9, PlayerType.Fire),
                new GemPosition(18, 5, PlayerType.Ice),
                new GemPosition(23, 5, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口
            map.SetTile(17, 5, TileType.IceDoor);
            map.SetTile(24, 5, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 38, map.Height - 3),
                CurrentLevel = 3,
                Message = "第3关：合作通关！"
            };
        }

        /// <summary>
        /// 第4关 - 垂直攀登
        /// 需要从底部爬到顶部，平台间距加大
        /// </summary>
        public static GameState CreateLevel4()
        {
            var map = new GameMap(38, 26, "第4关 - 垂直攀登");
            CreateBorder(map);

            // 底部平台
            for (int x = 1; x < map.Width - 1; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);

            // 交错的攀登平台 - 左右交替，间距3-4格
            // 第1层 (高度差3格)
            for (int x = 3; x < 16; x++)
                map.SetTile(x, 21, TileType.Platform);
            
            // 第2层 (高度差4格)
            for (int x = 22; x < 35; x++)
                map.SetTile(x, 17, TileType.Platform);
            
            // 第3层 (高度差4格)
            for (int x = 3; x < 16; x++)
                map.SetTile(x, 13, TileType.Platform);
            
            // 第4层 (高度差4格)
            for (int x = 22; x < 35; x++)
                map.SetTile(x, 9, TileType.Platform);

            // 顶部出口平台
            for (int x = 12; x < 26; x++)
                map.SetTile(x, 5, TileType.Platform);

            // 连接平台 - 更宽更好跳
            for (int x = 16; x < 22; x++)
            {
                map.SetTile(x, 19, TileType.Platform);
                map.SetTile(x, 11, TileType.Platform);
            }

            // 危险区域
            map.SetTile(18, 19, TileType.Ice);
            map.SetTile(19, 19, TileType.Ice);
            map.SetTile(18, 11, TileType.Fire);
            map.SetTile(19, 11, TileType.Fire);

            // 宝石 - 每层都有
            map.Gems = new List<GemPosition>
            {
                new GemPosition(5, map.Height - 3, PlayerType.Ice),
                new GemPosition(32, map.Height - 3, PlayerType.Fire),
                new GemPosition(8, 20, PlayerType.Fire),
                new GemPosition(12, 20, PlayerType.Ice),
                new GemPosition(26, 16, PlayerType.Ice),
                new GemPosition(30, 16, PlayerType.Fire),
                new GemPosition(6, 12, PlayerType.Fire),
                new GemPosition(11, 12, PlayerType.Ice),
                new GemPosition(27, 8, PlayerType.Ice),
                new GemPosition(31, 8, PlayerType.Fire),
                new GemPosition(16, 4, PlayerType.Ice),
                new GemPosition(21, 4, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口
            map.SetTile(15, 4, TileType.IceDoor);
            map.SetTile(22, 4, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 34, map.Height - 3),
                CurrentLevel = 4,
                Message = "第4关：向上攀登！"
            };
        }

        /// <summary>
        /// 第5关 - 终极挑战
        /// 综合所有元素的最终关卡，平台间距优化
        /// </summary>
        public static GameState CreateLevel5()
        {
            var map = new GameMap(48, 26, "第5关 - 终极挑战");
            CreateBorder(map);

            // 底部起点
            for (int x = 1; x < 12; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);
            for (int x = 36; x < map.Width - 1; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);

            // 中央平台 - 需要跳跃到达
            for (int x = 14; x < 22; x++)
                map.SetTile(x, map.Height - 4, TileType.Platform);
            for (int x = 26; x < 34; x++)
                map.SetTile(x, map.Height - 4, TileType.Platform);

            // 冰桥和火桥
            for (int x = 12; x < 14; x++)
                map.SetTile(x, map.Height - 2, TileType.Ice);
            for (int x = 34; x < 36; x++)
                map.SetTile(x, map.Height - 2, TileType.Fire);

            // 第一层平台 (高度差4格)
            for (int x = 3; x < 14; x++)
                map.SetTile(x, 18, TileType.Platform);
            for (int x = 18; x < 30; x++)
                map.SetTile(x, 18, TileType.Platform);
            for (int x = 34; x < 45; x++)
                map.SetTile(x, 18, TileType.Platform);

            // 第二层平台 (高度差4格)
            for (int x = 8; x < 20; x++)
                map.SetTile(x, 14, TileType.Platform);
            for (int x = 28; x < 40; x++)
                map.SetTile(x, 14, TileType.Platform);

            // 第三层平台 (高度差4格)
            for (int x = 4; x < 16; x++)
                map.SetTile(x, 10, TileType.Platform);
            for (int x = 32; x < 44; x++)
                map.SetTile(x, 10, TileType.Platform);

            // 中央挑战区
            for (int x = 18; x < 30; x++)
                map.SetTile(x, 10, TileType.Platform);

            // 顶层 - 出口
            for (int x = 18; x < 30; x++)
                map.SetTile(x, 6, TileType.Platform);

            // 危险区域
            map.SetTile(22, 10, TileType.Ice);
            map.SetTile(25, 10, TileType.Fire);

            // 宝石
            map.Gems = new List<GemPosition>
            {
                new GemPosition(5, map.Height - 3, PlayerType.Ice),
                new GemPosition(42, map.Height - 3, PlayerType.Fire),
                new GemPosition(17, map.Height - 5, PlayerType.Ice),
                new GemPosition(30, map.Height - 5, PlayerType.Fire),
                new GemPosition(6, 17, PlayerType.Ice),
                new GemPosition(10, 17, PlayerType.Fire),
                new GemPosition(37, 17, PlayerType.Ice),
                new GemPosition(41, 17, PlayerType.Fire),
                new GemPosition(11, 13, PlayerType.Ice),
                new GemPosition(16, 13, PlayerType.Fire),
                new GemPosition(31, 13, PlayerType.Ice),
                new GemPosition(36, 13, PlayerType.Fire),
                new GemPosition(7, 9, PlayerType.Ice),
                new GemPosition(12, 9, PlayerType.Fire),
                new GemPosition(35, 9, PlayerType.Ice),
                new GemPosition(40, 9, PlayerType.Fire),
                new GemPosition(21, 5, PlayerType.Ice),
                new GemPosition(26, 5, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口
            map.SetTile(20, 5, TileType.IceDoor);
            map.SetTile(27, 5, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 44, map.Height - 3),
                CurrentLevel = 5,
                Message = "终极挑战！收集所有宝石，冲向终点！"
            };
        }

        /// <summary>
        /// 创建边界墙壁
        /// </summary>
        private static void CreateBorder(GameMap map)
        {
            for (int x = 0; x < map.Width; x++)
            {
                map.SetTile(x, 0, TileType.Wall);
                map.SetTile(x, map.Height - 1, TileType.Wall);
            }
            for (int y = 0; y < map.Height; y++)
            {
                map.SetTile(0, y, TileType.Wall);
                map.SetTile(map.Width - 1, y, TileType.Wall);
            }
        }

        /// <summary>
        /// 根据关卡编号创建关卡
        /// </summary>
        public static GameState CreateLevel(int levelNumber)
        {
            return levelNumber switch
            {
                1 => CreateLevel1(),
                2 => CreateLevel2(),
                3 => CreateLevel3(),
                4 => CreateLevel4(),
                5 => CreateLevel5(),
                _ => CreateLevel1()
            };
        }
    }
}
