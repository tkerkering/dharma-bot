namespace Dharma_DSharp.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset RoundUpToNearest30(this DateTimeOffset datetimeoffset)
        {
            double atMinuteInBlock = datetimeoffset.Minute % 30;
            double minutesToAdd = 30 - atMinuteInBlock;
            return datetimeoffset.AddMinutes(minutesToAdd);
        }
    }
}