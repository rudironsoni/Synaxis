// <copyright file="ModelsDevResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto
{
    using System.Collections.Generic;

    // Root object is a dictionary keyed by provider id/name
    public sealed class ModelsDevResponse : Dictionary<string, ProviderDto>
    {
    }
}