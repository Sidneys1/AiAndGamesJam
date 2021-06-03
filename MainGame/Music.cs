using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Media;

namespace AiAndGamesJam {
    public partial class AntGame {
        private readonly Song[] _songs = new Song[4];
        private int _currentSong = 0;

        private void On_SongEnd(object _, System.EventArgs __) {
            if (MediaPlayer.State == MediaState.Stopped)
                NextSong();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NextSong() => MediaPlayer.Play(_songs[_currentSong = ++_currentSong % _songs.Length]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PreviousSong() => MediaPlayer.Play(_songs[_currentSong = ++_currentSong % _songs.Length]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResumeSong() => MediaPlayer.Resume();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PauseSong() => MediaPlayer.Pause();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToggleSong() {
            switch (MediaPlayer.State) {
                case MediaState.Playing: PauseSong(); break;
                case MediaState.Paused: ResumeSong(); break;
                default: System.Diagnostics.Trace.WriteLine($"Song was in unexpected state {MediaPlayer.State}"); break;
            }
        }
    }
}