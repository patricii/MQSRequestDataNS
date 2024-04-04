using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace MQSRequestData
{
    class SQLManager
    {
        public static string connectionString = "server=localhost;userid=developer;password=xxxxxx;database=TestCapacityControlappdb";
        public void ConvertCSVtoDataTable(string strFilePath)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(strFilePath))
                {
                    string[] headers = sr.ReadLine().Split(',');
                    foreach (string header in headers)
                    {
                        dataTable.Columns.Add(header);
                    }
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');
                        DataRow dataRow = dataTable.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dataRow[i] = rows[i];
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error to convert csv to Dt - reason: " + ex);
            }
            InsertDataIntoSQLServerUsingSQLBulkCopy(dataTable);

        }
        private void InsertDataIntoSQLServerUsingSQLBulkCopy(DataTable csvFileData)
        {
            try
            {
                using (SqlConnection dbConnection = new SqlConnection(connectionString))
                {
                    dbConnection.Open();
                    using (SqlBulkCopy s = new SqlBulkCopy(dbConnection))
                    {
                        s.DestinationTableName = "dailymqsdata";
                        foreach (var column in csvFileData.Columns)
                            s.ColumnMappings.Add(column.ToString(), column.ToString());
                        s.WriteToServer(csvFileData);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error to insert Data on DB - reason : " + ex);
            }
        }
    }
}
