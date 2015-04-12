using System;
using System.Linq;
using System.Web.Mvc;
using CmsWeb.Areas.Dialog.Models;
using CmsData;
using CmsWeb.Code;
using UtilityExtensions;

namespace CmsWeb.Areas.Dialog.Controllers
{
    [RouteArea("Dialog", AreaPrefix = "OrgMemberDialog"), Route("{action}")]
    public class OrgMemberDialogController : CmsStaffController
    {
        [HttpPost, Route("~/OrgMemberDialog/{group}/{oid}/{pid}")]
        public ActionResult Display(string group, int oid, int pid)
        {
            var m = new OrgMemberModel(group, oid, pid);
            return View("Display", m);
        }
        [HttpPost]
        public ActionResult Display(OrgMemberModel m)
        {
            return View("Display", m);
        }
        [Authorize(Roles = "Admin,ManageGroups")]
        [HttpPost, Route("SmallGroupChecked/{sgtagid:int}")]
        public ActionResult SmallGroupChecked(int sgtagid, bool ck, OrgMemberModel m)
        {
            return Content(m.SmallGroupChanged(sgtagid, ck));
        }

        [HttpPost]
        public ActionResult Edit(OrgMemberModel m)
        {
            return View(m);
        }
        [HttpPost]
        public ActionResult Update(OrgMemberModel m)
        {
            try
            {
                m.UpdateModel();
            }
            catch (Exception)
            {
                ViewData["MemberTypes"] = CodeValueModel.ConvertToSelect(CodeValueModel.MemberTypeCodes(), "Id");
                return View("Edit", m);
            }
            return View("Display", m);
        }
        [HttpPost]
        public ActionResult Move(OrgMemberMoveModel m)
        {
            return View(m);
        }
        [HttpPost, Route("MoveResults")]
        public ActionResult MoveResults(OrgMemberMoveModel m)
        {
            return View("Move", m);
        }
        [HttpPost, Route("MoveSelect/{toid:int}")]
        public ActionResult MoveSelect(int toid, OrgMemberMoveModel m)
        {
            var ret = m.Move(toid);
            return Content(ret);
        }
        public string HelpLink()
        {
            return "";
        }
        [HttpPost]
        public ActionResult MissionSupport(MissionSupportModel m)
        {
            return View(m);
        }
        [HttpPost]
        public ActionResult AddMissionSupport(MissionSupportModel m)
        {
            m.PostContribution();
            return View("MissionSupportDone", m);
        }
        [HttpPost]
        public ActionResult AddTransaction(OrgMemberTransactionModel m)
        {
            return View(m);
        }
        [HttpPost]
        public ActionResult AddFeeAdjustment(OrgMemberTransactionModel m)
        {
            m.AdjustFee = true;
            return View(m);
        }
        [HttpPost]
        public ActionResult PostTransaction(OrgMemberTransactionModel m)
        {
            m.PostTransaction(ModelState);
            if (!ModelState.IsValid)
                return View("AddTransaction", m);
            return View("AddTransactionDone", m);
        }
        [HttpPost, Route("ShowDrop")]
        public ActionResult ShowDrop(OrgMemberModel m)
        {
            return View(m);
        }
        [HttpPost]
        public ActionResult Drop(OrgMemberModel m)
        {
            DbUtil.LogActivity(m.RemoveFromEnrollmentHistory
                ? "removed enrollment history on {0} for {1}".Fmt(m.PeopleId, m.OrgId)
                : "dropped {0} for {1}".Fmt(m.PeopleId, m.OrgId));
            m.Drop();
            return View("Dropped", m);
        }
    }
}
