/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CmsWeb.Areas.Finance.Controllers;
using UtilityExtensions;
using CmsData;
using CmsData.Codes;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using LumenWorks.Framework.IO.Csv;

namespace CmsWeb.Models
{
	public class PostBundleModel
	{
		public class FundTotal
		{
			public string Name { get; set; }
			public decimal? Total { get; set; }
		}
		public int id { get; set; }
		public int? editid { get; set; }
		public string pid { get; set; }
		public decimal? amt { get; set; }
		public int? splitfrom { get; set; }
		public int fund { get; set; }
		public bool pledge { get; set; }
		public string notes { get; set; }
		public string checkno { get; set; }
		private BundleHeader _bundle;
		public string FundName { get; set; }
		public BundleHeader bundle
		{
			get
			{
				if (_bundle == null)
				{
					_bundle = DbUtil.Db.BundleHeaders.SingleOrDefault(bh => bh.BundleHeaderId == id);
					if (_bundle != null && _bundle.FundId.HasValue)
						FundName = _bundle.Fund.FundName;
				}
				return _bundle;
			}
		}
		public PostBundleModel()
		{

		}
		public PostBundleModel(int id)
		{
			this.id = id;
		}

		public IEnumerable<ContributionInfo> FetchContributions(int? cid = null)
		{
			var q = from d in DbUtil.Db.BundleDetails
					where d.BundleHeaderId == id || cid != null
					where cid == null || d.ContributionId == cid
					let sort = d.BundleSort1 > 0 ? d.BundleSort1 : d.BundleDetailId
					orderby sort descending, d.ContributionId ascending
					select new ContributionInfo
					{
						ContributionId = d.ContributionId,
						PeopleId = d.Contribution.PeopleId,
						Name = d.Contribution.Person.Name2
							 + (d.Contribution.Person.DeceasedDate.HasValue ? " [DECEASED]" : ""),
						Amt = d.Contribution.ContributionAmount,
						Fund = d.Contribution.ContributionFund.FundName,
						FundId = d.Contribution.FundId,
						Notes = d.Contribution.ContributionDesc,
						CheckNo = d.Contribution.CheckNo,
						eac = d.Contribution.BankAccount,
						Address = d.Contribution.Person.PrimaryAddress,
						City = d.Contribution.Person.PrimaryCity,
						State = d.Contribution.Person.PrimaryState,
						Zip = d.Contribution.Person.PrimaryZip,
						Age = d.Contribution.Person.Age,
						extra = d.Contribution.ExtraDatum.Data,
						pledge = d.Contribution.PledgeFlag,
						memstatus = d.Contribution.Person.MemberStatus.Description,
					};
			var list = q.ToList();
			foreach (var c in list)
			{
				string s = null;
				if (!c.PeopleId.HasValue)
				{
					s = c.extra ?? "";
					if (c.eac.HasValue())
						s += " (" + Util.Decrypt(c.eac) + ")";
					if (s.HasValue())
						c.Name = s;
				}
			}
			return list;
		}
		public IEnumerable<FundTotal> TotalsByFund()
		{
			var q = from d in DbUtil.Db.BundleDetails
					where d.BundleHeaderId == id
					group d by d.Contribution.ContributionFund.FundName into g
					orderby g.Key
					select new FundTotal
					{
						Name = g.Key,
						Total = g.Sum(d => d.Contribution.ContributionAmount)
					};
			return q;
		}
		public IEnumerable<SelectListItem> Funds()
		{
			var q = from f in DbUtil.Db.ContributionFunds
					where f.FundStatusId == 1
					orderby f.FundId
					select new SelectListItem
					{
						Text = "{0} - {1}".Fmt(f.FundId, f.FundName),
						Value = f.FundId.ToString()
					};
			return q;
		}
		public IEnumerable Funds2()
		{
			var q = from f in DbUtil.Db.ContributionFunds
					where f.FundStatusId == 1
					orderby f.FundId
					select new
					{
						Code = f.FundId.ToString(),
						Value = "{0} - {1}".Fmt(f.FundId, f.FundName),
					};
			return q.ToDictionary(k => k.Code, v => v.Value);
		}
		public object GetNamePidFromId()
		{
			IEnumerable<object> q;
			if (pid.Length > 0 && (pid[0] == 'e' || pid[0] == '-'))
			{
				var env = pid.Substring(1).ToInt();
				q = from e in DbUtil.Db.PeopleExtras
					where e.Field == "EnvelopeNumber"
					where e.IntValue == env
					orderby e.Person.Family.HeadOfHouseholdId == e.PeopleId ? 1 : 2
					select new
					{
						e.PeopleId,
						name = e.Person.Name2 + (e.Person.DeceasedDate.HasValue ? "[DECEASED]" : "")
					};
			}
			else
			{
				q = from i in DbUtil.Db.People
					where i.PeopleId == pid.ToInt()
					select new
					{
						i.PeopleId,
						name = i.Name2 + (i.DeceasedDate.HasValue ? "[DECEASED]" : "")
					};
			}
			var o = q.FirstOrDefault();
			if (o == null)
				return new { error = "not found" };
			return o;
		}
		public static string Names(string q, int limit)
		{
			var qu = from p in DbUtil.Db.People
					 where p.Name2.StartsWith(q)
					 orderby p.Name2
					 select p.Name2
					 + (p.DeceasedDate.HasValue ? " [DECEASED]" : "")
					 + "|" + p.PeopleId
					 + "|" + (p.Age ?? 0)
					 + "|" + (p.PrimaryAddress ?? "");
			var ret = string.Join("\n", qu.Take(limit).ToArray());
			return ret;
		}

		public object ContributionRowData(PostBundleController ctl, int cid, decimal? othersplitamt = null)
		{
			var cinfo = FetchContributions(cid).Single();
			var body = CmsController.RenderPartialViewToString(ctl, "Row", cinfo);
			var q = from c in DbUtil.Db.Contributions
					let bh = c.BundleDetails.First().BundleHeader
					where c.ContributionId == cid
					select new
					{
						row = body,
						amt = c.ContributionAmount.ToString2("N2"),
						cid,
						totalitems = bh.BundleDetails.Sum(d =>
							d.Contribution.ContributionAmount).ToString2("N2"),
						itemcount = bh.BundleDetails.Count(),
						othersplitamt = othersplitamt.ToString2("N2")
					};
			return q.First();
		}
		public object PostContribution(PostBundleController ctl)
		{
			try
			{
				var bd = new CmsData.BundleDetail
				{
					BundleHeaderId = id,
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
				};
				int type;
				if (pledge == true)
					type = (int)Contribution.TypeCode.Pledge;
				else
					type = (int)Contribution.TypeCode.CheckCash;

				decimal? othersplitamt = null;
				if (splitfrom > 0)
				{
					var q = from c in DbUtil.Db.Contributions
							where c.ContributionId == splitfrom
							select new
								   {
									   c,
									   bd = c.BundleDetails.First(),
								   };
					var i = q.Single();
					othersplitamt = i.c.ContributionAmount - amt;
					i.c.ContributionAmount = othersplitamt;
					DbUtil.Db.SubmitChanges();
					bd.BundleSort1 = i.bd.BundleDetailId;
				}

				bd.Contribution = new Contribution
				{
					CreatedBy = Util.UserId,
					CreatedDate = bd.CreatedDate,
					FundId = fund,
					PeopleId = pid.ToInt2(),
					ContributionDate = bundle.ContributionDate,
					ContributionAmount = amt,
					ContributionStatusId = 0,
					PledgeFlag = pledge,
					ContributionTypeId = type,
					ContributionDesc = notes,
					CheckNo = checkno
				};
				bundle.BundleDetails.Add(bd);
				DbUtil.Db.SubmitChanges();
				return ContributionRowData(ctl, bd.ContributionId, othersplitamt);
			}
			catch (Exception ex)
			{
				return new { error = ex.Message };
			}
		}
		public object UpdateContribution(PostBundleController ctl)
		{
			int type;
			if (pledge == true)
				type = (int)Contribution.TypeCode.Pledge;
			else
				type = (int)Contribution.TypeCode.CheckCash;
			var c = DbUtil.Db.Contributions.SingleOrDefault(cc => cc.ContributionId == editid);
			if (c == null)
				return null;
			c.FundId = fund;
			c.PeopleId = pid.ToInt2();
			c.ContributionAmount = amt;
			c.PledgeFlag = pledge;
			c.ContributionTypeId = type;
			c.ContributionDesc = notes;
			c.CheckNo = checkno;
			DbUtil.Db.SubmitChanges();
			return ContributionRowData(ctl, c.ContributionId);
		}
		public object DeleteContribution()
		{
			var bd = bundle.BundleDetails.SingleOrDefault(d => d.ContributionId == editid);
			if (bd != null)
			{
				var c = bd.Contribution;
				DbUtil.Db.BundleDetails.DeleteOnSubmit(bd);
				bundle.BundleDetails.Remove(bd);
				DbUtil.Db.Contributions.DeleteOnSubmit(c);
				DbUtil.Db.SubmitChanges();
			}
			return new
			{
				totalitems = bundle.BundleDetails.Sum(d =>
					d.Contribution.ContributionAmount).ToString2("N2"),
				itemcount = bundle.BundleDetails.Count(),
			};
		}
		private static string[] columns = 
        { 
            "Submit Date=Date,Post Amount=Amount,Check Number=Check,R/T=Route,Account Number=Account,Deposit Number=Bundle",
            "Date Entered=Date,Total=Amount,ProfileID=Account"
        };
		private static string[] _MagTek = { "From MICR :" };

		private static bool CheckNames(string[] names, IEnumerable<string> lookfor)
		{
			var q = from n in names
					join r in lookfor on n equals r
					select n;
			return q.Count() == lookfor.Count();
		}

		private static Dictionary<string, string> GetNames(string[] names)
		{
			foreach (var s in columns)
			{
				var rq = from c in s.Split(',')
						 let a = c.Split('=')
						 select new { col = a[0], name = a[1] };
				var rd = rq.ToDictionary(d => d.col, d => d.name);
				if (CheckNames(names, rd.Keys))
					return rd;
			}
			return null;
		}
		public static int? BatchProcess(string text, DateTime date, int? fundid)
		{
			if (DbUtil.Db.Setting("BankDepositFormat", "none") == "FBCStark")
				return BatchProcessFbcStark2(text, date, fundid);
			if (text.StartsWith("From MICR :"))
				return BatchProcessMagTek(text, date);
			if (text.StartsWith("Financial_Institution"))
				using (var csv = new CsvReader(new StringReader(text), true))
					return BatchProcessSunTrust(csv, date, fundid);
			using (var csv = new CsvReader(new StringReader(text), true))
			{
				var names = csv.GetFieldHeaders();
				if (names.Contains("ProfileID"))
					return BatchProcessServiceU(csv, date);
				return BatchProcess(csv, date, fundid);
			}
		}
		private static int? BatchProcessMagTek(string lines, DateTime date)
		{
			var now = DateTime.Now;
			var bh = new BundleHeader
			{
				BundleHeaderTypeId = BundleTypeCode.ChecksAndCash,
				BundleStatusId = BundleStatusCode.Open,
				ContributionDate = date,
				CreatedBy = Util.UserId,
				CreatedDate = now,
				FundId = 1
			};
			DbUtil.Db.BundleHeaders.InsertOnSubmit(bh);

			var re = new Regex(
@"(T(?<rt>[\d?]+)T(?<ac>[\d ?]*)U\s*(?<ck>[\d?]+))|
(CT(?<rt>[\d?]+)A(?<ac>[\d ?]*)C(?<ck>[\d?]+)M)",
				RegexOptions.IgnoreCase);
			var m = re.Match(lines);
			while (m.Success)
			{
				var rt = m.Groups["rt"].Value;
				var ac = m.Groups["ac"].Value;
				var ck = m.Groups["ck"].Value;
				var bd = new CmsData.BundleDetail
				{
					CreatedBy = Util.UserId,
					CreatedDate = now,
				};
				bh.BundleDetails.Add(bd);
				var qf = from f in DbUtil.Db.ContributionFunds
						 where f.FundStatusId == 1
						 orderby f.FundId
						 select f.FundId;

				bd.Contribution = new Contribution
				{
					CreatedBy = Util.UserId,
					CreatedDate = now,
					ContributionDate = date,
					FundId = qf.First(),
					ContributionStatusId = 0,
					ContributionTypeId = (int)Contribution.TypeCode.CheckCash,
				};
				bd.Contribution.ContributionDesc = ck;
				var eac = Util.Encrypt(rt + "," + ac);
				var q = from kc in DbUtil.Db.CardIdentifiers
						where kc.Id == eac
						select kc.PeopleId;
				var pid = q.SingleOrDefault();
				if (pid != null)
					bd.Contribution.PeopleId = pid;
				bd.Contribution.BankAccount = eac;
				bd.Contribution.ContributionDesc = ck;

				m = m.NextMatch();
			}
			bh.TotalChecks = 0;
			bh.TotalCash = 0;
			bh.TotalEnvelopes = 0;
			DbUtil.Db.SubmitChanges();
			return bh.BundleHeaderId;
		}

		private static BundleHeader GetBundleHeader(DateTime date, DateTime now)
		{
			var bh = new BundleHeader
						{
							BundleHeaderTypeId = BundleTypeCode.PreprintedEnvelope,
							BundleStatusId = BundleStatusCode.Open,
							ContributionDate = date,
							CreatedBy = Util.UserId,
							CreatedDate = now,
							FundId = 1
						};
			DbUtil.Db.BundleHeaders.InsertOnSubmit(bh);
			bh.BundleStatusId = BundleStatusCode.Open;
			bh.BundleHeaderTypeId = BundleTypeCode.ChecksAndCash;
			return bh;
		}
		private static void FinishBundle(BundleHeader bh)
		{
			bh.TotalChecks = bh.BundleDetails.Sum(d => d.Contribution.ContributionAmount);
			bh.TotalCash = 0;
			bh.TotalEnvelopes = 0;
			DbUtil.Db.SubmitChanges();
		}
		public static int? BatchProcess(CsvReader csv, DateTime date, int? fundid)
		{
			var prevbundle = -1;
			var curbundle = 0;

			var bh = GetBundleHeader(date, DateTime.Now);

			Regex re = new Regex(
				@"(?<g1>d(?<rt>.*?)d\sc(?<ac>.*?)(?:c|\s)(?<ck>.*?))$
		|(?<g2>d(?<rt>.*?)d(?<ck>.*?)(?:c|\s)(?<ac>.*?)c[\s!]*)$
		|(?<g3>d(?<rt>.*?)d(?<ac>.*?)c(?<ck>.*?$))
		|(?<g4>c(?<ck>.*?)c\s*d(?<rt>.*?)d(?<ac>.*?)c\s*$)
		", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
			int fieldCount = csv.FieldCount;
			var cols = csv.GetFieldHeaders();

			while (csv.ReadNextRecord())
			{
				var bd = new CmsData.BundleDetail
				{
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
				};
				var qf = from f in DbUtil.Db.ContributionFunds
						 where f.FundStatusId == 1
						 orderby f.FundId
						 select f.FundId;

				bd.Contribution = new Contribution
				{
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
					ContributionDate = date,
					FundId = fundid ?? qf.First(),
					ContributionStatusId = 0,
					ContributionTypeId = (int)Contribution.TypeCode.CheckCash,
				};
				string ac = null, rt = null;
				for (var c = 1; c < fieldCount; c++)
				{
					switch (cols[c].ToLower())
					{
						case "deposit number":
							curbundle = csv[c].ToInt();
							if (curbundle != prevbundle)
							{
								if (curbundle == 3143)
								{
									foreach (var i in bh.BundleDetails)
									{
										Debug.WriteLine(i.Contribution.ContributionDesc);
										Debug.WriteLine(i.Contribution.BankAccount);
									}
								}

								FinishBundle(bh);
								bh = GetBundleHeader(date, DateTime.Now);
								prevbundle = curbundle;
							}
							break;
						case "post amount":
							bd.Contribution.ContributionAmount = csv[c].GetAmount();
							break;
						//    break;
						case "micr":
							var m = re.Match(csv[c]);
							rt = m.Groups["rt"].Value;
							ac = m.Groups["ac"].Value;
							bd.Contribution.ContributionDesc = m.Groups["ck"].Value;
							break;
					}
				}
				var eac = Util.Encrypt(rt + "|" + ac);
				var q = from kc in DbUtil.Db.CardIdentifiers
						where kc.Id == eac
						select kc.PeopleId;
				var pid = q.SingleOrDefault();
				if (pid != null)
					bd.Contribution.PeopleId = pid;
				bd.Contribution.BankAccount = eac;
				bh.BundleDetails.Add(bd);
			}
			FinishBundle(bh);
			return bh.BundleHeaderId;
		}
		public static int? BatchProcessSunTrust(CsvReader csv, DateTime date, int? fundid)
		{
			var prevbundle = -1;
			var curbundle = 0;

			var bh = GetBundleHeader(date, DateTime.Now);

			int fieldCount = csv.FieldCount;
			var cols = csv.GetFieldHeaders();

			while (csv.ReadNextRecord())
			{
				var bd = new CmsData.BundleDetail
				{
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
				};
				var qf = from f in DbUtil.Db.ContributionFunds
						 where f.FundStatusId == 1
						 orderby f.FundId
						 select f.FundId;

				bd.Contribution = new Contribution
				{
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
					ContributionDate = date,
					FundId = fundid ?? qf.First(),
					ContributionStatusId = 0,
					ContributionTypeId = (int)Contribution.TypeCode.CheckCash,
				};
				string ac = null, rt = null, ck = null, sn = null;
				for (var c = 1; c < fieldCount; c++)
				{
					switch (cols[c].ToLower())
					{
						case "deposit_id":
							curbundle = csv[c].ToInt();
							if (curbundle != prevbundle)
							{
								if (curbundle == 3143)
								{
									foreach (var i in bh.BundleDetails)
									{
										Debug.WriteLine(i.Contribution.ContributionDesc);
										Debug.WriteLine(i.Contribution.BankAccount);
									}
								}

								FinishBundle(bh);
								bh = GetBundleHeader(date, DateTime.Now);
								prevbundle = curbundle;
							}
							break;
						case "amount":
							bd.Contribution.ContributionAmount = csv[c].GetAmount();
							break;
						case "tran_code":
							ck = csv[c];
							break;
						case "serial_number":
							sn = csv[c];
							break;
						case "routing_transit":
							rt = csv[c];
							break;
						case "account_number":
							ac = csv[c];
							break;
					}
				}
				if (!ck.HasValue())
					if (ac.Contains(' '))
					{
						var a = ac.SplitStr(" ", 2);
						ck = a[0];
						ac = a[1];
					}
				bd.Contribution.ContributionDesc = string.Join(" ", sn, ck);
				var eac = Util.Encrypt(rt + "|" + ac);
				var q = from kc in DbUtil.Db.CardIdentifiers
						where kc.Id == eac
						select kc.PeopleId;
				var pid = q.SingleOrDefault();
				if (pid != null)
					bd.Contribution.PeopleId = pid;
				bd.Contribution.BankAccount = eac;
				bh.BundleDetails.Add(bd);
			}
			FinishBundle(bh);
			return bh.BundleHeaderId;
		}
		public static int? BatchProcessFbcStark2(string text, DateTime date, int? fundid)
		{
			var prevdt = DateTime.MinValue;
			BundleHeader bh = null;
			var sr = new StringReader(text);
			string line = "";
			do
			{
				line = sr.ReadLine();
				if (line == null)
					return null;
			} while (!line.StartsWith("Batch ID"));
			var sep = ',';
			if (line.StartsWith("Batch ID\t"))
				sep = '\t';

			for (; ; )
			{
				line = sr.ReadLine();
				if (line == null)
					break;
				var csv = line.Split(sep);
				var bd = new BundleDetail
				{
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
				};
				var qf = from f in DbUtil.Db.ContributionFunds
						 where f.FundStatusId == 1
						 orderby f.FundId
						 select f.FundId;

				bd.Contribution = new Contribution
				{
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
					ContributionDate = date,
					FundId = fundid ?? qf.First(),
					ContributionStatusId = 0,
					ContributionTypeId = (int)Contribution.TypeCode.CheckCash,
				};

				var s = csv[3];
				var m = s.Substring(0, 2).ToInt();
				var d = s.Substring(2, 2).ToInt();
				var y = s.Substring(4, 2).ToInt() + 2000;
				var dt = new DateTime(y, m, d);

				if (dt != prevdt)
				{
					if (bh != null)
						FinishBundle(bh);
					bh = GetBundleHeader(dt, DateTime.Now);
					prevdt = dt;
				}

				string ck, rt, ac;
				rt = csv[7];
				ac = csv[8];
				ck = csv[9];
				bd.Contribution.ContributionAmount = csv[10].GetAmount();

				bd.Contribution.CheckNo = ck;
				var eac = Util.Encrypt(rt + "|" + ac);
				var q = from kc in DbUtil.Db.CardIdentifiers
						where kc.Id == eac
						select kc.PeopleId;
				var pid = q.SingleOrDefault();
				if (pid != null)
					bd.Contribution.PeopleId = pid;
				bd.Contribution.BankAccount = eac;
				bh.BundleDetails.Add(bd);
			}
			FinishBundle(bh);
			return bh.BundleHeaderId;
		}
		public static int? BatchProcessFbcStark(string text, DateTime date, int? fundid)
		{
			var prevdt = DateTime.MinValue;
			BundleHeader bh = null;
			var sr = new StringReader(text);
			for (; ; )
			{
				var line = sr.ReadLine();
				if (line == null)
					break;
				var csv = line.Split(',');
				var bd = new BundleDetail
				{
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
				};
				var qf = from f in DbUtil.Db.ContributionFunds
						 where f.FundStatusId == 1
						 orderby f.FundId
						 select f.FundId;

				bd.Contribution = new Contribution
				{
					CreatedBy = Util.UserId,
					CreatedDate = DateTime.Now,
					ContributionDate = date,
					FundId = fundid ?? qf.First(),
					ContributionStatusId = 0,
					ContributionTypeId = (int)Contribution.TypeCode.CheckCash,
				};

				var dtint = csv[3].ToLong();
				var y = (int)(dtint % 10000);
				var m = (int)(dtint / 1000000);
				var d = (int)(dtint / 10000) % 100;
				var dt = new DateTime(y, m, d);

				if (dt != prevdt)
				{
					if (bh != null)
						FinishBundle(bh);
					bh = GetBundleHeader(dt, DateTime.Now);
					prevdt = dt;
				}
				bd.Contribution.ContributionAmount = csv[5].GetAmount() / 100;

				string ck, rt, ac;
				ck = csv[4];
				rt = csv[6];
				ac = csv[7];

				bd.Contribution.CheckNo = ck;
				var eac = Util.Encrypt(rt + "|" + ac);
				var q = from kc in DbUtil.Db.CardIdentifiers
						where kc.Id == eac
						select kc.PeopleId;
				var pid = q.SingleOrDefault();
				if (pid != null)
					bd.Contribution.PeopleId = pid;
				bd.Contribution.BankAccount = eac;
				bh.BundleDetails.Add(bd);
			}
			FinishBundle(bh);
			return bh.BundleHeaderId;
		}
		private static int? FindFund(string s)
		{
			var qf = from f in DbUtil.Db.ContributionFunds
					 where f.FundName == s
					 select f;
			var fund = qf.FirstOrDefault();
			if (fund == null)
				return null;
			return fund.FundId;
		}
		private static CmsData.BundleDetail CreateContribution(DateTime date, int fundid)
		{
			var bd = new CmsData.BundleDetail
			{
				CreatedBy = Util.UserId,
				CreatedDate = Util.Now,
			};
			bd.Contribution = new Contribution
			{
				CreatedBy = Util.UserId,
				CreatedDate = Util.Now,
				ContributionDate = date,
				FundId = fundid,
				ContributionStatusId = 0,
				ContributionTypeId = (int)Contribution.TypeCode.CheckCash,
			};
			return bd;
		}
		public static int? BatchProcessServiceU(CsvReader csv, DateTime date)
		{
			var cols = csv.GetFieldHeaders();
			var now = DateTime.Now;

			var bh = GetBundleHeader(date, now);

			while (csv.ReadNextRecord())
			{
				string ac = null, oth = null, first = null, last = null, addr = null, name = null, email = null;
				var dt = date;
				for (var c = 1; c < csv.FieldCount; c++)
				{
					var col = cols[c].Trim();
					switch (col)
					{
						case "Date Entered":
							dt = csv[c].ToDate() ?? date;
							break;
						case "ProfileID":
							ac = csv[c];
							break;
						case "First Name":
							first = csv[c];
							break;
						case "Last Name":
							last = csv[c];
							break;
						case "Full Name":
							name = csv[c];
							break;
						case "Address":
							addr = csv[c];
							break;
						case "Email Address":
							email = csv[c];
							break;
						case "Designation for &quot;Other&quot;":
							oth = csv[c];
							break;
					}
				}
				if (ac.ToInt() == 0)
					ac = email;
				var eac = Util.Encrypt(ac);
				var q = from kc in DbUtil.Db.CardIdentifiers
						where kc.Id == eac
						select kc.PeopleId;
				var pid = q.SingleOrDefault();
				string bankac = null;
				ExtraDatum ed = null;
				if (pid == null)
				{
					bankac = eac;
					string person;
					if (last.HasValue())
						person = "{1}, {0}; {2}".Fmt(first, last, addr);
					else
						person = "{0}; {1}".Fmt(name, addr);
					ed = new ExtraDatum { Data = person, Stamp = Util.Now };
				}
				CmsData.BundleDetail bd = null;
				for (var c = 0; c < csv.FieldCount; c++)
				{
					var col = cols[c].Trim();
					if (col != "Amount" && !col.Contains("Comment") && csv[c].StartsWith("$") && csv[c].GetAmount() > 0)
					{
						var fundid = FindFund(col);
						bd = CreateContribution(date, fundid ?? 1);
						bd.Contribution.ContributionAmount = csv[c].GetAmount();
						if (col == "Other")
							col = oth;
						if (!fundid.HasValue)
							bd.Contribution.ContributionDesc = col;
						if (ac.HasValue())
							bd.Contribution.BankAccount = bankac;
						bd.Contribution.PeopleId = pid;
						bh.BundleDetails.Add(bd);
						if (ed != null)
							bd.Contribution.ExtraDatum = ed;
					}
				}
			}
			FinishBundle(bh);
			return bh.BundleHeaderId;
		}
		public static string Tip(int? pid, int? age, string memstatus, string address, string city, string state, string zip)
		{
			return "PeopleId: {0}|Age: {1}|{2}|{3}|{4}".Fmt(pid, age, memstatus, address, Util.FormatCSZ(city, state, zip));
		}
		public class ContributionInfo
		{
			public int ContributionId { get; set; }
			public string eac { get; set; }
			public string extra { get; set; }
			public int? PeopleId { get; set; }
			public int? Age { get; set; }
			public string Address { get; set; }
			public string City { get; set; }
			public string State { get; set; }
			public string Zip { get; set; }
			public bool pledge { get; set; }
			public string memstatus { get; set; }
			public string CityStateZip
			{
				get
				{
					return Util.FormatCSZ(City, State, Zip);
				}
			}
			public string Name { get; set; }
			public decimal? Amt { get; set; }
			public string AmtDisplay
			{
				get
				{
					return Amt.ToString2("N2");
				}
			}
			public string Fund { get; set; }
			public int FundId { get; set; }
			public string FundDisplay
			{
				get
				{
					return "{0} - {1}".Fmt(FundId, Fund);
				}
			}
			public string Notes { get; set; }
			public string CheckNo { get; set; }
			public string tip
			{
				get
				{
					return Tip(PeopleId, Age, memstatus, Address, City, State, Zip);
				}
			}
		}
	}
}

