// <copyright file="IAudioSynthesisCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands
{
    using Synaxis.Abstractions.Commands;

    /// <summary>
    /// Marker interface for audio synthesis commands that produce an <see cref="Messages.AudioSynthesisResponse"/>.
    /// </summary>
    /// <typeparam name="TAudioSynthesisResponse">The type of audio synthesis response produced by the command.</typeparam>
    public interface IAudioSynthesisCommand<out TAudioSynthesisResponse> : ICommand<TAudioSynthesisResponse>
    {
    }
}
