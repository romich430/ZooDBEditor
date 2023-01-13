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
    public partial class DbEditor : Form
    {
        private string accessOptions;
        private Auth auth;
        private SqlConnection conn;
        private List<Form> forms;
        public DbEditor(Auth auth_, string access, SqlConnection conn_)
        {
            InitializeComponent();

            accessOptions = access;
            auth = auth_;
            conn = conn_;

            forms = new List<Form>();
        }

        private void DbEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            auth.Show();
            foreach(var form in forms)
            {
                form.Close();
            }
        }

        private void table_Click(object sender, EventArgs e)
        {
            string table = "";
            switch((sender as Button).Text)
            {
                case "Complexes": table = "complex"; break;
                case "Workers": table = "worker"; break;
                case "Cages": table = "building"; break;
                case "Species": table = "species"; break;
            }

            TableLayoutForm c = new TableLayoutForm(conn, table, accessOptions != "db_datareader");
            c.Show();
            forms.Add(c);
        }

    }
}
