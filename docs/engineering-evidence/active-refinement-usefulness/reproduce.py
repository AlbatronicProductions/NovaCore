"""Explicit bounded batches; never run the whole capture campaign by default."""
import argparse,sys
sys.dont_write_bytecode=True
from run import run

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('batch',choices=['fixed','timing','parity','primitives'])
    args=parser.parse_args()
    if args.batch=='fixed':
        for pose in 'ABCD':run(pose,'normal','fixed-'+pose)
    elif args.batch=='timing':
        for i in range(1,4):
            for probe in ['normal','fragment-ncsm1']:
                run('B',probe,f'repeat-{i}-{probe}')
    else:
        from capture_analysis import Parity
        for pose in ('ABCD' if args.batch=='parity' else 'BA'):
            parity=Parity()
            run(pose,'normal','parity-'+pose+'-normal',capture='tes',callback=parity)
            if args.batch=='parity':
                run(pose,'fragment-ncsm1','parity-'+pose+'-fragment-ncsm1',capture='tes',callback=parity)
            else:
                run(pose,'normal','capture-'+pose+'-triangles',capture='triangles',callback=parity)
                run(pose,'normal','capture-'+pose+'-ids',capture='ids',callback=parity)

if __name__=='__main__':main()
