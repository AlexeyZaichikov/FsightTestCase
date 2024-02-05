using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FsightTestCase
{
    public partial class Form1 : Form
    {
        private IMainRepository _mainRepository;

        public Form1()
        {
            InitializeComponent();
            label3.Text = "0";
            label5.Text = "0";
            tb_MaxTableCount.Text = "1";
            _mainRepository = new MainRepository();

            try
            {
                TreeNode systemTable = new TreeNode() { Text = "Системные таблицы", Tag = "SYSTEM_TABLE" };
                TreeNode view = new TreeNode() { Text = "Представления", Tag = "VIEW" };
                TreeNode sqlStoredProcedure = new TreeNode() { Text = "Хранимые процедуры", Tag = "SQL_STORED_PROCEDURE" };
                TreeNode serviceQueue = new TreeNode() { Text = "Последовательности", Tag = "SERVICE_QUEUE" };
                TreeNode userTable = new TreeNode() { Text = "Таблицы", Tag = "USER_TABLE" };
                TreeNode internalTable = new TreeNode() { Text = "Внешние таблицы", Tag = "INTERNAL_TABLE" };

                DataSet data = _mainRepository.GetAllObjects();

                foreach (DataRow row in data.Tables[0].Rows)
                {
                    switch(row[6])
                    {
                        case "SYSTEM_TABLE":
                            systemTable.Nodes.Add(new TreeNode(row[0].ToString()));
                            break;
                        case "VIEW":
                            view.Nodes.Add(new TreeNode(row[0].ToString()));
                            break;
                        case "SQL_STORED_PROCEDURE":
                            sqlStoredProcedure.Nodes.Add(new TreeNode(row[0].ToString()));
                            break;
                        case "SERVICE_QUEUE":
                            serviceQueue.Nodes.Add(new TreeNode(row[0].ToString()));
                            break;
                        case "USER_TABLE":
                            userTable.Nodes.Add(new TreeNode(row[0].ToString()));
                            break;
                        case "INTERNAL_TABLE":
                            internalTable.Nodes.Add(new TreeNode(row[0].ToString()));
                            break;
                    }
                }
                
                tv_DataBaseTree.Nodes.Add(systemTable);
                tv_DataBaseTree.Nodes.Add(view);
                tv_DataBaseTree.Nodes.Add(sqlStoredProcedure);
                tv_DataBaseTree.Nodes.Add(serviceQueue);
                tv_DataBaseTree.Nodes.Add(userTable);
                tv_DataBaseTree.Nodes.Add(internalTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = e.Node;
            
            try
            {
                if (node != null && node.Parent != null && node.Parent.Tag.ToString() == "USER_TABLE")
                {
                    DataSet data = _mainRepository.GetTableData(node);
                    dgv_TableView.DataSource = data.Tables[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            label3.Text = "0";
            label5.Text = "0";
            try
            {
                if (tb_MaxTableCount.Text != "")
                {
                    int numCheckedTables = 0;
                    foreach (TreeNode node in tv_DataBaseTree.Nodes)
                    {
                        if (node.Tag.ToString() == "USER_TABLE")
                        {
                            foreach (TreeNode nodeChild in node.Nodes)
                            {
                                if (nodeChild.Checked == true)
                                {
                                    numCheckedTables++;
                                }
                            }
                            break;
                        }
                    }
                    if (numCheckedTables != 0)
                    {
                        int countActiveTasks = 0, countExportSRows = 0;
                        List<string> names = new List<string>();
                        List<Task<int>> listTasks = new List<Task<int>>();
                        SemaphoreSlim semaphore = new SemaphoreSlim(Convert.ToInt32(tb_MaxTableCount.Text));
                        foreach (TreeNode node in tv_DataBaseTree.Nodes)
                        {
                            if (node.Tag.ToString() == "USER_TABLE")
                            {
                                foreach (TreeNode nodeChild in node.Nodes)
                                {
                                    if (nodeChild.Checked == true)
                                    {
                                        listTasks.Add(_mainRepository.ExportCSV(nodeChild.Text.ToString(), semaphore));
                                        countActiveTasks++;
                                        label3.Text = countActiveTasks.ToString();
                                    }
                                }
                                break;
                            }
                        }
                        while (listTasks.Count > 0)
                        {
                            Task<int> completedTask = await Task.WhenAny(listTasks);
                            int result = await completedTask;
                            countActiveTasks--;
                            countExportSRows += result;
                            label3.Text = countActiveTasks.ToString();
                            label5.Text = countExportSRows.ToString();
                            listTasks.Remove(completedTask);
                        }

                        MessageBox.Show("Экспорт завершен!");
                    }
                    else
                    {
                        MessageBox.Show("Не выбрана ни одна таблица!");
                    }
                }
                else
                {
                    MessageBox.Show("Введите ограничение по экспорту/импорту таблиц!");
                }
            }
            catch (Exception ex)
            {
                label3.Text = "0";
                label5.Text = "0";
                MessageBox.Show(ex.Message);
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            label3.Text = "0";
            label5.Text = "0";
            try
            {
                if (tb_MaxTableCount.Text != "")
                {
                    if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                        return;
                    string[] files = openFileDialog1.FileNames;
                    int countActiveTasks = 0, countImportSRows = 0;
                    List<string> names = new List<string>();
                    List<Task<int>> listTasks = new List<Task<int>>();
                    SemaphoreSlim semaphore = new SemaphoreSlim(Convert.ToInt32(tb_MaxTableCount.Text));
                    foreach (string file in files)
                    {
                        listTasks.Add(_mainRepository.ImportCSV(file, semaphore));
                        countActiveTasks++;
                        label3.Text = countActiveTasks.ToString();
                    }
                    while (listTasks.Count > 0)
                    {
                        Task<int> completedTask = await Task.WhenAny(listTasks);
                        int result = await completedTask;
                        countActiveTasks--;
                        countImportSRows += result;
                        label3.Text = countActiveTasks.ToString();
                        label5.Text = countImportSRows.ToString();
                        listTasks.Remove(completedTask);
                    }
                    MessageBox.Show("Импорт завершен!");
                }
                else
                {
                    MessageBox.Show("Введите ограничение по экспорту/импорту таблиц!");
                }
            }
            catch (Exception ex)
            {
                label3.Text = "0";
                label5.Text = "0";
                MessageBox.Show(ex.Message);
            }
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            TreeNode currentNode = e.Node;

            if (currentNode.Level == 0 && currentNode.Checked == true)
            {
                foreach (TreeNode node in currentNode.Nodes)
                {
                    node.Checked = true;
                }
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if (!Char.IsDigit(number) && number != 8)
            {
                e.Handled = true;
            }
        }
    }
}
