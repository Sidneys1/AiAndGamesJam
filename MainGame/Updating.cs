using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AiAndGamesJam {
    public partial class AntGame {
        const int MAX_EXPENSIVE = 100;
        int _expensiveThisLoop = 0, _expensiveDebug = 0;

        double _lastStateTime = 7.0;

        const string SECRET = "aiandgames";
        private bool _secretUpdated = false;
        private CircularBuffer<char> _secret = new(SECRET.Length, new char[SECRET.Length]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateLogoState(GameTime gameTime) {
            double gts = gameTime.TotalGameTime.TotalSeconds;
            if (gts >= 7.0) {
                _state = GameState.Intro;
            } else if (InputManager.KeyWentDown(Keys.Enter)) {
                _state = GameState.Intro;
                _lastStateTime = gts;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateIntroState(GameTime gameTime) {
            if (gameTime.TotalGameTime.TotalSeconds >= (_lastStateTime + 10.0) || InputManager.KeyWentDown(Keys.Enter)) {
                _state = GameState.GenerateScenario;
                Window.AllowUserResizing = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateGameState(GameTime gameTime) {
            if (_secretUpdated) {
                string v = new(_secret.ToArray());
                if (v == SECRET) {
                    System.Diagnostics.Trace.WriteLine("SECRET!");
                    AddAntity(AntityType.Tommy, position: new Vector2((float)(200 + (_rand.NextDouble() * (GraphicsDevice.Viewport.Width - 200 - _tommy.Width))), GraphicsDevice.Viewport.Height));
                }
                _secretUpdated = false;
            }

            if (_expensiveThisLoop >= MAX_EXPENSIVE)
                _expensiveDebug++;
            _expensiveThisLoop = 0;


            if (!_sandbox && InputManager.KeyWentDown(Keys.F5))
                _sandbox = true;

            if (!_sandbox) {
                if (IsGoalSatisfied()) {
                    _state = GameState.Won;
                    return;
                }

                if (IsAllLost()) {
                    _state = GameState.Lost;
                    return;
                }
            }

            UpdateTrims(gameTime);

            if (InputManager.LeftMouseWentDown) {
                var mouse_position = InputManager.CurrentMouseState.Position.ToVector2();
                if (mouse_position.X < _leftPanelRect.Width) {
                    int newSelection = (int)(mouse_position.Y - 185) / 20;
                    if (newSelection < 0 || newSelection >= _jobs[Team.Player].Count)
                        _selectedJob = -1;
                    else {
                        _selectedJob = newSelection;
                        _selectedThing = _jobs[Team.Player][_selectedJob].Target;
                        _selectedAntity = -1;
                    }
                } else {
                    short thing = FindNearestThing(ref mouse_position, collision: true);
                    if (thing != -1) {
                        _selectedThing = thing;
                        _selectedAntity = _selectedJob = -1;
                    } else {
                        thing = FindNearestAntity(ref mouse_position, AntityType.Anthill, Team.Player, collision: true);
                        if (thing != -1) {
                            _selectedAntity = thing;
                            _selectedJob = _selectedThing = -1;
                        } else {
                            _selectedAntity = _selectedJob = _selectedThing = -1;
                        }
                    }
                }
            }

            if (_sandbox) {
                Vector2? addedFireant = null;
                if (InputManager.RightMouseWentDown) {
                    Vector2 position = InputManager.CurrentMouseState.Position.ToVector2();
                    switch (_sandboxSelection) {
                        case SandboxSelection.Food:
                            AddThing(ThingType.Food, position, value: 100);
                            break;
                        case SandboxSelection.Anthill:
                            AddAntity(AntityType.Anthill, Team.Player, position, Actions.Stockpile, value: 105);
                            break;
                        case SandboxSelection.Ant:
                            AddAntity(AntityType.Ant, Team.Player, position, Actions.Idle, 1.0);
                            break;
                        case SandboxSelection.Fire_Ant:
                            AddAntity(AntityType.Ant, Team.Fireants, position, Actions.Idle, 1.0);
                            addedFireant = position;
                            break;
                    }
                }
                if (InputManager.CurrentKeyboardState.IsKeyDown(Keys.F6)) {
                    for (int i = 0; i < 10; i++) {
                        AddAntity(AntityType.Ant,
                                  Team.Player,
                                  new Vector2((float)(_rand.NextDouble() * (GraphicsDevice.Viewport.Width - 200)) + 200,
                                              (float)(_rand.NextDouble() * GraphicsDevice.Viewport.Height)),
                                  Actions.Idle,
                                  1.0 + (_rand.NextDouble() - 0.5));
                    }
                }
                if (InputManager.CurrentKeyboardState.IsKeyDown(Keys.F7)) {
                    addedFireant = new Vector2((float)(_rand.NextDouble() * (GraphicsDevice.Viewport.Width - 200)) + 200,
                                                              (float)(_rand.NextDouble() * GraphicsDevice.Viewport.Height));
                    AddAntity(AntityType.Ant,
                              Team.Fireants,
                              addedFireant.Value,
                              Actions.Idle,
                              1.0 + (_rand.NextDouble() - 0.5));
                }

                if (addedFireant.HasValue && _firstSandboxFireant) {
                    Vector2 position = addedFireant.Value;
                    AddJob(JobType.Attack, Team.Fireants);
                    _firstSandboxFireant = false;
                }

                int scrollwheelDelta = InputManager.ScrollwheelDelta;
                if (scrollwheelDelta != 0) {
                    if (scrollwheelDelta > 0) _sandboxSelection++;
                    else _sandboxSelection--;

                    if (_sandboxSelection == SandboxSelection.BEGIN) _sandboxSelection = SandboxSelection.Fire_Ant;
                    else if (_sandboxSelection == SandboxSelection.END) _sandboxSelection = SandboxSelection.Anthill;
                }
            }

            if (InputManager.KeyWentDown(Keys.F1))
                _debug.Enabled = !_debug.Enabled;

            if (InputManager.KeyWentDown(Keys.F2) || InputManager.KeyWentDown(Keys.MediaPreviousTrack))
                PreviousSong();

            if (InputManager.KeyWentDown(Keys.F3) || InputManager.KeyWentDown(Keys.MediaNextTrack))
                NextSong();

            if (InputManager.KeyWentDown(Keys.F4) || InputManager.KeyWentDown(Keys.MediaPlayPause))
                ToggleSong();

            if (InputManager.KeyWentDown(Keys.F11))
                _graphics.ToggleFullScreen();

            if (InputManager.KeyWentDown(Keys.Tab))
                SelectNextAntity();

            if (InputManager.KeyWentDown(Keys.H))
                SelectNextAntity(AntityType.Anthill);

            if (InputManager.KeyWentDown(Keys.A))
                SelectNextAntity(AntityType.Ant);

            if (InputManager.KeyWentDown(Keys.I))
                SelectNextAntity(AntityType.Ant, Actions.Idle);

            if (InputManager.KeyWentDown(Keys.F))
                SelectNextThing(ThingType.Food);

            if (InputManager.KeyWentDown(Keys.G))
                AddJob(JobType.Gather, Team.Player, _selectedThing);

            if (InputManager.KeyWentDown(Keys.D))
                AddJob(JobType.Distribute, Team.Player);

            if (_selectedJob != -1 && InputManager.KeyWentDown(Keys.Delete)) {
                RemoveJob(_jobs[Team.Player][_selectedJob], Team.Player);

            }

            for (short i = 0; i < _rightmostAntity; i++) {
                if (!_antitiesSet[i]) continue;

                var egt = gameTime.ElapsedGameTime.TotalSeconds;
                _antities[i].Age += egt;

                if (_selectedAntity == i && _antities[i].Type == AntityType.Anthill) {
                    if (InputManager.KeyWentDown(Keys.C))
                        _antities[i].Action = Actions.NewAnts;
                    else if (InputManager.KeyWentDown(Keys.S)) {
                        if (_antities[i].Action == Actions.BuildingAnt)
                            _antities[i].Value += 20;
                        _antities[i].CoolDown = 0;
                        _antities[i].Action = Actions.Stockpile;
                    }
                }

                if (_antities[i].CoolDown > egt) {
                    _antities[i].CoolDown -= egt;
                    continue;
                }

                Dictionary<Job, bool> jobCache = new();

                switch (_antities[i].Type) {
                    case AntityType.Anthill:
                        UpdateAnthill(i, ref _antities[i], egt);
                        break;
                    case AntityType.Ant:
                        Job j = _antities[i].Job;
                        if (j != null) {
                            if (!jobCache.TryGetValue(j, out bool valid)) {
                                valid = _jobs[_antities[i].Team].Contains(j);
                                jobCache.Add(j, valid);
                            }
                            if (!valid) {
                                _antities[i].Job = null;
                                _antities[i].CoolDown = _rand.NextDouble();
                                _antities[i].Action = Actions.Idle;
                                break;
                            }
                        }
                        UpdateAnt(i, ref _antities[i], egt);
                        break;
                    case AntityType.Tommy:
                        UpdateTommy(i, ref _antities[i], egt);
                        break;
                }
            }
        }

        private bool IsAllLost() {
            if (_anthillCache[Team.Player].Count == 0)
                return true;

            int remainingFood = 0;
            for (short i = 0; i < _rightmostThing; i++) {
                if (!_thingsSet[i] || _things[i].Type != ThingType.Food) continue;
                ref Thing thing = ref _things[i];
                remainingFood += thing.Value;
            }

            if ((_goalValue - _goalCurrent) > remainingFood)
                return true;

            return false;
        }

        private bool IsGoalSatisfied() {
            switch (_goal) {
                case GoalTypes.AmountOfFood:
                    _goalCurrent = _anthillCache[Team.Player].Select(i => _antities[i].Value).Sum();
                    return _goalCurrent >= _goalValue;
                // TODO: add more goal states
                default: return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTommy(short _, ref Antity antity, double egt) {
            switch (antity.Value) {
                case 0:
                    // Coming up
                    int targetHeight = GraphicsDevice.Viewport.Height - _tommy.Height;
                    if (antity.Position.Y > targetHeight) {
                        float diff = (float)(TOMMY_SPEED * egt);
                        if ((antity.Position.Y - targetHeight) <= diff) {
                            antity.Value = 1;
                            antity.CoolDown = 5 * _rand.NextDouble();
                            antity.Position.Y = targetHeight;
                        } else antity.Position.Y -= diff;
                    }
                    break;
                case 1:
                    // Going down...
                    targetHeight = GraphicsDevice.Viewport.Height;
                    if (antity.Position.Y < targetHeight) {
                        float diff = (float)(TOMMY_SPEED * egt);
                        if ((targetHeight - antity.Position.Y) <= diff) {
                            antity.Value = 2;
                            // antity.CoolDown = 30 + (60 * _rand.NextDouble());
                            antity.Position.Y = targetHeight;
                            antity.Position.X = (float)(200 + (_rand.NextDouble() * (GraphicsDevice.Viewport.Width - 200 - _tommy.Width)));
                        } else antity.Position.Y += diff;
                    }
                    break;
                case 2:
                    // Hidden
                    antity.Value = 0;
                    break;
                default: antity.CoolDown = 1; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAnthill(int _, ref Antity anthill, double __) {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool UpdateAntDistribute(ref Antity ant, ref Vector2 diff, float speed) {
            Vector2 targetPos;
            if (ant.Value > 0) {
                // Going to low anthill...
                if (ant.TargetAntity == -1) {
                    // Pick a target
                    ant.CoolDown = _rand.NextDouble();
                    if (_expensiveThisLoop >= MAX_EXPENSIVE)
                        return false;
                    int lowest = int.MaxValue;
                    short lowestAnthill = -1;
                    foreach (var anthill in _anthillCache[ant.Team]) {
                        if (_antities[anthill].Value >= lowest)
                            continue;
                        lowestAnthill = anthill;
                        lowest = _antities[anthill].Value;
                    }
                    ant.TargetAntity = lowestAnthill;
                    _expensiveThisLoop++;
                    return false;
                }
                if (!_antitiesSet[ant.TargetAntity]) {
                    // Our anthill got deleted??
                    ant.TargetAntity = -1;
                    ant.CoolDown = _rand.NextDouble();
                    return false;
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
                    return false;
                }
            } else {
                // Going to higher anthill
                if (ant.TargetAntity == -1) {
                    // Pick a target
                    ant.CoolDown = _rand.NextDouble();
                    if (_expensiveThisLoop >= MAX_EXPENSIVE)
                        return false;
                    int highest = int.MinValue;
                    short highestAnthill = -1;
                    foreach (var anthill in _anthillCache[ant.Team]) {
                        if (_antities[anthill].Value <= highest || _antities[anthill].Value <= 25)
                            continue;
                        highestAnthill = anthill;
                        highest = _antities[anthill].Value;
                    }
                    ant.TargetAntity = highestAnthill;
                    _expensiveThisLoop++;
                    return false;
                }
                if (!_antitiesSet[ant.TargetAntity]) {
                    // Our anthill got deleted?
                    ant.Action = Actions.Idle;
                    ant.Job = null;
                    ant.TargetAntity = -1;
                    ant.CoolDown = _rand.NextDouble();
                    return false;
                }
                ref Antity target = ref _antities[ant.TargetAntity];
                targetPos = target.Position;
                diff = targetPos - ant.Position;
                float distance = diff.Length();
                if ((distance - 5) < speed) {
                    // We're here!
                    ant.CoolDown = _rand.NextDouble() + 1;
                    target.Value -= ant.Value = 1;
                    ant.TargetAntity = -1;
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool UpdateAntGather(ref Antity ant, ref Vector2 diff, float speed) {
            Vector2 targetPos;
            if (ant.Value > 0) {
                // Going to anthill...
                if (ant.TargetAntity == -1) {
                    // Pick a target
                    ant.CoolDown = _rand.NextDouble();
                    if (_expensiveThisLoop >= MAX_EXPENSIVE)
                        return false;
                    ant.TargetAntity = FindNearestAntity(ref ant.Position, AntityType.Anthill, ant.Team);
                    _expensiveThisLoop++;
                    return false;
                }
                if (!_antitiesSet[ant.TargetAntity]) {
                    // Our anthill got deleted??
                    ant.TargetAntity = -1;
                    ant.CoolDown = _rand.NextDouble();
                    return false;
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
                    return false;
                }
            } else {
                // Going to food
                short targetIdx = ant.Job.Target;
                if (targetIdx == -1) targetIdx = ant.TargetAntity;
                if (targetIdx == -1) targetIdx = ant.TargetAntity = FindNearestThing(ref ant.Position, ThingType.Food);

                if (targetIdx == -1) {
                    ant.Job = null;
                    ant.Action = Actions.Idle;
                    ant.TargetAntity = -1;
                    return false;
                }
                if (!_thingsSet[targetIdx]) {
                    // Our food got deleted?
                    ant.Action = Actions.Idle;
                    ant.Job = null;
                    ant.TargetAntity = -1;
                    return false;
                }
                ref Thing target = ref _things[targetIdx];
                targetPos = target.Position;
                diff = targetPos - ant.Position;
                float distance = diff.Length();
                if ((distance - 5) < speed) {
                    // We're here!
                    ant.CoolDown = _rand.NextDouble() + 1;
                    target.Value -= ant.Value = 1;//Math.Min(1, target.Value);
                    if (target.Value == 0) {
                        // Delete food
                        RemoveThing(targetIdx);
                        // Delete job(s)
                        if (ant.Job.Target != -1) {
                            RemoveJobsForThing(targetIdx);
                            ant.Action = Actions.Idle;
                            ant.Job = null;
                            ant.TargetAntity = -1;
                        }
                    }
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAnt(short idx, ref Antity ant, double egt) {
            if (ant.Age >= 600 && _rand.NextDouble() >= 0.99) {
                RemoveAntity(idx);
                return;
            }

            switch (ant.Action) {
                case Actions.Idle: {
                        ant.CoolDown = 0.05;
                        if (_rand.NextDouble() >= 0.95 && _expensiveThisLoop < MAX_EXPENSIVE) {
                            ant.Job = SelectRandomJob(ant.Team);
                            if (ant.Job != null)
                                ant.Action = Actions.Job;
                            _expensiveThisLoop++;
                            break;
                        }
                        var rotation = (float)_rand.NextDouble() * MathHelper.TwoPi;
                        var move = new Vector2((float)System.Math.Cos(rotation), (float)System.Math.Sin(rotation));
                        move.Normalize();
                        var speed = (float)(ANT_SPEED * egt);
                        ant.Position += move * speed;
                    }
                    break;

                case Actions.Job: {
                        // Trace.WriteLine("Ant is on the job");
                        Vector2 diff = default;
                        var speed = (float)(egt * ANT_SPEED);

                        if (ant.Job.Type == JobType.Gather) {
                            if (!UpdateAntGather(ref ant, ref diff, speed))
                                break;
                        } else if (ant.Job.Type == JobType.Distribute) {
                            if (!UpdateAntDistribute(ref ant, ref diff, speed))
                                break;
                        } else if (ant.Job.Type == JobType.Attack) {
                            if (!UpdateAntAttack(ref ant, ref diff, speed))
                                break;
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

        private bool UpdateAntAttack(ref Antity ant, ref Vector2 diff, float speed) {
            // Going to attack
            short targetIdx;

            if (ant.Job.Target != -1) targetIdx = ant.Job.Target;
            else if (ant.TargetAntity != -1) targetIdx = ant.TargetAntity;
            else targetIdx = ant.TargetAntity = FindNearestAntity(ref ant.Position, AntityType.Anthill, ant.Team switch { Team.Fireants => Team.Player, _ => null });

            if (targetIdx == -1) {
                ant.CoolDown = _rand.NextDouble();
                ant.Action = Actions.Idle;
                return false;
            }

            if (!_antitiesSet[targetIdx]) {
                // Target anthill got deleted?
                ant.Action = Actions.Idle;
                ant.Job = null;
                ant.TargetAntity = -1;
                ant.CoolDown = _rand.NextDouble();
                return false;
            }
            ref Antity target = ref _antities[targetIdx];
            Vector2 targetPos = target.Position;
            diff = targetPos - ant.Position;
            float distance = diff.Length();
            if ((distance - 5) < speed) {
                // We're here!
                ant.CoolDown = _rand.NextDouble() + 1;
                target.Value--;
                if (target.Value == 0) {
                    var rotation = (float)_rand.NextDouble() * MathHelper.TwoPi;
                    var move = new Vector2((float)System.Math.Cos(rotation), (float)System.Math.Sin(rotation));
                    move.Normalize();
                    ant.Position += move * speed;

                    // Delete anthill
                    RemoveAntity(targetIdx);
                    // Delete job(s)
                    RemoveJobsForAntity(targetIdx);
                    ant.Action = Actions.Idle;
                    ant.Job = null;
                    ant.TargetAntity = -1;
                    ant.CoolDown = _rand.NextDouble();
                }
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTrims(GameTime gameTime) {
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

            if (totalSeconds - _goalTextLastUpdated >= 0.25) {
                _goalText = _goal switch {
                    GoalTypes.AmountOfFood => $"     {_goalCurrent / _goalValue,3:P0} Done",
                    _ => "UNKNOWN",
                };
                _goalTextLastUpdated = totalSeconds;
            }
        }
    }
}