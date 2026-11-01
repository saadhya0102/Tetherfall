using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private float combatHitstopUntilRealtime;

        public bool CombatHitstopActive =>
            Time.realtimeSinceStartup < combatHitstopUntilRealtime;

        public void RequestCombatHitstop(float duration)
        {
            var scaledDuration = TetherfallAccessibilityRuntime.ScaleHitstopDuration(duration);
            if (scaledDuration <= 0f)
            {
                return;
            }

            scaledDuration = Mathf.Min(0.065f, scaledDuration);
            combatHitstopUntilRealtime = Mathf.Max(
                combatHitstopUntilRealtime,
                Time.realtimeSinceStartup + scaledDuration);
        }
    }
}
