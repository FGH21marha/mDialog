using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable] public class Node
{
    public static Vector2 initialSize = new Vector2(72, 72);
    protected Vector2 minimumSize;
    public float MinX => minimumSize.x - 40 + optionsSizeModifier.x;
    public float MinY => minimumSize.y - 40 + optionsSizeModifier.y;
    public float MaxX => 400 + optionsSizeModifier.x;
    public float MaxY => 400 + optionsSizeModifier.y;

    public string GUID;
    public int nodeID;

    public NodeType type = NodeType.Custom;

    public DialogObject dialogObject;
    public Vector2 optionsSizeModifier = Vector2.zero;

    public Color titleColor = new Color(0.25f, 0.25f, 0.25f,1);

    public drawNodeTitleDelegate drawNodeTitle;
    public drawNodeContentDelegate drawNodeContent;
    public onSelectNode onSelectNode;
    public onDeselectNode onDeselectNode;
    public onEnable onEnableNode;
    public onDisable onDisableNode;
    public onDestroy onDestroyNode;

    public List<Port> InputPorts = new List<Port>();
    public List<Port> OutputPorts = new List<Port>();
    public List<Port> ValueOutputPorts = new List<Port>();
    public List<Port> ValueInputPorts = new List<Port>();

    public string FirstInput => InputPorts.Count > 0 ? InputPorts[0].GUID : "";
    public string FirstOutput => OutputPorts.Count > 0 ? OutputPorts[0].GUID : "";
    public string FirstValueInput => ValueInputPorts.Count > 0 ? ValueInputPorts[0].GUID : "";
    public string FirstValueOutput => ValueOutputPorts.Count > 0 ? ValueOutputPorts[0].GUID : "";

    public bool selected;
    public bool visible = true;
    public bool expanded = true;
    public bool draggable = true;
    public bool resizable = false;

    public Rect Rect = new Rect(Vector2.one * 200, initialSize);
    public Rect TitleRect => new Rect(0, 0, Rect.width, 24);
    public Rect ContentRect => new Rect(0, TitleRect.height, Rect.width, Rect.height - TitleRect.height);
    public Rect DiagonalResizeRect => new Rect(Rect.position.x + Rect.width - 8, Rect.position.y + Rect.height - 8, 12, 12);
    public Rect HorizontalResizeRect => new Rect(Rect.position.x + Rect.width - 8, Rect.position.y, 12, Rect.height - 12);
    public Rect VerticalResizeRect => new Rect(Rect.position.x, Rect.position.y + Rect.height - 8, Rect.width - 8, 12);

    protected float highlightAlpha;

    public Node()
    {
        InputPorts.Add(new Port(PortDirection.input, GUID));
        OutputPorts.Add(new Port(PortDirection.output, GUID));
        ValueOutputPorts.Add(new Port(PortDirection.valueIn, GUID));
        ValueInputPorts.Add(new Port(PortDirection.valueOut, GUID));
        minimumSize = initialSize;

        drawNodeTitle = (rect, skin) =>
        {
            GUI.color = titleColor;
            GUI.Box(rect, GUIContent.none, skin.FindStyle("nodeTitle"));
        };
        onSelectNode = (node) =>
        {
            GUI.FocusWindow(node.nodeID);
        };
        onDeselectNode = (node) =>
        {
            GUI.FocusWindow(-1);
        };
    }

    public Node SetTarget(DialogObject target)
    {
        dialogObject = target;
        return this;
    }

    public void SetActive(bool state) => visible = state;
    public void SetPosition(Vector2 position) => Rect.position = position - Rect.size / 2;
    public void OnCreateNode()
    {
        GUID = Guid.NewGuid().ToString();
        nodeID = UnityEngine.Random.Range(0, 1000000);

        for (int i = 0; i < InputPorts.Count; i++)
        {
            InputPorts[i].GUID = Guid.NewGuid().ToString();
            InputPorts[i].parentNodeGUID = GUID;
        }

        for (int i = 0; i < OutputPorts.Count; i++)
        {
            OutputPorts[i].GUID = Guid.NewGuid().ToString();
            OutputPorts[i].parentNodeGUID = GUID;
        }

        for (int i = 0; i < ValueOutputPorts.Count; i++)
        {
            ValueOutputPorts[i].GUID = Guid.NewGuid().ToString();
            ValueOutputPorts[i].parentNodeGUID = GUID;
        }

        for (int i = 0; i < ValueInputPorts.Count; i++)
        {
            ValueInputPorts[i].GUID = Guid.NewGuid().ToString();
            ValueInputPorts[i].parentNodeGUID = GUID;
        }
    }
    public void OnEnable()
    {
        onEnableNode?.Invoke(this);
    }
    public void OnDisable()
    {
        onDisableNode?.Invoke(this);
    }
    public void OnDestroy()
    {
        onDestroyNode?.Invoke(this);
    }

    public virtual Node ProcessNode() 
    {
        highlightAlpha = 1f;
        return this; 
    }
    public virtual Node OnEnterNode(int option = 0)
    {
        highlightAlpha = 1f;

        var valueNodes = dialogObject.GetConnectedValueNodes(this);

        if (valueNodes.Count != 0)
            valueNodes.ForEach(node => node.ProcessNode());

        return this;
    }
    public virtual Node OnExitNode(int option = 0)
    {
        var node = dialogObject.FindNextNode(this, option);

        if (node != null)
            dialogObject.UpdateDialogue(node);

        return node;
    }
    public virtual Node PrintNodeInfo()
    {
        Debug.Log(GetType());
        return this;
    }

    public Node Duplicate()
    {
        var newNode = (Node)MemberwiseClone();
        newNode.OnCreateNode();
        newNode.OnEnable();
        return newNode;
    }

    public virtual void DrawTitle(Rect windowRect, GUISkin nodeStyle = null, float deltatime = 0.003f)
    {
        GUI.color = titleColor;
        GUI.Box(windowRect, GUIContent.none, nodeStyle.FindStyle("nodeTitle"));
        GUI.color = Color.white;

        drawNodeTitle?.Invoke(windowRect, nodeStyle);
    }
    public virtual void DrawWindow(Rect windowRect, GUISkin nodeStyle = null, bool canEdit = true, float deltaTime = 0.003f)
    {
        var color = GUI.color;
        GUI.color = nodeStyle.FindStyle("nodeBackground").normal.textColor;
        GUI.Box(ContentRect, GUIContent.none, nodeStyle.FindStyle("nodeBackground"));

        if (highlightAlpha > 0f)
        {
            highlightAlpha -= deltaTime;
            var c = nodeStyle.settings.selectionColor;
            c.a = highlightAlpha;
            GUI.color = c;
            GUI.Box(ContentRect, GUIContent.none, nodeStyle.FindStyle("nodeBackground"));
        }
        else
            highlightAlpha = 0f;

        GUI.color = color;

        var contentRect = new Rect(ContentRect.x + 4, ContentRect.y + 4, ContentRect.width - 8, ContentRect.height - 8);
        using (new GUILayout.AreaScope(contentRect))
        {
            drawNodeContent?.Invoke(new Rect(0, 0, ContentRect.width-8, ContentRect.height-8), nodeStyle);
        }

        var e = Event.current;

        if (e.type == EventType.MouseUp && e.button == 1 && e.control)
            dialogObject.RemoveNode(this);
    }
    public virtual void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        var inOffset = Vector2.left * 10;
        var outOffset = Vector2.right * 10 + Vector2.right * Rect.size.x;

        var valueInOffset = Vector2.down * 11 + Vector2.right * Rect.size.x / 2;
        var valueOutOffset = Vector2.up * (Rect.size.y + 11) + Vector2.right * Rect.size.x / 2;

        foreach (var port in InputPorts)
        {
            port.open = !dialogObject.IsPortOccupied(port.GUID);
            port.DrawPort(Rect, nodeStyle, "connector", inOffset, onPortClicked, visible);
        }

        foreach (var port in OutputPorts)
        {
            port.open = !dialogObject.IsPortOccupied(port.GUID);
            port.DrawPort(Rect, nodeStyle, "connector", outOffset, onPortClicked, visible);
        }

        foreach (var port in ValueOutputPorts)
        {
            port.open = !dialogObject.IsPortOccupied(port.GUID);
            port.DrawPort(Rect, nodeStyle, "connectorValue", valueInOffset, onPortClicked, visible);
        }

        foreach (var port in ValueInputPorts)
        {
            port.open = !dialogObject.IsPortOccupied(port.GUID);
            port.DrawPort(Rect, nodeStyle, "connectorValue", valueOutOffset, onPortClicked, visible);
        }

        if(InputPorts.Count >= 1)
            InputPorts[0].heightOffset = 36f;

        if (OutputPorts.Count >= 1)
            OutputPorts[0].heightOffset = 36f;
    }
    public virtual void DragWindow(Vector2 delta)
    {
        if (draggable)
            Rect.position += delta;
    }
}
[Serializable] public class Connection
{
    public string startNodeGUID;
    public string targetNodeGUID;

    public bool visible;

    public void DrawConnection(DialogObject targetObject, out bool deleted, GUISkin nodeStyle = null)
    {
        deleted = false;

        if (targetObject.GetPortByGUID(startNodeGUID) == null || targetObject.GetPortByGUID(targetNodeGUID) == null)
        {
            targetObject.connections.Remove(this);
            deleted = true;
            return;
        }

        if (targetObject.GetPortByGUID(startNodeGUID).direction == PortDirection.output)
        {
            DialogEditorUtility.DrawFlowConnection(startNodeGUID, targetNodeGUID, targetObject, PortDirection.output, nodeStyle, "connection");
        }
        else if (targetObject.GetPortByGUID(startNodeGUID).direction == PortDirection.valueOut)
        {
            DialogEditorUtility.DrawValueConnection(startNodeGUID, targetNodeGUID, targetObject, PortDirection.valueOut, nodeStyle, "connectionValue");
        }
    }
}
[Serializable] public class Port
{
    public string GUID;
    public string parentNodeGUID;
    public Rect rect;
    public Rect screenRect;
    public Vector2 position;
    public float heightOffset;
    public float widthOffset;
    public PortDirection direction;

    public bool active = true;
    public bool open = true;

    public Port(PortDirection direction, string parentNodeGUID)
    {
        GUID = Guid.NewGuid().ToString();
        rect = new Rect(-10, -10, 20, 20);
        this.direction = direction;
        this.parentNodeGUID = parentNodeGUID;
    }

    public void DrawPort(Rect windowRect, GUISkin nodeStyle = null, string style = null, Vector2 offset = new Vector2(), Action<string, PortDirection> onPortClick = null, bool visible = true)
    {
        var rect = new Rect(windowRect.position + this.rect.position + offset + Vector2.up * heightOffset + Vector2.right * widthOffset, this.rect.size);
        screenRect = rect;

        position = rect.position;

        if (!visible || !active) return;

        var isOpen = open ? "Open" : "";

        var color = GUI.color;
        GUI.color = nodeStyle.FindStyle(style + isOpen).normal.textColor;
        GUI.Box(rect, GUIContent.none, nodeStyle.FindStyle(style + isOpen));

        if(rect.Contains(Event.current.mousePosition))
            onPortClick.Invoke(GUID, direction);

        GUI.color = color;
    }
}

public delegate void drawNodeTitleDelegate(Rect titleRect, GUISkin skin);
public delegate void drawNodeContentDelegate(Rect windowRect, GUISkin skin);
public delegate void onSelectNode(Node node);
public delegate void onDeselectNode(Node node);
public delegate void onEnable(Node node);
public delegate void onDisable(Node node);
public delegate void onDestroy(Node node);

public enum PortDirection { input, output, valueIn, valueOut }
public enum NodeType { Flow, Dialogue, Variable, Conditional, Animation, Custom }
public interface ValueNode<T>
{
    public T GetValue();
}
public interface GroupableNode 
{ 
}

[Serializable] public class StartNode : Node
{
    public int ID;

    public StartNode()
    {
        Rect.size = new Vector2(80, 52);
        InputPorts.Clear();
        ValueOutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Flow;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.1f, 0.6f, 0.8f, 1);
            GUI.Label(rect, "Start", skin.label);
        };

        drawNodeContent = (rect, skin) => 
        {
            GUI.Label(new Rect(rect.x + rect.width - 20, rect.y, 22, 20), "ID", skin.FindStyle("leftAlign"));
            ID = EditorGUI.IntField(new Rect(rect.x, rect.y, rect.width - 24, 20), ID, skin.textField);
        };
    }
}
[Serializable] public class ResetNode : Node
{
    public ResetNode()
    {
        Rect.size = new Vector2(80, 52);
        InputPorts.Clear();
        ValueOutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Flow;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.1f, 0.6f, 0.8f, 1);
            GUI.Label(rect, "Reset", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x + rect.width - 20, rect.y, 22, 20), "ID", skin.FindStyle("leftAlign"));
        };
    }
}
[Serializable] public class DialogNode : Node
{
    public string Speaker = "Speaker...";
    public string DialogText = "Dialog...";
    public DialogNode()
    {
        Rect.size = new Vector2(160, 120);
        minimumSize = Rect.size;
        ValueOutputPorts.Clear();
        type = NodeType.Dialogue;
        resizable = true;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            GUI.Label(rect, "Prompt", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            Speaker = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width - 20, 20), Speaker, skin.textField);
            DialogText = EditorGUI.TextArea(new Rect(rect.x, rect.y + 24, rect.width, rect.height - 24 - optionsSizeModifier.y), DialogText, skin.textArea);

            if (GUI.Button(new Rect(rect.x + rect.width - 24, rect.y, 20, 20), GUIContent.none, skin.FindStyle("blank")))
            {
                var pos = Event.current.mousePosition;
                GenericMenu menu = new GenericMenu();

                foreach (var variable in dialogObject.GraphSpeakers.speakers)
                    menu.AddItem(new GUIContent(variable), false, () => Speaker = variable);

                menu.DropDown(new Rect(pos, Vector2.zero));
            }
            var color = GUI.color;
            GUI.color = skin.GetStyle("radio").normal.textColor;
            GUI.DrawTexture(new Rect(rect.x + rect.width - 17, rect.y + 3, 16, 16), skin.GetStyle("radio").normal.background);
            GUI.color = color;
        };
    }
    /*
    public override Node PrintNodeInfo()
    {
        Debug.Log(Speaker);
        Debug.Log(DialogText);
        return this;
    }
    */
    public override Node OnExitNode(int option = 0)
    {
        var node = dialogObject.FindNextNode(this);

        if (node != null)
        {
            dialogObject.UpdateDialogue(node);
        }

        return node;
    }

    public override void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        base.DrawPorts(windowPos, nodeStyle, onPortClicked);

        InputPorts[0].heightOffset = 62;
        OutputPorts[0].heightOffset = 62;
    }
}
[Serializable] public class MultipleChoiceNode : Node, GroupableNode
{
    public List<string> options = new List<string>();

    public MultipleChoiceNode()
    {
        Rect.size = new Vector2(160, 76);
        minimumSize = Rect.size;
        InputPorts.Clear();
        options.Add("Option");
        type = NodeType.Dialogue;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.9f, 0.6f, 0.1f, 1);
            GUI.Label(rect, "Choice", skin.label);
        };

        drawNodeContent = (windowRect, skin) =>
        {
            var rect = windowRect;

            if (GUI.Button(new Rect(rect.x, rect.y + rect.height - 20, rect.width, 20), "New Branch", skin.button))
            {
                options.Add("Option");
                OutputPorts.Add(new Port(PortDirection.output, GUID));

                Rect.height += 22;
                optionsSizeModifier.y += 22;
            }

            if (options.Count > 0)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    var optionsRect = new Rect(rect.x, rect.y + rect.height - 44 - optionsSizeModifier.y + (i * 22), rect.width - 24, 20);
                    options[i] = EditorGUI.TextField(optionsRect, options[i], skin.textField);

                    var buttonRect = new Rect(rect.x + rect.width - 20, rect.y + rect.height - 44 - optionsSizeModifier.y + (i * 22), 20, 20);
                    if (GUI.Button(buttonRect, "-", skin.button))
                    {
                        if (options.Count <= 1) return;

                        options.Remove(options[i]);
                        OutputPorts.RemoveAt(i);

                        i--;
                        optionsSizeModifier.y -= 22;

                        Rect.height -= 22;
                    }
                }
            }
        };
    }
    /*
    public override Node ProcessNode()
    {
        base.ProcessNode();
        options.ForEach(option => Debug.Log(option));
        return this;
    }
    */
    public override Node OnExitNode(int option = 0)
    {
        if (options.Count > 0)
        {
            var node = dialogObject.FindNextNode(this, option);

            if (node != null)
                dialogObject.UpdateDialogue(node);

            return node;
        }

        return this;
    }
    
    public override Node PrintNodeInfo()
    {
        foreach (string option in options)
            Debug.Log(option);

        return this;
    }
    
    public override void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        base.DrawPorts(windowPos, nodeStyle, onPortClicked);

        if (dialogObject != null)
            ValueInputPorts[0].open = !dialogObject.IsPortOccupied(ValueInputPorts[0].GUID);

        for (int i = 0; i < OutputPorts.Count; i++)
        {
            OutputPorts[i].heightOffset = Rect.height - 38 - optionsSizeModifier.y + (i * 22);
        }
    }
}
[Serializable] public class UserInputNode : Node, GroupableNode
{
    public enum InputType { String, Float, Int, Bool }
    public InputType inputType;

    Vector2 initSize = new Vector2(160, 98);

    public bool isEnumVisible;
    public int selectedVariable;
    public string variableName;
    public string userInput;

    public UserInputNode()
    {
        Rect.size = initSize;
        InputPorts.Clear();
        type = NodeType.Dialogue;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.9f, 0.6f, 0.1f, 1);
            GUI.Label(rect, "Input", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            inputType = (InputType)EditorGUI.EnumPopup(new Rect(rect.x, rect.y, rect.width, 20), inputType, skin.FindStyle("enumField"));

            GUI.Label(new Rect(rect.x + 2, rect.y + 22, rect.width, 20), "Store result in variable:", skin.FindStyle("leftAlign"));

            var options = GetOptions();
            selectedVariable = EditorGUI.Popup(new Rect(rect.x, rect.y + 46, rect.width, 20), selectedVariable, options, skin.FindStyle("enumField"));

            if (selectedVariable > options.Length)
                selectedVariable = options.Length;

            if (options.Length == 1)
            {
                variableName = "";
            }
            else
            {
                Debug.Log(options.Length + " " + selectedVariable.ToString());
                variableName = options[selectedVariable];
            }
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();

        var variable = dialogObject.GraphVariables.variables.Find(var=> var == dialogObject.GetVariable(variableName));
        if (variable != null)
            variable.stringValue = userInput;

        return this;
    }

    public override Node OnExitNode(int option = 0)
    {
        ProcessNode();
        return base.OnExitNode(option);
    }

    public override void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        base.DrawPorts(windowPos, nodeStyle, onPortClicked);
        OutputPorts[0].heightOffset = 62;
    }

    private string[] GetOptions()
    {
        List<string> options = new List<string>() { "None" };

        switch (inputType)
        {
            case InputType.String:
                dialogObject.GetOptionsOfType(VariableType.String)?.ForEach(i=> options.Add(i.variableID));
                break;
            case InputType.Float:
                dialogObject.GetOptionsOfType(VariableType.Float)?.ForEach(i => options.Add(i.variableID));
                break;
            case InputType.Int:
                dialogObject.GetOptionsOfType(VariableType.Int)?.ForEach(i => options.Add(i.variableID));
                break;
            case InputType.Bool:
                dialogObject.GetOptionsOfType(VariableType.Bool)?.ForEach(i => options.Add(i.variableID));
                break;
            default:
                break;
        }

        return options.ToArray();
    }
}
[Serializable] public class FromNode : Node
{
    public int ID;

    public FromNode()
    {
        Rect.size = new Vector2(80, 52);
        OutputPorts.Clear();
        ValueOutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Flow;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(1f, 0.2f, 0.2f, 1);
            GUI.Label(rect, "From", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x + rect.width - 20, rect.y, 22, 20), "ID", skin.FindStyle("leftAlign"));
            ID = EditorGUI.IntField(new Rect(rect.x, rect.y, rect.width - 24, 20), ID, skin.textField);
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }

    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode();
        OnExitNode(option);
        return this;
    }
    public override Node OnExitNode(int option = 0)
    {
        var node = dialogObject.FindToPrompt(ID);

        if(node != null)
            node = node.OnEnterNode();
        else
            dialogObject.EndDialogue();

        return node;
    }
}
[Serializable] public class ToNode : Node
{
    public int ID;

    public ToNode()
    {
        Rect.size = new Vector2(80, 52);
        InputPorts.Clear();
        ValueOutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Flow;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(1f, 0.2f, 0.2f, 1);
            GUI.Label(rect, "To", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x + rect.width - 20, rect.y, 22, 20), "ID", skin.FindStyle("leftAlign"));
            ID = EditorGUI.IntField(new Rect(rect.x, rect.y, rect.width - 24, 20), ID, skin.textField);
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }
    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode();
        OnExitNode(option);
        return this;
    }
    public override Node OnExitNode(int option = 0)
    {
        var node = dialogObject.FindNextNode(this);

        if (node != null)
            dialogObject.UpdateDialogue(node);
        else
            dialogObject.EndDialogue();

        return node;
    }
}

[Serializable] public class PlayAnimationNode : Node, GroupableNode
{
    public DialogAnimationEvent dialogueEvent;
    public AnimationClip animationClip;
    public PlayAnimationNode()
    {
        Rect.size = new Vector2(160, 74);
        InputPorts.Clear();
        OutputPorts.Clear();
        type = NodeType.Animation;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.2f, 0.7f, 0.1f, 1f);
            GUI.Label(rect, "Play Animation", skin.label);
        };

        drawNodeContent = (rect, skin) => 
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 64, 20), "Event");
            dialogueEvent = EditorGUI.ObjectField(new Rect(rect.x + 44, rect.y, rect.width - 44, 20), dialogueEvent, typeof(DialogAnimationEvent), true) as DialogAnimationEvent;

            GUI.Label(new Rect(rect.x, rect.y + 22, rect.width - 64, 20), "Clip");
            animationClip = EditorGUI.ObjectField(new Rect(rect.x + 44, rect.y + 22, rect.width - 44, 20), animationClip, typeof(AnimationClip), true) as AnimationClip;
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();

        if (dialogueEvent != null && animationClip != null)
            dialogueEvent.Raise(animationClip);

        return this;
    }
}
[Serializable] public class PlayAudioClipNode : Node, GroupableNode
{
    public DialogAudioEvent dialogueEvent;
    public AudioClip audioClip;
    public PlayAudioClipNode()
    {
        Rect.size = new Vector2(160, 74);
        InputPorts.Clear();
        OutputPorts.Clear();
        type = NodeType.Animation;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.2f, 0.7f, 0.1f, 1f);
            GUI.Label(rect, "Play AudioClip", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 64, 20), "Event");
            dialogueEvent = EditorGUI.ObjectField(new Rect(rect.x + 44, rect.y, rect.width - 44, 20), dialogueEvent, typeof(DialogAudioEvent), true) as DialogAudioEvent;

            GUI.Label(new Rect(rect.x, rect.y + 22, rect.width - 64, 20), "Clip");
            audioClip = EditorGUI.ObjectField(new Rect(rect.x + 44, rect.y + 22, rect.width - 44, 20), audioClip, typeof(AudioClip), true) as AudioClip;
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();

        if (dialogueEvent != null && audioClip != null)
            dialogueEvent.Raise(audioClip);

        return this;
    }
}
[Serializable] public class DialogEventNode : Node, GroupableNode
{
    public DialogueEvent dialogueEvent;
    public DialogEventNode()
    {
        Rect.size = new Vector2(160, 52);
        InputPorts.Clear();
        OutputPorts.Clear();
        type = NodeType.Animation;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.2f, 0.7f, 0.1f, 1f);
            GUI.Label(rect, "Dialog Event", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 64, 20), "Event");
            dialogueEvent = EditorGUI.ObjectField(new Rect(rect.x + 44, rect.y, rect.width - 44, 20), dialogueEvent, typeof(DialogueEvent), true) as DialogueEvent;
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();

        if(dialogueEvent != null)
            dialogueEvent.Raise();

        return this;
    }
}
[Serializable] public class TriggerDialogEventNode : Node, GroupableNode
{
    public DialogueEvent dialogueEvent;
    public TriggerDialogEventNode()
    {
        Rect.size = new Vector2(160, 52);
        ValueInputPorts.Clear();
        ValueOutputPorts.Clear();
        type = NodeType.Animation;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.2f, 0.7f, 0.1f, 1f);
            GUI.Label(rect, "Trigger Dialog Event", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 64, 20), "Event");
            dialogueEvent = EditorGUI.ObjectField(new Rect(rect.x + 44, rect.y, rect.width - 44, 20), dialogueEvent, typeof(DialogueEvent), true) as DialogueEvent;
        };
    }

    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode();
        dialogueEvent?.Raise();
        OnExitNode(option);
        return this;
    }
}
[Serializable] public class GiveObjectiveNode : Node, GroupableNode
{
    public Objective objective;

    public GiveObjectiveNode()
    {
        Rect.size = new Vector2(160, 52);
        ValueInputPorts.Clear();
        ValueOutputPorts.Clear();
        type = NodeType.Animation;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.2f, 0.7f, 0.1f, 1f);
            GUI.Label(rect, "Give Objective", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 84, 20), "Objective");
            objective = EditorGUI.ObjectField(new Rect(rect.x + 64, rect.y, rect.width - 64, 20), objective, typeof(Objective), true) as Objective;
        };
    }

    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode();
        QuestManager.NewQuest(objective);
        OnExitNode(option);
        return this;
    }
}
[Serializable] public class CompleteObjectiveNode : Node, GroupableNode
{
    public Objective objective;

    public CompleteObjectiveNode()
    {
        Rect.size = new Vector2(160, 52);
        ValueInputPorts.Clear();
        ValueOutputPorts.Clear();
        type = NodeType.Animation;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.2f, 0.7f, 0.1f, 1f);
            GUI.Label(rect, "Complete Objective", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 84, 20), "Objective");
            objective = EditorGUI.ObjectField(new Rect(rect.x + 64, rect.y, rect.width - 64, 20), objective, typeof(Objective), true) as Objective;
        };
    }

    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode();
        QuestManager.CompleteQuest(objective);
        OnExitNode(option);
        return this;
    }
}
[Serializable] public class CheckObjectiveNode : Node, GroupableNode
{
    public Objective objective;

    public CheckObjectiveNode()
    {
        Rect.size = new Vector2(160, 116);
        ValueInputPorts.Clear();
        ValueOutputPorts.Clear();
        OutputPorts.Add(new Port(PortDirection.output, GUID));
        OutputPorts.Add(new Port(PortDirection.output, GUID));
        type = NodeType.Animation;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.2f, 0.7f, 0.1f, 1f);
            GUI.Label(rect, "Check Objective", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 84, 20), "Objective");
            objective = EditorGUI.ObjectField(new Rect(rect.x + 64, rect.y, rect.width - 64, 20), objective, typeof(Objective), true) as Objective;

            GUI.Label(new Rect(rect.x, rect.y + 22, rect.width, 20), "Inactive", skin.FindStyle("rightAlign"));
            GUI.Label(new Rect(rect.x, rect.y + 44, rect.width, 20), "Active", skin.FindStyle("rightAlign"));
            GUI.Label(new Rect(rect.x, rect.y + 66, rect.width, 20), "Completed", skin.FindStyle("rightAlign"));
        };
    }

    public override void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        base.DrawPorts(windowPos, nodeStyle, onPortClicked);

        OutputPorts[0].heightOffset = 60;
        OutputPorts[1].heightOffset = 82;
        OutputPorts[2].heightOffset = 104;
    }

    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode();

        int completed = 0;

        if (objective.Active)
            completed = 1;
        else if (objective.Completed)
            completed = 2;

        OnExitNode(completed);
        return this;
    }
}
[Serializable] public class SetCameraPositionNode : Node, GroupableNode
{
    public Vector3 position;
    public Vector3 lookAtTarget;

    public enum CameraSpace { Local, World }
    public CameraSpace worldSpace;

    public SetCameraPositionNode()
    {
        Rect.size = new Vector2(160, 94);
        InputPorts.Clear();
        OutputPorts.Clear();
        type = NodeType.Animation;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.2f, 0.7f, 0.1f, 1f);
            GUI.Label(rect, "Set Camera Position", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 64, 20), "Position");
            position.x = EditorGUI.FloatField(new Rect(rect.x + 52, rect.y, 32, 20), "", position.x);
            position.y = EditorGUI.FloatField(new Rect(rect.x + 86, rect.y, 32, 20), "", position.y);
            position.z = EditorGUI.FloatField(new Rect(rect.x + 120, rect.y, 32, 20), "", position.z);

            GUI.Label(new Rect(rect.x, rect.y + 22, rect.width - 64, 20), "Target");
            lookAtTarget.x = EditorGUI.FloatField(new Rect(rect.x + 52, rect.y + 22, 32, 20), "", lookAtTarget.x);
            lookAtTarget.y = EditorGUI.FloatField(new Rect(rect.x + 86, rect.y + 22, 32, 20), "", lookAtTarget.y);
            lookAtTarget.z = EditorGUI.FloatField(new Rect(rect.x + 120, rect.y + 22, 32, 20), "", lookAtTarget.z);

            GUI.Label(new Rect(rect.x, rect.y + 44, rect.width - 64, 20), "Space");
            worldSpace = (CameraSpace)EditorGUI.EnumPopup(new Rect(rect.x + 52, rect.y + 44, rect.width - 52, 20), worldSpace);
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();

        var camera = MonoBehaviour.FindObjectOfType<CameraController>();

        if (camera == null) return this;

        if (position != null && lookAtTarget != null)
        {
            if(worldSpace == CameraSpace.World)
                camera.SetDialogCameraState(lookAtTarget, position, 10f, true);
            if(worldSpace == CameraSpace.Local)
                camera.SetDialogCameraState(lookAtTarget, position, 10f, true);
        }

        return this;
    }
}

[Serializable] public class BranchNode : Node
{
    private bool result;

    public BranchNode()
    {
        Rect.size = new Vector2(80, 74);
        ValueOutputPorts.Clear();

        OutputPorts[0].heightOffset = 12;
        OutputPorts.Add(new Port(PortDirection.output, GUID) { heightOffset = 32 });

        type = NodeType.Conditional;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Branch", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            GUI.Label(new Rect(rect.width - 60, rect.y + 0, 64, 20), "True", skin.FindStyle("rightAlign"));
            GUI.Label(new Rect(rect.width - 60, rect.y + 22, 64, 20), "False", skin.FindStyle("rightAlign"));
        };
    }

    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode(option);
        ProcessNode();
        OnExitNode();
        return this;
    }
    public override Node ProcessNode()
    {
        result = false;
        var value = dialogObject.GetFirstValueNode(ValueInputPorts[0].GUID);

        if(value != null)
        {
            if(value.GetType() == typeof(CompareBoolNode))
                result = (value as CompareBoolNode).GetConditionResult();

            if (value.GetType() == typeof(CompareStringNode))
                result = (value as CompareStringNode).GetConditionResult();

            if (value.GetType() == typeof(BoolNode))
                result = (value as BoolNode).GetValue();

            if (value.GetType() == typeof(GetVariableNode))
                result = (value as GetVariableNode).GetValue().boolValue;
        }

        return this;
    }
    public override Node OnExitNode(int option = 0)
    {
        var node1 = dialogObject.FindNextNode(this, 0);
        var node2 = dialogObject.FindNextNode(this, 1);

        if (result)
        {
            if (node1 != null)
            {
                dialogObject.UpdateDialogue(node1);
                return node1;
            }
        }
        else
        {
            if (node2 != null)
            {
                dialogObject.UpdateDialogue(node2);
                return node2;
            }

            if (node1 != null)
            {
                dialogObject.UpdateDialogue(node1);
                return node1;
            }
        }

        dialogObject.EndDialogue();
        return this;
    }

    public override void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        base.DrawPorts(windowPos, nodeStyle, onPortClicked);

        InputPorts[0].heightOffset = 36;
        OutputPorts[0].heightOffset = 39;
        OutputPorts[1].heightOffset = 61;
    }
}
[Serializable] public class CompareBoolNode : Node
{
    new public static Vector2 initialSize = new Vector2(100, 52);

    public string[] conditions = new string[] { "Is", "Is Not", "And", "Or" };
    public int currentCondition;

    public CompareBoolNode()
    {
        Rect.size = initialSize;
        InputPorts.Clear();
        OutputPorts.Clear();

        ValueInputPorts.Add(new Port(PortDirection.valueOut, GUID));
        type = NodeType.Conditional;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Compare Bool", skin.label);
        };

        drawNodeContent = (rect, skin) => 
        {
            currentCondition = EditorGUI.Popup(rect, currentCondition, conditions, skin.FindStyle("enumFieldCenter"));
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }

    public bool GetConditionResult()
    {
        highlightAlpha = 1f;

        var node1 = dialogObject.GetFirstValueNode(ValueInputPorts[0].GUID);
        var node2 = dialogObject.GetFirstValueNode(ValueInputPorts[1].GUID);

        bool A = GetNodeBoolValue(node1);
        bool B = GetNodeBoolValue(node2);

        switch (currentCondition)
        {
            case 0: return A == B;
            case 1: return A != B;
            case 6: return A && B;
            case 7: return A || B;
        }


        return true;
    }

    bool GetNodeBoolValue(Node node)
    {
        if (node != null)
        {
            if(node.GetType() == typeof(GetVariableNode) && (node as GetVariableNode).GetValue().type == VariableType.Bool)
                return (node as GetVariableNode).GetValue().boolValue;

            if (node.GetType() == typeof(BoolNode))
                return (node as BoolNode).GetValue();
        }

        return false;
    }

    public override void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        base.DrawPorts(windowPos, nodeStyle, onPortClicked);

        ValueInputPorts[0].widthOffset = -20;
        ValueInputPorts[1].widthOffset = 20;
    }
}
[Serializable] public class CompareStringNode : Node
{
    new public static Vector2 initialSize = new Vector2(100, 52);

    public string[] conditions = new string[] { "Is", "Is Not", "Contains", "Not Contain", "Starts With", "Ends With" };
    public int currentCondition;

    public CompareStringNode()
    {
        Rect.size = initialSize;
        InputPorts.Clear();
        OutputPorts.Clear();

        ValueInputPorts.Add(new Port(PortDirection.valueOut, GUID));
        type = NodeType.Conditional;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Compare String", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            currentCondition = EditorGUI.Popup(rect, currentCondition, conditions, skin.FindStyle("enumFieldCenter"));
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }

    public bool GetConditionResult()
    {
        highlightAlpha = 1f;

        var node1 = dialogObject.GetFirstValueNode(ValueInputPorts[0].GUID);
        var node2 = dialogObject.GetFirstValueNode(ValueInputPorts[1].GUID);

        string A = GetNodeStringValue(node1);
        string B = GetNodeStringValue(node2);

        switch (currentCondition)
        {
            case 0: return Equals(A, B);
            case 1: return !Equals(A, B);
            case 2: return A.Contains(B);
            case 3: return !A.Contains(B);
            case 4: return A.StartsWith(B);
            case 5: return A.EndsWith(B);
        }

        return true;
    }

    string GetNodeStringValue(Node node)
    {
        if (node != null)
        {
            if (node.GetType() == typeof(GetVariableNode) && (node as GetVariableNode).GetValue().type == VariableType.String)
                return (node as GetVariableNode).GetValue().stringValue;

            if (node.GetType() == typeof(StringNode))
                return (node as StringNode).GetValue();
        }

        return "";
    }

    public override void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        base.DrawPorts(windowPos, nodeStyle, onPortClicked);

        ValueInputPorts[0].widthOffset = -20;
        ValueInputPorts[1].widthOffset = 20;
    }
}
[Serializable] public class RandomNode : Node
{
    public int branches;
    private int lastSelected;
    public RandomNode()
    {
        Rect.size = new Vector2(100, 52);
        minimumSize = Rect.size;
        ValueOutputPorts.Clear();
        ValueInputPorts.Clear();
        branches = 2;
        OutputPorts.Add(new Port(PortDirection.output, GUID));
        type = NodeType.Flow;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Random", skin.label);
        };

        drawNodeContent = (windowRect, skin) =>
        {
            var rect = windowRect;

            var oldbranchCount = branches;
            branches = EditorGUI.IntField(new Rect(rect.x, rect.y, rect.width - 24, 20), branches, skin.textField);
            branches = Mathf.Clamp(branches, 1, 99);

            if (oldbranchCount < branches)
            {
                var portsToAdd = branches - oldbranchCount;

                for (int i = 0; i < portsToAdd; i++)
                    OutputPorts.Add(new Port(PortDirection.output, GUID));
            }

            if (branches < oldbranchCount)
            {
                var portsToRemove = oldbranchCount - branches;

                OutputPorts.RemoveRange(OutputPorts.Count - portsToRemove, portsToRemove);
            }

            for (int i = 0; i < branches; i++)
            {
                GUI.Label(new Rect(rect.x + rect.width - 16, rect.y + (i * 22), 20, 20), i.ToString());
            }

            Rect.height = 30 + branches * 22;
        };
    }

    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode();

        int next = UnityEngine.Random.Range(0, branches);

        for (int i = 0; i < 10; i++)
        {
            if (next == lastSelected)
            {
                next = UnityEngine.Random.Range(0, branches);
                continue;
            }

            lastSelected = next;
            break;
        }

        OnExitNode(next);

        return this;
    }

    public override void DrawPorts(Vector2 windowPos, GUISkin nodeStyle = null, Action<string, PortDirection> onPortClicked = null)
    {
        base.DrawPorts(windowPos, nodeStyle, onPortClicked);

        InputPorts[0].heightOffset = 38;

        for (int i = 0; i < OutputPorts.Count; i++)
        {
            OutputPorts[i].heightOffset = 38 + optionsSizeModifier.y + (i * 22);
        }
    }
}

[Serializable] public class StringNode : Node, ValueNode<string>
{
    new public static Vector2 initialSize = new Vector2(120, 52);

    public string value;
    public string GetValue()
    {
        highlightAlpha = 1f;
        return value;
    }

    public StringNode()
    {
        Rect.size = initialSize;
        InputPorts.Clear();
        OutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Variable;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "String", skin.label);
        };

        drawNodeContent = (rect, skin) => 
        {
            value = EditorGUI.TextArea(rect, value, skin.textField);
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }
}
[Serializable] public class FloatNode : Node, ValueNode<float>
{
    new public static Vector2 initialSize = new Vector2(80, 52);

    public float value;
    public float GetValue()
    {
        highlightAlpha = 1f;
        return value;
    }

    public FloatNode()
    {
        Rect.size = initialSize;
        InputPorts.Clear();
        OutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Variable;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Float", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            value = EditorGUI.FloatField(rect, value, skin.textField);
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }
}
[Serializable] public class IntNode : Node, ValueNode<int>
{
    new public static Vector2 initialSize = new Vector2(80, 52);

    public int value;
    public int GetValue()
    {
        highlightAlpha = 1f;
        return value;
    }

    public IntNode()
    {
        Rect.size = initialSize;
        InputPorts.Clear();
        OutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Variable;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Int", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            value = EditorGUI.IntField(rect, value, skin.textField);
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }
}
[Serializable] public class BoolNode : Node, ValueNode<bool>
{
    new public static Vector2 initialSize = new Vector2(90, 52);

    public bool value;
    private float t;
    public bool GetValue()
    {
        highlightAlpha = 1f;
        return value;
    }

    public BoolNode()
    {
        Rect.size = initialSize;
        InputPorts.Clear();
        OutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Variable;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Bool", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            var toggleRect = new Rect(rect.x + 6, rect.y + 4, 32, 20);
            DialogEditorUtility.DrawBoolField(toggleRect, ref value, ref t, skin, 6f, 0.003f);
        };
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }
}

[Serializable] public class GetVariableNode : Node, ValueNode<DialogVariable>
{
    new public static Vector2 initialSize = new Vector2(120, 76);

    public string SelectedVariableID;
    public int variableFlag;

    public GetVariableNode()
    {
        Rect.size = initialSize;
        InputPorts.Clear();
        OutputPorts.Clear();
        ValueInputPorts.Clear();
        type = NodeType.Variable;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Get Variable", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            variableFlag = EditorGUI.Popup(new Rect(rect.x, rect.y, rect.width, 20), variableFlag, new string[] { "Local", "Object" }, skin.FindStyle("enumField"));
            GUILayout.Space(4);
            SelectedVariableID = EditorGUI.TextField(new Rect(rect.x, rect.y + 24, rect.width, 20), SelectedVariableID, skin.textField);
        };
    }

    public DialogVariable GetValue()
    {
        if (string.IsNullOrEmpty(SelectedVariableID))
            return null;

        highlightAlpha = 1f;
        DialogVariable variable = dialogObject.GraphVariables.variables.Find(x => x.variableID == SelectedVariableID);
        return variable;
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();
        return this;
    }
}
[Serializable] public class SetVariableNode : Node
{
    new public static Vector2 initialSize = new Vector2(120, 76);

    public string SelectedVariableID;
    public int variableFlag;

    public SetVariableNode()
    {
        Rect.size = initialSize;
        ValueOutputPorts.Clear();
        type = NodeType.Variable;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.5f, 0.2f, 0.9f, 1f);
            GUI.Label(rect, "Set Variable", skin.label);
        };

        drawNodeContent = (rect, skin) =>
        {
            variableFlag = EditorGUI.Popup(new Rect(rect.x, rect.y, rect.width, 20), variableFlag, new string[] { "Local", "Object" }, skin.FindStyle("enumField"));
            GUILayout.Space(4);
            SelectedVariableID = EditorGUI.TextField(new Rect(rect.x, rect.y + 24, rect.width, 20), SelectedVariableID, skin.textField);
        };
    }

    public override Node ProcessNode()
    {
        var nodes = dialogObject.GetConnectedValueNodes(this);

        var node = nodes[0];

        if(node != null && !string.IsNullOrEmpty(SelectedVariableID))
        {
            if (node.GetType() == typeof(BoolNode))
                dialogObject.GraphVariables.variables.Find(x=>x.variableID == SelectedVariableID).boolValue = (node as BoolNode).value;

            if (node.GetType() == typeof(StringNode))
                dialogObject.GraphVariables.variables.Find(x => x.variableID == SelectedVariableID).stringValue = (node as StringNode).value;

            if (node.GetType() == typeof(FloatNode))
                dialogObject.GraphVariables.variables.Find(x => x.variableID == SelectedVariableID).floatValue = (node as FloatNode).value;

            if (node.GetType() == typeof(IntNode))
                dialogObject.GraphVariables.variables.Find(x => x.variableID == SelectedVariableID).intValue = (node as IntNode).value;
        }

        return this;
    }

    public override Node OnEnterNode(int option = 0)
    {
        base.OnEnterNode(option);
        ProcessNode();
        OnExitNode(option);
        return this;
    }

    public override Node OnExitNode(int option = 0)
    {
        base.OnExitNode(option);

        return this;
    }
}

[Serializable] public class ContainerNode : Node, GroupableNode
{
    public static new Vector2 initialSize = new Vector2(200, 120);

    private bool editContainer;

    [SerializeReference] private List<Node> nodeList = new List<Node>();
    private UnityEditorInternal.ReorderableList list;

    private GUISkin skin;

    public ContainerNode()
    {
        Rect.size = initialSize;
        InputPorts.Clear();
        OutputPorts.Clear();
        resizable = true;

        drawNodeTitle = (rect, skin) =>
        {
            titleColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            GUI.Label(rect, "Container", skin.label);

            var buttonRect = new Rect(rect.x + rect.width - 22, rect.y + 2, 20, 20);
            GUI.DrawTexture(buttonRect, EditorGUIUtility.IconContent("d__Popup@2x").image);

            var e = Event.current;
            if (buttonRect.Contains(e.mousePosition) && e.button == 0 && e.type == EventType.MouseDown)
            {
                e.Use();
                editContainer = !editContainer;
            }
        };
        drawNodeContent = (rect, skin) =>
        {
            this.skin = skin;

            TryInitializeList();

            var color = GUI.color;
            GUI.color = skin.FindStyle("gridBG").normal.textColor;
            GUI.Box(new Rect(rect.x, rect.y, rect.width, 14), GUIContent.none, skin.FindStyle("nodeTitle"));
            GUI.Box(new Rect(rect.x, rect.y + 14, rect.width, rect.height - 42), GUIContent.none, skin.FindStyle("nodeBackground"));
            GUI.color = color;

            list.DoList(new Rect(rect.x, rect.y+2, rect.width, list.GetHeight()));

            Rect.height = list.GetHeight() + 38;

            if (GUI.Button(new Rect(rect.x, rect.y + rect.height-24, rect.width, 24), "Add Node", skin.button))
            {
                var pos = Event.current.mousePosition;
                List<Node> nodes = DialogEditorUtility.GetNodesWithInterface<GroupableNode>();
                GenericMenu menu = new GenericMenu();

                foreach (var node in nodes)
                {
                    menu.AddItem(new GUIContent(Enum.GetName(typeof(NodeType), node.type) + "/" + node.GetType().Name.Replace("Node", "") + " Node"), false, () =>
                    {
                        node.dialogObject = dialogObject;
                        nodeList.Add(node);
                    });
                }

                menu.DropDown(new Rect(pos, Vector2.zero));
            }
        };
        onDeselectNode = (node) =>
        {
            GUI.FocusWindow(-1);

            TryInitializeList();

            for (int i = 0; i < list.count; i++)
                list.Deselect(i);

            editContainer = false;
        };
    }

    void TryInitializeList()
    {
        if(list == null)
        {
            list = new UnityEditorInternal.ReorderableList(nodeList, typeof(Node));
            list.headerHeight = 0;
            list.showDefaultBackground = false;
            list.drawFooterCallback = (rect) => { };
            list.drawElementBackgroundCallback = (rect, index, isActive, isFocused) => {  };
            list.drawElementCallback = (rect, index, isActive, isFocused)=>
            {
                if (index > (nodeList.Count - 1))
                    return;

                var color = GUI.color;
                GUI.color = nodeList[index].titleColor;
                var titleRect = new Rect(rect.x - 16, rect.y, rect.width + 18, 24);
                GUI.Box(titleRect, GUIContent.none, skin.FindStyle("nodeTitle"));

                if (nodeList[index].expanded)
                {
                    GUI.color = skin.FindStyle("nodeBackground").active.textColor;
                    var bgRect = new Rect(rect.x - 16, rect.y + 24, rect.width + 18, nodeList[index].Rect.height-24);
                    GUI.Box(bgRect, GUIContent.none, skin.FindStyle("nodeBackground"));
                }
                GUI.color = color;

                if (editContainer)
                {
                    var buttonRect = new Rect(titleRect.x + titleRect.width - 20, titleRect.y + 4, 16, 16);
                    GUI.DrawTexture(buttonRect, EditorGUIUtility.IconContent("CrossIcon").image);

                    var e = Event.current;
                    if (buttonRect.Contains(e.mousePosition) && e.button == 0 && e.type == EventType.MouseDown)
                    {
                        list.list.RemoveAt(list.index);
                        index--;
                        return;
                    }
                }

                nodeList[index].drawNodeTitle.Invoke(titleRect, skin);
                
                if(nodeList[index].expanded)
                    nodeList[index].drawNodeContent.Invoke(new Rect(rect.x - 12, rect.y + 28, rect.width + 10, nodeList[index].Rect.height - 32), skin);
            };
            list.elementHeightCallback = (i) => 
            {
                if (list.count > 0)
                {
                    if (nodeList[i].expanded)
                        return nodeList[i].Rect.height + 2;
                    else
                        return 26;
                }
                else
                    return 26;
            };
        }
    }

    public override Node ProcessNode()
    {
        base.ProcessNode();

        foreach (var node in nodeList)
            node.ProcessNode();

        return this;
    }
}