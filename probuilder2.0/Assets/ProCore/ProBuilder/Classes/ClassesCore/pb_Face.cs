/*
 *	Storage class for associating triangles to faces.
 *	Basically behaves like a dictionary entry, but with
 *	some added functionality.
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

namespace ProBuilder2.Common
{
[System.Serializable]
/**
 *	\brief Contains mesh and material information.  Used in the creation of #pb_Objects.
 */
public class pb_Face : ISerializable, IEquatable<pb_Face>
{

#region SERIALIZATION

	// OnSerialize
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("indices",			_indices, 					typeof(int[]));
		info.AddValue("distinctIndices", 	_distinctIndices, 			typeof(int[]));
		info.AddValue("edges", 				_edges, 					typeof(pb_Edge[]));
		info.AddValue("smoothingGroup",	 	_smoothingGroup, 			typeof(int));
		info.AddValue("uv",	 				_uv, 						typeof(pb_UV));
		info.AddValue("material",			_mat.name,					typeof(string));
		info.AddValue("manualUV", 			manualUV, 					typeof(bool));
		info.AddValue("elementGroup", 		elementGroup, 				typeof(int));
	}

	// The pb_SerializedMesh constructor is used to deserialize values. 
	public pb_Face(SerializationInfo info, StreamingContext context)
	{
		this._indices = 			(int[])		info.GetValue( "indices",			typeof(int[]));
		this._distinctIndices = 	(int[])		info.GetValue( "distinctIndices",	typeof(int[]));
		this._edges = 				(pb_Edge[])	info.GetValue( "edges",				typeof(pb_Edge[]));
		this._smoothingGroup = 		(int) 		info.GetValue( "smoothingGroup",	typeof(int));
		this._uv = 					(pb_UV) 	info.GetValue( "uv",				typeof(pb_UV));
		this.manualUV = 			(bool) 		info.GetValue( "manualUV",			typeof(bool));
		this.elementGroup = 		(int) 		info.GetValue( "elementGroup",		typeof(int));

		// material is a little different - it requires some fanaglin'
		this._mat = pb_Constant.DefaultMaterial;

		string matName = (string)info.GetValue("material", typeof(string));

		foreach(Material mat in Resources.FindObjectsOfTypeAll(typeof(Material)))
		{
			if(mat.name.Equals(matName))
			{
				this._mat = mat;
				break;
			}
		}
	}
#endregion

#region CONSTRUCTORS

	public pb_Face() {}

	public pb_Face(int[] i)
	{
		SetIndices(i);
		_uv = new pb_UV();
		_mat = pb_Constant.DefaultMaterial;
		_smoothingGroup = 0;
		elementGroup = 0;
	}

	public pb_Face(int[] i, Material m, pb_UV u, int smoothingGroup, int textureGroup, int elementGroup, bool manualUV)
	{
		this.SetIndices(i);
		this._uv = new pb_UV(u);
		this._mat = m;
		this._smoothingGroup = smoothingGroup;
		this.textureGroup = textureGroup;
		this.elementGroup = elementGroup;
		this.manualUV = manualUV;
	}

	/**
	 * Deep copy constructor.
	 */
	public pb_Face(pb_Face face)
	{
		_indices = new int[face.indices.Length];
		System.Array.Copy(face.indices, _indices, face.indices.Length);
		_uv = new pb_UV(face.uv);
		_mat = face.material;
		_smoothingGroup = face.smoothingGroup;
		textureGroup = face.textureGroup;
		elementGroup = face.elementGroup;
		manualUV = face.manualUV;

		RebuildCaches();
	}

	/**
	 *	Copies values from other to this face.
	 */
	public void CopyFrom(pb_Face other)
	{
		int len = other.indices == null ? 0 : other.indices.Length;
		_indices = new int[len];
		System.Array.Copy(other.indices, _indices, len);
		_smoothingGroup = other.smoothingGroup;
		_uv = new pb_UV(other.uv);
		_mat = other.material;
		manualUV = other.manualUV;
		elementGroup = other.elementGroup;
		RebuildCaches();
	}

	public bool Equals(pb_Face b)
	{
		if(!pbUtil.IsEqual<int>(_indices, b.indices))
			return false;

		// at the moment PB doesn't allow faces that share indices,
		// so don't bother comparing anything else.

		return true;
	}

	public override bool Equals(System.Object b)
	{
		return b is pb_Face && ((pb_Face)b).Equals(this);
	}

	public override int GetHashCode()
	{
		// http://stackoverflow.com/questions/3404715/c-sharp-hashcode-for-array-of-ints
		int hc = _indices.Length;

		for(int i = 0; i <_indices.Length; ++i)
		{
			hc = unchecked(hc * 17 + _indices[i]);
		}

		return hc;
	}

	public static explicit operator pb_EdgeConnection(pb_Face face)
	{
		return new pb_EdgeConnection(face, null);
	}

	public static explicit operator pb_VertexConnection(pb_Face face)
	{
		return new pb_VertexConnection(face, null);
	}
#endregion

#region MEMBERS

	[SerializeField] int[] _indices;
	[SerializeField] int[] _distinctIndices;

	///< A cache of the calculated #pb_Edge edges for this face.  Call pb_Face::RebuildCaches to update. 
	[SerializeField] pb_Edge[] _edges;

	///< Adjacent faces sharing this smoothingGroup will have their abutting edge normals averaged.
	[SerializeField] int _smoothingGroup;

	///< If manualUV is false, these parameters determine how this face's vertices are projected to 2d space.
	[SerializeField] pb_UV _uv;

	///< What material does this face use.
	[SerializeField] Material _mat;

	///< If this face has had it's UV coordinates done by hand, don't update them with the auto unwrap crowd.
	public bool manualUV;

	///< UV Element group.
	public int elementGroup;

	///< What texture group this face belongs to.
	public int textureGroup = -1;

#endregion

#region ACCESS

	public int[] indices { get { return _indices; } }
	public int[] distinctIndices { get { return _distinctIndices == null ? CacheDistinctIndices() : _distinctIndices; } }
	public pb_Edge[] edges { get { return _edges == null ? CacheEdges() : _edges; } }	// todo -- remove this after a while
	public int smoothingGroup { get { return _smoothingGroup; } set { _smoothingGroup = value; } }
	public Material material { get { return _mat; } set { _mat = value; } }
	public pb_UV uv { get { return _uv; } set { _uv = value; } }

	[System.Obsolete("Use face.material property.")]
	public void SetMaterial(Material material) { _mat = material; }

	[System.Obsolete("Use face.uv property.")]
	public void SetUV(pb_UV uvs) { _uv = uvs; }

	[System.Obsolete("Use face.smoothingGroup property.")]
	public void SetSmoothingGroup(int smoothing) { _smoothingGroup = smoothing; }
#endregion

#region GET

	public bool IsValid()
	{
		return indices.Length > 2;
	}

	public Vector3[] GetDistinctVertices(Vector3[] verts)
	{
		int[] di = distinctIndices;
		Vector3[] v = new Vector3[di.Length];
		
		for(int i = 0; i < di.Length; i++) {
			v[i] = verts[di[i]];
		}
		return v;
	}

	/**
	 * Returns the triangle at index in the indices array.
	 * {
	 *	 0, 1, 2,	// tri at index 0
	 *	 2, 3, 1,	// tri at index 1, etc
	 * }
	 */
	public int[] GetTriangle(int index)
	{
		if(index*3+3 > indices.Length)
			return null;
		else
			return new int[3]{indices[index*3+0],indices[index*3+1],indices[index*3+2]};
	}

	public pb_Edge[] GetAllEdges()
	{
		pb_Edge[] edges = new pb_Edge[indices.Length];

		for(int i = 0; i < indices.Length; i+=3)
		{
			edges[i  ] = new pb_Edge(indices[i+0], indices[i+1]);
			edges[i+1] = new pb_Edge(indices[i+1], indices[i+2]);
			edges[i+2] = new pb_Edge(indices[i+2], indices[i+0]);
		}
		return edges;
	}
#endregion

#region INSTANCE_METHODS

	/**
	 * Sets this face's indices to a new value.
	 */
	public void SetIndices(int[] i)
	{
		_indices = i;
		RebuildCaches();
	}

	/**
	 * Add offset to each value in the indices array.
	 */
	public void ShiftIndices(int offset)
	{
		for(int i = 0; i <_indices.Length; i++)
			_indices[i] += offset;
	}

	/**
	 * Returns the smallest value in the indices array.
	 */
	public int SmallestIndexValue()
	{
		int smallest = _indices[0];
		for(int i = 0; i < _indices.Length; i++)
		{
			if(_indices[i] < smallest)
				smallest = _indices[i];
		}
		return smallest;
	}

	/**
	 *	\brief Shifts all triangles to be zero indexed.
	 *	\ex 
	 *	new pb_Face(3,4,5).ShiftIndicesToZero();
	 *	Sets the pb_Face index array to 0,1,2
	 */
	public void ShiftIndicesToZero()
	{
		int offset = SmallestIndexValue();
 
		for(int i = 0; i < indices.Length; i++)
			_indices[i] -= offset;

		for(int i = 0; i < _distinctIndices.Length; i++)
			_distinctIndices[i] -= offset;

		for(int i = 0; i < _edges.Length; i++)
		{
			_edges[i].x -= offset;
			_edges[i].y -= offset;
		}
	}

	/**
	 * Reverse the winding order of this face.
	 */
	public void ReverseIndices()
	{
		System.Array.Reverse(_indices);
	}

	/**
	 *	\brief Rebuilds all property caches on pb_Face.
	 */
	public void RebuildCaches()
	{
		CacheDistinctIndices();
		CacheEdges();
	}

	private pb_Edge[] CacheEdges()
	{
		if(_indices == null)
			return null;
			
		// _edges = pb_Edge.GetPerimeterEdges( GetAllEdges() );

		HashSet<pb_Edge> dist = new HashSet<pb_Edge>();
		List<pb_Edge> dup = new List<pb_Edge>();

		for(int i = 0; i < indices.Length; i+=3)
		{
			pb_Edge a = new pb_Edge(indices[i+0],indices[i+1]);
			pb_Edge b = new pb_Edge(indices[i+1],indices[i+2]);
			pb_Edge c = new pb_Edge(indices[i+2],indices[i+0]);

			if(!dist.Add(a)) dup.Add(a);
			if(!dist.Add(b)) dup.Add(b);
			if(!dist.Add(c)) dup.Add(c);
		}

		dist.ExceptWith(dup);

		_edges = dist.ToArray();

		return _edges;
	}

	private int[] CacheDistinctIndices()
	{
		if(_indices == null)
			return null;

		_distinctIndices = new HashSet<int>(_indices).ToArray();

		return distinctIndices;
	}
#endregion

#region QUERY

	public bool Contains(int[] triangle)
	{
		for(int i = 0; i < indices.Length; i+=3)
		{
			if(	triangle.Contains(indices[i+0]) &&
				triangle.Contains(indices[i+1]) &&
				triangle.Contains(indices[i+2]) )
				return true;
		}

		return false;
	}
#endregion

#region Special

	/**
	 *	\brief Returns all triangles contained within the #pb_Face array.
	 *	@param faces #pb_Face array to extract triangle data from.
	 *	\returns int[] containing all triangles.  Triangles may point to duplicate vertices that share a world point (not 'distinct' by ProBuilder terminology).
	 */
	public static int[] AllTriangles(pb_Face[] q)
	{
		List<int> all = new List<int>();
		foreach(pb_Face quad in q)
		{
			all.AddRange(quad.indices);
		}
		return all.ToArray();
	}

	public static int[] AllTriangles(List<pb_Face> q)
	{
		List<int> all = new List<int>();
		foreach(pb_Face quad in q)
		{
			all.AddRange(quad.indices);
		}
		return all.ToArray();
	}

	/**
	 *	\brief Returns all distinct triangles contained within the #pb_Face array.
	 *	@param faces #pb_Face array to extract triangle data from.
	 *	\returns int[] containing all triangles.  Triangles may point to duplicate vertices that share a world point (not 'distinct' by ProBuilder terminology).
	 */
	public static int[] AllTrianglesDistinct(pb_Face[] q)
	{
		List<int> all = new List<int>();
		foreach(pb_Face quad in q)
			all.AddRange(quad.distinctIndices);

		return all.ToArray();
	}

	public static List<int> AllTrianglesDistinct(List<pb_Face> f)
	{
		List<int> all = new List<int>();
		foreach(pb_Face quad in f)
			all.AddRange(quad.distinctIndices);

		return all;
	}

	/**
	 * Sorts faces by material and returns a jagged array of their combined triangles.
	 */
	public static int MeshTriangles(pb_Face[] faces, out int[][] submeshes, out Material[] materials)
	{
		// Sort the faces into groups of like materials
		Dictionary<Material, List<pb_Face>> matDic = new Dictionary<Material, List<pb_Face>>();
		
		int i = 0;

#if PROTOTYPE
			matDic.Add(pb_Constant.DefaultMaterial, new List<pb_Face>(faces));
#else
			for(i = 0; i < faces.Length; i++)
			{
				if(faces[i] == null)
				{
					Debug.LogWarning("Null face found!  Skipping these triangles.");
					continue;
				}

				Material face_mat = faces[i].material ?? pb_Constant.UnityDefaultDiffuse;

				if(matDic.ContainsKey(face_mat))
				{
					matDic[face_mat].Add(faces[i]);
				}
				else
				{
					matDic.Add(face_mat, new List<pb_Face>(1) { faces[i] } );
				}
			}
#endif

		materials = new Material[matDic.Count];
		submeshes = new int[materials.Length][];

		i = 0;
		foreach( KeyValuePair<Material, List<pb_Face>> kvp in matDic )
		{
			submeshes[i] = pb_Face.AllTriangles(kvp.Value);
			materials[i] = kvp.Key;
			i++;
		}

		return submeshes.Length;
	}
#endregion

#region OVERRIDE

	public override string ToString()
	{
		// shouldn't ever be the case
		if(indices.Length % 3 != 0)
			return "Index count is not a multiple of 3.";

		System.Text.StringBuilder sb = new System.Text.StringBuilder();

		for(int i = 0; i < indices.Length; i += 3)
		{	
			sb.Append("[");
			sb.Append(indices[i]);
			sb.Append(", ");
			sb.Append(indices[i+1]);
			sb.Append(", ");
			sb.Append(indices[i+2]);
			sb.Append("]");
			
			if(i < indices.Length-3)
				sb.Append(", ");
		}

		return sb.ToString();
	}

	public string ToStringDetailed()
	{
		string str = 
			"index count: " + _indices.Length + "\n" + 
			"mat name : " + material.name + "\n" +
			"isManual : " + manualUV + "\n" +
			"smoothing group: " + smoothingGroup + "\n";

		for(int i = 0; i < indices.Length; i+=3)
			str += "Tri " + i + ": " + _indices[i+0] + ", " +  _indices[i+1] + ", " +  _indices[i+2] + "\n";

		str += "Distinct Indices:\n";
		for(int i = 0; i < distinctIndices.Length; i++)
			str += distinctIndices[i] + ", ";

		return str;
	}
#endregion
}
}
