using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace PayrollByJobCodeReport

{
    public class RateCodeEntry
    {
        public string RateId { get; set; }
        public double Lower { get; set; }
        public double Upper { get; set; }
        public string WCode { get; set; }
        public double BillRate { get; set; }

        public RateCodeEntry() { }

        public RateCodeEntry(string rateId, double lower, double upper, string wcode, double billRate)
        {
            RateId = rateId;
            Lower = lower;
            Upper = upper;
            WCode = wcode;
            BillRate = billRate;
        }

        public static RateCodeEntry FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');

            RateCodeEntry rateCodeEntry = new RateCodeEntry
            {
                RateId = values[0],
                Lower = Convert.ToDouble(values[1]),
                Upper = Convert.ToDouble(values[2]),
                WCode = values[3],
                BillRate = Convert.ToDouble(values[4])
            };
            return rateCodeEntry;
        }
    }
}