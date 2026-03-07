using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Contracts
{
    public interface IEmpresaScoped
    {
        Guid EmpresaId { get; set; }
    }
}
