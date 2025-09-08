1- Este é um modelamento base e vão ter alterações com o avançar do projeto, em razão de melhorias e ajustes. 
2- Foi criado com base na linguagem C# para ser usado no framework .NET
3- Junto deste arquivo se acompanha o diagrama feito por meio do BR modelo para a modelagem visual do banco.


Entidades:

public class Administrador {
	public Guid Id;
	public string Nome,
	public string Email,
	public Roles funcoes;
}

public class Motorista {
	public Guid Id;
	string nome,
	string telefone,
	public ICollection<Agendamento>? Agendamentos;
	public ICollection<Veiculo>? veiculos; 
}

(MOTORISTA E VEICULO 1:N) pois um motorista pode ter um ou vários veiculos.

public class Veiculo {
	public Guid Id;
	public string Nome;
	public string placa;
	public enum tipoVeiculo;
	public Guid MotoristaId;
	public Motorista Motorista;
}


public class Carga  {
	public Guid Id;
	public Guid AgendamentoId;
	public enum TipoCarga,
}

public class NotaFiscal {
	public Guid Id;
	public string Numero;
	
	public Fornecedor Fornecedor;
		
	public TipoCarga TipoCarga;
}


public class Agendamento {
	public Guid Id;
	
	public Guid FornecedorId;
	public Fornecedor Fornecedor;
	
	public Guid MotoristaId,
	public Motorista Motorista;
	
	public Veiculo Veiculo;
	public Guid VeiculoId;
	
	public DateTime Horario;
	
	public TipoCarga TipoCarga;

	public string VolumeCarga;
	
	public enum StatusAgendamento --enum de status.
	
	Public Guid UnidadeEntregaId;
	public UnidadeEntrega unidadeEntrega;
	
	public Guid NotaFiscalId;
	public NotaFiscal NotaFiscal;
	
	public ICollection<Notificacao> Notificacoes;
}

public class UnidadeDeEntrega {
	public Guid Id;
	public string Nome;
	public string Localizacao;
}

public class Fornecedor {
	public Guid Id;
	public string nome;
}

public class Notificacao {
	public Guid Id;
	public string nome;
	public string Descricao;
	public Guid AgendamentoId;
	public DateTime DataEnvio;
}

public Enum Roles {
	1- Funcionario,
	2- Supervisor,
	3- Gerente
}

public Enum StatusAgendamento {
	Confirmado = 1,
	Cancelado = 2,
	Pendente = 3,
}

public class Produto {
	public Guid Id;
	public string Nome;
	public LocalDescarga LocalDescarga;
}

public enum TipoCarga {
	Milho = 1,
	Farelo = 2,
	Soja = 3,
	etc = 4,
}

public enum LocalDescarga {
	...
}