﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal class GenerateConfigTemplateOperationConfig
    {
        [Required]
        public Operations.OperationName OperationName { get; set; }

        public bool Overwrite { get; set; }
    }
}
