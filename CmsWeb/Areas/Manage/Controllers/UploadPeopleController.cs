using System;
using System.Linq;
using System.Web.Mvc;
using System.Threading;
using CmsWeb.Models;
using UtilityExtensions;
using CmsData;
using Alias = System.Threading.Tasks;

namespace CmsWeb.Areas.Manage.Controllers
{
	[Authorize(Roles = "Admin")]
    [RouteArea("Manage", AreaPrefix= "UploadPeople"), Route("{action=index}")]
	public class UploadPeopleController : CmsStaffController
	{
		[HttpGet]
		public ActionResult Index()
		{
			ViewData["text"] = "";
			return View();
		}

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult Upload(string text, bool noupdate)
		{
			string host = Util.Host;
			var runningtotals = new UploadPeopleRun { Started = DateTime.Now, Count = 0, Processed = 0 };
			DbUtil.Db.UploadPeopleRuns.InsertOnSubmit(runningtotals);
			DbUtil.Db.SubmitChanges();
			var pid = Util.UserPeopleId;
		    var cs = Util.GetConnectionString(host);

			Alias.Task.Factory.StartNew(() =>
			{
				Thread.CurrentThread.Priority = ThreadPriority.Lowest;
				var Db = new CMSDataContext(cs);
			    Db.Host = host;
				try
				{
					var m = new UploadPeopleModel(Db, pid ?? 0, noupdate, cs);
					m.DoUpload(text, testing: true);
					Db.Dispose();
    				Db = new CMSDataContext(cs);
    			    Db.Host = host;

        			runningtotals = new UploadPeopleRun { Started = DateTime.Now, Count = 0, Processed = 0 };
        			Db.UploadPeopleRuns.InsertOnSubmit(runningtotals);
        			Db.SubmitChanges();

					m = new UploadPeopleModel(Db, pid ?? 0, noupdate, cs);
					m.DoUpload(text);
				}
				catch (Exception ex)
				{
					Db.Dispose();
    				Db = new CMSDataContext(cs);
    			    Db.Host = host;

				    var q = from r in Db.UploadPeopleRuns
				            where r.Id == Db.UploadPeopleRuns.Max(rr => rr.Id)
				            select r;
				    var rt = q.Single();
					rt.Error = ex.Message.Truncate(200);
        			Db.SubmitChanges();
				}
			});
			return Redirect("/UploadPeople/Progress");
		}

		[HttpGet]
		public ActionResult Progress()
		{
			var rt = DbUtil.Db.UploadPeopleRuns.OrderByDescending(mm => mm.Id).First();
			return View(rt);
		}

		[HttpPost]
		public JsonResult Progress2()
		{
			var r = DbUtil.Db.UploadPeopleRuns.OrderByDescending(mm => mm.Id).First();
			return Json(new {r.Count, r.Error, r.Processed, Completed = r.Completed.ToString(), r.Running});
		}

	}
}

