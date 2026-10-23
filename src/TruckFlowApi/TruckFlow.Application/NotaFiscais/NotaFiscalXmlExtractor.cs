using System;
using System.Linq;
using DFe.Utils;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.NotaFiscal;

namespace TruckFlow.Application.NotaFiscais
{
    /// <summary>
    /// Extração pura (sem I/O, sem banco) dos dados de uma NF-e a partir do XML.
    /// Único ponto de leitura de XML da NF-e no sistema — usado tanto pelo upload
    /// manual quanto pela busca direta na SEFAZ (NFeDistribuicaoDFe), garantindo que
    /// os dois caminhos extraiam os campos exatamente da mesma forma.
    /// </summary>
    public static class NotaFiscalXmlExtractor
    {
        public static NFe.Classes.NFe ParseXml(string xml)
        {
            xml = (xml ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(xml))
            {
                throw new BusinessException("XML da nota fiscal está vazio.");
            }

            try
            {
                if (xml.Contains("<nfeProc"))
                {
                    var proc = FuncoesXml.XmlStringParaClasse<NFe.Classes.nfeProc>(xml, true);

                    if (proc?.NFe?.infNFe == null)
                        throw new BusinessException("XML nfeProc inválido.");

                    return proc.NFe;
                }

                if (xml.Contains("<NFe"))
                {
                    var nfe = FuncoesXml.XmlStringParaClasse<NFe.Classes.NFe>(xml, true);

                    if (nfe?.infNFe == null)
                        throw new BusinessException("XML NFe inválido.");

                    return nfe;
                }

                throw new BusinessException("XML não é uma NF-e válida.");
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BusinessException($"Falha ao processar XML da Nota Fiscal: {ex.Message}");
            }
        }

        public static NotaFiscalXmlExtraidaDto Extrair(NFe.Classes.NFe nfe)
        {
            var infNFe = nfe.infNFe;

            DateTime dataEmissao;
            if (infNFe.ide.dhEmi != DateTimeOffset.MinValue)
            {
                dataEmissao = infNFe.ide.dhEmi.UtcDateTime;
            }
            else if (infNFe.ide.dEmi != DateTime.MinValue)
            {
                dataEmissao = DateTime.SpecifyKind(infNFe.ide.dEmi, DateTimeKind.Utc);
            }
            else
            {
                throw new BusinessException("Data de emissão não encontrada na NF-e.");
            }

            var itens = (infNFe.det ?? new System.Collections.Generic.List<NFe.Classes.Informacoes.Detalhe.det>())
                .Where(det => det.prod != null)
                .Select(det => new NotaFiscalXmlItemExtraidoDto
                {
                    Codigo = det.prod.cProd ?? string.Empty,
                    Ean = det.prod.cEAN,
                    Descricao = det.prod.xProd ?? string.Empty,
                    Quantidade = det.prod.qCom,
                    Unidade = det.prod.uCom ?? string.Empty,
                    ValorUnitario = det.prod.vUnCom,
                    ValorTotal = det.prod.vProd
                })
                .ToList();

            return new NotaFiscalXmlExtraidaDto
            {
                ChaveAcesso = infNFe.Id?.Replace("NFe", "") ?? string.Empty,
                Numero = infNFe.ide.nNF,
                Serie = infNFe.ide.serie.ToString(),
                DataEmissao = dataEmissao,
                EmitenteNome = infNFe.emit?.xNome ?? string.Empty,
                EmitenteCnpj = infNFe.emit?.CNPJ ?? string.Empty,
                DestinatarioNome = infNFe.dest?.xNome ?? string.Empty,
                DestinatarioCpfCnpj = infNFe.dest?.CNPJ ?? infNFe.dest?.CPF ?? string.Empty,
                ValorTotal = infNFe.total?.ICMSTot?.vNF ?? 0,
                PesoBruto = infNFe.transp?.vol?.FirstOrDefault()?.pesoB,
                VolumeQuantidade = (int?)infNFe.transp?.vol?.FirstOrDefault()?.qVol,
                PlacaVeiculo = infNFe.transp?.veicTransp?.placa ?? string.Empty,
                Itens = itens
            };
        }
    }
}
