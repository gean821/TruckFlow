using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Exceptions
{
    public class NotFoundException(string message) : HttpResponseException("NOT_FOUND", message)
    {
    }
}
