using System;
using System.Linq;
using System.Web.Mvc;
using CmsWeb.Areas.Finance.Models.Report;
using CmsData;
using UtilityExtensions;
using System.Web.Configuration;
using System.Text;

namespace CmsWeb.Areas.Finance.Controllers
{
	[Authorize(Roles="Finance")]
	public class StatementsController : CmsController 
	{
		public ActionResult Index(string startswith)
		{
		    if (startswith.HasValue())
		        ViewBag.startswith = startswith;
			return View();
		}
		[HttpPost]
		public ActionResult ContributionStatements(bool? PDF, DateTime? FromDate, DateTime? EndDate, string startswith)
		{
			if (!FromDate.HasValue || !EndDate.HasValue)
				return Content("<h3>Must have a Startdate and Enddate</h3>");
			else
			{
				var runningtotals = new ContributionsRun
				{
					Started = DateTime.Now,
					Count = 0,
					Processed = 0
				};
			    if (!startswith.HasValue())
			        startswith = null;
				DbUtil.Db.ContributionsRuns.InsertOnSubmit(runningtotals);
				DbUtil.Db.SubmitChanges();
				var host = Util.Host;
				var output = Output(PDF);
				System.Threading.Tasks.Task.Factory.StartNew(() =>
				{
					System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
					var m = new ContributionStatementsExtract(host, FromDate.Value, EndDate.Value, PDF.Value, output, startswith);
					m.DoWork();
				});
			}
			return Redirect("/Statements/Progress");
		}
		public ActionResult SomeTaskCompleted(string result)
		{
			return Content(result);
		}
		private string Output(bool? PDF)
		{
			string output = WebConfigurationManager.AppSettings["SharedFolder"];
			if (PDF == true)
				output = output + "/Statements/contributions_{0}.pdf".Fmt(Util.Host);
			else
				output = output + "/Statements/contributions_{0}.txt".Fmt(Util.Host);
			return output;
		}
		[HttpPost]
		public JsonResult Progress2()
		{
			var r = DbUtil.Db.ContributionsRuns.OrderByDescending(mm => mm.Id).First();
			var html = new StringBuilder("<a href=\"/Statements/Download\">All Pages</a>");
			if (r.Sets.HasValue())
			{
				var sets = r.Sets.Split(',').Select(ss => ss.ToInt()).ToList();
				foreach (var set in sets)
					html.AppendFormat(" | <a href=\"/Statements/Download/{0}\">Set {0}</a>", set);
			}
			return Json(new 
			{ 
				r.Count, 
				Error = r.Error ?? "", 
				r.Processed, 
				r.CurrSet, 
				Completed = r.Completed.ToString(), 
				r.Running,
				html = html.ToString()
			});
		}
		[HttpGet]
		public ActionResult Progress()
		{
			var r = DbUtil.Db.ContributionsRuns.OrderByDescending(mm => mm.Id).First();
			return View(r);
		}
		public ActionResult Download(int? id, bool? PDF = true)
		{
			string output = Output(PDF);
			string fn = output;
			if (id.HasValue)
				fn = ContributionStatementsExtract.Output(output, id.Value);
			if (!System.IO.File.Exists(fn))
				return Content("no pending download");
			return new ContributionStatementsResult(fn);
		}
	}
}
