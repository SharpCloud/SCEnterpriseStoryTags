using SCEnterpriseStoryTags.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SCEnterpriseStoryTags.Services
{
    public class PasswordService : IPasswordService
    {
        private const string Password = "Password";
        private const string Dpapi = "Dpapi";
        private const string Entropy = "Entropy";

        private readonly Encoding _textEncoding = Encoding.UTF8;
        private readonly IRegistryService _registryService;

        public PasswordService(IRegistryService registryService)
        {
            _registryService = registryService;
        }

        public string LoadPassword()
        {
            var regPassword = _registryService.RegRead($"{Password}{Dpapi}", string.Empty);
            var regPasswordEntropy = _registryService.RegRead($"{Password}{Dpapi}{Entropy}", null);
            try
            {
                return _textEncoding.GetString(
                    Decrypt(
                        regPassword,
                        regPasswordEntropy,
                        DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException ex) when (ex.Message.Contains("The parameter is incorrect"))
            {
                // Fallback method for backwards compatibility
                regPassword = _registryService.RegRead(Password, string.Empty);

                _registryService.RegDelete(Password);

                return Encoding.Default.GetString(
                    Convert.FromBase64String(regPassword));
            }
        }

        public void SavePassword(string password)
        {
            _registryService.RegWrite($"{Password}{Dpapi}", Convert.ToBase64String(
                Encrypt(
                    _textEncoding.GetBytes(password),
                    out var entropy,
                    DataProtectionScope.CurrentUser)));

            _registryService.RegWrite($"{Password}{Dpapi}{Entropy}", Convert.ToBase64String(entropy));
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
