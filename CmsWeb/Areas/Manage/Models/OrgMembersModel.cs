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
using CmsData.Codes;

namespace CmsWeb.Models
{
    public class OrgMembersModel
    {
        public bool MembersOnly { get; set; }
        public bool SmallGroupsToo { get; set; }
        public int TargetId { get; set; }
        public int SourceId { get; set; }
        public int ProgId { get; set; }
        public int DivId { get; set; }
        public bool EmailAllNotices { get; set; }
        public string Grades { get; set; }
        public string Sort { get; set; }
        public string Dir { get; set; }

        public OrgMembersModel()
        {
            MembersOnly = true;
        }

        private IList<string> list = new List<string>();
        public IList<string> List
        {
            get { return list; }
            set { list = value; }
        }

        public void FetchSavedIds()
        {
            string pref = DbUtil.Db.UserPreference("OrgMembersModelIds", "0.0.0");
            var a = pref.Split('.').Select(s => s.ToInt()).ToArray();
            var prog = DbUtil.Db.Programs.SingleOrDefault(p => p.Id == a[0]);
            if (prog != null)
                ProgId = a[0];

            var div = DbUtil.Db.Divisions.SingleOrDefault(d => d.Id == a[1] && d.ProgId == ProgId);
            if (div != null)
                DivId = a[1];

            var source = DbUtil.Db.Organizations.Where(o => o.OrganizationId == a[2]).Select(o => o.OrganizationId).SingleOrDefault();
            SourceId = a[2];
        }
        public void ValidateIds()
        {
            var q = from prog in DbUtil.Db.Programs
                    where prog.Id == ProgId
                    let div = DbUtil.Db.Divisions.SingleOrDefault(d => d.Id == DivId && d.ProgId == ProgId)
                    let org = DbUtil.Db.Organizations.SingleOrDefault(o => o.OrganizationId == SourceId && o.DivOrgs.Any(d => d.DivId == DivId))
                    let org2 = DbUtil.Db.Organizations.SingleOrDefault(o => o.OrganizationId == SourceId && o.DivOrgs.Any(d => d.DivId == DivId))
                    select new { div, noorg = org == null, noorg2 = org2 == null };
            var i = q.SingleOrDefault();
            if (i == null)
                ProgId = DivId = SourceId = TargetId = 0;
            else
            {
                if (i.div == null)
                    DivId = SourceId = TargetId = 0;
                else
                {
                    if (i.noorg)
                        SourceId = 0;
                    if (i.noorg2)
                        TargetId = 0;
                }
            }
        }
        public IEnumerable<SelectListItem> Programs()
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
        public IEnumerable<SelectListItem> Divisions()
        {
            var q = from d in DbUtil.Db.Divisions
                    where d.ProgId == ProgId
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
                Text = "(not specified)",
            });
            return list;
        }
        public IEnumerable<SelectListItem> Organizations()
        {
        	var roles = DbUtil.Db.CurrentRoles();
            var q = from o in DbUtil.Db.Organizations
        	        where o.LimitToRole == null || roles.Contains(o.LimitToRole)
                    where o.DivOrgs.Any(di => di.DivId == DivId)
                    where o.OrganizationStatusId == OrgStatusCode.Active
                    orderby o.OrganizationName
                    let sctime = o.OrgSchedules.Count() == 1 ? " " + DbUtil.Db.GetScheduleDesc(o.OrgSchedules.First().MeetingTime) : ""
                    select new SelectListItem
                    {
                        Value = o.OrganizationId.ToString(),
                        Text = o.OrganizationName + sctime
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem { Value = "0", Text = "(not specified)" });
            return list;
        }
        public IEnumerable<SelectListItem> Organizations2()
        {
            var member = MemberTypeCode.Member;
        	var roles = DbUtil.Db.CurrentRoles();
            var q = from o in DbUtil.Db.Organizations
        	        where o.LimitToRole == null || roles.Contains(o.LimitToRole)
                    where o.DivOrgs.Any(di => di.DivId == DivId)
                    where o.OrganizationStatusId == OrgStatusCode.Active
                    orderby o.OrganizationName
                    let sctime = o.OrgSchedules.Count() == 1 ? " " + DbUtil.Db.GetScheduleDesc(o.OrgSchedules.First().MeetingTime) : ""
                    let cmales = o.OrganizationMembers.Count(m => m.Person.GenderId == 1 && m.MemberTypeId == member)
                    let cfemales = o.OrganizationMembers.Count(m => m.Person.GenderId == 2 && m.MemberTypeId == member)
                    select new SelectListItem
                    {
                        Value = o.OrganizationId.ToString(),
                        Text = o.OrganizationName + sctime + " (" + cmales + "+" + cfemales + "=" + (cmales + cfemales) + ")"
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem { Value = "0", Text = "(not specified)" });
            return list;
        }
        IQueryable<OrganizationMember> _members;
        private IQueryable<OrganizationMember> GetMembers()
        {
            if (_members == null)
            {
                var glist = new int[] {};
                if (Grades.HasValue())
                    glist = (from g in (Grades ?? "").Split(',')
                             select g.ToInt()).ToArray();
                var q = from om in DbUtil.Db.OrganizationMembers
                        where om.Organization.DivOrgs.Any(di => di.DivId == DivId)
                        where SourceId == 0 || om.OrganizationId == SourceId
                        where glist.Length == 0 || glist.Contains(om.Grade.Value)
                        where !MembersOnly || om.MemberTypeId == MemberTypeCode.Member
                        select om;
                _members = q;
            }
            return _members;
        }
        public IEnumerable<MemberInfo> Members()
        {
            var q = ApplySort();
            var q2 = from om in q
                     select new MemberInfo
                     {
                         PeopleId = om.PeopleId,
                         Age = om.Person.Age,
                         DOB = Util.FormatBirthday(om.Person.BirthYear, om.Person.BirthMonth, om.Person.BirthDay),
                         Gender = om.Person.Gender.Code,
                         Grade = om.Person.Grade,
                         OrgId = om.OrganizationId,
                         Request = om.Request,
                         Name = om.Person.Name,
                         isChecked = om.OrganizationId == TargetId,
                         MemberStatus = om.MemberType.Description,
                         OrgName = om.Organization.OrganizationName,
                     };
            return q2;
        }
        public int Count()
        {
            return GetMembers().Count();
        }
        public IEnumerable<OrganizationMember> ApplySort()
        {
            var q = GetMembers();

            if (Dir == "asc")
                switch (Sort)
                {
                    default:
                    case "Name":
                        q = from om in q
                            orderby om.Person.Name2
                            select om;
                        break;
                    case "DOB":
                        q = from om in q
                            orderby om.Person.BirthYear, om.Person.BirthMonth, om.Person.BirthDay
                            select om;
                        break;
                    case "Organization":
                        q = from om in q
                            orderby om.Organization.OrganizationName, om.Person.Name2
                            select om;
                        break;
                    case "Grade":
                        q = from om in q
                            orderby om.Person.Grade, om.Organization.OrganizationName, om.Person.Name2
                            select om;
                        break;
                    case "Gender":
                        q = from om in q
                            orderby om.Person.Gender.Code, om.Organization.OrganizationName, om.Person.Name2
                            select om;
                        break;
                }
            else
                switch (Sort)
                {
                    default:
                    case "Name":
                        q = from om in q
                            orderby om.Person.Name2 descending
                            select om;
                        break;
                    case "DOB":
                        q = from om in q
                            orderby om.Person.BirthYear descending, om.Person.BirthMonth descending, om.Person.BirthDay descending
                            select om;
                        break;
                    case "Organization":
                        q = from om in q
                            orderby om.Organization.OrganizationName descending, om.Person.Name2
                            select om;
                        break;
                    case "Grade":
                        q = from om in q
                            orderby om.Person.Grade descending, om.Organization.OrganizationName, om.Person.Name2
                            select om;
                        break;
                    case "Gender":
                        q = from om in q
                            orderby om.Person.Gender.Code descending, om.Organization.OrganizationName, om.Person.Name2
                            select om;
                        break;
                }
            return q;
        }
        public void Move()
        {
            foreach (var i in List)
            {
                if (!i.HasValue())
                    continue;
                var a = i.Split('.');
                if (a.Length != 2)
                    continue;
                var pid = a[0].ToInt();
                var oid = a[1].ToInt();
                if (oid == TargetId)
                    continue;
                var om = DbUtil.Db.OrganizationMembers.Single(m => m.PeopleId == pid && m.OrganizationId == oid);
                var sg = om.OrgMemMemTags.Select(mt => mt.MemberTag.Name).ToList();
                var tom = DbUtil.Db.OrganizationMembers.SingleOrDefault(m => m.PeopleId == pid && m.OrganizationId == TargetId);
                if (tom == null)
                {
                    tom = OrganizationMember.InsertOrgMembers(DbUtil.Db,
                        TargetId, pid, MemberTypeCode.Member, om.EnrollmentDate.Value, om.InactiveDate, om.Pending ?? false);
                    if (tom == null)
                        continue;
                }
                tom.Request = om.Request;
                tom.Amount = om.Amount;
                tom.UserData = om.UserData;
                tom.Grade = om.Grade;
                tom.RegisterEmail = om.RegisterEmail;
                tom.MemberTypeId = om.MemberTypeId;
                tom.ShirtSize = om.ShirtSize;
                tom.Tickets = om.Tickets;
                foreach (var s in sg)
                    tom.AddToGroup(DbUtil.Db, s);
                if (om.OrganizationId != tom.OrganizationId)
                    tom.Moved = true;
                om.Drop(DbUtil.Db, addToHistory:true);
                DbUtil.Db.SubmitChanges();
            }
			DbUtil.Db.UpdateMainFellowship(TargetId);
        }
        public int MovedCount()
        {
            var q = from om in DbUtil.Db.OrganizationMembers
                    where om.Organization.DivOrgs.Any(di => di.DivId == DivId)
                    where om.Moved == true
                    select om;
            return q.Count();
        }
        public int AllCount()
        {
            var q = from om in DbUtil.Db.OrganizationMembers
                    where om.Organization.DivOrgs.Any(di => di.DivId == DivId)
                    where om.Moved == true || EmailAllNotices
                    select om;
            return q.Count();
        }
        public void SendMovedNotices()
        {
            var Db = DbUtil.Db;
            var q0 = from o in Db.Organizations
                     where o.DivOrgs.Any(di => di.DivId == DivId)
                     where o.NotifyIds.Length > 0
                     where o.RegistrationTypeId > 0
                     select o;
            var onlineorg = q0.FirstOrDefault();
            if (onlineorg == null)
                return;

            var q = from om in Db.OrganizationMembers
                    where om.Organization.DivOrgs.Any(di => di.DivId == DivId)
                    where om.Moved == true || EmailAllNotices
                    select new
                    {
                        om,
                        om.Person,
                        om.Person.FromEmail,
                        om.Person.EmailAddress,
                        om.RegisterEmail,
                        om.Person.Name,
                        om.PeopleId,
                        om.Organization.OrganizationName,
                        om.Organization.Location,
                        om.Organization.LeaderName,
                        om.Organization.PhoneNumber
                    };
            var sb = new StringBuilder("Room Notices sent to:\r\n<pre>\r\n");
            foreach (var i in q)
            {
                string subj = "{0} room assignment".Fmt(i.OrganizationName);
                var msg = DbUtil.Db.ContentHtml("OrgMembersModel_SendMovedNotices", Resource1.OrgMembersModel_SendMovedNotices);
                msg = msg.Replace("{name}", i.Name);
                msg = msg.Replace("{org}", i.OrganizationName);
                msg = msg.Replace("{room}", i.Location);
                msg = msg.Replace("{leader}", i.LeaderName);
                msg = msg.Replace("{phone}", Util.PickFirst(i.PhoneNumber.FmtFone(), DbUtil.Db.Setting("ChurchPhone", "ChurchPhone")));
                msg = msg.Replace("{church}", DbUtil.Db.Setting("NameOfChurch", "NameOfChurch"));

                if (i.om.Moved == true || EmailAllNotices)
                {
                    if (i.RegisterEmail.HasValue())
                    {
                        Db.Email(Db.CurrentUser.Person.FromEmail, 
                            i.om.Person, Util.ToMailAddressList(i.RegisterEmail),
                            subj, msg, false);
                        sb.AppendFormat("\"{0}\" [{1}]R ({2}): {3}\r\n".Fmt(i.Name, i.FromEmail, i.PeopleId, i.Location));
                        i.om.Moved = false;
                    }
                
                    var flist = (from fm in i.om.Person.Family.People
                                 where fm.EmailAddress != null && fm.EmailAddress != ""
                                 where fm.EmailAddress != i.RegisterEmail
                                 where fm.PositionInFamilyId == PositionInFamily.PrimaryAdult
                                 select fm);
                    Db.Email(Db.CurrentUser.Person.FromEmail, flist, subj, msg);
                    foreach (var m in flist)
                    {
                        sb.AppendFormat("{0}P ({1}): {2}\r\n".Fmt(m, i.PeopleId, i.Location));
                        i.om.Moved = false;
                    }
                }
            }
            sb.Append("</pre>\n");
            Db.Email(Db.CurrentUser.Person.FromEmail, 
                Db.PeopleFromPidString(onlineorg.NotifyIds), 
                "room notices sent to:", sb.ToString());
            Db.SubmitChanges();
        }

        public class MemberInfo
        {
            public int PeopleId { get; set; }
            public string Name { get; set; }
            public int OrgId { get; set; }
            public string OrgName { get; set; }
            public string MemberStatus { get; set; }
            public int? Grade { get; set; }
            public int? Age { get; set; }
            public string DOB { get; set; }
            public string Gender { get; set; }
            public string Request { get; set; }
            public bool isChecked { get; set; }
            public string Checked
            {
                get { return isChecked ? "checked='checked'" : ""; }
            }
        }
        public class OrgExcelResult : ActionResult
        {
            private int oid;
            public OrgExcelResult(int oid)
            {
                this.oid = oid;
            }
            public override void ExecuteResult(ControllerContext context)
            {
                var Response = context.HttpContext.Response;
                Response.Buffer = true;
                Response.ContentType = "application/vnd.ms-excel";
                Response.AddHeader("Content-Disposition", "attachment;filename=CMSOrganizations.xls");
                Response.Charset = "";
                var d = from om in DbUtil.Db.OrganizationMembers
                        where om.OrganizationId == oid
                        select new
                        {
                            om.PeopleId,
                            om.OrganizationId,
                            Groups = string.Join(",", om.OrgMemMemTags.Select(mt => mt.MemberTag.Name).ToArray()),
                        };
                var dg = new DataGrid();
                dg.DataSource = d;
                dg.DataBind();
                dg.RenderControl(new HtmlTextWriter(Response.Output));
            }
        }
    }
}
