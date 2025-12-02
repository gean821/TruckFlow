using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Exceptions
{
    public abstract class HttpResponseException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}
