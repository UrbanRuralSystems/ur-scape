// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;

public class EmptyEnumerator : IEnumerator
{
	private EmptyEnumerator() { }
	public object Current { get { return null; } }
	public bool MoveNext() { return false; }
	public void Reset() { }

	public static IEnumerator Instance = new EmptyEnumerator();
}
