using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Collections.Generic;

namespace AiAndGamesJam {
    public static class ContentManager {
        static readonly Dictionary<string, Texture2D> _textures = new();

        static readonly Dictionary<string, SpriteFont> _fonts = new() {
            { "perfect_dos", null },
        };

        public static void Load(Microsoft.Xna.Framework.Content.ContentManager Content) {
            _textures.Keys.ToList().ForEach(key => {
                _textures[key] = Content.Load<Texture2D>(key);
                System.Diagnostics.Trace.WriteLine($"Loaded texture '{key}'", nameof(ContentManager));
            });

            _fonts.Keys.ToList().ForEach(key => {
                _fonts[key] = Content.Load<SpriteFont>(key);
                System.Diagnostics.Trace.WriteLine($"Loaded font '{key}'", nameof(ContentManager));
            });
        }

        public static Texture2D GetTexture(string name) => _textures.ContainsKey(name) ? _textures[name] : null;
        public static SpriteFont GetFont(string name) => _fonts.ContainsKey(name) ? _fonts[name] : null;
    }
}
