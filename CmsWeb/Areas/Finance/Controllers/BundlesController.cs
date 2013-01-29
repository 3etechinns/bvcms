﻿using System;
using System.Collections;
using System.Web;
using System.Web.Mvc;
using CmsData;
using CmsData.Codes;
using CmsWeb.Areas.Finance.Models;
using UtilityExtensions;

namespace CmsWeb.Areas.Finance.Controllers
{
    [Authorize(Roles = "Finance")]
    public class BundlesController : CmsStaffController
    {
        public ActionResult Index()
        {
            var m = new BundlesModel();
            return View(m);
        }
        [HttpPost]
        public ActionResult Results(BundlesModel m)
        {
            return View(m);
        }
        public ActionResult NewBundle()
        {
            var dt = Util.Now.Date;
            var dw = (int)dt.DayOfWeek;
            dt = dt.AddDays(-dw);
            var b = new BundleHeader
            {
                BundleHeaderTypeId = BundleTypeCode.PreprintedEnvelope,
                BundleStatusId = BundleStatusCode.Open,
                ChurchId = 1,
                ContributionDate = dt,
                CreatedBy = Util.UserId1,
                CreatedDate = Util.Now,
                RecordStatus = false,
                FundId = DbUtil.Db.Setting("DefaultFundId", "1").ToInt(), 
            };
            DbUtil.Db.BundleHeaders.InsertOnSubmit(b);
            DbUtil.Db.SubmitChanges();
            TempData["createbundle"] = true;
            return Redirect("/Bundle/Index/" + b.BundleHeaderId);
        }
    }
}
