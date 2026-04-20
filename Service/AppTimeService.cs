using BE_API.Common;
using BE_API.Configuration;
using Microsoft.Extensions.Options;

namespace BE_API.Service
{
    public class AppTimeService : IAppTime
    {
        private readonly TimeZoneInfo _tz;

        public AppTimeService(IOptions<AppTimeSettings> options)
        {
            _tz = ResolveTimeZone(options.Value.TimeZoneId);
            TimeZone = _tz;
        }

        public TimeZoneInfo TimeZone { get; }

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime TodayDate => GetCalendarDateForUtc(UtcNow);

        public DateOnly Today => DateOnly.FromDateTime(TodayDate);

        public TimeSpan GetTimeOfDay()
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(UtcNow, DateTimeKind.Utc), _tz);
            return local.TimeOfDay;
        }

        public DateTime GetCalendarDateForUtc(DateTime utc)
        {
            var utcKind = utc.Kind switch
            {
                DateTimeKind.Utc => utc,
                DateTimeKind.Local => utc.ToUniversalTime(),
                _ => DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            };
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcKind, _tz);
            return local.Date;
        }

        public DateTime GetRideCalendarDate(DateTime? rideDate)
        {
            if (!rideDate.HasValue)
                return TodayDate;

            var d = rideDate.Value;
            return d.Kind switch
            {
                DateTimeKind.Utc => GetCalendarDateForUtc(d),
                DateTimeKind.Local => TimeZoneInfo.ConvertTime(d, _tz).Date,
                _ => d.Date
            };
        }

        public DateTime NormalizeToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static TimeZoneInfo ResolveTimeZone(string? configuredId)
        {
            foreach (var id in CandidateIds(configuredId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Utc;
        }

        private static IEnumerable<string> CandidateIds(string? configuredId)
        {
            if (!string.IsNullOrWhiteSpace(configuredId))
                yield return configuredId.Trim();

            yield return "Asia/Ho_Chi_Minh";
            yield return "SE Asia Standard Time";
        }
    }
}
