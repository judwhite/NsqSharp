using System.Configuration;

namespace PointOfSale.Common.Config
{
    internal class AppSettings : IAppSettings
    {
        public AppSettings()
        {
            UseSql = bool.Parse(ConfigurationManager.AppSettings["UseSql"]);
            UseFakeServices = bool.Parse(ConfigurationManager.AppSettings["UseFakeServices"]); // TODO
            ServiceCallNemesis = int.Parse(ConfigurationManager.AppSettings["Nemesis"]);
        }

        public bool UseSql { get; private set; }
        public bool UseFakeServices { get; private set; }
        public int ServiceCallNemesis { get; private set; }
    }

    public interface IAppSettings
    {
        bool UseSql { get; }
        bool UseFakeServices { get; }
        int ServiceCallNemesis { get; }
    }
}
