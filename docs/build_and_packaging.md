# Build and package verification

The build executable targets .NET 10. Visual Studio MSBuild compiles the shipped library against the real net20 reference assemblies and the tests against net481. See the README for Windows prerequisites.

## Commands

Run managed and private SQL CE tests without packaging:

```powershell
./build/build.ps1 --target=Test --configuration=Debug
./build/build.ps1 --target=Test --configuration=Release
```

Run the complete Release pipeline (also the default Release target):

```powershell
./build/build.ps1 --target="Verify Consumer" --configuration=Release
./build/build.ps1 --configuration=Release
```

`Package` creates artifacts; `Verify Package` additionally validates them. Packaging rejects Debug configuration. `Verify Consumer` adds package installation and x86/x64 CLR 2.0 execution. The build workflow uses `Verify Consumer` and uploads validated artifacts. The separate [release workflow](releasing.md) publishes only after an intentional GitHub Release.

| Stage | Checks and behavior |
| --- | --- |
| Clean/restore/compile | Clears the selected configuration's bin and obj directories and package artifact directory, restores packages.config dependencies, validates versions/tags, and compiles the solution. Debug outputs remain separate. |
| Test | Runs all library tests through VSTest, including SQL CE integration. Failure or zero discovered tests fails the pipeline. |
| Image generation | Renders the source SVG into logo/deployment PNGs and single-image PNG-backed ICOs. Truncating writes replace previous files. SVG, picture-owned resources, color spaces, bitmaps, canvases, encoded images, and streams are disposed on success/failure. |
| Image verification | Decodes PNGs, checks requested dimensions and visible pixels, and validates ICO header/directory/payload bounds. A real 256-to-16 pixel overwrite must shrink and contain no stale tail. Malformed/wrong-size fixtures must fail; files are reopened exclusively to detect retained handles. |
| NuGet packing | Uses the NuGet CLI already required for restore. Package analysis is enabled; warnings are visible rather than globally suppressed. The toolkit ships only its selected net20 DLL/XML, README, and icon. Source files, dependencies' DLLs, native SQL CE assets, test assemblies, and intermediate/Debug outputs are excluded. |
| Artifact verification | Checks the exact allowed payload, duplicate paths, byte identity with Release/source files, package identity/version/license/repository commit, and one net20 dependency group. Runtime dependencies must exactly match packages.config; development/build/test dependencies are excluded. |
| Binary/symbol verification | Checks the packaged DLL's CLR 2.0 metadata, assembly/file/informational versions, and optimized Release flags. Portable PDB bytes must match build output, contain source documents, and match the DLL's debug identity and SHA-256 checksum (with the PDB identifier zeroed as specified by the portable-symbol format). |
| Failure regressions | Seven generated ZIP fixtures must fail: missing XML, unexpected test DLL, substituted DLL, duplicate README, missing PDB, substituted PDB, and incorrect dependency version. Version/tag and image rejection cases also run in the build executable. These are build checks, separate from the net481 library test count. |

Artifacts are written to `artifacts/packages/Release/`:

| File | Purpose |
| --- | --- |
| `AaronSalisbury.DotNetFrameworkToolkit.<version>.nupkg` | Consumer package. |
| `AaronSalisbury.DotNetFrameworkToolkit.<version>.snupkg` | Portable symbols in the matching `lib/net20` path. Source retrieval/Source Link is not configured by this phase. |
| `validation.json` | Versions, source commit, configuration/runtime target, artifact SHA-256 hashes, and successful package/image regression checks. |

CI also uploads generated images, the library output, and TRX test results. The full pipeline also uploads consumer runtime logs and a validation report. See [consumer verification](consumer_usage.md) for installation checks, CLR 2.0 execution and the declared Windows environment.

## Version and tag policy

`version.props` is the single version input. MSBuild generates version attributes in the selected obj directory; the nuspec receives the package version and current Git commit from Cake.

| Value | Rule |
| --- | --- |
| `PackageVersion` | `major.minor.patch`, optionally followed by a SemVer prerelease suffix. Numeric components/identifiers cannot have leading zeroes. Build metadata is deliberately unsupported to avoid package filename/version normalization ambiguity. |
| `AssemblyVersion` | Four-part assembly binding identity; retains `1.0.0.0` for the current compatibility line. Change deliberately when changing binding compatibility, rather than on every package release. |
| `AssemblyFileVersion` | The package's numeric `major.minor.patch` plus `.0`, including for prereleases. |
| Informational version | Full `PackageVersion`, including its prerelease suffix. |
| Dependency versions | Exact ranges matching restored runtime package versions. Upgrade deliberately and validate before distribution. |
| Release tag | Exactly `v<PackageVersion>`. A GitHub tag checkout validates its tag automatically; a local check can supply `--release-tag=v0.3.0`. A mismatch fails before compilation/packaging. |

Update the package and file versions together in `version.props`. Assembly/file components must fit the compiler's 0–65534 range. Updating version metadata does not publish anything or automatically create a tag. See [releasing](releasing.md) for trusted publishing setup and the release procedure.

NuGet's official [pack command](https://learn.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-pack) and [portable symbol package requirements](https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg) describe the CLI analysis and symbol format used here.
