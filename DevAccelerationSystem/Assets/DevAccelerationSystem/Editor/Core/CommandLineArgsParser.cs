using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DevAccelerationSystem.Core
{
    public class CommandLineArgsParser
    {
        private readonly Dictionary<string, string> _args = new Dictionary<string, string>();
        private string _executableFileName;

        public CommandLineArgsParser()
        {
            var args = Environment.GetCommandLineArgs(); //can be thrown unsupported exception
            Parse(args);
        }

        public CommandLineArgsParser(string line)
        {
            var args = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            Parse(args);
        }

        public CommandLineArgsParser(string[] args)
        {
            Parse(args);
        }

        public bool IsValid { get; protected set; }

        public string this[string key]
        {
            get
            {
                _args.TryGetValue(key, out var value);
                return value;
            }
        }

        private void Parse(string[] args)
        {
            if (args == null || args.Length <= 0) return;

            _executableFileName = args[0].Length > 2 && args[0].IndexOfAny(Path.GetInvalidPathChars()) == -1 && args[0].Contains(Path.DirectorySeparatorChar)
                ? args[0]
                : string.Empty;
            IsValid = !string.IsNullOrEmpty(_executableFileName);

            for (var index = 1; index < args.Length; index++)
            {
                var arg = args[index];
                _args[arg] = string.Empty;
                var value = index + 1 < args.Length
                    ? args[index + 1]
                    : string.Empty;
                if (value.Length > 0 && value[0] != '-') _args[arg] = value;
            }
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            if (_args.ContainsKey(key))
            {
                if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(_args[key], out var flag)) return (T) (object) flag;

                    return defaultValue;
                }

                if (typeof(T) == typeof(int))
                {
                    int.TryParse(_args[key], out var result);
                    return (T) (object) result;
                }

                if (typeof(T) == typeof(string)) return (T) (object) _args[key];

                if (typeof(T).IsEnum)
                {
                    var value = _args[key];
                    if (string.IsNullOrEmpty(value)) return defaultValue;
                    try
                    {
                        var enumValue = Enum.Parse(typeof(T), value, true);
                        return (T) enumValue;
                    }
                    catch (Exception)
                    {
                        return defaultValue;
                    }
                }

                throw new ArgumentException("Unsupported", typeof(T).ToString());
            }

            return defaultValue;
        }

        public override string ToString()
        {
            var str = string.Join(";", _args.Select(i => $"{i.Key}: {i.Value}"));
            return str;
        }
    }
}