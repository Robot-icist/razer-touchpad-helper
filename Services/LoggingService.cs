using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace RazerTouchpadHelper.Services
{
    public sealed class LoggingService : IDisposable
    {
        private readonly string _logPath;
        private readonly StreamWriter _writer;
        private readonly object _sync = new object();
        private readonly Queue<string> _pending = new Queue<string>();

        public LoggingService(string logDirectory)
        {
            Directory.CreateDirectory(logDirectory);
            _logPath = Path.Combine(logDirectory, $"razer-touchpad-helper-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            _writer = new StreamWriter(_logPath, append: false, System.Text.Encoding.UTF8) { AutoFlush = true };
        }

        public string LogPath => _logPath;

        public void Write(string line)
        {
            string full = $"{DateTime.Now:HH:mm:ss.fff} {line}";
            lock (_sync)
            {
                _pending.Enqueue(full);
                try { _writer.WriteLine(full); } catch { }
            }
        }

        public IEnumerable<string> DrainPending()
        {
            lock (_sync)
            {
                while (_pending.Count > 0)
                    yield return _pending.Dequeue();
            }
        }

        public void Dispose()
        {
            try { _writer.Flush(); } catch { }
            try { _writer.Dispose(); } catch { }
        }
    }
}