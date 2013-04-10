/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using UtilityExtensions;
using System.Web.Routing;
using CmsWeb;
using CmsWeb.Areas.Org.Models;
using CmsData;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;

namespace CmsWeb.Areas.Org.Controllers
{
	public class VolunteersController : CmsStaffController
	{
		[AcceptVerbs(HttpVerbs.Post)]
		public JsonResult Codes(string id)
		{
			var q = from p in DbUtil.Db.VolInterestInterestCodes
					where p.VolInterestCode.Org == id
					select new
					{
						Key = p.VolInterestCode.Org + p.VolInterestCode.Code,
						PeopleId = "p" + p.PeopleId,
						Name = p.Person.Name
					};
			return Json(q);
		}

		public class PostTargetInfo
		{
			public int id { get; set; }
			public DragDropInfo[] list { get; set; }
			public string target { get; set; }
			public int? week { get; set; }
			public DateTime? time { get; set; }
			public bool SortByWeek { get; set; }
			public string sg1 {get; set;}
			public string sg2 {get; set;}
		}

		public ActionResult Calendar(int id, string sg1, string sg2, bool? SortByWeek)
		{
			var m = new VolunteerCommitmentsModel(id);
			m.SmallGroup1 = sg1;
			m.SmallGroup2 = sg2;
			m.SortByWeek = SortByWeek ?? false;
			return View(m);
		}

		[HttpPost]
		public ActionResult ManageArea(PostTargetInfo i)
		{
			var m = new VolunteerCommitmentsModel(i.id);
			m.SmallGroup1 = i.sg1;
			m.SmallGroup2 = i.sg2;
			m.SortByWeek = i.SortByWeek;
			foreach (var s in i.list)
				m.ApplyDragDrop(i.target, i.week, i.time, s);
			return View(m);
		}

		[HttpPost]
		public ActionResult ManageArea2(int id, string sg1, string sg2, bool? SortByWeek)
		{
			var m = new VolunteerCommitmentsModel(id);
			m.SmallGroup1 = sg1;
			m.SmallGroup2 = sg2;
			m.SortByWeek = SortByWeek ?? false;
			return View("ManageArea", m);
		}

		public ActionResult EmailReminders(int id)
		{
			var qb = DbUtil.Db.QueryBuilderScratchPad();
			qb.CleanSlate(DbUtil.Db);
			var clause = qb.AddNewClause(QueryType.RegisteredForMeetingId, CompareType.Equal, id.ToString());
			DbUtil.Db.SubmitChanges();

			var meeting = DbUtil.Db.Meetings.Single(m => m.MeetingId == id);
			var subject = "{0} Reminder".Fmt(meeting.Organization.OrganizationName);
			var body =
@"<blockquote><table>
<tr><td>Time:</td><td>{0:f}</td></tr>
<tr><td>Location:</td><td>{1}</td></tr>
</table></blockquote><p>{2}</p>".Fmt(
								meeting.MeetingDate,
								meeting.Organization.Location,
								meeting.Organization.LeaderName);

			TempData["body"] = body;
			return Redirect("/Email/Index/{0}?subj={1}&ishtml=true"
				.Fmt(qb.QueryId, Server.UrlEncode(subject)));
		}
		[HttpGet]
		public ActionResult Request(int mid, int limit)
		{
			var vs = new VolunteerRequestModel(mid, Util.UserPeopleId.Value) {limit = limit };
			vs.ComposeMessage();
			return View(vs);
		}
		[HttpGet]
		public ActionResult Request0(long ticks, int oid, int limit)
		{
			var time = new DateTime(ticks); // ticks here is meeting time
			var mid = DbUtil.Db.CreateMeeting(oid, time);
			var vs = new VolunteerRequestModel(mid, Util.UserPeopleId.Value) {limit = limit };
			vs.ComposeMessage();
			return View("Request", vs);
		}
		[HttpPost]
		[ValidateInput(false)]
		public ActionResult Request(int mid, long ticks, int[] pids, string subject, string message, int limit, int? additional)
		{
			var m = new VolunteerRequestModel(mid, Util.UserPeopleId.Value, ticks)
				{subject = subject, message = message, pids = pids, limit = limit };
			m.SendEmails(additional ?? 0);
			return Content("Emails are being sent, thank you.");
		}
        public ActionResult EmailSlot(int id)
        {
            var m = DbUtil.Db.Meetings.Single(mm => mm.MeetingId == id);
            var qb = DbUtil.Db.QueryBuilderScratchPad();
            qb.CleanSlate(DbUtil.Db);
            qb.AddNewClause(QueryType.RegisteredForMeetingId, CompareType.Equal, m.MeetingId);
            DbUtil.Db.SubmitChanges();
            return Redirect("/Email/Index/{0}?TemplateId=0&body={1} {2}&subj={1} {2}".Fmt(qb.QueryId, m.Organization.OrganizationName, m.MeetingDate.FormatDateTm()));
        }
	}
}
