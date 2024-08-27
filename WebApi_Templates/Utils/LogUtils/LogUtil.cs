namespace WebApi_Templates.Utils.LogUtils;

public class LogUtil
{
    // 确保日志路径存在
    public static void EnsureLogPathExist()
    {
        var logPath = Path.Combine(App.AppPath, "logs");
        if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
    }
}