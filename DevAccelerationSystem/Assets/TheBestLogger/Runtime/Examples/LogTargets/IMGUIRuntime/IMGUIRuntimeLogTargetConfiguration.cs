using System;
using UnityEngine;

namespace TheBestLogger.Examples.LogTargets
{
    [Serializable]
    public sealed class IMGUIRuntimeLogTargetConfiguration : LogTargetConfiguration
    {
        public int CountLogsToPick = 100;
        public KeyCode KeyboardKeyDownToActivateConsole = KeyCode.Space;
        public int CountOfScreenTouchesToActivateConsole = 3;
        public int MaxStringLengthForOneMessage = 100;

        public override void ApplyRuntimeDefaults()
        {
            base.ApplyRuntimeDefaults();
            CountLogsToPick = Math.Max(1, CountLogsToPick);
            CountOfScreenTouchesToActivateConsole = Math.Max(1, CountOfScreenTouchesToActivateConsole);
            MaxStringLengthForOneMessage = Math.Max(1, MaxStringLengthForOneMessage);
        }
    }
}
