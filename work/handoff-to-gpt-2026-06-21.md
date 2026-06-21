# Handoff para o GPT — track de teste live (2026-06-21)

Resumo de tudo que foi descoberto no período em que você não estava atuando. Você sincronizou pela
última vez no nível do **FIX PASS 1** (neuter de weapon/ability + harness do death-gate, commit
`465542d`). Desde então rodamos a sequência de testes live até o fim e **viramos a arquitetura**.
Quatro descobertas, em ordem de importância. Tudo abaixo é **provado in-game**, não teoria.

Contexto fixo que continua valendo: hook `battle_base_ptr` (sig `0F B7 41 30 66 89 42 0C`,
module+0x226D98), registry de ponteiros de unidade, poll ~25ms, struct de 0x200 bytes, HP em `+0x30`.
Denuvo continua bloqueando hookar a rotina de fórmula virtualizada — por isso a abordagem é
"observar o resultado e reescrever", nunca hookar o cálculo.

---

## 1. Morte por escrita de memória é IMPOSSÍVEL (decisivo, fecha a questão)

Tínhamos mapeado em 2a que toda morte real muda **exatamente** `+0x30→00` (HP) **e** `+0x61: 00→20`.
A hipótese natural era: escreve esses mesmos bytes → unidade morre. **Está REFUTADO, em definitivo:**

- **HP=0 sozinho → zumbi.** Unidade fica de pé com 0/HP, CT continua subindo, ela toma turnos.
  (Screenshot do usuário: Beowulf 0/314, vivo e ativo.)
- **HP=0 + `+0x61 |= 0x20` (nosso `DeathStateWrites`/`CauseDeathOnZeroHp`) → AINDA zumbi.** Log:
  `[DEATH-WRITE +0x61 0->20]`, `[DEATH-DIFF +0x30->00 +0x61:00->20]`, e logo depois
  `[HEALING 0->27->61]` — **Regen curou a unidade de volta**. Regen não tica em unidade morta ⇒ a
  engine ainda a considera VIVA. Escrever o bit produz um estado parcial bugado (unidade fica imune,
  ataques atravessam) que a engine nunca espera ver.
- **Conclusão:** `+0x61 0x20` é **EFEITO** da morte, não **gatilho**. A morte é uma transição de
  estado interna (provavelmente dentro da rotina de dano protegida por Denuvo) que atualiza
  estruturas **fora** do struct da unidade (turn manager / lista de unidades ativas), chaveada pela
  engine atingir 0 de dano por conta própria. Conseguimos **replicar os sintomas, não invocar a
  rotina**. Reescrever bytes de fora não dispara.

Implicação que travou a arquitetura: **a MORTE tem que pertencer à vanilla.** Nós não causamos morte.

---

## 2. Arquitetura que FUNCIONA — "a engine é dona da morte, nós somos donos do número"

Provado live (LIVE TEST 3). Novo lever no `Mod.cs`: **`MinHpFloor`** (default 0).

- Toda reescrita de HP faz clamp em **≥ MinHpFloor**, então NUNCA escrevemos 0 → nunca criamos zumbi.
- `MaybeRewriteHpEvent` faz **skip** quando o HP observado já é ≤ 0 (`[REWRITE-SKIP-DEATH]`), pra não
  ressuscitar um kill que a engine fez.

Fluxo: **neutraliza a vanilla → observa o hit → escreve HP = max(MinHpFloor, hp − dano_custom)**.
Resultado letal **deixa a unidade no piso (1 HP)**; o **próprio chip neutralizado da engine no
PRÓXIMO hit** leva 1→0 e a **engine mata de verdade** (aí sim `+0x61` é setado pela rotina real dela,
e a unidade FICA morta — sem Regen revivendo).

Evidência: hit 1 → `[REWRITE …HP 304->1]`; hit 2 → `[DEATH-DIFF +0x30:01->00 +0x61:00->20]` → morte
real e permanente. **Custo: morte é kill de 2 hits** (nosso write trava em 1, o chip da engine fecha
no hit seguinte). Morte limpa em 1 hit depende da *pre-damage window* (ver seção 5).

➡️ **A meta central do projeto — fórmula de dano custom dependente de atacante+alvo+equipamento — está
provada viável.** Já conseguimos arbitrar o número de HP aplicado; a morte fica delegada à engine.

---

## 3. ⭐ NOVO (ainda não documentado): resolução de ATACANTE por CT — `+0x41` = Charge Time

Esse é o bloco principal que você ainda não tinha. Até aqui só sabíamos o **alvo** (quem teve HP
mudado); todos os `[RUNTIME]` mostravam `attacker=none action=none`. Sem atacante, fórmula dependente
de atacante não computa. **Resolvido por RE de struct.**

Rodei um perfil **observe-only** (`actor-probe.json`) que, em todo evento de dano, dá snapshot da
janela `0x40–0x52` de **todas** as unidades registradas (`[ACTOR-PROBE]`). O usuário fez **6 ataques
controlados** e me disse quem bateu em quem. Correlacionando:

- **`+0x40` = Speed** (confirmado). Estável por unidade, valores distintos:
  Ramza=10, Ninja=16, Agrias=12, Cloud=9, Beowulf=9.
- **`+0x41` = CT (charge time).** Sobe a cada tick proporcional ao Speed; quando a unidade age, **reseta
  pra um valor baixo**. **O atacante é a unidade registrada (≠ alvo) cujo CT acabou de resetar / está
  mais baixo / teve a maior queda recente.** Modelo de CT clássico de FFT (CT += Speed por tick, age em
  ≥100), o que corrobora fortemente a leitura.

### Evidência bruta (CT `+0x41` por unidade, em cada evento de dano)

Time: **Ramza=0x01, Ninja=0x80, Agrias=0x1E, Cloud=0x32, Beowulf=0x1F.**

| # | Ataque relatado | Atacante real | Evento de HP | CT — Ramza / Ninja / Agrias / Cloud / Beowulf | Menor CT |
|---|---|---|---|---|---|
| 1 | Ninja→Agrias (hit 1, dual wield) | Ninja `0x80` | `0x1E 322→310` | 70 / **12** / 84 / 63 / 63 | Ninja ✓ |
| 1 | Ninja→Agrias (hit 2) | Ninja `0x80` | `0x1E 310→298` | 70 / **12** / 84 / 63 / 63 | Ninja ✓ |
| 2 | Agrias→Beowulf | Agrias `0x1E` | `0x1F 314→304` | 90 / 64 / **8** / 81 / 81 | Agrias ✓ |
| 3 | Ramza→Cloud (Mana Shield) | Ramza `0x01` | — (foi pra MP, sem evento de HP) | — | — |
| 4 | Beowulf→Agrias | Beowulf `0x1F` | `0x1E 298→295` | 20 / 52 / 64 / 28 / **8** | Beowulf ✓ |
| 5 | Ramza→Agrias | Ramza `0x01` | `0x1E 295→281` | **0** / 60 / 100 / 100 / 100 | Ramza ✓ |
| 6 | Cloud→Beowulf (Materia Blade+, letal) | Cloud `0x32` | `0x1F 304→0` | **0** / 60 / 40 / **0** / 100 | empate → delta |

- **5/6 fecham no CT-mínimo absoluto.**
- O #6 é o único empate (Ramza=0 e Cloud=0). **Desempate por delta de CT:** Cloud caiu 100(no #5)→0(no
  #6) = acabou de agir; Ramza já estava em 0 no #5 e seguiu 0 (não caiu). ⇒ atacante = **Cloud**. **6/6
  com o tiebreak por maior queda recente.**
- #3 (Mana Shield) não gerou evento de HP (dano foi redirecionado pra MP pela engine) — consistente,
  não é falha.

Corroboração: no #5 os três que ainda não agiram (Agrias/Cloud/Beowulf) estavam todos em CT=100
("carregado, esperando na fila de ação"), e Ramza, que acabou de agir, estava em 0. Bate exatamente
com a mecânica de CT do FFT.

### Regra de implementação (vou codar)
Trocar a heurística frágil de recência (`InferAttackerFromRecentUnits` / `ResolveRecentAttacker`) por:
manter histórico de CT (`+0x41`) por ponteiro; no evento de dano, atacante = unidade registrada,
≠ alvo, com **maior queda de CT recente** (tiebreak), ou menor CT absoluto. Faction-agnóstico.

---

## 4. Gap do neuter (TODO do mod real)

O neuter de X/Y em `OverrideAbilityActionData` **não cobre** alguns skillsets especiais — eles
contornam o X/Y:
- **Materia Blade+ do Cloud (ataque básico):** a fórmula da arma ignora o WP neutralizado → dá dano
  pesado / one-shot (foi o que matou o Beowulf no #6).
- **Cloud Limit** e **algumas magias** (provável %-damage / Gravity, que ignoram X/Y/WP).
- O classificador `HP`+`TargetEnemies` & não-`TargetAllies` é estrito demais e pulou skills ofensivas
  em AoE que também acertam aliados.

Não bloqueia a arquitetura (esses ainda caem no leave-at-1 do MinHpFloor), mas precisa de uma rota de
dado/fórmula própria pra esses casos antes de virar mod "de verdade".

---

## 5. Estado atual e onde eu mais preciso de você (RE pesado)

**Próximo passo que EU faço:** implementar a resolução de atacante por CT no codemod, buildar, rodar
os offline checks, deployar, e montar uma **demo de fórmula real** (ex.: `dano = attacker.pa*4 −
target.faith`) pra o usuário testar live — o payoff que demonstra fórmula custom dependente de
atacante+alvo+equipamento funcionando in-game.

**Onde a sua RE em paralelo vale mais (itens A2/C1):**

1. **Pre-damage window (stat puppeteering, A2 — o grande unlock).** Achar um sinal que dispare ANTES da
   escrita de HP (na turn-state / em volta de `battle_base_ptr`). Se acharmos, dá pra sobrescrever o
   stat do atacante **logo antes** do cálculo da engine → a engine computa **nosso número exato E mata
   no mesmo hit** (fórmula arbitrária + morte limpa em 1 hit, eliminando o custo de 2 hits da seção 2).
2. **Ponteiro de "unidade agindo" direto no battle struct.** Existe um campo "currently-acting unit" na
   estrutura de batalha? Seria mais robusto/barato que inferir por CT. Vale um olhar de RE em paralelo
   (eu sigo com o CT, que já funciona).
3. **Rota de dado pros skillsets da seção 4** (Materia Blade+, Cloud Limit, %-damage/Gravity).

Mapa de offsets consolidado do struct (0x200 bytes): charId `+0x00`, team `+0x04`, foe-bit `+0x05&0x10`,
Level `+0x29`, Brave `+0x2B`, Faith `+0x2D`, HP `+0x30` (word), MaxHP `+0x32`, MP `+0x34`, PA `+0x3E`,
MA `+0x3F`, **Speed `+0x40`**, **CT `+0x41`** (novo), status `+0x61` (bit `0x20` = KO, efeito não gatilho).
