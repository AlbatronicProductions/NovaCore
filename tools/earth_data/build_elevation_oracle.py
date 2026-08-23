#!/usr/bin/env python3
"""Build the topology-neutral checked Earth CPU elevation oracle."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None

WIDTH = 8192
HEIGHT = 4096
MINIMUM_METRES = -11_000.0
MAXIMUM_METRES = 9_000.0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--elevation", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    if not args.elevation.is_file():
        raise FileNotFoundError(args.elevation)
    with Image.open(args.elevation) as image:
        source = np.asarray(image.resize((WIDTH, HEIGHT), Image.Resampling.BILINEAR), dtype=np.float64)
    normalized = np.clip((source - MINIMUM_METRES) / (MAXIMUM_METRES - MINIMUM_METRES), 0.0, 1.0)
    encoded = np.rint(normalized * 65535.0).astype("<u2")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    encoded.tofile(args.output)
    digest = hashlib.sha256(args.output.read_bytes()).hexdigest()
    print(f"{args.output}: {args.output.stat().st_size} bytes; sha256={digest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
