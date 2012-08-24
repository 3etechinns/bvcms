﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UtilityExtensions;

namespace CmsWeb.Code
{
	public class Payments
	{
		public static void ValidateCreditCardInfo(ModelStateDictionary ModelState, string Cardnumber, string Expires, string Cardcode)
		{
			if (!ValidateCard(Cardnumber))
				ModelState.AddModelError("Cardnumber", "invalid card number");
			if (!Expires.HasValue())
			{
				ModelState.AddModelError("Expires", "need expiration date");
				return;
			}

			Expires = Expires.Trim();
			DateTime dt;
			if (Expires.Length != 4)
				ModelState.AddModelError("Expires", "invalid expiration date (MMYY)");
			else
			{
				var s = Expires.Insert(2, "/15/");
				if (!DateTime.TryParse(s, out dt))
					ModelState.AddModelError("Expires", "invalid expiration date (MMYY)");
			}
			if (!Cardcode.HasValue())
			{
				ModelState.AddModelError("Cardcode", "need Cardcode");
				return;
			}
			var ccvlen = Cardcode.GetDigits().Length;
			if (ccvlen < 3 || ccvlen > 4)
				ModelState.AddModelError("Cardcode", "invalid Cardcode");
		}
		public static void ValidateBankAccountInfo(ModelStateDictionary ModelState, string Routing, string Account)
		{
			if (!checkABA(Routing))
				ModelState.AddModelError("Routing", "invalid routing number");
			if (!Account.HasValue())
				ModelState.AddModelError("Account", "need account number");
		}
		private static bool checkABA(string s)
		{
			if (!s.HasValue())
				return false;
			if (s.StartsWith("X"))
				return true;
			var t = s.GetDigits();
			if (t.Length != 9)
				return false;
			var n = 0;
			for (var i = 0; i < t.Length; i += 3)
			{
				n += int.Parse(t.Substring(i, 1)) * 3
					+ int.Parse(t.Substring(i + 1, 1)) * 7
					+ int.Parse(t.Substring(i + 2, 1));
			}
			if (n != 0 && n % 10 == 0)
				return true;
			return false;
		}

		public static bool ValidateCard(string s)
		{
			if (!s.HasValue())
				return false;
			if (s.StartsWith("X"))
				return true;
			var number = new int[16];
			int len = 0;
			for (int i = 0; i < s.Length; i++)
			{
				if (char.IsDigit(s, i))
				{
					if (len == 16)
						return false;
					number[len++] = s[i] - '0';
				}
			}

			switch (s[0])
			{
				case '5':
					if (len != 16)
						return false;
					if (number[1] == 0 || number[1] > 5)
						return false;
					break;

				case '4':
					if (len != 16 && len != 13)
						return false;
					break;

				case '3':
					if (len != 15)
						return false;
					if ((number[1] != 4 && number[1] != 7))
						return false;
					break;

				case '6':
					if (len != 16)
						return false;
					if (number[1] != 0 || number[2] != 1 || number[3] != 1)
						return false;
					break;
			}
			int sum = 0;
			for (int i = len - 1; i >= 0; i--)
				if (i % 2 == len % 2)
				{
					int n = number[i] * 2;
					sum += (n / 10) + (n % 10);
				}
				else
					sum += number[i];
			return sum % 10 == 0;
		}
	}
}