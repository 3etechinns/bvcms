/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using CMSPresenter;
using System.Text;
using System.Web.Mvc;
using CmsWeb.Models;
using UtilityExtensions;

namespace CmsWeb.Areas.Main.Models.Report
{
    public class AveryAddressResult : ActionResult
    {
        public int? id;
        public string format;
        public bool? titles; 
        public bool usephone { get; set; }
		public int skip = 0;

        const float H = 72f;
        const float W = 197f;
        private Font font = FontFactory.GetFont(FontFactory.HELVETICA, 10);
        private Font smfont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

        protected PdfContentByte dc;

        public override void ExecuteResult(ControllerContext context)
        {
            var ctl = new MailingController { UseTitles = titles == true };
            var Response = context.HttpContext.Response;

            const string STR_Name = "Name";
            IEnumerable<MailingInfo> q = null;
            switch (format)
            {
                case "Individual":
                    q = ctl.FetchIndividualList(STR_Name, id.Value);
                    break;
                case "Family":
                case "FamilyMembers":
                    q = ctl.FetchFamilyList(STR_Name, id.Value);
                    break;
                case "ParentsOf":
                    q = ctl.FetchParentsOfList(STR_Name, id.Value);
                    break;
                case "CouplesEither":
                    q = ctl.FetchCouplesEitherList(STR_Name, id.Value);
                    break;
                case "CouplesBoth":
                    q = ctl.FetchCouplesBothList(STR_Name, id.Value);
                    break;
                default:
                    Response.Write("unknown format");
                    return;
            }
            if (!q.Any())
            {
                Response.Write("no data found");
                return;
            }
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "filename=foo.pdf");

            var document = new Document(PageSize.LETTER);
            document.SetMargins(50f, 36f, 32f, 36f);
            var w = PdfWriter.GetInstance(document, Response.OutputStream);
            document.Open();
            dc = w.DirectContent;

            var cols = new float[] { W, W, W - 25f };
            var t = new PdfPTable(cols);
            t.SetTotalWidth(cols);
            t.HorizontalAlignment = Element.ALIGN_CENTER;
            t.LockedWidth = true;
            t.DefaultCell.Border = PdfPCell.NO_BORDER;
            t.DefaultCell.FixedHeight = H;
            t.DefaultCell.VerticalAlignment = PdfPCell.ALIGN_MIDDLE;
            t.DefaultCell.PaddingLeft = 8f;
            t.DefaultCell.PaddingRight = 8f;
            t.DefaultCell.SetLeading(2.0f, 1f);

			if (skip > 0)
			{
				var blankCell = new PdfPCell(t.DefaultCell);

				for (int iX = 0; iX < skip; iX++)
				{
					t.AddCell(blankCell);
				}
			}

            foreach (var m in q)
            {
                var c = new PdfPCell(t.DefaultCell);
                var ph = new Paragraph();
                ph.AddLine(m.LabelName, font);
                ph.AddLine(m.Address, font);
                ph.AddLine(m.Address2, font);
                ph.AddLine(m.CityStateZip, font);
                c.AddElement(ph);
                if (usephone)
                {
                    var phone = Util.PickFirst(m.CellPhone.FmtFone("C "), m.HomePhone.FmtFone("H "));
                    var p = new Paragraph();
                    c.PaddingRight = 7f;
                    p.Alignment = Element.ALIGN_RIGHT;
                    p.Add(new Chunk(phone, smfont));
                    p.ExtraParagraphSpace = 0f;
                    c.AddElement(p);
                }
                t.AddCell(c);
            }
            t.CompleteRow();
            document.Add(t);

			document.Close();
        }
    }
}