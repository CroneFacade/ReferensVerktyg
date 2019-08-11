using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReferensVerktygAlpha
{
    
    public partial class ReferenceForm : Form
    {
        public List<ReferenceList> Lists { get; set; }
        public ReferenceForm()
        {
            InitializeComponent();
        }

        private void OpenFilesButtonClick(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "CSV files | *.csv"; // file types, that will be allowed to upload
            dialog.Multiselect = true; // allow/deny user to upload more than one file at a time
            if (dialog.ShowDialog() == DialogResult.OK) // if user clicked OK
            {
                Lists = new List<ReferenceList>();
                foreach (var path in dialog.FileNames)
                {
                    var refList = new ReferenceList();
                    refList.FileName = path.Substring(path.LastIndexOf("\\"));
                    using (StreamReader sr = new StreamReader(new FileStream(path, FileMode.Open), new UTF8Encoding())) // do anything you want, e.g. read it
                    {
                        string data = sr.ReadLine();
                        refList.Headers = data.Split(',');
                        refList.Rows = new List<string[]>();

                        while ((data = sr.ReadLine()) != null)
                        {
                            var regx = new Regex(',' + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                            var row = regx.Split(data).ToList();

                            for (int i = 0; i < row.Count; i++)
                            {
                                row[i] = row[i].Replace("\"", "");
                            }
                            row.Add(refList.FileName);
                            refList.Rows.Add(row.ToArray());
                        }
                    }

                    Lists.Add(refList);
                }
                if (Lists.Count > 0)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
            }

            
        }


        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var data = new DataTable();
            var columns = new List<DataColumn>();

            columns.Add(new DataColumn("Author"));
            columns.Add(new DataColumn("Title"));
            columns.Add(new DataColumn("Year"));
            columns.Add(new DataColumn("# matches", Type.GetType("System.Int32")));
            columns.Add(new DataColumn("Files"));
            data.Columns.AddRange(columns.ToArray());

            var totalRows = Lists.SelectMany(x => x.Rows).ToList();

            var foundTitles = new List<string>();

            foreach (var row in totalRows)
            {
                var title = row[2];

                if (foundTitles.Contains(title))
                {
                    continue;
                }
                foundTitles.Add(title);
                var year = row[3];
                var files = totalRows
                        .Where(x => x[2] == title)
                        .Select(m => new { Title = m[2], FileName = m.Last() })
                        .Distinct();
                var author = row[0];
                var dr = data.NewRow();

                dr["Author"] = author;
                dr["Title"] = title;
                dr["Year"] = year;
                dr["# matches"] = files.Count();

                var fileNames = "";

                foreach (var file in files)
                {
                    fileNames += file.FileName + " | ";
                }

                dr["Files"] = fileNames;


                data.Rows.Add(dr);
                
            }

            data.AcceptChanges();
            dataGridView1.BeginInvoke(new Action(() => {
                dataGridView1.DataSource = data;
            }));
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string searchTitle = dataGridView1.SelectedCells[0].Value.ToString();

            var data = new DataTable();
            var columns = new List<DataColumn>();

            columns.Add(new DataColumn("Author"));
            columns.Add(new DataColumn("Title"));
            columns.Add(new DataColumn("Year"));
            columns.Add(new DataColumn("CSV File"));
            data.Columns.AddRange(columns.ToArray());

            var totalRows = new List<string[]>();

            foreach (var refList in Lists)
            {
                totalRows.AddRange((from r in refList.Rows
                                    where r[2] == searchTitle
                                    select r).ToArray());
            }


            foreach (var row in totalRows)
            {
                var title = row[2];

                var year = row[3];
                var author = row[0];

                var dr = data.NewRow();

                dr["Author"] = author;
                dr["Title"] = title;
                dr["Year"] = year;
                dr["CSV File"] = row.Last();

                data.Rows.Add(dr);

            }

            data.AcceptChanges();
            dataGridView2.BeginInvoke(new Action(() => {
                dataGridView2.DataSource = data;
            }));
        }

        private void SaveResultButtonClick(object sender, EventArgs e)
        {
            WriteCSVResult(dataGridView1);
            MessageBox.Show("Sparat!");

        }

        public void WriteCSVResult(DataGridView gridIn)
        {
            //test to see if the DataGridView has any rows
            if (gridIn.RowCount > 0)
            {
                string filename = "";
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CSV (*.csv)|*.csv";
                sfd.FileName = "Output.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(filename))
                    {
                        try
                        {
                            File.Delete(filename);
                        }
                        catch (IOException ex)
                        {
                            MessageBox.Show("Det gick inte att spara." + ex.Message);
                        }
                    }
                    int columnCount = dataGridView1.ColumnCount;
                    string columnNames = "";
                    string[] output = new string[dataGridView1.RowCount + 1];
                    for (int i = 0; i < columnCount; i++)
                    {
                        columnNames += dataGridView1.Columns[i].Name.ToString() + ",";
                    }
                    output[0] += columnNames;
                    for (int i = 1; (i - 1) < dataGridView1.RowCount; i++)
                    {
                        for (int j = 0; j < columnCount; j++)
                        {
                            output[i] += "\"" + dataGridView1.Rows[i - 1].Cells[j].Value.ToString() + "\",";
                        }
                    }
                    System.IO.File.WriteAllLines(sfd.FileName, output, System.Text.Encoding.UTF8);
                }
            }
        }
    }
}
