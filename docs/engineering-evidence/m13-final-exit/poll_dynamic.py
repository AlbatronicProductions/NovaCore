"""Same-host cached/uncached controls, exact deployed managed movement driver."""
import sys
import poll

mode, label, cached = sys.argv[1], sys.argv[2], sys.argv[3] == 'cached'
original = poll.a.environment
def environment():
    env = original()
    if cached:
        env['NOVACORE_EXIT_CACHED_KEYS'] = '1'
    return env
poll.a.environment = environment
if len(sys.argv) > 4 and sys.argv[4] == 'quiet':
    original_execute = poll.a.execute
    def execute(label, args, env, *rest):
        env.pop('NOVACORE_PERFORMANCE_FRAME_LOG', None)
        return original_execute(label, args, env, *rest)
    poll.a.execute = execute
poll.e.dynamic(mode, True, label)
