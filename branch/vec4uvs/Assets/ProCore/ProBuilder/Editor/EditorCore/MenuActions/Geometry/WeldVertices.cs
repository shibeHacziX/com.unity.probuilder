using UnityEngine;
using UnityEditor;
using ProBuilder2.Common;
using ProBuilder2.EditorCommon;
using ProBuilder2.Interface;
using System.Linq;

namespace ProBuilder2.Actions
{
	public class WeldVertices : pb_MenuAction
	{
		public override pb_IconGroup group { get { return pb_IconGroup.Geometry; } }
		public override Texture2D icon { get { return pb_IconUtility.GetIcon("Vert_Weld"); } }
		public override pb_TooltipContent tooltip { get { return _tooltip; } }

		static readonly pb_TooltipContent _tooltip = new pb_TooltipContent
		(
			"Weld Vertices",
			@"Searches the current selection for vertices that are within the specified distance of on another and merges them into a single vertex.",
			CMD_ALT, 'V'
		);

		public override bool IsEnabled()
		{
			return 	pb_Editor.instance != null &&
					pb_Editor.instance.editLevel == EditLevel.Geometry &&
					pb_Editor.instance.selectionMode == SelectMode.Vertex &&
					selection != null &&
					selection.Length > 0 &&
					selection.Any(x => x.SelectedTriangleCount > 1);
		}
		
		public override bool IsHidden()
		{
			return 	pb_Editor.instance == null ||
					pb_Editor.instance.editLevel != EditLevel.Geometry ||
					pb_Editor.instance.selectionMode != SelectMode.Vertex;
					
		}

		public override bool SettingsEnabled()
		{
			return true;
		}

		static readonly GUIContent gc_weldDistance = new GUIContent("Weld Distance", "The maximum distance between two vertices in order to be welded together.");
		const float MIN_WELD_DISTANCE = .00001f;

		public override void OnSettingsGUI()
		{
			GUILayout.Label("Weld Settings", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();

			float weldDistance = pb_Preferences_Internal.GetFloat(pb_Constant.pbWeldDistance);

			if(weldDistance <= MIN_WELD_DISTANCE)
				weldDistance = MIN_WELD_DISTANCE;

			weldDistance = EditorGUILayout.FloatField(gc_weldDistance, weldDistance);

			if( EditorGUI.EndChangeCheck() )
			{
				if(weldDistance < MIN_WELD_DISTANCE)
					weldDistance = MIN_WELD_DISTANCE;
				EditorPrefs.SetFloat(pb_Constant.pbWeldDistance, weldDistance);
			}
		}

		public override pb_ActionResult DoAction()
		{
			return pb_Menu_Commands.MenuWeldVertices(selection);
		}
	}
}