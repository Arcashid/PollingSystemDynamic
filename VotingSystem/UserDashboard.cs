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

        // ADD: fields inside class `UserDashboard`
        private CancellationTokenSource _refreshCts;
        private Task _refreshTask;

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

            await Task.Delay(1000);
            LoadEventsIntoFlowPanel();

            // start background refresh loop
            StartRefreshLoop();
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

                            // Show events that are currently active (start <= now <= end)
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

                    string votedQuery = @"SELECT h.TeamName, h.EventName, h.VoteDate
                                        FROM History h
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
                                Panel votedPanel = CreateVotedTeamPanel(
                                    votedReader["TeamName"].ToString(),
                                    votedReader["EventName"].ToString(),
                                    Convert.ToDateTime(votedReader["VoteDate"])
                                );
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
                    HomePanel.Visible = false;
                    VotePanel.Visible = false;
                    EventVoteProfile.Visible = true;
                    ShowVotingInterface(teamName, eventName);
                }
            }
        }

        private void ShowVotingInterface(string teamName, string eventName)
        {
            EventVoteProfile.Controls.Clear();
            EventVoteProfile.BackColor = Color.Black;

            Button btnBackToTeams = new Button();
            btnBackToTeams.Text = "← Back to Teams";
            btnBackToTeams.Font = new Font("Arial", 16, FontStyle.Regular);
            btnBackToTeams.ForeColor = Color.White;
            btnBackToTeams.Location = new Point(10, 10);
            btnBackToTeams.Size = new Size(150, 40);
            btnBackToTeams.Click += BackToTeams_Click;
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
            lblTeamName.Text = teamName;
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
            btnVote.Tag = new { TeamName = teamName, EventName = eventName };
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
                            // Check if already voted
                            string checkSql = @"
                                IF EXISTS (
                                    SELECT 1 
                                    FROM History h 
                                    WHERE h.StudentNo = @StudentNo 
                                    AND h.EventName = @EventName
                                )
                                SELECT 1
                                ELSE
                                SELECT 0";

                            using (SqlCommand checkCmd = new SqlCommand(checkSql, conn, transaction))
                            {
                                checkCmd.Parameters.AddWithValue("@StudentNo", StudentID);
                                checkCmd.Parameters.AddWithValue("@EventName", currentEvent);

                                int hasVoted = (int)checkCmd.ExecuteScalar();

                                if (hasVoted == 1)
                                {
                                    transaction.Rollback();
                                    MessageBox.Show($"You have already voted for the '{currentEvent}' event.",
                                        "Vote Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }

                            // Insert new vote - Note HistoryID is now auto-incrementing
                            string insertSql = @"INSERT INTO History (StudentNo, TeamName, EventName, VoteDate) 
                                       VALUES (@StudentNo, @TeamName, @EventName, GETDATE())";

                            using (SqlCommand insertCmd = new SqlCommand(insertSql, conn, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@StudentNo", StudentID);
                                insertCmd.Parameters.AddWithValue("@TeamName", teamToVoteFor);
                                insertCmd.Parameters.AddWithValue("@EventName", currentEvent);

                                int rowsAffected = insertCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    transaction.Commit();
                                    MessageBox.Show($"Successfully voted for: {teamToVoteFor} in {currentEvent}!",
                                        "Vote Confirmed", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                        catch (Exception ex)
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

        //private void LoadVoteHistory()
        //{
        //    DataTable dt = new DataTable();

        //    ConfigureHistoryDataGridView();

        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(ConnectionString))
        //        {
        //            conn.Open();
        //            string sql = @"SELECT EventName as Event, TeamName as Team, VoteDate as Date
        //                             FROM History 
        //                             WHERE StudentNo = @StudentNo
        //                             ORDER BY VoteDate DESC";

        //            using (SqlCommand cmd = new SqlCommand(sql, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@StudentNo", StudentID);

        //                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
        //                {
        //                    da.Fill(dt);
        //                    guna2DataGridView1.DataSource = dt;
        //                }
        //            }
        //        }
        //    }
        //    catch (SqlException sqlex)
        //    {
        //        MessageBox.Show($"Error loading vote history: {sqlex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error loading vote history: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}

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

        private Panel CreateVotedTeamPanel(string teamName, string eventName, DateTime voteDate)
        {
            Panel panel = new Panel();
            panel.Width = 250;
            panel.Height = 80;
            panel.BackColor = Color.FromArgb(30, 126, 230);
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Margin = new Padding(2);

            // Event and Team Names
            Label lblInfo = new Label
            {
                Text = $"{eventName}\nVoted Team: {teamName}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(5, 5),
                AutoSize = true,
                ForeColor = Color.White
            };
            panel.Controls.Add(lblInfo);

            // Vote Date
            Label lblDate = new Label
            {
                Text = $"Voted on: {voteDate:MM/dd/yyyy hh:mm tt}",
                Font = new Font("Arial", 9, FontStyle.Italic),
                Location = new Point(5, 45),
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

        // INSERT: new methods inside class `UserDashboard`
        private void StartRefreshLoop()
        {
            StopRefreshLoop(); // ensure only one loop runs

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

                        // update UI on UI thread
                        BeginInvoke((Action)(() =>
                        {
                            try
                            {
                                LoadEventsIntoFlowPanel();
                            }
                            catch
                            {
                                // swallow or log; avoid crashing the loop on UI update errors
                            }
                        }));
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // swallow unexpected errors to keep loop alive
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
                // ignore
            }
            finally
            {
                _refreshCts?.Dispose();
                _refreshCts = null;
                _refreshTask = null;
            }
        }

        // INSERT: ensure cleanup on form close inside class `UserDashboard`
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopRefreshLoop();
            base.OnFormClosed(e);
        }
    }
}