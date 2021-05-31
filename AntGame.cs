using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AiAndGamesJam {
    enum AntityType : byte {
        None,
        Anthill,
        Ant,
        Food,
    }
    enum Team : byte {
        Player,
        AI
    }
    enum Actions : byte {
        None,
        BuildingAnt,
        NewAnts,
        Wander,
        Gather,
    }

    struct Antity {
        public AntityType Type;
        public Team Team;
        public Vector2 Position;
        public Actions Action;
        public double CoolDown;
        public double Age;
        public Vector2 Target;
        public int TargetAntity;
        public int Value;

        public static string[] AnthillActions = new[] {
            null,
            "New Ant in ",
            null,
        };
    }

    public class AntGame : Game {
        const int MAX_ANTITIES = 50;
        const int ANT_SPEED = 50;
        private readonly GraphicsDeviceManager _graphics;
        public SpriteBatch SpriteBatch;

        private readonly Random _rand = new();

        private readonly DebugComponent _debug;
        private readonly InputManager _input;

        private readonly System.Collections.BitArray _antitiesSet = new(MAX_ANTITIES, false);
        private readonly Antity[] _antities = new Antity[MAX_ANTITIES];
        private int _selectedAntity = -1;

        void AddAntity(AntityType type = AntityType.None, Team team = Team.Player, Vector2? position = null, Actions action = 0, double coolDown = 0, Vector2? target = null, int value = 0) {
            var pos = -1;
            for (int i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) {
                    pos = i;
                    break;
                }
            }
            if (pos == -1)
                throw new System.Exception("All entity slots are taken!");

            _antities[pos] = new Antity {
                Type = type,
                Team = team,
                Position = position.GetValueOrDefault(),
                Action = action,
                CoolDown = coolDown,
                Target = target.GetValueOrDefault(),
                Value = value,
                TargetAntity = -1
            };
            _antitiesSet[pos] = true;
        }

        void RemoveAntity(int pos) => _antitiesSet[pos] = false;

        private Texture2D _selectionPixel, _pixel, _antHill, _ant, _food;
        private Vector2 _anthillOffset = new(24, 24);
        private Vector2 _antOffset = new(3, 2);
        private Vector2 _foodOffset = new(16, 16);
        private SpriteFont _font;
        private readonly Color _bgColor = new(114, 94, 0);

        public AntGame() {
            _graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _debug = new DebugComponent(this);
            _input = new InputManager(this);

            AddAntity(type: AntityType.Anthill, position: new Vector2(400, 300), action: Actions.NewAnts, coolDown: 1.0);
            AddAntity(type: AntityType.Anthill, position: new Vector2(200, 300), action: Actions.NewAnts, coolDown: 1.0);
            AddAntity(type: AntityType.Food, position: new Vector2(300, 300), value: 100);
            for (int i = 0; i < 20; i++) {
                AddAntity(type: AntityType.Ant, position: new Vector2(300, 50), action: Actions.Gather, coolDown: 5);
            }
        }

        protected override void LoadContent() {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            ContentManager.Load(this);
            _selectionPixel = ContentManager.GetTexture("pixel");
            _antHill = ContentManager.GetTexture("anthill");
            _ant = ContentManager.GetTexture("ant");
            _food = ContentManager.GetTexture("food");
            _font = ContentManager.GetFont("perfect_dos");

            _debug.Initialize();
            _selectionPixel = new Texture2D(GraphicsDevice, 1, 1);
            _selectionPixel.SetData(new Color[] { new Color(Color.CornflowerBlue, 50) });
            _pixel = _debug._pixel;
        }

        private int FindNearest(ref Vector2 position, AntityType type, Team? team = null) {
            int ret = -1;
            float nearest = float.MaxValue;
            for (int i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) continue;
                ref Antity ent = ref _antities[i];
                if (ent.Type != type || (team.HasValue && ent.Team != team.Value)) continue;
                var distance = (ent.Position - position).Length();
                if (distance < nearest) {
                    ret = i;
                    nearest = distance;
                }
            }
            return ret;
        }

        private void UpdateAnthill(ref Antity anthill, double egt) {
            switch (anthill.Action) {
                case Actions.NewAnts:
                    if (anthill.Value >= 20) {
                        anthill.CoolDown = 30;
                        anthill.Action = Actions.BuildingAnt;
                        break;
                    }
                    anthill.CoolDown = 1;
                    break;

                case Actions.BuildingAnt:
                    anthill.CoolDown = 1;
                    AddAntity(AntityType.Ant, anthill.Team, anthill.Position, action: Actions.Wander);
                    anthill.Value -= 20;
                    break;

                default:
                    anthill.CoolDown = 0;
                    break;

            }
        }

        private void UpdateAnt(ref Antity ant, double egt) {
            switch (ant.Action) {
                case Actions.Wander:
                    ant.CoolDown = 0.1;
                    var rotation = (float)_rand.NextDouble() * MathHelper.TwoPi;
                    var move = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));
                    move.Normalize();
                    var speed = (float)(ANT_SPEED * egt);
                    ant.Position += move * speed;
                    break;

                case Actions.Gather:
                    if (ant.TargetAntity != -1) {
                        // Move
                        if (!_antitiesSet[ant.TargetAntity]) {
                            ant.TargetAntity = -1;
                            ant.Action = Actions.Wander;
                            ant.CoolDown = 2;
                            break;
                        }
                        ref Antity target = ref _antities[ant.TargetAntity];
                        var targetPos = target.Position;
                        Vector2 diff = targetPos - ant.Position;
                        float distance = diff.Length();
                        speed = (float)(egt * ANT_SPEED);
                        if ((distance - 5) < speed) {
                            // We're here!
                            ant.CoolDown = 0.25 + (float)(_rand.NextDouble() / 2.0);
                            if (ant.Value != 0) {
                                target.Value += ant.Value;
                                ant.Value = 0;
                            } else {
                                target.Value -= ant.Value = 1;//Math.Min(1, target.Value);
                                if (target.Value == 0)
                                    RemoveAntity(ant.TargetAntity);
                            }
                            ant.TargetAntity = -1;
                            break;
                        }
                        // Wiggle
                        diff.Normalize();
                        // var randomRot = (float)(_rand.NextDouble() - 0.5) * 2;
                        // diff = Vector2.Transform(diff, Matrix.CreateRotationX((float)(randomRot * Math.PI)));
                        diff += new Vector2((float)(_rand.NextDouble() - 0.5), (float)(_rand.NextDouble() - 0.5));
                        diff.Normalize();
                        ant.Position += diff * speed;

                    } else if (ant.Value != 0) {
                        // Target anthill
                        var nearestAnthill = FindNearest(ref ant.Position, AntityType.Anthill, ant.Team);
                        if (nearestAnthill == -1) {
                            Trace.WriteLine("COULD NOT FIND A NEARBY ANTHILL!");
                            ant.CoolDown = 1;
                            ant.Action = Actions.Wander;
                        }
                        ant.TargetAntity = nearestAnthill;
                    } else {
                        // Target food
                        var nearestFood = FindNearest(ref ant.Position, AntityType.Food);
                        if (nearestFood == -1) {
                            Trace.WriteLine("COULD NOT FIND A NEARBY FOOD!");
                            ant.CoolDown = 1;
                            ant.Action = Actions.Wander;
                        }
                        ant.TargetAntity = nearestFood;
                    }
                    break;

                default:
                    ant.CoolDown = 0;
                    break;
            }
        }

        private void SelectNextEntity() {
            Trace.WriteLine("Selecting next entity...");
            int next = -1;
            for (int i = 0; i < MAX_ANTITIES; i++) {
                int actualSelection = _selectedAntity != -1 ? (i + _selectedAntity) % MAX_ANTITIES : i;
                ref Antity ent = ref _antities[actualSelection];
                if (actualSelection == _selectedAntity || ent.Type == AntityType.None || ent.Team != Team.Player) continue;
                if (ent.Type == AntityType.Food) continue; // Can't select food
                next = actualSelection;
                break;
            }
            _selectedAntity = next;
        }

        protected override void Update(GameTime gameTime) {
            _input.Update(gameTime);
            _debug.Update(gameTime);

            if (InputManager.KeyWentDown(Keys.Escape))
                Exit();

            if (InputManager.KeyWentDown(Keys.F3))
                _debug.Enabled = !_debug.Enabled;

            if (InputManager.KeyWentDown(Keys.Tab))
                SelectNextEntity();

            for (int i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) continue;

                var egt = gameTime.ElapsedGameTime.TotalSeconds;
                _antities[i].Age += egt;

                if (_antities[i].CoolDown > egt) {
                    _antities[i].CoolDown -= egt;
                    continue;
                }

                switch (_antities[i].Type) {
                    case AntityType.Anthill:
                        UpdateAnthill(ref _antities[i], egt);
                        break;
                    case AntityType.Ant:
                        UpdateAnt(ref _antities[i], egt);
                        break;
                }
            }

            base.Update(gameTime);
        }

        private void DrawSelection(Rectangle rectangle) => SpriteBatch.Draw(_selectionPixel, rectangle, Color.White);

        private void DrawAnthill(ref Antity anthill, bool selected) {
            var pos = anthill.Position;
            if (selected) {
                Vector2 padding = new(3);
                Rectangle selection = new((pos - _anthillOffset - padding).ToPoint(), ((_anthillOffset * 2) + (padding * 2)).ToPoint());
                DrawSelection(selection);
            }
            SpriteBatch.Draw(_antHill, pos - _anthillOffset, Color.White);
            string v = Antity.AnthillActions[(int)anthill.Action];
            if (v != null) {
                var text = v + anthill.CoolDown.ToString("0s");
                SpriteBatch.DrawString(_font, text, pos - new Vector2(_font.MeasureString(text).X / 2, -30), Color.White);
            }
        }

        private void DrawAnt(ref Antity ant, bool selected) {
            var pos = ant.Position;
            if (selected) {
                Vector2 padding = new(3);
                Rectangle selection = new((pos - _antOffset - padding).ToPoint(), ((_antOffset * 2) + (padding * 2)).ToPoint());
                DrawSelection(selection);
            }
            pos = ant.Position;
            SpriteBatch.Draw(_ant, pos - _antOffset, Color.Black);

            if (ant.Action == Actions.Gather && ant.Value > 0)
                SpriteBatch.Draw(_pixel, new Rectangle((int)pos.X, (int)pos.Y, 2, 2), Color.Green);

        }

        private void DrawFood(ref Antity food) {
            Color color;
            if (food.Value >= 66)
                color = Color.White;
            else if (food.Value >= 50)
                color = Color.SandyBrown;
            else
                color = Color.Brown;
            SpriteBatch.Draw(_food, food.Position - _foodOffset, color);
            Rectangle rect = new((int)(food.Position.X - _foodOffset.X), (int)(food.Position.Y + _foodOffset.Y) + 3, (int)(32 * (food.Value / 100f)), 3);
            if (food.Value >= 66)
                color = Color.Green;
            else if (food.Value >= 50)
                color = Color.Orange;
            else
                color = Color.Red;
            SpriteBatch.Draw(_pixel, rect, color);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(_bgColor);

            SpriteBatch.Begin();

            _debug.Draw(gameTime);

            for (int i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) continue;

                switch (_antities[i].Type) {
                    case AntityType.Anthill:
                        DrawAnthill(ref _antities[i], i == _selectedAntity);
                        break;
                    case AntityType.Ant:
                        DrawAnt(ref _antities[i], i == _selectedAntity);
                        break;
                    case AntityType.Food:
                        DrawFood(ref _antities[i]);
                        break;
                    default: continue;
                }
            }

            SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
