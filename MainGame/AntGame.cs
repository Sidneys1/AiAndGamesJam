using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace AiAndGamesJam {
    public enum GameState {
        Logo,
        Intro,
        Game,
    }

    public partial class AntGame : Game {
        private readonly GraphicsDeviceManager _graphics;
        public SpriteBatch SpriteBatch;

        private readonly Random _rand = new();

        private readonly DebugComponent _debug;
        private readonly InputManager _input;
        private GameState _state = GameState.Logo;

        public AntGame() {
            _graphics = new GraphicsDeviceManager(this) {
                // PreferredBackBufferWidth = 1280,
                // PreferredBackBufferHeight = 720,
                GraphicsProfile = GraphicsProfile.HiDef,
            };
            Window.ClientSizeChanged += On_ClientSizeChanged;
            _leftPanelRect = new(Point.Zero, new Point(200, _graphics.PreferredBackBufferHeight));
            _leftPanelSplitterRect = new Rectangle(5, 100, _leftPanelRect.Width - 10, 1);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _input = new InputManager(this);

            _debug = new DebugComponent(this) {
                Offset = new Vector2(200, 0)
            };
            _debug.AddDebugLine(() => $"Entity Slots: {_antitiesSet.GetCardinality()} used/{_rightmostAntity + 1} allocated/{MAX_ANTITIES} max");
            _debug.AddDebugLine(() => $" Thing Slots: {_thingsSet.GetCardinality()} used/{_rightmostThing + 1} allocated/{MAX_THINGS} max");
            _debug.AddDebugLine(() => $"        Jobs: {_jobs.Count}");
            _debug.AddDebugLine(() => {
                var ret = $"   Expensive: {_expensiveDebug}";
                _expensiveDebug = 0;
                return ret;
            });

            AddAntity(AntityType.Anthill, Team.Player, position: new Vector2(600, 300), action: Actions.NewAnts, coolDown: 5.0 * _rand.NextDouble(), value: 105);
            AddAntity(AntityType.Anthill, Team.Player, position: new Vector2(300, 300), action: Actions.NewAnts, coolDown: 5.0 * _rand.NextDouble(), value: 105);
            AddAntity(AntityType.Anthill, Team.Player, position: new Vector2(450, 150), action: Actions.NewAnts, coolDown: 5.0 * _rand.NextDouble(), value: 105);
            for (int i = 0; i < 5; i++) {
                AddThing(ThingType.Food,
                         new Vector2(_rand.Next(216, _graphics.PreferredBackBufferWidth - 16),
                                     _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
                         value: 100);
            }

            for (short i = 0; i < 5; i++)
                AddJob(JobType.Gather, i);

            Trace.WriteLine("Creating ants...");
            for (int i = 0; i < 500; i++) {
                AddAntity(
                    AntityType.Ant,
                    Team.Player,
                    position: new Vector2(_rand.Next(216, _graphics.PreferredBackBufferWidth - 16), _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
                    action: Actions.Idle,
                    coolDown: (_rand.NextDouble() * 2) + 3);
            }
        }

        private void On_ClientSizeChanged(object sender, EventArgs e) =>
            _leftPanelRect = new(Point.Zero, new Point(200, GraphicsDevice.Viewport.Height));

        protected override void LoadContent() {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            ContentManager.Load(this);

            _selectionPixel = ContentManager.GetTexture("pixel");
            _antHill = ContentManager.GetTexture("anthill");
            _ant = ContentManager.GetTexture("ant");
            _food = ContentManager.GetTexture("food");
            _bg = ContentManager.GetTexture("bg");
            _logo = ContentManager.GetTexture("logo");
            _logoPos = new Vector2((GraphicsDevice.Viewport.Width / 2) - (_logo.Width / 2), (GraphicsDevice.Viewport.Height / 2) - (_logo.Height / 2));

            _font = ContentManager.GetFont("perfect_dos");
            _largeFont = ContentManager.GetFont("perfect_dos_large");
            Vector2 vector2 = _largeFont.MeasureString(TITLE);
            var titleSize = vector2;
            _titlePos = new Vector2((GraphicsDevice.Viewport.Width / 2) - (titleSize.X / 2), 200);
            _introPos = new Vector2((GraphicsDevice.Viewport.Width / 2) - (_font.MeasureString(INTRO).X / 2), 250);
            _pixelmix = ContentManager.GetFont("pixelmix");

            _songs[0] = ContentManager.GetSong("modular-ambient-01-789");
            _songs[1] = ContentManager.GetSong("modular-ambient-02-790");
            _songs[2] = ContentManager.GetSong("modular-ambient-03-791");
            _songs[3] = ContentManager.GetSong("modular-ambient-04-792");

            _debug.Initialize();
            _selectionPixel = new Texture2D(GraphicsDevice, 1, 1);
            _selectionPixel.SetData(new Color[] { new Color(Color.CornflowerBlue, 50) });
            _pixel = _debug._pixel;

            MediaPlayer.Play(_songs[_currentSong]);
            MediaPlayer.MediaStateChanged += On_SongEnd;
        }

        protected override void Update(GameTime gameTime) {
            _input.Update(gameTime);
            _debug.Update(gameTime);

            switch (_state) {
                case GameState.Game:
                    UpdateGameState(gameTime);
                    break;
                case GameState.Logo:
                    UpdateLogoState(gameTime);
                    break;
                case GameState.Intro:
                    UpdateIntroState(gameTime);
                    break;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            switch (_state) {
                case GameState.Game:
                    DrawGameState(gameTime);
                    break;
                case GameState.Logo:
                    DrawLogoState(gameTime);
                    break;
                case GameState.Intro:
                    DrawIntroState(gameTime);
                    break;
            }

            base.Draw(gameTime);
        }
    }
}
