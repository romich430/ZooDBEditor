using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZooDBEditor
{
    public partial class Auth : Form
    {

        SplashScreen ss;
        SqlConnection db;
        public Auth(SplashScreen ss1)
        {
            InitializeComponent();

            ss = ss1;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //while(ss.progressBar1.Value != 100)
            //{
            //    Thread.Sleep(1000);
            //    ss.progressBar1.PerformStep();
            //}

            ss.Close();
            ss = null;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(Login())
            {
                SqlCommand command = new SqlCommand("SELECT r.name " +
                                                      "FROM sys.database_role_members AS m " +
                                                      "INNER JOIN sys.database_principals AS r " +
                                                      "ON m.role_principal_id = r.principal_id " +
                                                      "INNER JOIN sys.database_principals AS u " +
                                                      "ON u.principal_id = m.member_principal_id " +
                                                      "WHERE u.name=\'" + textBox1.Text + "\';", db);
                this.Hide();
                DbEditor editor = new DbEditor(this, command.ExecuteScalar().ToString(), db);
                editor.Show();
                Task.Factory.StartNew(() =>
                {
                    int time = 0;
                    while(time != 3600)
                    {
                        Thread.Sleep(1000);
                        time += 1;
                        if (!editor.Visible) return;
                        else editor.BeginInvoke((MethodInvoker)(() => editor.label1.Text = String.Format("{0:D2}:{1:D2}", (3600 - time) / 60, (3600 - time) % 60)));
                    }

                    editor.BeginInvoke((MethodInvoker)(() => editor.Close()));

                });
            } 
            else
            {
                MessageBox.Show("Wrong username or password", "Error");
            }
        }

        /* IMPORTANT!!!
         * USER LOGINS
         * 
         * User: OwnerUser
         * Password: zookeeper
         * 
         * User: WorkerUser
         * Password: work_in_zoo
         * 
         * User: UsualUser
         * Password: readonly_access
         * 
         * */

        private bool Login()
        {
            /*
             * INSERT HERE YOUR OWN SERVER (backup of database is in src/zoo folder)
             */
            string connectionString = "Server=DESKTOP-O1SJKGD;Database=Zoo;User Id=" + textBox1.Text + ";Password=" + textBox2.Text + ";";

            db = new SqlConnection(connectionString);
            try
            {
                db.Open();
                return true;
            }
            catch (SqlException ex)
            {
                return false;
            }
        }
    }
}
