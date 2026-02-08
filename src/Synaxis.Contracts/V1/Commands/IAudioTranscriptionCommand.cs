// <copyright file="IAudioTranscriptionCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands
{
    using Synaxis.Abstractions.Commands;

    /// <summary>
    /// Marker interface for audio transcription commands that produce an <see cref="Messages.AudioTranscriptionResponse"/>.
    /// </summary>
    /// <typeparam name="TAudioTranscriptionResponse">The type of audio transcription response produced by the command.</typeparam>
    public interface IAudioTranscriptionCommand<out TAudioTranscriptionResponse> : ICommand<TAudioTranscriptionResponse>
    {
    }
}
