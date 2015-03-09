﻿using System.Collections.Generic;
using System.Linq;
using CmsData;

namespace CmsWeb.Areas.People.Models
{
    public class TasksAssignedModel : TasksModel
    {
        override public IQueryable<Task> DefineModelList()
        {
            return from t in DbUtil.Db.Tasks
                   where t.WhoId != null
                   where (t.CoOwnerId ?? t.OwnerId) == Person.PeopleId
                   select t;
        }

        public override string AddTask { get { return null; } }

        public override IEnumerable<TaskInfo> DefineViewList(IQueryable<Task> q)
        {
            return from t in q
                   select new TaskInfo
                   {
                       TaskId = t.Id,
                       CreatedDate = t.CreatedOn,
                       DueDate = t.Due,
                       About = t.AboutWho.Name,
                       AssignedTo = Person.PreferredName,
                       AboutId = t.WhoId,
                       AssignedToId = (t.CoOwnerId ?? t.OwnerId),
                       link = "/Task/List/" + t.Id + "#detail",
                       Desc = t.Description,
                       completed = t.CompletedOn
                   };
        }
    }
}