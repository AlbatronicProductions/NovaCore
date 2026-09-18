"""Generate bounded diagnostic compile inputs outside source; do not alter candidate files."""
from pathlib import Path
import hashlib,json
r=Path('E:/NovaCore');e=Path(__file__).parent;b=r/'build/srv01-stage5-final-closure';g=b/'generated';g.mkdir(parents=True,exist_ok=True)
seals=json.loads((e.parent/'stock-florida-restart/candidate-seals.json').read_text())
for s in seals['changed']:assert hashlib.sha256((r/s['path']).read_bytes()).hexdigest().upper()==s['sha256']
program=(r/'tests/NovaCore.Graphics.Tests/Program.cs').read_text()
program=program.replace('if(args.Contains("--assembly-florida-site-cheap"','if(args.Contains("--assembly-florida-terminal-proof",StringComparer.Ordinal)){AssemblyFloridaSiteTests.TerminalCreditProof();return 0;}\nif(args.Contains("--assembly-florida-site-cheap"',1)
(g/'GraphicsProgram.cs').write_text(program)
scene=(r/'samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs').read_text()
scene=scene.replace('    private long sequence,timestamp,remainder;', '    private FloridaTimingProbe? timingProbe;\n    private long sequence,timestamp,remainder;',1)
scene=scene.replace('        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;', '        timingProbe ??= new FloridaTimingProbe(session);\n        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;',1)
# Initialize before host sampling, not during the first callback. The fallback above is unreachable but fail-safe.
scene=scene.replace('started=true;timestamp=Stopwatch.GetTimestamp();','timingProbe=new FloridaTimingProbe(session);started=true;timestamp=Stopwatch.GetTimestamp();',1)
scene=scene.replace('        var serviceStart=Stopwatch.GetTimestamp();Advance(new((long)ticks));\n        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;',
'''        var stamp=timingProbe.Begin(frameCount-1,now,observation);
        var serviceStart=Stopwatch.GetTimestamp();Advance(new((long)ticks));
        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;
        timingProbe.End(stamp,observation,frameMilliseconds[frameCount-1],serviceMilliseconds[frameCount-1]);''',1)
scene=scene.replace('    private void Report()\n    {','    private void Report()\n    {\n        timingProbe?.Report(observation);',1)
(g/'StockAssemblyDevelopmentScene.cs').write_text(scene)
target=f'''<Project>
<Target Name="FinalFloridaClosureDiagnosticInputs" BeforeTargets="CoreCompile">
  <ItemGroup Condition="'$(MSBuildProjectName)' == 'NovaCore.Graphics.Tests'">
    <Compile Remove="Program.cs" />
    <Compile Include="{g/'GraphicsProgram.cs'}" />
    <Compile Include="{e/'TerminalCreditProof.cs'}" />
  </ItemGroup>
  <ItemGroup Condition="'$(MSBuildProjectName)' == 'NovaCore.Triangle'">
    <Compile Remove="StockAssemblyDevelopmentScene.cs" />
    <Compile Include="{g/'StockAssemblyDevelopmentScene.cs'}" />
    <Compile Include="{e/'FloridaTimingProbe.cs'}" />
  </ItemGroup>
</Target>
</Project>'''
(g/'closure.targets').write_text(target)
print('Generated isolated compile inputs only:',g)
