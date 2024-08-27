using System.Runtime.CompilerServices;
using Yitter.IdGenerator;

namespace WebApi_Templates.Utils.UniqueKeyUtils;

public class UniqueKeyUtil
{
    //该方法同一时间只允许一个线程使用
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static string GetGuid()
    {
        return Guid.NewGuid().ToString("N");
    }

    //该方法同一时间只允许一个线程使用
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static long GetSnowID()
    {
        return YitIdHelper.NextId();
    }
}