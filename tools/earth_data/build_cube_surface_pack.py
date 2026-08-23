#!/usr/bin/env python3
"""Build NovaCore's patch-aligned relaxed cube-sphere Earth surface pack.

This is an offline migration tool.  It reads lawful EPSG:4326 source rasters,
samples one continuous spherical function into deterministic
face/level/x/y patch records, and generates gutters by crossing canonical cube
face boundaries.  No equirectangular page identity, independent page fallback,
or runtime-visible page transition is encoded in the output.

The initial NCCUBE1 payload uses deterministic raw RGB8, R16, and R8 channel
records.  Raw storage deliberately keeps native ingestion independent of a
host compression library; a later GPU block encoder may replace the channel
encoding without changing patch identity or transactional ownership.  Records
for all channels of one patch are written together and share one patch identity
and payload digest.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
from pathlib import Path

import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None

MAGIC = b"NCCUBE1\0"
VERSION = 1
HEADER_BYTES = 256
RECORD_HEADER_BYTES = 96
DEFAULT_TILE_SIZE = 256
DEFAULT_GUTTER = 4
FACE_NAMES = ("PositiveX", "NegativeX", "PositiveY", "NegativeY", "PositiveZ", "NegativeZ")
MINIMUM_ELEVATION_METRES = -11000.0
MAXIMUM_ELEVATION_METRES = 9000.0


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest()


def patch_ordinal(face: int, level: int, x: int, y: int) -> int:
    preceding = sum(6 << (2 * previous) for previous in range(level))
    morton = 0
    for bit in range(24):
        morton |= ((x >> bit) & 1) << (2 * bit)
        morton |= ((y >> bit) & 1) << (2 * bit + 1)
    return preceding + face * (1 << (2 * level)) + morton


def relaxed_directions(face: int, u: np.ndarray, v: np.ndarray) -> np.ndarray:
    """Match RelaxedCubeSphereProjection.ProjectExtended exactly in vector form."""
    a = 2.0 * u - 1.0
    b = 2.0 * v - 1.0
    one = np.ones_like(a)
    if face == 0:
        cube = np.stack((one, b, -a), axis=-1)
    elif face == 1:
        cube = np.stack((-one, b, a), axis=-1)
    elif face == 2:
        cube = np.stack((a, one, -b), axis=-1)
    elif face == 3:
        cube = np.stack((a, -one, b), axis=-1)
    elif face == 4:
        cube = np.stack((a, b, one), axis=-1)
    else:
        cube = np.stack((-a, b, -one), axis=-1)
    cube /= np.max(np.abs(cube), axis=-1, keepdims=True)
    x, y, z = cube[..., 0], cube[..., 1], cube[..., 2]
    x2, y2, z2 = x * x, y * y, z * z
    result = np.stack((
        x * np.sqrt(np.maximum(0.0, 1.0 - 0.5 * (y2 + z2) + y2 * z2 / 3.0)),
        y * np.sqrt(np.maximum(0.0, 1.0 - 0.5 * (z2 + x2) + z2 * x2 / 3.0)),
        z * np.sqrt(np.maximum(0.0, 1.0 - 0.5 * (x2 + y2) + x2 * y2 / 3.0)),
    ), axis=-1)
    return result


def source_uv(direction: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    u = np.mod(np.arctan2(direction[..., 2], direction[..., 0]) / (2.0 * math.pi) + 0.5, 1.0)
    v = np.arccos(np.clip(direction[..., 1], -1.0, 1.0)) / math.pi
    return u, v


def bilinear(source: np.ndarray, u: np.ndarray, v: np.ndarray) -> np.ndarray:
    height, width = source.shape[:2]
    px, py = u * width - 0.5, v * height - 0.5
    x0, y0 = np.floor(px).astype(np.int64), np.clip(np.floor(py).astype(np.int64), 0, height - 1)
    x1, y1 = np.mod(x0 + 1, width), np.minimum(y0 + 1, height - 1)
    x0 = np.mod(x0, width)
    tx, ty = (px - np.floor(px))[..., None], (py - np.floor(py))[..., None]
    if source.ndim == 2:
        source = source[..., None]
    a, b, c, d = source[y0, x0], source[y0, x1], source[y1, x0], source[y1, x1]
    return (a * (1.0 - tx) + b * tx) * (1.0 - ty) + (c * (1.0 - tx) + d * tx) * ty


def load_image(path: Path, mode: str, maximum_level: int, tile_size: int) -> np.ndarray:
    with Image.open(path) as opened:
        image = opened.convert(mode)
        useful_width = tile_size * (1 << maximum_level) * 4
        if image.width > useful_width:
            useful_height = max(1, useful_width // 2)
            image = image.resize((useful_width, useful_height), Image.Resampling.LANCZOS)
        return np.asarray(image).copy()


def load_elevation(path: Path, maximum_level: int, tile_size: int) -> np.ndarray:
    values = np.memmap(path, dtype="<u2", mode="r", shape=(4096, 8192))
    useful_width = tile_size * (1 << maximum_level) * 4
    if useful_width >= 8192:
        return np.asarray(values).copy()
    image = Image.fromarray(np.asarray(values))
    image = image.resize((useful_width, useful_width // 2), Image.Resampling.BILINEAR)
    return np.asarray(image, dtype=np.uint16).copy()


def encoded_elevation(samples: np.ndarray) -> np.ndarray:
    return np.clip(np.rint(samples[..., 0]), 0, 65535).astype("<u2")


def build(args: argparse.Namespace) -> dict:
    if args.maximum_level < 0 or args.maximum_level > 8 or args.tile_size <= 0 or args.gutter < 1:
        raise ValueError("invalid bounded cube-surface build dimensions")
    albedo = load_image(args.albedo, "RGB", args.maximum_level, args.tile_size)
    elevation = load_elevation(args.elevation, args.maximum_level, args.tile_size)
    clouds = load_image(args.clouds, "L", min(args.maximum_level, 3), args.tile_size)
    extent = args.tile_size + 2 * args.gutter
    record_count = sum(6 << (2 * level) for level in range(args.maximum_level + 1))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    records: list[dict] = []
    payload_digest = hashlib.sha256()
    with args.output.open("wb") as stream:
        stream.write(bytes(HEADER_BYTES))
        for level in range(args.maximum_level + 1):
            side = 1 << level
            sample = (np.arange(extent, dtype=np.float64) - args.gutter + 0.5) / args.tile_size
            local_u, local_v = np.meshgrid(sample, sample)
            for face in range(6):
                for y in range(side):
                    for x in range(side):
                        face_u, face_v = (x + local_u) / side, (y + local_v) / side
                        direction = relaxed_directions(face, face_u, face_v)
                        u, v = source_uv(direction)
                        rgb = np.clip(np.rint(bilinear(albedo, u, v)), 0, 255).astype(np.uint8)
                        encoded_height = encoded_elevation(bilinear(elevation, u, v))
                        land = (encoded_height.astype(np.float64) / 65535.0 *
                                (MAXIMUM_ELEVATION_METRES - MINIMUM_ELEVATION_METRES) + MINIMUM_ELEVATION_METRES >= 0.0).astype(np.uint8) * 255
                        cloud = np.clip(np.rint(bilinear(clouds, u, v)[..., 0]), 0, 255).astype(np.uint8)
                        raw_channels = (rgb.tobytes(), encoded_height.tobytes(), land.tobytes(), cloud.tobytes())
                        identity = struct.pack("<QIBB2xII", args.body_id, args.terrain_version, face, level, x, y)
                        digest = hashlib.sha256(identity + b"".join(raw_channels)).digest()
                        ordinal = patch_ordinal(face, level, x, y)
                        offset = stream.tell()
                        header = bytearray(RECORD_HEADER_BYTES)
                        struct.pack_into("<QIBB2xIIQ4I32s", header, 0, args.body_id, args.terrain_version,
                                         face, level, x, y, ordinal, *(len(value) for value in raw_channels), digest)
                        stream.write(header)
                        for channel in raw_channels:
                            stream.write(channel)
                        payload_digest.update(header)
                        for channel in raw_channels:
                            payload_digest.update(channel)
                        records.append({"face": FACE_NAMES[face], "level": level, "x": x, "y": y,
                                        "ordinal": ordinal, "offset": offset,
                                        "storedBytes": [len(value) for value in raw_channels],
                                        "payloadSha256": digest.hex()})
        header = bytearray(HEADER_BYTES)
        struct.pack_into("<8s8I2f32s32s32s", header, 0, MAGIC, VERSION, HEADER_BYTES,
                         args.tile_size, args.gutter, extent, args.maximum_level, record_count,
                         args.terrain_version, MINIMUM_ELEVATION_METRES, MAXIMUM_ELEVATION_METRES,
                         bytes.fromhex(sha256(args.albedo)), bytes.fromhex(sha256(args.elevation)), payload_digest.digest())
        stream.seek(0)
        stream.write(header)
    pack_hash = sha256(args.output)
    manifest = {
        "schema": "NovaCore.PlanetaryCubeSurface/1",
        "pack": args.output.name,
        "packBytes": args.output.stat().st_size,
        "packSha256": pack_hash,
        "bodyId": args.body_id,
        "terrainVersion": args.terrain_version,
        "projection": "body-fixed hierarchical relaxed cube-sphere",
        "patchIdentity": "body/terrain-version/face/level/x/y",
        "tile": {"interior": args.tile_size, "canonicalSphericalGutter": args.gutter, "storedExtent": extent},
        "maximumLevel": args.maximum_level,
        "recordCount": record_count,
        "channels": ["RGB8_SRGB", "R16_UNORM_ELEVATION", "R8_LAND_MASK", "R8_CLOUD"],
        "channelEncoding": "raw-patch-transaction-v1",
        "promotion": "geometry+elevation+material transactional quartet",
        "source": {
            "albedo": {"path": args.albedo.name, "sha256": sha256(args.albedo)},
            "elevation": {"path": args.elevation.name, "sha256": sha256(args.elevation)},
            "clouds": {"path": args.clouds.name, "sha256": sha256(args.clouds)},
        },
        "records": records,
    }
    manifest_path = args.output.with_suffix(args.output.suffix + ".manifest.json")
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return manifest


def parse_args() -> argparse.Namespace:
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser()
    parser.add_argument("--albedo", type=Path, default=root / "assets/earth/source/nasa-blue-marble-2004-12-21600x10800.png")
    parser.add_argument("--elevation", type=Path, default=root / "assets/earth/runtime/earth_elevation_8192x4096.r16")
    parser.add_argument("--clouds", type=Path, default=root / "assets/earth/source/nasa-blue-marble-clouds-2048.jpg")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--body-id", type=int, default=6)
    parser.add_argument("--terrain-version", type=int, default=4)
    parser.add_argument("--maximum-level", type=int, default=5)
    parser.add_argument("--tile-size", type=int, default=DEFAULT_TILE_SIZE)
    parser.add_argument("--gutter", type=int, default=DEFAULT_GUTTER)
    return parser.parse_args()


if __name__ == "__main__":
    result = build(parse_args())
    print(json.dumps({key: result[key] for key in ("pack", "packBytes", "packSha256", "recordCount", "maximumLevel")}, indent=2))
