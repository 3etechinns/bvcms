using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CmsData;
using CmsWeb.Models;

namespace CmsWeb.Areas.People.Models.Person
{
    public class PreviousEnrollments : PagedTableModel<EnrollmentTransaction, OrgMemberInfo>
    {
        private int PeopleId;
        public CmsData.Person person { get; set; }
        public PreviousEnrollments(int id)
            : base("Org Name", "asc")
        {
            PeopleId = id;
            person = DbUtil.Db.LoadPersonById(id);
        }
        private IQueryable<EnrollmentTransaction> enrollments;
        override public IQueryable<EnrollmentTransaction> ModelList()
        {
            if (enrollments != null)
                return enrollments;
            var limitvisibility = Util2.OrgMembersOnly || Util2.OrgLeadersOnly
                                  || !HttpContext.Current.User.IsInRole("Access");
            var roles = DbUtil.Db.CurrentRoles();
            return enrollments = from etd in DbUtil.Db.EnrollmentTransactions
                                 let org = etd.Organization
                                 where etd.TransactionStatus == false
                                 where etd.PeopleId == PeopleId
                                 where etd.TransactionTypeId >= 4
                                 where !(limitvisibility && etd.Organization.SecurityTypeId == 3)
                                 where org.LimitToRole == null || roles.Contains(org.LimitToRole)
                                 select etd;
        }
        override public IEnumerable<OrgMemberInfo> ViewList()
        {
            var q = ApplySort().Skip(Pager.StartRow).Take(Pager.PageSize);
            var q2 = from om in q
                     select new OrgMemberInfo
                     {
                         OrgId = om.OrganizationId,
                         PeopleId = om.PeopleId,
                         Name = om.OrganizationName,
                         MemberType = om.MemberType.Description,
                         EnrollDate = om.FirstTransaction.TransactionDate,
                         DropDate = om.TransactionDate,
                         AttendPct = om.AttendancePercentage,
                         DivisionName = om.Organization.Division.Program.Name + "/" + om.Organization.Division.Name,
                         OrgType = om.Organization.OrganizationType.Description ?? "Other"
                     };
            return q2;
        }
        override public IQueryable<EnrollmentTransaction> ApplySort()
        {
            var q = ModelList();
            switch (Pager.SortExpression)
            {
                case "Enroll Date":
                case "Enroll Date desc":
                    q = from om in q
                        orderby om.Organization.OrganizationType.Code ?? "z", om.EnrollmentDate
                        select om;
                    break;
                case "Org Name":
                case "Org Name desc":
                    q = from om in q
                        orderby om.Organization.OrganizationType.Code ?? "z", om.Organization.OrganizationName
                        select om;
                    break;
            }
            return q;
        }
    }
}
