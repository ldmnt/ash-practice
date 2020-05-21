using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ASHDebug
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector
    {
        public float X, Y, Z;

        public float Length()
        {
            return (float) Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public Vector Sub(Vector other)
        {
            Vector result;
            result.X = X - other.X;
            result.Y = Y - other.Y;
            result.Z = Z - other.Z;
            return result;
        }

        public float Distance(Vector other)
        {
            return Sub(other).Length();
        }
    }
}
