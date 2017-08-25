using CSharpFunctionalExtensions;
using System.Collections.Generic;

namespace WinProcessExtensions
{
    public interface IServiceControllerExeWrapper
    {
        Result<ServiceControllerExeWrapper.ExpectedResults> ReInstallService(string serviceDisplayName, string serviceAssemblyPath);
    }
}
