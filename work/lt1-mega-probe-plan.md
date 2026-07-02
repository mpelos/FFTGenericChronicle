# LT1 — mega-probe read-only (um teste, máximo de informação)

Estratégia (2026-07-02): em vez de um live test por frente, UM probe observa todas as frentes ao
mesmo tempo com delta-logging, e UMA batalha com um roteiro de ações cobre os itens 1, 2, 3, 4 e 6
do `work/dcl-live-test-master-plan.md` e observa o item 5 (semântica do `g_7B07AC`) — tudo
read-only, zero risco de contaminação (perfil neutro deployado: todos os levers do mod OFF).

## Peças

- Probe: `tools/lt1_mega_probe.py` (attach READ-ONLY em FFT_enhanced.exe, ~40 Hz).
  - Log humano: `work/lt1_mega_probe.log`; raw completo: `work/lt1_mega_probe.raw.jsonl`
    (bloco 0x200 inteiro a cada mudança → re-análise offline sem repetir o teste).
  - Observa: bloco global de ação (`0x14186AFF0` ability id / `0x14186AFF4` caster idx / ptrs),
    forecast global, `g_7B07AC`, e por unidade: turn markers, posição/facing, os 4 arrays de
    status (com decode de bits), durações, pending record, staged fields, JP/nibbles — e QUALQUER
    outro byte que mudar no struct (auto-mute de ruído de frame) → o teste também descobre campos
    não previstos.
- Perfil do mod: `work/battle-runtime-settings.lt1-neutral.json` (deployado em
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`).

## O que cada ação do roteiro prova

| Ação in-game | Frente confirmada |
| --- | --- |
| Mover 1 tile + mudar facing no Wait | posX/posY `+0x4F/50`, facing `+0x51` |
| Abrir forecast de ataque básico e cancelar | ability id em `0x14186AFF0` no preview-open (a pergunta universal) |
| Forecast de skill nomeada e cancelar | id muda por ação; caster idx correto |
| Fire (charged) num tile | pending record `+0x1A1/+0x1A2/+0x18D` + epicentro `+0x1AC/+0x1B0` |
| Habilidade de status num inimigo | `g_7B07AC` staged %, `+0x1A8` ailment, `+0x1D0` mask, arrays de status flipando com decode |
| Turno da AI passar | `+0x1B8` exactly-one segue a AI; pulsos de staged fields = evidência same-calc (bônus item 10) |
| Fim de batalha (opcional) | delta nos arrays JP `+0xF0/+0x11E` e nibbles `+0xE4` |
| (automático, sem ação) | tile map `0x140D8DCB0`: dims, altura sob cada unidade (log no move), byte de marcação `+5` durante o highlight de targeting/AoE |

## Sequência de testes seguintes

- LT2 (mesma sessão de jogo, sem rebuild): pokes — forçar `g_7B07AC` 100/0 durante ação de status;
  write-test de um byte de immunity (`+0x5C..0x60`).
- LT3 (rebuild, jogo fechado): hooks log-only — magic always-hit `0x304E2B`, gates de reaction,
  action-id no pre-clamp, watcher de AI same-calc.

Resultados e vereditos: apêndice abaixo.

## ✅ RESULTADOS — LT1 + LT2 executados 2026-07-02 (log: `lt1_mega_probe.log`, raw: `.raw.jsonl`)

Roteiro executado pelo Marcelo: Ninja move+facing; Agrias→Cloud forecast attack (201/100%);
Agrias→Cloud forecast Fire (157/100%); Fire confirmado (AoE: Agrias 99, Cloud "157" via Mana
Shield, Beowulf 123); Beowulf→Cloud Blind 100% (pegou); turnos de AI durante o charge.
LT2 na mesma sessão: immunity poke na Agrias → Blind 0% + errou; poke `g_7B07AC=0` →
engine reescreveu 3854 no compute → Blind 100% pegou.

| Frente | Veredito |
| --- | --- |
| Turn owner `+0x1B8` | ✅ **PROVEN** — exactly-one em todos os turnos (player+AI); `+0x1BA` = dono da ação em voo (set no confirm); NOVO: `+0x2E` flipa na concessão do turno |
| Posição/facing `+0x4F/50/51` | ✅ **PROVEN** — todos os moves/facings trackeados com tile coerente |
| Tile table `0x140D8DCB0` | ✅ **PROVEN** — dims 13x13, alturas plausíveis; mark byte `+5`: `0x20`=range de movimento, `0x40/0xC0`=cursor/target → membership de range/AoE é leitura direta (level bit/ponte não testado) |
| Status arrays | ✅ **PROVEN** — Blind = `eff[1]\|=0x20 [+Darkness]`+master (bit clássico exato); Charging `0x08` no charge do Fire; imunidades de equipamento visíveis em `+0x5C` (Ramza Darkness+Sleep, Ninja DM+DA) |
| Immunity write-control | ✅ **PROVEN (LT2)** — `imm[1]\|=0x20` na Agrias → forecast 0% → miss ("parried" anim) |
| Pending record `+0x1A1/+0x1A2` | ✅ **PROVEN** — Fire=`0x10`, Blind=`0xEA`, skill inimiga=`0x118` (espaço de id clássico confirmado); timer/epicentro coerentes |
| JP arrays `+0xF0/+0x11E` | ✅ **PROVEN** — index `jobId-0x4A` exato (Agrias +48 @6=0x50-0x4A; Beowulf +53 @8=0x52-0x4A; spillover +9/10); JP1=atual, JP2=total; `+0x28` = candidato EXP |
| `word[0x14186AFF0]` | ❌ **REFUTADO como action id** (ficou 0 nos previews) → **reinterpretado: reaction-id em avaliação** (445=0x1BD no Mana Shield, 451 no parry) — fecha parcialmente o gap "id da reação" |
| `g_7B07AC` poke | ❌ **REFUTADO como lever** — engine reescreve no compute (0→3854 observado); é observável (0 ↔ 0% imune, 3854 ↔ caso 100%), controle = hook `0x30662C` (LT3) |
| AI same-calc | ✅ **Strong (live)** — staged fields pulsaram nos units do player durante turnos inimigos com `casterIdx`=inimigo |
| AoE batch | staged escrito nos 3 alvos no mesmo tick, HP aplicado no mesmo tick; Mana Shield capturado como `stagedMpDebit 157` + `resFlag 32` (redireção HP→MP staged) |
| hitPct `+0x1EA` | ✅ valores live coerentes (100 alvo, 71 self-AoE da Agrias) |

**Ainda aberto → LT3 (rebuild, hooks log-only):** id da ação em tempo de PREVIEW (hook
`computeActionResult 0x309A44`, `rcx+2`); magic always-hit (`0x304E2B`); gates de reaction;
hook de status `0x30662C`; discriminador AI (team do caster no hook `0x309A44`).
