﻿/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Web;
using System.Web.Mvc;
using CmsData;
using UtilityExtensions;
using System.Data.Linq.SqlClient;
using System.Web.UI.WebControls;
using System.Transactions;
using CMSPresenter;
using System.Text.RegularExpressions;

namespace CmsWeb.Models
{
    public class QuickSearchInfo
    {
        public QuickSearchInfo()
        {
            marital = 99;
            gender = 99;
        }
        public string name { get; set; }
        public string communication { get; set; }
        public string address { get; set; }
        public string birthdate { get; set; }
        public int campus { get; set; }
        public int memberstatus { get; set; }
        public int marital { get; set; }
        public int gender { get; set; }
    }
    public class QuickSearchModel : PagerModel2
    {
        private CMSDataContext Db;
        private int TagTypeId { get; set; }
        private string TagName { get; set; }
        private int? TagOwner { get; set; }

        public QuickSearchInfo m;

        public QuickSearchModel()
        {
            Db = DbUtil.Db;
            Direction = "asc";
            Sort = "Name";
            TagTypeId = DbUtil.TagTypeId_Personal;
            TagName = Util2.CurrentTagName;
            TagOwner = Util2.CurrentTagOwnerId;
            m = new QuickSearchInfo();
            GetCount = Count;
        }

        public bool usersonly { get; set; }

        private IQueryable<Person> people;
        public IQueryable<Person> FetchPeople()
        {
            if (people != null)
                return people;

            DbUtil.Db.SetNoLock();

            if (Util2.OrgMembersOnly)
                people = DbUtil.Db.OrgMembersOnlyTag2().People(DbUtil.Db);
            else
                people = DbUtil.Db.People.AsQueryable();
            
            if (usersonly)
                people = people.Where(p => p.Users.Count() > 0);
            
            if (m.memberstatus > 0)
                people = from p in people
                        where p.MemberStatusId == m.memberstatus
                        select p;
            if (m.name.HasValue())
            {
                string First, Last;
                NameSplit(m.name, out First, out Last);
                if (First.HasValue())
                    people = from p in people
                            where (p.LastName.StartsWith(Last) || p.MaidenName.StartsWith(Last)
                                || p.LastName.StartsWith(m.name) || p.MaidenName.StartsWith(m.name))
                            && (p.FirstName.StartsWith(First) || p.NickName.StartsWith(First) || p.MiddleName.StartsWith(First))
                            select p;
                else
                    if (Last.AllDigits())
                        people = from p in people
                                where p.PeopleId == Last.ToInt()
                                select p;
                    else
                        people = from p in people
                                where p.LastName.StartsWith(Last) || p.MaidenName.StartsWith(Last)
                                    || p.LastName.StartsWith(m.name) || p.MaidenName.StartsWith(m.name)
                                select p;
            }
            if (m.address.IsNotNull())
            {
                if (AddrRegex.IsMatch(m.address))
                {
                    var match = AddrRegex.Match(m.address);
                    m.address = match.Groups["addr"].Value;
                }
                m.address = m.address.Trim();
                if (m.address.HasValue())
                    people = from p in people
                            where p.Family.AddressLineOne.Contains(m.address)
                               || p.Family.AddressLineTwo.Contains(m.address)
                               || p.Family.CityName.Contains(m.address)
                               || p.Family.ZipCode.Contains(m.address)
                            select p;
            }
            if (m.communication.IsNotNull())
            {
                m.communication = m.communication.Trim();
                if (m.communication.HasValue())
                    people = from p in people
                            where p.CellPhone.Contains(m.communication)
                            || p.EmailAddress.Contains(m.communication)
                            || p.Family.HomePhone.Contains(m.communication)
                            || p.WorkPhone.Contains(m.communication)
                            select p;
            }
            if (m.birthdate.HasValue() && m.birthdate != "na")
            {
                DateTime dt;
                if (DateTime.TryParse(m.birthdate, out dt))
                    if (Regex.IsMatch(m.birthdate, @"\d+/\d+/\d+"))
                        people = people.Where(p => p.BirthDay == dt.Day && p.BirthMonth == dt.Month && p.BirthYear == dt.Year);
                    else
                        people = people.Where(p => p.BirthDay == dt.Day && p.BirthMonth == dt.Month);
                else
                {
                    int n;
                    if (int.TryParse(m.birthdate, out n))
                        if (n >= 1 && n <= 12)
                            people = people.Where(p => p.BirthMonth == n);
                        else
                            people = people.Where(p => p.BirthYear == n);
                }
            }
            if (m.campus > 0)
                people = people.Where(p => p.CampusId == m.campus);
            else if (m.campus == -1)
                people = people.Where(p => p.CampusId == null);
            if (m.gender != 99)
                people = people.Where(p => p.GenderId == m.gender);
            if (m.marital != 99)
                people = people.Where(p => p.MaritalStatusId == m.marital);
            return people;
        }
        
        public IEnumerable<TaggedPersonInfo> PeopleList()
        {
            var people = FetchPeople();
            if (!_count.HasValue)
                _count = people.Count();
            people = ApplySort(people)
                .Skip(StartRow).Take(PageSize);
            return PeopleList(people);
        }
        private IEnumerable<TaggedPersonInfo> PeopleList(IQueryable<Person> query)
        {
            var q = from p in query
                    select new TaggedPersonInfo
                    {
                        PeopleId = p.PeopleId,
                        Name = p.Name,
                        BirthDate = Util.FormatBirthday(p.BirthYear, p.BirthMonth, p.BirthDay),
                        Address = p.PrimaryAddress,
                        Address2 = p.PrimaryAddress2,
                        CityStateZip = Util.FormatCSZ(p.PrimaryCity, p.PrimaryState, p.PrimaryZip),
                        HomePhone = p.HomePhone,
                        CellPhone = p.CellPhone,
                        WorkPhone = p.WorkPhone,
                        PhonePref = p.PhonePrefId,
                        MemberStatus = p.MemberStatus.Description,
                        Email = p.EmailAddress,
                        BFTeacher = p.BFClass.LeaderName,
                        BFTeacherId = p.BFClass.LeaderId,
                        Age = p.Age.ToString(),
                        Deceased = p.DeceasedDate.HasValue,
                        HasTag = p.Tags.Any(t => t.Tag.Name == TagName && t.Tag.PeopleId == TagOwner && t.Tag.TypeId == TagTypeId),
                    };
            return q;
        }

        private static void NameSplit(string name, out string First, out string Last)
        {
            var a = name.Split(' ');
            First = "";
            if (a.Length > 1)
            {
                First = a[0];
                Last = a[1];
            }
            else
                Last = a[0];

        }
        public Regex AddrRegex = new Regex(
        @"\A(?<addr>.*);\s*(?<city>.*),\s+(?<state>[A-Z]*)\s+(?<zip>\d{5}(-\d{4})?)\z");

        public IQueryable<Person> ApplySort(IQueryable<Person> query)
        {
            switch (Direction)
            {
                case "asc":
                    switch (Sort)
                    {
                        case "Member":
                            query = from p in query
                                    orderby p.MemberStatus.Code,
                                    p.LastName,
                                    p.FirstName,
                                    p.PeopleId
                                    select p;
                            break;
                        case "Address":
                            query = from p in query
                                    orderby p.PrimaryState,
                                    p.PrimaryCity,
                                    p.PrimaryAddress,
                                    p.PeopleId
                                    select p;
                            break;
                        case "Fellowship Leader":
                            query = from p in query
                                    orderby p.BFClass.LeaderName,
                                    p.LastName,
                                    p.FirstName,
                                    p.PeopleId
                                    select p;
                            break;
                        case "DOB":
                            query = from p in query
                                    orderby p.BirthMonth, p.BirthDay,
                                    p.LastName, p.FirstName
                                    select p;
                            break;
                        case "Name":
                            query = from p in query
                                    orderby p.LastName,
                                    p.FirstName,
                                    p.PeopleId
                                    select p;
                            break;
                    }
                    break;
                case "desc":
                    switch (Sort)
                    {
                        case "Name":
                            query = from p in query
                                    orderby p.LastName descending,
                                    p.FirstName,
                                    p.PeopleId
                                    select p;
                            break;
                        case "Member":
                            query = from p in query
                                    orderby p.MemberStatus.Code descending,
                                    p.LastName descending,
                                    p.FirstName descending,
                                    p.PeopleId descending
                                    select p;
                            break;
                        case "Address":
                            query = from p in query
                                    orderby p.PrimaryState descending,
                                    p.PrimaryCity descending,
                                    p.PrimaryAddress descending,
                                    p.PeopleId descending
                                    select p;
                            break;
                        case "Fellowship Leader":
                            query = from p in query
                                    orderby p.BFClass.LeaderName descending,
                                    p.LastName descending,
                                    p.FirstName descending,
                                    p.PeopleId descending
                                    select p;
                            break;
                        case "DOB":
                            query = from p in query
                                    orderby p.BirthMonth descending, p.BirthDay descending,
                                    p.LastName descending, p.FirstName descending
                                    select p;
                            break;
                    }
                    break;
            }
            return query;
        }
        CodeValueController cv = new CodeValueController();
        public SelectList GenderCodes()
        {
            return new SelectList(cv.GenderCodesWithUnspecified(), "Id", "Value", m.gender);
        }
        public SelectList MaritalCodes()
        {
            return new SelectList(cv.MaritalStatusCodes99(), "Id", "Value", m.marital);
        }
        public IEnumerable<SelectListItem> Campuses()
        {
            var q = from c in DbUtil.Db.Campus
                    orderby c.Description
                    select new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Description
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem
            {
                Value = "-1",
                Text = "(not assigned)"
            });
            list.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "(not specified)"
            });
            return list;
        }
        public SelectList MemberStatusCodes()
        {
            return new SelectList(cv.MemberStatusCodes0(), "Id", "Value", m.memberstatus);
        }
        private int? _count;
        public int Count()
        {
            if (!_count.HasValue)
                _count = FetchPeople().Count();
            return _count.Value;
        }
        public int ConvertToQuery()
        {
            var qb = DbUtil.Db.QueryBuilderScratchPad();
            qb.CleanSlate(DbUtil.Db);

            if (m.memberstatus > 0)
                qb.AddNewClause(QueryType.MemberStatusId, CompareType.Equal, m.memberstatus);

            if (m.name.HasValue())
            {
                string First, Last;
                NameSplit(m.name, out First, out Last);
                if (First.HasValue())
                {
                    var g = qb.AddNewGroupClause(CompareType.AnyTrue);
                    g.AddNewClause(QueryType.LastName, CompareType.StartsWith, Last);
                    g.AddNewClause(QueryType.MaidenName, CompareType.StartsWith, Last);
                    g = qb.AddNewGroupClause(CompareType.AnyTrue);
                    g.AddNewClause(QueryType.FirstName, CompareType.StartsWith, First);
                    g.AddNewClause(QueryType.NickName, CompareType.StartsWith, First);
                    g.AddNewClause(QueryType.MiddleName, CompareType.StartsWith, First);
                }
                else
                {
                    if (Last.AllDigits())
                        qb.AddNewClause(QueryType.PeopleId, CompareType.Equal, Last.ToInt());
                    else
                        qb.AddNewClause(QueryType.LastName, CompareType.StartsWith, Last);
                }
            }
            if (m.address.IsNotNull())
            {
                if (AddrRegex.IsMatch(m.address))
                {
                    var match = AddrRegex.Match(m.address);
                    m.address = match.Groups["addr"].Value;
                }
                m.address = m.address.Trim();
                if (m.address.HasValue())
                {
                    var g = qb.AddNewGroupClause(CompareType.AnyTrue);
                    g.AddNewClause(QueryType.PrimaryAddress, CompareType.Contains, m.address);
                    g.AddNewClause(QueryType.PrimaryAddress2, CompareType.Contains, m.address);
                    g.AddNewClause(QueryType.PrimaryCity, CompareType.Contains, m.address);
                    g.AddNewClause(QueryType.PrimaryZip, CompareType.Contains, m.address);
                }
            }
            if (m.communication.IsNotNull())
            {
                m.communication = m.communication.Trim();
                if (m.communication.HasValue())
                {
                    var g = qb.AddNewGroupClause(CompareType.AnyTrue);
                    g.AddNewClause(QueryType.CellPhone, CompareType.Contains, m.communication);
                    g.AddNewClause(QueryType.EmailAddress, CompareType.Contains, m.communication);
                    g.AddNewClause(QueryType.HomePhone, CompareType.Contains, m.communication);
                    g.AddNewClause(QueryType.WorkPhone, CompareType.Contains, m.communication);
                }
            }
            if (m.birthdate.HasValue() && m.birthdate != "na")
            {
                DateTime dt;
                if (DateTime.TryParse(m.birthdate, out dt))
                    if (Regex.IsMatch(m.birthdate, @"\d+/\d+/\d+"))
                        qb.AddNewClause(QueryType.Birthday, CompareType.Equal, m.birthdate);
                    else
                        qb.AddNewClause(QueryType.Birthday, CompareType.Equal, m.birthdate);
                else
                {
                    int n;
                    if (int.TryParse(m.birthdate, out n))
                        if (n >= 1 && n <= 12)
                            qb.AddNewClause(QueryType.Birthday, CompareType.Equal, m.birthdate);
                        else
                            qb.AddNewClause(QueryType.Birthday, CompareType.Equal, m.birthdate);
                }
            }
            if (m.campus > 0)
                qb.AddNewClause(QueryType.CampusId, CompareType.Equal, m.campus);
            else if (m.campus == -1)
                qb.AddNewClause(QueryType.CampusId, CompareType.IsNull, m.campus);
            if (m.gender != 99)
                qb.AddNewClause(QueryType.GenderId, CompareType.Equal, m.gender);
            if (m.marital != 99)
                qb.AddNewClause(QueryType.MaritalStatusId, CompareType.Equal, m.marital);
            qb.AddNewClause(QueryType.IncludeDeceased, CompareType.Equal, "1,T");
            DbUtil.Db.SubmitChanges();
            return qb.QueryId;
        }
    }
}
