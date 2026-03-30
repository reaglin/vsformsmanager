using System.Security.Cryptography;
using System.Text;

namespace VSFormsManager.Services
{
    /// <summary>
    /// Encrypts and decrypts strings using the Windows Data Protection API (DPAPI).
    ///
    /// Encryption is scoped to <see cref="DataProtectionScope.CurrentUser"/>, meaning:
    ///   • Only the Windows account that encrypted the value can decrypt it.
    ///   • No keys, passwords, or certificates need to be managed by the application.
    ///   • Encrypted blobs are machine- and user-specific — they cannot be transferred
    ///     to another machine or user account and decrypted there.
    ///
    /// Values are stored on disk as Base64-encoded ciphertext.
    /// An empty or null plaintext round-trips as an empty string (no ciphertext stored).
    /// </summary>
    public static class EncryptionHelper
    {
        // Optional entropy adds application-specific context to the DPAPI key,
        // making the encrypted blobs usable only by this application even if
        // another application on the same machine tries to decrypt them.
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("VSFormsManager-v1-ApiKey-Salt");

        // ── Encrypt ───────────────────────────────────────────────────────────

        /// <summary>
        /// Encrypts <paramref name="plaintext"/> with DPAPI and returns a Base64 string
        /// suitable for JSON storage.
        /// Returns <see cref="string.Empty"/> when <paramref name="plaintext"/> is null or empty.
        /// </summary>
        public static string Encrypt(string? plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return string.Empty;

            var bytes      = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(ciphertext);
        }

        // ── Decrypt ───────────────────────────────────────────────────────────

        /// <summary>
        /// Decrypts a Base64 DPAPI blob previously produced by <see cref="Encrypt"/>.
        /// Returns <see cref="string.Empty"/> when <paramref name="cipherBase64"/> is null
        /// or empty, or when decryption fails (e.g. the value was encrypted by a
        /// different user account or is corrupted).
        /// Never throws — failed decryption yields an empty string so the application
        /// gracefully prompts the user to re-enter the key.
        /// </summary>
        public static string Decrypt(string? cipherBase64)
        {
            if (string.IsNullOrEmpty(cipherBase64))
                return string.Empty;

            try
            {
                var ciphertext = Convert.FromBase64String(cipherBase64);
                var plainBytes = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Decryption can fail if: the blob was encrypted by a different Windows
                // user, the user profile has changed, or the data is corrupt.
                // Return empty string so the app gracefully prompts for a new key.
                return string.Empty;
            }
        }
    }
}
