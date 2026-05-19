using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Logging
{
    public sealed class FileLogManager : ILogManager
    {
        readonly ILog _logger;

        static FileLogManager()
        {
            log4net.GlobalContext.Properties["LogFolderPath"] = Constants.LOG_FOLDERNAME;
            // Gets directory path of the calling application
            // RelativeSearchPath is null if the executing assembly i.e. calling assembly is a
            // stand alone exe file (Console, WinForm, etc). 
            // RelativeSearchPath is not null if the calling assembly is a web hosted application i.e. a web site
            var log4NetConfigDirectory = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;

            var log4NetConfigFilePath = Path.Combine(log4NetConfigDirectory, Constants.LOG_CONFIGURATION_FILENAME);
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(log4NetConfigFilePath));
        }
        /// <summary>
        /// Hack method to forcefully write log from anywhere
        /// </summary>
        /// <param name="forematString"></param>
        public static void ForceLog(string formatString, params object[] arguments)
        {
            var logger = LogManager.GetLogger("");
            logger.Info(string.Format(formatString, arguments));
        }

        public FileLogManager(Type logClass)
        {
            _logger = LogManager.GetLogger(logClass);
        }


        public void Fatal(string errorMessage)
        {
            if (_logger.IsFatalEnabled)
                _logger.Fatal(errorMessage);
        }

        public void Error(string errorMessage)
        {
            if (_logger.IsErrorEnabled)
                _logger.Error(errorMessage);
        }

        public void Warn(string message)
        {
            if (_logger.IsWarnEnabled)
                _logger.Warn(message);
        }

        public void Info(string message)
        {
            if (_logger.IsInfoEnabled)
                _logger.Info(message);
        }

        public void Debug(string message)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug(message);
        }
    }
}
