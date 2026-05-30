using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Contracts
{
    public interface IEmailService
    {
        Task SendAsync(string para, string assunto, string corpoHtml, CancellationToken token = default);
    }
}
