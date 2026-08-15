#!/usr/bin/env python3
"""Generate NovaCore's deterministic tileable Earth ground-material PBR pack."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
from pathlib import Path

import numpy as np


MAGIC = b"NCPBR01\0"
VERSION = 1
HEADER_BYTES = 256
SIZE = 1024
FAMILIES = ("arid", "temperate", "rock", "snow_ice", "fallback")
# These are intentionally identical to the registered 3F-3 BC5 normal scales.
TILE_SCALES_METRES = (3.5, 3.0, 2.5, 4.5, 4.0)
BASE_ALBEDO_SRGB = np.array(
    ((0.56, 0.39, 0.20), (0.23, 0.18, 0.10), (0.43, 0.42, 0.40),
     (0.84, 0.89, 0.95), (0.48, 0.47, 0.44)), dtype=np.float64)


def block_pixels(image: np.ndarray) -> np.ndarray:
    height, width = image.shape[:2]
    padded_height = max(4, (height + 3) & ~3)
    padded_width = max(4, (width + 3) & ~3)
    padding = ((0, padded_height - height), (0, padded_width - width))
    if image.ndim == 3:
        padded = np.pad(image, padding + ((0, 0),), mode="wrap")
        return padded.reshape(padded_height // 4, 4, padded_width // 4, 4, image.shape[2]).transpose(0, 2, 1, 3, 4).reshape(-1, 16, image.shape[2])
    padded = np.pad(image, padding, mode="wrap")
    return padded.reshape(padded_height // 4, 4, padded_width // 4, 4).transpose(0, 2, 1, 3).reshape(-1, 16)


def quantize_mode6_endpoint(values: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    expanded = np.concatenate((values.astype(np.int32), np.full((values.shape[0], 1), 255, dtype=np.int32)), axis=1)
    candidates = []
    for pbit in (0, 1):
        quantized = np.clip(np.rint((expanded - pbit) / 2.0), 0, 127).astype(np.uint64)
        decoded = (quantized.astype(np.int32) << 1) | pbit
        candidates.append((quantized, decoded, ((decoded - expanded) ** 2).sum(axis=1)))
    use_one = candidates[1][2] < candidates[0][2]
    return (np.where(use_one[:, None], candidates[1][0], candidates[0][0]),
            use_one.astype(np.uint64),
            np.where(use_one[:, None], candidates[1][1], candidates[0][1]))


def encode_bc7_mode6(rgb: np.ndarray) -> bytes:
    source_blocks = block_pixels(rgb).astype(np.int32)
    encoded_chunks: list[bytes] = []
    weights = np.array((0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64), dtype=np.int32)
    for start in range(0, source_blocks.shape[0], 4096):
        source = source_blocks[start:start + 4096]
        low = source.min(axis=1).astype(np.uint8)
        high = source.max(axis=1).astype(np.uint8)
        q0, p0, endpoint0 = quantize_mode6_endpoint(high)
        q1, p1, endpoint1 = quantize_mode6_endpoint(low)
        palette = ((64 - weights[None, :, None]) * endpoint0[:, None, :3] +
                   weights[None, :, None] * endpoint1[:, None, :3] + 32) >> 6
        distances = ((source[:, :, None, :] - palette[:, None, :, :]) ** 2).sum(axis=3)
        indices = distances.argmin(axis=2).astype(np.uint64)
        swap = indices[:, 0] >= 8
        old_q0, old_p0, old_endpoint0 = q0.copy(), p0.copy(), endpoint0.copy()
        q0[swap], p0[swap], endpoint0[swap] = q1[swap], p1[swap], endpoint1[swap]
        q1[swap], p1[swap], endpoint1[swap] = old_q0[swap], old_p0[swap], old_endpoint0[swap]
        indices[swap] = 15 - indices[swap]
        low_bits = np.full(source.shape[0], 1 << 6, dtype=np.uint64)
        for component, shift in ((0, 7), (1, 21), (2, 35), (3, 49)):
            low_bits |= q0[:, component] << shift
            low_bits |= q1[:, component] << (shift + 7)
        low_bits |= p0 << 63
        high_bits = p1.copy() | (indices[:, 0] << 1)
        for pixel in range(1, 16):
            high_bits |= indices[:, pixel] << (4 + (pixel - 1) * 4)
        encoded = np.concatenate((low_bits.astype("<u8").view(np.uint8).reshape(-1, 8),
                                  high_bits.astype("<u8").view(np.uint8).reshape(-1, 8)), axis=1)
        encoded_chunks.append(encoded.tobytes())
    return b"".join(encoded_chunks)


def encode_bc4(channel: np.ndarray) -> bytes:
    source_blocks = block_pixels(channel).astype(np.int32)
    encoded_chunks: list[bytes] = []
    for start in range(0, source_blocks.shape[0], 16384):
        source = source_blocks[start:start + 16384]
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
        encoded_chunks.append(encoded.tobytes())
    return b"".join(encoded_chunks)


def encode_bc5(red: np.ndarray, green: np.ndarray) -> bytes:
    red_blocks = np.frombuffer(encode_bc4(red), dtype=np.uint8).reshape(-1, 8)
    green_blocks = np.frombuffer(encode_bc4(green), dtype=np.uint8).reshape(-1, 8)
    return np.concatenate((red_blocks, green_blocks), axis=1).tobytes()


def periodic_noise(cells: int, seed: int) -> np.ndarray:
    coordinate = np.arange(SIZE, dtype=np.float64) * (cells / SIZE)
    base = np.floor(coordinate).astype(np.int64)
    fraction = coordinate - base
    fraction = fraction * fraction * (3.0 - 2.0 * fraction)
    x0, y0 = np.meshgrid(base % cells, base % cells)
    fx, fy = np.meshgrid(fraction, fraction)

    def hashed(x: np.ndarray, y: np.ndarray) -> np.ndarray:
        value = (x.astype(np.uint64) * np.uint64(0x9E3779B1) +
                 y.astype(np.uint64) * np.uint64(0x85EBCA77) + np.uint64(seed * 0xC2B2AE3D)) & np.uint64(0xFFFFFFFF)
        value ^= value >> np.uint64(16)
        value = (value * np.uint64(0x7FEB352D)) & np.uint64(0xFFFFFFFF)
        value ^= value >> np.uint64(15)
        return (value & np.uint64(0x00FFFFFF)).astype(np.float64) / 16777215.0

    x1, y1 = (x0 + 1) % cells, (y0 + 1) % cells
    a = hashed(x0, y0) * (1.0 - fx) + hashed(x1, y0) * fx
    b = hashed(x0, y1) * (1.0 - fx) + hashed(x1, y1) * fx
    return a * (1.0 - fy) + b * fy


def normal_source_height(layer: int) -> np.ndarray:
    axis = np.arange(SIZE, dtype=np.float64) * (2.0 * math.pi / SIZE)
    x, y = np.meshgrid(axis, axis)
    if layer == 0:
        return .58 * np.sin(17 * x + 5 * y) + .25 * np.sin(41 * x - 13 * y) + .12 * np.cos(89 * x + 31 * y)
    if layer == 1:
        return .46 * np.sin(11 * x) * np.cos(13 * y) + .31 * np.sin(29 * x + 23 * y) + .18 * np.cos(61 * x - 37 * y)
    if layer == 2:
        return .62 * np.abs(np.sin(7 * x + 9 * y)) + .34 * np.sin(23 * x - 17 * y) + .22 * np.cos(53 * x + 47 * y)
    if layer == 3:
        return .62 * np.sin(5 * x) * np.cos(7 * y) + .22 * np.sin(19 * x + 11 * y) + .08 * np.cos(43 * x - 29 * y)
    return .48 * np.sin(13 * x + 7 * y) + .29 * np.cos(31 * x - 19 * y) + .15 * np.sin(67 * x + 41 * y)


def material_fields(layer: int) -> tuple[np.ndarray, np.ndarray]:
    broad = periodic_noise((17, 19, 13, 11, 23)[layer], 101 + layer * 17)
    aggregate = periodic_noise((47, 41, 31, 29, 53)[layer], 307 + layer * 31)
    grain = periodic_noise((137, 113, 89, 73, 127)[layer], 701 + layer * 43)
    source_height = normal_source_height(layer)
    source_height = (source_height - source_height.min()) / max(source_height.max() - source_height.min(), 1e-12)

    if layer == 0:
        mineral = np.clip(.54 * broad + .30 * aggregate + .16 * grain, 0.0, 1.0)
        color_delta = np.dstack((.22 * (mineral - .5), .10 * (broad - .5), -.12 * (grain - .5)))
        roughness = .69 + .18 * aggregate + .06 * grain
    elif layer == 1:
        organic = np.clip(.48 * broad + .34 * aggregate + .18 * grain, 0.0, 1.0)
        color_delta = np.dstack((.10 * (organic - .5), .15 * (aggregate - .5), -.08 * (grain - .5)))
        roughness = .77 + .15 * aggregate + .05 * grain
    elif layer == 2:
        fracture = 1.0 - np.abs(2.0 * aggregate - 1.0)
        mineral = np.clip(.52 * broad + .30 * fracture + .18 * grain, 0.0, 1.0)
        color_delta = np.dstack((.14 * (mineral - .5), .11 * (broad - .5), .09 * (grain - .5)))
        roughness = .64 + .25 * (1.0 - fracture) + .05 * grain
    elif layer == 3:
        ice = np.clip(.62 * broad + .26 * aggregate + .12 * grain, 0.0, 1.0)
        color_delta = np.dstack((.055 * (ice - .5), .075 * (ice - .5), .13 * (aggregate - .5)))
        roughness = .43 + .25 * broad + .08 * grain
    else:
        neutral = np.clip(.55 * broad + .30 * aggregate + .15 * grain, 0.0, 1.0)
        color_delta = np.dstack((.08 * (neutral - .5), .075 * (aggregate - .5), .07 * (grain - .5)))
        roughness = .72 + .15 * aggregate + .05 * grain

    albedo_srgb = np.clip(BASE_ALBEDO_SRGB[layer][None, None, :] + color_delta, .035, .97)
    albedo_linear = np.where(albedo_srgb <= .04045, albedo_srgb / 12.92, ((albedo_srgb + .055) / 1.055) ** 2.4)
    micro_height = np.clip(.58 * source_height + .27 * aggregate + .15 * grain, 0.0, 1.0)
    surface = np.dstack((np.clip(roughness, .30, .98), micro_height))
    return albedo_linear, surface


def linear_to_srgb_bytes(linear: np.ndarray) -> np.ndarray:
    srgb = np.where(linear <= .0031308, linear * 12.92, 1.055 * np.power(linear, 1.0 / 2.4) - .055)
    return np.clip(np.rint(srgb * 255.0), 0, 255).astype(np.uint8)


def downsample(image: np.ndarray) -> np.ndarray:
    if image.shape[0] == 1 and image.shape[1] == 1:
        return image
    padded = image
    if padded.shape[0] & 1:
        padded = np.concatenate((padded, padded[:1]), axis=0)
    if padded.shape[1] & 1:
        padded = np.concatenate((padded, padded[:, :1]), axis=1)
    return padded.reshape(padded.shape[0] // 2, 2, padded.shape[1] // 2, 2, padded.shape[2]).mean(axis=(1, 3))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    args = parser.parse_args()

    mip_levels = int(math.log2(SIZE)) + 1
    albedo_layers: list[bytes] = []
    surface_layers: list[bytes] = []
    for layer in range(len(FAMILIES)):
        albedo, surface = material_fields(layer)
        albedo_mips: list[bytes] = []
        surface_mips: list[bytes] = []
        for _ in range(mip_levels):
            albedo_mips.append(encode_bc7_mode6(linear_to_srgb_bytes(albedo)))
            surface_bytes = np.clip(np.rint(surface * 255.0), 0, 255).astype(np.uint8)
            surface_mips.append(encode_bc5(surface_bytes[:, :, 0], surface_bytes[:, :, 1]))
            albedo = downsample(albedo)
            surface = downsample(surface)
        albedo_layers.append(b"".join(albedo_mips))
        surface_layers.append(b"".join(surface_mips))

    albedo_payload = b"".join(albedo_layers)
    surface_payload = b"".join(surface_layers)
    albedo_hash = hashlib.sha256(albedo_payload).digest()
    surface_hash = hashlib.sha256(surface_payload).digest()
    identity_source = MAGIC + struct.pack("<7I", VERSION, SIZE, SIZE, len(FAMILIES), mip_levels,
                                          len(albedo_layers[0]), len(surface_layers[0]))
    identity_source += struct.pack("<5f", *TILE_SCALES_METRES) + albedo_hash + surface_hash
    identity_hash = hashlib.sha256(identity_source).digest()

    header = bytearray(HEADER_BYTES)
    struct.pack_into("<8s10I2Q", header, 0, MAGIC, VERSION, HEADER_BYTES, SIZE, SIZE, len(FAMILIES), mip_levels,
                     len(albedo_layers[0]), len(surface_layers[0]), len(albedo_payload), len(surface_payload),
                     HEADER_BYTES, HEADER_BYTES + len(albedo_payload))
    header[64:96] = identity_hash
    header[96:128] = albedo_hash
    header[128:160] = surface_hash
    struct.pack_into("<5f", header, 160, *TILE_SCALES_METRES)
    struct.pack_into("<5I", header, 180, *range(len(FAMILIES)))
    struct.pack_into("<2I", header, 200, 1, 2)  # BC7_SRGB, BC5_UNORM

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(header + albedo_payload + surface_payload)
    manifest = {
        "schema": "NovaCore.EarthMaterialPbr/1",
        "provenance": "Deterministic procedural PBR material arrays generated by repository-owned code; no external source assets.",
        "formats": {"albedo": "BC7_SRGB", "roughnessMicroHeight": "BC5_UNORM"},
        "dimensions": [SIZE, SIZE],
        "mipLevels": mip_levels,
        "families": list(FAMILIES),
        "tileScalesMetres": list(TILE_SCALES_METRES),
        "payloadBytes": {"albedo": len(albedo_payload), "roughnessMicroHeight": len(surface_payload)},
        "payloadSha256": {"albedo": albedo_hash.hex(), "roughnessMicroHeight": surface_hash.hex()},
        "identitySha256": identity_hash.hex(),
        "runtimeBytes": HEADER_BYTES + len(albedo_payload) + len(surface_payload),
        "mipPolicy": "Full linear-light normalized hierarchy; repeat sampling with anisotropic filtering.",
    }
    args.manifest.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
