"""Validate the exact artifact handoff; optionally attach it without replacing assets."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile

PACKAGE_ID = "AaronSalisbury.DotNetFrameworkToolkit"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def hash_file(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify(directory, tag, commit):
    report = json.loads((directory / "validation.json").read_text(encoding="utf-8-sig"))
    consumer = json.loads((directory / "consumer-validation.json").read_text(encoding="utf-8-sig"))
    version = report["PackageVersion"]
    require(tag == "v" + version, "Release tag and package version differ.")
    require(report["SourceCommit"] == commit, "Artifact source commit differs from the checked-out tag.")
    require(report["Configuration"] == "Release" and report["Framework"] == "net20"
            and report["RuntimeMetadata"] == "v2.0.50727", "Unexpected build configuration/runtime.")
    require(report["CorruptPackageFixturesRejected"] == 7
            and report["ImagesAndOverwriteVerified"] is True, "Package/image checks were not successful.")
    require(consumer["PackageVersion"] == version, "Consumer tested a different version.")
    consumers = consumer["Consumers"]
    require(len(consumers) == 2 and {item["Architecture"] for item in consumers} == {"x86", "x64"}
            and all(item["Passed"] is True and item["Runtime"] == "CLR 2.0" for item in consumers),
            "Both CLR 2.0 consumer checks must pass.")
    expected = {"validation.json", "consumer-validation.json", "release-notes.md"}
    for extension, field in [("nupkg", "PackageSha256"), ("snupkg", "SymbolsSha256")]:
        name = PACKAGE_ID + "." + version + "." + extension
        expected.add(name)
        package = directory / name
        digest = hash_file(package)
        require(digest.lower() == report[field].lower(), "Artifact hash mismatch: " + name)
        if extension == "nupkg":
            require(digest.lower() == consumer["PackageSha256"].lower(), "Consumer tested different package bytes.")
        with zipfile.ZipFile(package) as archive:
            manifests = [name for name in archive.namelist() if name.endswith(".nuspec")]
            require(len(manifests) == 1, "Expected exactly one package manifest.")
            root = ET.fromstring(archive.read(manifests[0]))
            metadata = root.find("{*}metadata")
            require(metadata.find("{*}id").text == PACKAGE_ID
                    and metadata.find("{*}version").text == version, "Manifest identity mismatch.")
            if extension == "nupkg":
                require(metadata.find("{*}repository").get("commit") == commit, "Manifest commit mismatch.")
    require({path.name for path in directory.iterdir()} == expected, "Unexpected/missing release artifact files.")
    require((directory / "release-notes.md").read_text(encoding="utf-8").strip(), "Release notes are required.")
    return sorted(directory.iterdir())


def attach(files, repository, tag):
    release = json.loads(subprocess.check_output(
        ["gh", "api", "repos/" + repository + "/releases/tags/" + tag], text=True))
    assets = {asset["name"]: asset for asset in release["assets"]}
    # Validate every existing asset before uploading anything; never clobber.
    with tempfile.TemporaryDirectory() as temporary:
        for path in files:
            if path.name in assets:
                original = Path(temporary) / path.name
                subprocess.run(["gh", "release", "download", tag, "--repo", repository,
                                "--pattern", path.name, "--dir", temporary], check=True)
                require(hash_file(original) == hash_file(path), "Existing release asset differs: " + path.name)
    missing = [str(path) for path in files if path.name not in assets]
    if missing:
        subprocess.run(["gh", "release", "upload", tag, "--repo", repository, *missing], check=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("tag")
    parser.add_argument("commit")
    parser.add_argument("--attach", metavar="OWNER/REPO")
    args = parser.parse_args()
    files = verify(args.directory, args.tag, args.commit)
    if args.attach:
        attach(files, args.attach, args.tag)
    print("Release artifacts match the tag, source commit, package hashes and CLR 2.0 consumer checks.")
