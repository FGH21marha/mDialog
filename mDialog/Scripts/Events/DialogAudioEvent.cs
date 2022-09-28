using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Dialog/Dialog Audio Event", fileName = "New Dialog Audio Event")]
public class DialogAudioEvent : ScriptableObject
{
    private List<DialogueEventListener> listeners = new List<DialogueEventListener>();

    public void AddListener(DialogueEventListener listener) => listeners.Add(listener);
    public void RemoveListener(DialogueEventListener listener) => listeners.Remove(listener);
    public void Clear() => listeners.Clear();
    public void Raise(AudioClip clip)
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i].Raise(clip);
    }
}