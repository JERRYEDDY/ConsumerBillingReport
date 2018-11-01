using System;
using System.Collections.Generic;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using TSheets;
using System.Data;
using System.IO;
using System.Linq;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Web;
using log4net;
using Newtonsoft.Json;
using PayrollByJobCodeReport;
using PBJReport;

namespace TSheetReports
{
    public partial class _Default : Page
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static string _baseUri = "https://rest.tsheets.com/api/v1";

        private static ConnectionInfo _connection;
        private static IOAuth2 _authProvider;

        private static string _clientId;
        private static string _redirectUri;
        private static string _clientSecret;
        private static string _manualToken;

        protected void Page_Load(object sender, EventArgs e)
        {
            log.Info("Hello logging world!");

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
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        protected void SubmitButton_OnClick(object sender, EventArgs e)
        {
            Utility ut = new Utility();

            DateTimeOffset sDate = ut.FromString(txtStartDate.Text);
            DateTimeOffset eDate = ut.FromString(txtEndDate.Text);

            ReportDocument crystalReport = new ReportDocument();
            crystalReport.Load(Server.MapPath("CrystalReport2.rpt"));

            DataTable dataTable1 = ConsumerBillingReportSummary(sDate, eDate);
            
            //Sort datatable
            dataTable1.DefaultView.Sort = "ConsumerName, Jobcode, Date";
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

                            log.Info(logEntry);

                            double ratio = (double)totals.Value.TotalReSeconds / totalHours;
                            double percentage = ratio * 100;

                            RateCodeEntry rateEntry = rateEntries.Find(c => (c.RateId == rateId) && (communityPercentage >= c.Lower) && (communityPercentage <= c.Upper));

                            double amount = units * rateEntry.BillRate;

                            cbeTable.Rows.Add(consumer, jc.Name, sDate.DateTime, hours, units, percentage, rateEntry.WCode, rateEntry.BillRate, amount);
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

                                log.Info(logEntry);
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
                            int units = utility.DurationToUnits(totals.Value.TotalReSeconds);

                            string logEntry = $"Consumer: {consumer} TotalReSeconds: {totals.Value.TotalReSeconds} overSeconds: {overSeconds} roundedSeconds: {roundedSeconds} roundedHours: {roundedHours} ";
                            log.Info(logEntry);
                            
                            RateCodeEntry rateEntry = rateEntries.Find(c => (c.RateId == rateId) && (communityPercentage >= c.Lower) && (communityPercentage <= c.Upper));
                            double ratio = (double)totals.Value.TotalReSeconds / totalHours;
                            double percentage = ratio * 100;
                            double amount = units * rateEntry.BillRate;

                            //cbeTable.Rows.Add(consumer, jc.Name, sDate.DateTime, hours, units, percentage, rateEntry.WCode, rateEntry.BillRate, amount);
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
                                string rateId = jc.Name[10].ToString(); //RateId A,B,C,D,E,F

                                long overSeconds = dateTotals.Value.TotalReSeconds % 900;
                                long roundedSeconds = dateTotals.Value.TotalReSeconds - overSeconds;
                                double roundedHours = utility.DurationToHours(roundedSeconds);

                                double hours = utility.DurationToHours(dateTotals.Value.TotalReSeconds);
                                int units = utility.DurationToUnits(dateTotals.Value.TotalReSeconds);

                                string logEntry = $"{consumer},{jc.Name},{date},{hours},{units}";
                                log.Info(logEntry);

                                RateCodeEntry rateEntry = rateEntries.Find(c => (c.RateId == rateId) && (communityPercentage >= c.Lower) && (communityPercentage <= c.Upper));
                                double ratio = (double)dateTotals.Value.TotalReSeconds / totalHours;
                                double percentage = ratio * 100;
                                double amount = units * rateEntry.BillRate;

                                cbeTable.Rows.Add(consumer, jc.Name, sDate.DateTime, hours, units, percentage, rateEntry.WCode, rateEntry.BillRate, amount);
                            }
                        }
                    }
                }
            }
            return cbeTable;
        }

        public RateCodeEntry FindRate(string rateId, double percent)
        {
            List<RateCodeEntry> rateEntries = OpenRateCodeTableFileAsList(Server.MapPath("~/App_Data/RateCodeTable.csv"), 1);
            RateCodeEntry rateEntry = rateEntries.Find(c => (c.RateId == rateId) && (percent >= c.Lower) && (percent <= c.Upper));
            return rateEntry;
        }

        public List<RateCodeEntry> OpenRateCodeTableFileAsList(string fileName, int firstLineIsHeader)
        {
            List<RateCodeEntry> entries = File.ReadAllLines(fileName)
                .Skip(firstLineIsHeader)
                .Select(v => RateCodeEntry.FromCsv(v))
                .ToList();

            return entries;
        }

        public static DataTable ObjectToData(object o)
        {
            DataTable dt = new DataTable("OutputData");

            DataRow dr = dt.NewRow();
            dt.Rows.Add(dr);

            o.GetType().GetProperties().ToList().ForEach(f =>
            {
                try
                {
                    f.GetValue(o, null);
                    dt.Columns.Add(f.Name, f.PropertyType);
                    dt.Rows[0][f.Name] = f.GetValue(o, null);
                }
                catch { }
            });
            return dt;
        }
    }
}