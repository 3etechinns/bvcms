﻿using System;
using System.Collections;
using System.Data.Linq;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsData;
using CmsData.Codes;
using CmsWeb.Areas.Finance.Models;
using UtilityExtensions;


namespace CmsWeb.Areas.Finance.Controllers
{
    [Authorize(Roles = "Finance")]
    public class BundleController : CmsStaffController
    {
        public ActionResult Index(int id, bool? create)
        {
            var m = new BundleModel(id);
            if (m.Bundle == null)
                return Content("no bundle");
            return View(m);
        }
        [HttpPost]
        public ActionResult Results(BundleModel m)
        {
            return View(m);
        }
        [HttpPost]
        public ActionResult Edit(int id)
        {
            var m = new BundleModel(id);
            return View(m);
        }
        [HttpPost]
        public ActionResult Update(int id)
        {
            var m = new BundleModel(id);
            UpdateModel<BundleModel>(m);
            UpdateModel<BundleHeader>(m.Bundle, "Bundle");
            if (m.Bundle.ContributionDateChanged)
            {
                var q = from d in DbUtil.Db.BundleDetails
                        where d.BundleHeaderId == m.Bundle.BundleHeaderId
                        select d.Contribution;
                foreach (var c in q)
                    c.ContributionDate = m.Bundle.ContributionDate;
            }
            var postingdt = Util.Now;
            if (m.Bundle.BundleStatusIdChanged && m.Bundle.BundleStatusId == BundleStatusCode.Closed)
            {
                foreach (var d in m.Bundle.BundleDetails)
                    d.Contribution.PostingDate = postingdt;
            }
            DbUtil.Db.SubmitChanges();
            m.BundleId = id; // refresh values
            return View("Display", m);
        }
        [HttpPost]
        public ActionResult Cancel(int id)
        {
            var m = new BundleModel(id);
            return View("Display", m);
        }
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var m = new BundleModel(id);
            var q = from d in m.Bundle.BundleDetails
                    select d.Contribution;
            DbUtil.Db.Contributions.DeleteAllOnSubmit(q);
            DbUtil.Db.BundleDetails.DeleteAllOnSubmit(m.Bundle.BundleDetails);
            DbUtil.Db.BundleHeaders.DeleteOnSubmit(m.Bundle);
            DbUtil.Db.SubmitChanges();
            return Content("/Bundles");
        }
    }
}
