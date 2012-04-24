using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Xml.Linq;
using System.Drawing.Printing;
using System.Xml.Serialization;
using System.IO;

namespace CmsCheckin
{
    public partial class StartUp : Form
    {
        public XDocument campuses { get; set; }
        public int CampusId
        {
            get
            {
                var c = cbCampusId.SelectedItem as Campus;
                if (c != null)
                    return c.Id;
                return 0;
            }
        }
        public string AdminPassword
        {
            get
            {
                var c = cbCampusId.SelectedItem as Campus;
                if (c != null)
                    return c.password;
                return "kio.";
            }
        }
        public int DayOfWeek
        {
            get
            {
                var d = cbDayOfWeek.SelectedItem as DayOfWeek;
                return d.Day;
            }
        }
        public StartUp()
        {
            InitializeComponent();
            cbDayOfWeek.Items.Add(new DayOfWeek { Day = 0, Text = "Sunday" });
            cbDayOfWeek.Items.Add(new DayOfWeek { Day = 1, Text = "Monday" });
            cbDayOfWeek.Items.Add(new DayOfWeek { Day = 2, Text = "Tuesday" });
            cbDayOfWeek.Items.Add(new DayOfWeek { Day = 3, Text = "Wednesday" });
            cbDayOfWeek.Items.Add(new DayOfWeek { Day = 4, Text = "Thursday" });
            cbDayOfWeek.Items.Add(new DayOfWeek { Day = 5, Text = "Friday" });
            cbDayOfWeek.Items.Add(new DayOfWeek { Day = 6, Text = "Saturday" });
            cbDayOfWeek.SelectedIndex = (int)DateTime.Now.DayOfWeek;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (cbCampusId.Items.Count > 0)
                Settings1.Default.Campus = ((Campus)(cbCampusId.SelectedItem)).Name;
            Settings1.Default.AskChurch = AskChurch.Checked;
            Settings1.Default.AskChurchName = AskChurchName.Checked;
            Settings1.Default.AskEmFriend = AskEmFriend.Checked;
            Settings1.Default.AskGrade = AskGrade.Checked;
            Settings1.Default.KioskName = KioskName.Text;
            Settings1.Default.LateMinutes = LateMinutes.Text.ToInt();
            Settings1.Default.LeadHours = LeadHours.Text.ToInt();
            Settings1.Default.LateMinutes = LateMinutes.Text.ToInt();
            Settings1.Default.DisableJoin = DisableJoin.Checked;
            Settings1.Default.Save();
            this.Hide();
        }

        private void StartUp_Load(object sender, EventArgs e)
        {
#if DEBUG
            cbDayOfWeek.SelectedIndex = 0;
            HideCursor.Checked = false;
#endif

            foreach (var i in campuses.Descendants("campus"))
                cbCampusId.Items.Add(new Campus
                {
                    Id = int.Parse(i.Attribute("id").Value),
                    Name = i.Attribute("name").Value,
                    password = i.Attribute("password").Value
                });
            if (cbCampusId.Items.Count > 0)
                cbCampusId.SelectedIndex = 0;
            var ii = cbCampusId.FindStringExact(Settings1.Default.Campus);
            if (ii >= 0)
                cbCampusId.SelectedIndex = ii;
            ii = LeadHours.FindStringExact(Settings1.Default.LeadHours.ToString());
            if (ii >= 0)
                LeadHours.SelectedIndex = ii;
            ii = LateMinutes.FindStringExact(Settings1.Default.LateMinutes.ToString());
            if (ii >= 0)
                LateMinutes.SelectedIndex = ii;
            AskEmFriend.Checked = Settings1.Default.AskEmFriend;
            AskGrade.Checked = Settings1.Default.AskGrade;
            AskChurch.Checked = Settings1.Default.AskChurch;
            AskChurchName.Checked = Settings1.Default.AskChurchName;
            KioskName.Text = Settings1.Default.KioskName;
            DisableJoin.Checked = Settings1.Default.DisableJoin;
        }
    }
    class DayOfWeek
    {
        public string Text { get; set; }
        public int Day { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }
    class Campus
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string password { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
