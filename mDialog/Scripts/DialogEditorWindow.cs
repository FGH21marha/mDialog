using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class DialogEditorWindow : EditorWindow
{
    [MenuItem("Dialogue/Open Dialogue Editor")]  public static void OpenWindow()
    {
        DialogEditorWindow window = GetWindow<DialogEditorWindow>();
        var texture = Resources.Load("DialogueIconSmall") as Texture;
        window.titleContent = new GUIContent("Dialogue Editor", texture);
        window.RefreshWindow();
    }
    [MenuItem("Dialogue/Create New Dialogue")] public static void CreateAndEditDialogue()
    {
        DialogObject newDialogue = CreateInstance<DialogObject>();

        AssetDatabase.CreateAsset(newDialogue, "Assets/" + GUID.Generate().ToString() + ".asset");
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        Selection.SetActiveObjectWithContext(newDialogue, null);

        OpenAndEditDialogue(newDialogue);
    }
    public static void OpenAndEditDialogue(DialogObject target)
    {
        DialogEditorWindow window = GetWindow<DialogEditorWindow>();
        var texture = Resources.Load("DialogueIconSmall") as Texture;
        window.titleContent = new GUIContent("Dialogue Editor", texture);
        window.targetObject = target;
        window.RefreshWindow();
    }

    public static DialogueEditorSettings settings;
    public DialogObject targetObject;
    SerializedObject sTargetObject;

    public GraphMode currentMode;
    public GraphCreateMode createMode = new GraphCreateMode();
    public GraphConnectNodesMode connectNodesMode = new GraphConnectNodesMode();
    public GraphResizeNodesMode resizeNodesMode = new GraphResizeNodesMode();

    public EditorKey inputs = new EditorKey();

    public float PanelsSize = 300f;
    private Rect graphRectInternal
    {
        get
        {
            float x = PanelsSize;
            float y = 20;
            float width = position.width - PanelsSize;
            float height = position.height - 20;

            return new Rect(x, y, width, height);
        }
    }
    public Rect graphRect
    { 
        get 
        {
            var rect = graphRectInternal;

            if (settings != null)
            {
                float panelsize = settings.previewPanelOpen == true ? PanelsSize : 0;

                return new Rect(rect.x, rect.y, rect.width - panelsize, rect.height);
            }
            else
            {
                return new Rect(rect.x, rect.y, rect.width, rect.height);
            }
        } 
    }
    public Rect settingsPanelRect => new Rect(0, 20, PanelsSize, position.height - 20);
    public Rect previewPanelRect => new Rect(position.width - PanelsSize, 20, PanelsSize, position.height - 20);

    public List<Node> nodes = DialogEditorUtility.GetNodeInstances();
    public GUISkin nodeStyle;

    private float time;
    public float editorDeltaTime;
    public int lastSelectedWindow;
    public static bool previewPanelOpen = true;

    public bool MouseInGraph
    { 
        get 
        { 
            return graphRect.Contains(Event.current.mousePosition); 
        } 
    }
    public bool IsMouseOverNode
    {
        get
        {
            bool selection = false;

            foreach (var node in targetObject.Nodes)
            {
                if (node.Rect.Contains(Event.current.mousePosition))
                {
                    selection = true;
                }
            }

            return selection;
        }
    }
    public bool IsMouseOverPort
    {
        get
        {
            bool selection = false;

            foreach (var node in targetObject.Nodes)
            {
                foreach (var port in node.InputPorts)
                {
                    if (port.screenRect.Contains(Event.current.mousePosition))
                        selection = true;
                }
                foreach (var port in node.OutputPorts)
                {
                    if (port.screenRect.Contains(Event.current.mousePosition))
                        selection = true;
                }
                foreach (var port in node.ValueOutputPorts)
                {
                    if (port.screenRect.Contains(Event.current.mousePosition))
                        selection = true;
                }
                foreach (var port in node.ValueInputPorts)
                {
                    if (port.screenRect.Contains(Event.current.mousePosition))
                        selection = true;
                }
            }

            return selection;
        }
    }
    public bool CanEditNodes
    { 
        get 
        { 
            return currentMode.GetType() == typeof(GraphCreateMode); 
        } 
    }

    private void OnEnable()
    {
        RefreshWindow();
        EnableNodes();
    }
    private void OnFocus()
    {
        if (targetObject != null)
            SaveGraph();
    }
    private void OnDestroy()
    {
        SaveChanges();

        if(sTargetObject != null)
            sTargetObject.ApplyModifiedProperties();

        sTargetObject = null;
        createMode.selected.Clear();

        if(targetObject != null)
            targetObject.EndDialogue();

        DisableNodes();

        targetObject = null;
    }
    private void OnGUI()
    {
        float currentTime = time;

        if(targetObject != null && focusedWindow)
            Repaint();

        inputs.Update();

        GetReferences();
        DrawGraph();

        DrawConnections();
        DrawSelection();
        DrawNodes();

        DrawGraphOverlay();

        currentMode.UpdateMode(this);

        DrawGraphSettingsPanel();
        DrawPreviewWindowPanel();
        DrawTopPanel();

        time = (float)EditorApplication.timeSinceStartup;
        editorDeltaTime = time - currentTime;
    }

    public void EnableNodes()
    {
        if (targetObject == null || targetObject.Nodes.Count < 1) return;

        foreach (var node in targetObject.Nodes)
            node.OnEnable();
    }
    public void DisableNodes()
    {
        if (targetObject == null || targetObject.Nodes.Count < 1) return;

        foreach (var node in targetObject.Nodes)
            node.OnDisable();
    }
    public void DestroyNodes()
    {
        if (targetObject == null || targetObject.Nodes.Count < 1) return;

        foreach (var node in targetObject.Nodes)
            node.OnDestroy();
    }

    public void RefreshWindow()
    {
        GetReferences();
        SetGraphMode(createMode);

        if (targetObject == null && settings != null)
        {
            if (settings.lastAssetPath != "")
            {
                targetObject = AssetDatabase.LoadAssetAtPath(settings.lastAssetPath, typeof(DialogObject)) as DialogObject;
                Debug.Log("Loaded graph from saved settings");
            }
        }
    }
    public void SetGraphMode(GraphMode newMode)
    {
        if(currentMode != null)
            currentMode.ExitMode(this);

        if(newMode != null)
            currentMode = newMode;

        currentMode.EnterMode(this);
    }
    public void CreateAndSelectNode(Node newNode, Vector2 position)
    {
        Undo.RecordObject(targetObject, "Created " + newNode.GetType().Name);

        newNode.dialogObject = targetObject;
        newNode.SetPosition(position);
        newNode.OnCreateNode();
        newNode.OnEnable();
        targetObject.AddNewNode(newNode);
        nodes = DialogEditorUtility.GetNodeInstances();

        foreach (var node in targetObject.Nodes)
        {
            if (node.Rect.Contains(position))
            {
                if (!createMode.selected.Contains(node.GUID))
                {
                    if (!createMode.IsLastSelected(node.GUID)) createMode.selected.Clear();

                    createMode.selected.Add(node.GUID);
                    createMode.lastSelectedGUID = node.GUID;
                    createMode.lastSelectedPosition = node.Rect.position;
                }
            }
        }

        Repaint();
    }
    public void DuplicateSelection()
    {
        if (createMode.selected.Count == 0) return;

        Undo.RecordObject(targetObject, "Duplicated Selection");

        List<Node> duplicatedNodes = new List<Node>();

        foreach (var nodeToClone in createMode.selected)
            duplicatedNodes.Add(targetObject.GetNodeByGUID(nodeToClone).Duplicate());

        foreach (var nodesToCreate in duplicatedNodes)
            CreateAndSelectNode(nodesToCreate, nodesToCreate.Rect.position + Vector2.one * 300);
    }
    private void GetReferences()
    {
        if(targetObject == null)
            sTargetObject = null;

        if (settings == null)
        {
            var i = Resources.FindObjectsOfTypeAll(typeof(DialogueEditorSettings));

            if (i.Length != 0)
            {
                settings = i[0] as DialogueEditorSettings;
            }
            else
            {
                var settings = CreateInstance<DialogueEditorSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/DialogScript/Resources/DialogueEditorSettings.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                DialogEditorWindow.settings = settings;
            }
        }

        if (settings != null && targetObject != null)
            settings.lastAssetPath = AssetDatabase.GetAssetPath(targetObject);

        if (nodeStyle == null)
            nodeStyle = Resources.Load("DialogueEditorSkin") as GUISkin;

        if (createMode == null)
            createMode = new GraphCreateMode();

        if (connectNodesMode == null)
            connectNodesMode = new GraphConnectNodesMode();

        if (nodes == null || nodes.Count == 0)
            nodes = DialogEditorUtility.GetNodeInstances();
    }

    private void DrawTopPanel()
    {
        var panelRect = new Rect(0,0,position.width, 20);
        GUI.Box(panelRect, GUIContent.none, EditorStyles.toolbar);

        DialogEditorUtility.DrawToolbarButton(new Rect(0, 0, 120, 20), targetObject == null, "Save Graph", SaveGraph);

        DialogEditorUtility.DrawToolbarButton(new Rect(120, 0, 120, 20), false, "Load Graph", () =>
        {
            string path = EditorUtility.OpenFilePanel("Load Graph", Application.dataPath, "asset");
            var obj = AssetDatabase.LoadAssetAtPath<DialogObject>(path.Replace(Application.dataPath, "Assets"));

            if (obj == null)
                return;

            if(obj.GetType() == typeof(DialogObject))
                targetObject = obj;
        });

        DialogEditorUtility.DrawToolbarButton(new Rect(240, 0, 120, 20), false, "Refresh Nodes", RefreshNodes);

        if (targetObject == null) return;

        DialogEditorUtility.DrawToolbarButton(new Rect(580, 0, 120, 20), false, "Recenter Graph", () =>
        {
            foreach (var node in targetObject.Nodes)
            {
                node.Rect.position -= targetObject.windowPos;
            }

            targetObject.windowPos = Vector2.zero;
        });

        DialogEditorUtility.DrawToolbarButton(new Rect(panelRect.width - 360, 0, 120, 20), false, "Preview Dialogue", () =>
        {
            if(settings != null)
            {
                settings.previewPanelOpen = !settings.previewPanelOpen;
            }

            targetObject.EndDialogue();
        });

        DialogEditorUtility.DrawToolbarButton(new Rect(panelRect.width - 240, 0, 120, 20), false, "Clear Graph", () => 
        {
            if (EditorUtility.DisplayDialog("Clear Graph", "You are about to clear this graph.", "Continue", "Cancel"))
            {
                DisableNodes();
                DestroyNodes();
                createMode.selected.Clear();
                targetObject.Nodes.Clear();
            }
        });

        DialogEditorUtility.DrawToolbarButton(new Rect(panelRect.width - 120, 0, 120, 20), false, "Close Graph", () =>
        {
            SaveGraph();
            targetObject = null;
            createMode.targetObject = null;
            connectNodesMode.targetObject = null;
            Repaint();
        });

        var sliderRect = new Rect(370, 4, 190, 20);
        DialogEditorUtility.connectionSmoothness = GUI.Slider(sliderRect, DialogEditorUtility.connectionSmoothness, 0, 0, 1, nodeStyle.FindStyle("horizontalSlider"), nodeStyle.FindStyle("horizontalSliderThumb"), true, 9999);
    }
    private void DrawGraphSettingsPanel()
    {
        var width = GUILayout.Width(settingsPanelRect.width - 8);
        float height = 0;

        if(targetObject != null && nodeStyle != null)
            height = nodeStyle.textArea.CalcHeight(new GUIContent(targetObject.graphDescription), settingsPanelRect.width - 8)-4;

        EditorGUI.DrawRect(settingsPanelRect, new Color(0.21f, 0.21f, 0.21f, 1));

        var TitleRect = new Rect(2, 34, settingsPanelRect.width - 4, 32);
        GUI.Label(TitleRect, "Graph Settings", nodeStyle.FindStyle("centerAlignBoldLabel"));


        using (new GUI.GroupScope(new Rect(settingsPanelRect.x, settingsPanelRect.y+48, settingsPanelRect.width, settingsPanelRect.height+2)))
        {
            if(targetObject != null)
            {
                var titleRect = new Rect(4,8, settingsPanelRect.width - 8, 26);
                var descriptionRect = new Rect(4, 38, settingsPanelRect.width - 8, height);
                targetObject.graphTitle = EditorGUI.TextField(titleRect, targetObject.graphTitle, nodeStyle.FindStyle("graphTitle"));
                targetObject.graphDescription = EditorGUI.TextArea(descriptionRect, targetObject.graphDescription, nodeStyle.FindStyle("graphDescription"));

                GUILayout.Space(40 + height);
            }

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (targetObject == null) return;

                if (sTargetObject == null)
                    sTargetObject = new SerializedObject(targetObject);

                sTargetObject.Update();
                EditorGUILayout.PropertyField(sTargetObject.FindProperty("GraphVariables"), width);
                EditorGUILayout.PropertyField(sTargetObject.FindProperty("GraphSpeakers"), width);

                if (check.changed)
                {
                    Undo.RecordObject(targetObject, "Changed Graph Variables");
                    sTargetObject.ApplyModifiedProperties();
                }
            }
        }
    }
    private void DrawPreviewWindowPanel()
    {
        if (settings == null || !settings.previewPanelOpen) return;

        EditorGUI.DrawRect(previewPanelRect, new Color(0.21f, 0.21f, 0.21f, 1));

        if (targetObject == null) return;

        using (new GUI.GroupScope(new Rect(previewPanelRect.x, previewPanelRect.y - 2, previewPanelRect.width, previewPanelRect.height + 2)))
        {
            var StartButtonRect = new Rect(2, 130, previewPanelRect.width - 4, 24);
            if (!targetObject.DialogueActive)
            {
                for (int i = 0; i < targetObject.StartNodeCount; i++)
                {
                    var startRect = new Rect(StartButtonRect.position + Vector2.up * (i * 26), StartButtonRect.size);

                    if (GUI.Button(startRect, "Start from " + i.ToString(), nodeStyle.button))
                    {
                        targetObject.StartDialogue(i);
                    }
                }
            }

            var labelRect = new Rect(4, 14, previewPanelRect.width - 8, 32);
            var dialogueRect = new Rect(4, 54, previewPanelRect.width - 8, 72);
            GUI.Box(dialogueRect, GUIContent.none, nodeStyle.textArea);

            if (targetObject.current != null)
            {
                var node = targetObject.current;

                if (node.GetType() == typeof(DialogNode))
                {
                    var dialogueNode = node as DialogNode;
                    GUI.Label(labelRect, dialogueNode.Speaker, nodeStyle.FindStyle("leftAlignBold"));

                    string input = dialogueNode.DialogText;
                    
                    string output = Regex.Replace(input, "<CharName>", "<color=cyan>" + targetObject.GraphVariables.variables.Find(x=>x.variableID == "CharName")?.stringValue + "</color>");

                    for (int i = 0; i < targetObject.GraphSpeakers.speakers.Count; i++)
                    {
                        output = Regex.Replace(output, "<Speaker" + i.ToString() + ">", "<color=cyan>" + targetObject.GraphSpeakers.speakers[i] + "</color>");
                    }

                    GUI.Label(dialogueRect, output, nodeStyle.FindStyle("previewText"));
                }

                if (node.OutputPorts.Count == 1)
                {

                    if(node.GetType() == typeof(UserInputNode))
                    {
                        var InputRect = new Rect(2, 130, previewPanelRect.width - 4, 24);
                        var inputNode = node as UserInputNode;

                        inputNode.userInput = EditorGUI.TextField(InputRect, inputNode.userInput, nodeStyle.textField);

                        var NextButtonRect = new Rect(2, 158, previewPanelRect.width - 4, 24);
                        if (GUI.Button(NextButtonRect, "Next", nodeStyle.button))
                        {
                            targetObject.current?.OnExitNode();
                            Repaint();
                        }
                    }
                    else
                    {
                        var NextButtonRect = new Rect(2, 130, previewPanelRect.width - 4, 24);
                        if (GUI.Button(NextButtonRect, "Next", nodeStyle.button))
                        {
                            targetObject.current?.OnExitNode();
                            Repaint();
                        }
                    }
                }
                else if (node.GetType() == typeof(MultipleChoiceNode))
                {
                    var choice = (MultipleChoiceNode)node;
                    int heightOffset = 0;
                    for (int i = 0; i < choice.options.Count; i++)
                    {
                        var NextButtonRect = new Rect(2, 130 + heightOffset, previewPanelRect.width - 4, 24);
                        if (GUI.Button(NextButtonRect, choice.options[i], nodeStyle.button))
                        {
                            targetObject.current?.OnExitNode(i);
                            Repaint();
                        }

                        heightOffset += 26;
                    }
                }
            }
            else
            {
                GUI.Label(labelRect, "Preview Dialogue", nodeStyle.FindStyle("leftAlignBold"));
            }
        }
    }
    private void DrawGraph()
    {
        if (nodeStyle == null) return;

        var color = GUI.color;

        GUI.color = nodeStyle.GetStyle("gridBG").normal.textColor;
        GUI.DrawTexture(graphRect, Texture2D.whiteTexture);
        GUI.color = color;

        Rect grid = new Rect(150, 10, graphRect.width, graphRect.height);

        Color minor = nodeStyle.GetStyle("grid").normal.textColor;
        Color Major = nodeStyle.GetStyle("grid").hover.textColor;

        if (targetObject != null)
            DrawGrid(grid, targetObject.windowPos, 10, Major, minor);
        else
            DrawGrid(grid, Vector2.zero, 10, Major, minor);
    }
    private void DrawGraphOverlay()
    {
        var color = GUI.color;
        GUI.color = nodeStyle.FindStyle("windowOverlay").normal.textColor;
        GUI.Box(graphRect, GUIContent.none, nodeStyle.FindStyle("windowOverlay"));
        GUI.color = color;
    }

    public void DrawGrid(Rect windowRect, Vector2 offset, float gridSize, Color Major, Color minor)
    {
        if(Event.current.type == EventType.Repaint)
        {
            Material material = new Material(Shader.Find("Hidden/Internal-Colored"));
            GUI.BeginClip(windowRect);
            GL.PushMatrix();
            GL.Clear(true, false, Color.black);
            material.SetPass(0);

            DrawGridLines(windowRect, offset + Vector2.one * 10000, gridSize, minor);
            DrawGridLines(windowRect, offset + Vector2.one * 10000, gridSize * 10, Major);

            GL.PopMatrix();
            GUI.EndClip();
        }
    }
    private void DrawGridLines(Rect windowRect, Vector2 offset, float spacing, Color gridColor)
    {
        float minX = windowRect.xMin + offset.x % spacing;
        float minY = windowRect.yMin + offset.y % spacing;

        for (float x = minX; x < windowRect.xMax; x += spacing)
        {
            GL.Begin(GL.LINES);
            GL.Color(gridColor);
            GL.Vertex(new Vector2(x, windowRect.yMin));
            GL.Vertex(new Vector2(x, windowRect.yMax));
            GL.End();
        }

        for (float y = minY; y < windowRect.yMax; y += spacing)
        {
            GL.Begin(GL.LINES);
            GL.Color(gridColor);
            GL.Vertex(new Vector2(windowRect.xMin, y));
            GL.Vertex(new Vector2(windowRect.xMax, y));
            GL.End();
        }
    }

    private void DrawNodes()
    {
        if (targetObject == null || targetObject.Nodes.Count <= 0) return;

        var c = GUI.color;
        GUI.color = nodeStyle.FindStyle("nodeShadow").normal.textColor;
        foreach (var node in targetObject.Nodes)
            GUI.Box(new Rect(node.Rect.position - Vector2.one * 8, node.Rect.size + Vector2.one * 16), GUIContent.none, nodeStyle.FindStyle("nodeShadow"));
        GUI.color = c;

        foreach (var node in targetObject.Nodes)
            node.DrawPorts(targetObject.windowPos, nodeStyle, TryConnectNodes);

        BeginWindows();

        foreach (var node in targetObject.Nodes)
        {
            node.Rect = GUI.Window(node.nodeID, node.Rect, (ID) =>
            {
                node.DrawTitle(node.TitleRect, nodeStyle, editorDeltaTime);
                node.DrawWindow(node.ContentRect, nodeStyle, true, editorDeltaTime);
            },
            "",
            nodeStyle.window);

            if (node.DiagonalResizeRect.Contains(Event.current.mousePosition) && node.resizable)
                EditorGUIUtility.AddCursorRect(node.DiagonalResizeRect, MouseCursor.ResizeUpLeft);

            if (node.HorizontalResizeRect.Contains(Event.current.mousePosition) && node.resizable)
                EditorGUIUtility.AddCursorRect(node.HorizontalResizeRect, MouseCursor.ResizeHorizontal);

            if (node.VerticalResizeRect.Contains(Event.current.mousePosition) && node.resizable)
                EditorGUIUtility.AddCursorRect(node.VerticalResizeRect, MouseCursor.ResizeVertical);
        }

        EndWindows();

    }
    private void DrawConnections()
    {
        if (targetObject == null || targetObject.connections.Count <= 0) return;

        foreach (var connection in targetObject.connections)
        {
            connection.DrawConnection(targetObject, out bool deleted, nodeStyle);

            if (deleted)
                break;
        }
    }
    public void DrawSelection()
    {
        if (!createMode.hasTarget || targetObject.Nodes.Count <= 0) return;

        foreach (var selected in createMode.selected)
        {
            if(targetObject.GetNodeByGUID(selected) != null)
            {
                var Node = targetObject.GetNodeByGUID(selected);
                var Pos = Node.Rect.position - Vector2.one * 2;
                var size = Node.Rect.size + Vector2.one * 4;

                GUI.Box(new Rect(Pos, size), GUIContent.none, nodeStyle.GetStyle("Outline"));
            }
        }
    }

    private void TryConnectNodes(string PortGUID, PortDirection direction)
    {
        Event e = Event.current;

        if(e.type == EventType.MouseDown || e.type == EventType.MouseUp)
        {
            connectNodesMode.SetStartPort(PortGUID, direction);
            SetGraphMode(connectNodesMode);
        }

        if (e.button == 1)
        {
            Undo.RecordObject(targetObject, "Removed Connection");
            targetObject.RemoveConnection(PortGUID);
        }
    }
    public void RefreshNodes()
    {
        nodes = DialogEditorUtility.GetNodeInstances();
    }
    private void SaveGraph()
    {
        RefreshWindow();
        nodes = DialogEditorUtility.GetNodeInstances();
        nodeStyle = Resources.Load("DialogueEditorSkin") as GUISkin;
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(targetObject);
        EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect();
    }
}

public abstract class GraphMode
{
    public DialogEditorWindow window;
    public GUISkin nodeStyle;

    public DialogObject targetObject;

    public Vector2 leftClickPosition;
    public Vector2 rightClickPosition;
    public Vector2 scrollWheelClickPosition;

    public bool hasTarget => targetObject != null;

    public virtual void EnterMode(DialogEditorWindow window)
    {
        this.window = window;
        targetObject = window.targetObject;
        nodeStyle = window.nodeStyle;
    }
    public virtual void UpdateMode(DialogEditorWindow window) {  }
    public virtual void ExitMode(DialogEditorWindow window) {  }
}
public class GraphCreateMode: GraphMode
{
    public bool hasSelection => selected.Count > 0;
    public List<string> selected = new List<string>();
    public string lastSelectedGUID;
    public Vector2 lastSelectedPosition;

    public bool selecting;
    public Rect selectionRect;

    public List<Vector2> selectedStartPos = new List<Vector2>();
    public List<Vector2> selectedCurrentPos = new List<Vector2>();
    public override void EnterMode(DialogEditorWindow window)
    {
        base.EnterMode(window);
        selected.Clear();
        lastSelectedGUID = "";
        lastSelectedPosition = Vector2.zero;
        selectionRect = new Rect();
    }
    public override void UpdateMode(DialogEditorWindow window)
    {
        base.UpdateMode(window);

        HandleInput();
        TrySelectNodes();
        MoveNode();
    }
    public override void ExitMode(DialogEditorWindow window) 
    {
        selected.Clear();
        lastSelectedGUID = "";
        lastSelectedPosition = Vector2.zero;
        selectionRect = new Rect();
    }

    private void HandleInput()
    {
        if (!hasTarget) return;

        EditorGUI.BeginChangeCheck();

        Event e = Event.current;

        if (e.button == 0 && e.type == EventType.MouseDown)
        {
            if (!hasSelection)
                selected.Clear();

            Node node = DialogEditorUtility.CreateNodeWithKey(window.inputs, targetObject);

            if (node != null)
                window.CreateAndSelectNode(node, e.mousePosition);
        }

        if (e.button == 1 && e.type == EventType.MouseDown && !window.IsMouseOverNode && window.MouseInGraph)
        {
            var pos = e.mousePosition;
            GenericMenu menu = new GenericMenu();

            foreach (var node in window.nodes)
            {
                menu.AddItem(new GUIContent("Create/" + Enum.GetName(typeof(NodeType), node.type) + "/" + node.GetType().Name.Replace("Node", "") + " Node"), false, () => 
                {
                    window.CreateAndSelectNode(node, pos);
                    window.SetGraphMode(window.createMode);
                });
            }

            menu.DropDown(new Rect(pos, Vector2.zero));

            e.Use();
        }

        if (e.button == 2 && e.type == EventType.MouseDrag)
        {
            MoveCanvas(e);
            window.Repaint();
        }

        if (e.keyCode == KeyCode.Delete)
        {
            RemoveSelection();
            window.Repaint();
        }

        if (GUI.changed || EditorGUI.EndChangeCheck())
            window.Repaint();
    }
    private void TrySelectNodes()
    {
        if (!hasTarget || !window.MouseInGraph) return;

        Event e = Event.current;

        if(!hasSelection)
            GUI.Box(selectionRect, GUIContent.none, nodeStyle.GetStyle("Selection"));

        if (e.button == 0 && e.type == EventType.MouseDown && window.MouseInGraph)
        {
            leftClickPosition = e.mousePosition;
            selectionRect = new Rect(leftClickPosition, e.mousePosition - leftClickPosition);

            foreach (var item in targetObject.Nodes)
            {
                if (!item.resizable) continue;

                if (item.DiagonalResizeRect.Contains(leftClickPosition))
                {
                    window.resizeNodesMode.resizeDirection = GraphResizeNodesMode.ResizeDirection.Diagonal;
                    window.resizeNodesMode.nodeGUID = item.GUID;
                    window.SetGraphMode(window.resizeNodesMode);
                    return;
                }

                if (item.HorizontalResizeRect.Contains(leftClickPosition))
                {
                    window.resizeNodesMode.resizeDirection = GraphResizeNodesMode.ResizeDirection.Horizontal;
                    window.resizeNodesMode.nodeGUID = item.GUID;
                    window.SetGraphMode(window.resizeNodesMode);
                    return;
                }

                if (item.VerticalResizeRect.Contains(leftClickPosition))
                {
                    window.resizeNodesMode.resizeDirection = GraphResizeNodesMode.ResizeDirection.Vertical;
                    window.resizeNodesMode.nodeGUID = item.GUID;
                    window.SetGraphMode(window.resizeNodesMode);
                    return;
                }
            }

            foreach (var item in targetObject.Nodes)
            {
                if (item.Rect.Contains(e.mousePosition))
                {
                    if(selected.Count <= 1)
                    {
                        selected.Clear();

                        foreach (var node in window.nodes)
                            node.onDeselectNode?.Invoke(node);

                        selected.Add(item.GUID);
                    }
                }
            }

            window.Repaint();

            if (!window.IsMouseOverNode)
            {
                selected.Clear();

                foreach (var node in window.nodes)
                    node.onDeselectNode?.Invoke(node);

                lastSelectedGUID = null;
                GUI.FocusControl(null);
            }
        }

        if (e.type == EventType.MouseDrag && e.button == 0 && window.MouseInGraph)
        {
            e.Use();
            selectionRect = new Rect(leftClickPosition, e.mousePosition - leftClickPosition);
            window.Repaint();
        }

        if (e.button == 0 && e.type == EventType.MouseUp && !DraggingSelection() && !hasSelection)
        {
            if (!window.IsMouseOverNode)
            {
                selected.Clear();
                foreach (var node in window.nodes)
                    node.onDeselectNode?.Invoke(node);
            }

            var selectedNode = 0;
            foreach (var node in targetObject.Nodes)
            {
                if (selectionRect.size.magnitude > 10)
                {
                    if (selectionRect.Overlaps(new Rect(node.Rect.position, node.Rect.size), true))
                    {
                        if (!selected.Contains(node.GUID))
                        {
                            selected.Add(node.GUID);
                            lastSelectedPosition = node.Rect.position;
                            lastSelectedGUID = node.GUID;
                            selectedNode++;
                        }
                        else
                            selectedNode++;
                    }
                }
            }
            if (selectedNode == 0 || selected.Count == 0)
            {
                selected.Clear();

                foreach (var node in window.nodes)
                    node.onDeselectNode?.Invoke(node);

                lastSelectedGUID = null;
                GUI.FocusControl(null);
            }

            selectionRect = new Rect(0,0,0,0);
            leftClickPosition = e.mousePosition;
            window.Repaint();
        }
    }

    public void RemoveSelection()
    {
        Undo.RecordObject(targetObject, "Deleted Nodes");

        for (int i = 0; i < selected.Count; i++)
        {
            var node = targetObject.GetNodeByGUID(selected[i]);
            node.OnDestroy();
            targetObject.Nodes.Remove(node);
            selected.Remove(selected[i]);
            i--;
        }

        foreach (var node in window.nodes)
            node.onDeselectNode?.Invoke(node);
            
        selected.Clear();
        selecting = false;
        selectionRect = new Rect();
        window.SaveChanges();
    }
    public void MoveNode()
    {
        if (Event.current.button != 0 || targetObject == null) return;

        Undo.RecordObject(targetObject, "MoveSelection");

        for (int i = 0; i < selected.Count; i++)
        {
            for (int j = 0; j < targetObject.Nodes.Count; j++)
            {
                if (targetObject.Nodes[j].GUID == selected[i])
                    targetObject.Nodes[j].DragWindow(Event.current.delta / 2);
            }
        }
    }
    public void MoveCanvas(Event e)
    {
        targetObject.windowPos += e.delta;

        foreach (var node in targetObject.Nodes)
            node.Rect.position += e.delta;
    }

    public bool IsLastSelected(string GUID)
    {
        return GUID == lastSelectedGUID;
    }
    public bool DraggingSelection()
    {
        if (targetObject != null && targetObject.GetNodeByGUID(lastSelectedGUID) != null && !string.IsNullOrEmpty(lastSelectedGUID))
            return targetObject.GetNodeByGUID(lastSelectedGUID).Rect.position != lastSelectedPosition;
        else
            return false;
    }
}
public class GraphConnectNodesMode: GraphMode
{
    bool isConnecting;
    string startGUID;
    string hoverGUID;
    PortDirection startDirection;
    PortDirection hoverDirection;
    bool CanConnect
    {
        get
        {
            return !string.IsNullOrEmpty(startGUID) &&
                !string.IsNullOrEmpty(hoverGUID) &&
                startGUID != hoverGUID && 
                hoverGUID != startGUID &&
                window.IsMouseOverPort;
        }
    }

    public override void UpdateMode(DialogEditorWindow window)
    {
        base.UpdateMode(window);

        if (isConnecting)
        {
            if(startDirection == PortDirection.input || startDirection == PortDirection.output)
            {
                DialogEditorUtility.DrawFlowConnection(startGUID, null, targetObject, startDirection, nodeStyle, "connection");
            }
            else if(startDirection == PortDirection.valueIn || startDirection == PortDirection.valueOut)
            {
                DialogEditorUtility.DrawValueConnection(startGUID, null, targetObject, startDirection, nodeStyle, "connectionValue");
            }
        }

        Event e = Event.current;

        if(e.button == 0 && e.type == EventType.MouseUp)
        {
            if (CanConnect)
            {
                TryToConnectNodes();
                return;
            }

            Node node = DialogEditorUtility.CreateNodeWithKey(window.inputs, targetObject);

            if (node != null)
            {
                window.CreateAndSelectNode(node, e.mousePosition);

                TryToConnectNodes(window.createMode.lastSelectedGUID);
                window.SetGraphMode(window.createMode);
                isConnecting = false;
                window.Repaint();
                return;
            }
            else
            {
                window.SetGraphMode(window.createMode);
                isConnecting = false;
            }
        }
        else if (e.button == 1 && e.type == EventType.MouseUp)
        {
            window.SetGraphMode(window.createMode);
            isConnecting = false;
        }
    }

    public void SetStartPort(string GUID, PortDirection direction)
    {
        if (!isConnecting)
        {
            startGUID = GUID;
            startDirection = direction;
            isConnecting = true;
        }
        else
        {
            SetHoverPort(GUID, direction);
            TryToConnectNodes();
            isConnecting = false;
        }
    }
    public void SetHoverPort(string GUID, PortDirection direction)
    {
        hoverGUID = GUID;
        hoverDirection = direction;
    }

    void TryToConnectNodes(string NodeGUID = null, bool isValue = false)
    {
        if((startDirection == PortDirection.valueOut || startDirection == PortDirection.valueIn) && 
            (hoverDirection == PortDirection.input || hoverDirection == PortDirection.output) ||
            (startDirection == PortDirection.input || startDirection == PortDirection.output) && 
            (hoverDirection == PortDirection.valueIn || hoverDirection == PortDirection.valueOut) ||
            (hoverDirection == PortDirection.input && startDirection == PortDirection.input) ||
            (hoverDirection == PortDirection.output && startDirection == PortDirection.output) ||
            (hoverDirection == PortDirection.valueIn && startDirection == PortDirection.valueIn) ||
            (hoverDirection == PortDirection.valueOut && startDirection == PortDirection.valueOut) ||
            startGUID == hoverGUID)
        {
            SaveAndExit();
            return;
        }

        string inGUID = null;

        if(targetObject.GetNodeByGUID(NodeGUID) != null)
        {
            if(isValue)
                inGUID = targetObject.GetNodeByGUID(NodeGUID).ValueOutputPorts[0].GUID;
            else
                inGUID = targetObject.GetNodeByGUID(NodeGUID).InputPorts[0].GUID;
        }

        var newConnection = new Connection
        {
            startNodeGUID = startGUID,
            targetNodeGUID = string.IsNullOrEmpty(inGUID) ? hoverGUID : inGUID
        };

        if (targetObject.GetConnection(newConnection) == null)
        {
            if((startDirection == PortDirection.input && hoverDirection == PortDirection.output) || (startDirection == PortDirection.valueIn && hoverDirection == PortDirection.valueOut))
            {
                var start = newConnection.targetNodeGUID;
                newConnection.targetNodeGUID = newConnection.startNodeGUID;
                newConnection.startNodeGUID = start;
            }

            if (targetObject.IsPortOccupied(newConnection.startNodeGUID))
            {
                targetObject.connections.Remove(targetObject.connections.Find(c=>c.startNodeGUID == newConnection.startNodeGUID));
            }

            Undo.RecordObject(targetObject, "Connected Nodes");
            targetObject.connections.Add(newConnection);
            isConnecting = false;
        }

        SaveAndExit();
    }

    void SaveAndExit()
    {
        window.SetGraphMode(window.createMode);
        isConnecting = false;
        window.SaveChanges();
    }
}
public class GraphResizeNodesMode: GraphMode
{
    public string nodeGUID;
    Node node;

    public enum ResizeDirection { Vertical, Horizontal, Diagonal }
    public ResizeDirection resizeDirection;

    public override void EnterMode(DialogEditorWindow window)
    {
        base.EnterMode(window);

        if (!string.IsNullOrEmpty(nodeGUID))
        { 
            node = targetObject.Nodes.Find(x => x.GUID == nodeGUID);

            if (node == null)
            {
                nodeGUID = "";
                window.SetGraphMode(window.createMode);
            }
        }
    }

    public override void UpdateMode(DialogEditorWindow window)
    {
        window.Repaint();

        switch (resizeDirection)
        {
            case ResizeDirection.Vertical: ResizeVertical(); break;
            case ResizeDirection.Horizontal: ResizeHorizontal(); break;
            case ResizeDirection.Diagonal: ResizeDiagonal(); break;
        }

        if (Event.current.type == EventType.MouseUp)
        {
            nodeGUID = "";
            window.SetGraphMode(window.createMode);
            Event.current.Use();
        }
    }

    private void ResizeDiagonal()
    {
        node.Rect.size += Event.current.delta / 2;
        node.Rect.width = Mathf.Clamp(node.Rect.width, node.MinX, node.MaxX);
        node.Rect.height = Mathf.Clamp(node.Rect.height, node.MinY, node.MaxY);

        EditorGUIUtility.AddCursorRect(node.DiagonalResizeRect, MouseCursor.ResizeUpLeft);
    }

    private void ResizeHorizontal()
    {
        node.Rect.size += Vector2.right * Event.current.delta.x / 2;
        node.Rect.width = Mathf.Clamp(node.Rect.width, node.MinX, node.MaxX);

        EditorGUIUtility.AddCursorRect(node.DiagonalResizeRect, MouseCursor.ResizeHorizontal);
    }

    private void ResizeVertical()
    {
        node.Rect.size += Vector2.up * Event.current.delta.y / 2;
        node.Rect.height = Mathf.Clamp(node.Rect.height, node.MinY, node.MaxY);

        EditorGUIUtility.AddCursorRect(node.DiagonalResizeRect, MouseCursor.ResizeVertical);
    }
}
#endif

public class EditorKey
{
    public bool[] isKeyHeld;
    public KeyCode[] keys;

    public EditorKey()
    {
        keys = Enum.GetValues(typeof(KeyCode)) as KeyCode[];
        isKeyHeld = new bool[keys.Length];
    }

    public void Update()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (Event.current.keyCode == keys[i])
            {
                if (Event.current.type == EventType.KeyDown)
                    isKeyHeld[i] = true;

                if (Event.current.type == EventType.KeyUp)
                    isKeyHeld[i] = false;
            }
        }
    }

    public bool GetKey(KeyCode code)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if(keys[i] == code)
            {
                return isKeyHeld[i];
            }
        }

        return false;
    }
}