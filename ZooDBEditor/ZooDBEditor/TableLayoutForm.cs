using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZooDBEditor
{
    public partial class TableLayoutForm : Form
    {
        private SqlConnection conn;
        private string currTable;
        private DataSet ds;
        private List<Form> forms;
        private SqlDataAdapter da;
        public TableLayoutForm(SqlConnection conn_, string table, bool IsWriter)
        {
            InitializeComponent();

            this.Text = table;

            conn = conn_;
            currTable = table;
            forms = new List<Form>();

            da = new SqlDataAdapter("SELECT * FROM " + table, conn);
            ds = new DataSet();
            da.Fill(ds, table);

            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = table;

            if(!IsWriter)
            {
                addToolStripMenuItem.Visible = false;
                editToolStripMenuItem.Visible = false;
                deleteToolStripMenuItem.Visible = false;
            }

            ToolStripButton b = new ToolStripButton();
            b.Text = "Print settings";
            b.DisplayStyle = ToolStripItemDisplayStyle.Text;
            b.Click += printPreview_PrintClick;
            ((ToolStrip)(printPreviewDialog1.Controls[1])).Items.RemoveAt(0);
            ((ToolStrip)(printPreviewDialog1.Controls[1])).Items.Insert(0, b);
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RecordLayoutForm form = new RecordLayoutForm(false, null, ds, conn);
            form.Show();
            forms.Add(form);
        }

        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RecordLayoutForm form = new RecordLayoutForm(true, null, ds, conn);
            form.Show();
            forms.Add(form);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("You must select one row", "Error");
                return;
            }
            string id = dataGridView1.SelectedRows[0].Cells[idCol(currTable)].Value.ToString();
            int id_ = 0;
            if(!int.TryParse(id, out id_))
            {
                id = "'" + id + "'";
            }

            try
            {
                SqlCommand cmd = new SqlCommand("DELETE FROM " + currTable + " WHERE " + idCol(currTable) + "=" + id, conn);
                cmd.ExecuteNonQuery();

                da = new SqlDataAdapter("SELECT * FROM " + currTable, conn);
                ds.Clear();
                da.Fill(ds, currTable);
            } catch(Exception ex)
            {
                MessageBox.Show("Check for column '" + idCol(currTable) + "' reference in other tables");
            }
        }

        public static string idCol(string table)
        {
            switch(table)
            {
                case "complex": case "species": return "name";
                case "worker": return "id";
                case "building": return "number";
                default: return "";
            }
        }

        private void TableLayoutForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach(var form in forms)
            {
                form.Close();
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("You must select one row", "Error");
                return;
            }
            RecordLayoutForm form = new RecordLayoutForm(false, dataGridView1.SelectedRows[0], ds, conn);
            form.Show();
            forms.Add(form);
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            printDocument1.DocumentName = currTable;
            printPreviewDialog1.Document = printDocument1;
            printPreviewDialog1.ShowDialog();
        }

        private void printPreview_PrintClick(object sender, EventArgs e)
        {
            try
            {
                printDialog1.Document = printDocument1;
                if (printDialog1.ShowDialog() == DialogResult.OK)
                {
                    printDocument1.PrinterSettings = printDialog1.PrinterSettings;

                    printDocument1.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ToString());
            }
        }

        private List<int> arrColumnLefts = new List<int>(), arrColumnWidths = new List<int>();
        private int iCellHeight, iRow, iTotalWidth, iHeaderHeight, totalrecord;
        private bool bFirstPage, bNewPage;
        private StringFormat strFormat;

        private void printDocument1_BeginPrint(object sender, PrintEventArgs e)
        {
            try
            {
                strFormat = new StringFormat();
                strFormat.Alignment = StringAlignment.Near;
                strFormat.LineAlignment = StringAlignment.Center;
                strFormat.Trimming = StringTrimming.EllipsisCharacter;
                arrColumnLefts.Clear();
                arrColumnWidths.Clear();
                iCellHeight = 0;
                iRow = 0;
                bFirstPage = true;
                bNewPage = true;
                iTotalWidth = 0;
                foreach (DataGridViewColumn dgvGridCol in dataGridView1.Columns)
                {
                    iTotalWidth += dgvGridCol.Width;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {

                int iLeftMargin = e.MarginBounds.Left;
                int iTopMargin = e.MarginBounds.Top;
                bool bMorePagesToPrint = false;
                int iTmpWidth = 0;
                if (bFirstPage)
                {
                    foreach (DataGridViewColumn GridCol in dataGridView1.Columns)
                    {
                        iTmpWidth = (int)(Math.Floor((double)((double)GridCol.Width /
                                        (double)iTotalWidth * (double)iTotalWidth *
                                        ((double)e.MarginBounds.Width / (double)iTotalWidth))));
                        iHeaderHeight = (int)(e.Graphics.MeasureString(GridCol.HeaderText,
                                        GridCol.InheritedStyle.Font, iTmpWidth).Height) + 22;

                        arrColumnLefts.Add(iLeftMargin);
                        arrColumnWidths.Add(iTmpWidth);
                        iLeftMargin += iTmpWidth;
                    }
                }

                while (iRow <= dataGridView1.Rows.Count - 1)
                {
                    DataGridViewRow GridRow = dataGridView1.Rows[iRow];

                    iCellHeight = GridRow.Height + 15;
                    int iCount = 0;
                    double totalcount = Convert.ToDouble(iRow) % Convert.ToDouble(10);
                    if (totalcount == 0 && iRow != 0 && totalrecord == 0)
                    {
                        bNewPage = true;
                        bFirstPage = false;
                        bMorePagesToPrint = true;
                        totalrecord = 1;
                        break;
                    }
                    else
                    {
                        if (bNewPage)
                        {
                            e.Graphics.DrawString(currTable, new System.Drawing.Font(dataGridView1.Font, FontStyle.Bold),
                                                                Brushes.Black, e.MarginBounds.Left, e.MarginBounds.Top -
                                                                e.Graphics.MeasureString(currTable, new System.Drawing.Font(dataGridView1.Font,
                                                                FontStyle.Bold), e.MarginBounds.Width).Height - 13);

                            String strDate = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToShortTimeString();
                            e.Graphics.DrawString(strDate, new System.Drawing.Font(dataGridView1.Font, FontStyle.Bold),
                                    Brushes.Black, e.MarginBounds.Left + (e.MarginBounds.Width -
                                    e.Graphics.MeasureString(strDate, new System.Drawing.Font(dataGridView1.Font,
                                    FontStyle.Bold), e.MarginBounds.Width).Width), e.MarginBounds.Top -
                                    e.Graphics.MeasureString(currTable, new System.Drawing.Font(new System.Drawing.Font(dataGridView1.Font,
                                    FontStyle.Bold), FontStyle.Bold), e.MarginBounds.Width).Height - 13);
                            iTopMargin = e.MarginBounds.Top;
                            foreach (DataGridViewColumn GridCol in dataGridView1.Columns)
                            {
                                e.Graphics.FillRectangle(new SolidBrush(Color.LightGray),
                                    new System.Drawing.Rectangle((int)arrColumnLefts[iCount], iTopMargin,
                                    (int)arrColumnWidths[iCount], iHeaderHeight));

                                e.Graphics.DrawRectangle(Pens.Black,
                                    new System.Drawing.Rectangle((int)arrColumnLefts[iCount], iTopMargin,
                                    (int)arrColumnWidths[iCount], iHeaderHeight));

                                e.Graphics.DrawString(GridCol.HeaderText, GridCol.InheritedStyle.Font,
                                    new SolidBrush(GridCol.InheritedStyle.ForeColor),
                                    new RectangleF((int)arrColumnLefts[iCount], iTopMargin,
                                    (int)arrColumnWidths[iCount], iHeaderHeight), strFormat);
                                iCount++;
                            }
                            bNewPage = false;
                            iTopMargin += iHeaderHeight;
                        }
                        iCount = 0;

                        foreach (DataGridViewCell Cel in GridRow.Cells)
                        {
                            if (Cel.Value != null)
                            {
                                e.Graphics.DrawString(Cel.Value.ToString(), Cel.InheritedStyle.Font,
                                            new SolidBrush(Cel.InheritedStyle.ForeColor),
                                            new RectangleF((int)arrColumnLefts[iCount], (float)iTopMargin,
                                            (int)arrColumnWidths[iCount], (float)iCellHeight), strFormat);
                            }
                            e.Graphics.DrawRectangle(Pens.Black, new Rectangle((int)arrColumnLefts[iCount],
                                    iTopMargin, (int)arrColumnWidths[iCount], iCellHeight));

                            iCount++;
                        }
                    }

                    iRow++;
                    iTopMargin += iCellHeight;
                    totalrecord = 0;
                }
                if (bMorePagesToPrint)
                    e.HasMorePages = true;
                else
                    e.HasMorePages = false;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
