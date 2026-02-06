// <copyright file="ProviderDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto
{
    using System.Collections.Generic;

    public sealed class ProviderDto
    {
        public Dictionary<string, ModelDto>? models { get; set; }
    }
}