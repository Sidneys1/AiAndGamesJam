using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AiAndGamesJam {


    public class AntGame : Game {
        const int MAX_ANTITIES = 10_000;
        const int ANT_SPEED = 100;

        private readonly GraphicsDeviceManager _graphics;
        public SpriteBatch SpriteBatch;

        private readonly Random _rand = new();

        private readonly DebugComponent _debug;
        private readonly InputManager _input;

        private readonly System.Collections.BitArray _antitiesSet = new(MAX_ANTITIES, false);
        private readonly Antity[] _antities = new Antity[MAX_ANTITIES];
        private int _rightmostAntity = 0;
        private double _lastTrim = 0;
        private int _selectedAntity = -1;
        private int _foodCount = 0;

        void AddAntity(AntityType type = AntityType.None, Team team = Team.None, Vector2? position = null, Actions action = 0, double coolDown = 0, Vector2? target = null, int value = 0) {
            var pos = -1;
            for (int i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) {
                    pos = i;
                    break;
                }
            }
            if (pos == -1)
                throw new Exception("All entity slots are taken!");

            if (pos > _rightmostAntity) _rightmostAntity = pos;

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
            if (type == AntityType.Food) ++_foodCount;
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

            _input = new InputManager(this);

            _debug = new DebugComponent(this);
            _debug.AddDebugLine(() => $"Slots: {_antitiesSet.GetCardinality()} used/{_rightmostAntity + 1} allocated/{MAX_ANTITIES} max");

            AddAntity(AntityType.Anthill, Team.Player, position: new Vector2(400, 300), action: Actions.NewAnts, coolDown: 1.0);
            AddAntity(AntityType.Anthill, Team.Player, position: new Vector2(200, 300), action: Actions.NewAnts, coolDown: 1.0, value: 20);
            for (int i = 0; i < 500; i++) {
                AddAntity(AntityType.Food, position: new Vector2(_rand.Next(16, _graphics.PreferredBackBufferWidth - 16), _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)), value: 100);
            }
            Trace.WriteLine("Creating ants...");
            for (int i = 0; i < 9_400; i++) {
                AddAntity(
                    AntityType.Ant,
                    Team.Player,
                    position: new Vector2(_rand.Next(16, _graphics.PreferredBackBufferWidth - 16), _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
                    action: Actions.Gather,
                    coolDown: (_rand.NextDouble() * 2) + 3);
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

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // private int AskNearby(AntityType type, Team? team = null) {
        //     for (int i = 0; i < _rightmostAntity; i++) {
        //         if (!_antitiesSet[i]) continue;
        //         ref Antity ent = ref _antities[i];
        //         if (ent.TargetAntity == -1 || !_antitiesSet[ent.TargetAntity] || (team.HasValue && ent.Team != team.Value)) continue;
        //         ref Antity target = ref _antities[ent.TargetAntity];
        //         if (target.Type != type) continue;
        //         return ent.TargetAntity;
        //     }
        //     return -1;
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindNearest(ref Vector2 position, AntityType type, Team? team = null) {
            if (type == AntityType.Food && _foodCount == 0) return -1;
            int ret = -1;
            float nearest = float.MaxValue;
            for (int i = 0; i < _rightmostAntity; i++) {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAnthill(int idx, ref Antity anthill, double egt) {
            switch (anthill.Action) {
                case Actions.NewAnts:
                    if (anthill.Value >= 20) {
                        anthill.CoolDown = 5;
                        anthill.Action = Actions.BuildingAnt;
                        anthill.Value -= 20;
                        break;
                    }
                    anthill.CoolDown = 1;
                    break;

                case Actions.BuildingAnt:
                    anthill.CoolDown = 1;
                    anthill.Action = Actions.NewAnts;
                    AddAntity(AntityType.Ant, anthill.Team, anthill.Position, action: Actions.Idle);
                    break;

                default:
                    anthill.CoolDown = 0;
                    break;

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAnt(int idx, ref Antity ant, double egt) {
            if (ant.Age >= 10 && _rand.NextDouble() >= 0.99) {
                RemoveAntity(idx);
                return;
            }

            switch (ant.Action) {
                case Actions.Idle:
                    ant.CoolDown = 0.05;
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
                            ant.TargetAntity = -1;//FindNearest(ref ant.Position, AntityType.Anthill, ant.Team);
                            ant.CoolDown = _rand.NextDouble();
                            break;
                        }
                        ref Antity target = ref _antities[ant.TargetAntity];
                        var targetPos = target.Position;
                        Vector2 diff = targetPos - ant.Position;
                        float distance = diff.Length();
                        speed = (float)(egt * ANT_SPEED);
                        if ((distance - 5) < speed) {
                            // We're here!
                            ant.CoolDown = _rand.NextDouble() + 1;
                            if (ant.Value != 0) {
                                target.Value += ant.Value;
                                ant.Value = 0;
                            } else if (target.Type == AntityType.Food) {
                                target.Value -= ant.Value = 1;//Math.Min(1, target.Value);
                                if (target.Value == 0) {
                                    RemoveAntity(ant.TargetAntity);
                                    --_foodCount;
                                }
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
                            // Trace.WriteLine("COULD NOT FIND A NEARBY ANTHILL!");
                            ant.CoolDown = 0.5 + _rand.NextDouble();
                            ant.Action = Actions.Idle;
                        }
                        ant.TargetAntity = nearestAnthill;
                    } else {
                        // Target food
                        var nearestFood = FindNearest(ref ant.Position, AntityType.Food);
                        if (nearestFood == -1) {
                            // Trace.WriteLine("COULD NOT FIND A NEARBY FOOD!");
                            ant.CoolDown = 0.5 + _rand.NextDouble();
                            ant.Action = Actions.Idle;
                        }
                        ant.TargetAntity = nearestFood;
                    }
                    break;

                default:
                    ant.CoolDown = 0;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SelectNextEntity(AntityType? type = null, Actions? action = null) {
            Trace.WriteLine("Selecting next entity...");
            int next = -1;
            for (int i = 0; i < MAX_ANTITIES + 1; i++) {
                // LOOP
                int actualSelection = _selectedAntity != -1 ? (i + _selectedAntity) % MAX_ANTITIES : i;
                ref Antity ent = ref _antities[actualSelection];

                if (actualSelection == _selectedAntity || ent.Team != Team.Player || ent.Type == AntityType.None) continue;

                if (type.HasValue && ent.Type != type.Value) continue;
                else if (ent.Type == AntityType.Food) continue;

                if (action.HasValue && ent.Action != action.Value) continue;

                next = actualSelection;
                break;
            }
            _selectedAntity = next;
            Trace.WriteLine($"Selecting {next}");
        }

        protected override void Update(GameTime gameTime) {
            if (gameTime.TotalGameTime.TotalSeconds - _lastTrim >= 1.0) {
                var newRightmost = _rightmostAntity;
                for (int i = _rightmostAntity - 1; i >= 0; i--) {
                    if (_antitiesSet[i]) break;
                    newRightmost = i;
                }
                _rightmostAntity = newRightmost;
                _lastTrim = gameTime.TotalGameTime.TotalSeconds;
            }

            _input.Update(gameTime);
            _debug.Update(gameTime);

            if (InputManager.KeyWentDown(Keys.Escape))
                Exit();

            if (InputManager.KeyWentDown(Keys.F3))
                _debug.Enabled = !_debug.Enabled;

            if (InputManager.KeyWentDown(Keys.Tab))
                SelectNextEntity();

            if (InputManager.KeyWentDown(Keys.H))
                SelectNextEntity(AntityType.Anthill);

            if (InputManager.KeyWentDown(Keys.A))
                SelectNextEntity(AntityType.Ant);

            if (InputManager.KeyWentDown(Keys.I))
                SelectNextEntity(AntityType.Ant, Actions.Idle);

            for (int i = 0; i < _rightmostAntity; i++) {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawSelection(Rectangle rectangle) => SpriteBatch.Draw(_selectionPixel, rectangle, Color.White);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            for (int i = 0; i < _rightmostAntity; i++) {
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

            _debug.Draw(gameTime);

            SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
