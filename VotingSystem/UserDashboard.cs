using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace VotingSystem
{
    public partial class UserDashboard : Form
    {
        // Use a static readonly field for the connection string
        private const string ConnectionString = @"Data Source=DESKTOP-54DEN4R\SQLEXPRESS;Initial Catalog=POLLINGSYSTEM;Integrated Security=True;Encrypt=False";

        public UserDashboard()
        {
            InitializeComponent();

            // Initial state: Show the main events list (HomePanel), hide the detail view (panelEvent)
            HomePanel.Visible = true;
            VotePanel.Visible = false;
            EventVoteProfile.Visible = false;

            LoadEventsIntoFlowPanel();
        }

        // --- 1. EVENT LOADING AND DYNAMIC PANEL CREATION ---

        private void LoadEventsIntoFlowPanel()
        {
            // Clear any previous events from the container
            flowLayoutPanel1.Controls.Clear();

            // Select distinct events to create one panel per event name
            string sqlQuery = "SELECT DISTINCT EventName, TeamGroup, TimeStart, TimeEnd FROM dbo.EventTb";

            string previousEventName = null;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    try
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Extract Event Data
                                string name = reader["EventName"].ToString();
                                string team = reader["TeamGroup"].ToString();
                                string start = reader["TimeStart"].ToString();
                                string end = reader["TimeEnd"].ToString();
                                if (name != previousEventName)
                                {
                                    previousEventName = name;
                                    // Create a clickable Panel for the event and add it to the flow layout
                                    Panel eventPanel = CreateEventPanel(name, team, start, end);
                                    flowLayoutPanel1.Controls.Add(eventPanel);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Database Error: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private Panel CreateEventPanel(string eventName, string teamGroup, string timeStart, string timeEnd)
        {
            Panel panel = new Panel();
            panel.Width = 250;
            panel.Height = 100;
            panel.BackColor = Color.Black;
            panel.ForeColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Margin = new Padding(10);

            // Store EventName for the click event
            panel.Tag = eventName;
            panel.Click += EventPanel_Click;

            // Create and configure Labels
            Label lblName = new Label();
            lblName.Text = eventName;
            lblName.Font = new Font("Arial", 12, FontStyle.Bold);
            lblName.Location = new Point(5, 5);
            lblName.AutoSize = true;
            lblName.ForeColor = Color.White;
            lblName.Click += EventPanel_Click; // Allows clicking the text

            Label lblTime = new Label();
            lblTime.Text = $"Time: {timeStart} - {timeEnd}";
            lblTime.Location = new Point(5, 30);
            lblTime.AutoSize = true;
            lblTime.ForeColor = Color.White;
            lblTime.Click += EventPanel_Click;

            Label lblTeam = new Label();
            lblTeam.Text = $"Group: {teamGroup}";
            lblTeam.Location = new Point(5, 50);
            lblTeam.AutoSize = true;
            lblTeam.ForeColor = Color.White;
            lblTeam.Click += EventPanel_Click;

            panel.Controls.Add(lblName);
            panel.Controls.Add(lblTime);
            panel.Controls.Add(lblTeam);

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

        private void HomeButton_Click(object sender, EventArgs e)
        {
            VotePanel.Visible = false;
            HomePanel.Visible = true;
        }

        private void GoBackToHome_Click(object sender, EventArgs e)
        {
            VotePanel.Visible = false;
            HomePanel.Visible = true;
        }


        private void LoadEventTeams(string eventName)
        {
            VotePanel.Controls.Clear();
            HomePanel.Visible = false; 
            VotePanel.Visible = true;
            panelHistory.Visible = false;

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
            titleLabel.ForeColor = Color.White;
            VotePanel.Controls.Add(titleLabel);

            FlowLayoutPanel flpTeams = new FlowLayoutPanel();
            flpTeams.Location = new Point(0, 50);
            flpTeams.Size = new Size(VotePanel.Width, VotePanel.Height - 50);
            flpTeams.AutoScroll = true;
            flpTeams.BackColor = Color.Transparent;
            VotePanel.Controls.Add(flpTeams);

            string sql = "SELECT TeamGroup FROM dbo.EventTb WHERE EventName = @EventName";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
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
                                Panel teamBox = CreateTeamBox(teamName);
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
        }

        private Panel CreateTeamBox(string teamName)
        {
            Panel panel = new Panel();
            panel.Width = 150;
            panel.Height = 150;
            panel.BackColor = Color.Gray;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Margin = new Padding(10);

            // *** 1. Store the Team Name in the Tag property ***
            panel.Tag = teamName;

            // *** 2. Attach the Click handler to the Panel ***
            panel.Click += TeamBox_Click;

            // Add a label inside to show the team name
            Label lblTeamName = new Label();
            lblTeamName.Text = teamName;
            lblTeamName.Font = new Font("Arial", 10, FontStyle.Bold);
            lblTeamName.Location = new Point(10, 10);
            lblTeamName.AutoSize = true;
            lblTeamName.ForeColor = Color.Black;

            // *** 3. Attach the Click handler to the Label as well ***
            lblTeamName.Click += TeamBox_Click;

            panel.Controls.Add(lblTeamName);

            return panel;
        }

        private void TeamBox_Click(object sender, EventArgs e)
        {
            Control clickedControl = (Control)sender;

            // Get the parent Panel, which holds the TeamName in its Tag
            Panel clickedPanel = clickedControl as Panel ?? clickedControl.Parent as Panel;

            if (clickedPanel != null && clickedPanel.Tag is string selectedTeamName)
            {
                // Now you have the name of the team that was clicked!

                MessageBox.Show(
                    $"You selected the team: {selectedTeamName}. You can now begin voting or show details.",
                    "Team Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // --- PUT YOUR NEXT ACTION HERE ---
                // Example: Open a new panel/form for voting, passing the selectedTeamName
                // ShowVotingInterface(selectedTeamName); 
            }
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            HomePanel.Visible = true;
            VotePanel.Visible = false;
            panelHistory.Visible = false;
        }

        private void panelHistory_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            panelHistory.Visible = true;
            HomePanel.Visible = false;
            VotePanel.Visible = false;

        }
    }
}