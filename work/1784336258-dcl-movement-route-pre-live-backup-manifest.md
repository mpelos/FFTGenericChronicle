# DCL movement-route pre-live backup manifest

The game and Reloaded-II were stopped before deployment.

| Artifact | Backup | SHA-256 |
| --- | --- | --- |
| Installed codemod DLL | `1784336258-movement-route-pre-dll.dll` | `52C255F9F70EA956D9B45B68A9969FAF033C220E4028E0E19528E1537848FA04` |
| Installed codemod PDB | `1784336258-movement-route-pre-pdb.pdb` | `66C30C07E096DCDF34BDD45E5D8DF748CBCE83A59DDD9510DECF5C356CCC76CD` |
| Installed runtime settings | `1784336258-movement-route-pre-settings.json` | `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624` |
| Previous runtime log | `1784336258-movement-route-pre-log.txt` | `E39010BF6AB0004AE7B4C23F0ECA492B9E068E899BA888E091DA5B476A9CCA40` |

Deployed observe-only hashes:

- DLL: `E64A74652D33FBB187E886E2440686C2917B4EE8EE91CAC33384244403B4CFB0`
- PDB: `027CAAE70E9097E2F170BD66D7D86878042AD85A2C21B058556C3D5C0E6790ED`
- Settings: `4EC999555AAA9CEED2029D7E5807C2FEFC0AC65D443FA49308898F97CF90A67C`

Restore all three installed artifacts from the named backups after collecting the live log.
