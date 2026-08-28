# Developer tools

Repository-local validation, content processing, and packaging utilities belong here. Tools are not included in game exports and must avoid writing generated files outside ignored build or cache directories.

`ContentValidator` loads all shipped catalogs and checks stable identifiers and cross-catalog references. `AssetLedgerValidator` checks provenance and distribution permission. `Verify.ps1` runs both gates before a Windows export, while `Build-Release.ps1` adds the installer and portable archive.

