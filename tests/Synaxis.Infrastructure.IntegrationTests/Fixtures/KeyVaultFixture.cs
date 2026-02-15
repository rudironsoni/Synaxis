// <copyright file="KeyVaultFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System.Threading.Tasks;
using Moq;
using Synaxis.Abstractions.Cloud;
using Xunit;

/// <summary>
/// Fixture for KeyVault testing using mocks.
/// </summary>
public sealed class KeyVaultFixture : IAsyncLifetime
{
    private readonly Dictionary<string, string> _secrets;
    private readonly Dictionary<string, byte[]> _keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultFixture"/> class.
    /// </summary>
    public KeyVaultFixture()
    {
        _secrets = new Dictionary<string, string>();
        _keys = new Dictionary<string, byte[]>();

        var mock = new Mock<IKeyVault>();

        mock.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Callback<string, string, CancellationToken>((name, value, _) => _secrets[name] = value)
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.GetSecretAsync(It.IsAny<string>(), default))
            .Returns<string, CancellationToken>((name, _) =>
                Task.FromResult<string?>(_secrets.TryGetValue(name, out var value) ? value : null));

        mock.Setup(x => x.DeleteSecretAsync(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((name, _) => _secrets.Remove(name))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.EncryptAsync(It.IsAny<string>(), It.IsAny<byte[]>(), default))
            .Returns<string, byte[], CancellationToken>((name, data, _) =>
            {
                var encrypted = new byte[data.Length];
                Array.Copy(data, encrypted, data.Length);
                for (int i = 0; i < encrypted.Length; i++)
                {
                    encrypted[i] = (byte)(encrypted[i] ^ 0xFF);
                }

                _keys[name] = encrypted;
                return Task.FromResult(encrypted);
            });

        mock.Setup(x => x.DecryptAsync(It.IsAny<string>(), It.IsAny<byte[]>(), default))
            .Returns<string, byte[], CancellationToken>((name, data, _) =>
            {
                var decrypted = new byte[data.Length];
                Array.Copy(data, decrypted, data.Length);
                for (int i = 0; i < decrypted.Length; i++)
                {
                    decrypted[i] = (byte)(decrypted[i] ^ 0xFF);
                }

                return Task.FromResult(decrypted);
            });

        KeyVault = mock.Object;
    }

    /// <summary>
    /// Gets the mocked KeyVault instance.
    /// </summary>
    public IKeyVault KeyVault { get; }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _secrets.Clear();
        _keys.Clear();
        return Task.CompletedTask;
    }
}
