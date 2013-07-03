﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsData.API;
using ContributionSearchModel = CmsWeb.Models.ContributionSearchModel;

namespace CmsWeb.Areas.Finance.Controllers
{
    [Authorize(Roles = "Finance")]
    public class ContributionsController : CmsStaffController
    {
        public ActionResult Index(int? id, int? year)
        {
        	var m = new ContributionSearchModel(id, year);
            return View(m);
        }
        [HttpPost]
		public ActionResult Results(ContributionSearchModel m)
        {
			return View(m);
		}
    }
}
