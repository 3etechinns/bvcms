﻿using System.Linq;
using System.Web.Mvc;
using CmsData;
using CmsWeb.Areas.Dialog.Models;
using CmsWeb.Areas.Org.Models;
using CmsWeb.Areas.Search.Models;
using CmsWeb.Code;
using UtilityExtensions;

namespace CmsWeb.Areas.Dialog.Controllers
{
    public partial class DialogController
    {
        [HttpGet, Route("ForNewMeeting/{orgid:int}")]
        public ActionResult ForNewMeeting(int orgid)
        {
            var oi = new SettingsAttendanceModel() { Id = orgid };
            var m = new NewMeetingInfo()
            {
                MeetingDate = oi.PrevMeetingDate,
                Schedule = new CodeInfo(0, oi.SchedulesPrev()),
                AttendCredit = new CodeInfo(0, oi.AttendCreditList()),
            };
            ViewBag.Action = "/CreateNewMeeting/";
            ViewBag.Method = "POST";
            return View("MeetingInfo", m);
        }
        [HttpGet, Route("ForNewRollsheet/{orgid:int}")]
        public ActionResult ForNewRollsheet(int orgid)
        {
            var oi = new SettingsAttendanceModel() { Id = orgid };
            var m = new NewMeetingInfo()
            {
                MeetingDate =  oi.NextMeetingDate,
                Schedule = new CodeInfo(0, oi.SchedulesNext()),
                AttendCredit = new CodeInfo(0, oi.AttendCreditList()),
            };
            ViewBag.Action = "/Reports/RollsheetForOrg/" + orgid;
            ViewBag.Method = "POST";
            return View("MeetingInfo", m);
        }
        [HttpGet, Route("ForNewRallyRollsheet/{orgid:int}")]
        public ActionResult ForNewRallyRollsheet(int orgid)
        {
            var oi = new SettingsAttendanceModel { Id = orgid };
            var m = new NewMeetingInfo()
            {
                MeetingDate =  oi.NextMeetingDate,
                Schedule = new CodeInfo(0, oi.SchedulesNext()),
                AttendCredit = new CodeInfo(0, oi.AttendCreditList()),
            };
            ViewBag.Action = "/Reports/RallyRollsheetForOrg/" + orgid;
            ViewBag.Method = "POST";
            return View("MeetingInfo", m);
        }
        [HttpPost, Route("ForNewRollsheets/{schedule:int}")]
        public ActionResult ForNewRollsheets(int schedule)
        {
            var m = new NewMeetingInfo()
            {
                MeetingDate =  OrgSearchModel.DefaultMeetingDate(schedule),
                Schedule = null,
                AttendCredit = null
            };
            return View("MeetingInfo", m);
        }
        [HttpPost, Route("ForNewRallyRollsheets/{schedule:int}")]
        public ActionResult ForNewRallyRollsheets(int schedule)
        {
            var m = new NewMeetingInfo()
            {
                MeetingDate =  OrgSearchModel.DefaultMeetingDate(schedule),
                Schedule = null,
                AttendCredit = null,
            };
            return View("MeetingInfo", m);
        }
        [HttpPost, Route("~/CreateNewMeeting")]
        public ActionResult CreateNewMeeting(NewMeetingInfo model)
        {
            if (!ModelState.IsValid)
                return View("MeetingInfo", model);
            var organization = DbUtil.Db.LoadOrganizationById(Util2.CurrentOrganization.Id);
            if (organization == null)
                return Content("error: no org");
            var mt = DbUtil.Db.Meetings.SingleOrDefault(m => m.MeetingDate == model.MeetingDate
                    && m.OrganizationId == organization.OrganizationId);

            if (mt != null)
                return Redirect("/Meeting/" + mt.MeetingId);

            mt = new Meeting
            {
                CreatedDate = Util.Now,
                CreatedBy = Util.UserId1,
                OrganizationId = organization.OrganizationId,
                GroupMeetingFlag = model.ByGroup,
                Location = organization.Location,
                MeetingDate = model.MeetingDate,
                AttendCreditId = model.AttendCredit.Value.ToInt()
            };
            DbUtil.Db.Meetings.InsertOnSubmit(mt);
            DbUtil.Db.SubmitChanges();
            DbUtil.LogActivity("Creating new meeting for {0}".Fmt(organization.OrganizationName));
            return Redirect("/Meeting/" + mt.MeetingId);
        }
    }
}
