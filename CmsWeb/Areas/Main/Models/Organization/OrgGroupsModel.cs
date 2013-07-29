using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CmsData;
using System.Web.Mvc;
using CmsWeb.Code;
using UtilityExtensions;
using System.Text.RegularExpressions;
using System.Collections;
using CmsData.Codes;

namespace CmsWeb.Models
{
    public class OrgGroupsModel
    {
        public int orgid { get; set; }
        public int? groupid { get; set; }
        public string GroupName { get; set; }
        public string ingroup { get; set; }
        public string notgroup { get; set; }
        public bool notgroupactive { get; set; }
        public string sort { get; set; }
        public int tagfilter { get; set; }
        public bool isRecreationTeam { get; set; }
        public string OrgName { get; set; }

        public OrgGroupsModel() { }

        public OrgGroupsModel( int id )
        {
            orgid = id;

            var org = DbUtil.Db.LoadOrganizationById(orgid);
            isRecreationTeam = org.IsRecreationTeam;
            OrgName = org.OrganizationName;
        }

        /*
        public string OrgName
        {
            get { return DbUtil.Db.LoadOrganizationById(orgid).OrganizationName; }
        }
        // */

        public int memtype { get; set; }

        private IList<int> list = new List<int>();
        public IList<int> List
        {
            get { return list; }
            set { list = value; }
        }
        public SelectList Groups()
        {
            var q = from g in DbUtil.Db.MemberTags
                    where g.OrgId == orgid
                    orderby g.Name
                    select new
                    {
                        value = g.Id,
                        text = g.Name,
                    };
            var list = q.ToList();
            list.Insert(0, new { value = 0, text = "(not specified)" });
            return new SelectList(list, "value", "text", groupid.ToString());
        }
        private List<SelectListItem> mtypes;
        private List<SelectListItem> MemberTypes()
        {
            if (mtypes == null)
            {
                var q = from om in DbUtil.Db.OrganizationMembers
                        where om.OrganizationId == orgid
                        where (om.Pending ?? false) == false
                        where om.MemberTypeId != MemberTypeCode.InActive
                        group om by om.MemberType into g
                        orderby g.Key.Description
                        select new SelectListItem
                        {
                            Value = g.Key.Id.ToString(),
                            Text = g.Key.Description,
                        };
                mtypes = q.ToList();
            }
            return mtypes;
        }
        public IEnumerable<SelectListItem> MemberTypeCodesWithNotSpecified()
        {
            var mt = MemberTypes().ToList();
            mt.Insert(0, new SelectListItem { Value = "0", Text = "(not specified)" });
            return mt;
        }

        public int count;
        public IEnumerable<PersonInfo> FetchOrgMemberList()
        {
            if (ingroup == null)
                ingroup = string.Empty;
            var q = OrgMembers();
            if (memtype != 0)
                q = q.Where(om => om.MemberTypeId == memtype);
			if (ingroup.HasValue())
			{
				var groups = ingroup.Split(',');
				for (var i = 0; i < groups.Length; i++)
				{
					var group = groups[i];
					q = q.Where(om => om.OrgMemMemTags.Any(omt => omt.MemberTag.Name.StartsWith(group)));
				}
			}
            if (notgroupactive)
                if (notgroup.HasValue())
                    q = q.Where(om => !om.OrgMemMemTags.Any(omt => omt.MemberTag.Name.StartsWith(notgroup)));
                else
                    q = q.Where(om => om.OrgMemMemTags.Count() == 0);

            count = q.Count();
            if (!sort.HasValue())
                sort = "Name";
            switch(sort)
            {
                case "Request":
                    q = from m in q
                         let ck = m.OrgMemMemTags.Any(g => g.MemberTagId == groupid.ToInt())
                        orderby !ck, m.Request == null ? 2 : 1, m.Request
                        select m;
                    break;
                case "Score":
                    q = from m in q
                        let ck = m.OrgMemMemTags.Any(g => g.MemberTagId == groupid.ToInt())
                        orderby !ck, m.Score descending
                        select m;
                    break;
                case "Name":
                    q = from m in q
                        let ck = m.OrgMemMemTags.Any(g => g.MemberTagId == groupid.ToInt())
                        orderby !ck, m.Person.Name2
                        select m;
                    break;
                case "Groups":
                    q = from m in q
                        let ck = m.OrgMemMemTags.Any(g => g.MemberTagId == groupid.ToInt())
                        let grp = (from g in m.OrgMemMemTags
                                  where g.MemberTag.Name.StartsWith(ingroup)
                                  orderby g.MemberTag.Name
                                  select g.MemberTag.Name).FirstOrDefault()
                        orderby !ck, grp, m.Person.Name2
                        select m;
                    break;
            }
            var q2 = from m in q
                     let p = m.Person
                     let ck = m.OrgMemMemTags.Any(g => g.MemberTagId == groupid.ToInt())
                     select new PersonInfo
                     {
                         PeopleId = m.PeopleId,
                         Name = p.Name,
                         LastName = p.LastName,
                         JoinDate = p.JoinDate,
                         BirthDate = p.DOB,
                         Address = p.PrimaryAddress,
                         Address2 = p.PrimaryAddress2,
                         CityStateZip = p.CityStateZip5,
                         HomePhone = p.HomePhone.FmtFone(),
                         CellPhone = p.CellPhone.FmtFone(),
                         WorkPhone = p.WorkPhone.FmtFone(),
                         Email = p.EmailAddress,
                         Age = p.Age,
                         MemberStatus = p.MemberStatus.Description,
                         ischecked = ck,
                         Gender = p.Gender.Description,
                         Request = m.Request,
                         Score = m.Score,
                         Groups = from mt in m.OrgMemMemTags
                                  let ck2 = mt.MemberTag.Name.StartsWith(ingroup)
                                  orderby ck2 descending, mt.MemberTag.Name
                                  select new GroupInfo
                                  {
                                      Name = mt.MemberTag.Name,
                                      Count = mt.MemberTag.OrgMemMemTags.Count()
                                  }
                     };
            return q2;
        }
        public IQueryable<OrganizationMember> OrgMembers()
        {
            var q = from om in DbUtil.Db.OrganizationMembers
                    where om.OrganizationId == orgid
                    where tagfilter == 0 || DbUtil.Db.TagPeople.Any(tt => tt.PeopleId == om.PeopleId && tt.Id == tagfilter)
                    //where om.OrgMemMemTags.Any(g => g.MemberTagId == sg) || (sg ?? 0) == 0
                    select om;
            return q;
        }
        public IEnumerable<SelectListItem> Tags()
        {
            var cv = new CodeValueModel();
            var tg = CodeValueModel.ConvertToSelect(cv.UserTags(Util.UserPeopleId), "Id").ToList();
            tg.Insert(0, new SelectListItem { Value = "0", Text = "(not specified)" });
            return tg;
        }
        public class GroupInfo
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }
        public class PersonInfo
        {
            public int PeopleId { get; set; }
            public string Name { get; set; }
            public string LastName { get; set; }
            public DateTime? JoinDate { get; set; }
            public string Email { get; set; }
            public string BirthDate { get; set; }
            public string Address { get; set; }
            public string Address2 { get; set; }
            public string CityStateZip { get; set; }
            public string HomePhone { get; set; }
            public string CellPhone { get; set; }
            public string WorkPhone { get; set; }
            public int? Age { get; set; }
            public string MemberStatus { get; set; }
            public string Gender { get; set; }
            public string Request { get; set; }
            public int Score { get; set; }
            public IEnumerable<GroupInfo> Groups { get; set; }
            public HtmlString GroupsDisplay
            {
                get
                {
                    var s = string.Join(",~", Groups.Select(g => "{0}({1})".Fmt(g.Name, g.Count)).ToArray());
                    s = s.Replace(" ", "&nbsp;").Replace(",~", "<br />\n");
                    return new HtmlString(s);
                }
            }
            public bool ischecked { get; set; }
            public HtmlString IsInGroup()
            {
                var s = ischecked ? "style='color:blue;'" : "";
                return new HtmlString(s);
            }
            public string ToolTip
            {
                get
                {
                    return "{0} ({1})|Cell Phone: {2}|Work Phone: {3}|Home Phone: {4}|BirthDate: {5:d}|Join Date: {6:d}|Status: {7}|Email: {8}"
                        .Fmt(Name, PeopleId, CellPhone, WorkPhone, HomePhone, BirthDate, JoinDate, MemberStatus, Email);
                }
            }
        }
    }
}
