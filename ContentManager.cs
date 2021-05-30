using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace AiAndGamesJam {
    public static class ContentManager {
        static readonly Dictionary<string, Texture2D> _textures = new() {
            { "anthill", null },
            { "ant", null },
        };

        static readonly Dictionary<string, SpriteFont> _fonts = new() {
            { "perfect_dos", null },
        };

        public static void Load(AntGame _game) {
            _textures.Keys.ToList().ForEach(key => {
                _textures[key] = _game.Content.Load<Texture2D>(key);
                System.Diagnostics.Trace.WriteLine($"Loaded texture '{key}'", nameof(ContentManager));
            });

            _fonts.Keys.ToList().ForEach(key => {
                _fonts[key] = _game.Content.Load<SpriteFont>(key);
                System.Diagnostics.Trace.WriteLine($"Loaded font '{key}'", nameof(ContentManager));
            });

            // Texture2D _pixel = new(_game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            // _pixel.SetData(new Color[] { Color.White });
            // _textures.Add("pixel", _pixel);
        }

        public static Texture2D GetTexture(string name) => _textures.ContainsKey(name) ? _textures[name] : null;
        public static SpriteFont GetFont(string name) => _fonts.ContainsKey(name) ? _fonts[name] : null;
    }
}
