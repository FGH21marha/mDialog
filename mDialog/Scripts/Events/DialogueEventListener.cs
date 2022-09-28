using UnityEngine.Events;
using UnityEngine;

public class DialogueEventListener : MonoBehaviour
{
    public DialogueEvent dialogueEvent;
    public UnityEvent response;
    public UnityEvent<AnimationClip> animationResponse;
    public UnityEvent<AudioClip> audioResponse;

    private void OnEnable() => dialogueEvent.AddListener(this);
    private void OnDisable() => dialogueEvent.RemoveListener(this);
    public void Raise() => response?.Invoke();
    public void Raise(AnimationClip clip) => animationResponse?.Invoke(clip);
    public void Raise(AudioClip clip) => audioResponse?.Invoke(clip);
}