// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;

public struct Vector4d
{
    public double x;
    public double y;
    public double z;
    public double w;

    public double this[int index]
    {
        get
        {
            switch (index)
            {
                case 0:
                    return x;
                case 1:
                    return y;
                case 2:
                    return z;
                case 3:
                    return w;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector4d index");
            }
        }
        set
        {
            switch (index)
            {
                case 0:
                    x = value;
                    break;
                case 1:
                    y = value;
                    break;
                case 2:
                    z = value;
                    break;
                case 3:
                    w = value;
                    break;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector4d index!");
            }
        }
    }

    public Vector4d normalized { get { return Normalize(this); } }
    public double magnitude { get { return Math.Sqrt(x * x + y * y + z * z + w * w); } }
    public double sqrMagnitude { get { return x * x + y * y + z * z + w * w; } }
    public static Vector4d zero { get { return new Vector4d(0d, 0d, 0d, 0d); } }
    public static Vector4d one { get { return new Vector4d(1d, 1d, 1d, 1d); } }

    public Vector4d(double x, double y, double z, double w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public Vector4d(Vector4 v4)
    {
        x = v4.x;
        y = v4.y;
        z = v4.z;
        w = v4.w;
    }

    public Vector4d(Vector3d v3, double w)
    {
        x = v3.x;
        y = v3.y;
        z = v3.z;
        this.w = w;
    }

    public static implicit operator Vector4d(Vector3d v)
    {
        return new Vector4d(v.x, v.y, v.z, 0.0f);
    }

    public static implicit operator Vector3d(Vector4d v)
    {
        return new Vector3d(v.x, v.y, v.z);
    }

    public static Vector4d operator +(Vector4d a, Vector4d b)
    {
        return new Vector4d(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    }

    public static Vector4d operator -(Vector4d a, Vector4d b)
    {
        return new Vector4d(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    }

    public static Vector4d operator -(Vector4d a)
    {
        return new Vector4d(-a.x, -a.y, -a.z, -a.w);
    }

    public static Vector4d operator *(Vector4d a, double d)
    {
        return new Vector4d(a.x * d, a.y * d, a.z * d, a.w * d);
    }

    public static Vector4d operator *(double d, Vector4d a)
    {
        return new Vector4d(a.x * d, a.y * d, a.z * d, a.w * d);
    }

    public static Vector4d operator /(Vector4d a, double d)
    {
        return new Vector4d(a.x / d, a.y / d, a.z / d, a.w / d);
    }

    private const double nearZero = 0.0 / 1.0; //9.99999943962493E-11;

    public static bool operator ==(Vector4d lhs, Vector4d rhs)
    {
        return SqrMagnitude(lhs - rhs) < nearZero;
    }

    public static bool operator !=(Vector4d lhs, Vector4d rhs)
    {
        return SqrMagnitude(lhs - rhs) >= nearZero;
    }

    public void Set(double x, double y, double z, double w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

	public static Vector4d Scale(Vector4d a, Vector4d b)
	{
		return new Vector4d(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
	}

	public void Scale(Vector4d scale)
    {
        x *= scale.x;
        y *= scale.y;
        z *= scale.z;
        w *= scale.w;
    }

    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2 ^ w.GetHashCode() >> 1;
    }

    public override bool Equals(object other)
    {
        if (!(other is Vector4d))
            return false;
        Vector4d vector4d = (Vector4d)other;
        if (x.Equals(vector4d.x) && y.Equals(vector4d.y) && z.Equals(vector4d.z))
            return w.Equals(vector4d.w);
        return false;
    }

    public static Vector4d Normalize(Vector4d value)
    {
        double num = Magnitude(value);
        if (num > 9.99999974737875E-06)
            return value / num;
        return zero;
    }

    public void Normalize()
    {
        double num = Magnitude(this);
        if (num > 9.99999974737875E-06)
            this = this / num;
        else
            this = zero;
    }

    public override string ToString()
    {
        return string.Format("({0:F1}, {1:F1}, {2:F1}, {3:F1})", x, y, z, w);
    }

    public string ToString(string format)
    {
        return string.Format("({0}, {1}, {2}, {3})", x.ToString(format), y.ToString(format), z.ToString(format), w.ToString(format));
    }

	public static Vector4d Lerp(Vector4d a, Vector4d b, double t)
	{
		t = Mathd.Clamp01(t);
		return new Vector4d(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t);
	}

	public static Vector4d LerpUnclamped(Vector4d a, Vector4d b, double t)
	{
		return new Vector4d(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t);
	}

	public static double Dot(Vector4d lhs, Vector4d rhs)
    {
        return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w;
    }

    public static Vector4d Project(Vector4d vector, Vector4d onNormal)
    {
        double num = Dot(onNormal, onNormal);
        if (num < 1.40129846432482E-45d)
            return zero;
        else
            return onNormal * Dot(vector, onNormal) / num;
    }

    public static double Distance(Vector4d a, Vector4d b)
    {
        return Magnitude(a - b);
    }

    public static Vector4d ClampMagnitude(Vector4d vector, double maxLength)
    {
        if (vector.sqrMagnitude > maxLength * maxLength)
            return vector.normalized * maxLength;
        else
            return vector;
    }

    public static double Magnitude(Vector4d a)
    {
        return Math.Sqrt(Dot(a, a));
    }

    public static double SqrMagnitude(Vector4d a)
    {
        return Dot(a, a);
    }

    public static Vector4d Min(Vector4d lhs, Vector4d rhs)
    {
        return new Vector4d(Math.Min(lhs.x, rhs.x), Math.Min(lhs.y, rhs.y), Math.Min(lhs.z, rhs.z), Math.Min(lhs.w, rhs.w));
    }

    public static Vector4d Max(Vector4d lhs, Vector4d rhs)
    {
        return new Vector4d(Math.Max(lhs.x, rhs.x), Math.Max(lhs.y, rhs.y), Math.Max(lhs.z, rhs.z), Math.Max(lhs.w, rhs.w));
    }
}
