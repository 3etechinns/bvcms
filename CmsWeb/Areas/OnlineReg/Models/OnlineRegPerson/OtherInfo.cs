using System.Collections.Generic;
using System.Linq;
using CmsData;
using CmsData.Registration;
using UtilityExtensions;
using System.Web.Mvc;

namespace CmsWeb.Models
{
    public partial class OnlineRegPersonModel
    {

        public string ExtraQuestionValue(int set, string s)
        {
            if (ExtraQuestion[set].ContainsKey(s))
                return ExtraQuestion[set][s];
            return null;
        }
        public string TextValue(int set, string s)
        {
            if (Text[set].ContainsKey(s))
                return Text[set][s];
            return null;
        }

        public bool Attended(int id)
        {
            if (FamilyAttend == null) 
                return false;
            var a = FamilyAttend.SingleOrDefault(aa => aa.PeopleId == id);
            if (a == null)
                return false;
            return a.Attend;
        }

        public bool YesNoChecked(string key, bool value)
        {
            if (YesNoQuestion != null && YesNoQuestion.ContainsKey(key))
                return YesNoQuestion[key] == value;
            return false;
        }

        public bool CheckboxChecked(string sg)
        {
            if (Checkbox == null)
                return false;
            return Checkbox.Contains(sg);
        }

        private List<string> _GroupTags;
        public List<string> GroupTags
        {
            get
            {
                if (_GroupTags == null)
                    _GroupTags = (from mt in DbUtil.Db.OrgMemMemTags
                                  where mt.OrgId == org.OrganizationId
                                  select mt.MemberTag.Name).ToList();
                var gtdd = (from pp in Parent.List
                            where pp != this
                            where pp.option != null
                            from oo in pp.option
                            where oo.HasValue()
                            select oo).ToList();
                var gtcb = (from pp in Parent.List
                            where pp != this
                            where pp.Checkbox != null
                            from cc in pp.Checkbox
                            where cc.HasValue()
                            select cc).ToList();
                var r = new List<string>();
                r.AddRange(_GroupTags);
                r.AddRange(gtdd);
                r.AddRange(gtcb);
                return r;
            }
        }
        public class SelectListItemFilled : SelectListItem
        {
            public bool Filled { get; set; }
        }
        public IEnumerable<SelectListItemFilled> DropdownList(Ask ask)
        {
            var q = from s in ((AskDropdown)ask).list
                    let amt = s.Fee.HasValue ? " ({0:C})".Fmt(s.Fee) : ""
                    select new SelectListItemFilled
                    {
                        Text = s.Description + amt,
                        Value = s.SmallGroup,
                        Filled = s.IsSmallGroupFilled(GroupTags),
                        Selected = s.SmallGroup == option[ask.UniqueId]
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItemFilled { Text = "(please select)", Value = "00" });
            return list;
        }
        public class FundItemChosen
        {
            public string desc { get; set; }
            public int fundid { get; set; }
            public decimal amt { get; set; }
        }
        public IEnumerable<FundItemChosen> FundItemsChosen()
        {
            if (FundItem == null)
                return new List<FundItemChosen>();
            var items = Funds();
            var q = from i in FundItem
                    join m in items on i.Key equals m.Value.ToInt()
                    where i.Value.HasValue
                    select new FundItemChosen { fundid = m.Value.ToInt(), desc = m.Text, amt = i.Value.Value };
            return q;
        }
        public IEnumerable<SelectListItem> GradeOptions(Ask ask)
        {
            var q = from s in ((AskGradeOptions)ask).list
                    select new SelectListItem { Text = s.Description, Value = s.Code.ToString() };
            var list = q.ToList();
            list.Insert(0, new SelectListItem { Text = "(please select)", Value = "00" });
            return list;
        }
        public static List<SelectListItem> ShirtSizes(CMSDataContext Db, Organization org)
        {
            var setting = new Settings(org.RegSetting, Db, org.OrganizationId);
            return ShirtSizes(setting);
        }
        private static List<SelectListItem> ShirtSizes(Settings setting)
        {
            var askSize = setting.AskItems.FirstOrDefault(aa => aa is AskSize) as AskSize;
            var q = from ss in askSize.list
                    select new SelectListItem
                    {
                        Value = ss.SmallGroup,
                        Text = ss.Description
                    };
            var list = q.ToList();
            list.Insert(0, new SelectListItem { Value = "0", Text = "(please select)" });
            if (askSize.AllowLastYear)
                list.Add(new SelectListItem { Value = "lastyear", Text = "Use shirt from last year" });
            return list;
        }
        public List<SelectListItem> ShirtSizes()
        {
            return ShirtSizes(setting);
        }
        public List<SelectListItem> MissionTripGoers()
        {
            var q = from g in DbUtil.Db.OrganizationMembers
                where g.OrganizationId == orgid
                where g.OrgMemMemTags.Any(mm => mm.MemberTag.Name == "Goer")
                select new SelectListItem()
                {
                    Value = g.PeopleId.ToString(),
                    Text = g.Person.Name
                };
            var list = q.ToList();
            list.Insert(0, new SelectListItem() {Value = "0", Text = "(please select)"});
            return list;
        }
        public void FillPriorInfo()
        {
            if (!IsNew && LoggedIn == true)
            {
                var rr = DbUtil.Db.RecRegs.SingleOrDefault(r => r.PeopleId == PeopleId);
                if (rr != null)
                {
                    if (setting.AskVisible("AskRequest"))
                    {
                        var om = GetOrgMember();
                        if (om != null)
                            request = om.Request;
                    }
                    if (setting.AskVisible("AskSize"))
                        shirtsize = rr.ShirtSize;
                    if (setting.AskVisible("AskEmContact"))
                    {
                        emcontact = rr.Emcontact;
                        emphone = rr.Emphone;
                    }
                    if (setting.AskVisible("AskInsurance"))
                    {
                        insurance = rr.Insurance;
                        policy = rr.Policy;
                    }
                    if (setting.AskVisible("AskDoctor"))
                    {
                        docphone = rr.Docphone;
                        doctor = rr.Doctor;
                    }
                    if (setting.AskVisible("AskParents"))
                    {
                        mname = rr.Mname;
                        fname = rr.Fname;
                    }
                    if (setting.AskVisible("AskAllergies"))
                        medical = rr.MedicalDescription;
                    if (setting.AskVisible("AskCoaching"))
                        coaching = rr.Coaching;
                    if (setting.AskVisible("AskChurch"))
                    {
                        otherchurch = rr.ActiveInAnotherChurch ?? false;
                        memberus = rr.Member ?? false;
                    }
                    if (setting.AskVisible("AskTylenolEtc"))
                    {
                        tylenol = rr.Tylenol;
                        advil = rr.Advil;
                        robitussin = rr.Robitussin;
                        maalox = rr.Maalox;
                    }
                }
            }
#if DEBUG2
            request = "Toby";
            ntickets = 1;
            gradeoption = "12";
            YesNoQuestion["Facebook"] = true;
            YesNoQuestion["Twitter"] = true;
            ExtraQuestion["Your Occupation"] = "programmer";
            ExtraQuestion["Your Favorite Snack"] = "peanuts";
            MenuItem["Fish"] = 1;
            MenuItem["Turkey"] = 0;
            option = "opt2";
            option2 = "none";
            paydeposit = false;
            Checkbox = new string[] { "PuttPutt", "Horseshoes" };
            shirtsize = "XL";
            emcontact = "dc";
            emphone = "br545";
            insurance = "bcbs";
            policy = "2424";
            doctor = "costalot";
            docphone = "35353365";
            tylenol = true;
            advil = true;
            maalox = false;
            robitussin = false;
            fname = "david carroll";
            coaching = false;
            paydeposit = false;
            grade = "4";
#endif
        }
        public bool NeedsCopyFromPrevious()
        {
            if (org != null)
                return (setting.AskVisible("AskEmContact")
                    || setting.AskVisible("AskInsurance")
                    || setting.AskVisible("AskDoctor")
                    || setting.AskVisible("AskParents"));
            return false;
        }
    }
}