using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityExtensions;
using System.Web.Security;

namespace CmsData
{
    public partial class User
    {
        public bool InRole(string role)
        {
            return Roles.Any(ro => ro == role);
        }

        public bool IsOnLine
        {
            get
            {
                var onlineSpan = new TimeSpan(0, Membership.UserIsOnlineTimeWindow, 0);
                var compareTime = Util.Now.Subtract(onlineSpan);
                return LastActivityDate > compareTime && LastActivityDate != CreationDate;
            }
        }

        public string BestName
        {
            get { return PeopleId.HasValue ? Name2 : Username; }
        }

        public string[] Roles
        {
            get { return UserRoles.Select(ur => ur.Role.RoleName).ToArray(); }
        }

        public static IEnumerable<Role> AllRoles(CMSDataContext Db)
        {
            var roles = Db.Roles.ToList();
//            var list = new List<string> 
//            {
//               "Admin",
//               "Edit",
//               "Access",
//               "Developer",
//               "ApplicationReview",
//               "OrgTagger",
//               "OrgMembersOnly",
//               "Attendance",
//               "Finance",
//               "Membership",
//               "ManageGroups",
//               "Coupon",
//               "Manager",
//               "OrgLeadersOnly",
//               "ScheduleEmails",
//               "ManageTransactions",
//               "ManageEmails",
//               "BackgroundCheck",
//               "CreditCheck",
//               "Coupon2",
//               "Manager2",
//               "ContentEdit",
//               "Design",
//            };
//            var adds = from r in list
//                       join e in Db.Roles on r equals e.RoleName into j
//                       from e in j.DefaultIfEmpty()
//                       where e == null
//                       select r;
//            foreach (var r in adds)
//            {
//                var role = new Role {Hardwired = true, RoleName = r};
//                Db.Roles.InsertOnSubmit(role);
//                Db.SubmitChanges();
//                roles.Add(role);
//            }
            return roles.OrderBy(rr => rr.RoleName == "NEW" ? 1 : 0).ThenBy(rr => rr.RoleName);
        }

        public void SetRoles(CMSDataContext Db, string[] value)
        {
            if (value == null)
            {
                Db.UserRoles.DeleteAllOnSubmit(UserRoles);
                return;
            }
            var qdelete = from r in UserRoles
                where !value.Contains(r.Role.RoleName)
                select r;
            Db.UserRoles.DeleteAllOnSubmit(qdelete);

            var q = from s in value
                join r in UserRoles on s equals r.Role.RoleName into g
                from t in g.DefaultIfEmpty()
                where t == null
                select s;

            foreach (var s in q)
            {
                var role = Db.Roles.Single(r => r.RoleName == s);
                UserRoles.Add(new UserRole {Role = role});
            }
        }

        public void ChangePassword(string newpassword)
        {
            CMSMembershipProvider.provider.AdminOverride = true;
            var mu = CMSMembershipProvider.provider.GetUser(Username, false);
            if (mu == null)
                return;
            mu.UnlockUser();
            mu.ChangePassword(mu.ResetPassword(), newpassword);
            TempPassword = newpassword;
            CMSMembershipProvider.provider.AdminOverride = false;
        }

        public string PasswordSetOnly { get; set; }

        public bool CanAssign(CMSDataContext db, string role)
        {
            if (role == "Finance" || role == "FinanceAdmin")
                return db.CurrentUser.InRole("Finance") && db.CurrentUser.InRole("Admin");
            if (role == "Developer")
                return db.CurrentUser.InRole("Developer");
            if (role == "Delete")
                return db.CurrentUser.InRole("Developer");
            return db.CurrentUser.InRole("Admin");
        }
    }
}
