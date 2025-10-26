using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace VotingSystem
{
    public partial class AdminDashboard : Form


    {
        private string originalStudentID;

        List<string> programs = new List<string> {
            "Bachelor Of Science in Information Technology",
            "Bachelor Of Science in Computer Engineering",
            "Bachelor Of Science in Computer Science",
            "Bachelor Of Science in Tourism Management",
            "Bachelor Of Science in Business Administration"
        };

        List<string> positions = new List<string> {
            "President",
            "Vice President",
            "Secretary",
            "Auditor",
            "Treasurer"
        };

        private const string ConnectionString = @"Data Source=DESKTOP-54DEN4R\SQLEXPRESS;Initial Catalog=VotingSystem;Integrated Security=True";


        public AdminDashboard()
        {
            InitializeComponent();

            foreach (string program in programs)
            {
                cbProgramV.Items.Add(program);
            }

            foreach (string position in positions)
            {
                cbPosition.Items.Add(position);
            }

            foreach (string program in programs)
            {
                cbProgramC.Items.Add(program);
            }
            foreach (string program in programs)
            {
                cbProgramA.Items.Add(program);
            }
            LoadVotersData();
            LoadCandidateData();
        }

        private void btnDashboard_Click_1(object sender, EventArgs e)
        {
            dashboardP.Visible = true;
            mCreateNewEvent.Visible = false;
            mVotersP.Visible = false;
            mAccountP.Visible = false;
        }

        private void btnmCandidate_Click_1(object sender, EventArgs e)
        {
            dashboardP.Visible = false;
            mCreateNewEvent.Visible = true;
            mVotersP.Visible = false;
            mAccountP.Visible = false;
            LoadCandidateData();
        }

        private void btnmVoters_Click(object sender, EventArgs e)
        {
            dashboardP.Visible = false;
            mCreateNewEvent.Visible = false;
            mVotersP.Visible = true;
            mAccountP.Visible = false;
            LoadVotersData();
        }

        private void btnmAccount_Click(object sender, EventArgs e)
        {
            dashboardP.Visible = false;
            mCreateNewEvent.Visible = false;
            mVotersP.Visible = false;
            mAccountP.Visible = true;
            LoadAccountData();
        }

        private void LoadVotersData()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {

                con.Open();
                SqlDataAdapter da = new SqlDataAdapter("SELECT StudentID, FirstName, LastName, MiddleName, Program, Gender FROM Voters", con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                TableVoters.Columns.Clear();
                TableVoters.DataSource = dt;

                int totalRows = TableVoters.RowCount;

                if (TableVoters.AllowUserToAddRows)
                {
                    totalRows--;
                }

                totalParticipants.Text = totalRows.ToString();
            }
        }
        private void TableVoters_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            String studentID = TableVoters.Rows[e.RowIndex].Cells[0].Value?.ToString();

            this.originalStudentID = studentID;

            String FirstName = TableVoters.Rows[e.RowIndex].Cells[1].Value?.ToString();
            String LastName = TableVoters.Rows[e.RowIndex].Cells[2].Value?.ToString();
            String MiddleName = TableVoters.Rows[e.RowIndex].Cells[3].Value?.ToString();
            String Program = TableVoters.Rows[e.RowIndex].Cells[4].Value?.ToString();
            String Gender = TableVoters.Rows[e.RowIndex].Cells[5].Value?.ToString();

            tbStudentNumV.Text = studentID;
            tbFirstNameV.Text = FirstName;
            tbLastNameV.Text = LastName;
            tbMiddleNameV.Text = MiddleName;

            if (Gender != null && Gender.Equals("Male", StringComparison.OrdinalIgnoreCase))
            {
                rbMaleV.Checked = true;
                rbFemaleV.Checked = false;
            }
            else if (Gender != null && Gender.Equals("Female", StringComparison.OrdinalIgnoreCase))
            {
                rbFemaleV.Checked = true;
                rbMaleV.Checked = false;
            }
            else
            {
                rbMaleV.Checked = false;
                rbFemaleV.Checked = false;
            }

            int index = cbProgramV.FindString(Program);
            if (index != -1)
            {
                cbProgramV.SelectedIndex = index;
            }
            else
            {
                cbProgramV.SelectedIndex = -1;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                string StudentID = tbStudentNumV.Text;
                string FirstName = tbFirstNameV.Text;
                string LastName = tbLastNameV.Text;
                string MiddleName = tbMiddleNameV.Text;
                string Gender = rbMaleV.Checked ? "Male" : rbFemaleV.Checked ? "Female" : "";

                if (string.IsNullOrEmpty(Gender))
                {
                    MessageBox.Show("Please Select a gender.");
                    return;
                }

                if (cbProgramV.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a program.");
                    return;
                }

                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Voters (StudentID, FirstName, LastName, MiddleName, Gender, Program) VALUES (@StudentID, @FirstName, @LastName, @MiddleName, @Gender, @Program)", con);

                cmd.Parameters.AddWithValue("@StudentID", StudentID);
                cmd.Parameters.AddWithValue("@FirstName", FirstName);
                cmd.Parameters.AddWithValue("@LastName", LastName);
                cmd.Parameters.AddWithValue("@MiddleName", MiddleName);
                cmd.Parameters.AddWithValue("@Gender", Gender);
                cmd.Parameters.AddWithValue("@Program", cbProgramV.SelectedItem.ToString());

                cmd.ExecuteNonQuery();
                MessageBox.Show("Voters Account has been saved.");
            }

            tbFirstNameV.Clear();
            tbLastNameV.Clear();
            tbMiddleNameV.Clear();
            tbStudentNumV.Clear();
            rbMaleV.Checked = false;
            rbFemaleV.Checked = false;
            cbProgramV.SelectedIndex = -1;


            LoadVotersData();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.originalStudentID))
            {
                MessageBox.Show("Please select a voter from the table before attempting to update.");
                return;
            }

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                string NewStudentID = tbStudentNumV.Text;
                string FirstName = tbFirstNameV.Text;
                string LastName = tbLastNameV.Text;
                string MiddleName = tbMiddleNameV.Text;
                string Gender = rbMaleV.Checked ? "Male" : rbFemaleV.Checked ? "Female" : "";
                string Program = cbProgramV.SelectedIndex != -1 ? cbProgramV.SelectedItem.ToString() : null;

                if (string.IsNullOrEmpty(Gender))
                {
                    MessageBox.Show("Please select a gender.");
                    return;
                }

                if (cbProgramV.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a program.");
                    return;
                }

                SqlCommand cmd = new SqlCommand(
                    "UPDATE Voters SET StudentID=@NewStudentID, FirstName=@FirstName, LastName=@LastName, MiddleName=@MiddleName, Gender=@Gender, Program=@Program WHERE StudentID=@OriginalStudentID", con);

                cmd.Parameters.AddWithValue("@NewStudentID", NewStudentID);
                cmd.Parameters.AddWithValue("@FirstName", FirstName);
                cmd.Parameters.AddWithValue("@LastName", LastName);
                cmd.Parameters.AddWithValue("@MiddleName", MiddleName);
                cmd.Parameters.AddWithValue("@Gender", Gender);
                cmd.Parameters.AddWithValue("@Program", Program);
                cmd.Parameters.AddWithValue("@OriginalStudentID", this.originalStudentID);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Voters information updated successfully.");
                }
                else
                {
                    MessageBox.Show("Update failed. The voter may not exist or no changes were made.");
                }

                tbFirstNameV.Clear();
                tbLastNameV.Clear();
                tbMiddleNameV.Clear();
                tbStudentNumV.Clear();
                rbMaleV.Checked = false;
                rbFemaleV.Checked = false;
                cbProgramV.SelectedIndex = -1;
            }

            LoadVotersData();
            this.originalStudentID = null;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.originalStudentID))
            {
                MessageBox.Show("Please select a voter from the table before attempting to Delete.");
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete the voter with Student ID: {this.originalStudentID}?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM Voters WHERE StudentID=@OriginalStudentID", con);

                cmd.Parameters.AddWithValue("@OriginalStudentID", this.originalStudentID);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Voters account has been deleted.");
                }
                else
                {
                    MessageBox.Show("Delete failed.");
                }

                tbFirstNameV.Clear();
                tbLastNameV.Clear();
                tbMiddleNameV.Clear();
                tbStudentNumV.Clear();
                rbMaleV.Checked = false;
                rbFemaleV.Checked = false;
                cbProgramV.SelectedIndex = -1;
            }

            LoadVotersData();
            this.originalStudentID = null;
        }

        private void LoadCandidateData()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter("SELECT StudentID, FirstName, LastName, MiddleName, Program, Gender, Position FROM Candidate", con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                TableCandidate.Columns.Clear();
                TableCandidate.DataSource = dt;

                int rowCount = TableCandidate.RowCount;

                if (TableCandidate.AllowUserToAddRows)
                {
                    rowCount--;
                }

                candidate.Text = rowCount.ToString();
            }
        }
        private void btnAddC_Click(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                string StudentID = tbStudentNumberC.Text;
                string FirstName = tbFirstNameC.Text;
                string LastName = tbLastNameC.Text;
                string MiddleName = tbMiddleNameC.Text;
                string Gender = rbMaleC.Checked ? "Male" : rbFemaleC.Checked ? "Female" : "";
                string Program = cbProgramC.SelectedIndex != -1 ? cbProgramC.SelectedItem.ToString() : null;
                string Position = cbPosition.SelectedIndex != -1 ? cbPosition.SelectedItem.ToString() : null;

                if (cbPosition.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a position.");
                    return;

                }

                if (string.IsNullOrEmpty(Gender))
                {
                    MessageBox.Show("Please Select a gender.");
                    return;
                }

                if (cbProgramC.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a program.");
                    return;
                }

                if (cbPosition.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a position.");
                    return;
                }

                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Candidate (StudentID, FirstName, LastName, MiddleName, Gender, Program, Position) VALUES (@StudentID, @FirstName, @LastName, @MiddleName, @Gender, @Program, @Position)", con);

                cmd.Parameters.AddWithValue("@StudentID", StudentID);
                cmd.Parameters.AddWithValue("@FirstName", FirstName);
                cmd.Parameters.AddWithValue("@LastName", LastName);
                cmd.Parameters.AddWithValue("@MiddleName", MiddleName);
                cmd.Parameters.AddWithValue("@Gender", Gender);
                cmd.Parameters.AddWithValue("@Program", cbProgramC.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@Position", cbPosition.SelectedItem.ToString());
                cmd.ExecuteNonQuery();
                MessageBox.Show("Candidate Account has been saved.");

                con.Close();
            }

            tbFirstNameC.Clear();
            tbLastNameC.Clear();
            tbMiddleNameC.Clear();
            tbStudentNumberC.Clear();
            rbMaleC.Checked = false;
            rbFemaleC.Checked = false;
            cbProgramC.SelectedIndex = -1;

            LoadCandidateData();
        }

        private void TableCandidate_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            String studentID = TableCandidate.Rows[e.RowIndex].Cells[0].Value?.ToString();

            this.originalStudentID = studentID;

            String FirstName = TableCandidate.Rows[e.RowIndex].Cells[1].Value?.ToString();
            String LastName = TableCandidate.Rows[e.RowIndex].Cells[2].Value?.ToString();
            String MiddleName = TableCandidate.Rows[e.RowIndex].Cells[3].Value?.ToString();
            String Program = TableCandidate.Rows[e.RowIndex].Cells[4].Value?.ToString();
            String Gender = TableCandidate.Rows[e.RowIndex].Cells[5].Value?.ToString();
            String Position = TableCandidate.Rows[e.RowIndex].Cells[6].Value?.ToString();

            tbStudentNumberC.Text = studentID;
            tbFirstNameC.Text = FirstName;
            tbLastNameC.Text = LastName;
            tbMiddleNameC.Text = MiddleName;

            if (Gender != null && Gender.Equals("Male", StringComparison.OrdinalIgnoreCase))
            {
                rbMaleC.Checked = true;
                rbFemaleC.Checked = false;
            }
            else if (Gender != null && Gender.Equals("Female", StringComparison.OrdinalIgnoreCase))
            {
                rbFemaleC.Checked = true;
                rbMaleC.Checked = false;
            }
            else
            {
                rbMaleC.Checked = false;
                rbFemaleC.Checked = false;
            }

            int indexProg = cbProgramC.FindString(Program);
            if (indexProg != -1)
            {
                cbProgramC.SelectedIndex = indexProg;
            }
            else
            {
                cbProgramC.SelectedIndex = -1;
            }

            int indexPos = cbPosition.FindString(Position);
            if (indexPos != -1)
            {
                cbPosition.SelectedIndex = indexPos;
            }
            else
            {
                cbPosition.SelectedIndex = -1;
            }
        }

        private void btnUpdateC_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.originalStudentID))
            {
                MessageBox.Show("Please select a Candidate from the table before attempting to update.");
                return;
            }

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                string NewStudentID = tbStudentNumberC.Text;
                string FirstName = tbFirstNameC.Text;
                string LastName = tbLastNameC.Text;
                string MiddleName = tbMiddleNameC.Text;
                string Gender = rbMaleC.Checked ? "Male" : rbFemaleC.Checked ? "Female" : "";
                string Program = cbProgramC.SelectedIndex != -1 ? cbProgramC.SelectedItem.ToString() : null;
                string Position = cbPosition.SelectedIndex != -1 ? cbPosition.SelectedItem.ToString() : null;

                if (string.IsNullOrEmpty(Gender))
                {
                    MessageBox.Show("Please select a gender.");
                    return;
                }

                if (cbProgramC.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a program.");
                    return;
                }

                if (cbPosition.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a position.");
                    return;
                }

                SqlCommand cmd = new SqlCommand(
                    "UPDATE Candidate SET StudentID=@NewStudentID, FirstName=@FirstName, LastName=@LastName, MiddleName=@MiddleName, Gender=@Gender, Program=@Program, Position=@Position WHERE StudentID=@OriginalStudentID", con);

                cmd.Parameters.AddWithValue("@NewStudentID", NewStudentID);
                cmd.Parameters.AddWithValue("@FirstName", FirstName);
                cmd.Parameters.AddWithValue("@LastName", LastName);
                cmd.Parameters.AddWithValue("@MiddleName", MiddleName);
                cmd.Parameters.AddWithValue("@Gender", Gender);
                cmd.Parameters.AddWithValue("@Program", Program);
                cmd.Parameters.AddWithValue("@OriginalStudentID", this.originalStudentID);
                cmd.Parameters.AddWithValue("@Position", Position);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Candidate information updated successfully.");
                }
                else
                {
                    MessageBox.Show("Update failed. The voter may not exist or no changes were made.");
                }

                tbFirstNameC.Clear();
                tbLastNameC.Clear();
                tbMiddleNameC.Clear();
                tbStudentNumberC.Clear();
                rbMaleC.Checked = false;
                rbFemaleC.Checked = false;
                cbProgramC.SelectedIndex = -1;
            }

            LoadCandidateData();
            this.originalStudentID = null;
        }

        private void btnDeleteC_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.originalStudentID))
            {
                MessageBox.Show("Please select a Candidate from the table before attempting to Delete.");
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete the Candidate with Student ID: {this.originalStudentID}?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM Candidate WHERE StudentID=@OriginalStudentID", con);

                cmd.Parameters.AddWithValue("@OriginalStudentID", this.originalStudentID);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Candidate account has been deleted.");
                }
                else
                {
                    MessageBox.Show("Delete failed.");
                }

                tbFirstNameC.Clear();
                tbLastNameC.Clear();
                tbMiddleNameC.Clear();
                tbStudentNumberC.Clear();
                rbMaleC.Checked = false;
                rbFemaleC.Checked = false;
                cbProgramC.SelectedIndex = -1;
            }

            LoadCandidateData();
            this.originalStudentID = null;
        }

        private void TableAccount_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            String studentID = TableAccount.Rows[e.RowIndex].Cells[0].Value?.ToString();

            this.originalStudentID = studentID;

            String FirstName = TableAccount.Rows[e.RowIndex].Cells[1].Value?.ToString();
            String LastName = TableAccount.Rows[e.RowIndex].Cells[2].Value?.ToString();
            String MiddleName = TableAccount.Rows[e.RowIndex].Cells[3].Value?.ToString();
            String Program = TableAccount.Rows[e.RowIndex].Cells[4].Value?.ToString();
            String Password = TableAccount.Rows[e.RowIndex].Cells[5].Value?.ToString();

            tbStudentNumberA.Text = studentID;
            tbFirstNameA.Text = FirstName;
            tbLastNameA.Text = LastName;
            tbMiddleNameA.Text = MiddleName;
            tbPasswordA.Text = Password;

            int index = cbProgramA.FindString(Program);
            if (index != -1)
            {
                cbProgramA.SelectedIndex = index;
            }
            else
            {
                cbProgramA.SelectedIndex = -1;
            }
        }

        private void LoadAccountData()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter("SELECT StudentID, FirstName, LastName, MiddleName, Program, Password FROM Account", con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                TableAccount.Columns.Clear();
                TableAccount.DataSource = dt;
            }
        }


        private void btnAddA_Click(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                string StudentID = tbStudentNumberA.Text;
                string FirstName = tbFirstNameA.Text;
                string LastName = tbLastNameA.Text;
                string MiddleName = tbMiddleNameA.Text;
                string Program = cbProgramA.SelectedIndex != -1 ? cbProgramA.SelectedItem.ToString() : null;
                string Password = tbPasswordA.Text;

                if (cbProgramA.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a program.");
                    return;
                }

                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Account (StudentID, FirstName, LastName, MiddleName, Program, Password) VALUES (@StudentID, @FirstName, @LastName, @MiddleName, @Program, @Password)", con);

                cmd.Parameters.AddWithValue("@StudentID", StudentID);
                cmd.Parameters.AddWithValue("@FirstName", FirstName);
                cmd.Parameters.AddWithValue("@LastName", LastName);
                cmd.Parameters.AddWithValue("@MiddleName", MiddleName);
                cmd.Parameters.AddWithValue("@Program", cbProgramA.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@Password", Password);
                cmd.ExecuteNonQuery();
                MessageBox.Show("Account has been saved.");

                con.Close();

                tbFirstNameA.Clear();
                tbLastNameA.Clear();
                tbMiddleNameA.Clear();
                tbStudentNumberA.Clear();
                cbProgramA.SelectedIndex = -1;
                tbPasswordA.Clear();
            }
            LoadAccountData();
        }

        private void btnUpdateA_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.originalStudentID))
            {
                MessageBox.Show("Please select a voter from the table before attempting to update.");
                return;
            }

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                string NewStudentID = tbStudentNumberA.Text;
    string FirstName = tbFirstNameA.Text;
    string LastName = tbLastNameA.Text;
    string MiddleName = tbMiddleNameA.Text;
    string Program = cbProgramA.SelectedIndex != -1 ? cbProgramA.SelectedItem.ToString() : null;
    string Password = tbPasswordA.Text;

                if (cbProgramA.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a program.");
                    return;
                }

SqlCommand cmd = new SqlCommand(
    "UPDATE Account SET StudentID=@NewStudentID, FirstName=@FirstName, LastName=@LastName, MiddleName=@MiddleName, Program=@Program, Password=@Password WHERE StudentID=@OriginalStudentID", con);

cmd.Parameters.AddWithValue("@NewStudentID", NewStudentID);
cmd.Parameters.AddWithValue("@FirstName", FirstName);
cmd.Parameters.AddWithValue("@LastName", LastName);
cmd.Parameters.AddWithValue("@MiddleName", MiddleName);
cmd.Parameters.AddWithValue("@Program", Program);
cmd.Parameters.AddWithValue("@OriginalStudentID", this.originalStudentID);
cmd.Parameters.AddWithValue("@Password", Password);
int rowsAffected = cmd.ExecuteNonQuery();
if (rowsAffected > 0)
{
    MessageBox.Show("Account information updated successfully.");
}
else
{
    MessageBox.Show("Update failed. The Account may not exist or no changes were made.");
}

tbFirstNameA.Clear();
tbLastNameA.Clear();
tbMiddleNameA.Clear();
tbStudentNumberA.Clear();
cbProgramA.SelectedIndex = -1;
tbPasswordA.Clear();

            }
            LoadAccountData();
this.originalStudentID = null;
        }

        private void btnDeleteA_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.originalStudentID))
            {
                MessageBox.Show("Please select a Account from the table before attempting to Delete.");
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete the Candidate with Student ID: {this.originalStudentID}?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM Account WHERE StudentID=@OriginalStudentID", con);

                cmd.Parameters.AddWithValue("@OriginalStudentID", this.originalStudentID);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Account account has been deleted.");
                }
                else
                {
                    MessageBox.Show("Delete failed.");
                }

                tbFirstNameA.Clear();
                tbLastNameA.Clear();
                tbMiddleNameA.Clear();
                tbStudentNumberA.Clear();
                cbProgramA.SelectedIndex = -1;
                tbPasswordA.Clear();

            }

            LoadAccountData();
            this.originalStudentID = null;
        }

        private void guna2HtmlLabel4_Click(object sender, EventArgs e)
        {

        }

        private void mVotersP_Paint(object sender, PaintEventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void guna2HtmlLabel20_Click(object sender, EventArgs e)
        {

        }
    }
}

