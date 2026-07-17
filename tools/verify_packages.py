#!/usr/bin/env python3
"""Fail a release if a package would ship without its licence and third-party notices.

Every version published before 2026-07-17 shipped without them — that is why this
exists. The failure was silent: the repo's LICENSE and NOTICE.txt were correct, but
nothing put them inside the .nupkg, and nobody looks inside a .nupkg.

Each packable project must produce a package that carries:

  * <license type="file"> — never an SPDX expression. nuget.org rejects BUSL-1.1 as
    an expression, and an expression packs no file at all: it just links to the
    generic SPDX text, which has no copyright line. That is how nine Avr8Sharp
    versions ended up shipping Uri Shaked's code with his name nowhere in the box.
  * the licence file the nuspec names.
  * NOTICE.txt — the attribution the MIT and BSD components oblige us to pass on.

Usage:  python3 tools/verify_packages.py nupkgs/*.nupkg
"""

import re
import sys
import zipfile

REQUIRED = "NOTICE.txt"


def check(path):
    """Return a list of problems with one package. Empty means it is good to ship."""
    problems = []
    try:
        z = zipfile.ZipFile(path)
    except (zipfile.BadZipFile, OSError) as e:
        return [f"cannot be read as a package ({e})"]

    names = z.namelist()
    nuspecs = [n for n in names if n.endswith(".nuspec")]
    if not nuspecs:
        return ["contains no .nuspec"]

    nuspec = z.read(nuspecs[0]).decode("utf-8", "replace")
    m = re.search(r"<license\s+type=\"(\w+)\"\s*>([^<]*)</license>", nuspec)

    if not m:
        problems.append("declares no <license> at all")
    elif m.group(1) == "expression":
        problems.append(
            f"declares the SPDX expression '{m.group(2)}' instead of a licence file — "
            "an expression packs no file, so no copyright notice reaches the consumer"
        )
    else:
        licence_file = m.group(2).strip()
        if licence_file not in names:
            problems.append(f"names '{licence_file}' as its licence but does not contain it")

    if REQUIRED not in names:
        problems.append(f"does not contain {REQUIRED}")

    return problems


def main(paths):
    if not paths:
        raise SystemExit(__doc__)

    failed = False
    for path in paths:
        problems = check(path)
        name = path.split("/")[-1]
        if problems:
            failed = True
            for p in problems:
                # GitHub Actions renders ::error:: as an annotation; harmless locally.
                print(f"::error::{name} {p}")
        else:
            print(f"ok  {name}")

    if failed:
        print("::error::refusing to publish: see above. Fix the packaging, not this check.")
        return 1
    print(f"\nall {len(paths)} package(s) carry their licence and {REQUIRED}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
