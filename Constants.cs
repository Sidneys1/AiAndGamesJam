using Microsoft.Xna.Framework;

namespace AiAndGamesJam {
    public partial class AntGame {
        private const int MAX_ANTITIES = 1_000;
        private const int MAX_THINGS = 1_000;
        private const int ANT_SPEED = 200;

        private readonly Color ANT_MIDDLE_AGE = new(25, 25, 25);
        private readonly Color ANT_OLD_AGE = new(50, 50, 50);
    }

}