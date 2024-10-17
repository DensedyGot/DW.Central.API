using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Services.Internal
{
    internal class StringServices
    {
        internal string GetLineNumber(Exception ex)
        {
            string stackTrace = ex.StackTrace ?? "";
            string[] stackTraceArray = stackTrace.Split(" ");
            string lineNumber = stackTraceArray[stackTraceArray.Length - 1];
            return lineNumber;
        }
        internal string GetErrorPath(Exception ex)
        {
            string stackTrace = ex.StackTrace ?? "";
            string[] stackTraceArray = stackTrace.Split(" at ");
            stackTrace = stackTraceArray[stackTraceArray.Length - 1];
            stackTraceArray = stackTrace.Split("(");
            stackTrace = stackTraceArray[0];
            return stackTrace;
        }
        internal string GetOriginalMethodName([CallerMemberName] string memberName = "")
        {
            return memberName;
        }
    }
}
