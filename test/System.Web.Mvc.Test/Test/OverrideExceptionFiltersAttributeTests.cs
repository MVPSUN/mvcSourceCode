﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Mvc.Filters;

namespace System.Web.Mvc.Test
{
    public class OverrideExceptionFiltersAttributeTests : OverrideFiltersAttributeTests
    {
        protected override Type ExpectedFiltersToOverride
        {
            get { return typeof(IExceptionFilter); }
        }
        
        protected override Type ProductUnderTestType
        {
            get { return typeof(OverrideExceptionFiltersAttribute); }
        }

        protected override IOverrideFilter CreateProductUnderTest()
        {
            return new OverrideExceptionFiltersAttribute();
        }
    }
}
