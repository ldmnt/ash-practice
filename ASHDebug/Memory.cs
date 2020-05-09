using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ASHDebug
{
    class Memory
    {
        private IntPtr ProcessHandle { get; }

        public Memory(IntPtr processHandle)
        {
            ProcessHandle = processHandle;
        }

        public T ReadValue<T>(int address)
        {
            var byteSize = typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf(typeof(T));
            var buffer = new byte[byteSize];
            int bytesRead = 0;
            WinAPI.ReadProcessMemory((int)ProcessHandle, address, buffer, buffer.Length, ref bytesRead);
            return (T) ConvertToType(buffer, typeof(T));
        }

        public int ReadPointer(int address)
        {
            return ReadValue<int>(address);
        }

        public T ReadPointer<T>(int address, int offset)
        {
            return ReadValue<T>(address + offset);
        }

        public T ReadPointerChain<T>(int baseAddress, int[] offsets)
        {
            int ptr = baseAddress;
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                ptr = ReadPointer(ptr + offsets[i]);
            }
            return ReadPointer<T>(ptr, offsets[offsets.Length - 1]);
        }

        private object ConvertToType(byte[] bytes, Type type)
        {
            if (type == typeof(int))
            {
                return BitConverter.ToInt32(bytes, 0);
            }
            else if (type == typeof(float))
            {
                return BitConverter.ToSingle(bytes, 0);
            }
            else if (type == typeof(double))
            {
                return BitConverter.ToDouble(bytes, 0);
            }
            else if (type == typeof(bool))
            {
                return BitConverter.ToBoolean(bytes, 0);
            }
            else if (type == typeof(Vector))
            {
                if (bytes.Length != 12)
                {
                    throw new Exception("Invalid input byte size.");
                }
                Vector v;
                v.X = BitConverter.ToSingle(bytes, 0);
                v.Y = BitConverter.ToSingle(bytes, 4);
                v.Z = BitConverter.ToSingle(bytes, 8);
                return v;
            }
            else
            {
                throw new NotImplementedException("Conversion is not implemented for this data type.");
            }
        }
    }
}
