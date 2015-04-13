﻿using System;
using System.Globalization;
using System.IO;
using CmsData;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace CmsWeb.Areas.Finance.Models.BatchImport
{
    /// <summary>
    /// See https://www.regaltek.com/docs/NACHA%20Format.pdf for good overview of ACH format.
    /// </summary>
    internal class AchImporter : IContributionBatchImporter
    {
        private BundleHeader _bundleHeader;
        private DateTime _batchDate;
        private int _fundId;
        
        public int? RunImport(string text, DateTime date, int? fundid, bool fromFile)
        {
            _fundId = fundid ?? BatchImportContributions.FirstFundId();

            using (var sr = new StringReader(text))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    HandleRecord(line);
                }
            }

            BatchImportContributions.FinishBundle(_bundleHeader);
            return _bundleHeader.BundleHeaderId;
        }

        private void HandleRecord(string line)
        {
            var recordType = (RecordType) int.Parse(line.Substring(0, 1));
            switch (recordType)
            {
                case RecordType.BatchHeader:
                    ParseBatchHeader(line);
                    break;
                case RecordType.EntryDetail:
                    ParseEntryDetail(line);
                    break;
                case RecordType.BatchControlTotal:
                    ParseBatchControlTotal(line);
                    break;
            }
        }

        private void ParseBatchHeader(string line)
        {
            var companyName = line.Substring(4, 16).Trim();
            var discretionaryData = line.Substring(20, 20).Trim();
            _batchDate = DateTime.ParseExact(line.Substring(69, 6).Trim(), "yyMMdd", CultureInfo.InvariantCulture);
            var bankBatchNumber = int.Parse(line.Substring(87, 7).Trim());

            _bundleHeader = BatchImportContributions.GetBundleHeader(_batchDate, DateTime.Now);
        }

        private void ParseEntryDetail(string line)
        {
            var transactionCode = line.Substring(1, 2).Trim();
            var routingNumber = line.Substring(3, 8).Trim();
            var accountNumber = line.Substring(12, 17).Trim();
            var amountWithoutDecimal = line.Substring(29, 10).Trim();
            var individualIdNumber = line.Substring(39, 15).Trim();
            var name = line.Substring(54, 22).Trim();
            var traceNumber = line.Substring(79, 15).Trim();

            var dollars = amountWithoutDecimal.Substring(0, amountWithoutDecimal.Length - 2);
            var cents = amountWithoutDecimal.Substring(amountWithoutDecimal.Length - 2);

            var amount = string.Format("{0}.{1}", dollars, cents);

            var detail = BatchImportContributions.AddContributionDetail(_batchDate, _fundId, amount, traceNumber,
                routingNumber,
                accountNumber);

            _bundleHeader.BundleDetails.Add(detail);
        }

        private static void ParseBatchControlTotal(string line)
        {
            var entryCount = line.Substring(4, 6).Trim();
            var totalDebitAmount = line.Substring(20, 12).Trim();
            var totalCreditAmount = line.Substring(32, 12).Trim();
            var company = line.Substring(44, 10).Trim();
            var batchNumber = line.Substring(87, 7).Trim();
        }

        private enum RecordType
        {
            FileHeader = 1,
            BatchHeader = 5,
            EntryDetail = 6,
            EntryDetailAddendum = 7,
            BatchControlTotal = 8,
            FileControlRecord = 9
        }
    }
}