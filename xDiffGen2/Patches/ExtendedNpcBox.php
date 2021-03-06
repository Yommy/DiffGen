<?php
function ExtendedNpcBox($exe) {
    if ($exe === true) {
        return new xPatch(69, 'ExtendNpcBox', 'UI', 0, '');
    }
	// 04 08 (2052) to 00 10 (4096)
	if ($exe->clientdate() <= 20130605)
		$code =  "\x81\xEC\x08\x08\x00\x00" 				 // SUB     ESP,808h
				."\xA1\xAB\xAB\xAB\x00" 					 // MOV     EAX,DWORD PTR DS:[___security_cookie]
				."\x33\xC4"									 // XOR     EAX,ESP
				."\x89\x84\x24\x04\x08\x00\x00" 			 // MOV     DWORD PTR SS:[ESP+804h],EAX
				."\x56"										 // push    esi
				."\x8B\xC1"									 // mov     eax, ecx
				."\x57"										 // push    edi
				."\x8B\xBC\x24\x14\x08\x00\x00";			 // mov     edi, [esp+810h+arg_0]
	else
		$code =  "\x81\xEC\x08\x08\x00\x00"					 // SUB     ESP,808h
				."\xA1\xAB\xAB\xAB\x00"						 // MOV     EAX,DWORD PTR DS:[___security_cookie]
				."\x33\xC5"									 // XOR     EAX,ESP
				."\x89\x45\xFC"								 // MOV     DWORD PTR SS:[EBP],EAX
				."\x56"										 // push    esi
				."\x8B\xC1"									 // mov     eax, ecx
				."\x57"										 // push    edi
				."\x8B\x7D\x08"								 // mov     edi, [ebp+arg_0]
				."\xC7\x80\xE0\x02\x00\x00\x01\x00\x00\x00"; // mov     dword ptr [eax+2E0h], 1
          
    $offset = $exe->match($code, "\xAB");

    if ($offset === false) {
        echo "Failed in part 1";
        return false;
    }

    $exe->replace($offset, array(2 => "\x04\x10"));
	
	if ($exe->clientdate() <= 20130605){
		$exe->replace($offset, array(16 => "\x00\x10"));
		$exe->replace($offset, array(27 => "\x10\x10"));

		$code =  "\xFF\xD2\x8B\x8C\x24\x0C\x08\x00\x00\x5F\x5E\x33\xCC\xE8\xAB\xAB\x0C\x00\x81\xC4\x08\x08\x00\x00";
			  
		$offset = $exe->match($code, "\xAB");

		if ($offset === false) {
			echo "Failed in part 2";
			return false;
		}

		$exe->replace($offset, array(5 => "\x08\x10"));
		$exe->replace($offset, array(20 => "\x04\x10"));	

	}
	
    return true;
	
}
?>