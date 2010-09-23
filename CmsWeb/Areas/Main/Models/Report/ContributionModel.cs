﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CmsData;
using System.Text;
using System.Collections;
using UtilityExtensions;
using System.Text.RegularExpressions;

namespace CmsWeb.Areas.Main.Models.Report
{
    public class ContributionModel
    {
        public Person person { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public ContributionModel(int pid)
        {
            person = DbUtil.Db.LoadPersonById(pid);
            var intQtr = (Util.Now.Date.Month) / 3 + 1;

            if (intQtr == 1)
            {
                FromDate = new DateTime(Util.Now.Date.Year - 1, 1, 1);
                ToDate = new DateTime(Util.Now.Date.Year - 1, 12, 31);
            }
            else
            {
                FromDate = new DateTime(Util.Now.Date.Year, 1, 1);
                ToDate = (new DateTime(Util.Now.Date.Year, ((intQtr - 1) * 3), 1)).AddMonths(1).AddDays(-1);
            }
        }

        //public static int[] ReturnedReversedTypes = new int[] 
        //{ 
        //    (int)Contribution.TypeCode.ReturnedCheck, 
        //    (int)Contribution.TypeCode.Reversed 
        //};

        public IEnumerable<YearInfo> FetchYears()
        {
            var q = from c in DbUtil.Db.Contributions
                    where person.PeopleId == c.PeopleId || person == null
                    where c.PledgeFlag == false
                    where c.ContributionStatusId == (int)Contribution.StatusCode.Recorded
                    where !ReturnedReversedTypes.Contains(c.ContributionTypeId)
                    group c by c.ContributionDate.Value.Year into g
                    orderby g.Key descending
                    select new YearInfo
                    {
                        Year = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(c => c.ContributionAmount),
                        PeopleId = person == null ? 0 : person.PeopleId,
                    };
            return q;
        }
        public static int[] ReturnedReversedTypes = new int[] 
        { 
            (int)Contribution.TypeCode.ReturnedCheck, 
            (int)Contribution.TypeCode.Reversed 
        };

        int[] Gifts = new int[] { 2, 3, 4 };

        //public IEnumerable contributions(int pid, DateTime fromDate, DateTime toDate)
        //{
        //    var q = from c in DbUtil.Db.Contributions
        //            where c.PeopleId == pid
        //            where c.ContributionDate >= fromDate.Date && c.ContributionDate <= toDate.Date
        //            where !ReturnedReversedTypes.Contains(c.ContributionTypeId)
        //            where c.ContributionStatusId == (int)Contribution.StatusCode.Recorded
        //            select new
        //            {
        //                amount = c.ContributionAmount,
        //                date = c.ContributionDate,
        //                fundname = c.ContributionFund.FundName
        //            };
        //    return q;
        //}

        public static IEnumerable<ContributorInfo> contributors(DateTime fromDate, DateTime toDate, int PeopleId, int? SpouseId, int FamilyId)
        {
            //var pids = new int[] { 817023, 865610, 828611, 828612 };

            var q11 = from p in DbUtil.Db.Contributors(fromDate, toDate, PeopleId, SpouseId, FamilyId)
                      let option = (p.ContributionOptionsId ?? 0) == 0 ? 1 : p.ContributionOptionsId
                      let name = (option == 1 ?
                                 (p.Title != null ? p.Title + " " + p.Name : p.Name)
                                 : (p.SpouseId == null ?
                                     (p.Title != null ? p.Title + " " + p.Name : p.Name)
                                     : (p.HohFlag == 1 ?
                                         (p.Title != null ?
                                             p.Title + " and Mrs. " + p.Name
                                             : "Mr. and Mrs. " + p.Name)
                                         : (p.SpouseTitle != null ?
                                             p.SpouseTitle + " and Mrs. " + p.SpouseName
                                             : "Mr. and Mrs. " + p.SpouseName))))
                           + ((p.Suffix == null || p.Suffix == "") ? "" : ", " + p.Suffix)
                      //where pids.Contains(p.PeopleId)
                      where option == 1 || (option == 2 && p.HeadOfHouseholdId == p.PeopleId)
                      orderby p.FamilyId, p.PositionInFamilyId, p.Age
                      select new ContributorInfo
                      {
                          Name = name,
                          Address1 = p.PrimaryAddress,
                          Address2 = p.PrimaryAddress2,
                          City = p.PrimaryCity,
                          State = p.PrimaryState,
                          Zip = p.PrimaryZip,
                          PeopleId = p.PeopleId,
                          SpouseID = p.SpouseId,
                          DeacesedDate = p.DeceasedDate,
                          FamilyId = p.FamilyId,
                          Age = p.Age,
                          FamilyPositionId = p.PositionInFamilyId,
                          hohInd = p.HohFlag,
                      };
            return q11;
        }

        public static IEnumerable<ContributionInfo> contributions(int pid, int? spid, DateTime fromDate, DateTime toDate)
        {
            var q = from c in DbUtil.Db.Contributions
                    where !ReturnedReversedTypes.Contains(c.ContributionTypeId)
                    where c.ContributionTypeId != (int)Contribution.TypeCode.BrokeredProperty
                    where c.ContributionStatusId == (int)Contribution.StatusCode.Recorded
                    where c.ContributionDate >= fromDate.Date && c.ContributionDate <= toDate.Date
                    where c.PeopleId == pid || (c.Person.ContributionOptionsId == 2 && c.PeopleId == spid)
                    where !c.PledgeFlag
                    orderby c.ContributionDate
                    select new ContributionInfo
                    {
                        ContributionAmount = c.ContributionAmount,
                        ContributionDate = c.ContributionDate,
                        Fund = c.ContributionFund.FundName,
                    };

            return q;
        }

        public IEnumerable<ContributionInfo> gifts(int pid, int? spid, DateTime fromDate, DateTime toDate)
        {
            var q = from c in DbUtil.Db.Contributions
                    where c.ContributionTypeId == (int)Contribution.TypeCode.BrokeredProperty
                    where c.ContributionDate >= fromDate
                    where c.ContributionDate <= toDate
                    where c.PeopleId == pid || (c.Person.ContributionOptionsId == 2 && c.PeopleId == spid)
                    where c.PledgeFlag == false
                    orderby c.ContributionDate
                    select new ContributionInfo
                    {
                        ContributionAmount = c.ContributionAmount,
                        ContributionDate = c.ContributionDate,
                        Fund = c.ContributionFund.FundName,
                        Description = c.ContributionDesc,
                    };
            return q;
        }

        public static IEnumerable<PledgeSummaryInfo> pledges(int pid, int? spid, DateTime toDate)
        {
            var PledgeExcludes = new int[] 
            { 
                (int)Contribution.TypeCode.BrokeredProperty, 
                (int)Contribution.TypeCode.GraveSite, 
                (int)Contribution.TypeCode.Reversed 
            };

            var qp = from p in DbUtil.Db.Contributions
                     where p.PeopleId == pid || (p.Person.ContributionOptionsId == 2 && p.PeopleId == spid)
                     where p.PledgeFlag && p.ContributionTypeId == (int)Contribution.TypeCode.Pledge
                     where p.ContributionStatusId.Value != (int)Contribution.StatusCode.Reversed
                     where p.ContributionFund.FundStatusId == 1 // active
                     where p.ContributionDate <= toDate
                     select p;
            var qc = from c in DbUtil.Db.Contributions
                     where !PledgeExcludes.Contains(c.ContributionTypeId)
                     where c.PeopleId == pid || (c.Person.ContributionOptionsId == 2 && c.PeopleId == spid)
                     where !c.PledgeFlag
                     where c.ContributionStatusId != (int)Contribution.StatusCode.Reversed
                     where c.ContributionDate <= toDate
                     group c by c.FundId into g
                     select new { FundId = g.Key, Total = g.Sum(c => c.ContributionAmount) };
            var q = from p in qp
                    join c in qc on p.FundId equals c.FundId into items
                    from c in items.DefaultIfEmpty()
                    orderby p.ContributionFund.FundName descending
                    select new PledgeSummaryInfo
                    {
                        Fund = p.ContributionFund.FundName,
                        ContributionAmount = c.Total,
                        PledgeAmount = p.ContributionAmount,
                    };
            return q;
        }

        public static IEnumerable<ContributionInfo> quarterlySummary(int pid, int? spid, DateTime fromDate, DateTime toDate)
        {
            var q = from c in DbUtil.Db.Contributions
                    where c.ContributionTypeId == (int)Contribution.TypeCode.CheckCash
                    where c.ContributionStatusId == (int)Contribution.StatusCode.Recorded
                    where c.ContributionDate >= fromDate
                    where c.ContributionDate <= toDate
                    where c.PeopleId == pid || (c.Person.ContributionOptionsId == 2 && c.PeopleId == spid)
                    where c.PledgeFlag == false
                    group c by c.ContributionFund.FundName into g
                    orderby g.Key
                    select new ContributionInfo
                    {
                        ContributionAmount = g.Sum(z => z.ContributionAmount.Value),
                        Fund = g.Key,
                    };

            return q;
        }
    }
    public class PledgeSummaryInfo
    {
        public string Fund { get; set; }
        public decimal? ContributionAmount { get; set; }
        public decimal? PledgeAmount { get; set; }
        public string Description { get; set; }
    }
    public class ContributorInfo
    {
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public int PeopleId { get; set; }
        public int? SpouseID { get; set; }
        public int FamilyId { get; set; }
        public DateTime? DeacesedDate { get; set; }
        public string CityStateZip { get { return UtilityExtensions.Util.FormatCSZ4(City, State, Zip); } }
        public int hohInd { get; set; }
        public int FamilyPositionId { get; set; }
        public int? Age { get; set; }

    }
    public class ContributionInfo
    {
        public int BundleId { get; set; }
        public int ContributionId { get; set; }
        public string Fund { get; set; }
        public string ContributionType { get; set; }
        public int? ContributionTypeId { get; set; }
        public int? PeopleId { get; set; }
        public string Name { get; set; }
        public DateTime? ContributionDate { get; set; }
        public decimal? ContributionAmount { get; set; }
        public string Status { get; set; }
        public int? StatusId { get; set; }
        public bool Pledge { get; set; }
        public string Description { get; set; }
        public bool NotIncluded
        {
            get
            {
                if (!StatusId.HasValue)
                    return true;
                return StatusId.Value != (int)Contribution.StatusCode.Recorded
                    || ContributionModel.ReturnedReversedTypes.Contains(ContributionTypeId.Value);
            }
        }
    }
    public class YearInfo
    {
        public int PeopleId { get; set; }
        public int? Year { get; set; }
        public int? Count { get; set; }
        public decimal? Amount { get; set; }
    }
}


