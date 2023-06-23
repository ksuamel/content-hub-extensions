using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Foundation.ContentHubExtensions.Utils
{
    public static class HashUtil
    {
        public static Guid GetSitecoreGuid(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Guid.Empty;
            }

            var truncatedHash = GetHashForId(input);
            return new Guid(truncatedHash);
        }

        private static byte[] GetHashForId(string input)
        {
            return GetMd5Hash(input).Take(16).ToArray();
        }

        public static IEnumerable<byte> GetMd5Hash(string input)
        {
            using (var md5Hash = MD5.Create())
            {
                return md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }
    }
}