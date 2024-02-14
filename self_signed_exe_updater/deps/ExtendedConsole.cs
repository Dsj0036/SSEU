
namespace System
{
    public static class ExConsole
    {
        private static void WL(object s) => System.Console.WriteLine(s);
        private static void WLC(object s, ConsoleColor color)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(s);
            System.Console.ForegroundColor = ConsoleColor.White;
        }
        public static void Log(string senderName, string msg, ConsoleColor color = ConsoleColor.White)
        {
            var dtn = DateTime.Now.ToString("H-mm-ss");
            var frm = $"[{dtn}] {senderName} : {msg}";
            Log(frm, color);
        }
        public static void Log(string msg) => WL(msg);
        public static void Log(string msg, ConsoleColor color) => WLC(msg, color);
        public static void LogValues<T>(T[] values, ConsoleColor color = ConsoleColor.White)
        {
            foreach (T t in values) Log(t.ToString() ?? "[null]", color);
        }
        public static void LogException(Exception e)
        {
            var cl = ConsoleColor.Red;
            Log("error", e.Message, cl);
            Log("info", "type: " + e.GetType().FullName, cl);
            Log("info", "hresult: " + e.HResult, cl);

        }
        public static string ReadLine()
        {
            return System.Console.ReadLine() ?? "";
        }
        public static void Break() => Log(Environment.NewLine);
        public static void Clear() => System.Console.Clear();
        public static string Write(string msg, ConsoleColor e = ConsoleColor.White)
        {
            System.Console.ForegroundColor = e;
            System.Console.Write(msg);
            System.Console.ForegroundColor = ConsoleColor.White;
            return msg;
        }
        public static char Write(char ch, ConsoleColor e = ConsoleColor.White)
        {
            System.Console.ForegroundColor = e;
            System.Console.Write(ch);
            System.Console.ForegroundColor = ConsoleColor.White;
            return ch;
        }
    }
}
