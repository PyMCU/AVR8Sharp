# hatch_build.py
# Custom hatchling build hook: compiles the Avr8Sharp.Native AOT shared library and places it
# next to the avr8sharp package (as libavr8sharp.{dylib,so} / avr8sharp.dll) before packaging.
#
# Environment variables:
#   AVR8SHARP_SKIP_DOTNET_BUILD=1
#       Skip `dotnet publish` and use an existing native library at build/native/ instead.
#       When none is present (e.g. sdist-only builds), the hook returns early (source-only).
#   DOTNET_RID
#       Override the target Runtime Identifier (e.g. linux-x64, osx-arm64). Defaults to host.
#   WHEEL_PLATFORM_TAG
#       Override the wheel platform tag for cross-compilation
#       (e.g. manylinux_2_28_x86_64, win_amd64, macosx_14_0_arm64).

from __future__ import annotations

import os
import platform
import shutil
import subprocess
import sys
import sysconfig
from pathlib import Path

from hatchling.builders.hooks.plugin.interface import BuildHookInterface

# Native library filename produced by `dotnet publish` (assembly name = Avr8Sharp.Native),
# and the bundled name the ctypes loader (_native.py) looks for, per platform.
_PUBLISHED = {
    "darwin": "Avr8Sharp.Native.dylib",
    "linux": "Avr8Sharp.Native.so",
    "win32": "Avr8Sharp.Native.dll",
}
_BUNDLED = {
    "darwin": "libavr8sharp.dylib",
    "linux": "libavr8sharp.so",
    "win32": "avr8sharp.dll",
}


def _platform_key() -> str:
    if sys.platform.startswith("linux"):
        return "linux"
    return sys.platform


class CustomBuildHook(BuildHookInterface):
    PLUGIN_NAME = "custom"

    def initialize(self, version: str, build_data: dict) -> None:
        root = Path(self.root)
        key = _platform_key()
        published_name = _PUBLISHED[key]
        bundled_name = _BUNDLED[key]

        dst = root / "src" / "avr8sharp" / bundled_name
        cache = root / "build" / "native" / published_name

        if os.environ.get("AVR8SHARP_SKIP_DOTNET_BUILD") == "1":
            if cache.exists():
                self.app.display_info(
                    f"[hatch-hook] Skipping dotnet publish; using cached binary: {cache}"
                )
                src = cache
            else:
                self.app.display_info(
                    "[hatch-hook] AVR8SHARP_SKIP_DOTNET_BUILD=1 and no cached binary found; "
                    "building source-only package (no native library included)."
                )
                return
        else:
            # The csproj lives in the monorepo, two levels up from bindings/python.
            csproj = root.parent.parent / "src" / "Avr8Sharp.Native" / "Avr8Sharp.Native.csproj"
            publish_dir = root / "build" / "native"
            publish_dir.mkdir(parents=True, exist_ok=True)

            cmd = [
                "dotnet", "publish", str(csproj),
                "-c", "Release",
                "-o", str(publish_dir),
                "--nologo",
            ]
            rid = _get_rid()
            if rid:
                cmd += ["-r", rid]
                self.app.display_info(f"[hatch-hook] Target RID: {rid}")

            self.app.display_info(f"[hatch-hook] Running dotnet publish -> {publish_dir}")
            if subprocess.run(cmd).returncode != 0:
                raise RuntimeError(f"dotnet publish failed: {' '.join(cmd)}")

            src = publish_dir / published_name
            if not src.exists():
                raise FileNotFoundError(f"Native library not found after publish: {src}")

        shutil.copy2(str(src), str(dst))
        if sys.platform != "win32":
            dst.chmod(0o755)
        self.app.display_info(f"[hatch-hook] Native library placed at: {dst}")

        build_data["artifacts"].append(str(dst.relative_to(root)))
        build_data["pure_python"] = False
        build_data["tag"] = f"py3-none-{_get_wheel_platform_tag()}"
        self.app.display_info(f"[hatch-hook] Wheel tag: py3-none-{_get_wheel_platform_tag()}")


def _get_rid() -> str | None:
    override = os.environ.get("DOTNET_RID")
    if override:
        return override
    m = platform.machine().lower()
    s = platform.system().lower()
    table = {
        ("linux", "x86_64"): "linux-x64",
        ("linux", "aarch64"): "linux-arm64",
        ("darwin", "x86_64"): "osx-x64",
        ("darwin", "arm64"): "osx-arm64",
        ("windows", "amd64"): "win-x64",
        ("windows", "arm64"): "win-arm64",
    }
    return table.get((s, m))


def _get_wheel_platform_tag() -> str:
    override = os.environ.get("WHEEL_PLATFORM_TAG")
    if override:
        return override
    if sys.platform.startswith("linux"):
        arch = platform.machine().lower()
        return f"manylinux_2_28_{arch}"
    return sysconfig.get_platform().replace("-", "_").replace(".", "_")
