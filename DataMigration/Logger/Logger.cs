namespace DataMigration.Loggers
{
    public class Logger
    {
        private MainWindow _window;

        private object _locker = new object();
        public Logger(MainWindow window) { this._window = window; }

        public Logger() { }

        public Logger(Logger logger)
        {
            this._window = logger._window;
        }

        public void WriteLog(LogLevel l, string message)
        {
            lock (_locker)
            {
                _window.OutputBlock.Inlines.Add(l + ":" + message);
            }
        }
    }
    
}
