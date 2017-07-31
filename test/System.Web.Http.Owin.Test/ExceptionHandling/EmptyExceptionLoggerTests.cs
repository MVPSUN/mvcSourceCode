﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Microsoft.TestCommon;

namespace System.Web.Http.Owin.ExceptionHandling
{
    public class EmptyExceptionLoggerTests
    {
        [Fact]
        public void LogAsync_ReturnsCompletedTask()
        {
            // Arrange
            IExceptionLogger product = CreateProductUnderTest();
            ExceptionLoggerContext context = CreateContext();
            CancellationToken cancellationToken = CancellationToken.None;

            // Act
            Task task = product.LogAsync(context, cancellationToken);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        private static ExceptionLoggerContext CreateContext()
        {
            return new ExceptionLoggerContext(new ExceptionContext(new Exception(), ExceptionCatchBlocks.HttpServer));
        }

        private static EmptyExceptionLogger CreateProductUnderTest()
        {
            return new EmptyExceptionLogger();
        }
    }
}
