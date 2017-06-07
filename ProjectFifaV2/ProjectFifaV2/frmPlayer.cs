﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ProjectFifaV2
{
    public partial class frmPlayer : Form
    {
        // This is letting the user to bet in the numericupdowns, creates an databasehandler to execute some query's if he/she presses an button and remember the users id and username.

        internal DatabaseHandler dbh = new DatabaseHandler();

        const int lengthInnerArray = 2;
        const int lengthOutterArray = 2;

        private Form frmRanking;
        private DataTable tblUsers;
        private DataRow rowUser;

        private string userName;

        internal int resultId;
        internal int counter = 0;

        internal bool quit;
        internal bool saved;

        private int test = 0;


        List<NumericUpDown> txtBoxList;
        List<NumericUpDown>[,] newRows = new List<NumericUpDown>[2, 2];

        public NumericUpDown txtHomePred;
        public NumericUpDown txtAwayPred;

        NumericUpDown[,] rows;


        public frmPlayer(Form frm, string un)
        {
            // This is letting the user to see the preditions, result and scorecard. We need an dbh to excute sqls.

            int amount = dbh.DTInt("SELECT COUNT(*) FROM TblGames");

            rows = new NumericUpDown[amount, lengthInnerArray];

            this.ControlBox = false;

            frmRanking = frm;

            dbh = new DatabaseHandler();

            InitializeComponent();

            // Disables buttons if its passed it expire date.

            if (DisableEditButton())
            {
                btnClearPrediction.Enabled = false;
                btnSaveButton.Enabled = false;
            }

            ShowResults();
            ShowScoreCard();
            ShowPredictions();

            this.Text = un;

            // Checks if some preditions already has been saved.

            DataTable tblUsers = dbh.FillDT("SELECT * FROM TblUsers WHERE (Username='" + this.Text + "')");

            dbh.TestConnection();
            dbh.OpenConnectionToDB();

            using (SqlCommand cmd = new SqlCommand("SELECT id FROM TblUsers WHERE Username =  @Username", dbh.GetCon()))
            {
                cmd.Parameters.AddWithValue("Username", this.Text);

                string sql = Convert.ToString(cmd.ExecuteScalar());

                int.TryParse(sql, out this.resultId);
            }

            int userId = resultId;

            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM [tblPredictions] WHERE User_id = @User_id AND Saved = 1", dbh.GetCon()))
            {
                cmd.Parameters.AddWithValue("User_id", userId);
                saved = (int)cmd.ExecuteScalar() > 0;
            }

            dbh.CloseConnectionToDB();

            if (saved)
            {
                btnSaveButton.Enabled = false;
            }
            else
            {
                btnClearPrediction.Enabled = false;
            }
        }

        private void btnLogOut_Click(object sender, EventArgs e)
        {
            // This hides the form from the user, to make sure if he wants to log in again or to close the application.

            if (quit == true)
            {
                btnSaveButton.Enabled = false;
            }

            Hide();
        }

        private void btnShowRanking_Click(object sender, EventArgs e)
        {
            // This shows the frmRanking for the user.

            frmRanking.Show();
        }

        private void btnClearPrediction_Click(object sender, EventArgs e)
        {
            // This is letting the user to clear his/her preditions.

            DialogResult result = MessageBox.Show("Are you sure you want to clear your prediction?", "Clear Predictions", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (result.Equals(DialogResult.Yes))
            {
                // We are trying to get the users id to make sure we delete his/her preditions.

                DataTable tblUsers = dbh.FillDT("SELECT * FROM TblUsers WHERE (Username='" + this.Text + "')");

                dbh.TestConnection();
                dbh.OpenConnectionToDB();

                using (SqlCommand cmd = new SqlCommand("SELECT id FROM TblUsers WHERE Username =  @Username", dbh.GetCon()))
                {
                    cmd.Parameters.AddWithValue("Username", this.Text);

                    string sql = Convert.ToString(cmd.ExecuteScalar());

                    int.TryParse(sql, out this.resultId);
                }

                dbh.CloseConnectionToDB();

                int userId = resultId;
                int zero = 0;

                string home = "";
                string away = "";
                string sqlStr = "DELETE FROM TblPredictions WHERE user_id ='" + userId + "'";

                for (; zero < lengthOutterArray; zero++)
                {
                    for (int k = 0; k < lengthInnerArray; k++)
                    {
                        if (k == 0)
                        {
                            home = rows[zero, k].Text;
                        }
                        else
                        {
                            away = rows[zero, k].Text;
                        }
                    }
                }

                dbh.Execute(sqlStr);

                lvPredictions.Items.Clear();

                ShowPredictions();

                dbh.TestConnection();
                dbh.OpenConnectionToDB();

                // Making sure that 0 predictions from the user will stay in the database.

                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM [tblPredictions] WHERE User_id = @User_id AND Saved = 1", dbh.GetCon()))
                {
                    cmd.Parameters.AddWithValue("User_id", userId);
                    saved = (int)cmd.ExecuteScalar() > 0;
                }

                if (saved)
                {
                    MessageBox.Show("Er gaat iets fout...");
                }
                else
                {
                    btnSaveButton.Enabled = true;
                    btnClearPrediction.Enabled = false;
                }
                dbh.CloseConnectionToDB();
            }
        }

        private bool DisableEditButton()
        {
            // This is the deadline for filling in the predictions.

            bool hasPassed;

            if (!isElton())
            {

                DateTime deadline = new DateTime(2019, 06, 12);
                DateTime curTime = DateTime.Now;

                int result = DateTime.Compare(deadline, curTime);

                if (result < 0)
                {
                    hasPassed = true;
                }
                else
                {
                    hasPassed = false;
                }

                return hasPassed;
            }
            else
            {
                hasPassed = false;
                return hasPassed;
            }
        }

        private void ShowResults()
        {
            // This shows the game results.

            dbh.TestConnection();
            dbh.OpenConnectionToDB();

            DataTable homeTable = dbh.FillDT("SELECT TblTeams.TeamName, TblGames.HomeTeamScore FROM TblGames INNER JOIN TblTeams ON TblGames.HomeTeam = TblTeams.Team_ID");
            DataTable awayTable = dbh.FillDT("SELECT TblTeams.TeamName, TblGames.AwayTeamScore FROM TblGames INNER JOIN TblTeams ON TblGames.AwayTeam = TblTeams.Team_ID");

            dbh.CloseConnectionToDB();

            for (int i = 0; i < homeTable.Rows.Count; i++)
            {
                DataRow dataRowHome = homeTable.Rows[i];
                DataRow dataRowAway = awayTable.Rows[i];

                ListViewItem lstItem = new ListViewItem(dataRowHome["Teamname"].ToString());

                lstItem.SubItems.Add(dataRowHome["HomeTeamScore"].ToString());
                lstItem.SubItems.Add(dataRowAway["AwayTeamScore"].ToString());
                lstItem.SubItems.Add(dataRowAway["Teamname"].ToString());

                lvOverview.Items.Add(lstItem);
            }
        }

        private void ShowPredictions()
        {
            // This shows your predition result.

            dbh.TestConnection();
            dbh.OpenConnectionToDB();

            DataTable homeTable = dbh.FillDT("SELECT TblTeams.TeamName, TblPredictions.PredictedHomeScore FROM TblPredictions, TblGames INNER JOIN TblTeams ON TblGames.HomeTeam = TblTeams.Team_ID");
            DataTable awayTable = dbh.FillDT("SELECT TblTeams.TeamName, TblPredictions.PredictedAwayScore FROM TblPredictions, TblGames INNER JOIN TblTeams ON TblGames.AwayTeam = TblTeams.Team_ID");

            dbh.CloseConnectionToDB();
            int r = dbh.DTInt("SELECT COUNT(Game_id)FROM TBLGAMES"); 

            for (int i = 0; i < homeTable.Rows.Count; i++)
            {
                //i++;
                if (i %  r == 0)
                {
                    DataRow dataRowHome = homeTable.Rows[i];
                    DataRow dataRowAway = awayTable.Rows[i];
                    ListViewItem lstItem = new ListViewItem(dataRowHome["Teamname"].ToString());

                    lstItem.SubItems.Add(dataRowHome["PredictedHomeScore"].ToString());
                    lstItem.SubItems.Add(dataRowAway["PredictedAwayScore"].ToString());
                    lstItem.SubItems.Add(dataRowAway["Teamname"].ToString());

                    lvPredictions.Items.Add(lstItem);
                }
                

               
                //     lvPredictions.Update();
                //  //   dbh.CloseConnectionToDB();
                //     dbh.OpenConnectionToDB();       

                //     DataTable homeTable = dbh.FillDT("SELECT * FROM TblPredictions INNER JOIN TblGames ON TblPredictions.Game_id = TblGames.Game_id");
                //     DataTable awayTable = dbh.FillDT("SELECT * FROM TblPredictions INNER JOIN TblGames ON TblPredictions.Game_id = TblGames.Game_id");
                //     //DataTable t1 = dbh.FillDT("SELECT TblTeams.TeamName FROM TblGames INNER JOIN TblTeams ON TblGames.AwayTeam = TblTeams.Team_ID");

                //     dbh.CloseConnectionToDB();

                //     for (int i = 0; i < homeTable.Rows.Count; i++)
                //     {
                //         DataRow dataRowHome = homeTable.Rows[i];
                //         DataRow dataRowAway = awayTable.Rows[i];

                //         ListViewItem lstItem = new ListViewItem(dataRowHome["Game_id"].ToString());

                //         lstItem.SubItems.Add(dataRowHome["HomeTeamScore"].ToString());
                //         lstItem.SubItems.Add(dataRowAway["AwayTeamScore"].ToString());
                //         lstItem.SubItems.Add(dataRowAway["Game_id"].ToString());

                //         lvPredictions.Items.Add(lstItem);
                //     }
                ////     dbh.CloseConnectionToDB();
            }
        }

        private void ShowScoreCard()
        {
            // This allows the user to make his/her bet.

            DataTable homeTable = dbh.FillDT("SELECT TblTeams.Teamname FROM TblGames INNER JOIN TblTeams ON TblGames.HomeTeam = TblTeams.Team_id");
            DataTable awayTable = dbh.FillDT("SELECT TblTeams.Teamname FROM TblGames INNER JOIN TblTeams ON TblGames.AwayTeam = TblTeams.Team_id");

            for (int i = 0; i < homeTable.Rows.Count; i++)
            {
                DataRow dataRowHome = homeTable.Rows[i];
                DataRow dataRowAway = awayTable.Rows[i];

                Label lblHomeTeam = new Label();
                Label lblAwayTeam = new Label();

                txtHomePred = new NumericUpDown();
                txtAwayPred = new NumericUpDown();

                lblHomeTeam.TextAlign = ContentAlignment.BottomRight;
                lblHomeTeam.Text = dataRowHome["TeamName"].ToString();
                lblHomeTeam.Location = new Point(15, txtHomePred.Bottom + (i * 30));
                lblHomeTeam.AutoSize = true;

                txtHomePred.Text = "0";
                txtHomePred.Location = new Point(lblHomeTeam.Width, lblHomeTeam.Top - 3);
                txtHomePred.Width = 40;
                rows[i, 0] = txtHomePred;

                txtAwayPred.Text = "0";
                txtAwayPred.Location = new Point(txtHomePred.Width + lblHomeTeam.Width, txtHomePred.Top);
                txtAwayPred.Width = 40;
                rows[i, 1] = txtAwayPred;

                lblAwayTeam.Text = dataRowAway["TeamName"].ToString();
                lblAwayTeam.Location = new Point(txtHomePred.Width + lblHomeTeam.Width + txtAwayPred.Width, txtHomePred.Top + 3);
                lblAwayTeam.AutoSize = true;

                pnlPredCard.Controls.Add(lblHomeTeam);
                pnlPredCard.Controls.Add(txtHomePred);
                pnlPredCard.Controls.Add(txtAwayPred);
                pnlPredCard.Controls.Add(lblAwayTeam);

                this.counter++;

                ListViewItem lstItem = new ListViewItem(dataRowHome["TeamName"].ToString());
            }
        }


        private bool isElton()
        {
            // Checking if Elton is logging in.

            dbh.OpenConnectionToDB();

            bool admin;

            string userName = this.Text;

            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM [tblUsers] WHERE Username = @Username AND IsAdmin = 2", dbh.GetCon()))
            {
                cmd.Parameters.AddWithValue("Username", userName);
                admin = (int)cmd.ExecuteScalar() > 0;
            }

            dbh.CloseConnectionToDB();

            if (admin)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void SaveButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to save your prediction?", "Clear Predictions", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (result.Equals(DialogResult.Yes))
            {
                test++;

                if (test > 2)
                {
                    test = 0;
                }

                // We are trying to get the users id to make sure we that we will save his/her preditions.

                DataTable tblUsers = dbh.FillDT("SELECT * FROM tblUsers WHERE (Username='" + this.Text + "')");

                dbh.TestConnection();
                dbh.OpenConnectionToDB();

                using (SqlCommand cmd = new SqlCommand("SELECT id FROM TblUsers WHERE Username =  @Username", dbh.GetCon()))
                {
                    cmd.Parameters.AddWithValue("Username", this.Text);

                    string sqlStr = Convert.ToString(cmd.ExecuteScalar());

                    int.TryParse(sqlStr, out this.resultId);
                }

                dbh.CloseConnectionToDB();

                int userId = resultId;
                int zero = 0;

                string home = txtHomePred.Value.ToString();
                string away = txtAwayPred.Value.ToString();

                this.counter--;

                if (counter < 0)
                {
                    counter = 0;
                }

                for (; zero <= this.counter; zero++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        if (k == 0)
                        {
                            home = rows[zero, k].Text;

                        }
                        else
                        {
                            int f = k - 1;
                            k++;
                            away = rows[zero, f].Text;
                            f = 0;
                        }
                    }
                    string sql = "INSERT INTO tblPredictions (User_id, Game_id, PredictedHomeScore, PredictedAwayScore, Saved) VALUES ('" + userId + "', " + Convert.ToInt32(zero) + ", '" + home + "', '" + away + "', '1')";

                    dbh.Execute(sql);
                }

                btnSaveButton.Enabled = false;
                ShowPredictions();

                // Making sure that the predictions will stay in the database.

                dbh.TestConnection();
                dbh.OpenConnectionToDB();

                bool admin;

                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM [tblPredictions] WHERE User_id = @User_id AND Saved = 1", dbh.GetCon()))
                {
                    cmd.Parameters.AddWithValue("User_id", userId);
                    admin = (int)cmd.ExecuteScalar() > 0;
                }

                if (admin)
                {
                    btnSaveButton.Enabled = false;
                    btnClearPrediction.Enabled = true;
                    dbh.CloseConnectionToDB();
                }
                else
                {
                    MessageBox.Show("Er gaat iets fout...");
                }
                dbh.CloseConnectionToDB();
            }
            
        }
    }
}