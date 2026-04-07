namespace LocalhostTunnel.Desktop.Utilities;

public static class AppTimeZone
{
    private const string WindowsTimeZoneId = "SE Asia Standard Time";
    private const string IanaTimeZoneId = "Asia/Ho_Chi_Minh";

    private static readonly TimeZoneInfo HoChiMinhTimeZone = ResolveTimeZone();

    public static string DisplayLabel => "GMT+7 (Ho Chi Minh)";

    public static DateTimeOffset ToHoChiMinh(DateTimeOffset value)
    {
        return TimeZoneInfo.ConvertTime(value, HoChiMinhTimeZone);
    }

    public static string Format(DateTimeOffset value, string format)
    {
        return ToHoChiMinh(value).ToString(format);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(IanaTimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "GMT+7",
                    TimeSpan.FromHours(7),
                    "GMT+7",
                    "GMT+7");
            }
        }
    }
}
