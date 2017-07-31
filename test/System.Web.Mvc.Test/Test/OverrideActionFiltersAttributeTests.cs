﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Mvc.Filters;

namespace System.Web.Mvc.Test
{
    public class OverrideActionFiltersAttributeTests : OverrideFiltersAttributeTests
    {
        protected override Type ExpectedFiltersToOverride
        {
            get { return typeof(IActionFilter); }
        }
        
        protected override Type ProductUnderTestType
        {
            get { return typeof(OverrideActionFiltersAttribute); }
        }

        protected override IOverrideFilter CreateProductUnderTest()
        {
            return new OverrideActionFiltersAttribute();
        }
    }
}
