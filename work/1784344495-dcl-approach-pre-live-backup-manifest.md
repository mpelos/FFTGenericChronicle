# DCL Approach pre-live backup manifest

The game and Reloaded-II are stopped. These copies are independent of the deploy helper's rolling
settings backup and the autosave helper's source-directory backup.

| Artifact | Backup | SHA-256 |
| --- | --- | --- |
| Installed codemod DLL | `1784344495-approach-pre-dll.dll` | `52C255F9F70EA956D9B45B68A9969FAF033C220E4028E0E19528E1537848FA04` |
| Installed codemod PDB | `1784344495-approach-pre-pdb.pdb` | `66C30C07E096DCDF34BDD45E5D8DF748CBCE83A59DDD9510DECF5C356CCC76CD` |
| Installed runtime settings | `1784344495-approach-pre-settings.json` | `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624` |
| Reloaded FFT AppConfig | `1784344495-approach-pre-appconfig.json` | `05D0D96D5CA9C1A8090264CC71600740697B3D5433F91AD9F8834B11F3FDCE05` |
| Previous runtime log | `1784344495-approach-pre-log.txt` | `067E5448C799823CEFC37AF385711774AB9D247415558B62750A0B93B64A0967` |
| Enhanced autosave | `1784344495-approach-pre-autosave.png` | `536E4EFE1FF3F05E5DDE6E7D02547B08BD8E4F5D859987E4BA300C189099B7E2` |

The AppConfig enables only `fftivc.utility.modloader` and
`fftivc.generic.chronicle.codemod`. Restore all six external artifacts after the live capture.
