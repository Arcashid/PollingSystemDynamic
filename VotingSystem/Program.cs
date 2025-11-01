using POLLINGSYSTEM;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VotingSystem
{
    internal static class Program
    {
        public static int StudentID;
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ApplicationExit += OnApplicationExit;
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.Run(new AdminDashboard());
        }

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            if (StudentID == 0) return;
            string sql = "UPDATE Voters SET IsActive = 0 WHERE StudentNo = @StudentNo";

            using (SqlConnection con = new SqlConnection("Data Source=DESKTOP-RVB0L7Q\\SQLEXPRESS;Initial Catalog=POLLINGSYSTEM;Integrated Security=True;Encrypt=False"))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@StudentNo", StudentID);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during status update: {ex.Message}");
                }
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show("Application is exiting normally.", "Exit", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Application is exiting normally.", "Exit", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

