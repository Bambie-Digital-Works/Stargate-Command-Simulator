# Visual and audio asset policy

Every retained visual, audio, font, video, and model file must have traceable provenance in `assets/asset-ledger.json`. Repository presence does not make an asset safe to distribute. The ledger and automated validator are release inputs, not optional documentation.

## Accepted origins

Production assets may be:

- original work created for the project by `bambiejomurphy-ux`;
- commissioned work covered by a written agreement granting the required rights;
- third-party work under a verified licence compatible with proprietary commercial distribution;
- verified public-domain material after review for trademarks, privacy, publicity, and jurisdictional limits; or
- generated material only when the provider terms, source inputs, and retained evidence permit the intended commercial use.

An asset with unclear authorship, missing terms, an incompatible licence, unlicensed franchise content, or unverifiable generation inputs is `unverified` or `rejected` and cannot enter a distributable path.

## Repository boundaries

- `assets/reference/` is research-only, contains `.gdignore`, and must never be exported.
- `assets/production/` is the only home for distributable visual/audio assets.
- `content/` contains authored data, not untracked binary art or audio.
- Licence texts, contracts, receipts, and proof snapshots belong under `assets/proof/` when they may be retained in the repository. Otherwise the ledger records the controlled external location.

Reference files may use terms or licences unsuitable for distribution only when their ledger status is `reference-only` and `distributionAllowed` is `false`.

## Required ledger fields

Each entry records:

- stable asset ID and repository-relative path;
- category, intended scope, and origin;
- creator and original source URL;
- licence expression and required attribution;
- acquisition date and modifications;
- proof-of-licence location;
- review status, reviewer, and review date; and
- explicit distribution permission.

Dates use `YYYY-MM-DD`. Paths use forward slashes and exact repository casing. An empty value is not evidence.

## Review states

| State | Meaning | Distributable |
|---|---|---|
| `unverified` | Evidence or review is incomplete | No |
| `approved` | Rights and intended use were reviewed | Only when `scope` is `production` and `distributionAllowed` is `true` |
| `reference-only` | Retained solely for internal visual/audio study | No |
| `rejected` | Rights, provenance, or project fit failed review | No; remove from retained asset paths |

Approval is specific to the recorded file and modifications. Replacing or materially changing a file requires a new review. Attribution text must ship with the product whenever the licence requires it.

## Automated gate

Run:

```powershell
dotnet run --project tools/AssetLedgerValidator/AssetLedgerValidator.csproj -- --root .
```

The validator rejects malformed ledgers, incomplete entries, duplicate IDs or paths, missing files or proof, unrecorded retained assets, invalid review combinations, and production assets without explicit approval. CI must run this check before creating a distributable artifact.

