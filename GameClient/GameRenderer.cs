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

