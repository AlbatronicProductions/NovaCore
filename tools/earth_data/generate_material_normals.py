#!/usr/bin/env python3
"""Generate NovaCore's deterministic, repository-authored Earth material BC5 array."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
from pathlib import Path

import numpy as np


MAGIC = b"NCNRM01\0"
VERSION = 1
HEADER_BYTES = 256
SIZE = 1024
FAMILIES = ("arid", "temperate", "rock", "snow_ice", "fallback")
TILE_SCALES_METRES = (3.5, 3.0, 2.5, 4.5, 4.0)


def blocks(channel: np.ndarray) -> np.ndarray:
    height, width = channel.shape
    padded_height = max(4, (height + 3) & ~3)
    padded_width = max(4, (width + 3) & ~3)
    padded = np.pad(channel, ((0, padded_height - height), (0, padded_width - width)), mode="wrap")
    return padded.reshape(padded_height // 4, 4, padded_width // 4, 4).transpose(0, 2, 1, 3).reshape(-1, 16)


def encode_bc4(channel: np.ndarray) -> np.ndarray:
    source = blocks(channel).astype(np.int32)
    endpoint0 = source.max(axis=1).astype(np.uint8)
    endpoint1 = source.min(axis=1).astype(np.uint8)
    palette = np.empty((source.shape[0], 8), dtype=np.int32)
    palette[:, 0], palette[:, 1] = endpoint0, endpoint1
    for index in range(1, 7):
        palette[:, index + 1] = ((7 - index) * endpoint0.astype(np.int32) + index * endpoint1.astype(np.int32) + 3) // 7
    indices = np.abs(source[:, :, None] - palette[:, None, :]).argmin(axis=2).astype(np.uint64)
    packed = np.zeros(source.shape[0], dtype=np.uint64)
    for pixel in range(16):
        packed |= indices[:, pixel] << (3 * pixel)
    encoded = np.empty((source.shape[0], 8), dtype=np.uint8)
    encoded[:, 0], encoded[:, 1] = endpoint0, endpoint1
    encoded[:, 2:] = packed.astype("<u8").view(np.uint8).reshape(-1, 8)[:, :6]
    return encoded


def encode_bc5(red: np.ndarray, green: np.ndarray) -> bytes:
    red_blocks = encode_bc4(red)
    green_blocks = encode_bc4(green)
    return np.concatenate((red_blocks, green_blocks), axis=1).tobytes()


def periodic_height(layer: int) -> np.ndarray:
    axis = np.arange(SIZE, dtype=np.float64) * (2.0 * math.pi / SIZE)
    x, y = np.meshgrid(axis, axis)
    if layer == 0:  # wind-shaped arid granules
        return .58 * np.sin(17 * x + 5 * y) + .25 * np.sin(41 * x - 13 * y) + .12 * np.cos(89 * x + 31 * y)
    if layer == 1:  # aggregated temperate soil
        return .46 * np.sin(11 * x) * np.cos(13 * y) + .31 * np.sin(29 * x + 23 * y) + .18 * np.cos(61 * x - 37 * y)
    if layer == 2:  # angular exposed rock
        ridge = np.abs(np.sin(7 * x + 9 * y))
        return .62 * ridge + .34 * np.sin(23 * x - 17 * y) + .22 * np.cos(53 * x + 47 * y)
    if layer == 3:  # broad, subdued snow/ice undulation
        return .62 * np.sin(5 * x) * np.cos(7 * y) + .22 * np.sin(19 * x + 11 * y) + .08 * np.cos(43 * x - 29 * y)
    return .48 * np.sin(13 * x + 7 * y) + .29 * np.cos(31 * x - 19 * y) + .15 * np.sin(67 * x + 41 * y)


def encoded_normal(layer: int) -> np.ndarray:
    height = periodic_height(layer)
    strength = (2.1, 2.5, 3.8, 1.35, 2.2)[layer]
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * (.5 * strength)
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * (.5 * strength)
    length = np.sqrt(dx * dx + dy * dy + 1.0)
    encoded = np.empty((SIZE, SIZE, 2), dtype=np.uint8)
    encoded[:, :, 0] = np.clip(np.rint((-dx / length * .5 + .5) * 255.0), 0, 255).astype(np.uint8)
    encoded[:, :, 1] = np.clip(np.rint((-dy / length * .5 + .5) * 255.0), 0, 255).astype(np.uint8)
    return encoded


def downsample_normal(encoded: np.ndarray) -> np.ndarray:
    signed_xy = encoded.astype(np.float64) / 255.0 * 2.0 - 1.0
    z = np.sqrt(np.maximum(0.0, 1.0 - np.sum(signed_xy * signed_xy, axis=2)))
    normal = np.dstack((signed_xy, z))
    if normal.shape[0] == 1 and normal.shape[1] == 1:
        return encoded
    if normal.shape[0] & 1:
        normal = np.concatenate((normal, normal[:1, :, :]), axis=0)
    if normal.shape[1] & 1:
        normal = np.concatenate((normal, normal[:, :1, :]), axis=1)
    reduced = normal.reshape(normal.shape[0] // 2, 2, normal.shape[1] // 2, 2, 3).mean(axis=(1, 3))
    reduced /= np.maximum(np.linalg.norm(reduced, axis=2, keepdims=True), 1e-12)
    return np.clip(np.rint((reduced[:, :, :2] * .5 + .5) * 255.0), 0, 255).astype(np.uint8)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    args = parser.parse_args()

    layers = []
    mip_levels = int(math.log2(SIZE)) + 1
    for layer in range(len(FAMILIES)):
        normal = encoded_normal(layer)
        mips = []
        for _ in range(mip_levels):
            mips.append(encode_bc5(normal[:, :, 0], normal[:, :, 1]))
            normal = downsample_normal(normal)
        layers.append(b"".join(mips))
    payload = b"".join(layers)
    payload_hash = hashlib.sha256(payload).digest()
    identity_source = MAGIC + struct.pack("<IIII", VERSION, SIZE, SIZE, len(FAMILIES))
    identity_source += struct.pack("<5f", *TILE_SCALES_METRES) + payload_hash
    identity_hash = hashlib.sha256(identity_source).digest()

    header = bytearray(HEADER_BYTES)
    layer_bytes = len(layers[0])
    struct.pack_into("<8sIIIIIII", header, 0, MAGIC, VERSION, HEADER_BYTES, SIZE, SIZE, len(FAMILIES), layer_bytes, mip_levels)
    header[64:96] = identity_hash
    header[96:128] = payload_hash
    struct.pack_into("<5f", header, 128, *TILE_SCALES_METRES)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(header + payload)
    manifest = {
        "schema": "NovaCore.EarthMaterialNormals/1",
        "provenance": "Deterministic procedural normal maps generated by repository-owned code; no external source assets.",
        "format": "BC5_UNORM",
        "dimensions": [SIZE, SIZE],
        "mipLevels": mip_levels,
        "families": list(FAMILIES),
        "tileScalesMetres": list(TILE_SCALES_METRES),
        "payloadBytes": len(payload),
        "payloadSha256": payload_hash.hex(),
        "identitySha256": identity_hash.hex(),
        "runtimeBytes": HEADER_BYTES + len(payload),
    }
    args.manifest.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
