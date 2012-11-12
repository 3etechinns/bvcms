using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CmsData;
using CmsData.Registration;
using UtilityExtensions;

namespace CmsWeb.Models
{
	public partial class OnlineRegPersonModel
	{
		public decimal AmountToPay()
		{
			if (paydeposit == true && setting.Deposit.HasValue && setting.Deposit > 0)
				return setting.Deposit.Value;
			return TotalAmount();
		}
		public decimal TotalAmount()
		{
			if (org == null)
				return 0;
			decimal amt = 0;
			var countorgs = 0;

			// compute special fees first, in order of precedence, lowest to highest
			if (setting.AskVisible("AskTickets"))
				// fee based on number of tickets
				amt = (setting.Fee ?? 0) * (ntickets ?? 0);
			if (setting.AskVisible("AskSuggestedFee"))
				amt = Suggestedfee ?? 0;
			if (setting.AgeGroups.Count > 0) // fee based on age
			{
				var q = from o in setting.AgeGroups
						where age >= o.StartAge
						where age <= o.EndAge || o.EndAge == 0
						select o.Fee ?? 0;
				if (q.Any())
					amt = q.First();
			}
			decimal? orgfee = null;
			if (setting.orgFees.HasValue)
			// fee based on being in an organization
			{
				var q = (from o in setting.orgFees.list
						 where person != null
							   && person.OrganizationMembers.Any(om => om.OrganizationId == o.OrgId)
						 orderby o.Fee
						 select o.Fee ?? 0).ToList();
				countorgs = q.Count();
				if (countorgs > 0)
					orgfee = q.First();
			}
			// just use the simple fee if nothing else has been used yet.
			if (amt == 0 && countorgs == 0 && !setting.AskVisible("AskSuggestedFee"))
				amt = setting.Fee ?? 0;
			if (orgfee.HasValue)
				amt = orgfee.Value; // special price for org member
			else
				amt += TotalOther();
			return amt;
		}
		public decimal TotalOther()
		{
			decimal amt = 0;
			foreach (var ask in setting.AskItems)
				switch (ask.Type)
				{
					case "AskMenu":
						amt += ((AskMenu)ask).MenuItemsChosen(MenuItem).Sum(m => m.number * m.amt);
						break;
					case "AskDropdown":
				        var cc = ((AskDropdown) ask).SmallGroupChoice(option);
				        if (cc != null) 
                            amt += cc.Fee ?? 0;
				        break;
					case "AskCheckboxes":
						if (((AskCheckboxes)ask).list.Any(vv => vv.Fee > 0))
							amt += ((AskCheckboxes)ask).CheckboxItemsChosen(GroupTags).Sum(c => c.Fee ?? 0);
						break;
				}
			if (org.LastDayBeforeExtra.HasValue && setting.ExtraFee.HasValue)
				if (Util.Now > org.LastDayBeforeExtra.Value.AddHours(24))
					amt += setting.ExtraFee.Value;
			if (FundItem.Count > 0)
				amt += FundItemsChosen().Sum(f => f.amt);
			var askSize = setting.AskObject<AskSize>();
			if (askSize != null && shirtsize != "lastyear" && askSize.Fee.HasValue)
				amt += askSize.Fee.Value;
			return amt;
		}
	}
}