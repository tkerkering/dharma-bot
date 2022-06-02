namespace Dharma_DSharp.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset RoundToNearestHalfHour(this DateTimeOffset dateTimeOffset)
        {
            int amountToAdd;

            if ((dateTimeOffset.Minute <= 15) || ((dateTimeOffset.Minute > 30) && (dateTimeOffset.Minute <= 45)))
            {
                amountToAdd = -(dateTimeOffset.Minute % 30);
            }
            else
            {
                amountToAdd = dateTimeOffset.Minute % 30;
            }

            return dateTimeOffset.AddMinutes(amountToAdd);
        }
    }
}