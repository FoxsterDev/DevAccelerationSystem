#!/usr/bin/env python3
"""License-free consistency checks for the Dev Acceleration System monorepo."""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ROOT = ROOT / "DevAccelerationSystem" / "Assets"
HARNESS_ROUTER_REFRESH = ROOT / "scripts" / "refresh_harness_routing.py"
HARNESS_ADAPTER = ROOT / "Docs" / "ai" / "unity-unified-harness-adapter.md"
CANONICAL_ROUTERS = (
    ROOT / "AGENTS.md",
    ROOT / "DevAccelerationSystem" / "AGENTS.md",
    ROOT / "DevAccelerationSystem.DemoProject" / "AGENTS.md",
)
PACKAGES = {
    "com.foxsterdev.devaccelerationsystem": PACKAGE_ROOT / "DevAccelerationSystem",
    "com.foxsterdev.thebestlogger": PACKAGE_ROOT / "TheBestLogger",
    "com.foxsterdev.loqui": PACKAGE_ROOT / "Loqui",
}
UNITY_PROJECTS = {
    "DAS-SRC": (
        ROOT / "DevAccelerationSystem",
        "com.fd.das.src",
    ),
    "DAS-DEMO": (
        ROOT / "DevAccelerationSystem.DemoProject",
        "com.fd.das.demo",
    ),
}
SEMVER = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$")
PACKAGE_NAME = re.compile(r"^[a-z0-9][a-z0-9.-]*\.[a-z0-9][a-z0-9.-]*\.[a-z0-9][a-z0-9.-]*$")
ABSOLUTE_PATH = re.compile(r"(?<![A-Za-z0-9_])(?:/Users/|[A-Za-z]:\\\\)")
REQUIRED_FIELDS = ("name", "version", "displayName", "description", "unity", "keywords", "author", "repository", "documentationUrl", "changelogUrl", "licensesUrl", "dependencies", "testables")


class Reporter:
    def __init__(self) -> None:
        self.errors: list[str] = []
        self.warnings: list[str] = []

    def error(self, message: str) -> None:
        self.errors.append(message)

    def warning(self, message: str) -> None:
        self.warnings.append(message)


def load_json(path: Path, report: Reporter) -> dict | None:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        report.error(f"{path.relative_to(ROOT)}: invalid JSON ({exc})")
        return None


def validate_packages(report: Reporter) -> dict[str, dict]:
    manifests: dict[str, dict] = {}
    for package_id, package_dir in PACKAGES.items():
        manifest_path = package_dir / "package.json"
        manifest = load_json(manifest_path, report)
        if manifest is None:
            continue
        manifests[package_id] = manifest
        for field in REQUIRED_FIELDS:
            if field not in manifest or manifest[field] in (None, "", []):
                report.error(f"{manifest_path.relative_to(ROOT)}: missing required '{field}'")
        if manifest.get("name") != package_id:
            report.error(f"{manifest_path.relative_to(ROOT)}: package id must be {package_id}")
        if not PACKAGE_NAME.fullmatch(str(manifest.get("name", ""))):
            report.error(f"{manifest_path.relative_to(ROOT)}: package id is not reverse-domain notation")
        if not SEMVER.fullmatch(str(manifest.get("version", ""))):
            report.error(f"{manifest_path.relative_to(ROOT)}: version is not SemVer")
        if not re.fullmatch(r"\d{4}\.\d+", str(manifest.get("unity", ""))):
            report.error(f"{manifest_path.relative_to(ROOT)}: unity must be major.minor")
        if not isinstance(manifest.get("keywords"), list) or not manifest.get("keywords"):
            report.error(f"{manifest_path.relative_to(ROOT)}: keywords must be a non-empty array")
        if not isinstance(manifest.get("author"), dict) or not manifest["author"].get("name"):
            report.error(f"{manifest_path.relative_to(ROOT)}: author.name is required")
        if not isinstance(manifest.get("repository"), dict) or not manifest["repository"].get("url", "").startswith("https://"):
            report.error(f"{manifest_path.relative_to(ROOT)}: repository.url must be HTTPS")
        for field in ("documentationUrl", "changelogUrl", "licensesUrl"):
            if not str(manifest.get(field, "")).startswith("https://github.com/FoxsterDev/DevAccelerationSystem/"):
                report.error(f"{manifest_path.relative_to(ROOT)}: {field} must point to the public repository")
        for required_file in ("README.md", "CHANGELOG.md", "LICENSE.md"):
            if not (package_dir / required_file).is_file():
                report.error(f"{package_dir.relative_to(ROOT)}: missing {required_file}")
    return manifests


def validate_asmdefs(report: Reporter, manifests: dict[str, dict]) -> None:
    asmdefs: dict[str, tuple[Path, dict]] = {}
    for package_dir in PACKAGES.values():
        for asmdef_path in sorted(package_dir.rglob("*.asmdef")):
            asmdef = load_json(asmdef_path, report)
            if asmdef is None:
                continue
            name = asmdef.get("name")
            if not name:
                report.error(f"{asmdef_path.relative_to(ROOT)}: asmdef name is required")
                continue
            if name in asmdefs:
                report.error(f"{asmdef_path.relative_to(ROOT)}: duplicate asmdef name '{name}'")
            asmdefs[name] = (asmdef_path, asmdef)
            parts = asmdef_path.parts
            platforms = asmdef.get("includePlatforms") or []
            if "Editor" in parts and "Editor" not in platforms:
                report.error(f"{asmdef_path.relative_to(ROOT)}: Editor assembly must include only the Editor platform")
            if "Tests" in parts and not any(ref in (asmdef.get("references") or []) for ref in ("UnityEngine.TestRunner", "UnityEditor.TestRunner")):
                report.error(f"{asmdef_path.relative_to(ROOT)}: test assembly must reference Unity Test Runner")
    for package_id, manifest in manifests.items():
        for testable in manifest.get("testables", []):
            if testable not in asmdefs:
                report.error(f"{package_id}: declared testable '{testable}' has no asmdef")


def validate_source_boundaries(report: Reporter) -> None:
    for package_dir in PACKAGES.values():
        for source_path in package_dir.rglob("*.cs"):
            relative = source_path.relative_to(package_dir)
            if "Tests" in relative.parts or "Editor" in relative.parts:
                continue
            text = source_path.read_text(encoding="utf-8")
            editor_using = re.search(r"^\s*using\s+UnityEditor(?:\.|\s*;)", text, re.MULTILINE)
            if editor_using and "#if UNITY_EDITOR" not in text[:editor_using.start()]:
                report.error(f"{source_path.relative_to(ROOT)}: runtime source references UnityEditor")
            if ABSOLUTE_PATH.search(text):
                report.error(f"{source_path.relative_to(ROOT)}: contains a machine-specific absolute path")


def validate_docs(report: Reporter, manifests: dict[str, dict]) -> None:
    root_readme = (ROOT / "README.md").read_text(encoding="utf-8")
    for package_id, manifest in manifests.items():
        if package_id not in root_readme or manifest["version"] not in root_readme:
            report.error(f"README.md: does not document {package_id}@{manifest['version']}")
    stale_versions = ("2.2.15", "3.0.1")
    for version in stale_versions:
        if version in root_readme:
            report.error(f"README.md: stale package version {version}")
    docs_to_check = [ROOT / "README.md", *(directory / "README.md" for directory in PACKAGES.values())]
    link_pattern = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
    for doc in docs_to_check:
        for link in link_pattern.findall(doc.read_text(encoding="utf-8")):
            if "://" in link or link.startswith("#") or link.startswith("mailto:"):
                continue
            target = (doc.parent / link.split("#", 1)[0]).resolve()
            if not target.exists():
                report.error(f"{doc.relative_to(ROOT)}: broken internal link '{link}'")


def validate_tracked_generated_files(report: Reporter) -> None:
    result = subprocess.run(["git", "ls-files"], cwd=ROOT, text=True, capture_output=True, check=False)
    if result.returncode:
        report.error("unable to inspect tracked files with git")
        return
    generated = re.compile(r"(^|/)(Library|Temp|Logs|obj|Build|Builds|UserSettings|\.DS_Store)(/|$)|\.(csproj|sln|user)$", re.IGNORECASE)
    for path in result.stdout.splitlines():
        if generated.search(path):
            report.error(f"tracked generated file: {path}")


def validate_yaml(report: Reporter) -> None:
    workflow_paths = sorted((ROOT / ".github" / "workflows").glob("*.y*ml"))
    if not workflow_paths:
        report.error("missing GitHub Actions workflow YAML")
        return
    ruby_program = "require 'yaml'; ARGV.each { |path| YAML.load_file(path) }"
    try:
        result = subprocess.run(["ruby", "-e", ruby_program, *(str(path) for path in workflow_paths)],
                                cwd=ROOT, text=True, capture_output=True, check=False)
    except FileNotFoundError:
        report.error("Ruby with its standard YAML parser is required to validate workflow YAML")
        return
    if result.returncode:
        report.error(f"invalid workflow YAML: {result.stderr.strip() or result.stdout.strip()}")


def validate_harness_routing(report: Reporter) -> None:
    result = subprocess.run(
        [sys.executable, str(HARNESS_ROUTER_REFRESH), "--check"],
        cwd=ROOT,
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        detail = result.stdout.strip() or result.stderr.strip()
        report.error(f"Unity Harness routers are stale: {detail}")

    required_adapter_sections = (
        "## Journey Zero",
        "## Four Lanes",
        "### `docs`",
        "### `ordinary`",
        "### `high-risk`",
        "### `release`",
        "## Authoring And Consumer Boundary",
        "## Proof Routing And Ceilings",
        "## Harness-Owned Gate Scope",
        "## Version Decision",
    )
    if not HARNESS_ADAPTER.is_file():
        report.error(f"missing Unity Harness adapter: {HARNESS_ADAPTER.relative_to(ROOT)}")
    else:
        adapter_text = HARNESS_ADAPTER.read_text(encoding="utf-8")
        for section in required_adapter_sections:
            if section not in adapter_text:
                report.error(
                    f"{HARNESS_ADAPTER.relative_to(ROOT)}: missing required section '{section}'"
                )

    privacy_paths = (HARNESS_ROUTER_REFRESH, HARNESS_ADAPTER, *CANONICAL_ROUTERS)
    for path in privacy_paths:
        if not path.is_file():
            report.error(f"missing Harness-owned file: {path.relative_to(ROOT)}")
            continue
        if ABSOLUTE_PATH.search(path.read_text(encoding="utf-8")):
            report.error(
                f"{path.relative_to(ROOT)}: contains a machine-specific absolute path"
            )


def validate_unity_project_privacy(report: Reporter) -> None:
    """Keep the two standalone Unity projects detached from Unity cloud services."""
    banned_package_terms = ("analytics", "collab")
    legacy_identity_markers = (
        "DevAccelerationSystem",
        "FoxsterDev",
        "FoxsterEntertaiment",
        "DefaultCompany",
        "2D_BuiltInRenderer",
    )

    for product_name, (project_root, application_id) in UNITY_PROJECTS.items():
        settings_path = project_root / "ProjectSettings" / "ProjectSettings.asset"
        connect_path = project_root / "ProjectSettings" / "UnityConnectSettings.asset"
        manifest_path = project_root / "Packages" / "manifest.json"

        try:
            settings_text = settings_path.read_text(encoding="utf-8")
        except OSError as exc:
            report.error(f"{settings_path.relative_to(ROOT)}: unreadable ({exc})")
            continue

        required_settings = (
            "  companyName: FD",
            f"  productName: {product_name}",
            "  submitAnalytics: 0",
            "  cloudServicesEnabled: {}",
            "  cloudEnabled: 0",
        )
        for expected in required_settings:
            if expected not in settings_text.splitlines():
                report.error(
                    f"{settings_path.relative_to(ROOT)}: expected privacy setting '{expected.strip()}'"
                )

        for empty_field in ("cloudProjectId", "projectName", "organizationId"):
            match = re.search(rf"^  {empty_field}:[ \t]*(.*)$", settings_text, re.MULTILINE)
            if match is None or match.group(1).strip():
                report.error(
                    f"{settings_path.relative_to(ROOT)}: {empty_field} must be present and empty"
                )

        identifier_match = re.search(
            r"^  applicationIdentifier:\n(?P<entries>(?:    [^\n]*\n)+)",
            settings_text,
            re.MULTILINE,
        )
        if identifier_match is None:
            report.error(
                f"{settings_path.relative_to(ROOT)}: applicationIdentifier block is missing"
            )
        else:
            identifiers = [
                line.split(":", 1)[1].strip()
                for line in identifier_match.group("entries").splitlines()
                if ":" in line
            ]
            if not identifiers or any(value != application_id for value in identifiers):
                report.error(
                    f"{settings_path.relative_to(ROOT)}: every applicationIdentifier must be {application_id}"
                )

        for project_settings_path in sorted((project_root / "ProjectSettings").rglob("*")):
            if not project_settings_path.is_file():
                continue
            try:
                project_settings_text = project_settings_path.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                continue
            for marker in legacy_identity_markers:
                if marker.casefold() in project_settings_text.casefold():
                    report.error(
                        f"{project_settings_path.relative_to(ROOT)}: legacy project identity '{marker}' remains"
                    )

        try:
            connect_text = connect_path.read_text(encoding="utf-8")
        except OSError as exc:
            report.error(f"{connect_path.relative_to(ROOT)}: unreadable ({exc})")
        else:
            for flag in ("m_Enabled", "m_InitializeOnStartup"):
                values = re.findall(rf"^\s+{flag}:\s*(\S+)\s*$", connect_text, re.MULTILINE)
                if not values:
                    report.error(
                        f"{connect_path.relative_to(ROOT)}: no {flag} flags found"
                    )
                elif any(value != "0" for value in values):
                    report.error(
                        f"{connect_path.relative_to(ROOT)}: every {flag} flag must be 0"
                    )

        manifest = load_json(manifest_path, report)
        if manifest is not None:
            dependencies = manifest.get("dependencies")
            if not isinstance(dependencies, dict):
                report.error(f"{manifest_path.relative_to(ROOT)}: dependencies must be an object")
            else:
                banned = sorted(
                    package_id
                    for package_id in dependencies
                    if any(term in package_id.casefold() for term in banned_package_terms)
                )
                if banned:
                    report.error(
                        f"{manifest_path.relative_to(ROOT)}: explicit Analytics/Collab packages are forbidden: "
                        + ", ".join(banned)
                    )


def validate_release_tag(report: Reporter, manifests: dict[str, dict], release_tag: str | None) -> None:
    if not release_tag:
        return
    match = re.fullmatch(r"(com\.foxsterdev\.[a-z0-9-]+)/(.+)", release_tag)
    if not match:
        report.error("release tag must be '<package-id>/<semver>'")
        return
    package_id, version = match.groups()
    if package_id not in manifests:
        report.error(f"release tag references an unknown package: {package_id}")
    elif manifests[package_id].get("version") != version:
        report.error(f"release tag version {version} does not match {package_id} package.json")
    if not SEMVER.fullmatch(version):
        report.error("release tag version is not SemVer")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--release-tag", help="validate a proposed package-specific release tag")
    args = parser.parse_args()
    report = Reporter()
    manifests = validate_packages(report)
    validate_asmdefs(report, manifests)
    validate_source_boundaries(report)
    validate_docs(report, manifests)
    validate_tracked_generated_files(report)
    validate_yaml(report)
    validate_harness_routing(report)
    validate_unity_project_privacy(report)
    validate_release_tag(report, manifests, args.release_tag)
    for warning in report.warnings:
        print(f"WARNING: {warning}")
    for error in report.errors:
        print(f"ERROR: {error}")
    if report.errors:
        print(f"Validation failed with {len(report.errors)} error(s).")
        return 1
    print(f"Validation passed for {len(manifests)} packages.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
