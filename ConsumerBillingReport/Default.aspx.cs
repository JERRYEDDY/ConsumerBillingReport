using System;
using System.Collections.Generic;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using TSheets;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Web;
using Newtonsoft.Json;
using PayrollByJobCodeReport;
using PBJReport;


namespace ConsumerBillingReports
{
    public partial class _Default : Page
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static string _baseUri = "https://rest.tsheets.com/api/v1";
        private static ConnectionInfo _connection;
        private static IOAuth2 _authProvider;
        private static string _clientId;
        private static string _redirectUri;
        private static string _clientSecret;
        private static string _manualToken;

        protected void Page_Load(object sender, EventArgs e)
        {
            logger.Info("Hello NLog World");

            try
            {
                EnvironmentVariableTarget tgt = EnvironmentVariableTarget.Machine;
                _clientId = Environment.GetEnvironmentVariable("TSHEETS_CLIENTID", tgt);
                _clientSecret = Environment.GetEnvironmentVariable("TSHEETS_CLIENTSECRET", tgt);
                _redirectUri = Environment.GetEnvironmentVariable("TSHEETS_REDIRECTURI", tgt);
                _manualToken = Environment.GetEnvironmentVariable("TSHEETS_MANUALTOKEN", tgt);

                // set up the ConnectionInfo object which tells the API how to connect to the server
                _connection = new ConnectionInfo(_baseUri, _clientId, _redirectUri, _clientSecret);

                AuthenticateWithManualToken();

                //DataTable cbeTable = DataTableGenerator.ConsumerBillingEntries();
                //cbeTable.Rows.Add("Mr., Jones", "1:3F_1: 1C_D", 5.8833333, 23, 32.2374429223744, "W5950", 5.58, 128.34);
                //cbeTable.Rows.Add("Mr., Jones", "1:3F_1: 1C_D", 2.8, 11, 15.3424657534247, "W5950", 5.58, 61.38);
                //cbeTable.Rows.Add("Mr., Jones", "1:1C_1:3F_D", 3.33333333333333, 13, 18.2648401826484, "W5950", 5.58, 72.54);
                //cbeTable.Rows.Add("Mr., Jones", "1:3F_1: 1C_D", 6.23333333333333, 24, 34.1552511415525, "W5950", 5.58, 133.92);

                //var newDt = cbeTable.AsEnumerable()
                //    .GroupBy(r => new
                //        {
                //            ConsumerName = r["ConsumerName"],
                //            Jobcode = r["Jobcode"],
                //            WCode = r["WCode"],
                //            Rate = r["Rate"]
                //        })
                //    .Select(g =>
                //    {
                //        var row = cbeTable.NewRow();
                //        row["ConsumerName"] = g.Key.ConsumerName;
                //        row["Jobcode"] = g.Key.Jobcode;
                //        row["Hours"] = g.Sum(x => x.Field<double>("Hours"));
                //        row["Units"] = g.Sum(x => x.Field<int>("Units"));
                //        row["Ratio"] = g.Sum(x => x.Field<double>("Ratio"));
                //        row["WCode"] = g.Key.WCode;
                //        row["Rate"] = g.Key.Rate;
                //        row["Amount"] = g.Sum(x => x.Field<double>("Amount"));
                //        return row;
                //    })
                //    .CopyToDataTable();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        protected void SubmitButton_OnClick(object sender, EventArgs e)
        {
            Utility ut = new Utility();

            DateTimeOffset sDate = ut.FromString(txtStartDate.Text);
            DateTimeOffset eDate = ut.FromString(txtEndDate.Text);

            ReportDocument crystalReport = new ReportDocument();
            crystalReport.Load(Server.MapPath("CrystalReport3.rpt"));

            DataTable dataTable1 = ConsumerBillingReportSummary(sDate, eDate);

            //Sort datatable
            dataTable1.DefaultView.Sort = "ConsumerName, Jobcode";
            dataTable1 = dataTable1.DefaultView.ToTable();

            DataSet1 ds = new DataSet1();
            ds.Tables.Add(dataTable1);
            crystalReport.SetDataSource(ds.Tables[1]);
            crystalReport.SetParameterValue("Start", txtStartDate.Text);
            crystalReport.SetParameterValue("End", txtEndDate.Text);

            CrystalReportViewer1.ReportSource = crystalReport;
            CrystalReportViewer1.PrintMode = PrintMode.Pdf;
        }

        void AuthenticateWithManualToken()
        {
            _authProvider = new StaticAuthentication(_manualToken);
        }

        void AuthenticateWithBrowser()
        {
            var userAuthProvider = new UserAuthentication(_connection);
            _authProvider = userAuthProvider;

            userAuthProvider.TokenChanged += userAuthProvider_TokenChanged;

            OAuthToken authToken = userAuthProvider.GetToken();

            string savedToken = authToken.ToJson();

            // This can be restored into a UserAuthentication object later to reuse:
            OAuthToken restoredToken = OAuthToken.FromJson(savedToken);
            UserAuthentication restoredAuthProvider = new UserAuthentication(_connection, restoredToken);

            // Now the user will not be prompted when we call GetToken
            OAuthToken cachedToken = restoredAuthProvider.GetToken();
        }

        void userAuthProvider_TokenChanged(object sender, TokenChangedEventArgs e)
        {
            if (e.CurrentToken != null)
            {
                Response.Write("<p>Received new auth token:</p>");
                Response.Write(e.CurrentToken.ToJson());
            }
            else
            {
                Response.Write("<p>Token no longer valid</p>");
            }
        }

        public DataTable ConsumerBillingReportDetail(DateTimeOffset sDate, DateTimeOffset eDate)
        {
            var tsheetsApi = new RestClient(_connection, _authProvider);

            string startDate = sDate.ToString("yyyy-MM-dd");
            string endDate = eDate.ToString("yyyy-MM-dd");
            dynamic reportOptions = new JObject();
            reportOptions.data = new JObject();
            reportOptions.data.start_date = startDate;
            reportOptions.data.end_date = endDate;

            var payrollByJobcodeData = tsheetsApi.GetReport(ReportType.PayrollByJobcode, reportOptions.ToString());
            var payrollByJobcode = PayrollByJobcode.FromJson(payrollByJobcodeData);

            PayrollByJobcode pbj = new PayrollByJobcode();

            DataTable cbeTable = DataTableGenerator.ConsumerBillingEntries();

            Utility utility = new Utility();

            List<RateCodeEntry> rateEntries = OpenRateCodeTableFileAsList(Server.MapPath("~/App_Data/RateCodeTable.csv"), 1);

            var pbjReportObject = JsonConvert.DeserializeObject<PayrollByJobcode>(payrollByJobcodeData);
            foreach (KeyValuePair<string, ByUser> userObject in pbjReportObject.Results.PayrollByJobcodeReport.ByUser)
            {
                if (pbjReportObject.SupplementalData.Users.TryGetValue(userObject.Key, out PBJReport.User user))
                {
                    string consumer = user.FirstName + ", " + user.LastName;

                    long totalHours = 0;
                    foreach (KeyValuePair<string, Total> totals in userObject.Value.Totals)
                    {
                        totalHours += totals.Value.TotalReSeconds;
                    }

                    double communityPercentage = 0.00;
                    foreach (KeyValuePair<string, Total> totals in userObject.Value.Totals)
                    {
                        var jobcodes = pbjReportObject.SupplementalData.Jobcodes;
                        if (jobcodes.TryGetValue(totals.Value.JobcodeId.ToString(), out PBJReport.Jobcode jc))
                        {
                            if (jc.Name[3] == 'C') //Community Jobcode
                            {
                                double ratio = (double)totals.Value.TotalReSeconds / totalHours;
                                double percentage = ratio * 100;
                                communityPercentage = percentage;
                            }
                        }
                    }

                    //string jobcode = null;
                    foreach (KeyValuePair<string, Total> totals in userObject.Value.Totals)
                    {
                        var jobcodes = pbjReportObject.SupplementalData.Jobcodes;
                        if (jobcodes.TryGetValue(totals.Value.JobcodeId.ToString(), out PBJReport.Jobcode jc))
                        {
                            string rateId = jc.Name[10].ToString(); //RateId A,B,C,D,E,F

                            long overSeconds = totals.Value.TotalReSeconds % 900;
                            long roundedSeconds = totals.Value.TotalReSeconds - overSeconds;
                            double roundedHours = utility.DurationToHours(roundedSeconds);

                            double hours = utility.DurationToHours(totals.Value.TotalReSeconds);
                            //double hours = roundedHours;

                            int units = utility.DurationToUnits(totals.Value.TotalReSeconds);
                            //int units = utility.DurationToUnits(roundedSeconds);

                            string logEntry = $"Consumer: {consumer} TotalReSeconds: {totals.Value.TotalReSeconds} overSeconds: {overSeconds} roundedSeconds: {roundedSeconds} roundedHours: {roundedHours} ";

                            logger.Info(logEntry);

                            double ratio = (double)totals.Value.TotalReSeconds / totalHours;
                            double percentage = ratio * 100;

                            RateCodeEntry rateEntry = rateEntries.Find(c => (c.RateId == rateId) && (communityPercentage >= c.Lower) && (communityPercentage <= c.Upper));

                            double amount = units * rateEntry.BillRate;

                            cbeTable.Rows.Add(consumer, jc.Name, hours, units, percentage, rateEntry.WCode, rateEntry.BillRate, amount);
                        }
                    }

                    foreach (KeyValuePair<string, Dictionary<string, Total>> dates in userObject.Value.Dates)
                    {
                        string date = dates.Key;
                        foreach (KeyValuePair<string, Total> dateTotals in dates.Value)
                        {
                            var jobcodes = pbjReportObject.SupplementalData.Jobcodes;
                            if (jobcodes.TryGetValue(dateTotals.Value.JobcodeId.ToString(), out PBJReport.Jobcode jc))
                            {
                                double hours = utility.DurationToHours(dateTotals.Value.TotalReSeconds);
                                int units = utility.DurationToUnits(dateTotals.Value.TotalReSeconds);
                                string logEntry = $"{consumer},{jc.Name},{date},{hours},{units}";

                                logger.Info(logEntry);
                            }
                        }
                    }
                }
            }
            return cbeTable;
        }

        public DataTable ConsumerBillingReportSummary(DateTimeOffset sDate, DateTimeOffset eDate)
        {
            var tsheetsApi = new RestClient(_connection, _authProvider);

            string startDate = sDate.ToString("yyyy-MM-dd");
            string endDate = eDate.ToString("yyyy-MM-dd");
            dynamic reportOptions = new JObject();
            reportOptions.data = new JObject();
            reportOptions.data.start_date = startDate;
            reportOptions.data.end_date = endDate;

            var payrollByJobcodeData = tsheetsApi.GetReport(ReportType.PayrollByJobcode, reportOptions.ToString());
            //var payrollByJobcode = PayrollByJobcode.FromJson(payrollByJobcodeData);

            PayrollByJobcode pbj = new PayrollByJobcode();
            DataTable cbeTable = DataTableGenerator.ConsumerBillingEntries();
            Utility utility = new Utility();

            List<RateCodeEntry> rateEntries = OpenRateCodeTableFileAsList(Server.MapPath("~/App_Data/RateCodeTable.csv"), 1);

            var pbjReportObject = JsonConvert.DeserializeObject<PayrollByJobcode>(payrollByJobcodeData);
            foreach (KeyValuePair<string, ByUser> userObject in pbjReportObject.Results.PayrollByJobcodeReport.ByUser)
            {
                if (pbjReportObject.SupplementalData.Users.TryGetValue(userObject.Key, out PBJReport.User user))
                {
                    string consumer = user.FirstName + ", " + user.LastName;

                    //Total the hours by Employee so we can calculate the Facility/Community ratio
                    long totalHours = 0;
                    foreach (KeyValuePair<string, Total> totals in userObject.Value.Totals)
                    {
                        totalHours += totals.Value.TotalReSeconds;
                    }

                    //Calculate the Community percentage ratio
                    double communityPercentage = 0.00;
                    foreach (KeyValuePair<string, Total> totals in userObject.Value.Totals)
                    {
                        var jobcodes = pbjReportObject.SupplementalData.Jobcodes;
                        if (jobcodes.TryGetValue(totals.Value.JobcodeId.ToString(), out PBJReport.Jobcode jc))
                        {
                            if (jc.Name[0] == 'C') //Community Jobcode
                            {
                                double ratio = (double)totals.Value.TotalReSeconds / totalHours;
                                double percentage = ratio * 100;
                                communityPercentage = percentage;
                            }
                        }
                    }

                    foreach (KeyValuePair<string, Total> totals in userObject.Value.Totals)
                    {
                        var jobcodes = pbjReportObject.SupplementalData.Jobcodes;
                        if (jobcodes.TryGetValue(totals.Value.JobcodeId.ToString(), out PBJReport.Jobcode jc))
                        {
                            string rateId = jc.Name[10].ToString(); //RateId A,B,C,D,E,F

                            long overSeconds = totals.Value.TotalReSeconds % 900;
                            long roundedSeconds = totals.Value.TotalReSeconds - overSeconds;
                            double roundedHours = utility.DurationToHours(roundedSeconds);

                            //double hours = utility.DurationToHours(totals.Value.TotalReSeconds);
                            //int units = utility.DurationToUnits(totals.Value.TotalReSeconds);

                            string logEntry = $"Consumer: {consumer} TotalReSeconds: {totals.Value.TotalReSeconds} overSeconds: {overSeconds} roundedSeconds: {roundedSeconds} roundedHours: {roundedHours} ";
                            logger.Info(logEntry);
                            
                            RateCodeEntry rateEntry = rateEntries.Find(c => (c.RateId == rateId) && (communityPercentage >= c.Lower) && (communityPercentage <= c.Upper));
                            double ratio = (double)totals.Value.TotalReSeconds / totalHours;
                            //double percentage = ratio * 100;
                            //double amount = units * rateEntry.BillRate;

                            //cbeTable.Rows.Add(consumer, jc.Name, sDate.DateTime, hours, units, percentage, rateEntry.WCode, rateEntry.BillRate, amount);
                        }
                    }

                    foreach (KeyValuePair<string, Dictionary<string, Total>> dates in userObject.Value.Dates)
                    {
                        string dateString = dates.Key;
                        DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime);

                        foreach (KeyValuePair<string, Total> dateTotals in dates.Value)
                        {
                            var jobcodes = pbjReportObject.SupplementalData.Jobcodes;
                            if (jobcodes.TryGetValue(dateTotals.Value.JobcodeId.ToString(), out PBJReport.Jobcode jc))
                            {
                                string rateId = jc.Name[10].ToString(); //RateId A,B,C,D,E,F

                                //long overSeconds = dateTotals.Value.TotalReSeconds % 900;
                                //long roundedSeconds = dateTotals.Value.TotalReSeconds - overSeconds;
                                //double roundedHours = utility.DurationToHours(roundedSeconds);

                                double hours = utility.DurationToHours(dateTotals.Value.TotalReSeconds);
                                int units = utility.DurationToUnits(dateTotals.Value.TotalReSeconds);

                                string logEntry = $"{consumer},{jc.Name},{parsedDateTime:MM-dd-yyyy},{hours},{units}";
                                logger.Info(logEntry);

                                RateCodeEntry rateEntry = rateEntries.Find(c => (c.RateId == rateId) && (communityPercentage >= c.Lower) && (communityPercentage <= c.Upper));
                                double ratio = (double)dateTotals.Value.TotalReSeconds / totalHours;
                                double percentage = ratio * 100;
                                double amount = units * rateEntry.BillRate;

                                cbeTable.Rows.Add(consumer, jc.Name, hours, units, percentage, rateEntry.WCode, rateEntry.BillRate, amount);
                            }
                        }
                    }
                }
            }
            return cbeTable;
        }

        //public RateCodeEntry FindRate(string rateId, double percent)
        //{
        //    List<RateCodeEntry> rateEntries = OpenRateCodeTableFileAsList(Server.MapPath("~/App_Data/RateCodeTable.csv"), 1);
        //    RateCodeEntry rateEntry = rateEntries.Find(c => (c.RateId == rateId) && (percent >= c.Lower) && (percent <= c.Upper));
        //    return rateEntry;
        //}

        public List<RateCodeEntry> OpenRateCodeTableFileAsList(string fileName, int firstLineIsHeader)
        {
            List<RateCodeEntry> entries = File.ReadAllLines(fileName)
                .Skip(firstLineIsHeader)
                .Select(v => RateCodeEntry.FromCsv(v))
                .ToList();

            return entries;
        }
    }
}