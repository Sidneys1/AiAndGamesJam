using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace AiAndGamesJam {
    public enum GameState {
        Logo,
        Intro,
        GenerateScenario,
        Game,
        Won,
        Lost,
    }
    public enum GoalTypes {
        NumberOfAnts,
        NumberOfAnthills,
        AmountOfFood,
        Survival,
        Domination,
    }

    public enum SandboxSelection {
        BEGIN,
        Anthill,
        Food,
        Ant,
        Fire_Ant,
        END
    }

    public partial class AntGame : Game {
        private readonly GraphicsDeviceManager _graphics;
        public SpriteBatch SpriteBatch;

        private readonly Random _rand = new();

        private readonly DebugComponent _debug;
        private readonly InputManager _input;
        private GameState _state = GameState.Logo;
        private int _level = 0;
        private GoalTypes _goal = GoalTypes.AmountOfFood;
        private double _goalValue = 100, _goalCurrent = 0;
        private string _goalText, _goalDesc = "Stockpile 100 Food";
        private double _goalTextLastUpdated = 0;
        private bool _sandbox = false, _firstSandboxFireant = true;
        private SandboxSelection _sandboxSelection = SandboxSelection.Food;

        private void GenerateScenario() {
            _level++;

            for (short i = 0; i < _rightmostAntity; i++) {
                if (_antitiesSet[i])
                    RemoveAntity(i);
            }

            for (short i = 0; i < _rightmostThing; i++) {
                if (_thingsSet[i])
                    RemoveThing(i);
            }

            List<GoalTypes> goalOptions = new() {
                // GoalTypes.NumberOfAnts,
                GoalTypes.AmountOfFood,
            };

            foreach (var team in _jobs)
                team.Value.Clear();

            int numAnthills = 3;
            int numAnts = 10;

            if (_level > 5) {
                // goalOptions.Add(GoalTypes.Survival);
                numAnthills--;
                numAnts -= 3;
            }

            if (_level > 10) {
                // goalOptions.Add(GoalTypes.Domination);
                numAnthills--;
                numAnts -= 5;
            }

            Trace.WriteLine($"Number of anthills: {numAnthills}");

            for (int i = 0; i < numAnthills; i++) {
                var x = _rand.NextDouble() * (GraphicsDevice.Viewport.Width - 200) + 200;
                var y = _rand.NextDouble() * GraphicsDevice.Viewport.Height;
                AddAntity(AntityType.Anthill, Team.Player, new Vector2((float)x, (float)y), Actions.Stockpile, 1, 105);
            }

            Trace.WriteLine($"Number of ants: {numAnts}");

            for (int i = 0; i < numAnts; i++) {
                var x = _rand.NextDouble() * (GraphicsDevice.Viewport.Width - 200) + 200;
                var y = _rand.NextDouble() * GraphicsDevice.Viewport.Height;
                AddAntity(AntityType.Ant, Team.Player, new Vector2((float)x, (float)y), Actions.Idle);
            }


            Trace.WriteLine("Goal Options: " + string.Join(", ", goalOptions.Select(o => o.ToString())));

            _goal = goalOptions[_rand.Next(goalOptions.Count)];

            Trace.WriteLine($"Selected: {_goal}");

            var minFood = 0;
            double levelBonus = _level switch {
                <= 5 => 4,
                <= 10 => 3,
                _ => 2
            };
            switch (_goal) {
                case GoalTypes.AmountOfFood: {
                        _goalValue = numAnthills * 100 + _level * 300 + _rand.Next(-50, 50);
                        minFood = (int)((_goalValue * levelBonus + 0.5) / 100);
                        break;
                    }

                case GoalTypes.NumberOfAnts: {
                        _goalValue = _level * 10 + _rand.Next(-5, 5);
                        minFood = (int)((_goalValue * 20 * levelBonus + 0.5) / 100);
                        break;
                    }
            }
            Trace.WriteLine($"Goal Value: {_goalValue}");
            Trace.WriteLine($"Minimum food: {minFood}");

            for (int i = 0; i < minFood; i++) {
                var x = _rand.NextDouble() * (GraphicsDevice.Viewport.Width - 200) + 200;
                var y = _rand.NextDouble() * GraphicsDevice.Viewport.Height;
                AddThing(ThingType.Food, new Vector2((float)x, (float)y), 100);
            }

            _goalDesc = _goal switch {
                GoalTypes.AmountOfFood => $"Stockpile {_goalValue} Food\n",
                GoalTypes.NumberOfAnts => $"  Have {_goalValue} Ants\n",
                GoalTypes.NumberOfAnthills => $"Have {_goalValue} Anthills\n",
                GoalTypes.Survival => $"Survive for {_goalValue}s\n",
                GoalTypes.Domination => "Destroy all other Colonies\n",
                _ => "UNKNOWN",
            };
        }

        public AntGame() {
            _graphics = new GraphicsDeviceManager(this) {
                GraphicsProfile = GraphicsProfile.HiDef,
            };

            Window.ClientSizeChanged += On_ClientSizeChanged;
            Window.TextInput += On_TextInput;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _input = new InputManager(this);

            _debug = new DebugComponent(this) {
                Offset = new Vector2(200, 0)
            };
            _debug.AddDebugLine(() => $"Entity Slots: {_antitiesSet.GetCardinality()} used/{_rightmostAntity + 1} allocated/{MAX_ANTITIES} max");
            _debug.AddDebugLine(() => $" Thing Slots: {_thingsSet.GetCardinality()} used/{_rightmostThing + 1} allocated/{MAX_THINGS} max");
            _debug.AddDebugLine(() => $"        Jobs: {_jobs[Team.Player].Count}");
            _debug.AddDebugLine(() => {
                var ret = $"   Expensive: {_expensiveDebug}";
                _expensiveDebug = 0;
                return ret;
            });
        }

        protected override void Initialize() {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            Window.Title = "Ants";

            _leftPanelRect = new(Point.Zero, new Point(200, _graphics.PreferredBackBufferHeight));
            _leftPanelSplitterRect = new Rectangle(5, 150, _leftPanelRect.Width - 10, 1);

            AddAntity(
                AntityType.Ant,
                Team.Player,
                position: new Vector2(_rand.Next(216, _graphics.PreferredBackBufferWidth - 16), _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
                action: Actions.Idle);

            AddAntity(AntityType.Anthill, Team.Player, position: new Vector2(600, 300), action: Actions.Stockpile, value: 5);

            // AddAntity(
            //     AntityType.Ant,
            //     Team.Fireants,
            //     position: new Vector2(_rand.Next(216, _graphics.PreferredBackBufferWidth - 16), _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
            //     action: Actions.Idle);

            // Trace.WriteLine(hill);
            // AddJob(JobType.Attack, Team.Fireants, hill);
            for (int i = 0; i < 2; i++) {
                AddThing(ThingType.Food,
                         new Vector2(_rand.Next(216, _graphics.PreferredBackBufferWidth - 16),
                                     _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
                         value: 100);
            }

            AddJob(JobType.Gather, Team.Player);

            for (int i = 0; i < 10; i++) {
                AddAntity(
                    AntityType.Ant,
                    Team.Player,
                    position: new Vector2(_rand.Next(216, _graphics.PreferredBackBufferWidth - 16), _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
                    action: Actions.Idle);
            }

            base.Initialize();
        }

        private void On_TextInput(object _, TextInputEventArgs e) {
            if (!char.IsLetter(e.Character)) return;
            _secret.PushBack(e.Character);
            _secretUpdated = true;
        }

        private void On_ClientSizeChanged(object _, EventArgs __) {
            _leftPanelRect = new(Point.Zero, new Point(200, GraphicsDevice.Viewport.Height));
            _scenarioTitlePos = new Vector2((float)((GraphicsDevice.Viewport.Width / 2) - (_largeFont.MeasureString(SCENARIO_TITLE).X / 2)), 100);
        }

        const string SCENARIO_TITLE = "YOUR GOAL IS";

        protected override void LoadContent() {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            ContentManager.Load(this);

            _selectionPixel = ContentManager.GetTexture("pixel");
            _antHill = ContentManager.GetTexture("anthill");
            _ant = ContentManager.GetTexture("ant");
            _food = ContentManager.GetTexture("food");
            _bg = ContentManager.GetTexture("bg");
            _tommy = ContentManager.GetTexture("tommy");
            _logo = ContentManager.GetTexture("logo");

            _logoPos = new Vector2((GraphicsDevice.Viewport.Width / 2) - (_logo.Width / 2), (GraphicsDevice.Viewport.Height / 2) - (_logo.Height / 2));

            _font = ContentManager.GetFont("perfect_dos");
            _largeFont = ContentManager.GetFont("perfect_dos_large");
            var titleSize = _largeFont.MeasureString(TITLE);
            _titlePos = new Vector2((GraphicsDevice.Viewport.Width / 2) - (titleSize.X / 2), 200);
            _introPos = new Vector2((GraphicsDevice.Viewport.Width / 2) - (_font.MeasureString(INTRO).X / 2), 250);
            _pixelmix = ContentManager.GetFont("pixelmix");
            _scenarioTitlePos = new Vector2((float)((GraphicsDevice.Viewport.Width / 2) - (_largeFont.MeasureString(SCENARIO_TITLE).X / 2)), 100);

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
            if (InputManager.KeyWentDown(Keys.Escape))
                Exit();

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
                case GameState.Won:
                    if (InputManager.KeyWentDown(Keys.Enter)) {
                        GenerateScenario();
                        _state = GameState.GenerateScenario;
                    } else if (InputManager.KeyWentDown(Keys.Escape))
                        Exit();
                    break;
                case GameState.Lost:
                    if (InputManager.KeyWentDown(Keys.Escape) || InputManager.KeyWentDown(Keys.Enter))
                        Exit();
                    break;
                case GameState.GenerateScenario:
                    if (InputManager.KeyWentDown(Keys.Enter))
                        _state = GameState.Game;
                    else if (InputManager.KeyWentDown(Keys.Escape))
                        Exit();
                    break;
            }

            base.Update(gameTime);
        }

        Vector2 _scenarioTitlePos;

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
                case GameState.Won:
                    GraphicsDevice.Clear(Color.Black);
                    SpriteBatch.Begin();
                    SpriteBatch.DrawString(_largeFont, "YOU WON!", _titlePos, Color.White);
                    SpriteBatch.End();
                    break;
                case GameState.Lost:
                    GraphicsDevice.Clear(Color.Maroon);
                    SpriteBatch.Begin();
                    SpriteBatch.DrawString(_largeFont, "YOU LOST", _titlePos, Color.White);
                    SpriteBatch.End();
                    break;
                case GameState.GenerateScenario:
                    GraphicsDevice.Clear(Color.Black);
                    SpriteBatch.Begin();
                    SpriteBatch.DrawString(_largeFont, SCENARIO_TITLE, _scenarioTitlePos, Color.White);

                    Vector2 size = _font.MeasureString(_goalDesc);
                    size.Y = 200;
                    size.X /= -2;
                    size.X += GraphicsDevice.Viewport.Width / 2;
                    SpriteBatch.DrawString(_font, _goalDesc, size, Color.White);

                    size.Y = GraphicsDevice.Viewport.Height - 20;
                    size.X = GraphicsDevice.Viewport.Width - 250;
                    SpriteBatch.DrawString(_font, "Press ENTER to continue...", size, Color.Gray);

                    SpriteBatch.End();
                    break;
            }

            base.Draw(gameTime);
        }
    }
}
