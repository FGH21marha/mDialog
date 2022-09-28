using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

public class DialogueReader : MonoBehaviour
{
    [SerializeField] public DialogObject dialogue;
    [SerializeField] public DialogVariables ObjectVariables;

    [HideInInspector] public List<string> options = new List<string>();

    public event Action onDialogueStarted;
    public event Action<Node> onDialogueUpdated;
    public event Action onDialogueEnded;

    public static DialogueReader instance;

    private void Awake()
    {
        instance = this;
    }

    public static void StartDialog(DialogObject dialog)
    {
        instance.dialogue = dialog;
        instance.StartDialogue();
    }

    public void StartDialogue()
    {
        options.Clear();
        dialogue.AddListener(onDialogueUpdated, onDialogueEnded);
        onDialogueStarted?.Invoke();

        dialogue.StartDialogue(0);

        if (dialogue.current != null && dialogue.current.GetType() == typeof(MultipleChoiceNode))
            (dialogue.current as MultipleChoiceNode).options.ForEach(option => options.Add(option));
    }

    public void FindNextNode(int option = 0)
    {
        options.Clear();
        dialogue.current?.OnExitNode(option);

        if (dialogue.current != null && dialogue.current.GetType() == typeof(MultipleChoiceNode))
            (dialogue.current as MultipleChoiceNode).options.ForEach(option => options.Add(option));
    }

    public void EndDialogue()
    {
        options.Clear();
        dialogue.EndDialogue();
    }
}

[CustomEditor(typeof(DialogueReader))]
public class DialogueReaderEditor : Editor
{
    SerializedProperty dialogue;
    SerializedProperty ObjectVariables;

    private void OnEnable()
    {
        var serializedObject = new SerializedObject(target);
        dialogue = serializedObject.FindProperty("dialogue");
        ObjectVariables = serializedObject.FindProperty("ObjectVariables");
    }

    public override void OnInspectorGUI()
    {
        var targ = (DialogueReader)target;
        var serializedObject = new SerializedObject(target);
        serializedObject.Update();
        var skin = Resources.Load("DialogueEditorSkin") as GUISkin;

        GUILayout.Label("Dialogue Reader", skin.FindStyle("leftAlignBold"));
        EditorGUILayout.PropertyField(dialogue, GUIContent.none);

        if (GUILayout.Button("Edit Dialogue", GUILayout.ExpandWidth(true), GUILayout.Height(24)))
            DialogEditorWindow.OpenAndEditDialogue(targ.dialogue);

        EditorGUILayout.PropertyField(ObjectVariables, true);

        if (targ.dialogue != null)
            targ.dialogue.UpdateDialogueReaders(targ);

        serializedObject.ApplyModifiedProperties();
    }
}