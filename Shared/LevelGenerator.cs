using System.Collections.Generic;

namespace IceFireMan.Shared
{
    /// <summary>
    /// 关卡生成器
    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>
        /// 创建第一关
        /// </summary>
        public static GameState CreateLevel1()
        {
            var map = new GameMap(40, 20, "森林冰火人 - 第一关");

            // 初始化为空（默认就是0/Empty，可以省略）

            // 创建边界墙壁
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

            // 地面平台
            for (int x = 1; x < map.Width - 1; x++)
            {
                map.SetTile(x, map.Height - 2, TileType.Platform);
            }

            // 第一层平台
            for (int x = 5; x < 15; x++)
            {
                map.SetTile(x, 15, TileType.Platform);
            }
            for (int x = 20; x < 35; x++)
            {
                map.SetTile(x, 15, TileType.Platform);
            }

            // 第二层平台
            for (int x = 10; x < 25; x++)
            {
                map.SetTile(x, 11, TileType.Platform);
            }

            // 第三层平台
            for (int x = 3; x < 12; x++)
            {
                map.SetTile(x, 7, TileType.Platform);
            }
            for (int x = 28; x < 38; x++)
            {
                map.SetTile(x, 7, TileType.Platform);
            }

            // 添加冰区域（冰人可通过，火人会死）
            for (int x = 16; x < 19; x++)
            {
                map.SetTile(x, map.Height - 2, TileType.Ice);
            }

            // 添加火区域（火人可通过，冰人会死）
            for (int x = 22; x < 25; x++)
            {
                map.SetTile(x, map.Height - 2, TileType.Fire);
            }

            // 添加水（两者都会死）
            map.SetTile(30, map.Height - 2, TileType.Water);
            map.SetTile(31, map.Height - 2, TileType.Water);

            // 添加宝石
            map.Gems = new List<GemPosition>
            {
                new GemPosition(8, 14, PlayerType.Ice),
                new GemPosition(12, 14, PlayerType.Fire),
                new GemPosition(25, 14, PlayerType.Ice),
                new GemPosition(30, 14, PlayerType.Fire),
                new GemPosition(15, 10, PlayerType.Ice),
                new GemPosition(20, 10, PlayerType.Fire),
                new GemPosition(5, 6, PlayerType.Ice),
                new GemPosition(8, 6, PlayerType.Fire),
                new GemPosition(32, 6, PlayerType.Ice),
                new GemPosition(35, 6, PlayerType.Fire),
            };

            // 在地图上标记宝石
            foreach (var gem in map.Gems)
            {
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);
            }

            // 添加出口门
            map.SetTile(6, 6, TileType.IceDoor);
            map.SetTile(33, 6, TileType.FireDoor);

            // 创建玩家
            var icePlayer = new Player(PlayerType.Ice, 3, map.Height - 3);
            var firePlayer = new Player(PlayerType.Fire, 5, map.Height - 3);

            return new GameState
            {
                Map = map,
                IcePlayer = icePlayer,
                FirePlayer = firePlayer,
                CurrentLevel = 1,
                GameOver = false,
                Victory = false,
                Message = "收集所有宝石并到达出口！冰人(蓝)走冰路，火人(红)走火路"
            };
        }

        /// <summary>
        /// 创建第二关
        /// </summary>
        public static GameState CreateLevel2()
        {
            var map = new GameMap(40, 20, "森林冰火人 - 第二关");

            // 创建边界墙壁
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

            // 地面 - 交替的冰火区域
            for (int x = 1; x < map.Width - 1; x++)
            {
                if (x < 10 || (x >= 20 && x < 30))
                    map.SetTile(x, map.Height - 2, TileType.Platform);
                else if (x < 15)
                    map.SetTile(x, map.Height - 2, TileType.Ice);
                else if (x < 20)
                    map.SetTile(x, map.Height - 2, TileType.Fire);
                else
                    map.SetTile(x, map.Height - 2, TileType.Water);
            }

            // 阶梯式平台
            for (int i = 0; i < 5; i++)
            {
                int y = 16 - i * 2;
                int startX = 3 + i * 3;
                for (int x = startX; x < startX + 5; x++)
                {
                    if (x < map.Width - 1)
                        map.SetTile(x, y, TileType.Platform);
                }
            }

            // 右侧阶梯
            for (int i = 0; i < 5; i++)
            {
                int y = 16 - i * 2;
                int startX = 35 - i * 3;
                for (int x = startX - 4; x < startX; x++)
                {
                    if (x > 0)
                        map.SetTile(x, y, TileType.Platform);
                }
            }

            // 中央平台
            for (int x = 15; x < 25; x++)
            {
                map.SetTile(x, 10, TileType.Platform);
            }

            // 顶部平台
            for (int x = 5; x < 15; x++)
            {
                map.SetTile(x, 5, TileType.Platform);
            }
            for (int x = 25; x < 35; x++)
            {
                map.SetTile(x, 5, TileType.Platform);
            }

            // 宝石
            map.Gems = new List<GemPosition>
            {
                new GemPosition(5, 15, PlayerType.Ice),
                new GemPosition(7, 13, PlayerType.Fire),
                new GemPosition(10, 11, PlayerType.Ice),
                new GemPosition(17, 9, PlayerType.Ice),
                new GemPosition(22, 9, PlayerType.Fire),
                new GemPosition(33, 15, PlayerType.Fire),
                new GemPosition(30, 13, PlayerType.Ice),
                new GemPosition(27, 11, PlayerType.Fire),
                new GemPosition(8, 4, PlayerType.Ice),
                new GemPosition(12, 4, PlayerType.Fire),
                new GemPosition(28, 4, PlayerType.Ice),
                new GemPosition(32, 4, PlayerType.Fire),
            };

            foreach (var gem in map.Gems)
            {
                map.SetTile(gem.X, gem.Y, gem.ForPlayer == PlayerType.Ice ? TileType.IceGem : TileType.FireGem);
            }

            // 出口
            map.SetTile(10, 4, TileType.IceDoor);
            map.SetTile(30, 4, TileType.FireDoor);

            var icePlayer = new Player(PlayerType.Ice, 3, map.Height - 3);
            var firePlayer = new Player(PlayerType.Fire, 5, map.Height - 3);

            return new GameState
            {
                Map = map,
                IcePlayer = icePlayer,
                FirePlayer = firePlayer,
                CurrentLevel = 2,
                GameOver = false,
                Victory = false,
                Message = "第二关更有挑战性！小心危险区域！"
            };
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
                _ => CreateLevel1()
            };
        }
    }
}
