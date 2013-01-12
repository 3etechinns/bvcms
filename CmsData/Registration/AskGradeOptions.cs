﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CmsData.Registration
{
	public class AskGradeOptions : Ask
	{
		public string Label { get; set; }
		public List<GradeOption> list { get; set; }

		public AskGradeOptions()
			: base("AskGradeOptions")
		{
			list = new List<GradeOption>();
		}

		public override void Output(StringBuilder sb)
		{
			if (list.Count == 0)
				return;
			Settings.AddValueNoCk(0, sb, "AskGradeOptions", Label);
			foreach (var g in list)
				g.Output(sb);
			sb.AppendLine();
		}
		public static AskGradeOptions Parse(Parser parser)
		{
			var go = new AskGradeOptions();
			go.Label = parser.GetString("AskGradeOptions");
			if (parser.curr.indent == 0)
				throw parser.GetException("expected indented Options");
			var startindent = parser.curr.indent;
			while (parser.curr.indent == startindent)
			{
				var option = GradeOption.Parse(parser, startindent);
                if(go.list.All(gg => gg.Code != option.Code))
    				go.list.Add(option);
			}
			return go;
		}
		public class GradeOption
		{
			public string Name { get; set; }
			public string Description { get; set; }
			public int Code { get; set; }
			public void Output(StringBuilder sb)
			{
				Settings.AddValueCk(1, sb, Description);
				Settings.AddValueCk(2, sb, "Code", Code);
			}
			public static GradeOption Parse(Parser parser, int startindent)
			{
				if (parser.curr.kw != Parser.RegKeywords.None)
					throw parser.GetException("expected description only");
				var option = new GradeOption();
				option.Description = parser.GetLine();
				if (parser.curr.indent <= startindent)
					throw parser.GetException("expected greater indent");
				if (parser.curr.kw != Parser.RegKeywords.Code)
					throw parser.GetException("expected Code");
				var code = parser.GetNullInt();
				if (!code.HasValue)
				{
					parser.lineno--;
					throw parser.GetException("expected integer code");
				}
				option.Code = code.Value;
				return option;
			}
		}
	}
}
