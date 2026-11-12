using Microsoft.Extensions.Logging;

namespace HardwareRegistry.Application.Common
{
    public abstract class ServiceBase
    {
        protected ServiceBase(ILoggerFactory loggerFactory)
            => this.Logger = loggerFactory.CreateLogger(this.GetType());

        protected readonly ILogger Logger;
    }
}