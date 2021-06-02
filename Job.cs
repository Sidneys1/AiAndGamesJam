namespace AiAndGamesJam {
    public enum JobType : byte {
        Gather,
    }

    public class Job {
        public JobType Type;
        public byte Priority;
        public short Target;
    }
}
