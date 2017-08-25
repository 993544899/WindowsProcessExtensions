using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace WinProcessExtensions
{
    public interface IServiceControllerExeWrapper
    {
        Result<ServiceControllerExeWrapper.ExpectedResults> ReInstallService(string serviceDisplayName, string serviceAssemblyPath);
    }
}
