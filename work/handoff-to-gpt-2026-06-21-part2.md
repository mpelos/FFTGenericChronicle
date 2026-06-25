# Handoff para o GPT — você assume o track de runtime/RE daqui (2026-06-21, parte 2)

Continuação do `work/handoff-to-gpt-2026-06-21.md`. Aqui está o estado, meu raciocínio sobre os
próximos passos, e a lista concreta do que recomendo você implementar (os "trackers"). Você toca daqui.

---

## 1. Estado atual (baseline exato)

**Commits na `main`:**
- `1441a92` — resolução de atacante por CT (+0x41) no codemod + 3 smoke tests + demo de fórmula + docs 05/07.
- `159a037` — seu tooling de análise offline (analyze_actor_probe_ct.py, report_runtime_profiles.py, etc.).

**Deployado e pronto pra teste live (em voo agora):**
- DLL nova com CT em `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\` (242 KB, 19:02).
- Profile ativo = demo: `RewriteObservedDamage=true`, `ResolveAttackerByCt=true`, `CtDropWindowMs=4000`,
  `MinHpFloor=1`, `RewriteConditionFormula="event.isDamage && a.present"`,
  `FinalDamageFormula="if(a.present, max(1, a.pa*10 - t.faith), vanillaDamage)"`.
- Neuter data mod deployado. Log antigo arquivado (corrida sai limpa).
- **Pendente:** o usuário roda os ataques de teste; o Claude lê o log e confirma a fórmula computando ao
  vivo (atacante por CT, dano = PA×10−Faith). Assuma que isso confirma; se não confirmar, o log tem
  `[CTX source=ct-reset ctCandidates=...]` + trace pra diagnosticar.

**Arquitetura travada (não re-litigar):**
- "A engine é dona da morte, nós do número." Neuter vanilla → observa hit → escreve
  `HP = max(MinHpFloor, hp − danoCustom)`. Resultado letal fica no piso (1); o chip neutralizado da
  engine no próximo hit mata de verdade. Morte por escrita de bytes é IMPOSSÍVEL (provado, Test 2b/2c).
- Atacante = unidade (≠ alvo) cujo CT (+0x41) resetou mais recentemente. Alvo = unidade cujo HP mudou.

---

## 2. Mapa do código (pra você editar)

**`codemod/fftivc.generic.chronicle.codemod/ActionContextResolver.cs`**
- `BattleContextResolver.ResolveRecentAttacker` → tenta `ResolveByCt` primeiro, cai pro legado.
- `ResolveByCt`: candidatos ≠ alvo, vivos, com CT-drop dentro de `CtDropWindowMs`; ordena por drop mais
  recente, desempate por CT absoluto menor e maior drop. Label "ct-reset".
- `UnitObservation(Unit, SeenTick, CtDropTick, CtDropAmount)`; `CtCandidate`.

**`codemod/fftivc.generic.chronicle.codemod/Mod.cs`**
- Hook: `battle_base_ptr` em `module+0x226D98`, sig `0F B7 41 30 66 89 42 0C` (= `movzx eax,[rcx+0x30]`,
  rcx = ponteiro da unidade). `Poll()` (~150) → `PollRegisteredUnits` (~189) lê cada unidade a cada tick.
- `ProcessObservedUnit` (~205): rastreio do CT-drop (~223-238) + detecção do evento de dano (~258-283) que
  chama `_contextResolver.ResolveRecentAttacker` (~272) e `MaybeRewriteHpEvent` (~280).
- `TryCreateUnitSnapshot` (~332): parser do struct; lê `ct = rb(0x41)`.
- `MaybeRewriteHpEvent` (~538): rewrite + `MinHpFloor` + `[REWRITE-SKIP-DEATH]`.
- `BuildFormulaContext` (~1438), `AddUnitVariables` (~1755-1777, expõe `.pa .ma .faith` etc. — NÃO expõe
  `.ct` ainda), `attacker.sourceRecent` (~1494).
- Settings (~2959): `ResolveAttackerByCt`, `CtDropWindowMs`, `MinHpFloor`, `ActorProbe*`; `Describe()` (~3053).

**Offsets do struct (0x200 bytes):** charId+0x00, team+0x04, foe+0x05&0x10, Level+0x29, Brave+0x2B,
Faith+0x2D, HP+0x30, MaxHP+0x32, MP+0x34, PA+0x3E, MA+0x3F, Speed+0x40, **CT+0x41**, status+0x61(bit0x20=KO).

**Verificação (prove offline antes de live):** `codemod/run-offline-checks.ps1` (gate completo);
`settingsvalidate` (valida profile); `settingssimulate <profile> <cenário.json>` (prova a matemática da
fórmula deterministicamente — veja `work/runtime-simulation.custom-formula-demo.json`); smoke tests em
`smoketests/Program.cs`; seu `analyze_actor_probe_ct.py` + `report_runtime_profiles.py`.

---

## 3. Meu raciocínio sobre os próximos passos

A demo prova fórmulas dependentes de **atributos do atacante + alvo**. Pra chegar num sistema de batalha
custom completo, faltam quatro dimensões: **(a)** identidade da ação (elemento, família da skill,
físico/mágico, arma) — sem isso a fórmula não consegue ramificar pelo que foi usado; **(b)** morte limpa
no mesmo hit (hoje é kill de 2 hits); **(c)** equipamento (parcialmente coberto via slots); **(d)**
robustez (counters, AoE, skillsets especiais).

Minha priorização e o porquê:
- **(a) identidade da ação é o próximo de maior valor E não está travado por RE difícil** — dá pra fazer
  AGORA pelo canal sentinela que a arquitetura sempre previu. É a dimensão de contexto que falta.
- **(b) pre-damage window é o maior teto, mas é RE pesado e aberto** — roda em paralelo como pesquisa,
  não no caminho crítico.
- **(d) robustez protege o que já temos** — barato, importante (counters podem quebrar a atribuição).

---

## 4. O que recomendo você implementar (concreto)

### PRIORIDADE 1 — Identidade da ação via canal sentinela (implementável já, sem RE novo)
A meta: resolver QUAL ação/arma foi usada (hoje `action=none`), pra expor `action.element`,
`action.isMagical`, família etc. às fórmulas. Mecanismo (já planejado na arquitetura):
- Hoje o neuter colapsa todo dano ofensivo num placeholder uniforme. Em vez disso, **emita um placeholder
  pequeno DISTINTO por família/elemento de ação** (bandas sentinela), não-letais e distinguíveis.
- No runtime, **decodifique o `vanillaDamage` observado** de volta em variáveis de ação via
  `ActionSignalRules` (já existe; siga o exemplo `docs/modding/examples/battle-runtime-settings.sentinel-bands.example.json`
  + a sim correspondente).
- Onde mexer: `tools/build_neuter_data.py` (lado dos dados — atribuir as bandas) + um profile com
  `ActionSignalRules` (lado do decode). Prove offline com `settingssimulate` (um cenário por banda) e
  registre no `report_runtime_profiles.py`. Depois o Claude/usuário valida live (atacar com Fire/Bolt/
  físico e confirmar o sinal decodificado).
- **Cuidado honesto:** a banda não-letal tem pouca largura → poucas bandas distinguíveis. Decida a
  taxonomia (elemento? físico/mágico? ~6 famílias?) e documente o que cobre e o que não cobre.

### PRIORIDADE 2 — Pre-damage window / "stat puppeteering" (A2) — RE, em paralelo
A meta: achar um sinal que dispara ANTES da escrita de HP, sobrescrever o stat do atacante logo antes do
cálculo da engine → a engine computa NOSSO número E mata no mesmo hit (elimina o custo de 2 hits).
- Construa um **probe de CONTEXTO de batalha** (além do struct por-unidade): em cada disparo do hook,
  snapshote o estado de nível-de-batalha em volta de `battle_base_ptr` procurando **(i)** um ponteiro/índice
  de "unidade agindo agora" e **(ii)** um campo de "ação/alvo/dano pendente" que apareça ANTES do HP mudar.
  rcx é a unidade; caminhe pra fora pro battle manager.
- Cruze com `docs/modding/04-re-strategy.md` (mapa de funções `BATTLE_*` do decomp PSX) pra achar as
  entradas da rotina de cálculo e um touchpoint NÃO-virtualizado logo antes dela (a rotina em si é Denuvo).
- Bônus: o ponteiro de "unidade agindo" (ii) é um sinal de atacante mais robusto que a inferência por CT —
  vale plugar como fonte primária com o CT de fallback.

### PRIORIDADE 3 — Robustez do resolver por CT + edge cases (barato, faça já)
- **COUNTERS (gap real):** um contra-ataque NÃO reseta o CT do contra-atacante (reação não consome turno).
  Sequência: A bate em B (evento em B, atacante=A por CT ✓); B contra-ataca → A perde HP. Nesse 2º evento o
  alvo é A (excluído), e ninguém mais teve CT-drop recente (B não resetou) → `ResolveByCt` retorna null e o
  counter fica sem atacante. **Trate:** o atacante de um counter é a unidade que ACABOU de ser dano-ada pelo
  agora-alvo — i.e., inverta o evento anterior (A↔B). Detecte o padrão (dois eventos de HP em rajada entre o
  mesmo par). É um gap de correção que importa.
- **AoE / multi-hit:** já funcionam (o CT-drop do atacante é recente pra todos os eventos dos alvos; vimos
  o dual-wield do Ninja resolver 2/2). Só confirme nos testes.
- **Exponha `attacker.ct`/`target.ct`** às fórmulas em `AddUnitVariables` (eu faço o parse do Ct mas não
  exponho). E opcionalmente `attacker.sourceCt` (espelhe `sourceRecent` em ~1494) pra fórmulas/trace verem
  que a resolução foi por CT.

### PRIORIDADE 4 — Gap do neuter (dados)
Materia Blade+ (ataque básico do Cloud — a fórmula da arma ignora o WP=1 neutralizado e one-shota),
Cloud Limit, e abilities %-damage/Gravity (ignoram X/Y/WP). Ache o lever certo: qual formula id o ataque
básico da Materia Blade+ usa, se `ItemWeaponData` tem um campo separado, e um neuter próprio pro %-damage.

---

## 5. Divisão de trabalho (mantém)
O Claude roda TODOS os scripts/builds/deploys/análise-de-log e coordena o teste live com o usuário (que só
faz ações in-game GUI). Você faz RE/análise offline/dev-de-codemod e entrega specs + dados + código que o
Claude integra e leva pra live. Pra evitar dois editando `Mod.cs` ao mesmo tempo, alinhe pelo arquivo
(você pode mandar patch/spec; o Claude integra). A fila de teste live é serial.

Bom proveito. O alicerce está provado: HP-write funciona, morte é da engine, atacante resolve por CT,
fórmula custom atacante+alvo roda. As peças que faltam são identidade-da-ação, morte same-hit e robustez.
