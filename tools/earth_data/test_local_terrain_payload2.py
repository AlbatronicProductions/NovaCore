#!/usr/bin/env python3
"""Preserve the active schema-2 writer with real, bounded synthetic input files."""
import hashlib
import json
import math
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest

sys.dont_write_bytecode = True
import numpy as np
from PIL import Image
import build_local_terrain_pack as generator

ROOT = Path(__file__).resolve().parents[2]


def generate(directory):
    directory = Path(directory)
    albedo = directory / "albedo.png"
    elevation = directory / "elevation.r16"
    Image.new("RGB", (8, 4), (100, 120, 140)).save(albedo)
    # Actual documented input dimensions; bounded temporary file, no source/cache dependency.
    with elevation.open("wb") as stream:
        row = np.full(8192, 55000, dtype="<u2").tobytes()
        for _ in range(4096):
            stream.write(row)
    output = directory / "current.nccube"
    result = subprocess.run([sys.executable, "-B", str(ROOT / "tools/earth_data/build_local_terrain_pack.py"),
        "--albedo", str(albedo), "--elevation", str(elevation), "--sector-level", "3",
        "--output", str(output)], check=True, capture_output=True, text=True)
    package = output.read_bytes()
    return package, json.loads(result.stdout), albedo.stat().st_size + elevation.stat().st_size + len(package)


def first_record(package):
    stored = struct.unpack_from("<3I", package, 288)
    result = bytearray(package[:384 + sum(stored)])
    struct.pack_into("<I", result, 32, 1)
    return result


class LocalTerrainPayload2Tests(unittest.TestCase):
    def test_intended_sites_use_current_geography(self):
        selected = set(generator.sectors(12, False))
        for latitude, longitude in generator.SITES:
            lat, lon = math.radians(latitude), math.radians(longitude)
            direction = np.array((math.cos(lat)*math.cos(lon), math.sin(lat), -math.cos(lat)*math.sin(lon)))
            face, u, v = generator.face_uv(direction)
            self.assertIn((face, int(u*4096), int(v*4096)), selected)
            projected = np.array(generator.relaxed_direction(face, np.asarray(u), np.asarray(v)))
            np.testing.assert_allclose(projected, direction, atol=1e-12, rtol=0)

    def test_current_real_input_writer(self):
        self.assertEqual(np.__version__, "2.3.5")
        with tempfile.TemporaryDirectory(prefix="novacore-cube2-") as temporary:
            package, metrics, created = generate(temporary)
            self.assertEqual(package[:8], b"NCCUBE2\0")
            self.assertEqual(struct.unpack_from("<I", package, 8)[0], 2)
            self.assertEqual(struct.unpack_from("<I", package, 48)[0], 5)
            count = struct.unpack_from("<I", package, 32)[0]
            self.assertGreater(count, 1)
            cursor = 256
            for _ in range(count):
                self.assertEqual(package[cursor + 15], 2)
                self.assertEqual(struct.unpack_from("<Q", package, cursor + 24)[0], cursor + 128)
                cursor += 128 + sum(struct.unpack_from("<3I", package, cursor + 32))
            self.assertEqual(cursor, len(package))
            self.assertEqual(hashlib.sha256(package).hexdigest(), metrics["sha256"])
            fixture = (ROOT / "tests/fixtures/terrain/local-payload2.nccube").read_bytes()
            self.assertEqual(first_record(package), fixture, "frozen current producer output consumed by the managed/native reader regression")
            self.assertLess(len(fixture), 192 * 1024)
            print(f"payload2 records={count}; package={len(package)}B; temporary created/removed={created}B; sha256={metrics['sha256']}")


if __name__ == "__main__":
    unittest.main()
