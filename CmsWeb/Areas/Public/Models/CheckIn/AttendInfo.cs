using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CmsWeb.Models.OrganizationPage
{
    public class MeetingInfo
    {
        public int MeetingId { get; set; }
        public DateTime? MeetingDate { get; set; }
        public DateTime? MeetingTime
        {
            get
            {
                if (MeetingDate.HasValue)
                    if (MeetingDate.Value.TimeOfDay.TotalSeconds > 0)
                        return MeetingDate;
                return null;
            }
        }
        public int OrganizationId { get; set; }
        public int NumPresent { get; set; }
        public int NumVisitors { get; set; }
        public string Location { get; set; }
        public bool GroupMeeting { get; set; }
        public string Description { get; set; }
    }
}
