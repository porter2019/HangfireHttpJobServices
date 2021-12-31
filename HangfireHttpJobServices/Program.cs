using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.HttpJob;
using Hangfire.SqlServer;
using Hangfire.Tags.SqlServer;
using NLog;
using NLog.Web;
using System.Text.RegularExpressions;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        ContentRootPath = Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
        Args = args
    });
    builder.Host.UseWindowsService();

    //程序根目录必须这样取
    logger.Info($"程序根目录:{AppContext.BaseDirectory}");

    #region 设置监听端口

    var port = 2810;
    //dotnet xxx.dll -p 端口号
    if (args.Length > 1 && args[0].ToLower() == "-p")
    {
        string portStr = args[1];
        if (Regex.IsMatch(portStr, @"^\d*$")) port = Convert.ToInt32(portStr);
    }
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(port);
        serverOptions.Limits.MaxRequestBodySize = null;
    });

    #endregion

    //Nlog
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    #region Hanfire

    builder.Services.AddHangfire(globalConfiguration =>
    {
        globalConfiguration
                .UseSqlServerStorage(builder.Configuration.GetSection("HangfireSqlserverConnectionString").Get<string>(), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    UsePageLocksOnDequeue = true,
                    DisableGlobalLocks = true
                })
                .UseTagsWithSql()
                .UseHangfireHttpJob(new HangfireHttpJobOptions
                {
                    MailOption = new MailOption
                    {
                        Server = builder.Configuration.GetSection("HangfireMail:Server").Get<string>(),
                        Port = builder.Configuration.GetSection("HangfireMail:Port").Get<int>(),
                        UseSsl = builder.Configuration.GetSection("HangfireMail:UseSsl").Get<bool>(),
                        User = builder.Configuration.GetSection("HangfireMail:User").Get<string>(),
                        Password = builder.Configuration.GetSection("HangfireMail:Password").Get<string>(),
                    },
                    DefaultRecurringQueueName = builder.Configuration.GetSection("DefaultRecurringQueueName").Get<string>(),
                    DefaultBackGroundJobQueueName = "DEFAULT",
                });
    });

    builder.Services.AddHangfireServer(options =>
    {
        options.ServerTimeout = TimeSpan.FromMinutes(4);
        options.SchedulePollingInterval = TimeSpan.FromSeconds(15);//秒级任务需要配置短点，一般任务可以配置默认时间，默认15秒
        options.ShutdownTimeout = TimeSpan.FromMinutes(30);//超时时间
        options.Queues = builder.Configuration.GetSection("HangfireQueues").Get<List<string>>().ToArray();
        options.WorkerCount = Math.Max(Environment.ProcessorCount, 40);//工作线程数，当前允许的最大线程，默认20
    });

    #endregion

    var app = builder.Build();

    //强制显示中文
    //Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");

    var hangfireStartUpPath = app.Configuration.GetSection("HangfireStartUpPath").Get<string>();
    if (string.IsNullOrWhiteSpace(hangfireStartUpPath)) hangfireStartUpPath = "/job";

    var dashbordConfig = new DashboardOptions
    {
        AppPath = "#",
        DisplayStorageConnectionString = false,
        IsReadOnlyFunc = Context => false
    };
    var dashbordUserName = app.Configuration.GetSection("HangfireUserName").Get<string>();
    var dashbordPwd = app.Configuration.GetSection("HangfirePwd").Get<string>();
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

    var hangfireReadOnlyPath = app.Configuration.GetSection("HangfireReadOnlyPath").Get<string>();
    if (!string.IsNullOrWhiteSpace(hangfireReadOnlyPath))
    {
        //只读面板，只能读取不能操作
        app.UseHangfireDashboard(hangfireReadOnlyPath, new DashboardOptions
        {
            IgnoreAntiforgeryToken = true,
            AppPath = hangfireStartUpPath, //返回时跳转的地址
            DisplayStorageConnectionString = false, //是否显示数据库连接信息
            IsReadOnlyFunc = Context => true
        });
    }

    app.UseAppStartup(app.Lifetime);

    app.MapGet("/", () => "hangfire is running...");

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "系统无法启动");
    throw;
}
finally
{
    LogManager.Shutdown();
}