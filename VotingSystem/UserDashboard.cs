using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VotingSystem
{
    public partial class UserDashboard : Form
    {
        private const string ConnectionString = @"Data Source=DESKTOP-RVB0L7Q\SQLEXPRESS;Initial Catalog=POLLINGSYSTEM;Integrated Security=True;Encrypt=False";
        public int StudentID { get; set; }
        private System.Windows.Forms.DataGridView VotedhistoryData;

        private CancellationTokenSource _refreshCts;
        private Task _refreshTask;

        private bool _historyHasPositionColumn;

        public UserDashboard()
        {
            InitializeComponent();
            InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            HomePanel.Visible = true;
            VotePanel.Visible = false;
            EventVoteProfile.Visible = false;

            VotedhistoryData = new System.Windows.Forms.DataGridView();

            await EnsureHistorySchemaAsync();

            await Task.Delay(1000);
            LoadEventsIntoFlowPanel();

            StartRefreshLoop();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await LoadVoterNameAsync();
        }

        private async Task EnsureHistorySchemaAsync()
        {
            try
            {
                using (var con = new SqlConnection(ConnectionString))
                {
                    await con.OpenAsync();

                    bool hasPosition;
                    using (var checkCmd = new SqlCommand(
                        "SELECT CASE WHEN COL_LENGTH('dbo.History','Position') IS NULL THEN 0 ELSE 1 END", con))
                    {
                        hasPosition = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 1;
                    }

                    if (!hasPosition)
                    {
                        try
                        {
                            using (var addCmd = new SqlCommand(
                                "ALTER TABLE dbo.History ADD Position NVARCHAR(255) NULL;", con))
                            {
                                await addCmd.ExecuteNonQueryAsync();
                            }
                            hasPosition = true;
                        }
                        catch
                        {
                            hasPosition = false;
                        }
                    }

                    _historyHasPositionColumn = hasPosition;

                    if (_historyHasPositionColumn)
                    {
                        try
                        {
                            using (var dropIdx = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_History_Student_Event' AND object_id=OBJECT_ID('dbo.History'))
    DROP INDEX [UQ_History_Student_Event] ON dbo.History;", con))
                            {
                                await dropIdx.ExecuteNonQueryAsync();
                            }

                            using (var createIdx = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_History_Student_Event_Position' AND object_id=OBJECT_ID('dbo.History'))
    CREATE UNIQUE INDEX [UQ_History_Student_Event_Position]
        ON dbo.History (StudentNo, EventName, Position);", con))
                            {
                                await createIdx.ExecuteNonQueryAsync();
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
            catch
            {
                _historyHasPositionColumn = false;
            }
        }

        private async Task LoadVoterNameAsync()
        {
            if (lblVotersName == null) return;

            lblVotersName.Text = "Loading...";

            if (StudentID == 0)
            {
                lblVotersName.Text = string.Empty;
                return;
            }

            const string sql = @"
                SELECT LastName, FirstName, MiddleName
                FROM dbo.Voters
                WHERE StudentNo = @StudentNo";

            try
            {
                using (var con = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@StudentNo", SqlDbType.Int).Value = StudentID;

                    await con.OpenAsync();
                    using (var r = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow))
                    {
                        if (await r.ReadAsync())
                        {
                            var last = Convert.ToString(r["LastName"])?.Trim();
                            var first = Convert.ToString(r["FirstName"])?.Trim();
                            var middle = Convert.ToString(r["MiddleName"])?.Trim();

                            var fullName = string.IsNullOrWhiteSpace(middle)
                                ? $"{last}, {first}"
                                : $"{last}, {first} {middle}";

                            lblVotersName.Text = fullName;
                        }
                        else
                        {
                            lblVotersName.Text = "Unknown voter";
                        }
                    }
                }
            }
            catch
            {
                lblVotersName.Text = "Unknown voter";
            }
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            HomePanel.Visible = true;
            VotePanel.Visible = false;
            EventVoteProfile.Visible = false;
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            HomePanel.Visible = false;
            VotePanel.Visible = false;
            EventVoteProfile.Visible = false;
        }

        private void btnSignOut_Click(object sender, EventArgs e)
        {
            UpdateIsActiveStatus(0);
            VotingSystem loginForm = new VotingSystem();
            loginForm.Show();
            this.Close();
        }

        private void GoBackToHome_Click(object sender, EventArgs e)
        {
            VotePanel.Visible = false;
            HomePanel.Visible = true;
            EventVoteProfile.Visible = false;
        }

        private void BackToTeams_Click(object sender, EventArgs e)
        {
            EventVoteProfile.Visible = false;
            VotePanel.Visible = true;
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            btnHome_Click(sender, e);
        }

        private void UpdateIsActiveStatus(int status)
        {
            if (StudentID == 0) return;

            string sql = "UPDATE Voters SET IsActive = @Status WHERE StudentNo = @StudentNo";

            using (SqlConnection con = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Status", status);
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

        private void LoadEventsIntoFlowPanel()
        {
            Boolean hasEvents = false;
            flowLayoutPanel1.Controls.Clear();
            VotedTeam.Controls.Clear();
            DateTime now = DateTime.Now;
            DateTime dtStart;
            DateTime dtEnd;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sqlQuery = @"SELECT DISTINCT e1.EventName, e1.TimeStart, e1.TimeEnd 
                               FROM dbo.EventTb e1
                               WHERE e1.TimeStart = (
                                   SELECT MIN(e2.TimeStart)
                                   FROM dbo.EventTb e2 
                                   WHERE e2.EventName = e1.EventName
                               )
                               ORDER BY e1.TimeStart DESC";

                    using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dtStart = Convert.ToDateTime(reader["TimeStart"]);
                            dtEnd = Convert.ToDateTime(reader["TimeEnd"]);

                            if (now >= dtStart && now <= dtEnd)
                            {
                                hasEvents = true;
                                string name = reader["EventName"].ToString();
                                string start = reader["TimeStart"].ToString();
                                string end = reader["TimeEnd"].ToString();
                                Panel eventPanel = CreateEventPanel(name, start, end);
                                flowLayoutPanel1.Controls.Add(eventPanel);
                            }
                        }

                        if(!hasEvents)
                        {
                            string name = "No data";
                            string start = "No data";
                            string end = "No data";
                            Panel eventPanel = CreateEventPanel(name, start, end);
                            flowLayoutPanel1.Controls.Add(eventPanel);
                        }
                    }

                    Panel participantVotedContainer = new Panel
                    {
                        Width = flowLayoutPanel1.Width - 20,
                        AutoSize = true,
                        Margin = new Padding(10),
                        BackColor = Color.Transparent
                    };
                    FlowLayoutPanel votedItemsPanel = new FlowLayoutPanel
                    {
                        Width = participantVotedContainer.Width,
                        AutoSize = true,
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = true,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(0, 10, 0, 0)
                    };

                    string votedQuery = _historyHasPositionColumn
    ? @"SELECT h.TeamName, h.EventName, h.Position, h.VoteDate
        FROM dbo.History h
        WHERE h.StudentNo = @StudentNo
        ORDER BY h.VoteDate DESC"
    : @"SELECT h.TeamName, h.EventName, h.VoteDate
        FROM dbo.History h
        WHERE h.StudentNo = @StudentNo
        ORDER BY h.VoteDate DESC";

                    using (SqlCommand votedCmd = new SqlCommand(votedQuery, conn))
                    {
                        votedCmd.Parameters.AddWithValue("@StudentNo", StudentID);
                        using (SqlDataReader votedReader = votedCmd.ExecuteReader())
                        {
                            bool hasVotes = false;
                            while (votedReader.Read())
                            {
                                hasVotes = true;
                                var team = Convert.ToString(votedReader["TeamName"]);
                                var evt  = Convert.ToString(votedReader["EventName"]);
                                var pos  = _historyHasPositionColumn && votedReader["Position"] != DBNull.Value
                                            ? Convert.ToString(votedReader["Position"])
                                            : null;
                                var when = Convert.ToDateTime(votedReader["VoteDate"]);

                                Panel votedPanel = CreateVotedTeamPanel(team, evt, pos, when);
                                votedItemsPanel.Controls.Add(votedPanel);
                            }

                            if (!hasVotes)
                            {
                                Label noVotesLabel = new Label
                                {
                                    Text = "No votes recorded yet",
                                    Font = new Font("Arial", 10, FontStyle.Italic),
                                    ForeColor = Color.LightGray,
                                    AutoSize = true,
                                    Margin = new Padding(10)
                                };
                                votedItemsPanel.Controls.Add(noVotesLabel);
                            }
                        }
                    }

                    participantVotedContainer.Controls.Add(votedItemsPanel);
                    VotedTeam.Controls.Add(participantVotedContainer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading events and voted teams: {ex.Message}",
                        "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private Panel CreateEventPanel(string eventName, string timeStart, string timeEnd)
        {
            Panel panel = new Panel();
            panel.Width = 250;
            panel.Height = 100;
            panel.BackColor = Color.FromArgb(30, 126, 230);
            panel.ForeColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Margin = new Padding(5);

            panel.Tag = eventName;
            panel.Click += EventPanel_Click;

            Label lblName = new Label();
            lblName.Text = eventName;
            lblName.Font = new Font("Arial", 12, FontStyle.Bold);
            lblName.Location = new Point(5, 5);
            lblName.AutoSize = true;
            lblName.ForeColor = Color.White;
            lblName.Click += EventPanel_Click;

            Label lblTimeStart = new Label();
            lblTimeStart.Text = $"Start: {timeStart}";
            lblTimeStart.Font = new Font("Arial", 9, FontStyle.Italic);
            lblTimeStart.Location = new Point(5, 30);
            lblTimeStart.AutoSize = true;
            lblTimeStart.ForeColor = Color.White;
            lblTimeStart.Click += EventPanel_Click;

            Label lblTimeEnd = new Label();
            lblTimeEnd.Text = $"End: {timeEnd}";
            lblTimeEnd.Font = new Font("Arial", 9, FontStyle.Italic);
            lblTimeEnd.Location = new Point(5, 50);
            lblTimeEnd.AutoSize = true;
            lblTimeEnd.ForeColor = Color.White;
            lblTimeEnd.Click += EventPanel_Click;

            panel.Controls.Add(lblName);
            panel.Controls.Add(lblTimeStart);
            panel.Controls.Add(lblTimeEnd);

            return panel;
        }

        private void EventPanel_Click(object sender, EventArgs e)
        {
            Control clickedControl = (Control)sender;
            Panel clickedPanel = clickedControl as Panel ?? clickedControl.Parent as Panel;

            if (clickedPanel != null && clickedPanel.Tag is string eventName)
            {
                LoadEventTeams(eventName);
            }
        }

        private void LoadEventTeams(string eventName)
        {
            VotePanel.Controls.Clear();
            HomePanel.Visible = false;
            VotePanel.Visible = true;
            EventVoteProfile.Visible = false;

            Button btnBack = new Button();
            btnBack.Text = "← Back to Events";
            btnBack.Location = new Point(10, 10);
            btnBack.Size = new Size(120, 30);
            btnBack.Click += GoBackToHome_Click;
            VotePanel.Controls.Add(btnBack);

            Label titleLabel = new Label();
            titleLabel.Text = $"Event Title: {eventName}";
            titleLabel.Font = new Font("Arial", 16, FontStyle.Bold);
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(150, 15);
            titleLabel.ForeColor = Color.Black;
            VotePanel.Controls.Add(titleLabel);

            FlowLayoutPanel flpTeams = new FlowLayoutPanel();
            flpTeams.Location = new Point(0, 50);
            flpTeams.Size = new Size(VotePanel.Width, VotePanel.Height - 50);
            flpTeams.AutoScroll = true;
            flpTeams.BackColor = Color.Transparent;
            VotePanel.Controls.Add(flpTeams);

            string sql = "SELECT DISTINCT TeamGroup FROM dbo.EventTb WHERE EventName = @EventName";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@EventName", eventName);

                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string teamName = reader["TeamGroup"].ToString();
                            Panel teamBox = CreateTeamBox(teamName, eventName);
                            flpTeams.Controls.Add(teamBox);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading teams: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private Panel CreateTeamBox(string teamName, string eventName)
        {
            Panel panel = new Panel();
            panel.Width = 150;
            panel.Height = 150;
            panel.BackColor = Color.LightGray;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Margin = new Padding(5);

            panel.Tag = new { TeamName = teamName, EventName = eventName };
            panel.Click += TeamBox_Click;

            Label lblTeamName = new Label();
            lblTeamName.Text = teamName;
            lblTeamName.Font = new Font("Arial", 10, FontStyle.Bold);
            lblTeamName.Location = new Point(10, 10);
            lblTeamName.AutoSize = true;
            lblTeamName.ForeColor = Color.Black;
            lblTeamName.Click += TeamBox_Click;

            panel.Controls.Add(lblTeamName);

            var positions = GetTeamPositions(eventName, teamName);
            int y = lblTeamName.Bottom + 8;

            int showCount = Math.Min(3, positions.Count);
            for (int i = 0; i < showCount; i++)
            {
                var pos = positions[i];
                var posLabel = new Label
                {
                    Text = pos,
                    AutoSize = true,
                    Font = new Font("Arial", 9f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(30, 126, 230),
                    Padding = new Padding(6, 2, 6, 2),
                    Location = new Point(10, y),
                    Cursor = Cursors.Hand,
                    Tag = new { TeamName = teamName, EventName = eventName, Position = pos }
                };
                posLabel.Click += PositionLabel_Click;
                panel.Controls.Add(posLabel);
                y += posLabel.Height + 4;

                if (y > panel.Height - 24) break;
            }

            if (positions.Count > showCount && y <= panel.Height - 20)
            {
                var more = new Label
                {
                    Text = $"+{positions.Count - showCount} more",
                    AutoSize = true,
                    Font = new Font("Arial", 8f, FontStyle.Italic),
                    ForeColor = Color.DimGray,
                    Location = new Point(10, y),
                    Cursor = Cursors.Hand,
                    Tag = new { TeamName = teamName, EventName = eventName }
                };
                more.Click += TeamBox_Click;
                panel.Controls.Add(more);
            }

            return panel;
        }

        private void TeamBox_Click(object sender, EventArgs e)
        {
            Control clickedControl = (Control)sender;
            Panel clickedPanel = clickedControl as Panel ?? clickedControl.Parent as Panel;

            if (clickedPanel != null && clickedPanel.Tag != null)
            {
                Type tagType = clickedPanel.Tag.GetType();
                string teamName = tagType.GetProperty("TeamName")?.GetValue(clickedPanel.Tag, null)?.ToString();
                string eventName = tagType.GetProperty("EventName")?.GetValue(clickedPanel.Tag, null)?.ToString();

                if (!string.IsNullOrEmpty(teamName) && !string.IsNullOrEmpty(eventName))
                {
                    var positions = GetTeamPositions(eventName, teamName);
                    if (positions.Count == 0)
                    {
                        HomePanel.Visible = false;
                        VotePanel.Visible = false;
                        EventVoteProfile.Visible = true;
                        ShowVotingInterface(teamName, eventName, null);
                    }
                    else
                    {
                        ShowPositionSelection(eventName, teamName);
                    }
                }
            }
        }

        private void PositionLabel_Click(object sender, EventArgs e)
        {
            var ctrl = (Control)sender;
            var tag = ctrl.Tag;
            if (tag == null) return;

            var type = tag.GetType();
            string teamName = type.GetProperty("TeamName")?.GetValue(tag, null)?.ToString();
            string eventName = type.GetProperty("EventName")?.GetValue(tag, null)?.ToString();
            string position = type.GetProperty("Position")?.GetValue(tag, null)?.ToString();

            if (!string.IsNullOrEmpty(teamName) && !string.IsNullOrEmpty(eventName) && !string.IsNullOrEmpty(position))
            {
                HomePanel.Visible = false;
                VotePanel.Visible = false;
                EventVoteProfile.Visible = true;
                ShowVotingInterface(teamName, eventName, position);
            }
        }

        private void ShowPositionSelection(string eventName, string teamName)
        {
            VotePanel.Controls.Clear();
            HomePanel.Visible = false;
            VotePanel.Visible = true;
            EventVoteProfile.Visible = false;

            var btnBack = new Button
            {
                Text = "← Back to Teams",
                Location = new Point(10, 10),
                Size = new Size(140, 30)
            };
            btnBack.Click += (s, e) => LoadEventTeams(eventName);
            VotePanel.Controls.Add(btnBack);

            var title = new Label
            {
                Text = $"Select Position: {teamName}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(170, 15)
            };
            VotePanel.Controls.Add(title);

            var flp = new FlowLayoutPanel
            {
                Location = new Point(0, 60),
                Size = new Size(VotePanel.Width, VotePanel.Height - 60),
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            VotePanel.Controls.Add(flp);

            var positions = GetTeamPositions(eventName, teamName);

            if (positions.Count == 0)
            {
                ShowVotingInterface(teamName, eventName, null);
                return;
            }

            foreach (var pos in positions)
            {
                var btn = new Button
                {
                    Text = pos,
                    AutoSize = false,
                    Size = new Size(200, 40),
                    BackColor = Color.FromArgb(30, 126, 230),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Tag = new { TeamName = teamName, EventName = eventName, Position = pos },
                    Margin = new Padding(10)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) =>
                {
                    var t = ((Button)s).Tag;
                    var tp = t.GetType();
                    var tn = tp.GetProperty("TeamName").GetValue(t, null).ToString();
                    var en = tp.GetProperty("EventName").GetValue(t, null).ToString();
                    var pn = tp.GetProperty("Position").GetValue(t, null).ToString();

                    HomePanel.Visible = false;
                    VotePanel.Visible = false;
                    EventVoteProfile.Visible = true;
                    ShowVotingInterface(tn, en, pn);
                };
                flp.Controls.Add(btn);
            }
        }

        private void ShowVotingInterface(string teamName, string eventName, string positionName)
        {
            EventVoteProfile.Controls.Clear();
            EventVoteProfile.BackColor = Color.Black;

            Button btnBackToTeams = new Button();
            btnBackToTeams.Text = "← Back to Teams";
            btnBackToTeams.Font = new Font("Arial", 16, FontStyle.Regular);
            btnBackToTeams.ForeColor = Color.White;
            btnBackToTeams.Location = new Point(10, 10);
            btnBackToTeams.Size = new Size(180, 40);
            btnBackToTeams.Click += (s, e) => ShowPositionSelection(eventName, teamName);
            EventVoteProfile.Controls.Add(btnBackToTeams);

            Label lblEventName = new Label();
            lblEventName.Text = $"Event: {eventName}";
            lblEventName.Font = new Font("Arial", 16, FontStyle.Regular);
            lblEventName.AutoSize = true;
            lblEventName.ForeColor = Color.White;
            lblEventName.Location = new Point(
                (EventVoteProfile.Width / 2) - (TextRenderer.MeasureText(lblEventName.Text, lblEventName.Font).Width / 2),
                60
            );
            EventVoteProfile.Controls.Add(lblEventName);

            Label lblTeamName = new Label();
            lblTeamName.Text = positionName == null ? teamName : $"{teamName} - {positionName}";
            lblTeamName.Font = new Font("Arial", 22, FontStyle.Bold);
            lblTeamName.AutoSize = true;
            lblTeamName.ForeColor = Color.White;
            lblTeamName.Location = new Point(
                (EventVoteProfile.Width / 2) - (TextRenderer.MeasureText(lblTeamName.Text, lblTeamName.Font).Width / 2),
                100
            );
            EventVoteProfile.Controls.Add(lblTeamName);

            Button btnVote = new Button();
            btnVote.Text = "Vote";
            btnVote.Size = new Size(200, 50);
            btnVote.BackColor = Color.Blue;
            btnVote.ForeColor = Color.White;
            btnVote.FlatStyle = FlatStyle.Flat;
            btnVote.FlatAppearance.BorderSize = 0;
            btnVote.Tag = new { TeamName = teamName, EventName = eventName, Position = positionName };
            btnVote.Location = new Point(
                (EventVoteProfile.Width / 2) - (btnVote.Width / 2),
                500
            );
            btnVote.Click += FinalVoteButton_Click;
            EventVoteProfile.Controls.Add(btnVote);
        }

        private void FinalVoteButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            dynamic tagData = btn.Tag;

            string teamToVoteFor = tagData.TeamName;
            string currentEvent = tagData.EventName;
            string positionName = null;
            try { positionName = tagData.Position; } catch { positionName = null; }

            if (StudentID == 0)
            {
                MessageBox.Show("Error: Student ID is not set. Please log in again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string checkSql = _historyHasPositionColumn
                                ? @"
IF EXISTS (
    SELECT 1 
    FROM dbo.History h 
    WHERE h.StudentNo = @StudentNo 
      AND h.EventName = @EventName
      AND ((@Position IS NULL AND h.Position IS NULL) OR (h.Position = @Position))
)
SELECT 1 ELSE SELECT 0"
                                : @"
IF EXISTS (
    SELECT 1 
    FROM dbo.History h 
    WHERE h.StudentNo = @StudentNo 
      AND h.EventName = @EventName
)
SELECT 1 ELSE SELECT 0";

                            using (SqlCommand checkCmd = new SqlCommand(checkSql, conn, transaction))
                            {
                                checkCmd.Parameters.AddWithValue("@StudentNo", StudentID);
                                checkCmd.Parameters.AddWithValue("@EventName", currentEvent);
                                if (_historyHasPositionColumn)
                                    checkCmd.Parameters.AddWithValue("@Position", (object)positionName ?? DBNull.Value);

                                int hasVoted = (int)checkCmd.ExecuteScalar();

                                if (hasVoted == 1)
                                {
                                    transaction.Rollback();
                                    var what = _historyHasPositionColumn && positionName != null
                                        ? $"the '{positionName}' position"
                                        : "this event";
                                    MessageBox.Show($"You have already voted for {what}.",
                                        "Vote Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }

                            string insertSql = _historyHasPositionColumn
                                ? @"INSERT INTO dbo.History (StudentNo, TeamName, EventName, Position, VoteDate) 
                           VALUES (@StudentNo, @TeamName, @EventName, @Position, GETDATE())"
                                : @"INSERT INTO dbo.History (StudentNo, TeamName, EventName, VoteDate) 
                           VALUES (@StudentNo, @TeamName, @EventName, GETDATE())";

                            using (SqlCommand insertCmd = new SqlCommand(insertSql, conn, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@StudentNo", StudentID);
                                insertCmd.Parameters.AddWithValue("@TeamName", teamToVoteFor);
                                insertCmd.Parameters.AddWithValue("@EventName", currentEvent);
                                if (_historyHasPositionColumn)
                                    insertCmd.Parameters.AddWithValue("@Position", (object)positionName ?? DBNull.Value);

                                int rowsAffected = insertCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    transaction.Commit();
                                    MessageBox.Show(
                                        _historyHasPositionColumn && positionName != null
                                            ? $"Successfully voted for: {teamToVoteFor} - {positionName} in {currentEvent}!"
                                            : $"Successfully voted for: {teamToVoteFor} in {currentEvent}!",
                                        "Vote Confirmed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    LogActivity(
                                        "Vote",
                                        _historyHasPositionColumn && positionName != null
                                            ? $"User {StudentID} voted '{teamToVoteFor}' for '{positionName}' in event '{currentEvent}'."
                                            : $"User {StudentID} voted '{teamToVoteFor}' in event '{currentEvent}'.",
                                        currentEvent,
                                        teamToVoteFor);

                                    LoadEventsIntoFlowPanel();
                                    HomePanel.Visible = true;
                                    VotePanel.Visible = false;
                                    EventVoteProfile.Visible = false;
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Unable to record your vote. Please try again.",
                                        "Vote Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recording vote: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool CheckIfStudentVoted(string eventName)
        {
            string sql = @"SELECT COUNT(*) 
                  FROM History h
                  INNER JOIN EventTb e ON h.EventName = e.EventName
                  WHERE h.StudentNo = @StudentNo 
                  AND h.EventName = @EventName
                  AND EXISTS (
                      SELECT 1
                      FROM EventTb e 
                      WHERE e.EventName = h.EventName
                      AND e.TimeStart >= CAST(GETDATE() AS DATE)
                  )";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@StudentNo", StudentID);
                cmd.Parameters.AddWithValue("@EventName", eventName);

                try
                {
                    conn.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error checking previous vote: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        private void ConfigureHistoryDataGridView()
        {
            VotedhistoryData.AutoGenerateColumns = false;
            VotedhistoryData.Columns.Clear();

            VotedhistoryData.Columns.Add(new DataGridViewTextBoxColumn() { Name = "EventName", HeaderText = "Event", DataPropertyName = "EventName", Width = 200 });
            VotedhistoryData.Columns.Add(new DataGridViewTextBoxColumn() { Name = "TeamName", HeaderText = "Participant", DataPropertyName = "TeamName", Width = 150 });
            VotedhistoryData.Columns.Add(new DataGridViewTextBoxColumn() { Name = "VoteTime", HeaderText = "Time", Width = 100 });
            VotedhistoryData.Columns.Add(new DataGridViewTextBoxColumn() { Name = "VoteDate", HeaderText = "Date", Width = 120 });

            VotedhistoryData.AllowUserToAddRows = false;
            VotedhistoryData.ReadOnly = true;
            VotedhistoryData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            VotedhistoryData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private Panel CreateVotedTeamPanel(string teamName, string eventName, string position, DateTime voteDate)
        {
            Panel panel = new Panel();
            panel.Width = 270;
            panel.Height = 90;
            panel.BackColor = Color.FromArgb(30, 126, 230);
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Margin = new Padding(2);

            string header = string.IsNullOrWhiteSpace(position)
                ? $"{eventName}\nVoted Team: {teamName}"
                : $"{eventName}\nVoted: {teamName} - {position}";

            Label lblInfo = new Label
            {
                Text = header,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(5, 5),
                AutoSize = true,
                ForeColor = Color.White
            };
            panel.Controls.Add(lblInfo);

            Label lblDate = new Label
            {
                Text = $"Voted on: {voteDate:MM/dd/yyyy hh:mm tt}",
                Font = new Font("Arial", 9, FontStyle.Italic),
                Location = new Point(5, 58),
                AutoSize = true,
                ForeColor = Color.LightGray
            };
            panel.Controls.Add(lblDate);

            return panel;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Program.StudentID = this.StudentID;
            Application.Exit();
        }

        private void StartRefreshLoop()
        {
            StopRefreshLoop();

            _refreshCts = new CancellationTokenSource();
            var token = _refreshCts.Token;

            _refreshTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), token);
                        if (token.IsCancellationRequested) break;

                        if (IsDisposed || !IsHandleCreated) break;

                        BeginInvoke((Action)(() =>
                        {
                            try
                            {
                                LoadEventsIntoFlowPanel();
                            }
                            catch
                            {
                            }
                        }));
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                    }
                }
            }, token);
        }

        private void StopRefreshLoop()
        {
            try
            {
                if (_refreshCts != null && !_refreshCts.IsCancellationRequested)
                {
                    _refreshCts.Cancel();
                }
            }
            catch
            {
            }
            finally
            {
                _refreshCts?.Dispose();
                _refreshCts = null;
                _refreshTask = null;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopRefreshLoop();
            base.OnFormClosed(e);
        }

        private void LogActivity(string action, string details, string eventName = null, string teamName = null)
        {
            try
            {
                using (var con = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(
                    @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLog' AND type = 'U')
                      BEGIN
                        CREATE TABLE dbo.AuditLog(
                            LogId INT IDENTITY(1,1) PRIMARY KEY,
                            StudentNo INT NULL,
                            [Action] NVARCHAR(100) NOT NULL,
                            [Details] NVARCHAR(1000) NULL,
                            EventName NVARCHAR(255) NULL,
                            TeamName NVARCHAR(255) NULL,
                            OccurredAt DATETIME NOT NULL CONSTRAINT DF_AuditLog_OccurredAt DEFAULT (GETDATE())
                        );
                      END;
                      INSERT INTO dbo.AuditLog(StudentNo,[Action],[Details],EventName,TeamName)
                      VALUES (@StudentNo,@Action,@Details,@EventName,@TeamName);", con))
                {
                    cmd.Parameters.AddWithValue("@StudentNo", StudentID == 0 ? (object)DBNull.Value : StudentID);
                    cmd.Parameters.AddWithValue("@Action", action ?? "Unknown");
                    cmd.Parameters.AddWithValue("@Details", (object)details ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EventName", string.IsNullOrWhiteSpace(eventName) ? (object)DBNull.Value : eventName);
                    cmd.Parameters.AddWithValue("@TeamName", string.IsNullOrWhiteSpace(teamName) ? (object)DBNull.Value : teamName);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private System.Collections.Generic.List<string> GetTeamPositions(string eventName, string teamName)
        {
            var list = new System.Collections.Generic.List<string>();

            const string sql = @"
        SELECT position
        FROM dbo.Participants
        WHERE [Event] = @EventName
          AND [Team]  = @TeamName
          AND position IS NOT NULL
          AND LTRIM(RTRIM(position)) <> ''";

            try
            {
                using (var con = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@EventName", eventName);
                    cmd.Parameters.AddWithValue("@TeamName", teamName);
                    con.Open();

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(Convert.ToString(r["position"]));
                        }
                    }
                }
            }
            catch
            {
            }

            return list;
        }
    }
}