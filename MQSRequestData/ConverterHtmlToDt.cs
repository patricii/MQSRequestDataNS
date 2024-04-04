using HtmlAgilityPack;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace MQSRequestData
{
    class ConverterHtmlToDt
    {
        public void htmlToCsv(string htmlCode)
        {
            htmlCode = htmlCode.Replace(",", "");
            try
            {
                ApkMQS apk = new ApkMQS();
                HtmlDocument doc = new HtmlDocument();
                DataTable dataTable = new DataTable();

                doc.LoadHtml(htmlCode);
                var headers = doc.DocumentNode.SelectNodes("//tr/th");
                foreach (HtmlNode header in headers)
                {
                    dataTable.Columns.Add(header.InnerText);
                }

                foreach (var row in doc.DocumentNode.SelectNodes("//tr[td]"))
                {
                    dataTable.Rows.Add(row.SelectNodes("td").Select(td => td.InnerText).ToArray());
                }

                StringBuilder sb = new StringBuilder();
                string[] columnNames = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
                sb.AppendLine(string.Join(",", columnNames));
                string itemSubst = string.Empty;

                foreach (DataRow row in dataTable.Rows)
                {                                     
                    if (!row.ItemArray[2].ToString().Contains("***Total***"))
                    {
                        string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
                        sb.AppendLine(string.Join(",", fields));
                    }

                }

                File.WriteAllText(apk.textBoxSave.Text + @"\DailyMQSData.csv", sb.ToString());
            }
            catch (Exception ex)
            {
                string errorMessaeg = ex.ToString();
            }

        }

    }
}
