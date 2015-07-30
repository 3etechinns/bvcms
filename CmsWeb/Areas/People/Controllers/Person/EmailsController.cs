using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using CmsData;
using CmsWeb.Areas.People.Models;
using UtilityExtensions;

namespace CmsWeb.Areas.People.Controllers
{
    public partial class PersonController
    {
        [HttpPost]
        public ActionResult Optouts(int id)
        {
            var p = DbUtil.Db.LoadPersonById(id);
            return View("Emails/Optouts", p);
        }

        [HttpPost]
        public ActionResult DeleteOptout(int id, string email)
        {
            var p = DbUtil.Db.LoadPersonById(id);
            var oo = DbUtil.Db.EmailOptOuts.SingleOrDefault(o => o.FromEmail == email && o.ToPeopleId == id);
            if (oo == null)
            {
                ViewBag.Error = $"Email not found ({email})";
                return View("Emails/Optouts", p);
            }
            DbUtil.Db.EmailOptOuts.DeleteOnSubmit(oo);
            DbUtil.Db.SubmitChanges();
            return View("Emails/Optouts", p);
        }

        [HttpPost]
        public ActionResult AddOptout(int id, string emailaddress)
        {
            var oo = DbUtil.Db.EmailOptOuts.SingleOrDefault(o => o.FromEmail == emailaddress && o.ToPeopleId == id);
            if (oo == null)
            {
                DbUtil.Db.EmailOptOuts.InsertOnSubmit(new EmailOptOut {FromEmail = emailaddress, ToPeopleId = id, DateX = DateTime.Now});
                DbUtil.Db.SubmitChanges();
            }
            var p = DbUtil.Db.LoadPersonById(id);
            return View("Emails/Optouts", p);
        }

        [HttpPost]
        public ActionResult FailedEmails(FailedMailModel m)
        {
            return View("Emails/Failed", m);
        }

        [HttpPost, Route("EmailUnblock"), Authorize(Roles = "Admin")]
        public ActionResult Unblock(string email)
        {
            var deletebounce = ConfigurationManager.AppSettings["DeleteBounce"] + email;
            var wc = new WebClient();
            var ret = wc.DownloadString(deletebounce);
            return Content(ret);
        }

        [HttpPost, Route("EmailUnspam"), Authorize(Roles = "Developer")]
        public ActionResult Unspam(string email)
        {
            var deletespam = ConfigurationManager.AppSettings["DeleteSpamReport"] + email;
            var wc = new WebClient();
            var ret = wc.DownloadString(deletespam);
            DbUtil.Db.SpamReporterRemove(email);
            return Content(ret);
        }

        [HttpPost]
        public ActionResult ReceivedEmails(EmailReceivedModel m)
        {
            return View("Emails/Emails", m);
        }

        [HttpPost]
        public ActionResult SentEmails(EmailSentModel m)
        {
            return View("Emails/Emails", m);
        }

        [HttpPost]
        public ActionResult ScheduledEmails(EmailScheduledModel m)
        {
            return View("Emails/Emails", m);
        }

        [HttpPost]
        public ActionResult TransactionalEmails(EmailTransactionalModel m)
        {
            return View("Emails/Emails", m);
        }

        [HttpGet, Route("ViewEmail/{emailid:int}")]
        public ActionResult ViewEmail(int emailid)
        {
            var email = DbUtil.Db.EmailQueues.SingleOrDefault(ee => ee.Id == emailid);
            if (email == null)
                return Content("no email found");
            var curruser = DbUtil.Db.LoadPersonById(Util.UserPeopleId ?? 0);
            if (curruser == null)
                return Content("no user");
            if (User.IsInRole("Admin")
                || User.IsInRole("ManageEmails")
                || email.FromAddr == curruser.EmailAddress
                || email.QueuedBy == curruser.PeopleId
                || email.EmailQueueTos.Any(et => et.PeopleId == curruser.PeopleId))
            {
                var em = new EmailQueue
                {
                    Subject = email.Subject,
                    Body = email.Body.Replace("{track}", "", true).Replace("{first}", "", true)
                };
                return View("Emails/View", em);
            }
            return Content("not authorized");
        }
    }
}
