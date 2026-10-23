```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD Ryzen 5 5500 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 8.0.19 (8.0.19, 8.0.1925.36514), X64 RyuJIT x86-64-v3
  Job-CNUJVU : .NET 8.0.19 (8.0.19, 8.0.1925.36514), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                                   | Mean       | Error     | StdDev      | Median     | Allocated |
|----------------------------------------- |-----------:|----------:|------------:|-----------:|----------:|
| Agendamento_Reservar                     | 3,219.4 ns | 137.83 ns |   391.00 ns | 3,100.0 ns |     104 B |
| StatusAgendamento_PodeTransitar_Valido   |   141.0 ns |  18.10 ns |    53.36 ns |   100.0 ns |         - |
| StatusAgendamento_PodeTransitar_Invalido |   128.0 ns |  17.43 ns |    51.40 ns |   100.0 ns |         - |
| EanValidator_Ean13_Valido                | 3,853.5 ns | 547.57 ns | 1,605.94 ns | 3,300.0 ns |         - |
| EanValidator_Ean13_Invalido              | 2,943.9 ns | 118.15 ns |   344.64 ns | 2,900.0 ns |         - |
| EanValidator_Ean8_Valido                 | 2,527.0 ns | 123.73 ns |   364.83 ns | 2,500.0 ns |         - |
| ChaveAcesso_ExtrairUf_SP                 | 2,417.9 ns | 140.61 ns |   403.44 ns | 2,400.0 ns |     224 B |
| ChaveAcesso_ExtrairUf_CodigoInexistente  | 1,852.0 ns | 121.77 ns |   355.22 ns | 1,850.0 ns |     224 B |
| Agendamento_PodeExpirar_Disponivel       |   636.2 ns |  31.62 ns |    90.22 ns |   600.0 ns |         - |
