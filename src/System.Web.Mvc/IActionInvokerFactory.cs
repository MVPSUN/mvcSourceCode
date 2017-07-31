﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc
{
    /// <summary>
    /// Used to create an <see cref="IActionInvoker"/> instance for the current request.
    /// </summary>
    public interface IActionInvokerFactory
    {
        /// <summary>
        /// Creates an instance of action invoker for the current request.
        /// </summary>
        /// <returns>The created <see cref="IActionInvoker"/>.</returns>
        IActionInvoker CreateInstance();
    }
}
