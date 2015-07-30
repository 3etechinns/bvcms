﻿using System.Web.Mvc;
using CmsData;
using CmsWeb.Areas.Dialog.Models;

namespace CmsWeb.Areas.Dialog.Controllers
{
    [RouteArea("Dialog", AreaPrefix="AddToOrgFromTag"), Route("{action}/{id?}")]
    public class AddToOrgFromTagController : CmsStaffController
    {
        [HttpPost, Route("~/AddToOrgFromTag/{id:int}/{group}")]
        public ActionResult Index(int id, string group)
        {
            var model = new AddToOrgFromTag(id, group);
            model.RemoveExistingLop(DbUtil.Db, id, AddToOrgFromTag.Op);
            return View(model);
        }

        [HttpPost]
        public ActionResult Process(AddToOrgFromTag model)
        {
            model.Validate(ModelState);

            if(!ModelState.IsValid) // show validation errors
                return View("Index", model);

            model.UpdateLongRunningOp(DbUtil.Db, AddToOrgFromTag.Op);
            if(model.ShowCount(DbUtil.Db))
                return View("Index", model); // let them confirm by seeing the count and the tagname

            if (!model.Started.HasValue)
            {
                DbUtil.LogActivity($"Add to org from tag for {Session["ActiveOrganization"]}");
                model.Process(DbUtil.Db);
            }

            return View(model);
        }
    }
}
