namespace TruckFlow.Domain.Entities
{
    public class Empresa : EntidadeBase
    {
        public required string RazaoSocial { get; set; }
        public required string NomeFantasia { get; set; }

        public required string Cnpj { get; set; }
        public string? InscricaoEstadual { get; set; }
        public string? InscricaoMunicipal { get; set; }

        public required string Email { get; set; }
        public string? Telefone { get; set; }
        public required string Logradouro { get; set; }
        public required string Numero { get; set; }
        public string? Complemento { get; set; }
        public required string Bairro { get; set; }
        public required string Cidade { get; set; }
        public required string Estado { get; set; }
        public required string Cep { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool Ativa { get; set; } = false;

        public ICollection<UnidadeEntrega> Unidades { get; set; } = [];
        public ICollection<Fornecedor> Fornecedores { get; set; } = [];
        public ICollection<Produto> Produtos { get; set; } = [];
        public ICollection<ItemPlanejamento>? ItensPlanejamento { get; set; }

        public ICollection<Usuario> Usuarios { get; set; } = [];
    }
}