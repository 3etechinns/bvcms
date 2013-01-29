using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsData;
using UtilityExtensions;
using CmsWeb.Models;

namespace CmsWeb.Areas.Main.Controllers
{
    public class OrgChildrenController : CmsStaffController
    {
        public ActionResult Index(int id)
        {
            var m = new OrgChildrenModel { orgid = id };
            return View(m);
        }
        [HttpPost]
        public ActionResult Filter(OrgChildrenModel m)
        {
            return View("Rows", m);
        }
        [HttpPost]
        public ActionResult UpdateOrg(int ParentOrg, int ChildOrg, bool Checked)
        {
            var o = DbUtil.Db.LoadOrganizationById(ChildOrg);
            if (Checked)
                o.ParentOrgId = ParentOrg;
            else
                o.ParentOrgId = null;
            DbUtil.Db.SubmitChanges();
            return Content("ok");
        }
    }
}
