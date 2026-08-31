#!/usr/bin/env python3
"""Build sparse NCCUBE2 regional terrain refinements in GPU-native formats."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
from pathlib import Path

import numpy as np
from PIL import Image

# The tracked, hash-provenanced NASA Blue Marble source is intentionally 21.6K.
Image.MAX_IMAGE_PIXELS = None

MAGIC = b"NCCUBE2\0"
LEGACY_VERSION = 2
VERSION = 3
HEADER_BYTES = 256
RECORD_HEADER_BYTES = 128
INTERIOR = 256
GUTTER = 4
EXTENT = INTERIOR + GUTTER * 2
BLOCK_COUNT = (EXTENT // 4) ** 2
BODY_ID = 6
TERRAIN_VERSION = 5
PAYLOAD_VERSION = 3
DETAIL_FREQUENCY = 1
RESIDUAL_MINIMUM = -512.0
RESIDUAL_MAXIMUM = 512.0
FLORIDA_LAUNCH_LATITUDE = 28.6084
FLORIDA_LAUNCH_LONGITUDE = -80.6042
FLORIDA_RESERVATION_RADIUS_METRES = 275.0
FLORIDA_BOUNDARY_FEATHER_METRES = 4000.0
FLORIDA_RESIDUAL_MINIMUM_METRES = -128.0
# Span is exactly 65,535 / 256 metres, so zero is represented exactly at
# code 32,768 while retaining a 1/256-metre quantization step.
FLORIDA_RESIDUAL_MAXIMUM_METRES = 127.99609375

CONTROL_OCEAN = 0
CONTROL_COASTAL = 1
CONTROL_BEACH = 2
CONTROL_WETLAND = 3
CONTROL_GRASS = 4
CONTROL_SCRUB = 5
CONTROL_FOREST = 6
CONTROL_DEVELOPED = 7
CONTROL_LAUNCH_RESERVATION = 8

SITES = (
    (-45.0, -70.0), (-30.0, -90.0), (-25.0, -70.0),
    (24.0, 12.0), (48.0, 12.0), (77.0, -42.0), (-27.0, 133.0),
)


def blocks(values: np.ndarray) -> np.ndarray:
    channels = 1 if values.ndim == 2 else values.shape[2]
    reshaped = values.reshape(EXTENT // 4, 4, EXTENT // 4, 4, channels)
    return reshaped.transpose(0, 2, 1, 3, 4).reshape(BLOCK_COUNT, 16, channels)


def encode_bc4(channel: np.ndarray) -> tuple[bytes, np.ndarray]:
    source = blocks(channel)[:, :, 0].astype(np.int32)
    endpoint0 = source.max(axis=1).astype(np.uint8)
    endpoint1 = source.min(axis=1).astype(np.uint8)
    palette = np.empty((BLOCK_COUNT, 8), dtype=np.int32)
    palette[:, 0], palette[:, 1] = endpoint0, endpoint1
    for index in range(1, 7):
        palette[:, index + 1] = ((7 - index) * endpoint0.astype(np.int32) + index * endpoint1.astype(np.int32) + 3) // 7
    distances = np.abs(source[:, :, None] - palette[:, None, :])
    indices = distances.argmin(axis=2).astype(np.uint64)
    packed = np.zeros(BLOCK_COUNT, dtype=np.uint64)
    for pixel in range(16):
        packed |= indices[:, pixel] << (3 * pixel)
    encoded = np.empty((BLOCK_COUNT, 8), dtype=np.uint8)
    encoded[:, 0], encoded[:, 1] = endpoint0, endpoint1
    encoded[:, 2:] = packed.astype("<u8").view(np.uint8).reshape(-1, 8)[:, :6]
    decoded_blocks = palette[np.arange(BLOCK_COUNT)[:, None], indices].astype(np.uint8)
    decoded = decoded_blocks.reshape(EXTENT // 4, EXTENT // 4, 4, 4).transpose(0, 2, 1, 3).reshape(EXTENT, EXTENT)
    return encoded.tobytes(), decoded


def quantize_mode6_endpoint(values: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    expanded = np.concatenate((values.astype(np.int32), np.full((values.shape[0], 1), 255, dtype=np.int32)), axis=1)
    candidates = []
    for pbit in (0, 1):
        quantized = np.clip(np.rint((expanded - pbit) / 2.0), 0, 127).astype(np.uint64)
        decoded = (quantized.astype(np.int32) << 1) | pbit
        candidates.append((quantized, decoded, ((decoded - expanded) ** 2).sum(axis=1)))
    use_one = candidates[1][2] < candidates[0][2]
    return (np.where(use_one[:, None], candidates[1][0], candidates[0][0]),
            use_one.astype(np.uint64), np.where(use_one[:, None], candidates[1][1], candidates[0][1]))


def encode_bc7_mode6(rgb: np.ndarray) -> tuple[bytes, np.ndarray]:
    source = blocks(rgb).astype(np.int32)
    low, high = source.min(axis=1).astype(np.uint8), source.max(axis=1).astype(np.uint8)
    q0, p0, endpoint0 = quantize_mode6_endpoint(high)
    q1, p1, endpoint1 = quantize_mode6_endpoint(low)
    weights = np.array((0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64), dtype=np.int32)
    palette = ((64 - weights[None, :, None]) * endpoint0[:, None, :3] + weights[None, :, None] * endpoint1[:, None, :3] + 32) >> 6
    indices = ((source[:, :, None, :] - palette[:, None, :, :]) ** 2).sum(axis=3).argmin(axis=2).astype(np.uint64)
    swap = indices[:, 0] >= 8
    old_q0, old_p0, old_endpoint0 = q0.copy(), p0.copy(), endpoint0.copy()
    q0[swap], p0[swap], endpoint0[swap] = q1[swap], p1[swap], endpoint1[swap]
    q1[swap], p1[swap], endpoint1[swap] = old_q0[swap], old_p0[swap], old_endpoint0[swap]
    indices[swap] = 15 - indices[swap]
    palette = ((64 - weights[None, :, None]) * endpoint0[:, None, :3] + weights[None, :, None] * endpoint1[:, None, :3] + 32) >> 6
    low_bits = np.full(BLOCK_COUNT, 1 << 6, dtype=np.uint64)
    for shift, value in ((7, q0[:, 0]), (14, q1[:, 0]), (21, q0[:, 1]), (28, q1[:, 1]),
                         (35, q0[:, 2]), (42, q1[:, 2]), (49, q0[:, 3]), (56, q1[:, 3])):
        low_bits |= value << shift
    low_bits |= p0 << 63
    high_bits = p1.copy() | (indices[:, 0] << 1)
    for pixel in range(1, 16):
        high_bits |= indices[:, pixel] << (4 + (pixel - 1) * 4)
    encoded = np.concatenate((low_bits.astype("<u8").view(np.uint8).reshape(-1, 8),
                              high_bits.astype("<u8").view(np.uint8).reshape(-1, 8)), axis=1)
    decoded_blocks = palette[np.arange(BLOCK_COUNT)[:, None], indices].astype(np.uint8)
    decoded = decoded_blocks.reshape(EXTENT // 4, EXTENT // 4, 4, 4, 3).transpose(0, 2, 1, 3, 4).reshape(EXTENT, EXTENT, 3)
    return encoded.tobytes(), decoded


def packbits(source: bytes) -> bytes:
    output = bytearray(); literal = bytearray(); index = 0
    def flush() -> None:
        nonlocal literal
        while literal:
            count = min(128, len(literal)); output.append(count - 1); output.extend(literal[:count]); del literal[:count]
    while index < len(source):
        run = 1
        while index + run < len(source) and source[index + run] == source[index] and run < 130:
            run += 1
        if run >= 3:
            flush(); output.append(0x80 | (run - 3)); output.append(source[index]); index += run
        else:
            literal.append(source[index]); index += 1
            if len(literal) == 128: flush()
    flush()
    return bytes(output)


def stored(payload: bytes) -> tuple[bytes, int]:
    packed = packbits(payload)
    return (packed, 1) if len(packed) < len(payload) else (payload, 0)


def cube_point(face: int, u: np.ndarray, v: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    a, b = 2.0 * u - 1.0, 2.0 * v - 1.0
    if face == 0: return np.ones_like(a), b, -a
    if face == 1: return -np.ones_like(a), b, a
    if face == 2: return a, np.ones_like(a), -b
    if face == 3: return a, -np.ones_like(a), b
    if face == 4: return a, b, np.ones_like(a)
    return -a, b, -np.ones_like(a)


def relaxed_direction(face: int, u: np.ndarray, v: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    x, y, z = cube_point(face, u, v); x2, y2, z2 = x*x, y*y, z*z
    return (x * np.sqrt(np.maximum(0.0, 1.0 - .5*(y2+z2) + y2*z2/3.0)),
            y * np.sqrt(np.maximum(0.0, 1.0 - .5*(z2+x2) + z2*x2/3.0)),
            z * np.sqrt(np.maximum(0.0, 1.0 - .5*(x2+y2) + x2*y2/3.0)))


def face_uv(direction: np.ndarray) -> tuple[int, float, float]:
    x, y, z = direction; ax, ay, az = abs(x), abs(y), abs(z)
    if ax >= ay and ax >= az: face, a, b = (0, -z/ax, y/ax) if x >= 0 else (1, z/ax, y/ax)
    elif ay >= az: face, a, b = (2, x/ay, -z/ay) if y >= 0 else (3, x/ay, z/ay)
    else: face, a, b = (4, x/az, y/az) if z >= 0 else (5, -x/az, y/az)
    target_a,target_b=a,b
    def coordinates(px: float,py: float,pz: float) -> tuple[float,float]:
        if face==0:return -pz/px,py/px
        if face==1:return pz/-px,py/-px
        if face==2:return px/py,-pz/py
        if face==3:return px/-py,pz/-py
        if face==4:return px/pz,py/pz
        return -px/-pz,py/-pz
    epsilon=1e-6
    for _ in range(8):
        values=[]
        for da,db in ((0,0),(epsilon,0),(0,epsilon)):
            px,py,pz=relaxed_direction(face,np.asarray((a+da)*.5+.5),np.asarray((b+db)*.5+.5))
            length=math.sqrt(float(px*px+py*py+pz*pz));values.append(coordinates(float(px)/length,float(py)/length,float(pz)/length))
        value,value_a,value_b=values
        du_a=(value_a[0]-value[0])/epsilon;dv_a=(value_a[1]-value[1])/epsilon
        du_b=(value_b[0]-value[0])/epsilon;dv_b=(value_b[1]-value[1])/epsilon
        determinant=du_a*dv_b-dv_a*du_b
        if abs(determinant)<1e-14:break
        error_u=value[0]-target_a;error_v=value[1]-target_b
        a=np.clip(a-(error_u*dv_b-error_v*du_b)/determinant,-1.0,1.0)
        b=np.clip(b-(du_a*error_v-dv_a*error_u)/determinant,-1.0,1.0)
    return face, float(a*.5+.5), float(b*.5+.5)


def sample_bilinear(source: np.ndarray, longitude: np.ndarray, latitude: np.ndarray) -> np.ndarray:
    height, width = source.shape[:2]
    px = np.mod((longitude / (2.0 * math.pi) + .5) * width, width)
    py = np.clip((.5 - latitude / math.pi) * height, 0.0, height - 1.000001)
    x0, y0 = np.floor(px).astype(np.int64), np.floor(py).astype(np.int64)
    x1, y1 = (x0 + 1) % width, np.minimum(y0 + 1, height - 1)
    tx, ty = px - x0, py - y0
    if source.ndim == 3: tx, ty = tx[..., None], ty[..., None]
    return ((source[y0, x0] * (1.0-tx) + source[y0, x1] * tx) * (1.0-ty) +
            (source[y1, x0] * (1.0-tx) + source[y1, x1] * tx) * ty)


class RegionalGeoTiff:
    def __init__(self, path: Path):
        Image.MAX_IMAGE_PIXELS = None
        with Image.open(path) as image:
            if image.mode != "F": raise ValueError("Regional elevation must be a floating-point GeoTIFF.")
            scale = image.tag_v2.get(33550); tie = image.tag_v2.get(33922)
            if not scale or not tie: raise ValueError("Regional elevation GeoTIFF has no affine georeference.")
            self.values = np.asarray(image, dtype=np.float32).copy()
            self.scale_x, self.scale_y = float(scale[0]), float(scale[1])
            self.origin_x, self.origin_y = float(tie[3]), float(tie[4])
        self.height, self.width = self.values.shape
        self.minimum_longitude = self.origin_x
        self.maximum_longitude = self.origin_x + self.width * self.scale_x
        self.maximum_latitude = self.origin_y
        self.minimum_latitude = self.origin_y - self.height * self.scale_y

    @property
    def bounds(self) -> tuple[float, float, float, float]:
        return self.minimum_longitude, self.minimum_latitude, self.maximum_longitude, self.maximum_latitude

    def sample(self, longitude_radians: np.ndarray, latitude_radians: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
        longitude = np.degrees(longitude_radians); latitude = np.degrees(latitude_radians)
        px = (longitude - self.origin_x) / self.scale_x - .5
        py = (self.origin_y - latitude) / self.scale_y - .5
        inside = (px >= 0.0) & (py >= 0.0) & (px < self.width - 1) & (py < self.height - 1)
        x0 = np.clip(np.floor(px).astype(np.int64), 0, self.width - 1)
        y0 = np.clip(np.floor(py).astype(np.int64), 0, self.height - 1)
        x1 = np.minimum(x0 + 1, self.width - 1); y1 = np.minimum(y0 + 1, self.height - 1)
        tx = px - np.floor(px); ty = py - np.floor(py)
        samples = np.stack((self.values[y0, x0], self.values[y0, x1], self.values[y1, x0], self.values[y1, x1]), axis=2)
        weights = np.stack(((1-tx)*(1-ty), tx*(1-ty), (1-tx)*ty, tx*ty), axis=2)
        valid_samples = samples > -1000.0
        weights *= valid_samples
        total = weights.sum(axis=2)
        valid = inside & (total > 1e-8)
        result = np.zeros_like(longitude, dtype=np.float32)
        result[valid] = (samples * weights).sum(axis=2)[valid] / total[valid]
        return result, valid


def regional_sectors(levels: range, source: RegionalGeoTiff) -> list[tuple[int, int, int, int]]:
    minimum_longitude, minimum_latitude, maximum_longitude, maximum_latitude = source.bounds
    addresses = []
    for latitude in np.linspace(minimum_latitude, maximum_latitude, 257):
        for longitude in (minimum_longitude, maximum_longitude):
            lat, lon = math.radians(latitude), math.radians(longitude)
            addresses.append(face_uv(np.array((math.cos(lat)*math.cos(lon), math.sin(lat), -math.cos(lat)*math.sin(lon)))))
    for longitude in np.linspace(minimum_longitude, maximum_longitude, 257):
        for latitude in (minimum_latitude, maximum_latitude):
            lat, lon = math.radians(latitude), math.radians(longitude)
            addresses.append(face_uv(np.array((math.cos(lat)*math.cos(lon), math.sin(lat), -math.cos(lat)*math.sin(lon)))))
    faces = {value[0] for value in addresses}
    if len(faces) != 1: raise ValueError("The first Florida region must occupy one canonical cube face.")
    face = next(iter(faces)); minimum_u = min(value[1] for value in addresses); maximum_u = max(value[1] for value in addresses)
    minimum_v = min(value[2] for value in addresses); maximum_v = max(value[2] for value in addresses)
    selected = []
    for level in levels:
        cells = 1 << level
        for y in range(max(0, int(math.floor(minimum_v*cells))), min(cells-1, int(math.floor(maximum_v*cells)))+1):
            for x in range(max(0, int(math.floor(minimum_u*cells))), min(cells-1, int(math.floor(maximum_u*cells)))+1):
                selected.append((face, level, x, y))
    return selected


def sectors(level: int, fixture: bool) -> list[tuple[int, int, int]]:
    if fixture: return sorted(((4, 5, 6), (4, 6, 6), (4, 5, 7), (4, 6, 7)))
    result = set(); size = 1 << level
    for latitude, longitude in SITES:
        lat, lon = math.radians(latitude), math.radians(longitude)
        direction = np.array((math.cos(lat)*math.cos(lon), math.sin(lat), -math.cos(lat)*math.sin(lon)))
        face, u, v = face_uv(direction); cx, cy = int(u*size), int(v*size)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                x, y = cx+dx, cy+dy
                if 0 <= x < size and 0 <= y < size: result.add((face, x, y))
    return sorted(result)


def tile_data(face: int, x: int, y: int, level: int, albedo_source: np.ndarray | None, elevation_source: np.ndarray | None):
    size = 1 << level
    coordinate = (np.arange(EXTENT, dtype=np.float64) - GUTTER + .5) / INTERIOR
    u = (x + coordinate[None, :]) / size; v = (y + coordinate[:, None]) / size
    dx, dy, dz = relaxed_direction(face, u, v)
    longitude, latitude = np.arctan2(-dz, dx), np.arcsin(np.clip(dy, -1.0, 1.0))
    if albedo_source is None:
        albedo = np.stack((110+50*np.sin(longitude*9), 120+45*np.sin(latitude*13), 100+35*np.cos((longitude+latitude)*11)), axis=2)
        base_elevation = 250.0 + 150.0*np.sin(longitude*7)*np.cos(latitude*5)
    else:
        albedo = sample_bilinear(albedo_source, longitude, latitude)
        encoded_elevation = sample_bilinear(elevation_source, longitude, latitude)
        base_elevation = -11000.0 + encoded_elevation * (20000.0/65535.0)
    residual = (18.0*np.sin(longitude*1800.0 + latitude*1300.0) +
                7.0*np.sin(longitude*5100.0 - latitude*3900.0))
    land = np.clip((base_elevation + 50.0) / 100.0, 0.0, 1.0)
    residual *= land
    meso = .91 + .09*np.sin(longitude*420.0)*np.cos(latitude*370.0)
    albedo = np.clip(albedo * meso[..., None], 0, 255).astype(np.uint8)
    residual_u8 = np.clip(np.rint((residual-RESIDUAL_MINIMUM)*255.0/(RESIDUAL_MAXIMUM-RESIDUAL_MINIMUM)), 0, 255).astype(np.uint8)
    step = 6371008.8 * math.pi / (2.0**level * INTERIOR)
    gradient_y, gradient_x = np.gradient(residual, step, step)
    normal_x = np.clip(np.rint(-gradient_x * 127.0 / np.sqrt(1.0+gradient_x**2+gradient_y**2) + 127.5), 0, 255).astype(np.uint8)
    normal_y = np.clip(np.rint(-gradient_y * 127.0 / np.sqrt(1.0+gradient_x**2+gradient_y**2) + 127.5), 0, 255).astype(np.uint8)
    return albedo, residual.astype(np.float32), residual_u8, normal_x, normal_y, longitude, latitude


def smoothstep(value: np.ndarray) -> np.ndarray:
    value = np.clip(value, 0.0, 1.0)
    return value * value * (3.0 - 2.0 * value)


def florida_control(longitude: np.ndarray, latitude: np.ndarray, regional_height: np.ndarray, valid: np.ndarray) -> np.ndarray:
    lon_degrees = np.degrees(longitude); lat_degrees = np.degrees(latitude)
    phase = np.sin(longitude*731.0 + latitude*557.0) * np.cos(longitude*293.0 - latitude*911.0)
    control = np.full(regional_height.shape, CONTROL_OCEAN, dtype=np.uint8)
    land = valid & (regional_height > .15)
    control[valid & (regional_height <= .15)] = CONTROL_COASTAL
    control[land & (regional_height <= 2.2)] = CONTROL_BEACH
    control[land & (regional_height > 2.2) & (regional_height < 8.0) & (phase < -.05)] = CONTROL_WETLAND
    control[land & (control == CONTROL_OCEAN)] = CONTROL_GRASS
    control[land & (regional_height >= 8.0) & (phase > .35)] = CONTROL_SCRUB
    control[land & (regional_height >= 10.0) & (phase < -.28)] = CONTROL_FOREST
    # A bounded coarse developed mask establishes the control contract without
    # importing road/building data or pretending it is survey-grade land use.
    developed = land & (np.abs(np.sin(longitude*180.0))*np.abs(np.cos(latitude*210.0)) > .85)
    control[developed] = CONTROL_DEVELOPED
    metres_north = (lat_degrees-FLORIDA_LAUNCH_LATITUDE)*110_574.0
    metres_east = (lon_degrees-FLORIDA_LAUNCH_LONGITUDE)*(111_320.0*np.cos(np.radians(FLORIDA_LAUNCH_LATITUDE)))
    control[metres_east*metres_east+metres_north*metres_north <= FLORIDA_RESERVATION_RADIUS_METRES**2] = CONTROL_LAUNCH_RESERVATION
    return control


def regional_tile_data(face: int, x: int, y: int, level: int, albedo_source: np.ndarray,
                       elevation_source: np.ndarray, regional_source: RegionalGeoTiff):
    size = 1 << level
    coordinate = (np.arange(EXTENT, dtype=np.float64) - GUTTER + .5) / INTERIOR
    u = (x + coordinate[None, :]) / size; v = (y + coordinate[:, None]) / size
    dx, dy, dz = relaxed_direction(face, u, v)
    longitude, latitude = np.arctan2(-dz, dx), np.arcsin(np.clip(dy, -1.0, 1.0))
    albedo = sample_bilinear(albedo_source, longitude, latitude)
    encoded_elevation = sample_bilinear(elevation_source, longitude, latitude)
    global_height = -11000.0 + encoded_elevation * (20000.0/65535.0)
    regional_height, valid = regional_source.sample(longitude, latitude)
    residual = np.where(valid, regional_height-global_height, 0.0)
    minimum_longitude, minimum_latitude, maximum_longitude, maximum_latitude = regional_source.bounds
    lon_degrees = np.degrees(longitude); lat_degrees = np.degrees(latitude)
    longitude_metres = 111_320.0*np.maximum(np.cos(latitude), .1)
    boundary_distance = np.minimum.reduce(((lon_degrees-minimum_longitude)*longitude_metres,
        (maximum_longitude-lon_degrees)*longitude_metres,
        (lat_degrees-minimum_latitude)*110_574.0,
        (maximum_latitude-lat_degrees)*110_574.0))
    coverage = smoothstep(boundary_distance/FLORIDA_BOUNDARY_FEATHER_METRES)*valid
    residual *= coverage
    control = florida_control(longitude, latitude, regional_height, valid)
    # Preserve geographic Blue Marble identity and add restrained control-led
    # meso contrast. Runtime material families remain independent authority.
    class_scale = np.take(np.array((.90,.96,1.05,.91,1.00,.98,.92,.97,.98),dtype=np.float32), control)
    albedo = np.clip(albedo*class_scale[...,None],0,255).astype(np.uint8)
    step = 6371008.8 * math.pi / (2.0**level * INTERIOR)
    gradient_y, gradient_x = np.gradient(global_height+residual, step, step)
    divisor = np.sqrt(1.0+gradient_x**2+gradient_y**2)
    normal_x = np.clip(np.rint(-gradient_x*127.0/divisor+127.5),0,255).astype(np.uint8)
    normal_y = np.clip(np.rint(-gradient_y*127.0/divisor+127.5),0,255).astype(np.uint8)
    return albedo, residual.astype(np.float32), normal_x, normal_y, control, longitude, latitude, regional_height, coverage


def build_florida(args) -> int:
    source_sha = hashlib.sha256(args.regional_elevation.read_bytes()).hexdigest()
    if args.regional_elevation_sha256 and source_sha.lower() != args.regional_elevation_sha256.lower():
        raise ValueError(f"Regional source SHA-256 mismatch: {source_sha}")
    with Image.open(args.albedo) as image:
        albedo_source = np.asarray(image.convert("RGB"), dtype=np.float32)
    elevation_source = np.memmap(args.elevation, dtype="<u2", mode="r", shape=(4096,8192)).astype(np.float32)
    regional_source = RegionalGeoTiff(args.regional_elevation)
    selected = sorted(regional_sectors(range(args.minimum_sector_level,args.sector_level+1),regional_source),
                      key=lambda value:(value[0],value[1],value[2],value[3]))
    records=[];metrics=[];raw_bytes=stored_bytes=gpu_bytes=0;control_histogram=np.zeros(9,dtype=np.uint64)
    package_minimum=math.inf;package_maximum=-math.inf
    for face,level,x,y in selected:
        albedo,residual,normal_x,normal_y,control,longitude,latitude,regional_height,coverage=regional_tile_data(
            face,x,y,level,albedo_source,elevation_source,regional_source)
        record_minimum=FLORIDA_RESIDUAL_MINIMUM_METRES
        record_maximum=FLORIDA_RESIDUAL_MAXIMUM_METRES
        if float(np.min(residual))<record_minimum or float(np.max(residual))>record_maximum:
            raise ValueError("Florida residual exceeds the versioned R16 physical range.")
        residual_u16=np.clip(np.rint((residual-record_minimum)*65535.0/(record_maximum-record_minimum)),0,65535).astype("<u2")
        bc7,decoded_albedo=encode_bc7_mode6(albedo)
        bc5x,decoded_normal_x=encode_bc4(normal_x);bc5y,decoded_normal_y=encode_bc4(normal_y)
        bc5=b"".join(a+b for a,b in zip((bc5x[i:i+8] for i in range(0,len(bc5x),8)),(bc5y[i:i+8] for i in range(0,len(bc5y),8))))
        residual_bytes=residual_u16.tobytes();control_bytes=control.tobytes()
        channels=[stored(bc7),stored(residual_bytes),stored(bc5),stored(control_bytes)]
        identity=struct.pack("<QIBBBBII",BODY_ID,TERRAIN_VERSION,face,level,DETAIL_FREQUENCY,PAYLOAD_VERSION,x,y)
        range_bytes=struct.pack("<2f",record_minimum,record_maximum)
        digest=hashlib.sha256(identity+bc7+residual_bytes+bc5+range_bytes+control_bytes).digest()
        reconstructed=record_minimum+residual_u16.astype(np.float64)*(record_maximum-record_minimum)/65535.0
        vertical_error=np.abs(residual-reconstructed)
        control_histogram+=np.bincount(control.ravel(),minlength=9).astype(np.uint64)
        valid=coverage>.999
        metrics.append({
            "level":level,"maximumVerticalErrorMetres":float(np.max(vertical_error)),
            "rmsVerticalErrorMetres":float(np.sqrt(np.mean(vertical_error**2))),
            "interiorRegionalMinimumMetres":float(np.min(regional_height[valid])) if np.any(valid) else 0.0,
            "interiorRegionalMaximumMetres":float(np.max(regional_height[valid])) if np.any(valid) else 0.0,
            "minimumResidualMetres":float(np.min(residual)),"maximumResidualMetres":float(np.max(residual)),
            "albedoRmsByteError":float(np.sqrt(np.mean((albedo.astype(np.float32)-decoded_albedo.astype(np.float32))**2)))})
        gpu_lengths=(len(bc7),len(residual_bytes),len(bc5),len(control_bytes))
        records.append((face,level,x,y,channels,gpu_lengths,digest,record_minimum,record_maximum))
        package_minimum=min(package_minimum,record_minimum);package_maximum=max(package_maximum,record_maximum)
        raw_bytes+=EXTENT*EXTENT*(3+2+2+1);stored_bytes+=sum(len(value[0]) for value in channels);gpu_bytes+=sum(gpu_lengths)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    with args.output.open("wb") as stream:
        header=bytearray(HEADER_BYTES)
        struct.pack_into("<8s7I4xQI",header,0,MAGIC,VERSION,HEADER_BYTES,RECORD_HEADER_BYTES,INTERIOR,GUTTER,EXTENT,len(records),BODY_ID,TERRAIN_VERSION)
        struct.pack_into("<BB2x2f",header,52,args.minimum_sector_level,args.sector_level,package_minimum,package_maximum)
        stream.write(header)
        for face,level,x,y,channels,gpu_lengths,digest,record_minimum,record_maximum in records:
            payload_offset=stream.tell()+RECORD_HEADER_BYTES;record=bytearray(RECORD_HEADER_BYTES)
            struct.pack_into("<QIBBBBIIQ6I4B4x",record,0,BODY_ID,TERRAIN_VERSION,face,level,DETAIL_FREQUENCY,PAYLOAD_VERSION,x,y,payload_offset,
                len(channels[0][0]),len(channels[1][0]),len(channels[2][0]),gpu_lengths[0],gpu_lengths[1],gpu_lengths[2],
                channels[0][1],channels[1][1],channels[2][1],1)
            record[64:96]=digest
            record[60]=channels[3][1]
            struct.pack_into("<II2f",record,96,len(channels[3][0]),gpu_lengths[3],record_minimum,record_maximum)
            stream.write(record)
            for value,_ in channels:stream.write(value)
    package_sha=hashlib.sha256(args.output.read_bytes()).hexdigest()
    by_level={str(level):sum(1 for value in records if value[1]==level) for level in range(args.minimum_sector_level,args.sector_level+1)}
    manifest={
        "schema":"NovaCore.LocalTerrainContent/3","formatVersion":VERSION,"payloadVersion":PAYLOAD_VERSION,
        "channels":{"albedo":"BC7 sRGB","heightResidual":"R16 UNORM with per-record metre range","normal":"BC5 UNORM","control":"R8 UNORM class ID"},
        "records":len(records),"minimumSectorLevel":args.minimum_sector_level,"maximumSectorLevel":args.sector_level,
        "recordsByLevel":by_level,"coverage":{"kind":"USGS-3DEP-geographic-source","boundsDegrees":regional_source.bounds,
            "launchSiteDegrees":[FLORIDA_LAUNCH_LATITUDE,FLORIDA_LAUNCH_LONGITUDE],"boundaryFeatherMetres":FLORIDA_BOUNDARY_FEATHER_METRES},
        "source":{"path":args.regional_elevation.name,"sha256":source_sha,"horizontalArcSeconds":regional_source.scale_x*3600.0,
            "dimensions":[regional_source.width,regional_source.height],"crs":"EPSG:4269 (NAD83 geographic)","verticalUnits":"metres"},
        "rawBytes":raw_bytes,"gpuBytes":gpu_bytes,"storedBytes":stored_bytes,"supercompressionRatio":gpu_bytes/stored_bytes,
        "maximumVerticalErrorMetres":max(value["maximumVerticalErrorMetres"] for value in metrics),
        "rmsVerticalErrorMetres":math.sqrt(sum(value["rmsVerticalErrorMetres"]**2 for value in metrics)/len(metrics)),
        "minimumRegionalElevationMetres":min(value["interiorRegionalMinimumMetres"] for value in metrics),
        "maximumRegionalElevationMetres":max(value["interiorRegionalMaximumMetres"] for value in metrics),
        "minimumResidualMetres":min(value["minimumResidualMetres"] for value in metrics),
        "maximumResidualMetres":max(value["maximumResidualMetres"] for value in metrics),
        "controlClassTexels":{name:int(control_histogram[index]) for index,name in enumerate(("ocean","coastalIntertidal","beachSand","wetland","grass","scrub","forest","developed","launchSiteReservation"))},
        "sha256":package_sha}
    if args.content_manifest:
        args.content_manifest.parent.mkdir(parents=True,exist_ok=True);args.content_manifest.write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({**manifest,"bytes":args.output.stat().st_size},sort_keys=True))
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--albedo", type=Path)
    parser.add_argument("--elevation", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--content-manifest", type=Path)
    parser.add_argument("--sector-level", type=int, default=12)
    parser.add_argument("--minimum-sector-level", type=int)
    parser.add_argument("--regional-elevation", type=Path)
    parser.add_argument("--regional-elevation-sha256")
    parser.add_argument("--fixture", action="store_true")
    args = parser.parse_args()
    if args.regional_elevation:
        if not args.albedo or not args.elevation: raise ValueError("M12 regional builds require global albedo and elevation authorities.")
        if args.minimum_sector_level is None: args.minimum_sector_level=args.sector_level
        if not 3 <= args.minimum_sector_level <= args.sector_level <= 20: raise ValueError("Invalid regional sector hierarchy.")
        return build_florida(args)
    if not args.fixture and (not args.albedo or not args.elevation): raise ValueError("Production builds require --albedo and --elevation.")
    albedo_source = elevation_source = None
    if not args.fixture:
        with Image.open(args.albedo) as image: albedo_source = np.asarray(image.convert("RGB"), dtype=np.float32)
        elevation_source = np.memmap(args.elevation, dtype="<u2", mode="r", shape=(4096, 8192)).astype(np.float32)
    selected = sectors(args.sector_level, args.fixture)
    records = []; metrics = []; raw_bytes = stored_bytes = gpu_bytes = 0
    for face, x, y in selected:
        albedo, residual, residual_u8, normal_x, normal_y, longitude, latitude = tile_data(face, x, y, args.sector_level, albedo_source, elevation_source)
        bc7, decoded_albedo = encode_bc7_mode6(albedo)
        bc4, decoded_residual = encode_bc4(residual_u8)
        bc5x, decoded_normal_x = encode_bc4(normal_x); bc5y, decoded_normal_y = encode_bc4(normal_y)
        bc5 = b"".join(a+b for a,b in zip((bc5x[i:i+8] for i in range(0,len(bc5x),8)), (bc5y[i:i+8] for i in range(0,len(bc5y),8))))
        channels = [stored(bc7), stored(bc4), stored(bc5)]
        identity = struct.pack("<QIBBBBII", BODY_ID, TERRAIN_VERSION, face, args.sector_level, DETAIL_FREQUENCY, 2, x, y)
        digest = hashlib.sha256(identity + bc7 + bc4 + bc5).digest()
        reconstructed = RESIDUAL_MINIMUM + decoded_residual.astype(np.float32)*(RESIDUAL_MAXIMUM-RESIDUAL_MINIMUM)/255.0
        vertical_error = np.abs(residual-reconstructed)
        max_error = float(np.max(vertical_error))
        rms_error = float(np.sqrt(np.mean(vertical_error**2)))
        step = 6371008.8*math.pi/(2.0**args.sector_level*INTERIOR)
        source_gradient_y, source_gradient_x = np.gradient(residual,step,step)
        decoded_gradient_y, decoded_gradient_x = np.gradient(reconstructed,step,step)
        source_slope = np.sqrt(source_gradient_x**2+source_gradient_y**2)
        decoded_slope = np.sqrt(decoded_gradient_x**2+decoded_gradient_y**2)
        slope_error = np.abs(source_slope-decoded_slope)
        source_normal = np.stack((-source_gradient_x,-source_gradient_y,np.ones_like(source_gradient_x)),axis=2)
        source_normal /= np.linalg.norm(source_normal,axis=2,keepdims=True)
        decoded_nx = decoded_normal_x.astype(np.float32)*(2.0/255.0)-1.0
        decoded_ny = decoded_normal_y.astype(np.float32)*(2.0/255.0)-1.0
        decoded_nz = np.sqrt(np.maximum(0.0,1.0-decoded_nx**2-decoded_ny**2))
        decoded_normal = np.stack((decoded_nx,decoded_ny,decoded_nz),axis=2)
        decoded_normal /= np.maximum(np.linalg.norm(decoded_normal,axis=2,keepdims=True),1.0e-12)
        normal_error = np.degrees(np.arccos(np.clip(np.sum(source_normal*decoded_normal,axis=2),-1.0,1.0)))
        worst_index = np.unravel_index(int(np.argmax(vertical_error)),vertical_error.shape)
        worst_sample = {
            "face":face,"sectorX":x,"sectorY":y,
            "texelX":int(worst_index[1]),"texelY":int(worst_index[0]),
            "latitudeDegrees":float(np.degrees(latitude[worst_index])),
            "longitudeDegrees":float(np.degrees(longitude[worst_index])),
            "verticalErrorMetres":float(vertical_error[worst_index]),
        }
        records.append((face,x,y,channels,(len(bc7),len(bc4),len(bc5)),digest))
        metrics.append({
            "maximumVerticalErrorMetres":max_error,
            "rmsVerticalErrorMetres":rms_error,
            "maximumSlopeError":float(np.max(slope_error)),
            "rmsSlopeError":float(np.sqrt(np.mean(slope_error**2))),
            "maximumNormalErrorDegrees":float(np.max(normal_error)),
            "rmsNormalErrorDegrees":float(np.sqrt(np.mean(normal_error**2))),
            "albedoRmsByteError":float(np.sqrt(np.mean((albedo.astype(np.float32)-decoded_albedo.astype(np.float32))**2))),
            "worstVerticalSample":worst_sample,
        })
        raw_bytes += EXTENT*EXTENT*(3+2+2); stored_bytes += sum(len(value[0]) for value in channels); gpu_bytes += len(bc7)+len(bc4)+len(bc5)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("wb") as stream:
        header = bytearray(HEADER_BYTES)
        struct.pack_into("<8s7I4xQI", header, 0, MAGIC, LEGACY_VERSION, HEADER_BYTES, RECORD_HEADER_BYTES, INTERIOR, GUTTER, EXTENT, len(records), BODY_ID, TERRAIN_VERSION)
        struct.pack_into("<BB2x2f", header, 52, args.sector_level, args.sector_level, RESIDUAL_MINIMUM, RESIDUAL_MAXIMUM)
        stream.write(header)
        for face,x,y,channels,gpu_lengths,digest in records:
            payload_offset = stream.tell()+RECORD_HEADER_BYTES
            record = bytearray(RECORD_HEADER_BYTES)
            struct.pack_into("<QIBBBBIIQ6I4B4x", record, 0, BODY_ID,TERRAIN_VERSION,face,args.sector_level,DETAIL_FREQUENCY,2,x,y,payload_offset,
                             len(channels[0][0]),len(channels[1][0]),len(channels[2][0]),*gpu_lengths,channels[0][1],channels[1][1],channels[2][1],0)
            record[64:96] = digest
            stream.write(record)
            for value,_ in channels: stream.write(value)
    package_sha = hashlib.sha256(args.output.read_bytes()).hexdigest()
    manifest = {
        "schema":"NovaCore.LocalTerrainContent/1", "records":len(records), "sectorLevel":args.sector_level,
        "rawBytes":raw_bytes, "gpuBytes":gpu_bytes, "storedBytes":stored_bytes,
        "supercompressionRatio":gpu_bytes/stored_bytes if stored_bytes else 1.0,
        "bc7Bytes":len(records)*BLOCK_COUNT*16, "bc4Bytes":len(records)*BLOCK_COUNT*8, "bc5Bytes":len(records)*BLOCK_COUNT*16,
        "maximumVerticalErrorMetres":max(value["maximumVerticalErrorMetres"] for value in metrics),
        "rmsVerticalErrorMetres":math.sqrt(sum(value["rmsVerticalErrorMetres"]**2 for value in metrics)/len(metrics)),
        "maximumSlopeError":max(value["maximumSlopeError"] for value in metrics),
        "rmsSlopeError":math.sqrt(sum(value["rmsSlopeError"]**2 for value in metrics)/len(metrics)),
        "maximumNormalErrorDegrees":max(value["maximumNormalErrorDegrees"] for value in metrics),
        "rmsNormalErrorDegrees":math.sqrt(sum(value["rmsNormalErrorDegrees"]**2 for value in metrics)/len(metrics)),
        "worstVerticalSample":max((value["worstVerticalSample"] for value in metrics),key=lambda value:value["verticalErrorMetres"]),
        "albedoRmsByteError":math.sqrt(sum(value["albedoRmsByteError"]**2 for value in metrics)/len(metrics)),
        "sha256":package_sha,
    }
    if args.content_manifest:
        args.content_manifest.parent.mkdir(parents=True, exist_ok=True)
        args.content_manifest.write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    print(json.dumps({**manifest,"bytes":args.output.stat().st_size}, sort_keys=True))
    return 0


if __name__ == "__main__": raise SystemExit(main())
