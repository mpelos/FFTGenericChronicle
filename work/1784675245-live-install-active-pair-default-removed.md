# DCL live install active pair default removed

The DCL live-install preflight and transactional installer now fail closed when no `--pair` is
provided. The historical clean-v1 runtime/data pair remains named as
`HISTORICAL_CLEAN_V1_PAIR` for explicit archival references, but it is no longer the default active
bundle for either command.

Operational consequence: an integrated live regression cannot accidentally validate or install the
retired clean-v1 artifacts by omission. A current integrated runtime/data pair must be built,
hash-bound, and passed explicitly before any active live install or preflight.

Verified gates:

- `python tools\test_validate_dcl_live_install.py`
- `python tools\test_install_dcl_live_bundle.py`
- `python tools\validate_dcl_live_install.py` fails closed without `--pair`.
- `python tools\install_dcl_live_bundle.py` fails closed without `--pair`.
