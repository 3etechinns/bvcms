using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CmsData;
using System.Web.Mvc;
using UtilityExtensions;
using System.Text.RegularExpressions;
using CmsData.Codes;

namespace CmsWeb.Models
{
    public class NewOrganizationModel
    {
        public CmsData.Organization org { get; set; }
        public int? OrganizationId { get; set; }
		public bool copysettings { get; set; }
		public bool copyregistration { get; set; }
        public NewOrganizationModel(int? id)
        {
			if (!id.HasValue)
		        id = DbUtil.Db.Setting("DefaultOrgId", "0").ToInt();
	        org = DbUtil.Db.LoadOrganizationById(id);
			if (org == null)
				org = DbUtil.Db.Organizations.First();
	        OrganizationId = org.OrganizationId;
        }

	    public NewOrganizationModel()
        {
        }
        private CodeValueModel cv = new CodeValueModel();

        public IEnumerable<SelectListItem> CampusList()
        {
            return QueryModel.ConvertToSelect(cv.AllCampuses0(), "Id");
        }
        public IEnumerable<SelectListItem> OrgStatusList()
        {
            return QueryModel.ConvertToSelect(cv.OrganizationStatusCodes(), "Id");
        }
        public IEnumerable<SelectListItem> LeaderTypeList()
        {
            var items = CodeValueModel.MemberTypeCodes0().Select(c => new CodeValueItem { Code = c.Code, Id = c.Id, Value = c.Value });
            return QueryModel.ConvertToSelect(items, "Id");
        }
        public IEnumerable<SelectListItem> EntryPointList()
        {
            return QueryModel.ConvertToSelect(cv.EntryPoints(), "Id");
        }
        public IEnumerable<SelectListItem> GenderList()
        {
            return QueryModel.ConvertToSelect(cv.GenderCodes(), "Id");
        }
        public IEnumerable<SelectListItem> SecurityTypeList()
        {
            return QueryModel.ConvertToSelect(cv.SecurityTypeCodes(), "Id");
        }
    }
}
