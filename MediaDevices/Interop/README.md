# COM Wrapper Libraries

The project uses an unmanaged library with COM API, PortableDeviceApi and PortableDeviceTypes, which on typical Windows installation live in `C:/Windows/System32/PortableDevice(Api/Types).dll` (64-bit version) and in `C:/Windows/SysWOW64/PortableDevice(Api|Types).dll` (32-bit version).

.NET can use these libraries through COM interop wrapper `dll`s, generated through [`tlbimp`](https://msdn.microsoft.com/en-us/library/ms973800.aspx).
Howerver, the wrapper is not perfect and needs some modifications, which are done in this directory.

## 32/64-bitness
The `PortableDevice(Api|Types).dll` is sadly not architecture agnostic. Native applications don't mind, because they are compiled either for x86 or for x64, not for both, but .NET applications are often expected to not care. When native 32-bit application requests dll from `C:/Windows/System32/`, it gets [redirected](https://stackoverflow.com/questions/949959/why-do-64-bit-dlls-go-to-system32-and-32-bit-dlls-to-syswow64-on-64-bit-windows) to load the 32-bit version from `C:/Windows/SysWOW64/` and all is good. However, by default, .NET can run on either architecture, so it is not possible to rely on one particular `dll`, which complicates things.

Current solution is to provide two libraries, 32-bit only and 64-bit only. For this we need two different sets of COM interop wrapper `dll`s, with 32-bit set living in `x86/` folder and 64-bit in `x64/`. Content of these folders is described in the following section.

**NOTE**: For simplicity, only 32-bit is currently really supported. While the rest of the document provides instructions for 64-bit part as well, this is not yet properly supported in the rest of the library. **When using this library, ensure that you force your application to always run in 32-bit mode.** 64-bit support is not done and using it (even accidentally), will lead to pain, tears, crashes and mysterious memory corruptions.

# Process
Instructions are the same for 32/64-bit, but the values slightly differ. Those differences are marked with "regex" syntax: (32-bit variant|64-bit variant).
You don't need to do these steps in most cases, even when building the library. They serve just as a documentation of what has been done.
If you need to modify the wrappers, start from step 7.

1. Go to the appropriate `x86|x64` directory
2. Copy the system's `PortableDevice(Api|Types).dll` from `C:/Windows/(SysWOW64|System32)/` to this directory
	- Directories are **not** switched around, `SysWOW64` really does hold 32-bit libraries and vice-versa
	- You can verify that you have the correct version with [`dumpbin /headers`](https://stackoverflow.com/a/495369) command
	- This is done to ensure that the System32/SysWOW64 redirection does not cause problems in following commands
3. Generate the wrapper `dll`s with
	- `tlbimp /machine:(X86|X64) /sysarray /nologo /out:PortableDeviceApiLib.dll` PortableDeviceApi.dll
	- `tlbimp /machine:(X86|X64) /sysarray /nologo /out:PortableDeviceTypesLib.dll` PortableDeviceTypes.dll
	- Commands will typically generate some warnings, but no errors (see below, section tlbimp warnings)
	- Notice the addition of `Lib` suffix. This is done only for historical reasons and to differentiate from system `dll`s.
4. Disassembly generated `dll`s with
	- `ildasm PortableDeviceApiLib.dll /out:PortableDeviceApiLib.il`
	- `ildasm PortableDeviceTypesLib.dll /out:PortableDeviceTypesLib.il`
	- This will generate the specified .il files, possibly with extra .res files
5. Delete all four dlls generated so far - we won't need them anymore and we don't want them in the source tree
6. Backup remaining files (generated in step 4) by copying them and adding `.original` before their extension (so that `PortableDeviceApiLib.il` becomes `PortableDeviceApiLib.original.il`)
	- These files will never change in following steps, so that it is always possible to diff them against the working copies to see what has been changed so far
7. Apply the modifications to the `.il` files. Already performed modifications are described below.
8. Assembly the `.il` files back into `.dll`s. They are now ready to be used by the library.
	- `ilasm /dll /res:PortableDeviceApiLib.res /out:PortableDeviceApiLib.dll PortableDeviceApiLib.il`
	- `ilasm /dll /res:PortableDeviceTypesLib.res /out:PortableDeviceTypesLib.dll PortableDeviceTypesLib.il`
	- Files specified in `/res:` are the ones generated in step 4. Which files were generated for the particular `.il` files is usually also specified in a comment at the end of the `.il` file. Bunding resources into the `dll`s is not absolutely necessary, but is good for completeness.

Make sure you are using the correct version of `ilasm`/`ildasm`, eg. if you are building a .net 4.0 project use the one in `C:\Windows\Microsoft.NET\Framework64\v4.0.30319`.
	
# `.il` Modifications
1. Replace first parameter of
	- `GetDevices` and `GetPrivateDevices` (of `IPortableDeviceManager` and `PortableDeviceManagerClass`) from
	- `[in][out] string&  marshal( lpwstr) pPnPDeviceIDs`
	- to
	- [in][out] string[]  marshal( lpwstr[]) pPnPDeviceIDs`
2. For all instances of (in PortableDeviceApi)
	- `GetDeviceFriendlyName`, `GetDeviceDescription` and `GetDeviceManufacturer` (of `IPortableDeviceManager` and `PortableDeviceManagerClass`)
	- fix the marshalling for the unicode strings they return by changing
	- `[in][out] uint16&`
	- to
	- `[in][out] char[] marshal([])`
3. In method `Next` of `interface` `IEnumPortableDeviceObjectIDs` (in PortableDeviceApi) change the second argument from
	- `[out] string&  marshal( lpwstr) pObjIDs,`
	- to
	- `[out] string[] marshal( lpwstr[]) pObjIDs,`

First two modifications, together with this general approach, were first described by [Andrew Trevarrow](http://andrewt.com/2013/06/15/fun-with-mtp-in-c.html).

# Security
If you don't want to trust random pre-built `dll`s from the web, the easiest way to create your own would be to follow steps 2-5, the diffing your own `.il` files with `.original.il` files to verify that there are no modifications (some comments/GUIDs are randomly generated, so the files won't be 100% equal). Then diff `.original.il` with the project's `.il` files, to verify that only the modifications described here were made. Then build your own `dll`s from the project's files as described in step 8.

# `tlbimp` Warnings
As mentioned above, PortableDevice(Api|Types).dll generate some warnings when bindings are generated for them.
This is probably due to some shortcomings in the `tlbimp`, but can be worked around and is slowly getting better. For future reference, here are warnings printed by the last run of `tlbimp`.
Warnings for x86 were the same as the warnings for x64.

### PortableDeviceApi
```
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceApiLib.IPortableDeviceValues.GetBufferValue' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagBSTRBLOB.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagBLOB.pBlobData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAC.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAUB.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAI.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAUI.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAL.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAUL.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAFLT.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCADBL.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCABOOL.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCASCODE.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.pcVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.pbVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.piVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.puiVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.plVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.pulVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.pintVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.puintVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.pfltVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.pdblVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.pboolVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL___MIDL_itf_PortableDeviceApi_0001_0000_0001.pscode'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCLIPDATA.pClipData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagRemSNB.rgString'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._BYTE_SIZEDARR.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._SHORT_SIZEDARR.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._LONG_SIZEDARR.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._HYPER_SIZEDARR.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._wireSAFEARRAY.rgsabound'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._wireSAFEARR_BSTR.aBstr'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._FLAGGED_WORD_BLOB.asData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._wireSAFEARR_UNKNOWN.apUnknown'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._wireSAFEARR_DISPATCH.apDispatch'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._wireSAFEARR_VARIANT.aVariant'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pbVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.piVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.plVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pllVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pfltVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pdblVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pboolVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pscode'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pcVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.puiVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pulVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pullVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.pintVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.__MIDL_IOleAutomationTypes_0004.puintVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._wireBRECORD.pRecord'.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceApiLib.ITypeInfo.RemoteGetTypeAttr' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceApiLib.ITypeInfo.RemoteGetFuncDesc' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceApiLib.ITypeInfo.RemoteGetVarDesc' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagARRAYDESC.rgbounds'.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceApiLib.ITypeComp.RemoteBind' cannot be marshaled by the runtime marshaler.  Such arguments will therefore bepassed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagFUNCDESC.lprgscode'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagFUNCDESC.lprgelemdescParam'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagPARAMDESC.pparamdescex'.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceApiLib.ITypeLib.RemoteGetLibAttr' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._wireSAFEARR_BRECORD.aRecord'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib._wireSAFEARR_HAVEIID.apUnknown'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAH.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAUH.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCACY.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCADATE.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAFILETIME.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCACLSID.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCACLIPDATA.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCABSTR.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCABSTRBLOB.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCALPSTR.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCALPWSTR.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceApiLib.tagCAPROPVARIANT.pElems'.
```

### PortableDeviceTypes
```
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.IWpdSerializer.GetBufferFromIPortableDeviceValues' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.IPortableDeviceValues.GetBufferValue' cannot be marshaled by the runtime marshaler.  Such argumentswill therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagBSTRBLOB.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagBLOB.pBlobData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAC.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAUB.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAI.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAUI.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAL.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAUL.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAFLT.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCADBL.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCABOOL.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCASCODE.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.pcVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.pbVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.piVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.puiVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.plVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.pulVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.pintVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.puintVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.pfltVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.pdblVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.pboolVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL___MIDL_itf_PortableDeviceTypes_0003_0015_0001.pscode'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCLIPDATA.pClipData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagRemSNB.rgString'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._BYTE_SIZEDARR.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._SHORT_SIZEDARR.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._LONG_SIZEDARR.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._HYPER_SIZEDARR.pData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._wireSAFEARRAY.rgsabound'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._wireSAFEARR_BSTR.aBstr'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._FLAGGED_WORD_BLOB.asData'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._wireSAFEARR_UNKNOWN.apUnknown'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._wireSAFEARR_DISPATCH.apDispatch'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._wireSAFEARR_VARIANT.aVariant'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pbVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.piVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.plVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pllVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pfltVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pdblVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pboolVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pscode'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pcVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.puiVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pulVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pullVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.pintVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.__MIDL_IOleAutomationTypes_0004.puintVal'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._wireBRECORD.pRecord'.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.ITypeInfo.RemoteGetTypeAttr' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.ITypeInfo.RemoteGetFuncDesc' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.ITypeInfo.RemoteGetVarDesc' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagARRAYDESC.rgbounds'.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.ITypeComp.RemoteBind' cannot be marshaled by the runtime marshaler.  Such arguments will thereforebe passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagFUNCDESC.lprgscode'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagFUNCDESC.lprgelemdescParam'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagPARAMDESC.pparamdescex'.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.ITypeLib.RemoteGetLibAttr' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._wireSAFEARR_BRECORD.aRecord'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib._wireSAFEARR_HAVEIID.apUnknown'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAH.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAUH.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCACY.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCADATE.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAFILETIME.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCACLSID.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCACLIPDATA.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCABSTR.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCABSTRBLOB.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCALPSTR.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCALPWSTR.pElems'.
TlbImp : warning TI3016 : The type library importer could not convert the signature for the member 'PortableDeviceTypesLib.tagCAPROPVARIANT.pElems'.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.WpdSerializerClass.GetBufferFromIPortableDeviceValues' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
TlbImp : warning TI3015 : At least one of the arguments for 'PortableDeviceTypesLib.PortableDeviceValuesClass.GetBufferValue' cannot be marshaled by the runtime marshaler.  Such arguments will therefore be passed as a pointer and may require unsafe code to manipulate.
```