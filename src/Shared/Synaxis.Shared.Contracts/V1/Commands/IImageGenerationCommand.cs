// <copyright file="IImageGenerationCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Commands
{
    using Synaxis.Shared.Kernel.Application.Commands;

    /// <summary>
    /// Marker interface for image generation commands that produce an <see cref="Messages.ImageGenerationResponse"/>.
    /// </summary>
    /// <typeparam name="TImageGenerationResponse">The type of image generation response produced by the command.</typeparam>
    public interface IImageGenerationCommand<TImageGenerationResponse> : ICommand<TImageGenerationResponse>
    {
    }
}
