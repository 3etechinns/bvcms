/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UtilityExtensions;
using System.Text;
using System.Xml.Linq;
using System.Linq.Expressions;
using System.Web;

namespace CmsData
{

    public class FieldClass
    {
        private string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                _QueryType = ConvertQueryType(value);
            }
        }
        private QueryType _QueryType;
        public QueryType QueryType
        {
            get
            {
                return _QueryType;
            }
            set
            {
                _QueryType = value;
                Name = value.ToString();
            }
        }
        public string CategoryTitle { get; set; }
        private string _Title;
        public string Title
        {
            get { return _Title.HasValue() ? _Title : Name; }
            set { _Title = value; }
        }
        public FieldType Type { get; set; }
        public string DisplayAs { get; set; }
        private string _Params;
        public string Params
        {
            get { return _Params; }
            set
            {
                _Params = value;
                if (value.HasValue())
                    ParamList = value.SplitStr(",").ToList();
            }
        }
        public List<string> ParamList { get; set; }
        public string DataSource { get; set; }
        public string DataValueField { get; set; }
        public string DataTextField { get; set; }
        private string formatArgs(string fmt, QueryBuilderClause c)
        {
            var p = new List<object>();
            foreach (var s in ParamList)
            {
                var s2 = s;
                if (s2 == "Week")
                    s2 = "Quarters";
                object prop = Util.GetProperty(c, s2);
                if (s == "SavedQueryValue")
                    prop = ((string)prop).Split(',')[1];
                p.Add(prop);
            }
            return fmt.Fmt(p.ToArray());
        }
        internal string Display(QueryBuilderClause c)
        {
            if (!DisplayAs.HasValue() || !Params.HasValue())
                return Name;
            return formatArgs(DisplayAs, c);
        }
        public bool HasParam(string p)
        {
            return ParamList == null ? false : ParamList.Contains(p);
        }
        public static FieldType Convert(string type)
        {
            return (FieldType)Enum.Parse(typeof(FieldType), type);
        }
        public static QueryType ConvertQueryType(string type)
        {
            return (QueryType)Enum.Parse(typeof(QueryType), type);
        }
        public static Dictionary<string, FieldClass> Fields
        {
            get
            {
                var fields = HttpRuntime.Cache["fields"] as Dictionary<string, FieldClass>;
                if (fields == null)
                {
                    var q = from c in CategoryClass.Categories
                            from f in c.Fields
                            select f;
                    fields = q.ToDictionary(f => f.Name);
                    HttpRuntime.Cache["fields"] = fields;
                }
                return fields;
            }
        }
        public string Description { get; set; }
    }
}
