PUSHD x86
	ilasm /nologo /dll /quiet /32bitpreferred /res:PortableDeviceApiLib.res /out:PortableDeviceApiLib.dll PortableDeviceApiLib.il
	ilasm /nologo /dll /quiet /32bitpreferred /res:PortableDeviceTypesLib.res /out:PortableDeviceTypesLib.dll PortableDeviceTypesLib.il
POPD

PUSHD x64
	ilasm /nologo /dll /quiet /X64 /PE64 /res:PortableDeviceApiLib.res /out:PortableDeviceApiLib.dll PortableDeviceApiLib.il
	ilasm /nologo /dll /quiet /X64 /PE64 /res:PortableDeviceTypesLib.res /out:PortableDeviceTypesLib.dll PortableDeviceTypesLib.il
POPD
