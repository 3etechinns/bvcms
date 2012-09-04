using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CmsData;
using UtilityExtensions;
using System.Web;
using System.Web.Mvc;

namespace CmsWeb.Models
{
	public class VolunteerModel
	{
		public int OrgId { get; set; }
		public int PeopleId { get; set; }
		public bool IsLeader { get; set; }
		public bool SendEmail { get; set; }
		public DateTime[] Commit { get; set; }
		public DateTime dtlock { get; set; }

		public VolunteerModel(int orgId, int peopleId, bool leader = false)
		{
			OrgId = orgId;
			PeopleId = peopleId;
			dtlock = DateTime.Now.AddDays(Regsettings.TimeSlotLockDays ?? 0);
			IsLeader = leader;
			SendEmail = leader == false;
		}

		public VolunteerModel()
		{

		}

		private Organization _org;

		public Organization Org
		{
			get
			{
				return _org ??
					(_org = DbUtil.Db.Organizations.Single(oo => oo.OrganizationId == OrgId));
			}
		}

		private RegSettings _regsettings;

		public RegSettings Regsettings
		{
			get
			{
				return _regsettings ??
					(_regsettings = new RegSettings(Org.RegSetting, DbUtil.Db, OrgId));
			}
		}

		private Person _person;

		public Person Person
		{
			get { return _person ?? (_person = DbUtil.Db.People.Single(pp => pp.PeopleId == PeopleId)); }
		}


		public IEnumerable<List<Slot>> FetchSlotWeeks()
		{
			return from slot in FetchSlots()
				   group slot by slot.Sunday into g
				   where g.Any(gg => gg.Time > DateTime.Today)
				   orderby g.Key
				   select g.OrderBy(gg => gg.Time).ToList();
		}

		public class DateInfo
		{
			public Attend attend { get; set; }
			public DateTime MeetingDate { get; set; }
			public int count { get; set; }
			public bool iscommitted { get; set; }
		}

		public List<DateInfo> Meetings()
		{
			return (from a in DbUtil.Db.Attends
					where a.OrganizationId == OrgId
					where a.MeetingDate >= Sunday
					where a.MeetingDate <= EndDt
					where a.Registered == true
					group a by a.MeetingDate into g
					let attend = (from aa in g
								  where aa.PeopleId == PeopleId
								  select aa).SingleOrDefault()
					select new DateInfo()
					{
						attend = attend,
						MeetingDate = g.Key,
						count = g.Count(),
						iscommitted = attend != null
					}).ToList();
		}

		private DateTime? _endDt;

		public DateTime EndDt
		{
			get
			{
				if (!_endDt.HasValue)
				{
					var dt = Org.LastMeetingDate ?? DateTime.MinValue;
					if (dt == DateTime.MinValue)
						dt = DateTime.Today.AddMonths(7);
					_endDt = dt;
				}
				return _endDt.Value;
			}
		}

		private DateTime? _sunday;

		public DateTime Sunday
		{
			get
			{
				if (!_sunday.HasValue)
				{
					var dt = Org.FirstMeetingDate ?? DateTime.MinValue;
					if (dt == DateTime.MinValue || dt < DateTime.Today)
						dt = DateTime.Today;
					_sunday = dt.AddDays(-(int)dt.DayOfWeek);
				}
				return _sunday.Value;
			}
		}

		public IEnumerable<Slot> FetchSlots()
		{
			var list = new List<Slot>();
			var sunday = Sunday;
			var meetings = Meetings();
			for (; sunday <= EndDt; sunday = sunday.AddDays(7))
			{
				var dt = sunday;
				{
					var q = from ts in Regsettings.TimeSlots
							orderby ts.Datetime()
							let time = ts.Datetime(dt)
							let meeting = meetings.SingleOrDefault(cc => cc.MeetingDate == time)
							let count = meeting != null ? meeting.count : 0
							select new Slot()
									{
										AttendId = meeting != null ? (meeting.attend != null ? meeting.attend.AttendId : 0) : 0,
										Checked = meeting != null && meeting.iscommitted,
										Time = time,
										Sunday = dt,
										Month = dt.Month,
										Week = dt.WeekOfMonth(),
										Year = dt.Year,
										Full = meeting != null && meeting.count >= ts.Limit,
										Need = (ts.Limit ?? 0) - count,
										Disabled = time < DateTime.Now
									};
					list.AddRange(q);
				}
			}
			return list;
		}

		public class Slot
		{
			public int AttendId { get; set; }
			public DateTime Time { get; set; }
			public DateTime Sunday { get; set; }
			public int Year { get; set; }
			public int Month { get; set; }
			public int Week { get; set; }
			public bool Full { get; set; }
			public int? Need { get; set; }
			public bool Checked { get; set; }
			public bool Disabled { get; set; }
			public string CHECKED
			{
				get { return Checked ? "checked=\"checked\"" : ""; }
			}
			public string DISABLED
			{
				get { return Disabled ? "disabled=\"true\"" : ""; }
			}

		}

		public void UpdateCommitments()
		{
			var commitments = (from m in Meetings()
							   where m.iscommitted
							   select m.MeetingDate).ToList();

			if (Commit == null)
				Commit = new DateTime[] { };

			var decommits = from currcommit in commitments
							join newcommit in Commit on currcommit equals newcommit into j
							from newcommit in j.DefaultIfEmpty(DateTime.MinValue)
							where newcommit == DateTime.MinValue
							select currcommit;

			var commits = from newcommit in Commit
						  join currcommit in commitments on newcommit equals currcommit into j
						  from currcommit in j.DefaultIfEmpty(DateTime.MinValue)
						  where currcommit == DateTime.MinValue
						  select newcommit;

			foreach (var currcommit in decommits)
				Attend.MarkRegistered(DbUtil.Db, OrgId, PeopleId, currcommit, false);
			foreach (var newcommit in commits)
				Attend.MarkRegistered(DbUtil.Db, OrgId, PeopleId, newcommit, true);
			OrganizationMember.InsertOrgMembers(DbUtil.Db,
					OrgId, PeopleId, 220, DateTime.Now, null, false);
		}
		public string Summary(CmsController controller)
		{
			var q = from i in FetchSlots()
					where i.Checked
					select i;
			return !q.Any() ? "no commitments"
				: CmsController.RenderPartialViewToString(controller, "VolunteerSlotsSummary", q);
		}
		public string Instructions
		{
			get
			{
				var setting = OnlineRegModel.ParseSetting(Org.RegSetting, Org.OrganizationId);
				return @"
<div class=""instructions login"">{0}</div>
<div class=""instructions select"">{1}</div>
<div class=""instructions find"">{2}</div>
<div class=""instructions options"">{3}</div>
<div class=""instructions submit"">{4}</div>
<div class=""instructions sorry"">{5}</div>
".Fmt(setting.InstructionLogin,
					 setting.InstructionSelect,
					 setting.InstructionFind,
					 setting.InstructionOptions,
					 setting.InstructionSubmit,
					 setting.InstructionSorry
					 );
			}
		}
		public RegSettings setting
		{
			get { return new RegSettings(Org.RegSetting, DbUtil.Db, OrgId); }
		}
		public SelectList Volunteers()
		{
			var q = from m in DbUtil.Db.OrganizationMembers
					where m.OrganizationId == OrgId
					where m.MemberTypeId != CmsData.Codes.MemberTypeCode.InActive
					orderby m.Person.Name2
					select new { m.PeopleId, m.Person.Name };
			return new SelectList(q, "PeopleId", "Name", PeopleId);
		}
	}
}
