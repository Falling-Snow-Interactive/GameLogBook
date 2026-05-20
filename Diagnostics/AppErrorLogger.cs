using System.Text;
using Microsoft.Maui.Storage;

namespace GameLogBook.Diagnostics;

internal static class AppErrorLogger
{
    private static readonly Lock SyncRoot = new();
    private static bool isInitialized;

    public static string LogFilePath =>
        Path.Combine(FileSystem.Current.AppDataDirectory, "applog.txt");

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            if (isInitialized)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
            isInitialized = true;
        }
    }

    public static void Log(string source, Exception exception)
    {
        WriteEntry(source, exception.ToString());
    }

    public static void Log(string source, string message)
    {
        WriteEntry(source, message);
    }

    private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
        {
            Log("AppDomain.UnhandledException", exception);
            return;
        }

        Log("AppDomain.UnhandledException", args.ExceptionObject?.ToString() ?? "Unknown unhandled exception object.");
    }

    private static void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Log("TaskScheduler.UnobservedTaskException", args.Exception);
    }

    private static void WriteEntry(string source, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);

            StringBuilder builder = new();
            builder.AppendLine($"[{DateTimeOffset.Now:O}] {source}");
            builder.AppendLine(message);
            builder.AppendLine(new string('-', 80));

            lock (SyncRoot)
            {
                File.AppendAllText(LogFilePath, builder.ToString());
            }
        }
        catch
        {
        }
    }
}
