using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Exceptions
{
    public class BadRequestException(string message) : HttpResponseException("BAD_REQUEST", message)
    {
    }
}
