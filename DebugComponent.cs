using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System;
using System.Linq;

namespace AiAndGamesJam {
    public class DebugComponent : DrawableGameComponent {
        private readonly CircularBuffer<long> _fps = new(120);
        private readonly CircularBuffer<long> _ticks = new(120);
        DateTime _last_frame;
        private SpriteFont _perfectVga;
        private Texture2D _pixel;

        private readonly AntGame _game;
        public DebugComponent(AntGame game) : base(game) {
#if !DEBUG
            Enabled = false;
#endif
            _game = game;
        }
        protected override void LoadContent() {
            Trace.WriteLine("Loading debug component content...");

            _perfectVga = ContentManager.GetFont("perfect_dos");
            Trace.Assert(_perfectVga != null, "Could not load font...");

            _pixel = new Texture2D(_game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixel.SetData(new Color[] { Color.White });
        }

        protected override void OnEnabledChanged(object sender, EventArgs args) {
            if (Enabled) {
                _fps.Clear();
                _ticks.Clear();
            }
        }

        public override void Update(GameTime gameTime) {
            if (!Enabled) return;
            _ticks.PushFront(gameTime.ElapsedGameTime.Ticks);
        }

        const long target = TimeSpan.TicksPerSecond / 60;

        public override void Draw(GameTime gameTime) {
            if (!Enabled) return;
            var now = DateTime.Now;
            if (_last_frame == default) {
                _last_frame = now;
                return;
            }
            var delta = now - _last_frame;
            _fps.PushFront(delta.Ticks);
            _last_frame = now;


            var count = Math.Min(_fps.Size, 60);
            for (int i = 0; i < count; i++) {
                var frame = _fps[i];
                var color = Color.Green;
                var diff = frame - target;
                if (diff > TimeSpan.TicksPerMillisecond)
                    color = Color.Orange;
                else if (diff > TimeSpan.TicksPerMillisecond * 2)
                    color = Color.Red;
                var ms = frame / TimeSpan.TicksPerMillisecond;
                _game.SpriteBatch.Draw(_pixel, new Rectangle(5 * i, 0, 3, (int)(ms * 2)), null, color, 0, Vector2.Zero, SpriteEffects.None, 0);
            }

            var fps = 1 / (_fps.Average() / TimeSpan.TicksPerSecond);
            var tps = _ticks.IsEmpty ? 0.0 : 1 / (_ticks.Average() / TimeSpan.TicksPerSecond);
            _game.SpriteBatch.DrawString(_perfectVga, $"{fps:0.00}fps / {tps:0.00}tps".Replace('∞', '?'), Vector2.Zero, Color.White);
        }
    }
}
