using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.NotaFiscal;

namespace TruckFlow.Application.Validators.NotaFiscal
{
    public class NotaFiscalParsedDtoValidator : AbstractValidator<NotaFiscalParsedDto>
    {
        public NotaFiscalParsedDtoValidator()
        {
            RuleFor(x => x.ChaveAcesso)
                 .NotEmpty().WithMessage("A chave de acesso é obrigatória.")
                 .Length(44).WithMessage("A chave de acesso deve conter 44 dígitos numéricos.")
                 .Matches(@"^\d{44}$").WithMessage("A chave de acesso deve conter apenas números.");

            RuleFor(x => x.Numero)
                .GreaterThan(0).WithMessage("O número da nota fiscal deve ser maior que zero.");

            RuleFor(x => x.Fornecedor)
                .NotEmpty().WithMessage("O nome do fornecedor é obrigatório.")
                .MinimumLength(3).WithMessage("O nome do fornecedor deve ter pelo menos 3 caracteres.");

            RuleFor(x => x.Serie)
                .NotEmpty().WithMessage("A série da nota é obrigatória.")
                .MaximumLength(3).WithMessage("A série deve ter no máximo 3 dígitos.");

            RuleFor(x => x.DataEmissao)
                .NotEmpty().WithMessage("A data de emissão é obrigatória.")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("A data de emissão não pode ser futura.");

            RuleFor(x => x.EmitenteNome)
                .NotEmpty().WithMessage("O nome do emitente é obrigatório.")
                .MinimumLength(3).WithMessage("O nome do emitente deve ter pelo menos 3 caracteres.");

            RuleFor(x => x.EmitenteCnpj)
                .NotEmpty().WithMessage("O CNPJ do emitente é obrigatório.")
                .Matches(@"^\d{14}$").WithMessage("O CNPJ do emitente deve conter 14 dígitos numéricos.");

            RuleFor(x => x.DestinatarioNome)
                .NotEmpty().WithMessage("O nome do destinatário é obrigatório.")
                .MinimumLength(3).WithMessage("O nome do destinatário deve ter pelo menos 3 caracteres.");

            RuleFor(x => x.DestinatarioCpfCnpj)
                .NotEmpty().WithMessage("O CPF ou CNPJ do destinatário é obrigatório.")
                .Must(cpfCnpj =>
                {
                    cpfCnpj = cpfCnpj.Replace(".", "").Replace("-", "").Replace("/", "");
                    return Regex.IsMatch(cpfCnpj, @"^\d{11}$") || Regex.IsMatch(cpfCnpj, @"^\d{14}$");
                }).WithMessage("O CPF/CNPJ do destinatário deve conter 11 ou 14 dígitos numéricos.");

            RuleFor(x => x.ValorTotal)
                .GreaterThan(0).WithMessage("O valor total da nota fiscal deve ser maior que zero.");

            RuleFor(x => x.PesoBruto)
                .NotNull().WithMessage("O peso bruto deve ser informado.")
                .GreaterThan(0).WithMessage("O peso bruto deve ser maior que zero.");

            RuleFor(x => x.VolumeQuantidade)
                .GreaterThanOrEqualTo(0).When(x => x.VolumeQuantidade.HasValue)
                .WithMessage("A quantidade de volumes deve ser positiva.");

            RuleFor(x => x.PlacaVeiculo)
                .NotEmpty().WithMessage("A placa do veículo é obrigatória.")
                .Matches(@"^[A-Z]{3}\d{1}[A-Z0-9]{1}\d{2}$")
                .WithMessage("A placa do veículo deve estar no formato válido (ex: ABC1D23).");

            RuleFor(x => x.Itens)
                .NotEmpty().WithMessage("A nota fiscal deve conter ao menos um item.")
                .Must(itens => itens.All(i => i.Quantidade > 0 && i.ValorUnitario > 0))
                .WithMessage("Cada item deve ter quantidade e valor unitário maiores que zero.");
        }
    }
}

