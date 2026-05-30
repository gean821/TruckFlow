using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlowApi.Infra.Email
{
    public sealed class ResendOptions
    {
        public required string ApiKey { get; init; }
        public required string FromEmail { get; init; }
        public required string FromName { get; init; }
    }
}
