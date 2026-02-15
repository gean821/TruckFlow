using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public class UnidadeEntrega : EntidadeBase, IEmpresaScoped
    {
        public required string Nome { get; set; }
        public required string Localizacao { get; set; }

        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? Cep { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public ICollection<Agendamento>? Agendamentos { get; set; } = [];
        public ICollection<Grade>? Grades { get; set; } = [];
        public ICollection<LocalDescarga>? LocaisDeDescarga { get; set; } = [];
        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
    }
}
