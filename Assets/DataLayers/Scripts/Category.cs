// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;

public class Category : IEquatable<Category>
{
	public string name;
	public Color color;

	public override bool Equals(object other)
	{
		if (other == null || !GetType().Equals(other.GetType()))
			return false;
		if (ReferenceEquals(this, other))
			return true;

		Category cat = (Category)other;
		return name.Equals(cat.name) && color.Equals(cat.color);
	}

	public bool Equals(Category other)
	{
		return other != null &&
			   name.Equals(other.name) &&
			   color.Equals(other.color);
	}

	public override int GetHashCode()
	{
		var hashCode = 1395206702;
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
		hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(color);
		return hashCode;
	}

	public static bool operator ==(Category category1, Category category2)
	{
		return EqualityComparer<Category>.Default.Equals(category1, category2);
	}

	public static bool operator !=(Category category1, Category category2)
	{
		return !(category1 == category2);
	}

}
