using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsWeb.Areas.Main.Models.Report;
using System.IO;
using CmsData;
using UtilityExtensions;
using CmsWeb.Models;
using System.Text;
using System.Web.UI;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CmsData.Codes;

namespace CmsWeb.Areas.Dialog.Controllers
{
	[Authorize(Roles = "Edit")]
	public class AddAttendeesFromTagController : CmsController
	{
		public ActionResult Index(int id)
		{
			ViewBag.tag = CmsWeb.Models.OrganizationPage.OrganizationModel.Tags();
			ViewBag.meetingid = id;
			return View();
		}
		[HttpPost]
		public ActionResult Start(int tag, int meetingid, bool addasmembers)
		{
			var runningtotals = new AddToOrgFromTagRun
			{
				Started = DateTime.Now,
				Count = 0,
				Processed = 0,
				Orgid = meetingid
			};
			DbUtil.Db.AddToOrgFromTagRuns.InsertOnSubmit(runningtotals);
			DbUtil.Db.SubmitChanges();
			var host = Util.Host;
			var qid = Util.QueryBuilderScratchPadId;
			System.Threading.Tasks.Task.Factory.StartNew(() =>
			{
				System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;
				var Db = new CMSDataContext(Util.GetConnectionString(host));
				IEnumerable<int> q = null;
				if (tag == -1) // (last query)
					q = Db.PeopleQuery(qid).Select(pp => pp.PeopleId);
				else
					q = from t in Db.TagPeople
						where t.Id == tag
						select t.PeopleId;
				var pids = q.ToList();
				var meeting = Db.Meetings.SingleOrDefault(mm => mm.MeetingId == meetingid);
				var dt = meeting.MeetingDate.Value;
				var orgid = meeting.OrganizationId;
				foreach (var pid in pids)
				{
					Db.Dispose();
					Db = new CMSDataContext(Util.GetConnectionString(host));
					if (addasmembers)
						OrganizationMember.InsertOrgMembers(Db,
							orgid, pid, MemberTypeCode.Member, dt, null, false);
					Db.RecordAttendance(meetingid, pid, true);
					var r = Db.AddToOrgFromTagRuns.Where(mm => mm.Orgid == meetingid).OrderByDescending(mm => mm.Id).First();
					r.Processed++;
					r.Count = pids.Count;
					Db.SubmitChanges();
				}
				var rr = Db.AddToOrgFromTagRuns.Where(mm => mm.Orgid == meetingid).OrderByDescending(mm => mm.Id).First();
				rr.Completed = DateTime.Now;
				Db.SubmitChanges();
				Db.UpdateMainFellowship(orgid);
			});
			return Redirect("/AddAttendeesFromTag/Progress/" + meetingid);
		}
		[HttpPost]
		public JsonResult Progress2(int id)
		{
			var r = DbUtil.Db.AddToOrgFromTagRuns.Where(mm => mm.Orgid == id).OrderByDescending(mm => mm.Id).First();
			return Json(new { r.Count, r.Error, r.Processed, Completed = r.Completed.ToString(), r.Running });
		}
		[HttpGet]
		public ActionResult Progress(int id)
		{
			ViewBag.meetingid = id;
			var r = DbUtil.Db.AddToOrgFromTagRuns.Where(mm => mm.Orgid == id).OrderByDescending(mm => mm.Id).First();
			return View(r);
		}
	}
}
