using Microsoft.Extensions.Configuration;
using System.IO;

namespace EmailManager.Client
{
    public class ConfigService
    {
        private readonly IConfiguration _configuration;

        public ConfigService()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public string GetConfigValue(string key)
        {
            return _configuration[key];
        }
    }
}
