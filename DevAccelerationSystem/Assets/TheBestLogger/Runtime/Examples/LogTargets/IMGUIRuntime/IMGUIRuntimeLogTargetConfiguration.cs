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
    }
}
