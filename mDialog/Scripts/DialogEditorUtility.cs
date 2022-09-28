using System.Linq;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class DialogEditorUtility
{
    public static float connectionSmoothness = 0.5f;

    public static void DrawFlowConnection(string startGUID, string targetGUID, DialogObject targetObject, PortDirection startDirection, GUISkin nodeStyle, string style)
    {
        var start = targetObject.GetPortByGUID(startGUID).position + Vector2.one * 10;

        var target = Vector2.zero;
        if (targetGUID == null)
            target = Event.current.mousePosition;
        else
            target = targetObject.GetPortByGUID(targetGUID).position + Vector2.one * 10;

        var half = (startDirection == PortDirection.output) ? Mathf.Max(target.x - start.x, start.x - target.x) : Mathf.Min(target.x - start.x, start.x - target.x);

        var c0 = new Vector2(start.x + half * connectionSmoothness, start.y);
        var c1 = new Vector2(target.x - half * connectionSmoothness, target.y);

        if (Vector2.Distance(start, target) < 4f) return;

#if UNITY_EDITOR
        Handles.DrawBezier(start, target, c0, c1,
            nodeStyle.GetStyle(style).normal.textColor,
            nodeStyle.GetStyle(style).normal.background, 3f);
#endif
    }

    public static void DrawValueConnection(string startGUID, string targetGUID, DialogObject targetObject, PortDirection startDirection, GUISkin nodeStyle, string style)
    {
        var start = targetObject.GetPortByGUID(startGUID).position + Vector2.one * 10;

        var target = Vector2.zero;
        if (targetGUID == null)
            target = Event.current.mousePosition;
        else
            target = targetObject.GetPortByGUID(targetGUID).position + Vector2.one * 10;

        var half = (startDirection == PortDirection.valueOut) ? Mathf.Max(target.y - start.y, start.y - target.y) : Mathf.Min(target.y - start.y, start.y - target.y);

        var c0 = new Vector2(start.x, start.y + half * connectionSmoothness);
        var c1 = new Vector2(target.x, target.y - half * connectionSmoothness);

        if (Vector2.Distance(start, target) < 4f) return;

#if UNITY_EDITOR
        Handles.DrawBezier(start, target, c0, c1,
            nodeStyle.GetStyle(style).normal.textColor,
            nodeStyle.GetStyle(style).normal.background, 3f);
#endif
    }

    public static void DrawBoolField(Rect rect, ref bool value, ref float time, GUISkin nodeStyle, float speed = 1f, float deltaTime = 0.01f)
    {
        Event e = Event.current;

        if (GUI.Button(rect, "", GUIStyle.none))
        {
            e.Use();
            value = !value;
        }

        float i = value ? 1 : -1;
        time += i * deltaTime * speed;
        time = Mathf.Clamp01(time);

        GUI.HorizontalSlider(rect, time, 0, 1,
            nodeStyle.horizontalSlider, nodeStyle.horizontalSliderThumb);

        var labelRect = new Rect(rect.x + 36, rect.y - 4, 40, 20);
        GUI.Label(labelRect, value ? "True" : "False", nodeStyle.FindStyle("leftAlign"));
    }

    public static void DrawToolbarButton(Rect rect, bool enabled, string label, Action onClick)
    {
        using (new EditorGUI.DisabledGroupScope(enabled))
        {
            if (GUI.Button(rect, label, EditorStyles.toolbarButton))
            {
                onClick?.Invoke();
            }
        }
    }

    public static Node CreateNodeWithKey(EditorKey inputs, DialogObject targetObject)
    {
        Node node = null;

        if (inputs.GetKey(KeyCode.P))
            node = new DialogNode().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.S))
            node = new StartNode().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.B))
            node = new BranchNode().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.N))
            node = new Node().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.C))
            node = new MultipleChoiceNode().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.I))
            node = new UserInputNode().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.F))
            node = new FromNode().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.T))
            node = new ToNode().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.A))
            node = new PlayAnimationNode().SetTarget(targetObject);

        if (inputs.GetKey(KeyCode.R))
            node = new RandomNode().SetTarget(targetObject);

        if (node != null)
            return node;

        return null;
    }

    public static List<Node> GetNodeInstances()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(Node)))
            .Select(type => Activator.CreateInstance(type) as Node).ToList();
    }

    public static List<Node> GetNodesWithInterface<T>()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetInterfaces().Contains(typeof(T)))
            .Select(type => Activator.CreateInstance(type) as Node).ToList();
    }
}