﻿using UnityEngine;
using UnityEngine.ProBuilder;


#if !UNITY_2020_2_OR_NEWER
using ToolManager = UnityEditor.EditorTools.EditorTools;
#else
using ToolManager = UnityEditor.EditorTools.ToolManager;
#endif

namespace UnityEditor.ProBuilder.Actions
{
    public class CutToolToggle : MenuToggle
    {
        internal CutTool m_Tool;

        bool m_RestorePreviousMode;
        SelectMode m_PreviousMode;

        bool m_RestorePreviousTool;
        Tool m_PreviousTool;

        public override ToolbarGroup group
        {
            get { return ToolbarGroup.Geometry; }
        }

        public override Texture2D icon
        {
            get { return IconUtility.GetIcon("Toolbar/CutTool", IconSkin.Pro); }
        }

        public override TooltipContent tooltip
        {
            get { return s_Tooltip; }
        }

        public override SelectMode validSelectModes
        {
            get { return SelectMode.Vertex | SelectMode.Edge | SelectMode.Face | SelectMode.Object; }
        }

        protected override bool hasFileMenuEntry
        {
            get { return false; }
        }

        static readonly TooltipContent s_Tooltip = new TooltipContent
        (
            "Cut Tool",
            @"Create a cut in a face to subdivide it.",
            keyCommandAlt, keyCommandShift, 'C'
        );

        public override bool enabled
        {
            get { return base.enabled && MeshSelection.selectedObjectCount > 0; }
        }

        public override ActionResult StartActivation(StartEndCallBack onStart)
        {
            m_RestorePreviousMode = true;
            m_PreviousMode = ProBuilderEditor.selectMode;
            ProBuilderEditor.selectMode = SelectMode.Object;

            m_RestorePreviousTool = true;
            m_PreviousTool = Tools.current;
            m_Tool = ScriptableObject.CreateInstance<CutTool>();
            ToolManager.SetActiveTool(m_Tool);

            Undo.RegisterCreatedObjectUndo(m_Tool, "Open Cut Tool");

            ToolManager.activeToolChanged += ActiveToolChanged;
            ProBuilderEditor.selectModeChanged += OnSelectModeChanged;

            onStart();
            return new ActionResult(ActionResult.Status.Success,"Cut Tool Starts");
        }

        public override ActionResult EndActivation(StartEndCallBack onEnd)
        {
            ToolManager.activeToolChanged -= ActiveToolChanged;
            ProBuilderEditor.selectModeChanged -= OnSelectModeChanged;

            Object.DestroyImmediate(m_Tool);

            if(m_RestorePreviousMode)
                ProBuilderEditor.selectMode = m_PreviousMode;
            if(m_RestorePreviousTool)
                Tools.current = m_PreviousTool;

            onEnd();
            return new ActionResult(ActionResult.Status.Success,"Cut Tool Ends");
        }

        void OnSelectModeChanged(SelectMode obj)
        {
            m_RestorePreviousMode = false;
            LeaveTool();
        }

        void ActiveToolChanged()
        {
            if(Tools.current != Tool.None)
                m_RestorePreviousTool = false;
            EditorApplication.delayCall += () => LeaveTool();
        }

        void LeaveTool()
        {
            ActionResult result = EndActivation(OnEnd);
            EditorUtility.ShowNotification(result.notification);
        }
    }
}