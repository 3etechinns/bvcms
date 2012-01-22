using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsWeb.Models;
using UtilityExtensions;
using CmsData;

namespace CmsWeb.Areas.Dialog.Controllers
{
    public class SearchOrgsController : Controller
    {
        [HttpGet]
        public ActionResult Index(int id, bool? singlemode)
        {
            Response.NoCache();
            var list = Session["orgPickList"] as List<int>;
            var m = new SearchOrgsModel
            {
                id = id,
                singlemode = singlemode ?? false,
                cklist = list,
            };
            return View(m);
        }
        [HttpPost]
        public ActionResult Results(SearchOrgsModel m)
        {
            m.cklist = Session["orgPickList"] as List<int>;
            return View(m);
        }
        [HttpPost]
        public ActionResult SaveOrgIds(int id, string oids)
        {
            Session["orgPickList"] = oids.Split(',').Select(oo => oo.ToInt()).ToList();
            return new EmptyResult();
        }
    }
}
