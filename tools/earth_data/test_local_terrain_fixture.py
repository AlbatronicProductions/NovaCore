#!/usr/bin/env python3
"""Bounded regeneration contract; temporary packages are removed on completion."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.dont_write_bytecode = True
import numpy as np
import build_local_terrain_pack as generator


class LocalTerrainFixtureTests(unittest.TestCase):
    def test_current_fixture_reproduces_in_independent_paths(self):
        self.assertEqual(np.__version__, "2.3.5", "use the pinned offline NumPy environment")
        root = Path(__file__).resolve().parents[2]
        fixture = root / "tests/fixtures/terrain"
        manifest = json.loads((fixture / "tiny-local.json").read_text())
        expected_content = json.loads((fixture / "tiny-local.content.json").read_text())
        self.assertEqual(manifest["formatVersion"], generator.VERSION)
        self.assertEqual(manifest["terrainVersion"], generator.TERRAIN_VERSION)
        expected = (fixture / "tiny-local.nccube").read_bytes()
        self.assertEqual(len(expected), manifest["byteSize"])
        self.assertLess(len(expected), 1_200_000, "permanent fixture storage budget")
        self.assertEqual(hashlib.sha256(expected).hexdigest(), manifest["sha256"])
        with tempfile.TemporaryDirectory(prefix="novacore-tiny-local-") as temporary:
            for name in ("first", "different-directory/second"):
                output = Path(temporary) / name / "local.nccube"
                content = output.with_suffix(".json")
                args = [value.replace("{output}", str(output)) for value in manifest["generator"]["arguments"]]
                subprocess.run([sys.executable, str(root / manifest["generator"]["tool"]), *args,
                                "--content-manifest", str(content)], check=True, capture_output=True, text=True)
                self.assertEqual(output.read_bytes(), expected, "complete package identity, including all record digests")
                self.assertEqual(json.loads(content.read_text()), expected_content, "deterministic numerical provenance")

    def test_cardinal_longitude_and_synthetic_queries(self):
        # Independent cardinal geography: positive east is negative Z, west positive Z.
        face,u,v = generator.face_uv(np.array((0.0, 0.0, -1.0)))
        self.assertEqual((face,u,v), (5, .5, .5))
        face,u,v = generator.face_uv(np.array((0.0, 0.0, 1.0)))
        self.assertEqual((face,u,v), (4, .5, .5))
        for face in (4, 5):
            _, residual, _, _, _, longitude, latitude = generator.tile_data(face,5,6,4,None,None)
            self.assertTrue(np.all(longitude < 0) if face == 4 else np.all(longitude > 0))
            # Current fixture tiles are land; material detail and physical heights use the same geography.
            expected = (18*np.sin(longitude*1800+latitude*1300)+7*np.sin(longitude*5100-latitude*3900)).astype(np.float32)
            np.testing.assert_array_equal(residual, expected)

    def test_fixture_rejects_unbounded_or_mixed_inputs(self):
        from argparse import Namespace
        for level,minimum,albedo in ((12,None,None), (4,3,None), (4,None,Path("not-a-fixture"))):
            args = Namespace(sector_level=level, minimum_sector_level=minimum,
                             albedo=albedo, elevation=None, regional_elevation=None)
            with self.assertRaises(ValueError):
                generator.build_fixture(args)


if __name__ == "__main__":
    unittest.main()
