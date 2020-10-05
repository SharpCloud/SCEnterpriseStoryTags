using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SCEnterpriseStoryTags.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly Encoding _textEncoding = Encoding.UTF8;

        public string LoadPassword(EnterpriseSolution solution)
        {
            return _textEncoding.GetString(
                Decrypt(
                    solution.Password,
                    solution.PasswordEntropy,
                    DataProtectionScope.CurrentUser));
        }

        public void SavePassword(string password, EnterpriseSolution solution)
        {
            solution.Password = Convert.ToBase64String(
                Encrypt(
                    _textEncoding.GetBytes(password),
                    out var entropy,
                    DataProtectionScope.CurrentUser));

            solution.PasswordEntropy = Convert.ToBase64String(entropy);
        }

        private byte[] Decrypt(string base64CipherText, string entropy, DataProtectionScope scope)
        {
            byte[] bytes;

            var entropyBytes = string.IsNullOrWhiteSpace(entropy)
                ? null
                : Convert.FromBase64String(entropy);

            try
            {
                bytes = ProtectedData.Unprotect(
                    Convert.FromBase64String(base64CipherText),
                    entropyBytes,
                    scope);
            }
            catch (Exception)
            {
                bytes = new byte[0];
            }

            return bytes;
        }

        private byte[] Encrypt(byte[] plainTextBytes, out byte[] entropy, DataProtectionScope scope)
        {
            entropy = CreateRandomEntropy();

            return ProtectedData.Protect(
                plainTextBytes,
                entropy,
                scope);
        }

        private static byte[] CreateRandomEntropy()
        {
            var entropy = new byte[100];
            new RNGCryptoServiceProvider().GetBytes(entropy);
            return entropy;
        }
    }
}
