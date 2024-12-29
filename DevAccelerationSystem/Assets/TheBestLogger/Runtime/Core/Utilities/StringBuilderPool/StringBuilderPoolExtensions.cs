namespace TheBestLogger.Core.Utilities
{
    public static class StringBuilderPoolExtensions
    {
        public static PooledStringBuilder GetPooledStringBuilder(this StringBuilderPool pool)
        {
            return new PooledStringBuilder(pool);
        }
    }
}
