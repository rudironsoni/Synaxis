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
        public byte[] Protect(byte[] userData)
        {
            var s = Encoding.UTF8.GetString(userData);
            return Encoding.UTF8.GetBytes("protected:" + s);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            var s = Encoding.UTF8.GetString(protectedData);
            if (s.StartsWith("protected:")) s = s.Substring("protected:".Length);
            return Encoding.UTF8.GetBytes(s);
        }
    }
}
