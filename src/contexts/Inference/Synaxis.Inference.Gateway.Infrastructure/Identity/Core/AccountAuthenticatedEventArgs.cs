// <copyright file="AccountAuthenticatedEventArgs.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    using System;

    /// <summary>
    /// Event arguments for account authentication.
    /// </summary>
    public class AccountAuthenticatedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountAuthenticatedEventArgs"/> class.
        /// </summary>
        /// <param name="account">The authenticated account.</param>
        public AccountAuthenticatedEventArgs(IdentityAccount account)
        {
            this.Account = account;
        }

        /// <summary>
        /// Gets the authenticated account.
        /// </summary>
        public IdentityAccount Account { get; }
    }
}