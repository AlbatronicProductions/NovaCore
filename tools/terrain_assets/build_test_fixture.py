#!/usr/bin/env python3
"""Generate the tiny deterministic NCCUBE1 architecture fixture."""

from __future__ import annotations

import argparse
import hashlib
import struct
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    body_id, terrain_version, interior, gutter = 6006, 44, 8, 1
    extent, record_count = interior + 2 * gutter, 6
    args.output.parent.mkdir(parents=True, exist_ok=True)
    payload_digest = hashlib.sha256()
    with args.output.open("wb") as stream:
        stream.write(bytes(256))
        for face in range(6):
            identity = struct.pack("<QIBB2xII", body_id, terrain_version, face, 0, 0, 0)
            albedo = bytes(((face * 31 + index * 7) & 255) for index in range(extent * extent * 3))
            elevation = b"".join(struct.pack("<H", (face * 4096 + index * 257) & 65535) for index in range(extent * extent))
            land = bytes((255 if (index + face) % 3 else 0) for index in range(extent * extent))
            cloud = bytes(((index * 13 + face * 17) & 255) for index in range(extent * extent))
            channels = (albedo, elevation, land, cloud)
            digest = hashlib.sha256(identity + b"".join(channels)).digest()
            header = bytearray(96)
            struct.pack_into("<QIBB2xIIQ4I32s", header, 0, body_id, terrain_version, face, 0, 0, 0, face, *(len(value) for value in channels), digest)
            stream.write(header)
            for channel in channels:
                stream.write(channel)
            payload_digest.update(header)
            for channel in channels:
                payload_digest.update(channel)
        header = bytearray(256)
        struct.pack_into("<8s8I2f32s32s32s", header, 0, b"NCCUBE1\0", 1, 256, interior, gutter, extent, 0, record_count, terrain_version, -11000.0, 9000.0, bytes(32), bytes(32), payload_digest.digest())
        stream.seek(0)
        stream.write(header)


if __name__ == "__main__":
    main()
