using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace TSheetReports
{
    public class DataTableGenerator
    {
        public static DataTable ConsumerBillingEntries()
        {
            DataTable cbeDataTable = new DataTable();
            cbeDataTable.TableName = "ConsumerBillingEntries";
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "ConsumerName",
                DataType = typeof(string)
            });
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "Jobcode",
                DataType = typeof(string)
            });
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "Date",
                DataType = typeof(DateTime)
            });
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "Hours",
                DataType = typeof(double)
            });
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "Units",
                DataType = typeof(int)
            });
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "Ratio%",
                DataType = typeof(double)
            });
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "WCode",
                DataType = typeof(string)
            });
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "Rate",
                DataType = typeof(double)
            });
            cbeDataTable.Columns.Add(new DataColumn()
            {
                ColumnName = "Amount",
                DataType = typeof(double)
            });

            return cbeDataTable;
        }
    }
}