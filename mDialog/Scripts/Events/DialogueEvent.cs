using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Dialog/Dialog Event", fileName = "New Dialog Event")]
public class DialogueEvent : ScriptableObject
{
    private List<DialogueEventListener> listeners = new List<DialogueEventListener>();

    public void AddListener(DialogueEventListener listener) => listeners.Add(listener);
    public void RemoveListener(DialogueEventListener listener) => listeners.Remove(listener);
    public void Clear() => listeners.Clear();
    public void Raise()
    {
        for (int i = listeners.Count-1; i >= 0; i--)
            listeners[i].Raise();
    }
}