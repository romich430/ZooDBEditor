using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZooDBEditor
{
    public partial class RecordLayoutForm : Form
    {
        private const string getForeignCol = "SELECT tab2.name, col2.name " +
                                            "FROM sys.foreign_key_columns fkc " +
                                            "INNER JOIN sys.objects obj " +
                                            "ON obj.object_id = fkc.constraint_object_id " +
                                            "INNER JOIN sys.tables tab1 " +
                                            "ON tab1.object_id = fkc.parent_object_id " +
                                            "INNER JOIN sys.schemas sch " +
                                            "ON tab1.schema_id = sch.schema_id " +
                                            "INNER JOIN sys.columns col1 " +
                                            "ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id " +
                                            "INNER JOIN sys.tables tab2 " +
                                            "ON tab2.object_id = fkc.referenced_object_id " +
                                            "INNER JOIN sys.columns col2 " +
                                            "ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id " +
                                            "WHERE col1.name='";

        private SqlConnection conn;
        private List<string> foreignKeysCols = new List<string> { "attached_building", "head_id", "complex_name", "species_name", "building_number" };
        private DataGridViewRow row;
        private List<Control> controls;
        private DataSet ds;
        private bool isFilter;
        public RecordLayoutForm(bool isFilter_, DataGridViewRow row_, DataSet ds_, SqlConnection conn_)
        {
            InitializeComponent();

            if(isFilter)
            {
                this.Text = "Filter table";
            }

            controls = new List<Control>();
            isFilter = isFilter_;
            row = row_;
            conn = conn_;
            ds = ds_;

            var cols = ds_.Tables[0].Columns;
            int left = 25, top = 20;
            TextBox txt;
            NumericUpDown nud;
            Label lbl;
            CheckBox chb;
            ComboBox cmb;
            foreach(DataColumn col in cols)
            {
                if (col.ColumnName == "id") continue;

                lbl = new Label();
                lbl.Left = left;
                lbl.Top = top;
                lbl.Text = col.ColumnName;
                lbl.Width = 140;
                this.Controls.Add(lbl);

                if(foreignKeysCols.Contains(col.ColumnName))
                {
                    cmb = new ComboBox();
                    cmb.Left = lbl.Width + 2 * left;
                    cmb.Top = top;

                    SqlCommand cmd = new SqlCommand(getForeignCol + col.ColumnName + "'", conn);
                    var reader = cmd.ExecuteReader();
                    string table = "", column = "";
                    while(reader.Read())
                    {
                        table = reader.GetString(0);
                        column = reader.GetString(1);
                    }
                    reader.Close();

                    cmd.CommandText = "SELECT " + column + " FROM " + table;
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        cmb.Items.Add(reader.GetValue(0).ToString());
                    }
                    reader.Close();
                    cmb.Text = (row != null ? row.Cells[lbl.Text].Value.ToString() : "");

                    this.Controls.Add(cmb);
                    controls.Add(cmb);
                }
                else if (col.DataType.Name.ToString() == "String" || col.DataType.Name.ToString() == "Decimal")
                {
                    txt = new TextBox();
                    txt.Left = lbl.Width + 2 * left;
                    txt.Top = top;
                    txt.Text = (row != null ? row.Cells[lbl.Text].Value.ToString() : "");
                    if (col.DataType.Name.ToString() == "Decimal")
                    {
                        txt.Tag = "decimal";
                        var tt = new ToolTip();
                        tt.InitialDelay = 1;
                        tt.SetToolTip(txt, "Only 0-9 and , allowed");
                        txt.KeyPress += (sender, e) =>
                        {
                            if (!char.IsNumber(e.KeyChar) && (Keys)e.KeyChar != Keys.Back
                            && e.KeyChar != ',' || e.KeyChar == ',' && (sender as TextBox).Text.Contains(","))
                            {
                                e.Handled = true;
                            }
                        };
                    }
                    this.Controls.Add(txt);
                    controls.Add(txt);
                }
                else if (col.DataType.Name.ToString().Contains("Int"))
                {
                    nud = new NumericUpDown();
                    nud.Left = lbl.Width + 2 * left;
                    nud.Top = top;
                    nud.Maximum = 10000;
                    nud.Minimum = 0;
                    nud.Value = (row != null && row.Cells[lbl.Text].Value.GetType() != typeof(DBNull) ? Convert.ToInt32(row.Cells[lbl.Text].Value) : 0);
                    this.Controls.Add(nud);
                    controls.Add(nud);
                }
                else if (col.DataType.Name.ToString() == "Boolean")
                {
                    chb = new CheckBox();
                    if (isFilter) {
                        ToolTip tt = new ToolTip();
                        tt.InitialDelay = 1;
                        tt.SetToolTip(chb, "The square is for indetermined");
                        chb.ThreeState = true;
                    }
                    chb.Left = lbl.Width + 2 * left;
                    chb.Top = top;
                    if(isFilter)
                    {
                        chb.CheckState = CheckState.Indeterminate;
                    }
                    else
                    {
                        chb.Checked = (row != null ? row.Cells[lbl.Text].Value.ToString() == "True" : false);
                    }
                    this.Controls.Add(chb);
                    controls.Add(chb);
                }

                top += 40;
            }

            Button button = new Button();
            button.Text = "Confirm";
            button.Left = left;
            button.Top = top;
            this.Controls.Add(button);
            button.Click += confirmButton_Click;
        }

        private void confirmButton_Click(object sender, EventArgs e)
        {
            string commandString;
            bool firstEntrance = false;

            if(isFilter)
            {
                commandString = "SELECT * FROM " + ds.Tables[0].TableName + " WHERE ";
            }
            else if (row == null)
            {
                commandString = "INSERT INTO " + ds.Tables[0].TableName + " VALUES(";
            }
            else
            {
                commandString = "UPDATE " + ds.Tables[0].TableName + " SET ";
            }

            int offset = 0;
            for (int i = 0; i < ds.Tables[0].Columns.Count; i++)
            {
                if (ds.Tables[0].Columns[i].ColumnName == "id")
                {
                    offset = -1;
                    continue;
                }

                var tmp = GetValue(controls[i + offset]);
                if (isFilter && (tmp == "" || tmp == "''")) continue;
                else if(firstEntrance)
                {
                    commandString += " AND ";
                }

                if (row != null || isFilter)
                {
                    firstEntrance = true;
                    commandString += ds.Tables[0].Columns[i].ColumnName + 
                        (isStringLiteralControl(controls[i + offset]) ? " LIKE " : "=");
                }

                commandString += tmp;

                if (isStringLiteralControl(controls[i + offset])) commandString = commandString.Substring(0, commandString.Length-1) + "%'";

                if (i != ds.Tables[0].Columns.Count - 1)
                {
                    if(!isFilter)
                    {
                        commandString += ",";
                    }
                }
            }

            if (!isFilter && row == null)
            {
                commandString += ")";
            }
            else if (!isFilter && row != null)
            {
                string id = TableLayoutForm.idCol(ds.Tables[0].TableName);
                string idVal = row.Cells[id].Value.ToString();
                int id_ = 0;
                if (!int.TryParse(idVal, out id_))
                {
                    idVal = "'" + idVal + "'";
                }
                commandString += " WHERE " + id + "=" + idVal + ";";
            }

            try
            {
                if(isFilter)
                {
                    SqlDataAdapter da = new SqlDataAdapter(commandString, conn);
                    ds.Clear();
                    da.Fill(ds, ds.Tables[0].TableName);
                } 
                else
                {
                    SqlCommand cmd = new SqlCommand(commandString, conn);
                    cmd.ExecuteNonQuery();

                    SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM " + ds.Tables[0].TableName, conn);
                    ds.Clear();
                    da.Fill(ds, ds.Tables[0].TableName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Check inputs and related columns", "Error");
            }
        }

        private bool isStringLiteralControl(Control c)
        {
            return c.GetType().ToString() != "CheckBox" && c.GetType().ToString() != "NumericUpDown"
                && c.Tag != "decimal";
        }

        private string GetValue(Control control)
        {
            switch(control.GetType().Name)
            {
                case "CheckBox":
                    {
                        if ((control as CheckBox).CheckState == CheckState.Indeterminate) return "";
                        return ((control as CheckBox).Checked ? "1" : "0");
                    }
                case "NumericUpDown":
                    {
                        if ((control as NumericUpDown).Value == 0) return "";
                        return (control as NumericUpDown).Value.ToString();
                    }
                default:
                    {
                        if (control.Tag == "decimal") return control.Text.Replace(',', '.');
                        return "'" + control.Text + "'";
                    }
            }
        }
    }
}
