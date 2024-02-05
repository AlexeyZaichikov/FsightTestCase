using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace FsightTestCase
{
    public class MainRepository : IMainRepository
    {
        public SqlConnection GetConnection()
        {
            return new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=auto_registration;Trusted_Connection=True;");
        }

        public DataSet GetAllObjects()
        {
            using(SqlConnection connection = GetConnection())
            {
                connection.Open();
                DataSet data = new DataSet();
                SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM sys.objects", connection);
                dataAdapter.Fill(data);
                connection.Close();
                return data;
            }
        }

        public DataSet GetTableData(TreeNode node)
        {
            using (SqlConnection connection = GetConnection())
            {
                connection.Open();
                DataSet data = new DataSet();
                SqlDataAdapter dataAdapter = new SqlDataAdapter(string.Format("SELECT * FROM {0}", node.Text), connection);
                dataAdapter.Fill(data);
                connection.Close();
                return data;
            }
        }

        public async Task<int> ExportCSV(string tableName)
        {
            Random random = new Random();
            await Task.Delay(random.Next(10000));
            int rowsCount = 0;
            using (SqlConnection connection = GetConnection())
            {
                connection.Open();
                DataSet data = new DataSet();
                SqlDataAdapter dataAdapter = new SqlDataAdapter(string.Format("SELECT * FROM {0}", tableName), connection);
                dataAdapter.Fill(data);
                connection.Close();
                DataTable dataTable = data.Tables[0];
                rowsCount = dataTable.Rows.Count;
                if (rowsCount > 0)
                {
                    using (StreamWriter sw = new StreamWriter(string.Format(@"C:\{0}.csv", tableName), false))
                    {
                        int numColumns = dataTable.Columns.Count;

                        for (int i = 0; i < numColumns; i++)
                        {
                            sw.Write(dataTable.Columns[i]);
                            if (i < numColumns - 1)
                            {
                                sw.Write("\t");
                            }
                        }
                        sw.Write(sw.NewLine);

                        foreach (DataRow row in dataTable.Rows)
                        {
                            for (int i = 0; i < numColumns; i++)
                            {
                                {
                                    sw.Write(row[i].ToString());
                                    sw.Write("\t");
                                }
                            }
                            sw.Write(sw.NewLine);
                        }
                        sw.Close();
                    }
                }
            }
            return rowsCount;
        }

        public async Task<int> ImportCSV(string filePath)
        {
            Random random = new Random();
            await Task.Delay(random.Next(10000));
            int isExist = 0, rowsCount = 0;
            using (SqlConnection connection = GetConnection())
            {
                connection.Open();
                SqlCommand command = new SqlCommand(string.Format("SELECT count(*) FROM sys.objects WHERE name = '{0}'", Path.GetFileNameWithoutExtension(filePath)), connection);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        isExist = reader.GetInt32(0);
                    }
                }
                reader.Close();

                if (isExist == 1)
                {
                    command = new SqlCommand(string.Format("DROP TABLE {0}", Path.GetFileNameWithoutExtension(filePath)), connection);
                    command.ExecuteNonQuery();
                    rowsCount = CreateTable(connection, filePath, Path.GetFileNameWithoutExtension(filePath));
                }
                else
                {
                    rowsCount = CreateTable(connection, filePath, Path.GetFileNameWithoutExtension(filePath));
                }
                connection.Close();
            }
            return rowsCount;
        }

        public int CreateTable(SqlConnection connection, string filePath, string tableName)
        {
            int rowsCount = 0;
            string insertQuery = string.Format("CREATE TABLE {0} (", tableName);
            using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8))
            {
                DataTable dataTable = new DataTable();
                string[] headers = sr.ReadLine().Split('\t');
                foreach (string header in headers)
                {
                    insertQuery += header + " NVARCHAR(50) NOT NULL, ";
                    dataTable.Columns.Add(header);
                }
                insertQuery += ")";
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split('\t');
                    DataRow dr = dataTable.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dataTable.Rows.Add(dr);
                }
                foreach (DataRow d in dataTable.Rows)
                {
                    rowsCount++;
                    var cells = d.ItemArray;
                    foreach (var cell in cells)
                    {
                        System.Console.Write(cell + "\t");
                    }
                    System.Console.WriteLine();
                }

                System.Console.WriteLine(insertQuery);

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.ExecuteNonQuery();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = Path.GetFileNameWithoutExtension(filePath);
                    bulkCopy.WriteToServer(dataTable);
                }
                sr.Close();
            };
            return rowsCount;
        }
    }
}
