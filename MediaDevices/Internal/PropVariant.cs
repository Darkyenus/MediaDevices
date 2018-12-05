using System;
using System.Runtime.InteropServices;
using PROPVARIANT = PortableDeviceApiLib.tag_inner_PROPVARIANT;
using System.Diagnostics;
using System.Security;

namespace MediaDevices.Internal {

    /// <summary>
    /// Utility extension functions for nicer API access to PROPVARIANT struct.
    /// See: https://docs.microsoft.com/en-us/windows/desktop/api/propidl/ns-propidl-tagpropvariant
    /// </summary>
    internal static class PropVariantUtil {

        public static VarType GetVarType(this PROPVARIANT pv) {
            return (VarType)pv.vt;
        }
        
        private static bool CheckVarType(PROPVARIANT pv, VarType expectedType) {
            VarType type = pv.GetVarType();
            if (type == expectedType) 
                return true;
            if (type == VarType.VT_ERROR)
                return false;
            Trace.WriteLine($"Warning: Expected PropVariant.VarType {expectedType}, got {type}");
            return false;
        }

        private static bool CheckVarType(PROPVARIANT pv, VarType expectedType1, VarType expectedType2) {
            VarType type = pv.GetVarType();
            if (type == expectedType1 || type == expectedType2)
                return true;
            if (type == VarType.VT_ERROR)
                return false;
            Trace.WriteLine($"Warning: Expected PropVariant.VarType {expectedType1} or {expectedType2}, got {type}");
            return false;
        }

        public static sbyte GetSByte(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_I1))
                return pv.inner.cVal;
            else return (sbyte)FallbackGetInteger(pv);
        }

        public static byte GetByte(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_UI1))
                return pv.inner.bVal;
            else return (byte)FallbackGetInteger(pv);
        }

        public static short GetShort(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_I2))
                return pv.inner.iVal;
            else return (short)FallbackGetInteger(pv);
        }

        public static ushort GetUShort(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_UI2))
                return pv.inner.uiVal;
            else return (ushort)FallbackGetInteger(pv);
        }

        public static int GetInt(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_I4, VarType.VT_INT))
                return pv.inner.lVal;
            else return (int)FallbackGetInteger(pv);
        }

        public static uint GetUInt(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_UI4, VarType.VT_UINT))
                return pv.inner.ulVal;
            else return (uint)FallbackGetInteger(pv);
        }

        // Skipped intVal - same as lVal

        // Skipped uintVal - same as ulVal

        public static long GetLong(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_I8))
                return pv.inner.hVal.QuadPart;
            else return FallbackGetInteger(pv);
        }

        public static ulong GetULong(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_UI8))
                return pv.inner.uhVal.QuadPart;
            else return (ulong)FallbackGetInteger(pv);
        }

        public static float GetFloat(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_R4))
                return pv.inner.fltVal;
            else if (pv.GetVarType() == VarType.VT_R8)
                return (float)GetDouble(pv);
            else return FallbackGetInteger(pv);
        }

        public static double GetDouble(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_R8))
                return pv.inner.dblVal;
            else if (pv.GetVarType() == VarType.VT_R4)
                return GetFloat(pv);
            else return FallbackGetInteger(pv);
        }

        public static bool GetBool(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_BOOL))
                return pv.inner.boolVal != 0;
            else return FallbackGetInteger(pv) != 0;
        }

        public static HResult GetErrorCode(this PROPVARIANT pv) {
            if (pv.GetVarType() == VarType.VT_ERROR) {
                return (HResult)pv.inner.scode;
            } else {
                return HResult.S_OK;
            }
        }

        public static DateTime? GetDate(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_DATE))
                return DateTime.FromOADate(pv.inner.date);
            else return null;
        }

        // Skipped FILETIME filetime;

        public static Guid GetGuid(this PROPVARIANT pv) {
            if (CheckVarType(pv, VarType.VT_CLSID))
                // Hack: puuid seems to be missing, but we can use pcVal as a pointer type
                return (Guid)Marshal.PtrToStructure(pv.inner.pcVal, typeof(Guid));
            else return Guid.Empty;
        }
        
        // Skipped CLIPDATA* pclipdata;
        // Skipped BSTR bstrVal;
        // Skipped BSTRBLOB bstrblobVal;
        // Skipped BLOB blob;
        
        /// If this PROPVARIANT contains a string (VarType VT_LPSTR or VT_LPWSTR), returns it.
        /// Otherwise returns null and logs a warning.
        public static string GetString(this PROPVARIANT pv) {
            switch (pv.GetVarType()) {
                case VarType.VT_LPSTR:
                    // Hack: pszVal seems to be missing, but pcVal has equal semantics
                    return Marshal.PtrToStringAnsi(pv.inner.pcVal);
                case VarType.VT_LPWSTR:
                    // Hack: pwszVal seems to be missing, but pcVal has the correct type
                    return Marshal.PtrToStringUni(pv.inner.pcVal);
                case VarType.VT_BSTR:
                    // Hack: bstrVal seems to be missing, but bstrblobVal has the same type
                    Trace.WriteLine("GetString: BSTR is untested. If you see this message and everything works fine, you can remove this message.");
                    return Marshal.PtrToStringBSTR(pv.inner.bstrblobVal.pData);
                case VarType.VT_ERROR:
                    return null;
                default:
                    Trace.WriteLine("GetString: Unknown or non string varType "+pv.GetVarType());
                    return null;
            }
        }
        
        // Skipped IUnknown* punkVal;
        // Skipped IDispatch* pdispVal;
        // Skipped IStream* pStream;
        // Skipped IStorage* pStorage;
        // Skipped LPVERSIONEDSTREAM pVersionedStream;
        // Skipped LPSAFEARRAY parray;
        // Skipped CAC cac;

        public static byte[] GetByteArray(this PROPVARIANT pv) {
            if (!CheckVarType(pv, VarType.VT_VECTOR | VarType.VT_UI1)) {
                return null;
            }

            Trace.WriteLine("GetString: BSTR is untested. If you see this message and everything works fine, you can remove this message.");

            uint size = pv.inner.caub.cElems;
            byte[] result = new byte[size];
            unsafe {
                byte* basePtr = (byte*)pv.inner.caub.pElems.ToPointer();
                for (uint i = 0; i < size; i++) {
                    result[i] = basePtr[i];
                }
            }
            return result;
        }
        
        // Skipped CAI cai;
        // Skipped CAUI caui;
        // Skipped CAL cal;
        // Skipped CAUL caul;
        // Skipped CAH cah;
        // Skipped CAUH cauh;
        // Skipped CAFLT caflt;
        // Skipped CADBL cadbl;
        // Skipped CABOOL cabool;
        // Skipped CASCODE cascode;
        // Skipped CACY cacy;
        // Skipped CADATE cadate;
        // Skipped CAFILETIME cafiletime;
        // Skipped CACLSID cauuid;
        // Skipped CACLIPDATA caclipdata;
        // Skipped CABSTR cabstr;
        // Skipped CABSTRBLOB cabstrblob;
        // Skipped CALPSTR calpstr;
        // Skipped CALPWSTR calpwstr;
        // Skipped CAPROPVARIANT capropvar;
        // Skipped CHAR* pcVal;
        // Skipped UCHAR* pbVal;
        // Skipped SHORT* piVal;
        // Skipped USHORT* puiVal;
        // Skipped LONG* plVal;
        // Skipped ULONG* pulVal;
        // Skipped INT* pintVal;
        // Skipped UINT* puintVal;
        // Skipped FLOAT* pfltVal;
        // Skipped DOUBLE* pdblVal;
        // Skipped VARIANT_BOOL* pboolVal;
        // Skipped DECIMAL* pdecVal;
        // Skipped SCODE* pscode;
        // Skipped CY* pcyVal;
        // Skipped DATE* pdate;
        // Skipped BSTR* pbstrVal;
        // Skipped IUnknown** ppunkVal;
        // Skipped IDispatch** ppdispVal;
        // Skipped LPSAFEARRAY* pparray;
        // Skipped PROPVARIANT* pvarVal;

        /// Used by Get[Number] methods to return meaningful value when user specifies slightly wrong type, but still a number.
        private static long FallbackGetInteger(this PROPVARIANT pv) {
            VarType variantType = (VarType)pv.vt;
            switch (variantType) {
                case VarType.VT_EMPTY:
                case VarType.VT_NULL:
                case VarType.VT_ERROR:
                    return 0;
                case VarType.VT_I1:
                    return pv.GetSByte();
                case VarType.VT_UI1:
                    return pv.GetByte();
                case VarType.VT_I2:
                    return pv.GetShort();
                case VarType.VT_UI2:
                    return pv.GetUShort();
                case VarType.VT_I4:
                case VarType.VT_INT:
                    return pv.GetInt();
                case VarType.VT_UI4:
                case VarType.VT_UINT:
                    return pv.GetUInt();
                case VarType.VT_I8:
                    return pv.GetLong();
                case VarType.VT_UI8:
                    return (long)pv.GetULong();
                case VarType.VT_R4:
                    return (long)pv.GetFloat();
                case VarType.VT_R8:
                    return (long)pv.GetDouble();
                case VarType.VT_BOOL:
                    return pv.GetBool() ? 1 : 0;
            }
            return 0;
        }

        /// Returns the value of this PROPVARIANT as an helpful info string
        public static string ToDebugString(this PROPVARIANT prop) {
            VarType variantType = (VarType)prop.vt;
            switch (variantType) {
                case VarType.VT_EMPTY:
                    return "<emtpy>";
                case VarType.VT_NULL:
                    return "<null>";
                case VarType.VT_I1:
                    return prop.GetSByte().ToString();
                case VarType.VT_UI1:
                    return prop.GetByte().ToString();
                case VarType.VT_I2:
                    return prop.GetShort().ToString();
                case VarType.VT_UI2:
                    return prop.GetUShort().ToString();
                case VarType.VT_I4:
                case VarType.VT_INT:
                    return prop.GetInt().ToString();
                case VarType.VT_UI4:
                case VarType.VT_UINT:
                    return prop.GetUInt().ToString();
                case VarType.VT_I8:
                    return prop.GetLong().ToString();
                case VarType.VT_UI8:
                    return prop.GetULong().ToString();
                case VarType.VT_R4:
                    return prop.GetFloat().ToString();
                case VarType.VT_R8:
                    return prop.GetDouble().ToString();
                case VarType.VT_BOOL:
                    return prop.GetBool().ToString();
                case VarType.VT_ERROR:
                    var errorCode = prop.GetErrorCode();
                    string name = Enum.GetName(typeof(HResult), errorCode) ?? errorCode.ToString("X");
                    return $"Error: {name}";
                case VarType.VT_DATE:
                    return prop.GetDate().ToString();
                // Some skipped..
                case VarType.VT_CLSID:
                    return prop.GetGuid().ToString();
                case VarType.VT_LPSTR:
                case VarType.VT_LPWSTR:
                case VarType.VT_BSTR:
                    return "\""+prop.GetString()+"\"";
                default:
                    return $"Unknown type {variantType}";
            }
        }

        public static void Dispose(this PROPVARIANT prop) {
            PropVariantClear(ref prop);
        }

        /// For this to work, PROPVARIANT must be blittable (=not contain any marshalled types), otherwise all sorts of weird things happen.
        // http://www.pinvoke.net/default.aspx/iprop/PropVariantClear.html
        // https://social.msdn.microsoft.com/Forums/windowsserver/en-US/ec242718-8738-4468-ae9d-9734113d2dea/quotipropdllquot-seems-to-be-missing-in-windows-server-2008-and-x64-systems?forum=winserver2008appcompatabilityandcertification
        // https://referencesource.microsoft.com/#windowsbase/Base/MS/Internal/Interop/NativeStructs.cs
        [DllImport("ole32.dll")]
        internal static unsafe extern int PropVariantClear(ref PROPVARIANT pVar);

        /// Create a new PROPVARIANT of VT_LPWSTR type. Must be disposed when no longer used.
        public static PROPVARIANT NewWithString(string value) {
            PROPVARIANT result = new PROPVARIANT {
                vt = (ushort)VarType.VT_LPWSTR
            };
            // Hack, see GetString
            result.inner.pcVal = Marshal.StringToCoTaskMemUni(value);
            return result;
        }

        /// Create a new PROPVARIANT of VT_UI4 type. Must be disposed when no longer used.
        public static PROPVARIANT NewWithUInt(uint value) {
            PROPVARIANT result = new PROPVARIANT {
                vt = (ushort)VarType.VT_UI4
            };
            // Hack, see GetString
            result.inner.ulVal = value;
            return result;
        }
    }
}
