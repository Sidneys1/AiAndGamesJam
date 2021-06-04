using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace AiAndGamesJam {
    public partial class AntGame {
        private readonly System.Collections.BitArray _antitiesSet = new(MAX_ANTITIES, false);
        private readonly Antity[] _antities = new Antity[MAX_ANTITIES];
        private readonly Dictionary<Team, List<short>> _anthillCache = new() {
            { Team.Player, new() },
            { Team.Fireants, new() },
        };
        private int _rightmostAntity = 0;
        private double _lastAntitiesTrim = 0;
        private int _selectedAntity = -1;

        short AddAntity(AntityType type = AntityType.None, Team team = Team.None, Vector2? position = null, Actions action = 0, double coolDown = 0, int value = 0) {
            short pos = -1;
            for (short i = 0; i < MAX_ANTITIES; i++) {
                if (!_antitiesSet[i]) {
                    pos = i;
                    break;
                }
            }

            if (pos == -1)
                return -1; // All entity slots are taken!

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
            if (type == AntityType.Anthill) _anthillCache[team].Add(pos);
            return pos;
        }

        void RemoveAntity(short pos) {
            if (_antities[pos].Type == AntityType.Anthill)
                _anthillCache[_antities[pos].Team].Remove(pos);
            _antitiesSet[pos] = false;
            _antities[pos].Job = null;
            if (_selectedAntity == pos)
                _selectedAntity = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private short FindNearestAntity(ref Vector2 position, AntityType? type = null, Team? team = null, bool collision = false) {
            short ret = -1;
            float nearest = float.MaxValue;
            IEnumerable<short> range;
            if (type.HasValue && type.Value == AntityType.Anthill)
                range = team.HasValue ? _anthillCache[team.Value].AsEnumerable() : _anthillCache.SelectMany(x => x.Value);
            else range = Enumerable.Range(0, _rightmostAntity).Select(x => (short)x);

            foreach (short i in range) {
                if (!_antitiesSet[i]) continue;
                ref Antity ent = ref _antities[i];
                if ((type.HasValue && ent.Type != type.Value) || (team.HasValue && ent.Team != team.Value)) continue;
                // Manhattan distance
                var distance = System.Math.Abs(ent.Position.X - position.X) + System.Math.Abs(ent.Position.Y - position.Y);

                if (collision) {
                    switch (ent.Type) {
                        case AntityType.Anthill:
                            if (distance > 48) continue;
                            break;
                        case AntityType.Ant:
                            if (distance > 10) continue;
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
        private void SelectNextAntity(AntityType? type = null, Actions? action = null) {
            int next = -1;
            for (int i = 0; i < MAX_ANTITIES; i++) {
                // LOOP
                int actualSelection = _selectedAntity != -1 ? (i + _selectedAntity) % MAX_ANTITIES : i;
                if (!_antitiesSet[actualSelection]) continue;
                ref Antity ent = ref _antities[actualSelection];

                if (actualSelection == _selectedAntity || ent.Team != Team.Player || ent.Type == AntityType.None) continue;

                if (type.HasValue && ent.Type != type.Value) continue;

                if (action.HasValue && ent.Action != action.Value) continue;

                next = actualSelection;
                break;
            }
            _selectedAntity = next;

            if (next != -1)
                _selectedJob = _selectedThing = -1;
        }
    }
}
