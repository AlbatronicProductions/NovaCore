#!/usr/bin/env python3
"""Build NovaCore's deterministic Earth virtual-texture dataset.

The output is a fixed-layout, equirectangular tile pyramid.  Each tile carries
two-pixel geographic gutters and three independently addressable layers: sRGB albedo plus
land mask, unsigned-16 elevation, and unsigned-8 cloud opacity.  The runtime
never parses the large source rasters.
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

MAGIC = b"NCVTEAR1"
VERSION = 2
HEADER_SIZE = 128
PRODUCTION_TILE_SIZE = 256
PRODUCTION_GUTTER_PIXELS = 2
MIN_ELEVATION_METRES = -11_000.0
MAX_ELEVATION_METRES = 9_000.0


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest()


def resize(source: Image.Image, size: tuple[int, int], mode: str) -> Image.Image:
    converted = source.convert(mode)
    return converted.resize(size, Image.Resampling.LANCZOS)


def guttered(array: np.ndarray, tile_size: int, gutter: int, x: int, y: int) -> np.ndarray:
    height, width = array.shape[:2]
    x0, y0 = x * tile_size, y * tile_size
    xs = np.mod(np.arange(x0 - gutter, x0 + tile_size + gutter), width)
    ys = np.clip(np.arange(y0 - gutter, y0 + tile_size + gutter), 0, height - 1)
    return array[np.ix_(ys, xs)]


def validate_gutters(source: np.ndarray, tile: np.ndarray, tile_size: int, gutter: int, x: int, y: int) -> None:
    """Verify interior registration, geographic neighbors, longitude wrap, and polar clamp."""
    height, width = source.shape[:2]
    x0, y0 = x * tile_size, y * tile_size
    expected_shape = (tile_size + 2 * gutter, tile_size + 2 * gutter) + source.shape[2:]
    if tile.shape != expected_shape:
        raise RuntimeError(f"Tile {x},{y} has shape {tile.shape}; expected {expected_shape}.")
    if not np.array_equal(tile[gutter:-gutter, gutter:-gutter], source[y0:y0 + tile_size, x0:x0 + tile_size]):
        raise RuntimeError(f"Tile {x},{y} interior registration failed.")
    horizontal_y = np.arange(y0, y0 + tile_size)
    left_x = np.mod(np.arange(x0 - gutter, x0), width)
    right_x = np.mod(np.arange(x0 + tile_size, x0 + tile_size + gutter), width)
    if not np.array_equal(tile[gutter:-gutter, :gutter], source[np.ix_(horizontal_y, left_x)]):
        raise RuntimeError(f"Tile {x},{y} left geographic gutter failed.")
    if not np.array_equal(tile[gutter:-gutter, -gutter:], source[np.ix_(horizontal_y, right_x)]):
        raise RuntimeError(f"Tile {x},{y} right geographic gutter failed.")
    all_x = np.mod(np.arange(x0 - gutter, x0 + tile_size + gutter), width)
    top_y = np.clip(np.arange(y0 - gutter, y0), 0, height - 1)
    bottom_y = np.clip(np.arange(y0 + tile_size, y0 + tile_size + gutter), 0, height - 1)
    if not np.array_equal(tile[:gutter], source[np.ix_(top_y, all_x)]):
        raise RuntimeError(f"Tile {x},{y} north-polar gutter failed.")
    if not np.array_equal(tile[-gutter:], source[np.ix_(bottom_y, all_x)]):
        raise RuntimeError(f"Tile {x},{y} south-polar gutter failed.")


def encode_elevation(values: np.ndarray) -> np.ndarray:
    normalized = np.clip(
        (values.astype(np.float64) - MIN_ELEVATION_METRES)
        / (MAX_ELEVATION_METRES - MIN_ELEVATION_METRES),
        0.0,
        1.0,
    )
    return np.rint(normalized * 65535.0).astype("<u2")


def write_header(
    stream,
    *,
    tile_size: int,
    gutter: int,
    maximum_level: int,
    tile_count: int,
    payload_hash: bytes,
    identity_hash: bytes,
) -> None:
    extent = tile_size + 2 * gutter
    albedo_bytes = extent * extent * 4
    elevation_bytes = extent * extent * 2
    cloud_bytes = extent * extent
    record_bytes = albedo_bytes + elevation_bytes + cloud_bytes
    header = struct.pack(
        "<8s11I2f32s32sI",
        MAGIC,
        VERSION,
        HEADER_SIZE,
        tile_size,
        gutter,
        maximum_level,
        tile_count,
        extent,
        record_bytes,
        albedo_bytes,
        elevation_bytes,
        cloud_bytes,
        MIN_ELEVATION_METRES,
        MAX_ELEVATION_METRES,
        identity_hash,
        payload_hash,
        0,
    )
    if len(header) != HEADER_SIZE:
        raise RuntimeError(f"Earth dataset header is {len(header)} bytes, expected {HEADER_SIZE}.")
    stream.write(header)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--albedo", required=True, type=Path)
    parser.add_argument("--elevation", required=True, type=Path)
    parser.add_argument("--clouds", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--tile-size", type=int, default=PRODUCTION_TILE_SIZE)
    parser.add_argument("--gutter", type=int, default=PRODUCTION_GUTTER_PIXELS)
    parser.add_argument("--maximum-level", type=int, default=4)
    args = parser.parse_args()

    if args.tile_size < 32 or args.tile_size > 512 or args.tile_size & (args.tile_size - 1):
        raise ValueError("tile-size must be a power of two in [32, 512].")
    if args.tile_size != PRODUCTION_TILE_SIZE or args.gutter != PRODUCTION_GUTTER_PIXELS:
        raise ValueError("The production Earth format requires 256-pixel tiles and two-pixel gutters.")
    if args.maximum_level < 0 or args.maximum_level > 10:
        raise ValueError("maximum-level must be in [0, 10].")
    for source in (args.albedo, args.elevation, args.clouds):
        if not source.is_file():
            raise FileNotFoundError(source)

    args.output.mkdir(parents=True, exist_ok=True)
    width = args.tile_size * (1 << (args.maximum_level + 1))
    height = args.tile_size * (1 << args.maximum_level)
    target_size = (width, height)

    with Image.open(args.albedo) as image:
        albedo_full = np.asarray(resize(image, target_size, "RGB"), dtype=np.uint8)
    with Image.open(args.clouds) as image:
        cloud_full = np.asarray(resize(image, target_size, "L"), dtype=np.uint8)
    with Image.open(args.elevation) as image:
        elevation_image = image.resize(target_size, Image.Resampling.BILINEAR)
        elevation_full = np.asarray(elevation_image, dtype=np.float32)

    elevation_encoded = encode_elevation(elevation_full)
    land_mask = np.where(elevation_full >= 0.0, 255, 0).astype(np.uint8)
    albedo_rgba = np.dstack((albedo_full, land_mask))
    elevation_path = args.output / f"earth_elevation_{width}x{height}.r16"
    elevation_encoded.tofile(elevation_path)

    tile_count = sum(2 * (4**level) for level in range(args.maximum_level + 1))
    pack_path = args.output / "earth_surface_v2.ncvtex"
    identity = hashlib.sha256()
    identity.update(MAGIC)
    identity.update(struct.pack("<IIII", VERSION, args.tile_size, args.gutter, args.maximum_level))
    for source in (args.albedo, args.elevation, args.clouds):
        identity.update(bytes.fromhex(sha256(source)))
    identity_hash = identity.digest()

    payload_digest = hashlib.sha256()
    with pack_path.open("wb") as stream:
        write_header(
            stream,
            tile_size=args.tile_size,
            gutter=args.gutter,
            maximum_level=args.maximum_level,
            tile_count=tile_count,
            payload_hash=bytes(32),
            identity_hash=identity_hash,
        )
        for level in range(args.maximum_level + 1):
            level_width = args.tile_size * (1 << (level + 1))
            level_height = args.tile_size * (1 << level)
            if level == args.maximum_level:
                albedo_level = albedo_rgba
                elevation_level = elevation_encoded
                cloud_level = cloud_full
            else:
                albedo_level = np.asarray(
                    Image.fromarray(albedo_rgba, "RGBA").resize(
                        (level_width, level_height), Image.Resampling.LANCZOS
                    ),
                    dtype=np.uint8,
                )
                elevation_level = encode_elevation(
                    np.asarray(
                        Image.fromarray(elevation_full, "F").resize(
                            (level_width, level_height), Image.Resampling.BILINEAR
                        ),
                        dtype=np.float32,
                    )
                )
                cloud_level = np.asarray(
                    Image.fromarray(cloud_full, "L").resize(
                        (level_width, level_height), Image.Resampling.LANCZOS
                    ),
                    dtype=np.uint8,
                )
            x_tiles, y_tiles = 1 << (level + 1), 1 << level
            for y in range(y_tiles):
                for x in range(x_tiles):
                    tiles = (
                        guttered(albedo_level, args.tile_size, args.gutter, x, y),
                        guttered(elevation_level, args.tile_size, args.gutter, x, y),
                        guttered(cloud_level, args.tile_size, args.gutter, x, y),
                    )
                    for source, tile in zip((albedo_level, elevation_level, cloud_level), tiles, strict=True):
                        validate_gutters(source, tile, args.tile_size, args.gutter, x, y)
                    chunks = tuple(tile.tobytes(order="C") for tile in tiles)
                    for chunk in chunks:
                        stream.write(chunk)
                        payload_digest.update(chunk)
        stream.seek(0)
        write_header(
            stream,
            tile_size=args.tile_size,
            gutter=args.gutter,
            maximum_level=args.maximum_level,
            tile_count=tile_count,
            payload_hash=payload_digest.digest(),
            identity_hash=identity_hash,
        )

    manifest = {
        "schema": "NovaCore.EarthVirtualTexture/2",
        "identitySha256": identity_hash.hex(),
        "payloadSha256": payload_digest.hexdigest(),
        "projection": "EPSG:4326 equirectangular, north-up, longitude [-180,180)",
        "tileSize": args.tile_size,
        "gutterPixels": args.gutter,
        "physicalTileExtent": args.tile_size + 2 * args.gutter,
        "maximumLevel": args.maximum_level,
        "tileCount": tile_count,
        "levels": [
            {
                "level": level,
                "offset": sum(2 * (4**prior) for prior in range(level)),
                "tilesX": 1 << (level + 1),
                "tilesY": 1 << level,
                "tileCount": 2 * (4**level),
                "parentRule": "floor(x/2),floor(y/2)" if level else "self",
            }
            for level in range(args.maximum_level + 1)
        ],
        "fullResolution": [width, height],
        "elevationEncoding": {
            "format": "R16_UNORM",
            "minimumMetres": MIN_ELEVATION_METRES,
            "maximumMetres": MAX_ELEVATION_METRES,
        },
        "layers": [
            {"name": "albedoLand", "format": "R8G8B8A8_SRGB", "alpha": "land mask"},
            {"name": "elevation", "format": "R16_UNORM"},
            {"name": "cloud", "format": "R8_UNORM"},
        ],
        "sources": [
            {"role": "albedo", "file": args.albedo.name, "sha256": sha256(args.albedo)},
            {"role": "elevation", "file": args.elevation.name, "sha256": sha256(args.elevation)},
            {"role": "cloud", "file": args.clouds.name, "sha256": sha256(args.clouds)},
        ],
        "runtimeFiles": {
            pack_path.name: {"bytes": pack_path.stat().st_size, "sha256": sha256(pack_path)},
            elevation_path.name: {"bytes": elevation_path.stat().st_size, "sha256": sha256(elevation_path)},
        },
    }
    manifest_path = args.output / "earth_surface_v2.manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
