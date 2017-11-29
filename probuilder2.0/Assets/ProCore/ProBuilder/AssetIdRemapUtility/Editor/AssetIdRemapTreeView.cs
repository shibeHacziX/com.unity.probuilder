﻿using System;
using System.Collections.Generic;
using System.Net.Mail;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.WSA;

namespace ProBuilder.AssetUtility
{
	class AssetIdRemapTreeView : TreeView
	{
		AssetIdRemapObject m_RemapObject = null;
		const float k_RowHeight = 20f;
		const float k_RowHeightSearching = 76f;

		public AssetIdRemapObject remapObject
		{
			get { return m_RemapObject; }
			set { m_RemapObject = value; }
		}

		public AssetIdRemapTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
		{
			rowHeight = 20f;
			showAlternatingRowBackgrounds = true;
			showBorder = true;
			extraSpaceBeforeIconAndLabel = 18f;
		}

		protected override TreeViewItem BuildRoot()
		{
			StringTupleTreeElement root = new StringTupleTreeElement(0, -1, -1, "Root", "", "");

			var all = new List<TreeViewItem>();

			int index = 1;

			for (int i = 0, c = remapObject.map.Count; i < c; i++)
			{
				all.Add(new StringTupleTreeElement(index++, 0, i, "Remap Entry", remapObject[i].source.name, remapObject[i].destination.name));
				all.Add(new StringTupleTreeElement(index++, 1, i, "Local Path", remapObject[i].source.localPath, remapObject[i].destination.localPath));
				all.Add(new StringTupleTreeElement(index++, 1, i, "GUID", remapObject[i].source.guid, remapObject[i].destination.guid));
				all.Add(new StringTupleTreeElement(index++, 1, i, "File ID", remapObject[i].source.fileId, remapObject[i].destination.fileId));
				all.Add(new StringTupleTreeElement(index++, 1, i, "Type", remapObject[i].source.type, remapObject[i].destination.type));
			}

			SetupParentsAndChildrenFromDepths(root, all);
			return root;
		}

		public void SetRowHeight()
		{
			rowHeight = hasSearch ? k_RowHeightSearching : k_RowHeight;
		}

		protected override void RowGUI (RowGUIArgs args)
		{
			StringTupleTreeElement item = args.item as StringTupleTreeElement;

			for (int i = 0; i < args.GetNumVisibleColumns (); ++i)
			{
				CellGUI(args.GetCellRect(i), item, i, ref args);
			}
		}

		GUIContent m_CellContents = new GUIContent();

		void CellGUI(Rect rect, StringTupleTreeElement item, int visibleColumn, ref RowGUIArgs args)
		{
			if (hasSearch)
			{
				AssetId id = visibleColumn == 0 ? m_RemapObject[item.index].source : m_RemapObject[item.index].destination;

				m_CellContents.text = "<b>Name: </b>" + id.name +
				        "\n<b>Path: </b>" + id.localPath +
				        "\n<b>Guid: </b>" + id.guid +
				        "\n<b>FileId: </b>" + id.fileId +
				        "\n<b>Type: </b>" + id.type;
			}
			else
			{
				m_CellContents.text = item.GetLabel(visibleColumn);
			}

			rect.x += foldoutWidth + 4;
			rect.width -= (foldoutWidth + 4);

			if (hasSearch)
			{
				float textHeight = GUI.skin.label.CalcHeight(m_CellContents, rect.width);
				rect.y += (rect.height - textHeight) * .5f;
				rect.height = textHeight;
			}
			else
			{
				CenterRectUsingSingleLineHeight(ref rect);
			}

			GUI.skin.label.richText = true;
			GUI.Label(rect, m_CellContents);
		}

		protected override bool DoesItemMatchSearch(TreeViewItem element, string search)
		{
			StringTupleTreeElement tup = element as StringTupleTreeElement;

			if (tup == null || tup.depth > 0)
				return false;

			return tup.item1.Contains(search) || tup.item2.Contains(search);
		}

		protected override void ContextClicked()
		{
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Compare", ""), false, () =>
			{
				IList<int> selected = GetSelection();
				if (selected.Count == 2)
				{
					StringTupleTreeElement a = FindItem(selected[0], rootItem) as StringTupleTreeElement;
					StringTupleTreeElement b = FindItem(selected[1], rootItem) as StringTupleTreeElement;

					if (a != null && b != null)
					{
						AssetId left = m_RemapObject[a.index].source;
						AssetId right = m_RemapObject[b.index].destination;
						Debug.Log( left.AssetEquals2(right).ToString() );
						return;
					}
				}

				Debug.Log("Compare requires exactly two items be selected.");
			});
			menu.ShowAsContext();
		}
	}

	class StringTupleTreeElement : TreeViewItem
	{
		public string item1;
		public string item2;
		public int index;

		public StringTupleTreeElement(int id, int depth, int sourceIndex, string displayName, string key, string value) : base(id, depth, displayName)
		{
			item1 = key;
			item2 = value;
			index = sourceIndex;
		}

		public string GetLabel(int column)
		{
			return column < 1 ? item1 : item2;
		}
	}
}