using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Net3Migrations.Delegates;

namespace WinProcessExtensions
{
    public class ProcessRunner : IProcessRunner
    {
        //Constants 
        private const string StrWin32ProcessStartup = "Win32_ProcessStartup";

        private const string StrWin32Process = "Win32_Process";
        private const string StrCreate = "Create";
        private const string StrCommandLine = "CommandLine";
        private const string StrProcessStartupInformation = "ProcessStartupInformation";
        private const string StrProcessId = "ProcessId";

        public int RunDetachedProcess(string appname) { return RunDetachedProcess(appname, string.Empty); }

        public int RunDetachedProcess(string appname, string arguments)
        {
            var res = 0;
            using (var managementClass = new ManagementClass(StrWin32Process))
            {
                var processInfo = new ManagementClass(StrWin32ProcessStartup);
                //processInfo.Properties["CreateFlags"].Value = 0x00000008;
                var inParameters = managementClass.GetMethodParameters(StrCreate);
                inParameters[StrCommandLine] = $"\"{appname}\" {arguments}";
                inParameters[StrProcessStartupInformation] = processInfo;
                var result = managementClass.InvokeMethod(StrCreate, inParameters, null);

                try { res = Convert.ToInt32(result.Properties[StrProcessId].Value.ToString()); }
                catch { return res; }
            }
            return res;
        }

        public int RunProcess(string fileName, string arguments)
        {
            var process = new Process { StartInfo = { FileName = fileName } };
            process.StartInfo.Arguments = arguments;
            var started = TryStartProcess(process);
            return started ? process.Id : 0;
        }

        public int LaunchProcessWithoutShellExecute(string fileName, string arguments)
        {
            var process = CreateProcessInfoToRunWithoutShellExecute(fileName, arguments, true, true, logOutput: null, logError: null);
            var started = TryStartProcess(process);
            if (!started) return 0;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return process.Id;
        }

        public int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments, Fiction<string> logOutput, Fiction<string> logError)
        {
            var process = CreateProcessInfoToRunWithoutShellExecute(fileName, arguments, true, true, logOutput: logOutput, logError: logError);
            var started = process.Start();
            if (!started) throw new Exception("Couldn't start process");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            return process.ExitCode;
        }

        public int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments, Fiction<string> logOutput, Fiction<string> logError, int timeoutMilliseconds)
        {
            var process = CreateProcessInfoToRunWithoutShellExecute(fileName, arguments, true, true, logOutput: logOutput, logError: logError);
            var started = process.Start();
            if (!started) throw new Exception("Couldn't start process");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit(timeoutMilliseconds);
            return process.ExitCode;
        }
        //NOTE! This method is very similar to one above
        public int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments, bool getStdOutput, bool getStdError, Fiction<string> logOutput, Fiction<string> logError)
        {
            var process = CreateProcessInfoToRunWithoutShellExecute(fileName, arguments, getStdOutput, getStdError, logOutput: logOutput, logError: logError);
            var started = process.Start();
            if (!started) throw new Exception("Couldn't start process");

            if (getStdOutput)
                process.BeginOutputReadLine();
            if (getStdError)
                process.BeginErrorReadLine();

            process.WaitForExit();
            return process.ExitCode;
        }

        private Process CreateProcessInfoToRunWithoutShellExecute(string fileName, string arguments, bool getStdOutput, bool getStdError, Fiction<string> logOutput, Fiction<string> logError)
        {
            var process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = System.IO.Path.GetDirectoryName(fileName),
                    Arguments = arguments,
                    FileName = fileName,
                    UseShellExecute = false,
                    RedirectStandardOutput = getStdOutput,
                    RedirectStandardError = getStdError,
                    CreateNoWindow = true
                }
            };

            process.ErrorDataReceived += (o, args) =>
            {
                var data = args?.Data;
                if (!string.IsNullOrEmpty(data))
                    logError?.Invoke($"External process error: {data}");
            };
            process.OutputDataReceived += (o, args) =>
            {
                var data = args?.Data;
                if (!string.IsNullOrEmpty(data))
                    logOutput?.Invoke($"External process output: {args?.Data}");
            };
            process.EnableRaisingEvents = true;
            return process;
        }
        public string StartProcessAndWaitForOutput(string processFileName, string processArguments)
        {
            var tool = new Process
            {
                StartInfo =
                {
                    FileName = processFileName,
                    Arguments = processArguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            tool.Start();
            tool.WaitForExit();
            var outputTool = tool.StandardOutput.ReadToEnd();
            tool.Dispose();
            return outputTool;
        }
        public int GetSimilarProcessesCount(string pname = null)
        {
            var currentProcess = Process.GetCurrentProcess();
            var name = pname ?? currentProcess.ProcessName;
            var sessionId = currentProcess.SessionId;
            return Process.GetProcessesByName(name).Count(p => p.SessionId == sessionId);
        }

        private bool TryStartProcess(Process process)
        {
            bool started;
            try { started = process.Start(); }
            catch (Exception ex)
            {
                var fileInfo = "FileName: " + process.StartInfo.FileName + "; Arguments: " +
                               process.StartInfo.Arguments + Environment.NewLine;
                throw new ProcessRunnerException($"Couldn't run {fileInfo} because of exception. See inner exception.", ex);
            }
            return started;
        }
    }
}