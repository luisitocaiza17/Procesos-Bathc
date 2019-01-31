using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.ServiceModel;

namespace SW.Common
{
    public static class ExceptionManager
    {
        #region "Exception"

        public static string ReportException(Exception ex, ExceptionSources source)
        {
            return ReportException(ex, source, SGS.ErrorLogPath);
        }
        public static string ReportException(ExceptionDetail ex, ExceptionSources source)
        {
            return ReportException(ex, source, SGS.ErrorLogPath);
        }
        public static string ReportException(Exception exception, ExceptionSources source, string ErrorLogFilesFolder)
        {
            string ErrorLogFile = "";
            string ClientCode = "";
            ErrorLogFilesFolder = AppDomain.CurrentDomain.BaseDirectory + @"Errors\";
            if (!System.IO.Directory.Exists(ErrorLogFilesFolder))
                System.IO.Directory.CreateDirectory(ErrorLogFilesFolder);

            while (true)
            {
                string Prefix = source == ExceptionSources.Server ? "S_" : "C_";
                string Token = Guid.NewGuid().ToString();
                ClientCode = Prefix + Token.Substring(Token.Length - 6, 6);

                ErrorLogFile = ErrorLogFilesFolder + ClientCode + ".log";

                if (!System.IO.File.Exists(ErrorLogFile))
                    break;
            }

            #region logeo v2
            StringBuilder error = new StringBuilder();

            //error.AppendLine("Application:       " + Application.ProductName);
            //error.AppendLine("Version:           " + Application.ProductVersion);
            error.AppendLine("Date:              " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            //error.AppendLine("Computer name:     " + SystemInformation.ComputerName);
            //error.AppendLine("User name:         " + SystemInformation.UserName);
            error.AppendLine("OS:                " + Environment.OSVersion.ToString());
            error.AppendLine("Culture:           " + CultureInfo.CurrentCulture.Name);
            //error.AppendLine("Resolution:        " + SystemInformation.PrimaryMonitorSize.ToString());
            //error.AppendLine("System up time:    " + GetSystemUpTime());
            error.AppendLine("App up time:       " +
              (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString());

            //MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            //if (GlobalMemoryStatusEx(memStatus))
            //{
            //    error.AppendLine("Total memory:      " + memStatus.ullTotalPhys / (1024 * 1024) + "Mb");
            //    error.AppendLine("Available memory:  " + memStatus.ullAvailPhys / (1024 * 1024) + "Mb");
            //}

            error.AppendLine("");
            error.AppendLine(exception.Message);
            error.AppendLine(exception.Source);
            error.AppendLine(exception.TargetSite.ToString());
            error.AppendLine(exception.HelpLink);
            error.AppendLine("");

            error.AppendLine("Exception classes:   ");
            error.Append(GetExceptionTypeStack(exception));
            error.AppendLine("");
            error.AppendLine("Exception messages: ");
            error.Append(GetExceptionMessageStack(exception));

            error.AppendLine("");
            error.AppendLine("Stack Traces:");
            error.Append(GetExceptionCallStack(exception));

            error.AppendLine("");
            error.AppendLine("Data:");
            error.Append(GetExceptionCallStack(exception));
            error.AppendLine("");
            error.AppendLine("Loaded Modules:");
            Process thisProcess = Process.GetCurrentProcess();
            foreach (ProcessModule module in thisProcess.Modules)
            {
                error.AppendLine(module.FileName + " " + module.FileVersionInfo.FileVersion);
            }

            //for (int i = 0; i < loggers.Count; i++)
            //{
            //    loggers[i].LogError(error.ToString());
            //}
            #endregion
            System.IO.File.WriteAllText(ErrorLogFile, error.ToString(), System.Text.Encoding.Default);

            //TODO: Envío de mail a través de la cola de Mensajería
            //Enviar al servidor el error para guardarlo
            Salud.DataAccess.DataModelTableAdapters.MailingTA mailingta = new Salud.DataAccess.DataModelTableAdapters.MailingTA();
            long mailid = mailingta.Ins(string.Empty, string.Empty, "Excepcion no controlada", error.ToString(), 666, DateTime.Now, null, 13)[0].MailingID;

            return ClientCode;
        }
        public static string ReportException(ExceptionDetail exception, ExceptionSources source, string ErrorLogFilesFolder)
        {
            string ErrorLogFile = "";
            string ClientCode = "";

            if (!System.IO.Directory.Exists(ErrorLogFilesFolder))
                System.IO.Directory.CreateDirectory(ErrorLogFilesFolder);

            while (true)
            {
                string Prefix = source == ExceptionSources.Server ? "S_" : "C_";
                string Token = Guid.NewGuid().ToString();
                ClientCode = Prefix + Token.Substring(Token.Length - 6, 6);

                ErrorLogFile = ErrorLogFilesFolder + ClientCode + ".log";

                if (!System.IO.File.Exists(ErrorLogFile))
                    break;
            }
            //StringBuilder Contenido = new StringBuilder();
            //Contenido.Append(ex.Message); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //Contenido.Append(ex.Source); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //Contenido.Append(ex.StackTrace); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //Contenido.Append(ex.TargetSite); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //Contenido.Append(ex.HelpLink); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //if (ex.InnerException != null)
            //{
            //    Contenido.Append(ex.InnerException.Message); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //    Contenido.Append(ex.InnerException.Source); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //    Contenido.Append(ex.InnerException.StackTrace); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //    Contenido.Append(ex.InnerException.TargetSite); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //    Contenido.Append(ex.InnerException.HelpLink); Contenido.Append(Environment.NewLine); Contenido.Append(Environment.NewLine);
            //}

            #region logeo v2
            StringBuilder error = new StringBuilder();

            //error.AppendLine("Application:       " + Application.ProductName);
            //error.AppendLine("Version:           " + Application.ProductVersion);
            error.AppendLine("Date:              " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            //error.AppendLine("Computer name:     " + SystemInformation.ComputerName);
            //error.AppendLine("User name:         " + SystemInformation.UserName);
            error.AppendLine("OS:                " + Environment.OSVersion.ToString());
            error.AppendLine("Culture:           " + CultureInfo.CurrentCulture.Name);
            //error.AppendLine("Resolution:        " + SystemInformation.PrimaryMonitorSize.ToString());
            //error.AppendLine("System up time:    " + GetSystemUpTime());
            error.AppendLine("App up time:       " +
              (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString());

            //MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            //if (GlobalMemoryStatusEx(memStatus))
            //{
            //    error.AppendLine("Total memory:      " + memStatus.ullTotalPhys / (1024 * 1024) + "Mb");
            //    error.AppendLine("Available memory:  " + memStatus.ullAvailPhys / (1024 * 1024) + "Mb");
            //}

            error.AppendLine("");
            error.AppendLine(exception.Message);
            error.AppendLine(exception.Type);
            //error.AppendLine(exception.TargetSite.ToString());
            error.AppendLine(exception.HelpLink);
            error.AppendLine("");

            error.AppendLine("Exception classes:   ");
            error.Append(GetExceptionTypeStack(exception));
            error.AppendLine("");
            error.AppendLine("Exception messages: ");
            error.Append(GetExceptionMessageStack(exception));

            error.AppendLine("");
            error.AppendLine("Stack Traces:");
            error.Append(GetExceptionCallStack(exception));
            error.AppendLine("");
            error.AppendLine("Loaded Modules:");
            Process thisProcess = Process.GetCurrentProcess();
            foreach (ProcessModule module in thisProcess.Modules)
            {
                error.AppendLine(module.FileName + " " + module.FileVersionInfo.FileVersion);
            }

            //for (int i = 0; i < loggers.Count; i++)
            //{
            //    loggers[i].LogError(error.ToString());
            //}
            #endregion
            System.IO.File.WriteAllText(ErrorLogFile, error.ToString(), System.Text.Encoding.Default);

            //TODO: Envío de mail a través de la cola de Mensajería
            //Enviar al servidor el error para guardarlo
            Salud.DataAccess.DataModelTableAdapters.MailingTA mailingta = new Salud.DataAccess.DataModelTableAdapters.MailingTA();
            long mailid = mailingta.Ins(string.Empty, string.Empty,"Excepcion no controlada", error.ToString(), 666, DateTime.Now, null, 13)[0].MailingID;

            return ClientCode;
        }
        public enum ExceptionSources { Server, Client }
        public static void ManageException(Exception ex)
        {
            if (ex is ApplicationException)
                throw new UserNotificableException(ex, ""); //ExceptionManager.ReportException(ex, ExceptionSources.Server));
            else
            {
                string ReferenceNumber = ExceptionManager.ReportException(ex, ExceptionSources.Server);
                throw new TechnicalException(new Exception(ReferenceNumber, ex), ReferenceNumber);
            }
        }

        #region Support
        private static string GetExceptionTypeStack(Exception e)
        {
            if (e.InnerException != null)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(GetExceptionTypeStack(e.InnerException));
                message.AppendLine("   " + e.GetType().ToString());
                return (message.ToString());
            }
            else
            {
                return "   " + e.GetType().ToString();
            }
        }
        private static string GetExceptionTypeStack(ExceptionDetail e)
        {
            if (e.InnerException != null)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(GetExceptionTypeStack(e.InnerException));
                message.AppendLine("   " + e.GetType().ToString());
                return (message.ToString());
            }
            else
            {
                return "   " + e.GetType().ToString();
            }
        }
        private static string GetExceptionMessageStack(Exception e)
        {
            if (e.InnerException != null)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(GetExceptionMessageStack(e.InnerException));
                message.AppendLine("   " + e.Message);
                return (message.ToString());
            }
            else
            {
                return "   " + e.Message;
            }
        }
        private static string GetExceptionMessageStack(ExceptionDetail e)
        {
            if (e.InnerException != null)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(GetExceptionMessageStack(e.InnerException));
                message.AppendLine("   " + e.Message);
                return (message.ToString());
            }
            else
            {
                return "   " + e.Message;
            }
        }
        private static string GetExceptionCallStack(Exception e)
        {
            if (e.InnerException != null)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(GetExceptionCallStack(e.InnerException));
                message.AppendLine("--- Next Call Stack:");
                message.AppendLine(e.StackTrace);
                return (message.ToString());
            }
            else
            {
                return e.StackTrace;
            }
        }
        private static string GetExceptionCallStack(ExceptionDetail e)
        {
            if (e.InnerException != null)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(GetExceptionCallStack(e.InnerException));
                message.AppendLine("--- Next Call Stack:");
                message.AppendLine(e.StackTrace);
                return (message.ToString());
            }
            else
            {
                return e.StackTrace;
            }
        }

        private static string GetExceptionData(Exception e)
        {
            if (e.InnerException != null)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(GetExceptionData(e.InnerException));
                message.AppendLine("--- Next Data:");
                message.AppendLine(e.Data.ToString());
                return (message.ToString());
            }
            else
            {
                return e.Data.ToString();
            }
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return "{" + string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
        }
        #endregion
        #endregion
    }
    public class UserNotificableException : ApplicationException
    {
        public UserNotificableException(Exception ex, string ReferenceNumber)
            : base("Server Exception", ex)
        {
            this._ReferenceNumber = ReferenceNumber;
        }

        private string _ReferenceNumber;

        public string ReferenceNumber
        {
            get { return _ReferenceNumber; }
            set { _ReferenceNumber = value; }
        }
    }
    public class TechnicalException : ApplicationException
    {
        public TechnicalException(Exception ex, string ReferenceNumber)
            : base("Technical Exception", ex)
        {
        }
    }
    public class SessionException : ApplicationException
    {
        public SessionException(Exception ex)
            : base("Session Exception", ex)
        {
        }
    }
}
