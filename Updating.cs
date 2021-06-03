using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace AiAndGamesJam {
    public partial class AntGame {
        const int MAX_EXPENSIVE = 100;
        int _expensiveThisLoop = 0, _expensiveDebug = 0;

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
                        var move = new Vector2((float)System.Math.Cos(rotation), (float)System.Math.Sin(rotation));
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
                                // Our food got deleted?
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
                                    // Delete job(s)
                                    RemoveJobsFor(ant.Job.Target);
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
        }
    }
}