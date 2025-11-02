using POLLINGSYSTEM;
using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace VotingSystem
{
    public partial class VotingSystem : Form
    {
        private const string ConnectionString = @"Data Source=DESKTOP-RVB0L7Q\SQLEXPRESS;Initial Catalog=POLLINGSYSTEM;Integrated Security=True;Encrypt=False";
        private readonly object check;

        public VotingSystem()
        {
            InitializeComponent();
        }

        private void btnSignIn_Click(object sender, EventArgs e)
        {
            int studentId = 0;
            string currentUser = "";
            string studentNo = tbUsername.Text.Trim();
            tbPassword.PasswordChar = '•';
            string password = tbPassword.Text;

            if (string.IsNullOrWhiteSpace(studentNo) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter Student Number and Password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string userRole = null;

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                try
                {
                    con.Open();

                    string query = "SELECT [StudentNo], [Role] FROM [dbo].[Voters] WHERE [StudentNo] = @Username AND [Password] = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", studentNo);
                        cmd.Parameters.AddWithValue("@Password", password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userRole = reader["Role"].ToString().Trim();
                                currentUser = reader["StudentNo"].ToString().Trim();
                                if (!int.TryParse(reader["StudentNo"].ToString(), out studentId))
                                {
                                    throw new FormatException("Student Number retrieved from database is not a valid integer.");
                                }
                            }
                        }
                    }

                    if (userRole != null)
                    {
                        string updateActiveSql = "UPDATE Voters SET IsActive = 1 WHERE StudentNo = @StudentNo";
                        using (SqlCommand updateCmd = new SqlCommand(updateActiveSql, con))
                        {
                            updateCmd.Parameters.AddWithValue("@StudentNo", studentNo);
                            updateCmd.ExecuteNonQuery();
                        }

                        switch (userRole)
                        {
                            case "Admin":
                                AdminDashboard adminDash = new AdminDashboard();
                                adminDash.CurrentUser = currentUser;
                                adminDash.Show(); object check1 = check;
                                this.Hide();
                                break;

                            case "School Staff":
                            case "Student":
                                UserDashboard userDash = new UserDashboard();
                                userDash.StudentID = studentId;
                                userDash.Show();
                                this.Hide();
                                break;

                            default:
                                MessageBox.Show("Invalid user role", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid Student Number or Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (FormatException fex)
                {
                    MessageBox.Show($"Data Error: {fex.Message}", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void masked(object sender, EventArgs e)
        {
            tbPassword.PasswordChar = '•';
        }
    }
}