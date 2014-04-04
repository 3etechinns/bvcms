using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CmsData;
using CmsData.Codes;
using CmsWeb.Areas.People.Models;
using Newtonsoft.Json;
using UtilityExtensions;
using System.Web.Routing;

namespace CmsWeb.Areas.People.Controllers
{
    [ValidateInput(false)]
    [SessionExpire]
    public partial class PersonController : CmsStaffController
    {
        protected override void Initialize(RequestContext requestContext)
        {
            NoCheckRole = true;
            base.Initialize(requestContext);
        }
        [HttpGet, Route("Person2/Current")]
        public ActionResult Current()
        {
            return Redirect("/Person2/" + Util2.CurrentPeopleId);
        }

        [HttpGet, Route("Person2/User/{id:int}")]
        public ActionResult UserPerson(int? id)
        {
            var pid = (from p in DbUtil.Db.People
                where p.Users.Any(uu => uu.UserId == id)
                select p.PeopleId).SingleOrDefault();
            if (pid == 0)
                return Content("no person");
            return Redirect("/Person2/" + pid);
        }
        [HttpGet]
        [Route("Person2/{id:int}")]
        [Route("{id:int}")]
        public ActionResult Index(int? id)
        {
            if (!ViewExtensions2.UseNewLook() && User.IsInRole("Access"))
                return Redirect("/Person/Index/" + id);
            if (!id.HasValue)
                return Content("no id");

            var m = new PersonModel(id.Value);
            var noview = m.CheckView();
            if (noview.HasValue())
                return Content(noview);

            ViewBag.Comments = Util.SafeFormat(m.Person.Comments);
            ViewBag.PeopleId = id.Value;
            Util2.CurrentPeopleId = id.Value;
            Session["ActivePerson"] = m.Person.Name;
            DbUtil.LogActivity("Viewing Person: {0}".Fmt(m.Person.Name), m.Person.Name, pid: id);
            InitExportToolbar(id);
            return View(m);
        }

        private void InitExportToolbar(int? id)
        {
            var qb = DbUtil.Db.QueryIsCurrentPerson();
            ViewBag.queryid = qb.QueryId;
            ViewBag.PeopleId = Util2.CurrentPeopleId;
            ViewBag.TagAction = "/Person2/Tag/" + id;
            ViewBag.UnTagAction = "/Person2/UnTag/" + id;
            ViewBag.AddContact = "/Person2/AddContactReceived/" + id;
            ViewBag.AddTasks = "/Person2/AddTaskAbout/" + id;
        }

        [HttpPost, Route("Person2/Tag/{id:int}")]
        public ActionResult Tag(int id, string tagname, bool? cleartagfirst)
        {
            if (Util2.CurrentTagName == tagname && !(cleartagfirst ?? false))
            {
                Person.Tag(DbUtil.Db, id, Util2.CurrentTagName, Util2.CurrentTagOwnerId, DbUtil.TagTypeId_Personal);
                DbUtil.Db.SubmitChanges();
                return Content("OK");
            }
            var tag = DbUtil.Db.FetchOrCreateTag(tagname, Util.UserPeopleId, DbUtil.TagTypeId_Personal);
            if (cleartagfirst ?? false)
                DbUtil.Db.ClearTag(tag);
            Person.Tag(DbUtil.Db, id, Util2.CurrentTagName, Util2.CurrentTagOwnerId, DbUtil.TagTypeId_Personal);
            DbUtil.Db.SubmitChanges();
            Util2.CurrentTag = tagname;
            DbUtil.Db.TagCurrent();
            return Content("OK");
        }
        [HttpPost]
        public ActionResult UnTag(int id)
        {
            Person.UnTag(id, Util2.CurrentTagName, Util2.CurrentTagOwnerId, DbUtil.TagTypeId_Personal);
            DbUtil.Db.SubmitChanges();
            return new EmptyResult();
        }

        [HttpPost, Route("Person2/InlineEdit/{id:int}")]
        public ActionResult InlineEdit(int id, int pk, string name, string value)
        {
            var m = new PersonModel(id);
            switch (name)
            {
                case "ContributionOptions":
                case "EnvelopeOptions":
                    m.UpdateEnvelopeOption(name, value.ToInt());
                    break;
            }
            return new EmptyResult();
        }

        [HttpGet, Route("Person2/InlineCodes/{name}")]
        public ActionResult InlineCodes(string name)
        {
            var q = from v in new List<string>()
                    select new { value = "", text = "" };
            switch (name)
            {
                case "ContributionOptions":
                case "EnvelopeOptions":
                    q = from c in DbUtil.Db.EnvelopeOptions
                        select new { value = c.Id.ToString(), text = c.Description };
                    break;
            }
            var j = JsonConvert.SerializeObject(q.ToArray());
            return Content(j);
        }
    }
}
