using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace AiAndGamesJam {
    public partial class AntGame {
        private Rectangle _leftPanelRect, _leftPanelSplitterRect;
        private Texture2D _selectionPixel, _pixel, _antHill, _ant, _food, _bg, _logo, _tommy;
        private Vector2 _anthillOffset = new(24, 24), _antOffset = new(3, 2), _foodOffset = new(16, 16), _logoPos, _titlePos, _introPos;
        private SpriteFont _font, _largeFont, _pixelmix;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawLogoState(GameTime gameTime) {
            var gts = gameTime.TotalGameTime.TotalSeconds;
            if (gts <= 2.0) {
                GraphicsDevice.Clear(Color.Lerp(Color.Black, Color.White, (float)(gts / 2)));
                return;
            } else if (gts >= 7.0) { GraphicsDevice.Clear(Color.Black); return; }

            SpriteBatch.Begin();
            if (gts <= 4.0) {
                GraphicsDevice.Clear(Color.White);
                SpriteBatch.Draw(_logo, _logoPos, Color.Lerp(Color.Transparent, Color.White, (float)((gts - 2) / 2)));
            } else if (gts <= 5.0) {
                GraphicsDevice.Clear(Color.White);
                SpriteBatch.Draw(_logo, _logoPos, Color.White);
            } else if (gts <= 7.0) {
                GraphicsDevice.Clear(Color.Lerp(Color.White, Color.Black, (float)((gts - 5) / 2)));
                SpriteBatch.Draw(_logo, _logoPos, Color.Lerp(Color.White, Color.Transparent, (float)((gts - 5) / 2)));
            }
            SpriteBatch.End();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawIntroState(GameTime gameTime) {
            var gts = gameTime.TotalGameTime.TotalSeconds;

            GraphicsDevice.Clear(Color.Black);
            SpriteBatch.Begin();
            var textColor = Color.Lerp(Color.Transparent, Color.White, (float)((gts - _lastStateTime) / 2));
            SpriteBatch.DrawString(_largeFont, TITLE, _titlePos, textColor);
            SpriteBatch.DrawString(_font, INTRO, _introPos, textColor);
            SpriteBatch.End();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawGameState(GameTime gameTime) {
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
                    case AntityType.Tommy:
                        if (ent.Value != 2)
                            SpriteBatch.Draw(_tommy, ent.Position, Color.White);
                        break;
                    default: continue;
                }
            }

            DrawPanel();

            _debug.Draw(gameTime);

            SpriteBatch.End();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawSelection(Rectangle rectangle, bool secondarySelection = false) =>
            SpriteBatch.Draw(_selectionPixel, rectangle, secondarySelection ? Color.Gray : Color.White);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawAnthill(ref Antity anthill, bool selected) {
            var pos = anthill.Position;
            if (selected) {
                Vector2 padding = new(3);
                Rectangle selection = new((pos - _anthillOffset - padding).ToPoint(), ((_anthillOffset * 2) + (padding * 2)).ToPoint());
                DrawSelection(selection);
            }
            SpriteBatch.Draw(_antHill, pos - _anthillOffset, Color.White);

            if (anthill.Action == Actions.BuildingAnt)
                SpriteBatch.DrawString(_pixelmix, $"New Ant in {anthill.CoolDown:0.0}s", pos - new Vector2(_pixelmix.MeasureString($"New Ant in {anthill.CoolDown:0.0}s").X / 2, -30), Color.White);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawAnt(ref Antity ant, bool selected) {
            var pos = ant.Position;
            if (ant.Team == Team.Fireants) {
                SpriteBatch.Draw(_ant, pos - _antOffset, FIRE_ANT_COLOR);
            } else {
                if (selected) {
                    Vector2 padding = new(3);
                    Rectangle selection = new((pos - _antOffset - padding).ToPoint(), ((_antOffset * 2) + (padding * 2)).ToPoint());
                    DrawSelection(selection);
                }
                var color = ant.Age switch {
                    >= 300 => ANT_OLD_AGE,
                    >= 200 => ANT_MIDDLE_AGE,
                    _ => Color.Black,
                };
                SpriteBatch.Draw(_ant, pos - _antOffset, color);
            }

            if (ant.Value > 0)
                SpriteBatch.Draw(_pixel, new Rectangle((int)pos.X, (int)pos.Y, 2, 2), Color.Green);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawFood(ref Thing food, bool selected, bool secondarySelection) {
            var pos = food.Position;
            if (selected) {
                Vector2 padding = new(3);
                Rectangle selection = new((pos - _anthillOffset - padding).ToPoint(), ((_anthillOffset * 2) + (padding * 2)).ToPoint());
                DrawSelection(selection, secondarySelection);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawPanel() {
            SpriteBatch.Draw(_pixel, _leftPanelRect, null, Color.Black);

            bool isAntitySelected = _selectedAntity != -1;
            bool isThingSelected = _selectedThing != -1;
            bool isJobSelected = _selectedJob != -1;
            Vector2 textPos = new(25, 1);
            SpriteBatch.DrawString(_pixelmix, $"Currently {(MediaPlayer.State == MediaState.Playing ? "playing" : "paused")}: Track {_currentSong + 1}", textPos, ANT_OLD_AGE);

            textPos.X = 15;
            textPos.Y = 15;
            SpriteBatch.DrawString(_font, "       GOAL", textPos, Color.White);
            textPos.X = 20;
            textPos.Y += 20;

            SpriteBatch.DrawString(_font, _goalDesc + _goalText, textPos, Color.Gray);

            textPos.X = 15;
            textPos.Y = 75;
            SpriteBatch.DrawString(_font, "      DETAILS", textPos, Color.White);
            textPos.X = 20;
            textPos.Y += 20;
            string detailLine;
            if (isJobSelected) {
                Job job = _jobs[Team.Player][_selectedJob];
                detailLine = job.Type switch {
                    JobType.Gather => "   Gather Food",
                    JobType.Distribute => " Distribute Food",
                    _ => "    missingno",
                };
            } else if (isThingSelected) {
                ref Thing thing = ref _things[_selectedThing];
                detailLine = thing.Type switch {
                    ThingType.Food => $" Food: {100 - thing.Value}% eaten",
                    _ => "    missingno",
                };
            } else if (isAntitySelected) {
                ref Antity antity = ref _antities[_selectedAntity];
                detailLine = antity.Type switch {
                    AntityType.Anthill => antity.Action switch {
                        Actions.BuildingAnt => "  Making an Ant",
                        Actions.NewAnts => " Waiting for Food",
                        Actions.Stockpile => " Stockpiling food",
                        _ => "    missingno",
                    },
                    AntityType.Ant => antity.Action switch {
                        Actions.Idle => "     Idle...",
                        Actions.Job => antity.Job.Type switch {
                            JobType.Gather => "  Gathering Food",
                            _ => "    missingno",
                        },
                        _ => "    missingno",
                    },
                    _ => "    missingno",
                };
                detailLine += "\n\n" + antity.Type switch {
                    AntityType.Anthill => $"    Food: {antity.Value}",
                    _ => "",
                };
            } else {
                detailLine = "-nothing selected-";
            }
            SpriteBatch.DrawString(_font, detailLine, textPos, Color.Gray);

            SpriteBatch.Draw(_pixel, _leftPanelSplitterRect, null, ANT_OLD_AGE);

            textPos.X = 10;
            textPos.Y = 155;
            SpriteBatch.DrawString(_font, "        JOBS        ", textPos, Color.White);
            for (int i = 0; i < _jobs[Team.Player].Count; i++) {
                textPos.Y += 20;
                if (i == _selectedJob)
                    DrawSelection(new Rectangle(5, 170 + (i * 20), _leftPanelRect.Width - 10, 25));

                Job job = _jobs[Team.Player][i];
                string text = job.Type switch {
                    JobType.Gather => "Gather Food",
                    JobType.Distribute => "Distribute Food",
                    _ => $"Unknown: '{job.Type}'",
                };

                SpriteBatch.DrawString(_font, $"{text,-15} x{job.Priority}", textPos, Color.Gray);
            }
        }

    }
}