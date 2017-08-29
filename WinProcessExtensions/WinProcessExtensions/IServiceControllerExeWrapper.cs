using CSharpFunctionalExtensions3_0;
using System.Collections.Generic;

namespace WinProcessExtensions
{
    public interface IServiceControllerExeWrapper
    {
        Result<ServiceControllerExeWrapper.ExpectedResults> ReInstallService(string serviceName, string serviceAssemblyPath);
    }
}
