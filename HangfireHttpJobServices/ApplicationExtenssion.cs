using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireHttpJobServices
{
    public static class ApplicationExtenssion
    {
        /// <summary>
        /// 程序启动后进行的操作
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAppStartup(this IApplicationBuilder appBuilder)
        {
            var services = appBuilder.ApplicationServices.CreateScope().ServiceProvider;

            var logger = NLog.LogManager.GetCurrentClassLogger();

            var lifeTime = services.GetService<IHostApplicationLifetime>();


            var assemblyName = AppDomain.CurrentDomain.FriendlyName;

            lifeTime.ApplicationStarted.Register(() =>
            {
                logger.Info("【ApplicationStarted】");
            });
            lifeTime.ApplicationStopping.Register(() =>
            {
                logger.Info("【ApplicationStopping】");
            });

            return appBuilder;
        }
    }
}
