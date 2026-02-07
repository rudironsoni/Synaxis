// <copyright file="ModelDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto
{
    using System.Collections.Generic;

    public sealed class ModelDto
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? family { get; set; }

        public LimitDto? limit { get; set; }
        public CostDto? cost { get; set; }

        public ModalitiesDto? modalities { get; set; }

        public bool? open_weights { get; set; }

        public bool? tool_call { get; set; }
        public bool? reasoning { get; set; }
        public bool? structured_output { get; set; }

        public string? release_date { get; set; }

        public sealed class LimitDto
        {
            public int? context { get; set; }
            public int? output { get; set; }
        }

        public sealed class CostDto
        {
            public decimal? input { get; set; }
            public decimal? output { get; set; }
        }

        public sealed class ModalitiesDto
        {
            public string[]? input { get; set; }
            public string[]? output { get; set; }
        }
    }
}
