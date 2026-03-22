using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SpreadsheetApp
{
    public partial class Form1 : Form
    {
        private SharableSpreadSheet sheet;

        public Form1()
        {
            InitializeComponent();

            // יצירת גיליון עם 5 שורות ו־5 עמודות
            sheet = new SharableSpreadSheet(5, 5);
            sheet.sheetSetUp(); // מילוי תאים בערכי ברירת מחדל
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MessageBox.Show("Form1_Load התחיל");
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            var data = sheet.ToList();

            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            if (data.Count == 0)
                return;

            int cols = data[0].Count;


            for (int col = 0; col < cols; col++)
                dataGridView1.Columns.Add($"col{col}", $"Column {col}");


            foreach (var row in data)
            {
                dataGridView1.Rows.Add(row.ToArray());
            }
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            int row = e.RowIndex;
            int col = e.ColumnIndex;

            if (row >= 0 && col >= 0)
            {
                string value = dataGridView1[col, row].Value?.ToString() ?? "";
                sheet.setCell(row, col, value); // עדכון בתשתית שלך
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Spreadsheet Files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                sheet.load(openFileDialog.FileName);
                RefreshGrid();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Spreadsheet Files (*.txt)|*.txt|All files (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                sheet.save(saveFileDialog.FileName);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void button2_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
