namespace UnityEngine.ProBuilder
{
    [System.Serializable]
    public abstract class Shape
    {
        public abstract void RebuildMesh(ProBuilderMesh mesh, Vector3 size);
    }
}