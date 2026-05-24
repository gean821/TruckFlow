using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;

namespace TruckFlow.Application
{
    public class CurrentUserService(IHttpContextAccessor http) : ICurrentUserService
    {
        private readonly IHttpContextAccessor _http = http;

        public Guid UserId =>
            Guid.Parse(_http.HttpContext!.User
                .FindFirst("UserId")!.Value);

        public Guid? UserIdOrNull
        {
            get
            {
                var claim = _http.HttpContext?.User?.FindFirst("UserId")?.Value;
                return Guid.TryParse(claim, out var id) ? id : null;
            }
        }

        public Guid? EmpresaId =>
            _http.HttpContext!.User
                .FindFirst("EmpresaId") != null
            ? Guid.Parse(_http.HttpContext!.User
                .FindFirst("EmpresaId")!.Value)
            : null;

        public bool IsAdmin =>
            _http.HttpContext!.User.IsInRole("Admin");

        public bool IsMotorista =>
            _http.HttpContext!.User.IsInRole("Motorista");

        public Guid? MotoristaId =>
          _http.HttpContext!.User.FindFirst("MotoristaId") != null
        ? Guid.Parse(_http.HttpContext!.User.FindFirst("MotoristaId")!.Value)
            : null;

        public string? NomeAutor =>
            _http.HttpContext!.User.FindFirst(ClaimTypes.Name)?
            .Value;
    }
}
