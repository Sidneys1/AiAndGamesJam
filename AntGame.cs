using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AiAndGamesJam {
    enum AntityType : byte {
        None,
        Anthill,
        Ant,
        Food
    }
    enum Team : byte {
        Player,
        AI
    }
    enum AntihillActions : byte {
        None,
        NewAnts,
    }
    enum AntActions : byte {
        None,
        Wander,
    }

    struct Antity {
        public AntityType Type;
        public Team Team;
        public Vector2 Position;
        public byte Action;
        public double CoolDown;
        public double Age;
        public float Facing;

        public Antity(AntityType type = AntityType.None, Team team = Team.Player, Vector2? position = null, byte action = 0, double coolDown = 0) {
            Type = type;
            Team = team;
            Position = position.GetValueOrDefault();
            Action = action;
            CoolDown = coolDown;
            Facing = 0;
            Age = 0;
        }

        public static string[] AnthillActions = new[] {
            "<NOTHING>",
            "New Ant in ",
        };
    }

    public class AntGame : Game {
        const int MAX_ANTITIES = 16;
        private readonly GraphicsDeviceManager _graphics;
        public SpriteBatch SpriteBatch;

        private readonly System.Random _rand = new();

        private readonly DebugComponent _debug;
        private readonly InputManager _input;

        private readonly System.Collections.BitArray _antitiesSet = new(MAX_ANTITIES, false);
        private readonly Antity[] _antities = new Antity[MAX_ANTITIES];
        private int _selectedAntity = 2;

        void AddAntity(AntityType type = AntityType.None, Team team = Team.Player, Vector2? position = null, byte action = 0, double coolDown = 0) {
            var pos = -1;
            for (int i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) {
                    pos = i;
                    break;
                }
            }
            if (pos == -1)
                throw new System.Exception("All entity slots are taken!");

            _antities[pos] = new Antity(type, team, position, action, coolDown);
            _antitiesSet[pos] = true;
        }

        void RemoveAntity(int pos) => _antitiesSet[pos] = false;

        private Texture2D _pixel, _antHill, _ant;
        private Vector2 _anthillOffset = new(24, 24);
        private Vector2 _antOffset = new(16, 16);
        private SpriteFont _font;
        private readonly Color _bgColor = new(114, 94, 0);
        private readonly Color _selectionColor = new(Color.LimeGreen, 10);

        public AntGame() {
            _graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _debug = new DebugComponent(this);
            _input = new InputManager(this);

            AddAntity(type: AntityType.Anthill, position: new Vector2(400, 300), action: (byte)AntihillActions.NewAnts, coolDown: 5.0);
            AddAntity(type: AntityType.Anthill, position: new Vector2(200, 100), action: (byte)AntihillActions.NewAnts, coolDown: 35.0);
        }

        // protected override void Initialize() {
        //     // TODO: Add your initialization logic here

        //     base.Initialize();
        // }

        protected override void LoadContent() {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            ContentManager.Load(this);
            _pixel = ContentManager.GetTexture("pixel");
            _antHill = ContentManager.GetTexture("anthill");
            _ant = ContentManager.GetTexture("ant");
            _font = ContentManager.GetFont("perfect_dos");

            _debug.Initialize();
            _pixel = _debug._pixel;
        }

        protected override void Update(GameTime gameTime) {
            _input.Update(gameTime);

            if (InputManager.KeyWentDown(Keys.Escape))
                Exit();

            if (InputManager.KeyWentDown(Keys.F3))
                _debug.Enabled = !_debug.Enabled;

            for (int i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) continue;

                var egt = gameTime.ElapsedGameTime.TotalSeconds;
                _antities[i].Age += egt;

                var cd = _antities[i].CoolDown;
                if (cd <= egt) {
                    switch (_antities[i].Type) {
                        case AntityType.Anthill:
                            switch ((AntihillActions)_antities[i].Action) {
                                case AntihillActions.NewAnts: {
                                        _antities[i].CoolDown = 500;
                                        AddAntity(AntityType.Ant, position: _antities[i].Position, action: (byte)AntActions.Wander);
                                        break;
                                    }
                                default: {
                                        _antities[i].CoolDown = 0;
                                        break;
                                    }
                            }
                            break;
                        case AntityType.Ant:
                            switch ((AntActions)_antities[i].Action) {
                                case AntActions.Wander: {
                                        _antities[i].CoolDown = 0.1;
                                        _antities[i].Position += new Vector2(_rand.Next(-5, 5), _rand.Next(-5, 5));
                                        break;
                                    }
                                default: {
                                        _antities[i].CoolDown = 0;
                                        break;
                                    }
                            }
                            break;
                    }
                } else {
                    _antities[i].CoolDown -= egt;
                }
            }


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(_bgColor);

            SpriteBatch.Begin();

            _debug.Draw(gameTime);

            for (int i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) continue;
                var selected = i == _selectedAntity;

                var pos = _antities[i].Position;
                switch (_antities[i].Type) {
                    case AntityType.Anthill:
                        if (selected) {
                            Vector2 padding = new(3);
                            Rectangle selection = new((pos - _anthillOffset - padding).ToPoint(), ((_anthillOffset * 2) + (padding * 2)).ToPoint());
                            SpriteBatch.Draw(_pixel, selection, null, _selectionColor);
                        }
                        SpriteBatch.Draw(_antHill, pos - _anthillOffset, Color.White);
                        if (_antities[i].Action != 0) {
                            var text = Antity.AnthillActions[_antities[i].Action] + _antities[i].CoolDown.ToString("0.00s");
                            SpriteBatch.DrawString(_font, text, pos - new Vector2((_font.MeasureString(text).X / 2), -30), Color.White);
                        }
                        break;
                    case AntityType.Ant:
                        if (selected) {
                            Vector2 padding = new(3);
                            Rectangle selection = new((pos - _antOffset - padding).ToPoint(), ((_antOffset * 2) + (padding * 2)).ToPoint());
                            SpriteBatch.Draw(_pixel, selection, null, _selectionColor);
                        }
                        pos = _antities[i].Position;
                        SpriteBatch.Draw(_ant, pos - _antOffset, Color.Black);
                        break;
                    default: continue;
                }
                SpriteBatch.Draw(_pixel, pos, Color.Magenta);
            }


            SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
