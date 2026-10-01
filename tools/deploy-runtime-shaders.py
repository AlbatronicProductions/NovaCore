"""Deploy only CMake-owned runtime shaders; never infer ownership from a directory glob."""
import argparse
import hashlib
import json
import pathlib
import re
import shutil
import stat


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def safe_path(path):
    path = path.absolute()
    for part in (path, *path.parents):
        try:
            attributes = part.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(attributes.st_mode) or getattr(attributes, 'st_file_attributes', 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
            raise ValueError('Reparse/symlink deployment path refused: ' + str(part))
    return path.resolve()


def shader_name(name):
    if not re.fullmatch(r'[A-Za-z0-9_.-]+\.spv', name) or name.startswith('.'):
        raise ValueError('Invalid owned shader name: ' + name)
    return name


def deploy(args):
    native = safe_path(args.native)
    repository = args.repository.resolve()
    manifest = json.loads((native / 'runtime-shaders.json').read_text())
    cache = (native / 'CMakeCache.txt').read_text()
    if manifest.get('schema') != 1 or manifest.get('configuration') != args.configuration:
        raise ValueError('Runtime shader manifest configuration/schema mismatch')
    if not re.search(r'^CMAKE_BUILD_TYPE:STRING=' + args.configuration + r'$', cache, re.M):
        raise ValueError('Native CMake configuration mismatch')
    source = repository / 'native/NovaCore.Native'
    for key, name in [('cmakeSha256', 'CMakeLists.txt'), ('ownerSha256', 'RuntimeShaderDeployment.cmake')]:
        if manifest[key] != sha(source / name):
            raise ValueError('Stale native shader authority; configure/build native first: ' + name)
    names = manifest['shaders']
    if not names or names != sorted(set(names)):
        raise ValueError('Empty, duplicate or unordered native shader inventory')
    expected = {shader_name(name): sha(safe_path(native / 'shaders' / name)) for name in names}
    if not (native / 'NovaCore.Native.dll').is_file():
        raise ValueError('Missing matching native runtime DLL')
    binary = json.loads((native / 'native-runtime-build.json').read_text())
    if binary.get('configuration') != args.configuration or binary.get('dllSha256') != sha(native / 'NovaCore.Native.dll'):
        raise ValueError('Native link provenance mismatch; build the matching native configuration')

    destination = safe_path(args.destination)
    shaders = safe_path(destination / 'shaders')
    receipt_path = safe_path(destination / 'novacore-runtime-shaders.json')
    owned = {}
    if receipt_path.exists():
        prior = json.loads(receipt_path.read_text())
        if prior.get('owner') != 'NovaCore runtime shader deployment' or prior.get('schema') != 1:
            raise ValueError('Unrecognized deployment ownership receipt')
        owned = {shader_name(name): digest for name, digest in prior['files'].items()}
    elif args.legacy_file_list and args.legacy_file_list.is_file():
        # One-time migration from MSBuild's own copy ledger. Require both the exact
        # destination path and matching native output bytes; no filename blacklist.
        for line in args.legacy_file_list.read_text(encoding='utf-8-sig').splitlines():
            path = pathlib.Path(line)
            if not path.is_absolute() or path.parent != shaders or path.suffix != '.spv':
                continue
            name = shader_name(path.name)
            path = safe_path(path)
            if path.exists() and name not in expected:
                original = safe_path(native / 'shaders' / name)
                if not original.is_file() or sha(path) != sha(original):
                    raise ValueError('Cannot establish legacy deployment bytes: ' + str(path))
                owned[name] = sha(path)

    # Validate every deletion and source before touching the destination.
    obsolete = []
    for name, digest in owned.items():
        path = safe_path(shaders / name)
        if name not in expected and path.exists():
            if sha(path) != digest:
                raise ValueError('Modified obsolete owned shader preserved: ' + str(path))
            obsolete.append(path)
    actual = {p.name for p in shaders.glob('*.spv')} if shaders.exists() else set()
    unknown = actual - expected.keys() - owned.keys()
    if unknown:
        raise ValueError('Unowned shader output preserved; deployment refused: ' + repr(sorted(unknown)))
    for name in expected:
        safe_path(shaders / name)
    shaders.mkdir(parents=True, exist_ok=True)
    for name, digest in expected.items():
        path = shaders / name
        if not path.exists() or sha(path) != digest:
            shutil.copy2(native / 'shaders' / name, path)
    for path in obsolete:
        path.unlink()
    receipt = {'schema': 1, 'owner': 'NovaCore runtime shader deployment',
               'configuration': args.configuration, 'authoritySha256': sha(native / 'runtime-shaders.json'),
               'files': expected}
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')
    if args.items_output:
        args.items_output.parent.mkdir(parents=True, exist_ok=True)
        args.items_output.write_text('\n'.join(str(native / 'shaders' / name) for name in expected) + '\n')
    print(f'Runtime shaders: {args.configuration}, {len(expected)} owned, {len(obsolete)} obsolete removed')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', type=pathlib.Path, required=True)
    parser.add_argument('--native', type=pathlib.Path, required=True)
    parser.add_argument('--configuration', choices=['Debug', 'Release'], required=True)
    parser.add_argument('--destination', type=pathlib.Path, required=True)
    parser.add_argument('--legacy-file-list', type=pathlib.Path)
    parser.add_argument('--items-output', type=pathlib.Path)
    try:
        deploy(parser.parse_args())
    except (ValueError, KeyError, OSError) as error:
        parser.exit(1, 'Runtime shader deployment failed: ' + str(error) + '\n')
