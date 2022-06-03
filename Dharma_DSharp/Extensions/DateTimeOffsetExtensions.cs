namespace Dharma_DSharp.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset RoundToNearestHalfHour(this DateTimeOffset dateTimeOffset)
        {
            int amountToAdd;
            var modulo = dateTimeOffset.Minute % 30;

            if ((dateTimeOffset.Minute < 15) || ((dateTimeOffset.Minute > 30) && (dateTimeOffset.Minute < 45)))
            {
                amountToAdd = -(dateTimeOffset.Minute % 30);
            }
            else
            {
                amountToAdd = modulo != 0 ? 30 - modulo : modulo;
            }

            return dateTimeOffset.AddMinutes(amountToAdd);
        }
    }
}