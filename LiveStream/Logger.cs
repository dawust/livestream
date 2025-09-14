using System;

namespace LiveStream
{
    public class Logger<T>
    {
        public void Info(string text)
        {
            Log(text, Severity.Info);    
        }
        
        public void Debug(string text)
        {
            Log(text, Severity.Debug);    
        }
        
        public void Warning(string text)
        {
            Log(text, Severity.Warning);    
        }

        public void Error(string text)
        {
            Log(text, Severity.Error);    
        }
        
        private void Log(string text, Severity logLevel)
        {
            Console.Error.WriteLine($"[{DateTime.UtcNow.ToString()}][{logLevel}][{typeof(T).Name}]: {text}");
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