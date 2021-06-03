using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AiAndGamesJam {
    public partial class AntGame {
        private Rectangle _leftPanelRect, _leftPanelSplitterRect;
        private Texture2D _selectionPixel, _pixel, _antHill, _ant, _food, _bg;
        private Vector2 _anthillOffset = new(24, 24), _antOffset = new(3, 2), _foodOffset = new(16, 16);
        private SpriteFont _font;

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
            Vector2 textPos = new(15, 10);
            SpriteBatch.DrawString(_font, "      DETAILS       ", textPos, Color.White);
            textPos.X = 20;
            textPos.Y += 20;
            string detailLine1, detailLine2 = "";
            if (isJobSelected) {
                Job job = _jobs[_selectedJob];
                detailLine1 = job.Type switch {
                    JobType.Gather => "  Gather Plants",
                    _ => "    missingno",
                };
            } else if (isThingSelected) {
                ref Thing thing = ref _things[_selectedThing];
                detailLine1 = thing.Type switch {
                    ThingType.Food => $" Food: {100 - thing.Value}% eaten",
                    _ => "    missingno",
                };
            } else if (isAntitySelected) {
                ref Antity antity = ref _antities[_selectedAntity];
                detailLine1 = antity.Type switch {
                    AntityType.Anthill => antity.Action switch {
                        Actions.BuildingAnt => "  Making Ants...",
                        Actions.NewAnts => "Waiting for Food...",
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
                detailLine2 = antity.Type switch {
                    AntityType.Anthill => $"    Food: {antity.Value}",
                    _ => "",
                };
            } else {
                detailLine1 = "-nothing selected-";
            }
            SpriteBatch.DrawString(_font, detailLine1, textPos, Color.Gray);
            textPos.Y += 40;
            SpriteBatch.DrawString(_font, detailLine2, textPos, Color.Gray);

            SpriteBatch.Draw(_pixel, _leftPanelSplitterRect, null, Color.Gray);

            textPos.X = 10;
            textPos.Y = 110;
            SpriteBatch.DrawString(_font, "        JOBS        ", textPos, Color.White);
            for (int i = 0; i < _jobs.Count; i++) {
                textPos.Y += 20;
                if (i == _selectedJob)
                    DrawSelection(new Rectangle(5, 125 + (i * 20), _leftPanelRect.Width - 10, 25));

                Job job = _jobs[i];
                string text = job.Type switch {
                    JobType.Gather => "Gather Food",
                    _ => $"Unknown: '{job.Type}'",
                };

                SpriteBatch.DrawString(_font, $"{text,-15} x{job.Priority}", textPos, Color.Gray);
            }
        }

    }
}