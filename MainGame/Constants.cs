using Microsoft.Xna.Framework;

namespace AiAndGamesJam {
    public partial class AntGame {
        private const short MAX_ANTITIES = 1_000;
        private const short MAX_THINGS = 1_000;
        private const int ANT_SPEED = 200;
        private const string TITLE = "ANTS";
        private const string INTRO =
@"        Pesky little creatures, ants.
You never really know how they make decisions.
 Well, today *you* get to make the decisions!";

        private readonly Color ANT_MIDDLE_AGE = new(25, 25, 25);
        private readonly Color ANT_OLD_AGE = new(50, 50, 50);
    }

}