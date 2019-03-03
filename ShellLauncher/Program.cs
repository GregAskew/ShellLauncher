namespace ShellLauncher {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// Launches an alternate shell application.
    /// Users with the alternate shell are logged off when the application is closed.
    /// Builtin Administrator account and Domain Admins are excluded from alternate shell and load Explorer.exe.
    ///
    /// To use, copy ShellLauncher.exe and ShellLauncher.exe.config to %systemroot%, and set the following registry value:
    /// Key: HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon
    /// Value: Shell
    /// Value Type: REG_SZ
    /// Value Data: %systemroot%\ShellLauncher.exe
    /// </summary>
    internal class Program {

        #region Members

        private static IReadOnlyList<string> ExclusionGroups { get; set; }

        private static bool LogGroupMembershipsToEventLog { get; set; }

        private static string ShellApplication { get; set; }

        private static IReadOnlyList<NTAccount> WindowsGroups { get; set; }

        #endregion

        private static void Initialize(string[] args) {

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => {
                e.Cancel = true;
            };

            ExclusionGroups = new List<string>();
            WindowsGroups = new List<NTAccount>();

            var windowsGroups = new List<NTAccount>();
            foreach (var identityReference in WindowsIdentity.GetCurrent().Groups) {
                NTAccount ntAccount = null;
                try {
                    ntAccount = identityReference.Translate(typeof(NTAccount)) as NTAccount;
                    if (ntAccount != null) {
                        windowsGroups.Add(ntAccount);
                    }
                }
                catch { }
            }
            WindowsGroups = windowsGroups;

            if (ConfigurationManager.AppSettings["ExclusionGroups"] != null) {
                var exclusionGroups = ConfigurationManager.AppSettings["ExclusionGroups"].Split(new char[] { '|' });
                ExclusionGroups = exclusionGroups.Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();
            }

            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["LogGroupMembershipsToEventLog"])) {
                LogGroupMembershipsToEventLog = Convert.ToBoolean(ConfigurationManager.AppSettings["LogGroupMembershipsToEventLog"]);
            }

            if (LogGroupMembershipsToEventLog) {
                EventLog.WriteEntry("Application", $"ShellLauncher: Identity: {WindowsIdentity.GetCurrent().Name} Groups: {Environment.NewLine}{string.Join(Environment.NewLine, windowsGroups.Select(x => x.Value))}", EventLogEntryType.Information);
            }

            if (WindowsGroups.Any(x => ExclusionGroups.Any(y => string.Equals(x.Value, y, StringComparison.OrdinalIgnoreCase)))
                || WindowsIdentity.GetCurrent().User.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)
                || WindowsIdentity.GetCurrent().User.IsWellKnown(WellKnownSidType.AccountDomainAdminsSid)) {
                ShellApplication = Path.Combine(Environment.ExpandEnvironmentVariables("%systemroot%"), "explorer.exe");
            }
            else {
                if (ConfigurationManager.AppSettings["ShellApplication"] != null) {
                    ShellApplication = ConfigurationManager.AppSettings["ShellApplication"].Trim();
                }
                var shellApplicationParameter = args
                    .Where(x => x.StartsWith("ShellApplication:"))
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(shellApplicationParameter)) {
                    ShellApplication = shellApplicationParameter.Substring("ShellApplication:".Length).Trim();
                }

                if (string.IsNullOrWhiteSpace(ShellApplication)) {
                    throw new ApplicationException("ShellApplication not specified");
                }
                if (!File.Exists(ShellApplication)) {
                    throw new ApplicationException($"ShellApplication does not exist: {ShellApplication}");
                }
            }
        }

        static void Main(string[] args) {

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Initialize(args);

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.UseShellExecute = false;
            processStartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            processStartInfo.CreateNoWindow = false;
            processStartInfo.FileName = ShellApplication;

            var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();

            if (!WindowsGroups.Any(x => ExclusionGroups.Any(y => string.Equals(x.Value, y, StringComparison.OrdinalIgnoreCase)))
                && !WindowsIdentity.GetCurrent().User.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)
                && !WindowsIdentity.GetCurrent().User.IsWellKnown(WellKnownSidType.AccountDomainAdminsSid)) {
                processStartInfo = new ProcessStartInfo();
                processStartInfo.UseShellExecute = false;
                processStartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                processStartInfo.CreateNoWindow = false;
                var fileName = Path.Combine(Environment.ExpandEnvironmentVariables("%systemroot%"), "system32", "logoff.exe");
                processStartInfo.FileName = fileName;

                process = new Process();
                process.StartInfo = processStartInfo;
                process.Start();
            }
        }

        /// <summary>
        /// Unhandled Exception Handler
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Args</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var exception = e.ExceptionObject as Exception;
            EventLog.WriteEntry("Application", exception.VerboseExceptionString(), EventLogEntryType.Error);
        }
    }
}
