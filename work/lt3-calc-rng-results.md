# LT3a/LT3b — calc-entry probe + RNG map (2026-07-02, live)

Duas rodadas com hooks log-only (perfis `battle-runtime-settings.lt3-logonly.json` e `.lt3b-rng.json`;
código: `CalcEntryProbe*`, `MagicAccuracyControl*`, `StatusChanceControl*`, `RollRngProbe*` no Mod.cs).
Roteiro por batalha: forecast attack → Fire (charged, resolvido) → Blind do Beowulf → turno(s) de AI.

## LT3a — computeActionResult `0x309A44` (hook head, ring 64)

✅ **PROVEN: é a superfície universal de contexto de ação.** `rcx` = order record em `caster+0x1A0`
(`[0]`=caster slot, `[1]`=type, `[2..3]`=ability id), `dl` = target idx. Observado:

| Ação | type | abilityId | fires |
| --- | --- | --- | --- |
| Attack (só preview) | `0x01` | 0 | 1 no preview-open |
| Fire (charged) | `0x0B` | 16 (`0x10`) | contínuo (~10/poll) do confirm até resolver — pending re-avaliado sem parar |
| Blind (Beowulf) | `0x45` | 234 (`0xEA`) | 2-6 |
| Skills inimigas | `0xB0`/`0xB9` | 265/280 | inclui **sweep multi-alvo** |

✅ **AI same-calc PROVEN**: inimigo em turno próprio varreu targetIdx 16 → 20 pelo mesmo record
(`casterTeam=3`) = scoring de candidatos; execução idem. Discriminador funciona: team do caster
(`unit+0x04`) + turnOwner global `dword[0x1407B0708]` (logado coerente o teste todo).

❌ **Hooks `0x304E2E` (magic) e `0x306633` (status): 0 fires** com Fire resolvendo e Blind rolando
71%. Os caminhos real-code de fn `0x304DF0`/`0x3065F0` NÃO são os executados para essas abilities.

## LT3b — RNG map (hook no head `0x278EE0`, que é trampolim Denuvo `jmp`→VM)

Callers reais capturados via return-address (uma batalha):

| Caller | count | last (range, chance) | Leitura |
| --- | --- | --- | --- |
| `0x10A0D713` (VM) | 1 | (100, **71**) | **o roll do Blind — bate exato com o % exibido** |
| `0x10B44721` (VM) | 2 | (100, 97) | acurácia (Fire/ataques) |
| `0x10A5E5F4` (VM) | 3 | (100, 19) | rolls ~19% nos turnos inimigos (crit/evade) |
| `0x107989A3` (VM) | 376 | (100, varia; último 0) | caller quente de batalha (scheduler/AI interno) |
| `0xF16A5D2` (VM) | 128 (só idle) | (100, 50) | coin-flip de idle |
| **`0x30BE8B` (REAL)** | 2 | (100, **61**) | **Brave-gate de reaction — código real, confirmado live** (chance = Brave do defensor; retorno do call `0x30BE86`) |

## Conclusões de controle (consequência doutrinária)

1. **Os rolls de combate (magia/status/crit) rodam DENTRO da VM** — os sites real-code
   `0x304E33`/`0x306636` existem mas não atendem Fire/Blind. Forçar "no roll" está fora da mesa
   para esses caminhos.
2. **Controle correto = dados, como sempre**: INPUT que a VM lê antes (evade bytes ✅, immunity
   bits ✅, Faith snapshot – candidato) ou OUTPUT staged depois do roll e antes do apply
   (pre-clamp ✅ para dano; **apply-mask `+0x1D0` / kind `+0x1C0`** = os candidatos para status e
   miss→hit de magia). Reforça o output-control-first.
3. **Reactions são a exceção real-code**: o Brave-gate `0x30BE86/8B` roda em código real e é
   hookável/forçável (suprimir/forçar reaction por hook é viável).
4. O hook `0x309A44` é o ponto de contexto por (ação, alvo) para o DCL: id em preview, caster/target,
   e distingue player/AI — a espinha do runtime DCL.

## LT4 — output control na janela compute→apply (staged-bundle probe)

**Ponto de injeção achado (static + validado):** o sweep driver `0x281F85 call 0x309A44` tem o
pós-call em **`0x281F8A`** (`inc rbx; cmp rbx,0x15; jl`). Nesse instante o VM já escreveu o bundle
staged no alvo e o engine ainda NÃO aplicou. O índice do alvo (unit index) está vivo em
`[rbp+rbx-0x28]` (rbx = índice do loop; rbp e rbx são callee-saved e sobrevivem ao call). Segundo
caller `0x307F68` (single-target path via global) não hookado ainda.

**Hook `StagedBundleProbe*` (Mod.cs, ExecuteFirst @ 0x281F8A):** deriva `target = 0x141853CE0 +
targetIdx*0x200`, loga `+0x1C0` kind / `+0x1C4` dmg / `+0x1A8` ailment / `+0x1D0` mask / `+0x1E5`
resFlag, e (gated por `ForceTargetCharId`) sobrescreve qualquer campo com `Force*>=0`. Bytes de
validação `48 FF C3 48 83 FB 15`.

Sequência de sub-testes:
- **LT4a (log-only):** confirmar que o bundle está populado por-alvo em 0x281F8A (Fire AoE +
  Blind), e que Blind passa pelo sweep (senão usa o caller 0x307F68). Perfil
  `battle-runtime-settings.lt4a-bundle.json`.
- **LT4b (status force):** `ForceTargetCharId` = alvo, `ForceApplyMask=0x08` (+`ForceAilment`) para
  FORÇAR Blind num alvo que resistiria; e `ForceApplyMask=0` para SUPRIMIR num que pegaria. Prova
  output-control de status.
- **LT4c (magic miss→hit):** num miss de Fire (`kind=0x06`, dmg=0), `ForceKind=0` + `ForceDmg=N` →
  provar que re-stage antes do apply converte miss em hit com dano.
- **Reaction force/suppress (real-code, separado):** hook no Brave-gate `0x30BE86` forçando chance
  0/100 — o único roll real-code; fica para LT5.

## ✅ LT4 RESULTADOS (executado 2026-07-02, hook `StagedBundleProbe*` @ 0x281F8A)

**LT4a (log-only):** o bundle staged é lido por-alvo no pós-compute. Confirmado que passam por aqui:
Fire AoE (4 alvos, dmg 175/99/157/123), Blind (id 234, mas ERROU → `kind=0x06 dmg=0 resFlag=0`),
e **ataques inimigos de execução** (Skeleton matou o Ninja: dmg 180→324). Campos: `+0x1C0` kind
(0x00 hit / 0x06 miss), `+0x1C4` dmg, `+0x1E5` resFlag (0x80 hit-apply / 0x00 miss / bits 0x08|0x01
= proc de status da arma), `+0x1A8` ailment e `+0x1D0` mask **ficaram 0** em todos os casos vistos
(o Blind errou, então staging de status-hit não foi observado aqui — provável que status-apply seja
no outro caller `0x307F68` ou VM-interno).

**LT4b (force-miss, INCONCLUSIVO):** forçar `kind=6/dmg=0/resFlag=0` no ataque físico do Ramza deu
"animação de defesa, sem texto de Miss, e o alvo petrificou" — contaminado: a **arma do Ramza tem
proc de petrify** (natural `resFlag=0x89`, bits 0x08|0x01), caminho separado que não tocamos. Leitura
ambígua.

**LT4b2 (force-dmg, ✅ PROVEN):** forçar só `stagedDmg=111` no charId 0x82 via **Fire** (sem proc de
arma). Dano natural seria 78/138 por alvo (no log); **em jogo todos os esqueletos tomaram exatamente
111**. → **Escrever `target+0x1C4` no pós-compute `0x281F8A` VAZA para o resultado aplicado** — é um
lever de OUTPUT autoritativo, no ponto por-(ação,alvo), para magia (e por simetria física/AI, mesmo
sweep). Segundo lever de dano além do pre-clamp, e mais cedo/rico (tem kind + alvo + contexto).

**Estado dos levers de combate (final da campanha LT):**
- Dano/HP: ✅ pre-clamp `0x30A66F` (apply) **e** staged-bundle `0x281F8A` (compute) — ambos provados.
- Magic miss→hit: campo `kind`+`dmg`+`resFlag` é reescrevível no bundle; o dano provou vazar. O
  texto "Miss"/animação depende de mais que `kind` (LT4b sugere que resFlag/messaging tem nuance) —
  refinar num LT5 sem arma-com-proc.
- Status infliction: ✅ INPUT provado (immunity `+0x5C` suprime — LT2; `StatusOverride +0x1EF/+0x61`
  força — Undead 2026-06-27). Output de status (ailment/mask) NÃO está em 0x281F8A; não é bloqueio.
- Reactions: Brave-gate real-code `0x30BE86` (força/suprime) — LT5.
- AI: ✅ same-calc; os writes de INPUT e o bundle-force em 0x281F8A são vistos pela AI (mesmo sweep).
