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

        private void BtnSignIn_Click(object sender, EventArgs e)
        {

            string connectionString = @"Data Source=DESKTOP-G0UC22L\SQLEXPRESS;Initial Catalog=VotingSystem;Integrated Security=True";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();

                    string query = "SELECT COUNT(1) FROM Account WHERE StudentID = @StudentID AND Password = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@StudentID", tbStudentNumber.Text);
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
