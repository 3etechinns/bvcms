using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.Mvc;
using CmsWeb.Models;
using CmsData;
using UtilityExtensions;
using System.Text.RegularExpressions;

namespace CmsWeb.Areas.Main.Controllers
{
	public class ContactSearchController : CmsStaffController
	{
		[Serializable]
		class ContactSearchInfo
		{
			public string ContacteeName { get; set; }
			public string ContactorName { get; set; }
			public DateTime? StartDate { get; set; }
			public DateTime? EndDate { get; set; }
			public int? ContactTypeId { get; set; }
			public int? ContactReasonId { get; set; }
			public int? StatusId { get; set; }
			public int? MinistryId { get; set; }
		}
		private const string STR_ContactSearch = "ContactSearch";

		public ActionResult Index()
		{
			Response.NoCache();
			var m = new ContactSearchModel();

			var os = Session[STR_ContactSearch] as ContactSearchInfo;
			if (os != null)
			{
				m.ContactReason = os.ContactReasonId;
				m.ContacteeName = os.ContacteeName;
				m.ContactorName = os.ContactorName;
				m.ContactType = os.ContactTypeId;
				m.Ministry = os.MinistryId;
				m.StartDate = os.StartDate;
				m.EndDate = os.EndDate;
				m.Status = os.StatusId;
			}
			return View(m);
		}
		[HttpPost]
		public ActionResult Results(ContactSearchModel m)
		{
			SaveToSession(m);
			return View(m);
		}
		private void SaveToSession(ContactSearchModel m)
		{
			Session[STR_ContactSearch] = new ContactSearchInfo
			{
				EndDate = m.EndDate,
				StartDate = m.StartDate,
				MinistryId = m.Ministry,
				ContactTypeId = m.ContactType,
				ContactorName = m.ContactorName,
				ContacteeName = m.ContacteeName,
				ContactReasonId = m.ContactReason,
				StatusId = m.Status
			};
		}
		[HttpPost]
		public ContentResult Edit(string id, string value)
		{
			var a = id.Split('-');
			var c = new ContentResult();
			c.Content = value;
			var org = DbUtil.Db.LoadOrganizationById(a[1].ToInt());
			if (org == null)
				return c;
			switch (a[0])
			{
				case "bs":
					org.BirthDayStart = value.ToDate();
					break;
				case "be":
					org.BirthDayEnd = value.ToDate();
					break;
				case "ck":
					org.CanSelfCheckin = value == "yes";
					break;
			}
			DbUtil.Db.SubmitChanges();
			return c;
		}
        [HttpPost]
		public ActionResult ConvertToQuery(ContactSearchModel m)
		{
			var qb = DbUtil.Db.QueryBuilderScratchPad();
			qb.CleanSlate(DbUtil.Db);
			var comp = CompareType.Equal;
			var clause = qb.AddNewClause(QueryType.MadeContactTypeAsOf, comp, "1,T");
			clause.Program = m.Ministry ?? 0;
			clause.StartDate = m.StartDate ?? DateTime.Parse("1/1/2000");
			clause.EndDate = m.EndDate ?? DateTime.Today;
			var cvc = new CodeValueModel();
			var q = from v in cvc.ContactTypeCodes0()
					where v.Id == m.ContactType
					select v.IdCode;
			var idvalue = q.Single();
			clause.CodeIdValue = idvalue;
			DbUtil.Db.SubmitChanges();
			return Redirect("/QueryBuilder/Main/{0}".Fmt(qb.QueryId));
		}
		public ActionResult ContactorSummary(string start, string end, int ministry)
		{
		    var sdt = start.ToDate();
		    var edt = end.ToDate();

			var q = from c in DbUtil.Db.Contactors
					where c.contact.ContactDate >= sdt
					where c.contact.ContactDate <= edt
					where ministry == 0 || ministry == c.contact.MinistryId
					group c by new
					{
						c.PeopleId,
						c.person.Name,
						c.contact.ContactType.Description,
						c.contact.MinistryId,
						c.contact.Ministry.MinistryName
					} into g
					where g.Key.MinistryId != null
					orderby g.Key.MinistryId
					select new
					{
						g.Key.PeopleId,
						g.Key.Name,
						g.Key.Description,
						g.Key.MinistryName,
						cnt = g.Count()
					};
			return new DataGridResult(q);
		}

		public ActionResult ContactTypeTotals(string start, string end, int? ministry)
		{
		    var sdt = start.ToDate();
		    var edt = end.ToDate();

		    var q = from c in DbUtil.Db.ContactTypeTotals(sdt, edt, ministry ?? 0)
		            orderby c.Count descending
		            select c;
		    ViewBag.candelete = User.IsInRole("Developer") && sdt == null && edt == null && (ministry ?? 0) == 0;
			return View(q);
		}

	    public ActionResult ContactTypeSearchBuilder(int id)
	    {
			var qb = DbUtil.Db.QueryBuilderScratchPad();
			qb.CleanSlate(DbUtil.Db);
			var comp = CompareType.Equal;
			var clause = qb.AddNewClause(QueryType.RecentContactType, comp, "1,T");
	        clause.Days = 10000;
			var cvc = new CodeValueModel();
			var q = from v in cvc.ContactTypeCodes()
					where v.Id == id
					select v.IdCode;
	        clause.CodeIdValue = q.Single();
			DbUtil.Db.SubmitChanges();
			return Redirect("/QueryBuilder/Main/{0}".Fmt(qb.QueryId));
	    }
        [Authorize(Roles = "Developer")]
	    public ActionResult DeleteContactsForType(int id)
        {
            DbUtil.Db.ExecuteCommand("DELETE dbo.Contactees FROM dbo.Contactees ce JOIN dbo.Contact c ON ce.ContactId = c.ContactId WHERE c.ContactTypeId = {0}", id);
            DbUtil.Db.ExecuteCommand("DELETE dbo.Contactors FROM dbo.Contactors co JOIN dbo.Contact c ON co.ContactId = c.ContactId WHERE c.ContactTypeId = {0}", id);
            DbUtil.Db.ExecuteCommand("DELETE dbo.Contact WHERE ContactTypeId = {0}", id);
            return Redirect("/ContactSearch/ContactTypeTotals");
        }
	}
}
