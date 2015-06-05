/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CmsWeb.Areas.Finance.Controllers;
using UtilityExtensions;
using CmsData;
using CmsData.Codes;

namespace CmsWeb.Models
{
    public class PostBundleModel
    {
        public class FundTotal
        {
            public int FundId { get; set; }
            public string Name { get; set; }
            public decimal? Total { get; set; }
        }
        public int id { get; set; }
        public int? editid { get; set; }
        public string pid { get; set; }
        public decimal? amt { get; set; }
        public int? splitfrom { get; set; }
        public int fund { get; set; }
        public string PLNT { get; set; }
        public string notes { get; set; }
        public string checkno { get; set; }
        public DateTime? contributiondate { get; set; }
        private BundleHeader _bundle;
        public string FundName { get; set; }
        public bool DefaultFundIsPledge { get; set; }
        public BundleHeader bundle
        {
            get
            {
                if (_bundle == null)
                {
                    _bundle = DbUtil.Db.BundleHeaders.SingleOrDefault(bh => bh.BundleHeaderId == id);
                    if (_bundle != null && _bundle.FundId.HasValue)
                    {
                        FundName = _bundle.Fund.FundName;
                        DefaultFundIsPledge = _bundle.Fund.FundPledgeFlag;
                    }
                }
                return _bundle;
            }
        }
        public PostBundleModel()
        {
        }
        public PostBundleModel(int id)
        {
            this.id = id;
            PLNT = bundle.BundleHeaderTypeId == BundleTypeCode.Pledge ? "PL" :
                bundle.BundleHeaderTypeId == BundleTypeCode.GiftsInKind ? "GK" : 
                bundle.BundleHeaderTypeId == BundleTypeCode.Stock ? "SK" : "CN";
        }

        public IEnumerable<ContributionInfo> FetchContributions(int? cid = null)
        {
            var q = from d in DbUtil.Db.BundleDetails
                    where d.BundleHeaderId == id || cid != null
                    where cid == null || d.ContributionId == cid
                    let sort = d.BundleSort1 > 0 ? d.BundleSort1 : d.BundleDetailId
                    orderby sort descending, d.ContributionId ascending
                    select new ContributionInfo
                    {
                        ContributionId = d.ContributionId,
                        BundleTypeId = d.BundleHeader.BundleHeaderTypeId,
                        PeopleId = d.Contribution.PeopleId,
                        Name = d.Contribution.Person.Name2
                             + (d.Contribution.Person.DeceasedDate.HasValue ? " [DECEASED]" : ""),
                        Amt = d.Contribution.ContributionAmount,
                        Fund = d.Contribution.ContributionFund.FundName,
                        FundId = d.Contribution.FundId,
                        Notes = d.Contribution.ContributionDesc,
                        CheckNo = d.Contribution.CheckNo,
                        eac = d.Contribution.BankAccount,
                        Address = d.Contribution.Person.PrimaryAddress,
                        City = d.Contribution.Person.PrimaryCity,
                        State = d.Contribution.Person.PrimaryState,
                        Zip = d.Contribution.Person.PrimaryZip,
                        Age = d.Contribution.Person.Age,
                        extra = d.Contribution.ExtraDatum.Data,
                        Date = d.Contribution.ContributionDate,
                        PLNT = ContributionTypeCode.SpecialTypes.Contains(d.Contribution.ContributionTypeId) ? d.Contribution.ContributionType.Code : "",
                        memstatus = d.Contribution.Person.MemberStatus.Description,
                    };
            var list = q.ToList();
            foreach (var c in list)
            {
                string s = null;
                if (!c.PeopleId.HasValue)
                {
                    s = c.extra ?? "";
                    if (c.eac.HasValue())
                        s += " not associated";
                    if (s.HasValue())
                        c.Name = s;
                }
            }
            return list;
        }
        public IEnumerable<FundTotal> TotalsByFund()
        {
            var q = from d in DbUtil.Db.BundleDetails
                    where d.BundleHeaderId == id
                    group d by new { d.Contribution.ContributionFund.FundName, d.Contribution.ContributionFund.FundId } into g
                    orderby g.Key.FundName
                    select new FundTotal
                    {
                        FundId = g.Key.FundId,
                        Name = g.Key.FundName,
                        Total = g.Sum(d => d.Contribution.ContributionAmount)
                    };
            return q;
        }
        public IEnumerable<SelectListItem> Funds()
        {
            var q = from f in DbUtil.Db.ContributionFunds
                    where f.FundStatusId == 1
                    orderby f.FundId
                    select new SelectListItem
                    {
                        Text = "{0} - {1}".Fmt(f.FundId, f.FundName),
                        Value = f.FundId.ToString()
                    };
            return q;
        }
        
        public object GetNamePidFromId()
        {
            IEnumerable<object> q;
            if (pid != null && pid.Length > 0 && (pid[0] == 'e' || pid[0] == '-'))
            {
                var env = pid.Substring(1).ToInt();
                q = from e in DbUtil.Db.PeopleExtras
                    where e.Field == "EnvelopeNumber"
                    where e.IntValue == env
                    orderby e.Person.Family.HeadOfHouseholdId == e.PeopleId ? 1 : 2
                    select new
                    {
                        e.PeopleId,
                        name = e.Person.Name2 + (e.Person.DeceasedDate.HasValue ? "[DECEASED]" : "")
                    };
            }
            else
            {
                q = from i in DbUtil.Db.People
                    where i.PeopleId == pid.ToInt()
                    select new
                    {
                        i.PeopleId,
                        name = i.Name2 + (i.DeceasedDate.HasValue ? "[DECEASED]" : "")
                    };
            }
            var o = q.FirstOrDefault();
            if (o == null)
                return new { error = "not found" };
            return o;
        }
        public static IEnumerable<NamesInfo> Names(string q, int limit)
        {
            var qp = FindNames(q);

            var rp = from p in qp
                     let age = p.Age.HasValue ? " (" + p.Age + ")" : ""
                     let spouse = DbUtil.Db.People.SingleOrDefault(ss =>
                         ss.PeopleId == p.SpouseId
                         && ss.ContributionOptionsId == StatementOptionCode.Joint
                         && p.ContributionOptionsId == StatementOptionCode.Joint)
                     orderby p.Name2
                     select new NamesInfo()
                                {
                                    Pid = p.PeopleId,
                                    Name = p.Name2 + age,
                                    spouse = spouse.Name,
                                    Addr = p.PrimaryAddress ?? "",
                                };
            return rp.Take(limit);
        }

        public class RecentContribution
        {
            public decimal? Amount;
            public DateTime? DateGiven;
            public string CheckNo;
        }

        public class NamesInfo
        {
            public string Name { get; set; }
            public string Addr { get; set; }
            public int Pid { get; set; }
            internal List<PostBundleModel.RecentContribution> recent { get; set; }
            internal string spouse { get; set; }

            public string Spouse
            {
                get
                {
                    if (spouse.HasValue())
                        return "<br>Giving with: " + spouse;
                    return "";
                }
            }
            public string RecentGifts
            {
                get
                {
                    if (recent == null) 
                        return "";
                    const string row =
                        "<tr><td class='right'>{0}</td><td class='center nowrap'>&nbsp;{1}</td><td>&nbsp;{2}</td></tr>";
                    var list = from rr in recent
                        select row.Fmt(rr.Amount.ToString2("N2"), rr.DateGiven.ToSortableDate(), rr.CheckNo);
                    var s = string.Join("\n", list);
                    return s.HasValue() ? "<table style='margin-left:2em'>{0}</table>".Fmt(s) : "";
                }
            }
        }

        public static IEnumerable<NamesInfo> Names2(string q, int limit)
        {
            var qp = FindNames(q);

            var rp = from p in qp
                     let age = p.Age.HasValue ? " (" + p.Age + ")" : ""
                     let spouse = DbUtil.Db.People.SingleOrDefault(ss => 
                         ss.PeopleId == p.SpouseId 
                         && ss.ContributionOptionsId == StatementOptionCode.Joint 
                         && p.ContributionOptionsId == StatementOptionCode.Joint)
                     orderby p.Name2
                     select new NamesInfo()
                                {
                                    Pid = p.PeopleId,
                                    Name = p.Name2 + age,
                                    spouse = spouse.Name,
                                    Addr = p.PrimaryAddress ?? "",
                                    recent = (from c in p.Contributions
                                              where c.ContributionStatusId == 0
                                              orderby c.ContributionDate descending
                                              select new RecentContribution()
                                              {
                                                  Amount = c.ContributionAmount,
                                                  DateGiven = c.ContributionDate,
                                                  CheckNo = c.CheckNo
                                              }).Take(4).ToList()
                                };
            return rp.Take(limit);
        }

        private static IQueryable<Person> FindNames(string q)
        {
            string First, Last;
            var qp = DbUtil.Db.People.AsQueryable();
            qp = from p in qp
                 where p.DeceasedDate == null
                 select p;

            Util.NameSplit(q, out First, out Last);
            var hasfirst = First.HasValue();

            if (q.AllDigits())
            {
                string phone = null;
                if (q.HasValue() && q.AllDigits() && q.Length == 7)
                    phone = q;
                if (phone.HasValue())
                {
                    var id = Last.ToInt();
                    qp = from p in qp
                         where
                             p.PeopleId == id
                             || p.CellPhone.Contains(phone)
                             || p.Family.HomePhone.Contains(phone)
                             || p.WorkPhone.Contains(phone)
                         select p;
                }
                else
                {
                    var id = Last.ToInt();
                    qp = from p in qp
                         where p.PeopleId == id
                         select p;
                }
            }
            else
            {
                qp = from p in qp
                     where
                         (
                             (p.LastName.StartsWith(Last) || p.MaidenName.StartsWith(Last)
                              || p.LastName.StartsWith(q) || p.MaidenName.StartsWith(q))
                             &&
                             (!hasfirst || p.FirstName.StartsWith(First) || p.NickName.StartsWith(First) ||
                              p.MiddleName.StartsWith(First)
                              || p.LastName.StartsWith(q) || p.MaidenName.StartsWith(q))
                             )
                     select p;
            }
            return qp;
        }

        public object ContributionRowData(PostBundleController ctl, int cid, decimal? othersplitamt = null)
        {
            var cinfo = FetchContributions(cid).Single();
            var body = ViewExtensions2.RenderPartialViewToString(ctl, "Row", cinfo);
            var q = from c in DbUtil.Db.Contributions
                    let bh = c.BundleDetails.First().BundleHeader
                    where c.ContributionId == cid
                    select new
                    {
                        row = body,
                        amt = c.ContributionAmount.ToString2("N2"),
                        cid,
                        totalitems = bh.BundleDetails.Sum(d =>
                            d.Contribution.ContributionAmount).ToString2("C2"),
                        diff = ((bh.TotalCash.GetValueOrDefault() + bh.TotalChecks.GetValueOrDefault() + bh.TotalEnvelopes.GetValueOrDefault()) - bh.BundleDetails.Sum(d => d.Contribution.ContributionAmount.GetValueOrDefault())),
                        difference = ((bh.TotalCash.GetValueOrDefault() + bh.TotalChecks.GetValueOrDefault() + bh.TotalEnvelopes.GetValueOrDefault()) - bh.BundleDetails.Sum(d => d.Contribution.ContributionAmount)).ToString2("C2"),
                        itemcount = bh.BundleDetails.Count(),
                        othersplitamt = othersplitamt.ToString2("N2")
                    };
            return q.First();
        }
        public object PostContribution(PostBundleController ctl)
        {
            try
            {
                var bd = new CmsData.BundleDetail
                {
                    BundleHeaderId = id,
                    CreatedBy = Util.UserId,
                    CreatedDate = DateTime.Now,
                };
                int type;
                switch (PLNT)
                {
                    case "PL":
                        type = ContributionTypeCode.Pledge;
                        break;
                    case "NT":
                        type = ContributionTypeCode.NonTaxDed;
                        break;
                    case "GK":
                        type = ContributionTypeCode.GiftInKind;
                        break;
                    case "SK":
                        type = ContributionTypeCode.Stock;
                        break;
                    default:
                        type = ContributionTypeCode.CheckCash;
                        break;
                }

                decimal? othersplitamt = null;
                if (splitfrom > 0)
                {
                    var q = from c in DbUtil.Db.Contributions
                            where c.ContributionId == splitfrom
                            select new
                                   {
                                       c,
                                       bd = c.BundleDetails.First(),
                                   };
                    var i = q.Single();
                    othersplitamt = i.c.ContributionAmount - amt;
                    i.c.ContributionAmount = othersplitamt;
                    DbUtil.Db.SubmitChanges();
                    bd.BundleSort1 = i.bd.BundleDetailId;
                }

                bd.Contribution = new Contribution
                {
                    CreatedBy = Util.UserId,
                    CreatedDate = bd.CreatedDate,
                    FundId = fund,
                    PeopleId = pid.ToInt2(),
                    ContributionDate = contributiondate ?? bundle.ContributionDate,
                    ContributionAmount = amt,
                    ContributionStatusId = 0,
                    ContributionTypeId = type,
                    ContributionDesc = notes,
                    CheckNo = (checkno ?? "").Trim().Truncate(20)
                };
                bundle.BundleDetails.Add(bd);
                DbUtil.Db.SubmitChanges();
                return ContributionRowData(ctl, bd.ContributionId, othersplitamt);
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }
        public object UpdateContribution(PostBundleController ctl)
        {
            var c = DbUtil.Db.Contributions.SingleOrDefault(cc => cc.ContributionId == editid);
            if (c == null)
                return null;

            int type = c.ContributionTypeId;
            switch (PLNT)
            {
                case "PL":
                    type = ContributionTypeCode.Pledge;
                    break;
                case "NT":
                    type = ContributionTypeCode.NonTaxDed;
                    break;
                case "GK":
                    type = ContributionTypeCode.GiftInKind;
                    break;
                case "SK":
                    type = ContributionTypeCode.Stock;
                    break;
                default:
                    type = ContributionTypeCode.CheckCash;
                    break;
            }
            c.FundId = fund;
            c.PeopleId = pid.ToInt2();
            c.ContributionAmount = amt;
            c.ContributionTypeId = type;
            c.ContributionDesc = notes;
            c.ContributionDate = contributiondate;
            c.CheckNo = checkno;
            DbUtil.Db.SubmitChanges();
            return ContributionRowData(ctl, c.ContributionId);
        }
        public object DeleteContribution()
        {
            var bd = bundle.BundleDetails.SingleOrDefault(d => d.ContributionId == editid);
            if (bd != null)
            {
                var c = bd.Contribution;
                DbUtil.Db.BundleDetails.DeleteOnSubmit(bd);
                bundle.BundleDetails.Remove(bd);
                DbUtil.Db.Contributions.DeleteOnSubmit(c);
                DbUtil.Db.SubmitChanges();
            }

            var totalItems = bundle.BundleDetails.Sum(d => d.Contribution.ContributionAmount);
            var diff = (bundle.TotalCash.GetValueOrDefault() + bundle.TotalChecks.GetValueOrDefault() + bundle.TotalEnvelopes.GetValueOrDefault()) - totalItems;
            return new
            {
                totalitems = totalItems.ToString2("C2"),
                diff = diff,
                difference = diff.ToString2("C2"),
                itemcount = bundle.BundleDetails.Count(),
            };
        }
        public static string Tip(int? pid, int? age, string memstatus, string address, string city, string state, string zip)
        {
            return "<label>People Id:</label> {0}<br/><label>Age:</label> {1}<br/>{2}<br/>{3}<br/>{4}".Fmt(pid, age, memstatus, address, Util.FormatCSZ(city, state, zip));
        }

        public decimal TotalItems
        {
            get { return bundle.BundleDetails.Sum(dd => dd.Contribution.ContributionAmount) ?? 0; }
        }
        public int TotalCount
        {
            get { return bundle.BundleDetails.Count(); }
        }
        public class ContributionInfo
        {
            public int ContributionId { get; set; }
            public string eac { get; set; }
            public string extra { get; set; }
            public int? PeopleId { get; set; }
            public int? Age { get; set; }
            public int BundleTypeId { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public DateTime? Date { get; set; }
            public string PLNT { get; set; }
            public string memstatus { get; set; }
            public string CityStateZip
            {
                get
                {
                    return Util.FormatCSZ(City, State, Zip);
                }
            }
            public string Name { get; set; }
            public decimal? Amt { get; set; }
            public string AmtDisplay
            {
                get
                {
                    return Amt.ToString2("N2");
                }
            }
            public string Fund { get; set; }
            public int FundId { get; set; }
            public string FundDisplay
            {
                get
                {
                    return "{0} - {1}".Fmt(FundId, Fund);
                }
            }
            public string Notes { get; set; }
            public string CheckNo { get; set; }
            public string tip
            {
                get
                {
                    return Tip(PeopleId, Age, memstatus, Address, City, State, Zip);
                }
            }
        }
    }
}

