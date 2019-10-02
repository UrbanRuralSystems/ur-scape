// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface IComponent
{
    void ResetComponent();
    bool HasBookmarkData();
    void SaveToBookmark(BinaryWriter bw, string bookmarkPath);
    void LoadFromBookmark(BinaryReader br, string bookmarkPath);
}

public class UrsComponent : MonoBehaviour, IComponent
{
    protected virtual void Awake()
    {
        ComponentManager.Instance.Register(this, GetType());
    }

    protected virtual void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ComponentManager.Instance.Deregister(GetType());
        }
    }

    public virtual void ResetComponent() { }
    public virtual bool HasBookmarkData() { return false; }
    public virtual void SaveToBookmark(BinaryWriter bw, string bookmarkPath) { }
    public virtual void LoadFromBookmark(BinaryReader br, string bookmarkPath) { }

}

public class RegistrationTimer : MonoBehaviour
{
    public delegate void RegistrationFinishedDelegate(RegistrationTimer timer);

    public void StartRegistration(RegistrationFinishedDelegate registrationFinished)
    {
        StartCoroutine(WaitForIt(registrationFinished));
    }

    private IEnumerator WaitForIt(RegistrationFinishedDelegate registrationFinished)
    {
        yield return WaitFor.Frames(WaitFor.InitialFrames);
        registrationFinished(this);
    }
}

public sealed class ComponentManager
{
    private class Nested
    {
        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static Nested() { }
        internal static readonly ComponentManager instance = new ComponentManager();
    }

    public static ComponentManager Instance { get { return Nested.instance; } }

    private Dictionary<Type, IComponent> componentMap = new Dictionary<Type, IComponent>();

    public delegate void OnRegistrationFinishedDelegate();
    public event OnRegistrationFinishedDelegate OnRegistrationFinished;

    private ComponentManager()
    {
        var registrationTimer = new GameObject("RegistrationTimer").AddComponent<RegistrationTimer>();
        registrationTimer.StartRegistration(RegistrationFinished);
    }

    private void RegistrationFinished(RegistrationTimer timer)
    {
        GameObject.Destroy(timer.gameObject);
        if (OnRegistrationFinished != null)
        {
            OnRegistrationFinished();
			OnRegistrationFinished = null;
		}
    }

    public void Register<T>(T component) where T : IComponent
    {
#if SAFETY_CHECK
        if (componentMap.ContainsKey(typeof(T)))
        {
            Debug.LogWarning("Component " + typeof(T).Name + " is already registered!");
            return;
        }
#endif
        componentMap.Add(typeof(T), component);
    }

    public void Register<T>(T component, Type type) where T : IComponent
    {
#if SAFETY_CHECK
        if (componentMap.ContainsKey(type))
        {
            Debug.LogWarning("Component " + type.Name + " is already registered!");
            return;
        }
#endif
        componentMap.Add(type, component);
    }

    public void Deregister<T>(T component) where T : IComponent
    {
        componentMap.Remove(typeof(T));
    }

    public void Deregister(Type type)
    {
        componentMap.Remove(type);
    }

    public bool Has<T>() where T : IComponent
    {
        return componentMap.ContainsKey(typeof(T));
    }

    public T Get<T>() where T : UrsComponent
    {
#if SAFETY_CHECK
        if (!componentMap.ContainsKey(typeof(T)))
        {
            Debug.LogWarning("Couldn't find component " + typeof(T).Name);
            return null;
        }
#endif
        return componentMap[typeof(T)] as T;
    }

    public T GetOrNull<T>() where T : UrsComponent
    {
        if (componentMap.ContainsKey(typeof(T)))
            return componentMap[typeof(T)] as T;
        return null;
    }

    public IEnumerable<IComponent> AllComponents()
    {
        return componentMap.Values;
    }

}
