﻿/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Data.SqlClient;
using System.Linq;
using ImageData;
using IronPython.Modules;
using UtilityExtensions;
using System.Text;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Data.Linq.SqlClient;
using System.Web;
using CmsData.Codes;

namespace CmsData
{

    public partial class Person : ITableWithExtraValues
    {
        public static int[] DiscClassStatusCompletedCodes = new int[]
        { 
            NewMemberClassStatusCode.AdminApproval, 
            NewMemberClassStatusCode.Attended, 
            NewMemberClassStatusCode.ExemptedChild 
        };
        public static int[] DropCodesThatDrop = new int[] 
        { 
            DropTypeCode.Administrative,
            DropTypeCode.AnotherDenomination,
            DropTypeCode.LetteredOut,
            DropTypeCode.Requested,
            DropTypeCode.Other,
        };
        public DateTime Now()
        {
            return Util.Now;
        }
        /* Origins
        10		Visit						Worship or BFClass Visit
        30		Referral					see Request
        40		Request						Task, use this for Referral too
        50		Deacon Telephone			Contact, type = phoned in
        60		Survey (EE)					Contact, EE
        70		Enrollment					Member of org
        80		Membership Decision			Contact, Type=Worship Visit
        90		Contribution				-1 peopleid in Excel with Name?
        98		Other						Task, use task description
        */
        public string CityStateZip
        {
            get { return Util.FormatCSZ4(PrimaryCity, PrimaryState, PrimaryZip); }
        }
        public string CityStateZip5
        {
            get { return Util.FormatCSZ(PrimaryCity, PrimaryState, PrimaryZip); }
        }
        public string AddrCityStateZip
        {
            get { return PrimaryAddress + " " + CityStateZip; }
        }
        public string Addr2CityStateZip
        {
            get { return PrimaryAddress2 + " " + CityStateZip; }
        }
        public string FullAddress
        {
            get
            {
                var sb = new StringBuilder(PrimaryAddress + "\n");
                if (Util.HasValue(PrimaryAddress2))
                    sb.AppendLine(PrimaryAddress2);
                sb.Append(CityStateZip);
                return sb.ToString();
            }
        }
        public string SpouseName(CMSDataContext Db)
        {
            if (SpouseId.HasValue)
            {
                var q = from p in Db.People
                        where p.PeopleId == SpouseId
                        select p.Name;
                return q.SingleOrDefault();
            }
            return "";
        }
        public DateTime? BirthDate
        {
            get
            {
                DateTime dt;
                if (DateTime.TryParse(DOB, out dt))
                    return dt;
                return null;
            }
        }
        public string DOB
        {
            get
            { return Util.FormatBirthday(BirthYear, BirthMonth, BirthDay); }
            set
            {
                // reset all values before replacing b/c replacement may be partial
                BirthDay = null;
                BirthMonth = null;
                BirthYear = null;
                DateTime dt;
                if (DateTime.TryParse(value, out dt))
                {
                    BirthDay = dt.Day;
                    BirthMonth = dt.Month;
                    if (Regex.IsMatch(value, @"\d+/\d+/\d+"))
                        BirthYear = dt.Year;
                }
                else
                {
                    int n;
                    if (int.TryParse(value, out n))
                        if (n >= 1 && n <= 12)
                            BirthMonth = n;
                        else
                            BirthYear = n;
                }
            }
        }
        public DateTime? GetBirthdate()
        {
            DateTime dt;
            if (DateTime.TryParse(DOB, out dt))
                return dt;
            return null;
        }
        public int GetAge()
        {
            int years;
            var dt0 = GetBirthdate();
            if (!dt0.HasValue)
                return -1;
            var dt = dt0.Value;
            years = Util.Now.Year - dt.Year;
            if (Util.Now.Month < dt.Month || (Util.Now.Month == dt.Month && Util.Now.Day < dt.Day))
                years--;
            return years;
        }
        public void MovePersonStuff(CMSDataContext db, int targetid)
        {
            var toperson = db.People.Single(p => p.PeopleId == targetid);
            foreach (var om in this.OrganizationMembers)
            {
                var om2 = OrganizationMember.InsertOrgMembers(db, om.OrganizationId, targetid, om.MemberTypeId, om.EnrollmentDate.Value, om.InactiveDate, om.Pending ?? false);
                db.UpdateMainFellowship(om.OrganizationId);
                om2.CreatedBy = om.CreatedBy;
                om2.CreatedDate = om.CreatedDate;
                om2.AttendPct = om.AttendPct;
                om2.AttendStr = om.AttendStr;
                om2.LastAttended = om.LastAttended;
                om2.Request = om.Request;
                om2.Grade = om.Grade;
                om2.Amount = om.Amount;
                om2.TranId = om.TranId;
                om2.AmountPaid = om.AmountPaid;
                om2.PayLink = om.PayLink;
                om2.Moved = om.Moved;
                om2.InactiveDate = om.InactiveDate;
                om.Pending = om.Pending;
                om.Request = om.Request;
                om2.RegisterEmail = om.RegisterEmail;
                om2.ShirtSize = om.ShirtSize;
                om2.Tickets = om.Tickets;
                om2.UserData = om.UserData;
                db.SubmitChanges();
                foreach (var m in om.OrgMemMemTags)
                    if (!om2.OrgMemMemTags.Any(mm => mm.MemberTagId == m.MemberTagId))
                        om2.OrgMemMemTags.Add(new OrgMemMemTag { MemberTagId = m.MemberTagId });
                db.SubmitChanges();
                db.OrgMemMemTags.DeleteAllOnSubmit(om.OrgMemMemTags);
                db.SubmitChanges();
                TrySubmit(db, "Organizations (orgid:{0})".Fmt(om.OrganizationId));
            }
            db.OrganizationMembers.DeleteAllOnSubmit(this.OrganizationMembers);
            TrySubmit(db, "DeletingMemberships");

            foreach (var et in this.EnrollmentTransactions)
                et.PeopleId = targetid;
            TrySubmit(db, "EnrollmentTransactions");

            var tplist = TransactionPeople.ToList();
            if (tplist.Any())
            {
                db.TransactionPeople.DeleteAllOnSubmit(TransactionPeople);
                TrySubmit(db, "Delete TransactionPeople");

                foreach (var tp in tplist)
                    db.TransactionPeople.InsertOnSubmit(new TransactionPerson
                    {
                        OrgId = tp.OrgId, 
                        Amt = tp.Amt, 
                        Id = tp.Id, 
                        PeopleId = targetid
                    });
                TrySubmit(db, "Add TransactionPeople");
            }

            var q = from a in db.Attends
                    where a.AttendanceFlag == true
                    where a.PeopleId == this.PeopleId
                    select a;
            foreach (var a in q)
                Attend.RecordAttendance(db, targetid, a.MeetingId, true);
            db.AttendUpdateN(targetid, 10);

            foreach (var c in this.Contributions)
                c.PeopleId = targetid;
            TrySubmit(db, "Contributions");

            foreach (var u in this.Users)
                u.PeopleId = targetid;
            TrySubmit(db, "Users");

            if (this.Volunteers.Any() && !toperson.Volunteers.Any())
                foreach (var v in this.Volunteers)
                {
                    var vv = new Volunteer
                    {
                        PeopleId = targetid,
                        Children = v.Children,
                        Comments = v.Comments,
                        Leader = v.Leader,
                        ProcessedDate = v.ProcessedDate,
                        Standard = v.Standard,
                        StatusId = v.StatusId,
                    };
                    db.Volunteers.InsertOnSubmit(vv);
                }
            TrySubmit(db, "Volunteers");

            foreach (var v in this.VolunteerForms)
                v.PeopleId = targetid;
            TrySubmit(db, "VolunteerForms");

            foreach (var c in this.contactsMade)
            {
                var cp = db.Contactors.SingleOrDefault(c2 => c2.PeopleId == targetid && c.ContactId == c2.ContactId);
                if (cp == null)
                    c.contact.contactsMakers.Add(new Contactor { PeopleId = targetid });
                db.Contactors.DeleteOnSubmit(c);
            }
            TrySubmit(db, "ContactsMade");

            foreach (var c in this.contactsHad)
            {
                var cp = db.Contactees.SingleOrDefault(c2 => c2.PeopleId == targetid && c.ContactId == c2.ContactId);
                if (cp == null)
                    c.contact.contactees.Add(new Contactee { PeopleId = targetid });
                db.Contactees.DeleteOnSubmit(c);
            }
            TrySubmit(db, "ContactsHad");

            foreach (var e in this.PeopleExtras)
            {
                var field = e.Field;
            FindExisting:
                var cp = db.PeopleExtras.FirstOrDefault(c2 => c2.PeopleId == targetid && c2.Field == field);
                if (cp != null)
                {
                    field = field + "_mv";
                    goto FindExisting;
                }
                var e2 = new PeopleExtra
                             {
                                 PeopleId = targetid,
                                 Field = field,
                                 Data = e.Data,
                                 StrValue = e.StrValue,
                                 DateValue = e.DateValue,
                                 IntValue = e.IntValue,
                                 IntValue2 = e.IntValue2,
                                 TransactionTime = e.TransactionTime
                             };
                db.PeopleExtras.InsertOnSubmit(e2);
                TrySubmit(db, "ExtraValues (pid={0},field={1})".Fmt(e2.PeopleId, e2.Field));
            }
            db.PeopleExtras.DeleteAllOnSubmit(PeopleExtras);
            TrySubmit(db, "Delete ExtraValues");

            var torecreg = toperson.RecRegs.SingleOrDefault();
            var frrecreg = RecRegs.SingleOrDefault();
            if (torecreg == null && frrecreg != null)
                frrecreg.PeopleId = targetid;
            if (torecreg != null && frrecreg != null)
            {
                torecreg.Comments = frrecreg.Comments + "\n" + torecreg.Comments;
                if (Util.HasValue(frrecreg.ShirtSize))
                    torecreg.ShirtSize = frrecreg.ShirtSize;
                if (Util.HasValue(frrecreg.MedicalDescription))
                    torecreg.MedicalDescription = frrecreg.MedicalDescription;
                if (Util.HasValue(frrecreg.Doctor))
                    torecreg.Doctor = frrecreg.Doctor;
                if (Util.HasValue(frrecreg.Docphone))
                    torecreg.Docphone = frrecreg.Docphone;
                if (frrecreg.MedAllergy.HasValue)
                    torecreg.MedAllergy = frrecreg.MedAllergy;
                if (frrecreg.Tylenol.HasValue)
                    torecreg.Tylenol = frrecreg.Tylenol;
                if (frrecreg.Robitussin.HasValue)
                    torecreg.Robitussin = frrecreg.Robitussin;
                if (frrecreg.Advil.HasValue)
                    torecreg.Advil = frrecreg.Advil;
                if (frrecreg.Maalox.HasValue)
                    torecreg.Maalox = frrecreg.Maalox;
                if (Util.HasValue(frrecreg.Insurance))
                    torecreg.Insurance = frrecreg.Insurance;
                if (Util.HasValue(frrecreg.Policy))
                    torecreg.Policy = frrecreg.Policy;
                if (Util.HasValue(frrecreg.Mname))
                    torecreg.Mname = frrecreg.Mname;
                if (Util.HasValue(frrecreg.Fname))
                    torecreg.Fname = frrecreg.Fname;
                if (Util.HasValue(frrecreg.Emcontact))
                    torecreg.Emcontact = frrecreg.Emcontact;
                if (Util.HasValue(frrecreg.Emphone))
                    torecreg.Emphone = frrecreg.Emphone;
                if (frrecreg.ActiveInAnotherChurch.HasValue)
                    torecreg.ActiveInAnotherChurch = frrecreg.ActiveInAnotherChurch;
            }
            TrySubmit(db, "RegReg");

            var mg = db.ManagedGivings.FirstOrDefault(mm => mm.PeopleId == targetid);
            if (mg == null)
            {
                var v = this.ManagedGivings.FirstOrDefault();
                if (v != null)
                {
                    db.ManagedGivings.InsertOnSubmit(new ManagedGiving()
                                {
                                    Day1 = v.Day1,
                                    Day2 = v.Day2,
                                    EveryN = v.EveryN,
                                    NextDate = v.NextDate,
                                    PeopleId = targetid,
                                    Period = v.Period,
                                    SemiEvery = v.SemiEvery,
                                    StartWhen = v.StartWhen,
                                    StopAfter = v.StopAfter,
                                    StopWhen = v.StopWhen,
                                    Type = v.Type,
                                });
                    var qq = from ra in db.RecurringAmounts
                             where ra.PeopleId == PeopleId
                             select ra;
                    foreach (var ra in qq)
                        db.RecurringAmounts.InsertOnSubmit(
                            new RecurringAmount()
                                {
                                    PeopleId = targetid,
                                    Amt = ra.Amt,
                                    FundId = ra.FundId,
                                });
                }
                TrySubmit(db, "ManagedGivings");
            }

            var pi = db.PaymentInfos.FirstOrDefault(mm => mm.PeopleId == targetid);
            if (pi == null) // the target has none
                foreach (var i in PaymentInfos)
                    DbUtil.Db.PaymentInfos.InsertOnSubmit(
                        new PaymentInfo()
                            {
                                Address = i.Address,
                                AuNetCustId = i.AuNetCustId,
                                AuNetCustPayId = i.AuNetCustPayId,
                                City = i.City,
                                Expires = i.Expires,
                                FirstName = i.FirstName,
                                LastName = i.LastName,
                                MaskedAccount = i.MaskedAccount,
                                MaskedCard = i.MaskedCard,
                                MiddleInitial = i.MiddleInitial,
                                PeopleId = targetid,
                                Phone = i.Phone,
                                PreferredGivingType = i.PreferredGivingType,
                                PreferredPaymentType = i.PreferredPaymentType,
                                Routing = i.Routing,
                                SageBankGuid = i.SageBankGuid,
                                SageCardGuid = i.SageCardGuid,
                                State = i.State,
                                Suffix = i.Suffix,
                                Testing = i.Testing,
                                Zip = i.Zip,
                            });
            TrySubmit(db, "PaymentInfos");

            foreach (var bc in this.BackgroundChecks)
                bc.PeopleID = targetid;
            TrySubmit(db, "BackgroundChecks");

            foreach (var c in this.CheckInTimes)
                c.PeopleId = targetid;
            TrySubmit(db, "CheckinTimes");

            db.ExecuteCommand(@"
UPDATE dbo.GoerSupporter SET GoerId = {1} WHERE GoerId = {0};
UPDATE dbo.GoerSupporter SET SupporterId = {1} WHERE SupporterId = {0};
UPDATE dbo.GoerSenderAmounts SET GoerId = {1} WHERE GoerId = {0};
UPDATE dbo.GoerSenderAmounts SET SupporterId = {1} WHERE SupporterId = {0}", PeopleId, targetid);
        }

        private void TrySubmit(CMSDataContext db, string message)
        {
            try
            {
                db.SubmitChanges();
            }
            catch (SqlException ex)
            {
                throw new Exception("Merge Error: " + message + " \nFrom SQL: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Merge Error: " + message + " \n" + ex.Message);
            }
        }

        public bool Deceased
        {
            get { return DeceasedDate.HasValue; }
        }
        public string FromEmail
        {
            get { return Util.FullEmail(EmailAddress, Name); }
        }
        public string FromEmail2
        {
            get { return Util.FullEmail(EmailAddress2, Name); }
        }
        private static void NameSplit(string name, out string First, out string Last)
        {
            First = "";
            Last = "";
            if (!Util.HasValue(name))
                return;
            var a = name.Trim().Split(' ');
            if (a.Length > 1)
            {
                First = a[0];
                Last = a[1];
            }
            else
                Last = a[0];

        }
        public static Person Add(Family fam, int position, Tag tag, string name, string dob, bool Married, int gender, int originId, int? EntryPointId)
        {
            string First, Last;
            NameSplit(name, out First, out Last);
            if (!Util.HasValue(First) || Married)
                switch (gender)
                {
                    case 0: First = "A"; break;
                    case 1: if (!Util.HasValue(First)) First = "Husbander"; break;
                    case 2: First = "Wifey"; break;
                }
            return Add(fam, position, tag, First, null, Last, dob, Married, gender, originId, EntryPointId);
        }
        public static Person Add(Family fam,
            int position,
            Tag tag,
            string firstname,
            string nickname,
            string lastname,
            string dob,
            int MarriedCode,
            int gender,
            int originId,
            int? EntryPointId)
        {
            return Person.Add(DbUtil.Db, true, fam, position, tag, firstname, nickname, lastname, dob, MarriedCode, gender, originId, EntryPointId);
        }

        // Used for Conversions
        public static Person Add(CMSDataContext Db, Family fam, string firstname, string nickname, string lastname, DateTime? dob)
        {
            return Add(Db, false, fam, 20, null, firstname, nickname, lastname, dob.FormatDate(), 0, 0, 0, 0);
        }
        public static Person Add(CMSDataContext Db, bool SendNotices, Family fam, int position, Tag tag, string firstname, string nickname, string lastname, string dob, int MarriedCode, int gender, int originId, int? EntryPointId, bool testing = false)
        {
            var p = new Person();
            p.CreatedDate = Util.Now;
            p.CreatedBy = Util.UserId;
            Db.People.InsertOnSubmit(p);
            p.PositionInFamilyId = position;
            p.AddressTypeId = 10;

            if (Util.HasValue(firstname))
                p.FirstName = firstname.Trim().ToProper().Truncate(25);
            else
                p.FirstName = "";

            if (Util.HasValue(nickname))
                p.NickName = nickname.Trim().ToProper().Truncate(15);

            if (Util.HasValue(lastname))
                p.LastName = lastname.Trim().ToProper().Truncate(30);
            else
                p.LastName = "?";

            p.GenderId = gender;
            if (p.GenderId == 99)
                p.GenderId = 0;
            p.MaritalStatusId = MarriedCode;

            DateTime dt;
            if (Util.BirthDateValid(dob, out dt))
            {
                if (dt.Year == Util.SignalNoYear)
                {
                    p.BirthDay = dt.Day;
                    p.BirthMonth = dt.Month;
                    p.BirthYear = null;
                }
                else
                {
                    while (dt.Year < 1900)
                        dt = dt.AddYears(100);
                    if (dt > Util.Now)
                        dt = dt.AddYears(-100);
                    p.BirthDay = dt.Day;
                    p.BirthMonth = dt.Month;
                    p.BirthYear = dt.Year;
                }
                if (p.GetAge() < 18 && MarriedCode == 0)
                    p.MaritalStatusId = MaritalStatusCode.Single;
            }
                // I think this else statement is no longer necessary
            else if (DateTime.TryParse(dob, out dt))
            {
                p.BirthDay = dt.Day;
                p.BirthMonth = dt.Month;
                if (Regex.IsMatch(dob, @"\d+[-/]\d+[-/]\d+"))
                {
                    p.BirthYear = dt.Year;
                    while (p.BirthYear < 1900)
                        p.BirthYear += 100;
                    if (p.GetAge() < 18 && MarriedCode == 0)
                        p.MaritalStatusId = MaritalStatusCode.Single;
                }
            }

            p.MemberStatusId = MemberStatusCode.JustAdded;
            if (fam == null)
            {
                fam = new Family();
                Db.Families.InsertOnSubmit(fam);
                p.Family = fam;
            }
            else
                fam.People.Add(p);

            if (tag != null)
                tag.PersonTags.Add(new TagPerson { Person = p });

            p.OriginId = originId;
            p.EntryPointId = EntryPointId;
            p.FixTitle();
            if(Db.Setting("ElectronicStatementDefault", "false").Equal("true"))
                p.ElectronicStatement = true;
            if (!testing)
                Db.SubmitChanges();
            if (SendNotices)
            {
                if (Util.UserPeopleId.HasValue
                    && Util.UserPeopleId.Value != Db.NewPeopleManagerId
                    && HttpContext.Current.User.IsInRole("Access")
                    && !HttpContext.Current.User.IsInRole("OrgMembersOnly")
                    && !HttpContext.Current.User.IsInRole("OrgLeadersOnly"))
                    Task.AddNewPerson(Db, p.PeopleId);
                else
                {
                    var np = Db.GetNewPeopleManagers();
                    if(np != null)
                        Db.Email(Util.SysFromEmail, np,
                            "Just Added Person on " + Db.Host, "{0} ({1})".Fmt(p.Name, p.PeopleId));
                }
            }
            return p;
        }
        public static Person Add(Family fam, int position, Tag tag, string firstname, string nickname, string lastname, string dob, bool Married, int gender, int originId, int? EntryPointId)
        {
            return Add(fam, position, tag, firstname, nickname, lastname, dob, Married ? 20 : 10, gender, originId, EntryPointId);
        }
        public List<Duplicate> PossibleDuplicates()
        {
            var fone = Util.GetDigits(Util.PickFirst(CellPhone, HomePhone));
            using (var ctx = new CMSDataContext(Util.ConnectionString))
            {
                ctx.SetNoLock();
                string street = GetStreet(ctx) ?? "--";
                var nick = NickName ?? "--";
                var maid = MaidenName ?? "--";
                var em = EmailAddress ?? "--";
                if (!Util.HasValue(em))
                    em = "--";
                var bd = BirthDay ?? -1;
                var bm = BirthMonth ?? -1;
                var byr = BirthYear ?? -1;
                var q = from p in ctx.People
                        let firstmatch = p.FirstName == FirstName || (p.NickName ?? "") == FirstName || (p.MiddleName ?? "") == FirstName
                                    || p.FirstName == nick || (p.NickName ?? "") == nick || (p.MiddleName ?? "") == nick
                        let lastmatch = p.LastName == LastName || (p.MaidenName ?? "") == LastName
                                    || (p.MaidenName ?? "") == maid || p.LastName == maid
                        let nobday = (p.BirthMonth == null && p.BirthYear == null && p.BirthDay == null)
                                    || (BirthMonth == null && BirthYear == null && BirthDay == null)
                        let bdmatch = (p.BirthDay ?? -2) == bd && (p.BirthMonth ?? -2) == bm && (p.BirthYear ?? -2) == byr
                        let bdmatchpart = (p.BirthDay ?? -2) == bd && (p.BirthMonth ?? -2) == bm
                        let emailmatch = p.EmailAddress != null && p.EmailAddress == em
                        let addrmatch = (p.AddressLineOne ?? "").Contains(street) || (p.Family.AddressLineOne ?? "").Contains(street)
                        let phonematch = (p.CellPhoneLU == CellPhoneLU
                                            || p.CellPhoneLU == Family.HomePhoneLU
                                            || p.CellPhone == WorkPhoneLU
                                            || p.Family.HomePhoneLU == CellPhoneLU
                                            || p.Family.HomePhoneLU == Family.HomePhoneLU
                                            || p.Family.HomePhoneLU == WorkPhoneLU
                                            || p.WorkPhoneLU == CellPhoneLU
                                            || p.WorkPhoneLU == Family.HomePhoneLU
                                            || p.WorkPhoneLU == WorkPhoneLU)
                        let samefamily = p.FamilyId == FamilyId && p.PeopleId != PeopleId
                        let nmatches = samefamily ? 0 :
                                        (firstmatch ? 1 : 0)
                                        + (bdmatch ? 1 : 0)
                                        + (emailmatch ? 1 : 0)
                                        + (phonematch ? 1 : 0)
                                        + (addrmatch ? 1 : 0)
                        where (lastmatch && nmatches >= 3)
                                || ((firstmatch && lastmatch && bdmatchpart))
                        where p.PeopleId != PeopleId
                        select new Duplicate
                                                {
                                                    PeopleId = p.PeopleId,
                                                    First = p.FirstName,
                                                    Last = p.LastName,
                                                    Nick = p.NickName,
                                                    Middle = p.MiddleName,
                                                    BMon = p.BirthMonth,
                                                    BDay = p.BirthDay,
                                                    BYear = p.BirthYear,
                                                    Email = p.EmailAddress,
                                                    FamAddr = p.Family.AddressLineOne,
                                                    PerAddr = p.AddressLineOne,
                                                    Member = p.MemberStatus.Description
                                                };
                var list = q.ToList();
                return list;
            }
        }
        public class Duplicate
        {
            public bool s0 { get; set; }
            public bool s1 { get; set; }
            public bool s2 { get; set; }
            public bool s3 { get; set; }
            public bool s4 { get; set; }
            public bool s5 { get; set; }
            public bool s6 { get; set; }
            public int PeopleId { get; set; }
            public string First { get; set; }
            public string Last { get; set; }
            public string Nick { get; set; }
            public string Middle { get; set; }
            public string Maiden { get; set; }
            public int? BMon { get; set; }
            public int? BDay { get; set; }
            public int? BYear { get; set; }
            public string Email { get; set; }
            public string FamAddr { get; set; }
            public string PerAddr { get; set; }
            public string Member { get; set; }
        }
        public List<Duplicate> PossibleDuplicates2()
        {
            using (var ctx = new CMSDataContext(Util.ConnectionString))
            {
                ctx.SetNoLock();
                string street = GetStreet(ctx) ?? "--";
                var nick = NickName ?? "--";
                var maid = MaidenName ?? "--";
                var em = EmailAddress ?? "--";
                if (!Util.HasValue(em))
                    em = "--";
                var bd = BirthDay ?? -1;
                var bm = BirthMonth ?? -1;
                var byr = BirthYear ?? -1;
                var q = from p in ctx.People
                        where p.PeopleId != PeopleId
                        let firstmatch = p.FirstName == FirstName || (p.NickName ?? "") == FirstName || (p.MiddleName ?? "") == FirstName
                                    || p.FirstName == nick || (p.NickName ?? "") == nick || (p.MiddleName ?? "") == nick
                        let lastmatch = p.LastName == LastName || (p.MaidenName ?? "") == LastName
                                    || (p.MaidenName ?? "") == maid || p.LastName == maid
                        let nobday = (p.BirthMonth == null && p.BirthYear == null && p.BirthDay == null)
                                    || (BirthMonth == null && BirthYear == null && BirthDay == null)
                        let bdmatch = (p.BirthDay ?? -2) == bd && (p.BirthMonth ?? -2) == bm && (p.BirthYear ?? -2) == byr
                        let bdmatchpart = (p.BirthDay ?? -2) == bd && (p.BirthMonth ?? -2) == bm
                        let emailmatch = p.EmailAddress != null && p.EmailAddress == em
                        let addrmatch = (p.AddressLineOne ?? "").Contains(street) || (p.Family.AddressLineOne ?? "").Contains(street)
                        let s1 = firstmatch && bdmatchpart
                        let s2 = firstmatch && bdmatch
                        let s3 = firstmatch && lastmatch && nobday
                        let s4 = firstmatch && addrmatch
                        let s5 = firstmatch && emailmatch
                        let s6 = lastmatch && bdmatch
                        where s1 || s2 || s3 || s4 || s5 || s6
                        select new Duplicate
                        {
                            s1 = s1,
                            s2 = s2,
                            s3 = s3,
                            s4 = s4,
                            s5 = s5,
                            s6 = s6,
                            PeopleId = p.PeopleId,
                            First = p.FirstName,
                            Last = p.LastName,
                            Nick = p.NickName,
                            Middle = p.MiddleName,
                            BMon = p.BirthMonth,
                            BDay = p.BirthDay,
                            BYear = p.BirthYear,
                            Email = p.EmailAddress,
                            FamAddr = p.Family.AddressLineOne,
                            PerAddr = p.AddressLineOne,
                            Member = p.MemberStatus.Description
                        };
                try
                {
                    var list = q.ToList();
                    var t = new Duplicate
                    {
                        s0 = true,
                        PeopleId = PeopleId,
                        First = FirstName,
                        Last = LastName,
                        Nick = NickName,
                        Middle = MiddleName,
                        BMon = BirthMonth,
                        BDay = BirthDay,
                        BYear = BirthYear,
                        Email = EmailAddress,
                        FamAddr = Family.AddressLineOne,
                        PerAddr = AddressLineOne,
                        Member = MemberStatus.Description
                    };
                    list.Insert(0, t);

                    return list;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        private string GetStreet(CMSDataContext db)
        {
            if (!Util.HasValue(PrimaryAddress))
                return null;
            try
            {
                var s = PrimaryAddress.Replace(".", "");
                var a = s.SplitStr(" ");
                var la = a.ToList();
                if (la[0].AllDigits())
                    la.RemoveAt(0);
                var quadrants = new string[] { "N", "NORTH", "S", "SOUTH", "E", "EAST", "W", "WEST", "NE", "NORTHEAST", "NW", "NORTHWEST", "SE", "SOUTHEAST", "SW", "SOUTHWEST" };
                if (quadrants.Contains(a[0].ToUpper()))
                    la.RemoveAt(0);
                la.Reverse();
                if (la[0].AllDigits())
                    la.RemoveAt(0);
                if (la[0].StartsWith("#"))
                    la.RemoveAt(0);
                var apt = new string[] { "APARTMENT", "APT", "BUILDING", "BLDG", "DEPARTMENT", "DEPT", "FLOOR", "FL", "HANGAR", "HNGR", "LOT", "LOT", "PIER", "PIER", "ROOM", "RM", "SLIP", "SLIP", "SPACE", "SPC", "STOP", "STOP", "SUITE", "STE", "TRAILER", "TRLR", "UNIT", "UNIT", "UPPER", "UPPR",
        	                    "BASEMENT","BSMT", "FRONT","FRNT", "LOBBY","LBBY", "LOWER","LOWR", "OFFICE","OFC", "PENTHOUSE","PH", "REAR", "SIDE" };
                if (apt.Contains(la[0].ToUpper()))
                    la.RemoveAt(0);
                if (db.StreetTypes.Any(t => t.Type == la[0]))
                    la.RemoveAt(0);
                la.Reverse();
                var street = string.Join(" ", la);
                return street;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public void FixTitle()
        {
            if (Util.HasValue(TitleCode))
                return;
            TitleCode = ComputeTitle();
        }
        public string ComputeTitle()
        {
            if (GenderId == 1)
                return "Mr.";
            if (GenderId == 2)
                if (MaritalStatusId == 20 || MaritalStatusId == 50)
                    return "Mrs.";
                else
                    return "Ms.";
            return null;
        }
        public string OptOutKey(string FromEmail)
        {
            return Util.EncryptForUrl("{0}|{1}".Fmt(PeopleId, FromEmail));
        }

        public static bool ToggleTag(int PeopleId, string TagName, int? OwnerId, int TagTypeId)
        {
            var Db = DbUtil.Db;
            var tag = Db.FetchOrCreateTag(TagName, OwnerId, TagTypeId);
            if (tag == null)
                throw new Exception("ToggleTag, tag '{0}' not found");
            var tp = Db.TagPeople.SingleOrDefault(t => t.Id == tag.Id && t.PeopleId == PeopleId);
            if (tp == null)
            {
                tag.PersonTags.Add(new TagPerson { PeopleId = PeopleId });
                return true;
            }
            Db.TagPeople.DeleteOnSubmit(tp);
            return false;
        }
        public static void Tag(CMSDataContext db, int PeopleId, string TagName, int? OwnerId, int TagTypeId)
        {
            var tag = db.FetchOrCreateTag(TagName, OwnerId, TagTypeId);
            var tp = db.TagPeople.SingleOrDefault(t => t.Id == tag.Id && t.PeopleId == PeopleId);
            var isperson = db.People.Count(p => p.PeopleId == PeopleId) > 0;
            if (tp == null && isperson)
                tag.PersonTags.Add(new TagPerson { PeopleId = PeopleId });
        }
        public static void UnTag(CMSDataContext db, int PeopleId, string TagName, int? OwnerId, int TagTypeId)
        {
            var tag = db.FetchOrCreateTag(TagName, OwnerId, TagTypeId);
            var tp = db.TagPeople.SingleOrDefault(t => t.Id == tag.Id && t.PeopleId == PeopleId);
            if (tp != null)
                db.TagPeople.DeleteOnSubmit(tp);
        }
        partial void OnNickNameChanged()
        {
            if (NickName != null && NickName.Trim() == String.Empty)
                NickName = null;
        }
        private bool _DecisionTypeIdChanged;
        public bool DecisionTypeIdChanged
        {
            get { return _DecisionTypeIdChanged; }
        }
        partial void OnDecisionTypeIdChanged()
        {
            _DecisionTypeIdChanged = true;
        }
        private bool _NewMemberClassStatusIdChanged;
        public bool NewMemberClassStatusIdChanged
        {
            get { return _NewMemberClassStatusIdChanged; }
        }
        partial void OnNewMemberClassStatusIdChanged()
        {
            _NewMemberClassStatusIdChanged = true;
        }
        private bool _BaptismStatusIdChanged;
        public bool BaptismStatusIdChanged
        {
            get { return _BaptismStatusIdChanged; }
        }
        partial void OnBaptismStatusIdChanged()
        {
            _BaptismStatusIdChanged = true;
        }
        private bool _DeceasedDateChanged;
        public bool DeceasedDateChanged
        {
            get { return _DeceasedDateChanged; }
        }
        partial void OnDeceasedDateChanged()
        {
            _DeceasedDateChanged = true;
        }
        private bool _DropCodeIdChanged;
        public bool DropCodeIdChanged
        {
            get { return _DropCodeIdChanged; }
        }
        partial void OnDropCodeIdChanged()
        {
            _DropCodeIdChanged = true;
        }
        //internal static int FindResCode(string zipcode)
        //{
        //    if (zipcode.HasValue() && zipcode.Length >= 5)
        //    {
        //        var z5 = zipcode.Substring(0, 5);
        //        var z = DbUtil.Db.Zips.SingleOrDefault(zip => z5 == zip.ZipCode);
        //        if (z == null)
        //            return 30;
        //        return z.MetroMarginalCode ?? 30;
        //    }
        //    return 30;
        //}
        private bool? canUserEditAll;
        public bool CanUserEditAll
        {
            get
            {
                if (!canUserEditAll.HasValue)
                    canUserEditAll = HttpContext.Current.User.IsInRole("Edit");
                return canUserEditAll.Value;
            }
        }
        private bool? canUserEditFamilyAddress;
        public bool CanUserEditFamilyAddress
        {
            get
            {
                if (!canUserEditFamilyAddress.HasValue)
                    canUserEditFamilyAddress = CanUserEditAll
                        || Util.UserPeopleId == Family.HeadOfHouseholdId
                        || Util.UserPeopleId == Family.HeadOfHouseholdSpouseId;
                return canUserEditFamilyAddress.Value;
            }
        }
        private bool? canUserEditBasic;
        public bool CanUserEditBasic
        {
            get
            {
                if (!canUserEditBasic.HasValue)
                    canUserEditBasic = CanUserEditFamilyAddress
                        || Util.UserPeopleId == PeopleId;
                return canUserEditBasic.Value;
            }
        }
        private bool? canUserSee;
        public bool CanUserSee
        {
            get
            {
                if (!canUserSee.HasValue)
                    canUserSee = CanUserEditBasic
                        || Family.People.Any(m => m.PeopleId == Util.UserPeopleId);
                return canUserSee.Value;
            }
        }
        private bool? canUserSeeGiving;
        public bool CanUserSeeGiving
        {
            get
            {
                if (!canUserSeeGiving.HasValue)
                {
                    var sameperson = Util.UserPeopleId == PeopleId;
                    var infinance = HttpContext.Current.User.IsInRole("Finance")
                                    && ((string) HttpContext.Current.Session["testnofinance"]) != "true";
                    var ishead = (new int?[] {
                        Family.HeadOfHouseholdId,
                        Family.HeadOfHouseholdSpouseId } )
                        .Contains(Util.UserPeopleId);
                    canUserSeeGiving = sameperson || infinance || ishead;
                }
                return canUserSeeGiving.Value;
            }
        }

        public RecReg GetRecReg()
        {
            var rr = RecRegs.SingleOrDefault();
            if (rr == null)
                return new RecReg();
            return rr;
        }
        public RecReg SetRecReg()
        {
            var rr = RecRegs.SingleOrDefault();
            if (rr == null)
            {
                rr = new RecReg();
                RecRegs.Add(rr);
            }
            return rr;
        }
        private List<ChangeDetail> psbDefault;
        public void UpdateValue(string field, object value)
        {
            if (psbDefault == null)
                psbDefault = new List<ChangeDetail>();
            this.UpdateValue(psbDefault, field, value);
        }
        public void UpdateValueFromText(string field, string value)
        {
            if (psbDefault == null)
                psbDefault = new List<ChangeDetail>();
            this.UpdateValueFromText(psbDefault, field, value);
        }

        public void UpdateValueFromText(List<ChangeDetail> psb, string field, string value)
        {
            value = value.TrimEnd();
            var o = Util.GetProperty(this, field);
            if (o is string)
                o = ((string)o).TrimEnd();
            if (o == null && value == null)
                return;
            if (o is int)
                if ((int)o == value.ToInt())
                    return;
            var i = o as int?;
            if (i != null)
                if (i == value.ToInt2())
                    return;
            if (o != null && o.Equals(value))
                return;
            if (o == null && value is string && !Util.HasValue(((string)value)))
                return;
            if (value == null && o is string && !Util.HasValue(((string)o)))
                return;
            //psb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>\n", field, o, value ?? "(null)");
            psb.Add(new ChangeDetail(field, o, value));
            Util.SetPropertyFromText(this, field, value);
        }

        public void LogChanges(CMSDataContext Db)
        {
            if (psbDefault != null)
                LogChanges(Db, psbDefault, Util.UserPeopleId ?? 0);
        }

        public void LogChanges(CMSDataContext Db, int UserPeopleId)
        {
            if (psbDefault != null)
                LogChanges(Db, psbDefault, UserPeopleId);
        }

        public void LogChanges(CMSDataContext Db, List<ChangeDetail> changes)
        {
            LogChanges(Db, changes, Util.UserPeopleId ?? 0);
        }

        public void LogChanges(CMSDataContext Db, List<ChangeDetail> changes, int UserPeopleId)
        {
            if (changes.Count > 0)
            {
                var c = new ChangeLog
                {
                    UserPeopleId = UserPeopleId,
                    PeopleId = PeopleId,
                    Field = "Basic Info",
                    Created = Util.Now
                };
                Db.ChangeLogs.InsertOnSubmit(c);
                c.ChangeDetails.AddRange(changes);
            }
        }
        public void LogPictureUpload(CMSDataContext Db, int UserPeopleId)
        {
            var c = new ChangeLog
            {
                UserPeopleId = UserPeopleId,
                PeopleId = PeopleId,
                Field = "Basic Info",
                Created = Util.Now
            };
            Db.ChangeLogs.InsertOnSubmit(c);
            c.ChangeDetails.Add(new ChangeDetail("Picture", null, "(new upload)"));
        }
        public override string ToString()
        {
            return Name + "(" + PeopleId + ")";
        }
        public void SetExtra(string field, string value)
        {
            var e = PeopleExtras.FirstOrDefault(ee => ee.Field == field);
            if (e == null)
            {
                e = new PeopleExtra { Field = field, PeopleId = PeopleId, TransactionTime = DateTime.Now };
                this.PeopleExtras.Add(e);
            }
            e.StrValue = value;
        }
        public string GetExtra(string field)
        {
            var e = PeopleExtras.SingleOrDefault(ee => ee.Field == field);
            if (e == null)
                return "";
            if (Util.HasValue(e.StrValue))
                return e.StrValue;
            if (Util.HasValue(e.Data))
                return e.Data;
            if (e.DateValue.HasValue)
                return e.DateValue.FormatDate();
            if (e.IntValue.HasValue)
                return e.IntValue.ToString();
            return e.BitValue.ToString();
        }
        public PeopleExtra GetExtraValue(string field)
        {
            if (!Util.HasValue(field))
                field = "blank";
            field = field.Replace(",", "_");
            var ev = PeopleExtras.AsEnumerable().FirstOrDefault(ee => string.Compare(ee.Field, field, ignoreCase: true) == 0);
            if (ev == null)
            {
                ev = new PeopleExtra
                {
                    Field = field,
                    TransactionTime = DateTime.Now
                };
                PeopleExtras.Add(ev);
            }
            return ev;
        }
        public void RemoveExtraValue(CMSDataContext Db, string field)
        {
            var ev = (from ee in Db.PeopleExtras
                      where ee.Field == field
                      where ee.PeopleId == PeopleId
                      select ee).FirstOrDefault();
            if (ev != null)
                Db.PeopleExtras.DeleteOnSubmit(ev);
        }
        public void AddEditExtraValue(string field, string value)
        {
            if (!Util.HasValue(field))
                return;
            if (!Util.HasValue(value))
                return;
            var ev = GetExtraValue(field);
            ev.StrValue = value;
            ev.TransactionTime = DateTime.Now;
        }
        public void AddEditExtraDate(string field, DateTime? value)
        {
            if (!value.HasValue)
                return;
            var ev = GetExtraValue(field);
            ev.DateValue = value;
            ev.TransactionTime = DateTime.Now;
        }
        public void AddEditExtraData(string field, string value)
        {
            if (!Util.HasValue(value))
                return;
            var ev = GetExtraValue(field);
            ev.Data = value;
            ev.TransactionTime = DateTime.Now;
        }
        public void AddToExtraData(string field, string value)
        {
            if (!Util.HasValue(value))
                return;
            var ev = GetExtraValue(field);
            if (Util.HasValue(ev.Data))
                ev.Data = value + "\n" + ev.Data;
            else
                ev.Data = value;
            ev.TransactionTime = DateTime.Now;
        }
        public void AddEditExtraInt(string field, int value)
        {
            var ev = GetExtraValue(field);
            ev.IntValue = value;
            ev.TransactionTime = DateTime.Now;
        }
        public void AddEditExtraBool(string field, bool tf)
        {
            if (!Util.HasValue(field))
                return;
            var ev = GetExtraValue(field);
            ev.BitValue = tf;
            ev.TransactionTime = DateTime.Now;
        }
        public void AddEditExtraInts(string field, int value, int value2)
        {
            var ev = GetExtraValue(field);
            ev.IntValue = value;
            ev.IntValue2 = value2;
            ev.TransactionTime = DateTime.Now;
        }
        public static PeopleExtra GetExtraValue(CMSDataContext db, int id, string field)
        {
            field = field.Replace('/', '-');
            var q = from v in db.PeopleExtras
                    where v.Field == field
                    where v.PeopleId == id
                    select v;
            var ev = q.SingleOrDefault();
            if (ev == null)
            {
                ev = new PeopleExtra
                {
                    PeopleId = id,
                    Field = field,
                    TransactionTime = DateTime.Now
                };
                db.PeopleExtras.InsertOnSubmit(ev);
            }
            return ev;
        }
        public static bool ExtraValueExists(CMSDataContext db, int id, string field)
        {
            field = field.Replace('/', '-');
            var q = from v in db.PeopleExtras
                    where v.Field == field
                    where v.PeopleId == id
                    select v;
            var ev = q.SingleOrDefault();
            return ev != null;
        }
        public static PeopleExtra GetExtraValue(CMSDataContext db, int id, string field, string value)
        {
            var novalue = !Util.HasValue(value);
            var q = from v in db.PeopleExtras
                    where v.PeopleId == id
                    where v.Field == field
                    where novalue || v.StrValue == value
                    select v;
            var ev = q.SingleOrDefault();
            return ev;
        }
        public static void AddEditExtraValue(CMSDataContext db, int id, string field, string value)
        {
            if (!Util.HasValue(value))
                return;
            var ev = GetExtraValue(db, id, field);
            ev.StrValue = value;
            ev.TransactionTime = DateTime.Now;
        }
        public static void AddEditExtraData(CMSDataContext db, int id, string field, string value)
        {
            if (!Util.HasValue(value))
                return;
            var ev = GetExtraValue(db, id, field);
            ev.Data = value;
            ev.TransactionTime = DateTime.Now;
        }
        public static void AddEditExtraDate(CMSDataContext db, int id, string field, DateTime? value)
        {
            if (!value.HasValue)
                return;
            var ev = GetExtraValue(db, id, field);
            ev.DateValue = value;
            ev.TransactionTime = DateTime.Now;
        }
        public static void AddEditExtraInt(CMSDataContext db, int id, string field, int? value)
        {
            if (!value.HasValue)
                return;
            var ev = GetExtraValue(db, id, field);
            ev.IntValue = value;
            ev.TransactionTime = DateTime.Now;
        }
        public static void AddEditExtraBool(CMSDataContext db, int id, string field, bool? value)
        {
            if (!value.HasValue)
                return;
            var ev = GetExtraValue(db, id, field);
            ev.BitValue = value;
            ev.TransactionTime = DateTime.Now;
        }
        public ManagedGiving ManagedGiving()
        {
            var mg = ManagedGivings.SingleOrDefault();
            return mg;
        }
        public PaymentInfo PaymentInfo()
        {
            var pi = PaymentInfos.SingleOrDefault();
            return pi;
        }
        public Contribution PostUnattendedContribution(CMSDataContext Db, decimal Amt, int? Fund, string Description, bool pledge = false, int? typecode = null, int? tranid = null)
        {
            if (!typecode.HasValue)
            {
                typecode = BundleTypeCode.Online;
                if (pledge)
                    typecode = BundleTypeCode.OnlinePledge;
            }

            var now = Util.Now;
            var d = now.Date;
            BundleHeader bundle = null;

            var spec = Db.Setting("OnlineContributionBundleDayTime", "");
            if (Util.HasValue(spec))
            {
                var a = spec.SplitStr(" ", 2);
                try
                {
                    var next = DateTime.Parse(now.ToShortDateString() + " " + a[1]);
                    var dow = Enum.Parse(typeof(DayOfWeek), a[0], ignoreCase: true);
                    next = next.Sunday().Add(next.TimeOfDay).AddDays(dow.ToInt());
                    if(now > next)
                    	next = next.AddDays(7);
                    var prev = next.AddDays(-7);
                    var bid = BundleTypeCode.MissionTrip == typecode
                        ? Db.GetCurrentMissionTripBundle(next, prev)
                        : Db.GetCurrentOnlineBundle(next, prev);
                    bundle = Db.BundleHeaders.SingleOrDefault(bb => bb.BundleHeaderId == bid);
                }
                catch (Exception)
                {
                    spec = "";
                }
            }
            if(!Util.HasValue(spec))
            {
                var nextd = d.AddDays(1);
                var bid = BundleTypeCode.MissionTrip == typecode
                    ? Db.GetCurrentMissionTripBundle(nextd, d)
                    : Db.GetCurrentOnlineBundle(nextd, d);
                bundle = Db.BundleHeaders.SingleOrDefault(bb => bb.BundleHeaderId == bid);
            }
            if (bundle == null)
            {
                bundle = new BundleHeader
                {
                    BundleHeaderTypeId = typecode.Value,
                    BundleStatusId = BundleStatusCode.Open,
                    CreatedBy = Util.UserId1,
                    ContributionDate = d,
                    CreatedDate = now,
                    FundId = Db.Setting("DefaultFundId", "1").ToInt(),
                    RecordStatus = false,
                    TotalCash = 0,
                    TotalChecks = 0,
                    TotalEnvelopes = 0,
                    BundleTotal = 0
                };
                Db.BundleHeaders.InsertOnSubmit(bundle);
            }
            if (!Fund.HasValue)
                Fund = (from f in Db.ContributionFunds
                        where f.FundStatusId == 1
                        orderby f.FundId
                        select f.FundId).First();

            var FinanceManagerId = Db.Setting("FinanceManagerId", "").ToInt2();
            if (!FinanceManagerId.HasValue)
            {
                var qu = from u in Db.Users
                         where u.UserRoles.Any(ur => ur.Role.RoleName == "Finance")
                         orderby u.Person.LastName
                         select u.UserId;
                FinanceManagerId = qu.FirstOrDefault();
                if (!FinanceManagerId.HasValue)
                    FinanceManagerId = 1;
            }
            var bd = new CmsData.BundleDetail
            {
                BundleHeaderId = bundle.BundleHeaderId,
                CreatedBy = FinanceManagerId.Value,
                CreatedDate = now,
            };
            var typid = ContributionTypeCode.CheckCash;
            if (pledge)
                typid = ContributionTypeCode.Pledge;
            bd.Contribution = new Contribution
            {
                CreatedBy = FinanceManagerId.Value,
                CreatedDate = bd.CreatedDate,
                FundId = Fund.Value,
                PeopleId = PeopleId,
                ContributionDate = bd.CreatedDate,
                ContributionAmount = Amt,
                ContributionStatusId = 0,
                ContributionTypeId = typid,
                ContributionDesc = Description,
                TranId = tranid,
                Source = Util2.FromMobile.HasValue() ? 1 : (int?)null
            };
            bundle.BundleDetails.Add(bd);
            Db.SubmitChanges();
            return bd.Contribution;
        }
        public static int FetchOrCreateMemberStatus(CMSDataContext Db, string type)
        {
            var ms = Db.MemberStatuses.SingleOrDefault(m => m.Description == type);
            if (ms == null)
            {
                var max = Db.MemberStatuses.Max(mm => mm.Id) + 1;
                ms = new MemberStatus() { Id = max, Code = "M" + max, Description = type };
                Db.MemberStatuses.InsertOnSubmit(ms);
                Db.SubmitChanges();
            }
            return ms.Id;
        }
        public static int FetchOrCreateJoinType(CMSDataContext Db, string status)
        {
            var ms = Db.JoinTypes.SingleOrDefault(m => m.Description == status);
            if (ms == null)
            {
                var max = Db.JoinTypes.Max(mm => mm.Id) + 1;
                ms = new JoinType() { Id = max, Code = "J" + max, Description = status };
                Db.JoinTypes.InsertOnSubmit(ms);
                Db.SubmitChanges();
            }
            return ms.Id;
        }
        public static int FetchOrCreateBaptismType(CMSDataContext Db, string type)
        {
            var bt = Db.BaptismTypes.SingleOrDefault(m => m.Description == type);
            if (bt == null)
            {
                var max = Db.BaptismTypes.Max(mm => mm.Id) + 10;
                bt = new BaptismType() { Id = max, Code = "b" + max, Description = type };
                Db.BaptismTypes.InsertOnSubmit(bt);
                Db.SubmitChanges();
            }
            return bt.Id;
        }
        public static int FetchOrCreateDecisionType(CMSDataContext Db, string type)
        {
            var dt = Db.DecisionTypes.SingleOrDefault(m => m.Description == type);
            if (dt == null)
            {
                var max = Db.DecisionTypes.Max(mm => mm.Id) + 10;
                dt = new DecisionType() { Id = max, Code = "d" + max, Description = type };
                Db.DecisionTypes.InsertOnSubmit(dt);
                Db.SubmitChanges();
            }
            return dt.Id;
        }
        public static int FetchOrCreateNewMemberClassStatus(CMSDataContext db, string type)
        {
            var i = db.NewMemberClassStatuses.SingleOrDefault(m => m.Description == type);
            if (i == null)
            {
                var max = db.NewMemberClassStatuses.Max(mm => mm.Id) + 10;
                i = new NewMemberClassStatus() { Id = max, Code = "NM" + max, Description = type };
                db.NewMemberClassStatuses.InsertOnSubmit(i);
                db.SubmitChanges();
            }
            return i.Id;
        }
        public static Campu FetchOrCreateCampus(CMSDataContext Db, string campus)
        {
            if (!Util.HasValue(campus))
                return null;
            var cam = Db.Campus.SingleOrDefault(pp => pp.Description == campus);
            if (cam == null)
            {
                int max = 10;
                if (Db.Campus.Any())
                    max = Db.Campus.Max(mm => mm.Id) + 10;
                cam = new Campu() { Id = max, Description = campus, Code = campus.Truncate(20) };
                Db.Campus.InsertOnSubmit(cam);
                Db.SubmitChanges();
            }
            else if (!Util.HasValue(cam.Code))
            {
                cam.Code = campus.Truncate(20);
                Db.SubmitChanges();
            }
            return cam;
        }
        public Task AddTaskAbout(CMSDataContext Db, int AssignTo, string description)
        {
            var t = new Task
            {
                OwnerId = AssignTo,
                Description = description,
                ForceCompleteWContact = true,
                ListId = Task.GetRequiredTaskList(Db, "InBox", AssignTo).Id,
                StatusId = TaskStatusCode.Active,
            };
            TasksAboutPerson.Add(t);
            return t;
        }
        public void UpdatePosition(CMSDataContext db, int value)
        {
            this.UpdateValue("PositionInFamilyId", value);
            LogChanges(db, Util.UserPeopleId.Value);
            db.SubmitChanges();
        }
        public void UpdateCampus(CMSDataContext db, object value)
        {
            var campusid = value.ToInt2();
            if (campusid == 0)
                campusid = null;
            this.UpdateValue("CampusId", campusid);
            LogChanges(db, Util.UserPeopleId.Value);
            db.SubmitChanges();
        }
        public void UploadPicture(CMSDataContext db, System.IO.Stream stream)
        {
            if (Picture == null)
                Picture = new Picture();
            var bits = new byte[stream.Length];
            stream.Read(bits, 0, bits.Length);
            var p = Picture;
            p.CreatedDate = Util.Now;
            p.CreatedBy = Util.UserName;
            p.ThumbId = ImageData.Image.NewImageFromBits(bits, 50, 50).Id;
            p.SmallId = ImageData.Image.NewImageFromBits(bits, 120, 120).Id;
            p.MediumId = ImageData.Image.NewImageFromBits(bits, 320, 400).Id;
            p.LargeId = ImageData.Image.NewImageFromBits(bits).Id;
            LogPictureUpload(db, Util.UserPeopleId ?? 1);
            db.SubmitChanges();

        }
        public void DeletePicture(CMSDataContext db)
        {
            if (Picture == null)
                return;
            Image.Delete(Picture.ThumbId);
            Image.Delete(Picture.SmallId);
            Image.Delete(Picture.MediumId);
            Image.Delete(Picture.LargeId);
            var pid = PictureId;
            Picture = null;
            db.SubmitChanges();
            db.ExecuteCommand("DELETE dbo.Picture WHERE PictureId = {0}", pid);
        }
        public void DeleteThumbnail(CMSDataContext db)
        {
            if (Picture == null)
                return;
            Image.Delete(Picture.ThumbId);
            Picture.ThumbId = null;
            db.SubmitChanges();
        }

        public void UploadDocument(CMSDataContext db, System.IO.Stream stream, string name, string mimetype)
        {
            var mdf = new MemberDocForm
            {
                PeopleId = PeopleId,
                DocDate = Util.Now,
                UploaderId = Util2.CurrentPeopleId,
                Name = System.IO.Path.GetFileName(name).Truncate(100)
            };
            db.MemberDocForms.InsertOnSubmit(mdf);
            var bits = new byte[stream.Length];
            stream.Read(bits, 0, bits.Length);
            switch (mimetype)
            {
                case "image/jpeg":
                case "image/pjpeg":
                case "image/gif":
                case "image/png":
                    mdf.IsDocument = false;
                    mdf.SmallId = ImageData.Image.NewImageFromBits(bits, 165, 220).Id;
                    mdf.MediumId = ImageData.Image.NewImageFromBits(bits, 675, 900).Id;
                    mdf.LargeId = ImageData.Image.NewImageFromBits(bits).Id;
                    break;
                case "text/plain":
                case "application/pdf":
                case "application/msword":
                case "application/vnd.ms-excel":
                    mdf.MediumId = ImageData.Image.NewImageFromBits(bits, mimetype).Id;
                    mdf.SmallId = mdf.MediumId;
                    mdf.LargeId = mdf.MediumId;
                    mdf.IsDocument = true;
                    break;
                default:
                    throw new FormatException("file type not supported: " + mimetype);
            }
            db.SubmitChanges();
        }

        public void SplitFamily(CMSDataContext db)
        {
            var f = new Family
            {
                CreatedDate = Util.Now,
                CreatedBy = Util.UserId1,
                AddressLineOne = PrimaryAddress,
                AddressLineTwo = PrimaryAddress2,
                CityName = PrimaryCity,
                StateCode = PrimaryState,
                ZipCode = PrimaryZip,
                HomePhone = Family.HomePhone
            };
            var oldf = this.FamilyId;
            f.People.Add(this);
            db.Families.InsertOnSubmit(f);
            db.SubmitChanges();
        }

        public RelatedFamily AddRelated(CMSDataContext db, int pid)
        {
            var p2 = db.LoadPersonById(pid);
            var rf = db.RelatedFamilies.SingleOrDefault(r =>
                (r.FamilyId == FamilyId && r.RelatedFamilyId == p2.FamilyId)
                || (r.FamilyId == p2.FamilyId && r.RelatedFamilyId == FamilyId)
                );
            if (rf == null)
            {
                rf = new RelatedFamily
                {
                    FamilyId = FamilyId,
                    RelatedFamilyId = p2.FamilyId,
                    FamilyRelationshipDesc = "",
                    CreatedBy = Util.UserId1,
                    CreatedDate = Util.Now,
                };
                db.RelatedFamilies.InsertOnSubmit(rf);
                db.SubmitChanges();
            }
            return rf;
        }

        public static void TryExtraValueIntegrity(CMSDataContext Db, string type, string newfield, List<string> BitCodes)
        {
            const string nameAlreadyExistsAsADifferentType = "name already exists as a different type";
        }

        public bool CanViewStatementFor(CMSDataContext Db, int id)
        {
            bool canview = Util.UserPeopleId == id || HttpContext.Current.User.IsInRole("Finance");
            if (!canview)
            {
                var p = Db.CurrentUserPerson;
                if (p.SpouseId == id)
                {
                    var sp = Db.LoadPersonById(id);
                    if ((p.ContributionOptionsId ?? StatementOptionCode.Joint) == StatementOptionCode.Joint &&
                        (sp.ContributionOptionsId ?? StatementOptionCode.Joint) == StatementOptionCode.Joint)
                        canview = true;
                }
            }
            return canview;
        }

        public List<EmailOptOut> GetOptOuts()
        {
            return EmailOptOuts.ToList();
        }

        public List<User> GetUsers()
        {
            return Users.ToList();
        }
    }
}