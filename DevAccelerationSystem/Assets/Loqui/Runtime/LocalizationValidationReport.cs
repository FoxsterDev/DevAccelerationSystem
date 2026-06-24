using System.Collections.Generic;

namespace Loqui
{
    public sealed class LocalizationValidationReport
    {
        public readonly List<string> Errors = new();
        public readonly List<string> Warnings = new();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => Errors.Count == 0;

        public void Error(string message)
        {
            Errors.Add(message);
        }

        public void Warning(string message)
        {
            Warnings.Add(message);
        }
    }
}
