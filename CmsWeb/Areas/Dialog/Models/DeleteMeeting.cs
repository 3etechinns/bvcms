using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using CmsData;
using UtilityExtensions;
using Tasks = System.Threading.Tasks;

namespace CmsWeb.Areas.Dialog.Models
{
    public class DeleteMeeting : LongRunningOp
    {
        public const string Op = "deletemeeting";

        public DeleteMeeting() { }
        public DeleteMeeting(int id)
        {
            Id = id;
            var mm = DbUtil.Db.Meetings.Single(m => m.MeetingId == id);
            Count = mm.Attends.Count(a => a.AttendanceFlag || a.EffAttendFlag == true);
        }

        internal List<int> pids;

        public void Process(CMSDataContext db)
        {
            var q = from a in db.Attends
                    where a.MeetingId == Id
                    where a.AttendanceFlag || a.EffAttendFlag == true
                    select a.PeopleId;
            pids = q.ToList();
            var lop = new LongRunningOp()
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

        private static void DoWork(DeleteMeeting model)
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
                db = CMSDataContext.Create(Util.GetConnectionString(model.host));
                Attend.RecordAttendance(db, pid, model.Id, false);
                lop = FetchLongRunningOp(db, model.Id, Op);
                Debug.Assert(lop != null, "r != null");
                lop.Processed++;
                db.SubmitChanges();
            }
            db.ExecuteCommand(@"
DELETE dbo.SubRequest 
WHERE EXISTS(
    SELECT NULL FROM dbo.Attend a 
    WHERE a.AttendId = AttendId 
    AND a.MeetingId = {0}
)", model.Id);
            db.ExecuteCommand("DELETE dbo.VolRequest WHERE MeetingId = {0}", model.Id);
            db.ExecuteCommand("DELETE dbo.attend WHERE MeetingId = {0}", model.Id);
            db.ExecuteCommand("DELETE dbo.MeetingExtra WHERE MeetingId = {0}", model.Id);
            db.ExecuteCommand("DELETE dbo.meetings WHERE MeetingId = {0}", model.Id);

            db.SubmitChanges();

            // finished
            lop = FetchLongRunningOp(db, model.Id, Op);
            lop.Completed = DateTime.Now;
            db.SubmitChanges();
        }
    }
}