using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Dialog/Dialog Animation Event", fileName = "New Dialog Animation Event")]
public class DialogAnimationEvent : ScriptableObject
{
    private List<DialogueEventListener> listeners = new List<DialogueEventListener>();

    public void AddListener(DialogueEventListener listener) => listeners.Add(listener);
    public void RemoveListener(DialogueEventListener listener) => listeners.Remove(listener);
    public void Clear() => listeners.Clear();
    public void Raise(AnimationClip clip)
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i].Raise(clip);
    }
}