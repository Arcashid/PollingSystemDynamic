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

        private string currentAccountStudID = "";

        private Timer dashboardTimer;
        internal string CurrentUser;

        private Guna2Button btnTogglePassword;

        public AdminDashboard()
        {
            InitializeComponent();

            // Ensure audit log table exists
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

            //EventTableData.AutoGenerateColumns = false;
            //EventTableData.Columns.Clear();

            //if (!EventTableData.Columns.Contains("EventID"))
            //{
            //    EventTableData.Columns.Add("EventID", "Event ID");
            //    EventTableData.Columns["EventID"].DataPropertyName = "EventID";
            //    EventTableData.Columns["EventID"].Visible = false;
            //}

            //if (!EventTableData.Columns.Contains("EventName"))
            //{
            //    EventTableData.Columns.Add("EventName", "Event Name");
            //    EventTableData.Columns["EventName"].DataPropertyName = "EventName";
            //}

            //if (!EventTableData.Columns.Contains("description"))
            //{
            //    EventTableData.Columns.Add("description", "Description");
            //    EventTableData.Columns["description"].DataPropertyName = "description";
            //}

            //if (!EventTableData.Columns.Contains("TeamGroup"))
            //{
            //    EventTableData.Columns.Add("TeamGroup", "Group");
            //    EventTableData.Columns["TeamGroup"].DataPropertyName = "TeamGroup";
            //}

            //if (!EventTableData.Columns.Contains("TimeStart"))
            //{
            //    EventTableData.Columns.Add("TimeStart", "Time Start");
            //    EventTableData.Columns["TimeStart"].DataPropertyName = "TimeStart";
            //    EventTableData.Columns["TimeStart"].DefaultCellStyle.Format = "MM/dd/yy hh:mm tt";
            //}

            //if (!EventTableData.Columns.Contains("TimeEnd"))
            //{
            //    EventTableData.Columns.Add("TimeEnd", "Time End");
            //    EventTableData.Columns["TimeEnd"].DataPropertyName = "TimeEnd";
            //    EventTableData.Columns["TimeEnd"].DefaultCellStyle.Format = "MM/dd/yy hh:mm tt";
            //}

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
                tbPassword.PasswordChar = '\0'; // show
            }
            else
            {
                tbPassword.PasswordChar = '•';  // hide
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
                } else {
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
                    // Use real column names so the rest of the code can rely on them.
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

                // Optional: set friendly column headers without breaking column names used in code.
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

                // Optional: set friendly column headers without breaking column names used in code.
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

                // Optional: set friendly column headers without breaking column names used in code.
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

                // Optional: set friendly column headers without breaking column names used in code.
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

                // Configure AuditLog grid columns
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

                    // 1) Prevent duplicates of (EventName, TeamGroup)
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

                    // 2) Insert without EventID (IDENTITY generates it)
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
                // Unique key violation (duplicate EventName + TeamGroup)
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

                    // Prevent duplicates on update (ignore current row)
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
            if (EventTableData.SelectedRows.Count == 0 || string.IsNullOrWhiteSpace(tbEventID.Text))
            {
                MessageBox.Show("Please select an event to delete, and ensure Event ID is loaded.");
                return;
            }

            if (!int.TryParse(tbEventID.Text, out int eventIdToDelete))
            {
                MessageBox.Show("Invalid Event ID format.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("DELETE FROM EventTb WHERE EventID=@EventID", con);
                    cmd.Parameters.AddWithValue("@EventID", eventIdToDelete);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        LogActivity(
                            "DeleteEvent",
                            $"User {this.CurrentUser} deleted event ID {eventIdToDelete}.",
                            null,
                            null);
                    }

                    MessageBox.Show(rowsAffected > 0 ? "Event deleted successfully from the database."
                                                 : "No event found with the selected ID to delete.");

                    LoadEventsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting event: " + ex.Message);
            }

            ClearFields();
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

                // Prefill form fields from Voters without touching the Participants grid
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
                    return; // Important: do not change TableParticipant binding
                }

                // Normal Participants listing/binding (parameterized search)
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
            // Participants panel
            tbStudentNum.Text = string.Empty;
            tbLastName.Text = string.Empty;
            tbFirstName.Text = string.Empty;
            tbMiddleName.Text = string.Empty;
            tbPosition.Text = string.Empty;
            cbProgram.SelectedIndex = -1;
            cbProgram.Text = string.Empty;

            // Events panel
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

            // Accounts panel (use existing helper)
            ClearAccountFields();
            currentAccountStudID = string.Empty;

            // Search fields
            guna2TextBox1.Text = string.Empty; // participants search
            guna2TextBox2.Text = string.Empty; // events search
            guna2TextBox3.Text = string.Empty; // voters search
            tbSearchEventName.Text = string.Empty; // chart search

            // Chart
            chartEvent.Series.Clear();
            chartEvent.Titles.Clear();

            // Clear grid selections
            if (EventTableData != null) EventTableData.ClearSelection();
            if (TableParticipant != null) TableParticipant.ClearSelection();
            if (votersData != null) votersData.ClearSelection();

            // Trackers
            currentStudID = string.Empty;

            // Keep the toggle button aligned after layout changes
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

                int eventIndex = cbEvent.Items.IndexOf(eventName);
                if (eventIndex >= 0)
                    cbEvent.SelectedIndex = eventIndex;
                else
                    cbEvent.SelectedIndex = -1;

                if (!string.IsNullOrEmpty(teamName))
                {
                    if (cbEvent.SelectedItem != null)
                    {
                        FilterAndPopulateTeamDropdown(cbEvent.SelectedItem.ToString());
                    }

                    int teamIndex = cbTeam.Items.IndexOf(teamName);
                    if (teamIndex >= 0)
                        cbTeam.SelectedIndex = teamIndex;
                    else
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
                MessageBoxIcon.Warning
                );
                return;
            }
            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(
                    "INSERT INTO [dbo].[Participants] " +
                    "([StudentNo], [LastName], [FirstName], [MiddleName], [Program], [Event], [Team], [position]) " +
                    "VALUES (@studID, @tbLastName, @tbFirstName, @tbMiddleName, @cbProgram, @cbEvent, @cbTeam, @position)",
                    con);


                    cmd.Parameters.AddWithValue("@studID", tbStudentNum.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbLastName", tbLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbFirstName", tbFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbMiddleName", tbMiddleName.Text.Trim());
                    cmd.Parameters.AddWithValue("@cbProgram", cbProgram.Text.Trim());
                    cmd.Parameters.AddWithValue("@cbEvent", cbEvent.Text);
                    cmd.Parameters.AddWithValue("@cbTeam", cbTeam.Text);
                    cmd.Parameters.AddWithValue("@position", tbPosition.Text);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Successfully saved participant details.");

                    LogActivity(
                        "AddParticipant",
                        $"User {this.CurrentUser} saved new participant '{tbStudentNum.Text.Trim()}' - {tbLastName.Text.Trim()}, {tbFirstName.Text.Trim()} ({cbProgram.Text.Trim()})",
                        cbEvent.Text,
                        cbTeam.Text);

                    LoadParticipantsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                }
            }
            catch (SqlException sqlex)
            {
                if (sqlex.Number == 2627)
                {
                    MessageBox.Show($"Error: Student Number '{tbStudentNum.Text.Trim()}' already exists.", "Duplicate Student ID", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Database Error: " + sqlex.Message);
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
            string.IsNullOrWhiteSpace(currentStudID))
            {

                MessageBox.Show(
                "Please select a participant, and fill out all required fields.",
                "Missing Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
                );
                return;
            }
            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(
                    "UPDATE [dbo].[Participants] SET " +
                    "[StudentNo] = @studID, " +
                    "[LastName] = @tbLastName, " +
                    "[FirstName] = @tbFirstName, " +
                    "[MiddleName] = @tbMiddleName, " +
                    "[Program] = @cbProgram, " +
                    "[Event] = @cbEvent, " +
                    "[Team] = @cbTeam " +
                    " WHERE [StudentNo] = @currentStudID", con);

                    cmd.Parameters.AddWithValue("@studID", tbStudentNum.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbLastName", tbLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbFirstName", tbFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbMiddleName", tbMiddleName.Text.Trim());
                    cmd.Parameters.AddWithValue("@cbProgram", cbProgram.Text.Trim());
                    cmd.Parameters.AddWithValue("@cbEvent", cbEvent.Text);
                    cmd.Parameters.AddWithValue("@cbTeam", cbTeam.Text);
                    cmd.Parameters.AddWithValue("@currentStudID", currentStudID.Trim());

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        LogActivity(
                            "UpdateParticipant",
                            $"User {this.CurrentUser} updated participant '{currentStudID}' -> '{tbStudentNum.Text.Trim()}'.",
                            cbEvent.Text,
                            cbTeam.Text);
                        MessageBox.Show("Successfully updated participant details.");
                    }
                    else
                    {
                        MessageBox.Show("No participant found with the original Student ID to update.", "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    LoadParticipantsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
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
                        "VALUES (@StudentNo, @Password, @Role, @LastName, @FirstName, @MiddleName, @Program, 0)", con); // Set IsActive=0 by default

                    cmd.Parameters.AddWithValue("@StudentNo", tbStudentNumberAcc.Text.Trim());
                    cmd.Parameters.AddWithValue("@Password", tbPassword.Text); // ⚠️ Remember to HASH this!
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

        // --- AUDIT LOGGING: create table if missing ---
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
                // swallow: do not block UI if table creation fails
            }
        }

        // --- AUDIT LOGGING: helper ---
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
                // swallow: logging must not break the main flow
            }
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            // Show Dashboard, hide others
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
            // Show Events page
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
            // Show Participant page
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
            // Add a temporary header above existing titles for printing
            var header = new System.Windows.Forms.DataVisualization.Charting.Title
            {
                Text = "Polling Result",
                Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top,
                Alignment = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.Black
            };

            // Date at upper-right
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
                // Clean up so the extras don’t remain on the live chart
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
            // Determine selected role from SelectedItem or Text fallback
            var selectedRole = cbRoleAcc.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedRole))
                selectedRole = cbRoleAcc.Text;

            bool showProgram = string.Equals(selectedRole, "Student", StringComparison.OrdinalIgnoreCase)
                               || string.IsNullOrWhiteSpace(selectedRole);

            // Show/Hide Program label and combobox for non-students (e.g., Admin)
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

            const string sql = @"
                SELECT 
                    e.TeamGroup AS Team,
                    e.EventName,
                    COUNT(h.TeamName) AS VoteCount
                FROM EventTb e
                LEFT JOIN History h 
                    ON e.TeamGroup = h.TeamName 
                   AND e.EventName = h.EventName
                WHERE e.EventName = @EventName
                GROUP BY e.TeamGroup, e.EventName
                ORDER BY VoteCount DESC";

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                chartEvent.Series.Clear();
                chartEvent.Titles.Clear();

                var series = new Series("Votes")
                {
                    ChartType = SeriesChartType.Pie,
                    IsValueShownAsLabel = true,
                    Font = new Font("Arial", 10, FontStyle.Bold)
                };

                cmd.Parameters.AddWithValue("@EventName", tbSearchEventName.Text.Trim());

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        string eventName = string.Empty;
                        int totalVotes = 0;

                        while (reader.Read())
                        {
                            eventName = Convert.ToString(reader["EventName"]);
                            string teamName = Convert.ToString(reader["Team"]);
                            int voteCount = reader["VoteCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["VoteCount"]);
                            totalVotes += voteCount;

                            series.Points.AddXY(teamName, voteCount);
                        }

                        if (!string.IsNullOrEmpty(eventName))
                        {
                            chartEvent.Titles.Add($"{eventName} - Total Votes: {totalVotes}");
                            chartEvent.Series.Add(series);
                        }
                        else
                        {
                            chartEvent.Titles.Add("No event found with that name");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading chart data: {ex.Message}", "Database Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    chartEvent.Titles.Add("Error loading data");
                }
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
            tbLastNameAcc.Text      = Convert.ToString(row.Cells["LastName"].Value);
            tbFirstNameAcc.Text     = Convert.ToString(row.Cells["FirstName"].Value);

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

            // Load password from DB for selected StudentNo
            var pwd = GetPasswordForStudent(tbStudentNumberAcc.Text?.Trim());
            tbPassword.Text = pwd ?? string.Empty;
            //tbPassword.PasswordChar = '•';

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
        // Avoid breaking UI if lookup fails
        return string.Empty;
    }   
}

private void PositionTogglePasswordButton()
{
    if (btnTogglePassword == null || tbPassword == null) return;

    // Place the button inside the right edge of tbPassword with small padding
    var paddingRight = 6;
    var x = tbPassword.Right - btnTogglePassword.Width - paddingRight;
    var y = tbPassword.Top + (tbPassword.Height - btnTogglePassword.Height) / 2;

    btnTogglePassword.Location = new Point(x, y);
    btnTogglePassword.BringToFront();
}
    }
}