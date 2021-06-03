using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AiAndGamesJam {
    public partial class AntGame : Game {
        private readonly GraphicsDeviceManager _graphics;
        public SpriteBatch SpriteBatch;

        private readonly Random _rand = new();

        private readonly DebugComponent _debug;
        private readonly InputManager _input;

        public AntGame() {
            _graphics = new GraphicsDeviceManager(this) {
                // PreferredBackBufferWidth = 1280,
                // PreferredBackBufferHeight = 720,
                GraphicsProfile = GraphicsProfile.HiDef,
            };
            Window.AllowUserResizing = true;
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
            _font = ContentManager.GetFont("perfect_dos");

            _debug.Initialize();
            _selectionPixel = new Texture2D(GraphicsDevice, 1, 1);
            _selectionPixel.SetData(new Color[] { new Color(Color.CornflowerBlue, 50) });
            _pixel = _debug._pixel;
        }

        protected override void Update(GameTime gameTime) {
            if (_expensiveThisLoop >= MAX_EXPENSIVE)
                _expensiveDebug++;
            _expensiveThisLoop = 0;

            UpdateTrims(gameTime);

            _input.Update(gameTime);
            _debug.Update(gameTime);

            if (InputManager.LeftMouseWentDown()) {
                var mpos = InputManager.CurrentMouseState.Position.ToVector2();
                if (mpos.X < _leftPanelRect.Width) {
                    int newSelection = (int)(mpos.Y - 130) / 20;
                    if (newSelection < 0 || newSelection >= _jobs.Count)
                        _selectedJob = -1;
                    else {
                        _selectedJob = newSelection;
                        _selectedThing = _jobs[_selectedJob].Target;
                        _selectedAntity = -1;
                    }
                } else {
                    short thing = FindNearestThing(ref mpos, collision: true);
                    if (thing != -1) {
                        Trace.WriteLine("Clicked on thing");
                        _selectedThing = thing;
                        _selectedAntity = _selectedJob = -1;
                    } else {
                        thing = FindNearestAntity(ref mpos, AntityType.Anthill, Team.Player, collision: true);
                        if (thing != -1) {
                            Trace.WriteLine("Clicked on Antity");
                            _selectedAntity = thing;
                            _selectedJob = _selectedThing = -1;
                        } else {
                            _selectedAntity = _selectedJob = _selectedThing = -1;
                        }
                    }
                }
            }

            if (InputManager.RightMouseWentDown())
                AddThing(ThingType.Food, InputManager.CurrentMouseState.Position.ToVector2(), value: 100);

            if (InputManager.KeyWentDown(Keys.Escape))
                Exit();

            if (InputManager.KeyWentDown(Keys.F11))
                _graphics.ToggleFullScreen();

            if (InputManager.KeyWentDown(Keys.F3))
                _debug.Enabled = !_debug.Enabled;

            if (InputManager.KeyWentDown(Keys.Tab))
                SelectNextAntity();

            if (InputManager.KeyWentDown(Keys.H))
                SelectNextAntity(AntityType.Anthill);

            if (InputManager.KeyWentDown(Keys.A))
                SelectNextAntity(AntityType.Ant);

            if (InputManager.KeyWentDown(Keys.I))
                SelectNextAntity(AntityType.Ant, Actions.Idle);

            if (_selectedThing != -1 && InputManager.KeyWentDown(Keys.G))
                AddJob(JobType.Gather, _selectedThing);


            for (short i = 0; i < _rightmostAntity; i++) {
                if (!_antitiesSet[i]) continue;

                var egt = gameTime.ElapsedGameTime.TotalSeconds;
                _antities[i].Age += egt;

                if (_antities[i].CoolDown > egt) {
                    _antities[i].CoolDown -= egt;
                    continue;
                }

                switch (_antities[i].Type) {
                    case AntityType.Anthill:
                        UpdateAnthill(i, ref _antities[i], egt);
                        break;
                    case AntityType.Ant:
                        UpdateAnt(i, ref _antities[i], egt);
                        break;
                }
            }



            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Magenta);

            SpriteBatch.Begin();

            SpriteBatch.Draw(_bg, Vector2.Zero, Color.White);

            for (int i = 0; i < _rightmostThing; i++) {
                if (!_thingsSet[i]) continue;

                ref Thing thing = ref _things[i];

                switch (thing.Type) {
                    case ThingType.Food:
                        DrawFood(ref thing, i == _selectedThing, _selectedJob != -1);
                        break;
                }
            }

            for (int i = 0; i < _rightmostAntity; i++) {
                if (!_antitiesSet[i]) continue;

                ref Antity ent = ref _antities[i];

                switch (ent.Type) {
                    case AntityType.Anthill:
                        DrawAnthill(ref ent, i == _selectedAntity);
                        break;
                    case AntityType.Ant:
                        DrawAnt(ref ent, i == _selectedAntity);
                        break;
                    default: continue;
                }
            }

            DrawPanel();

            _debug.Draw(gameTime);

            SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
