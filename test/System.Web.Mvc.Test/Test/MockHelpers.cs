﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.TestCommon;
using Moq;
using Moq.Language.Flow;

namespace System.Web.Mvc.Test
{
    public static class MockHelpers
    {
        public static ISetup<HttpContextBase> ExpectMvcVersionResponseHeader(this Mock<HttpContextBase> mock)
        {
            Version mvcVersion = VersionTestHelper.GetVersionFromAssembly("System.Web.Mvc", typeof(Controller));

            string majorMinor = mvcVersion.Major + "." + mvcVersion.Minor;
            return mock.Setup(r => r.Response.AppendHeader(MvcHandler.MvcVersionHeaderName, majorMinor));
        }
    }
}
