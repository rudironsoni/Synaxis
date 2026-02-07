using System;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity
{
    internal class FakeDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return new FakeDataProtector();
        }
    }

    internal class FakeDataProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose)
        {
            // For tests a fake can return itself regardless of purpose
            return this;
        }

        public byte[] Protect(byte[] userData)
        {
            var s = Encoding.UTF8.GetString(userData);
            return Encoding.UTF8.GetBytes("protected:" + s);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            var s = Encoding.UTF8.GetString(protectedData);
            if (s.StartsWith("protected:", StringComparison.Ordinal))
            {
                s = s.Substring("protected:".Length);
            }

            return Encoding.UTF8.GetBytes(s);
        }
    }
}
