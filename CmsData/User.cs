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
        public bool IsOnLine
        {
            get
            {
                var onlineSpan = new TimeSpan(0, Membership.UserIsOnlineTimeWindow, 0);
                var compareTime = Util.Now.Subtract(onlineSpan);
                return LastActivityDate > compareTime && LastActivityDate != CreationDate;
            }
        }
        public string BestName { get { return PeopleId.HasValue ? Name2 : Username; } }
        public string[] Roles
        {
            get { return UserRoles.Select(ur => ur.Role.RoleName).ToArray(); }
        }
        public void SetRoles(CMSDataContext Db, string[] value, bool InFinance)
        {
            if (value == null)
            {
                Db.UserRoles.DeleteAllOnSubmit(UserRoles);
                return;
            }
            var qdelete = from r in UserRoles
                          where !value.Contains(r.Role.RoleName)
                          where r.Role.RoleName != "Finance" || InFinance
                          select r;
            Db.UserRoles.DeleteAllOnSubmit(qdelete);

            var q = from s in value
                    join r in UserRoles on s equals r.Role.RoleName into g
                    from t in g.DefaultIfEmpty()
                    where t == null
                    select s;

            foreach (var s in q)
            {
                if (s == "Finance" && !InFinance)
                    continue;
                var role = Db.Roles.Single(r => r.RoleName == s);
                UserRoles.Add(new UserRole { Role = role });
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
    }
}
