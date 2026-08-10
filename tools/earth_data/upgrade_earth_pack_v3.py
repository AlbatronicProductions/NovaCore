#!/usr/bin/env python3
"""Deterministically convert NovaCore Earth SVT v2 into GPU-native v3.

The converter is deliberately offline.  It preserves R16 elevation verbatim,
separates the coastline mask from albedo alpha, measures BC1 against the
selected BC7 sRGB output, writes BC4 linear masks/clouds, and caps clouds at
their native 2048x1024 (SVT L2) information limit.  No encoder or decoder is
linked into the runtime.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
from pathlib import Path

import numpy as np

NUMPY_VERSION = "2.3.5"

V2_MAGIC = b"NCVTEAR1"
V3_MAGIC = b"NCVTEAR2"
V2_HEADER_BYTES = 128
V3_HEADER_BYTES = 256
VERSION = 3
TILE_SIZE = 256
GUTTER = 2
EXTENT = 260
MAXIMUM_LEVEL = 4
TILE_COUNT = 682
BLOCKS_PER_TILE = (EXTENT // 4) ** 2
BC1_TILE_BYTES = BLOCKS_PER_TILE * 8
BC7_TILE_BYTES = BLOCKS_PER_TILE * 16
BC4_TILE_BYTES = BLOCKS_PER_TILE * 8
R16_TILE_BYTES = EXTENT * EXTENT * 2
V2_ALBEDO_BYTES = EXTENT * EXTENT * 4
V2_RECORD_BYTES = V2_ALBEDO_BYTES + R16_TILE_BYTES + EXTENT * EXTENT
CHANNELS = (
    # semantic, format, color space, maximum LOD, tile bytes
    (1, 4, 1, 4, BC7_TILE_BYTES),  # albedo / BC7 SRGB
    (2, 2, 0, 4, R16_TILE_BYTES),  # elevation / R16 UNORM linear
    (3, 3, 0, 4, BC4_TILE_BYTES),  # coastline mask / BC4 UNORM linear
    (4, 3, 0, 2, BC4_TILE_BYTES),  # clouds / BC4 UNORM linear
)


def tile_count(maximum_level: int) -> int:
    return sum(2 * 4**level for level in range(maximum_level + 1))


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest()


def blocks(values: np.ndarray) -> np.ndarray:
    channels = 1 if values.ndim == 2 else values.shape[2]
    reshaped = values.reshape(EXTENT // 4, 4, EXTENT // 4, 4, channels)
    return reshaped.transpose(0, 2, 1, 3, 4).reshape(BLOCKS_PER_TILE, 16, channels)


def rgb565(values: np.ndarray) -> np.ndarray:
    r = np.rint(values[:, 0].astype(np.float64) * 31.0 / 255.0).astype(np.uint16)
    g = np.rint(values[:, 1].astype(np.float64) * 63.0 / 255.0).astype(np.uint16)
    b = np.rint(values[:, 2].astype(np.float64) * 31.0 / 255.0).astype(np.uint16)
    return (r << 11) | (g << 5) | b


def expand565(values: np.ndarray) -> np.ndarray:
    result = np.empty((values.size, 3), dtype=np.uint16)
    result[:, 0] = ((values >> 11) & 31) * 255 // 31
    result[:, 1] = ((values >> 5) & 63) * 255 // 63
    result[:, 2] = (values & 31) * 255 // 31
    return result


def encode_bc1(rgb: np.ndarray) -> tuple[bytes, np.ndarray]:
    source = blocks(rgb).astype(np.int32)
    low = source.min(axis=1).astype(np.uint8)
    high = source.max(axis=1).astype(np.uint8)
    endpoint0 = rgb565(high)
    endpoint1 = rgb565(low)
    equal = endpoint0 == endpoint1
    endpoint0[equal & (endpoint0 < 65535)] += 1
    endpoint1[equal & (endpoint0 == 65535)] -= 1
    swap = endpoint0 < endpoint1
    endpoint0[swap], endpoint1[swap] = endpoint1[swap], endpoint0[swap].copy()
    color0 = expand565(endpoint0)
    color1 = expand565(endpoint1)
    palette = np.stack((color0, color1, (2 * color0 + color1) // 3, (color0 + 2 * color1) // 3), axis=1)
    distances = ((source[:, :, None, :] - palette[:, None, :, :].astype(np.int32)) ** 2).sum(axis=3)
    indices = distances.argmin(axis=2).astype(np.uint32)
    packed = np.zeros(BLOCKS_PER_TILE, dtype=np.uint32)
    for pixel in range(16):
        packed |= indices[:, pixel] << (2 * pixel)
    encoded = np.empty((BLOCKS_PER_TILE, 8), dtype=np.uint8)
    encoded[:, :2] = endpoint0.astype("<u2").view(np.uint8).reshape(-1, 2)
    encoded[:, 2:4] = endpoint1.astype("<u2").view(np.uint8).reshape(-1, 2)
    encoded[:, 4:] = packed.astype("<u4").view(np.uint8).reshape(-1, 4)
    decoded_blocks = palette[np.arange(BLOCKS_PER_TILE)[:, None], indices].astype(np.uint8)
    decoded = decoded_blocks.reshape(EXTENT // 4, EXTENT // 4, 4, 4, 3).transpose(0, 2, 1, 3, 4).reshape(EXTENT, EXTENT, 3)
    return encoded.tobytes(), decoded


def encode_bc4(channel: np.ndarray) -> tuple[bytes, np.ndarray]:
    source = blocks(channel)[:, :, 0].astype(np.int32)
    endpoint0 = source.max(axis=1).astype(np.uint8)
    endpoint1 = source.min(axis=1).astype(np.uint8)
    palette = np.empty((BLOCKS_PER_TILE, 8), dtype=np.int32)
    palette[:, 0], palette[:, 1] = endpoint0, endpoint1
    for index in range(1, 7):
        palette[:, index + 1] = ((7 - index) * endpoint0.astype(np.int32) + index * endpoint1.astype(np.int32) + 3) // 7
    distances = np.abs(source[:, :, None] - palette[:, None, :])
    indices = distances.argmin(axis=2).astype(np.uint64)
    packed = np.zeros(BLOCKS_PER_TILE, dtype=np.uint64)
    for pixel in range(16):
        packed |= indices[:, pixel] << (3 * pixel)
    encoded = np.empty((BLOCKS_PER_TILE, 8), dtype=np.uint8)
    encoded[:, 0], encoded[:, 1] = endpoint0, endpoint1
    packed_bytes = packed.astype("<u8").view(np.uint8).reshape(-1, 8)
    encoded[:, 2:] = packed_bytes[:, :6]
    decoded_blocks = palette[np.arange(BLOCKS_PER_TILE)[:, None], indices].astype(np.uint8)
    decoded = decoded_blocks.reshape(EXTENT // 4, EXTENT // 4, 4, 4).transpose(0, 2, 1, 3).reshape(EXTENT, EXTENT)
    return encoded.tobytes(), decoded


def quantize_mode6_endpoint(values: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Return BC7 mode-6 7-bit endpoint components, p-bit, and decoded bytes."""
    expanded = np.concatenate((values.astype(np.int32), np.full((values.shape[0], 1), 255, dtype=np.int32)), axis=1)
    candidates = []
    for pbit in (0, 1):
        quantized = np.clip(np.rint((expanded - pbit) / 2.0), 0, 127).astype(np.uint64)
        decoded = (quantized.astype(np.int32) << 1) | pbit
        error = ((decoded - expanded) ** 2).sum(axis=1)
        candidates.append((quantized, decoded, error))
    use_one = candidates[1][2] < candidates[0][2]
    quantized = np.where(use_one[:, None], candidates[1][0], candidates[0][0])
    decoded = np.where(use_one[:, None], candidates[1][1], candidates[0][1])
    return quantized, use_one.astype(np.uint64), decoded


def encode_bc7_mode6(rgb: np.ndarray) -> tuple[bytes, np.ndarray]:
    """Encode opaque RGB with the deterministic high-quality BC7 mode-6 subset."""
    source = blocks(rgb).astype(np.int32)
    low = source.min(axis=1).astype(np.uint8)
    high = source.max(axis=1).astype(np.uint8)
    q0, p0, endpoint0 = quantize_mode6_endpoint(high)
    q1, p1, endpoint1 = quantize_mode6_endpoint(low)
    weights = np.array((0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64), dtype=np.int32)
    palette = ((64 - weights[None, :, None]) * endpoint0[:, None, :3] +
               weights[None, :, None] * endpoint1[:, None, :3] + 32) >> 6
    distances = ((source[:, :, None, :] - palette[:, None, :, :]) ** 2).sum(axis=3)
    indices = distances.argmin(axis=2).astype(np.uint64)
    swap = indices[:, 0] >= 8
    old_q0, old_p0, old_endpoint0 = q0.copy(), p0.copy(), endpoint0.copy()
    q0[swap], p0[swap], endpoint0[swap] = q1[swap], p1[swap], endpoint1[swap]
    q1[swap], p1[swap], endpoint1[swap] = old_q0[swap], old_p0[swap], old_endpoint0[swap]
    indices[swap] = 15 - indices[swap]
    palette = ((64 - weights[None, :, None]) * endpoint0[:, None, :3] +
               weights[None, :, None] * endpoint1[:, None, :3] + 32) >> 6

    low_bits = np.full(BLOCKS_PER_TILE, 1 << 6, dtype=np.uint64)
    low_bits |= q0[:, 0] << 7
    low_bits |= q1[:, 0] << 14
    low_bits |= q0[:, 1] << 21
    low_bits |= q1[:, 1] << 28
    low_bits |= q0[:, 2] << 35
    low_bits |= q1[:, 2] << 42
    low_bits |= q0[:, 3] << 49
    low_bits |= q1[:, 3] << 56
    low_bits |= p0 << 63
    high_bits = p1.copy()
    high_bits |= indices[:, 0] << 1
    for pixel in range(1, 16):
        high_bits |= indices[:, pixel] << (4 + (pixel - 1) * 4)
    encoded = np.concatenate((low_bits.astype("<u8").view(np.uint8).reshape(-1, 8),
                              high_bits.astype("<u8").view(np.uint8).reshape(-1, 8)), axis=1)
    decoded_blocks = palette[np.arange(BLOCKS_PER_TILE)[:, None], indices].astype(np.uint8)
    decoded = decoded_blocks.reshape(EXTENT // 4, EXTENT // 4, 4, 4, 3).transpose(0, 2, 1, 3, 4).reshape(EXTENT, EXTENT, 3)
    return encoded.tobytes(), decoded


def psnr(squared_error: int, samples: int) -> float:
    if squared_error == 0:
        return math.inf
    return 10.0 * math.log10(255.0 * 255.0 / (squared_error / samples))


def write_header(stream, identity: bytes, payload: bytes, descriptors: list[tuple[int, int, int, int, int, int, int]]) -> None:
    header = bytearray(V3_HEADER_BYTES)
    struct.pack_into("<8s8I2f32s32s", header, 0, V3_MAGIC, VERSION, V3_HEADER_BYTES, TILE_SIZE, GUTTER,
                     MAXIMUM_LEVEL, TILE_COUNT, EXTENT, len(descriptors), -11000.0, 9000.0, identity, payload)
    for index, descriptor in enumerate(descriptors):
        semantic, fmt, color_space, maximum_lod, count, byte_count, offset = descriptor
        struct.pack_into("<6IQ", header, 112 + index * 32, semantic, fmt, color_space, maximum_lod,
                         count, byte_count, offset)
    stream.seek(0)
    stream.write(header)


def main() -> int:
    if np.__version__ != NUMPY_VERSION:
        raise RuntimeError(f"This deterministic converter requires NumPy {NUMPY_VERSION}; found {np.__version__}.")
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--manifest", required=True, type=Path)
    args = parser.parse_args()
    if not args.input.is_file():
        raise FileNotFoundError(args.input)

    descriptors: list[tuple[int, int, int, int, int, int, int]] = []
    offset = V3_HEADER_BYTES
    for semantic, fmt, color_space, maximum_lod, byte_count in CHANNELS:
        count = tile_count(maximum_lod)
        descriptors.append((semantic, fmt, color_space, maximum_lod, count, byte_count, offset))
        offset += count * byte_count

    quality = {codec: {name: [0, 0] for name in ("global", "coastline", "high_contrast", "desert", "ice_snow", "ocean")}
               for codec in ("bc1", "bc7")}
    mask_error = [0, 0]
    cloud_error = [0, 0]
    payload = hashlib.sha256()
    identity = hashlib.sha256()
    identity.update(V3_MAGIC)
    identity.update(struct.pack("<6I", VERSION, TILE_SIZE, GUTTER, MAXIMUM_LEVEL, TILE_COUNT, 2))
    identity.update(bytes.fromhex(sha256(args.input)))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.input.open("rb") as source, args.output.open("w+b") as target:
        v2_header = source.read(V2_HEADER_BYTES)
        if len(v2_header) != V2_HEADER_BYTES or v2_header[:8] != V2_MAGIC or struct.unpack_from("<I", v2_header, 8)[0] != 2:
            raise ValueError("Input is not a NovaCore Earth SVT v2 pack.")
        target.truncate(offset)
        for tile in range(TILE_COUNT):
            source.seek(V2_HEADER_BYTES + tile * V2_RECORD_BYTES)
            record = source.read(V2_RECORD_BYTES)
            if len(record) != V2_RECORD_BYTES:
                raise EOFError(f"Short v2 record {tile}.")
            rgba = np.frombuffer(record, np.uint8, V2_ALBEDO_BYTES).reshape(EXTENT, EXTENT, 4)
            elevation_start = V2_ALBEDO_BYTES
            elevation = record[elevation_start:elevation_start + R16_TILE_BYTES]
            cloud = np.frombuffer(record, np.uint8, EXTENT * EXTENT, elevation_start + R16_TILE_BYTES).reshape(EXTENT, EXTENT)
            _, decoded_bc1 = encode_bc1(rgba[:, :, :3])
            albedo_bytes, decoded_rgb = encode_bc7_mode6(rgba[:, :, :3])
            mask_bytes, decoded_mask = encode_bc4(rgba[:, :, 3])
            channel_payloads = (albedo_bytes, elevation, mask_bytes)
            if tile < tile_count(2):
                cloud_bytes, decoded_cloud = encode_bc4(cloud)
                channel_payloads += (cloud_bytes,)
                delta = cloud.astype(np.int32) - decoded_cloud.astype(np.int32)
                cloud_error[0] += int((delta * delta).sum()); cloud_error[1] += delta.size

            masks = {
                "global": np.ones((EXTENT, EXTENT), dtype=bool),
                "coastline": np.repeat(np.repeat((blocks(rgba[:, :, 3])[:, :, 0].min(1) != blocks(rgba[:, :, 3])[:, :, 0].max(1)).reshape(65, 65), 4, 0), 4, 1),
                "high_contrast": np.repeat(np.repeat(((blocks(rgba[:, :, :3]).max((1, 2)) - blocks(rgba[:, :, :3]).min((1, 2))) > 96).reshape(65, 65), 4, 0), 4, 1),
                "desert": (rgba[:, :, 3] > 127) & (rgba[:, :, 0] > rgba[:, :, 2] * 1.15) & (rgba[:, :, 0] > 95),
                "ice_snow": (rgba[:, :, 3] > 127) & (rgba[:, :, :3].min(2) > 175),
                "ocean": rgba[:, :, 3] < 128,
            }
            for codec, decoded in (("bc1", decoded_bc1), ("bc7", decoded_rgb)):
                delta = rgba[:, :, :3].astype(np.int32) - decoded.astype(np.int32)
                for name, selected in masks.items():
                    values = delta[selected]
                    quality[codec][name][0] += int((values * values).sum()); quality[codec][name][1] += values.size
            mask_delta = rgba[:, :, 3].astype(np.int32) - decoded_mask.astype(np.int32)
            mask_error[0] += int((mask_delta * mask_delta).sum()); mask_error[1] += mask_delta.size

            for descriptor, data in zip(descriptors[:len(channel_payloads)], channel_payloads, strict=True):
                channel_offset = descriptor[6] + tile * descriptor[5]
                target.seek(channel_offset); target.write(data)
        payload = hashlib.sha256()
        target.seek(V3_HEADER_BYTES)
        while block := target.read(1024 * 1024):
            payload.update(block)
        write_header(target, identity.digest(), payload.digest(), descriptors)

    quality_report = {codec: {name: {"rmse": math.sqrt(error / count), "psnrDb": psnr(error, count), "samples": count}
                              for name, (error, count) in categories.items() if count}
                      for codec, categories in quality.items()}
    quality_report["coastlineMaskBc4"] = {"rmse": math.sqrt(mask_error[0] / mask_error[1]), "psnrDb": psnr(*mask_error)}
    quality_report["cloudBc4"] = {"rmse": math.sqrt(cloud_error[0] / cloud_error[1]), "psnrDb": psnr(*cloud_error)}
    manifest = {
        "schema": "NovaCore.EarthVirtualTexture/3",
        "pack": args.output.name,
        "packBytes": args.output.stat().st_size,
        "packSha256": sha256(args.output),
        "identitySha256": identity.hexdigest(),
        "payloadSha256": payload.hexdigest(),
        "tile": {"logical": TILE_SIZE, "gutter": GUTTER, "physical": EXTENT, "blockAligned": True},
        "channels": [
            {"semantic": name, "format": fmt_name, "colorSpace": color, "maximumLevel": descriptor[3],
             "tileCount": descriptor[4], "tileBytes": descriptor[5], "sectionOffset": descriptor[6]}
            for name, fmt_name, color, descriptor in zip(
                ("Albedo", "Elevation", "LandMask", "Cloud"),
                ("BC7_SRGB", "R16_UNORM", "BC4_UNORM", "BC4_UNORM"),
                ("SRGB", "Linear", "Linear", "Linear"), descriptors, strict=True)
        ],
        "quality": quality_report,
        "sourcePack": {"name": "earth_surface_v2.ncvtex", "sha256": sha256(args.input)},
        "encoder": {"name": "NovaCore deterministic NumPy BC1/BC4/BC7-mode6", "version": 1,
                    "numpyVersion": NUMPY_VERSION, "runtimeDependency": False},
    }
    args.manifest.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
