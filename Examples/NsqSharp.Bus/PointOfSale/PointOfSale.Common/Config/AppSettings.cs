using System.Configuration;

namespace PointOfSale.Common.Config
{
    internal class AppSettings : IAppSettings
    {
        public AppSettings()
        {
            UseSql = bool.Parse(ConfigurationManager.AppSettings["UseSql"]);
            UseServiceCallCache = bool.Parse(ConfigurationManager.AppSettings["UseServiceCallCache"]);
            ServiceCallNemesis = int.Parse(ConfigurationManager.AppSettings["Nemesis"]);
        }

        public bool UseSql { get; private set; }
        public bool UseServiceCallCache { get; private set; }
        public int ServiceCallNemesis { get; private set; }
    }

    public interface IAppSettings
    {
        bool UseSql { get; }
        bool UseServiceCallCache { get; }
        int ServiceCallNemesis { get; }
    }
}
