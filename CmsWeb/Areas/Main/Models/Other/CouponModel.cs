/* Author: David Carroll
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
using System.Web.Routing;
using UtilityExtensions;
using System.Text;
using CmsData;
using System.Data.Linq.SqlClient;

namespace CmsWeb.Models
{
    public class CouponModel
    {
        public string regid { get; set; }
        public decimal amount { get; set; }
        public string name { get; set; }
        public string couponcode { get; set; }

        public int useridfilter { get; set; }
        public string regidfilter { get; set; }
        public string usedfilter { get; set; }
        public string date { get; set; }

        public string Registration()
        {
            return OnlineRegs().Single(r => r.Value == regid).Text;
        }

        public IEnumerable<CouponInfo> Coupons()
        {
            var q = from c in DbUtil.Db.Coupons
                    where c.DivOrg == regidfilter || regidfilter == "0" || regidfilter == null
                    where c.UserId == useridfilter || useridfilter == 0
                    select c;
            switch (usedfilter)
            {
                case "Used":
                    q = q.Where(c => c.Used != null && c.Canceled == null);
                    break;
                case "UnUsed":
                    q = q.Where(c => c.Used == null && c.Canceled == null);
                    break;
                case "Canceled":
                    q = q.Where(c => c.Canceled != null);
                    break;
            }
            if (name.HasValue())
                q = q.Where(c => c.Name.Contains(name) || c.Person.Name.Contains(name));
            if (date.HasValue())
            {
                DateTime bd;
                if (DateTime.TryParse(date, out bd))
                    q = q.Where(c => c.Created.Date == bd);
            }


            var q2 = from c in q
                     orderby c.Created descending
                     select new CouponInfo
                     {
                         Amount = c.Amount,
                         Canceled = c.Canceled,
                         Code = c.Id,
                         Created = c.Created,
                         OrgDivName = c.OrgId != null ? c.Organization.OrganizationName : c.Division.Name,
                         Used = c.Used,
                         PeopleId = c.PeopleId,
                         Name = c.Name,
                         Person = c.Person.Name,
                         UserId = c.UserId,
                         UserName = c.User.Name,
                         RegAmt = c.RegAmount
                     };
            return q2.Take(200);
        }
        public IEnumerable<CouponInfo2> Coupons2()
        {
            var q = from c in DbUtil.Db.Coupons
                    where c.DivOrg == regidfilter || regidfilter == "0" || regidfilter == null
                    where c.UserId == useridfilter || useridfilter == 0
                    select c;
            switch (usedfilter)
            {
                case "Used":
                    q = q.Where(c => c.Used != null && c.Canceled == null);
                    break;
                case "UnUsed":
                    q = q.Where(c => c.Used == null && c.Canceled == null);
                    break;
                case "Canceled":
                    q = q.Where(c => c.Canceled != null);
                    break;
            }
            if (name.HasValue())
                q = q.Where(c => c.Name.Contains(name) || c.Person.Name.Contains(name));
            if (date.HasValue())
            {
                DateTime bd;
                if (DateTime.TryParse(date, out bd))
                    q = q.Where(c => c.Created.Date == bd);
            }


            var q2 = from c in q
                     orderby c.Created descending
                     select new CouponInfo2
                     {
                         Amount = c.Amount ?? 0,
                         Canceled = c.Canceled ?? DateTime.Parse("1/1/80"),
                         Code = c.Id,
                         Created = c.Created,
                         OrgDivName = c.OrgId != null ? c.Organization.OrganizationName : c.Division.Name,
                         Used = c.Used ?? DateTime.Parse("1/1/80"),
                         PeopleId = c.PeopleId ?? 0,
                         Name = c.Name,
                         Person = c.Person.Name,
                         UserId = c.UserId ?? 0,
                         UserName = c.User.Name,
                         RegAmt = c.RegAmount ?? 0
                     };
            return q2.Take(200);
        }
        public List<SelectListItem> OnlineRegs()
        {
            var orgregtypes = new int[] { 1, 2 };
            var divregtypes = new int[] { 3, 4 };

            var q = (from o in DbUtil.Db.Organizations
                     where orgregtypes.Contains(o.RegistrationTypeId.Value)
                     where o.ClassFilled != true
                     where (o.RegistrationClosed ?? false) == false
                     select new { DivisionName = o.Division.Name, o.OrganizationName, o.RegSetting, o.OrganizationId }).ToList();

            var q2 = (from o in DbUtil.Db.Organizations
                      where divregtypes.Contains(o.RegistrationTypeId.Value)
                      where o.ClassFilled != true
                      where (o.RegistrationClosed ?? false) == false
                      select new { o.DivisionId, DivisionName = o.Division.Name, o.RegSetting, o.OrganizationId }).ToList();

            var qq = from i in q
                     let os = new RegSettings(i.RegSetting, DbUtil.Db, i.OrganizationId)
                     where (os.Fee ?? 0) > 0
                     select new SelectListItem
                     { 
                         Text = i.DivisionName + ":" + i.OrganizationName,
                         Value = "org." + i.OrganizationId
                     };

            var qq2 = from i in q2
                      let os = new RegSettings(i.RegSetting, DbUtil.Db, i.OrganizationId)
                      where (os.Fee ?? 0) > 0
                      group i by new { i.DivisionId, i.DivisionName } into g
                      select new SelectListItem
                      { 
                          Text = g.Key.DivisionName,
                          Value = "div." + g.Key.DivisionId
                      };

            var list = qq.Union(qq2).OrderBy(n => n.Text).ToList(); 

            list.Insert(0, new SelectListItem { Text = "(not specified)", Value = "0" });
            return list;
        }
        public List<SelectListItem> Users()
        {
            var q = from c in DbUtil.Db.Coupons
                    where c.UserId != null
                    group c by c.UserId into g
                    select new SelectListItem
                    {
                        Value = g.Key.ToString(),
                        Text = g.First().User.Name,
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem { Text = "(not specified)", Value = "0" });
            return list;
        }
        public List<SelectListItem> CouponStatus()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "(not specified)" },
                new SelectListItem { Text = "Used" },
                new SelectListItem { Text = "UnUsed" },
                new SelectListItem { Text = "Canceled" },
            };
        }
        public static bool IsExisting(string code)
        {
            bool existing;
            var q = from cp in DbUtil.Db.Coupons
                    where cp.Id == code
                    where cp.Used == null && cp.Canceled == null
                    select cp;
            existing = q.SingleOrDefault() != null;
            return existing;
        }
        public Coupon CreateCoupon()
        {
            string code = couponcode;
            if (!couponcode.HasValue())
            {
                do
                {
                    code = Util.RandomPassword(12);
                }
                while (IsExisting(code));
            }

            var c = new Coupon
            {
                Id = code,
                Created = DateTime.Now,
                Amount = amount,
                Name = name,
                UserId = Util.UserId,
            };
            SetDivOrgIds(c);
            DbUtil.Db.Coupons.InsertOnSubmit(c);
            DbUtil.Db.SubmitChanges();
            couponcode = Util.fmtcoupon(c.Id);

            return c;
        }
        private void SetDivOrgIds(Coupon c)
        {
            if (regid.HasValue())
            {
                var a = regid.Split('.');
                switch (a[0])
                {
                    case "org":
                        c.OrgId = a[1].ToInt();
                        break;
                    case "div":
                        c.DivId = a[1].ToInt();
                        break;
                }
            }
        }
        public class CouponInfo
        {
            public string Code { get; set; }
            public string Coupon
            {
                get { return Util.fmtcoupon(Code); }
            }
            public string OrgDivName { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Used { get; set; }
            public DateTime? Canceled { get; set; }
            public decimal? Amount { get; set; }
            public decimal? RegAmt { get; set; }
            public int? PeopleId { get; set; }
            public string Name { get; set; }
            public string Person { get; set; }
            public int? UserId { get; set; }
            public string UserName { get; set; }
        }
        public class CouponInfo2
        {
            public string Code;
            public string Coupon
            {
                get { return Code.Insert(8, " ").Insert(4, " "); }
            }
            public string OrgDivName { get; set; }
            public DateTime Created { get; set; }
            public DateTime Used { get; set; }
            public DateTime Canceled { get; set; }
            public decimal Amount { get; set; }
            public decimal RegAmt { get; set; }
            public int PeopleId { get; set; }
            public string Name { get; set; }
            public string Person { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; }
        }
    }
}
