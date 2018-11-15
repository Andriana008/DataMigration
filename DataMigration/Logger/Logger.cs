using System.Threading.Tasks;

namespace DataMigration.Logger
{
    public class Logger
    {
        private readonly MainWindow _window;

        private readonly object _locker = new object();
        public Logger(MainWindow window) { _window = window; }

        public Logger() { }

        public Logger(Logger logger)
        {
            _window = logger._window;
        }

        public async void WriteLog(LogLevel l, string message)
        {
             await Task.Run(() => _window.Dispatcher.Invoke(() => _window.OutputBlock.Inlines.Add(l + ":" + message)));
        }
    }
    
}
