<?php
function SkipServiceSelect($exe){
	if ($exe === true) {
		return "[UI]_Skip_Service_Select";
	}
	
	// Find JE SHORT <address>
	$code	=	"\x74\x07\xC6\x05\xAB\xAB\xAB\xAB\x01\x68".pack("I", $exe->str("passwordencrypt","rva"));
	$offset	=	$exe->code($code,	"\xAB");
	if ($offset	===	false) {
		echo "Failed in	part 1";
		return false;
	}
	
	// Skip short jump
	$exe->replace($offset, array(0 =>	"\x90\x90"));
	
	return true;
}
?>