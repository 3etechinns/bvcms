using System;
using System.IO;
using System.Web.Mvc;
using CmsData;
using UtilityExtensions;
using CmsData.API;

namespace CmsWeb.Areas.Public.Controllers
{
    public class APIContributionController : CmsController
    {
		[HttpPost]
		public ActionResult PostContribution(int PeopleId, decimal Amount, string desc, int FundId, string date, int? contributiontype, string checkno)
		{
			var ret = AuthenticateDeveloper(addrole: "Finance");
			if (ret.StartsWith("!"))
				return Content(@"<PostContribution status=""error"">" + ret.Substring(1) + "</PostContribution>");
			DbUtil.LogActivity("APIContribution PostContribution");
			return Content(new APIContribution(DbUtil.Db).PostContribution(PeopleId, Amount, FundId, desc, date, contributiontype, checkno), "text/xml");
		}
		[HttpGet]
		public ActionResult Contributions(int id, int Year)
		{
			var ret = AuthenticateDeveloper(addrole: "Finance");
			if (ret.StartsWith("!"))
				return Content(@"<Contributions status=""error"">" + ret.Substring(1) + "</Contributions>");
			DbUtil.LogActivity("APIContribution Contributions for ({0})".Fmt(id));
			return Content(new APIContribution(DbUtil.Db).Contributions(id, Year), "text/xml");
		}
		[HttpPost]
		public ActionResult ContributionSearch(ContributionSearchInfo m, int? page)
		{
			var ret = AuthenticateDeveloper(addrole: "Finance");
			if (ret.StartsWith("!"))
				return Content(@"<Contributions status=""error"">" + ret.Substring(1) + "</Contributions>");
			DbUtil.LogActivity("APIContribution ContributionSearch");
			return Content(new APIContributionSearchModel(DbUtil.Db, m).ContributionsXML(((page ?? 1) -1) * 100, 100), "text/xml");
		}
    }
}