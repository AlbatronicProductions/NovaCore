"""One normal deployed Florida smoke after scratch retirement; no capture hooks."""
import os,pathlib,subprocess
import validate as v

empty=(v.ROOT/'build/m13-final-exit-empty-implicit').resolve()
assert empty==pathlib.Path('E:/NovaCore/build/m13-final-exit-empty-implicit')
assert not empty.exists()
empty.mkdir()
try:
    env={k:x for k,x in os.environ.items() if not k.startswith(('NOVACORE_','VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER','VK_VALIDATION','VK_INSTANCE_LAYERS'))}
    env.update(VK_LAYER_PATH=str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin'),
        VK_IMPLICIT_LAYER_PATH=str(empty),VK_LAYER_SETTINGS_PATH=str(empty),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',
        NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1',NOVACORE_EARTH_ROUTE_VALIDATION='florida')
    args=[v.ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe',
        '--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate',
        '--solar-epoch=j2000','--benchmark-frames=500','--log=startup,validation,vulkan']
    log=v.v.execute('post-cleanup-normal-Florida',args,env,180)
    assert '500 frames' in log and 'physicalReady=true' in log and 'owners=1' in log
    assert not any(t in log for t in ['VUID-','Validation Error','VK_ERROR','FAIL:'])
    v.deployment()
finally:
    assert empty.resolve()==pathlib.Path('E:/NovaCore/build/m13-final-exit-empty-implicit')
    assert not empty.lstat().st_file_attributes&0x400 and not list(empty.iterdir())
    empty.rmdir()
