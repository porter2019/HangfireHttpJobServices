namespace HangfireHttpJobServices
{
    public static class ApplicationExtenssion
    {
        /// <summary>
        /// 程序启动后进行的操作
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAppStartup(this IApplicationBuilder appBuilder,IHostApplicationLifetime lifeTime)
        {
            var services = appBuilder.ApplicationServices.CreateScope().ServiceProvider;

            var logger = NLog.LogManager.GetCurrentClassLogger();

            var assemblyName = AppDomain.CurrentDomain.FriendlyName;

            lifeTime.ApplicationStarted.Register(() =>
            {
                logger.Info("【Application Started】");
            });
            lifeTime.ApplicationStopping.Register(() =>
            {
                logger.Info("【Application Stopping】");
            });

            return appBuilder;
        }
    }
}
