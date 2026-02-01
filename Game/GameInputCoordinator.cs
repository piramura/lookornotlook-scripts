using UnityEngine;
using VContainer.Unity;
using Piramura.LookOrNotLook.Game.State;
namespace Piramura.LookOrNotLook.Game
{
    /// <summary>
    /// 入力を一括管理する（Oculus + PCデバッグ）
    /// フェーズごとの「A/B/X/Y/R」などの役割をここに集約する。
    /// </summary>
    public sealed class GameInputCoordinator : ITickable
    {
        private readonly GameLoop loop;
        private readonly IGameStateService state; // ← 無いなら一旦仮で後述
        private readonly IResultFlow resultFlow;  // ← Resultの操作先（後述）

        public GameInputCoordinator(GameLoop loop, IGameStateService state, IResultFlow resultFlow)
        {
            this.loop = loop;
            this.state = state;
            this.resultFlow = resultFlow;
        }

        public void Tick()
        {
            if (Time.frameCount % 60 == 0)
                Debug.Log($"[Input] tick phase={state.Phase}");
            // ---- 共通：緊急リセット（デバッグ） ----
            if (Input.GetKeyDown(KeyCode.R))
            {
                loop.DebugResetToPlaying(); // ← これを用意
                return;
            }

            // Quest（Oculus Integration）
            bool a = OVRInput.GetDown(OVRInput.Button.One);   // A
            bool b = OVRInput.GetDown(OVRInput.Button.Two);   // B
            bool x = OVRInput.GetDown(OVRInput.Button.Three); // X
            bool y = OVRInput.GetDown(OVRInput.Button.Four);  // Y

            switch (state.Phase)
            {
                case GamePhase.TitleScreen:
                    if (a)
                    {
                        // TODO: StartGame相当（今はResetGameでもOKなら仮で）
                        loop.StartGameFromTitle();
                    }
                    break;

                case GamePhase.Playing:
                    if (a)
                    {
                        // Playing中は
                        // 空でもOK
                    }
                    break;

                case GamePhase.Result:
                    if (a) resultFlow.Retry();
                    if (b) resultFlow.ToggleAchievementOverlay();
                    if (y) resultFlow.GoTitle();
                    break;
                
                case GamePhase.AchievementScreen:
                    // もし overlay を独立Phaseにするならここで閉じる等
                    if (b) resultFlow.ToggleAchievementOverlay();
                    break;
            }
        }
    }
}
