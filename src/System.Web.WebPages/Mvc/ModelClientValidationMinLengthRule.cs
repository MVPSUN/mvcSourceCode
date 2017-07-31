﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc
{
    public class ModelClientValidationMinLengthRule : ModelClientValidationRule
    {
        public ModelClientValidationMinLengthRule(string errorMessage, int minimumLength)
        {
            ErrorMessage = errorMessage;
            ValidationType = "minlength";
            ValidationParameters["min"] = minimumLength;
        }
    }
}
