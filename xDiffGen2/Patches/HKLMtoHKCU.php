<?php
function HKLMtoHKCU($exe){
  if ($exe === true) {
    return new xPatch(27, 'HKLM To HKCU', 'Fix');
  }
  
  $code = "\x68\x02\x00\x00\x80";
  $offsets = $exe->code($code, "\xAB", -1);
  if ($offsets === false) {
    echo "Failed in part 1";
    return false;
  }
  
  foreach ($offsets as $offset) {
    $exe->replace($offset, array(1 => "\x01"));
  }
  
  return true;
}
?>