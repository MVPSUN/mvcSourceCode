﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.TestCommon;

namespace System.Web.Mvc.Test
{
    public class ModelClientValidationMaxLengthRuleTest
    {
        [Fact]
        public void ModelClientValidationMaxLengthRuleTestAddsMaxLengthParameter()
        {
            // Arrange
            var clientValidationRule = new ModelClientValidationMaxLengthRule("Max length error message", 8);

            // Assert
            Assert.Equal(1, clientValidationRule.ValidationParameters.Count);
            Assert.Equal(8, clientValidationRule.ValidationParameters["max"]);
            Assert.Equal("Max length error message", clientValidationRule.ErrorMessage);
        }
    }
}
