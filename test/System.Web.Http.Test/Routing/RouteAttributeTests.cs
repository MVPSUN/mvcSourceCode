﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.TestCommon;

namespace System.Web.Http
{
    public class RouteAttributeTest
    {
        [Fact]
        public void DefaultCtor_IsEmptyString()
        {
            RouteAttribute attribute = new RouteAttribute();

            Assert.Equal(String.Empty, attribute.Template);
        }
        
        [Fact]
        public void Ctor_NotNull()
        {
            Assert.ThrowsArgumentNull(() => new RouteAttribute(null), "template");
        }
    }
}
