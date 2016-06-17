using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProBuilder2.Common;

namespace ProBuilder2.MeshOperations
{
	/**
	 *	Functions for beveling edges.
	 */
	public static class pb_Bevel
	{
		public static pb_ActionResult BevelEdges(pb_Object pb, IList<pb_Edge> edges, float amount)
		{
			Dictionary<int, int> lookup = pb.sharedIndices.ToDictionary();
			List<pb_EdgeLookup> m_edges = pb_EdgeLookup.GetEdgeLookup(edges, lookup).ToList();
			m_edges.Distinct();
			List<pb_WingedEdge> wings = pb_WingedEdge.GenerateWingedEdges(pb);
			List<pb_Vertex> vertices = new List<pb_Vertex>( pb_Vertex.GetVertices(pb) );
			List<pb_FaceRebuildData> appendFaces = new List<pb_FaceRebuildData>();

			HashSet<pb_Face> ignore = new HashSet<pb_Face>();
			HashSet<int> slide = new HashSet<int>();
			Dictionary<int, List<pb_Vertex>> holes = new Dictionary<int, List<pb_Vertex>>();
	
			foreach(pb_EdgeLookup lup in m_edges)
			{
				pb_WingedEdge we = wings.FirstOrDefault(x => x.edge.Equals(lup));

				if(we == null || we.opposite == null)
					continue;

				ignore.Add(we.face);
				ignore.Add(we.face);
				ignore.Add(we.opposite.face);
				ignore.Add(we.opposite.face);

				// after initial slides go back and split indirect triangles at the intersecting index into two vertices
				slide.Add(we.edge.common.x);
				slide.Add(we.edge.common.y);

				SlideEdge(vertices, we, holes, amount);
				SlideEdge(vertices, we.opposite, holes, amount);

				appendFaces.AddRange( GetBridgeFaces(vertices, we, we.opposite) );
			}

			Dictionary<pb_Face, List<pb_Tuple<pb_WingedEdge, int>>> sorted = new Dictionary<pb_Face, List<pb_Tuple<pb_WingedEdge, int>>>();

			foreach(int c in slide)
			{
				IEnumerable<pb_WingedEdge> matches = wings.Where(x => x.edge.common.Contains(c) && !ignore.Contains(x.face));

				HashSet<pb_Face> used = new HashSet<pb_Face>();

				foreach(pb_WingedEdge match in matches)
				{
					if(!used.Add(match.face))
						continue;

					sorted.AddOrAppend(match.face, new pb_Tuple<pb_WingedEdge, int>(match, c));
				}
			}

			foreach(var kvp in sorted)
			{
				Dictionary<int, List<pb_Vertex>> appendedVertices;

				pb_FaceRebuildData f = pbVertexOps.ExplodeVertex(vertices, kvp.Value, amount, out appendedVertices);

				foreach(var apv in appendedVertices)
					holes.AddOrAppendRange(apv.Key, apv.Value);

				if(f != null)
					appendFaces.Add(f);
			}

			foreach(var k in holes)
			{		
				HashSet<int> used = new HashSet<int>();
				List<pb_Vertex> v = new List<pb_Vertex>();

				foreach(pb_Vertex vert in k.Value)
					if(used.Add(pb_Vector.GetHashCode(vert.position)))
						v.Add(vert);

				if(v.Count < 3)
					continue;

				pb_FaceRebuildData filledHole = pb_AppendPolygon.FaceWithVertices(v);
				
				if(filledHole != null)
					appendFaces.Add(filledHole);
			}

			List<pb_Face> faces = new List<pb_Face>(pb.faces);
			pb_FaceRebuildData.Apply(appendFaces, vertices, faces, null, null);
			pb.SetVertices(vertices);
			pb.SetFaces(faces.ToArray());
			pb.SetSharedIndices( pb_IntArrayUtility.ExtractSharedIndices(pb.vertices) );
			pb.DeleteFaces(sorted.Keys);
			
			pb.ToMesh();

			return new pb_ActionResult(Status.Success, "Bevel Edges");
 		}

 		private static List<pb_FaceRebuildData> GetBridgeFaces(IList<pb_Vertex> vertices, pb_WingedEdge left, pb_WingedEdge right)
 		{
 			List<pb_FaceRebuildData> faces = new List<pb_FaceRebuildData>();

 			pb_FaceRebuildData rf = new pb_FaceRebuildData();

 			pb_EdgeLookup a = left.edge;
 			pb_EdgeLookup b = right.edge;

 			rf.vertices = new List<pb_Vertex>() 
 			{
 				vertices[a.local.x],
 				vertices[a.local.y],
 				vertices[b.local.y],
 				vertices[b.local.x]
 			};

 			rf.face = new pb_Face(
 				new int[] { 2, 1, 0, 2, 3, 1 },
 				left.face.material,
 				new pb_UV(),
 				-1,
 				-1,
 				-1,
 				false);

 			faces.Add(rf);

 			return faces;
 		}

 		private static void SlideEdge(IList<pb_Vertex> vertices, pb_WingedEdge we, Dictionary<int, List<pb_Vertex>> holes, float amount)
 		{
			we.face.manualUV = true;
			we.face.textureGroup = -1;

			pb_Edge slide_x = GetLeadingEdge(we, we.edge.common.x);
			pb_Edge slide_y = GetLeadingEdge(we, we.edge.common.y);
			
			if(slide_x == null || slide_y == null) 
				return;

			Vector3 x = (vertices[slide_x.x].position - vertices[slide_x.y].position).normalized;
			Vector3 y = (vertices[slide_y.x].position - vertices[slide_y.y].position).normalized;

			vertices[we.edge.local.x].position += x * amount;
			vertices[we.edge.local.y].position += y * amount;

			holes.AddOrAppend(we.edge.common.x, vertices[we.edge.local.x]);
			holes.AddOrAppend(we.edge.common.y, vertices[we.edge.local.y]);
		}

		private static pb_Edge GetLeadingEdge(pb_WingedEdge wing, int common)
		{
			if(wing.previous.edge.common.x == common)
				return new pb_Edge(wing.previous.edge.local.y, wing.previous.edge.local.x);
			else if(wing.previous.edge.common.y == common)
				return new pb_Edge(wing.previous.edge.local.x, wing.previous.edge.local.y);
			else if(wing.next.edge.common.x == common)
				return new pb_Edge(wing.next.edge.local.y, wing.next.edge.local.x);
			else if(wing.next.edge.common.y == common)
				return new pb_Edge(wing.next.edge.local.x, wing.next.edge.local.y);
			return null;
		}
	}
}