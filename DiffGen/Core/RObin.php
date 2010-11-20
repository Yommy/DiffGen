<?php
/*
RObin.php

This file creates the RObin class, which is used to
apply modifications to the Ragnarok Online client.

*/

class RObin
{
	public /*protected*/ $exe = "";
	public /*protected*/ $size = 0;
	public $dif = array();
	
	private $PEHeader = null;
	private $image_base = 0;
	private $sections;
	private $client_date = 0;
	
	// Loads file from $path
	public function load($path)
	{
		$file = file_get_contents($path);
		if ($file === false) {
			return false;
		}
		$this->exe = $file;
		$this->size = strlen($file);
		
		$this->PEHeader = $this->match("\x50\x45\x00\x00");
		echo "PE Header\t".dechex($this->PEHeader)."h\n";
		
		// If the loaded file isn't a valid PE file, then it's not necessary to continue
		// with the diff process anyway, so just die~ >:)
		if($this->PEHeader === false)
			die("Invalid PE file used!\n");
			
		$this->image_base = $this->read($this->PEHeader + 0x34, 4, "V");
		echo "Image Base\t".dechex($this->image_base)."h\n";
		
		$date = $this->read($this->PEHeader+8, 4, 'V');
		$this->client_date = date('Y', $date) * 10000 + date('m', $date) * 100 + date('d', $date);
		echo "Client Date\t".$this->client_date."\n";
		
		echo "\nName\tvSize\tvOffset\trSize\trOffset\tvrDiff\n";
		echo "----\t-----\t-------\t-----\t-------\t------\n";
		// Get section information
		$sectionCount = $this->read($this->PEHeader + 0x6, 2, "S");
		for($i = 0, $curSection = $this->PEHeader + 0x18 + 0x60 + 0x10 * 0x8; $i < $sectionCount; $i++) {
			// http://www.microsoft.com/whdc/system/platform/firmware/PECOFFdwn.mspx
			$sectionInfo['name'] = $this->read($curSection, 8);
			// Also: There's also possibility that a new inserted section name could contain some trash bytes after
			// the zero terminator. So get rid of them..
			$sectionInfo['name'] = substr($sectionInfo['name'], 0, strpos($sectionInfo['name'], "\x00"));
			
			$sectionInfo['vSize'] 		= $this->read($curSection+8+0*4, 4, "V");
			$sectionInfo['vOffset'] 	= $this->read($curSection+8+1*4, 4, "V");
			$sectionInfo['rSize'] 		= $this->read($curSection+8+2*4, 4, "V");
			$sectionInfo['rOffset'] 	= $this->read($curSection+8+3*4, 4, "V");
			$sectionInfo['vrDiff']		= dechex($sectionInfo['vOffset'] - $sectionInfo['rOffset']);
			echo "$sectionInfo[name]\t$sectionInfo[vSize]\t$sectionInfo[vOffset]\t$sectionInfo[rSize]\t$sectionInfo[rOffset]\t0x$sectionInfo[vrDiff]\n";
			// Convert to object for easier access
			// E.g: $exe->getSection(".rdata")->rOffset...
			$this->sections[$sectionInfo['name']] = new stdClass();
			if(is_array($sectionInfo) && count($sectionInfo) > 0) {
				foreach($sectionInfo as $name => $value) {
					if (!empty($name))
						$this->sections[$sectionInfo['name']]->$name = $value;
				}
			}
			
			$curSection += 0x28;
		}
		
		return true;
	}

	// Reads $size bytes starting at $offset, might format the return using
	// $format, if provided. (see http://www.php.net/pack for more details)
	// Returns raw data if no format specified, a single variable (like an
	// integer or a float) if format only contains one, or an array with the
	// unpack()'ed data.
	public function read($offset, $size, $format = null)
	{
		if (($offset >= $this->size) || ($size < 1)) {
			return false;
		}
		$data = substr($this->exe, $offset, $size);
		if (strlen($data) != $size) {
			// if read size was not $size
			echo bin2hex($data) . "\n";
			return false;
		}
		if (is_string($format)) {
			// If $format is a string, it tries to unpack()
			$data = unpack($format, $data);
			if ($data === false) {
				// Bad format
				return false;
			}
			// Little hack to make code simpler when you want to read only one
			// type of data, like reading a single integer:
			// $this->read($offset, $size, "I")
			// It'll return only the integer, instead of an array of only 1 position with it.
			if ((count($data) == 1) && (isset($data[1]))) {
				$data = $data[1];
			}
		}
		return $data;
	}
	
	// Searches for $pattern (using $wildcard as wildcard), starting at $start, and
	// stopping at $finish (if omitted, it'll search until the end of the file). Returns
	// the first offset it matches, or false if none matched.
	public function match($pattern, $wildcard = "", $start = null, $finish = null)
	{
		$length = strlen($pattern);
		$start = (is_null($start) || ($start <= 0) ? 0 : $start);
		$finish = (is_null($finish) || ($finish > $this->size) ? $this->size : $finish);
		if (($length < 1) || ($start >= $this->size-$length) || ($finish <= $start)) {
			return false;
		}
		$pos = 0;
		$offset = $start;
		// Is there a wildcard?
		if (strlen($wildcard) == 1) {
			// Remove wildcards at the ending of the pattern
			while ($pattern[strlen($pattern) - 1] == $wildcard) {
				$pattern = substr($pattern, 0, strlen($pattern) - 1);
			}
			// Check if wildcard appears in the pattern
			$wpos = strpos($pattern, $wildcard);
			if ($wpos === false) {
				// If not... then we don't need it
				$wildcard = "";
			}
		}
		// Check to see if there's a wildcard
		if (strlen($wildcard) == 1) {
			// If there's a wildcard...
			// First separate it in pieces (offset and value/strlen)
			$exploded = explode($wildcard, $pattern);
			$offset = 0;
			$pieces = array();
			foreach ($exploded as $key => $value)
			{
				if (empty($value) === false)
				{
					if ($key != 0)
					{
						$pieces[$offset] = array($value, strlen($value));
					}
					else
					{
						$partial = $value;
					}
					$offset += strlen($value);
				}
				$offset++;
			}
			
			// Then search for the first part and try to match the rest
			for ($i = strpos($this->exe, $partial, $start); ($i !== false) && ($i < $finish); $i = strpos($this->exe, $partial, $i + 1))
			{
				foreach ($pieces as $offset => $value)
				{
					if (substr_compare($this->exe, $value[0], $i + $offset, $value[1]) != 0)
					{
						continue 2;
					}
				}
				return $i;
			}
		} else {
			// If not, ordinary strpos() can do it
			$i = strpos($this->exe, $pattern, $start);
			return ($i >= $finish ? false : $i);
		}
		return false;
	}
	
	// Does the same as match(), but it returns an array
	// of all the matching offsets (might be empty)
	public function matches($pattern, $wildcard = "", $start = null, $finish = null)
	{
		$offsets = array();
		$offset = $start;
		while ($offset = $this->match($pattern, $wildcard, $offset + strlen($pattern), $finish)) {
			$offsets[] = $offset;
		}
		return $offsets;
	}
	
	// Returns an offset where there are $size null bytes, searching
	// on the space after .text section ends and before .rdata begins.
	public function zeroed($size)
	{
		$zeroed = str_repeat("\x00", $size);
		$sectionNames = array(".text", ".rdata", ".data", ".rsrc");
		
		$zero = false;
		foreach ($sectionNames as $name) {
			$section = $this->getSection($name);
			if($section === false)
				continue;
			
			if (($section->rSize - $section->vSize) >= $size) {
				$offset = $this->match($zeroed, "", $section->rOffset + $section->vSize, $section->rOffset + $section->rSize);
				if ($offset !== false) {
					$zero = $offset;
					break;
				}
			}
		}
		return $zero;
	}
	
	// It was meant to be used for patches that add extra code, where it would
	// replace null bytes (checking if they were really null). Works like replace().
	public function insert($code, $offset)
	{
		$length = strlen($code);
		if ($length < 1) {
			return false;
		}
		for ($i = 0; $i < $length; $i++) {
			if ($this->exe[$offset + $i] != $code[$i]) {
				$poffset = strtoupper(dechex($offset + $i));
				$pvalue1 = str_pad(strtoupper(dechex(ord($this->exe[$offset + $i]))),2,"0", STR_PAD_LEFT);
				$pvalue2 = str_pad(strtoupper(dechex(ord($code[$i]))),2,"0", STR_PAD_LEFT);
				$this->dif[] = $poffset.":".$pvalue1.":".$pvalue2;
			}
			$this->exe[$offset + $i] = $code[$i];
		}
		return true;
	}
	
	// Replaces stuff starting with a base offset of $offset. $replace is an array
	// Where the key (index) tells the relative offset, and the value is the data
	// that will replace existing data.
	// For instance:
	// replace(400, array(4 => "\x00", 2 => "\xAB"))
	// Replaces the byte at 404 (400 + 4) with a null (x00) byte;
	// Replaces the byte at 402 (400 + 2) with a xAB byte.
	public function replace($offset, $replace)
	{
		foreach ($replace as $pos => $value) {
			for ($i = 0; $i < strlen($value); $i++) {
				if ($this->exe[$offset + $pos + $i] != $value[$i]) {
					$poffset = strtoupper(dechex($offset + $pos + $i));
					$pvalue1 = str_pad(strtoupper(dechex(ord($this->exe[$offset + $pos + $i]))),2,"0", STR_PAD_LEFT);
					$pvalue2 = str_pad(strtoupper(dechex(ord($value[$i]))),2,"0", STR_PAD_LEFT);
					$this->dif[] = $poffset.":".$pvalue1.":".$pvalue2;
				}
				$this->exe[$offset + $pos + $i] = $value[$i];
			}
		}
		return true;
	}
	
	// Returns an array with the changes made since last diff() call.
	public function diff()
	{
		$diff = $this->dif;
		$this->dif = array();
		return $diff;
	}
	
	// Searchs for $code pattern (using $wildcard) , which should match
	// _exactly_ $count times. Returns the offset (or an array of offsets)
	// if it works, or false if the pattern doesn't match exactly $count times.
	// That means if you pass $count as 2, and it finds the pattern once or
	// 3 times, it'll return false. Also, it searches only in .text section.
	// Please note it's meant to be used for searching for code (machine
	// code the client runs, "assembly"). Use matches() for general search.
	// Change: now passing -1 to $count will make the function return all
	// matches.
	public function code($code, $wildcard, $count = 1)
	{
		$section = $this->getSection(".text");
		$offsets = $this->matches($code, $wildcard, $section->rOffset, $section->rOffset + $section->rSize);
		
		if (($count != -1) && (count($offsets) != $count)){
			echo "#code() found ".count($offsets)." matches# ";
			return false;
		}
		if (($count == -1) && count($offsets) == 0){
			echo "#code() found no matches# ";
			return false;
		}
		return ($count == 1 ? $offsets[0] : $offsets);
	}
	
	// Searches for string $str in .rdata section of the client (where
	// strings are located). Returns the address (to be used with
	// asm stuff, NOT offset inside the client exe). Returns the
	// address on success, or false on failure.
	public function str($str)
	{
		$iBase = $this->imagebase();
		$section = $this->getSection(".rdata");
		$virtual = $section->vOffset - $section->rOffset;
		// Strings are null terminated, hence $str . "\x00"
		$offset = $this->match("\x00".$str."\x00", "", $section->rOffset, $section->rOffset + $section->rSize) + 1;
		if ($offset === false) {
			return false;
		}
		return $offset + $virtual + $iBase;
	}
	
	// I don't really understand how it works (assembly-wise), but...
	// Searches for a function on .data section, and returns the address to be
	// used in asm stuff (call instructions, I guess), or false on failure.
	// Some functions work by just searching for their names ($str = true),
	// others however have to be looked for using numbers ($str = false).
	// Used in both ways on Enable DNS Support patch.
	public function func($func, $str = true)
	{
		$iBase = $this->imagebase();
		$section = $this->getSection(".data");
		$virtual = $section->vOffset - $section->rOffset;
		if ($str) {
			// It has to resolve the name or something... can't remember
			$offset = $this->match($func . "\x00", "", $section->rOffset, $section->rOffset + $section->rSize);
			$code = pack("I", $offset - 2);
		} else {
			$code = $func;
		}
		$offset = $this->match($code, "", $section->rOffset, $section->rOffset + $section->rSize);
		if ($offset === false) {
			return false;
		}
		return $offset + $virtual + $iBase;
	}
	
	// Converts a string representing a byte sequence into the aequivalent unicode sequence
	static public function Hex($string, $flag = false)
	{
		$trimmed = preg_replace('/\s*/m', '', $string);
		$ret = "";
		for($i = 0; $i < strlen($trimmed); $i += 2)
			$ret .= pack('C', intval($trimmed[$i].(($flag == false) ? (isset($trimmed[$i+1]) ? $trimmed[$i+1] : '0') : $trimmed[$i+1]), 16));
		
		return $ret;
	}
	
	// Workaround for public access, 'cause they shouldn't be changed outside the class
	public function PEHeader() {return $this->PEHeader;}
	public function imagebase() {return $this->image_base;}
	// Returns the client date
	public function clientdate(){return $this->client_date;}
	
	// Returns the section specified by name
	public function getSection($name)
	{
		if(!isset($this->sections[$name]))
			return false;
			
		return $this->sections[$name];
	}
}
?>