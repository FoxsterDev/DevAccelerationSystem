namespace TheBestLogger
{
    public enum LogImportance
    {
        //will wait a regular processing
        NiceToHave = 0,
        //will be scheduled to log target as soon as possible
        Important = 1,
        //will be pushed immediately to logtargets
        Critical = 2
    }
}
