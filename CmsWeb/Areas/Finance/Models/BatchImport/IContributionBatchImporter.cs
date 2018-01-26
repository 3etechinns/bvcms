﻿using System;

namespace CmsWeb.Areas.Finance.Models.BatchImport
{
    internal interface IContributionBatchImporter
    {
        int? RunImport(string text, DateTime date, string fundid, bool fromFile);
    }
}
