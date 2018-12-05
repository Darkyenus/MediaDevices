MD x86
PUSHD x86
	COPY "C:\Windows\SysWOW64\PortableDeviceApi.dll" "PortableDeviceApi.dll"
	COPY "C:\Windows\SysWOW64\PortableDeviceTypes.dll" "PortableDeviceTypes.dll"
	tlbimp /machine:X86 /sysarray /nologo /out:PortableDeviceApiLib.dll PortableDeviceApi.dll
	tlbimp /machine:X86 /sysarray /nologo /out:PortableDeviceTypesLib.dll PortableDeviceTypes.dll
	CALL :disassemblyAndCleanup
POPD


MD x64
PUSHD x64
	COPY "C:\Windows\System32\PortableDeviceApi.dll" "PortableDeviceApi.dll"
	COPY "C:\Windows\System32\PortableDeviceTypes.dll" "PortableDeviceTypes.dll"
	tlbimp /machine:X64 /sysarray /nologo /out:PortableDeviceApiLib.dll PortableDeviceApi.dll
	tlbimp /machine:X64 /sysarray /nologo /out:PortableDeviceTypesLib.dll PortableDeviceTypes.dll
	CALL :disassemblyAndCleanup
POPD

GOTO :eof

:disassemblyAndCleanup
	ildasm PortableDeviceApiLib.dll /out:PortableDeviceApiLib.il
	ildasm PortableDeviceTypesLib.dll /out:PortableDeviceTypesLib.il
	COPY PortableDeviceApiLib.il PortableDeviceApiLib.original.il
	COPY PortableDeviceApiLib.res PortableDeviceApiLib.original.res
	COPY PortableDeviceTypesLib.il PortableDeviceTypesLib.original.il
	COPY PortableDeviceTypesLib.res PortableDeviceTypesLib.original.res
	DEL PortableDeviceApi.dll
	DEL PortableDeviceApiLib.dll
	DEL PortableDeviceTypes.dll
	DEL PortableDeviceTypesLib.dll
EXIT /B

:eof
