"""Independent offline inspector mutation tests against the deterministic V2 page export."""
import copy
import json
from pathlib import Path
import sys
import unittest
import validate_export as v

class Tests(unittest.TestCase):
    def inspect(self,value):return v.inspect_bytes(json.dumps(value).encode())
    def test_valid_offline_export(self):
        result=self.inspect(FIXTURE);self.assertTrue(result['structuralPass'],result['errors'])
        self.assertEqual(len(result['postMarkerProofs']),2)
        self.assertFalse(result['storageReady']) # explicit quota test; not hardware qualification
    def test_proof_mutations_rejected(self):
        def changes(value,path,replacement):
            target=value
            for key in path[:-1]:target=target[key]
            target[path[-1]]=replacement
        for path,replacement in [
            (['marks',0,'proofs','primary','nonce'],'f'*64),
            (['marks',0,'proofs','primary','sendMono'],0),
            (['marks',0,'proofs','primary','generation'],90),
            (['marks',0,'proofs','primary','accepted'],False),
            (['marks',0,'proofs','beacon','data','startUtcTicks'],'1'),
            (['marks',0,'proofs','primary','data','sample','observation','session'],'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
            (['marks',0,'proofs','primary','data','sample','observation','latestEventWords',19],18446744073709551615),
            (['marks',0,'binding'],'["wrong"]')]:
            with self.subTest(path=path):
                value=copy.deepcopy(FIXTURE);changes(value,path,replacement)
                self.assertFalse(self.inspect(value)['structuralPass'])
    def test_duplicate_json_and_nonfinite_rejected(self):
        for raw in (b'{"schema":1,"schema":2}',b'{"value":NaN}'):
            with self.assertRaises(ValueError):v.inspect_bytes(raw)
    def test_identity_and_oversized_restore_rejected(self):
        value=copy.deepcopy(FIXTURE);value['beaconIdentity']['witness']='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'
        with self.assertRaises(ValueError):self.inspect(value)
        with self.assertRaises(ValueError):v.inspect_bytes(b' '* (v.MAX_BYTES+1))

if __name__=='__main__':
    FIXTURE=json.loads(Path(sys.argv[1]).read_bytes());sys.argv=sys.argv[:1]
    unittest.main(verbosity=2)
