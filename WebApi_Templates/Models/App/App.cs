using WebApi_Templates.Models.XWebSocket;

namespace WebApi_Templates;

public static class App
{
    //当前程序接口支持的版本
    public static List<double> ApiVersions = new();

    //当前程序的工作目录
    public static string AppPath = AppDomain.CurrentDomain.BaseDirectory;

    //当前程序的配置文件名
    public static string AppConfigFileName = "appsettings.json";

    //当前程序的配置文件路径
    public static string AppConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConfigFileName);

    //当前程序的日志文件路径
    public static string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

    //服务提供
    public static IServiceProvider Service = null;

    //WebSocketServer管理器
    public static WebSocketServerManager WebSocketServerManager = new ();

    // 机密字符串使用此类型保存
    // public static SecureString SecureString;
}