#!/usr/bin/env python3
"""Generates an SPDX 2.3 SBOM for a NuGet package without external dependencies."""

import hashlib
import json
import os
import sys
import zipfile
from datetime import datetime, timezone


def sha1_bytes(data):
    return hashlib.sha1(data).hexdigest()


def sha256_file(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main():
    if len(sys.argv) != 3:
        print("usage: generate-sbom.py <package.nupkg> <output-directory>", file=sys.stderr)
        return 1

    package_path = os.path.abspath(sys.argv[1])
    output_directory = os.path.abspath(sys.argv[2])
    os.makedirs(output_directory, exist_ok=True)

    file_name = os.path.basename(package_path)
    parts = file_name.split(".")
    if len(parts) < 3 or not file_name.endswith(".nupkg"):
        print("unexpected package name: " + file_name, file=sys.stderr)
        return 1

    package_name = parts[0]
    version = ".".join(parts[1:-1])
    package_id = "SPDXRef-Package-" + package_name

    with zipfile.ZipFile(package_path) as archive:
        entries = sorted(
            (entry for entry in archive.infolist() if not entry.is_dir()),
            key=lambda entry: entry.filename,
        )
        file_hashes = [(entry.filename, sha1_bytes(archive.read(entry.filename))) for entry in entries]

    verification = sha1_bytes("".join(digest for _, digest in sorted(file_hashes)).encode("utf-8"))
    created = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    files = []
    relationships = [
        {
            "spdxElementId": "SPDXRef-DOCUMENT",
            "relationshipType": "DESCRIBES",
            "relatedSpdxElement": package_id,
        }
    ]

    for index, (entry_name, digest) in enumerate(file_hashes, start=1):
        spdx_id = "SPDXRef-File-%d" % index
        files.append(
            {
                "SPDXID": spdx_id,
                "fileName": "./" + entry_name,
                "checksums": [{"algorithm": "SHA1", "checksumValue": digest}],
                "licenseConcluded": "NOASSERTION",
                "licenseInfoInFiles": ["NOASSERTION"],
                "copyrightText": "NOASSERTION",
            }
        )
        relationships.append(
            {
                "spdxElementId": package_id,
                "relationshipType": "CONTAINS",
                "relatedSpdxElement": spdx_id,
            }
        )

    document = {
        "spdxVersion": "SPDX-2.3",
        "dataLicense": "CC0-1.0",
        "SPDXID": "SPDXRef-DOCUMENT",
        "name": "%s-%s" % (package_name, version),
        "documentNamespace": "https://github.com/karlmoriaty/RunesHela/spdx/%s-%s" % (package_name, version),
        "creationInfo": {
            "created": created,
            "creators": ["Tool: runeshela-sbom-generator"],
        },
        "packages": [
            {
                "SPDXID": package_id,
                "name": package_name,
                "versionInfo": version,
                "downloadLocation": "NOASSERTION",
                "filesAnalyzed": True,
                "packageVerificationCode": {
                    "packageVerificationCodeValue": verification,
                },
                "checksums": [{"algorithm": "SHA256", "checksumValue": sha256_file(package_path)}],
                "licenseConcluded": "Apache-2.0",
                "licenseDeclared": "Apache-2.0",
                "copyrightText": "NOASSERTION",
                "supplier": "Organization: The RunesHela Authors",
                "externalRefs": [
                    {
                        "referenceCategory": "PACKAGE-MANAGER",
                        "referenceType": "purl",
                        "referenceLocator": "pkg:nuget/%s@%s" % (package_name, version),
                    }
                ],
            }
        ],
        "files": files,
        "relationships": relationships,
    }

    output_path = os.path.join(output_directory, "%s-%s.spdx.json" % (package_name, version))
    with open(output_path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(document, handle, indent=2)
        handle.write("\n")

    print(output_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
