// <copyright file="IAudioSynthesisCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Commands
{
    using Synaxis.Shared.Kernel.Application.Commands;

    /// <summary>
    /// Marker interface for audio synthesis commands that produce an <see cref="Messages.AudioSynthesisResponse"/>.
    /// </summary>
    /// <typeparam name="TAudioSynthesisResponse">The type of audio synthesis response produced by the command.</typeparam>
    public interface IAudioSynthesisCommand<TAudioSynthesisResponse> : ICommand<TAudioSynthesisResponse>
    {
    }
}
