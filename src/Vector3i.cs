using System;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using element = System.Int32;
using vector = Jk.Vector3i;

namespace Jk {
	[XmlType("Jk.Vector3i")]
	[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 12)]
	[Serializable]
	public struct Vector3i : IJsonable {
		public static readonly vector Zero = new vector();
		public static readonly vector AxisX = new vector(1, 0, 0);
		public static readonly vector AxisY = new vector(0, 1, 0);
		public static readonly vector AxisZ = new vector(0, 0, 1);
		public static readonly vector MinValue = new vector(element.MinValue, element.MinValue, element.MinValue);
		public static readonly vector MaxValue = new vector(element.MaxValue, element.MaxValue, element.MaxValue);

		[FieldOffset(0)]
		public element X;
		[FieldOffset(4)]
		public element Y;
		[FieldOffset(8)]
		public element Z;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3i(element s) {
			X = s;
			Y = s;
			Z = s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3i(element x, element y, element z) {
			X = x;
			Y = y;
			Z = z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3i(Vector3f v) {
			X = (element)v.X;
			Y = (element)v.Y;
			Z = (element)v.Z;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3i(Vector3d v) {
			X = (element)v.X;
			Y = (element)v.Y;
			Z = (element)v.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3i(element[] arr) {
			X = arr[0];
			Y = arr[1];
			Z = arr[2];
		}

		public ref element this[int i] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				unsafe
				{
					if (3 <= (uint)i)
						throw new IndexOutOfRangeException();
					fixed (element* p = &this.X) {
						return ref p[i];
					}
				}
			}
		}

		public bool IsZero {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return X == 0 && Y == 0 && Z == 0;
			}
		}

		public bool HasZero {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return X == 0 || Y == 0 || Z == 0;
			}
		}

		public override bool Equals(object obj) {
			if (obj is vector)
				return (vector)obj == this;
			else
				return false;
		}

		public override int GetHashCode() {
			return (X.GetHashCode()) ^ (Y.GetHashCode() << 2) ^ (Z.GetHashCode() >> 2);
		}

		public override string ToString() {
			return string.Concat("{ ", "\"X\": " + X, ", ", "\"Y\": " + Y, ", ", "\"Z\": " + Z, " }");
		}

		public string ToString(string f) {
			return string.Concat("{ ", "\"X\": " + X.ToString(f), ", ", "\"Y\": " + Y.ToString(f), ", ", "\"Z\": " + Z.ToString(f), " }");
		}

		public string ToJsonString() {
			return this.ToString();
		}

		public element LengthSquare {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return X * X + Y * Y + Z * Z; }
		}

		public element Length {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return (element)Math.Sqrt(LengthSquare); }
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void NormalizeSelf() {
			var l = LengthSquare;
			if (l == 0 || l == 1)
				return;
			l = 1 / (element)Math.Sqrt(l);
			X *= l; Y *= l; Z *= l;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Normalize() {
			var l = LengthSquare;
			if (l == 0 || l == 1)
				return this;
			l = 1 / (element)Math.Sqrt(l);
			return new vector(X * l, Y * l, Z * l);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RelengthSelf(element length) {
			var l = LengthSquare;
			if (l == 0 || l == length)
				return;
			l = length / (element)Math.Sqrt(l);
			X *= l; Y *= l; Z *= l;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Relength(element length) {
			var l = LengthSquare;
			if (l == 0 || l == length)
				return this;
			l = length / (element)Math.Sqrt(l);
			return new vector(X * l, Y * l, Z * l);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSelf(element s) {
			X += s;
			Y += s;
			Z += s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSelf(vector v) {
			X += v.X;
			Y += v.Y;
			Z += v.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubSelf(element s) {
			X -= s;
			Y -= s;
			Z -= s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubSelf(vector v) {
			X -= v.X;
			Y -= v.Y;
			Z -= v.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MulSelf(element s) {
			X *= s;
			Y *= s;
			Z *= s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MulSelf(vector v) {
			X *= v.X;
			Y *= v.Y;
			Z *= v.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DivSelf(element s) {
			X /= s;
			Y /= s;
			Z /= s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DivSelf(vector v) {
			X /= v.X;
			Y /= v.Y;
			Z /= v.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClipSelf(element min, element max) {
			if (X < min) X = min; else if (max < X) X = max;
			if (Y < min) Y = min; else if (max < Y) Y = max;
			if (Z < min) Z = min; else if (max < Z) Z = max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Clip(element min, element max) {
			var v = this;
			if (v.X < min) v.X = min; else if (max < v.X) v.X = max;
			if (v.Y < min) v.Y = min; else if (max < v.Y) v.Y = max;
			if (v.Z < min) v.Z = min; else if (max < v.Z) v.Z = max;
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClipSelf(vector min, vector max) {
			if (X < min.X) X = min.X; else if (max.X < X) X = max.X;
			if (Y < min.Y) Y = min.Y; else if (max.Y < Y) Y = max.Y;
			if (Z < min.Z) Z = min.Z; else if (max.Z < Z) Z = max.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Clip(vector min, vector max) {
			vector v = this;
			if (v.X < min.X) v.X = min.X; else if (max.X < v.X) v.X = max.X;
			if (v.Y < min.Y) v.Y = min.Y; else if (max.Y < v.Y) v.Y = max.Y;
			if (v.Z < min.Z) v.Z = min.Z; else if (max.Z < v.Z) v.Z = max.Z;
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AbsSelf() {
			X = Math.Abs(X);
			Y = Math.Abs(Y);
			Z = Math.Abs(Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Abs() {
			return new vector(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Sum() {
			return X + Y + Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Product() {
			return X * Y * Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Max() {
			var m = X;
			if (Y > m) m = Y;
			if (Z > m) m = Z;
			return m;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Min() {
			var m = X;
			if (Y < m) m = Y;
			if (Z < m) m = Z;
			return m;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ArgMax() {
			var m = X;
			var i = 0;
			if (Y > m) { m = Y; i = 1; }
			if (Z > m) { m = Z; i = 2; }
			return i;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ArgMin() {
			var m = X;
			var i = 0;
			if (Y < m) { m = Y; i = 1; }
			if (Z < m) { m = Z; i = 2; }
			return i;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ElementWiseMinSelf(element s) {
			if (s < X) X = s;
			if (s < Y) Y = s;
			if (s < Z) Z = s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ElementWiseMaxSelf(element s) {
			if (s > X) X = s;
			if (s > Y) Y = s;
			if (s > Z) Z = s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ElementWiseMinSelf(vector v) {
			if (v.X < X) X = v.X;
			if (v.Y < Y) Y = v.Y;
			if (v.Z < Z) Z = v.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ElementWiseMaxSelf(vector v) {
			if (v.X > X) X = v.X;
			if (v.Y > Y) Y = v.Y;
			if (v.Z > Z) Z = v.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Dot(vector v) {
			return X * v.X + Y * v.Y + Z * v.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Cross(vector v) {
			return new vector(Y * v.Z - Z * v.Y, Z * v.X - X * v.Z, X * v.Y - Y * v.X);
		}


		public bool LessIdThan(vector v) {
			if (X < v.X) return true;
			if (X > v.X) return false;
			if (Y < v.Y) return true;
			if (Y > v.Y) return false;
			if (Z < v.Z) return true;
			if (Z > v.Z) return false;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator ==(vector v1, vector v2) {
			return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator !=(vector v1, vector v2) {
			return v1.X != v2.X || v1.Y != v2.Y || v1.Z != v2.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator <(vector v1, vector v2) {
			if (v1.X >= v2.X) return false;
			if (v1.Y >= v2.Y) return false;
			if (v1.Z >= v2.Z) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator >(vector v1, vector v2) {
			if (v1.X <= v2.X) return false;
			if (v1.Y <= v2.Y) return false;
			if (v1.Z <= v2.Z) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator <=(vector v1, vector v2) {
			if (v1.X > v2.X) return false;
			if (v1.Y > v2.Y) return false;
			if (v1.Z > v2.Z) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator >=(vector v1, vector v2) {
			if (v1.X < v2.X) return false;
			if (v1.Y < v2.Y) return false;
			if (v1.Z < v2.Z) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator +(vector v) {
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator -(vector v) {
			return new vector(-v.X, -v.Y, -v.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator +(vector v1, vector v2) {
			return new vector(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator -(vector v1, vector v2) {
			return new vector(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator +(vector v, element s) {
			return new vector(v.X + s, v.Y + s, v.Z + s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator +(element s, vector v) {
			return new vector(s + v.X, s + v.Y, s + v.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator -(vector v, element s) {
			return new vector(v.X - s, v.Y - s, v.Z - s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator -(element s, vector v) {
			return new vector(s - v.X, s - v.Y, s - v.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator *(vector v, element s) {
			return new vector(v.X * s, v.Y * s, v.Z * s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator *(element s, vector v) {
			return new vector(s * v.X, s * v.Y, s * v.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator /(vector v, element s) {
			return new vector(v.X / s, v.Y / s, v.Z / s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator *(vector v1, vector v2) {
			return new vector(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator /(vector v1, vector v2) {
			return new vector(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector ElementWiseMin(vector v1, vector v2) {
			if (v2.X < v1.X) v1.X = v2.X;
			if (v2.Y < v1.Y) v1.Y = v2.Y;
			if (v2.Z < v1.Z) v1.Z = v2.Z;
			return v1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector ElementWiseMax(vector v1, vector v2) {
			if (v2.X < v1.X) v2.X = v1.X;
			if (v2.Y < v1.Y) v2.Y = v1.Y;
			if (v2.Z < v1.Z) v2.Z = v1.Z;
			return v2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ElementWiseMinMax(vector v1, vector v2, out vector min, out vector max) {
			if (v2.X < v1.X) { var t = v1.X; v1.X = v2.X; v2.X = t; };
			if (v2.Y < v1.Y) { var t = v1.Y; v1.Y = v2.Y; v2.Y = t; };
			if (v2.Z < v1.Z) { var t = v1.Z; v1.Z = v2.Z; v2.Z = t; };
			min = v1;
			max = v2;
		}

		public element[] Flatten(vector[] srcArray, int start, int count) {
			unsafe
			{
				var dstArray = new element[3 * count];
				if (count != 0) {
					fixed (vector* pSrcOrg = &srcArray[start])
					fixed (element* pDstOrg = &dstArray[0]) {
						var pSrc = pSrcOrg;
						var pDst = pDstOrg;
						var pSrcEnd = pSrc + count;
						while (pSrc < pSrcEnd) {
							*(vector*)pDst = *pSrc;
							pSrc++;
							pDst += 3;
						}
					}
				}
				return dstArray;
			}
		}

#if UNITY_5_3_OR_NEWER
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3i(UnityEngine.Vector3 v) {
			X = (element)v.x;
			Y = (element)v.y;
			Z = (element)v.z;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator vector(UnityEngine.Vector3 v) {
			return new vector((element)v.x, (element)v.y, (element)v.z);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator UnityEngine.Vector3(vector v) {
			return new UnityEngine.Vector3((float)v.X, (float)v.Y, (float)v.Z);
		}
#endif
	}
}
