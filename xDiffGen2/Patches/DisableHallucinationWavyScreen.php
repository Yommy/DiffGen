<?php
    function DisableHallucinationWavyScreen($exe) {
        if ($exe === true) {
            return new xPatch(14, 'Disable Hallucination Wavy Screen', 'Fix', 0, 'Disables the Hallucination effect (screen becomes wavy and lags the client), used by baphomet, horongs, and such.');
        }
		
		if ($exe->clientdate() <= 20130605) {
			$code =  "\x83\xC6\xAB" 				// add     esi, 6Ch
					."\x89\x3D\xAB\xAB\xAB\xAB";	// mov     dword_C08A84, edi
		}
		else {
			$code =  "\x8D\x4E\xAB" 				// lea     ecx, [esi+6Ch]
					."\x89\x3D\xAB\xAB\xAB\xAB";	// mov     dword_C08A84, edi
		}
		
        $offset = $exe->code($code, "\xAB");
        if ($offset === false) {
            echo "Failed in part 1";
            return false;
        }
        $dword = $exe->read($offset + 5, 4);
        //echo bin2hex($dword) . "#";
		
        $code =  "\x8B\xCF"
                ."\xE8\xAB\xAB\xAB\xAB"
                ."\x83\x3D" . $dword . "\x00"
                ."\x0F\x84\xAB\xAB\xAB\xAB";
        $offset = $exe->code($code, "\xAB");
        if ($offset === false) {
            echo "Failed in part 2";
            return false;
        }
        $exe->replace($offset, array(14 => "\x90\xE9"));
        return true;
    }
?>