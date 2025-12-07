using System.Collections.Generic;

namespace FireboyAndWatergirl.Shared
{
    /// <summary>
    /// 关卡生成器 - 包含5个精心设计的关卡
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
            var map = new GameMap(40, 20, "第1关 - 新手教学");

            // 边界墙壁
            CreateBorder(map);

            // 地面平台
            for (int x = 1; x < map.Width - 1; x++)
            {
                map.SetTile(x, map.Height - 2, TileType.Platform);
            }

            // 简单的阶梯平台
            for (int x = 5; x < 12; x++)
                map.SetTile(x, 15, TileType.Platform);
            
            for (int x = 15; x < 25; x++)
                map.SetTile(x, 12, TileType.Platform);
            
            for (int x = 28; x < 38; x++)
                map.SetTile(x, 15, TileType.Platform);

            // 顶部平台（出口所在）
            for (int x = 8; x < 18; x++)
                map.SetTile(x, 8, TileType.Platform);
            
            for (int x = 22; x < 32; x++)
                map.SetTile(x, 8, TileType.Platform);

            // 宝石
            map.Gems = new List<GemPosition>
            {
                new GemPosition(8, 14, PlayerType.Ice),
                new GemPosition(10, 14, PlayerType.Fire),
                new GemPosition(30, 14, PlayerType.Ice),
                new GemPosition(33, 14, PlayerType.Fire),
                new GemPosition(18, 11, PlayerType.Ice),
                new GemPosition(22, 11, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口门
            map.SetTile(12, 7, TileType.IceDoor);
            map.SetTile(27, 7, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 5, map.Height - 3),
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
            var map = new GameMap(40, 20, "第2关 - 危险区域");
            CreateBorder(map);

            // 地面 - 分为三段，中间有危险区域
            for (int x = 1; x < 12; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);
            
            // 冰区域 - 只有Watergirl能过
            for (int x = 12; x < 17; x++)
                map.SetTile(x, map.Height - 2, TileType.Ice);
            
            for (int x = 17; x < 23; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);
            
            // 火区域 - 只有Fireboy能过
            for (int x = 23; x < 28; x++)
                map.SetTile(x, map.Height - 2, TileType.Fire);
            
            for (int x = 28; x < map.Width - 1; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);

            // 上层平台
            for (int x = 3; x < 10; x++)
                map.SetTile(x, 14, TileType.Platform);
            
            for (int x = 14; x < 26; x++)
                map.SetTile(x, 14, TileType.Platform);
            
            for (int x = 30; x < 37; x++)
                map.SetTile(x, 14, TileType.Platform);

            // 中层平台
            for (int x = 8; x < 15; x++)
                map.SetTile(x, 10, TileType.Platform);
            
            for (int x = 25; x < 32; x++)
                map.SetTile(x, 10, TileType.Platform);

            // 顶层平台
            for (int x = 15; x < 25; x++)
                map.SetTile(x, 6, TileType.Platform);

            // 水域（致命）
            map.SetTile(20, map.Height - 2, TileType.Water);

            // 宝石
            map.Gems = new List<GemPosition>
            {
                new GemPosition(5, 13, PlayerType.Ice),
                new GemPosition(7, 13, PlayerType.Fire),
                new GemPosition(33, 13, PlayerType.Ice),
                new GemPosition(35, 13, PlayerType.Fire),
                new GemPosition(10, 9, PlayerType.Ice),
                new GemPosition(12, 9, PlayerType.Fire),
                new GemPosition(27, 9, PlayerType.Ice),
                new GemPosition(29, 9, PlayerType.Fire),
                new GemPosition(18, 5, PlayerType.Ice),
                new GemPosition(21, 5, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口
            map.SetTile(17, 5, TileType.IceDoor);
            map.SetTile(22, 5, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 36, map.Height - 3),
                CurrentLevel = 2,
                Message = "第2关：小心危险区域！冰人走冰路，火人走火路"
            };
        }

        /// <summary>
        /// 第3关 - 合作挑战（简化版）
        /// 需要两人配合通过
        /// </summary>
        public static GameState CreateLevel3()
        {
            var map = new GameMap(40, 20, "第3关 - 合作挑战");
            CreateBorder(map);

            // 地面
            for (int x = 1; x < map.Width - 1; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);

            // 简单的阶梯平台
            // 左侧阶梯
            for (int x = 3; x < 12; x++)
                map.SetTile(x, 15, TileType.Platform);
            
            // 中央平台
            for (int x = 14; x < 26; x++)
                map.SetTile(x, 13, TileType.Platform);
            
            // 右侧阶梯
            for (int x = 28; x < 37; x++)
                map.SetTile(x, 15, TileType.Platform);

            // 顶层平台
            for (int x = 8; x < 18; x++)
                map.SetTile(x, 9, TileType.Platform);
            for (int x = 22; x < 32; x++)
                map.SetTile(x, 9, TileType.Platform);

            // 出口平台
            for (int x = 15; x < 25; x++)
                map.SetTile(x, 5, TileType.Platform);

            // 简单的危险区域
            map.SetTile(18, 13, TileType.Ice);  // 冰人专用
            map.SetTile(21, 13, TileType.Fire); // 火人专用

            // 宝石（减少数量）
            map.Gems = new List<GemPosition>
            {
                new GemPosition(6, 14, PlayerType.Ice),
                new GemPosition(33, 14, PlayerType.Fire),
                new GemPosition(16, 12, PlayerType.Ice),
                new GemPosition(23, 12, PlayerType.Fire),
                new GemPosition(11, 8, PlayerType.Ice),
                new GemPosition(28, 8, PlayerType.Fire),
                new GemPosition(18, 4, PlayerType.Ice),
                new GemPosition(21, 4, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口
            map.SetTile(17, 4, TileType.IceDoor);
            map.SetTile(22, 4, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 36, map.Height - 3),
                CurrentLevel = 3,
                Message = "第3关：合作通关！"
            };
        }

        /// <summary>
        /// 第4关 - 垂直攀登
        /// 需要从底部爬到顶部
        /// </summary>
        public static GameState CreateLevel4()
        {
            var map = new GameMap(35, 25, "第4关 - 垂直攀登");
            CreateBorder(map);

            // 底部平台
            for (int x = 1; x < map.Width - 1; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);

            // 交错的攀登平台 - 左右交替
            // 第1层
            for (int x = 3; x < 15; x++)
                map.SetTile(x, 21, TileType.Platform);
            
            // 第2层
            for (int x = 20; x < 32; x++)
                map.SetTile(x, 18, TileType.Platform);
            
            // 第3层
            for (int x = 3; x < 15; x++)
                map.SetTile(x, 15, TileType.Platform);
            
            // 第4层
            for (int x = 20; x < 32; x++)
                map.SetTile(x, 12, TileType.Platform);
            
            // 第5层
            for (int x = 3; x < 15; x++)
                map.SetTile(x, 9, TileType.Platform);
            
            // 第6层
            for (int x = 20; x < 32; x++)
                map.SetTile(x, 6, TileType.Platform);

            // 顶部出口平台
            for (int x = 10; x < 25; x++)
                map.SetTile(x, 3, TileType.Platform);

            // 中间连接小平台
            for (int x = 15; x < 20; x++)
            {
                map.SetTile(x, 19, TileType.Platform);
                map.SetTile(x, 13, TileType.Platform);
                map.SetTile(x, 7, TileType.Platform);
            }

            // 危险区域
            for (int x = 16; x < 19; x++)
                map.SetTile(x, 19, TileType.Ice);  // 冰桥
            for (int x = 16; x < 19; x++)
                map.SetTile(x, 7, TileType.Fire);  // 火桥
            
            // 水坑
            map.SetTile(17, 13, TileType.Water);

            // 宝石 - 每层都有
            map.Gems = new List<GemPosition>
            {
                // 底层
                new GemPosition(5, map.Height - 3, PlayerType.Ice),
                new GemPosition(30, map.Height - 3, PlayerType.Fire),
                // 第1层
                new GemPosition(8, 20, PlayerType.Fire),
                new GemPosition(12, 20, PlayerType.Ice),
                // 第2层
                new GemPosition(23, 17, PlayerType.Ice),
                new GemPosition(28, 17, PlayerType.Fire),
                // 第3层
                new GemPosition(6, 14, PlayerType.Fire),
                new GemPosition(11, 14, PlayerType.Ice),
                // 第4层
                new GemPosition(24, 11, PlayerType.Ice),
                new GemPosition(29, 11, PlayerType.Fire),
                // 第5层
                new GemPosition(7, 8, PlayerType.Fire),
                new GemPosition(10, 8, PlayerType.Ice),
                // 第6层
                new GemPosition(25, 5, PlayerType.Ice),
                new GemPosition(28, 5, PlayerType.Fire),
                // 顶层
                new GemPosition(14, 2, PlayerType.Ice),
                new GemPosition(20, 2, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口
            map.SetTile(13, 2, TileType.IceDoor);
            map.SetTile(21, 2, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 31, map.Height - 3),
                CurrentLevel = 4,
                Message = "第4关：向上攀登！小心中间的桥！"
            };
        }

        /// <summary>
        /// 第5关 - 终极挑战
        /// 综合所有元素的最终关卡
        /// </summary>
        public static GameState CreateLevel5()
        {
            var map = new GameMap(50, 25, "第5关 - 终极挑战");
            CreateBorder(map);

            // 复杂的地形设计
            // 底部 - 分隔的起点
            for (int x = 1; x < 10; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);
            for (int x = 40; x < map.Width - 1; x++)
                map.SetTile(x, map.Height - 2, TileType.Platform);

            // 中央水池
            for (int x = 15; x < 35; x++)
                map.SetTile(x, map.Height - 2, TileType.Water);

            // 跳跃平台穿越水池
            for (int x = 10; x < 15; x++)
                map.SetTile(x, map.Height - 2, TileType.Ice);  // 冰桥
            for (int x = 35; x < 40; x++)
                map.SetTile(x, map.Height - 2, TileType.Fire); // 火桥

            // 中间小平台
            for (int x = 22; x < 28; x++)
                map.SetTile(x, map.Height - 4, TileType.Platform);

            // 第二层
            for (int x = 3; x < 12; x++)
                map.SetTile(x, 19, TileType.Platform);
            for (int x = 16; x < 24; x++)
                map.SetTile(x, 19, TileType.Platform);
            for (int x = 26; x < 34; x++)
                map.SetTile(x, 19, TileType.Platform);
            for (int x = 38; x < 47; x++)
                map.SetTile(x, 19, TileType.Platform);

            // 第三层 - 危险区域
            for (int x = 8; x < 15; x++)
                map.SetTile(x, 15, TileType.Platform);
            for (int x = 15; x < 18; x++)
                map.SetTile(x, 15, TileType.Fire);
            for (int x = 18; x < 25; x++)
                map.SetTile(x, 15, TileType.Platform);
            for (int x = 25; x < 28; x++)
                map.SetTile(x, 15, TileType.Ice);
            for (int x = 28; x < 35; x++)
                map.SetTile(x, 15, TileType.Platform);
            for (int x = 35; x < 42; x++)
                map.SetTile(x, 15, TileType.Platform);

            // 第四层
            for (int x = 5; x < 20; x++)
                map.SetTile(x, 11, TileType.Platform);
            for (int x = 30; x < 45; x++)
                map.SetTile(x, 11, TileType.Platform);

            // 中央挑战区
            for (int x = 20; x < 30; x++)
                map.SetTile(x, 9, TileType.Platform);
            // 水障碍
            map.SetTile(24, 9, TileType.Water);
            map.SetTile(25, 9, TileType.Water);

            // 第五层
            for (int x = 10; x < 22; x++)
                map.SetTile(x, 6, TileType.Platform);
            for (int x = 28; x < 40; x++)
                map.SetTile(x, 6, TileType.Platform);

            // 顶层 - 出口
            for (int x = 20; x < 30; x++)
                map.SetTile(x, 3, TileType.Platform);

            // 宝石 - 分布在各层
            map.Gems = new List<GemPosition>
            {
                // 底层
                new GemPosition(5, map.Height - 3, PlayerType.Ice),
                new GemPosition(44, map.Height - 3, PlayerType.Fire),
                new GemPosition(24, map.Height - 5, PlayerType.Ice),
                new GemPosition(26, map.Height - 5, PlayerType.Fire),
                // 第二层
                new GemPosition(6, 18, PlayerType.Ice),
                new GemPosition(9, 18, PlayerType.Fire),
                new GemPosition(19, 18, PlayerType.Ice),
                new GemPosition(30, 18, PlayerType.Fire),
                new GemPosition(41, 18, PlayerType.Ice),
                new GemPosition(44, 18, PlayerType.Fire),
                // 第三层
                new GemPosition(10, 14, PlayerType.Fire),
                new GemPosition(21, 14, PlayerType.Ice),
                new GemPosition(31, 14, PlayerType.Fire),
                new GemPosition(38, 14, PlayerType.Ice),
                // 第四层
                new GemPosition(8, 10, PlayerType.Ice),
                new GemPosition(15, 10, PlayerType.Fire),
                new GemPosition(35, 10, PlayerType.Ice),
                new GemPosition(42, 10, PlayerType.Fire),
                // 中央
                new GemPosition(22, 8, PlayerType.Ice),
                new GemPosition(27, 8, PlayerType.Fire),
                // 第五层
                new GemPosition(14, 5, PlayerType.Fire),
                new GemPosition(18, 5, PlayerType.Ice),
                new GemPosition(32, 5, PlayerType.Fire),
                new GemPosition(36, 5, PlayerType.Ice),
                // 顶层
                new GemPosition(23, 2, PlayerType.Ice),
                new GemPosition(26, 2, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);

            // 出口
            map.SetTile(22, 2, TileType.IceDoor);
            map.SetTile(27, 2, TileType.FireDoor);

            return new GameState
            {
                Map = map,
                IcePlayer = new Player(PlayerType.Ice, 3, map.Height - 3),
                FirePlayer = new Player(PlayerType.Fire, 46, map.Height - 3),
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
                _ => CreateLevel1()  // 通关后返回第1关
            };
        }
    }
}
