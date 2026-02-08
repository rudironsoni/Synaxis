// <copyright file="IImageGenerationCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands
{
    using Synaxis.Abstractions.Commands;

    /// <summary>
    /// Marker interface for image generation commands that produce an <see cref="Messages.ImageGenerationResponse"/>.
    /// </summary>
    /// <typeparam name="TImageGenerationResponse">The type of image generation response produced by the command.</typeparam>
    public interface IImageGenerationCommand<out TImageGenerationResponse> : ICommand<TImageGenerationResponse>
    {
    }
}
