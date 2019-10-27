using System;

namespace LiveStream
{
    public static class Logger
    {
        public static void Info<T>(string text)
        {
            Log<T>(text, Severity.Info);    
        }
        
        public static void Debug<T>(string text)
        {
            Log<T>(text, Severity.Debug);    
        }
        
        public static void Warning<T>(string text)
        {
            Log<T>(text, Severity.Warning);    
        }

        public static void Error<T>(string text)
        {
            Log<T>(text, Severity.Error);    
        }
        
        private static void Log<T>(string text, Severity logLevel)
        {
            Console.Error.WriteLine($"[{DateTime.Now.ToString()}][{logLevel}][{typeof(T).Name}]: {text}");
        }
        
        private enum Severity
        {
            Info,
            Warning,
            Error,
            Debug
        }
    }
}