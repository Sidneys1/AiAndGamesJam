using System.Linq;

namespace AiAndGamesJam {
    public partial class AntGame {
        private readonly System.Collections.Generic.List<Job> _jobs = new();
        private int _totalWeight = 0;
        private int _selectedJob = -1;

        void AddJob(JobType type, short target, byte priority = 1) {
            var existingJob = _jobs.Where(j => j.Type == type && j.Target == target).FirstOrDefault();
            if (existingJob == null)
                _jobs.Add(new Job() { Type = type, Target = target, Priority = priority });
            else
                existingJob.Priority += priority;
            _totalWeight += priority;
        }

        void RemoveJob(Job job) {
            if (_jobs[_selectedJob] == job)
                _selectedJob = -1;
            if (_jobs.Remove(job))
                _totalWeight -= job.Priority;
        }

        void RemoveJobsFor(short thing) =>
            _jobs.Where(j => j.Target == thing).ToList().ForEach(this.RemoveJob);

        Job SelectRandomJob() {
            int randomNumber = _rand.Next(_totalWeight);
            for (int i = 0; i < _jobs.Count; i++) {
                var job = _jobs[i];
                if (randomNumber < job.Priority)
                    return job;
                randomNumber -= job.Priority;
            }
            return null;
        }
    }
}