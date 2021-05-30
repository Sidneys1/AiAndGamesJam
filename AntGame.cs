using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AiAndGamesJam {
    enum AntityType : byte {
        None,
        Anthill,
        Ant
    }
    enum Team : byte {
        Player,
        AI
    }

    struct Antity {
        public Team Team;
        public AntityType Type;
        public Vector2 Position;
        public byte Action;
    }

    public class AntGame : Game {
        private readonly GraphicsDeviceManager _graphics;
        public SpriteBatch SpriteBatch;

        private readonly DebugComponent _debug;
        private readonly InputManager _input;

        private readonly Antity[] _antities = new Antity[16];

        private Texture2D _antHill;

        public AntGame() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _debug = new DebugComponent(this);
            _input = new InputManager(this);
        }

        protected override void Initialize() {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent() {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            ContentManager.Load(Content);
            _antHill = ContentManager.GetTexture("anthill");

            _debug.Initialize();
        }

        protected override void Update(GameTime gameTime) {
            _input.Update(gameTime);

            if (InputManager.KeyWentDown(Keys.Escape))
                Exit();

            if (InputManager.KeyWentDown(Keys.F3))
                _debug.Enabled = !_debug.Enabled;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            SpriteBatch.Begin();

            _debug.Draw(gameTime);

            foreach (var antity in _antities) {
                switch (antity.Type) {
                    case AntityType.Anthill:
                        SpriteBatch.Draw(_antHill, antity.Position, Color.White);
                        break;
                    default: break;
                }
            }


            SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
