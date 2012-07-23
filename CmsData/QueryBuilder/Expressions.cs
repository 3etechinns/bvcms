﻿/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using UtilityExtensions;
using System.Configuration;
using System.Reflection;
using System.Collections;
using System.Data.Linq.SqlClient;
using System.Text.RegularExpressions;
using System.Web;
using CmsData.Codes;

namespace CmsData
{
	internal static class Expressions
	{
		internal static Expression MemberTypeIds(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int org,
			int orgtype,
			int sched,
			int campus,
			CompareType op,
			params int[] ids)
		{
			Expression<Func<Person, bool>> pred;
			if (sched == -1)
				pred = p => (
					from m in p.OrganizationMembers
					from dg in m.Organization.DivOrgs
					from pg in dg.Division.ProgDivs
					where ids.Contains(m.MemberTypeId)
					where (campus == 0
							|| m.Organization.CampusId == campus
							|| (campus == -1 && m.Organization.CampusId == null)
							)
					where orgtype == 0 || m.Organization.OrganizationTypeId == orgtype
					where org == 0 || m.OrganizationId == org
					where divid == 0 || dg.DivId == divid
					where progid == 0 || pg.ProgId == progid
					where !m.Organization.OrgSchedules.Any()
					select m
					).Any();
			else
				pred = p => (
					from m in p.OrganizationMembers
					from dg in m.Organization.DivOrgs
					from pg in dg.Division.ProgDivs
					where ids.Contains(m.MemberTypeId)
					where (campus == 0
							|| m.Organization.CampusId == campus
							|| (campus == -1 && m.Organization.CampusId == null)
							)
					where orgtype == 0 || m.Organization.OrganizationTypeId == orgtype
					where org == 0 || m.OrganizationId == org
					where divid == 0 || dg.DivId == divid
					where progid == 0 || pg.ProgId == progid
					where sched == 0 || m.Organization.OrgSchedules.Any(os => os.ScheduleId == sched)
					select m
					).Any();
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression MembOfOrgWithSched(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int org,
			CompareType op,
			params int[] ids)
		{
			Expression<Func<Person, bool>> pred = p => (
					from m in p.OrganizationMembers
					from dg in m.Organization.DivOrgs
					from pg in dg.Division.ProgDivs
					where org == 0 || m.OrganizationId == org
					where divid == 0 || dg.DivId == divid
					where progid == 0 || pg.ProgId == progid
					where m.Organization.OrgSchedules.Any(os => ids.Contains(os.ScheduleId.Value))
					select m
					).Any();
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression MembOfOrgWithCampus(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int org,
			int orgtype,
			CompareType op,
			params int[] ids)
		{
			Expression<Func<Person, bool>> pred = p => (
				from m in p.OrganizationMembers
				from dg in m.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where orgtype == 0 || m.Organization.OrganizationTypeId == orgtype
				where org == 0 || m.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				where ids.Contains(m.Organization.CampusId ?? 0) 
					|| (ids[0] == -1 && m.Organization.CampusId == null)
				select m
				).Any();
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression WasMemberAsOf(
			ParameterExpression parm,
			DateTime? startdt,
			DateTime? enddt,
			int? progid,
			int? divid,
			int? orgid,
			int orgtype,
			CompareType op,
			bool tf)
		{
			var now = DateTime.Now;
			enddt = enddt.HasValue ? enddt.Value.AddDays(1) : startdt.Value.AddDays(1);
			Expression<Func<Person, bool>> pred = p => (
				from et in p.EnrollmentTransactions
				from dg in et.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where et.TransactionTypeId <= 3 // things that start a change
				where et.TransactionStatus == false
				where et.TransactionDate <= enddt // transaction starts <= looked for end
				where (et.Pending ?? false) == false
				where (et.NextTranChangeDate ?? now) >= startdt // transaction ends >= looked for start
				where orgtype == 0 || et.Organization.OrganizationTypeId == orgtype
				where orgid == 0 || et.OrganizationId == orgid
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select et
				).Any();
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression MemberTypeAsOf(
			ParameterExpression parm,
			DateTime? frdt,
			DateTime? todt,
			int? progid,
			int? divid,
			int? org,
			int orgtype,
			CompareType op,
			params int[] ids)
		{
			todt = todt.HasValue ? todt.Value.AddDays(1) : frdt.Value.AddDays(1);

			Expression<Func<Person, bool>> pred = p => (
				from et in p.EnrollmentTransactions
				from dg in et.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where et.TransactionTypeId <= 3 // things that start a change
				where et.TransactionStatus == false
				where frdt <= (et.NextTranChangeDate ?? Util.Now) // where it ends
				where et.TransactionDate <= todt // where it begins
				where (et.Pending ?? false) == false
				where ids.Contains(et.MemberTypeId)  // what it's type was during that time
				where orgtype == 0 || et.Organization.OrganizationTypeId == orgtype
				where org == 0 || et.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select et
				).Any();
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression AttendanceTypeAsOf(
			ParameterExpression parm,
			DateTime? from,
			DateTime? to,
			int? progid,
			int? divid,
			int? org,
			int orgtype,
			CompareType op,
			params int[] ids)
		{
			to = to.HasValue ? to.Value.AddDays(1) : from.Value.AddDays(1);
			Expression<Func<Person, bool>> pred = p => (
				from a in p.Attends
				from dg in a.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where a.MeetingDate >= @from
				where a.MeetingDate < to
				where (a.AttendanceFlag 
						|| (ids.Length == 1 && ids[0] == AttendTypeCode.Offsite))
				where ids.Contains(a.AttendanceTypeId.Value)
				where orgtype == 0 || a.Organization.OrganizationTypeId == orgtype
				where org == 0 || a.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select a
				).Any();
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression DaysBetween12Attendance(
			ParameterExpression parm, CMSDataContext Db,
			int? lookback,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			int days)
		{
			Expression<Func<Person, int>> pred = p =>
					Db.DaysBetween12Attend(p.PeopleId, progid, divid, org, lookback).Value;
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, bool>> pred2 = p => (
				from a in p.Attends
				from dg in a.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where a.MeetingDate >= mindt
				where a.AttendanceFlag
				where org == 0 || a.Meeting.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select a
				).Any();
			Expression left = Expression.Invoke(pred, parm);
			Expression attended = Expression.Invoke(pred2, parm);
			var right = Expression.Convert(Expression.Constant(days), left.Type);
			Expression cmp = Compare(left, op, right);
			return Expression.And(attended, cmp);
		}
		internal static Expression RecentJoinChurch(
			ParameterExpression parm,
			int days,
			CompareType op,
			bool tf)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, bool>> pred = p =>
				p.JoinDate > mindt;
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual
				|| op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;

		}
		internal static Expression RecentAttendType(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			int orgtype,
			int days,
			CompareType op,
			int[] ids)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, bool>> pred = p => (
				from a in p.Attends
				from dg in a.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where a.MeetingDate >= mindt
				where a.AttendanceFlag || (ids.Length == 1 && ids[0] == AttendTypeCode.Offsite)
				where ids.Contains(a.AttendanceTypeId.Value)
				where orgtype == 0 || a.Organization.OrganizationTypeId == orgtype
				where org == 0 || a.Meeting.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select a
				).Any();
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression RecentRegistrationType(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			int orgtype,
			int days,
			CompareType op,
			int[] ids)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, bool>> pred = p => (
				from m in p.OrganizationMembers
				from dg in m.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where m.EnrollmentDate >= mindt
				where ids.Contains(m.Organization.RegistrationTypeId.Value)
				where orgtype == 0 || m.Organization.OrganizationTypeId == orgtype
				where org == 0 || m.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select m
				).Any();
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression HasTaskWithName(
			ParameterExpression parm,
			CompareType op,
			string task)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.TasksAboutPerson.Any(t => t.Description.Contains(task)
					&& t.StatusId != TaskStatusCode.Complete);
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasIncompleteTask(
			ParameterExpression parm,
			CompareType op,
			string task)
		{
			var empty = !task.HasValue();
			Expression<Func<Person, bool>> pred = p => (
				from t in p.TasksCoOwned
				where empty || t.Description.Contains(task) || t.Owner.Name.Contains(task)
				where t.StatusId != TaskStatusCode.Complete
				select t).Any();
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression MadeContactTypeAsOf(
			ParameterExpression parm,
			DateTime? from,
			DateTime? to,
			int? ministryid,
			CompareType op,
			params int[] ids)
		{
			to = to.HasValue ? to.Value.AddDays(1) : from.Value.AddDays(1);

			Expression<Func<Person, bool>> pred = p => (
				from c in p.contactsMade
				where c.contact.MinistryId == ministryid || ministryid == 0
				where ids.Contains(c.contact.ContactTypeId) || ids.Length == 0 || ids[0] == 0
				where @from <= c.contact.ContactDate // where it ends
				where c.contact.ContactDate <= to // where it begins
				select c
				).Any();
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression RecentContactType(
			ParameterExpression parm,
			int days,
			CompareType op,
			int[] ids)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, bool>> pred = p =>
				p.contactsHad.Any(a => a.contact.ContactDate >= mindt
					&& ids.Contains(a.contact.ContactTypeId));
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression RecentContactMinistry(
			ParameterExpression parm,
			int days,
			CompareType op,
			int[] ids)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, bool>> pred = p =>
				p.contactsHad.Any(a => a.contact.ContactDate >= mindt
					&& ids.Contains(a.contact.MinistryId.Value));
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression MeetingId(
			ParameterExpression parm,
			CompareType op,
			int id)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Attends.Any(a =>
					(a.AttendanceFlag == true)
					&& a.MeetingId == id
					);
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression RegisteredForMeetingId(
			ParameterExpression parm,
			CompareType op,
			int id)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Attends.Any(a =>
					(a.Registered == true)
					&& a.MeetingId == id
					);
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression PeopleExtra(
			ParameterExpression parm,
			CompareType op,
			string[] values)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.PeopleExtras.Any(e =>
					values.Contains(e.FieldValue));
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasPeopleExtraField(
			ParameterExpression parm,
			CompareType op,
			string field)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.PeopleExtras.Any(e =>
					e.Field == field);
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression PeopleExtraData(
			ParameterExpression parm,
			string field,
			CompareType op,
			string value)
		{
			Expression<Func<Person, string>> pred = p =>
				p.PeopleExtras.Where(ff => ff.Field == field).Select(ff => ff.Data).SingleOrDefault();
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Constant(value, typeof(string));
			return Compare(left, op, right);
		}
		internal static Expression PeopleExtraInt(
			ParameterExpression parm,
			string field,
			CompareType op,
			int? value)
		{
			if (!value.HasValue)
				return Expressions.CompareConstant(parm, "PeopleId", CompareType.Equal, 0);

			Expression<Func<Person, int>> pred = p =>
				p.PeopleExtras.Single(e =>
					e.Field == field).IntValue ?? 0;
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(value), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression PeopleExtraDate(
			ParameterExpression parm,
			string field,
			CompareType op,
			DateTime? value)
		{
			if (op == CompareType.IsNull)
			{
				Expression<Func<Person, bool>> pred = p =>
					!p.PeopleExtras.Any(e => e.Field == field)
					|| p.PeopleExtras.SingleOrDefault(e => e.Field == field).DateValue == null;
				return Expression.Invoke(pred, parm);
			}
			else if (op == CompareType.IsNotNull)
			{
				Expression<Func<Person, bool>> pred = p =>
					p.PeopleExtras.SingleOrDefault(e => e.Field == field).DateValue != null;
				return Expression.Invoke(pred, parm);
			}
			else
			{
				if (!value.HasValue)
					return Expressions.CompareConstant(parm, "PeopleId", CompareType.Equal, 0);

				Expression<Func<Person, DateTime>> pred = p =>
					p.PeopleExtras.SingleOrDefault(e =>
						e.Field == field).DateValue.Value;
				Expression left = Expression.Invoke(pred, parm);
				var right = Expression.Convert(Expression.Constant(value), left.Type);
				return Compare(left, op, right);
			}
		}
		internal static Expression HasEmailOptout(
			ParameterExpression parm,
			CompareType op,
			string email)
		{
			Expression<Func<Person, bool>> pred = p =>
				(from oo in p.EmailOptOuts
				 where email == null || email == "" || oo.FromEmail == email
				 select oo).Any();
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression RecentAttendCount(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			int orgtype,
			int days,
			CompareType op,
			int cnt)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, int>> pred = p => (
				from a in p.Attends
				from dg in a.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where a.MeetingDate > mindt
				where a.AttendanceFlag
				where orgtype == 0 || a.Organization.OrganizationTypeId == orgtype
				where org == 0 || a.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select a
				).Count();

			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(cnt), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression RecentAttendCountAttCred(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			int? sched,
			int days,
			CompareType op,
			int cnt)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, int>> pred = p => (
				from a in p.Attends
				from dg in a.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where a.AttendanceFlag
				where a.MeetingDate >= mindt
				where sched == 0 || a.Meeting.AttendCreditId == sched
				where org == 0 || a.Meeting.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select a
				).Count();
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(cnt), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression VisitNumber(
			ParameterExpression parm, CMSDataContext Db,
			string number,
			CompareType op,
			DateTime? dt)
		{
			int n = number.ToInt2() ?? 1;

			Expression<Func<Person, bool>> pred = null;
			switch (op)
			{
				case CompareType.Greater:
					pred = p =>
					       p.Attends.Any(aa => aa.SeqNo == n && aa.MeetingDate > dt);
					break;
				case CompareType.Less:
					pred = p =>
					       p.Attends.Any(aa => aa.SeqNo == n && aa.MeetingDate < dt);
					break;
				case CompareType.GreaterEqual:
					pred = p =>
					       p.Attends.Any(aa => aa.SeqNo == n && aa.MeetingDate >= dt);
					break;
				case CompareType.LessEqual:
					pred = p =>
					       p.Attends.Any(aa => aa.SeqNo == n && aa.MeetingDate <= dt);
					break;
				case CompareType.Equal:
					pred = p =>
					       p.Attends.Any(aa => aa.SeqNo == n && aa.MeetingDate.Date == dt);
					break;
				case CompareType.NotEqual:
				case CompareType.IsNull:
					pred = p =>
					       !p.Attends.Any(aa => aa.SeqNo == n && aa.MeetingDate.Date == dt);
					break;
				case CompareType.IsNotNull:
					pred = p =>
					       p.Attends.Any(aa => aa.SeqNo == n);
					break;
			}
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression RecentVisitNumber(
			ParameterExpression parm, CMSDataContext Db,
			string number,
			int days,
			CompareType op,
			bool tf)
		{
			int n = number.ToInt2() ?? 1;
			var dt = DateTime.Today.AddDays(-days);
			Expression<Func<Person, bool>> pred = p =>
				p.Attends.Any(aa => aa.SeqNo == n && aa.MeetingDate > dt);
			Expression expr = Expression.Invoke(pred, parm);

			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression RecentNewVisitCount(
			ParameterExpression parm,
			CMSDataContext Db,
			int? progid,
			int? divid,
			int? org,
			int orgtype,
			string days0,
			int days,
			CompareType op,
			int cnt)
		{
			var dt1 = DateTime.Today.AddDays(-(days0.ToInt2() ?? 365));
			var dt2 = DateTime.Today.AddDays(-days);

			IQueryable<int> q = null;
			switch (op)
			{
				case CompareType.Greater:
					q = from p in Db.People
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								where a.AttendanceFlag
								where a.MeetingDate >= dt1
								select a
						where !g.Any(aa => aa.MeetingDate < dt2)
						where g.Count(aa => aa.MeetingDate > dt2) > cnt
						select p.PeopleId;
					break;
				case CompareType.GreaterEqual:
					q = from p in Db.People
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								where a.AttendanceFlag
								where a.MeetingDate >= dt1
								select a
						where !g.Any(aa => aa.MeetingDate < dt2)
						where g.Count(aa => aa.MeetingDate > dt2) >= cnt
						select p.PeopleId;
					break;
				case CompareType.Less:
					q = from p in Db.People
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								where a.AttendanceFlag
								where a.MeetingDate >= dt1
								select a
						where !g.Any(aa => aa.MeetingDate < dt2)
						where g.Count(aa => aa.MeetingDate > dt2) < cnt
						select p.PeopleId;
					break;
				case CompareType.LessEqual:
					q = from p in Db.People
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								where a.AttendanceFlag
								where a.MeetingDate >= dt1
								select a
						where !g.Any(aa => aa.MeetingDate < dt2)
						where g.Count(aa => aa.MeetingDate > dt2) <= cnt
						select p.PeopleId;
					break;
				case CompareType.Equal:
					q = from p in Db.People
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								where a.AttendanceFlag
								where a.MeetingDate >= dt1
								select a
						where !g.Any(aa => aa.MeetingDate < dt2)
						where g.Count(aa => aa.MeetingDate > dt2) == cnt
						select p.PeopleId;
					break;
				case CompareType.NotEqual:
					q = from p in Db.People
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								where a.AttendanceFlag
								where a.MeetingDate >= dt1
								select a
						where !g.Any(aa => aa.MeetingDate < dt2)
						where g.Count(aa => aa.MeetingDate > dt2) != cnt
						select p.PeopleId;
					break;
			}
			var tag = Db.PopulateTemporaryTag(q);
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression KidsRecentAttendCount(
			ParameterExpression parm,
			int days,
			CompareType op,
			int cnt)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, int>> pred = p =>
				p.Family.People.Where(k => k.PositionInFamilyId == 30).Max(k =>
					k.Attends.Count(a => a.AttendanceFlag == true && a.MeetingDate >= mindt));
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(cnt), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression RecentDecisionType(
			ParameterExpression parm,
			int days,
			CompareType op,
			int[] ids)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, bool>> pred = p =>
				p.DecisionDate > mindt
				&& ids.Contains(p.DecisionTypeId.Value);
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression EmailRecipient(
			ParameterExpression parm,
			CompareType op,
			int id)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.EmailQueueTos.Any(e => e.Id == id);
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression RecentEmailCount(
			ParameterExpression parm,
			int days,
			CompareType op,
			int cnt)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, int>> pred = p =>
				p.EmailQueueTos.Count(e => e.Sent >= mindt);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(cnt), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression AlwaysFalse(ParameterExpression parm)
		{
				Expression<Func<Person, bool>> pred = p => false;
				Expression expr = Expression.Invoke(pred, parm);
				return expr;
		}

		internal static Expression RecentPledgeCount(
			ParameterExpression parm, CMSDataContext Db,
			int days,
			int fund,
			CompareType op,
			int cnt)
		{
			if (!Db.CurrentUser.Roles.Any(rr => rr == "Finance"))
				return AlwaysFalse(parm);
			
			var now = DateTime.Now;
			var dt = now.AddDays(-days);
			IQueryable<int> q = null;
			switch (op)
			{
				case CompareType.Greater:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.PledgeAmount > 0
						group c by c.CreditGiverId into g
						where g.Count() > cnt
						select g.Key ?? 0;
					break;
				case CompareType.GreaterEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.PledgeAmount > 0
						group c by c.CreditGiverId into g
						where g.Count() >= cnt
						select g.Key ?? 0;
					break;
				case CompareType.Less:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.PledgeAmount > 0
						group c by c.CreditGiverId into g
						where g.Count() < cnt
						select g.Key ?? 0;
					break;
				case CompareType.LessEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.PledgeAmount > 0
						group c by c.CreditGiverId into g
						where g.Count() <= cnt
						select g.Key ?? 0;
					break;
				case CompareType.Equal:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.PledgeAmount > 0
						group c by c.CreditGiverId into g
						where g.Count() == cnt
						select g.Key ?? 0;
					break;
				case CompareType.NotEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.PledgeAmount > 0
						group c by c.CreditGiverId into g
						where g.Count() != cnt
						select g.Key ?? 0;
					break;
			}
			var tag = Db.PopulateTemporaryTag(q);
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression RecentPledgeAmount(
			ParameterExpression parm, CMSDataContext Db,
			int days,
			int fund,
			CompareType op,
			decimal amt)
		{
			if (!Db.CurrentUser.Roles.Any(rr => rr == "Finance"))
				return AlwaysFalse(parm);
			var now = DateTime.Now;
			var dt = now.AddDays(-days);
			IQueryable<int> q = null;
			switch (op)
			{
				case CompareType.Greater:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.PledgeAmount) > amt
						select g.Key ?? 0;
					break;
				case CompareType.GreaterEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.PledgeAmount) >= amt
						select g.Key ?? 0;
					break;
				case CompareType.Less:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.PledgeAmount) < amt
						select g.Key ?? 0;
					break;
				case CompareType.LessEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.PledgeAmount) <= amt
						select g.Key ?? 0;
					break;
				case CompareType.Equal:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.PledgeAmount) == amt
						select g.Key ?? 0;
					break;
				case CompareType.NotEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.PledgeAmount) != amt
						select g.Key ?? 0;
					break;
			}
			var tag = Db.PopulateTemporaryTag(q);
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression RecentContributionCount(
			ParameterExpression parm, CMSDataContext Db,
			int days,
			int fund,
			CompareType op,
			int cnt)
		{
			if (!Db.CurrentUser.Roles.Any(rr => rr == "Finance"))
				return AlwaysFalse(parm);
			var now = DateTime.Now;
			var dt = now.AddDays(-days);
			IQueryable<int> q = null;
			switch (op)
			{
				case CompareType.Greater:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Count() > cnt
						select g.Key ?? 0;
					break;
				case CompareType.GreaterEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Count() >= cnt
						select g.Key ?? 0;
					break;
				case CompareType.Less:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Count() < cnt
						select g.Key ?? 0;
					break;
				case CompareType.LessEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Count() <= cnt
						select g.Key ?? 0;
					break;
				case CompareType.Equal:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Count() == cnt
						select g.Key ?? 0;
					break;
				case CompareType.NotEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Count() != cnt
						select g.Key ?? 0;
					break;
			}
			var tag = Db.PopulateTemporaryTag(q);
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression RecentContributionAmount(
			ParameterExpression parm, CMSDataContext Db,
			int days,
			int fund,
			CompareType op,
			decimal amt)
		{
			if (!Db.CurrentUser.Roles.Any(rr => rr == "Finance"))
				return AlwaysFalse(parm);
			var now = DateTime.Now;
			var dt = now.AddDays(-days);
			IQueryable<int> q = null;
			switch (op)
			{
				case CompareType.Greater:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) > amt
						select g.Key ?? 0;
					break;
				case CompareType.GreaterEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) >= amt
						select g.Key ?? 0;
					break;
				case CompareType.Less:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) <= amt
						select g.Key ?? 0;
					break;
				case CompareType.LessEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) <= amt
						select g.Key ?? 0;
					break;
				case CompareType.Equal:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) == amt
						select g.Key ?? 0;
					break;
				case CompareType.NotEqual:
					q = from c in Db.Contributions2(dt, now, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) != amt
						select g.Key ?? 0;
					break;
			}
			var tag = Db.PopulateTemporaryTag(q);
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression ContributionAmount2(
			ParameterExpression parm, CMSDataContext Db,
			DateTime? start,
			DateTime? end,
			int fund,
			CompareType op,
			decimal amt)
		{
			if (!Db.CurrentUser.Roles.Any(rr => rr == "Finance"))
				return AlwaysFalse(parm);
			IQueryable<int> q = null;
			switch (op)
			{
				case CompareType.GreaterEqual:
					q = from c in Db.Contributions2(start, end, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) >= amt
						select g.Key ?? 0;
					break;
				case CompareType.Greater:
					q = from c in Db.Contributions2(start, end, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) > amt
						select g.Key ?? 0;
					break;
				case CompareType.LessEqual:
					q = from c in Db.Contributions2(start, end, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) <= amt
						select g.Key ?? 0;
					break;
				case CompareType.Less:
					q = from c in Db.Contributions2(start, end, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) < amt
						select g.Key ?? 0;
					break;
				case CompareType.Equal:
					q = from c in Db.Contributions2(start, end, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) == amt
						select g.Key ?? 0;
					break;
				case CompareType.NotEqual:
					q = from c in Db.Contributions2(start, end, 0, false, false, true)
						where fund == 0 || c.FundId == fund
						where c.Amount > 0
						group c by c.CreditGiverId into g
						where g.Sum(cc => cc.Amount) != amt
						select g.Key ?? 0;
					break;
			}
			var tag = Db.PopulateTemporaryTag(q);
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression ContributionChange(
			ParameterExpression parm, CMSDataContext Db,
			DateTime? dt1,
			DateTime? dt2,
			CompareType op,
			double pct)
		{
			if (!Db.CurrentUser.Roles.Any(rr => rr == "Finance"))
				return AlwaysFalse(parm);
			var q = Db.GivingCurrentPercentOfFormer(dt1, dt2, 
				op == CompareType.Greater ? ">" : 
				op == CompareType.GreaterEqual ? ">=" :
				op == CompareType.Less ? "<" :
				op == CompareType.LessEqual ? "<=" :
				op == CompareType.Equal ? "=" : "<>", pct);
			var tag = Db.PopulateTemporaryTag(q.Select(pp => pp.Pid));
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression RecentGivingAsPctOfPrevious(
			ParameterExpression parm, CMSDataContext Db,
			int Days,
			CompareType op,
			double pct)
		{
			var dt1 = DateTime.Today.AddDays(-Days * 2);
			var dt2 = DateTime.Today.AddDays(-Days);
			var q = Db.GivingCurrentPercentOfFormer(dt1, dt2, op == CompareType.Greater ? ">" : "<=", pct);
			var tag = Db.PopulateTemporaryTag(q.Select(pp => pp.Pid));
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);
			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}
		internal static Expression IsTopGiver(
			ParameterExpression parm, CMSDataContext Db,
			int days,
			string top,
			CompareType op,
			bool tf)
		{
			if (!Db.CurrentUser.Roles.Any(rr => rr == "Finance"))
				return Expressions.CompareConstant(parm, "PeopleId", CompareType.Equal, 0);

			var mindt = Util.Now.AddDays(-days).Date;
			var r = Db.TopGivers(top.ToInt(), mindt, DateTime.Now).ToList();
			var topgivers = r.Select(g => g.PeopleId).ToList();
			Expression<Func<Person, bool>> pred = p =>
				topgivers.Contains(p.PeopleId);

			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression IsTopPledger(
			ParameterExpression parm, CMSDataContext Db,
			int days,
			string top,
			CompareType op,
			bool tf)
		{
			if (!Db.CurrentUser.Roles.Any(rr => rr == "Finance"))
				return Expressions.CompareConstant(parm, "PeopleId", CompareType.Equal, 0);

			var mindt = Util.Now.AddDays(-days).Date;
			var r = Db.TopPledgers(top.ToInt(), mindt, DateTime.Now).ToList();
			var toppledgers = r.Select(g => g.PeopleId).ToList();
			Expression<Func<Person, bool>> pred = p =>
				toppledgers.Contains(p.PeopleId);

			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression NumberOfFamilyMembers(
			ParameterExpression parm,
			CompareType op,
			int cnt)
		{
			Expression<Func<Person, int>> pred = p => p.Family.People.Count();
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(cnt), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression NumberOfMemberships(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			int? sched,
			CompareType op,
			int cnt)
		{
			Expression<Func<Person, int>> pred;
			if (sched == -1)
				pred = p => (
					from m in p.OrganizationMembers
					from dg in m.Organization.DivOrgs
					from pg in dg.Division.ProgDivs
					where org == 0 || m.OrganizationId == org
					where divid == 0 || dg.DivId == divid
					where progid == 0 || pg.ProgId == progid
					where (m.Pending ?? false) == false
					where !m.Organization.OrgSchedules.Any()
					select m
					).Count();
			else
				pred = p => (
					from m in p.OrganizationMembers
					from dg in m.Organization.DivOrgs
					from pg in dg.Division.ProgDivs
					where org == 0 || m.OrganizationId == org
					where divid == 0 || dg.DivId == divid
					where progid == 0 || pg.ProgId == progid
					where (m.Pending ?? false) == false
					where sched == 0 || m.Organization.OrgSchedules.Any(os => os.ScheduleId == sched)
					select m
					).Count();
			var left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(cnt), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression VisitedCurrentOrg(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			var mindt = Util.Now.AddDays(-Db.VisitLookbackDays).Date;
			var ids = new int[] 
            { 
                AttendTypeCode.NewVisitor, 
                AttendTypeCode.RecentVisitor, 
                AttendTypeCode.VisitingMember 
            };
			Expression<Func<Person, bool>> pred = p => (
					from a in p.Attends
					where a.AttendanceFlag
					where a.MeetingDate >= mindt
					let dt = a.Organization.FirstMeetingDate
					where dt == null || a.MeetingDate >= dt
					where a.MeetingDate > Db.LastDrop(a.OrganizationId, a.PeopleId)
					where ids.Contains(a.AttendanceTypeId.Value)
					where a.OrganizationId == Db.CurrentOrgId
					select a
				).Any() && !(
					from m in p.OrganizationMembers
					where m.OrganizationId == Db.CurrentOrgId
					where (m.Pending ?? false) == false
					select m
				).Any();
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression AttendMemberTypeAsOf(CMSDataContext Db,
			ParameterExpression parm,
			DateTime? from,
			DateTime? to,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			string ids)
		{
			to = to.HasValue ? to.Value.AddDays(1) : from.Value.AddDays(1);
			Expression<Func<Person, bool>> pred = p =>
				Db.AttendMemberTypeAsOf(from, to, progid, divid, org,
					op == CompareType.NotEqual || op == CompareType.NotOneOf,
					ids).Select(a => a.PeopleId).Contains(p.PeopleId);
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression RecentAttendMemberType(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			int days,
			CompareType op,
			int[] ids)
		{
			var mindt = Util.Now.AddDays(-days).Date;
			Expression<Func<Person, bool>> pred = p => (
				from a in p.Attends
				from dg in a.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where a.AttendanceFlag
				where a.MeetingDate >= mindt
				where ids.Contains(a.MemberTypeId)
				where org == 0 || a.Meeting.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select a
				).Any();
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression OrgMemberCreatedDate(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			DateTime? date)
		{
			Expression<Func<Person, bool>> pred = null;
			switch (op)
			{
				case CompareType.Equal:
					pred = p => (
						from m in p.OrganizationMembers
						from dg in m.Organization.DivOrgs
						from pg in dg.Division.ProgDivs
						where org == 0 || m.OrganizationId == org
						where divid == 0 || dg.DivId == divid
						where pg.ProgId == progid
						where m.CreatedDate.Value.Date == date
						select m
						).Any();
					break;
				case CompareType.Greater:
					pred = p => (
						from m in p.OrganizationMembers
						from dg in m.Organization.DivOrgs
						from pg in dg.Division.ProgDivs
						where org == 0 || m.OrganizationId == org
						where divid == 0 || dg.DivId == divid
						where pg.ProgId == progid
						where m.CreatedDate.Value.Date > date
						select m
						).Any();
					break;
				case CompareType.GreaterEqual:
					pred = p => (
						from m in p.OrganizationMembers
						from dg in m.Organization.DivOrgs
						from pg in dg.Division.ProgDivs
						where org == 0 || m.OrganizationId == org
						where divid == 0 || dg.DivId == divid
						where pg.ProgId == progid
						where m.CreatedDate.Value.Date >= date
						select m
						).Any();
					break;
				case CompareType.Less:
					pred = p => (
						from m in p.OrganizationMembers
						from dg in m.Organization.DivOrgs
						from pg in dg.Division.ProgDivs
						where org == 0 || m.OrganizationId == org
						where divid == 0 || dg.DivId == divid
						where pg.ProgId == progid
						where m.CreatedDate.Value.Date < date
						select m
						).Any();
					break;
				case CompareType.LessEqual:
					pred = p => (
						from m in p.OrganizationMembers
						from dg in m.Organization.DivOrgs
						from pg in dg.Division.ProgDivs
						where org == 0 || m.OrganizationId == org
						where divid == 0 || dg.DivId == divid
						where pg.ProgId == progid
						where m.CreatedDate.Value.Date <= date
						select m
						).Any();
					break;
				case CompareType.NotEqual:
					pred = p => (
						from m in p.OrganizationMembers
						from dg in m.Organization.DivOrgs
						from pg in dg.Division.ProgDivs
						where org == 0 || m.OrganizationId == org
						where divid == 0 || dg.DivId == divid
						where pg.ProgId == progid
						where m.CreatedDate.Value.Date != date
						select m
						).Any();
					break;
				case CompareType.IsNull:
					pred = p => (
						from m in p.OrganizationMembers
						from dg in m.Organization.DivOrgs
						from pg in dg.Division.ProgDivs
						where org == 0 || m.OrganizationId == org
						where divid == 0 || dg.DivId == divid
						where pg.ProgId == progid
						where m.CreatedDate == null
						select m
						).Any();
					break;
				case CompareType.IsNotNull:
					pred = p => (
						from m in p.OrganizationMembers
						from dg in m.Organization.DivOrgs
						from pg in dg.Division.ProgDivs
						where org == 0 || m.OrganizationId == org
						where divid == 0 || dg.DivId == divid
						where pg.ProgId == progid
						where m.CreatedDate != null
						select m
						).Any();
					break;
			}
			return Expression.Invoke(pred, parm);
		}
		internal static Expression CreatedBy(
			ParameterExpression parm, CMSDataContext Db,
			CompareType op,
			string name)
		{
			Expression<Func<Person, string>> pred = p =>
				Db.People.FirstOrDefault(u => u.Users.Any(u2 => u2.UserId == p.CreatedBy)).Name2;
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Constant(name, typeof(string));
			return Compare(left, op, right);
		}
		internal static Expression SmallGroup(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			string name)
		{
			Expression<Func<Person, bool>> pred1 = p =>
					p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						);
			var expr1 = Expression.Convert(Expression.Invoke(pred1, parm), typeof(bool));
			if (name.HasValue())
			{
				Expression<Func<Person, bool>> pred = p =>
						p.OrganizationMembers.Any(m =>
							m.OrgMemMemTags.Any(mt => mt.MemberTag.Name.Contains(name))
							&& (org == 0 || m.OrganizationId == org)
							&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
							&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
							);
				var expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
				if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
					expr = Expression.Not(expr);
				return Expression.And(expr1, expr);
			}
			else
			{
				Expression<Func<Person, bool>> pred = p =>
						p.OrganizationMembers.Any(m =>
							m.OrgMemMemTags.Count() == 0
							&& (org == 0 || m.OrganizationId == org)
							&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
							&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
							);
				var expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
				if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
					expr = Expression.Not(expr);
				return Expression.And(expr1, expr);
			}
		}

		internal static Expression OrgJoinDateCompare(
		   ParameterExpression parm,
		   int? progid,
		   int? divid,
		   int? org,
		   CompareType op,
		   string prop2)
		{
			Expression<Func<Person, DateTime?>> pred = p =>
				p.OrganizationMembers.Where(m =>
					(org == 0 || m.OrganizationId == org)
					&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
					&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
					).Min(m => m.EnrollmentDate);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Property(parm, prop2);
			return Compare(left, op, right);
		}

		internal static Expression OrgJoinDate(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			DateTime? date)
		{
			Expression<Func<Person, bool>> pred = null;
			switch (op)
			{
				case CompareType.Equal:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.EnrollmentDate.Value.Date == date);
					break;
				case CompareType.Greater:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.EnrollmentDate.Value.Date > date);
					break;
				case CompareType.GreaterEqual:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.EnrollmentDate.Value.Date >= date);
					break;
				case CompareType.Less:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.EnrollmentDate.Value.Date < date);
					break;
				case CompareType.LessEqual:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.EnrollmentDate.Value.Date <= date);
					break;
				case CompareType.NotEqual:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.EnrollmentDate.Value.Date != date);
					break;
				case CompareType.IsNull:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.EnrollmentDate == null);
					break;
				case CompareType.IsNotNull:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.EnrollmentDate != null);
					break;
			}
			return Expression.Invoke(pred, parm);
		}
		internal static Expression OrgJoinDateDaysAgo(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			int days)
		{
			Expression<Func<Person, int?>> pred = p =>
				SqlMethods.DateDiffDay(p.OrganizationMembers.Where(m =>
					(org == 0 || m.OrganizationId == org)
					&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
					&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
					).Min(m => m.EnrollmentDate), Util.Now);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Constant(days, typeof(int?));
			return Compare(left, op, right);
		}
		internal static Expression RecentCreated(
			ParameterExpression parm,
			int days,
			CompareType op,
			bool tf)
		{
			var dt = DateTime.Today.AddDays(-days);
			Expression<Func<Person, bool>> pred = p => p.CreatedDate >= dt;
			Expression expr = Expression.Invoke(pred, parm);
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression Birthday(
			ParameterExpression parm,
			CompareType op,
			string dob)
		{
			Expression<Func<Person, bool>> pred = p => false; // default
			DateTime dt;
			if (DateTime.TryParse(dob, out dt))
				if (Regex.IsMatch(dob, @"\d+/\d+/\d+"))
					pred = p => p.BirthDay == dt.Day && p.BirthMonth == dt.Month && p.BirthYear == dt.Year;
				else
					pred = p => p.BirthDay == dt.Day && p.BirthMonth == dt.Month;
			else
			{
				int y;
				if (int.TryParse(dob, out y))
					if (y <= 12 && y > 0)
						pred = p => p.BirthMonth == y;
					else
						pred = p => p.BirthYear == y;
			}
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression WeddingDate(
			ParameterExpression parm,
			CompareType op,
			string wed)
		{
			Expression<Func<Person, bool>> pred = p => false; // default
			DateTime dt;
			if (DateTime.TryParse(wed, out dt))
				if (Regex.IsMatch(wed, @"\d+/\d+/\d+"))
					pred = p => p.WeddingDate == dt;
				else
					pred = p => p.WeddingDate.Value.Day == dt.Day && p.WeddingDate.Value.Month == dt.Month;
			else
			{
				int y;
				if (int.TryParse(wed, out y))
					if (y <= 12 && y > 0)
						pred = p => p.WeddingDate.Value.Month == y;
					else
						pred = p => p.WeddingDate.Value.Year == y;
			}
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression OrgInactiveDate(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			DateTime? date)
		{
			Expression<Func<Person, bool>> pred = null;
			switch (op)
			{
				case CompareType.Equal:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.InactiveDate.Value.Date == date);
					break;
				case CompareType.Greater:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.InactiveDate.Value.Date > date);
					break;
				case CompareType.GreaterEqual:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.InactiveDate.Value.Date >= date);
					break;
				case CompareType.Less:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.InactiveDate.Value.Date < date);
					break;
				case CompareType.LessEqual:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.InactiveDate.Value.Date <= date);
					break;
				case CompareType.NotEqual:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.InactiveDate.Value.Date != date);
					break;
				case CompareType.IsNull:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.InactiveDate == null);
					break;
				case CompareType.IsNotNull:
					pred = p => p.OrganizationMembers.Any(m =>
						(org == 0 || m.OrganizationId == org)
						&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
						&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
						&& m.InactiveDate != null);
					break;
			}
			return Expression.Invoke(pred, parm);
		}
		internal static Expression WidowedDate(
			ParameterExpression parm, CMSDataContext Db,
			CompareType op,
			DateTime? date)
		{
			Expression<Func<Person, DateTime?>> pred = p =>
				Db.WidowedDate(p.PeopleId);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Constant(date, typeof(DateTime?));
			return Compare(left, op, right);
		}
		internal static Expression DaysTillBirthday(
			ParameterExpression parm, CMSDataContext Db,
			CompareType op,
			int days)
		{
			Expression<Func<Person, int?>> pred = p =>
				SqlMethods.DateDiffDay(Util.Now.Date, Db.NextBirthday(p.PeopleId));
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Constant(days, typeof(int?));
			return Compare(left, op, right);
		}
		internal static Expression DaysTillAnniversary(
			ParameterExpression parm, CMSDataContext Db,
			CompareType op,
			int days)
		{
			Expression<Func<Person, int?>> pred = p =>
				SqlMethods.DateDiffDay(Util.Now.Date, Db.NextAnniversary(p.PeopleId));
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Constant(days, typeof(int?));
			return Compare(left, op, right);
		}
		internal static Expression DaysSinceContact(
			ParameterExpression parm,
			CompareType op,
			int days)
		{
			Expression<Func<Person, bool>> hadcontact = p => p.contactsHad.Count() > 0;
			var dt = Util.Now.Date;
			Expression<Func<Person, int?>> pred = p =>
				SqlMethods.DateDiffDay(p.contactsHad.Max(cc => cc.contact.ContactDate), dt);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Constant(days, typeof(int?));
			var expr1 = Expression.Invoke(hadcontact, parm);
			return Expression.And(expr1, Compare(left, op, right));
		}
		internal static Expression HasContacts(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p => p.contactsHad.Count() > 0;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression AttendPct(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			decimal pct)
		{
			Expression<Func<Person, decimal>> pred = p => (
				from om in p.OrganizationMembers
				from dg in om.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where org == 0 || om.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select om
				).Average(om => om.AttendPct).Value;
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(pct), left.Type);
			return Compare(left, op, right);
		}

		internal static Expression AttendCntHistory(
			ParameterExpression parm, CMSDataContext Db,
			int? progid,
			int? divid,
			int? org,
			DateTime? start,
			DateTime? end,
			CompareType op,
			int cnt)
		{
			if (!end.HasValue)
				end = start.Value.AddDays(1);
			Expression<Func<Person, int>> pred = p => (
				from a in p.Attends
				from dg in a.Organization.DivOrgs
				from pg in dg.Division.ProgDivs
				where a.AttendanceFlag
				where a.MeetingDate >= start
				where a.MeetingDate < end
				where org == 0 || a.Meeting.OrganizationId == org
				where divid == 0 || dg.DivId == divid
				where progid == 0 || pg.ProgId == progid
				select a
				).Count();
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(cnt), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression AttendPctHistory(
		   ParameterExpression parm, CMSDataContext Db,
		   int? progid,
		   int? divid,
		   int? org,
		   DateTime? start,
		   DateTime? end,
		   CompareType op,
		   double pct)
		{
			if (!end.HasValue)
				end = start.Value.AddDays(1);
			// note: this only works for members because visitors do not have att%
			var now = DateTime.Now;

			var q = from p in Db.People
					let m = from et in p.EnrollmentTransactions
							from dg in et.Organization.DivOrgs
							from pg in dg.Division.ProgDivs
							where et.TransactionTypeId <= 3 // things that start a change
							where et.TransactionStatus == false
							where et.TransactionDate < end // transaction starts <= looked for end
							where (et.Pending ?? false) == false
							where (et.NextTranChangeDate ?? now) >= start // transaction ends >= looked for start
							where org == 0 || et.OrganizationId == org
							where divid == 0 || dg.DivId == divid
							where progid == 0 || pg.ProgId == progid
							select et
					where m.Any()
					select p;
			IQueryable<int> q2 = null;

			switch (op)
			{
				case CompareType.Greater:
					q2 = from p in q
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where a.MeetingDate >= start
								where a.MeetingDate <= end
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								select a
						let n = g.Count(aa => aa.EffAttendFlag == true)
						let d = g.Count(aa => aa.EffAttendFlag != null)
						where (d == 0 ? 0d : n * 100.0 / d) > pct
						select p.PeopleId;
					break;
				case CompareType.GreaterEqual:
					q2 = from p in q
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where a.MeetingDate >= start
								where a.MeetingDate <= end
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								select a
						let n = g.Count(aa => aa.EffAttendFlag == true)
						let d = g.Count(aa => aa.EffAttendFlag != null)
						where (d == 0 ? 0d : n * 100.0 / d) >= pct
						select p.PeopleId;
					break;
				case CompareType.Less:
					q2 = from p in q
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where a.MeetingDate >= start
								where a.MeetingDate <= end
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								select a
						let n = g.Count(aa => aa.EffAttendFlag == true)
						let d = g.Count(aa => aa.EffAttendFlag != null)
						where (d == 0 ? 0d : n * 100.0 / d) < pct
						select p.PeopleId;
					break;
				case CompareType.LessEqual:
					q2 = from p in q
						let g = from a in p.Attends
								from dg in a.Organization.DivOrgs
								from pg in dg.Division.ProgDivs
								where a.MeetingDate >= start
								where a.MeetingDate <= end
								where org == 0 || a.OrganizationId == org
								where divid == 0 || dg.DivId == divid
								where progid == 0 || pg.ProgId == progid
								select a
						let n = g.Count(aa => aa.EffAttendFlag == true)
						let d = g.Count(aa => aa.EffAttendFlag != null)
						where (d == 0 ? 0d : n * 100.0 / d) <= pct
						select p.PeopleId;
					break;
				case CompareType.NotEqual:
				case CompareType.Equal:
					return AlwaysFalse(parm);
			}

			var tag = Db.PopulateTemporaryTag(q2);
			Expression<Func<Person, bool>> pred = p => p.Tags.Any(t => t.Id == tag.Id);

			Expression expr = Expression.Invoke(pred, parm);
			return expr;
		}

		internal static Expression IsMemberOf(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.OrganizationMembers.Any(m =>
					m.MemberTypeId != MemberTypeCode.InActive
					&& (m.Pending ?? false) == false
					&& (org == 0 || m.OrganizationId == org)
					&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
					&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
					);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression IsInactiveMemberOf(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.OrganizationMembers.Any(m =>
					m.MemberTypeId == MemberTypeCode.InActive
					&& (org == 0 || m.OrganizationId == org)
					&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
					&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
					);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression IsPendingMemberOf(
			ParameterExpression parm,
			int? progid,
			int? divid,
			int? org,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.OrganizationMembers.Any(m =>
					(m.Pending ?? false) == true
					&& (org == 0 || m.OrganizationId == org)
					&& (divid == 0 || m.Organization.DivOrgs.Any(t => t.DivId == divid))
					&& (progid == 0 || m.Organization.DivOrgs.Any(t => t.Division.ProgDivs.Any(d => d.ProgId == progid)))
					);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression InBFClass(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.OrganizationMembers.Any(m =>
					m.Organization.IsBibleFellowshipOrg == true
					&& (m.Pending ?? false) == false);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression DuplicateEmails(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.EmailAddress != null && p.EmailAddress != ""
					&& Db.People.Any(pp => pp.PeopleId != p.PeopleId && pp.EmailAddress == p.EmailAddress);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression DuplicateNames(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					Db.People.Any(pp => pp.PeopleId != p.PeopleId && pp.FirstName == p.FirstName && pp.LastName == p.LastName);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression RecActiveOtherChurch(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> hasapp = p => p.RecRegs.Count() > 0;
			Expression<Func<Person, bool>> pred = p =>
					p.RecRegs.Any(v => v.ActiveInAnotherChurch == true)
					&& p.RecRegs.Count() > 0;
			Expression expr1 = Expression.Convert(Expression.Invoke(hasapp, parm), typeof(bool));
			Expression expr2 = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr2 = Expression.Not(expr2);
			return Expression.And(expr1, expr2);
		}
		internal static Expression RecInterestedCoaching(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> hasapp = p => p.RecRegs.Count() > 0;
			Expression<Func<Person, bool>> pred = p =>
					p.RecRegs.Any(v => v.Coaching == true)
					&& p.RecRegs.Count() > 0;
			Expression expr1 = Expression.Convert(Expression.Invoke(hasapp, parm), typeof(bool));
			Expression expr2 = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr2 = Expression.Not(expr2);
			return Expression.And(expr1, expr2);
		}
		internal static Expression HasParents(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Family.People.Any(m => m.PositionInFamilyId == 10 && p.PositionInFamilyId == 30);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression FamilyHasChildren(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Family.People.Any(m => m.Age <= 12);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression FamilyHasChildrenAged(
			ParameterExpression parm,
			int age,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Family.People.Any(m => m.Age <= age);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression FamilyHasChildrenAged2(
			ParameterExpression parm,
			string range,
			CompareType op,
			bool tf)
		{
			var a = range.Split('-');
			Expression<Func<Person, bool>> pred = p =>
				p.Family.People.Any(m => m.Age >= a[0].ToInt() && m.Age <= a[1].ToInt());
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression FamilyHasChildrenAged3(
			ParameterExpression parm,
			string range,
			CompareType op,
			int[] ids)
		{
			var a = range.Split('-');
			Expression<Func<Person, bool>> pred = p =>
				p.Family.People.Any(m =>
					m.Age >= a[0].ToInt()
					&& m.Age <= a[1].ToInt()
					&& ids.Contains(m.GenderId)
				);
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasRelatedFamily(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Family.RelatedFamilies1.Count() > 0
				|| p.Family.RelatedFamilies2.Count() > 0;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression IsHeadOfHousehold(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Family.HeadOfHouseholdId == p.PeopleId;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasLowerName(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					Db.StartsLower(p.FirstName).Value
					|| Db.StartsLower(p.LastName).Value;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression IsUser(
		   ParameterExpression parm,
		   CompareType op,
		   bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.Users.Count() > 0;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasPicture(ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p => p.PictureId != null;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasCurrentTag(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.Tags.Any(t => t.Tag.Name == Db.CurrentTagName && t.Tag.PeopleId == Db.CurrentTagOwnerId);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasBalanceInCurrentOrg(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			var cg = Db.CurrentGroups.ToArray();
			Expression<Func<Person, bool>> pred = p => (
				from m in p.OrganizationMembers
				where m.OrganizationId == Db.CurrentOrgId
				where m.OrgMemMemTags.Any(mt => cg.Contains(mt.MemberTagId)) || cg[0] <= 0
				where !m.OrgMemMemTags.Any() || cg[0] != -1
				where m.MemberTypeId != MemberTypeCode.InActive
				where (m.Pending ?? false) == false
				where (from t in Db.Transactions
					   where t.OriginalTransaction.TransactionPeople.Any(pp => pp.PeopleId == p.PeopleId)
					   where t.OriginalTransaction.OrgId == Db.CurrentOrgId
					   orderby t.Id descending
					   select t.Amtdue).FirstOrDefault() > 0
				select m).Any();
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression InCurrentOrg(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			var cg = Db.CurrentGroups.ToArray();
			Expression<Func<Person, bool>> pred = p =>
					p.OrganizationMembers.Any(m =>
						m.OrganizationId == Db.CurrentOrgId
						&& (m.OrgMemMemTags.Any(mt => cg.Contains(mt.MemberTagId)) || cg[0] <= 0)
						&& (m.OrgMemMemTags.Count() == 0 || cg[0] != -1)
						&& m.MemberTypeId != MemberTypeCode.InActive
						&& (m.Pending ?? false) == false);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression InactiveCurrentOrg(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.OrganizationMembers.Any(m =>
						m.OrganizationId == Db.CurrentOrgId
						&& m.MemberTypeId == MemberTypeCode.InActive);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression PendingCurrentOrg(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.OrganizationMembers.Any(m =>
						m.OrganizationId == Db.CurrentOrgId
						&& (m.Pending ?? false) == true);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression PreviousCurrentOrg(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.EnrollmentTransactions.Any(m =>
						m.OrganizationId == Db.CurrentOrgId
						&& m.TransactionTypeId > 3
						&& m.TransactionStatus == false
						&& (m.Pending ?? false) == false)
					&& !p.OrganizationMembers.Any(m =>
						m.OrganizationId == Db.CurrentOrgId
						&& (m.Pending ?? false) == false);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression IsCurrentPerson(CMSDataContext Db,
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.PeopleId == Db.CurrentPeopleId;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasMyTag(ParameterExpression parm,
			string tag,
			CompareType op,
			bool tf)
		{
			var a = tag.Split(';').Select(s => s.Split(',')[0].ToInt()).ToArray();
			Expression<Func<Person, bool>> pred = p =>
				p.Tags.Any(t => a.Contains(t.Id));
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasVolunteered(ParameterExpression parm,
			string View,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred;
			if (View == "ns")
				pred = p => p.VolInterestInterestCodes.Count() > 0;
			else
			{
				var orgkeys = Person.OrgKeys(View);
				pred = p =>
					  p.VolInterestInterestCodes.Any(vi => orgkeys.Contains(vi.VolInterestCode.Org));
			}
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression VolunteerApprovalCode(ParameterExpression parm,
			CompareType op,
			int[] ids)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Volunteers.Any(v =>
					(!v.Standard && !v.Children && !v.Leader && ids.Contains(0))
					|| (v.Standard && ids.Contains(10))
					|| (v.Children && ids.Contains(20))
					|| (v.Leader && ids.Contains(30)))
				|| (p.Volunteers.Count() == 0 && ids.Contains(0));
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression CampusId(ParameterExpression parm,
			CompareType op,
			int[] ids)
		{
			Expression<Func<Person, bool>> pred = null;
			if (op == CompareType.IsNull)
				pred = p => p.CampusId == null;
			else if (op == CompareType.IsNotNull)
				pred = p => p.CampusId != null;
			else
				pred = p => ids.Contains(p.CampusId.Value);
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);

			return expr;
		}
		internal static Expression VolAppStatusCode(ParameterExpression parm,
			CompareType op,
			int[] ids)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Volunteers.Any(v => ids.Contains(v.StatusId.Value));
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression VolunteerProcessedDateMonthsAgo(ParameterExpression parm,
			CompareType op,
			int months)
		{
			Expression<Func<Person, int?>> pred = p =>
				SqlMethods.DateDiffMonth(p.Volunteers.Max(v => v.ProcessedDate), Util.Now);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Constant(months, typeof(int?));
			return Compare(left, op, right);
		}
		internal static Expression HaveVolunteerApplications(ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
					p.Volunteers.Any(v => v.VolunteerForms.Count() > 0);
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression HasInvalidEmailAddress(CMSDataContext Db, ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
				(Db.IsValidEmail(p.EmailAddress ?? "") == false
				|| Db.IsValidEmail(p.EmailAddress2 ?? "") == false);
					
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			if (!(op == CompareType.Equal && tf))
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression IncludeDeceased(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = null;

			bool include = ((tf && op == CompareType.Equal) || (!tf && op == CompareType.NotEqual));
			if (include)
				pred = p => p.DeceasedDate == null || p.DeceasedDate != null;
			else
				pred = p => p.DeceasedDate == null;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			return expr;
		}
		internal static Expression ParentsOf(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = null;

			bool include = ((tf && op == CompareType.Equal) || (!tf && op == CompareType.NotEqual));
			pred = p => p.PeopleId > 0;
			Expression expr = Expression.Convert(Expression.Invoke(pred, parm), typeof(bool));
			return expr;
		}
		internal static Expression UserRole(
			ParameterExpression parm,
			CompareType op,
			int[] ids)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Users.Any(u => u.UserRoles.Any(ur => ids.Contains(ur.RoleId))
					|| (u.UserRoles.Count() == 0 && ids.Contains(0))
				);
			Expression expr = Expression.Invoke(pred, parm);
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		internal static Expression FamHasPrimAdultChurchMemb(
			ParameterExpression parm,
			CompareType op,
			bool tf)
		{
			Expression<Func<Person, bool>> pred = p =>
				p.Family.People.Any(m =>
					m.PositionInFamilyId == PositionInFamily.PrimaryAdult
					&& m.MemberStatusId == 10 // church member
					//&& m.PeopleId != p.PeopleId // someone else in family
					);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(tf), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression InOneOfMyOrgs(
			ParameterExpression parm, CMSDataContext Db,
			CompareType op,
			bool tf)
		{
			var uid = Util.UserPeopleId;
			Expression<Func<Person, bool>> pred = p =>
				p.OrganizationMembers.Any(m =>
					Db.OrganizationMembers.Any(um =>
						um.OrganizationId == m.OrganizationId && um.PeopleId == uid)
				);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(tf), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression SavedQuery(ParameterExpression parm,
			CMSDataContext Db,
			string QueryIdDesc,
			CompareType op,
			bool tf)
		{
			var a = QueryIdDesc.SplitStr(":", 2);
			var qid = a[0].ToInt();
			var savedquery = Db.QueryBuilderClauses.SingleOrDefault(q =>
				q.QueryId == qid);
			var pred = savedquery.Predicate(Db);
			Expression left = Expression.Invoke(pred, parm);
			var right = Expression.Convert(Expression.Constant(tf), left.Type);
			return Compare(left, op, right);
		}
		internal static Expression SavedQueryPlus(ParameterExpression parm,
			CMSDataContext Db,
			string QueryIdDesc,
			CompareType op,
			int[] ids)
		{
			var a = QueryIdDesc.SplitStr(":", 2);
			var savedquery = Db.QueryBuilderClauses.SingleOrDefault(q =>
				q.SavedBy == a[0] && q.Description == a[1]);
			var pred = savedquery.Predicate(Db);
			Expression expr = Expression.Invoke(pred, parm); // substitute parm for p
			if (op == CompareType.NotEqual || op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}
		private static MethodInfo EnumerableContains = null;
		private static Expression CompareContains(ParameterExpression parm,
			string prop,
			CompareType op,
			object a,
			Type arrayType,
			Type itemType)
		{
			var left = Expression.Constant(a, arrayType);
			var right = Expression.Convert(Expression.Property(parm, prop), itemType);
			if (EnumerableContains == null)
				EnumerableContains = typeof(Enumerable)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.First(m => m.Name == "Contains");
			var method = EnumerableContains.MakeGenericMethod(itemType);
			Expression expr = Expression.Call(method, new Expression[] { left, right });
			if (op == CompareType.NotOneOf)
				expr = Expression.Not(expr);
			return expr;
		}

		internal static Expression CompareDateConstant(
			ParameterExpression parm,
			string prop,
			CompareType op,
			DateTime? dt)
		{
			Expression left = Expression.Property(parm, prop);
			if (dt.HasValue && dt.Value.Date == dt) // 12:00:00 AM?
			{
				left = Expression.MakeMemberAccess(left, typeof(DateTime?).GetProperty("Value"));
				left = Expression.MakeMemberAccess(left, typeof(DateTime).GetProperty("Date"));
			}
			var right = Expression.Convert(Expression.Constant(dt), typeof(DateTime));
			return Compare(left, op, right);
		}
		internal static Expression CompareConstant(
			ParameterExpression parm,
			string prop,
			CompareType op,
			object value)
		{
			if (value != null)
				if (value.GetType() == typeof(int[])) // use isarray?
					return CompareContains(parm, prop, op, value, typeof(int[]), typeof(int));
				else if (value.GetType() == typeof(string[]))
					return CompareContains(parm, prop, op, value, typeof(string[]), typeof(string));
			var left = Expression.Property(parm, prop);
			var right = Expression.Convert(Expression.Constant(value), left.Type);
			return Compare(left, op, right);
		}

		internal static Expression CompareProperty(
			ParameterExpression parm,
			string prop,
			CompareType op,
			string prop2)
		{
			var left = Expression.Property(parm, prop);
			var right = Expression.Property(parm, prop2);
			return Compare(left, op, right);
		}

		private static Expression Compare(
			Expression left,
			CompareType op,
			Expression right)
		{
			Expression expr = null;
			switch (op)
			{
				case CompareType.NotEqual:
				case CompareType.IsNotNull:
					expr = Expression.NotEqual(left, right);
					break;
				case CompareType.Equal:
				case CompareType.IsNull:
					expr = Expression.Equal(left, right);
					break;
				case CompareType.Greater:
					expr = Expression.GreaterThan(left, right);
					break;
				case CompareType.GreaterEqual:
					expr = Expression.GreaterThanOrEqual(left, right);
					break;
				case CompareType.Less:
					expr = Expression.LessThan(left, right);
					break;
				case CompareType.LessEqual:
					expr = Expression.LessThanOrEqual(left, right);
					break;
				case CompareType.DoesNotStartWith:
				case CompareType.StartsWith:
					expr = Expression.Call(left,
						typeof(string).GetMethod("StartsWith", new[] { typeof(string) }),
						new[] { right });
					break;
				case CompareType.DoesNotEndWith:
				case CompareType.EndsWith:
					expr = Expression.Call(left,
						typeof(string).GetMethod("EndsWith", new[] { typeof(string) }),
						new[] { right });
					break;
				case CompareType.DoesNotContain:
				case CompareType.Contains:
					expr = Expression.Call(left,
						typeof(string).GetMethod("Contains", new[] { typeof(string) }),
						new[] { right });
					break;
				case CompareType.StrGreater:
				case CompareType.StrGreaterEqual:
				case CompareType.StrLess:
				case CompareType.StrLessEqual:
					expr = Expression.Call(left,
						typeof(string).GetMethod("CompareTo", new[] { typeof(string) }),
						new[] { right });
					break;
			}
			switch (op)
			{
				// now reverse the logic if necessary
				case CompareType.DoesNotEndWith:
				case CompareType.DoesNotContain:
				case CompareType.DoesNotStartWith:
					expr = Expression.Not(expr);
					break;
				case CompareType.StrGreater:
					expr = Expression.GreaterThan(expr, Expression.Constant(0));
					break;
				case CompareType.StrGreaterEqual:
					expr = Expression.GreaterThanOrEqual(expr, Expression.Constant(0));
					break;
				case CompareType.StrLess:
					expr = Expression.LessThan(expr, Expression.Constant(0));
					break;
				case CompareType.StrLessEqual:
					expr = Expression.LessThanOrEqual(expr, Expression.Constant(0));
					break;
			}
			return expr;
		}
	}
}
