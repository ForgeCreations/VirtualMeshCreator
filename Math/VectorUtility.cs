namespace VirtualMeshCreator.Math
{
    public static class VectorUtility
    {
        public static Vector3 GetAbs(Vector3 v)
        {
            return new Vector3(System.Math.Abs(v.x), System.Math.Abs(v.y), System.Math.Abs(v.z));
        }
        
        public static Vector3 GetUnsafeNormal(in Vector3 n)
        {
            float Scale = MathUtils.InvSqrt(n.x * n.x + n.y * n.y + n.z * n.z);
            return new Vector3(n.x * Scale, n.y * Scale, n.z * Scale);
        }

        public static Vector3 Or(Vector3 a, Vector3 b)
        {
            return new Vector3((uint)a.x | (uint)b.x, (uint)a.y | (uint)b.y, (uint)a.z | (uint)b.z);
        }
        
        public struct VectorRegister4Float
        {
            public float[] V;

            public VectorRegister4Float(int size = 4)
            {
                V = new float[size];
            }
        }
        
        public struct VectorRegister4Int
        {
            public int[] V;

            public VectorRegister4Int(int size = 4)
            {
                V = new int[size];
            }
        }
        
        /**
        * Returns a bitwise equivalent vector based on 4 DWORDs.
        *
        * @param X	1st uint component
        * @param Y	2nd uint component
        * @param Z	3rd uint component
        * @param W	4th uint component
        * @return	Bitwise equivalent vector with 4 floats
        */
        public static VectorRegister4Float MakeVectorRegisterFloat(uint X, uint Y, uint Z, uint W)
        {
            VectorRegister4Float Vec = new VectorRegister4Float();
            Vec.V[0] = X;
            Vec.V[1] = Y;
            Vec.V[2] = Z;
            Vec.V[3] = W;
            return Vec;
        }
        
        public static VectorRegister4Float MakeVectorRegister(uint X, uint Y, uint Z, uint W)
        {
            return MakeVectorRegisterFloat(X, Y, Z, W);
        }
        
        /**
        * Combines two vectors using bitwise OR (treating each vector as a 128 bit field)
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @return	VectorRegister(for each bit i: Vec1[i] | Vec2[i])
        */
        public static VectorRegister4Float VectorBitwiseOr(VectorRegister4Float Vec1, VectorRegister4Float Vec2)
        {
            return MakeVectorRegisterFloat(
                (uint)(((uint)Vec1.V[0]) | ((uint)Vec2.V[0])),
                (uint)(((uint)Vec1.V[1]) | ((uint)Vec2.V[1])),
                (uint)(((uint)Vec1.V[2]) | ((uint)Vec2.V[2])),
                (uint)(((uint)Vec1.V[3]) | ((uint)Vec2.V[3])));
        }
        
        /**
        * Combines two vectors using bitwise XOR (treating each vector as a 128 bit field)
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @return	VectorRegister( for each bit i: Vec1[i] ^ Vec2[i] )
        */
        public static VectorRegister4Float VectorBitwiseXor(VectorRegister4Float Vec1, VectorRegister4Float Vec2)
        {
            return MakeVectorRegisterFloat(
                ((uint)Vec1.V[0]) ^ ((uint)Vec2.V[0]),
                (((uint)Vec1.V[1]) ^ ((uint)Vec2.V[1])),
                (((uint)Vec1.V[2]) ^ ((uint)Vec2.V[2])),
                (((uint)Vec1.V[3]) ^ ((uint)Vec2.V[3])));
        }
        
        /**
        * Combines two vectors using bitwise AND (treating each vector as a 128 bit field)
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @return	VectorRegister(for each bit i: Vec1[i] & Vec2[i])
        */
        public static VectorRegister4Float VectorBitwiseAnd(VectorRegister4Float Vec1, VectorRegister4Float Vec2)
        {
            return MakeVectorRegisterFloat(
                (((uint)Vec1.V[0]) & ((uint)Vec2.V[0])),
                (((uint)Vec1.V[1]) & ((uint)Vec2.V[1])),
                (((uint)Vec1.V[2]) & ((uint)Vec2.V[2])),
                (((uint)Vec1.V[3]) & ((uint)Vec2.V[3])));
        }
        
        /**
        * Adds two vectors (component-wise) and returns the result.
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @return	VectorRegister(Vec1.x + Vec2.x, Vec1.y + Vec2.y, Vec1.z + Vec2.z, Vec1.w + Vec2.w)
        */
        public static VectorRegister4Float VectorAdd(VectorRegister4Float Vec1, VectorRegister4Float Vec2)
        {
            VectorRegister4Float Vec = new VectorRegister4Float();
            Vec.V[0] = Vec1.V[0] + Vec2.V[0];
            Vec.V[1] = Vec1.V[1] + Vec2.V[1];
            Vec.V[2] = Vec1.V[2] + Vec2.V[2];
            Vec.V[3] = Vec1.V[3] + Vec2.V[3];
            return Vec;
        }
        
        /**
        * Subtracts a vector from another (component-wise) and returns the result.
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @return	VectorRegister(Vec1.x - Vec2.x, Vec1.y - Vec2.y, Vec1.z - Vec2.z, Vec1.w - Vec2.w)
        */
        public static VectorRegister4Float VectorSubtract(VectorRegister4Float Vec1, VectorRegister4Float Vec2)
        {
            VectorRegister4Float Vec = new VectorRegister4Float();
            Vec.V[0] = Vec1.V[0] - Vec2.V[0];
            Vec.V[1] = Vec1.V[1] - Vec2.V[1];
            Vec.V[2] = Vec1.V[2] - Vec2.V[2];
            Vec.V[3] = Vec1.V[3] - Vec2.V[3];
            return Vec;
        }
        
        public static VectorRegister4Float VectorSqrt(VectorRegister4Float Vec)
        {
            return MakeVectorRegisterFloat(
                (uint)System.Math.Sqrt(Vec.V[0]),
                (uint)System.Math.Sqrt(Vec.V[1]),
                (uint)System.Math.Sqrt(Vec.V[2]),
                (uint)System.Math.Sqrt(Vec.V[3]));
        }
        
        /**
        * Swizzles the 4 components of a vector and returns the result.
        *
        * @param Vec	Source vector
        * @param X		Index for which component to use for X (literal 0-3)
        * @param Y		Index for which component to use for Y (literal 0-3)
        * @param Z		Index for which component to use for Z (literal 0-3)
        * @param W		Index for which component to use for W (literal 0-3)
        * @return		The swizzled vector
        */
        public static VectorRegister4Float VectorSwizzle(VectorRegister4Float Vec, int X, int Y, int Z, int W) => MakeVectorRegister((uint)Vec.V[X], (uint)Vec.V[Y], (uint)Vec.V[Z], (uint)Vec.V[W]);
        
        /**
        * Multiplies two vectors (component-wise), adds in the third vector and returns the result.
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @param Vec3	3rd vector
        * @return		VectorRegister(Vec1.x * Vec2.x + Vec3.x, Vec1.y * Vec2.y + Vec3.y, Vec1.z * Vec2.z + Vec3.z, Vec1.w * Vec2.w + Vec3.w)
        */
        public static VectorRegister4Float VectorMultiplyAdd(VectorRegister4Float Vec1, VectorRegister4Float Vec2, VectorRegister4Float Vec3)
        {
            VectorRegister4Float Vec = new VectorRegister4Float();
            Vec.V[0] = Vec1.V[0] * Vec2.V[0] + Vec3.V[0];
            Vec.V[1] = Vec1.V[1] * Vec2.V[1] + Vec3.V[1];
            Vec.V[2] = Vec1.V[2] * Vec2.V[2] + Vec3.V[2];
            Vec.V[3] = Vec1.V[3] * Vec2.V[3] + Vec3.V[3];
            return Vec;
        }
        
        /**
        * Returns the absolute value (component-wise).
        *
        * @param Vec	Source vector
        * @return		VectorRegister(abs(Vec.x), abs(Vec.y), abs(Vec.z), abs(Vec.w))
        */
        public static VectorRegister4Float VectorAbs(VectorRegister4Float Vec)
        {
            VectorRegister4Float Vec2 = new VectorRegister4Float();
            Vec2.V[0] = System.Math.Abs(Vec.V[0]);
            Vec2.V[1] = System.Math.Abs(Vec.V[1]);
            Vec2.V[2] = System.Math.Abs(Vec.V[2]);
            Vec2.V[3] = System.Math.Abs(Vec.V[3]);
            return Vec2;
        }
        
        /**
        * Stores a vector to memory (aligned or unaligned).
        *
        * @param Vec	Vector to store
        * @param Ptr	Memory pointer
        */
        public static void VectorIntStore(VectorRegister4Int A, ref int[] Ptr)
        {
            Ptr[0] = A.V[0];
            Ptr[1] = A.V[1];
            Ptr[2] = A.V[2];
            Ptr[3] = A.V[3];
        }
        
        /**
        * Divides two vectors (component-wise) and returns the result.
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @return		VectorRegister(Vec1.x / Vec2.x, Vec1.y / Vec2.y, Vec1.z / Vec2.z, Vec1.w / Vec2.w)
        */
        public static VectorRegister4Float VectorDivide(VectorRegister4Float Vec1, VectorRegister4Float Vec2)
        {
            VectorRegister4Float Vec = new VectorRegister4Float();
            Vec.V[0] = Vec1.V[0] / Vec2.V[0];
            Vec.V[1] = Vec1.V[1] / Vec2.V[1];
            Vec.V[2] = Vec1.V[2] / Vec2.V[2];
            Vec.V[3] = Vec1.V[3] / Vec2.V[3];
            return Vec;
        }
        
        /**
         * Returns the minimum values of two vectors (component-wise).
         *
         * @param Vec1	1st vector
         * @param Vec2	2nd vector
         * @return		VectorRegister( min(Vec1.x,Vec2.x), min(Vec1.y,Vec2.y), min(Vec1.z,Vec2.z), min(Vec1.w,Vec2.w) )
         */
        public static VectorRegister4Float VectorMin(VectorRegister4Float Vec1, VectorRegister4Float Vec2 )
        {
            VectorRegister4Float Vec = new VectorRegister4Float();
            Vec.V[0] = System.Math.Min(Vec1.V[0], Vec2.V[0]);
            Vec.V[1] = System.Math.Min(Vec1.V[1], Vec2.V[1]);
            Vec.V[2] = System.Math.Min(Vec1.V[2], Vec2.V[2]);
            Vec.V[3] = System.Math.Min(Vec1.V[3], Vec2.V[3]);
            return Vec;
        }
        
        /**
        * Returns the maximum values of two vectors (component-wise).
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @return		VectorRegister( max(Vec1.x,Vec2.x), max(Vec1.y,Vec2.y), max(Vec1.z,Vec2.z), max(Vec1.w,Vec2.w) )
        */
        public static VectorRegister4Float VectorMax(VectorRegister4Float Vec1, VectorRegister4Float Vec2 )
        {
            VectorRegister4Float Vec = new VectorRegister4Float();
            Vec.V[0] = System.Math.Max(Vec1.V[0], Vec2.V[0]);
            Vec.V[1] = System.Math.Max(Vec1.V[1], Vec2.V[1]);
            Vec.V[2] = System.Math.Max(Vec1.V[2], Vec2.V[2]);
            Vec.V[3] = System.Math.Max(Vec1.V[3], Vec2.V[3]);
            return Vec;
        }
        
        /**
        * Multiplies two vectors (component-wise) and returns the result.
        *
        * @param Vec1	1st vector
        * @param Vec2	2nd vector
        * @return		VectorRegister( Vec1.x*Vec2.x, Vec1.y*Vec2.y, Vec1.z*Vec2.z, Vec1.w*Vec2.w )
        */
        public static VectorRegister4Float VectorMultiply(VectorRegister4Float Vec1, VectorRegister4Float Vec2)
        {
            VectorRegister4Float Vec = new VectorRegister4Float();
            Vec.V[0] = Vec1.V[0] * Vec2.V[0];
            Vec.V[1] = Vec1.V[1] * Vec2.V[1];
            Vec.V[2] = Vec1.V[2] * Vec2.V[2];
            Vec.V[3] = Vec1.V[3] * Vec2.V[3];
            return Vec;
        }
        
        /**
        * Propagates passed in float to all registers
        *
        * @param F	Float to set
        * @return	VectorRegister4Float(F,F,F,F)
        */
        public static VectorRegister4Float VectorSetFloat1(float F)
        {
            return MakeVectorRegisterFloat((uint)F, (uint)F, (uint)F, (uint)F);
        }
        
        /**
        * Returns a bitwise equivalent vector based on 4 DWORDs.
        *
        * @param X	1st uint component
        * @param Y	2nd uint component
        * @param Z	3rd uint component
        * @param W	4th uint component
        * @return	Bitwise equivalent vector with 4 floats
        */
        public static VectorRegister4Int MakeVectorRegisterInt(int X, int Y, int Z, int W)
        {
            VectorRegister4Int Vec = new VectorRegister4Int();
            Vec.V[0] = X;
            Vec.V[1] = Y;
            Vec.V[2] = Z;
            Vec.V[3] = W;
            return Vec;
        }
        
        public static VectorRegister4Float VectorIntToFloat(VectorRegister4Int A)
        {
            return MakeVectorRegisterFloat(
                (uint)A.V[0],
                (uint)A.V[1],
                (uint)A.V[2],
                (uint)A.V[3]);
        }
        
        public static VectorRegister4Int VectorFloatToInt(VectorRegister4Float A)
        {
            return MakeVectorRegisterInt(
                (int)A.V[0],
                (int)A.V[1],
                (int)A.V[2],
                (int)A.V[3]);
        }

        public static VectorRegister4Int VectorIntAdd(VectorRegister4Int A, VectorRegister4Int B)
        {
	        return MakeVectorRegisterInt(
                A.V[0] + B.V[0],
                A.V[1] + B.V[1],
                A.V[2] + B.V[2],
                A.V[3] + B.V[3]);
        }
}
}