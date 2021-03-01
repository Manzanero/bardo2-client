
using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public MeshCollider meshCollider;
    
    public Chunk chunk;
    public int x;
    public int y;
    
    public int theme;
    public int top;
    public int s;
    public int e;
    public int n;
    public int w;
    public int altitude;
    public bool walkable;
    public bool transparent;
    
    public bool explored;
    public bool revealed;
    
    public void Refresh()
    {
        transform.position = new Vector3(x, 0, y);
        meshCollider.sharedMesh = CreateCube(altitude / 16f);
        gameObject.layer = transparent ? Scene.tilesLayer : Scene.blockerLayer;
    }

    private static Mesh CreateCube(float y) {
        Vector3[] vertices = {
            new Vector3 (-0.5f, -2, -0.5f), new Vector3 (0.5f, -2, -0.5f), 
            new Vector3 (0.5f, y, -0.5f), new Vector3 (-0.5f, y, -0.5f), 
            new Vector3 (-0.5f, y, 0.5f), new Vector3 (0.5f, y, 0.5f),
            new Vector3 (0.5f, -2, 0.5f), new Vector3 (-0.5f, -2, 0.5f)
        };
        int[] triangles = {0, 2, 1, 0, 3, 2, 2, 3, 4, 2, 4, 5, 1, 2, 5, 1, 5, 6, 0, 7, 4, 0, 4, 3, 5, 4, 7, 5, 7, 6};
        var mesh = new Mesh {vertices = vertices, triangles = triangles};
        mesh.Optimize ();
        mesh.RecalculateNormals ();
        return mesh;
    }

    [Serializable]
    public class Model
    {
        public int th;
        public int v;
        public int a;
        public int w;
        public int t;
    }
    
    public Model Serializable()
    {
        return new Model
        {
            th = theme,
            v = int.Parse($"{top}{s}{e}{n}{w}"),
            a = altitude,
            w = walkable ? 1 : 0,
            t = transparent ? 1 : 0
        };
    }
    
    public void Deserialize(Model model)
    {
        theme = model.th;
        var v = model.v.ToString().PadLeft(5, '0');
        top = int.Parse(v.Substring(0, 1));
        s = int.Parse(v.Substring(0, 1));
        e = int.Parse(v.Substring(1, 1));
        n = int.Parse(v.Substring(2, 1));
        w = int.Parse(v.Substring(3, 1));
        altitude = model.a;
        walkable = model.w == 1;
        transparent = model.t == 1;
        
        Refresh();
    }
}
