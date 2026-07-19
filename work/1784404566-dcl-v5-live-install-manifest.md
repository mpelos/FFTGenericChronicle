# DCL v5 exact live installation

The transactional installer selects
`work/1784403685-dcl-unified-sentinel-v5-runtime-data-pair.json` and installs the isolated three-mod
profile: utility mod loader, Generic Chronicle data mod, and Generic Chronicle code mod.

Bound artifacts:

- runtime settings SHA-256 `1AC63BEB6751E31F781DA28CBDB01EDE68CE6C94802F14178D6C3436BC09B66A`;
- action-data NXD SHA-256 `44B1E65F33FA5AF1C0A075645B898C5BDCC543F5D2DDF832017571B5C12741A9`;
- item catalog SHA-256 `889F7C6FFFD451859E895DCFCD193E28A8E9950DB7979320E3F547EDE7DCDEE3`;
- ability catalog SHA-256 `53F73C4D9A2357A09E855FE902D4B3337D5DDF04FBA7952EEE62E5D333E0992D`;
- code-mod DLL SHA-256 `B5F6C6F05141D156A4C2A9387416EDA4016FBA070490473BCC2391CB24353568`;
- canonical job baseline SHA-256 `90737CA80B31724DEDD01C51930305647E7DDF1A3D43D3F01F8B58C5CB0AB68D`;
- generated affinity fragment SHA-256 `017081BFAD7E13EB133577D7B47BFCEEB1A7C66010C952AE103A9ABC335C5E47`.

Every destination receives a `.bak-dcl-bundle-1784404566` backup. Atomic installation and the exact
post-install live preflight pass. The prior v4 log is archived as
`battleprobe_log.v4-fire-affinity-regression-1784404566.txt` before the v5 launch.
