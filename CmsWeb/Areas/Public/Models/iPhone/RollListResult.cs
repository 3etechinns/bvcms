using System;
using System.Collections.Generic;
using System.Xml;
using System.Web.Mvc;
using System.Xml.Linq;
using UtilityExtensions;
using System.Linq;
using CmsData;
using CmsWeb.Areas.Main.Models.Report;

namespace CmsWeb.Models.iPhone
{
    public class RollListResult : ActionResult
    {
        int? MeetingId;
        int OrgId;
        DateTime MeetingDate;
        public RollListResult(CmsData.Meeting meeting)
        {
            MeetingId = meeting.MeetingId;
            OrgId = meeting.OrganizationId;
            MeetingDate = meeting.MeetingDate.Value;
        }
        public RollListResult(int orgid, DateTime dt)
        {
            var meeting = DbUtil.Db.Meetings.SingleOrDefault(mm => mm.MeetingDate == dt && mm.OrganizationId == orgid);
            if (meeting != null)
                MeetingId = meeting.MeetingId;
            OrgId = orgid;
            MeetingDate = dt;
        }
        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.ContentType = "text/xml";
            var settings = new XmlWriterSettings();
            settings.Encoding = new System.Text.UTF8Encoding(false);
            settings.Indent = true;

            using (var w = XmlWriter.Create(context.HttpContext.Response.OutputStream, settings))
            {
                w.WriteStartElement("RollList");
                w.WriteAttributeString("MeetingId", MeetingId.ToString());

                foreach (var p in RollsheetModel.RollList(MeetingId, OrgId, MeetingDate))
                {
                    w.WriteStartElement("Person");
                    w.WriteAttributeString("Id", p.PeopleId.ToString());
                    w.WriteAttributeString("Name", p.Name);
                    w.WriteAttributeString("Attended", p.Attended.ToString());
                    w.WriteAttributeString("Member", p.Member.ToString());
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            }
        }
    }
}