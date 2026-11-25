using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Myria.Wpf.Services
{
    /// <summary>
    /// Checks that a downloaded file carries an intact Authenticode signature made with one
    /// specific certificate (pinned by thumbprint). Used before UpdateService runs an installer:
    /// it installs silently, so it must be exactly what the release script signed - not merely
    /// "some file the update server handed out".
    ///
    /// Myria's signing certificate is self-signed, so Windows' WinVerifyTrust reports
    /// CERT_E_UNTRUSTEDROOT for it on most PCs. That code is only returned AFTER the signature
    /// itself verified (a tampered file yields TRUST_E_BAD_DIGEST, an unsigned one
    /// TRUST_E_NOSIGNATURE), so "valid signature, unknown root" plus a matching signer thumbprint
    /// is exactly the check needed - no certificate has to be installed as trusted on the PC.
    /// </summary>
    public static class AuthenticodeVerifier
    {
        private const int CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109);

        public static bool IsSignedBy(string filePath, string expectedThumbprint, out string reason)
        {
            int result;
            try { result = VerifyTrust(filePath); }
            catch (Exception ex) { reason = "signature check failed: " + ex.Message; return false; }

            if (result != 0 && result != CERT_E_UNTRUSTEDROOT)
            {
                reason = $"signature missing or invalid (WinVerifyTrust 0x{result:X8})";
                return false;
            }

            X509Certificate2 signer;
            try
            {
#pragma warning disable SYSLIB0057 // CreateFromSignedFile is the supported way to read an Authenticode signer.
                signer = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
#pragma warning restore SYSLIB0057
            }
            catch (CryptographicException ex)
            {
                reason = "no signer certificate: " + ex.Message;
                return false;
            }

            if (!string.Equals(signer.Thumbprint, expectedThumbprint, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"signed by an unexpected certificate: {signer.Subject} ({signer.Thumbprint})";
                return false;
            }

            reason = "valid signature from " + signer.Subject;
            return true;
        }

        // ---- WinVerifyTrust interop -------------------------------------------------------
        private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
        private const uint WTD_UI_NONE = 2, WTD_REVOKE_NONE = 0, WTD_CHOICE_FILE = 1;
        private const uint WTD_STATEACTION_VERIFY = 1, WTD_STATEACTION_CLOSE = 2;
        private const uint WTD_REVOCATION_CHECK_NONE = 0x10, WTD_CACHE_ONLY_URL_RETRIEVAL = 0x1000;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)] public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, ref WINTRUST_DATA pWVTData);

        private static int VerifyTrust(string filePath)
        {
            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                pcwszFilePath = filePath,
            };
            IntPtr pFile = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
            try
            {
                Marshal.StructureToPtr(fileInfo, pFile, false);
                var data = new WINTRUST_DATA
                {
                    cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                    dwUIChoice = WTD_UI_NONE,
                    fdwRevocationChecks = WTD_REVOKE_NONE,
                    dwUnionChoice = WTD_CHOICE_FILE,
                    pFile = pFile,
                    dwStateAction = WTD_STATEACTION_VERIFY,
                    dwProvFlags = WTD_REVOCATION_CHECK_NONE | WTD_CACHE_ONLY_URL_RETRIEVAL,
                };
                var noUi = new IntPtr(-1);   // INVALID_HANDLE_VALUE: never show UI
                int result = WinVerifyTrust(noUi, WINTRUST_ACTION_GENERIC_VERIFY_V2, ref data);
                data.dwStateAction = WTD_STATEACTION_CLOSE;
                WinVerifyTrust(noUi, WINTRUST_ACTION_GENERIC_VERIFY_V2, ref data);
                return result;
            }
            finally
            {
                Marshal.DestroyStructure<WINTRUST_FILE_INFO>(pFile);
                Marshal.FreeHGlobal(pFile);
            }
        }
    }
}
