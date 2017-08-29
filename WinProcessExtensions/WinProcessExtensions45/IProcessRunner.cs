using System;

namespace WinProcessExtensions
{
    public interface IProcessRunner
    {
        int GetSimilarProcessesCount(string pname = null);
        int LaunchProcessWithoutShellExecute(string fileName, string arguments);
        int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments, Action<string> logOutput, Action<string> logError);

        int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments,
            Action<string> logOutput, Action<string> logError, int timeoutMilliseconds);
        int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments, bool getStdOutput, bool getStdError, Action<string> logOutput, Action<string> logError);

        int RunDetachedProcess(string appname);
        int RunDetachedProcess(string appname, string arguments);
        int RunProcess(string fileName, string arguments);
        string StartProcessAndWaitForOutput(string processFileName, string processArguments);
    }
}