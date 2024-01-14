using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectronicCertificateUpload.Core;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;

namespace ElectronicCertificateUpload.Services
{
    internal class LogController : ILogController
    {
        ILoggerRepository baseLoggerRepository;
        ILayout textLayout;
        FileAppender applicationFileAppender;
        Logger applicationLogger;

        internal bool LogEnabledFlag { get; set; }
        internal string LogFilePath { get; set; }
        internal string LogLevel { get; set; }

        internal LogController()
        {
            LogEnabledFlag = Provider.LogEnabledFlag;
            LogFilePath = Provider.LogFilePath + AppDomain.CurrentDomain.FriendlyName.Replace("exe", "log");
            LogLevel = Provider.LogLevel;

            if (LoggerManager.GetAllRepositories().FirstOrDefault(x => x.Name == "Base") != null)
                baseLoggerRepository = LoggerManager.GetRepository("Base");
            else
                baseLoggerRepository = LoggerManager.CreateRepository("Base");

            baseLoggerRepository.Configured = LogEnabledFlag;
            //baseLoggerRepository.Threshold = (Level)OptionConverter.ConvertStringTo(typeof(Level), Provider.LogLevel);

            //%date{yyyy-MM-dd HH:mm:ss}
            textLayout = new PatternLayout("%message%newline%newline");

            applicationFileAppender = new FileAppender();
            applicationFileAppender.Name = "ApplictionFlatFile";
            applicationFileAppender.Layout = textLayout;
            applicationFileAppender.AppendToFile = true;
            applicationFileAppender.File = LogFilePath;

            applicationLogger = (Logger)LoggerManager.GetLogger("Base", "Application");

            if (LogEnabledFlag == true)
            {
                applicationLogger.Level = applicationLogger.Hierarchy.LevelMap[LogLevel];

                if (applicationLogger.GetAppender("ApplictionFlatFile") != null)
                {
                    applicationLogger.RemoveAppender("ApplictionFlatFile");
                }

                applicationLogger.AddAppender(applicationFileAppender);

                applicationFileAppender.ActivateOptions();
            }
        }

        public void WriteDebug(LogMessageKind logMessageArg)
        {
            applicationLogger.Log(Level.Debug, JsonConvert.SerializeObject(logMessageArg), null);
        }

        public void WriteError(LogMessageKind logMessageArg)
        {
            applicationLogger.Log(Level.Error, JsonConvert.SerializeObject(logMessageArg), null);
        }
    }
}
