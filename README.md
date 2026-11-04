# Ada

Aplicativo Windows para monitorar desempenho de jogos com sensores, métricas de frames, overlay configurável e análise de gargalos.

Stack escolhida: C#/.NET, WPF, LibreHardwareMonitorLib, PresentMon SDK e APIs do Windows.

O [plano de desenvolvimento](PLANO.md) define as etapas e os critérios de conclusão.

## Protótipo da etapa 1

Requer Windows x64, .NET 10 e Intel PresentMon 2.6 instalado com o SDK no caminho padrão. Execute `dotnet run --project src/Ada.App` e informe o PID de um processo gráfico. O aplicativo lista sensores detectados pela LibreHardwareMonitorLib e permite abrir uma janela de overlay de teste. A instalação e o editor visual ainda fazem parte de etapas posteriores.

As observações e pendências de validação estão em [viabilidade.md](docs/viabilidade.md).
