﻿/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Collections;
using CmsData;
using UtilityExtensions;
using CMSPresenter;
using System.Text;
using System.Text.RegularExpressions;

namespace CMSWeb.Reports
{
    public partial class AttendanceReport : System.Web.UI.Page
    {
        private Font monofont = FontFactory.GetFont(FontFactory.COURIER, 8);
        private Font boldfont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
        private Font font = FontFactory.GetFont(FontFactory.HELVETICA, 10);
        private Font smallfont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new GrayColor(50));
        private Font xsmallfont = FontFactory.GetFont(FontFactory.HELVETICA, 7, new GrayColor(50));
        private PageEvent pageEvents = new PageEvent();
        private PdfPTable MainTable;
        private Document doc;
        private DateTime dt;
        private PdfContentByte dc;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "filename=foo.pdf");

            var qid = this.QueryString<int?>("id");
            dt = Util.Now;

            doc = new Document(PageSize.LETTER.Rotate(), 36, 36, 64, 64);
            var w = PdfWriter.GetInstance(doc, Response.OutputStream);
            w.PageEvent = pageEvents;
            doc.Open();
            dc = w.DirectContent;

            var ctl = new RollsheetController();

            StartPageSet();
            if (qid.HasValue) // print using a query
            {
                var qB = DbUtil.Db.LoadQueryById(qid.Value);
                var q = from p in DbUtil.Db.People.Where(qB.Predicate())
                        orderby p.Name2
                        select p;
                foreach (var p in q)
                    AddRow(p);
                if (MainTable.Rows.Count > 1)
                    doc.Add(MainTable);
                else
                    doc.Add(new Phrase("no data"));
            }
            else
                doc.Add(new Phrase("no data"));
            pageEvents.EndPageSet();
            doc.Close();
            Response.End();
        }

        float[] HeaderWids = new float[] { 40 + 70 + 80, 40 + 130 };
        float[] LeftWids = new float[] { 40, 70, 80 };
        float[] RightWids = new float[] { 40, 130 };
        int border = PdfPCell.NO_BORDER; //PdfPCell.BOX;

        private void StartPageSet()
        {
            MainTable = new PdfPTable(HeaderWids);
            MainTable.WidthPercentage = 100;
            MainTable.DefaultCell.Border = border;
            MainTable.DefaultCell.Padding = 5;
            MainTable.HeaderRows = 1;
            pageEvents.StartPageSet("Attendance Report: {0:d}".Fmt(dt));

            var LeftTable = new PdfPTable(LeftWids);
            LeftTable.WidthPercentage = 100;
            LeftTable.DefaultCell.Border = border;
            LeftTable.DefaultCell.Padding = 5;
            LeftTable.AddCell(new Phrase("\nPerson", boldfont));
            LeftTable.AddCell(new Phrase("\nAddress", boldfont));
            LeftTable.AddCell(new Phrase("Phone\nEmail", boldfont));
            var cell = new PdfPCell(MainTable.DefaultCell);
            cell.AddElement(LeftTable);
            cell.Padding = 0;
            MainTable.AddCell(cell);

            var RightTable = new PdfPTable(RightWids);
            RightTable.WidthPercentage = 100;
            RightTable.DefaultCell.Border = border;
            RightTable.DefaultCell.Padding = 5;
            RightTable.DefaultCell.PaddingBottom = 0;
            RightTable.AddCell(new Phrase("\nBirthday", boldfont));
            RightTable.AddCell(new Phrase("\nMember Status", boldfont));
            cell = new PdfPCell(MainTable.DefaultCell);
            cell.Padding = 0;
            cell.AddElement(RightTable);
            MainTable.AddCell(cell);
        }
        private void AddRow(Person p)
        {
            if (MainTable.Rows.Count % 2 == 0)
                MainTable.DefaultCell.BackgroundColor = new GrayColor(240);
            else
                MainTable.DefaultCell.BackgroundColor = Color.WHITE;

            var LeftTable = new PdfPTable(LeftWids);
            LeftTable.WidthPercentage = 100;
            LeftTable.DefaultCell.Border = border;
            LeftTable.DefaultCell.Padding = 5;
            var name = new Phrase(p.Name + "\n", font);
            name.Add(new Chunk("  ({0})".Fmt(p.PeopleId), smallfont));
            LeftTable.AddCell(name);
            var addr = new StringBuilder(p.PrimaryAddress);
            AddLine(addr, p.PrimaryAddress2);
            AddLine(addr, "{0}, {1} {2}".Fmt(p.PrimaryCity, p.PrimaryState, p.PrimaryZip.FmtZip()));
            LeftTable.AddCell(new Phrase(addr.ToString(), font));
            var phones = new StringBuilder();
            AddPhone(phones, p.HomePhone, "h ");
            AddPhone(phones, p.CellPhone, "c ");
            AddPhone(phones, p.WorkPhone, "w ");
            AddLine(phones, p.EmailAddress);
            LeftTable.AddCell(new Phrase(phones.ToString(), font));

            // attendance string, dates
            var AttendCell = new PdfPCell(MainTable.DefaultCell);
            AttendCell.AddElement(GetAttendance(p));
            AttendCell.Colspan = 3;
            LeftTable.AddCell(AttendCell);
            AttendCell = new PdfPCell(MainTable.DefaultCell);
            AttendCell.Padding = 0;
            AttendCell.AddElement(LeftTable);
            MainTable.AddCell(AttendCell);

            var RightTable = new PdfPTable(RightWids);
            RightTable.WidthPercentage = 100;
            RightTable.DefaultCell.Border = border;
            RightTable.DefaultCell.Padding = 5;
            RightTable.DefaultCell.PaddingBottom = 0;
            RightTable.AddCell(new Phrase(p.DOB, font));
            RightTable.AddCell(new Phrase(p.MemberStatus.Description));

            var cell = new PdfPCell(MainTable.DefaultCell);
            cell.Padding = 0;
            cell.AddElement(RightTable);
            MainTable.AddCell(cell);
        }
        private void AddLine(StringBuilder sb, string value)
        {
            AddLine(sb, value, String.Empty);
        }
        private void AddLine(StringBuilder sb, string value, string postfix)
        {
            if(value.HasValue())
            {
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append(value);
                if(postfix.HasValue())
                    sb.Append(postfix);
            }
        }
        private void AddPhone(StringBuilder sb, string value, string prefix)
        {
            if (value.HasValue())
            {
                value = value.FmtFone(prefix);
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append(value);
            }
        }

        private Paragraph GetAttendance(Person p)
        {
            var q = from a in p.Attends
                    where a.AttendanceFlag == true
                    orderby a.MeetingDate.Date descending
                    group a by a.MeetingDate.Date into g
                    select g.Key;
            var list = q.ToList();

            var attstr = new StringBuilder("\n");
            var dt = Util.Now;
            var yearago = dt.AddYears(-1);
            while (dt > yearago)
            {
                var dt2 = dt.AddDays(-7);
                var indicator = ".";
                foreach (var d in list)
                {
                    if (d < dt2)
                        break;
                    if (d <= dt)
                    {
                        indicator = "P";
                        break;
                    }
                }
                attstr.Insert(0, indicator);
                dt = dt2;
            }
            var ph = new Paragraph(attstr.ToString(), monofont);
            ph.SetLeading(0, 1.2f);

            attstr = new StringBuilder();
            foreach (var d in list.Take(8))
                attstr.Insert(0, "{0:M/d/yy}  ".Fmt(d));
            if (list.Count > 8)
            {
                attstr.Insert(0, "...  ");
                var q2 = q.OrderBy(d => d).Take(Math.Min(list.Count - 8, 3));
                foreach (var d in q2.OrderByDescending(d => d))
                    attstr.Insert(0, "{0:M/d/yy}  ".Fmt(d));
            }
            ph.Add(new Chunk(attstr.ToString(), smallfont));
            return ph;
        }
        class PageEvent : PdfPageEventHelper
        {
            private PdfTemplate npages;
            private PdfWriter writer;
            private Document document;
            private PdfContentByte dc;
            private BaseFont font;
            private string HeadText;

            public override void OnOpenDocument(PdfWriter writer, Document document)
            {
                this.writer = writer;
                this.document = document;
                base.OnOpenDocument(writer, document);
                font = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                dc = writer.DirectContent;
            }
            public void EndPageSet()
            {
                if (npages == null)
                    return;
                npages.BeginText();
                npages.SetFontAndSize(font, 8);
                npages.ShowText((writer.PageNumber + 1).ToString());
                npages.EndText();
            }
            public void StartPageSet(string header1)
            {
                EndPageSet();
                document.NewPage();
                document.ResetPageCount();
                this.HeadText = header1;
                npages = dc.CreateTemplate(50, 50);
            }
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                base.OnEndPage(writer, document);

                string text;
                float len;

                //---Header left
                text = HeadText;
                const float HeadFontSize = 11f;
                len = font.GetWidthPoint(text, HeadFontSize);
                dc.BeginText();
                dc.SetFontAndSize(font, HeadFontSize);
                dc.SetTextMatrix(30, document.PageSize.Height - 30);
                dc.ShowText(text);
                dc.EndText();
                //dc.BeginText();
                //dc.SetFontAndSize(font, HeadFontSize);
                //dc.SetTextMatrix(30, document.PageSize.Height - 30 - (HeadFontSize * 1.5f));
                //dc.ShowText(HeadText2);
                //dc.EndText();

                //---Column 1
                text = "Page " + (writer.PageNumber + 1) + " of ";
                len = font.GetWidthPoint(text, 8);
                dc.BeginText();
                dc.SetFontAndSize(font, 8);
                dc.SetTextMatrix(30, 30);
                dc.ShowText(text);
                dc.EndText();
                dc.AddTemplate(npages, 30 + len, 30);

                //---Column 2
                text = HeadText;
                len = font.GetWidthPoint(text, 8);
                dc.BeginText();
                dc.SetFontAndSize(font, 8);
                dc.SetTextMatrix(document.PageSize.Width / 2 - len / 2, 30);
                dc.ShowText(text);
                dc.EndText();

                //---Column 3
                text = Util.Now.ToShortDateString();
                len = font.GetWidthPoint(text, 8);
                dc.BeginText();
                dc.SetFontAndSize(font, 8);
                dc.SetTextMatrix(document.PageSize.Width - 30 - len, 30);
                dc.ShowText(text);
                dc.EndText();
            }
            private float PutText(string text, BaseFont font, float size, float x, float y)
            {
                dc.BeginText();
                dc.SetFontAndSize(font, size);
                dc.SetTextMatrix(x, y);
                dc.ShowText(text);
                dc.EndText();
                return font.GetWidthPoint(text, size);
            }
        }
    }
}
