using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AiAndGamesJam {
    public class AntGame : Game {
        const int MAX_ANTITIES = 1_000;
        const int MAX_THINGS = 1_000;
        const int ANT_SPEED = 200;

        private Color ANT_MIDDLE_AGE = new(10, 10, 10);
        private Color ANT_OLD_AGE = new(20, 20, 20);

        private readonly GraphicsDeviceManager _graphics;
        public SpriteBatch SpriteBatch;

        private readonly Random _rand = new();

        private readonly DebugComponent _debug;
        private readonly InputManager _input;

        private readonly BitArray _antitiesSet = new(MAX_ANTITIES, false);
        private readonly Antity[] _antities = new Antity[MAX_ANTITIES];
        private readonly List<short> _anthillCache = new();
        private int _rightmostAntity = 0;
        private double _lastAntitiesTrim = 0;
        private int _selectedAntity = -1;

        private readonly BitArray _thingsSet = new(MAX_THINGS, false);
        private readonly Thing[] _things = new Thing[MAX_THINGS];
        private int _rightmostThing = 0;
        private double _lastThingsTrim = 0;
        private short _selectedThing = -1;

        private readonly List<Job> _jobs = new();
        private int _totalWeight = 0;

        void AddJob(Job job) {
            _jobs.Add(job);
            _totalWeight += job.Priority;
        }

        void RemoveJob(Job job) {
            if (_jobs.Remove(job))
                _totalWeight -= job.Priority;
        }

        Job SelectRandomJob() {
            int randomNumber = _rand.Next(_totalWeight);
            for (int i = 0; i < _jobs.Count; i++) {
                var job = _jobs[i];
                if (randomNumber < job.Priority)
                    return job;
                randomNumber -= job.Priority;
            }
            return null;
        }

        void AddAntity(AntityType type = AntityType.None, Team team = Team.None, Vector2? position = null, Actions action = 0, double coolDown = 0, int value = 0) {
            short pos = -1;
            for (short i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) {
                    pos = i;
                    break;
                }
            }
            if (pos == -1)
                throw new Exception("All entity slots are taken!");

            if (pos >= _rightmostAntity) _rightmostAntity = pos + 1;

            ref Antity ent = ref _antities[pos];

            ent.Type = type;
            ent.Team = team;
            ent.Position = position.GetValueOrDefault();
            ent.Action = action;
            ent.CoolDown = coolDown;
            ent.Value = value;
            ent.Job = null;

            _antitiesSet[pos] = true;
            if (type == AntityType.Anthill) _anthillCache.Add(pos);
        }

        void AddThing(ThingType type, Vector2 position, int value = 0) {
            var pos = -1;
            for (int i = 0; i < MAX_THINGS; i++) {
                if (!_thingsSet[i]) {
                    pos = i;
                    break;
                }
            }
            if (pos == -1)
                throw new Exception("All thing slots are taken!");

            if (pos >= _rightmostThing) _rightmostThing = pos + 1;

            ref Thing thing = ref _things[pos];

            thing.Type = type;
            thing.Position = position;
            thing.Value = value;

            _thingsSet[pos] = true;
        }

        void RemoveAntity(short pos) {
            if (_antities[pos].Type == AntityType.Anthill)
                _anthillCache.Remove(pos);
            _antitiesSet[pos] = false;
        }

        void RemoveThing(int pos) => _thingsSet[pos] = false;

        private Texture2D _selectionPixel, _pixel, _antHill, _ant, _food, _bg;
        private Vector2 _anthillOffset = new(24, 24);
        private Vector2 _antOffset = new(3, 2);
        private Vector2 _foodOffset = new(16, 16);
        private SpriteFont _font;
        private readonly Color _bgColor = new(114, 94, 0);

        public AntGame() {
            _graphics = new GraphicsDeviceManager(this) {
                // PreferredBackBufferWidth = 1280,
                // PreferredBackBufferHeight = 720,
                GraphicsProfile = GraphicsProfile.HiDef,
            };

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

            AddAntity(AntityType.Anthill, Team.Player, position: new Vector2(400, 300), action: Actions.NewAnts, coolDown: 5.0 * _rand.NextDouble(), value: 105);
            AddAntity(AntityType.Anthill, Team.Player, position: new Vector2(200, 300), action: Actions.NewAnts, coolDown: 5.0 * _rand.NextDouble(), value: 105);
            for (int i = 0; i < 5; i++) {
                AddThing(ThingType.Food,
                         new Vector2(_rand.Next(16, _graphics.PreferredBackBufferWidth - 16),
                                     _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
                         value: 100);
            }

            for (short i = 0; i < 5; i++) {
                AddJob(new Job() {
                    Priority = 1,
                    Target = i,
                    Type = JobType.Gather
                });
            }
            // Trace.WriteLine("Creating ants...");
            // for (int i = 0; i < 500; i++) {
            //     AddAntity(
            //         AntityType.Ant,
            //         Team.Player,
            //         position: new Vector2(_rand.Next(16, _graphics.PreferredBackBufferWidth - 16), _rand.Next(16, _graphics.PreferredBackBufferHeight - 16)),
            //         action: Actions.Job, // Actions.Job,
            //         coolDown: (_rand.NextDouble() * 2) + 3);
            //     _antities[i + 1].Job = _jobs[i % 50];
            // }
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private short FindNearestAntity(ref Vector2 position, AntityType? type = null, Team? team = null, bool collision = false) {
            short ret = -1;
            float nearest = float.MaxValue;
            IEnumerable<short> range;
            if (type.HasValue && type.Value == AntityType.Anthill)
                range = _anthillCache.AsEnumerable();
            else range = Enumerable.Range(0, _rightmostAntity).Select(x => (short)x);

            foreach (short i in range) {
                if (!_antitiesSet[i]) continue;
                ref Antity ent = ref _antities[i];
                if ((type.HasValue && ent.Type != type.Value) || (team.HasValue && ent.Team != team.Value)) continue;
                // Manhattan distance
                var distance = Math.Abs(ent.Position.X - position.X) + Math.Abs(ent.Position.Y - position.Y);

                if (collision) {
                    switch (ent.Type) {
                        case AntityType.Anthill:
                            if (distance > 48) continue;
                            break;
                        case AntityType.Ant:
                            if (distance > 10) continue;
                            break;
                        default:
                            Trace.WriteLine("MISSING COLLISION HANDLER IN FindNearestThing!");
                            break;
                    }
                }

                if (distance < nearest) {
                    ret = i;
                    nearest = distance;
                }
            }
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private short FindNearestThing(ref Vector2 position, ThingType? type = null, bool collision = false) {
            short ret = -1;
            float nearest = float.MaxValue;
            for (short i = 0; i < _rightmostThing; i++) {
                if (!_thingsSet[i]) continue;
                ref Thing ent = ref _things[i];
                if (type.HasValue && ent.Type != type.Value) continue;
                // Manhattan distance
                var distance = Math.Abs(ent.Position.X - position.X) + Math.Abs(ent.Position.Y - position.Y);

                if (collision) {
                    switch (ent.Type) {
                        case ThingType.Food:
                            if (distance > 32) continue;
                            break;
                        default:
                            Trace.WriteLine("MISSING COLLISION HANDLER IN FindNearestThing!");
                            break;
                    }
                }

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
                    if (anthill.Value >= 25) {
                        anthill.CoolDown = 2;
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

        const int MAX_EXPENSIVE = 100;
        int _expensiveThisLoop = 0, _expensiveDebug = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAnt(short idx, ref Antity ant, double egt) {
            if (ant.Age >= 120 && _rand.NextDouble() >= 0.99) {
                RemoveAntity(idx);
                return;
            }

            switch (ant.Action) {
                case Actions.Idle: {
                        ant.CoolDown = 0.05;
                        if (_rand.NextDouble() >= 0.9 && _expensiveThisLoop < MAX_EXPENSIVE) {
                            ant.Job = SelectRandomJob();
                            if (ant.Job != null)
                                ant.Action = Actions.Job;
                            _expensiveThisLoop++;
                            break;
                        }
                        var rotation = (float)_rand.NextDouble() * MathHelper.TwoPi;
                        var move = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));
                        move.Normalize();
                        var speed = (float)(ANT_SPEED * egt);
                        ant.Position += move * speed;
                    }
                    break;

                case Actions.Job: {
                        // Trace.WriteLine("Ant is on the job");
                        Vector2 targetPos, diff;
                        var speed = (float)(egt * ANT_SPEED);
                        if (ant.Value > 0) {
                            // Going to anthill...
                            if (ant.TargetAntity == -1) {
                                // Pick a target
                                ant.CoolDown = _rand.NextDouble();
                                if (_expensiveThisLoop >= MAX_EXPENSIVE)
                                    break;
                                ant.TargetAntity = FindNearestAntity(ref ant.Position, AntityType.Anthill, ant.Team);
                                _expensiveThisLoop++;
                                break;
                            }
                            if (!_antitiesSet[ant.TargetAntity]) {
                                // Our anthill got deleted??
                                ant.TargetAntity = -1;
                                ant.CoolDown = _rand.NextDouble();
                                break;
                            }
                            ref Antity target = ref _antities[ant.TargetAntity];
                            targetPos = target.Position;
                            diff = targetPos - ant.Position;
                            float distance = diff.Length();

                            if ((distance - 5) < speed) {
                                // We're here!
                                ant.CoolDown = _rand.NextDouble() + 1;
                                target.Value += ant.Value;
                                ant.Value = 0;
                                ant.Action = Actions.Idle;
                                ant.Job = null;
                                ant.TargetAntity = -1;
                                break;
                            }
                        } else {
                            // Going to food
                            if (!_thingsSet[ant.Job.Target]) {
                                // Our food got deleted
                                RemoveJob(ant.Job);
                                ant.Action = Actions.Idle;
                                ant.Job = null;
                                ant.TargetAntity = -1;
                                ant.CoolDown = _rand.NextDouble();
                                break;
                            }
                            ref Thing target = ref _things[ant.Job.Target];
                            targetPos = target.Position;
                            diff = targetPos - ant.Position;
                            float distance = diff.Length();
                            if ((distance - 5) < speed) {
                                // We're here!
                                ant.CoolDown = _rand.NextDouble() + 1;
                                target.Value -= ant.Value = 1;//Math.Min(1, target.Value);
                                if (target.Value == 0) {
                                    // Delete food
                                    RemoveThing(ant.Job.Target);
                                    // Delete job
                                    RemoveJob(ant.Job);
                                    ant.Action = Actions.Idle;
                                    ant.Job = null;
                                    ant.TargetAntity = -1;
                                    ant.CoolDown = _rand.NextDouble();
                                }
                                break;
                            }
                        }
                        // Wiggle
                        diff.Normalize();
                        // var randomRot = (float)(_rand.NextDouble() - 0.5) * 2;
                        // diff = Vector2.Transform(diff, Matrix.CreateRotationX((float)(randomRot * Math.PI)));
                        diff += new Vector2((float)(_rand.NextDouble() - 0.5), (float)(_rand.NextDouble() - 0.5));
                        diff.Normalize();
                        ant.Position += diff * speed;
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
            for (int i = 0; i < MAX_ANTITIES; i++) {
                // LOOP
                int actualSelection = _selectedAntity != -1 ? (i + _selectedAntity) % MAX_ANTITIES : i;
                ref Antity ent = ref _antities[actualSelection];

                if (actualSelection == _selectedAntity || ent.Team != Team.Player || ent.Type == AntityType.None) continue;

                if (type.HasValue && ent.Type != type.Value) continue;

                if (action.HasValue && ent.Action != action.Value) continue;

                next = actualSelection;
                break;
            }
            _selectedAntity = next;
            Trace.WriteLine($"Selecting {next}");
        }

        protected override void Update(GameTime gameTime) {
            if (_expensiveThisLoop >= MAX_EXPENSIVE)
                _expensiveDebug++;
            _expensiveThisLoop = 0;

            double totalSeconds = gameTime.TotalGameTime.TotalSeconds;
            if (totalSeconds - _lastAntitiesTrim >= 1.0) {
                var newRightmost = _rightmostAntity;
                for (int i = _rightmostAntity - 1; i >= 0; i--) {
                    if (_antitiesSet[i]) break;
                    newRightmost = i;
                }
                _rightmostAntity = newRightmost + 1;
                _lastAntitiesTrim = totalSeconds;
            }

            if (totalSeconds - _lastThingsTrim >= 1.0) {
                var newRightmost = _rightmostThing;
                for (int i = _rightmostThing - 1; i >= 0; i--) {
                    if (_thingsSet[i]) break;
                    newRightmost = i;
                }
                _rightmostThing = newRightmost + 1;
                _lastThingsTrim = totalSeconds;
            }

            _input.Update(gameTime);
            _debug.Update(gameTime);

            if (InputManager.LeftMouseWentDown()) {
                var mpos = InputManager.CurrentMouseState.Position.ToVector2();
                short thing = FindNearestThing(ref mpos, collision: true);
                if (thing != -1) {
                    Trace.WriteLine("Clicked on thing");
                    _selectedThing = thing;
                    _selectedAntity = -1;
                } else {
                    thing = FindNearestAntity(ref mpos, team: Team.Player, collision: true);
                    if (thing != -1) {
                        Trace.WriteLine("Clicked on Antity");
                        _selectedAntity = thing;
                        _selectedThing = -1;
                    } else {
                        _selectedAntity = _selectedThing = -1;
                    }
                }
            }

            if (InputManager.RightMouseWentDown())
                AddThing(ThingType.Food, InputManager.CurrentMouseState.Position.ToVector2(), value: 100);

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

            if (_selectedThing != -1 && InputManager.KeyWentDown(Keys.G)) {
                AddJob(new Job {
                    Priority = 1,
                    Target = _selectedThing,
                    Type = JobType.Gather
                });
                SelectNextEntity(AntityType.Ant, Actions.Idle);
            }

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
                var text = v + anthill.CoolDown.ToString("0.0s");
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
            var color = ant.Age switch {
                >= 80 => ANT_OLD_AGE,
                >= 40 => ANT_MIDDLE_AGE,
                _ => Color.Black,
            };
            SpriteBatch.Draw(_ant, pos - _antOffset, color);

            if (ant.Action == Actions.Job && ant.Job?.Type == JobType.Gather && ant.Value > 0)
                SpriteBatch.Draw(_pixel, new Rectangle((int)pos.X, (int)pos.Y, 2, 2), Color.Green);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawFood(ref Thing food, bool selected) {
            var pos = food.Position;
            if (selected) {
                Vector2 padding = new(3);
                Rectangle selection = new((pos - _anthillOffset - padding).ToPoint(), ((_anthillOffset * 2) + (padding * 2)).ToPoint());
                DrawSelection(selection);
            }
            Color color;
            if (food.Value >= 66)
                color = Color.White;
            else if (food.Value >= 50)
                color = Color.SandyBrown;
            else
                color = Color.Brown;
            SpriteBatch.Draw(_food, pos - _foodOffset, color);
            Rectangle rect = new((int)(pos.X - _foodOffset.X), (int)(pos.Y + _foodOffset.Y) + 3, (int)(32 * (food.Value / 100f)), 3);
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

            SpriteBatch.Draw(_bg, Vector2.Zero, Color.White);

            for (int i = 0; i < _rightmostThing; i++) {
                if (!_thingsSet[i]) continue;

                ref Thing thing = ref _things[i];

                switch (thing.Type) {
                    case ThingType.Food:
                        DrawFood(ref thing, i == _selectedThing);
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

            Vector2 textPos = new(10, 10);
            for (int i = 0; i < _jobs.Count; i++) {
                Job job = _jobs[i];
                textPos.Y += 20;
                string text = job.Type switch {
                    JobType.Gather => "Gather Food",
                    _ => $"Unknown: '{job.Type}'",
                };
                SpriteBatch.DrawString(_font, $"{text} ({job.Priority})", textPos, Color.White);
            }

            _debug.Draw(gameTime);

            SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
