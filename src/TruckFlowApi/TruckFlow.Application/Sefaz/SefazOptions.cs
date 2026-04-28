namespace TruckFlow.Application.Sefaz
{
    public class SefazOptions
    {
        public const string SectionName = "Sefaz";

        public int Ambiente { get; set; } = 2;          // 1 = Produção, 2 = Homologação
        public string? UfEmitenteFallback { get; set; } // ex: "SP" — usado quando a Empresa não fornece UF
        public bool UseFake { get; set; } = true;        // true = não bate na SEFAZ, retorna mock canônico

        public SefazCertificadoOptions Certificado { get; set; } = new();
    }

    public class SefazCertificadoOptions
    {
        public string? Caminho { get; set; }     // .pfx (A1)
        public string? Senha { get; set; }
        public string? Thumbprint { get; set; }  // alternativa: cert instalado na store da máquina
    }
}
