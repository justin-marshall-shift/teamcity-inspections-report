using System;
using System.Text;

namespace teamcity_inspections_report.Common
{
    public static class HashHelper
    {
        public static string ComputeHash(string fragment)
        {
            if (string.IsNullOrEmpty(fragment))
                return string.Empty;

            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                var textData = Encoding.UTF8.GetBytes(fragment);
                var hash = sha.ComputeHash(textData);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
