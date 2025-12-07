using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using FireboyAndWatergirl.Shared;

namespace FireboyAndWatergirl.GameClient
{
    /// <summary>
    /// 游戏图形渲染器
    /// </summary>
    public class GameRenderer : IDisposable
    {
        private bool _disposed = false;
        // 颜色定义
        private readonly Color _backgroundColor = Color.FromArgb(20, 25, 35);
        private readonly Color _wallColor = Color.FromArgb(80, 85, 95);
        private readonly Color _platformColor = Color.FromArgb(139, 119, 101);
        private readonly Color _iceColor = Color.FromArgb(100, 180, 255);
        private readonly Color _fireColor = Color.FromArgb(255, 100, 50);
        private readonly Color _waterColor = Color.FromArgb(30, 80, 180);
        private readonly Color _iceGemColor = Color.FromArgb(0, 200, 255);
        private readonly Color _fireGemColor = Color.FromArgb(255, 150, 0);
        private readonly Color _iceDoorColor = Color.FromArgb(100, 200, 255);
        private readonly Color _fireDoorColor = Color.FromArgb(255, 120, 80);

        // 玩家颜色
        private readonly Color _icePlayerColor = Color.FromArgb(0, 180, 255);
        private readonly Color _icePlayerOutline = Color.FromArgb(200, 230, 255);
        private readonly Color _firePlayerColor = Color.FromArgb(255, 80, 30);
        private readonly Color _firePlayerOutline = Color.FromArgb(255, 200, 100);

        // 缓存的画笔和字体
        private Font _titleFont;
        private Font _messageFont;
        private Font _smallFont;

        public GameRenderer()
        {
            _titleFont = new Font("Microsoft YaHei", 24, FontStyle.Bold);
            _messageFont = new Font("Microsoft YaHei", 14, FontStyle.Bold);
            _smallFont = new Font("Microsoft YaHei", 10);
        }

        /// <summary>
        /// 渲染游戏画面
        /// </summary>
        public void Render(Graphics g, GameState state, Size panelSize, PlayerType localPlayer)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // 清除背景
            g.Clear(_backgroundColor);

            if (state?.Map == null) return;

            // 计算缩放和偏移
            float scaleX = (float)panelSize.Width / state.Map.Width;
            float scaleY = (float)panelSize.Height / state.Map.Height;
            float scale = Math.Min(scaleX, scaleY);
            
            float offsetX = (panelSize.Width - state.Map.Width * scale) / 2;
            float offsetY = (panelSize.Height - state.Map.Height * scale) / 2;

            // 绘制地图
            RenderMap(g, state.Map, scale, offsetX, offsetY);

            // 绘制玩家
            if (state.IcePlayer != null && state.IcePlayer.IsAlive)
                RenderPlayer(g, state.IcePlayer, scale, offsetX, offsetY, localPlayer == PlayerType.Ice);
            
            if (state.FirePlayer != null && state.FirePlayer.IsAlive)
                RenderPlayer(g, state.FirePlayer, scale, offsetX, offsetY, localPlayer == PlayerType.Fire);

            // 绘制游戏结束画面
            if (state.GameOver)
            {
                RenderGameOver(g, state, panelSize);
            }
        }

        /// <summary>
        /// 渲染地图
        /// </summary>
        private void RenderMap(Graphics g, GameMap map, float scale, float offsetX, float offsetY)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var tile = map.GetTile(x, y);
                    if (tile == TileType.Empty) continue;

                    float px = offsetX + x * scale;
                    float py = offsetY + y * scale;
                    RectangleF rect = new RectangleF(px, py, scale, scale);

                    RenderTile(g, tile, rect);
                }
            }
        }

        /// <summary>
        /// 渲染单个方块
        /// </summary>
        private void RenderTile(Graphics g, TileType tile, RectangleF rect)
        {
            Color color;
            bool isGem = false;
            bool isDoor = false;
            bool isHazard = false;

            switch (tile)
            {
                case TileType.Wall:
                    // 墙壁 - 带纹理效果
                    using (var brush = new LinearGradientBrush(rect, 
                        Color.FromArgb(100, 105, 115), Color.FromArgb(60, 65, 75), 45f))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    using (var pen = new Pen(Color.FromArgb(50, 55, 65), 1))
                    {
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    }
                    return;

                case TileType.Platform:
                    // 平台 - 木质效果
                    using (var brush = new LinearGradientBrush(rect,
                        Color.FromArgb(160, 140, 120), Color.FromArgb(120, 100, 80), 90f))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    using (var pen = new Pen(Color.FromArgb(100, 80, 60), 1))
                    {
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    }
                    return;

                case TileType.Ice:
                    isHazard = true;
                    // 冰区域 - 渐变蓝色
                    using (var brush = new LinearGradientBrush(rect,
                        Color.FromArgb(150, 200, 255, 255), Color.FromArgb(100, 150, 220), 90f))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    // 冰晶效果
                    using (var pen = new Pen(Color.FromArgb(180, 220, 255, 255), 1))
                    {
                        g.DrawLine(pen, rect.X + 2, rect.Y + 2, rect.X + rect.Width / 3, rect.Y + rect.Height / 2);
                        g.DrawLine(pen, rect.Right - 2, rect.Y + 2, rect.Right - rect.Width / 3, rect.Y + rect.Height / 2);
                    }
                    return;

                case TileType.Fire:
                    isHazard = true;
                    // 火区域 - 渐变红色
                    using (var brush = new LinearGradientBrush(rect,
                        Color.FromArgb(255, 150, 50), Color.FromArgb(200, 50, 0), 90f))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    // 火焰效果
                    float fireH = rect.Height * 0.6f;
                    PointF[] flame = new PointF[]
                    {
                        new PointF(rect.X + rect.Width * 0.2f, rect.Bottom),
                        new PointF(rect.X + rect.Width * 0.5f, rect.Bottom - fireH),
                        new PointF(rect.X + rect.Width * 0.8f, rect.Bottom)
                    };
                    using (var brush = new SolidBrush(Color.FromArgb(180, 255, 200, 50)))
                    {
                        g.FillPolygon(brush, flame);
                    }
                    return;

                case TileType.Water:
                    isHazard = true;
                    // 水 - 波浪效果
                    using (var brush = new LinearGradientBrush(rect,
                        Color.FromArgb(40, 100, 200), Color.FromArgb(20, 60, 150), 90f))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    // 波纹
                    using (var pen = new Pen(Color.FromArgb(100, 100, 180, 255), 1))
                    {
                        float waveY = rect.Y + rect.Height * 0.3f;
                        g.DrawArc(pen, rect.X, waveY - 3, rect.Width / 2, 6, 0, 180);
                        g.DrawArc(pen, rect.X + rect.Width / 2, waveY - 3, rect.Width / 2, 6, 180, 180);
                    }
                    return;

                case TileType.IceGem:
                    color = _iceGemColor;
                    isGem = true;
                    break;

                case TileType.FireGem:
                    color = _fireGemColor;
                    isGem = true;
                    break;

                case TileType.IceDoor:
                    color = _iceDoorColor;
                    isDoor = true;
                    break;

                case TileType.FireDoor:
                    color = _fireDoorColor;
                    isDoor = true;
                    break;

                default:
                    return;
            }

            if (isGem)
            {
                // 宝石 - 菱形
                float cx = rect.X + rect.Width / 2;
                float cy = rect.Y + rect.Height / 2;
                float size = Math.Min(rect.Width, rect.Height) * 0.35f;

                PointF[] diamond = new PointF[]
                {
                    new PointF(cx, cy - size),
                    new PointF(cx + size, cy),
                    new PointF(cx, cy + size),
                    new PointF(cx - size, cy)
                };

                // 发光效果
                using (var glowBrush = new SolidBrush(Color.FromArgb(50, color)))
                {
                    g.FillEllipse(glowBrush, cx - size * 1.5f, cy - size * 1.5f, size * 3, size * 3);
                }

                using (var brush = new LinearGradientBrush(
                    new PointF(cx, cy - size), new PointF(cx, cy + size),
                    Color.FromArgb(255, Color.White), color))
                {
                    g.FillPolygon(brush, diamond);
                }
                using (var pen = new Pen(Color.White, 1))
                {
                    g.DrawPolygon(pen, diamond);
                }
            }
            else if (isDoor)
            {
                // 出口门 - 拱门形状
                float doorWidth = rect.Width * 0.8f;
                float doorHeight = rect.Height * 0.9f;
                float doorX = rect.X + (rect.Width - doorWidth) / 2;
                float doorY = rect.Y + rect.Height - doorHeight;

                // 门框
                using (var brush = new LinearGradientBrush(rect, color, 
                    Color.FromArgb(color.R / 2, color.G / 2, color.B / 2), 90f))
                {
                    g.FillRectangle(brush, doorX, doorY + doorHeight * 0.3f, doorWidth, doorHeight * 0.7f);
                    g.FillEllipse(brush, doorX, doorY, doorWidth, doorHeight * 0.6f);
                }

                // 门内发光
                using (var innerBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 200)))
                {
                    float innerX = doorX + doorWidth * 0.15f;
                    float innerW = doorWidth * 0.7f;
                    g.FillRectangle(innerBrush, innerX, doorY + doorHeight * 0.4f, innerW, doorHeight * 0.55f);
                    g.FillEllipse(innerBrush, innerX, doorY + doorHeight * 0.1f, innerW, doorHeight * 0.5f);
                }

                // 边框
                using (var pen = new Pen(Color.FromArgb(200, Color.White), 2))
                {
                    g.DrawRectangle(pen, doorX, doorY + doorHeight * 0.3f, doorWidth, doorHeight * 0.7f);
                    g.DrawArc(pen, doorX, doorY, doorWidth, doorHeight * 0.6f, 180, 180);
                }
            }
        }

        /// <summary>
        /// 渲染玩家
        /// </summary>
        private void RenderPlayer(Graphics g, Player player, float scale, float offsetX, float offsetY, bool isLocalPlayer)
        {
            float px = offsetX + player.X * scale;
            float py = offsetY + player.Y * scale;
            float size = scale * 0.9f;
            float padding = (scale - size) / 2;

            Color bodyColor = player.Type == PlayerType.Ice ? _icePlayerColor : _firePlayerColor;
            Color outlineColor = player.Type == PlayerType.Ice ? _icePlayerOutline : _firePlayerOutline;

            // 玩家位置
            float cx = px + scale / 2;
            float cy = py + scale / 2;
            float radius = size / 2;

            // 发光效果（本地玩家更亮）
            if (isLocalPlayer)
            {
                using (var glowBrush = new SolidBrush(Color.FromArgb(60, bodyColor)))
                {
                    g.FillEllipse(glowBrush, cx - radius * 1.8f, cy - radius * 1.8f, radius * 3.6f, radius * 3.6f);
                }
            }

            // 身体 - 圆形
            using (var brush = new LinearGradientBrush(
                new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2),
                Color.FromArgb(255, Math.Min(255, bodyColor.R + 50), Math.Min(255, bodyColor.G + 50), Math.Min(255, bodyColor.B + 50)),
                bodyColor, 45f))
            {
                g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            // 轮廓
            using (var pen = new Pen(outlineColor, isLocalPlayer ? 3 : 2))
            {
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            // 眼睛
            float eyeSize = radius * 0.25f;
            float eyeY = cy - radius * 0.2f;
            float eyeSpacing = radius * 0.35f;

            using (var eyeBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(eyeBrush, cx - eyeSpacing - eyeSize / 2, eyeY - eyeSize / 2, eyeSize, eyeSize);
                g.FillEllipse(eyeBrush, cx + eyeSpacing - eyeSize / 2, eyeY - eyeSize / 2, eyeSize, eyeSize);
            }

            // 瞳孔
            float pupilSize = eyeSize * 0.5f;
            using (var pupilBrush = new SolidBrush(Color.Black))
            {
                g.FillEllipse(pupilBrush, cx - eyeSpacing - pupilSize / 2, eyeY - pupilSize / 2, pupilSize, pupilSize);
                g.FillEllipse(pupilBrush, cx + eyeSpacing - pupilSize / 2, eyeY - pupilSize / 2, pupilSize, pupilSize);
            }

            // 嘴巴
            using (var pen = new Pen(Color.FromArgb(200, 50, 50, 50), 2))
            {
                float mouthY = cy + radius * 0.3f;
                float mouthWidth = radius * 0.5f;
                g.DrawArc(pen, cx - mouthWidth / 2, mouthY - mouthWidth / 4, mouthWidth, mouthWidth / 2, 0, 180);
            }

            // 特效 - 冰人有雪花，火人有火焰
            if (player.Type == PlayerType.Ice)
            {
                // 冰晶特效
                using (var pen = new Pen(Color.FromArgb(150, 200, 230, 255), 1))
                {
                    float sparkleSize = radius * 0.3f;
                    // 顶部冰晶
                    g.DrawLine(pen, cx, cy - radius - sparkleSize, cx, cy - radius - 2);
                    g.DrawLine(pen, cx - sparkleSize / 2, cy - radius - sparkleSize / 2, cx + sparkleSize / 2, cy - radius - sparkleSize / 2);
                }
            }
            else
            {
                // 火焰特效
                float flameHeight = radius * 0.5f;
                PointF[] flame = new PointF[]
                {
                    new PointF(cx - radius * 0.3f, cy - radius),
                    new PointF(cx, cy - radius - flameHeight),
                    new PointF(cx + radius * 0.3f, cy - radius)
                };
                using (var brush = new SolidBrush(Color.FromArgb(180, 255, 200, 50)))
                {
                    g.FillPolygon(brush, flame);
                }
            }

            // 到达出口标记
            if (player.ReachedExit)
            {
                using (var pen = new Pen(Color.Gold, 3))
                {
                    g.DrawEllipse(pen, cx - radius * 1.3f, cy - radius * 1.3f, radius * 2.6f, radius * 2.6f);
                }
                using (var brush = new SolidBrush(Color.FromArgb(100, Color.Gold)))
                {
                    g.FillEllipse(brush, cx - radius * 1.3f, cy - radius * 1.3f, radius * 2.6f, radius * 2.6f);
                }
            }
        }

        /// <summary>
        /// 渲染游戏结束画面
        /// </summary>
        private void RenderGameOver(Graphics g, GameState state, Size panelSize)
        {
            // 半透明遮罩
            using (var brush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillRectangle(brush, 0, 0, panelSize.Width, panelSize.Height);
            }

            string title = state.Victory ? "🎉 恭喜通关！" : "💀 游戏结束";
            Color titleColor = state.Victory ? Color.Gold : Color.Red;

            // 标题
            var titleSize = g.MeasureString(title, _titleFont);
            float titleX = (panelSize.Width - titleSize.Width) / 2;
            float titleY = panelSize.Height / 2 - 60;

            // 阴影
            using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                g.DrawString(title, _titleFont, shadowBrush, titleX + 3, titleY + 3);
            }
            using (var brush = new SolidBrush(titleColor))
            {
                g.DrawString(title, _titleFont, brush, titleX, titleY);
            }

            // 消息
            string message = state.Message;
            var msgSize = g.MeasureString(message, _messageFont);
            float msgX = (panelSize.Width - msgSize.Width) / 2;
            float msgY = titleY + 60;

            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(message, _messageFont, brush, msgX, msgY);
            }

            // 提示
            string hint = "按 R 键重新开始";
            var hintSize = g.MeasureString(hint, _smallFont);
            float hintX = (panelSize.Width - hintSize.Width) / 2;
            float hintY = msgY + 50;

            using (var brush = new SolidBrush(Color.LightGray))
            {
                g.DrawString(hint, _smallFont, brush, hintX, hintY);
            }
        }

        /// <summary>
        /// 渲染等待画面
        /// </summary>
        public void RenderWaitingScreen(Graphics g, Size panelSize, string message)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_backgroundColor);

            // 标题
            string title = "❄ 森林冰火人 🔥";
            var titleSize = g.MeasureString(title, _titleFont);
            float titleX = (panelSize.Width - titleSize.Width) / 2;
            float titleY = panelSize.Height / 2 - 80;

            // 渐变背景
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, panelSize.Width, panelSize.Height),
                Color.FromArgb(30, 40, 60), Color.FromArgb(20, 25, 35), 90f))
            {
                g.FillRectangle(brush, 0, 0, panelSize.Width, panelSize.Height);
            }

            // 装饰性冰晶和火焰
            DrawDecorativeIce(g, panelSize.Width * 0.15f, panelSize.Height * 0.3f, 40);
            DrawDecorativeIce(g, panelSize.Width * 0.1f, panelSize.Height * 0.6f, 30);
            DrawDecorativeFire(g, panelSize.Width * 0.85f, panelSize.Height * 0.3f, 40);
            DrawDecorativeFire(g, panelSize.Width * 0.9f, panelSize.Height * 0.6f, 30);

            // 标题
            using (var brush = new LinearGradientBrush(
                new RectangleF(titleX, titleY, titleSize.Width, titleSize.Height),
                Color.Cyan, Color.Orange, 0f))
            {
                g.DrawString(title, _titleFont, brush, titleX, titleY);
            }

            // 等待消息
            var msgSize = g.MeasureString(message, _messageFont);
            float msgX = (panelSize.Width - msgSize.Width) / 2;
            float msgY = titleY + 80;

            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(message, _messageFont, brush, msgX, msgY);
            }

            // 动画点
            string dots = new string('.', (int)(DateTime.Now.Millisecond / 333) + 1);
            using (var brush = new SolidBrush(Color.LightGray))
            {
                g.DrawString(dots, _messageFont, brush, msgX + msgSize.Width, msgY);
            }
        }

        private void DrawDecorativeIce(Graphics g, float x, float y, float size)
        {
            using (var brush = new SolidBrush(Color.FromArgb(60, 100, 200, 255)))
            {
                PointF[] crystal = new PointF[]
                {
                    new PointF(x, y - size),
                    new PointF(x + size * 0.5f, y - size * 0.3f),
                    new PointF(x + size * 0.3f, y + size * 0.5f),
                    new PointF(x - size * 0.3f, y + size * 0.5f),
                    new PointF(x - size * 0.5f, y - size * 0.3f)
                };
                g.FillPolygon(brush, crystal);
            }
        }

        /// <summary>
        /// 渲染等待大厅
        /// </summary>
        public void RenderLobby(Graphics g, Size panelSize, int playerCount, bool myReady, bool otherReady, 
            PlayerType myType, string otherPlayerName)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // 背景渐变
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, panelSize.Width, panelSize.Height),
                Color.FromArgb(15, 25, 45), Color.FromArgb(35, 15, 35), 135f))
            {
                g.FillRectangle(brush, 0, 0, panelSize.Width, panelSize.Height);
            }

            // 装饰元素
            DrawDecorativeIce(g, panelSize.Width * 0.08f, panelSize.Height * 0.15f, 60);
            DrawDecorativeIce(g, panelSize.Width * 0.12f, panelSize.Height * 0.75f, 40);
            DrawDecorativeFire(g, panelSize.Width * 0.92f, panelSize.Height * 0.15f, 60);
            DrawDecorativeFire(g, panelSize.Width * 0.88f, panelSize.Height * 0.75f, 40);

            float centerX = panelSize.Width / 2;
            float startY = panelSize.Height * 0.08f;

            // 大标题
            string title = "🎮 游戏大厅";
            var titleSize = g.MeasureString(title, _titleFont);
            using (var brush = new LinearGradientBrush(
                new RectangleF(centerX - titleSize.Width / 2, startY, titleSize.Width, titleSize.Height),
                Color.Gold, Color.Orange, 0f))
            {
                g.DrawString(title, _titleFont, brush, centerX - titleSize.Width / 2, startY);
            }

            // 房间信息框
            float boxWidth = 500;
            float boxHeight = 350;
            float boxX = centerX - boxWidth / 2;
            float boxY = startY + 80;

            // 绘制房间框背景
            using (var brush = new SolidBrush(Color.FromArgb(40, 40, 60)))
            {
                g.FillRectangle(brush, boxX, boxY, boxWidth, boxHeight);
            }
            using (var pen = new Pen(Color.FromArgb(80, 150, 200), 2))
            {
                g.DrawRectangle(pen, boxX, boxY, boxWidth, boxHeight);
            }

            // 房间标题
            string roomTitle = $"房间状态: {playerCount}/2 玩家";
            var roomTitleSize = g.MeasureString(roomTitle, _messageFont);
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(roomTitle, _messageFont, brush, centerX - roomTitleSize.Width / 2, boxY + 20);
            }

            // 分隔线
            using (var pen = new Pen(Color.FromArgb(60, 100, 150), 1))
            {
                g.DrawLine(pen, boxX + 30, boxY + 60, boxX + boxWidth - 30, boxY + 60);
            }

            // 玩家1 (自己)
            float player1Y = boxY + 80;
            string myTypeStr = myType == PlayerType.Ice ? "💧 Watergirl" : "🔥 Fireboy";
            string myStatus = myReady ? "✅ 已准备" : "⏳ 等待中";
            
            DrawPlayerCard(g, boxX + 30, player1Y, boxWidth - 60, 90, 
                "你", myTypeStr, myStatus, myReady, myType == PlayerType.Ice);

            // 玩家2 (对方)
            float player2Y = player1Y + 110;
            if (playerCount >= 2)
            {
                string otherTypeStr = myType == PlayerType.Ice ? "🔥 Fireboy" : "💧 Watergirl";
                string otherStatus = otherReady ? "✅ 已准备" : "⏳ 等待中";
                string otherName = string.IsNullOrEmpty(otherPlayerName) ? "玩家2" : otherPlayerName;
                
                DrawPlayerCard(g, boxX + 30, player2Y, boxWidth - 60, 90, 
                    otherName, otherTypeStr, otherStatus, otherReady, myType != PlayerType.Ice);
            }
            else
            {
                // 等待玩家加入
                DrawEmptyPlayerSlot(g, boxX + 30, player2Y, boxWidth - 60, 90);
            }

            // 操作提示
            float hintY = boxY + boxHeight + 30;
            
            string hint1 = "点击右侧 [准备] 按钮准备游戏";
            string hint2 = playerCount >= 2 && myReady && otherReady ? 
                "✨ 两人都已准备，点击 [开始游戏] 开始！" : 
                "等待所有玩家准备...";

            using (var brush = new SolidBrush(Color.LightGray))
            {
                var hint1Size = g.MeasureString(hint1, _smallFont);
                g.DrawString(hint1, _smallFont, brush, centerX - hint1Size.Width / 2, hintY);
            }

            using (var brush = new SolidBrush(playerCount >= 2 && myReady && otherReady ? Color.LightGreen : Color.Yellow))
            {
                var hint2Size = g.MeasureString(hint2, _smallFont);
                g.DrawString(hint2, _smallFont, brush, centerX - hint2Size.Width / 2, hintY + 30);
            }

            // 动画点
            string dots = new string('.', (int)(DateTime.Now.Millisecond / 250) % 4);
            using (var brush = new SolidBrush(Color.Gray))
            {
                g.DrawString(dots, _messageFont, brush, centerX + 50, hintY + 25);
            }
        }

        private void DrawPlayerCard(Graphics g, float x, float y, float width, float height,
            string name, string type, string status, bool isReady, bool isIce)
        {
            // 卡片背景
            Color bgColor = isReady ? 
                Color.FromArgb(30, 80, 30) : Color.FromArgb(50, 50, 60);
            Color borderColor = isIce ? 
                Color.FromArgb(100, 180, 255) : Color.FromArgb(255, 150, 100);

            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, x, y, width, height);
            }
            using (var pen = new Pen(borderColor, 2))
            {
                g.DrawRectangle(pen, x, y, width, height);
            }

            // 玩家图标
            float iconSize = 50;
            float iconX = x + 20;
            float iconY = y + (height - iconSize) / 2;

            if (isIce)
            {
                using (var brush = new LinearGradientBrush(
                    new RectangleF(iconX, iconY, iconSize, iconSize),
                    Color.Cyan, Color.DodgerBlue, 90f))
                {
                    g.FillEllipse(brush, iconX, iconY, iconSize, iconSize);
                }
            }
            else
            {
                using (var brush = new LinearGradientBrush(
                    new RectangleF(iconX, iconY, iconSize, iconSize),
                    Color.Orange, Color.Red, 90f))
                {
                    g.FillEllipse(brush, iconX, iconY, iconSize, iconSize);
                }
            }

            // 玩家名称
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(name, _messageFont, brush, x + 90, y + 15);
            }

            // 角色类型
            using (var brush = new SolidBrush(isIce ? Color.Cyan : Color.Orange))
            {
                g.DrawString(type, _smallFont, brush, x + 90, y + 40);
            }

            // 状态
            using (var brush = new SolidBrush(isReady ? Color.LightGreen : Color.Yellow))
            {
                g.DrawString(status, _smallFont, brush, x + width - 100, y + 35);
            }
        }

        private void DrawEmptyPlayerSlot(Graphics g, float x, float y, float width, float height)
        {
            // 虚线边框
            using (var pen = new Pen(Color.FromArgb(80, 80, 100), 2))
            {
                pen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(pen, x, y, width, height);
            }

            // 等待文字
            string waitText = "⏳ 等待玩家加入...";
            var textSize = g.MeasureString(waitText, _messageFont);
            using (var brush = new SolidBrush(Color.Gray))
            {
                g.DrawString(waitText, _messageFont, brush, 
                    x + (width - textSize.Width) / 2, 
                    y + (height - textSize.Height) / 2);
            }
        }

        private void DrawDecorativeFire(Graphics g, float x, float y, float size)
        {
            using (var brush = new SolidBrush(Color.FromArgb(60, 255, 100, 50)))
            {
                PointF[] flame = new PointF[]
                {
                    new PointF(x - size * 0.4f, y + size),
                    new PointF(x - size * 0.2f, y),
                    new PointF(x, y - size),
                    new PointF(x + size * 0.2f, y),
                    new PointF(x + size * 0.4f, y + size)
                };
                g.FillPolygon(brush, flame);
            }
        }

        /// <summary>
        /// 渲染关卡选择菜单
        /// </summary>
        public void RenderMenu(Graphics g, Size panelSize, int selectedLevel, bool isConnected)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // 背景渐变
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, panelSize.Width, panelSize.Height),
                Color.FromArgb(20, 30, 50), Color.FromArgb(40, 20, 30), 45f))
            {
                g.FillRectangle(brush, 0, 0, panelSize.Width, panelSize.Height);
            }

            // 装饰
            DrawDecorativeIce(g, panelSize.Width * 0.1f, panelSize.Height * 0.2f, 50);
            DrawDecorativeIce(g, panelSize.Width * 0.15f, panelSize.Height * 0.7f, 35);
            DrawDecorativeFire(g, panelSize.Width * 0.9f, panelSize.Height * 0.2f, 50);
            DrawDecorativeFire(g, panelSize.Width * 0.85f, panelSize.Height * 0.7f, 35);

            // 标题
            string title = "🔥 Fireboy and Watergirl 💧";
            var titleSize = g.MeasureString(title, _titleFont);
            float titleX = (panelSize.Width - titleSize.Width) / 2;
            float titleY = panelSize.Height * 0.08f;

            using (var brush = new LinearGradientBrush(
                new RectangleF(titleX, titleY, titleSize.Width, titleSize.Height),
                Color.Orange, Color.Cyan, 0f))
            {
                g.DrawString(title, _titleFont, brush, titleX, titleY);
            }

            // 副标题
            string subtitle = "选择关卡";
            var subtitleSize = g.MeasureString(subtitle, _messageFont);
            float subtitleX = (panelSize.Width - subtitleSize.Width) / 2;
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(subtitle, _messageFont, brush, subtitleX, titleY + 50);
            }

            // 关卡按钮
            string[] levelNames = {
                "第1关 - 新手教学",
                "第2关 - 危险区域",
                "第3关 - 迷宫挑战",
                "第4关 - 垂直攀登",
                "第5关 - 终极挑战"
            };

            float buttonWidth = 280;
            float buttonHeight = 50;
            float buttonStartY = panelSize.Height * 0.25f;
            float buttonSpacing = 60;
            float buttonX = (panelSize.Width - buttonWidth) / 2;

            for (int i = 0; i < 5; i++)
            {
                float buttonY = buttonStartY + i * buttonSpacing;
                bool isSelected = (i + 1) == selectedLevel;

                // 按钮背景
                var buttonRect = new RectangleF(buttonX, buttonY, buttonWidth, buttonHeight);
                
                if (isSelected)
                {
                    // 选中状态 - 高亮
                    using (var brush = new LinearGradientBrush(buttonRect,
                        Color.FromArgb(80, 150, 220), Color.FromArgb(60, 100, 180), 90f))
                    {
                        g.FillRectangle(brush, buttonRect);
                    }
                    using (var pen = new Pen(Color.Cyan, 3))
                    {
                        g.DrawRectangle(pen, buttonX, buttonY, buttonWidth, buttonHeight);
                    }
                }
                else
                {
                    // 未选中状态
                    using (var brush = new SolidBrush(Color.FromArgb(50, 50, 70)))
                    {
                        g.FillRectangle(brush, buttonRect);
                    }
                    using (var pen = new Pen(Color.FromArgb(80, 80, 100), 1))
                    {
                        g.DrawRectangle(pen, buttonX, buttonY, buttonWidth, buttonHeight);
                    }
                }

                // 关卡编号
                string levelNum = $"{i + 1}";
                using (var brush = new SolidBrush(isSelected ? Color.Yellow : Color.Orange))
                {
                    g.DrawString(levelNum, _titleFont, brush, buttonX + 15, buttonY + 8);
                }

                // 关卡名称
                using (var brush = new SolidBrush(isSelected ? Color.White : Color.LightGray))
                {
                    g.DrawString(levelNames[i], _smallFont, brush, buttonX + 55, buttonY + 15);
                }
            }

            // 操作提示
            string hint1 = "按 1-5 选择关卡";
            string hint2 = "按 Enter 开始游戏";
            string hint3 = isConnected ? "✅ 已连接服务器" : "❌ 未连接服务器";

            float hintY = buttonStartY + 5 * buttonSpacing + 30;
            
            using (var brush = new SolidBrush(Color.LightGray))
            {
                var hint1Size = g.MeasureString(hint1, _smallFont);
                g.DrawString(hint1, _smallFont, brush, (panelSize.Width - hint1Size.Width) / 2, hintY);
            }
            
            using (var brush = new SolidBrush(Color.Gold))
            {
                var hint2Size = g.MeasureString(hint2, _smallFont);
                g.DrawString(hint2, _smallFont, brush, (panelSize.Width - hint2Size.Width) / 2, hintY + 25);
            }

            using (var brush = new SolidBrush(isConnected ? Color.LightGreen : Color.Red))
            {
                var hint3Size = g.MeasureString(hint3, _smallFont);
                g.DrawString(hint3, _smallFont, brush, (panelSize.Width - hint3Size.Width) / 2, hintY + 55);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    _titleFont?.Dispose();
                    _messageFont?.Dispose();
                    _smallFont?.Dispose();
                }
                _disposed = true;
            }
        }

        ~GameRenderer()
        {
            Dispose(false);
        }
    }
}

