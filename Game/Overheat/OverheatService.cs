using System;

namespace Piramura.LookOrNotLook.Game.Overheat
{
    public sealed class OverheatService : IOverheatService
    {
        // まずはコード直書きでOK（最後にConfigへ）
        private const float BaseChance = 0.05f;
        private const float StepPerCombo = 0.02f;
        private const float MaxChance = 0.35f;

        public int Combo { get; private set; }

        public float ForbiddenChance01 =>
            Math.Clamp(BaseChance + StepPerCombo * Combo, 0f, MaxChance);

        public event Action<int, float> Changed;

        public void Reset()
        {
            Combo = 0;
            Changed?.Invoke(Combo, ForbiddenChance01);
        }

        public void OnCollect(bool isForbidden)
        {
            if (isForbidden) Combo = 0;
            else Combo++;

            Changed?.Invoke(Combo, ForbiddenChance01);
        }
    }
}
