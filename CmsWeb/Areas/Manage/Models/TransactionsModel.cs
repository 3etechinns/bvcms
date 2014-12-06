using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using CmsData;
using CmsData.Finance;
using CmsData.View;
using MoreLinq;
using UtilityExtensions;

namespace CmsWeb.Models
{
    public class TransactionsModel
    {
        public string description { get; set; }
        public string name { get; set; }
        public string Submit { get; set; }
        public decimal? gtamount { get; set; }
        public decimal? ltamount { get; set; }
        public DateTime? startdt { get; set; }
        public DateTime? enddt { get; set; }
        public bool testtransactions { get; set; }
        public bool apprtransactions { get; set; }
        public bool nocoupons { get; set; }
        public string batchref { get; set; }
        public bool usebatchdates { get; set; }
        public PagerModel2 Pager { get; set; }
        int? _count;
        public int Count()
        {
            if (!_count.HasValue)
                _count = FetchTransactions().Count();
            return _count.Value;
        }
        public bool finance { get; set; }
        public bool admin { get; set; }
        public int? GoerId { get; set; } // for mission trip supporters of this goer
        public int? SenderId { get; set; } // for mission trip goers of this supporter

        public TransactionsModel(int? tranid)
            : this()
        {
            this.name = tranid.ToString();
            if (!tranid.HasValue)
                GoerId = null;
        }
        public TransactionsModel()
        {
            Pager = new PagerModel2(Count);
            Pager.Sort = "Date";
            Pager.Direction = "desc";
            finance = HttpContext.Current.User.IsInRole("Finance");
            admin = HttpContext.Current.User.IsInRole("Admin") || HttpContext.Current.User.IsInRole("ManageTransactions");
        }
        public IEnumerable<TransactionList> Transactions()
        {
            var q0 = ApplySort();
            q0 = q0.Skip(Pager.StartRow).Take(Pager.PageSize);
            return q0;
        }

        public class TotalTransaction
        {
            public int Count { get; set; }
            public decimal Amt { get; set; }
            public decimal Amtdue { get; set; }
            public decimal Donate { get; set; }
        }

        public TotalTransaction TotalTransactions()
        {
            var q0 = FetchTransactions();
            var q = from t in q0
                    group t by 1 into g
                    select new TotalTransaction()
                    {
                        Amt = g.Sum(tt => tt.Amt ?? 0),
                        Amtdue = g.Sum(tt => tt.Amtdue ?? 0),
                        Donate = g.Sum(tt => tt.Donate ?? 0),
                        Count = g.Count()
                    };
            return q.FirstOrDefault();
        }

        private IQueryable<TransactionList> _transactions;
        private IQueryable<TransactionList> FetchTransactions()
        {
            if (_transactions != null)
                return _transactions;
            if (!name.HasValue())
                name = null;
            string first, last;
            Util.NameSplit(name, out first, out last);
            var hasfirst = first.HasValue();
            var nameid = name.ToInt();
            _transactions
               = from t in DbUtil.Db.ViewTransactionLists
                 let donate = t.Donate ?? 0
                 where t.Amt > gtamount || gtamount == null
                 where t.Amt <= ltamount || ltamount == null
                 where description == null || t.Description.Contains(description)
                 where nameid > 0 || ((t.Testing ?? false) == testtransactions)
                 where apprtransactions == (t.Moneytran == true) || !apprtransactions
                 where (nocoupons && !t.TransactionId.Contains("Coupon")) || !nocoupons
                 where (t.Financeonly ?? false) == false || finance
                 select t;
            if (name != null)
                _transactions = from t in _transactions
                                where
                                    (
                                        (t.Last.StartsWith(last) || t.Last.StartsWith(name))
                                        && (!hasfirst || t.First.StartsWith(first) || t.Last.StartsWith(name))
                                    )
                                    || t.Batchref == name || t.TransactionId == name || t.OriginalId == nameid || t.Id == nameid
                                select t;
            if (!HttpContext.Current.User.IsInRole("Finance"))
                _transactions = _transactions.Where(tt => (tt.Financeonly ?? false) == false);

            var edt = enddt;
            if (!edt.HasValue && startdt.HasValue)
                edt = startdt;
            if (edt.HasValue)
                edt = edt.Value.AddHours(24);
            if (usebatchdates && startdt.HasValue)
            {
                CheckBatchDates(startdt.Value, edt.Value);
                _transactions = from t in _transactions
                                where t.Batch >= startdt || startdt == null
                                where t.Batch <= edt || edt == null
                                where t.Moneytran == true
                                select t;
            }
            else
                _transactions = from t in _transactions
                                where t.TransactionDate >= startdt || startdt == null
                                where t.TransactionDate <= edt || edt == null
                                select t;
            //			var q0 = _transactions.ToList();
            //            foreach(var t in q0)
            //                Debug.WriteLine("\"{0}\"\t{1}\t{2}", t.Description, t.Id, t.Amt);
            return _transactions;
        }

        public class BatchTranGroup
        {
            public int count { get; set; }
            public DateTime? batchdate { get; set; }
            public string BatchRef { get; set; }
            public string BatchType { get; set; }
            public decimal Total { get; set; }
        }

        public IQueryable<BatchTranGroup> FetchBatchTransactions()
        {
            var q = from t in FetchTransactions()
                    group t by t.Batchref into g
                    orderby g.First().Batch descending
                    select new BatchTranGroup()
                    {
                        count = g.Count(),
                        batchdate = g.Max(gg => gg.Batch),
                        BatchRef = g.Key,
                        BatchType = g.First().Batchtyp,
                        Total = g.Sum(gg => gg.Amt ?? 0)
                    };
            return q;
        }
        public class DescriptionGroup
        {
            public int count { get; set; }
            public string Description { get; set; }
            public decimal Total { get; set; }
        }
        public class BatchDescriptionGroup
        {
            public int count { get; set; }
            public DateTime? batchdate { get; set; }
            public string BatchRef { get; set; }
            public string BatchType { get; set; }
            public string Description { get; set; }
            public decimal Total { get; set; }
        }
        public IEnumerable<DescriptionGroup> FetchTransactionsByDescription()
        {
            var q0 = FetchTransactions();
            var q = from t in q0
                    group t by t.Description into g
                    orderby g.First().Batch descending
                    select new DescriptionGroup()
                    {
                        count = g.Count(),
                        Description = g.Key,
                        Total = g.Sum(gg => (gg.Amt ?? 0) - (gg.Donate ?? 0))
                    };
            return q;
        }
        public IQueryable<BatchDescriptionGroup> FetchTransactionsByBatchDescription()
        {
            var q = from t in FetchTransactions()
                    group t by new { t.Batchref, t.Description } into g
                    let f = g.First()
                    orderby f.Batch, f.Description descending
                    select new BatchDescriptionGroup()
                    {
                        count = g.Count(),
                        batchdate = f.Batch,
                        BatchRef = f.Batchref,
                        BatchType = f.Batchtyp,
                        Description = f.Description,
                        Total = g.Sum(gg => (gg.Amt ?? 0) - (gg.Donate ?? 0))
                    };
            return q;
        }

        
        private void CheckBatchDates(DateTime start, DateTime end)
        {
            var gateway = DbUtil.Db.Gateway();
            if (!gateway.CanGetSettlementDates)
                return;

            var response = gateway.GetBatchDetails(start, end);

            // get distinct batches
            var allBatchReferences = (from batchTran in response.BatchTransactions
                                      select batchTran.BatchReference).Distinct();

            // first filter out batches that we have already been updated or inserted.
            // now find unmatched batch references
            var unmatchedBatchReferences = allBatchReferences.Where(br => !DbUtil.Db.CheckedBatches.Any(tt => tt.BatchRef == br)).ToList();

            // given unmatched batch references, get the matched batch transactions again
            var unMatchedBatchTransactions =
                response.BatchTransactions.Where(x => unmatchedBatchReferences.Contains(x.BatchReference)).ToList();


            var batchTypes = unMatchedBatchTransactions.Select(x => x.BatchType).Distinct();

            foreach (var batchType in batchTypes)
            {

                // key it by transaction reference and payment type.
                var unMatchedKeyedByReference = unMatchedBatchTransactions.Where(x => x.BatchType == batchType).ToDictionary(x => x.Reference, x => x);

                // next let's get all the approved matching transactions from our transaction table by transaction id (reference).
                var approvedMatchingTransactions = from transaction in DbUtil.Db.Transactions
                                                   where unMatchedKeyedByReference.Keys.Contains(transaction.TransactionId)
                                                   where transaction.PaymentType == (batchType == BatchType.Ach ? PaymentType.Ach : PaymentType.CreditCard)
                                                   where transaction.Approved == true
                                                   select transaction;

                // next key the matching approved transactions that came from our transaction table by the transaction id (reference).
                var approvedMatchingTransactionsKeyedByTransactionId = approvedMatchingTransactions.ToDictionary(x => x.TransactionId, x => x);

                // finally let's get a list of all transactions that need to be inserted, which we don't already have.
                var transactionsToInsert = from transaction in unMatchedKeyedByReference
                                           where !approvedMatchingTransactionsKeyedByTransactionId.Keys.Contains(transaction.Key)
                                           select transaction.Value;

                var notbefore = DateTime.Parse("6/1/12"); // the date when Sage payments began in BVCMS (?)

                // spin through each transaction and insert them to the transaction table.
                foreach (var transactionToInsert in transactionsToInsert)
                {
                    // get the original transaction.
                    var originalTransaction = DbUtil.Db.Transactions.SingleOrDefault(t => t.TransactionId == transactionToInsert.Reference && transactionToInsert.TransactionDate >= notbefore && t.PaymentType == (batchType == BatchType.Ach ? PaymentType.Ach : PaymentType.CreditCard));

                    // get the first and last name.
                    string first, last;
                    Util.NameSplit(transactionToInsert.Name, out first, out last);

                    // get the settlement date, however we are not exactly sure why we add four hours to the settlement date.
                    // we think it is to handle all timezones and push to the next day??
                    var settlementDate = AdjustSettlementDateForAllTimeZones(transactionToInsert.SettledDate);

                    // insert the transaction record.
                    DbUtil.Db.Transactions.InsertOnSubmit(new Transaction
                    {
                        Name = transactionToInsert.Name,
                        First = first,
                        Last = last,
                        TransactionId = transactionToInsert.Reference,
                        Amt = transactionToInsert.TransactionType == TransactionType.Credit ||
                              transactionToInsert.TransactionType == TransactionType.Refund
                                ? -transactionToInsert.Amount
                                : transactionToInsert.Amount,
                        Approved = transactionToInsert.Approved,
                        Message = transactionToInsert.Message,
                        TransactionDate = transactionToInsert.TransactionDate,
                        TransactionGateway = gateway.GatewayType,
                        Settled = settlementDate,
                        Batch = settlementDate,  // this date now will be the same as the settlement date.
                        Batchref = transactionToInsert.BatchReference,
                        Batchtyp = transactionToInsert.BatchType == BatchType.Ach ? "eft" : "bankcard",
                        OriginalId = originalTransaction != null ? (originalTransaction.OriginalId ?? originalTransaction.Id) : (int?)null,
                        Fromsage = true,
                        Description = originalTransaction != null ? originalTransaction.Description : "no description from {0}, id={1}".Fmt(gateway.GatewayType, transactionToInsert.TransactionId),
                        PaymentType = transactionToInsert.BatchType == BatchType.Ach ? PaymentType.Ach : PaymentType.CreditCard,
                        LastFourCC = transactionToInsert.BatchType == BatchType.CreditCard ? transactionToInsert.LastDigits : null,
                        LastFourACH = transactionToInsert.BatchType == BatchType.Ach ? transactionToInsert.LastDigits : null
                    });
                }

                // next update existing transactions with new batch data if there are any.
                foreach (var existingTransaction in approvedMatchingTransactions)
                {
                    if (!unMatchedKeyedByReference.ContainsKey(existingTransaction.TransactionId))
                        continue;

                    // first get the matching batch transaction.
                    var batchTransaction = unMatchedKeyedByReference[existingTransaction.TransactionId];

                    // get the adjusted settlement date
                    var settlementDate = AdjustSettlementDateForAllTimeZones(batchTransaction.SettledDate);

                    existingTransaction.Batch = settlementDate;  // this date now will be the same as the settlement date.
                    existingTransaction.Batchref = batchTransaction.BatchReference;
                    existingTransaction.Batchtyp = batchTransaction.BatchType == BatchType.Ach ? "eft" : "bankcard";
                    existingTransaction.Settled = settlementDate;
                    existingTransaction.PaymentType = batchTransaction.BatchType == BatchType.Ach ? PaymentType.Ach : PaymentType.CreditCard;
                    existingTransaction.LastFourCC = batchTransaction.BatchType == BatchType.CreditCard ? batchTransaction.LastDigits : null;
                    existingTransaction.LastFourACH = batchTransaction.BatchType == BatchType.Ach ? batchTransaction.LastDigits : null;
                }
            }

            



            // finally we need to mark these batches as completed if there are any.
            foreach (var batch in unMatchedBatchTransactions.DistinctBy(x => x.BatchReference))
            {
                var checkedBatch = DbUtil.Db.CheckedBatches.SingleOrDefault(bb => bb.BatchRef == batch.BatchReference);
                if (checkedBatch == null)
                {
                    DbUtil.Db.CheckedBatches.InsertOnSubmit(
                        new CheckedBatch
                        {
                            BatchRef = batch.BatchReference,
                            CheckedX = DateTime.Now
                        });
                }
                else
                    checkedBatch.CheckedX = DateTime.Now;
            }
            
            DbUtil.Db.SubmitChanges();
        }

        /// <summary>
        /// we are not exactly sure why we add four hours to the settlement date
        /// we think it is to handle all timezones and push to the next day??
        /// </summary>
        /// <param name="settlementDate"></param>
        /// <returns></returns>
        private static DateTime AdjustSettlementDateForAllTimeZones(DateTime settlementDate)
        {
            return settlementDate.AddHours(4);
        }

        public IQueryable<TransactionList> ApplySort()
        {
            var q = FetchTransactions();
            if (Pager.Direction == "asc")
                switch (Pager.Sort)
                {
                    case "Id":
                        q = from t in q
                            orderby (t.OriginalId ?? t.Id), t.TransactionDate
                            select t;
                        break;
                    case "Tran Id":
                        q = from t in q
                            orderby t.TransactionId
                            select t;
                        break;
                    case "Appr":
                        q = from t in q
                            orderby t.Approved, t.TransactionDate descending
                            select t;
                        break;
                    case "Date":
                        q = from t in q
                            orderby t.TransactionDate
                            select t;
                        break;
                    case "Description":
                        q = from t in q
                            orderby t.Description, t.TransactionDate descending
                            select t;
                        break;
                    case "Name":
                        q = from t in q
                            orderby t.Name, t.First, t.Last, t.TransactionDate descending
                            select t;
                        break;
                    case "Amount":
                        q = from t in q
                            orderby t.Amt, t.TransactionDate descending
                            select t;
                        break;
                    case "Due":
                        q = from t in q
                            orderby t.TotDue, t.TransactionDate descending
                            select t;
                        break;
                }
            else
                switch (Pager.Sort)
                {
                    case "Id":
                        q = from t in q
                            orderby (t.OriginalId ?? t.Id) descending, t.TransactionDate descending
                            select t;
                        break;
                    case "Tran Id":
                        q = from t in q
                            orderby t.TransactionId descending
                            select t;
                        break;
                    case "Appr":
                        q = from t in q
                            orderby t.Approved descending, t.TransactionDate
                            select t;
                        break;
                    case "Date":
                        q = from t in q
                            orderby t.TransactionDate descending
                            select t;
                        break;
                    case "Description":
                        q = from t in q
                            orderby t.Description descending, t.TransactionDate
                            select t;
                        break;
                    case "Name":
                        q = from t in q
                            orderby t.Name descending, t.First descending, t.Last descending, t.TransactionDate
                            select t;
                        break;
                    case "Amount":
                        q = from t in q
                            orderby t.Amt descending, t.TransactionDate
                            select t;
                        break;
                    case "Due":
                        q = from t in q
                            orderby t.TotDue descending, t.TransactionDate
                            select t;
                        break;
                }

            return q;
        }
        public DataTable ExportTransactions()
        {
            var q = FetchTransactions();

            var q2 = from t in q
                     select new
                 {
                     t.Id,
                     t.TransactionId,
                     t.Approved,
                     TranDate = t.TransactionDate.FormatDate(),
                     BatchDate = t.Batch.FormatDate(),
                     t.Batchtyp,
                     t.Batchref,
                     RegAmt = (t.Amt ?? 0) - (t.Donate ?? 0),
                     Donate = t.Donate ?? 0,
                     TotalAmt = t.Amt ?? 0,
                     Amtdue = t.TotDue ?? 0,
                     t.Description,
                     t.Message,
                     FullName = Transaction.FullName(t),
                     t.Address,
                     t.City,
                     t.State,
                     t.Zip,
                     t.Fund
                 };
            return q2.ToDataTable();
        }

        public class SupporterInfo
        {
            public GoerSenderAmount gs { get; set; }
            public string Name { get; set; }
            public int PeopleId { get; set; }
        }
        public IQueryable<SupporterInfo> Supporters()
        {
            return from gs in DbUtil.Db.GoerSenderAmounts
                   where gs.GoerId == GoerId
                   where gs.SupporterId != gs.GoerId
                   let p = DbUtil.Db.People.Single(ss => ss.PeopleId == gs.SupporterId)
                   orderby gs.Created descending
                   select new SupporterInfo()
                   {
                       gs = gs,
                       Name = p.Name,
                       PeopleId = p.PeopleId
                   };
        }

        public IQueryable<GoerSenderAmount> SelfSupports()
        {
            return from gs in DbUtil.Db.GoerSenderAmounts
                   where gs.GoerId == GoerId
                   where gs.SupporterId == gs.GoerId
                   orderby gs.Created descending
                   select gs;
        }

        public IQueryable<SupporterInfo> SupportOthers()
        {
            return from gs in DbUtil.Db.GoerSenderAmounts
                   where gs.SupporterId == SenderId
                   where gs.SupporterId != gs.GoerId
                   let p = DbUtil.Db.People.Single(ss => ss.PeopleId == gs.GoerId)
                   orderby gs.Created descending
                   select new SupporterInfo()
                   {
                       gs = gs,
                       PeopleId = p.PeopleId,
                       Name = p.Name
                   };
        }

        public EpplusResult ToExcel()
        {
            return ExportTransactions().ToExcel("Transactions.xlsx");
        }
    }
}
