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

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            dgvDuplicates.CellValueChanged += dgvDuplicates_CellValueChanged;

        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string folderPath = folderBrowserDialog.SelectedPath;
                FindDuplicateFiles(folderPath);
            }
        }

        private void FindDuplicateFiles(string folderPath)
        {
            Dictionary<string, List<(string filePath, Version version)>> fileMap = new Dictionary<string, List<(string filePath, Version version)>>();

            foreach (var filePath in Directory.GetFiles(folderPath, "*.nsp"))
            {
                string fileName = Path.GetFileName(filePath);

                Match match = Regex.Match(fileName, @"(.*?)[\s\[\(]*(?:UPD|Upd)?[\s\[\(]*v?(\d+\.\d+\.\d+)", RegexOptions.IgnoreCase);

                if (match.Success && Version.TryParse(match.Groups[2].Value, out Version version))
                {
                    string fileKey = Regex.Replace(match.Groups[1].Value, @"[\[\]]", "").Trim().ToLowerInvariant();

                    if (!fileMap.ContainsKey(fileKey))
                    {
                        fileMap[fileKey] = new List<(string filePath, Version version)>();
                    }

                    fileMap[fileKey].Add((filePath, version));
                }
            }

            dgvDuplicates.Rows.Clear();
            dgvDuplicates.Columns.Clear();

            DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
            checkBoxColumn.Name = "Select";
            checkBoxColumn.HeaderText = "Select";
            checkBoxColumn.ReadOnly = false;
            dgvDuplicates.Columns.Add(checkBoxColumn);

            dgvDuplicates.Columns.Add("FileName", "File Name");
            dgvDuplicates.Columns.Add("FilePath", "File Path");

            foreach (var entry in fileMap)
            {
                if (entry.Value.Count > 1)
                {
                    Version maxVersion = entry.Value.Max(x => x.version);
                    foreach (var (filePath, version) in entry.Value)
                    {
                        bool isOldVersion = version.CompareTo(maxVersion) < 0;
                        int rowIndex = dgvDuplicates.Rows.Add(isOldVersion, Path.GetFileName(filePath), filePath);
                        dgvDuplicates.Rows[rowIndex].Cells["Select"].Value = isOldVersion;
                    }
                }
            }
            dgvDuplicates.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            UpdateSelectedCount();
        }





        private void UpdateSelectedCount()
        {
            int selectedCount = dgvDuplicates.Rows.Cast<DataGridViewRow>().Count(row => Convert.ToBoolean(row.Cells["Select"].Value));
            lblSelectedCount.Text = $"Selected: {selectedCount}";
        }

        private void dgvDuplicates_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvDuplicates.Columns["Select"].Index)
            {
                UpdateSelectedCount();
            }
        }


        private void btnDelete_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to delete the selected files?", "Delete Confirmation",
        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                for (int i = dgvDuplicates.Rows.Count - 1; i >= 0; i--)
                {
                    DataGridViewRow row = dgvDuplicates.Rows[i];
                    bool isSelected = Convert.ToBoolean(row.Cells["Select"].Value);

                    if (isSelected)
                    {
                        string filePath = row.Cells["FilePath"].Value.ToString();
                        try
                        {
                            File.Delete(filePath);
                            dgvDuplicates.Rows.RemoveAt(i);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error deleting file: " + filePath + "\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvDuplicates.Rows)
            {
                row.Cells["Select"].Value = false;
            }
        }
    }
}
