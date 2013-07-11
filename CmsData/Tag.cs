using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityExtensions;

namespace CmsData
{
    public partial class Tag
    {
        public void DeleteTag(CMSDataContext Db)
        {
            Db.TagPeople.DeleteAllOnSubmit(PersonTags);
            Db.TagShares.DeleteAllOnSubmit(TagShares);
            Db.Tags.DeleteOnSubmit(this);
        }
        public IQueryable<Person> People(CMSDataContext Db)
        {
            return Db.People.Where(p => p.Tags.Any(tp => tp.Id == Id));
        }
        public static List<int> InNamedTagForOwner(string name, int ownerPeopleid)
        {
            var t = DbUtil.Db.FetchTag(name, ownerPeopleid, DbUtil.TagTypeId_Personal);
            if (t == null)
                return new List<int>();
            return t.PersonTags.Select(tt => tt.PeopleId).ToList();
        }
    }
}
