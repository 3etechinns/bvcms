/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using CmsData.View;
using CmsWeb.Areas.Main.Controllers;
using UtilityExtensions;
using System.Web.Mvc;
using System.Web.Routing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Data.Linq;
using CmsData;
using System.Collections;
using CmsData.Codes;

namespace CmsWeb.Models
{
    public class OrgSearchModel : PagerModel2
    {
        public string Name { get; set; }
        public int? ProgramId { get; set; }
        public int? DivisionId { get; set; }
        public int? TagProgramId { get; set; }
        public int? TagDiv { get; set; }
        public int? ScheduleId { get; set; }
        public int? CampusId { get; set; }
        public int? StatusId { get; set; }
        public int? TypeId { get; set; }
        public string tagstr { get; set; }
        public int? OnlineReg { get; set; }
        public bool? MainFellowship { get; set; }
        public bool? ParentOrg { get; set; }

        public OrgSearchModel()
        {
            StatusId = OrgStatusCode.Active;
            GetCount = Count;
        }
        public Division Division()
        {
            var d = DbUtil.Db.Divisions.SingleOrDefault(dd => dd.Id == DivisionId);
            return d;
        }

        private IQueryable<CmsData.Organization> organizations;
        public IEnumerable<OrganizationInfo> OrganizationList()
        {
            organizations = FetchOrgs();
            if (!_count.HasValue)
                _count = organizations.Count();
            organizations = ApplySort(organizations).Skip(StartRow).Take(PageSize);
            return OrganizationList(organizations, TagProgramId, TagDiv);
        }
        public static IEnumerable<OrganizationInfo> OrganizationList(IQueryable<CmsData.Organization> query, int? TagProgramId, int? TagDiv)
        {
            var q = from o in query
                    let sc = o.OrgSchedules.FirstOrDefault() // SCHED
                    select new OrganizationInfo
                    {
                        Id = o.OrganizationId,
                        OrganizationStatus = o.OrganizationStatusId,
                        OrganizationName = o.OrganizationName,
                        LeaderName = o.LeaderName,
                        LeaderId = o.LeaderId,
                        MemberCount = o.MemberCount,
                        ClassFilled = o.ClassFilled ?? false,
                        RegClosed = o.RegistrationClosed ?? false,
                        RegTypeId = o.RegistrationTypeId,
                        ProgramName = o.Division.Program.Name,
                        DivisionId = o.DivisionId,
                        DivisionName = o.Division.Name,
                        Divisions = string.Join(",", o.DivOrgs.Select(d => d.Division.Name).ToArray()),
                        FirstMeetingDate = o.FirstMeetingDate.FormatDate(),
                        LastMeetingDate = o.LastMeetingDate.FormatDate(),
                        Schedule = DbUtil.Db.GetScheduleDesc(sc.MeetingTime),
                        Location = o.Location,
                        AllowSelfCheckIn = o.CanSelfCheckin ?? false,
                        BDayStart = o.BirthDayStart.FormatDate("na"),
                        BDayEnd = o.BirthDayEnd.FormatDate("na"),
                        Tag = (TagDiv ?? 0) == 0 ? "" : o.DivOrgs.Any(ot => ot.DivId == TagDiv) ? "Remove" : "Add",
                        ChangeMain = (o.DivisionId == null || o.DivisionId != TagDiv) && o.DivOrgs.Any(d => d.DivId == TagDiv),
                    };
            return q;
        }
        public IEnumerable OrganizationExcelList()
        {
            var q = FetchOrgs();
            var q2 = from o in q
                     let sc = o.OrgSchedules.FirstOrDefault() // SCHED
                     select new
                     {
                         OrgId = o.OrganizationId,
                         Name = o.OrganizationName,
                         o.Description,
                         Leader = o.LeaderName,
                         Members = o.MemberCount ?? 0,
                         Division = o.Division.Name,
                         FirstMeeting = o.FirstMeetingDate.FormatDate(),
                         LastMeeting = o.LastMeetingDate.FormatDate(),
                         Schedule = DbUtil.Db.GetScheduleDesc(sc.MeetingTime),
                         o.Location,
                         RegStart = o.RegStart.FormatDate(),
                         RegEnd = o.RegEnd.FormatDate(),
                         RollSheetVisitorWks = o.RollSheetVisitorWks ?? 0,
                         Limit = o.Limit.ToString(),
                         CampusId = o.CampusId ?? 0,
                         CanSelfCheckin = o.CanSelfCheckin ?? false,
                         BirthDayStart = o.BirthDayStart.FormatDate(),
                         BirthDayEnd = o.BirthDayEnd.FormatDate(),
                         Gender = o.Gender.Description,
                         GradeAgeStart = o.GradeAgeStart ?? 0,
                         LastDayBeforeExtra = o.LastDayBeforeExtra.FormatDate(),
                         NoSecurityLabel = o.NoSecurityLabel ?? false,
                         NumCheckInLabels = o.NumCheckInLabels ?? 0,
                         NumWorkerCheckInLabels = o.NumWorkerCheckInLabels ?? 0,
                         o.PhoneNumber,
                         MainFellowshipOrg = o.IsBibleFellowshipOrg ?? false,
                         EntryPoint = o.EntryPoint.Description,
                         o.OrganizationStatusId,
                     };
            return q2;
        }

        private int TagSubDiv(string s)
        {
            if (!s.HasValue())
                return 0;
            var a = s.Split(':');
            if (a.Length > 1)
                return a[1].ToInt();
            return 0;
        }

        private int? _count;
        public int Count()
        {
            if (!_count.HasValue)
                _count = FetchOrgs().Count();
            return _count.Value;
        }
        public IQueryable<CmsData.Organization> FetchOrgs()
        {
            var me = Util.UserPeopleId;

            if (organizations != null)
                return organizations;

            var u = DbUtil.Db.CurrentUser;

            var roles = u.UserRoles.Select(uu => uu.Role.RoleName).ToArray();
            organizations = from o in DbUtil.Db.Organizations
                            where o.LimitToRole == null || roles.Contains(o.LimitToRole)
                            select o;

            if (Util2.OrgMembersOnly)
                organizations = from o in organizations
                                where o.OrganizationMembers.Any(om => om.PeopleId == Util.UserPeopleId)
                                select o;
            else if (Util2.OrgLeadersOnly)
            {
                var oids = DbUtil.Db.GetLeaderOrgIds(Util.UserPeopleId);
                organizations = DbUtil.Db.Organizations.Where(o => oids.Contains(o.OrganizationId));
            }


            if (Name.HasValue())
            {
                if (Name.AllDigits())
                    organizations = from o in organizations
                                    where o.OrganizationId == Name.ToInt()
                                        || o.Location == Name
                                        || o.PendingLoc == Name
                                    select o;
                else
                    organizations = from o in organizations
                                    where o.OrganizationName.Contains(Name)
                                        || o.LeaderName.Contains(Name)
                                        || o.Location == Name
                                        || o.PendingLoc == Name
                                        || o.DivOrgs.Any(t => t.Division.Name.Contains(Name))
                                    select o;
            }
            if (DivisionId > 0)
                organizations = from o in organizations
                                where o.DivOrgs.Any(t => t.DivId == DivisionId)
                                select o;
            else if (ProgramId > 0)
                organizations = from o in organizations
                                where o.DivOrgs.Any(d => d.Division.ProgDivs.Any(p => p.ProgId == ProgramId))
                                || o.Division.ProgId == ProgramId
                                select o;

            if (ScheduleId > 0)
                organizations = from o in organizations
                                where o.OrgSchedules.Any(os => os.ScheduleId == ScheduleId)
                                select o;
            if (ScheduleId == -1)
                organizations = from o in organizations
                                where o.OrgSchedules.Count() == 0
                                select o;

            if (StatusId > 0)
                organizations = from o in organizations
                                where o.OrganizationStatusId == StatusId
                                select o;

            if (TypeId > 0)
                organizations = from o in organizations
                                where o.OrganizationTypeId == TypeId
                                select o;
            else if (TypeId == -1)
                organizations = from o in organizations
                                where o.OrganizationTypeId == null
                                select o;

            if (CampusId > 0)
                organizations = from o in organizations
                                where o.CampusId == CampusId
                                select o;
            else if (CampusId == -1)
                organizations = from o in organizations
                                where o.CampusId == null
                                select o;

            if (this.OnlineReg == 99)
                organizations = from o in organizations
                                where o.RegistrationTypeId > 0
                                select o;
            else if (this.OnlineReg > 0)
                organizations = from o in organizations
                                where o.RegistrationTypeId == OnlineReg
                                select o;
            else if (this.OnlineReg == 0)
                organizations = from o in organizations
                                where (o.RegistrationTypeId ?? 0) == 0
                                select o;

            if (MainFellowship == true)
                organizations = from o in organizations
                                where o.IsBibleFellowshipOrg == true
                                select o;

            if (ParentOrg == true)
                organizations = from o in organizations
                                where o.ChildOrgs.Any()
                                select o;

            return organizations;
        }
        public IQueryable<CmsData.Organization> ApplySort(IQueryable<CmsData.Organization> query)
        {
            if (Direction == "asc")
                switch (Sort)
                {
                    case "ID":
                        query = from o in query
                                orderby o.OrganizationId
                                select o;
                        break;
                    case "Division":
                    case "Program/Division":
                        query = from o in query
                                orderby o.Division.Program.Name, o.Division.Name,
                                o.OrganizationName
                                select o;
                        break;
                    case "Name":
                        query = from o in query
                                orderby o.OrganizationName
                                select o;
                        break;
                    case "Location":
                        query = from o in query
                                orderby o.Location
                                select o;
                        break;
                    case "Schedule":
                        query = from o in query
                                let sc = o.OrgSchedules.FirstOrDefault() // SCHED
                                orderby sc.ScheduleId
                                select o;
                        break;
                    case "Self CheckIn":
                        query = from o in query
                                orderby (o.CanSelfCheckin ?? false)
                                select o;
                        break;
                    case "Leader":
                        query = from o in query
                                orderby o.LeaderName,
                                o.OrganizationName
                                select o;
                        break;
                    case "Filled":
                        query = from o in query
                                orderby o.ClassFilled, o.OrganizationName
                                select o;
                        break;
                    case "Closed":
                        query = from o in query
                                orderby o.RegistrationClosed, o.OrganizationName
                                select o;
                        break;
                    case "Type":
                        query = from o in query
                                orderby o.RegistrationTypeId, o.OrganizationName
                                select o;
                        break;
                    case "Members":
                        query = from o in query
                                orderby o.MemberCount, o.OrganizationName
                                select o;
                        break;
                    case "FirstDate":
                        query = from o in query
                                orderby o.FirstMeetingDate, o.LastMeetingDate
                                select o;
                        break;
                    case "LastMeetingDate":
                        query = from o in query
                                orderby o.LastMeetingDate, o.FirstMeetingDate
                                select o;
                        break;
                }
            else
                switch (Sort)
                {
                    case "ID":
                        query = from o in query
                                orderby o.OrganizationId descending
                                select o;
                        break;
                    case "Program/Division":
                    case "Division":
                        query = from o in query
                                orderby o.Division.Program.Name descending, o.Division.Name descending,
                                o.OrganizationName descending
                                select o;
                        break;
                    case "Name":
                        query = from o in query
                                orderby o.OrganizationName descending
                                select o;
                        break;
                    case "Location":
                        query = from o in query
                                orderby o.Location descending
                                select o;
                        break;
                    case "Schedule":
                        query = from o in query
                                let sc = o.OrgSchedules.FirstOrDefault() // SCHED
                                orderby sc.ScheduleId descending
                                select o;
                        break;
                    case "Self CheckIn":
                        query = from o in query
                                orderby (o.CanSelfCheckin ?? false) descending
                                select o;
                        break;
                    case "Leader":
                        query = from o in query
                                orderby o.LeaderName descending,
                                o.OrganizationName descending
                                select o;
                        break;
                    case "Filled":
                        query = from o in query
                                orderby o.ClassFilled descending,
                                o.OrganizationName descending
                                select o;
                        break;
                    case "Closed":
                        query = from o in query
                                orderby o.RegistrationClosed descending,
                                o.OrganizationName descending
                                select o;
                        break;
                    case "Type":
                        query = from o in query
                                orderby o.RegistrationTypeId descending,
                                o.OrganizationName descending
                                select o;
                        break;
                    case "Members":
                        query = from o in query
                                orderby o.MemberCount descending,
                                o.OrganizationName descending
                                select o;
                        break;
                    case "FirstDate":
                        query = from o in query
                                orderby o.FirstMeetingDate descending,
                                o.LastMeetingDate descending
                                select o;
                        break;
                    case "LastMeetingDate":
                        query = from o in query
                                orderby o.LastMeetingDate descending,
                                o.FirstMeetingDate descending
                                select o;
                        break;
                }
            return query;
        }

        public IEnumerable<SelectListItem> StatusIds()
        {
            var q = from s in DbUtil.Db.OrganizationStatuses
                    select new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Description
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem { Value = "0", Text = "(not specified)" });
            return list;
        }
        public IEnumerable<SelectListItem> CampusIds()
        {
            var q = from c in DbUtil.Db.Campus
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
        public IEnumerable<SelectListItem> ProgramIds()
        {
            var q = from c in DbUtil.Db.Programs
                    orderby c.Name
                    select new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "(not specified)",
            });
            return list;
        }
        public IEnumerable<SelectListItem> DivisionIds()
        {
            return DivisionIds(ProgramId ?? 0);
        }
        public static IEnumerable<SelectListItem> DivisionIds(int ProgId)
        {
            var q = from d in DbUtil.Db.Divisions
                    where d.ProgId == ProgId || d.ProgDivs.Any(p => p.ProgId == ProgId)
                    orderby d.Name
                    select new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = ProgId == 0 ? "(select a program)" : "(not specified)",
            });
            return list;
        }
        public IEnumerable<SelectListItem> ScheduleIds()
        {
            var q = from sc in DbUtil.Db.OrgSchedules
                    group sc by new { sc.ScheduleId, sc.MeetingTime } into g
                    orderby g.Key.ScheduleId
                    where g.Key.ScheduleId != null
                    select new SelectListItem
                    {
                        Value = g.Key.ScheduleId.Value.ToString(),
                        Text = DbUtil.Db.GetScheduleDesc(g.Key.MeetingTime)
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem
            {
                Value = "-1",
                Text = "(None)",
            });
            list.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "(not specified)",
            });
            return list;
        }
        public IEnumerable<SelectListItem> OrgTypes()
        {
            var q = from t in DbUtil.Db.OrganizationTypes
                    orderby t.Code
                    select new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.Description
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem { Text = "(None)", Value = "-1", });
            list.Insert(0, new SelectListItem { Text = "(not specified)", Value = "0" });
            return list;
        }

        public IEnumerable<SelectListItem> RegistrationTypeIds()
        {
            var q = from o in CmsData.Codes.RegistrationTypeCode.GetCodePairs()
                    select new SelectListItem
                    {
                        Value = o.Key.ToString(),
                        Text = o.Value
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem
            {
                Value = "99",
                Text = "(any online reg)",
            });
            list.Insert(0, new SelectListItem
            {
                Value = "-1",
                Text = "(not specified)",
            });
            return list;
        }
        public static DateTime DefaultMeetingDate(int scheduleid)
        {
            var sdt = CmsData.Organization.GetDateFromScheduleId(scheduleid);
            if (sdt == null)
                return DateTime.Now.Date.AddHours(8);
            var dt = Util.Now.Date;
            dt = dt.AddDays(-(int)dt.DayOfWeek); // prev sunday
            dt = dt.AddDays((int)sdt.Value.Day);
            if (dt < Util.Now.Date)
                dt = dt.AddDays(7);
            return dt.Add(sdt.Value.TimeOfDay);
        }
        private static string RecentAbsentsEmail(OrgSearchController c, IEnumerable<RecentAbsent> list)
        {
            var q = from p in list
                    orderby p.Consecutive, p.Name2
                    select p;
            return ViewExtensions2.RenderPartialViewToString(c, "RecentAbsentsEmail", q);
        }
        public void SendNotices(OrgSearchController c)
        {
            const int days = 36;

            var olist = FetchOrgs().Select(oo => oo.OrganizationId).ToList();

            var alist = (from p in DbUtil.Db.RecentAbsents(null, null, days)
                         where olist.Contains(p.OrganizationId)
                         select p).ToList();

            var mlist = (from r in DbUtil.Db.LastMeetings(null, DivisionId, days)
                         where olist.Contains(r.OrganizationId)
                         select r).ToList();

            var plist = from om in DbUtil.Db.OrganizationMembers
                        where olist.Contains(om.OrganizationId)
                        where om.MemberType.AttendanceTypeId == AttendTypeCode.Leader
                        let u =
                            om.Person.Users.FirstOrDefault(uu => uu.UserRoles.Any(r => r.Role.RoleName == "Access"))
                        where u != null
                        group om.OrganizationId by om.Person
                            into leaderlist
                        orderby leaderlist.Key.Name2
                            select leaderlist;

            var sb2 = new StringBuilder("Notices sent to:</br>\n<table>\n");
            foreach (var p in plist)
            {
                var sb = new StringBuilder("The following meetings are ready to be viewed:<br/>\n");
                var leader = p.Key;
                var orgids = p.Select(vv => vv).ToList();
                var meetings = mlist.Where(m => orgids.Contains(m.OrganizationId)).ToList();
                foreach (var m in meetings)
                {
                    string orgname = Organization.FormatOrgName(m.OrganizationName, m.LeaderName, m.Location);
                    sb.AppendFormat("<a href='{0}Meeting/Index/{1}'>{2} - {3}</a><br/>\n",
                                    DbUtil.Db.CmsHost, m.MeetingId, orgname, m.Lastmeeting.FormatDateTm());
                    sb2.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2:g}</td></tr>\n",
                                     leader.Name, orgname, m.Lastmeeting.FormatDateTm());
                }
                foreach (var m in meetings)
                {
                    var absents = alist.Where(a => a.OrganizationId == m.OrganizationId);
                    sb.Append(RecentAbsentsEmail(c, absents));
                }
                DbUtil.Db.Email(DbUtil.Db.CurrentUser.Person.FromEmail, leader, null,
                                "Attendance reports are ready for viewing", sb.ToString(), false);
            }
            sb2.Append("</table>\n");
            DbUtil.Db.Email(DbUtil.Db.CurrentUser.Person.FromEmail, DbUtil.Db.CurrentUser.Person, null,
                            "Attendance emails sent", sb2.ToString(), false);
        }

        public class OrganizationInfo
        {
            public int Id { get; set; }
            public int? OrganizationStatus { get; set; }
            public string OrganizationName { get; set; }
            public string LeaderName { get; set; }
            public int? LeaderId { get; set; }
            public int? MemberCount { get; set; }
            public bool ClassFilled { get; set; }
            public bool RegClosed { get; set; }
            public int? RegTypeId { get; set; }
            public string RegType
            {
                get { return RegistrationTypeCode.Lookup(RegTypeId ?? 0); }
            }
            public string ProgramName { get; set; }
            public int? DivisionId { get; set; }
            public string DivisionName { get; set; }
            public string Divisions { get; set; }
            public string FirstMeetingDate { get; set; }
            public string LastMeetingDate { get; set; }
            public int SchedDay { get; set; }
            public string Schedule { get; set; }
            public string Location { get; set; }
            public string Tag { get; set; }
            public bool? ChangeMain { get; set; }
            public int? VisitorCount { get; set; }
            public bool AllowSelfCheckIn { get; set; }
            public string BDayStart { get; set; }
            public string BDayEnd { get; set; }
            public string ToolTip
            {
                get
                {
                    return "{0} ({1})|Program:{2}|Division: {3} ({4})|Leader: {5}|First Meeting: {6}|Last Meeting: {7}|Schedule: {8}|Location: {9}|Divisions: {10}".Fmt(
                               OrganizationName,
                               Id,
                               ProgramName,
                               DivisionName,
                               DivisionId,
                               LeaderName,
                               FirstMeetingDate,
                               LastMeetingDate,
                               Schedule,
                               Location,
                               Divisions
                               );
                }
            }
        }
        public class OrganizationInfoExcel
        {
            public int OrgId { get; set; }
            public string Status { get; set; }
            public string Name { get; set; }
            public string Leader { get; set; }
            public int Members { get; set; }
            public string Division { get; set; }
            public string FirstMeeting { get; set; }
            public DateTime? MeetingTime { get; set; }
            public string Schedule { get { return "{0:ddd h:mm tt}".Fmt(MeetingTime); } }
            public string Location { get; set; }
        }
    }
}
