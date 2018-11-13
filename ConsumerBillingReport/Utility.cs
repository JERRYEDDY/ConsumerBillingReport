using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConsumerBillingReports
{
    public class Utility
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public double DurationToHours(double duration)
        {
            var hours = TimeSpan.Zero;

            try
            {
                hours = TimeSpan.FromSeconds(duration);
            }
            catch (OverflowException ex)
            {
                Logger.Error(ex.Message +  "\n" + ex.GetType());
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex.Message + "\n" + ex.GetType());
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }

            return hours.TotalHours;
        }

        public int DurationToUnits(double duration)
        {
            int units;
            try
            {
                units = (int)duration / 900; //900 seconds in 15 minutes (unit)
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }
            return units;
        }

        public DateTimeOffset FromString(string offsetString)
        {
            if (!DateTimeOffset.TryParse(offsetString, out DateTimeOffset offset))
            {
                offset = DateTimeOffset.Now;
            }
            return offset;
        }

        //public string FormatIso8601(DateTime dt)
        //{
        //    var dto = new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
        //    var formatIso8601 = dto.ToString("yyyy-MM-ddTHH:mm:ssK");
        //    return formatIso8601;
        //}

        //public string FormatIso8601(DateTimeOffset dto)
        //{
        //    var formatIso8601 = dto.ToString("yyyy-MM-ddTHH:mm:ssK");
        //    return formatIso8601;
        //}



        //public double FormatIso8601Duration(DateTimeOffset sDate, DateTimeOffset eDate)
        //{
        //    TimeSpan duration = eDate - sDate;

        //    return duration.TotalSeconds;
        //}

        //public static bool BetweenRanges(double lower, double upper, double number)
        //{
        //    return (lower <= number && number <= upper);
        //}
    }
}