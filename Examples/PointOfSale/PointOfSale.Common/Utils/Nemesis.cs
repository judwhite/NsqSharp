using System;
using System.Security.Cryptography;
using NsqSharp.Utils.Extensions;
using PointOfSale.Common.Config;

namespace PointOfSale.Common.Utils
{
    public class Nemesis : INemesis
    {
        private readonly RNGCryptoServiceProvider _random = new RNGCryptoServiceProvider();

        private readonly int _value;

        public Nemesis(IAppSettings appSettings)
        {
            if (appSettings == null)
                throw new ArgumentNullException("appSettings");

            _value = appSettings.ServiceCallNemesis;
        }

        public void Invoke()
        {
            if (_random.Intn(100) < _value)
            {
                throw new Exception("Nemesis exception ヽ(`Д´)ﾉ");
            }
        }
    }

    public interface INemesis
    {
        /// <summary>
        /// Randomly throws an exception based on the 'nemesis' value in the app.config appSettings section.
        /// Used to test backoff, retry, and logging.
        /// </summary>
        void Invoke();
    }
}
