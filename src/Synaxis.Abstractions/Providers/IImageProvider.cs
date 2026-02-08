// <copyright file="IImageProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Providers
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for providers that support image generation.
    /// </summary>
    public interface IImageProvider : IProviderClient
    {
        /// <summary>
        /// Generates an image based on the specified prompt asynchronously.
        /// </summary>
        /// <param name="prompt">The text prompt describing the desired image.</param>
        /// <param name="model">The model to use for image generation.</param>
        /// <param name="options">Optional generation parameters.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the image generation response.</returns>
        Task<object> GenerateImageAsync(
            string prompt,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default);
    }
}
