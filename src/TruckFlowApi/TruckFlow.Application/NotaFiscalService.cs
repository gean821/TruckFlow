using DFe.Utils;
using FluentValidation;
using FluentValidation.Results;
using NFe.Classes.Informacoes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Enums;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.NotaFiscal;
using TruckFlow.Domain.Dto.NotaFiscal;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class NotaFiscalService : INotaFiscalService
    {
        private readonly INotaFiscalRepositorio _repo;
        private readonly IFornecedorRepositorio _fornecedorRepo;
        private readonly IValidator<NotaFiscalParsedDto> _parsedValidator;
        private readonly IValidator<NotaFiscalItemDto> _itemValidator;

        public NotaFiscalService(
            INotaFiscalRepositorio repo,
            IFornecedorRepositorio fornecedorRepo,
            IValidator<NotaFiscalParsedDto> parsedValidator,
            IValidator<NotaFiscalItemDto> itemValidator
            )
        {
            _repo = repo;
            _fornecedorRepo = fornecedorRepo;
            _parsedValidator = parsedValidator;
            _itemValidator = itemValidator;
        }

        public async Task<NotaFiscalParsedDto> ParseXmlAsync(
            Stream xmlStream,
            CancellationToken token
            )
        {
            using var sr = new StreamReader(xmlStream);
            var xml = await sr.ReadToEndAsync(token);
            
            // aqui nós convertemos o XML em um objeto NFe para manipular ele!
            var nfe = FuncoesXml.XmlStringParaClasse<NFe.Classes.NFe>(xml);
            var infNFe = nfe.infNFe;

            var notaFiscalDto = new NotaFiscalParsedDto
            {
                ChaveAcesso = infNFe.Id?.Replace("NFe", "") ?? string.Empty,
                Numero = infNFe.ide.nNF,
                TipoCarga = TipoCarga.Indefinido,
                Serie = infNFe.ide.serie.ToString(),
                DataEmissao = infNFe.ide.dhEmi.DateTime,
                EmitenteNome = infNFe.emit.xNome ?? string.Empty,
                EmitenteCnpj = infNFe.emit.CNPJ ?? string.Empty,
                DestinatarioNome = infNFe.dest?.xNome ?? string.Empty,
                DestinatarioCpfCnpj = infNFe.dest?.CNPJ ?? infNFe.dest?.CPF ?? string.Empty,
                Fornecedor = infNFe.emit.xNome ?? string.Empty,
                ValorTotal = infNFe.total.ICMSTot.vNF,
                PesoBruto = infNFe.transp?.vol?.FirstOrDefault()?.pesoB,
                VolumeQuantidade = (int?)infNFe.transp?.vol?.FirstOrDefault()?.qVol,
                PlacaVeiculo = infNFe.transp?.veicTransp?.placa ?? string.Empty,
                Itens = infNFe.det.Select(d => new NotaFiscalItemDto
                {
                    Codigo = d.prod.cProd ?? string.Empty,
                    Descricao = d.prod.xProd ?? string.Empty,
                    Quantidade = d.prod.qCom,
                    Unidade = d.prod.uCom ?? string.Empty,
                    ValorUnitario = d.prod.vUnCom,
                    ValorTotal = d.prod.vProd
                }).ToList(),
                ValidationWarnings = new List<string>()
            };

            return notaFiscalDto;
        }

        public async Task<NotaFiscalParsedDto> SaveParsedNotaAsync(
            NotaFiscalParsedDto dto,
            Guid uploadedByUserId,
            CancellationToken token
            )
        {
            ValidationResult validationResult = await _parsedValidator.ValidateAsync(dto, token);
    
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            foreach(var item in dto.Itens)
            {
                var itemResult = await _itemValidator.ValidateAsync(item, token);

                if (!itemResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }
            }
 
            var fornecedor = await _fornecedorRepo.GetByNome(dto.Fornecedor,token);

            var nota = new NotaFiscal
            {
                ChaveAcesso = dto.ChaveAcesso,
                Numero = dto.Numero,
                Serie = dto.Serie,
                DataEmissao = dto.DataEmissao,
                EmitenteNome = dto.EmitenteNome,
                EmitenteCnpj = dto.EmitenteCnpj,
                DestinatarioNome = dto.DestinatarioNome,
                DestinatarioCpfCnpj = dto.DestinatarioCpfCnpj,
                Fornecedor = fornecedor,
                ValorTotal = dto.ValorTotal,
                PesoBruto = dto.PesoBruto,
                VolumeQuantidade = dto.VolumeQuantidade,
                PlacaVeiculo = dto.PlacaVeiculo,
                FornecedorId = fornecedor.Id, 
                TipoCarga = dto.TipoCarga 
            };

            var notaSalva = await _repo.SaveParsedNotaAsync(nota,token);
            await _repo.SaveChangesAsync(token);
            
            return new NotaFiscalParsedDto
            {
                ChaveAcesso = notaSalva.ChaveAcesso,
                Numero = notaSalva.Numero,
                Serie = notaSalva.Serie,
                DataEmissao = notaSalva.DataEmissao,
                EmitenteNome = notaSalva.EmitenteNome,
                EmitenteCnpj = notaSalva.EmitenteCnpj,
                DestinatarioNome = notaSalva.DestinatarioNome,
                DestinatarioCpfCnpj = notaSalva.DestinatarioCpfCnpj,
                Fornecedor = notaSalva.Fornecedor?.Nome ?? string.Empty,
                ValorTotal = notaSalva.ValorTotal,
                PesoBruto = notaSalva.PesoBruto,
                VolumeQuantidade = notaSalva.VolumeQuantidade,
                PlacaVeiculo = notaSalva.PlacaVeiculo!,
                TipoCarga = notaSalva.TipoCarga,
                Itens = notaSalva.Itens.Select(x=> new NotaFiscalItemDto
                {
                    Codigo = x.Codigo,
                    Descricao = x.Descricao,
                    Quantidade = x.Quantidade,
                    Unidade = x.Unidade,
                    ValorUnitario = x.ValorUnitario,
                    ValorTotal = x.ValorTotal
                }).ToList() ?? new List<NotaFiscalItemDto>()
            };
        }
        public async Task<NotaFiscalParsedDto?> ObterPorChaveAsync
            (
                string chaveAcesso,
                CancellationToken token
            )
        {
            var nota = await _repo.ObterPorChaveAsync(chaveAcesso, token);
            
            if (nota == null)
            {
                return null;
            }

            return new NotaFiscalParsedDto
            {
                ChaveAcesso = nota.ChaveAcesso,
                Numero =  nota.Numero,
                Serie = nota.Serie,
                DataEmissao = nota.DataEmissao,
                EmitenteNome = nota.EmitenteNome,
                EmitenteCnpj = nota.EmitenteCnpj,
                DestinatarioNome = nota.DestinatarioNome,
                DestinatarioCpfCnpj = nota.DestinatarioCpfCnpj,
                Fornecedor = nota.Fornecedor?.Nome ?? string.Empty,
                ValorTotal = nota.ValorTotal,
                PesoBruto = nota.PesoBruto,
                VolumeQuantidade = nota.VolumeQuantidade,
                PlacaVeiculo = nota.PlacaVeiculo!,
                TipoCarga = nota.TipoCarga,
                Itens = nota.Itens?.Select(i => new NotaFiscalItemDto
                {
                    Codigo = i.Codigo,
                    Descricao = i.Descricao,
                    Quantidade = i.Quantidade,
                    Unidade = i.Unidade,
                    ValorUnitario = i.ValorUnitario,
                    ValorTotal = i.ValorTotal
                }).ToList() ?? new List<NotaFiscalItemDto>(),
                ValidationWarnings = new List<string>()
            };
        }
    }
}
    
  
