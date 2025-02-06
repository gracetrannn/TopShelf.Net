// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Topshelf.Runtime.Windows
{
    using System;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.ServiceProcess;
    using Logging;
    using HostConfigurators;
    using Microsoft.Win32;

    public class WindowsHostEnvironment :
        HostEnvironment
    {
        readonly LogWriter _log = HostLogger.Get(typeof(WindowsHostEnvironment));
        private HostConfigurator _hostConfigurator;

        public WindowsHostEnvironment(HostConfigurator configurator)
        {
            _hostConfigurator = configurator;
        }

        public bool IsServiceInstalled(string serviceName)
        {
            if (Type.GetType("Mono.Runtime") != null)
            {
                return false;
            }
            
            return IsServiceListed(serviceName);
        }

        public bool IsServiceStopped(string serviceName)
        {
            using (var sc = new ServiceController(serviceName))
            {
                return sc.Status == ServiceControllerStatus.Stopped;
            }
        }

        public void StartService(string serviceName, TimeSpan startTimeOut)
        {
            using (var sc = new ServiceController(serviceName))
            {
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    _log.InfoFormat("The {0} service is already running.", serviceName);
                    return;
                }

                if (sc.Status == ServiceControllerStatus.StartPending)
                {
                    _log.InfoFormat("The {0} service is already starting.", serviceName);
                    return;
                }

                if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.Paused)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, startTimeOut);
                }
                else
                {
                    // Status is StopPending, ContinuePending or PausedPending, print warning
                    _log.WarnFormat("The {0} service can't be started now as it has the status {1}. Try again later...", serviceName, sc.Status.ToString());
                }
            }
        }

        public void StopService(string serviceName, TimeSpan stopTimeOut)
        {
            using (var sc = new ServiceController(serviceName))
            {
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    _log.InfoFormat("The {0} service is not running.", serviceName);
                    return;
                }

                if (sc.Status == ServiceControllerStatus.StopPending)
                {
                    _log.InfoFormat("The {0} service is already stopping.", serviceName);
                    return;
                }

                if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, stopTimeOut);
                }
                else
                {
                    // Status is StartPending, ContinuePending or PausedPending, print warning
                    _log.WarnFormat("The {0} service can't be stopped now as it has the status {1}. Try again later...", serviceName, sc.Status.ToString());
                }
            }
        }

        public string CommandLine
        {
            get { return CommandLineParser.CommandLine.GetUnparsedCommandLine(); }
        }

        public bool IsAdministrator
        {
            get
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                var principal = new WindowsPrincipal(identity);

                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public bool IsRunningAsAService
        {
            get
            {
                try
                {
                    Process process = GetParent(Process.GetCurrentProcess());
                    if (process != null && process.ProcessName == "services")
                    {
                        _log.Debug("Started by the Windows services process");
                        return true;
                    }
                }
                catch (InvalidOperationException)
                {
                    // again, mono seems to fail with this, let's just return false okay?
                }
                return false;
            }
        }

        public bool RunAsAdministrator()
        {
            if (Environment.OSVersion.Version.Major == 6)
            {
                string commandLine = CommandLine.Replace("--sudo", "");

                var startInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location, commandLine)
                    {
                        Verb = "runas",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    };

                try
                {
                    HostLogger.Shutdown();

                    Process process = Process.Start(startInfo);
                    process.WaitForExit();

                    return true;
                }
                catch (Win32Exception ex)
                {
                    _log.Debug("Process Start Exception", ex);
                }
            }

            return false;
        }

        public Host CreateServiceHost(HostSettings settings, ServiceHandle serviceHandle)
        {
            return new WindowsServiceHost(this, settings, serviceHandle, this._hostConfigurator);
        }

        public void SendServiceCommand(string serviceName, int command)
        {
            using (var sc = new ServiceController(serviceName))
            {
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.ExecuteCommand(command);
                }
                else
                {
                    _log.WarnFormat("The {0} service can't be commanded now as it has the status {1}. Try again later...",
                        serviceName, sc.Status.ToString());
                }
            }
        }

        public void InstallService(InstallHostSettings settings, Action<InstallHostSettings> beforeInstall, Action afterInstall, Action beforeRollback, Action afterRollback)
        {
#if true
            using (var installer = new HostServiceInstaller(settings))
            {
                Action<InstallEventArgs> before = x =>
                {
                    if (beforeInstall != null)
                    {
                        beforeInstall(settings);
                        installer.ServiceProcessInstaller.Username = settings.Credentials.Username;
                        installer.ServiceProcessInstaller.Account = settings.Credentials.Account;

                        bool gMSA = false;
                        // Group Managed Service Account (gMSA) workaround per
                        // https://connect.microsoft.com/VisualStudio/feedback/details/795196/service-process-installer-should-support-virtual-service-accounts
                        if (settings.Credentials.Account == ServiceAccount.User &&
                            settings.Credentials.Username != null &&
                            ((gMSA = settings.Credentials.Username.EndsWith("$", StringComparison.InvariantCulture)) ||
                            string.Equals(settings.Credentials.Username, "NT SERVICE\\" + settings.ServiceName, StringComparison.InvariantCulture)))
                        {
                            _log.InfoFormat(gMSA ? "Installing as gMSA {0}." : "Installing as virtual service account", settings.Credentials.Username);
                            installer.ServiceProcessInstaller.Password = null;
                            installer.ServiceProcessInstaller
                                .GetType()
                                .GetField("haveLoginInfo", BindingFlags.Instance | BindingFlags.NonPublic)
                                .SetValue(installer.ServiceProcessInstaller, true);
                        }
                        else
                        {
                            installer.ServiceProcessInstaller.Password = settings.Credentials.Password;
                        }
                    }
                };

                Action<InstallEventArgs> after = x =>
                {
                    if (afterInstall != null)
                        afterInstall();
                };

                Action<InstallEventArgs> before2 = x =>
                {
                    if (beforeRollback != null)
                        beforeRollback();
                };

                Action<InstallEventArgs> after2 = x =>
                {
                    if (afterRollback != null)
                        afterRollback();
                };

                installer.InstallService(before, after, before2, after2);
            }
#else
            try
            {
                var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    throw new InvalidOperationException("Failed to determine the base directory for the executable.");
                }

                // Convert .dll path to .exe
                var executablePath = Path.ChangeExtension(assemblyLocation, ".exe");

                if (!File.Exists(executablePath))
                {
                    throw new FileNotFoundException("Executable not found for service installation.", executablePath);
                }
                string startMode = GetStartMode(settings.StartMode);

                if (_log.IsInfoEnabled)
                    _log.InfoFormat("Installing {0} service", settings.DisplayName);

                beforeInstall?.Invoke(settings);

                // Command to create the service using a service account (if specified)
                string installCommand = $"sc create {settings.ServiceName} binPath= \"{executablePath}\" start= {startMode}";
                //.string installCommand = $"sc create {this.ServiceName} binPath= \"{executablePath}\" DisplayName= \"{this.DisplayName}\" start= {startMode}";
                //.sc config YourServiceName description= "Your Service Description"
                //.sc config YourServiceName start= delayed-auto

                if (!string.IsNullOrWhiteSpace(settings.Credentials?.Username))
                {
                    installCommand += $" obj= \"{settings.Credentials.Username}\" password= \"{settings.Credentials.Password}\"";
                }
                _log.DebugFormat("Installing service with command: {0}", installCommand);
                ExecuteCommand(installCommand);

                using (var servicesKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Services", true))
                using (var serviceKey = servicesKey?.OpenSubKey(settings.ServiceName, true))
                {
                    serviceKey?.SetValue("Description", settings.Description);
                }

                afterInstall?.Invoke();

                _log.InfoFormat("Service {0} installed successfully.", settings.ServiceName);
            }
            catch (Exception ex)
            {
                ;// _logger.LogError("Error installing service {0}: {1}", settings.ServiceName, ex.Message);
                beforeRollback?.Invoke();

                ;// RollbackServiceInstallation(settings);

                afterRollback?.Invoke();
                _log.InfoFormat("Rollback completed for service {0}.", settings.ServiceName);
            }
#endif
        }
        private string GetStartMode(HostStartMode startMode)
        {
            return startMode switch
            {
                HostStartMode.Automatic => "auto",
                HostStartMode.Manual => "demand",
                HostStartMode.Disabled => "disabled",
                _ => throw new ArgumentOutOfRangeException(nameof(startMode), "Invalid start mode specified.")
            };
        }
        private void ExecuteCommand(string command)
        {
            var processStartInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processStartInfo))
            {
                if (process == null) return;

                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(output))
                    _log.InfoFormat(output);

                if (!string.IsNullOrWhiteSpace(error))
                    ;// _logger.LogError(error);
            }
        }
        public void UninstallService(HostSettings settings, Action beforeUninstall, Action afterUninstall)
        {
            using (var installer = new HostServiceInstaller(settings))
            {
                Action<InstallEventArgs> before = x =>
                    {
                        if (beforeUninstall != null)
                            beforeUninstall();
                    };

                Action<InstallEventArgs> after = x =>
                    {
                        if (afterUninstall != null)
                            afterUninstall();
                    };

                installer.UninstallService(before, after);
            }
        }


        Process GetParent(Process child)
        {
            if (child == null)
                throw new ArgumentNullException("child");

            try
            {
                int parentPid = 0;

                IntPtr hnd = Kernel32.CreateToolhelp32Snapshot(Kernel32.TH32CS_SNAPPROCESS, 0);

                if (hnd == IntPtr.Zero)
                    return null;

                var processInfo = new Kernel32.PROCESSENTRY32
                    {
                        dwSize = (uint)Marshal.SizeOf(typeof(Kernel32.PROCESSENTRY32))
                    };

                if (Kernel32.Process32First(hnd, ref processInfo) == false)
                    return null;

                do
                {
                    if (child.Id == processInfo.th32ProcessID)
                        parentPid = (int)processInfo.th32ParentProcessID;
                }
                while (parentPid == 0 && Kernel32.Process32Next(hnd, ref processInfo));

                if (parentPid > 0)
                    return Process.GetProcessById(parentPid);
            }
            catch (Exception ex)
            {
                _log.Error("Unable to get parent process (ignored)", ex);
            }
            return null;
        }

        bool IsServiceListed(string serviceName)
        {
            bool result = false;

            try
            {
                result = ServiceController.GetServices()
                    .Any(service => string.Equals(service.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
            }
            catch (InvalidOperationException)
            {
                _log.Debug("Cannot access Service List due to permissions. Assuming the service is not installed.");
            }

            return result;
        }
    }
}
