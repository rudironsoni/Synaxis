// <copyright file="IAudioTranscriptionCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Commands
{
    using Synaxis.Shared.Kernel.Application.Commands;

    /// <summary>
    /// Marker interface for audio transcription commands that produce an <see cref="Messages.AudioTranscriptionResponse"/>.
    /// </summary>
    /// <typeparam name="TAudioTranscriptionResponse">The type of audio transcription response produced by the command.</typeparam>
    public interface IAudioTranscriptionCommand<TAudioTranscriptionResponse> : ICommand<TAudioTranscriptionResponse>
    {
    }
}
