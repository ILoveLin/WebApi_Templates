using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Asp.Versioning;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApi_Templates.Models.Conventions;
using WebApi_Templates.Models.Filters;
using WebApi_Templates.Models.Middlewares;
using WebApi_Templates.Models.Providers;
using WebApi_Templates.Utils.LogUtils;
using WebApi_Templates.Utils.NullUtils;
using WebApi_Templates.Utils.SecurityUtil;
using Yitter.IdGenerator;
using ILogger = Serilog.ILogger;

namespace WebApi_Templates;

public partial class Program
{
    private static WebApplicationBuilder? builder;

    private static WebApplication? app;

    public static void Main(string[] args)
    {
        App.AppPath = AppDomain.CurrentDomain.BaseDirectory;
        App.AppConfigFileName = "appsettings.json";
        App.AppConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.AppConfigFileName);
        App.LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        ConfigureSupportApiVersion();


        try
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder().SetBasePath(App.AppPath)
                    .AddJsonFile(App.AppConfigFileName)
                    .Build())
                .CreateBootstrapLogger();
        }
        catch (Exception e)
        {
            LogUtil.EnsureLogPathExist();
            //因为日志没有初始化成功，所以手动写入日志文件
            File.WriteAllText(Path.Combine(App.LogPath, "logError.txt"), e.Message + "\n" + e.StackTrace);
            Console.Write(e.Message + "\n" + e.StackTrace);
            return;
        }

        try
        {
            BuilderConfig(args);
            if (!AppConfig()) Log.Logger.Fatal("程序配置异常！");
        }
        catch (Exception e)
        {
            Log.Fatal(e, "程序异常退出！");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }


    private static void BuilderConfig(string[] args)
    {
        builder = WebApplication.CreateBuilder(args);

        #region 配置日志

        builder.Services.AddSerilog((serviceProvider, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(serviceProvider);
        });

        #endregion

        #region 配置HTTPS

        var pfxpath = builder.Configuration.GetValue<string>("PfxPath");
        var pfxkey = builder.Configuration.GetValue<string>("PfxKey");
        if (!pfxkey.IsNullOrEmpty() && !pfxpath.IsNullOrEmpty())
            builder.WebHost.UseKestrel(opts => { opts.ConfigureHttpsDefaults(options => { options.ServerCertificate = new X509Certificate2(pfxpath, SecurityUtil.Decrypt(pfxkey)); }); });

        #endregion

        #region 配置本地化

        builder.Services.AddJsonLocalization(opts => { opts.ResourcesPath = "I18N"; });
        builder.Services.AddRequestLocalization(opts =>
        {
            opts.DefaultRequestCulture = new RequestCulture("zh-cn");
            opts.SupportedCultures = [new CultureInfo("zh-cn"), new CultureInfo("en-us")];
            opts.SupportedUICultures = [new CultureInfo("zh-cn"), new CultureInfo("en-us")];
            opts.RequestCultureProviders = [new StringRequestCultureProvider()];
        });

        #endregion

        #region 配置版本管理

        var apiVersioningBuilder = builder.Services.AddApiVersioning(opts =>
        {
            opts.DefaultApiVersion = new ApiVersion(1, 0);
            opts.AssumeDefaultVersionWhenUnspecified = true;
            opts.ApiVersionReader = new QueryStringApiVersionReader("ver");
            opts.ReportApiVersions = true;
        }).AddMvc(opts =>
        {
            //使用自定义的ApiVersionConventionBuilder替代默认的ApiVersionConventionBuilder
            opts.Conventions = new ApiVersionsBuilder();
        });

        #endregion

        #region 配置Filter，中间件

        builder.Services.Configure<ApiBehaviorOptions>(opt => { opt.SuppressModelStateInvalidFilter = true; });

        builder.Services.AddControllers(opts =>
        {
            opts.Filters[0] = new UnsupportMediaTypeFilter(); //使用自定义MediaType类型处理
            opts.Filters.Add<ResultFilter>();
            //如果使用insert方法将自定义过滤器插入首位，此时会违反内部过滤器执行顺序,这是内部会根据过滤器变量order大小来判断谁先执行(order越小，执行顺序越靠前)
            //因需要此过滤器的OnActionExecuted最后执行，所以使用insert插入首位
            opts.Filters.Add<ExceptionFilter>(); //使用自定义异常处理
            // opts.Filters.Add<BaseParamsFilter>(); //全局增加公共参数
            opts.Filters.Add<InputModelFilter>(); //使用自定义模型输入处理
            opts.Filters.Add<NeedLoginAttribute>(); //全局启用登录
        }).AddNewtonsoftJson(opts =>
        {
            opts.AllowInputFormatterExceptionMessages = true;
            opts.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ssZ"; // 设置UTC时间格式
            opts.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; // 忽略循环引用
            opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); // 返回的格式按照小驼峰形式
            opts.SerializerSettings.NullValueHandling = NullValueHandling.Include; // 包含空值
        });

        #endregion

        #region 配置跨域

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
                    .SetIsOriginAllowed(str => { return true; });
            });
        });

        #endregion

        #region 配置MediatR

        builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(Program).Assembly); });

        #endregion

        #region 配置数据库

        // builder.Services.AddDbContext<XXXContext>(op =>
        // {
        //     op.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        //     op.EnableSensitiveDataLogging();
        // });

        #endregion

#if DEBUG

        #region 配置Swagger

        if (builder.Environment.IsDevelopment())
        {
            apiVersioningBuilder.AddApiExplorer(opts => { opts.GroupNameFormat = "'v'VVV"; });
            builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            builder.Services.AddSwaggerGen();
        }

        #endregion

#endif
    }

    private static bool AppConfig()
    {
        app = builder.Build();

        app.UseMiddleware<TraceIdMiddleware>();

        #region 使Request可重复读取

        app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next(context);
        });

        #endregion

#if DEBUG

        #region 添加Swagger

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var description in app.DescribeApiVersions())
                {
                    var url = $"/swagger/{description.GroupName}/swagger.json";
                    var name = description.GroupName.ToUpperInvariant();
                    options.SwaggerEndpoint(url, name);
                }
            });
        }

        #endregion

#endif

        app.UseWebSockets();
        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseSerilogRequestLogging(SerilogRequestLoggingConfigure);
        app.UseRouting();
        app.UseCors();
        app.MapControllers();

        if (!AppBeforeRunningFun()) return false;

        app.Run();
        return true;
    }

    private static bool AppBeforeRunningFun()
    {
        var logger = app.Services.GetRequiredService<ILogger>();
        logger.Information($"启动时间{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

        App.Service = app.Services;

        #region 配置雪花算法

        #region 获取雪花算法WorkerID

        var snowConfiguration = new ConfigurationBuilder().SetBasePath(App.AppPath)
            .AddJsonFile("SnowFlake.json").Build();
        var snowWorkerIDStr = snowConfiguration["SnowFlakeWorkerID"];
        if (!ushort.TryParse(snowWorkerIDStr, out var snowWorkerID))
        {
            logger.Error("雪花算法WorkerID解析失败！请检查SnowFlake.json！");
            return false;
        }

        if (snowWorkerID < 0 || snowWorkerID > 63)
        {
            logger.Error("雪花算法WorkerID允许的值为0-63！请检查SnowFlake.json！");
            return false;
        }

        var options = new IdGeneratorOptions(snowWorkerID);

        // 默认值6，限定 WorkerId 最大值为2^6-1，即默认最多支持64个节点。
        //注意该值在已有雪花id的数据存储后，只能增大，禁止减小
        // options.WorkerIdBitLength = 10;

        // 默认值6，限制每毫秒生成的ID个数。若生成速度超过5万个/秒，建议加大 SeqBitLength 到 10。
        //注意该值在已有雪花id的数据存储后，只能增大，禁止减小
        options.SeqBitLength = 10;
        logger.Information($"当前程序雪花算法WorkerID为{snowWorkerID}，SeqBitLength为{options.SeqBitLength}");
        YitIdHelper.SetIdGenerator(options);

        #endregion

        #region 确保程序启动时间大于上一次停止时间

        //确保程序启动时间大于上一次停止时间
        if (File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}/start_lock"))
        {
            var timeStr = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}/start_lock");
            if (!DateTime.TryParse(timeStr, out var time))
            {
                logger.Error("时间检验失败！请检查start_lock文件！");
                return false;
            }

            var now = DateTime.Now;
            if (now.CompareTo(time) <= 0)
            {
                logger.Error("时间检验失败！程序启动的时间必须大于上次程序停止的时间，请检查电脑时间是否正确！");
                logger.Error($"上次程序停止时间为{time.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
                return false;
            }
        }

        #endregion

        #region 程序退出时将当前时间保存进start_lock文件中

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        #endregion

        #endregion

        return true;
    }
}