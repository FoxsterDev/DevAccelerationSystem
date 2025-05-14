namespace TheBestLogger.Examples
{
    public static class GameLogger
    {
        public static ILogger Main => LogManager.CreateLogger(nameof(Main));
        public static ILogger GameLoading => LogManager.CreateLogger(nameof(GameLoading));
        public static ILogger UI => LogManager.CreateLogger(nameof(UI), "FeatureController");
        public static ILogger Stability => LogManager.CreateLogger(nameof(Stability));
    }
}
