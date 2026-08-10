#!/usr/bin/env python3
"""Build and verify a bounded NovaCore regional planetary data pack.

This tool is offline by design.  It reads already-acquired scientific rasters,
validates their recorded provenance and checksums, reprojects them into the
existing Earth SVT body-fixed page grid, and emits a deterministic sparse pack.
No scientific raster parser or network client is linked into the runtime.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
import sys
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "earth_data"))

SCHEMA = "NovaCore.PlanetaryRegionalPack/1"
MAGIC = b"NCREGN1\0"
VERSION = 1
HEADER_BYTES = 256
TILE_SIZE = 256
GUTTER = 2
EXTENT = 260
MINIMUM_LEVEL = 5
MAXIMUM_LEVEL = 12
BODY_ID = 6
HASH_CAPACITY = 512
ELEVATION_MINIMUM = -11_000.0
ELEVATION_MAXIMUM = 9_000.0
RECORD_BYTES = 16
BC7_TILE_BYTES = (EXTENT // 4) ** 2 * 16
BC4_TILE_BYTES = (EXTENT // 4) ** 2 * 8
R16_TILE_BYTES = EXTENT * EXTENT * 2
RAW_BUDGET_BYTES = 128 * 1024 * 1024
PACK_BUDGET_BYTES = 64 * 1024 * 1024
PAGE_BUDGET = 256
AREA_BUDGET_KM2 = 1_000.0
EXPECTED_NUMPY = "2.3.5"


def load_build_dependencies() -> None:
    """Load scientific/encoder dependencies only for a build, not verification."""
    global np, rasterio, Image, from_bounds, Resampling, reproject, encode_bc4, encode_bc7_mode6
    import numpy as np
    import rasterio
    from PIL import Image
    from rasterio.transform import from_bounds
    from rasterio.warp import Resampling, reproject
    from upgrade_earth_pack_v3 import encode_bc4, encode_bc7_mode6


@dataclass(frozen=True, order=True)
class Page:
    level: int
    y: int
    x: int


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest()


def page_bounds(page: Page, gutter: bool) -> tuple[float, float, float, float]:
    width = 360.0 / (1 << (page.level + 1))
    height = 180.0 / (1 << page.level)
    west = -180.0 + page.x * width
    east = west + width
    north = 90.0 - page.y * height
    south = north - height
    if gutter:
        west -= GUTTER * width / TILE_SIZE
        east += GUTTER * width / TILE_SIZE
        south -= GUTTER * height / TILE_SIZE
        north += GUTTER * height / TILE_SIZE
    return west, south, east, north


def pages_for_bounds(bounds: dict[str, float]) -> list[Page]:
    result: list[Page] = []
    west, south, east, north = (bounds[k] for k in ("west", "south", "east", "north"))
    if not (-180.0 <= west < east <= 180.0 and -90.0 <= south < north <= 90.0):
        raise ValueError("This bounded proof must not cross the antimeridian or a pole.")
    for level in range(MINIMUM_LEVEL, MAXIMUM_LEVEL + 1):
        count_x, count_y = 1 << (level + 1), 1 << level
        x0 = max(0, int(math.floor((west + 180.0) / 360.0 * count_x)))
        x1 = min(count_x - 1, int(math.floor(math.nextafter((east + 180.0) / 360.0 * count_x, -math.inf))))
        y0 = max(0, int(math.floor((90.0 - north) / 180.0 * count_y)))
        y1 = min(count_y - 1, int(math.floor(math.nextafter((90.0 - south) / 180.0 * count_y, -math.inf))))
        result.extend(Page(level, y, x) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1))
    return result


def validate_source(source_dir: Path, definition: dict) -> tuple[Path, Path]:
    if definition.get("schema") != "NovaCore.PlanetaryRegionalSource/1":
        raise ValueError("Unsupported source schema.")
    by_role = {item["role"]: item for item in definition["sources"]}
    if set(by_role) != {"albedo", "elevation"}:
        raise ValueError("Exactly one albedo and one elevation source are required.")
    paths: dict[str, Path] = {}
    total = 0
    for role, item in by_role.items():
        path = source_dir / item["file"]
        if not path.is_file():
            raise FileNotFoundError(path)
        size = path.stat().st_size
        total += size
        if size != item["bytes"] or sha256(path) != item["sha256"]:
            raise ValueError(f"{role} source checksum/size differs from recorded provenance.")
        paths[role] = path
    if total > RAW_BUDGET_BYTES:
        raise ValueError(f"Raw source budget exceeded: {total} > {RAW_BUDGET_BYTES} bytes.")
    if definition["region"]["approximateAreaSquareKilometres"] > AREA_BUDGET_KM2:
        raise ValueError("Regional area budget exceeded.")
    return paths["albedo"], paths["elevation"]


def reproject_page(dataset, page: Page, bands: tuple[int, ...], dtype, resampling: Resampling) -> tuple[np.ndarray, np.ndarray]:
    west, south, east, north = page_bounds(page, True)
    transform = from_bounds(west, south, east, north, EXTENT, EXTENT)
    target = np.zeros((len(bands), EXTENT, EXTENT), dtype=dtype)
    for target_index, source_band in enumerate(bands):
        reproject(
            source=rasterio.band(dataset, source_band), destination=target[target_index],
            src_transform=dataset.transform, src_crs=dataset.crs,
            dst_transform=transform, dst_crs="EPSG:4326",
            src_nodata=dataset.nodata, dst_nodata=0,
            resampling=resampling, init_dest_nodata=True, num_threads=1,
        )
    source_validity = np.full((dataset.height, dataset.width), 255, dtype=np.uint8)
    validity = np.zeros((EXTENT, EXTENT), dtype=np.uint8)
    reproject(
        source=source_validity, destination=validity,
        src_transform=dataset.transform, src_crs=dataset.crs,
        dst_transform=transform, dst_crs="EPSG:4326",
        src_nodata=0, dst_nodata=0, resampling=Resampling.nearest,
        init_dest_nodata=True, num_threads=1,
    )
    return target, validity


def encode_elevation(values: np.ndarray) -> tuple[bytes, float, float, int]:
    clipped = np.clip(values.astype(np.float64), ELEVATION_MINIMUM, ELEVATION_MAXIMUM)
    encoded = np.rint((clipped - ELEVATION_MINIMUM) / (ELEVATION_MAXIMUM - ELEVATION_MINIMUM) * 65535.0).astype("<u2")
    decoded = ELEVATION_MINIMUM + encoded.astype(np.float64) / 65535.0 * (ELEVATION_MAXIMUM - ELEVATION_MINIMUM)
    difference = decoded - clipped
    return encoded.tobytes(), float(np.max(np.abs(difference))), float(np.sqrt(np.mean(difference * difference))), int(np.count_nonzero(values != clipped))


def quality_psnr(source: np.ndarray, decoded: np.ndarray, valid: np.ndarray) -> tuple[int, int]:
    mask = valid > 0
    if not np.any(mask):
        return 0, 0
    delta = source.transpose(1, 2, 0)[mask].astype(np.int32) - decoded[mask].astype(np.int32)
    return int(np.sum(delta * delta)), int(delta.size)


def pack_header(page_count: int, bounds: dict[str, float], identity: bytes, payload: bytes,
                descriptors: list[tuple[int, int, int, int, int, int, int]]) -> bytes:
    header = bytearray(HEADER_BYTES)
    struct.pack_into(
        "<8s12I2f4d32s32s", header, 0, MAGIC, VERSION, HEADER_BYTES, TILE_SIZE, GUTTER,
        EXTENT, MINIMUM_LEVEL, MAXIMUM_LEVEL, page_count, len(descriptors), HASH_CAPACITY,
        BODY_ID, 0, ELEVATION_MINIMUM, ELEVATION_MAXIMUM,
        bounds["west"], bounds["south"], bounds["east"], bounds["north"], identity, payload,
    )
    for index, descriptor in enumerate(descriptors):
        struct.pack_into("<6IQ", header, 160 + index * 32, *descriptor)
    return bytes(header)


def verify_pack(path: Path, expected_sha256: str | None = None) -> dict:
    data = path.read_bytes()
    if len(data) < HEADER_BYTES or data[:8] != MAGIC:
        raise ValueError("Regional pack magic/header is invalid.")
    fields = struct.unpack_from("<12I2f4d32s32s", data, 8)
    version, header_bytes, tile_size, gutter, extent, minimum_level, maximum_level, page_count, channel_count, hash_capacity, body_low, body_high = fields[:12]
    if (version, header_bytes, tile_size, gutter, extent, minimum_level, maximum_level, channel_count, hash_capacity, body_low, body_high) != (VERSION, HEADER_BYTES, TILE_SIZE, GUTTER, EXTENT, MINIMUM_LEVEL, MAXIMUM_LEVEL, 3, HASH_CAPACITY, BODY_ID, 0):
        raise ValueError("Regional pack contract mismatch.")
    payload_hash = hashlib.sha256()
    channel_summaries = []
    for channel in range(channel_count):
        semantic, fmt, color, maximum, count, tile_bytes, offset = struct.unpack_from("<6IQ", data, 160 + channel * 32)
        if not minimum_level <= maximum <= maximum_level or count < 1 or count > page_count or offset + count * RECORD_BYTES + count * tile_bytes > len(data):
            raise ValueError("Regional channel descriptor is out of range.")
        records_end = offset + count * RECORD_BYTES
        records = []
        previous: Page | None = None
        for index in range(count):
            level, x, y, reserved = struct.unpack_from("<4I", data, offset + index * RECORD_BYTES)
            page = Page(level, y, x)
            if reserved != 0 or level < minimum_level or level > maximum or x >= 1 << (level + 1) or y >= 1 << level or (previous is not None and page <= previous):
                raise ValueError("Regional page records are not canonical.")
            previous = page
            records.append(page)
        if len(set(records)) != count:
            raise ValueError("Duplicate regional page record.")
        payload_hash.update(data[offset:records_end + count * tile_bytes])
        channel_summaries.append({"semantic": semantic, "format": fmt, "colorSpace": color, "maximumLevel": maximum, "pageCount": count, "tileBytes": tile_bytes, "offset": offset})
    if payload_hash.digest() != fields[-1]:
        raise ValueError("Regional payload hash mismatch.")
    actual = hashlib.sha256(data).hexdigest()
    if expected_sha256 is not None and actual != expected_sha256:
        raise ValueError("Regional pack file hash mismatch.")
    return {"bytes": len(data), "sha256": actual, "identitySha256": fields[-2].hex(), "payloadSha256": fields[-1].hex(), "pageCount": page_count, "channels": channel_summaries}


def build(args: argparse.Namespace) -> dict:
    if np.__version__ != EXPECTED_NUMPY:
        raise RuntimeError(f"Deterministic ingestion requires NumPy {EXPECTED_NUMPY}; found {np.__version__}.")
    source_dir = args.source.resolve()
    definition = json.loads((source_dir / "source.json").read_text(encoding="utf-8"))
    albedo_path, elevation_path = validate_source(source_dir, definition)
    bounds = definition["region"]["boundsDegrees"]
    pages = pages_for_bounds(bounds)
    if len(pages) > PAGE_BUDGET:
        raise ValueError(f"Regional page budget exceeded: {len(pages)} > {PAGE_BUDGET}.")
    estimated = HEADER_BYTES + 3 * len(pages) * RECORD_BYTES + len(pages) * (BC7_TILE_BYTES + R16_TILE_BYTES + BC4_TILE_BYTES)
    if estimated > PACK_BUDGET_BYTES:
        raise ValueError(f"Regional pack budget exceeded before build: {estimated} > {PACK_BUDGET_BYTES} bytes.")

    albedo_payloads: list[bytes] = []
    elevation_payloads: list[bytes] = []
    validity_payloads: list[bytes] = []
    albedo_error = [0, 0]
    elevation_max_error = 0.0
    elevation_squared_error = 0.0
    elevation_samples = 0
    clipped_samples = 0
    with rasterio.open(albedo_path) as albedo_source, rasterio.open(elevation_path) as elevation_source:
        if albedo_source.count < 3 or elevation_source.count != 1:
            raise ValueError("Unexpected source band contract.")
        for page in pages:
            rgb, albedo_valid = reproject_page(albedo_source, page, (1, 2, 3), np.uint8, Resampling.lanczos)
            elevation, elevation_valid = reproject_page(elevation_source, page, (1,), np.float32, Resampling.bilinear)
            valid = np.minimum(albedo_valid, elevation_valid)
            albedo_bytes, decoded = encode_bc7_mode6(rgb.transpose(1, 2, 0))
            error, samples = quality_psnr(rgb, decoded, valid)
            albedo_error[0] += error
            albedo_error[1] += samples
            elevation_bytes, maximum_error, rms_error, clipped = encode_elevation(elevation[0])
            valid_samples = int(np.count_nonzero(valid))
            elevation_max_error = max(elevation_max_error, maximum_error)
            elevation_squared_error += rms_error * rms_error * EXTENT * EXTENT
            elevation_samples += EXTENT * EXTENT
            clipped_samples += clipped
            validity_bytes, _ = encode_bc4(valid)
            albedo_payloads.append(albedo_bytes)
            elevation_payloads.append(elevation_bytes)
            validity_payloads.append(validity_bytes)

    records = b"".join(struct.pack("<4I", page.level, page.x, page.y, 0) for page in pages)
    sections = []
    descriptors = []
    offset = HEADER_BYTES
    for semantic, fmt, color, tile_bytes, payloads in (
        (1, 4, 1, BC7_TILE_BYTES, albedo_payloads),
        (2, 2, 0, R16_TILE_BYTES, elevation_payloads),
        (7, 3, 0, BC4_TILE_BYTES, validity_payloads),
    ):
        section = records + b"".join(payloads)
        descriptors.append((semantic, fmt, color, MAXIMUM_LEVEL, len(pages), tile_bytes, offset))
        sections.append(section)
        offset += len(section)
    payload_digest = hashlib.sha256(b"".join(sections)).digest()
    identity_material = json.dumps({
        "schema": SCHEMA, "source": definition, "levels": [MINIMUM_LEVEL, MAXIMUM_LEVEL],
        "tile": [TILE_SIZE, GUTTER, EXTENT], "pages": [[p.level, p.x, p.y] for p in pages],
        "formats": ["BC7_SRGB", "R16_UNORM", "BC4_UNORM_VALIDITY"],
    }, sort_keys=True, separators=(",", ":")).encode("utf-8")
    identity_digest = hashlib.sha256(identity_material).digest()
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(pack_header(len(pages), bounds, identity_digest, payload_digest, descriptors) + b"".join(sections))
    if output.stat().st_size > PACK_BUDGET_BYTES:
        raise ValueError("Built pack exceeds regional pack budget.")
    verification = verify_pack(output)

    with rasterio.open(albedo_path) as source:
        preview = source.read((1, 2, 3), out_shape=(3, 512, 512), resampling=Resampling.bilinear).transpose(1, 2, 0)
    args.preview.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(preview).save(args.preview, optimize=True)
    psnr = math.inf if albedo_error[0] == 0 else 10.0 * math.log10(255.0 * 255.0 / (albedo_error[0] / albedo_error[1]))
    manifest = {
        "schema": SCHEMA,
        "region": definition["region"],
        "bodyId": BODY_ID,
        "sourceManifest": str((source_dir / "source.json").relative_to(ROOT)).replace("\\", "/"),
        "pack": output.name,
        "packBytes": verification["bytes"],
        "packSha256": verification["sha256"],
        "identitySha256": verification["identitySha256"],
        "payloadSha256": verification["payloadSha256"],
        "tileContract": {"logicalSize": TILE_SIZE, "gutter": GUTTER, "physicalExtent": EXTENT, "minimumLevel": MINIMUM_LEVEL, "maximumLevel": MAXIMUM_LEVEL, "pageCount": len(pages), "order": "level,y,x"},
        "channels": verification["channels"],
        "quality": {"albedoBc7PsnrDb": psnr, "elevationR16MaximumErrorMetres": elevation_max_error, "elevationR16RmsErrorMetres": math.sqrt(elevation_squared_error / elevation_samples), "elevationClippedSamples": clipped_samples},
        "budgets": {"rawBytes": sum((source_dir / item["file"]).stat().st_size for item in definition["sources"]), "rawBudgetBytes": RAW_BUDGET_BYTES, "packBudgetBytes": PACK_BUDGET_BYTES, "pageBudget": PAGE_BUDGET, "areaBudgetSquareKilometres": AREA_BUDGET_KM2},
        "fallback": "regional requested page -> regional parent -> global requested/parent/root",
        "determinism": {"numpy": EXPECTED_NUMPY, "rasterio": rasterio.__version__, "resamplingThreads": 1, "encoder": "NovaCore deterministic NumPy BC7-mode6/BC4"},
    }
    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    args.manifest.write_text(json.dumps(manifest, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    args.index.parent.mkdir(parents=True, exist_ok=True)
    args.index.write_text(f"{output.name} {verification['sha256']}\n", encoding="ascii")
    return manifest


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=ROOT / "assets/earth/source/regional/mount_st_helens")
    parser.add_argument("--output", type=Path, default=ROOT / "assets/earth/runtime/regions/mount_st_helens_v1.ncvreg")
    parser.add_argument("--manifest", type=Path, default=ROOT / "assets/earth/runtime/regions/mount_st_helens_v1.manifest.json")
    parser.add_argument("--index", type=Path, default=ROOT / "assets/earth/runtime/regions/earth_regions.index")
    parser.add_argument("--preview", type=Path, default=ROOT / "assets/earth/source/regional/mount_st_helens/preview.png")
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()
    if args.verify_only:
        expected = None
        if args.manifest.is_file():
            expected = json.loads(args.manifest.read_text(encoding="utf-8"))["packSha256"]
        print(json.dumps(verify_pack(args.output, expected), indent=2))
    else:
        load_build_dependencies()
        print(json.dumps(build(args), indent=2, allow_nan=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
