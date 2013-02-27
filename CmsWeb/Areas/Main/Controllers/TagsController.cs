using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsWeb.Models;
using CmsData;
using UtilityExtensions;

namespace CmsWeb.Areas.Main.Controllers
{
    public class TagsController : CmsStaffController
    {
        [HttpGet]
        public ActionResult Index(string tag)
        {
            var m = new TagsModel();
            if (tag.HasValue())
                Util2.CurrentTag = tag;
            m.SetCurrentTag();
            InitExportToolbar();
            return View(m);
        }
        [HttpPost]
        public ActionResult Results(TagsModel m)
        {
            m.SetCurrentTag();
            InitExportToolbar();
            return View(m);
        }
        [HttpPost]
        public ActionResult SetShared(TagsModel m)
        {
            m.SetShareIds();
            return View("Results", m);
        }
        [HttpPost]
        public ActionResult Delete()
        {
            var t = DbUtil.Db.TagCurrent();
            if (t.TagShares.Count() > 0 || t.PeopleId != Util.UserPeopleId)
                return Content("error");

            t.DeleteTag(DbUtil.Db);
            DbUtil.Db.SubmitChanges();
            Util2.CurrentTag = "UnNamed";
            var m = new TagsModel();
            return View("Tags", m);
        }
        [HttpPost]
        public ActionResult RenameTag(TagsModel m)
        {
            if (!m.tagname.HasValue())
                return View("Tags", m);
            DbUtil.Db.TagCurrent().Name = m.tagname;
            DbUtil.Db.SubmitChanges();
            Util2.CurrentTag = m.tagname;
            return View("Tags", m);
        }
        [HttpPost]
        public ActionResult NewTag(TagsModel m)
        {
            Util2.CurrentTag = m.tagname;
            DbUtil.Db.TagCurrent();
            return View("Tags", m);
        }
        private void InitExportToolbar()
        {
            var qb = DbUtil.Db.QueryBuilderHasCurrentTag();
            ViewData["queryid"] = qb.QueryId;
            ViewData["TagAction"] = "/Tags/TagAll/{0}?m=tag".Fmt(qb.QueryId);
            ViewData["UnTagAction"] = "/Tags/TagAll/{0}?m=untag".Fmt(qb.QueryId);
            ViewData["AddContact"] = "/Tags/AddContact/" + qb.QueryId;
            ViewData["AddTasks"] = "/Tags/AddTasks/" + qb.QueryId;
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ToggleTag(int id)
        {
            var t = Person.ToggleTag(id, Util2.CurrentTagName, Util2.CurrentTagOwnerId, DbUtil.TagTypeId_Personal);
            DbUtil.Db.SubmitChanges();
            return Content(t ? "Remove" : "Add");
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TagAll(int id, string m, string tagname, bool forcenew)
        {
            DbUtil.Db.SetNoLock();
            var q = DbUtil.Db.PeopleQuery(id);
            switch (m)
            {
                case "tag":
			        if (Util2.CurrentTagName == tagname && !forcenew)
			        {
			            DbUtil.Db.TagAll(q);
    					return Content("Remove");
			        }
			        {
    			        var tag = DbUtil.Db.FetchOrCreateTag(tagname, Util.UserPeopleId, DbUtil.TagTypeId_Personal);
                        if (forcenew)
                            DbUtil.Db.ClearTag(tag);
			            DbUtil.Db.TagAll(q, tag);
			            Util2.CurrentTag = tagname;
                        DbUtil.Db.TagCurrent();
    					return Content("Manage");
			        }
                case "untag":
                    DbUtil.Db.UnTagAll(q);
                    return Content("Add");
            }
            return Content("?");
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult ClearTag()
        {
			var tag = DbUtil.Db.TagCurrent();
			DbUtil.Db.ExecuteCommand("delete dbo.TagPerson where Id = {0}", tag.Id);
			return Content("ok");
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult AddContact(int id)
        {
            var cid = CmsData.Contact.AddContact(id);
            return Content("/Contact.aspx?id=" + cid);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult AddTasks(int id)
        {
            var c = new ContentResult();
            c.Content = Task.AddTasks(id).ToString();
            return c;
        }
        public ActionResult SharedTags()
        {
            var t = DbUtil.Db.FetchOrCreateTag(Util.SessionId, Util.UserPeopleId, DbUtil.TagTypeId_AddSelected);
            DbUtil.Db.TagPeople.DeleteAllOnSubmit(t.PersonTags);
            DbUtil.Db.SubmitChanges();
            var tag = DbUtil.Db.TagCurrent();
            foreach (var ts in tag.TagShares)
                t.PersonTags.Add(new TagPerson { PeopleId = ts.PeopleId });
            DbUtil.Db.SubmitChanges();
            return Redirect("/SearchUsers");
        }
        [HttpPost]
        public ActionResult UpdateShared()
        {
            var t = DbUtil.Db.FetchOrCreateTag(Util.SessionId, Util.UserPeopleId, DbUtil.TagTypeId_AddSelected);
            var tag = DbUtil.Db.TagCurrent();
            var selected_pids = (from p in t.People(DbUtil.Db)
                                 where p.PeopleId != Util.UserPeopleId
                                 select p.PeopleId).ToArray();
            var userDeletes = tag.TagShares.Where(ts => !selected_pids.Contains(ts.PeopleId));
            DbUtil.Db.TagShares.DeleteAllOnSubmit(userDeletes);
            var tag_pids = tag.TagShares.Select(ts => ts.PeopleId).ToArray();
            var userAdds = from pid in selected_pids
                           join tpid in tag_pids on pid equals tpid into j
                           from p in j.DefaultIfEmpty(-1)
                           where p == -1
                           select pid;
            foreach (var pid in userAdds)
                tag.TagShares.Add(new TagShare { PeopleId = pid });
            DbUtil.Db.TagPeople.DeleteAllOnSubmit(t.PersonTags);
            DbUtil.Db.Tags.DeleteOnSubmit(t);
            DbUtil.Db.SubmitChanges();
            return Content(DbUtil.Db.TagShares.Count(tt => tt.TagId == tag.Id).ToString());
        }
        [Authorize(Roles = "Admin")]
        public ActionResult ConvertTagToExtraValue(string tag, string field, string value)
        {
            if (Request.HttpMethod.ToUpper() == "GET")
            {
                var success = (string)TempData["success"];
                if (success.HasValue())
                    ViewData["success"] = success;
                ViewData["tag"] = tag;
                ViewData["field"] = tag;
                ViewData["value"] = "true";
                return View();
            }
            var t = DbUtil.Db.Tags.FirstOrDefault(tt =>
                tt.Name == tag && tt.PeopleId == Util.UserPeopleId && tt.TypeId == DbUtil.TagTypeId_Personal);
            if (t == null)
            {
                TempData["message"] = "tag not found";
                return Redirect("/Tags/ConvertTagToExtraValue");
            }

            var q = t.People(DbUtil.Db);
            foreach (var p in q)
            {
                p.AddEditExtraValue(field, value);
                DbUtil.Db.SubmitChanges();
            }
            TempData["message"] = "success";
            return Redirect("/Tags/ConvertTagToExtraValue");
        }
    }
}
