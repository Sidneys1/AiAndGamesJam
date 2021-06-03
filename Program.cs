using System;

namespace AiAndGamesJam {
    public static class Program {
        [STAThread]
        static void Main() {
            using var game2 = new AntGame();
            game2.Run();
        }
    }
}
