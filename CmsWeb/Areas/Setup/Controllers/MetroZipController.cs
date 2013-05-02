using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using CmsData;
using UtilityExtensions;

namespace CmsWeb.Areas.Setup.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MetroZipController : CmsStaffController
    {
        public ActionResult Index(string msg)
        {
            var m = DbUtil.Db.Zips.AsEnumerable();
            ViewData["msg"] = msg;
            return View(m);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Create(string zipcode)
        {
            var zip = DbUtil.Db.Zips.SingleOrDefault(mz => mz.ZipCode == zipcode);
            if (zip != null)
                return Content(Util.EndShowMessage("Zipcode already exists", "/Setup/MetroZip", "Go back"));

            var m = new Zip { ZipCode = zipcode };
            DbUtil.Db.Zips.InsertOnSubmit(m);
            DbUtil.Db.SubmitChanges();
            return Redirect("/Setup/MetroZip/");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult Edit(string id, string value)
        {
            id = id.Substring(1);
            var zip = DbUtil.Db.Zips.SingleOrDefault(m => m.ZipCode == id);
            zip.MetroMarginalCode = value.ToInt();
            DbUtil.Db.SubmitChanges();
            var c = new ContentResult();
            c.Content = zip.ResidentCode.Description;
            return c;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public EmptyResult Delete(string id)
        {
            id = id.Substring(1);
            var zip = DbUtil.Db.Zips.SingleOrDefault(m => m.ZipCode == id);
            if (zip == null)
                return new EmptyResult();
            DbUtil.Db.Zips.DeleteOnSubmit(zip);
            DbUtil.Db.SubmitChanges();
            return new EmptyResult();
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult UpdateMetroZips()
        {
            DbUtil.Db.UpdateResCodes();
            return Redirect("/Setup/MetroZip?msg=Updated%20all%20Codes");
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult ResidentCodes()
        {
            var q = from c in DbUtil.Db.ResidentCodes
                    select new
                    {
                        Code = c.Id.ToString(),
                        Value = c.Description,
                    };
            return Json(q.ToDictionary(k => k.Code, v => v.Value));
        }
    }
}
