﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using CmsData;
using CmsData.API;
using CmsData.View;
using CmsWeb.Models;
using Dapper;
using UtilityExtensions;

namespace CmsWeb.Areas.Reports.Models
{
    public class CustomReportsModel
    {
        public static IEnumerable<string> ReportList(CMSDataContext db)
        {
            var list = new List<string>();
            var body = db.ContentText("CustomReports", "");
            if (body.HasValue())
            {
                var xdoc = XDocument.Parse(body);
                if (xdoc.Root == null)
                    return list;
                var q = from e in xdoc.Root.Elements("Report")
                        let r = (string)e.Attribute("name")
                        where r != null
                        where r != "AllColumns"
                        select r;
                foreach (var r in q)
                    if (!list.Contains(r))
                        list.Add(r);
            }
            list.Add("AllColumns");
            return list;
        }

        public static EpplusResult Result(CMSDataContext db, Guid id, string report)
        {
            var cs = db.CurrentUser.InRole("Finance")
                ? Util.ConnectionString
                : Util.ConnectionStringReadOnly;
            var cn = new SqlConnection(cs);
            var sql = Sql(db, id, report);
            return cn.ExecuteReader(sql).ToExcel(report + ".xlsx");
        }
        public static string Sql(CMSDataContext db, Guid id, string report)
        {
            var body = db.ContentText("CustomReports", "");
            if (report == "AllColumns")
            {
                var settings = new XmlWriterSettings { Indent = true, Encoding = new System.Text.UTF8Encoding(false) };
                var sb2 = new StringBuilder();
                using (var w = XmlWriter.Create(sb2, settings))
                {
                    StandardColumns(db, w, includeRoot: true);
                    w.Flush();
                }
                body = sb2.ToString();
            }
            else if (!body.HasValue())
                throw new Exception("missing CustomReports");
            var xdoc = XDocument.Parse(body);
            if (xdoc.Root == null)
                throw new Exception("missing xml root");
            var r = (from e in xdoc.Root.Elements("Report")
                     where (string)e.Attribute("name") == report || report == "AllColumns"
                     select e).SingleOrDefault();
            if (r == null)
                throw new Exception("no report");
            var tag = db.PopulateSpecialTag(id, DbUtil.TagTypeId_Query);
            var sb = new StringBuilder("DECLARE @tagId INT = {0}\nSELECT\n".Fmt(tag.Id));
            var d = db.CustomColumns.ToDictionary(cc => cc.Column, cc => cc);
            Dictionary<string, StatusFlagList> flags = null;
            var comma = "";
            var joins = new List<string>();
            foreach (var e in r.Elements("Column"))
            {
                if ((string)e.Attribute("disabled") == "true")
                    continue;
                var name = (string)e.Attribute("name");
                if (!d.ContainsKey(name))
                    throw new Exception("missing column named '{0}'".Fmt(name));
                var cc = d[name];
                if (name == "StatusFlag")
                {
                    if (flags == null)
                        flags = db.ViewStatusFlagLists.Where(ff => ff.RoleName == null).ToDictionary(ff => ff.Flag, ff => ff);
                    var flag = (string)e.Attribute("flag");
                    if (!flag.HasValue())
                        throw new Exception("missing flag on column " + cc.Column);
                    if (!flags.ContainsKey(flag))
                        throw new Exception("missing flag '{0}' on column {1}".Fmt(flag, cc.Column));
                    var sel = cc.Select.Replace("{flag}", flag);
                    var desc = (string)e.Attribute("description");
                    if (!desc.HasValue())
                        desc = flags[flag].Name;
                    sb.AppendFormat("\t{0}{1} AS [{2}]\n", comma, sel, DblQuotes(desc));
                }
                else if (name.StartsWith("ExtraValue") && Regex.IsMatch(name, @"\AExtraValue(Code|Date|Text|Int|Bit)\z"))
                {
                    var field = (string)e.Attribute("field");
                    if (!field.HasValue())
                        throw new Exception("missing field on column " + cc.Column);
                    var sel = cc.Select.Replace("{field}", DblQuotes(field));
                    sb.AppendFormat("\t{0}{1} AS [{2}]\n", comma, sel, DblQuotes(field));
                }
                else
                {
                    sb.AppendFormat("\t{0}{1} AS [{2}]\n", comma, cc.Select, DblQuotes(cc.Column));
                    if (cc.JoinTable.HasValue())
                        if (!joins.Contains(cc.JoinTable))
                            joins.Add(cc.JoinTable);
                }
                comma = ",";
            }
            sb.AppendLine("FROM dbo.People p");
            foreach (var j in joins)
                sb.AppendLine(j);
            sb.AppendLine("JOIN dbo.TagPerson tp ON tp.PeopleId = p.PeopleId");
            sb.AppendLine("WHERE tp.Id = @tagId\n");
            return sb.ToString();
        }

        public static string DblQuotes(string s)
        {
            return s.Replace("'", "''");
        }
        public static void StandardColumns(CMSDataContext db, XmlWriter writer, bool includeRoot = true)
        {
            var list = db.CustomColumns.OrderBy(cc => cc.Ord).ToList();
            var dict = new Dictionary<string, CustomColumn>();
            var w = new APIWriter(writer);
            if (includeRoot)
                w.Start("CustomReports");
            w.Start("Report").Attr("name", "YourReportNameGoesHere");
            foreach (var c in list)
            {
                if (c.Column == "StatusFlag")
                    dict.Add(c.Column, c);
                else if (c.Column.StartsWith("ExtraValue"))
                    dict.Add(c.Column, c);
                else
                    w.Start("Column").Attr("name", c.Column).End();
            }
            var protectedevs = from value in CmsData.ExtraValue.Views.GetStandardExtraValues(DbUtil.Db, "People")
                         where value.VisibilityRoles.HasValue()
                         select value.Name;
            var standards = (from value in CmsData.ExtraValue.Views.GetStandardExtraValues(DbUtil.Db, "People")
                             select value.Name).ToList();
            var q = from ev in db.PeopleExtras
                    where !protectedevs.Contains(ev.Field)
                    group ev by new { ev.Field, ev.Type } into g
                    orderby g.Key.Field
                    select g.Key;
            foreach (var ev in q)
            {
                if (!Regex.IsMatch(ev.Type, @"Code|Date|Text|Int|Bit"))
                    continue;
                w.Start("Column");
                w.Attr("field", ev.Field).Attr("name", "ExtraValue" + ev.Type);
                if (!standards.Contains(ev.Field))
                    w.Attr("disabled", "true");
                w.End();
            }
            var q2 = from f in db.ViewStatusFlagLists
                     where f.RoleName == null
                     orderby f.Name
                     select f;
            foreach (var f in q2)
            {
                w.Start("Column")
                    .Attr("description", f.Name)
                    .Attr("flag", f.Flag)
                    .Attr("name", "StatusFlag")
                    .End();
            }
            w.End();
            if (includeRoot)
                w.End();
        }
    }
}