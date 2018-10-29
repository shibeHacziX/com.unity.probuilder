﻿using UnityEngine;
using UnityEngine.ProBuilder;

namespace UnityEditor.ProBuilder
{
	class VertexMoveTool : VertexTool
	{
		const float k_CardinalAxisError = .001f;
		Vector3 m_HandlePosition;
		Matrix4x4 m_Translation = Matrix4x4.identity;

		bool m_SnapInWorldCoordinates;
		Vector3 m_WorldSnapDirection;
		Vector3 m_WorldSnapMask;

		protected override void OnToolEngaged()
		{
			m_SnapInWorldCoordinates = false;
			m_WorldSnapMask = new Vector3Mask(0x0);
		}

		protected override void DoTool(Vector3 handlePosition, Quaternion handleRotation)
		{
			if (!isEditing)
				m_HandlePosition = handlePosition;

			EditorGUI.BeginChangeCheck();

			m_HandlePosition = Handles.PositionHandle(m_HandlePosition, handleRotation);

			if (EditorGUI.EndChangeCheck())
			{
				if (!isEditing)
					BeginEdit("Translate Selection");

				var delta = m_HandlePosition - handlePositionOrigin;

				if (vertexDragging)
				{
					Vector3 nearest;

					if (FindNearestVertex(currentEvent.mousePosition, out nearest))
					{
						var unrotated = handleRotationOriginInverse * delta;
						var dir = new Vector3Mask(unrotated, k_CardinalAxisError);

						if (dir.active == 1)
						{
							var rot_dir = handleRotationOrigin * dir * 10000f;

							m_HandlePosition = HandleUtility.ProjectPointLine(nearest,
								handlePositionOrigin + rot_dir,
								handlePositionOrigin - rot_dir);

							delta = m_HandlePosition - handlePositionOrigin;
						}
					}
				}
				else if (snapEnabled)
				{
					var localDir = handleRotationOriginInverse * delta;
					m_WorldSnapDirection = delta.normalized;

					if (!m_SnapInWorldCoordinates && (Math.IsCardinalAxis(delta) || !Math.IsCardinalAxis(localDir)))
						m_SnapInWorldCoordinates = true;

					if (m_SnapInWorldCoordinates)
					{
						m_WorldSnapMask |= new Vector3Mask(m_WorldSnapDirection, k_CardinalAxisError);
						m_HandlePosition = Snapping.SnapValue(m_HandlePosition, m_WorldSnapMask * snapValue);
						delta = m_HandlePosition - handlePositionOrigin;
					}
					else
					{
						var travel = delta.magnitude;
						delta = m_WorldSnapDirection * Snapping.SnapValue(travel, snapValue);
						m_HandlePosition = handlePositionOrigin + delta;
					}
				}

				switch (pivotPoint)
				{
					case PivotPoint.WorldBoundingBoxCenter:
						break;

					case PivotPoint.ModelBoundingBoxCenter:
						delta = handleRotationOriginInverse * delta;
						break;

					case PivotPoint.IndividualOrigins:
						delta = handleRotationOriginInverse * delta;
						break;
				}

				m_Translation.SetTRS(delta, Quaternion.identity, Vector3.one);

				Apply(m_Translation);
			}
		}
	}
}