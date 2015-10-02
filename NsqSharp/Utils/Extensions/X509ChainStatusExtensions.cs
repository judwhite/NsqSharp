using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NsqSharp.Utils.Extensions
{
    internal static class X509ChainStatusExtensions
    {
        public static string GetErrors(this X509ChainStatus[] chainStatus)
        {
            if (chainStatus == null)
                throw new ArgumentNullException("chainStatus");

            var errors = new StringBuilder();
            foreach (var status in chainStatus)
            {
                errors.AppendLine(string.Format("{0} - {1}", status.Status, status.StatusInformation));
            }

            return errors.ToString();
        }
    }
}
