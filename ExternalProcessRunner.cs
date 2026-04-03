namespace ModelTest;

using System.Diagnostics;
using System.Text;

public class ExternalProcessRunner
{
    public static void RunWithConsole(string exePath, string arguments = "")    // 弹窗运行外部程序
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            CreateNoWindow = false,          // 弹出新窗口
            UseShellExecute = true,        // 使用shell
        };
        
        using var process = new Process { StartInfo = processInfo };

        process.Start();
        process.WaitForExit();
    }

    /// <summary>
    /// 运行外部程序并实时显示输出
    /// </summary>
    /// <param name="exePath">可执行文件路径</param>
    /// <param name="arguments">命令行参数</param>
    /// <param name="timeoutMs">超时时间（毫秒），-1表示无限等待</param>
    /// <returns>进程退出码</returns>
    public static int Run(string exePath, string arguments = "", int timeoutMs = -1)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var processInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            CreateNoWindow = true,          // 不弹出新窗口
            UseShellExecute = false,        // 不使用shell
            RedirectStandardOutput = true,  // 重定向输出
            RedirectStandardError = true,   // 重定向错误
            RedirectStandardInput = false,  // 通常不需要输入
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default
        };
        
        using var process = new Process { StartInfo = processInfo };

        // 使用AutoResetEvent确保输出处理完成
        using var outputWaitHandle = new AutoResetEvent(false);
        using var errorWaitHandle = new AutoResetEvent(false);
        
        // 实时输出处理
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data == null)
                outputWaitHandle.Set();
            else
                Console.WriteLine(e.Data);
        };
        
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data == null)
                errorWaitHandle.Set();
            else
                Console.Error.WriteLine(e.Data);
        };
        
        try
        {
            // 启动进程
            if (!process.Start())
            {
                throw new InvalidOperationException($"无法启动进程: {exePath}");
            }
            
            // 开始异步读取输出
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 等待进程结束（带超时）
            bool exited;
            if (timeoutMs > 0)
                exited = process.WaitForExit(timeoutMs);
            else
            {
                process.WaitForExit();
                exited = true;
            }
            
            if (!exited)
            {
                // 超时则杀死进程
                process.Kill();
                Console.Error.WriteLine($"进程执行超时 ({timeoutMs}ms)，已被终止");
                return -1;
            }
            
            // 等待输出读取完成
            outputWaitHandle.WaitOne(timeoutMs > 0 ? timeoutMs : 5000);
            errorWaitHandle.WaitOne(timeoutMs > 0 ? timeoutMs : 5000);
            
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"执行失败: {ex.Message}");
            return -1;
        }
    }
    
    /// <summary>
    /// 异步版本
    /// </summary>
    public static async Task<int> RunAsync(string exePath, string arguments = "", int timeoutMs = -1)
    {
        return await Task.Run(() => Run(exePath, arguments, timeoutMs));
    }
}