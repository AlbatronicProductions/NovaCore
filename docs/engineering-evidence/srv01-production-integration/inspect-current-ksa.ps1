param([string[]]$Types,[string[]]$Methods,[switch]$List)
$ksaStream = [IO.File]::OpenRead('E:\Kitten Space Agency\KSA.dll')
$ksaPe = [Reflection.PortableExecutable.PEReader]::new($ksaStream)
$ksaMd = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($ksaPe)
function TokenName([int]$tok) {
  if ($tok -eq 0) { return '' }
  $idx = $tok -band 0x00ffffff
  try {
    switch ($tok -band 0xff000000) {
      0x06000000 { $m=$ksaMd.GetMethodDefinition([Reflection.Metadata.Ecma335.MetadataTokens]::MethodDefinitionHandle($idx)); $t=$ksaMd.GetTypeDefinition($m.GetDeclaringType()); return ($ksaMd.GetString($t.Name)+'.'+$ksaMd.GetString($m.Name)) }
      0x04000000 { $f=$ksaMd.GetFieldDefinition([Reflection.Metadata.Ecma335.MetadataTokens]::FieldDefinitionHandle($idx)); $t=$ksaMd.GetTypeDefinition($f.GetDeclaringType()); return ($ksaMd.GetString($t.Name)+'.'+$ksaMd.GetString($f.Name)) }
      0x0a000000 { $m=$ksaMd.GetMemberReference([Reflection.Metadata.Ecma335.MetadataTokens]::MemberReferenceHandle($idx)); return ($m.Parent.Kind.ToString()+':'+$ksaMd.GetString($m.Name)) }
      0x70000000 { return 'string:'+ $ksaMd.GetUserString([Reflection.Metadata.Ecma335.MetadataTokens]::UserStringHandle($idx)) }
      0x02000000 { return $ksaMd.GetString($ksaMd.GetTypeDefinition([Reflection.Metadata.Ecma335.MetadataTokens]::TypeDefinitionHandle($idx)).Name) }
      0x01000000 { return $ksaMd.GetString($ksaMd.GetTypeReference([Reflection.Metadata.Ecma335.MetadataTokens]::TypeReferenceHandle($idx)).Name) }
      0x2b000000 { $s=$ksaMd.GetMethodSpecification([Reflection.Metadata.Ecma335.MetadataTokens]::MethodSpecificationHandle($idx)); return 'generic:'+(TokenName ([Reflection.Metadata.Ecma335.MetadataTokens]::GetToken($s.Method))) }
    }
  } catch { return 'unresolved' }
  return ('token:{0:X8}' -f $tok)
}
$ops = @{}
[Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Public,Static') | ForEach-Object { $o=$_.GetValue($null); $ops[[int]$o.Value -band 65535]=$o }
try {
  foreach($th in $ksaMd.TypeDefinitions) {
    $t=$ksaMd.GetTypeDefinition($th); $tn=$ksaMd.GetString($t.Name)
    if ($Types -and $tn -notin $Types) { continue }
    foreach($mh in $t.GetMethods()) {
      $m=$ksaMd.GetMethodDefinition($mh); $mn=$ksaMd.GetString($m.Name)
      if ($Methods -and $mn -notin $Methods) { continue }
      $token=[Reflection.Metadata.Ecma335.MetadataTokens]::GetToken([Reflection.Metadata.EntityHandle]$mh)
      if($List) { '{0}.{1} {2:X8}' -f $tn,$mn,$token; continue }
      if($m.RelativeVirtualAddress -eq 0){ continue }
      [byte[]]$il=[Reflection.Metadata.PEReaderExtensions]::GetMethodBody($ksaPe,$m.RelativeVirtualAddress).GetILBytes()
      '{0}.{1} {2:X8} {3} bytes SHA256 {4}' -f $tn,$mn,$token,$il.Length,[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($il))
      $pos=0
      while($pos -lt $il.Length) {
        $offset=$pos; [int]$code=$il[$pos++]; if($code -eq 254){$code=0xfe00+$il[$pos++]}; $op=$ops[$code]; $val=''
        switch($op.OperandType.ToString()) {
          'InlineNone' { }
          'ShortInlineI' { $val=[sbyte]::Parse($il[$pos].ToString('X2'),[Globalization.NumberStyles]::HexNumber); $pos++ }
          'ShortInlineVar' { $val=$il[$pos++] }
          'InlineVar' { $val=[BitConverter]::ToUInt16($il,$pos); $pos+=2 }
          'InlineI' { $val=[BitConverter]::ToInt32($il,$pos); $pos+=4 }
          'InlineI8' { $val=[BitConverter]::ToInt64($il,$pos); $pos+=8 }
          'ShortInlineR' { $val=[BitConverter]::ToSingle($il,$pos); $pos+=4 }
          'InlineR' { $val=[BitConverter]::ToDouble($il,$pos); $pos+=8 }
          'ShortInlineBrTarget' { $n=[int]$il[$pos++];if($n -ge 128){$n-=256};$val='{0:X4}' -f ($pos+$n) }
          'InlineBrTarget' { $n=[BitConverter]::ToInt32($il,$pos);$pos+=4;$val='{0:X4}' -f ($pos+$n) }
          'InlineSwitch' { $n=[BitConverter]::ToInt32($il,$pos);$pos+=4;$base=$pos+4*$n;$targets=@();for($i=0;$i -lt $n;$i++){ $targets+=('{0:X4}' -f ($base+[BitConverter]::ToInt32($il,$pos)));$pos+=4 };$val=$targets -join ',' }
          default { $tok=[BitConverter]::ToInt32($il,$pos);$pos+=4;$val=('{0:X8} {1}' -f $tok,(TokenName $tok)) }
        }
        '  {0:X4} {1} {2}' -f $offset,$op.Name,$val
      }
    }
  }
} finally { $ksaPe.Dispose(); $ksaStream.Dispose() }
