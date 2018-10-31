using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace TSheetReports
{
    public class WCodeRate
    {
        public static DataSet Rates()
        {
            DataTable ratesDataTable = new DataTable();
            ratesDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "rate_code",
                DataType = typeof(string)
            });
            ratesDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "last_name",
                DataType = typeof(string)
            });
            ratesDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "dob",
                DataType = typeof(DateTime)
            });
            ratesDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "job_title",
                DataType = typeof(string)
            });
            ratesDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "taken_name",
                DataType = typeof(string)
            });
            ratesDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "is_american",
                DataType = typeof(string)
            });

            //ratesDataTable.Rows.Add(new object[] { "Lenny", "Belardo", new DateTime(1971, 3, 24), "Pontiff", "Pius XIII", "yes" });
            //ratesDataTable.Rows.Add(new object[] { "Angelo", "Voiello", new DateTime(1952, 11, 18), "Cardinal Secretary of State", "", "no" });
            //ratesDataTable.Rows.Add(new object[] { "Michael", "Spencer", new DateTime(1942, 5, 12), "Archbishop of New York", "", "yes" });
            //ratesDataTable.Rows.Add(new object[] { "Sofia", "(Unknown)", new DateTime(1974, 7, 2), "Director of Marketing", "", "no" });
            //ratesDataTable.Rows.Add(new object[] { "Bernardo", "Gutierrez", new DateTime(1966, 9, 16), "Master of Ceremonies", "", "no" });

            DataSet ratesDataSet = new DataSet();
            ratesDataSet.Tables.Add(ratesDataTable);

            return ratesDataSet;
        }
    }
}