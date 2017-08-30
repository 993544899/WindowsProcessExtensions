using System;
using System.Text;
using CSharpFunctionalExtensions3_0;

namespace WinProcessExtensions
{
    //TODO:Documment this class 
    public class ServiceControllerExeWrapper : IServiceControllerExeWrapper
    {
        const string ServiceControllerName = "sc.exe";

        readonly IProcessRunner _processRunner;

        public ServiceControllerExeWrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public Result<ExpectedResults> ReInstallService(string serviceName, string serviceAssemblyPath)
        {
            return UninstallServiceIfExists(serviceName)
                .OnSuccess(uninstallResult => InstallService(serviceName, serviceAssemblyPath))
                .OnSuccess(installResult => StartService(serviceName))
                .OnSuccess(startResult => SetDefaultRecoveryOptions(serviceName));
        }

        public Result<ExpectedResults> SetDefaultRecoveryOptions(string serviceDisplayName)
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

        public Result<ExpectedResults> StartService(string serviceName)
        {
            return ExecuteScAndGetResult($"start \"{serviceName}\"")
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
                        $"Couldn't start service:{serviceName}. Result:{startResult}");
                });
        }

        public Result<ExpectedResults> StopServiceIfExistsAndRunning(string serviceDisplayName)
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

        public Result<ExpectedResults> InstallService(string serviceName, string serviceAssemblyPath)
        {
            return ExecuteScAndGetResult($"create {serviceName} binPath= \"{serviceAssemblyPath}\" start= auto displayName= \"{serviceName}\"")
                .OnSuccess(installResult =>
                {
                    if (installResult != ExpectedResults.Success)
                    {
                        throw new ServiceControllerWrapperException(
                            $"Couldn't create service:{serviceName}. Result:{installResult}");
                    }
                }); ;
        }

        public Result<ExpectedResults> DeleteService(string serviceName)
        {
            var args = $"delete {serviceName}";
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

        public Result<ExpectedResults> CheckServiceExists(string serviceName)
        {
            var args = $"query {serviceName}";
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

        public Result<ExpectedResults> UninstallServiceIfExists(string serviceName)
        {
            var args = $"stop {serviceName}";
            return ExecuteScAndGetResult(args)
                .OnSuccess(result =>
                {
                    switch (result)
                    {
                        case ExpectedResults.Success:
                        case ExpectedResults.NotStarted:
                            return DeleteService(serviceName);
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