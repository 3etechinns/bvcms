﻿using System.Web.Mvc;
using CmsWeb.Areas.Dialog.Models;
using CmsData;
using UtilityExtensions;

namespace CmsWeb.Areas.Dialog.Controllers
{
    [RouteArea("Dialog", AreaPrefix="DeleteMeeting"), Route("{action}/{id?}")]
    public class DeleteMeetingController : CmsStaffController
    {
        [HttpPost, Route("~/DeleteMeeting/{id:int}")]
        public ActionResult Index(int id)
        {
            var model = new DeleteMeeting(id);
            model.RemoveExistingLop(DbUtil.Db, id, DeleteMeeting.Op);
            return View(model);
        }

        [HttpPost]
        public ActionResult Process(DeleteMeeting model)
        {
            model.UpdateLongRunningOp(DbUtil.Db, DeleteMeeting.Op);
            if (!model.Started.HasValue)
            { 
                DbUtil.LogActivity("Add to org from tag for {0}".Fmt(Session["ActiveOrganization"]));
                model.Process(DbUtil.Db);
            }
			return View(model);
		}
    }
}
