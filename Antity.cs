using Microsoft.Xna.Framework;

namespace AiAndGamesJam {
    enum Actions : byte {
        None,
        BuildingAnt,
        NewAnts,
        Idle,
        Gather,
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
        public bool TargetType;
        public short Target;
        public int Value;

        public static string[] AnthillActions = new[] {
            null,
            "New Ant in ",
            null,
        };
    }
}
