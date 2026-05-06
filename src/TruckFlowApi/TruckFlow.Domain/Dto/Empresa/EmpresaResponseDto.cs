using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Empresa
{
    public sealed class EmpresaResponseDto
    {
        public Guid Id { get; set; }
        public string RazaoSocial { get; set; }
        public string NomeFantasia { get; set; }
        public string Cnpj { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }      
        public string Cep { get; set; }           
        public string Logradouro { get; set; }    
        public string Numero { get; set; }        
        public string? Complemento { get; set; }  
        public string Bairro { get; set; }        
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public bool Ativa { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
