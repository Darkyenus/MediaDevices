﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using PortableDeviceApiLib;
using PortableDeviceTypesLib;
using PropertyKey = PortableDeviceApiLib._tagpropertykey;
using IPortableDeviceValues = PortableDeviceApiLib.IPortableDeviceValues;
using PROPVARIANT = PortableDeviceApiLib.tag_inner_PROPVARIANT;
using IPortableDevicePropVariantCollection = PortableDeviceApiLib.IPortableDevicePropVariantCollection;
using System.Runtime.InteropServices;

namespace MediaDevices.Internal
{
    internal class Command
    {
        private IPortableDeviceValues values;
        private IPortableDeviceValues result;

        private Command(PropertyKey commandKey)
        {
            this.values = (IPortableDeviceValues)new PortableDeviceValues();
            this.values.SetGuidValue(WPD.PROPERTY_COMMON_COMMAND_CATEGORY, commandKey.fmtid);
            this.values.SetUnsignedIntegerValue(WPD.PROPERTY_COMMON_COMMAND_ID, commandKey.pid);
        }

        public static Command Create(PropertyKey commandKey)
        {
            return new Command(commandKey);
        }

        public void Add(PropertyKey key, Guid value)
        {
            this.values.SetGuidValue(key, value);
        }

        public void Add(PropertyKey key, int value)
        {
            this.values.SetSignedIntegerValue(key, value);
        }

        public void Add(PropertyKey key, uint value)
        {
            this.values.SetUnsignedIntegerValue(key, value);
        }

        public void Add(PropertyKey key, IPortableDevicePropVariantCollection value)
        {
            this.values.SetIPortableDevicePropVariantCollectionValue(key, value);
        }
        
        public void Add(PropertyKey key, IEnumerable<uint> values)
        {
            IPortableDevicePropVariantCollection col = (IPortableDevicePropVariantCollection) new PortableDevicePropVariantCollection();
            foreach (var value in values)
            {
                PROPVARIANT var = PropVariantUtil.NewWithUInt(value);
                col.Add(ref var);
                var.Dispose();
            }
            this.values.SetIPortableDevicePropVariantCollectionValue(key, col);
        }

        public void Add(PropertyKey key, string value)
        {
            this.values.SetStringValue(key, value);
        }

        //public void Add(PropertyKey key, byte[] buffer, int size)
        //{
        //    Marshal..
        //    this.values.SetBufferValue(key, ref buffer, (uint)size);
        //}

        public Guid GetGuid(PropertyKey key)
        {
            Guid value;
            this.result.GetGuidValue(key, out value);
            return value;
        }

        public int GetInt(PropertyKey key)
        {
            int value;
            this.result.GetSignedIntegerValue(key, out value);
            return value;
        }

        public string GetString(PropertyKey key)
        {
            string value;
            this.result.GetStringValue(key, out value);
            return value;
        }
        
        private IPortableDevicePropVariantCollection GetPropVariantCollection(PropertyKey key) {
            this.result.GetIUnknownValue(key, out object obj);
            return obj as IPortableDevicePropVariantCollection;
        }
        
        public IEnumerable<string> GetStrings(PropertyKey key) {
            var col = GetPropVariantCollection(key);

            uint count = 0;
            col.GetCount(ref count);
            for (uint i = 0; i < count; i++) {
                PROPVARIANT val = new PROPVARIANT();
                col.GetAt(i, ref val);
                string value = val.GetString();
                val.Dispose();
                yield return value;
            }
        }

        public IEnumerable<uint> GetUInts(PropertyKey key) {
            var col = GetPropVariantCollection(key);

            uint count = 0;
            col.GetCount(ref count);
            for (uint i = 0; i < count; i++) {
                PROPVARIANT val = new PROPVARIANT();
                col.GetAt(i, ref val);
                uint value = val.GetUInt();
                val.Dispose();
                yield return value;
            }
        }

        public bool Has(PropertyKey key)
        {
            return ComHelper.HasKeyValue(result, key);
        }

        public bool Send(PortableDevice device)
        {
            device.SendCommand(0, this.values, out this.result);

            result.GetErrorValue(WPD.PROPERTY_COMMON_HRESULT, out int error);
            switch ((HResult)error)
            {
            case HResult.S_OK:
                return true;
            case HResult.E_NOT_IMPLEMENTED:
                Debug.WriteLine("Command not implemented!");
                return false;
            default:
                throw new Exception($"Error {error:X}");
            }
        }

        [Conditional("COMTRACE")]
        public void WriteResults()
        {
            ComTrace.WriteObject(this.result);
        }
    }
}
