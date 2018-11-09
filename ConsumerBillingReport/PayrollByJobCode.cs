// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using QuickType;
//
//    var payrollByJobcode = PayrollByJobcode.FromJson(jsonString);

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace PBJReport
{
    public partial class PayrollByJobcode
    {
        [JsonProperty("results")]
        public Results Results { get; set; }

        [JsonProperty("supplemental_data")]
        public SupplementalData SupplementalData { get; set; }
    }

    public partial class Results
    {
        [JsonProperty("filters")]
        public Filters Filters { get; set; }

        [JsonProperty("payroll_by_jobcode_report")]
        public PayrollByJobcodeReport PayrollByJobcodeReport { get; set; }
    }

    public partial class Filters
    {
        [JsonProperty("start_date")]
        public DateTimeOffset StartDate { get; set; }

        [JsonProperty("end_date")]
        public DateTimeOffset EndDate { get; set; }
    }

    public partial class PayrollByJobcodeReport
    {
        [JsonProperty("totals")]
        public Dictionary<string, Total> Totals { get; set; }

        [JsonProperty("by_user")]
        public Dictionary<string, ByUser> ByUser { get; set; }
    }

    public partial class ByUser
    {
        [JsonProperty("totals")]
        public Dictionary<string, Total> Totals { get; set; }

        [JsonProperty("dates")]
        public Dictionary<string, Dictionary<string, Total>> Dates { get; set; }

        [JsonProperty("user_id")]
        public long UserId { get; set; }
    }

    public partial class Total
    {
        [JsonProperty("total_work_seconds")]
        public long TotalWorkSeconds { get; set; }

        [JsonProperty("total_re_seconds")]
        public long TotalReSeconds { get; set; }

        [JsonProperty("total_pto_seconds")]
        public long TotalPtoSeconds { get; set; }

        [JsonProperty("overtime_seconds")]
        public OvertimeSeconds OvertimeSeconds { get; set; }

        [JsonProperty("jobcode_id")]
        public long JobcodeId { get; set; }
    }

    public partial class OvertimeSeconds
    {
        [JsonProperty("1.5")]
        public long The15 { get; set; }

        [JsonProperty("2")]
        public long The2 { get; set; }
    }

    public partial class SupplementalData
    {
        [JsonProperty("jobcodes")]
        public Dictionary<string, Jobcode> Jobcodes { get; set; }

        [JsonProperty("users")]
        public Dictionary<string, User> Users { get; set; }
    }

    public partial class Jobcode
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("parent_id")]
        public long ParentId { get; set; }

        [JsonProperty("assigned_to_all")]
        public bool AssignedToAll { get; set; }

        [JsonProperty("billable")]
        public bool Billable { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("has_children")]
        public bool HasChildren { get; set; }

        [JsonProperty("billable_rate")]
        public long BillableRate { get; set; }

        [JsonProperty("short_code")]
        public string ShortCode { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("last_modified")]
        public DateTimeOffset LastModified { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("filtered_customfielditems")]
        public string FilteredCustomfielditems { get; set; }

        [JsonProperty("required_customfields")]
        public List<object> RequiredCustomfields { get; set; }

        [JsonProperty("locations")]
        public List<object> Locations { get; set; }
    }

    public partial class User
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("employee_number")]
        public long EmployeeNumber { get; set; }

        [JsonProperty("salaried")]
        public bool Salaried { get; set; }

        [JsonProperty("exempt")]
        public bool Exempt { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonProperty("payroll_id")]
        public string PayrollId { get; set; }

        [JsonProperty("hire_date")]
        public string HireDate { get; set; }

        [JsonProperty("term_date")]
        public string TermDate { get; set; }

        [JsonProperty("last_modified")]
        public DateTimeOffset LastModified { get; set; }

        [JsonProperty("last_active")]
        public DateTimeOffset LastActive { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("client_url")]
        public string ClientUrl { get; set; }

        [JsonProperty("company_name")]
        public string CompanyName { get; set; }

        [JsonProperty("profile_image_url")]
        public Uri ProfileImageUrl { get; set; }

        [JsonProperty("mobile_number")]
        public string MobileNumber { get; set; }

        [JsonProperty("pto_balances")]
        public Dictionary<string, long> PtoBalances { get; set; }

        [JsonProperty("submitted_to")]
        public DateTimeOffset SubmittedTo { get; set; }

        [JsonProperty("approved_to")]
        public DateTimeOffset ApprovedTo { get; set; }

        [JsonProperty("manager_of_group_ids")]
        public List<object> ManagerOfGroupIds { get; set; }

        [JsonProperty("require_password_change")]
        public bool RequirePasswordChange { get; set; }

        [JsonProperty("pay_rate")]
        public long PayRate { get; set; }

        [JsonProperty("pay_interval")]
        public string PayInterval { get; set; }

        [JsonProperty("permissions")]
        public Dictionary<string, bool> Permissions { get; set; }

        [JsonProperty("customfields")]
        public string Customfields { get; set; }
    }

    public partial class PayrollByJobcode
    {
        public static PayrollByJobcode FromJson(string json) => JsonConvert.DeserializeObject<PayrollByJobcode>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this PayrollByJobcode self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
