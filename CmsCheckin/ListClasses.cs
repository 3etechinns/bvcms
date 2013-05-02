using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CmsCheckin
{
    public partial class ListClasses : UserControl
    {
        public ListClasses()
        {
            InitializeComponent();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            const int WM_KEYDOWN = 0x100;
            const int WM_SYSKEYDOWN = 0x104;

            if ((msg.Msg == WM_KEYDOWN) || (msg.Msg == WM_SYSKEYDOWN))
            {
                switch (keyData)
                {
                    case Keys.PageUp:
                        if (pgup.Visible)
                            ShowPage(page - 1);
                        return true;
                    case Keys.PageDown:
                        if (pgdn.Visible)
                            ShowPage(page + 1);
                        return true;
                    case Keys.Escape:
                        this.Swap(Program.home.family);
                        Program.home.family.ShowFamily(FamilyId);
                        return true;
                    case Keys.S | Keys.Alt:
                        Program.TimerReset();
                        Program.CursorShow();
                        return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private List<ClassInfo> list;
        private float points;
        private Font Labfont;
        private string Verdana;
        private XDocument x;
        DateTime time;
        int FamilyId;
        int PeopleId;
        int page;
        List<Control> controls = new List<Control>();
        bool ShowAllClasses;
        public bool JoiningNotAttending = false;

        public void ShowResults(int pid)
        {
            var url = "Checkin2/Classes/" + pid + Program.QueryString;
            if (ShowAllClasses)
                url += "&noagecheck=true";
            x = this.GetDocument(url);

            time = DateTime.Now;

            points = 14F;

            Verdana = "Verdana";
            FamilyId = x.Root.Attribute("fid").Value.ToInt();
            PeopleId = x.Root.Attribute("pid").Value.ToInt();

            if (x.Descendants("class").Count() == 0)
            {
                ClearControls();
                var lab = new Label();
                lab.Font = new Font(Verdana, points, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                lab.Location = new Point(15, 200);
                lab.AutoSize = true;
                lab.Text = "Sorry, no classes found";
                this.Controls.Add(lab);
                controls.Add(lab);
                GoBackButton.Text = "Go Back";
                return;
            }
            list = new List<ClassInfo>();
            foreach (var e in x.Descendants("class"))
            {
                var hr = DateTime.Today;
                DateTime.TryParse(e.Attribute("hour").Value, out hr);

                var ci = new ClassInfo
                {
                    display = e.Attribute("display").Value,
                    oid = e.Attribute("orgid").Value.ToInt(),
                    pid = PeopleId,
                    nlabels = e.Attribute("nlabels").Value.ToInt(),
                    hour = hr
                };
                var leadtime = double.Parse(e.Attribute("leadtime").Value);
                double howlate = -(Program.EarlyCheckin / 60d);
                if (ci.oid != 0 && leadtime <= Program.LeadTime && leadtime >= howlate)
                    list.Add(new ClassInfo
                    {
                        display = e.Attribute("display").Value,
                        oid = e.Attribute("orgid").Value.ToInt(),
                        pid = PeopleId,
                        nlabels = e.Attribute("nlabels").Value.ToInt(),
                        hour = hr
                    });
            }
            ShowPage(1);
        }
        public void ShowPage(int page)
        {
            ClearControls();
            this.page = page;
            var g = this.CreateGraphics();
            while (true)
            {
                var wid = 0;
                Labfont = new Font(Verdana, points, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                foreach (var c in list)
                {
                    var size = g.MeasureString(c.display, Labfont);
                    wid = Math.Max(wid, (int)size.Width);
                }
                if (wid > 1000)
                {
                    points -= 1F;
                    continue;
                }
                break;
            }
            const int PageSize = 10;

            int srow = (page - 1) * PageSize;
            int erow = srow + PageSize;
            if (erow > list.Count)
                erow = list.Count;
            pgdn.Visible = list.Count > erow;
            pgup.Visible = srow > 0;
            int rowheight = 50;
            int top = 50;

            for (var r = srow; r < erow; r++)
            {
                var c = list[r];
                var ab = new Button();
                controls.Add(ab);
                ab.BackColor = SystemColors.ControlLight;
                ab.Font = Labfont;
                top += rowheight;
                ab.Location = new Point(10, top);
                ab.Size = new Size(1000, 45);
                ab.TextAlign = ContentAlignment.MiddleLeft;
                ab.UseVisualStyleBackColor = false;
                ab.Tag = c;
                ab.Text = c.display;
                this.Controls.Add(ab);
                ab.Click += new EventHandler(ab_Click);
            }
            Program.TimerStart(timer1_Tick);
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            Program.TimerStop();
            Util.UnLockFamily();
            Program.ClearFields();
            ShowAllClasses = false;
            this.GoHome("");
        }

        void ab_Click(object sender, EventArgs e)
        {
            var ab = sender as Button;
            var c = ab.Tag as ClassInfo;
            if (JoiningNotAttending)
                Util.JoinUnJoin(c, true);
            else
                Util.AttendUnAttend(new Util.ClassCheckedInfo { c = c, ischecked = true });
            ShowAllClasses = false;
            JoiningNotAttending = false;
			if (Program.baseform.textbox.Parent is Home)
			{
				this.Swap(Program.home.family);
				Program.home.family.classlist.Add(c);
				Program.home.family.ShowFamily(FamilyId);
			}
			else if (Program.baseform.textbox.Parent is Home2)
			{
				this.Swap(Program.home2.family);
				Program.home2.family.classlist.Add(c);
				Program.home2.family.ShowFamily(FamilyId);
			}
        }

        private void GoBack_Click(object sender, EventArgs e)
        {
			JoiningNotAttending = false;
			if (Program.baseform.textbox.Parent is Home)
			{
				this.Swap(Program.home.family);
				Program.home.family.ShowFamily(FamilyId);
			}
			else if (Program.baseform.textbox.Parent is Home2)
			{
				this.Swap(Program.home2.family);
				Program.home2.family.ShowFamily(FamilyId);
			}
            ShowAllClasses = false;
        }
        private void ClearControls()
        {
			JoiningNotAttending = false;
            foreach (var c in controls)
            {
                this.Controls.Remove(c);
                c.Dispose();
            }
            controls.Clear();
        }
        private void pgdn_Click(object sender, EventArgs e)
        {
            ShowPage(page + 1);
        }

        private void pgup_Click(object sender, EventArgs e)
        {
            ShowPage(page - 1);
        }

        private void allclasses_Click(object sender, EventArgs e)
        {
            ShowAllClasses = true;
            ShowResults(PeopleId);
        }

    }
    public class ClassInfo
    {
        public string display { get; set; }
        public int oid { get; set; }
        public int pid { get; set; }
        public string mv { get; set; }
        public string bdays { get; set; }
        public int nlabels { get; set; }
        public DateTime? hour { get; set; }
    }
}
