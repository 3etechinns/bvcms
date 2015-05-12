﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using CmsData;
using CmsWeb.Code;
using UtilityExtensions;
using Tasks = System.Threading.Tasks;

namespace CmsWeb.Areas.Dialog.Models
{
    public class ValidateAddress : LongRunningOp
    {
        public const string Op = "ValidateAddress";

        public ValidateAddress()
        {
            Tag = new CodeInfo("0", "Tag");
        }

        [DisplayName("Choose A Tag")]
        public CodeInfo Tag { get; set; }

        public void Process(CMSDataContext db)
        {
            pids = FetchPeopleIds(db, Tag.Value.ToInt()).ToList();

            var lop = new LongRunningOp
            {
                Started = DateTime.Now,
                Count = pids.Count,
                Processed = 0,
                Id = Id,
                Operation = Op,
            };
            db.LongRunningOps.InsertOnSubmit(lop);
            db.SubmitChanges();

            Tasks.Task.Run(() => DoWork(this));
        }

        internal List<int> pids;

        public bool TagHasBeenSelected
        {
            get { return Count.HasValue; }
        }

        public static void DoWork(ValidateAddress model)
        {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            var db = CMSDataContext.Create(Util.GetConnectionString(model.host));
            db.Host = model.host;
            var cul = db.Setting("Culture", "en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(cul);
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cul);

            LongRunningOp lop = null;
            foreach (var pid in model.pids)
            {
                db.Dispose();
                var fsb = new List<ChangeDetail>();
                db = CMSDataContext.Create(Util.GetConnectionString(model.host));
                var f = db.LoadFamilyByPersonId(pid); 
                var ret = AddressVerify.LookupAddress(f.AddressLineOne, f.AddressLineTwo, f.CityName, f.StateCode, f.ZipCode);
                if (ret.found != false && !ret.error.HasValue() && ret.Line1 != "error")
                {
                    f.UpdateValue(fsb, "AddressLineOne", ret.Line1);
                    f.UpdateValue(fsb, "AddressLineTwo", ret.Line2);
                    f.UpdateValue(fsb, "CityName", ret.City);
                    f.UpdateValue(fsb, "StateCode", ret.State);
                    f.UpdateValue(fsb, "ZipCode", ret.Zip.GetDigits());
                    var rc = db.FindResCode(ret.Zip, null);
                    f.UpdateValue(fsb, "ResCodeId", rc.ToString());
                }
                else
                {
                    f.UpdateValue(fsb, "ZipCode", f.ZipCode.Zip5());
                }

                lop = FetchLongRunningOp(db, model.Id, Op);
                Debug.Assert(lop != null, "r != null");
                lop.Processed++;
                f.LogChanges(db, fsb, pid, Util.UserPeopleId ?? 0);
                db.SubmitChanges();
                //Thread.Sleep(1000);
            }
            // finished
            lop = FetchLongRunningOp(db, model.Id, Op);
            lop.Completed = DateTime.Now;
            db.SubmitChanges();
        }
        public static IQueryable<int> FetchPeopleIds(CMSDataContext db, int tagid)
        {
            return tagid == -1
                ? (from p in db.PeopleQueryLast()
                   group p by p.FamilyId into ff
                   select ff.First().PeopleId)
                : (from t in db.TagPeople
                   where t.Id == tagid
                   group t.Person by t.Person.FamilyId into ff
                   select ff.First().PeopleId);
        }
        public void Validate(ModelStateDictionary modelState)
        {
            if (Tag != null && Tag.Value == "0") // They did not choose a tag
                modelState.AddModelError("Tag", "Must choose a tag");
        }

        public bool ShowCount(CMSDataContext db)
        {
            if (Count == null && Tag != null)
            {
                var q = FetchPeopleIds(db, Tag.Value.ToInt());
                Count = q.Count();
                db.SubmitChanges();
                return true;
            }
            return false;
        }
    }
}
