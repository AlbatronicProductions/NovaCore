from pathlib import Path
r=Path.cwd();e=r/'docs/engineering-evidence/player-flight-controls-gauntlet/stage5-player-wasdqe';o=r/'build/player-flight-controls-gauntlet/native-hold-oracle';o.mkdir(exist_ok=True)
s=(r/'tests/NovaCore.Simulation.Tests/AssemblyPilotAllocationTests.cs').read_text();methods=s[s.index('    private static ushort OracleMask'):s.index('    private static void Near')]
(o/'Program.cs').write_text((e/'native-hold-oracle.cs.txt').read_text().replace('    ORACLE_METHODS',methods))
b=r/'build/player-flight-controls-gauntlet/candidate/bin/NovaCore.Simulation.Tests/release'
refs=''.join('<Reference Include="'+p.stem+'"><HintPath>'+str(p).replace('\\','/')+'</HintPath></Reference>' for p in b.glob('*.dll') if p.name!='NovaCore.Simulation.Tests.dll')
(o/'NativeHoldOracle.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><AssemblyName>NovaCore.Simulation.Tests</AssemblyName></PropertyGroup><ItemGroup>'+refs+'</ItemGroup></Project>')
