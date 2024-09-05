namespace Phi_MGUS;

public class LogManager
{
    private static bool logFileLock = false;
    public static void WriteLog(string text,LogLevel level = LogLevel.Info)
    {
        JustLogWrite(text,level);
        if ((level == LogLevel.Debug && Program.config.isDebug) || level != LogLevel.Debug)
        {
            JustConsoleWrite(text,level);
        }
    }
    
    
    
    /// <summary>
    /// 仅写入日志到控制台（指定颜色）
    /// </summary>
    /// <param name="text"></param>
    /// <param name="color"></param>
    public static void JustConsoleWrite(string text,ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
    /// <summary>
    /// 仅写入日志到控制台（指定日志等级）
    /// </summary>
    /// <param name="text"></param>
    /// <param name="level"></param>
    public static void JustConsoleWrite(string text,LogLevel level = LogLevel.Info)
    {
        ConsoleColor color;
        switch (level)
        {
            case LogLevel.Info:
                color = ConsoleColor.White;
                break;
            case LogLevel.Warning:
                color = ConsoleColor.Yellow;
                break;
            case LogLevel.Error:
                color = ConsoleColor.Red;
                break;
            case LogLevel.Fatal:
                color = ConsoleColor.DarkRed;
                break;
            case LogLevel.Debug:
                color = ConsoleColor.DarkGray;
                break;
            default:
                color = ConsoleColor.White;
                break;
        }
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] [{level}] {text}");
        Console.ResetColor();
    }
    
    /// <summary>
    /// 仅写入日志到文件
    /// </summary>
    /// <param name="text"></param>
    /// <param name="level"></param>
    public static void JustLogWrite(string text,LogLevel level)
    {
        while (logFileLock)
        {
            
        }
        logFileLock = true;
        //获取当前年月日，不获取时分秒，以便在日志中只记录当天的日志
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        string logPath = "Logs\\" + date + ".log";
        //如果日志文件夹不存在，则创建一个
        if (!Directory.Exists("Logs"))
        {
            Directory.CreateDirectory("Logs");
        }
        //如果日志文件不存在，则创建一个
        if (!File.Exists(logPath))
        {
            File.Create(logPath).Dispose();
        }
        //写入日志文件，前缀添加时间年月日时分秒，以及日志等级
        File.AppendAllText(logPath,$"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] [{level}] {text}\r\n");
        //File.AppendAllText(logPath,text+"\r\n");
        logFileLock = false;
    }
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Fatal,
        Debug
    }
}