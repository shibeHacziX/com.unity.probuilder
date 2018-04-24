using ProBuilder.Core;
using UnityEditor.ProBuilder;
using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder.UI;

namespace UnityEditor.ProBuilder.Actions
{
	class SubdivideObject : MenuAction
	{
		public override ToolbarGroup group { get { return ToolbarGroup.Object; } }
		public override Texture2D icon { get { return IconUtility.GetIcon("Toolbar/Object_Subdivide", IconSkin.Pro); } }
		public override TooltipContent tooltip { get { return _tooltip; } }
		public override bool isProOnly { get { return true; } }

		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"Subdivide Object",
			"Increase the number of edges and vertices on this object by creating 4 new quads in every face."
		);

		public override bool IsEnabled()
		{
			return 	ProBuilderEditor.instance != null && selection != null && selection.Length > 0;
		}

		public override pb_ActionResult DoAction()
		{
			return MenuCommands.MenuSubdivide(selection);
		}
	}
}
