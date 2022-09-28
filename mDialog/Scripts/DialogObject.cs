using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
#endif

[CreateAssetMenu(menuName = "Dialog/New Dialog Object")]
public class DialogObject : ScriptableObject
{
    public string graphTitle = "Title...";
    public string graphDescription = "Description...";

    public List<DialogueReaderReference> DialogueReaders = new List<DialogueReaderReference>();
    [SerializeReference] public List<Node> Nodes = new List<Node>();
    public List<Connection> connections = new List<Connection>();
    public DialogVariables GraphVariables = new DialogVariables();
    public DialogSpeakers GraphSpeakers = new DialogSpeakers();
    public Vector2 windowPos;

    public int StartNodeCount
    {
        get
        {
            return Nodes.FindAll(x => x.GetType() == typeof(StartNode)).Count;
        }
    }

    [SerializeReference] public Node current;

    public event Action onDialogueStarted;
    public event Action<Node> onDialogueUpdated;
    public event Action onDialogueEnded;
    public bool DialogueActive { get; private set; }
    public void AddListener(Action<Node> updateSubscriber, Action endedSubscriber)
    {
        onDialogueUpdated += updateSubscriber;
        onDialogueEnded += endedSubscriber;
    }

    private void OnValidate()
    {
        UpdateDialogueReaders();
    }
    public void UpdateDialogueReaders(DialogueReader reader = null)
    {
        if (reader != null)
        {
            var thisScene = new DialogueReaderReference { GameObject = reader.gameObject.name, SceneName = EditorSceneManager.GetActiveScene().name };

            if (!DialogueReaders.Contains(thisScene))
                DialogueReaders.Add(thisScene);
        }

        if (DialogueReaders.Count == 0) return;

        for (int i = 0; i < DialogueReaders.Count; i++)
        {
            var thisReader = DialogueReaders[i];
            var obj = GameObject.Find(thisReader.GameObject);

            if(obj == null)
            {
                DialogueReaders.Remove(DialogueReaders[i]);
                i--;
                continue;
            }

            var thisComp = obj.GetComponent<DialogueReader>();

            if (thisComp == null)
            {
                DialogueReaders.Remove(DialogueReaders[i]);
                i--;
                continue;
            }
            else if (thisComp.dialogue != this)
            {
                DialogueReaders.Remove(DialogueReaders[i]);
                i--;
                continue;
            }
        }
    }

    public void StartDialogue(int startID)
    {
        DialogueActive = true;
        onDialogueStarted?.Invoke();
        var start = FindStartNode(startID);

        if (start != null)
        {
            start.OnEnterNode();
            start.OnExitNode();
        }
        else
            EndDialogue();
    }
    public void UpdateDialogue(Node newNode)
    {
        current = newNode;
        onDialogueUpdated?.Invoke(newNode);

        current.OnEnterNode();
    }
    public void EndDialogue()
    {
        DialogueActive = false;
        onDialogueEnded?.Invoke();
        current = null;

        ClearListeners();
    }

    public void ClearListeners()
    {
        if(onDialogueUpdated != null)
        {
            var list1 = onDialogueUpdated.GetInvocationList();
            foreach (Action<Node> listener in list1)
                onDialogueUpdated -= listener;
        }

        if(onDialogueEnded != null)
        {
            var list2 = onDialogueEnded.GetInvocationList();
            foreach (Action listener in list2)
                onDialogueEnded -= listener;
        }
    }

    public void TryReset()
    {
        var ResetNode = Nodes.Find(x => x.GetType() == typeof(ResetNode));

        if(ResetNode != null)
        {
            var next = ResetNode.OnExitNode();
            next = next?.OnExitNode();
        }
    }

    public DialogVariable GetVariable(string ID)
    {
        return GraphVariables.variables.Find(x => x.variableID == ID);
    }
    public List<DialogVariable> GetOptionsOfType(VariableType type)
    {
        List<DialogVariable> options = new List<DialogVariable>();
        var variableMatch = GraphVariables.variables.Where(x => x.type == type).ToList();
        variableMatch.ForEach(variable => options.Add(variable));
        return options;
    }
    public List<DialogVariable> GetOptions()
    {
        return GraphVariables.variables;
    }

    public Node GetNodeByGUID(string GUID)
    {
        return Nodes.Find(x => x.GUID == GUID);
    }
    public Port GetPortByGUID(string GUID)
    {
        foreach (var node in Nodes)
        {
            var a = node.InputPorts.Find(p => p.GUID == GUID);
            var b = node.OutputPorts.Find(p => p.GUID == GUID);
            var c = node.ValueOutputPorts.Find(p => p.GUID == GUID);
            var d = node.ValueInputPorts.Find(p => p.GUID == GUID);

            if (a != null) return a;
            if (b != null) return b;
            if (c != null) return c;
            if (d != null) return d;
        }
        return null;
    }
    public Node GetConnectedFlowNodes(string currentNodeGUID)
    {
        var connection = connections.Find(c => c.startNodeGUID == currentNodeGUID);

        if (connection == null) return null;

        var port = GetPortByGUID(connection.targetNodeGUID);
        if (port == null) return null;

        var node = GetNodeByGUID(port.parentNodeGUID);
        if (node == null) return null;

        return node;
    }

    public List<Node> GetConnectedValueNodes(Node node, Action<Node> perNode = null)
    {
        List<Node> result = new List<Node>();
        Node current = node;

        if (current == null)
            return result;

        if (current != null)
        {
            for (int i = 0; i < 128; i++)
            {
                current = GetFirstValueNode(current.FirstValueInput);

                if (current == null) break;

                perNode?.Invoke(current);
                result.Add(current);
            }
        }

        return result;
    }
    public Node GetFirstValueNode(string currentNodeGUID)
    {
        var connection = connections.Find(c => c.startNodeGUID == currentNodeGUID);
        var port = GetPortByGUID(connection?.targetNodeGUID);
        var node = GetNodeByGUID(port?.parentNodeGUID);

        return node;
    }

    public Connection GetConnection(Connection compareConnection)
    {
        foreach (var connection in connections)
        {
            if(connection.startNodeGUID == compareConnection.startNodeGUID && connection.targetNodeGUID == compareConnection.targetNodeGUID ||
                connection.startNodeGUID == compareConnection.targetNodeGUID && connection.targetNodeGUID == compareConnection.startNodeGUID)
            {
                return connection;
            }
        }

        return null;
    }
    public bool IsPortOccupied(string portGUID)
    {
        foreach (var connection in connections)
        {
            if (connection.startNodeGUID == portGUID || connection.targetNodeGUID == portGUID)
            {
                return true;
            }
        }

        return false;
    }

    public void AddNewNode(Node newNode)
    {
        Nodes.Add(newNode);
    }
    public void RemoveNode(Node node, Action<string> onRemovedNode = null)
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            if (Nodes[i].GUID == node.GUID)
            {
                onRemovedNode?.Invoke(Nodes[i].GUID);
                Nodes.Remove(Nodes[i]);
                break;
            }
        }
    }
    public void RemoveConnection(string PortGUID)
    {
        for (int i = 0; i < connections.Count; i++)
            if(connections[i].startNodeGUID == PortGUID || connections[i].targetNodeGUID == PortGUID)
                connections.RemoveAt(i);
    }

    public Node FindStartNode(int ID)
    {
        var startNodes = GetStartNodes();

        StartNode next = null;

        foreach (var node in startNodes)
        {
            if (node.ID == ID)
            {
                next = node;
                break;
            }
        }
        
        return next;
    }
    public Node FindNextNode(Node current = null, int output = 0)
    {
        if (current != null)
        {
            if (current.OutputPorts.Count == 0)
            {
                EndDialogue();
                return null;
            }

            if (output > current.OutputPorts.Count - 1)
                output = 0;

            var i = GetConnectedFlowNodes(current.OutputPorts[output].GUID);

            if (i != null)
                return i;
        }

        EndDialogue();
        return null;
    }
    public Node FindToPrompt(int ID)
    {
        var ToNodes = GetToNodes();

        if (ToNodes.Count == 0)
            return null;

        ToNode next = null;

        foreach (var node in ToNodes)
        {
            if (node.ID == ID)
            {
                next = node;
                break;
            }
        }

        if (next != null)
            return next;

        return null;
    }
    private List<StartNode> GetStartNodes()
    {
        var startNodes = Nodes.FindAll(x => x is StartNode).ToList();
        List<StartNode> result = new List<StartNode>();
        startNodes.ForEach(x => result.Add((StartNode)x));
        return result;
    }
    private List<ToNode> GetToNodes()
    {
        var toNodes = Nodes.FindAll(x => x is ToNode).ToList();
        List<ToNode> result = new List<ToNode>();
        toNodes.ForEach(x => result.Add((ToNode)x));
        return result;
    }
}

[CustomEditor(typeof(DialogObject))]
public class DialogObjectEditor : Editor
{
    private void OnEnable()
    {
        var targ = (DialogObject)target;
        targ.UpdateDialogueReaders();
    }
    public override void OnInspectorGUI()
    {
        var targ = (DialogObject)target;

        EditorGUI.indentLevel--;

        GUISkin nodeStyle = Resources.Load("DialogueEditorSkin") as GUISkin;

        targ.graphTitle = EditorGUILayout.TextField(targ.graphTitle, nodeStyle.FindStyle("graphTitle"), GUILayout.Height(26), GUILayout.ExpandWidth(true));
        targ.graphDescription = EditorGUILayout.TextArea(targ.graphDescription, nodeStyle.FindStyle("graphDescription"), GUILayout.Height(78), GUILayout.ExpandWidth(true));

        SerializedObject serializedObject = new SerializedObject(targ);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("GraphVariables"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GraphSpeakers"));

        EditorGUI.indentLevel++;

        var a = EditorGUI.GetPropertyHeight(serializedObject.FindProperty("GraphVariables"));
        var b = EditorGUI.GetPropertyHeight(serializedObject.FindProperty("GraphSpeakers"));
        var c = EditorGUI.GetPropertyHeight(serializedObject.FindProperty("graphTitle"));
        var d = EditorGUI.GetPropertyHeight(serializedObject.FindProperty("graphDescription"));

        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(48);

        var buttonRect = new Rect(4, a + b + c + d + 86, EditorGUIUtility.currentViewWidth - 10, 42);

        if (GUI.Button(buttonRect, "Edit Dialogue", nodeStyle.button))
            DialogEditorWindow.OpenAndEditDialogue(targ);

        Repaint();
    }
}

[Serializable] public struct DialogueReaderReference
{
    public string GameObject;
    public string SceneName;
}
public enum VariableType { String, Int, Float, Bool, Object, Animation, Sprite }
[Serializable] public class DialogVariables
{
    public List<DialogVariable> variables = new List<DialogVariable>();
}
[Serializable] public class DialogVariable
{
    public VariableType type;
    public string variableID;

    public string stringValue;
    public int intValue;
    public float floatValue;
    public bool boolValue;
    public float boolValueAnimationTime;

    public GameObject objectValue;

    public Animator animator;
    public string animationName;

    public Sprite spriteValue;
}
[Serializable] public class DialogSpeakers
{
    public List<string> speakers = new List<string>();
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DialogVariables))] public class DialogVariablesDrawer : PropertyDrawer
{
    private bool initialized = false;
    ReorderableList list;
    private GUISkin nodeStyle;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (list != null)
            return list.GetHeight();
        else return 20;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var variables = property.FindPropertyRelative("variables");

        if (!initialized)
            Initialize(property, variables);

        list.showDefaultBackground = false;

        list.elementHeight = 44;

        EditorGUI.DrawRect(new Rect(position.x - 40, position.y + 1, position.width + 80, position.height - 22), new Color(0.25f, 0.25f, 0.25f, 1f));

        list.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.DrawRect(new Rect(rect.x - 40, rect.y, rect.width + 80, rect.height), new Color(0.15f, 0.15f, 0.15f, 1f));
            EditorGUI.LabelField(rect, label);
        };

        list.headerHeight = 22;

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
        {
            EditorGUI.PropertyField(rect, variables.GetArrayElementAtIndex(index));
        };

        list.drawFooterCallback = (Rect rect) =>
        {
            var footerRect = new Rect(rect.width - 62, rect.y - 1, 54, 20);
            GUI.Box(footerRect, GUIContent.none, nodeStyle.FindStyle("listFooter"));

            var addRect = new Rect(footerRect.x + 6, footerRect.y + 2, 24, 16);
            if (GUI.Button(addRect, new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus@2x")), GUIStyle.none))
            {
                variables.arraySize++;
                list.index = variables.arraySize - 1;
            }

            var removeRect = new Rect(footerRect.x + 30, footerRect.y + 2, 24, 16);
            if (GUI.Button(removeRect, new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Minus@2x")), GUIStyle.none))
            {
                if (variables.arraySize >= 1)
                {
                    variables.arraySize--;
                    list.index = variables.arraySize - 1;
                    return;
                }
            }
        };

        list.DoList(position);

        property.serializedObject.ApplyModifiedProperties();
    }
    private void Initialize(SerializedProperty property, SerializedProperty variables)
    {
        initialized = true;
        list = new ReorderableList(property.serializedObject, variables, true, true, true, true);
        nodeStyle = Resources.Load("DialogueEditorSkin") as GUISkin;
        list.draggable = true;
    }
}
[CustomPropertyDrawer(typeof(DialogVariable))] public class DialogVariableDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 46;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUISkin nodeStyle = Resources.Load("DialogueEditorSkin") as GUISkin;

        var valueIDLabelRect = new Rect(position.x + 6, position.y + 2, 74, 20);
        EditorGUI.LabelField(valueIDLabelRect, "Variable ID", nodeStyle.FindStyle("rightAlignBold"));

        var valueIDRect = new Rect(position.x + 82, position.y + 2, position.width - 78, 20);
        property.FindPropertyRelative("variableID").stringValue = EditorGUI.TextField(valueIDRect, property.FindPropertyRelative("variableID").stringValue, nodeStyle.textField);

        var enumRect = new Rect(position.x + 6, position.y + 24, 72, 20);
        property.FindPropertyRelative("type").enumValueIndex =
            EditorGUI.Popup(enumRect, property.FindPropertyRelative("type").enumValueIndex,
            Enum.GetNames(typeof(VariableType)), nodeStyle.FindStyle("enumField"));

        var ValueRect = new Rect(position.x + 82, position.y + 24, position.width - 78, 20);
        switch ((VariableType)property.FindPropertyRelative("type").enumValueIndex)
        {
            case VariableType.String: 
                property.FindPropertyRelative("stringValue").stringValue =
                    EditorGUI.TextField(ValueRect, 
                    property.FindPropertyRelative("stringValue").stringValue,
                    nodeStyle.textField); break;
            case VariableType.Int:
                property.FindPropertyRelative("intValue").intValue =
                    EditorGUI.IntField(ValueRect,
                    property.FindPropertyRelative("intValue").intValue,
                    nodeStyle.textField); break;
            case VariableType.Float:
                property.FindPropertyRelative("floatValue").floatValue =
                    EditorGUI.FloatField(ValueRect,
                    property.FindPropertyRelative("floatValue").floatValue,
                    nodeStyle.textField); break;
            case VariableType.Bool:
                bool a = property.FindPropertyRelative("boolValue").boolValue;
                float b = property.FindPropertyRelative("boolValueAnimationTime").floatValue;
                var toggleRect = new Rect(ValueRect.x + 4, ValueRect.y + 5, 32, 20);
                DialogEditorUtility.DrawBoolField(toggleRect, ref a, ref b, nodeStyle, 2f);
                property.FindPropertyRelative("boolValue").boolValue = a;
                property.FindPropertyRelative("boolValueAnimationTime").floatValue = b;
                break;
            case VariableType.Object:
                EditorGUI.ObjectField(ValueRect, property.FindPropertyRelative("objectValue"), typeof(GameObject), GUIContent.none); break;
            case VariableType.Animation:
                EditorGUI.ObjectField(ValueRect, property.FindPropertyRelative("animator"), typeof(Animator), GUIContent.none); break;
        }

        property.serializedObject.ApplyModifiedProperties();
    }
}
[CustomPropertyDrawer(typeof(DialogSpeakers))] public class DialogSpeakersDrawer : PropertyDrawer
{
    private bool initialized = false;
    ReorderableList list;
    private GUISkin nodeStyle;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (list != null)
            return list.GetHeight();
        else return 20;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var variables = property.FindPropertyRelative("speakers");

        if (!initialized)
            Initialize(property, variables);

        list.showDefaultBackground = false;

        list.elementHeight = 20;

        EditorGUI.DrawRect(new Rect(position.x - 40, position.y+1, position.width + 80, position.height-22), new Color(0.25f, 0.25f, 0.25f, 1f));

        list.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.DrawRect(new Rect(rect.x - 40, rect.y, rect.width + 80, rect.height), new Color(0.15f, 0.15f, 0.15f, 1f));
            EditorGUI.LabelField(rect, label);
        };

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            variables.GetArrayElementAtIndex(index).stringValue = EditorGUI.TextField(new Rect(rect.x+4, rect.y + 2, rect.width, 18), variables.GetArrayElementAtIndex(index).stringValue, nodeStyle.textField);
        };

        list.drawFooterCallback = (Rect rect) =>
        {
            var footerRect = new Rect(rect.width - 62, rect.y - 1, 54, 20);
            GUI.Box(footerRect, GUIContent.none, nodeStyle.FindStyle("listFooter"));

            var addRect = new Rect(footerRect.x + 6, footerRect.y + 2, 24, 16);
            if(GUI.Button(addRect, new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus@2x")), GUIStyle.none))
            {
                variables.arraySize++;
                list.index = variables.arraySize -1;
            }

            var removeRect = new Rect(footerRect.x + 30, footerRect.y + 2, 24, 16);
            if (GUI.Button(removeRect, new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Minus@2x")), GUIStyle.none))
            {
                if(variables.arraySize >= 1)
                {
                    variables.arraySize--;
                    list.index = variables.arraySize - 1;
                }
            }
        };


        list.DoList(position);

        property.serializedObject.ApplyModifiedProperties();
    }
    private void Initialize(SerializedProperty property, SerializedProperty variables)
    {
        initialized = true;
        list = new ReorderableList(property.serializedObject, variables, true, true, true, true);
        nodeStyle = Resources.Load("DialogueEditorSkin") as GUISkin;
        list.draggable = true;
    }
}
#endif