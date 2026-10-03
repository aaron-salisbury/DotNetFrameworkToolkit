# Publishing a release

The normal build workflow cannot publish. `release.yml` runs only when a GitHub Release is published, including a prerelease. It checks out that release's tag, runs the full Cake Release pipeline, and hands the validated package and portable symbols to a separate publishing job. That job attaches the exact files to the GitHub Release and publishes those bytes to NuGet.org. No package is rebuilt in the publishing job.

## One-time account setup

| Location | Setting |
| --- | --- |
| GitHub repository settings → Environments | Create an environment named `nuget`. Optional required reviewers can add a deliberate approval before publishing. Ensure any deployment tag restrictions allow the intended `v*` tags. |
| GitHub `nuget` environment → Variables | Add `NUGET_USER` with the NuGet.org account's profile username, not an email address. The account must own `AaronSalisbury.DotNetFrameworkToolkit`. |
| NuGet.org account → Trusted publishing | Create a GitHub trust policy for repository owner `aaron-salisbury`, repository `DotNetFrameworkToolkit`, workflow filename `release.yml`, environment `nuget`. Scope publication to `AaronSalisbury.DotNetFrameworkToolkit`. |

[NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) exchanges a GitHub OIDC token for a temporary publishing key. No long-lived API key is stored in GitHub. Only the publishing job has `id-token: write`; its `contents: write` permission attaches release assets.

## Each release

| Step | Action |
| --- | --- |
| 1 | Update `PackageVersion` and `AssemblyFileVersion` together in `version.props`, merge the changes into `master`, and wait for successful CI. `0.3.0` uses file version `0.3.0.0`. Retain the current `AssemblyVersion` binding identity unless deliberately changing it. |
| 2 | Create a GitHub Release with a new tag exactly `v<PackageVersion>`, targeting the reviewed `master` commit. Supply nonempty release notes describing the changes, including breaking API changes. The prepared stable release tag is `v0.3.0`. |
| 3 | Publish the GitHub Release. Draft releases and a tag push alone do not trigger publication. The release workflow rebuilds and verifies the tagged source, runs library tests and x86/x64 CLR 2.0 consumers, and rejects tag/version mismatches. |
| 4 | Review the attached `.nupkg`, `.snupkg`, `validation.json`, `consumer-validation.json`, and `release-notes.md`, and the successful publishing job. The NuGet package metadata links to these GitHub release notes. |
| 5 | After NuGet finishes validation/indexing, install the published version from NuGet.org into a clean consumer and confirm package/version/dependency resolution. Check the NuGet package page for symbol validation errors. Only then is roadmap phase 7 fully validated. |

For a trial prerelease, commit `PackageVersion` as `0.3.0-rc.1`, keep `AssemblyFileVersion` as `0.3.0.0`, and publish tag `v0.3.0-rc.1`. The GitHub prerelease checkbox alone does not make a NuGet version a prerelease; the package version needs the suffix. Advance to stable `0.3.0` in a subsequent version commit and tag.

## Failures and reruns

| Situation | Recovery |
| --- | --- |
| Build, test, package, or consumer verification fails | Nothing is published to NuGet. Correct the cause and use a new version/tag if the tagged source must change. |
| Trust policy or account configuration is wrong | Correct the settings, then rerun only the failed publishing job. It downloads the original verified artifact from the same workflow run. |
| Package push succeeds but symbol push fails | Rerun the failed publishing job. Package and symbol uploads run separately with `-SkipDuplicate`, allowing the missing upload to proceed. Existing versions are never overwritten. |
| Release assets already exist | Identical bytes are reused; differing bytes fail before authentication/publication. Do not rerun the successful build unnecessarily: rebuilding can change ZIP timestamps or build identifiers. Reuse the original run's artifact. |
| Original workflow artifact has expired | Do not delete published assets or move the tag to bypass a mismatch. Prepare a new version and release if the original verified handoff can no longer be reused. |

`-SkipDuplicate` treats NuGet's existing-version conflict as a no-op; it does not prove that an independently uploaded package has identical bytes. Avoid publishing the same version manually. GitHub asset hash checks protect this workflow's reruns, and the validation report records the package hashes and tagged source commit.

The workflow includes automated rejection checks for altered packages, a wrong tag/source commit, missing or failed consumer checks, unverified build results, extra artifacts and empty notes. Actual NuGet authentication/publication remains an owner-configured release operation.
