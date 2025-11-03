using LoginForm = VotingSystem.VotingSystem;
using Guna.UI2.WinForms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace POLLINGSYSTEM
{
    public partial class AdminDashboard : Form
    {
        List<string> programs = new List<string> {
         "Bachelor Of Science in Information Technology",
         "Bachelor Of Science in Computer Engineering",
         "Bachelor Of Science in Computer Science",
         "Bachelor Of Science in Tourism Management",
         "Bachelor Of Science in Business Administration"
        };

        List<string> roles = new List<string> {
            "Admin",
            "Student"
        };

        private const string ConnectionString = @"Data Source=DESKTOP-RVB0L7Q\SQLEXPRESS;Initial Catalog=POLLINGSYSTEM;Integrated Security=True;Encrypt=False";

        private string currentStudID = "";
        private string currentStudEvent = "";
        private string currentStudTeam = "";
        private string currentAccountStudID = "";

        private Timer dashboardTimer;
        internal string CurrentUser;

        private Guna2Button btnTogglePassword;

        public AdminDashboard()
        {
            InitializeComponent();

            EnsureAuditLogTable();

            UpdateTotalEventLabel();
            this.cbRoleAcc.SelectedIndexChanged += new System.EventHandler(this.cbRoleAcc_SelectedIndexChanged);
            UpdateProgramVisibility();

            btnTogglePassword = new Guna2Button
            {
                Name = "btnTogglePassword",
                Size = new Size(28, 28),
                BorderRadius = 14,
                FillColor = Color.White,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "👁",
                BackColor = Color.White
            };
            btnTogglePassword.Click += btnTogglePassword_Click;

            AccountPanel.Controls.Add(btnTogglePassword);
            PositionTogglePasswordButton();

            tbPassword.SizeChanged += (s, e) => PositionTogglePasswordButton();
            tbPassword.LocationChanged += (s, e) => PositionTogglePasswordButton();
            AccountPanel.Resize += (s, e) => PositionTogglePasswordButton();

            foreach (string program in programs)
            {
                cbProgram.Items.Add(program);
                cbProgramAcc.Items.Add(program);
            }

            foreach (string role in roles)
            {
                cbRoleAcc.Items.Add(role);
            }

            tbPassword.PasswordChar = '•';

            votersData.AutoGenerateColumns = false;
            if (!votersData.Columns.Contains("StudentNo"))
                votersData.Columns.Add("StudentNo", "Student No");
            if (!votersData.Columns.Contains("Role"))
                votersData.Columns.Add("Role", "Role");
            if (!votersData.Columns.Contains("LastName"))
                votersData.Columns.Add("LastName", "Last Name");
            if (!votersData.Columns.Contains("FirstName"))
                votersData.Columns.Add("FirstName", "First Name");
            if (!votersData.Columns.Contains("MiddleName"))
                votersData.Columns.Add("MiddleName", "Middle Name");
            if (!votersData.Columns.Contains("Program"))
                votersData.Columns.Add("Program", "Program");

            votersData.Columns["StudentNo"].DataPropertyName = "StudentNo";
            votersData.Columns["Role"].DataPropertyName = "Role";
            votersData.Columns["LastName"].DataPropertyName = "LastName";
            votersData.Columns["FirstName"].DataPropertyName = "FirstName";
            votersData.Columns["MiddleName"].DataPropertyName = "MiddleName";
            votersData.Columns["Program"].DataPropertyName = "Program";

            EventHistoryData.AutoGenerateColumns = false;
            EventHistoryData.Columns.Clear();

            if (!EventHistoryData.Columns.Contains("EventName"))
            {
                EventHistoryData.Columns.Add("EventName", "Event");
                EventHistoryData.Columns["EventName"].DataPropertyName = "EventName";
                EventHistoryData.Columns["EventName"].Width = 200;
            }

            if (!EventHistoryData.Columns.Contains("description"))
            {
                EventHistoryData.Columns.Add("description", "Description");
                EventHistoryData.Columns["description"].DataPropertyName = "description";
                EventHistoryData.Columns["description"].Width = 200;
            }

            if (!EventHistoryData.Columns.Contains("TeamName"))
            {
                EventHistoryData.Columns.Add("TeamName", "Team");
                EventHistoryData.Columns["TeamName"].DataPropertyName = "TeamName";
                EventHistoryData.Columns["TeamName"].Width = 150;
            }

            if (!EventHistoryData.Columns.Contains("VoteDate"))
            {
                EventHistoryData.Columns.Add("VoteDate", "Date & Time");
                EventHistoryData.Columns["VoteDate"].DataPropertyName = "VoteDate";
                EventHistoryData.Columns["VoteDate"].DefaultCellStyle.Format = "MM/dd/yy hh:mm tt";
            }
            LoadEventsData();
            LoadParticipantsData();
            LoadVotersData();
            LoadHistoryData();
            UpdateTotalEventLabel();
            UpdateEventDropdown();
            UpdateDashboardStats();

            dashboardTimer = new Timer();
            dashboardTimer.Interval = 5000;
            dashboardTimer.Tick += DashboardTimer_Tick;
            dashboardTimer.Start();

            cbEvent.SelectedIndexChanged += cbEvent_SelectedIndexChanged;
            this.tbSearchEventName.TextChanged += new System.EventHandler(this.tbSearchEventName_TextChanged);
        }

        private void tbSearchEventName_TextChanged(object sender, EventArgs e)
        {
            SearchEventAndDisplayChart();
        }

        private void DashboardTimer_Tick(object sender, EventArgs e)
        {
            if (dashboardP.Visible)
            {
                UpdateDashboardStats();
            }
        }

        private void UpdateDashboardStats()
        {
            UpdateTotalEventLabel();
            UpdateActiveVotersLabel();
            UpdateVotesCastLabel();
        }

        private void UpdateActiveVotersLabel()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM Voters WHERE IsActive = 1 AND Role = 'Student'";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        int voterCount = (int)cmd.ExecuteScalar();
                        lblVoters.Text = voterCount.ToString();
                    }
                }
            }
            catch (Exception)
            {
                lblVoters.Text = "0";
            }
        }
        private void UpdateVotesCastLabel()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = @"SELECT COUNT(*) 
                                 FROM History h
                                 INNER JOIN EventTb e ON h.EventName = e.EventName";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        int voteCount = (int)cmd.ExecuteScalar();
                        if (lblVotersVoteCast.InvokeRequired)
                        {
                            lblVotersVoteCast.Invoke(new Action(() => lblVotersVoteCast.Text = voteCount.ToString()));
                        }
                        else
                        {
                            lblVotersVoteCast.Text = voteCount.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (lblVotersVoteCast.InvokeRequired)
                {
                    lblVotersVoteCast.Invoke(new Action(() => lblVotersVoteCast.Text = "0"));
                }
                else
                {
                    lblVotersVoteCast.Text = "0";
                }
            }
        }

        private void btnTogglePassword_Click(object sender, EventArgs e)
        {
            if (tbPassword.PasswordChar == '•')
            {
                tbPassword.PasswordChar = '\0';
            }
            else
            {
                tbPassword.PasswordChar = '•';
            }
        }

        private void LoadVotersData(String searchKey = "")
        {
            SqlDataAdapter da;
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                if (searchKey == "")
                {
                    da = new SqlDataAdapter("SELECT StudentNo, Role, LastName, FirstName, MiddleName, Program FROM [dbo].[Voters]", con);
                }
                else
                {
                    da = new SqlDataAdapter("SELECT StudentNo, Role, LastName, FirstName, MiddleName, Program FROM [dbo].[Voters] " +
                        "where StudentNo like '%" + searchKey + "%' or LastName like '%" + searchKey + "%'", con);
                }

                DataTable dt = new DataTable();
                da.Fill(dt);

                votersData.DataSource = dt;
            }
        }

        private void ClearAccountFields()
        {
            tbStudentNumberAcc.Clear();
            tbPassword.Clear();
            cbRoleAcc.SelectedIndex = -1;
            tbLastNameAcc.Clear();
            tbMiddleNameAcc.Clear();
            tbFirstNameAcc.Clear();
            cbProgramAcc.SelectedIndex = -1;

            tbPassword.PasswordChar = '•';
            currentAccountStudID = "";

            cbRoleAcc.SelectedIndex = -1;
            cbProgramAcc.SelectedIndex = -1;

            UpdateProgramVisibility();
        }

        private void LoadEventsData(String searchKey = "")
        {
            SqlDataAdapter da;
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                if (searchKey == "")
                {
                    da = new SqlDataAdapter(
                        "SELECT EventID, EventName, TeamGroup, TimeStart, TimeEnd, description FROM EventTb",
                        con);
                }
                else
                {
                    da = new SqlDataAdapter(
                        "SELECT EventID, EventName, TeamGroup, TimeStart, TimeEnd, description " +
                        "FROM EventTb " +
                        "WHERE EventName LIKE '%" + searchKey + "%' OR EventID LIKE '%" + searchKey + "%'",
                        con);
                }

                DataTable dt = new DataTable();
                da.Fill(dt);

                EventTableData.DataSource = dt;

                if (EventTableData.Columns.Contains("EventID"))
                    EventTableData.Columns["EventID"].HeaderText = "Event ID";
                if (EventTableData.Columns.Contains("EventName"))
                    EventTableData.Columns["EventName"].HeaderText = "Name";
                if (EventTableData.Columns.Contains("TeamGroup"))
                    EventTableData.Columns["TeamGroup"].HeaderText = "Team";
                if (EventTableData.Columns.Contains("TimeStart"))
                    EventTableData.Columns["TimeStart"].HeaderText = "Start";
                if (EventTableData.Columns.Contains("TimeEnd"))
                    EventTableData.Columns["TimeEnd"].HeaderText = "End";
                if (EventTableData.Columns.Contains("description"))
                    EventTableData.Columns["description"].HeaderText = "Description";

                if (EventTableData.Columns.Contains("EventID"))
                    EventTableData.Columns["EventID"].HeaderText = "Event ID";
                if (EventTableData.Columns.Contains("EventName"))
                    EventTableData.Columns["EventName"].HeaderText = "Name";
                if (EventTableData.Columns.Contains("TeamGroup"))
                    EventTableData.Columns["TeamGroup"].HeaderText = "Team";
                if (EventTableData.Columns.Contains("TimeStart"))
                    EventTableData.Columns["TimeStart"].HeaderText = "Start";
                if (EventTableData.Columns.Contains("TimeEnd"))
                    EventTableData.Columns["TimeEnd"].HeaderText = "End";
                if (EventTableData.Columns.Contains("description"))
                    EventTableData.Columns["description"].HeaderText = "Description";

                if (EventTableData.Columns.Contains("EventID"))
                    EventTableData.Columns["EventID"].HeaderText = "Event ID";
                if (EventTableData.Columns.Contains("EventName"))
                    EventTableData.Columns["EventName"].HeaderText = "Name";
                if (EventTableData.Columns.Contains("TeamGroup"))
                    EventTableData.Columns["TeamGroup"].HeaderText = "Team";
                if (EventTableData.Columns.Contains("TimeStart"))
                    EventTableData.Columns["TimeStart"].HeaderText = "Start";
                if (EventTableData.Columns.Contains("TimeEnd"))
                    EventTableData.Columns["TimeEnd"].HeaderText = "End";
                if (EventTableData.Columns.Contains("description"))
                    EventTableData.Columns["description"].HeaderText = "Description";

                if (EventTableData.Columns.Contains("EventID"))
                    EventTableData.Columns["EventID"].HeaderText = "Event ID";
                if (EventTableData.Columns.Contains("EventName"))
                    EventTableData.Columns["EventName"].HeaderText = "Name";
                if (EventTableData.Columns.Contains("TeamGroup"))
                    EventTableData.Columns["TeamGroup"].HeaderText = "Team";
                if (EventTableData.Columns.Contains("TimeStart"))
                    EventTableData.Columns["TimeStart"].HeaderText = "Start";
                if (EventTableData.Columns.Contains("TimeEnd"))
                    EventTableData.Columns["TimeEnd"].HeaderText = "End";
                if (EventTableData.Columns.Contains("description"))
                    EventTableData.Columns["description"].HeaderText = "Description";

                ConfigureHistoryGridForAudit();
            }
        }

        private void UpdateTotalEventLabel()
        {
            if (EventTableData.DataSource is DataTable dt)
            {
                var uniqueEventCount = dt.AsEnumerable()
                .Select(row => row.Field<string>("EventName"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

                lblTotalEvent.Text = uniqueEventCount.ToString();
            }
        }

        private void UpdateEventDropdown()
        {
            cbEvent.Items.Clear();

            if (EventTableData.DataSource is DataTable dt)
            {
                var uniqueEventNames = dt.AsEnumerable()
                .Select(row => row.Field<string>("EventName"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

                foreach (string eventName in uniqueEventNames)
                {
                    cbEvent.Items.Add(eventName);
                }
            }
        }

        private void FilterAndPopulateTeamDropdown(string selectedEventName)
        {
            cbTeam.Items.Clear();

            if (string.IsNullOrWhiteSpace(selectedEventName) || !(EventTableData.DataSource is DataTable dt))
            {
                return;
            }

            var filteredTeamNames = dt.AsEnumerable()
            .Where(row => row.Field<string>("EventName")
            .Equals(selectedEventName, StringComparison.OrdinalIgnoreCase))
            .Select(row => row.Field<string>("TeamGroup"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

            foreach (string teamName in filteredTeamNames)
            {
                cbTeam.Items.Add(teamName);
            }
        }

        private void cbEvent_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbEvent.SelectedItem != null)
            {
                string selectedEvent = cbEvent.SelectedItem.ToString();
                FilterAndPopulateTeamDropdown(selectedEvent);
            }
            else
            {
                cbTeam.Items.Clear();
            }
        }

        private void btnCreateEvent_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbEventName.Text) || string.IsNullOrWhiteSpace(tbTeam.Text))
            {
                MessageBox.Show("Please fill in Event Name and Group.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();

                    using (var dup = new SqlCommand(
                        @"SELECT COUNT(1) 
                          FROM dbo.EventTb 
                          WHERE EventName = @EventName AND TeamGroup = @TeamGroup;", con))
                    {
                        dup.Parameters.AddWithValue("@EventName", tbEventName.Text.Trim());
                        dup.Parameters.AddWithValue("@TeamGroup", tbTeam.Text.Trim());

                        var exists = (int)dup.ExecuteScalar() > 0;
                        if (exists)
                        {
                            MessageBox.Show("This Event Name and Group already exist. Please choose a different group name.");
                            return;
                        }
                    }

                    using (var cmd = new SqlCommand(
                        @"INSERT INTO dbo.EventTb (EventName, TeamGroup, TimeStart, TimeEnd, [description])
                          VALUES (@EventName, @TeamGroup, @TimeStart, @TimeEnd, @description);
                          SELECT CAST(SCOPE_IDENTITY() AS int);", con))
                    {
                        cmd.Parameters.AddWithValue("@EventName", tbEventName.Text.Trim());
                        cmd.Parameters.AddWithValue("@TeamGroup", tbTeam.Text.Trim());
                        cmd.Parameters.AddWithValue("@TimeStart", dtpTimeStart.Value);
                        cmd.Parameters.AddWithValue("@TimeEnd", dtpTimeEnd.Value);
                        cmd.Parameters.AddWithValue("@description", tbDesc.Text.Trim());

                        var newId = (int)cmd.ExecuteScalar();
                        tbEventID.Text = newId.ToString();
                    }
                }

                MessageBox.Show("New Event created successfully and saved to database.");
                LoadEventsData();
                UpdateTotalEventLabel();
                UpdateEventDropdown();
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                MessageBox.Show("This Event Name and Group already exist. Please choose a different group name.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating event: " + ex.Message);
            }

            ClearFields();
        }

        private void btnUpdateEvent_Click_1(object sender, EventArgs e)
        {
            if (EventTableData.SelectedRows.Count == 0 || string.IsNullOrWhiteSpace(tbEventID.Text))
            {
                MessageBox.Show("Please select an event to update, and ensure Event ID is loaded.");
                return;
            }
            if (string.IsNullOrWhiteSpace(tbEventName.Text) || string.IsNullOrWhiteSpace(tbTeam.Text))
            {
                MessageBox.Show("Please fill in Event Name and Group.");
                return;
            }
            if (!int.TryParse(tbEventID.Text, out int eventIdToUpdate))
            {
                MessageBox.Show("Invalid Event ID format.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();

                    using (var dup = new SqlCommand(
                        @"SELECT COUNT(1) 
                          FROM dbo.EventTb 
                          WHERE EventID <> @EventID AND EventName = @EventName AND TeamGroup = @TeamGroup;", con))
                    {
                        dup.Parameters.AddWithValue("@EventID", eventIdToUpdate);
                        dup.Parameters.AddWithValue("@EventName", tbEventName.Text.Trim());
                        dup.Parameters.AddWithValue("@TeamGroup", tbTeam.Text.Trim());

                        var exists = (int)dup.ExecuteScalar() > 0;
                        if (exists)
                        {
                            MessageBox.Show("Another event already uses this Event Name and Group.");
                            return;
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(
                        @"UPDATE dbo.EventTb 
                          SET [description]=@description, EventName=@NewEventName, TeamGroup=@TeamGroup, 
                              TimeStart=@TimeStart, TimeEnd=@TimeEnd 
                          WHERE EventID=@EventID", con))
                    {
                        cmd.Parameters.AddWithValue("@EventID", eventIdToUpdate);
                        cmd.Parameters.AddWithValue("@NewEventName", tbEventName.Text.Trim());
                        cmd.Parameters.AddWithValue("@TeamGroup", tbTeam.Text.Trim());
                        cmd.Parameters.AddWithValue("@TimeStart", dtpTimeStart.Value);
                        cmd.Parameters.AddWithValue("@TimeEnd", dtpTimeEnd.Value);
                        cmd.Parameters.AddWithValue("@description", tbDesc.Text.Trim());

                        int rowsAffected = cmd.ExecuteNonQuery();
                        MessageBox.Show(rowsAffected > 0 ? "Event updated successfully in the database."
                                                 : "No event found with the selected ID to update.");

                        LogActivity(
                            "UpdateEvent",
                            $"User {VotingSystem.Program.StudentID} updated event '{tbEventName.Text.Trim()}' (Team '{tbTeam.Text.Trim()}').",
                            tbEventName.Text.Trim(),
                            tbTeam.Text.Trim());
                    }

                    LoadEventsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                MessageBox.Show("Another event already uses this Event Name and Group.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating event: " + ex.Message);
            }
            ClearFields();
        }

        private void btnDeleteEvent_Click_1(object sender, EventArgs e)
        {
            if (!int.TryParse(tbEventID.Text, out var eventId) ||
                string.IsNullOrWhiteSpace(tbEventName.Text) ||
                string.IsNullOrWhiteSpace(tbTeam.Text))
            {
                MessageBox.Show("Please select an event row (ID, Name, and Team) to delete.", 
                    "Missing selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string eventName = tbEventName.Text.Trim();
            string teamName  = tbTeam.Text.Trim();

            var confirm = MessageBox.Show(
                $"Delete event?\n\nID: {eventId}\nName: {eventName}\nTeam: {teamName}\n\n" +
                "This will also remove related participants and history for this event/team.",
                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                using (var con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    using (var tx = con.BeginTransaction())
                    {
                        try
                        {
                            using (var cmdP = new SqlCommand(
                                @"DELETE FROM dbo.Participants 
                                  WHERE [Event] = @EventName AND [Team] = @TeamName", con, tx))
                            {
                                cmdP.Parameters.AddWithValue("@EventName", eventName);
                                cmdP.Parameters.AddWithValue("@TeamName", teamName);
                                cmdP.ExecuteNonQuery();
                            }

                            using (var cmdH = new SqlCommand(
                                @"DELETE FROM dbo.History 
                                  WHERE [EventName] = @EventName AND [TeamName] = @TeamName", con, tx))
                            {
                                cmdH.Parameters.AddWithValue("@EventName", eventName);
                                cmdH.Parameters.AddWithValue("@TeamName", teamName);
                                cmdH.ExecuteNonQuery();
                            }

                            using (var cmdE = new SqlCommand(
                                @"DELETE FROM dbo.EventTb WHERE EventID = @EventID", con, tx))
                            {
                                cmdE.Parameters.AddWithValue("@EventID", eventId);
                                int rows = cmdE.ExecuteNonQuery();
                                if (rows == 0)
                                {
                                    tx.Rollback();
                                    MessageBox.Show("No event found with the selected ID.", 
                                        "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }

                LogActivity(
                    "DeleteEvent",
                    $"User {this.CurrentUser} deleted event '{eventName}' (Team '{teamName}', ID {eventId}).",
                    eventName, teamName);

                MessageBox.Show("Event deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadEventsData();
                UpdateTotalEventLabel();
                UpdateEventDropdown();
                ClearFields();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Database error deleting event: " + ex.Message, 
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting event: " + ex.Message, 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EventTableData_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < EventTableData.Rows.Count)
            {
                DataGridViewRow row = EventTableData.Rows[e.RowIndex];

                if (EventTableData.Columns.Contains("EventID") && row.Cells["EventID"].Value != null && row.Cells["EventID"].Value != DBNull.Value)
                    tbEventID.Text = row.Cells["EventID"].Value.ToString();
                else
                    tbEventID.Text = "No data";

                if (EventTableData.Columns.Contains("EventName") && row.Cells["EventName"].Value != null && row.Cells["EventName"].Value != DBNull.Value)
                    tbEventName.Text = row.Cells["EventName"].Value.ToString();

                if (EventTableData.Columns.Contains("description") && row.Cells["description"].Value != null && row.Cells["description"].Value != DBNull.Value)
                    tbDesc.Text = row.Cells["description"].Value.ToString();

                if (EventTableData.Columns.Contains("TeamGroup") && row.Cells["TeamGroup"].Value != null && row.Cells["TeamGroup"].Value != DBNull.Value)
                    tbTeam.Text = row.Cells["TeamGroup"].Value.ToString();

                if (EventTableData.Columns.Contains("TimeStart") && row.Cells["TimeStart"].Value != null &&
                DateTime.TryParse(row.Cells["TimeStart"].Value.ToString(), out DateTime start))
                    dtpTimeStart.Value = start;
                else
                    dtpTimeStart.Value = DateTime.Now;

                if (EventTableData.Columns.Contains("TimeEnd") && row.Cells["TimeEnd"].Value != null &&
                DateTime.TryParse(row.Cells["TimeEnd"].Value.ToString(), out DateTime end))
                    dtpTimeEnd.Value = end;
                else
                    dtpTimeEnd.Value = DateTime.Now;

            }
        }

        private void LoadParticipantsData(string searchKey = "", bool voterData = false)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                if (voterData && !string.IsNullOrWhiteSpace(searchKey))
                {
                    using (var cmd = new SqlCommand(
                        @"SELECT StudentNo, LastName, FirstName, MiddleName, Program
                  FROM dbo.Voters
                  WHERE StudentNo = @StudentNo", con))
                    {
                        cmd.Parameters.AddWithValue("@StudentNo", searchKey.Trim());

                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                tbStudentNum.Text = r["StudentNo"]?.ToString();
                                tbLastName.Text = r["LastName"]?.ToString();
                                tbFirstName.Text = r["FirstName"]?.ToString();
                                tbMiddleName.Text = r["MiddleName"] == DBNull.Value ? string.Empty : r["MiddleName"].ToString();
                                cbProgram.Text = r["Program"] == DBNull.Value ? string.Empty : r["Program"].ToString();
                            }
                        }
                    }
                    return;
                }

                using (var cmd = new SqlCommand(
                    @"SELECT StudentNo, LastName, FirstName, MiddleName, Program, Team, Event, position
              FROM dbo.Participants
              WHERE (@k = '' OR StudentNo LIKE '%' + @k + '%')", con))
                {
                    cmd.Parameters.AddWithValue("@k", searchKey ?? string.Empty);

                    var da = new SqlDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    TableParticipant.DataSource = dt;
                }
            }
        }

        private void ClearFields()
        {
            tbStudentNum.Text = string.Empty;
            tbLastName.Text = string.Empty;
            tbFirstName.Text = string.Empty;
            tbMiddleName.Text = string.Empty;
            tbPosition.Text = string.Empty;
            cbProgram.SelectedIndex = -1;
            cbProgram.Text = string.Empty;

            tbEventID.Text = "No data";
            tbEventName.Text = string.Empty;
            tbTeam.Text = string.Empty;
            tbDesc.Text = string.Empty;
            dtpTimeStart.Value = DateTime.Now;
            dtpTimeEnd.Value = DateTime.Now;
            cbEvent.SelectedIndex = -1;
            cbEvent.Text = string.Empty;
            cbTeam.Items.Clear();
            cbTeam.SelectedIndex = -1;
            cbTeam.Text = string.Empty;

            ClearAccountFields();
            currentAccountStudID = string.Empty;

            guna2TextBox1.Text = string.Empty;
            guna2TextBox2.Text = string.Empty;
            guna2TextBox3.Text = string.Empty; 
            tbSearchEventName.Text = string.Empty;

            chartEvent.Series.Clear();
            chartEvent.Titles.Clear();

            if (EventTableData != null) EventTableData.ClearSelection();
            if (TableParticipant != null) TableParticipant.ClearSelection();
            if (votersData != null) votersData.ClearSelection();

            currentStudID = string.Empty;
            currentStudEvent = string.Empty;
            currentStudTeam = string.Empty;

            PositionTogglePasswordButton();
        }

        private void TableParticipant_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < TableParticipant.Rows.Count)
            {
                DataGridViewRow row = TableParticipant.Rows[e.RowIndex];

                tbStudentNum.Text = row.Cells["StudentNo"].Value?.ToString();
                currentStudID = tbStudentNum.Text;
                tbLastName.Text = row.Cells["LastName"].Value?.ToString();
                tbFirstName.Text = row.Cells["FirstName"].Value?.ToString();
                tbMiddleName.Text = row.Cells["MiddleName"].Value?.ToString();
                cbProgram.Text = row.Cells["Program"].Value?.ToString();
                tbPosition.Text = row.Cells["position"].Value?.ToString();

                string eventName = row.Cells["Event"].Value?.ToString().Trim();
                string teamName = row.Cells["Team"].Value?.ToString().Trim();

                currentStudEvent = eventName ?? string.Empty;
                currentStudTeam = teamName ?? string.Empty;

                int eventIndex = cbEvent.Items.IndexOf(eventName);
                cbEvent.SelectedIndex = eventIndex >= 0 ? eventIndex : -1;

                if (!string.IsNullOrEmpty(teamName))
                {
                    if (cbEvent.SelectedItem != null)
                        FilterAndPopulateTeamDropdown(cbEvent.SelectedItem.ToString());

                    int teamIndex = cbTeam.Items.IndexOf(teamName);
                    cbTeam.SelectedIndex = teamIndex >= 0 ? teamIndex : -1;
                }
                else
                {
                    cbTeam.SelectedIndex = -1;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbStudentNum.Text) ||
                string.IsNullOrWhiteSpace(tbLastName.Text) ||
                string.IsNullOrWhiteSpace(tbFirstName.Text) ||
                string.IsNullOrWhiteSpace(cbProgram.Text) ||
                string.IsNullOrWhiteSpace(cbEvent.Text) ||
                string.IsNullOrWhiteSpace(cbTeam.Text))
            {
                MessageBox.Show(
                    "Please fill out all required fields: Student Number, Last Name, First Name, Program, Event, and Team.",
                    "Missing Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();

                    using (var dup = new SqlCommand(
                        @"SELECT COUNT(1)
                          FROM dbo.Participants
                          WHERE StudentNo=@StudentNo AND Event=@Event AND Team=@Team", con))
                    {
                        dup.Parameters.AddWithValue("@StudentNo", tbStudentNum.Text.Trim());
                        dup.Parameters.AddWithValue("@Event", cbEvent.Text.Trim());
                        dup.Parameters.AddWithValue("@Team", cbTeam.Text.Trim());

                        if ((int)dup.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("This student is already registered for the same Event and Team.",
                                "Duplicate membership", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(
                        @"INSERT INTO [dbo].[Participants]
                          ([StudentNo],[LastName],[FirstName],[MiddleName],[Program],[Event],[Team],[position])
                          VALUES (@studID,@tbLastName,@tbFirstName,@tbMiddleName,@cbProgram,@cbEvent,@cbTeam,@position)", con))
                    {
                        cmd.Parameters.AddWithValue("@studID", tbStudentNum.Text.Trim());
                        cmd.Parameters.AddWithValue("@tbLastName", tbLastName.Text.Trim());
                        cmd.Parameters.AddWithValue("@tbFirstName", tbFirstName.Text.Trim());
                        cmd.Parameters.AddWithValue("@tbMiddleName", (object)tbMiddleName.Text.Trim() ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cbProgram", cbProgram.Text.Trim());
                        cmd.Parameters.AddWithValue("@cbEvent", cbEvent.Text.Trim());
                        cmd.Parameters.AddWithValue("@cbTeam", cbTeam.Text.Trim());
                        cmd.Parameters.AddWithValue("@position", (object)tbPosition.Text ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Successfully saved participant membership.");
                    LogActivity("AddParticipant",
                        $"User {this.CurrentUser} registered '{tbStudentNum.Text.Trim()}' to '{cbEvent.Text}' - '{cbTeam.Text}'.",
                        cbEvent.Text, cbTeam.Text);

                    LoadParticipantsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding participant: " + ex.Message);
            }

            ClearFields();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbStudentNum.Text) ||
        string.IsNullOrWhiteSpace(tbLastName.Text) ||
        string.IsNullOrWhiteSpace(tbFirstName.Text) ||
        string.IsNullOrWhiteSpace(cbProgram.Text) ||
        string.IsNullOrWhiteSpace(cbEvent.Text) ||
        string.IsNullOrWhiteSpace(cbTeam.Text) ||
        string.IsNullOrWhiteSpace(currentStudID) ||
        string.IsNullOrWhiteSpace(currentStudEvent) ||
        string.IsNullOrWhiteSpace(currentStudTeam))
            {
                MessageBox.Show("Please select a membership (row) to update, and fill out all required fields.",
                    "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();

                    using (var dup = new SqlCommand(
                        @"SELECT COUNT(1)
                  FROM dbo.Participants
                  WHERE StudentNo=@NewStudentNo AND Event=@NewEvent AND Team=@NewTeam
                    AND NOT (StudentNo=@OldStudentNo AND Event=@OldEvent AND Team=@OldTeam)", con))
                    {
                        dup.Parameters.AddWithValue("@NewStudentNo", tbStudentNum.Text.Trim());
                        dup.Parameters.AddWithValue("@NewEvent", cbEvent.Text.Trim());
                        dup.Parameters.AddWithValue("@NewTeam", cbTeam.Text.Trim());
                        dup.Parameters.AddWithValue("@OldStudentNo", currentStudID.Trim());
                        dup.Parameters.AddWithValue("@OldEvent", currentStudEvent.Trim());
                        dup.Parameters.AddWithValue("@OldTeam", currentStudTeam.Trim());

                        if ((int)dup.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Another membership already exists with the same Student, Event and Team.",
                                "Duplicate membership", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(
                        @"UPDATE [dbo].[Participants] SET
                        [StudentNo] = @studID,
                        [LastName]  = @tbLastName,
                        [FirstName] = @tbFirstName,
                        [MiddleName]= @tbMiddleName,
                        [Program]   = @cbProgram,
                        [Event]     = @cbEvent,
                        [Team]      = @cbTeam,
                        [position]  = @position
                  WHERE [StudentNo] = @currentStudID AND [Event] = @currentEvent AND [Team] = @currentTeam", con))
                    {
                        cmd.Parameters.AddWithValue("@studID", tbStudentNum.Text.Trim());
                        cmd.Parameters.AddWithValue("@tbLastName", tbLastName.Text.Trim());
                        cmd.Parameters.AddWithValue("@tbFirstName", tbFirstName.Text.Trim());
                        cmd.Parameters.AddWithValue("@tbMiddleName", (object)tbMiddleName.Text.Trim() ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cbProgram", cbProgram.Text.Trim());
                        cmd.Parameters.AddWithValue("@cbEvent", cbEvent.Text.Trim());
                        cmd.Parameters.AddWithValue("@cbTeam", cbTeam.Text.Trim());
                        cmd.Parameters.AddWithValue("@position", (object)tbPosition.Text ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@currentStudID", currentStudID.Trim());
                        cmd.Parameters.AddWithValue("@currentEvent", currentStudEvent.Trim());
                        cmd.Parameters.AddWithValue("@currentTeam", currentStudTeam.Trim());

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            LogActivity("UpdateParticipant",
                                $"User {this.CurrentUser} updated membership '{currentStudID} | {currentStudEvent} | {currentStudTeam}' " +
                                $"-> '{tbStudentNum.Text.Trim()} | {cbEvent.Text} | {cbTeam.Text}'.",
                                cbEvent.Text, cbTeam.Text);
                            MessageBox.Show("Successfully updated participant membership.");
                        }
                        else
                        {
                            MessageBox.Show("No membership found to update.", "Update Failed",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    LoadParticipantsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();

                    currentStudID = tbStudentNum.Text.Trim();
                    currentStudEvent = cbEvent.Text.Trim();
                    currentStudTeam = cbTeam.Text.Trim();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating participant: " + ex.Message);
            }

            ClearFields();
        }

        private void btnDelete_Click_1(object sender, EventArgs e)
        {

            if (string.IsNullOrWhiteSpace(tbStudentNum.Text) || string.IsNullOrWhiteSpace(currentStudID))
            {
                MessageBox.Show("Please select a participant to delete.", "Missing ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult dialogResult = MessageBox.Show(
            $"Are you sure you want to permanently delete the record for Student ID: {tbStudentNum.Text.Trim()}?",
            "Confirm Deletion",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
            );

            if (dialogResult == DialogResult.No)
            {
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(
                    "DELETE FROM [dbo].[Participants] WHERE [StudentNo] = @studID", con);

                    cmd.Parameters.AddWithValue("@studID", tbStudentNum.Text.Trim());

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        LogActivity(
                            "DeleteParticipant",
                            $"User {this.CurrentUser} deleted participant '{tbStudentNum.Text.Trim()}'.",
                            null,
                            null);
                        MessageBox.Show("Successfully deleted participant details.");
                    }
                    else
                    {
                        MessageBox.Show("No participant found with that Student ID.", "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    LoadParticipantsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting participant: " + ex.Message);
            }
            ClearFields();
        }

        private void btnAccountAdd_Click(object sender, EventArgs e)
        {
            bool isStudent = (cbRoleAcc.SelectedItem?.ToString() == "Student");

            if (string.IsNullOrWhiteSpace(tbStudentNumberAcc.Text) ||
                string.IsNullOrWhiteSpace(tbPassword.Text) ||
                string.IsNullOrWhiteSpace(cbRoleAcc.Text) ||
                string.IsNullOrWhiteSpace(tbLastNameAcc.Text) ||
                string.IsNullOrWhiteSpace(tbFirstNameAcc.Text))
            {
                MessageBox.Show("Please fill out all required fields (Student No, Password, Role, Last Name, First Name).",
                    "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (isStudent && (cbProgramAcc.SelectedIndex == -1 || string.IsNullOrWhiteSpace(cbProgramAcc.Text)))
            {
                MessageBox.Show("Please select a Program. This is required for Student roles.",
                    "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO [dbo].[Voters] ([StudentNo], [Password], [Role], [LastName], [FirstName], [MiddleName], [Program], [IsActive]) " +
                        "VALUES (@StudentNo, @Password, @Role, @LastName, @FirstName, @MiddleName, @Program, 0)", con); 

                    cmd.Parameters.AddWithValue("@StudentNo", tbStudentNumberAcc.Text.Trim());
                    cmd.Parameters.AddWithValue("@Password", tbPassword.Text);
                    cmd.Parameters.AddWithValue("@Role", cbRoleAcc.Text);
                    cmd.Parameters.AddWithValue("@LastName", tbLastNameAcc.Text.Trim());
                    cmd.Parameters.AddWithValue("@FirstName", tbFirstNameAcc.Text.Trim());

                    if (string.IsNullOrWhiteSpace(tbMiddleNameAcc.Text))
                    {
                        cmd.Parameters.AddWithValue("@MiddleName", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@MiddleName", tbMiddleNameAcc.Text.Trim());
                    }

                    if (isStudent)
                    {
                        cmd.Parameters.AddWithValue("@Program", cbProgramAcc.Text);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Program", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("✅ Account successfully added and saved to database.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LogActivity(
                        "CreateAccount",
                        $"User {this.CurrentUser} created account '{tbStudentNumberAcc.Text.Trim()}' (Role: {cbRoleAcc.Text}).",
                        null,
                        null);

                    LoadVotersData();
                    ClearAccountFields();
                }
            }
            catch (SqlException sqlex)
            {
                if (sqlex.Number == 2627)
                {
                    MessageBox.Show($"❌ Error: Student Number '{tbStudentNumberAcc.Text.Trim()}' already exists.", "Duplicate Student ID", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (sqlex.Number == 515)
                {
                    MessageBox.Show("Database Error: Cannot insert a required value (likely Program) as NULL. Please ensure your 'Program' column allows NULL for non-students.", "Database Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Database Error: " + sqlex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAccountUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentAccountStudID))
            {
                MessageBox.Show("Please select an account from the grid to update.", "No Account Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool isStudent = (cbRoleAcc.SelectedItem?.ToString() == "Student");
            bool programInvalid = (isStudent && string.IsNullOrWhiteSpace(cbProgramAcc.Text));

            if (string.IsNullOrWhiteSpace(tbStudentNumberAcc.Text) ||
                string.IsNullOrWhiteSpace(cbRoleAcc.Text) ||
                string.IsNullOrWhiteSpace(tbLastNameAcc.Text) ||
                string.IsNullOrWhiteSpace(tbFirstNameAcc.Text) ||
                programInvalid)
            {
                MessageBox.Show("Please fill out all required fields. 'Program' is only required for 'Student' roles.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();

                    string query = @"UPDATE [dbo].[Voters] SET 
                                StudentNo = @NewStudentNo, 
                                Role = @Role, 
                                LastName = @LastName, 
                                FirstName = @FirstName, 
                                MiddleName = @MiddleName, 
                                Program = @Program";

                    if (!string.IsNullOrWhiteSpace(tbPassword.Text))
                    {
                        query += ", Password = @Password";
                    }

                    query += " WHERE StudentNo = @OriginalStudentNo";

                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@NewStudentNo", tbStudentNumberAcc.Text.Trim());
                    cmd.Parameters.AddWithValue("@Role", cbRoleAcc.Text);
                    cmd.Parameters.AddWithValue("@LastName", tbLastNameAcc.Text.Trim());
                    cmd.Parameters.AddWithValue("@FirstName", tbFirstNameAcc.Text.Trim());

                    if (string.IsNullOrWhiteSpace(tbMiddleNameAcc.Text))
                        cmd.Parameters.AddWithValue("@MiddleName", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@MiddleName", tbMiddleNameAcc.Text.Trim());

                    if (isStudent)
                        cmd.Parameters.AddWithValue("@Program", cbProgramAcc.Text);
                    else
                        cmd.Parameters.AddWithValue("@Program", DBNull.Value);

                    if (!string.IsNullOrWhiteSpace(tbPassword.Text))
                    {
                        cmd.Parameters.AddWithValue("@Password", tbPassword.Text);
                    }

                    cmd.Parameters.AddWithValue("@OriginalStudentNo", currentAccountStudID);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        LogActivity(
                            "UpdateAccount",
                            $"User {this.CurrentUser} updated account '{currentAccountStudID}' -> '{tbStudentNumberAcc.Text.Trim()}' (Role: {cbRoleAcc.Text}).",
                            null,
                            null);
                        MessageBox.Show("✅ Account successfully updated.");
                        LoadVotersData();
                        ClearAccountFields();
                    }
                    else
                    {
                        MessageBox.Show("Update failed. Account not found or no changes were made.", "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (SqlException sqlex)
            {
                if (sqlex.Number == 2627)
                {
                    MessageBox.Show($"❌ Error: Student Number '{tbStudentNumberAcc.Text.Trim()}' already exists.", "Duplicate Student ID", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Database Error: " + sqlex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btnAccountDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentAccountStudID))
            {
                MessageBox.Show("Please select an account from the grid to delete.", "No Account Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to permanently delete the account for '{tbFirstNameAcc.Text} {tbLastNameAcc.Text}' (ID: {currentAccountStudID})?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.No)
            {
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    string query = "DELETE FROM [dbo].[Voters] WHERE StudentNo = @StudentNo";

                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@StudentNo", currentAccountStudID);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        LogActivity(
                            "DeleteAccount",
                            $"User {this.CurrentUser} deleted account '{currentAccountStudID}'.",
                            null,
                            null);
                        MessageBox.Show("✅ Account successfully deleted.");
                        LoadVotersData();
                        ClearAccountFields();
                    }
                    else
                    {
                        MessageBox.Show("No account was found with that ID to delete.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (SqlException sqlex)
            {

                if (sqlex.Number == 547)
                {
                    MessageBox.Show("❌ Error: Cannot delete this account. The user may be linked to other records (e.g., votes cast).", "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Database Error: " + sqlex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsureAuditLogTable()
        {
            try
            {
                using (var con = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLog' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AuditLog(
        LogId       INT IDENTITY(1,1) PRIMARY KEY,
        StudentNo   INT NULL,
        [Action]    NVARCHAR(100) NOT NULL,
        [Details]   NVARCHAR(1000) NULL,
        EventName   NVARCHAR(255) NULL,
        TeamName    NVARCHAR(255) NULL,
        OccurredAt  DATETIME NOT NULL CONSTRAINT DF_AuditLog_OccurredAt DEFAULT (GETDATE())
    );
END", con))
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
            }
        }

        private void LogActivity(string action, string details, string eventName = null, string teamName = null)
        {
            try
            {
                using (var con = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(
                    @"INSERT INTO dbo.AuditLog(StudentNo,[Action],[Details],EventName,TeamName)
                      VALUES (@StudentNo,@Action,@Details,@EventName,@TeamName)", con))
                {
                    cmd.Parameters.AddWithValue("@StudentNo", this.CurrentUser);
                    cmd.Parameters.AddWithValue("@Action", action ?? "Unknown");
                    cmd.Parameters.AddWithValue("@Details", (object)details ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EventName", string.IsNullOrWhiteSpace(eventName) ? (object)DBNull.Value : eventName);
                    cmd.Parameters.AddWithValue("@TeamName", string.IsNullOrWhiteSpace(teamName) ? (object)DBNull.Value : teamName);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
            }
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            ClearFields();
            dashboardP.Visible = true;
            ListEventPanel.Visible = false;
            mPartcipant.Visible = false;
            AccountPanel.Visible = false;
            HistoryPanel.Visible = false;
            dashboardP.BringToFront();
        }

        private void btnAccount_Click(object sender, EventArgs e)
        {
            ClearFields();
            AccountPanel.Visible = true;
            dashboardP.Visible = false;
            ListEventPanel.Visible = false;
            mPartcipant.Visible = false;
            HistoryPanel.Visible = false;
            AccountPanel.BringToFront();
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            ClearFields();
            HistoryPanel.Visible = true;
            dashboardP.Visible = false;
            ListEventPanel.Visible = false;
            mPartcipant.Visible = false;
            AccountPanel.Visible = false;
            HistoryPanel.BringToFront();
            LoadHistoryData();
        }

        private void btnSignOut_Click(object sender, EventArgs e)
        {
            var loginForm = new LoginForm();
            loginForm.Show();
            this.Close();
        }

        private void btnEvents_Click(object sender, EventArgs e)
        {
            ClearFields();
            mPartcipant.Visible = false;
            ListEventPanel.Visible = true;
            dashboardP.Visible = false;
            HistoryPanel.Visible = false;
            AccountPanel.Visible = false;
            LoadEventsData();
        }

        private void btnPaticipant_Click(object sender, EventArgs e)
        {
            ClearFields();
            mPartcipant.Visible = true;
            ListEventPanel.Visible = false;
            dashboardP.Visible = false;
            HistoryPanel.Visible = false;
            AccountPanel.Visible = false;
            LoadParticipantsData();
        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            LoadParticipantsData(guna2TextBox1.Text);
        }

        private void guna2TextBox2_TextChanged(object sender, EventArgs e)
        {
            LoadEventsData(guna2TextBox2.Text);
        }

        private void guna2TextBox3_TextChanged(object sender, EventArgs e)
        {
            LoadVotersData(guna2TextBox3.Text);
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            var header = new System.Windows.Forms.DataVisualization.Charting.Title
            {
                Text = "Polling Result",
                Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top,
                Alignment = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.Black
            };

            var dateTitle = new System.Windows.Forms.DataVisualization.Charting.Title
            {
                Text = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt"),
                Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top,
                Alignment = ContentAlignment.TopRight,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.Black
            };

            chartEvent.Titles.Insert(0, header);
            chartEvent.Titles.Add(dateTitle);

            try
            {
                chartEvent.Printing.PrintPreview();
            }
            finally
            {
                chartEvent.Titles.Remove(header);
                chartEvent.Titles.Remove(dateTitle);
                header.Dispose();
                dateTitle.Dispose();
            }
        }

        private void tbStudentNum_TextChanged(object sender, EventArgs e)
        {
            LoadParticipantsData(tbStudentNum.Text, true);
        }

        private void UpdateProgramVisibility()
        {
            var selectedRole = cbRoleAcc.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedRole))
                selectedRole = cbRoleAcc.Text;

            bool showProgram = string.Equals(selectedRole, "Student", StringComparison.OrdinalIgnoreCase)
                               || string.IsNullOrWhiteSpace(selectedRole);

            lblProgram.Visible = showProgram;
            cbProgramAcc.Visible = showProgram;
            cbProgramAcc.Enabled = showProgram;

            if (!showProgram)
            {
                cbProgramAcc.SelectedIndex = -1;
                cbProgramAcc.Text = string.Empty;
            }
        }

        private void cbRoleAcc_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateProgramVisibility();
        }

        private void SearchEventAndDisplayChart()
        {
            if (string.IsNullOrWhiteSpace(tbSearchEventName.Text))
            {
                chartEvent.Series.Clear();
                chartEvent.Titles.Clear();
                return;
            }

            string eventNameFilter = tbSearchEventName.Text.Trim();

            const string sqlPositions = @"
        SELECT DISTINCT LTRIM(RTRIM(p.position)) AS Position
        FROM dbo.Participants p
        WHERE p.[Event] = @EventName
          AND p.position IS NOT NULL
          AND LTRIM(RTRIM(p.position)) <> ''
        ORDER BY Position";

            const string sqlVotesPerTeamForPosition = @"
        SELECT 
            e.TeamGroup AS Team,
            COUNT(h.TeamName) AS VoteCount
        FROM dbo.EventTb e
        LEFT JOIN dbo.History h
               ON e.EventName = h.EventName
              AND e.TeamGroup = h.TeamName
              AND h.Position = @Position
        WHERE e.EventName = @EventName
        GROUP BY e.TeamGroup
        ORDER BY VoteCount DESC";

            const string sqlVotesPerTeamNoPosition = @"
        SELECT 
            e.TeamGroup AS Team,
            COUNT(h.TeamName) AS VoteCount
        FROM dbo.EventTb e
        LEFT JOIN dbo.History h
               ON e.EventName = h.EventName
              AND e.TeamGroup = h.TeamName
        WHERE e.EventName = @EventName
        GROUP BY e.TeamGroup
        ORDER BY VoteCount DESC";

            const string sqlTotalVotesForEvent = @"
        SELECT COUNT(*) 
        FROM dbo.History 
        WHERE EventName = @EventName";

            const string sqlDistinctTeams = @"
        SELECT DISTINCT e.TeamGroup AS Team
        FROM dbo.EventTb e
        WHERE e.EventName = @EventName
        ORDER BY e.TeamGroup";

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmdPos   = new SqlCommand(sqlPositions, conn))
    using (var cmdTotal = new SqlCommand(sqlTotalVotesForEvent, conn))
    using (var cmdTeams = new SqlCommand(sqlDistinctTeams, conn))
            {
                chartEvent.Series.Clear();
                chartEvent.Titles.Clear();
                chartEvent.ChartAreas.Clear();
                chartEvent.Legends.Clear();

                var legend = new Legend("Legend1") { Docking = Docking.Right, BackColor = Color.Transparent };
                chartEvent.Legends.Add(legend);
                chartEvent.Palette = ChartColorPalette.None;

                cmdPos.Parameters.AddWithValue("@EventName", eventNameFilter);
                cmdTotal.Parameters.AddWithValue("@EventName", eventNameFilter);
                cmdTeams.Parameters.AddWithValue("@EventName", eventNameFilter);

                try
                {
                    conn.Open();

                    int totalVotes = Convert.ToInt32(cmdTotal.ExecuteScalar());
                    var globalTitle = new Title($"{eventNameFilter} - Total Votes: {totalVotes}")
                    {
                        Docking = Docking.Top,
                        IsDockedInsideChartArea = false,
                        DockingOffset = 10,
                        Font = new Font("Segoe UI", 11f, FontStyle.Regular)
                    };
                    chartEvent.Titles.Add(globalTitle);

                    // Team colors + single legend items
                    var teamList = new List<string>();
                    using (var tr = cmdTeams.ExecuteReader())
                        while (tr.Read())
                            teamList.Add(Convert.ToString(tr["Team"]));

                    var palette = new[]
                    {
                        Color.DodgerBlue, Color.Orange, Color.MediumSeaGreen, Color.MediumOrchid,
                        Color.Goldenrod, Color.Crimson, Color.DarkTurquoise, Color.SaddleBrown,
                        Color.SlateBlue, Color.Salmon
                    };
                    var teamColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < teamList.Count; i++)
                        teamColors[teamList[i]] = palette[i % palette.Length];

                    legend.CustomItems.Clear();
                    foreach (var t in teamList)
                    {
                        legend.CustomItems.Add(new LegendItem
                        {
                            Name = t,
                            Color = teamColors[t],
                            ImageStyle = LegendImageStyle.Rectangle
                        });
                    }

                    // Positions
                    var positions = new List<string>();
                    using (var r = cmdPos.ExecuteReader())
                        while (r.Read())
                            positions.Add(Convert.ToString(r["Position"]));

                    if (positions.Count == 0)
                    {
                        var area = new ChartArea("AllVotes");
                        HideAxes(area);
                        chartEvent.ChartAreas.Add(area);
                        ApplyPieInnerPlotSize(area);

                        // Legend outside the white box of this area
                        legend.DockedToChartArea = area.Name;
                        legend.IsDockedInsideChartArea = false;

                        // Title centered INSIDE the white box (top header)
                        var areaTitle = new Title("Position: All")
                        {
                            DockedToChartArea = area.Name,
                            Docking = Docking.Top,
                            IsDockedInsideChartArea = true,
                            Alignment = ContentAlignment.TopCenter,
                            DockingOffset = 2,
                            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
                        };
                        chartEvent.Titles.Add(areaTitle);

                        var series = new Series("Votes")
                        {
                            ChartType = SeriesChartType.Pie,
                            IsValueShownAsLabel = true,
                            Font = new Font("Arial", 10, FontStyle.Bold),
                            ChartArea = area.Name,
                            Legend = legend.Name,
                            IsVisibleInLegend = false
                        };

                        using (var cmd = new SqlCommand(sqlVotesPerTeamNoPosition, conn))
                        {
                            cmd.Parameters.AddWithValue("@EventName", eventNameFilter);
                            using (var rr = cmd.ExecuteReader())
                            {
                                while (rr.Read())
                                {
                                    string team = Convert.ToString(rr["Team"]);
                                    int count = rr["VoteCount"] == DBNull.Value ? 0 : Convert.ToInt32(rr["VoteCount"]);
                                    var p = series.Points.Add(count);
                                    p.AxisLabel = team;
                                    p.LegendText = "#VALX";
                                    if (teamColors.ContainsKey(team)) p.Color = teamColors[team];
                                }
                            }
                        }

                        chartEvent.Series.Add(series);
                        return;
                    }

                    // Multiple positions -> arrange in a 2-column grid with a little extra gap
                    int n = positions.Count;
                    int columns = n <= 2 ? n : 2;
                    int rows = (int)Math.Ceiling(n / (double)columns);

                    float margin = 4.5f; // increased gap between the two pies
                    float width = (100f - (columns + 1) * margin) / columns;
                    float height = (100f - (rows + 1) * margin) / rows;

                    for (int i = 0; i < positions.Count; i++)
                    {
                        string position = positions[i];
                        string areaName = $"Area_{i}_{position}";

                        int row = i / columns;
                        int col = i % columns;

                        float x = margin + col * (width + margin);
                        float y = margin + row * (height + margin);

                        var area = new ChartArea(areaName);
                        HideAxes(area);
                        area.Position = new ElementPosition(x, y, width, height);
                        chartEvent.ChartAreas.Add(area);
                        ApplyPieInnerPlotSize(area); // smaller pie and extra internal top padding

                        // Clean header OUTSIDE the pie area with padding
                        var title = new Title($"Position: {position}")
                        {
                            DockedToChartArea = areaName,
                            Docking = Docking.Top,
                            IsDockedInsideChartArea = false, // move outside the area
                            DockingOffset = 6,               // pixels of spacing from the area
                            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
                        };
                        chartEvent.Titles.Add(title);

                        var series = new Series($"Votes_{position}")
                        {
                            ChartType = SeriesChartType.Pie,
                            IsValueShownAsLabel = true,
                            Font = new Font("Arial", 9, FontStyle.Bold),
                            ChartArea = area.Name,
                            Legend = legend.Name,
                            IsVisibleInLegend = false // avoids duplicate legend items
                        };

                        using (var cmd = new SqlCommand(sqlVotesPerTeamForPosition, conn))
                        {
                            cmd.Parameters.AddWithValue("@EventName", eventNameFilter);
                            cmd.Parameters.AddWithValue("@Position", position);

                            using (var rr = cmd.ExecuteReader())
                            {
                                while (rr.Read())
                                {
                                    string team = Convert.ToString(rr["Team"]);
                                    int count = rr["VoteCount"] == DBNull.Value ? 0 : Convert.ToInt32(rr["VoteCount"]);
                                    var p = series.Points.Add(count);
                                    p.AxisLabel = team;
                                    p.LegendText = "#VALX";
                                    if (teamColors.ContainsKey(team)) p.Color = teamColors[team];
                                }
                            }
                        }

                        chartEvent.Series.Add(series);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading chart data: {ex.Message}", "Database Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    chartEvent.Titles.Clear();
                    chartEvent.Series.Clear();
                    chartEvent.ChartAreas.Clear();
                }
            }

            void HideAxes(ChartArea area)
            {
                area.AxisX.LabelStyle.Enabled = false;
                area.AxisY.LabelStyle.Enabled = false;
                area.AxisX.MajorGrid.Enabled = false;
                area.AxisY.MajorGrid.Enabled = false;
                area.AxisX.MajorTickMark.Enabled = false;
                area.AxisY.MajorTickMark.Enabled = false;
                area.AxisX.LineWidth = 0;
                area.AxisY.LineWidth = 0;
            }

            // Smaller pie and extra padding inside the area so labels/titles feel cleaner
            void ApplyPieInnerPlotSize(ChartArea area)
            {
                area.InnerPlotPosition.Auto = false;
                area.InnerPlotPosition.X = 14f;   // a bit more side padding
                area.InnerPlotPosition.Y = 18f;   // more top padding above pie
                area.InnerPlotPosition.Width  = 72f;
                area.InnerPlotPosition.Height = 70f;
            }
        }

        private void LoadHistoryData()
        {
            const string sql = @"
SELECT [LogId]
      ,[Action]
      ,[Details]
      ,[OccurredAt]
  FROM [POLLINGSYSTEM].[dbo].[AuditLog]
  ORDER BY [LogId] DESC";

            using (var con = new SqlConnection(ConnectionString))
            {
                try
                {
                    var da = new SqlDataAdapter(sql, con);
                    var dt = new DataTable();
                    da.Fill(dt);

                    EventHistoryData.DataSource = null;
                    EventHistoryData.AutoGenerateColumns = false;
                    EventHistoryData.DataSource = dt;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading history data: " + ex.Message,
                        "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ConfigureHistoryGridForAudit()
        {
            EventHistoryData.AutoGenerateColumns = false;
            EventHistoryData.Columns.Clear();

            var colId = new DataGridViewTextBoxColumn
            {
                Name = "LogId",
                HeaderText = "ID",
                DataPropertyName = "LogId",
                Width = 70
            };
            var colAction = new DataGridViewTextBoxColumn
            {
                Name = "Action",
                HeaderText = "Action",
                DataPropertyName = "Action",
                Width = 150
            };
            var colDetails = new DataGridViewTextBoxColumn
            {
                Name = "Details",
                HeaderText = "Details",
                DataPropertyName = "Details",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            var colOccurred = new DataGridViewTextBoxColumn
            {
                Name = "OccurredAt",
                HeaderText = "Date & Time",
                DataPropertyName = "OccurredAt",
                Width = 160
            };
            colOccurred.DefaultCellStyle.Format = "MM/dd/yy hh:mm tt";

            EventHistoryData.Columns.AddRange(new DataGridViewColumn[] { colId, colAction, colDetails, colOccurred });
        }

        private void votersData_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = votersData.Rows[e.RowIndex];
            FillAccountFieldsFromRow(row);
        }

        private void votersData_SelectionChanged(object sender, EventArgs e)
        {
            if (votersData.CurrentRow != null && votersData.CurrentRow.Index >= 0)
            {
                FillAccountFieldsFromRow(votersData.CurrentRow);
            }
        }

        private void FillAccountFieldsFromRow(DataGridViewRow row)
        {
            tbStudentNumberAcc.Text = Convert.ToString(row.Cells["StudentNo"].Value);
            tbLastNameAcc.Text = Convert.ToString(row.Cells["LastName"].Value);
            tbFirstNameAcc.Text = Convert.ToString(row.Cells["FirstName"].Value);

            var middle = row.Cells["MiddleName"].Value;
            tbMiddleNameAcc.Text = middle == null || middle == DBNull.Value ? string.Empty : Convert.ToString(middle);

            var role = Convert.ToString(row.Cells["Role"].Value);
            int roleIndex = cbRoleAcc.Items.IndexOf(role);
            cbRoleAcc.SelectedIndex = roleIndex;
            if (roleIndex == -1) cbRoleAcc.Text = role;

            var program = row.Cells["Program"].Value == null || row.Cells["Program"].Value == DBNull.Value
                ? string.Empty
                : Convert.ToString(row.Cells["Program"].Value);
            int progIndex = string.IsNullOrWhiteSpace(program) ? -1 : cbProgramAcc.Items.IndexOf(program);
            cbProgramAcc.SelectedIndex = progIndex;
            if (progIndex == -1) cbProgramAcc.Text = program;

            var pwd = GetPasswordForStudent(tbStudentNumberAcc.Text?.Trim());
            tbPassword.Text = pwd ?? string.Empty;

            currentAccountStudID = tbStudentNumberAcc.Text?.Trim();
            UpdateProgramVisibility();
        }

        private string GetPasswordForStudent(string studentNo)
        {
            if (string.IsNullOrWhiteSpace(studentNo)) return string.Empty;

            try
            {
                using (var con = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand("SELECT [Password] FROM [dbo].[Voters] WHERE StudentNo = @StudentNo", con))
                {
                    cmd.Parameters.AddWithValue("@StudentNo", studentNo);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? string.Empty : Convert.ToString(result);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private void PositionTogglePasswordButton()
        {
            if (btnTogglePassword == null || tbPassword == null) return;
            var paddingRight = 6;
            var x = tbPassword.Right - btnTogglePassword.Width - paddingRight;
            var y = tbPassword.Top + (tbPassword.Height - btnTogglePassword.Height) / 2;

            btnTogglePassword.Location = new Point(x, y);
            btnTogglePassword.BringToFront();

        }
    }
}