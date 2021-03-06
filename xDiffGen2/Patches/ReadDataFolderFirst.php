<?php
    function ReadDataFolderFirst($exe) {
        if ($exe === true) {
            return new xPatch(35, 'Read Data Folder First', 'Data', 0, 'Gives the data directory contents priority over the data/sdata.grf contents.');
        }
        // strings for pattern search
        $readfolder = pack("I", $exe->str("readfolder","rva"));
        $loading    = pack("I", $exe->str("loading","rva"));
        $code =  "\x68" . $readfolder           // push    offset aReadfolder ; "readfolder"
                ."\x8B\xAB"                     // mov     ecx, ebp
                ."\xE8\xAB\xAB\xAB\xAB"         // call    XMLElement::FindChild
                ."\x85\xC0"                     // test    eax, eax
                ."\x74\x07"                     // jz      short loc_543B67  <- remove conditional jump
                ."\xC6\x05\xAB\xAB\xAB\xAB\x01" // mov     Readfolder, 1
                ."\x68" . $loading;             // push    offset aLoading ; "loading"
        $offset = $exe->code($code, "\xAB");
        if ($offset === false) {
            echo "Failed in part 1";
            return false;
        }
        $exe->replace($offset, array(14 => "\x90\x90"));
        
        $readbyte = $exe->read($offset+18, 4);  // store variable address of ReadFolder
        //echo $exe->clientdate();
		if($exe->clientdate() < 20120101){ // not sure of actual date, 
			$patch_offset = 14;
			$code =  "\x80\x3D" .$readbyte . "\x00" // cmp     Readfolder, 0
					."\x57"                         // push    edi
					."\xB9\xAB\xAB\xAB\x00"         // mov     ecx, offset unk_84FCAC
					."\x56"                         // push    esi
					."\x74\x23";                    // jz      short loc_55FDFB
			$offset = $exe->code($code, "\xAB");
			if ($offset === false) {
				echo "Failed in part 2";
				return false;
			}
		} else if ($exe->clientdate() <= 20130605) {
			$patch_offset = 19;
			$code =  "\x80\x3D" .$readbyte . "\x00" // cmp     Readfolder, 0
					."\x53"                         // push    ebx
					."\x8B\xAB\xAB\xAB"             // mov     ebx, offset unk_84FCAC
					."\x57"                         // push    edi
					."\x8B\xAB\xAB\xAB"             //
					."\x57"
					."\x53"
					."\x74\xAB";
			$offset = $exe->code($code, "\xAB");
			if ($offset === false) {
				echo "Failed in part 3";
				return false;
			}
		}
		else {
			$patch_offset = 17;
			$code =  "\x80\x3D" .$readbyte . "\x00" // cmp     Readfolder, 0
					."\x53"                         // push    ebx
					."\x8B\xAB\xAB"             // mov     ebx, offset unk_84FCAC
					."\x57"                         // push    edi
					."\x8B\xAB\xAB"             //
					."\x57"
					."\x53"
					."\x74\xAB";
			$offset = $exe->code($code, "\xAB");
			if ($offset === false) {
				echo "Failed in part 3";
				return false;
			}		
		}
        $exe->replace($offset, array($patch_offset => "\x90\x90"));
        return true;
    }
?>