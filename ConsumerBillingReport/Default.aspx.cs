﻿using System;
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
using System.Web.UI.WebControls;

namespace ConsumerBillingReports
{
    public partial class _Default : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static string _baseUri = "https://rest.tsheets.com/api/v1";
        private static ConnectionInfo _connection;
        private static IOAuth2 _authProvider;
        private static string _clientId;
        private static string _redirectUri;
        private static string _clientSecret;
        private static string _manualToken;

        public const int RateIdOffset = 12;
        public const char Community = 'C';
        public const string YearMonthDayFormat = "yyyy-MM-dd";
        public const string RateTableFilename = "~/App_Data/RatecodeTable.csv";
        public const int WeeksOffset = 10;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
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

                    var weeks = GetWeeksList();
                    ddlWeekOf.DataSource = weeks;
                    ddlWeekOf.DataValueField = "Value";
                    ddlWeekOf.DataTextField = "Text";
                    ddlWeekOf.DataBind();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        protected void SubmitButton_OnClick(object sender, EventArgs e)
        {
            try
            {
                Utility ut = new Utility();
                string selectedWeekValue = ddlWeekOf.SelectedValue;
                string[] selectedWeek = selectedWeekValue.Split(',');

                DateTimeOffset sDate = ut.FromString(selectedWeek[0]);
                DateTimeOffset eDate = ut.FromString(selectedWeek[1]);

                ReportDocument crystalReport = new ReportDocument();
                crystalReport.Load(Server.MapPath("CrystalReport3.rpt"));

                DataTable dataTable1 = ConsumerBillingReportSummary(sDate, eDate);

                DataSet1 ds = new DataSet1();
                ds.Tables.Add(dataTable1);

                crystalReport.SetDataSource(ds.Tables[1]);
                crystalReport.SetParameterValue("Start", sDate.ToString("MM/dd/yyyy"));
                crystalReport.SetParameterValue("End", eDate.ToString("MM/dd/yyyy"));

                CrystalReportViewer1.ReportSource = crystalReport;
                CrystalReportViewer1.PrintMode = PrintMode.Pdf;
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex.Message + "\n" + ex.GetType());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
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
                Logger.Error("Token no longer valid");
            }
        }

        public DataTable ConsumerBillingReportSummary(DateTimeOffset sDate, DateTimeOffset eDate)
        {
            DataTable cbeTable = null;
            DataTable groupedTable = null;

            try
            {
                var tsheetsApi = new RestClient(_connection, _authProvider);

                dynamic reportOptions = new JObject();
                reportOptions.data = new JObject();
                reportOptions.data.start_date = sDate.ToString(YearMonthDayFormat); 
                reportOptions.data.end_date = eDate.ToString(YearMonthDayFormat);

                var payrollByJobcodeData = tsheetsApi.GetReport(ReportType.PayrollByJobcode, reportOptions.ToString());
                cbeTable = DataTableGenerator.ConsumerBillingEntries();

                List<RatecodeEntry> rateEntries = OpenRateCodeTableFileAsList(Server.MapPath(RateTableFilename), 1);

                var pbjReportObject = JsonConvert.DeserializeObject<PayrollByJobcode>(payrollByJobcodeData);
                foreach (KeyValuePair<string, ByUser> userObject in pbjReportObject.Results.PayrollByJobcodeReport.ByUser)
                {
                    //Get supplemental data for the User (Employee)
                    if (!pbjReportObject.SupplementalData.Users.TryGetValue(userObject.Key, out PBJReport.User user))
                        continue;

                    string consumer = user.FirstName + ", " + user.LastName;

                    //Total the hours by Employee so we can calculate the Facility/Community ratio
                    long totalHours = 0;
                    foreach (KeyValuePair<string, Total> totals in userObject.Value.Totals)
                    {
                        totalHours += totals.Value.TotalReSeconds;
                    }

                    //Calculate the Community percentage ratio
                    double communityPercentage = 0.00;

                    //Load Jobcodes information
                    var jobcodes = pbjReportObject.SupplementalData.Jobcodes;

                    foreach (KeyValuePair<string, Total> totals in userObject.Value.Totals)
                    {
                        if (jobcodes.TryGetValue(totals.Value.JobcodeId.ToString(), out PBJReport.Jobcode jc))
                        {
                            if (jc.Name[0] == Community) //Community Jobcode
                            {
                                double ratio = (double) totals.Value.TotalReSeconds / totalHours;
                                double percentage = ratio * 100;
                                communityPercentage = percentage;
                            }
                        }
                    }

                    Utility utility = new Utility();
                    foreach (KeyValuePair<string, Dictionary<string, Total>> dates in userObject.Value.Dates)
                    {
                        string dateString = dates.Key;
                        DateTime.TryParseExact(dateString, YearMonthDayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime);

                        foreach (KeyValuePair<string, Total> dateTotals in dates.Value)
                        {
                            if (jobcodes.TryGetValue(dateTotals.Value.JobcodeId.ToString(), out PBJReport.Jobcode jc))
                            {
                                if (jc.Type.CompareTo("regular") == 0) //regular type
                                {
                                    string rateId = jc.Name[RateIdOffset].ToString(); //RateId A,B,C,D,E,F

                                    double hours = utility.DurationToHours(dateTotals.Value.TotalReSeconds);
                                    int units = utility.DurationToUnits(dateTotals.Value.TotalReSeconds);

                                    RatecodeEntry rateEntry = rateEntries.Find(c =>
                                        (c.RateId == rateId) && (communityPercentage >= c.Lower) &&
                                        (communityPercentage <= c.Upper));
                                    double ratio = (double) dateTotals.Value.TotalReSeconds / totalHours;
                                    double percentage = ratio * 100;
                                    double amount = units * rateEntry.BillRate;

                                    int dayCount = 1;
                                    cbeTable.Rows.Add(consumer, jc.Name, hours, units, percentage, rateEntry.WCode,
                                        rateEntry.BillRate, amount);
                                }
                                else if (jc.Type.CompareTo("pto") == 0) //pto type 
                                {
                                    double hours = utility.DurationToHours(dateTotals.Value.TotalPtoSeconds);
                                    int units = 0;
                                    double percentage = 0;
                                    string wCode = "   ";
                                    double billRate = 0;
                                    double amount = 0;

                                    int dayCount = 1;
                                    cbeTable.Rows.Add(consumer, jc.Name, hours, units, percentage, wCode,
                                        billRate, amount);
                                }
                                else  //paid_break, or unpaid_break type
                                {
                                    
                                }
                            }
                        }
                    }
                }

                groupedTable = cbeTable.AsEnumerable()
                    .GroupBy(group => new
                    {
                        ConsumerName = group["ConsumerName"],
                        Jobcode = group["Jobcode"],
                        WCode = group["WCode"],
                        Rate = group["Rate"]
                    })
                    .Select(select =>
                    {
                        var row = cbeTable.NewRow();
                        row["ConsumerName"] = select.Key.ConsumerName;
                        row["Jobcode"] = select.Key.Jobcode;
                        row["Hours"] = select.Sum(x => x.Field<double>("Hours"));
                        row["Units"] = select.Sum(x => x.Field<int>("Units"));
                        row["Ratio"] = select.Sum(x => x.Field<double>("Ratio"));
                        row["WCode"] = select.Key.WCode;
                        row["Rate"] = select.Key.Rate;
                        row["Amount"] = select.Sum(x => x.Field<double>("Amount"));
                        return row;
                    })
                    .CopyToDataTable();

                groupedTable.DefaultView.Sort = "ConsumerName, Jobcode";//Sort datatable
                groupedTable = groupedTable.DefaultView.ToTable();
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex.Message + "\n" + ex.GetType());
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex.Message + "\n" + ex.GetType());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return groupedTable;
        }

        public List<RatecodeEntry> OpenRateCodeTableFileAsList(string fileName, int firstLineIsHeader)
        {
            List<RatecodeEntry> entries;
            try
            {
                entries = File.ReadAllLines(fileName).Skip(firstLineIsHeader).Select(RatecodeEntry.FromCsv).ToList();
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error(ex.Message + "\n" + ex.GetType());
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }

            return entries;
        }
        public List<ListItem> GetWeeksList()
        {
            List<ListItem> weeks = new List<ListItem>();
            DateTime startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            DateTime endDate = startDate.AddDays(6);
            for (int i = 0; i < WeeksOffset; i++)
            {
                weeks.Add(new ListItem($"{startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}",$"{startDate:MM/dd/yyyy},{endDate:MM/dd/yyyy}"));
                startDate = startDate.AddDays(-7);
                endDate = endDate.AddDays(-7);
            }
            return weeks;
        }

    }
}