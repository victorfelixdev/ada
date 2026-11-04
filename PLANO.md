# Plano de desenvolvimento do Ada

## Princípios e limites

- A coleta é somente leitura e não injeta código nem altera o processo do jogo.
- O catálogo mostra todos os sensores detectados, com unidade, origem, disponibilidade e atualização. A cobertura depende do hardware e do fabricante; fontes sem telemetria não fornecem leituras próprias.
- FPS, frametime, CPU Busy e GPU Busy são métricas de apresentação e execução de frames, com limitações que variam por API gráfica, driver e configuração do Windows.
- O diagnóstico de gargalo é uma estimativa explicável, não uma certeza. Limites de FPS, V-Sync e geração de frames precisam ser reconhecidos.
- O alerta de VRAM representa pressão sobre memória dedicada e orçamento de vídeo; swap é inferido por sinais correlacionados e nunca afirmado sem evidência suficiente.
- Overlay dentro de jogos protegidos ou em tela cheia exclusiva depende de compatibilidade a ser validada. O monitoramento no aplicativo deve continuar disponível quando o overlay não aparecer.

## Etapas e issues

### 1. Arquitetura, contratos e protótipo de viabilidade

Entregas: solução .NET/WPF mínima; contratos de métricas e sensores; protótipos de leitura do PresentMon, descoberta de sensores, janela transparente e coleta simultânea; matriz inicial de jogos, GPUs e modos de exibição.

Aceite: demonstrar FPS, frametime e ao menos sensores disponíveis em um PC real; registrar permissões, custo de coleta, compatibilidade e limitações antes de consolidar a arquitetura.

Agente: **Astra, esforço alto** para decisões e riscos de integração; **Sol, médio** para a solução inicial. Revisão cruzada por agente distinto.

### 2. Coleta de frames e seleção de jogo

Entregas: integração com PresentMon SDK, seleção automática e manual do processo, FPS, frametime, CPU Busy, GPU Busy e estados indisponíveis; histórico curto em memória.

Aceite: valores comparados com PresentMon em cenários controlados; troca e encerramento de jogo sem travamento ou dados antigos; teste com limite de FPS e ao menos dois modos de exibição.

Agente: **Astra, alto** para interop/ETW e validação; **Sol, médio** para UI de seleção e testes.

### 3. Catálogo de sensores

Entregas: integração de LibreHardwareMonitorLib e APIs Windows quando necessário; descoberta por categoria (placa-mãe, CPU, RAM, GPU, fonte quando exposta); nomes, unidades, frequência e estados de indisponibilidade.

Aceite: usuário escolhe qualquer sensor detectado; leituras ausentes ou inválidas não aparecem como zero; validação em hardware de fabricantes diferentes e registro da cobertura.

Agente: **Sol, médio** para integração; **Astra, alto** para investigar diferenças de hardware e permissões.

### 4. Editor visual e perfis

Entregas: editor WPF com arrasta e solta, redimensionamento, renomeação de componentes e labels; contadores e gráficos; prévia, salvamento e restauração de perfis; configuração guiada por objetivo (FPS, temperatura, gargalo ou visão completa).

Aceite: usuário não técnico cria, salva e reabre um layout sem editar arquivos; controles acessíveis por teclado; sensor indisponível identificado no editor.

Agente: **Sol, médio** para interface e persistência; **Luna, médio** para textos e cenários de usabilidade; revisão de acessibilidade por **Sol, alto**.

### 5. Overlay e controle durante o jogo

Entregas: janela transparente, sempre visível e sem capturar cliques durante o jogo; alternância por atalho; posicionamento e escala; indicação de incompatibilidade quando aplicável.

Aceite: não bloquear entrada no jogo, não causar queda relevante de desempenho conforme orçamento definido nos testes, funcionar nos modos suportados da matriz e desligar sem deixar recursos ativos.

Agente: **Astra, alto** para Win32/WPF, compatibilidade e desempenho; **Sol, médio** para controles e perfis.

### 6. Análise de gargalo e pressão de VRAM

Entregas: classificação CPU bound, GPU bound, limitado por FPS ou inconclusivo; evidências visíveis; alertas de pressão de VRAM com uso dedicado, orçamento, memória compartilhada e correlação com frametime, quando disponíveis.

Aceite: testes com cenários sintéticos e sessões reais; diagnósticos inconclusivos quando faltam sinais; não confundir alocação de VRAM com prova de swap.

Agente: **Astra, alto** para regras e calibração; **Sol, alto** para testes e explicações ao usuário.

### 7. API local para o futuro aplicativo Android

Entregas: contrato versionado para descoberta, catálogo, amostras em tempo real e perfis; autenticação por pareamento, acesso limitado à rede local e desligamento do servidor pelo usuário.

Aceite: cliente de teste recebe métricas e reconecta; conexões não autorizadas são recusadas; o monitoramento local funciona sem telefone; contrato documentado para React Native.

Agente: **Sol, médio** para API; **Astra, alto** para revisão de segurança e modelo de pareamento.

### 8. Instalador, qualidade e lançamento inicial

Entregas: pacote instalável e desinstalação, licenças de terceiros, logs locais de diagnóstico, matriz de compatibilidade, documentação básica e testes de instalação limpa, atualização e remoção.

Aceite: instalar, executar e desinstalar em Windows limpo sem perda de perfis na atualização; falhas de permissões ou componentes exibem orientação clara; regressões principais verificadas.

Agente: **Sol, médio** para empacotamento e documentação; **Luna, baixo** para checklist repetível; **Astra, alto** para revisão final de riscos.

## Ordem de execução

Concluir a etapa 1 antes de fixar a arquitetura de overlay e coleta. Etapas 2 e 3 podem avançar em paralelo depois dos contratos; o editor depende do catálogo de métricas, e o overlay reutiliza os layouts do editor. A API local reutiliza a mesma coleção de amostras sem criar uma segunda lógica de monitoramento.

Cada issue deve ter um responsável, entregáveis, critérios de aceite, dependências e evidências dos testes. Um agente implementa; outro revisa mudanças em interop, segurança, cálculo de diagnóstico e instalador.
