using UnityEngine;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Session;

namespace Piramura.LookOrNotLook.Audio
{
    public sealed class SfxService : ISfxService
    {
        private readonly AudioSource source;
        private readonly AudioClip collect, penalty, reset, timeUp, result;

        private readonly IGameSession session;
        private readonly ITimerService timer;

        public SfxService(
            AudioSource source,
            AudioClip collect, AudioClip penalty, AudioClip reset, AudioClip timeUp, AudioClip result,
            IGameSession session,
            ITimerService timer)
        {
            this.source = source;
            this.collect = collect;
            this.penalty = penalty;
            this.reset = reset;
            this.timeUp = timeUp;
            this.result = result;

            this.session = session;
            this.timer = timer;
        }

        public void PlayCollect() => Play(collect, allowAfterTimeUp: false);
        public void PlayPenalty() => Play(penalty, allowAfterTimeUp: false);
        public void PlayReset()  => Play(reset,  allowAfterTimeUp: true);
        public void PlayTimeUp() => Play(timeUp, allowAfterTimeUp: true);
        public void PlayResult() => Play(result, allowAfterTimeUp: true);
        public void StopAll()
        {
            if (source == null) return;
            source.Stop();
        }

        private void Play(AudioClip clip, bool allowAfterTimeUp)
        {
            if (source == null || clip == null) return;

            // ★ここが最重要：プレイ中でないなら鳴らさない
            if (session != null && !session.IsAlive) return;

            // ★収集音は時間切れ後に絶対鳴らさない（仕様：無効）
            if (!allowAfterTimeUp && timer != null && timer.IsTimeUp) return;

            source.PlayOneShot(clip);
        }
    }
}
