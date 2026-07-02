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

## Próximo (LT4, forcing)

- **Status outcome (output)**: rewrite pós-roll de `+0x1D0`/`+0x1C0` (e `+0x1A8` ailment) na janela
  entre compute e apply — provar forçar/suprimir status sem tocar o roll.
- **Magic miss→hit (output)**: no caso miss (`+0x1C0=6`, staged dmg 0), re-stage dmg + kind antes do
  apply (mesma janela do pre-clamp).
- **Reaction força/supressão (real-code)**: hook no Brave-gate forçando chance 0/100.
