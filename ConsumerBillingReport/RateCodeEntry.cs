using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace PayrollByJobCodeReport

{
    public class RatecodeEntry
    {
        public string RateId { get; set; }
        public double Lower { get; set; }
        public double Upper { get; set; }
        public string WCode { get; set; }
        public double BillRate { get; set; }

        public RatecodeEntry() { }

        public RatecodeEntry(string rateId, double lower, double upper, string wCode, double billRate)
        {
            RateId = rateId;
            Lower = lower;
            Upper = upper;
            WCode = wCode;
            BillRate = billRate;
        }

        public static RatecodeEntry FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');

            RatecodeEntry rateCodeEntry = new RatecodeEntry
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