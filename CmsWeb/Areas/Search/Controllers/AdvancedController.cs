/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */

using System;
using System.Data.SqlClient;
using System.Threading;
using System.Web.Mvc;
using CmsWeb.Areas.Search.Models;
using UtilityExtensions;
using CmsData;

namespace CmsWeb.Areas.Search.Controllers
{
	[SessionExpire]
    public class AdvancedController : CmsStaffAsyncController
    {
        public ActionResult Main(int? id, int? run)
        {
            if (!DbUtil.Db.UserPreference("newlook3", "false").ToBool()
                || !DbUtil.Db.UserPreference("advancedsearch", "false").ToBool())
                return Redirect(Request.RawUrl.ToLower().Replace("search/advanced", "querybuilder"));
            ViewData["Title"] = "QueryBuilder";
            ViewData["OnQueryBuilder"] = "true";
            ViewData["TagAction"] = "/Search/Advanced/TagAll/";
            ViewData["UnTagAction"] = "/Search/Advanced/UnTagAll/";
            ViewData["AddContact"] = "/Search/Advanced/AddContact/";
            ViewData["AddTasks"] = "/Search/Advanced/AddTasks/";
            var m = new AdvancedModel { QueryId = id };
            DbUtil.LogActivity("QueryBuilder");
            if (run.HasValue)
                m.ShowResults = true;
            m.LoadScratchPad();
            ViewData["queryid"] = m.QueryId;
            ViewBag.AutoRun = (bool?)(TempData["AutoRun"]) == true;
            var newsearchid = (int?)TempData["newsearch"];
            if (newsearchid.HasValue)
            {
                ViewBag.NewSearch = true;
                m.SelectedId = newsearchid.Value;
            }
            return View(m);
        }
        [HttpPost]
        public ActionResult CodesDropdown(AdvancedModel m)
        {
            m.SetCodes();
            return View(m);
        }
        [HttpPost]
        public ActionResult SelectCondition(int id, string conditionName)
        {
            var m = new AdvancedModel { SelectedId = id };
            m.LoadScratchPad();
            m.ConditionName = conditionName;
            m.SetVisibility();
            m.TextValue = "";
            m.IntegerValue = "";
            m.DateValue = "";
            m.CodeValue = "";
            m.CodeValues = new string[0];
            m.Days = "";
            m.Age = "";
            m.Program = 0;
            m.Quarters = "";
            m.StartDate = "";
            m.EndDate = "";
            m.Comparison = "Equal";
            m.UpdateCondition();
            m.SelectedId = id;
            return View("Conditions", m);
        }
        [HttpPost]
        public ActionResult EditCondition(int id)
        {
            var m = new AdvancedModel { SelectedId = id };
            m.LoadScratchPad();
            m.EditCondition();
            return View("Conditions", m);
        }

        [HttpPost]
        public ActionResult AddNewCondition(int id)
        {
            var m = new AdvancedModel { SelectedId = id };
            m.LoadScratchPad();
            m.EditCondition();
            if (m.ConditionName == "Group")
                m.AddConditionToGroup();
            else
                m.AddNewConditionAfterCurrent(id);
            return View("Conditions", m);
        }
        [HttpPost]
        public ActionResult DuplicateCondition(int id)
        {
            var m = new AdvancedModel();
            m.LoadScratchPad();
            m.EditCondition();
            m.CopyCurrentCondition(id);
            return View("Conditions", m);
        }
        [HttpPost]
        public ActionResult SaveCondition(AdvancedModel m)
        {
            m.LoadScratchPad();
            if (m.Validate(ModelState))
            {
                m.UpdateCondition();
                DbUtil.Db.SubmitChanges();
            }
            return View("Conditions", m);
        }
        [HttpPost]
        public ActionResult Reload()
        {
            var m = new AdvancedModel();
            m.LoadScratchPad();
            return View("Conditions", m);
        }
        [HttpPost]
        public ActionResult RemoveCondition(int id)
        {
            var m = new AdvancedModel { SelectedId = id };
            m.LoadScratchPad();
            m.DeleteCondition();
            m.SelectedId = null;
            return View("Conditions", m);
        }
        [HttpPost]
        public ActionResult InsGroupAbove(int id)
        {
            var m = new AdvancedModel { SelectedId = id };
            m.LoadScratchPad();
            m.InsertGroupAbove();
            var c = new ContentResult();
            c.Content = m.QueryId.ToString();
            return c;
        }
        [HttpPost]
        public ActionResult CopyAsNew(int id)
        {
            var m = new AdvancedModel { SelectedId = id };
            m.LoadScratchPad();
            m.CopyAsNew();
            var c = new ContentResult();
            c.Content = m.QueryId.ToString();
            return c;
        }
        public ActionResult Conditions()
        {
            var m = new AdvancedModel();
            return View(m);
        }
        [HttpPost]
        public ActionResult Divisions(int id)
        {
            return View(id);
        }
        [HttpPost]
        public ActionResult Organizations(int id)
        {
            return View(id);
        }
        [HttpPost]
        public JsonResult SavedQueries()
        {
            var m = new AdvancedModel();
            return Json(m.SavedQueries()); ;
        }
        [HttpPost]
        public ActionResult SaveQuery()
        {
            var m = new AdvancedModel();
            UpdateModel(m);
            m.LoadScratchPad();
            m.SaveQuery();
            var c = new ContentResult();
            c.Content = m.Description;
            return c;
        }
		public void Results2Async()
		{
			AsyncManager.OutstandingOperations.Increment();
			string host = Util.Host;
			ThreadPool.QueueUserWorkItem((e) =>
			{
				var Db = new CMSDataContext(Util.GetConnectionString(host));
				Db.DeleteQueryBitTags();
				foreach (var a in Db.StatusFlags())
				{
					var t = Db.FetchOrCreateSystemTag(a[0]);
					Db.TagAll(Db.PeopleQuery(a[0] + ":" + a[1]), t);
					Db.SubmitChanges();
				}
				AsyncManager.OutstandingOperations.Decrement();
			});
		}
		public ActionResult Results2Completed()
		{
			return null;
		}

    	[HttpPost]
        public ActionResult Results()
        {
			var cb = new SqlConnectionStringBuilder(Util.ConnectionString);
        	cb.ApplicationName = "qb";
			DbUtil.Db = new CMSDataContext(cb.ConnectionString);
            var m = new AdvancedModel();
			try
			{
	            UpdateModel(m);
			}
			catch (Exception ex)
			{
				return Content("Something went wrong<br><p>" + ex.Message + "</p>");
			}
            m.LoadScratchPad();

			var starttime = DateTime.Now;
			m.PopulateResults();
			DbUtil.LogActivity("QB Results ({0:N1}, {1})".Fmt(DateTime.Now.Subtract(starttime).TotalSeconds, m.QueryId));
            return View(m);
        }
        public ActionResult NewQuery()
        {
            var qb = DbUtil.Db.QueryBuilderScratchPad();
            var ncid = qb.CleanSlate2(DbUtil.Db);
            TempData["newsearch"] = ncid;
            return RedirectToAction("Main");
        }
        [HttpPost]
        public JsonResult ToggleTag(int id)
        {
			try
			{
	            var r = Person.ToggleTag(id, Util2.CurrentTagName, Util2.CurrentTagOwnerId, DbUtil.TagTypeId_Personal);
	            DbUtil.Db.SubmitChanges();
	            return Json(new { HasTag = r });
			}
			catch (Exception ex)
			{
				return Json(new { error = ex.Message + ". Please report this to support@bvcms.com" });
			}
        }
        [HttpPost]
        public ContentResult TagAll(string tagname, bool? cleartagfirst)
        {
            var m = new AdvancedModel();
            m.LoadScratchPad();
            if (Util2.CurrentTagName == tagname && !(cleartagfirst ?? false))
            {
                m.TagAll();
                return Content("Remove");
            }
            var tag = DbUtil.Db.FetchOrCreateTag(tagname, Util.UserPeopleId, DbUtil.TagTypeId_Personal);
            if (cleartagfirst ?? false)
                DbUtil.Db.ClearTag(tag);
            m.TagAll(tag);
            Util2.CurrentTag = tagname;
            DbUtil.Db.TagCurrent();
            return Content("Manage");
        }
        [HttpPost]
        public ContentResult UnTagAll()
        {
            var m = new AdvancedModel();
            m.LoadScratchPad();
            m.UnTagAll();
            var c = new ContentResult();
            c.Content = "Add";
            return c;
        }
        [HttpPost]
        public ContentResult AddContact()
        {
            var m = new AdvancedModel();
            m.LoadScratchPad();
            var cid = CmsData.Contact.AddContact(m.QueryId.Value);
            return Content("/Contact.aspx?id=" + cid);
        }
        [HttpPost]
        public ActionResult AddTasks()
        {
            var m = new AdvancedModel();
            m.LoadScratchPad();
            var c = new ContentResult();
            c.Content = Task.AddTasks(m.QueryId.Value).ToString();
            return c;
        }

        public ActionResult Export()
        {
            var m = new AdvancedModel();
            m.LoadScratchPad();
            return new QBExportResult(m.QueryId.Value);
        }
        [HttpGet]
        public ActionResult Import()
        {
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Import(string text, string name)
		{
			int id = QueryFunctions.Import(DbUtil.Db, text, name);
			return Redirect("/Search/Advanced/Main/" + id);
		}
    }
}
