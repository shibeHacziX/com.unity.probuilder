#if UNITY_2020_1_OR_NEWER
#define TOOL_CONTEXTS_ENABLED
#endif

using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UObject = UnityEngine.Object;
using UnityEngine.ProBuilder;
using ToolManager = UnityEditor.EditorTools.EditorTools;

namespace UnityEditor.ProBuilder
{
    // Handles forwarding the Unity tool to ProBuilder editor
    class ProBuilderToolManager : IDisposable
    {
        const int k_BuiltinToolCount = (int) Tool.Custom + 1;

        // When tool contexts are fully implemented there should be no need for `SelectMode`
        static Pref<SelectMode> s_SelectMode = new Pref<SelectMode>("editor.selectMode", SelectMode.Object);
        static Pref<SelectMode> s_LastMeshSelectMode = new Pref<SelectMode>("editor.lastMeshSelectMode", SelectMode.Object);

        static ProBuilderToolManager s_Instance;

        public static SelectMode selectMode
        {
            get => s_SelectMode.value;
            private set => s_SelectMode.SetValue(value);
        }

        bool m_IsDisposed = false;

#if !TOOL_CONTEXTS_ENABLED
        Type[] m_DefaultTools;
        EditorTool[] m_VertexTools;
        EditorTool[] m_TextureTools;
        static readonly Dictionary<Type, Tool> k_ToolTypeMap = new Dictionary<Type, Tool>()
        {
            { typeof(ProbuilderMoveTool), Tool.Move },
            { typeof(ProbuilderRotateTool), Tool.Rotate },
            { typeof(ProbuilderScaleTool), Tool.Scale },
            { typeof(TextureMoveTool), Tool.Move },
            { typeof(TextureRotateTool), Tool.Rotate },
            { typeof(TextureScaleTool), Tool.Scale }
        };
#endif

        public ProBuilderToolManager()
        {
            s_Instance = this;

#if !TOOL_CONTEXTS_ENABLED
            EditorApplication.update += ForwardBuiltinToolCheck;

            m_DefaultTools = new Type[k_BuiltinToolCount];
            m_VertexTools = new EditorTool[k_BuiltinToolCount];
            m_TextureTools = new EditorTool[k_BuiltinToolCount];

            m_DefaultTools[(int) Tool.Move] = typeof(MoveTool);
            m_DefaultTools[(int) Tool.Rotate] = typeof(RotateTool);
            m_DefaultTools[(int) Tool.Scale] = typeof(ScaleTool);

            m_VertexTools[(int)Tool.Move] = ScriptableObject.CreateInstance<ProbuilderMoveTool>();
            m_VertexTools[(int)Tool.Rotate] = ScriptableObject.CreateInstance<ProbuilderRotateTool>();
            m_VertexTools[(int)Tool.Scale] = ScriptableObject.CreateInstance<ProbuilderScaleTool>();

            m_TextureTools[(int)Tool.Move] = ScriptableObject.CreateInstance<TextureMoveTool>();
            m_TextureTools[(int)Tool.Rotate] = ScriptableObject.CreateInstance<TextureRotateTool>();
            m_TextureTools[(int)Tool.Scale] = ScriptableObject.CreateInstance<TextureScaleTool>();
#endif

            SetSelectMode(selectMode);
        }

        public void Dispose()
        {
            if (m_IsDisposed)
                return;
            m_IsDisposed = true;
            GC.SuppressFinalize(this);

            SetSelectModeInternal(SelectMode.Object);
            s_Instance = null;

#if !TOOL_CONTEXTS_ENABLED
            EditorApplication.update -= ForwardBuiltinToolCheck;

            for(int i = 0, c = m_VertexTools.Length; i < c; i++)
                if (m_VertexTools[i] != null)
                    UObject.DestroyImmediate(m_VertexTools[i]);
            for(int i = 0, c = m_TextureTools.Length; i < c; i++)
                if (m_TextureTools[i] != null)
                    UObject.DestroyImmediate(m_TextureTools[i]);
#endif
        }

        public static void SetSelectMode(SelectMode mode)
        {
            if (s_Instance == null)
                return;
            s_Instance.SetSelectModeInternal(mode);
        }

        void SetSelectModeInternal(SelectMode mode)
        {
            if (mode == selectMode)
                return;

            selectMode = mode;

#if TOOL_CONTEXTS_ENABLED
            if(selectMode.IsPositionMode() && ToolManager.activeContextType != typeof(PositionToolContext))
                ToolManager.SetActiveContext<PositionToolContext>();
            else if(selectMode.IsTextureMode() && ToolManager.activeContextType != typeof(TextureToolContext))
                ToolManager.SetActiveContext<TextureToolContext>();
            else if ( !selectMode.IsPositionMode() )
                ToolManager.SetActiveContext<GameObjectToolContext>();
#else
            if (mode.IsMeshElementMode())
                s_LastMeshSelectMode.SetValue(mode);

            var tool = activeTool;

            if (tool == Tool.None)
                ToolManager.SetActiveTool<MoveTool>();
            else  if(mode.IsPositionMode() && m_VertexTools[(int)tool] != null)
                ToolManager.SetActiveTool(m_VertexTools[(int)tool]);
            else if(mode.IsTextureMode() && m_TextureTools[(int)tool] != null)
                ToolManager.SetActiveTool(m_TextureTools[(int)tool]);
            else if (mode == SelectMode.Object && GetBuiltinToolType(ToolManager.activeToolType, out Type builtin))
                ToolManager.SetActiveTool(builtin);
#endif
        }

        public void ResetToLastSelectMode()
        {
            SetSelectMode(s_LastMeshSelectMode);
        }

        public static void NextMeshSelectMode()
        {
            // todo
            // if (s_SelectMode == SelectMode.Vertex)
            //     selectMode = SelectMode.Edge;
            // else if (s_SelectMode == SelectMode.Edge)
            //     selectMode = SelectMode.Face;
            // else if (s_SelectMode == SelectMode.Face)
            //     selectMode = SelectMode.Vertex;
        }

        public static Tool activeTool
        {
            get
            {
                if (k_ToolTypeMap.TryGetValue(ToolManager.activeToolType, out Tool tool))
                    return tool;
                return Tools.current;
            }
        }

#if !TOOL_CONTEXTS_ENABLED

        // Can't do this in `activeToolChanged` because it is forbidden by ToolManager to prevent recursion
        void ForwardBuiltinToolCheck()
        {
            if(selectMode.IsMeshElementMode() && GetProBuilderToolType(ToolManager.activeToolType, out EditorTool tool))
                ToolManager.SetActiveTool(tool);
        }

        bool GetBuiltinToolType(Type type, out Type builtin)
        {
            for (int i = 0; i < k_BuiltinToolCount; i++)
            {
                if (m_VertexTools[i]?.GetType() == type || m_TextureTools[i]?.GetType() == type)
                {
                    builtin = m_DefaultTools[i];
                    return true;
                }
            }

            builtin = null;
            return false;
        }

        bool GetProBuilderToolType(Type type, out EditorTool tool)
        {
            for (int i = 0; i < k_BuiltinToolCount; i++)
            {
                if (m_DefaultTools[i] == type)
                {
                    if(selectMode.IsPositionMode())
                        tool = m_VertexTools[i];
                    else if (selectMode.IsTextureMode())
                        tool = m_TextureTools[i];
                    else
                        continue;

                    return true;
                }
            }

            tool = null;
            return false;
        }
#endif
    }
}
