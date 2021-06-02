using Microsoft.Xna.Framework;

namespace AiAndGamesJam {
    public enum ThingType : byte {
        Food,
    }

    public struct Thing {
        public ThingType Type;
        public Vector2 Position;
        public int Value;
    }
}
