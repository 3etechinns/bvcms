using System;
using System.Linq;
using System.Data.Linq;
using System.Web.Mvc;
using CmsData;
using UtilityExtensions;
using CmsWeb.Models;

namespace CmsWeb.Areas.Setup.Controllers
{
    [RouteArea("Setup", AreaPrefix = "Division"), Route("{action}/{id?}")]
    public class DivisionController : CmsStaffController
    {
        [Authorize(Roles = "Admin")]
        [Route("~/Divisions")]
        public ActionResult Index()
        {
            var m = new DivisionModel();
            return View(m);
        }
        [HttpPost]
        public ActionResult Results(DivisionModel m)
        {
            return View(m);
        }
        [HttpPost]
        public ActionResult Create(DivisionModel m)
        {
            var d = new Division { Name = "New Division" };
            d.ProgId = m.TagProgramId;
            d.ProgDivs.Add(new ProgDiv { ProgId = m.TagProgramId.Value });
            DbUtil.Db.Divisions.InsertOnSubmit(d);
            DbUtil.Db.SubmitChanges();
            DbUtil.Db.Refresh(RefreshMode.OverwriteCurrentValues, d);
            var di = m.DivisionItem(d.Id).Single();
            return View("Row", di); 
        }

 
        public ActionResult Edit(string id, string value)
        {
            if (!id.HasValue())
                return new EmptyResult();
            var iid = id.Substring(1).ToInt();
            var div = DbUtil.Db.Divisions.SingleOrDefault(p => p.Id == iid);
            if (div != null)
                switch (id.Substring(0, 1))
                {
                    case "n":
                        div.Name = value;
                        DbUtil.Db.SubmitChanges();
                        return Content(value);
                    case "p":
                        div.ProgId = value.ToInt();
                        DbUtil.Db.SubmitChanges();
                        return Content(div.Program.Name);
                    case "r":
                        div.ReportLine = value.ToInt2();
                        DbUtil.Db.SubmitChanges();
                        return Content(value);
                    case "z":
                        div.NoDisplayZero = value == "yes";
                        DbUtil.Db.SubmitChanges();
                        return Content(value);
                }
            return new EmptyResult();
        }
        [HttpPost]
        public EmptyResult Delete(string id)
        {
            var iid = id.Substring(1).ToInt();
            var div = DbUtil.Db.Divisions.SingleOrDefault(m => m.Id == iid);
            if (div == null)
                return new EmptyResult();
            DbUtil.Db.ProgDivs.DeleteAllOnSubmit(
                DbUtil.Db.ProgDivs.Where(di => di.DivId == iid));
            DbUtil.Db.DivOrgs.DeleteAllOnSubmit(
                DbUtil.Db.DivOrgs.Where(di => di.DivId == iid));
            foreach (var o in div.Organizations)
                o.DivisionId = null;
            DbUtil.Db.Divisions.DeleteOnSubmit(div);
            DbUtil.Db.SubmitChanges();
            return new EmptyResult();
        }
        [Serializable]
        public class ToggleTagReturn
        {
            public string value;
            public string ChangeMain;
        }
        [HttpPost]
        public ActionResult ToggleProg(int id, DivisionModel m)
        {
            var division = DbUtil.Db.Divisions.Single(d => d.Id == id);
            bool t = division.ToggleTag(DbUtil.Db, m.TagProgramId.Value);
            DbUtil.Db.SubmitChanges();
            DbUtil.Db.Refresh(RefreshMode.OverwriteCurrentValues, division);
            var di = m.DivisionItem(id).Single();
            return View("Row", di);
        }
        [HttpPost]
        public ActionResult MainProg(int id, DivisionModel m)
        {
            var division = DbUtil.Db.Divisions.Single(d => d.Id == id);
            division.ProgId = m.TagProgramId;
            DbUtil.Db.SubmitChanges();
            DbUtil.Db.Refresh(RefreshMode.OverwriteCurrentValues, division);
            var di = m.DivisionItem(id).Single();
            return View("Row", di);
        }
    }
}
