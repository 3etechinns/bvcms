using System.Web.Mvc;
using CmsWeb.Areas.People.Models;
using UtilityExtensions;

namespace CmsWeb.Areas.People.Controllers
{
    [RouteArea("People", AreaPrefix = "Contact2"), Route("{action}/{cid:int}")]
    public class ContactController : CmsStaffController
    {
        [HttpGet, Route("~/Contact2/{cid}")]
        public ActionResult Index(int cid)
        {
            var m = new ContactModel(cid);
            if (m.contact == null)
                return Content("contact is private or does not exist");

            var edit = (bool?)TempData["ContactEdit"] == true;
            ViewBag.edit = edit;
            return View(m);
        }

        [HttpPost, Route("RemoveContactee/{cid:int}/{pid:int}")]
        public ActionResult RemoveContactee(int cid, int pid)
        {
            var m = new ContacteesModel(cid);
            m.RemoveContactee(pid);
            return Content("ok");

        }
        [HttpPost, Route("RemoveContactor/{cid:int}/{pid:int}")]
        public ActionResult RemoveContactor(int cid, int pid)
        {
            var m = new ContactorsModel(cid);
            if (m.Contact != null)
                m.RemoveContactor(pid);
            return Content("ok");
        }

        [HttpPost]
        public ActionResult Contactees(int cid)
        {
            var m = new ContactModel(cid);
            return View(m.MinisteredTo);
        }
        [HttpPost]
        public ActionResult Contactors(int cid)
        {
            var m = new ContactModel(cid);
            return View(m.Ministers);
        }
        [HttpPost]
        public ActionResult ContactEdit(int cid)
        {
            var m = new ContactModel(cid);
            if (!m.CanViewComments)
                return View("ContactDisplay", m);
            return View(m);
        }
        [HttpPost]
        public ActionResult ContactDisplay(int cid)
        {
            var m = new ContactModel(cid);
            return View(m);
        }
        [HttpGet]
        public ActionResult ConvertContacteesToQuery(int cid)
        {
            Response.NoCache();
            var m = new ContacteesModel(cid);
            var gid = m.ConvertToQuery();
            return Redirect($"/Query/{gid}");
        }
        [HttpGet]
        public ActionResult ConvertContactorsToQuery(int cid)
        {
            Response.NoCache();
            var m = new ContactorsModel(cid);
            var gid = m.ConvertToQuery();
            return Redirect($"/Query/{gid}");
        }
        [HttpPost]
        public ActionResult ContactUpdate(int cid, ContactModel c)
        {
            if (!User.IsInRole("Admin") && !ModelState.IsValid)
                return View("ContactEdit", c);
            c.UpdateContact();
            return View("ContactDisplay", c);
        }
        [HttpPost]
        public ActionResult ContactDelete(int cid)
        {
            ContactModel.DeleteContact(cid);
            return Redirect("/ContactSearch2");
        }
        [HttpPost]
        public ActionResult NewTeamContact(int cid)
        {
            var m = new ContactModel(cid);
            var nid = m.AddNewTeamContact();
            return Redirect("/Contact2/" + nid);
        }
        [HttpPost, Route("AddTask/{cid:int}/{pid:int}")]
        public ActionResult AddTask(int cid, int pid)
        {
            var m = new ContacteesModel(cid);
            var tid = m.AddTask(pid);
            return Redirect("/Task/List/" + tid);
        }
    }
}
