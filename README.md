# Generic Chronicle

**Generic Chronicle** e um mod para **FINAL FANTASY TACTICS - The Ivalice Chronicles** com o objetivo de reescrever o balanceamento de batalha a partir das formulas de dano, preservando a sensacao, a estetica e o espirito do FFT original.

O foco inicial e corrigir um problema estrutural do FFT: no fim do jogo, poucas categorias de equipamento continuam relevantes, especialmente espadas, enquanto muitas armas, armaduras e acessorios acabam virando escolhas inferiores ou situacionais demais. Este mod pretende fazer com que cada familia de arma, armadura e equipamento tenha uma funcao real, uma identidade clara e um motivo concreto para existir em builds diferentes, sem transformar o jogo em outro sistema.

## Objetivo Principal

O primeiro grande passo do mod sera uma reescrita ampla das formulas de dano e efeitos de batalha.

As novas formulas podem usar **GURPS** como uma referencia util, especialmente por suas ideias de **thrust**, **swing**, penetracao de armadura e tipos de dano. Mas isso nao e uma regra fixa, nem uma obrigacao de design. A intencao nao e aproximar FFT de um RPG de mesa, nem copiar tabelas literalmente. O criterio principal e sempre o resultado final dentro de FFT: formulas mais ricas, armas mais bem balanceadas e familias de equipamento com identidade real.

Qualquer referencia externa deve servir ao objetivo do mod, nao comandar o mod. Se uma ideia inspirada em GURPS ajudar a balancear melhor as armas mantendo o espirito de FFT, ela pode ser usada. Se atrapalhar a leitura, o ritmo, a estetica ou o balanceamento do jogo, ela deve ser adaptada ou descartada.

O jogador deve sentir que esta jogando um FFT melhorado. A complexidade pode existir por baixo, mas a leitura externa precisa continuar clara: cada familia de arma deve parecer naturalmente util por motivos diferentes, sem exigir que o jogador faca calculos ou aprenda outro jogo.

Na pratica, isso significa que armas diferentes devem escalar e se comportar de formas diferentes:

- espadas continuam boas e familiares, mas deixam de ser a resposta universal para dano fisico;
- lancas, machados, arcos, bestas, facas, katanas, cajados, punhos e armas exoticas devem ter papeis mecanicos proprios por familia;
- armaduras, escudos, acessorios e bonus de equipamento devem influenciar escolhas de sobrevivencia e build, nao apenas fornecer numeros maiores;
- magias tambem terao formulas revisadas para que dano, cura, custo, tempo de conjuracao e escalamento sejam parte de um mesmo equilibrio.

O objetivo desta fase nao e deixar o jogo mais dificil. Mudancas de dificuldade podem acontecer como consequencia do rebalanceamento, mas a meta principal e diversidade real de equipamentos no endgame: jogadores devem querer usar personagens com familias de armas diferentes porque cada familia tem uma identidade forte e viavel.

## Ordem de Trabalho

### Fase 1 - Formulas e Equipamentos

A primeira fase e estabelecer o novo modelo de dano por familia de arma:

- definir como cada tipo de arma calcula dano;
- decidir quais referencias e comportamentos fazem sentido para cada familia de arma, incluindo thrust, swing ou outras ideias apenas quando ajudarem o balanceamento;
- revisar formulas de magia, cura e efeitos especiais;
- ajustar a utilidade relativa de armas, armaduras, escudos e acessorios;
- validar quais mudancas podem ser feitas apenas por dados e quais exigem code mod.

Esta fase nao vai tentar balancear cada item individual. O escopo inicial e a identidade das familias de armas e das formulas que sustentam essas familias. Itens especificos, progressao fina e ajustes numericos detalhados ficam para etapas posteriores.

Esta fase vem antes de qualquer redesenho grande de jobs, porque os jobs dependem do valor real das armas, magias e equipamentos.

### Fase 2 - Jobs

Depois que o modelo de dano estiver funcionando, o mod passara para um balanceamento completo dos jobs.

Alguns jobs serao apenas ajustados. Outros podem ser removidos, substituidos ou reconstruidos com uma funcao nova. Essa etapa deve acontecer sobre uma base de formulas ja estabilizada, para evitar balancear classes em cima de um sistema de dano ainda instavel.

## Base Tecnica

O projeto esta sendo construido em cima de pesquisa de modding para FFT IVC:

- `docs/modding/` contem o mapa tecnico das tabelas, formulas, limites e estrategia de reverse engineering;
- `mod/fftivc.generic.chronicle/` e o pacote de dados do mod para Reloaded-II;
- `codemod/fftivc.generic.chronicle.codemod/` contem o esqueleto C# para investigacao e futuros hooks de batalha;
- `tools/` contem scripts auxiliares para baseline de dados, ENTD e inventario das tabelas.

Por enquanto, a direcao e fazer o maximo possivel por dados (`NXD`, `TableData XML` e `ENTD`) e usar code mod apenas quando uma formula ou mecanica nao puder ser expressa pelo catalogo existente do jogo.
