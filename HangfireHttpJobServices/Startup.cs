using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.HttpJob;
using Hangfire.SqlServer;
using Hangfire.Tags.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimeZoneConverter;

namespace HangfireHttpJobServices
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            JsonConfig = configuration;
        }
        public IConfiguration JsonConfig { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(Configuration);

            services.AddHangfireServer(options => { 
                options.ServerTimeout = TimeSpan.FromMinutes(4);
                options.SchedulePollingInterval = TimeSpan.FromSeconds(15);//�뼶������Ҫ���ö̵㣬һ�������������Ĭ��ʱ�䣬Ĭ��15��
                options.ShutdownTimeout = TimeSpan.FromMinutes(30);//��ʱʱ��
                options.Queues = JsonConfig.GetSection("HangfireQueues").Get<List<string>>().ToArray();
                options.WorkerCount = Math.Max(Environment.ProcessorCount, 40);//�����߳�������ǰ���������̣߳�Ĭ��20
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //ǿ����ʾ����
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");

            //var queues = JsonConfig.GetSection("HangfireQueues").Get<List<string>>().ToArray();
            //app.UseHangfireServer(new BackgroundJobServerOptions
            //{
            //    ServerTimeout = TimeSpan.FromMinutes(4),
            //    SchedulePollingInterval = TimeSpan.FromSeconds(15), //�뼶������Ҫ���ö̵㣬һ�������������Ĭ��ʱ�䣬Ĭ��15��
            //    ShutdownTimeout = TimeSpan.FromMinutes(30), //��ʱʱ��
            //    Queues = queues, //����
            //    WorkerCount = Math.Max(Environment.ProcessorCount, 40) //�����߳�������ǰ���������̣߳�Ĭ��20
            //});

            var hangfireStartUpPath = JsonConfig.GetSection("HangfireStartUpPath").Get<string>();
            if (string.IsNullOrWhiteSpace(hangfireStartUpPath)) hangfireStartUpPath = "/job";


            var dashbordConfig = new DashboardOptions
            {
                AppPath = "#",
                DisplayStorageConnectionString = false,
                IsReadOnlyFunc = Context => false
            };
            var dashbordUserName = JsonConfig.GetSection("HangfireUserName").Get<string>();
            var dashbordPwd = JsonConfig.GetSection("HangfirePwd").Get<string>();
            if (!string.IsNullOrEmpty(dashbordPwd) && !string.IsNullOrEmpty(dashbordUserName))
            {
                dashbordConfig.Authorization = new[]
                {
                    new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                    {
                        RequireSsl = false,
                        SslRedirect = false,
                        LoginCaseSensitive = true,
                        Users = new[]
                        {
                            new BasicAuthAuthorizationUser
                            {
                                Login = dashbordUserName,
                                PasswordClear = dashbordPwd
                            }
                        }
                    })
                };
            }
            app.UseHangfireDashboard(hangfireStartUpPath, dashbordConfig);

            var hangfireReadOnlyPath = JsonConfig.GetSection("HangfireReadOnlyPath").Get<string>();
            if (!string.IsNullOrWhiteSpace(hangfireReadOnlyPath))
            {
                //ֻ����壬ֻ�ܶ�ȡ���ܲ���
                app.UseHangfireDashboard(hangfireReadOnlyPath, new DashboardOptions
                {
                    IgnoreAntiforgeryToken = true,
                    AppPath = hangfireStartUpPath, //����ʱ��ת�ĵ�ַ
                    DisplayStorageConnectionString = false, //�Ƿ���ʾ���ݿ�������Ϣ
                    IsReadOnlyFunc = Context => true
                });
            }

            app.UseAppStartup();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("OK.");
            });
        }

        #region ˽�з���

        private void Configuration(IGlobalConfiguration globalConfiguration)
        {
            globalConfiguration
                .UseSqlServerStorage(JsonConfig.GetSection("HangfireSqlserverConnectionString").Get<string>(), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    UsePageLocksOnDequeue = true,
                    DisableGlobalLocks = true
                })
                .UseTagsWithSql()
                .UseConsole(new ConsoleOptions()
                {
                    BackgroundColor = "#000079"
                })
                .UseHangfireHttpJob(new HangfireHttpJobOptions
                {
                    MailOption = new MailOption
                    {
                        Server = JsonConfig.GetSection("HangfireMail:Server").Get<string>(),
                        Port = JsonConfig.GetSection("HangfireMail:Port").Get<int>(),
                        UseSsl = JsonConfig.GetSection("HangfireMail:UseSsl").Get<bool>(),
                        User = JsonConfig.GetSection("HangfireMail:User").Get<string>(),
                        Password = JsonConfig.GetSection("HangfireMail:Password").Get<string>(),
                    },
                    DefaultRecurringQueueName = JsonConfig.GetSection("DefaultRecurringQueueName").Get<string>(),
                    DefaultBackGroundJobQueueName = "DEFAULT",
                    //DefaultTimeZone = TZConvert.GetTimeZoneInfo("Asia/Shanghai"), //����ָ�������������jobʱ��ʱ��
                    // RecurringJobTimeZone = TimeZoneInfo.Local
                    // CheckHttpResponseStatusCode = code => (int)code < 400   //===��(default)
                });
        }


        #endregion

    }
}
