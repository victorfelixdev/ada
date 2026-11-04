# Viabilidade — etapa 1

## Contratos de leitura do protótipo

- `SensorReading`: identificador estável da biblioteca, dispositivo, nome, tipo, valor anulável, unidade derivada do tipo e instante da coleta.
- `FrameReading`: FPS apresentado, frametime apresentado, CPU Busy e GPU Busy em milissegundos, além do instante da consulta. Amostras sem FPS ou frametime positivo, ou sem avanço do marcador de apresentação nos últimos três segundos, são tratadas como indisponíveis.
- Os coletores operam de forma independente. Erro ou ausência de métricas do PresentMon não impede a leitura de sensores.

## Testes realizados em 04/11/2026

| Caso | Resultado |
| --- | --- |
| Build .NET 10 x64 | Compila sem erros após instalar LibreHardwareMonitorLib 0.9.6. |
| Sensores sem elevar a interface | 112 sensores enumerados nesta máquina com AMD Ryzen 7 5700X; durante o CS2, 96 tinham valor e 16 estavam indisponíveis. Cobertura por categoria e precisão ainda não aferidas. |
| PresentMon 2.6 | Serviço instalado e em execução. Abertura da sessão falhou no processo isolado pelo sandbox, mas funcionou no processo iniciado fora dele. A CLI independente não pôde iniciar ETW sem administrador ou grupo Performance Log Users. |
| CS2 em cena jogável | Cinco consultas pelo PID 35264 retornaram 243,6–304,5 FPS, 3,28–4,11 ms de frametime, 3,16–3,97 ms de CPU Busy e 2,18–2,51 ms de GPU Busy. Os valores variaram durante o teste. |
| Coleta simultânea | A janela WPF mostrou frames recentes do CS2 e 96 de 112 sensores com valor. Ao tomar o foco do jogo, o FPS caiu para cerca de 35; essa leitura não serve como medição de impacto do Ada. |
| Overlay | A faixa transparente apareceu sobre o CS2 com a janela principal minimizada e continuou mostrando as métricas. Passagem de cliques, foco sob interação, tela cheia exclusiva e custo de renderização ainda não foram medidos. |
| Custo aproximado do processo Ada | Em cinco segundos de coleta simultânea com overlay: 0,25 s de CPU, working set de 200,8 MB e memória privada de 136,8 MB. Uma única amostra, sem comparação de frametime com coleta desligada. |

## Próxima validação para aceitar a issue #1

1. Comparar FPS e frametime com a interface oficial do PresentMon usando o mesmo processo e janela temporal; a CLI independente falhou por permissão de ETW. Confirmar que a leitura desaparece quando o jogo deixa de apresentar frames.
2. Verificar as leituras de temperatura, potência, uso e memória de CPU/GPU, incluindo os sensores indisponíveis; repetir sem e com elevação apenas se necessário.
3. Identificar o modo de exibição usado no teste do CS2. Testar passagem de cliques e preservação de foco. Registrar tela cheia exclusiva e outros jogos protegidos como casos distintos.
4. Medir custo de CPU, RAM e frametime com coleta desligada, coleta ligada e coleta mais overlay.

O protótipo usa o loader do SDK PresentMon instalado em `C:\Program Files\Intel\PresentMon\SDK`; empacotamento e descoberta portátil ficam para a etapa de instalação. O SDK e o serviço devem permanecer compatíveis entre si.
