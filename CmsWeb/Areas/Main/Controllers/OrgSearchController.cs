using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsWeb.Models;
using CmsData;
using UtilityExtensions;
using System.Text.RegularExpressions;

namespace CmsWeb.Areas.Main.Controllers
{
    [SessionExpire]
    public class OrgSearchController : CmsStaffController
    {
        [Serializable]
        class OrgSearchInfo
        {
            public string Name { get; set; }
            public int? Program { get; set; }
            public int? Division { get; set; }
            public int? Type { get; set; }
            public int? Campus { get; set; }
            public int? Sched { get; set; }
            public int? Status { get; set; }
            public int? OnlineReg { get; set; }
        }
        private const string STR_OrgSearch = "OrgSearch2";
        public ActionResult Index(int? div, int? progid)
        {
            Response.NoCache();
            var m = new OrgSearchModel();

            if (div.HasValue)
            {
                m.DivisionId = div;
                if (progid.HasValue)
                    m.ProgramId = progid;
                else
                    m.ProgramId = m.Division().ProgId;
                m.TagProgramId = m.ProgramId;
                m.TagDiv = div;
            }
            else if (progid.HasValue)
            {
                m.ProgramId = progid;
                m.TagProgramId = m.ProgramId;
            }
            else if (Session[STR_OrgSearch].IsNotNull())
            {
                var os = Session[STR_OrgSearch] as OrgSearchInfo;
                m.Name = os.Name;
                m.ProgramId = os.Program;
                m.DivisionId = os.Division;
                m.ScheduleId = os.Sched;
                m.StatusId = os.Status;
                m.OnlineReg = os.OnlineReg;
                m.TypeId = os.Type;
                m.CampusId = os.Campus;
            }
            return View(m);
        }
        [HttpPost]
        public ActionResult Results(OrgSearchModel m)
        {
            SaveToSession(m);
            return View(m);
        }
        [HttpPost]
        public ActionResult DivisionIds(int id)
        {
            var m = new OrgSearchModel { ProgramId = id };
            return View(m);
            //return Json(OrgSearchModel.DivisionIds(id));
        }
        [HttpPost]
        public ActionResult TagDivIds(int id)
        {
            var m = new OrgSearchModel { ProgramId = id };
            return View("DivisionIds", m);
        }
        [HttpPost]
        public ActionResult ApplyType(int id, OrgSearchModel m)
        {
            foreach (var o in m.FetchOrgs())
                o.OrganizationTypeId = id == -1 ? (int?)null : id;
            DbUtil.Db.SubmitChanges();
            return View("Results", m);
        }
        [HttpPost]
        public ActionResult RenameDiv(int id, int divid, string name)
        {
            var d = DbUtil.Db.Divisions.Single(dd => dd.Id == divid);
            d.Name = name;
            DbUtil.Db.SubmitChanges();
            var m = new OrgSearchModel { ProgramId = id };
            return View("DivisionIds", m);
        }
        [HttpPost]
        public ActionResult MakeNewDiv(int id, string name)
        {
            var d = new Division { Name = name, ProgId = id };
            d.ProgDivs.Add(new ProgDiv { ProgId = id });
            DbUtil.Db.Divisions.InsertOnSubmit(d);
            DbUtil.Db.SubmitChanges();
            var m = new OrgSearchModel { ProgramId = id, TagDiv = d.Id };
            return View("DivisionIds", m);
        }
        [HttpPost]
        public ActionResult DefaultMeetingDate(int id)
        {
            var dt = OrgSearchModel.DefaultMeetingDate(id);
            return Json(new { date = dt.Date.ToShortDateString(), time = dt.ToShortTimeString() });
        }
        [HttpPost]
        public ActionResult ExportExcel(OrgSearchModel m)
        {
            return new OrgExcelResult(m);
        }
        private void SaveToSession(OrgSearchModel m)
        {
            Session[STR_OrgSearch] = new OrgSearchInfo
            {
                Name = m.Name,
                Program = m.ProgramId,
                Division = m.DivisionId,
                Sched = m.ScheduleId,
                Status = m.StatusId,
                OnlineReg = m.OnlineReg,
                Type = m.TypeId,
                Campus = m.CampusId,
            };
        }
        [HttpPost]
        public ContentResult Edit(string id, string value)
        {
            var a = id.Split('-');
            var c = new ContentResult();
            c.Content = value;
            var org = DbUtil.Db.LoadOrganizationById(a[1].ToInt());
            if (org == null)
                return c;
            switch (a[0])
            {
                case "bs":
                    org.BirthDayStart = value.ToDate();
                    break;
                case "be":
                    org.BirthDayEnd = value.ToDate();
                    break;
                case "ck":
                    org.CanSelfCheckin = value == "yes";
                    break;
            }
            DbUtil.Db.SubmitChanges();
            return c;
        }
        [HttpPost]
        public ActionResult ToggleTag(int id, int tagdiv)
        {
            var Db = DbUtil.Db;
            var organization = Db.LoadOrganizationById(id);
            if (tagdiv == 0)
                return Json(new { error = "bad tagdiv" });
            bool t = organization.ToggleTag(DbUtil.Db, tagdiv);
            Db.SubmitChanges();
            var m = new OrgSearchModel { StatusId = 0, TagDiv = tagdiv, Name = id.ToString() };
            var o = m.OrganizationList().SingleOrDefault();
            if (o == null)
                return Content("error");
            return View("Row", o);
        }
        [HttpPost]
        public ActionResult MainDiv(int id, int tagdiv)
        {
            var Db = DbUtil.Db;
            Db.SetMainDivision(id, tagdiv);
            var m = new OrgSearchModel { TagDiv = tagdiv, Name = id.ToString() };
            var o = m.OrganizationList().SingleOrDefault();
            if (o == null)
                return Content("error");
            return View("Row", o);
        }
        [HttpPost]
        public ActionResult PasteSettings(OrgSearchModel m)
        {
            var frorg = (int)Session["OrgCopySettings"];
            foreach (var o in m.FetchOrgs())
                o.CopySettings(DbUtil.Db, frorg);
            return new EmptyResult();
        }
        [HttpPost]
        public ActionResult RepairTransactions(OrgSearchModel m)
        {
            foreach (var o in m.FetchOrgs())
                DbUtil.Db.PopulateComputedEnrollmentTransactions(o.OrganizationId);
            return new EmptyResult();
        }
        [HttpPost]
        public ActionResult CreateMeeting(string id)
        {
            var n = id.ToCharArray().Count(c => c == 'M');
            if (n > 1)
                return RedirectShowError("More than one barcode string found({0})".Fmt(id));
            var a = id.SplitStr(".");
            var orgid = a[1].ToInt();
            var organization = DbUtil.Db.LoadOrganizationById(orgid);
            if (organization == null)
                return RedirectShowError("Cannot interpret barcode orgid({0})".Fmt(id));

            var re = new Regex(@"\A(0[1-9]|1[012])(0[1-9]|[12][0-9]|3[01])([0-9]{2})([012][0-9])([0-5][0-9])\Z");
            if (!re.IsMatch(a[2]))
                return RedirectShowError("Cannot interpret barcode datetime({0})".Fmt(id));
            var g = re.Match(a[2]);
            var dt = new DateTime(
                g.Groups[3].Value.ToInt() + 2000,
                g.Groups[1].Value.ToInt(),
                g.Groups[2].Value.ToInt(),
                g.Groups[4].Value.ToInt(),
                g.Groups[5].Value.ToInt(),
                0);
            var newMtg = DbUtil.Db.Meetings.SingleOrDefault(m => m.OrganizationId == orgid && m.MeetingDate == dt);
            if (newMtg == null)
            {
                var attsch = organization.OrgSchedules.SingleOrDefault(ss => ss.MeetingTime.Value.TimeOfDay == dt.TimeOfDay && ss.MeetingTime.Value.DayOfWeek == dt.DayOfWeek);
                int? attcred = null;
                if (attsch != null)
                    attcred = attsch.AttendCreditId;

                newMtg = new CmsData.Meeting
                {
                    CreatedDate = Util.Now,
                    CreatedBy = Util.UserId1,
                    OrganizationId = orgid,
                    GroupMeetingFlag = false,
                    Location = organization.Location,
                    MeetingDate = dt,
                    AttendCreditId = attcred,
                };
                DbUtil.Db.Meetings.InsertOnSubmit(newMtg);
                DbUtil.Db.SubmitChanges();
                DbUtil.LogActivity("Creating new meeting for {0}".Fmt(dt));
            }
            return Redirect("/Meeting/Index/{0}?showall=true".Fmt(newMtg.MeetingId));
        }

        [HttpPost]
        [Authorize(Roles = "Attendance")]
        public ActionResult EmailAttendanceNotices(OrgSearchModel m)
        {
            m.SendNotices(this);
            return Content("ok");
        }

        public ActionResult OrganizationStructure(bool? active, OrgSearchModel m)
        {
            var orgs = m.FetchOrgs();
            var q = from os in DbUtil.Db.ViewOrganizationStructures
                    join o in orgs on os.OrgId equals o.OrganizationId
                    select os;
            return View(q.OrderBy(oo => oo.Program).ThenBy(oo => oo.Division).ThenBy(oo => oo.Organization));
        }

    }
}
