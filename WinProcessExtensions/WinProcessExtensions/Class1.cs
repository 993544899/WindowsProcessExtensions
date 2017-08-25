using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace WinProcessExtensions
{
    public interface IServiceControllerExeWrapper
    {
        Result<ServiceControllerExeWrapper.ExpectedResults> ReInstallService(string serviceDisplayName, string serviceAssemblyPath);
    }
    public interface IProcessRunner
    {
        int GetSimilarProcessesCount(string pname = null);
        int LaunchProcessWithoutShellExecute(string fileName, string arguments);
        int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments, Fiction<string> logOutput, Fiction<string> logError);

        int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments,
            Fiction<string> logOutput, Fiction<string> logError, int timeoutMilliseconds);
        int LaunchProcessWithoutShellExecuteAndWaitExitCode(string fileName, string arguments, bool getStdOutput, bool getStdError, Fiction<string> logOutput, Fiction<string> logError);

        int RunDetachedProcess(string appname);
        int RunDetachedProcess(string appname, string arguments);
        int RunProcess(string fileName, string arguments);
        string StartProcessAndWaitForOutput(string processFileName, string processArguments);
    }

    public class ServiceControllerExeWrapper : IServiceControllerExeWrapper
    {
        //TODO:Some methods can be made public 

        const string ServiceControllerName = "sc.exe";

        readonly IProcessRunner _processRunner;

        public ServiceControllerExeWrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public Result<ExpectedResults> ReInstallService(string serviceDisplayName, string serviceAssemblyPath)
        {
            return UninstallServiceIfExists(serviceDisplayName)
                .OnSuccess(uninstallResult => InstallService(serviceDisplayName, serviceAssemblyPath))
                .OnSuccess(installResult => StartService(serviceDisplayName))
                .OnSuccess(startResult => SetDefaultRecoveryOptions(serviceDisplayName));
        }

        Result<ExpectedResults> SetDefaultRecoveryOptions(string serviceDisplayName)
        {
            //TODO:Add capability to customize recovery options
            return ExecuteScAndGetResult($"failure {serviceDisplayName} reset=86400 actions= restart/60000/restart/60000//")
                .OnSuccess(result =>
                {
                    if (result != ExpectedResults.Success)
                        throw new ServiceControllerWrapperException(
                            $"Couldn't set recovery options to service:{serviceDisplayName}. Result:{result}");
                });
        }

        Result<ExpectedResults> StartService(string serviceDisplayName)
        {
            return ExecuteScAndGetResult($"start \"{serviceDisplayName}\"")
                .OnSuccess(startResult =>
                {
                    switch (startResult)
                    {
                        case ExpectedResults.Success:
                        case ExpectedResults.AlreadyRunning:
                            return Result.Ok(startResult);
                        case ExpectedResults.SideBySideConfigurationIncorrect:
                            return Result.Fail<ExpectedResults>(startResult.ToString());
                    }
                    throw new ServiceControllerWrapperException(
                        $"Couldn't start service:{serviceDisplayName}. Result:{startResult}");
                });
        }

        Result<ExpectedResults> StopServiceIfExistsAndRunning(string serviceDisplayName)
        {
            var args = $"stop {serviceDisplayName}";
            return ExecuteScAndGetResult(args)
                .OnSuccess(result =>
                {
                    switch (result)
                    {
                        case ExpectedResults.Success:
                        case ExpectedResults.NotStarted:
                        case ExpectedResults.ServiceDoesNotExist:
                            return Result.Ok(result);
                        default:
                            throw new ServiceControllerWrapperException(
                                $"Sc exe returned unexpected result:{result} on command: {args}");
                    }
                });
        }

        Result<ExpectedResults> InstallService(string seviceDisplayName, string serviceAssemblyPath)
        {
            return ExecuteScAndGetResult($"create {seviceDisplayName} binPath= \"{serviceAssemblyPath}\" start= auto displayName= \"{seviceDisplayName}\"")
                .OnSuccess(installResult =>
                {
                    if (installResult != ExpectedResults.Success)
                    {
                        throw new ServiceControllerWrapperException(
                            $"Couldn't create service:{seviceDisplayName}. Result:{installResult}");
                    }
                }); ;
        }

        Result<ExpectedResults> DeleteService(string seviceDisplayName)
        {
            var args = $"delete {seviceDisplayName}";
            return ExecuteScAndGetResult(args)
                .OnSuccess(result =>
                {
                    switch (result)
                    {
                        case ExpectedResults.Success:
                        case ExpectedResults.ServiceDoesNotExist:
                            return Result.Ok(result);
                        default:
                            throw new ServiceControllerWrapperException(
                                $"Sc exe returned unexpected result:{result} on command: {args}");
                    }
                });
        }

        Result<ExpectedResults> UninstallServiceIfExists(string seviceDisplayName)
        {
            var args = $"stop {seviceDisplayName}";
            return ExecuteScAndGetResult(args)
                .OnSuccess(result =>
                {
                    switch (result)
                    {
                        case ExpectedResults.Success:
                        case ExpectedResults.NotStarted:
                            return DeleteService(seviceDisplayName);
                        case ExpectedResults.ServiceDoesNotExist:
                            return Result.Ok(result);
                        default:
                            throw new ServiceControllerWrapperException(
                                $"Sc exe returned unexpected result:{result} on command: {args}");
                    }
                });
        }

        Result<ExpectedResults> ExecuteScAndGetResult(string args)
        {
            var scExe = ServiceControllerName;
            var sb = new StringBuilder();
            var proceesExitCode = _processRunner.LaunchProcessWithoutShellExecuteAndWaitExitCode(scExe, args, s => sb.AppendLine(scExe + " Output:" + s), s => sb.AppendLine(scExe + " Error:" + s));
            if (!Enum.IsDefined(typeof(ExpectedResults), proceesExitCode))
                throw new ServiceControllerWrapperException($"Command {scExe} returned unexpected exit code:{proceesExitCode}. Process output:{sb}");
            var knownExitCode = (ExpectedResults)proceesExitCode;
            return Result.Ok(knownExitCode);
        }
        public enum ExpectedResults
        {
            Success = 0,
            AlreadyRunning = 1056,
            NotStarted = 1062,
            ServiceMarkedForDeletion = 1072,
            ServiceDoesNotExist = 1060,
            SideBySideConfigurationIncorrect = 14001
        }
    }
}
