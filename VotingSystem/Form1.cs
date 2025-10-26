using POLLINGSYSTEM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VotingSystem
{
    public partial class VotingSystem : Form
    {
        public VotingSystem()
        {
            InitializeComponent();
        }

        private void btnLogIn_Click(object sender, EventArgs e)
        {


            string connectionString = @"Data Source=DESKTOP-54DEN4R\SQLEXPRESS;Initial Catalog=POLLINGSYSTEM;Integrated Security=True;Encrypt=False";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();

                    string query = "SELECT Count([Username]) FROM [dbo].[AccountTb] WHERE [Username] = @Useername AND [Password] = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Useername", tbUsername.Text);
                        cmd.Parameters.AddWithValue("@Password", tbPassword.Text);

                        int count = (int)cmd.ExecuteScalar();

                        if (count == 1)
                        {
                            AdminDashboard theAdminDash = new AdminDashboard();
                            theAdminDash.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("Invalid Student Number or Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
