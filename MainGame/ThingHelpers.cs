using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace AiAndGamesJam {
    public partial class AntGame {
        private readonly System.Collections.BitArray _thingsSet = new(MAX_THINGS, false);
        private readonly Thing[] _things = new Thing[MAX_THINGS];
        private int _rightmostThing = 0;
        private double _lastThingsTrim = 0;
        private short _selectedThing = -1;

        void AddThing(ThingType type, Vector2 position, int value = 0) {
            var pos = -1;
            for (int i = 0; i < MAX_THINGS; i++) {
                if (!_thingsSet[i]) {
                    pos = i;
                    break;
                }
            }
            if (pos == -1)
                throw new System.Exception("All thing slots are taken!");

            if (pos >= _rightmostThing) _rightmostThing = pos + 1;

            ref Thing thing = ref _things[pos];

            thing.Type = type;
            thing.Position = position;
            thing.Value = value;

            _thingsSet[pos] = true;
        }

        void RemoveThing(short pos) {
            if (_selectedThing == pos)
                _selectedThing = -1;
            _thingsSet[pos] = false;
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
                var distance = System.Math.Abs(ent.Position.X - position.X) + System.Math.Abs(ent.Position.Y - position.Y);

                if (collision) {
                    switch (ent.Type) {
                        case ThingType.Food:
                            if (distance > 32) continue;
                            break;
                        default:
                            System.Diagnostics.Trace.WriteLine("MISSING COLLISION HANDLER IN FindNearestThing!");
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
        private void SelectNextThing(ThingType? type = null) {
            short next = -1;
            for (short i = 0; i < MAX_THINGS; i++) {
                // LOOP
                short actualSelection = _selectedThing != -1 ? (short)((i + _selectedThing) % MAX_THINGS) : i;
                if (!_thingsSet[actualSelection]) continue;

                ref Thing thing = ref _things[actualSelection];

                if (actualSelection == _selectedThing) continue;

                if (type.HasValue && thing.Type != type.Value) continue;

                next = actualSelection;
                break;
            }
            _selectedThing = next;

            if (next != -1)
                _selectedJob = _selectedAntity = -1;
        }
    }
}