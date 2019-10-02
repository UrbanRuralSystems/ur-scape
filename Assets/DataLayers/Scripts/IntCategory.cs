// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;

public class IntCategory : Category, IEquatable<IntCategory>
{
	public int value;

	public override bool Equals(object other)
	{
		return base.Equals(other) && value == ((IntCategory)other).value;
	}

	public bool Equals(IntCategory other)
	{
		return base.Equals(other) && value == other.value;
	}

	public override int GetHashCode()
	{
		var hashCode = 1737533024;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + value.GetHashCode();
		return hashCode;
	}

	public static bool operator ==(IntCategory category1, IntCategory category2)
	{
		return EqualityComparer<IntCategory>.Default.Equals(category1, category2);
	}

	public static bool operator !=(IntCategory category1, IntCategory category2)
	{
		return !(category1 == category2);
	}
}
