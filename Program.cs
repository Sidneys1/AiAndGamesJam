using System;

namespace AiAndGamesJam {
    public static class Program {
        [STAThread]
        static void Main() {
            using var game = new AntGame();
            game.Run();
        }
    }
}
