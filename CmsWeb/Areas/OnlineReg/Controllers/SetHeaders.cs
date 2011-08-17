﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CmsData;
using UtilityExtensions;
using System.Text.RegularExpressions;
using CmsWeb.Models;

namespace CmsWeb.Areas.OnlineReg.Controllers
{
    public partial class OnlineRegController : CmsController
    {
        private void SetHeaders(OnlineRegModel m2)
        {
            Session["gobackurl"] = m2.URL;
            ViewData["timeout"] = INT_timeout;
            SetHeaders(m2.orgid ?? m2.divid ?? 0);
        }
        private void SetHeaders(int id)
        {
            RegSettings setting = null;
            var org = DbUtil.Db.LoadOrganizationById(id);
            var shell = "";
            if ((settings == null || !settings.ContainsKey(id)) && org != null)
            {
                setting = OnlineRegModel.ParseSetting(org.RegSetting, id);
                shell = DbUtil.Content(setting.Shell, null);
            }
            if (!shell.HasValue() && settings != null && settings.ContainsKey(id))
            {
                shell = DbUtil.Content(settings[id].Shell, null);
            }
            if (!shell.HasValue())
                shell = DbUtil.Content("ShellDiv-" + id, DbUtil.Content("ShellDefault", ""));

            var s = shell;
            if (s.HasValue())
            {
                var re = new Regex(@"(.*<!--FORM START-->\s*).*(<!--FORM END-->.*)", RegexOptions.Singleline);
                var t = re.Match(s).Groups[1].Value.Replace("<!--FORM CSS-->", 
@"
<link href=""/Content/jquery-ui-1.8.13.custom.css"" rel=""stylesheet"" type=""text/css"" />
<link href=""/Content/onlinereg.css?v=7"" rel=""stylesheet"" type=""text/css"" />
"); 
                ViewData["hasshell"] = true;
                ViewData["top"] = t;
                var b = re.Match(s).Groups[2].Value;
                ViewData["bottom"] = b;
            }
            else
            {
                ViewData["hasshell"] = false;
                ViewData["header"] = DbUtil.Content("OnlineRegHeader-" + id,
                    DbUtil.Content("OnlineRegHeader", ""));
                ViewData["top"] = DbUtil.Content("OnlineRegTop-" + id,
                    DbUtil.Content("OnlineRegTop", ""));
                ViewData["bottom"] = DbUtil.Content("OnlineRegBottom-" + id,
                    DbUtil.Content("OnlineRegBottom", ""));
            }
        }
        
    }
}