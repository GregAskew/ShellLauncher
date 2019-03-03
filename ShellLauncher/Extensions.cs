using System;
using System.Collections.Generic;
using System.Data.SqlClient;
namespace ShellLauncher {

    #region Usings
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks; 
    #endregion

    internal static class Extensions {
        /// <summary>
        /// Gets the friendly formatted text value of the SqlException
        /// </summary>
        /// <param name="e">The Exception</param>
        /// <returns>The exception details</returns>
        [DebuggerStepThroughAttribute]
        private static string LogSqlErrors(SqlException e) {
            var error = new StringBuilder();
            for (int index = 0; index < e.Errors.Count; index++) {
                error.AppendLine();
                error.AppendLine("SqlException:");
                error.AppendLine($"Line Number: {e.Errors[index].LineNumber}");
                error.AppendLine($"Message: {e.Errors[index].Message}");
                error.AppendLine($"Error Number: {e.Errors[index].Number}");
                error.AppendLine($"Procedure: {e.Errors[index].Procedure}");
                error.AppendLine($"Server: {e.Errors[index].Server}");
                error.AppendLine($"Source: {e.Errors[index].Source}");
            }

            return error.ToString();
        }

        /// <summary>
        /// Stack trace, target site, and error message of outer and inner exception, formatted with newlines
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exception"></param>
        /// <returns></returns>
        [DebuggerStepThroughAttribute]
        internal static string VerboseExceptionString<T>(this T exception) where T : Exception {
            var exceptionString = new StringBuilder();

            exceptionString.AppendLine($" Exception: {exception.GetType().Name} Message: {exception.Message ?? "NULL"}");
            exceptionString.AppendLine($" StackTrace: {exception.StackTrace ?? "NULL"}");
            exceptionString.AppendLine($" TargetSite: {(exception.TargetSite != null ? exception.TargetSite.ToString() : "NULL")}");

            if (exception as SqlException != null) {
                exceptionString.AppendLine(LogSqlErrors(exception as SqlException));
            }

            if (exception.InnerException != null) {
                exceptionString.AppendLine();
                exceptionString.AppendLine("Inner Exception:");
                exceptionString.AppendLine(exception.InnerException.VerboseExceptionString());
            }

            return exceptionString.ToString();
        }
    }
}
