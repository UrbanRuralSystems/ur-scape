// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

[System.Serializable]
public class TypologyEntry
{
    public string name;
	public Transform icon;
	// to set mist level manualy to shader
	public int strength;
}

[CreateAssetMenu(menuName = "URS/TypologyLibrary")]
public class TypologyLibrary : ScriptableObject
{
    public TypologyEntry[] typologies;

    public TypologyEntry this[int index]
    {
        get { return typologies[index]; }
    }

}
