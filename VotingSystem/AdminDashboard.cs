using System;
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
using VotingSystem;

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

        private const string ConnectionString = @"Data Source=DESKTOP-54DEN4R\SQLEXPRESS;Initial Catalog=POLLINGSYSTEM;Integrated Security=True;Encrypt=False";

        public AdminDashboard()
        {
            InitializeComponent();

            UpdateTotalEventLabel();

            EventTableData.AutoGenerateColumns = false;

            EventTableData.Columns.Clear();

            if (!EventTableData.Columns.Contains("EventID"))
            {
                EventTableData.Columns.Add("EventID", "Event ID");
                EventTableData.Columns["EventID"].DataPropertyName = "EventID";
                EventTableData.Columns["EventID"].Visible = false;
            }

            if (!EventTableData.Columns.Contains("EventName"))
            {
                EventTableData.Columns.Add("EventName", "Event Name");
                EventTableData.Columns["EventName"].DataPropertyName = "EventName";
            }

            if (!EventTableData.Columns.Contains("TeamGroup"))
            {
                EventTableData.Columns.Add("TeamGroup", "Group");
                EventTableData.Columns["TeamGroup"].DataPropertyName = "TeamGroup";
            }

            if (!EventTableData.Columns.Contains("TimeStart"))
            {
                EventTableData.Columns.Add("TimeStart", "Time Start");
                EventTableData.Columns["TimeStart"].DataPropertyName = "TimeStart";
                EventTableData.Columns["TimeStart"].DefaultCellStyle.Format = "MM/dd/yy hh:mm tt";
            }

            if (!EventTableData.Columns.Contains("TimeEnd"))
            {
                EventTableData.Columns.Add("TimeEnd", "Time End");
                EventTableData.Columns["TimeEnd"].DataPropertyName = "TimeEnd";
                EventTableData.Columns["TimeEnd"].DefaultCellStyle.Format = "MM/dd/yy hh:mm tt";
            }

            foreach (string program in programs)
            {
                cbProgram.Items.Add(program);
            }

            LoadEventsData();
            LoadParticipantsData();

            UpdateTotalEventLabel();
            UpdateEventDropdown();

            // 🌟 1. REGISTER EVENT HANDLER: Attach the dynamic filtering method.
            cbEvent.SelectedIndexChanged += cbEvent_SelectedIndexChanged;

            // 🛑 REMOVED: Initial call to UpdateTeamDropdown() is removed.
            // Teams will now be loaded only when an event is selected in cbEvent.
        }

        private void LoadEventsData()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter("SELECT EventID, EventName, TeamGroup, TimeStart, TimeEnd FROM EventTb", con);

                DataTable dt = new DataTable();
                da.Fill(dt);

                EventTableData.DataSource = dt;
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

                // This correctly updates the dashboard label
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

        // 🌟 2. NEW METHOD: Dynamic filtering based on event selection.
        private void FilterAndPopulateTeamDropdown(string selectedEventName)
        {
            cbTeam.Items.Clear();

            if (string.IsNullOrWhiteSpace(selectedEventName) || !(EventTableData.DataSource is DataTable dt))
            {
                return;
            }

            // Filter the DataTable for rows matching the selected event
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

        // 🌟 3. NEW HANDLER: Triggers the team filtering whenever the event selection changes.
        private void cbEvent_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbEvent.SelectedItem != null)
            {
                string selectedEvent = cbEvent.SelectedItem.ToString();
                FilterAndPopulateTeamDropdown(selectedEvent);
            }
            else
            {
                // Clear the team dropdown if no event is selected
                cbTeam.Items.Clear();
            }
        }

        // 🛑 REMOVED/OBSOLETE: The old UpdateTeamDropdown() method is no longer needed.
        // Teams are now populated by FilterAndPopulateTeamDropdown() via cbEvent_SelectedIndexChanged.

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
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO EventTb (EventName, TeamGroup, TimeStart, TimeEnd) VALUES (@EventName, @TeamGroup, @TimeStart, @TimeEnd)", con);

                    cmd.Parameters.AddWithValue("@EventName", tbEventName.Text.Trim());
                    cmd.Parameters.AddWithValue("@TeamGroup", tbTeam.Text.Trim());
                    cmd.Parameters.AddWithValue("@TimeStart", dtpTimeStart.Value);
                    cmd.Parameters.AddWithValue("@TimeEnd", dtpTimeEnd.Value);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("New Event created successfully and saved to database.");

                    LoadEventsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                    // The Event/Team cascade will happen automatically next time cbEvent is changed.
                }
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

                    SqlCommand cmd = new SqlCommand(
                        "UPDATE EventTb SET EventName=@NewEventName, TeamGroup=@TeamGroup, TimeStart=@TimeStart, TimeEnd=@TimeEnd WHERE EventID=@EventID", con);

                    cmd.Parameters.AddWithValue("@EventID", eventIdToUpdate);
                    cmd.Parameters.AddWithValue("@NewEventName", tbEventName.Text.Trim());
                    cmd.Parameters.AddWithValue("@TeamGroup", tbTeam.Text.Trim());
                    cmd.Parameters.AddWithValue("@TimeStart", dtpTimeStart.Value);
                    cmd.Parameters.AddWithValue("@TimeEnd", dtpTimeEnd.Value);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Event updated successfully in the database.");
                    }
                    else
                    {
                        MessageBox.Show("No event found with the selected ID to update.");
                    }

                    LoadEventsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                    // The Event/Team cascade will happen automatically next time cbEvent is changed.
                }
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
                        MessageBox.Show("Event deleted successfully from the database.");
                    }
                    else
                    {
                        MessageBox.Show("No event found with the selected ID to delete.");
                    }

                    LoadEventsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                    // The Event/Team cascade will happen automatically next time cbEvent is changed.
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
                    tbEventID.Clear();

                if (EventTableData.Columns.Contains("EventName") && row.Cells["EventName"].Value != null && row.Cells["EventName"].Value != DBNull.Value)
                    tbEventName.Text = row.Cells["EventName"].Value.ToString();

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


        private void LoadParticipantsData()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                ClearFields();
                SqlDataAdapter da = new SqlDataAdapter("SELECT StudentNo, LastName, FirstName, MiddleName, Program, Team, Event FROM Participants", con);

                DataTable dt = new DataTable();
                da.Fill(dt);

                TableParticipant.DataSource = dt;
            }
        }

        private void ClearFields()
        {
            tbStudentNum.Clear();
            tbLastName.Clear();
            tbFirstName.Clear();
            tbMiddleName.Clear();
            cbProgram.SelectedIndex = -1;

            // It's good practice to clear cbTeam if the event is cleared
            cbTeam.Items.Clear();

            tbEventID.Clear();

            tbEventName.Clear();
            tbTeam.Clear();

            dtpTimeStart.Value = DateTime.Now;
            dtpTimeEnd.Value = DateTime.Now;

            cbEvent.SelectedIndex = -1;
        }

        private void TableParticipant_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < TableParticipant.Rows.Count)
            {
                DataGridViewRow row = TableParticipant.Rows[e.RowIndex];

                tbStudentNum.Text = row.Cells["StudentNo"].Value?.ToString();
                currentStudID = "";
                currentStudID = tbStudentNum.Text;
                tbLastName.Text = row.Cells["LastName"].Value?.ToString();
                tbFirstName.Text = row.Cells["FirstName"].Value?.ToString();
                tbMiddleName.Text = row.Cells["MiddleName"].Value?.ToString();
                cbProgram.Text = row.Cells["Program"].Value?.ToString();

                // Get event and team/role values
                string eventName = row.Cells["Event"].Value?.ToString().Trim();
                string teamName = row.Cells["Team"].Value?.ToString().Trim();

                // Set Event Combobox (will trigger the team filtering)
                int eventIndex = cbEvent.Items.IndexOf(eventName);
                if (eventIndex >= 0)
                    cbEvent.SelectedIndex = eventIndex;
                else
                    cbEvent.SelectedIndex = -1;

                // Set Team Combobox (must be done AFTER event filtering)
                if (!string.IsNullOrEmpty(teamName))
                {
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
            // ... (Add validation logic) ...
            if (string.IsNullOrWhiteSpace(tbStudentNum.Text) || string.IsNullOrWhiteSpace(tbLastName.Text) || cbProgram.SelectedIndex == -1 || cbTeam.SelectedIndex == -1 || cbEvent.SelectedIndex == -1)
            {
                MessageBox.Show("Please fill in all required fields: Student ID, Last Name, Program, Team, and Event.");
                return;
            }

            string selectedEvent = cbEvent.SelectedItem.ToString();
            string selectedRoleOrTeam = cbTeam.SelectedItem.ToString();

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                try
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO Participants (StudentID, LastName, FirstName, MiddleName, Program, Role, Events) VALUES (@StudentID, @LastName, @FirstName, @MiddleName, @Program, @Role, @Events)", con);

                    cmd.Parameters.AddWithValue("@StudentID", tbStudentNum.Text);
                    cmd.Parameters.AddWithValue("@LastName", tbLastName.Text);
                    cmd.Parameters.AddWithValue("@FirstName", tbFirstName.Text);
                    cmd.Parameters.AddWithValue("@MiddleName", tbMiddleName.Text);
                    cmd.Parameters.AddWithValue("@Program", cbProgram.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@Role", selectedRoleOrTeam);
                    cmd.Parameters.AddWithValue("@Events", selectedEvent);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Participant added successfully.");
                    LoadParticipantsData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error adding participant: " + ex.Message);
                }
            }

            ClearFields();
        }

        private void btnUpdate_Click_1(object sender, EventArgs e)
        {
            // ... (Update validation logic) ...
            if (TableParticipant.SelectedRows.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(tbStudentNum.Text))
                {
                    MessageBox.Show("Student ID is missing.");
                    return;
                }

                if (cbProgram.SelectedIndex == -1 || cbTeam.SelectedIndex == -1 || cbEvent.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select Program, Team, and Event.");
                    return;
                }

                string selectedEvent = cbEvent.SelectedItem.ToString();
                string selectedRoleOrTeam = cbTeam.SelectedItem.ToString();

                try
                {
                    using (SqlConnection con = new SqlConnection(ConnectionString))
                    {
                        con.Open();

                        SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.Participants SET LastName=@LastName, FirstName=@FirstName, MiddleName=@MiddleName, Program=@Program, Role=@Role, Events=@Events WHERE StudentID=@StudentID", con);

                        cmd.Parameters.AddWithValue("@StudentID", tbStudentNum.Text);
                        cmd.Parameters.AddWithValue("@LastName", tbLastName.Text);
                        cmd.Parameters.AddWithValue("@FirstName", tbFirstName.Text);
                        cmd.Parameters.AddWithValue("@MiddleName", tbMiddleName.Text);
                        cmd.Parameters.AddWithValue("@Program", cbProgram.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@Role", selectedRoleOrTeam);
                        cmd.Parameters.AddWithValue("@Events", selectedEvent);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Participant updated successfully.");
                        }
                        else
                        {
                            MessageBox.Show("No record found to update.");
                        }

                        LoadParticipantsData();
                    }

                    ClearFields();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating participant: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please select a participant to update.");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (TableParticipant.SelectedRows.Count > 0)
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    try
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("DELETE FROM Participants WHERE StudentID=@StudentID", con);
                        cmd.Parameters.AddWithValue("@StudentID", tbStudentNum.Text);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Participant deleted successfully.");
                        LoadParticipantsData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting participant: " + ex.Message);
                    }
                }

                ClearFields();
            }
            else
            {
                MessageBox.Show("Please select a participant to delete.");
            }
        }

        // ===== START: Navigation Button Clicks (Standard) =====
        private void btnDashboard_Click_1(object sender, EventArgs e)
        {
            mPartcipant.Visible = false;
            ListEventPanel.Visible = false;
            dashboardP.Visible = true;
        }

        private void btnEvents_Click_1(object sender, EventArgs e)
        {
            mPartcipant.Visible = false;
            ListEventPanel.Visible = true;
            dashboardP.Visible = false;
            LoadEventsData();
        }

        private void btnPaticipant_Click_1(object sender, EventArgs e)
        {
            mPartcipant.Visible = true;
            ListEventPanel.Visible = false;
            dashboardP.Visible = false;
            LoadParticipantsData();
        }

        private void btnAccount_Click_1(object sender, EventArgs e)
        {
            mPartcipant.Visible = false;
            ListEventPanel.Visible = false;
            dashboardP.Visible = false;
        }

        private void btnHistory_Click_1(object sender, EventArgs e)
        {
            mPartcipant.Visible = false;
            ListEventPanel.Visible = false;
            dashboardP.Visible = false;
        }

        private void btnAdd_Click_1(object sender, EventArgs e)
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
                        "INSERT INTO [dbo].[Participants]" +
                        "         ([StudentNo]" +
                        "          ,[LastName]" +
                        "           ,[FirstName]" +
                        "           ,[MiddleName]" +
                        "          ,[Program]" +
                        "           ,[Event]" +
                        "          ,[Team])" +
                        "     VALUES" +
                        "           (@studID," +
                        "          @tbLastName," +
                        "         @tbFirstName," +
                        "          @tbMiddleName," +
                        "          @cbProgram," +
                        "          @cbEvent," +
                        "          @cbTeam)", con);

                    cmd.Parameters.AddWithValue("@studID", tbStudentNum.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbLastName", tbLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbFirstName", tbFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbMiddleName", tbMiddleName.Text.Trim());
                    cmd.Parameters.AddWithValue("@cbProgram", cbProgram.Text.Trim());
                    cmd.Parameters.AddWithValue("@cbEvent", cbEvent.Text);
                    cmd.Parameters.AddWithValue("@cbTeam", cbTeam.Text);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Successfully saved participant details.");

                    LoadParticipantsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating event: " + ex.Message);
            }

        }

        private string currentStudID = "";

        private void btnUpdate_Click(object sender, EventArgs e)
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
                        "UPDATE [dbo].[Participants] SET " +
                        "[StudentNo] = @studID, " +
                        "[LastName] = @tbLastName, " +
                        "[FirstName] = @tbFirstName, " +
                        "[MiddleName] = @tbMiddleName, " +
                        "[Program] = @cbProgram, " +
                        "[Event] = @cbEvent, " +
                        "[Team] = @cbTeam" +
                        " WHERE [StudentNo] = @currentStudID", con);

                    cmd.Parameters.AddWithValue("@studID", tbStudentNum.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbLastName", tbLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbFirstName", tbFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@tbMiddleName", tbMiddleName.Text.Trim());
                    cmd.Parameters.AddWithValue("@cbProgram", cbProgram.Text.Trim());
                    cmd.Parameters.AddWithValue("@cbEvent", cbEvent.Text);
                    cmd.Parameters.AddWithValue("@cbTeam", cbTeam.Text);
                    cmd.Parameters.AddWithValue("@currentStudID", currentStudID.Trim());    

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Successfully updated participant details.");

                    LoadParticipantsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating event: " + ex.Message);
            }
        }

        private void btnDelete_Click_1(object sender, EventArgs e)
        {

            if (string.IsNullOrWhiteSpace(tbStudentNum.Text))
            {
                MessageBox.Show("Please enter the Student Number of the record you wish to delete.", "Missing ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Successfully deleted participant details.");

                    LoadParticipantsData();
                    UpdateTotalEventLabel();
                    UpdateEventDropdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating event: " + ex.Message);
            }
        }
    }
}