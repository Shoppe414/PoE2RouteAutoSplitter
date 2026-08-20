# PoE2 Route AutoSplitter

Uma ferramenta de configuração e autosplitter do LiveSplit para **speedruns da campanha de Path of Exile 2**.

Versão atual: **v3.0.0 Release Candidate**.

O PoE2 Route AutoSplitter oferece rotas predefinidas e personalizadas para:

* Exploração / conclusão de áreas
* Boss Rush
* Exploração + Boss Rush combinados
* Campaign Any%
* Campaign 100%
* Somente chefes obrigatórios da campanha
* Chefes Pinnacle 0.5
* Temple of Chaos
* Trial of the Sekhemas
* Rotas personalizadas definidas pelo usuário
* Maps

O aplicativo **PoE2RouteSetup** incluído cuida da maior parte da configuração.

Ele permite pausar de forma sincronizada o jogo e o cronômetro do LiveSplit ao abrir o menu de pausa.
A opção Game Time do LiveSplit exclui os tempos de carregamento e pausa o cronômetro quando a opção correspondente está ativa.

Capturas de tela: https://imgur.com/a/VgiRn6o

---
# Políticas de run

Tentei manter a ferramenta o mais independente possível de um conjunto específico de regras. Os jogadores têm bastante liberdade para decidir como administrar as regras da run e quais gatilhos desejam usar.

Em novos começos em Riverbank, o curto período entre o personagem acordar e falar com The Wounded Man não é cronometrado de propósito. Isso dá tempo para corrigir configurações, selecionar “skip tutorial” ou ajustar outras opções antes de realmente iniciar a run. Depois de interagir com The Wounded Man, o tempo começa na última fala de abertura dele.

Os inícios por transição de zona são ativados assim que o personagem entra na zona predefinida. Em runs dinâmicas, o cronômetro e o rastreamento só começam quando o personagem entra nessa zona específica, mesmo que a run tenha começado em outra zona.

Devido à duração do jogo, desenvolvi o GameTimeWatcher, um programa simples que instrui o LiveSplit a pausar o Game Time enquanto o menu Pause Game ou o menu de microtransações estiver aberto. A intenção é permitir pausas ou lidar com situações que exijam atenção total. Outros menus não pausam o cronômetro porque o personagem ainda pode ser controlado. O cronômetro continua durante cenas dentro do jogo, pois o inventário permanece acessível e pode ser usado para otimização da run. Atualmente, o cronômetro só pausa durante telas de carregamento, o menu de pausa e a loja de microtransações.

---

# Download

O download pode ser encontrado [aqui](https://github.com/ScottHoppe414/PoE2RouteAutoSplitter/tags)

OU

Acesse a seção **Releases** deste repositório no GitHub e baixe a versão mais recente:

**`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`**

Para a maioria dos usuários, o instalador é o método recomendado.

Também pode haver um ZIP portátil para quem preferir não usar o instalador. Nesse caso, será necessário usar o PowerShell para executar `\Setup-UI[Configuration]\Build.ps1` e gerar `RouteSetup.exe`.

---

# Início rápido

## 1. Instalar o PoE2 Route AutoSplitter

Execute:

`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`

Siga as instruções do instalador.

Após a instalação, abra:

**PoE2 Route AutoSplitter**

Isso inicia o aplicativo de configuração de rotas.

---

## 2. Escolher sua rota

O aplicativo Setup oferece uma lista de rotas predefinidas.

Selecione a rota que deseja executar.

Exemplos:

* Campaign Any%
* Campaign 100%
* Somente chefes obrigatórios
* Rotas de exploração
* Rotas Boss Rush
* Rotas combinadas de Exploração + Boss Rush

Você também pode selecionar **Custom Route** para criar sua própria rota.

---

## 3. Gerar a configuração do LiveSplit

Depois de selecionar a rota, clique no botão Generate.

O aplicativo criará os arquivos necessários dentro do diretório:

`LiveSplit Target`

Essa pasta contém os arquivos de que o LiveSplit precisa para a rota selecionada.

O conteúdo de **LiveSplit Target** é substituído sempre que uma nova configuração é gerada.

---

# Configuração do LiveSplit

Duas coisas precisam ser configuradas no LiveSplit:

1. O arquivo de splits (`.lss`)
2. O Scriptable Auto Splitter (`.asl`)

## Carregar o arquivo de splits

Na pasta **LiveSplit Target** gerada, localize o arquivo `.lss` e abra-o com o LiveSplit.

Também é possível carregá-lo manualmente usando:

**File → Open Splits → From File**

Selecione o arquivo `.lss` gerado.

---

## Adicionar o Scriptable Auto Splitter

O script do autosplitter deve ser adicionado manualmente ao layout do LiveSplit.

No LiveSplit:

1. Clique com o botão direito no LiveSplit.
2. Selecione **Edit Layout**.
3. Clique no botão **+**.
4. Selecione:

   **Control → Scriptable Auto Splitter**

5. Selecione o novo componente **Scriptable Auto Splitter**.
6. Aponte para o arquivo `.asl` dentro da pasta **LiveSplit Target**.
7. Salve o layout.

Você só precisa alterar esse caminho se mover os arquivos gerados ou trocar para uma configuração que use outro arquivo ASL.

> O PoE2 Route AutoSplitter **não** gera nem substitui seu layout pessoal do LiveSplit.

Seu layout continua sob seu controle.

---

# Configuração de Boss Rush

Rotas que rastreiam chefes usam o programa **BossWatcher** incluído.

O BossWatcher lê os nomes dos chefes no jogo e envia eventos de chefe para o autosplitter.

Se a rota selecionada exigir o BossWatcher, use o botão:

**Start BossWatcher**

dentro do PoE2 Route Setup.

Uma janela de console será aberta.

Durante o uso normal, o BossWatcher mostra apenas eventos úteis, como:

* Chefe encontrado
* Chefe derrotado
* Duração da luta

Exemplo:

`[21:42:18] Encountered: The Executioner`

`[21:43:07] Defeated: The Executioner | Fight time: 49.213 s`

Não é necessário interagir com o console do BossWatcher durante a run.

Mantenha-o aberto durante o speedrun.

---

# Rotas de exploração

Rotas de exploração detectam quando o personagem entra em áreas específicas de Path of Exile 2.

O BossWatcher **não é necessário** para rotas apenas de exploração.

O autosplitter lê automaticamente as informações de transição de áreas do Path of Exile 2.

---

# Exploração + Boss Rush combinados

Rotas combinadas rastreiam:

* Conclusão de áreas
* Derrotas de chefes

Para essas rotas:

1. Carregue o `.lss` gerado.
2. Faça o Scriptable Auto Splitter apontar para o `.asl` gerado.
3. Inicie o BossWatcher pelo PoE2 Route Setup.
4. Comece a run.

Os objetivos de área e de chefe serão tratados pela mesma rota.

---

# Rotas personalizadas

Selecione **Custom Route** no PoE2 Route Setup para criar sua própria rota.

Você pode incluir:

* Áreas
* Chefes
* Áreas e chefes

Adicione os objetivos desejados e organize-os na ordem desejada.

Quando terminar, gere a configuração.

O aplicativo criará dentro de **LiveSplit Target**:

* `.lss`
* `.asl`
* Configuração da rota

Carregue esses arquivos seguindo as mesmas instruções do LiveSplit acima.

---

# Trials

Destinado ao Trial of the Sekhemas e ao Temple of Chaos.

A condição de início ocorre quando você entra pela primeira vez no Trial em si. O saguão usado para preparação não é rastreado.

Há duas condições de término:

1. Você escolhe até que profundidade do Trial deseja chegar. Ao derrotar o chefe da profundidade definida, o Trial termina com sucesso. Não concluir o Trial é considerado uma run fracassada e exige reinício manual.

2. Sair do Trial o marca como concluído. Essa opção é para quem deseja considerar a saída da arena como condição de término. Nesse caso, coletar itens, caches, usar o mercador e escolher a Ascendancy fazem parte da run.

---

# Vaal Ruins

O saguão é considerado uma zona de limite por motivos de transição. Isso significa que entrar na sala do console vindo de um Map é tratado como sair do Map, e não como entrar em uma subárea dele.

Vaal Ruins ainda está em desenvolvimento.

---

# Maps

A preparação de um Map não é cronometrada enquanto o jogador está em um Hideout ou outro hub de Maps. Ao entrar no Map, o cronômetro começa automaticamente e faz split na primeira saída após o chefe da área ser derrotado. Se o jogador sair antes de derrotar o chefe, o cronômetro continua rodando. Isso permite correr até o chefe, derrotá-lo, sair do Map e entrar novamente no mesmo Map para fazer conteúdo extra com o cronômetro pausado. (Veja a política alternativa abaixo.)

Runs de Maps têm várias condições de término:

* Número fixo de Maps
* Até a primeira morte (Deathless Run)
* Finalização manual
* Derrotar um chefe Pinnacle específico

O rastreamento de mortes tem três opções:
* Sem rastreamento de mortes
* Apenas a primeira morte
* Rastrear mortes

Ao selecionar primeira morte ou rastreamento de mortes, você precisa informar o nome do personagem exatamente como aparece no jogo. O programa lê os logs do cliente para identificar a morte do personagem.

Há duas políticas de pausa:

* A derrota de um chefe é usada como evento de conclusão do Map, e o split termina na primeira saída após a derrota. É semelhante à política de conclusão de Maps do PoE2.
* Política alternativa: o cronômetro só pausa em telas de carregamento, durante uma pausa manual ou no menu de microtransações (se ativado). Em todos os outros momentos ele continua rodando, inclusive durante preparação do Map, gerenciamento de inventário e análise de loot.

# Trocar de rota

Para mudar para outra rota:

1. Abra o PoE2 Route Setup.
2. Selecione a nova rota.
3. Gere a configuração novamente.
4. Abra o novo `.lss` no LiveSplit.
5. Verifique se o Scriptable Auto Splitter aponta para o `.asl` dentro de **LiveSplit Target**.
6. Inicie o BossWatcher se a nova rota exigir detecção de chefes.

O conteúdo anterior de **LiveSplit Target** será substituído.

---

# Iniciar uma run

Quando a configuração estiver concluída:

1. Abra Path of Exile 2.
2. Abra o LiveSplit.
3. Carregue o `.lss` da rota.
4. Verifique se o componente Scriptable Auto Splitter está usando o `.asl` correto.
5. Inicie o BossWatcher se a rota usa chefes.
6. Comece a run.

O autosplitter cuidará automaticamente dos objetivos configurados.

---

# Atualização

Quando uma versão mais nova for lançada:

1. Baixe o instalador mais recente em **GitHub Releases**.
2. Execute o instalador.
3. Abra o PoE2 Route Setup.
4. Gere sua rota novamente.

Seu layout pessoal do LiveSplit não precisa ser substituído.

---

# Solução de problemas

## Chefes não estão gerando splits

Verifique se:

* O BossWatcher está em execução.
* Você iniciou o BossWatcher pelo PoE2 Route Setup.
* A rota selecionada realmente contém objetivos de chefe.
* O Scriptable Auto Splitter do LiveSplit aponta para o `.asl` gerado.

---

## Áreas não estão gerando splits

Verifique se:

* Path of Exile 2 está em execução.
* O Scriptable Auto Splitter do LiveSplit aponta para o `.asl` correto.
* Você gerou a rota de exploração correta.
* O `.lss` correto está carregado.

---

## LiveSplit abre os splits errados

Abra o `.lss` diretamente em:

`LiveSplit Target`

ou use:

**File → Open Splits → From File**

---

## Troquei de rota e algo parou de funcionar

Gere a nova rota novamente e verifique:

* O `.lss` correto está carregado.
* O Scriptable Auto Splitter aponta para o `.asl` atual dentro de **LiveSplit Target**.

---

## BossWatcher mostra um erro

Feche o BossWatcher e inicie-o novamente usando o botão **Start BossWatcher** no PoE2 Route Setup.

Se o problema continuar, inclua o erro exibido ao relatar o problema.

---
## BossWatcher fez split cedo demais ou no momento da morte do jogador

O BossWatcher registra quando a barra de vida do chefe sai da tela. Isso pode acontecer por vários motivos. Cabe ao usuário decidir se o split foi correto. A suposição padrão é que o chefe morreu, então o split ocorre. Se o split acontecer sem o chefe ter sido concluído, desfazer o split retorna o LiveSplit ao estado anterior e permite tentar o chefe novamente a partir do tempo atual. O atalho para desfazer split fica nas configurações do LiveSplit.

---

# Arquivos gerados para o LiveSplit

Dependendo da rota selecionada, **LiveSplit Target** pode conter:

### `.lss`

A lista de splits do LiveSplit.

### `.asl`

O script de autosplitter usado pelo componente Scriptable Auto Splitter do LiveSplit.

### Arquivos de rota/configuração

Informam ao autosplitter quais áreas e/ou chefes pertencem à rota selecionada.

### Arquivos de eventos de chefe

Usados pelo BossWatcher e por autosplitters com suporte a chefes.

Não edite esses arquivos manualmente a menos que saiba exatamente o que está alterando.

No uso normal, gere-os por meio do **PoE2 Route Setup**.

---

# Importante

O PoE2 Route AutoSplitter **não** controla nem substitui seu layout pessoal do LiveSplit.

Você é responsável por:

* Aparência do cronômetro
* Cores dos splits
* Fontes
* Tamanho da janela
* Configurações de comparação
* Outros componentes do LiveSplit

O PoE2 Route AutoSplitter fornece apenas os splits da rota e a configuração do autosplitter.

---

# Relatar problemas

Ao relatar um problema, inclua:

* Versão do PoE2 Route AutoSplitter
* Rota/modo em uso
* Se o BossWatcher estava rodando
* O que você esperava que acontecesse
* O que realmente aconteceu
* Qualquer mensagem de erro mostrada pelo PoE2 Route Setup, BossWatcher ou LiveSplit

Isso torna os problemas muito mais fáceis de reproduzir e corrigir.

---

# Verificação do pacote e diagnósticos

Os manifestos SHA-256 usados para verificar arquivos da versão ou do runtime ficam em:

`3 - verification files`

Os manifestos de validação da configuração, os manifestos SHA-256 de cada run, os logs de auditoria e os resumos legíveis dos runs também ficam nessa pasta. Eles permanecem fora de `LiveSplit Target`, para que gerar uma nova rota não apague os arquivos de auditoria de runs anteriores.

Os logs de diagnóstico do SetupUI, BossWatcher e GameTimeWatcher ficam centralizados em:

`4-README's_and_Diagnostics\Diagnostics`

As capturas PNG de diagnóstico ficam em:

`4-README's_and_Diagnostics\Diagnostics\images`

---

# Versão principal atual

**PoE2 Route AutoSplitter 3.x**

A versão 3 adiciona suporte multilíngue ao SetupUI e ao idioma do jogo, nomes localizados e verificados de chefes e áreas quando disponíveis, políticas ampliadas para Campaign, Trials, Vaal Ruins e Maps, diagnósticos e arquivos de verificação centralizados e geometria adaptativa de captura do BossWatcher baseada na altura para clientes de jogo 16:9, ultrawide e super-ultrawide.
