using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConsumerBillingReports
{
    public class Utility
    {
        public double DurationToHours(double duration)
        {
            var hours = TimeSpan.FromSeconds(duration);
            //var hoursString = $"{t.TotalHours,0:N2}";
            return hours.TotalHours;
        }

        //public string DurationToHours(double duration)
        //{
        //    var t = TimeSpan.FromSeconds(duration);
        //    var hoursString = $"{t.TotalHours,0:N2}";
        //    return hoursString;
        //}

        public int DurationToUnits(double duration)
        {
            var units = (int)duration / 900; //900 seconds in 15 minutes (unit)
            return units;
        }

        public string FormatIso8601(DateTime dt)
        {
            var dto = new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
            var formatIso8601 = dto.ToString("yyyy-MM-ddTHH:mm:ssK");
            return formatIso8601;
        }

        public string FormatIso8601(DateTimeOffset dto)
        {
            var formatIso8601 = dto.ToString("yyyy-MM-ddTHH:mm:ssK");
            return formatIso8601;
        }

        public  DateTimeOffset FromString(string offsetString)
        {
            if (!DateTimeOffset.TryParse(offsetString, out DateTimeOffset offset))
            {
                offset = DateTimeOffset.Now;
            }
            return offset;
        }

        public double FormatIso8601Duration(DateTimeOffset sDate, DateTimeOffset eDate)
        {
            TimeSpan duration = eDate - sDate;

            return duration.TotalSeconds;
        }

        public static bool BetweenRanges(double lower, double upper, double number)
        {
            return (lower <= number && number <= upper);
        }
    }
}