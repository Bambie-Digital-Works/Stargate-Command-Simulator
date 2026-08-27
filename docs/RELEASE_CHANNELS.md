# Windows release channels

Every release is identified by an immutable SemVer tag, the product version,
the four-part Windows package version, the CI build ID, the commit, content and
save schemas, the channel, and SHA-256 hashes. The installer and portable
archive are never rebuilt as part of channel promotion.

## Channels

- `preview` is the public beta channel. It may contain prereleases and is
  selected by the in-game preview update check.
- `stable` is the promoted release for general users. It is created from the
  already-tested preview binaries and receives a channel-specific manifest.
- `legacy` retains the previous stable artifact for rollback and save recovery.
  It is never overwritten.

The first public beta is
[`v0.8.0-beta.1`](https://github.com/Bambie-Digital-Works/Stargate-Command-Simulator/releases/tag/v0.8.0-beta.1).
It is a preview release and is intentionally unsigned; the release page
contains the installer, portable archive, manifests, notes, notices, and
`SHA256SUMS.txt`.

## Promotion and rollback

After a clean-machine compatibility record and signing approval exist:

```powershell
./tools/Promote-Release.ps1 `
  -SourceReleaseDirectory 'dist/v0.8.0-beta.1' `
  -DestinationTag 'v0.8.0' `
  -Channel stable
```

The script validates the source installer hash, requires the destination tag to
already exist, copies the tested files, writes only a channel-specific update
manifest, regenerates release-level hashes, and refuses to replace an existing
release. This preserves the exact tested executable and installer.

Before promoting a later stable release, retain the current stable tag and
release as `legacy` using the same script. Rollback points the update feed at
that immutable legacy release; it does not mutate or rebuild either artifact.

Promotion is controlled by a maintainer and is not performed automatically by
the game. Signing credentials, if used, belong only in protected CI or a
managed signing service and never in the repository.
