# Authored content

Version-controlled game data belongs here. Content must use stable identifiers, carry an explicit schema version once a format is introduced, and satisfy the asset-provenance policy before it can enter a distributable build. Reference-only research material does not belong in this directory.

`destinations.v1.json` is the schema-versioned Destination Registry used by the prototype. Destination and vector identifiers use approved stable `snake_case` names and are validated before simulation state can change.

Run `dotnet run --project tools/ContentValidator/ContentValidator.csproj -- --root .` from the repository root to validate every catalog and its cross-catalog references before packaging.

