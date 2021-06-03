using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Media;

namespace AiAndGamesJam {
    public static class ContentManager {
        static readonly Dictionary<string, Texture2D> _textures = new() {
            { "anthill", null },
            { "ant", null },
            { "food", null },
            { "bg", null },
            { "logo", null },
            { "tommy", null },
        };

        static readonly Dictionary<string, SpriteFont> _fonts = new() {
            { "perfect_dos", null },
            { "perfect_dos_large", null },
            { "pixelmix", null },
        };

        static readonly Dictionary<string, Song> _songs = new() {
            { "modular-ambient-01-789", null },
            { "modular-ambient-02-790", null },
            { "modular-ambient-03-791", null },
            { "modular-ambient-04-792", null },
        };

        public static void Load(AntGame _game) {
            System.Diagnostics.Trace.WriteLine("Loading textures...", nameof(ContentManager));
            _textures.Keys.ToList().ForEach(key => {
                _textures[key] = _game.Content.Load<Texture2D>(key);
                System.Diagnostics.Trace.WriteLine($"Loaded texture '{key}'", nameof(ContentManager));
            });

            System.Diagnostics.Trace.WriteLine("Loading fonts...", nameof(ContentManager));
            _fonts.Keys.ToList().ForEach(key => {
                _fonts[key] = _game.Content.Load<SpriteFont>(key);
                System.Diagnostics.Trace.WriteLine($"Loaded font '{key}'", nameof(ContentManager));
            });

            System.Diagnostics.Trace.WriteLine("Loading songs...", nameof(ContentManager));
            _songs.Keys.ToList().ForEach(key => {
                _songs[key] = _game.Content.Load<Song>(key);
                System.Diagnostics.Trace.WriteLine($"Loaded song '{key}'", nameof(ContentManager));
            });

            // Texture2D _pixel = new(_game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            // _pixel.SetData(new Color[] { Color.White });
            // _textures.Add("pixel", _pixel);
        }

        public static Texture2D GetTexture(string name) => _textures.ContainsKey(name) ? _textures[name] : null;
        public static SpriteFont GetFont(string name) => _fonts.ContainsKey(name) ? _fonts[name] : null;

        public static Song GetSong(string name) => _songs.ContainsKey(name) ? _songs[name] : null;
    }
}
