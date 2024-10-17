using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Services.Internal
{
    internal class LogErrorService
    {
        public LogErrorService()
        {
        }

        public void LogErrorWriteHost(ILogger logger, Exception ex)
        {
            StringServices strServices = new StringServices();
            logger.LogError($"{strServices.GetErrorPath(ex)} Error Message {ex.Message}");
            logger.LogError($"{strServices.GetErrorPath(ex)} Error Number {ex.HResult}");
            logger.LogError($"{strServices.GetErrorPath(ex)} Error LineNumber {strServices.GetLineNumber(ex)}");
        }
    }
}
