using Microsoft.Xna.Framework;

namespace AiAndGamesJam {
    public partial class AntGame {
        private const short MAX_ANTITIES = short.MaxValue - 1;
        private const short MAX_THINGS = 1_000;
        private const int ANT_SPEED = 200;
        const int TOMMY_SPEED = 100;
        private const string TITLE = "ANTS";
        private const string INTRO =
@"   Curious little creatures, ants.
 Colonies can achieve amazing things.
 But is each individual intelligent?

Today *you* get to control the colony!";
        //                                    |

        private readonly Color ANT_COLOR = new(0, 0, 0);

        private readonly Color FIRE_ANT_COLOR = new(100, 0, 0);
        private readonly Color ANT_MIDDLE_AGE = new(25, 25, 25);
        private readonly Color ANT_OLD_AGE = new(50, 50, 50);
    }

}