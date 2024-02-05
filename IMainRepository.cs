using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FsightTestCase
{
    public interface IMainRepository
    {
        SqlConnection GetConnection();
        DataSet GetAllObjects();
        DataSet GetTableData(TreeNode node);
        Task<int> ExportCSV(string tableName, SemaphoreSlim semaphore);
        Task<int> ImportCSV(string filePath, SemaphoreSlim semaphore);
        int CreateTable(SqlConnection connection, string filePath, string tableName);
    }
}
