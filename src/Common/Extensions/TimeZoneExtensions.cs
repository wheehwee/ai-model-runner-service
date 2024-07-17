namespace Common.Extensions
{
    public static class TimeZoneExtensions
    {
        public static TimeZoneInfo ToTimeZoneInfo(this string timeZoneId)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return timeZone;
            }
            catch
            {
                TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsTimeZoneId);
                if (windowsTimeZoneId == null)
                    throw new ArgumentException(
                        $"Time Zone IANA {timeZoneId} or WINDOWS {windowsTimeZoneId} does not exist");

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
                return timeZone;
            }
        }

        public static DateTimeOffset GetVietNamNow()
        {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo());
        }
        
        public static DateTimeOffset GetVietNamWithSpecificTime(int hours = 0, int minutes = 0, int seconds = 0)
        {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo()).Date.AddHours(hours)
                .AddMinutes(minutes)
                .AddSeconds(seconds);
        }
    }
}