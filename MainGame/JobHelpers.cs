using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AiAndGamesJam {
    public partial class AntGame {
        private readonly Dictionary<Team, List<Job>> _jobs = new() {
            { Team.Player, new() },
            { Team.Fireants, new() },
        };
        private int _totalWeight = 0;
        private int _selectedJob = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddJob(JobType type, Team team, short target = -1, byte priority = 1) {
            var existingJob = _jobs[team].Where(j => j.Type == type && j.Target == target).FirstOrDefault();
            if (existingJob == null)
                _jobs[team].Add(new Job() { Type = type, Target = target, Priority = priority });
            else
                existingJob.Priority += priority;
            _totalWeight += priority;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RemoveJob(Job job, Team team) {
            if (_selectedJob != -1 && _jobs[team][_selectedJob] == job)
                _selectedJob = -1;
            if (_selectedJob > _jobs[team].IndexOf(job))
                _selectedJob--;
            if (_jobs[team].Remove(job))
                _totalWeight -= job.Priority;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RemoveJobsForThing(short thing) {
            foreach (var team in _jobs.Keys)
                _jobs[team].Where(j => j.Target == thing).ToList().ForEach(x => RemoveJob(x, team));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RemoveJobsForAntity(short antity) {
            foreach (var team in _jobs.Keys)
                _jobs[team].Where(j => j.Target == antity).ToList().ForEach(x => RemoveJob(x, team));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Job SelectRandomJob(Team team) {
            int randomNumber = _rand.Next(_totalWeight);
            for (int i = 0; i < _jobs[team].Count; i++) {
                var job = _jobs[team][i];
                if (randomNumber < job.Priority)
                    return job;
                randomNumber -= job.Priority;
            }
            return null;
        }
    }
}