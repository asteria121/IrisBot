using Discord;
using Discord.WebSocket;

namespace IrisBot
{
    public class CustomLog
    {
        private static object _MessageLock = new object(); // ThreadSafe 상태로 color를 변경하기 위함

        public static async Task PrintLog(LogSeverity logLevel, string source, string text)
        {
            string ExceptionDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
            string FileName = $"[{DateTime.Now.ToString("yyyy-MM-dd")}]_Bot.log"; // ..\Log\[2023-02-16]_Bot.log
            
            try
            {
                if (!Directory.Exists(ExceptionDirectory))
                    Directory.CreateDirectory(ExceptionDirectory);

                using (StreamWriter sw = new StreamWriter(Path.Combine(ExceptionDirectory, FileName), true))
                {
                    await sw.WriteLineAsync($"{DateTime.Now.ToString("HH:mm:ss")} [{logLevel}] {source}\t{text}");
                }
            }
            catch
            {
                
            }

            lock (_MessageLock) // ThreadSafe 상태로 color를 변경하기 위함
            {
                Console.Write(DateTime.Now.ToString("HH:mm:ss"));
                switch (logLevel)
                {
                    case LogSeverity.Critical:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(" [CRITICAL] ");
                        break;
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(" [ERROR] ");
                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(" [WARN] ");
                        break;
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" [INFO] ");
                        break;
                    case LogSeverity.Verbose:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" [VERBOSE] ");
                        break;
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" [DEBUG] ");
                        break;
                }
                Console.ResetColor();
                Console.Write($"{source}\r\t\t\t\t{text}{Environment.NewLine}");
            }
        }

        /// <summary>
        /// Exception 처리 함수. 콘솔 및 로그 파일로 예외를 출력한다.
        /// </summary>
        /// <param name="ex">Exception</param>
        public async static Task ExceptionHandler(Exception ex)
        {
            try
            {
                lock (_MessageLock) // ThreadSafe 상태로 color를 변경하기 위함
                {
                    Console.Write(DateTime.Now.ToString("HH:mm:ss"));
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(" [EXCEPTION] ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Exception occured. See detailed message below.\r\n");
                    Console.WriteLine(ex.ToString());
                    Console.ResetColor();
                }

                if (!string.Equals(ex.GetType().ToString(), "Discord.WebSocket.GatewayReconnectException") || string.Equals(ex.Message, "WebSocket connection was closed")) // Discord는 한번씩 재접속을 요청한다.
                {
                    string ExceptionDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exception");
                    string FileName = $"[{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}]_Exception.log"; // ..\Exception\[2023-02-16-13-51-40]_Exception.log
                    if (!Directory.Exists(ExceptionDirectory))
                        Directory.CreateDirectory(ExceptionDirectory);

                    using (StreamWriter sw = new StreamWriter(Path.Combine(ExceptionDirectory, FileName)))
                    {
                        await sw.WriteLineAsync(ex.ToString());
                    }
                }
            }
            catch
            {
                
            }
        }
    }
}
