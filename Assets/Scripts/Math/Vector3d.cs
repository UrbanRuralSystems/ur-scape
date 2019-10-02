// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;

public struct Vector3d
{
    public double x;
    public double y;
    public double z;

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
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3d index");
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
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3d index!");
            }
        }
    }

    public Vector3d normalized { get { return Normalize(this); } }
    public double magnitude { get { return Math.Sqrt(x * x + y * y + z * z); } }
    public double sqrMagnitude { get { return x * x + y * y + z * z; } }

	public static readonly Vector3d zero = new Vector3d(0d, 0d, 0d);
	public static readonly Vector3d one = new Vector3d(1d, 1d, 1d);
	public static readonly Vector3d forward = new Vector3d(0d, 0d, 1d);
	public static readonly Vector3d back = new Vector3d(0d, 0d, -1d);
	public static readonly Vector3d up = new Vector3d(0d, 1d, 0d);
	public static readonly Vector3d down = new Vector3d(0d, -1d, 0d);
	public static readonly Vector3d left = new Vector3d(-1d, 0d, 0d);
	public static readonly Vector3d right = new Vector3d(1d, 0d, 0d);

    public Vector3d(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3d(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3d(Vector3 v3)
    {
        x = v3.x;
        y = v3.y;
        z = v3.z;
    }

    public Vector3d(double x, double y)
    {
        this.x = x;
        this.y = y;
        this.z = 0d;
    }

    public static Vector3d operator +(Vector3d a, Vector3d b)
    {
        return new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3d operator -(Vector3d a, Vector3d b)
    {
        return new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static Vector3d operator -(Vector3d a)
    {
        return new Vector3d(-a.x, -a.y, -a.z);
    }

    public static Vector3d operator *(Vector3d a, double d)
    {
        return new Vector3d(a.x * d, a.y * d, a.z * d);
    }

    public static Vector3d operator *(double d, Vector3d a)
    {
        return new Vector3d(a.x * d, a.y * d, a.z * d);
    }

    public static Vector3d operator /(Vector3d a, double d)
    {
        return new Vector3d(a.x / d, a.y / d, a.z / d);
    }

    public static bool operator ==(Vector3d lhs, Vector3d rhs)
    {
        return SqrMagnitude(lhs - rhs) < 0.0 / 1.0;
    }

    public static bool operator !=(Vector3d lhs, Vector3d rhs)
    {
        return SqrMagnitude(lhs - rhs) >= 0.0 / 1.0;
    }

    public void Set(double new_x, double new_y, double new_z)
    {
        x = new_x;
        y = new_y;
        z = new_z;
    }

    public static Vector3d Scale(Vector3d a, Vector3d b)
    {
        return new Vector3d(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public void Scale(Vector3d scale)
    {
        x *= scale.x;
        y *= scale.y;
        z *= scale.z;
    }

    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2;
    }

    public override bool Equals(object other)
    {
        if (!(other is Vector3d))
            return false;
        Vector3d vector3d = (Vector3d)other;
        if (x.Equals(vector3d.x) && y.Equals(vector3d.y))
            return z.Equals(vector3d.z);
        else
            return false;
    }

    public static Vector3d Reflect(Vector3d inDirection, Vector3d inNormal)
    {
        return -2d * Dot(inNormal, inDirection) * inNormal + inDirection;
    }

    public static Vector3d Normalize(Vector3d value)
    {
        double num = Magnitude(value);
        if (num > 9.99999974737875E-06)
            return value / num;
        else
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
        return string.Format("({0:F1}, {1:F1}, {2:F1})", x, y, z);
    }

    public string ToString(string format)
    {
        return string.Format("({0}, {1}, {2})", x.ToString(format), y.ToString(format), z.ToString(format));
    }

	public static Vector3d Lerp(Vector3d from, Vector3d to, double t)
	{
		t = Mathd.Clamp01(t);
		return new Vector3d(from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t, from.z + (to.z - from.z) * t);
	}

	public static Vector3d LerpUnclamped(Vector3d a, Vector3d b, double t)
	{
		return new Vector3d(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
	}


	public static double Dot(Vector3d lhs, Vector3d rhs)
    {
        return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
    }

	public static Vector3d Cross(Vector3d lhs, Vector3d rhs)
	{
		return new Vector3d(lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x);
	}

	public static Vector3d Project(Vector3d vector, Vector3d onNormal)
    {
        double num = Dot(onNormal, onNormal);
        if (num < 1.40129846432482E-45d)
            return zero;
        else
            return onNormal * Dot(vector, onNormal) / num;
    }

    public static double Distance(Vector3d a, Vector3d b)
    {
		return Magnitude(a - b);
    }

    public static Vector3d ClampMagnitude(Vector3d vector, double maxLength)
    {
        if (vector.sqrMagnitude > maxLength * maxLength)
            return vector.normalized * maxLength;
        else
            return vector;
    }

    public static double Magnitude(Vector3d a)
    {
        return Math.Sqrt(Dot(a, a));
    }

    public static double SqrMagnitude(Vector3d a)
    {
        return Dot(a, a);
    }

    public static Vector3d Min(Vector3d lhs, Vector3d rhs)
    {
        return new Vector3d(Math.Min(lhs.x, rhs.x), Math.Min(lhs.y, rhs.y), Math.Min(lhs.z, rhs.z));
    }

    public static Vector3d Max(Vector3d lhs, Vector3d rhs)
    {
        return new Vector3d(Math.Max(lhs.x, rhs.x), Math.Max(lhs.y, rhs.y), Math.Max(lhs.z, rhs.z));
    }
}
