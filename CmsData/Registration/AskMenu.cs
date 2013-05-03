using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using UtilityExtensions;

namespace CmsData.Registration
{
	public class AskMenu : Ask
	{
		public string Label { get; set; }
		public List<MenuItem> list { get; set; }

		public AskMenu()
			: base("AskMenu")
		{
			list = new List<MenuItem>();
		}

		public override void Output(StringBuilder sb)
		{
			if (list.Count == 0)
				return;
			Settings.AddValueNoCk(0, sb, "MenuItems", "");
			foreach (var i in list)
				i.Output(sb);
			sb.AppendLine();
		}

		public static AskMenu Parse(Parser parser)
		{
			var mi = new AskMenu();
			mi.Label = parser.GetString("Menu");
			mi.list = new List<MenuItem>();
			if (parser.curr.indent == 0)
				return mi;
			var startindent = parser.curr.indent;
			while (parser.curr.indent == startindent)
			{
				var m = MenuItem.Parse(parser, startindent);
				mi.list.Add(m);
			}
            var q = (from i in mi.list
                     group i by i.SmallGroup into g
                     where g.Count() > 1
                     select g.Key).ToList();
			if (q.Any())
				throw parser.GetException("Duplicate SmallGroup in MenuItems: {0}".Fmt(string.Join(",", q)));
			return mi;
		}
        public override List<string> SmallGroups()
        {
            var q = (from i in list
                     select i.SmallGroup).ToList();
            return q;
        }
        public class MenuItemChosen
        {
            public string sg { get; set; }
            public string desc { get; set; }
            public int number { get; set; }
            public decimal amt { get; set; }
        }
		public IEnumerable<MenuItemChosen> MenuItemsChosen(Dictionary<string, int?> items)
		{
			if (items == null)
				return new List<MenuItemChosen>();
			var q = from i in items
					join m in list on i.Key equals m.SmallGroup
					where i.Value.HasValue
					select new MenuItemChosen
					{
						sg = m.SmallGroup, 
						number = i.Value ?? 0, 
						desc = m.Description, 
						amt = m.Fee ?? 0
					};
            return q;
		}

		public class MenuItem
		{
			public string Name { get; set; }
			public string Description { get; set; }
			public string SmallGroup { get; set; }
			public decimal? Fee { get; set; }
			public int? Limit { get; set; }

			[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:g}")]
			public DateTime? MeetingTime { get; set; }

			public void Output(StringBuilder sb)
			{
				Settings.AddValueCk(1, sb, Description);
				Settings.AddValueCk(2, sb, "SmallGroup", SmallGroup);
				Settings.AddValueCk(2, sb, "Fee", Fee);
				Settings.AddValueCk(2, sb, "Limit", Limit);
				Settings.AddValueCk(2, sb, "Time", MeetingTime.ToString2("s"));
			}

			public static MenuItem Parse(Parser parser, int startindent)
			{
				var menuitem = new MenuItem();
				if (parser.curr.kw != Parser.RegKeywords.None)
					throw parser.GetException("unexpected line in MenuItem");
				menuitem.Description = parser.GetLine();
				menuitem.SmallGroup = menuitem.Description;
				if (parser.curr.indent <= startindent)
					return menuitem;
				var ind = parser.curr.indent;
				while (parser.curr.indent == ind)
				{
					switch (parser.curr.kw)
					{
						case Parser.RegKeywords.SmallGroup:
							menuitem.SmallGroup = parser.GetString(menuitem.Description);
							break;
						case Parser.RegKeywords.Fee:
							menuitem.Fee = parser.GetDecimal();
							break;
						case Parser.RegKeywords.Limit:
							menuitem.Limit = parser.GetNullInt();
							break;
						case Parser.RegKeywords.Time:
							menuitem.MeetingTime = parser.GetDateTime();
							break;
						default:
							throw parser.GetException("unexpected line in MenuItem");
					}
				}
				return menuitem;
			}
		}
	}
}