"""Reject case-only asset path collisions before Unity imports on Linux."""
from collections import defaultdict
from pathlib import Path
import subprocess
import sys


def main():
    root = Path(__file__).resolve().parent.parent
    tracked = subprocess.check_output(
        ["git", "ls-files", "-z", "--", "Assets"], cwd=root
    ).decode("utf-8").split("\0")
    variants = defaultdict(set)
    for path in filter(None, tracked):
        parts = path.split("/")
        # Include implicit directories: distinct filenames in differently cased
        # folders still collide in Unity, even without duplicate file paths.
        for depth in range(1, len(parts) + 1):
            prefix = "/".join(parts[:depth])
            variants[prefix.casefold()].add(prefix)

    collisions = [sorted(names) for names in variants.values() if len(names) > 1]
    if collisions:
        print("Case-only Unity asset path collisions:", file=sys.stderr)
        for names in sorted(collisions):
            print("  " + " | ".join(names), file=sys.stderr)
        return 1
    print("Unity asset path casing check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
