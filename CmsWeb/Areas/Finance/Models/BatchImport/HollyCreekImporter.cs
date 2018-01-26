/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */

using System;
using System.IO;
using CmsData;
using LumenWorks.Framework.IO.Csv;

namespace CmsWeb.Areas.Finance.Models.BatchImport
{
    internal class HollyCreekImporter : IContributionBatchImporter
    {
        public int? RunImport(string text, DateTime date, string fundid, bool fromFile)
        {
            using (var csv = new CsvReader(new StringReader(text), true))
                return BatchProcessHollyCreek(csv, date, fundid);
        }

        private static int? BatchProcessHollyCreek(CsvReader csv, DateTime date, string fundid)
        {
            BundleHeader bh = null;
            var firstfund = BatchImportContributions.FirstFundId();
            var fund = fundid ?? firstfund;

            // 0 Amount, 1 Account, 2 Serial, 3 RoutingNumber, 4 TransmissionDate, 5 DepositTotal"))
            while (csv.ReadNextRecord())
            {
                var amount = csv[0];
                var account = csv[1];
                var checkno = csv[2];
                var routing = csv[3];
                if (bh == null)
                    bh = BatchImportContributions.GetBundleHeader(date, DateTime.Now);
                var bd = BatchImportContributions.AddContributionDetail(date, fund, amount, checkno, routing, account);
                bh.BundleDetails.Add(bd);
            }
            if (bh == null)
                return null;
            BatchImportContributions.FinishBundle(bh);
            return bh.BundleHeaderId;
        }
    }
}
