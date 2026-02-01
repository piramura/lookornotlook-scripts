using System;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Game.Timer
{
    public sealed class TimerService : ITimerService, IStartable, ITickable
    {
        public float DurationSeconds { get; } = 60f;//今はデバッグで10秒ならそのままでOK

        public float RemainingSeconds { get; private set; }
        public bool IsTimeUp => RemainingSeconds <= 0f;

        public event Action<float> OnRemainingChanged;
        public event Action OnTimeUp;

        private bool firedTimeUp;
        private bool running; // ★追加

        public void Start()
        {
            UnityEngine.Debug.Log("[Timer] Start");
            Reset();
            StopAll(); // ★起動直後は止める（TitleScreen前提）
        }
        public void StartTimer()
        {
            if (firedTimeUp) return;
            running = true;
            OnRemainingChanged?.Invoke(RemainingSeconds);
        }

        public void StopAll()
        {
            // 一時停止（次のStartTimerで再開可能）
            running = false;
        }

        public void Tick()
        {
            if (!running) return; // ★追加
            if (firedTimeUp) return;
            // 60フレームに1回だけ表示（ログ爆発防止）
            if (UnityEngine.Time.frameCount % 60 == 0)
                UnityEngine.Debug.Log($"[Timer] Tick rem={RemainingSeconds:F2}");

            RemainingSeconds -= UnityEngine.Time.deltaTime;
            if (RemainingSeconds < 0f) RemainingSeconds = 0f;

            OnRemainingChanged?.Invoke(RemainingSeconds);

            if (!firedTimeUp && IsTimeUp)
            {
                firedTimeUp = true;
                running = false; // 時間切れで停止
                UnityEngine.Debug.Log("[Timer] TimeUp fired");
                OnTimeUp?.Invoke();
            }
        }

        public void Reset()
        {
            RemainingSeconds = DurationSeconds;
            firedTimeUp = false;
            OnRemainingChanged?.Invoke(RemainingSeconds);
        }
    }
}
