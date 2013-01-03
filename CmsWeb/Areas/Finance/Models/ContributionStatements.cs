/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using CmsData.API;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using CmsData;
using UtilityExtensions;
using iTextSharp.text.html.simpleparser;
using CmsWeb.Areas.Main.Models.Report;
using System.Diagnostics;

namespace CmsWeb.Areas.Finance.Models.Report
{
	public class ContributionStatements
	{
		public int FamilyId { get; set; }
		public int PeopleId { get; set; }
		public int? SpouseId { get; set; }
		public int typ { get; set; }
		public DateTime FromDate { get; set; }
		public DateTime ToDate { get; set; }
		private PageEvent pageEvents = new PageEvent();
		public int LastSet()
		{
			if (pageEvents.FamilySet.Count == 0)
				return 0;
			var m = pageEvents.FamilySet.Max(kp => kp.Value);
			return m;
		}
		public List<int> Sets()
		{
			if (pageEvents.FamilySet.Count == 0)
				return new List<int>();
			var m = pageEvents.FamilySet.Values.Distinct().ToList();
			return m;
		}

		public void Run(Stream stream, CMSDataContext Db, IEnumerable<ContributorInfo> q, int set = 0)
		{
			pageEvents.set = set;
			IEnumerable<ContributorInfo> contributors = q;
		    var toDate = ToDate.Date.AddHours(24).AddSeconds(-1);

			PdfContentByte dc;
			var font = FontFactory.GetFont(FontFactory.HELVETICA, 11);
			var boldfont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);

			var doc = new Document(PageSize.LETTER);
			doc.SetMargins(36f, 30f, 24f, 36f);
			var w = PdfWriter.GetInstance(doc, stream);
			w.PageEvent = pageEvents;
			doc.Open();
			dc = w.DirectContent;

			int prevfid = 0;
			var runningtotals = Db.ContributionsRuns.OrderByDescending(mm => mm.Id).FirstOrDefault();
			runningtotals.Processed = 0;
			Db.SubmitChanges();
			var count = 0;
			foreach (var ci in contributors)
			{
				if (set > 0 && pageEvents.FamilySet[ci.PeopleId] != set)
					continue;
			    doc.NewPage();
				if (prevfid != ci.FamilyId)
				{
					prevfid = ci.FamilyId;
					pageEvents.EndPageSet();
				}
				if (set == 0)
					pageEvents.FamilySet[ci.PeopleId] = 0;
				count++;

				var st = new StyleSheet();
				st.LoadTagStyle("h1", "size", "18px");
				st.LoadTagStyle("h2", "size", "8px");
				st.LoadTagStyle("p", "size", "8px");

				//----Church Name
				var t1 = new PdfPTable(1);
				t1.TotalWidth = 72f * 5f;
				t1.DefaultCell.Border = Rectangle.NO_BORDER;
				string html1 = @"<h1>Bellevue Baptist Church</h1>
<h2>2000 Appling Rd. | Cordova | TN 38088-1210 | (901) 347-2000</h2>";
				var content = Db.Contents.SingleOrDefault(c => c.Name == "StatementHeader");
				if (content != null)
					html1 = content.Body;
				var list = HTMLWorker.ParseToList(new StringReader(html1), st);
				var cell = new PdfPCell(t1.DefaultCell);
				for (int k = 0; k < list.Count; k++)
					cell.AddElement((IElement)list[k]);
				//cell.FixedHeight = 72f * 1.25f;
				t1.AddCell(cell);
				t1.AddCell("\n");

				var t1a = new PdfPTable(1);
				t1a.TotalWidth = 72f * 5f;
				t1a.DefaultCell.Border = Rectangle.NO_BORDER;

				var ae = new PdfPTable(1);
				ae.DefaultCell.Border = Rectangle.NO_BORDER;
				ae.WidthPercentage = 100;

				var a = new PdfPTable(1);
				a.DefaultCell.Indent = 25f;
				a.DefaultCell.Border = Rectangle.NO_BORDER;
				a.AddCell(new Phrase(ci.Name, font));
				a.AddCell(new Phrase(ci.Address1, font));
				if (ci.Address2.HasValue())
					a.AddCell(new Phrase(ci.Address2, font));
				a.AddCell(new Phrase(ci.CityStateZip, font));
				cell = new PdfPCell(a) { Border = Rectangle.NO_BORDER };
				//cell.FixedHeight = 72f * 1.0625f;
				ae.AddCell(cell);

				cell = new PdfPCell(t1a.DefaultCell);
				cell.AddElement(ae);
				t1a.AddCell(ae);


				//-----Notice
				var t2 = new PdfPTable(1);
				t2.TotalWidth = 72f * 3f;
				t2.DefaultCell.Border = Rectangle.NO_BORDER;
				t2.AddCell(new Phrase("\nPrint Date: {0:d}   (id:{1} {2})".Fmt(DateTime.Now, ci.PeopleId, ci.CampusId), font));
				t2.AddCell("");
				string html2 = @"<p><i>
NOTE: No goods or services were provided to you by the church in connection with any contibution;
any value received consisted entirely of intangible religious benefits.
Bellevue Baptist Church, FEIN # 62-60017-10, is a 501(c)(3) organization and
qualifies as a part of the Southern Baptist Convention's group tax exemption ruling number GEN #1674.
</i></p>
<p> </p>
<p><i>
Thank you for your faithfulness in the giving of your time, talents, and resources. Together we can share the love of Jesus with our city.
</i></p>";
				content = Db.Contents.SingleOrDefault(c => c.Name == "StatementNotice");
				if (content != null)
					html2 = content.Body;
				list = HTMLWorker.ParseToList(new StringReader(html2), st);
				cell = new PdfPCell(t1.DefaultCell);
				for (int k = 0; k < list.Count; k++)
					cell.AddElement((IElement)list[k]);
				t2.AddCell(cell);


				// POSITIONING OF ADDRESSES
				//----Header
				var yp = doc.BottomMargin +
					Db.Setting("StatementRetAddrPos", "10.125").ToFloat() * 72f;
				t1.WriteSelectedRows(0, -1,
					doc.LeftMargin - 0.1875f * 72f, yp, dc);

				yp = doc.BottomMargin +
					Db.Setting("StatementAddrPos", "8.3375").ToFloat() * 72f;
				t1a.WriteSelectedRows(0, -1, doc.LeftMargin, yp, dc);

				yp = doc.BottomMargin + 10.125f * 72f;
				t2.WriteSelectedRows(0, -1, doc.LeftMargin + 72f * 4.4f, yp, dc);


				//----Contributions
				doc.Add(new Paragraph(" "));
				doc.Add(new Paragraph(" ") { SpacingBefore = 72f * 2.125f });

				doc.Add(new Phrase("\n  Period: {0:d} - {1:d}".Fmt(FromDate, toDate), boldfont));

			    var pos = w.GetVerticalPosition(true);

			    var ct = new ColumnText(dc);
                float gutter = 20f;
                float colwidth = (doc.Right - doc.Left - gutter) / 2;

				var t = new PdfPTable(new float[] { 10f, 24f, 10f });
				t.WidthPercentage = 100;
				t.DefaultCell.Border = Rectangle.NO_BORDER;
				t.HeaderRows = 2;

				cell = new PdfPCell(t.DefaultCell);
				cell.Colspan = 3;
				cell.Phrase = new Phrase("Contributions\n", boldfont);
				t.AddCell(cell);

				t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
				t.AddCell(new Phrase("Date", boldfont));
				t.AddCell(new Phrase("Description", boldfont));
				cell = new PdfPCell(t.DefaultCell);
				cell.HorizontalAlignment = Element.ALIGN_RIGHT;
				cell.Phrase = new Phrase("Amount", boldfont);
				t.AddCell(cell);

				t.DefaultCell.Border = Rectangle.NO_BORDER;

				var total = 0m;
				foreach (var c in APIContribution.contributions(Db, ci, FromDate, toDate))
				{
					t.AddCell(new Phrase(c.ContributionDate.FormatDate(), font));
					t.AddCell(new Phrase(c.Fund, font));
					cell = new PdfPCell(t.DefaultCell);
					cell.HorizontalAlignment = Element.ALIGN_RIGHT;
					cell.Phrase = new Phrase(c.ContributionAmount.ToString2("N2"), font);
					t.AddCell(cell);
					total += (c.ContributionAmount ?? 0);
				}
				t.DefaultCell.Border = Rectangle.TOP_BORDER;
				cell = new PdfPCell(t.DefaultCell);
				cell.Colspan = 2;
				cell.Phrase = new Phrase("Total Contributions for period", boldfont);
				t.AddCell(cell);
				cell = new PdfPCell(t.DefaultCell);
				cell.HorizontalAlignment = Element.ALIGN_RIGHT;
				cell.Phrase = new Phrase(total.ToString("N2"), font);
				t.AddCell(cell);

				ct.AddElement(t);


				//------Pledges
				var pledges = APIContribution.pledges(Db, ci, toDate).ToList();
				if (pledges.Count > 0)
				{
					t = new PdfPTable(new float[] { 16f, 12f, 12f });
					t.WidthPercentage = 100;
					t.DefaultCell.Border = Rectangle.NO_BORDER;
					t.HeaderRows = 2;

					cell = new PdfPCell(t.DefaultCell);
					cell.Colspan = 3;
					cell.Phrase = new Phrase("\n\nPledges\n", boldfont);
					t.AddCell(cell);

					t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
					t.AddCell(new Phrase("Fund", boldfont));
					cell = new PdfPCell(t.DefaultCell);
					cell.HorizontalAlignment = Element.ALIGN_RIGHT;
					cell.Phrase = new Phrase("Pledge", boldfont);
					t.AddCell(cell);
					cell = new PdfPCell(t.DefaultCell);
					cell.HorizontalAlignment = Element.ALIGN_RIGHT;
					cell.Phrase = new Phrase("Given", boldfont);
					t.AddCell(cell);

					t.DefaultCell.Border = Rectangle.NO_BORDER;

					foreach (var c in pledges)
					{
						t.AddCell(new Phrase(c.Fund, font));
						cell = new PdfPCell(t.DefaultCell);
						cell.HorizontalAlignment = Element.ALIGN_RIGHT;
						cell.Phrase = new Phrase(c.PledgeAmount.ToString2("N2"), font);
						t.AddCell(cell);
						cell = new PdfPCell(t.DefaultCell);
						cell.HorizontalAlignment = Element.ALIGN_RIGHT;
						cell.Phrase = new Phrase(c.ContributionAmount.ToString2("N2"), font);
						t.AddCell(cell);
					}
					ct.AddElement(t);
				}

				//------Gifts In Kind
				var giftsinkind = APIContribution.GiftsInKind(Db, ci, FromDate, toDate).ToList();
				if (giftsinkind.Count > 0)
				{
					t = new PdfPTable(new float[] { 12f, 18f, 20f });
					t.WidthPercentage = 100;
					t.DefaultCell.Border = Rectangle.NO_BORDER;
					t.HeaderRows = 2;

					cell = new PdfPCell(t.DefaultCell);
					cell.Colspan = 3;
					cell.Phrase = new Phrase("\n\nGifts in Kind\n", boldfont);
					t.AddCell(cell);

					t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
					t.AddCell(new Phrase("Date", boldfont));
					cell = new PdfPCell(t.DefaultCell);
					cell.Phrase = new Phrase("Fund", boldfont);
					t.AddCell(cell);
					cell = new PdfPCell(t.DefaultCell);
					cell.Phrase = new Phrase("Description", boldfont);
					t.AddCell(cell);

					t.DefaultCell.Border = Rectangle.NO_BORDER;

					foreach (var c in giftsinkind)
					{
						t.AddCell(new Phrase(c.ContributionDate.FormatDate(), font));
						cell = new PdfPCell(t.DefaultCell);
						cell.Phrase = new Phrase(c.Fund, font);
						t.AddCell(cell);
						cell = new PdfPCell(t.DefaultCell);
						cell.Phrase = new Phrase(c.Description, font);
						t.AddCell(cell);
					}
					ct.AddElement(t);
				}

				//-----Summary
				t = new PdfPTable(new float[] { 29f, 9f });
				t.WidthPercentage = 100;
				t.DefaultCell.Border = Rectangle.NO_BORDER;
				t.HeaderRows = 2;

				cell = new PdfPCell(t.DefaultCell);
				cell.Colspan = 2;
				cell.Phrase = new Phrase("\n\nPeriod Summary\n", boldfont);
				t.AddCell(cell);

				t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
				t.AddCell(new Phrase("Fund", boldfont));
				cell = new PdfPCell(t.DefaultCell);
				cell.HorizontalAlignment = Element.ALIGN_RIGHT;
				cell.Phrase = new Phrase("Amount", boldfont);
				t.AddCell(cell);

				t.DefaultCell.Border = Rectangle.NO_BORDER;
				foreach (var c in APIContribution.quarterlySummary(Db, ci, FromDate, toDate))
				{
					t.AddCell(new Phrase(c.Fund, font));
					cell = new PdfPCell(t.DefaultCell);
					cell.HorizontalAlignment = Element.ALIGN_RIGHT;
					cell.Phrase = new Phrase(c.ContributionAmount.ToString2("N2"), font);
					t.AddCell(cell);
				}
				t.DefaultCell.Border = Rectangle.TOP_BORDER;
				t.AddCell(new Phrase("Total contributions for period", boldfont));
				cell = new PdfPCell(t.DefaultCell);
				cell.HorizontalAlignment = Element.ALIGN_RIGHT;
				cell.Phrase = new Phrase(total.ToString("N2"), font);
				t.AddCell(cell);
				ct.AddElement(t);

			    var col = 0;
                var status = 0;
                while(ColumnText.HasMoreText(status))
                {
                    var leftcol = new Rectangle(doc.Left, doc.Bottom, doc.Left + colwidth, pos);
                    var rightcol = new Rectangle(doc.Right - colwidth, doc.Bottom, doc.Right, pos);
                    if (col == 0)
                        ct.SetSimpleColumn(leftcol);
                    else if(col == 1)
                        ct.SetSimpleColumn(rightcol);
                    status = ct.Go();
                    ++col;
                    if (col > 1)
                    {
                        col = 0;
                        pos = doc.Top;
                        doc.NewPage();
                    }
                }

				runningtotals.Processed += 1;
				runningtotals.CurrSet = set;
				Db.SubmitChanges();
			}

            if (count == 0)
            {
                doc.NewPage();
                doc.Add(new Phrase("no data"));
            }
			doc.Close();

			if (set == LastSet())
				runningtotals.Completed = DateTime.Now;
			Db.SubmitChanges();
		}
		class PageEvent : PdfPageEventHelper
		{
            class NPages
            {
                public NPages(PdfContentByte dc)
                {
                    template = dc.CreateTemplate(50, 50);
                }
                public bool juststartednewset;
                public PdfTemplate template;
                public int n;
            }
            private NPages npages;
            private int pg;

            private PdfWriter writer;
			private Document document;
			private PdfContentByte dc;
			private BaseFont font;
		    private int recentpage;
			public int set { get; set; }

			public Dictionary<int, int> FamilySet { get; set; }

			public override void OnOpenDocument(PdfWriter writer, Document document)
			{
				this.writer = writer;
				this.document = document;
				base.OnOpenDocument(writer, document);
				font = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
				dc = writer.DirectContent;
				if (set == 0)
					FamilySet = new Dictionary<int, int>();
                npages = new NPages(dc);
			}
            public void EndPageSet()
            {
                if (npages == null)
                    return;
                npages.template.BeginText();
                npages.template.SetFontAndSize(font, 8);
                npages.template.ShowText(npages.n.ToString());
                if (set == 0)
                {
                        var list = FamilySet.Where(kp => kp.Value == 0).ToList();
                        foreach (var kp in list)
                            if (kp.Value == 0)
                                FamilySet[kp.Key] = npages.n;
                    
                }
                pg = 1;
                npages.template.EndText();
                npages = new NPages(dc);
            }
            public void StartPageSet()
            {
                npages.juststartednewset = true;
            }
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                base.OnEndPage(writer, document);
                if (npages.juststartednewset)
                    EndPageSet();

                string text;
                float len;

                text = "Page " + (pg) + " of ";
                len = font.GetWidthPoint(text, 8);
                dc.BeginText();
                dc.SetFontAndSize(font, 8);
                dc.SetTextMatrix(30, 30);
                dc.ShowText(text);
                dc.EndText();
                dc.AddTemplate(npages.template, 30 + len, 30);
                npages.n = pg++;
            }
            public override void OnCloseDocument(PdfWriter writer, Document document)
            {
                base.OnCloseDocument(writer, document);
                EndPageSet();
            }
		}
	}
}

