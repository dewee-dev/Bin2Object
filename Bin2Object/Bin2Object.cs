// Copyright (c) 2016 Perfare - https://github.com/Perfare/Il2CppDumper/
// Copyright (c) 2016 Alican Çubukçuoğlu - https://github.com/AlicanC/AlicanC-s-Modern-Warfare-2-Tool/
// Copyright (c) 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty/Bin2Object/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NoisyCowStudios.Bin2Object
{
    public enum Endianness
    {
        Little,
        Big
    }

    public class BinaryObjectReader : BinaryReader
    {
        // Generic method cache to dramatically speed up repeated calls to ReadObject<T> with the same T
        private Dictionary<string, MethodInfo> readObjectGenericCache = new Dictionary<string, MethodInfo>();

        // VersionAttribute cache to dramatically speed up repeated calls to ReadObject<T> with the same T
        private Dictionary<Type, Dictionary<FieldInfo, (double Min, double Max)>> readObjectVersionCache = new Dictionary<Type, Dictionary<FieldInfo, (double, double)>>();

        // Thread synchronization objects (for thread safety)
        private object readLock = new object();

        // Initialization
        public BinaryObjectReader(Stream stream, Endianness endianness = Endianness.Little) : base(stream) {
            Endianness = endianness;
        }

        // Position in the stream
        public long Position {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        // Allows you to specify types which should be read as different types in the stream
        public Dictionary<Type, Type> PrimitiveMappings { get; } = new Dictionary<Type, Type>();

        public Endianness Endianness { get; set; }

        public double Version { get; set; } = 1;

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public override byte[] ReadBytes(int count) {
            var bytes = base.ReadBytes(count);
            return (Endianness == Endianness.Little ? bytes : bytes.Reverse().ToArray());
        }

        public override long ReadInt64() => BitConverter.ToInt64(ReadBytes(8), 0);
        
        public override ulong ReadUInt64() => BitConverter.ToUInt64(ReadBytes(8), 0);

        public override int ReadInt32() => BitConverter.ToInt32(ReadBytes(4), 0);

        public override uint ReadUInt32() => BitConverter.ToUInt32(ReadBytes(4), 0);

        public override short ReadInt16() => BitConverter.ToInt16(ReadBytes(2), 0);

        public override ushort ReadUInt16() => BitConverter.ToUInt16(ReadBytes(2), 0);

        public byte[] ReadBytes(long addr, int count) {
            lock (readLock) {
                Position = addr;
                return ReadBytes(count);
            }
        }

        public long ReadInt64(long addr) {
            lock (readLock) {
                Position = addr;
                return ReadInt64();
            }
        }

        public ulong ReadUInt64(long addr) {
            lock (readLock) {
                Position = addr;
                return ReadUInt64();
            }
        }

        public int ReadInt32(long addr) {
            lock (readLock) {
                Position = addr;
                return ReadInt32();
            }
        }

        public uint ReadUInt32(long addr) {
            lock (readLock) {
                Position = addr;
                return ReadUInt32();
            }
        }

        public short ReadInt16(long addr) {
            lock (readLock) {
                Position = addr;
                return ReadInt16();
            }
        }

        public ushort ReadUInt16(long addr) {
            lock (readLock) {
                Position = addr;
                return ReadUInt16();
            }
        }

        public byte ReadByte(long addr) {
            lock (readLock) {
                Position = addr;
                return ReadByte();
            }
        }

        public bool ReadBoolean(long addr) {
            lock (readLock) {
                Position = addr;
                return ReadBoolean();
            }
        }

        public T ReadObject<T>(long addr) where T : new() {
            lock (readLock) {
                Position = addr;
                return ReadObject<T>();
            }
        }

               public T ReadObject<T>() where T : new()
        {
            Type type = typeof(T);
            if (type.GetTypeInfo().IsPrimitive)
            {
                Type mapping2 = Enumerable.FirstOrDefault(Enumerable.Select(Enumerable.Where(PrimitiveMappings, delegate (KeyValuePair<Type, Type> m)
                {
                    KeyValuePair<Type, Type> keyValuePair4 = m;
                    return keyValuePair4.Key.GetTypeInfo().Name == type.Name;
                }), delegate (KeyValuePair<Type, Type> m)
                {
                    KeyValuePair<Type, Type> keyValuePair3 = m;
                    return keyValuePair3.Value;
                }));
                if ((object)mapping2 != null)
                {
                    return (T)Convert.ChangeType(Enumerable.FirstOrDefault(Enumerable.Where(GetType().GetMethods(), (MethodInfo m) => m.Name.StartsWith("Read") && m.ReturnType == mapping2 && !Enumerable.Any(m.GetParameters())))?.Invoke(this, null), typeof(T));
                }
                object obj;
                switch (type.Name)
                {
                    case "Int64":
                        obj = ReadInt64();
                        break;
                    case "UInt64":
                        obj = ReadUInt64();
                        break;
                    case "Int32":
                        obj = ReadInt32();
                        break;
                    case "UInt32":
                        obj = ReadUInt32();
                        break;
                    case "Int16":
                        obj = ReadInt16();
                        break;
                    case "UInt16":
                        obj = ReadUInt16();
                        break;
                    case "Byte":
                        obj = ReadByte();
                        break;
                    case "Boolean":
                        obj = ReadBoolean();
                        break;
                    default:
                        throw new ArgumentException("Unsupported primitive type specified: " + type.FullName);
                }
                return (T)obj;
            }
            T val = new T();
            if (!readObjectVersionCache.TryGetValue(type, out Dictionary<FieldInfo, (double, double)> _))
            {
                Dictionary<FieldInfo, (double, double)> dictionary = new Dictionary<FieldInfo, (double, double)>();
                FieldInfo[] fields = type.GetFields();
                foreach (FieldInfo fieldInfo in fields)
                {
                    VersionAttribute customAttribute = fieldInfo.GetCustomAttribute<VersionAttribute>(inherit: false);
                    if (customAttribute != null)
                    {
                        dictionary.Add(fieldInfo, (customAttribute.Min, customAttribute.Max));
                    }
                    else
                    {
                        dictionary.Add(fieldInfo, (-1.0, -1.0));
                    }
                }
                readObjectVersionCache.Add(type, dictionary);
            }
            foreach (KeyValuePair<FieldInfo, (double, double)> item in readObjectVersionCache[type])
            {
                item.Deconstruct(out FieldInfo key, out (double, double) value2);
                FieldInfo i = key;
                (double, double) valueTuple = value2;
                if ((valueTuple.Item1 == -1.0 || !(valueTuple.Item1 > Version)) && (valueTuple.Item2 == -1.0 || !(valueTuple.Item2 < Version)))
                {
                    if (i.FieldType == typeof(string))
                    {
                        StringAttribute customAttribute2 = i.GetCustomAttribute<StringAttribute>(inherit: false);
                        if (customAttribute2 == null || customAttribute2.IsNullTerminated)
                        {
                            i.SetValue(val, ReadNullTerminatedString());
                        }
                        else
                        {
                            if (customAttribute2.FixedSize <= 0)
                            {
                                throw new ArgumentException("String attribute for array field " + i.Name + " configuration invalid");
                            }
                            i.SetValue(val, ReadFixedLengthString(customAttribute2.FixedSize));
                        }
                    }
                    else if (i.FieldType.IsArray)
                    {
                        ArrayLengthAttribute arrayLengthAttribute = i.GetCustomAttribute<ArrayLengthAttribute>(inherit: false) ?? throw new InvalidOperationException("Array field " + i.Name + " must have ArrayLength attribute");
                        int num;
                        if (arrayLengthAttribute.FieldName != null)
                        {
                            num = Convert.ToInt32((type.GetField(arrayLengthAttribute.FieldName) ?? throw new ArgumentException("Array field " + i.Name + " has invalid FieldName in ArrayLength attribute"))!.GetValue(val));
                        }
                        else
                        {
                            if (arrayLengthAttribute.FixedSize <= 0)
                            {
                                throw new ArgumentException("ArrayLength attribute for array field " + i.Name + " configuration invalid");
                            }
                            num = arrayLengthAttribute.FixedSize;
                        }
                        MethodInfo methodInfo = GetType().GetMethod("ReadArray", new Type[1]
                        {
                    typeof(int)
                        })!.MakeGenericMethod(i.FieldType.GetElementType());
                        i.SetValue(val, methodInfo.Invoke(this, new object[1]
                        {
                    num
                        }));
                    }
                    else if (i.FieldType.IsPrimitive)
                    {
                        Type mapping = Enumerable.FirstOrDefault(Enumerable.Select(Enumerable.Where(PrimitiveMappings, delegate (KeyValuePair<Type, Type> m)
                        {
                            KeyValuePair<Type, Type> keyValuePair2 = m;
                            return keyValuePair2.Key.GetTypeInfo().Name == i.FieldType.Name;
                        }), delegate (KeyValuePair<Type, Type> m)
                        {
                            KeyValuePair<Type, Type> keyValuePair = m;
                            return keyValuePair.Value;
                        }));
                        if ((object)mapping != null)
                        {
                            MethodInfo methodInfo2 = Enumerable.FirstOrDefault(Enumerable.Where(GetType().GetMethods(), (MethodInfo m) => m.Name.StartsWith("Read") && m.ReturnType == mapping && !Enumerable.Any(m.GetParameters())));
                            i.SetValue(val, methodInfo2?.Invoke(this, null));
                        }
                        else
                        {
                            key = i;
                            object obj = val;
                            object value3;
                            switch (i.FieldType.Name)
                            {
                                case "Int64":
                                    value3 = ReadInt64();
                                    break;
                                case "UInt64":
                                    value3 = ReadUInt64();
                                    break;
                                case "Int32":
                                    value3 = ReadInt32();
                                    break;
                                case "UInt32":
                                    value3 = ReadUInt32();
                                    break;
                                case "Int16":
                                    value3 = ReadInt16();
                                    break;
                                case "UInt16":
                                    value3 = ReadUInt16();
                                    break;
                                case "Byte":
                                    value3 = ReadByte();
                                    break;
                                case "Boolean":
                                    value3 = ReadBoolean();
                                    break;
                                default:
                                    throw new ArgumentException("Unsupported primitive type specified: " + i.FieldType.FullName);
                            }
                            key.SetValue(obj, value3);
                        }
                    }
                    else
                    {
                        if (!readObjectGenericCache.TryGetValue(i.FieldType.FullName, out MethodInfo value4))
                        {
                            value4 = GetType().GetMethod("ReadObject", Type.EmptyTypes)!.MakeGenericMethod(i.FieldType);
                            readObjectGenericCache.Add(i.FieldType.FullName, value4);
                        }
                        i.SetValue(val, value4.Invoke(this, null));
                    }
                }
            }
            return val;
        }

        public T[] ReadArray<T>(long addr, int count) where T : new() {
            lock (readLock) {
                Position = addr;
                return ReadArray<T>(count);
            }
        }

        public T[] ReadArray<T>(int count) where T : new() {
            T[] t = new T[count];
            for (int i = 0; i < count; i++) {
                t[i] = ReadObject<T>();
            }
            return t;
        }

        public string ReadNullTerminatedString(long addr, Encoding encoding = null) {
            lock (readLock) {
                Position = addr;
                return ReadNullTerminatedString(encoding);
            }
        }

        public string ReadNullTerminatedString(Encoding encoding = null) {
            List<byte> bytes = new List<byte>();
            byte b;
            while ((b = ReadByte()) != 0)
                bytes.Add(b);
            return encoding?.GetString(bytes.ToArray()) ?? Encoding.GetString(bytes.ToArray());
        }

        public string ReadFixedLengthString(long addr, int length, Encoding encoding = null) {
            lock (readLock) {
                Position = addr;
                return ReadFixedLengthString(length, encoding);
            }
        }

        public string ReadFixedLengthString(int length, Encoding encoding = null) {
            byte[] b = ReadArray<byte>(length);
            List<byte> bytes = new List<byte>();
            foreach (var c in b)
                if (c == 0)
                    break;
                else
                    bytes.Add(c);
            return encoding?.GetString(bytes.ToArray()) ?? Encoding.GetString(bytes.ToArray());
        }
    }
}
