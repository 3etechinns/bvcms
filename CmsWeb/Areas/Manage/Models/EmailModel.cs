using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Data.Linq;
using System.Web;
using CmsData;
using UtilityExtensions;
using System.Web.Mvc;
using System.Text;
using System.Net.Mail;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace CmsWeb.Models
{
    public class EmailModel
    {
        public int id { get; set; }
        public string filter { get; set; }
        private EmailQueue _Queue;
        public EmailQueue queue
        {
            get
            {
                if (_Queue == null)
                    _Queue = DbUtil.Db.EmailQueues.SingleOrDefault(ee => ee.Id == id);
                return _Queue;
            }
        }
        public PagerModel2 Pager { get; set; }
        int? _count;
        public int Count()
        {
            if (!_count.HasValue)
                _count = GetEmailTos().Count();
            return _count.Value;
        }
        public EmailModel()
        {
            Pager = new PagerModel2(Count);
            filter = "All";
        }
		public bool CanDelete()
		{
		    if (HttpContext.Current.User.IsInRole("Admin"))
		        return true;
		    if (queue.QueuedBy == Util.UserPeopleId)
		        return true;
		    var u = DbUtil.Db.LoadPersonById(Util.UserPeopleId.Value);
		    if (queue.FromAddr == u.EmailAddress)
		        return true;
		    return false;
		}
        public IEnumerable<RecipientInfo> Recipients()
        {
            var q = GetEmailTos();
            var q2 = from e in q.OrderBy(ee => ee.Person.Name2)
                         .Skip(Pager.StartRow).Take(Pager.PageSize)
					 //let fail = e.EmailQueueToFails.FirstOrDefault()
                     select new RecipientInfo
                     {
                         peopleid = e.PeopleId,
                         name = e.Person.Name,
                         address = e.Person.EmailAddress,
                         nopens = e.Person.EmailResponses.Count(er => er.EmailQueueId == e.Id),
						 //failtype = fail.EventX + " " + fail.Bouncetype,
                     };
            return q2;
        }
        public IQueryable<EmailQueueTo> GetEmailTos()
        {
            var q = from t in DbUtil.Db.EmailQueueTos
                    let opened = t.Person.EmailResponses.Any(er => er.EmailQueueId == t.Id)
					//let fail = t.EmailQueueToFails.FirstOrDefault()
                    where t.Id == id
                    where filter == "All" 
					|| (opened == true && filter == "Opened") 
					|| (opened == false && filter == "Not Opened")
					//|| (fail != null && filter == "Failed")
                    select t;
            if ((!DbUtil.Db.CurrentUser.Roles.Contains("Admin") 
                        || DbUtil.Db.CurrentUser.Roles.Contains("ManageEmails"))
                    && queue.QueuedBy != Util.UserPeopleId)
                q = q.Where(ee => ee.PeopleId == Util.UserPeopleId);
            return q;
        }
    }
    public class RecipientInfo
    {
        public string name { get; set; }
        public int peopleid { get; set; }
        public string address { get; set; }
        public int nopens { get; set; }
		public string failtype { get; set; }
    }
}
