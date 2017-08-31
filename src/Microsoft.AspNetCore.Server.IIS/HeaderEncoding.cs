using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.IIS
{
    internal static class HeaderEncoding
    {
        // It should just be ASCII or ANSI, but they break badly with un-expected values. We use UTF-8 because it's the same for
        // ASCII, and because some old client would send UTF8 Host headers and expect UTF8 Location responses
        // (e.g. IE and HttpWebRequest on intranets).
        private static Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        internal static unsafe string GetString(byte* pBytes, int byteCount)
        {
            // net451: return new string(pBytes, 0, byteCount, Encoding);

            var charCount = Encoding.GetCharCount(pBytes, byteCount);
            var chars = new char[charCount];
            fixed (char* pChars = chars)
            {
                var count = Encoding.GetChars(pBytes, byteCount, pChars, charCount);
                System.Diagnostics.Debug.Assert(count == charCount);
            }
            return new string(chars);
        }

        internal static byte[] GetBytes(string myString)
        {
            return Encoding.GetBytes(myString);
        }
    }
}