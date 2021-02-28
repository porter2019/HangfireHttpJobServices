using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HangfireHttpJobServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string nlogConfig = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? "nlog.Development.config" : "nlog.config";
            var logger = NLogBuilder.ConfigureNLog(nlogConfig).GetCurrentClassLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "程序启动失败");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                host = host.UseWindowsService();
            }
            var port = 2810;
            //dotnet xxx.dll -p 端口号
            if (args.Length > 1 && args[0].ToLower() == "-p")
            {
                string portStr = args[1];
                if (Regex.IsMatch(portStr, @"^\d*$")) port = Convert.ToInt32(portStr);
            }
            return host.ConfigureWebHostDefaults(webBuilder =>
            {
                //var port = 2810;//设置服务端口
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Listen(IPAddress.Any, port);
                    serverOptions.Limits.MaxRequestBodySize = null;
                });
                webBuilder.UseStartup<Startup>().ConfigureLogging(logger =>
                {
                    logger.ClearProviders();
                    logger.SetMinimumLevel(LogLevel.Trace);
                }).UseNLog();
            });
        }
    }
}
