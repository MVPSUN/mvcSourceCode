﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Hosting;

namespace System.Web.Http.Cors
{
    internal static class HttpRequestMessageExtensions
    {
        public static void SetActionDescriptor(this HttpRequestMessage request, HttpActionDescriptor actionDescriptor)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            request.Properties[HttpPropertyKeys.HttpActionDescriptorKey] = actionDescriptor;
        }
    }
}
