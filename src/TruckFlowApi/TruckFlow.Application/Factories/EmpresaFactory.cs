using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Empresa;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Factories
{
    public static class EmpresaFactory
    {
        public static Empresa Create(EmpresaCreateDto dto)
        {
            return new Empresa
            {
                Id = Guid.NewGuid(),
                RazaoSocial = dto.RazaoSocial.Trim(),
                NomeFantasia = dto.NomeFantasia.Trim(),
                Cnpj = NormalizeCnpj(dto.Cnpj),
                Email = dto.Email.Trim(),
                Telefone = dto.Telefone.Trim(),
                Cep = dto.Cep,
                Logradouro = dto.Logradouro,
                Numero = dto.Numero,
                Complemento = dto.Complemento,
                Bairro = dto.Bairro,
                Cidade = dto.Cidade,
                Estado = dto.Estado,
                Ativa = true,
                CreatedAt = DateTime.UtcNow
            };
        }
        private static string NormalizeCnpj(string cnpj)
            => new([.. cnpj.Where(char.IsDigit)]);
    }

}
