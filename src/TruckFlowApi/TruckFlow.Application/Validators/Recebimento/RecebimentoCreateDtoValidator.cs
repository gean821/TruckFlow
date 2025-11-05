using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Validators.ItemPlanejamento;
using TruckFlow.Domain.Dto.Recebimento;

namespace TruckFlow.Application.Validators.Recebimento
{
    public class RecebimentoCreateDtoValidator : AbstractValidator<RecebimentoCreateDto>
    {
        public RecebimentoCreateDtoValidator()
        {
            RuleFor(x => x.FornecedorId)
              .NotEmpty().WithMessage("O campo FornecedorId é obrigatório.");

            RuleFor(x => x.DataInicio)
                .NotEmpty().WithMessage("A data de início é obrigatória.")
                .Must(date => date.Date >= DateTime.UtcNow.Date)
                .WithMessage("A data de início não pode ser anterior à data atual.");

            RuleFor(x => x.ItensPlanejamento)
                .NotNull().WithMessage("É necessário informar ao menos um item de planejamento.")
                .Must(x => x?.Count < 1).WithMessage("A lista de itens de planejamento não pode estar vazia.");

            // Validação individual dos itens
            RuleForEach(x => x.ItensPlanejamento)
                .SetValidator(new ItemPlanejamentoCreateDtoValidator());
        }
    }
 }

