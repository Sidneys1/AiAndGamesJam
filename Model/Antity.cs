using Microsoft.Xna.Framework;

namespace AiAndGamesJam {
    enum Actions : byte {
        None,
        BuildingAnt,
        NewAnts,
        Idle,
        Job,
        Stockpile,
    }

    enum AntityType : byte {
        None,
        Anthill,
        Ant,
    }

    enum Team : byte {
        None,
        Player,
        AI
    }

    struct Antity {
        public AntityType Type;
        public Team Team;
        public Vector2 Position;
        public Actions Action;
        public double CoolDown;
        public double Age;
        public Job Job;
        public short TargetAntity;
        public int Value;

        public static string[] AnthillActions = new[] {
            null,
            "New Ant in ",
            null,
            null,
            null,
            null,
        };
    }
}
