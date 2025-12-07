using System;
using IceFireMan.Shared;

namespace IceFireMan.Server
{
    /// <summary>
    /// 游戏逻辑处理器
    /// </summary>
    public class GameLogic
    {
        /// <summary>
        /// 更新游戏状态
        /// </summary>
        public void Update(GameState state, PlayerAction iceInput, PlayerAction fireInput)
        {
            if (state.GameOver) return;

            // 更新玩家
            UpdatePlayer(state.IcePlayer, iceInput, state);
            UpdatePlayer(state.FirePlayer, fireInput, state);

            // 检查宝石收集
            CheckGemCollection(state);

            // 检查胜利条件
            CheckVictoryCondition(state);

            // 检查失败条件
            CheckDefeatCondition(state);
        }

        /// <summary>
        /// 更新单个玩家
        /// </summary>
        private void UpdatePlayer(Player player, PlayerAction input, GameState state)
        {
            if (!player.IsAlive || player.ReachedExit) return;

            // 处理水平移动
            if ((input & PlayerAction.MoveLeft) != 0)
            {
                player.VelocityX = -GameConfig.MoveSpeed;
            }
            else if ((input & PlayerAction.MoveRight) != 0)
            {
                player.VelocityX = GameConfig.MoveSpeed;
            }
            else
            {
                player.VelocityX *= GameConfig.Friction;
                if (Math.Abs(player.VelocityX) < 0.1f)
                    player.VelocityX = 0;
            }

            // 处理跳跃
            if ((input & PlayerAction.Jump) != 0 && player.IsOnGround)
            {
                player.VelocityY = GameConfig.JumpForce;
                player.IsOnGround = false;
            }

            // 应用重力
            player.VelocityY += GameConfig.Gravity;
            if (player.VelocityY > GameConfig.MaxFallSpeed)
                player.VelocityY = GameConfig.MaxFallSpeed;

            // 计算新位置
            float newX = player.X + player.VelocityX;
            float newY = player.Y + player.VelocityY;

            // 水平碰撞检测
            if (!IsValidPosition(newX, player.Y, player, state))
            {
                newX = player.X;
                player.VelocityX = 0;
            }

            // 垂直碰撞检测
            if (!IsValidPosition(newX, newY, player, state))
            {
                if (player.VelocityY > 0)
                {
                    // 落地 - 将玩家放在碰撞方块上方
                    player.IsOnGround = true;
                    // 找到脚下的地面位置
                    newY = (float)Math.Floor(player.Y + player.VelocityY);
                    // 向上调整直到不再碰撞
                    while (!IsValidPosition(newX, newY, player, state) && newY > player.Y - 1)
                    {
                        newY -= 0.1f;
                    }
                }
                else
                {
                    // 撞到天花板
                    newY = (float)Math.Ceiling(player.Y);
                }
                player.VelocityY = 0;
            }
            else
            {
                // 检查玩家脚下是否有地面（判断是否在空中）
                bool groundBelow = !IsValidPosition(newX, newY + 0.1f, player, state);
                player.IsOnGround = groundBelow && player.VelocityY >= 0;
            }

            // 更新位置
            player.X = newX;
            player.Y = newY;

            // 检查危险区域
            CheckHazards(player, state);

            // 检查是否到达出口
            CheckExit(player, state);
        }

        /// <summary>
        /// 检查位置是否有效（碰撞检测）
        /// </summary>
        private bool IsValidPosition(float x, float y, Player player, GameState state)
        {
            // 检查玩家四个角的碰撞
            int left = (int)x;
            int right = (int)(x + GameConfig.PlayerWidth - 0.1f);
            int top = (int)y;
            int bottom = (int)(y + GameConfig.PlayerHeight - 0.1f);

            // 检查每个角
            for (int checkY = top; checkY <= bottom; checkY++)
            {
                for (int checkX = left; checkX <= right; checkX++)
                {
                    var tile = state.Map.GetTile(checkX, checkY);
                    if (IsSolidTile(tile))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 检查是否是实体方块
        /// </summary>
        private bool IsSolidTile(TileType tile)
        {
            return tile == TileType.Wall || tile == TileType.Platform;
        }

        /// <summary>
        /// 检查危险区域
        /// </summary>
        private void CheckHazards(Player player, GameState state)
        {
            int x = (int)(player.X + 0.5f);
            int y = (int)(player.Y + 0.5f);
            var tile = state.Map.GetTile(x, y);

            // 水对所有人都是致命的
            if (tile == TileType.Water)
            {
                player.IsAlive = false;
                state.Message = $"{(player.Type == PlayerType.Ice ? "冰人" : "火人")}掉进水里了！";
                return;
            }

            // 冰人怕火
            if (player.Type == PlayerType.Ice && tile == TileType.Fire)
            {
                player.IsAlive = false;
                state.Message = "冰人被火融化了！";
                return;
            }

            // 火人怕冰
            if (player.Type == PlayerType.Fire && tile == TileType.Ice)
            {
                player.IsAlive = false;
                state.Message = "火人被冰冻住了！";
                return;
            }
        }

        /// <summary>
        /// 检查出口
        /// </summary>
        private void CheckExit(Player player, GameState state)
        {
            int x = (int)(player.X + 0.5f);
            int y = (int)(player.Y + 0.5f);
            var tile = state.Map.GetTile(x, y);

            // 冰人到达冰门
            if (player.Type == PlayerType.Ice && tile == TileType.IceDoor)
            {
                player.ReachedExit = true;
            }

            // 火人到达火门
            if (player.Type == PlayerType.Fire && tile == TileType.FireDoor)
            {
                player.ReachedExit = true;
            }
        }

        /// <summary>
        /// 检查宝石收集
        /// </summary>
        private void CheckGemCollection(GameState state)
        {
            foreach (var gem in state.Map.Gems)
            {
                if (gem.Collected) continue;

                // 检查冰人是否收集冰宝石
                if (gem.ForPlayer == PlayerType.Ice)
                {
                    float iceX = state.IcePlayer.X + 0.5f;
                    float iceY = state.IcePlayer.Y + 0.5f;
                    
                    if (Math.Abs(iceX - gem.X) < 1.0f && Math.Abs(iceY - gem.Y) < 1.0f)
                    {
                        gem.Collected = true;
                        state.IcePlayer.GemsCollected++;
                        state.Map.SetTile(gem.X, gem.Y, TileType.Empty);
                    }
                }

                // 检查火人是否收集火宝石
                if (gem.ForPlayer == PlayerType.Fire)
                {
                    float fireX = state.FirePlayer.X + 0.5f;
                    float fireY = state.FirePlayer.Y + 0.5f;
                    
                    if (Math.Abs(fireX - gem.X) < 1.0f && Math.Abs(fireY - gem.Y) < 1.0f)
                    {
                        gem.Collected = true;
                        state.FirePlayer.GemsCollected++;
                        state.Map.SetTile(gem.X, gem.Y, TileType.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// 检查胜利条件
        /// </summary>
        private void CheckVictoryCondition(GameState state)
        {
            if (state.BothPlayersReachedExit())
            {
                state.GameOver = true;
                state.Victory = true;
                state.Message = "🎉 恭喜通关！双方都到达了出口！";
            }
        }

        /// <summary>
        /// 检查失败条件
        /// </summary>
        private void CheckDefeatCondition(GameState state)
        {
            if (state.AnyPlayerDead())
            {
                state.GameOver = true;
                state.Victory = false;
            }
        }
    }
}

