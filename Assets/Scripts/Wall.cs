using System;
using System.Linq;
using UnityEngine;

public class Wall : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    
    public Tile tileInner;
    public Tile tileOuter;
    public int theme;
    public int variant;
    public bool transparent;
    public bool open;
    public bool locked;
    public bool leftHanded;
    
    public bool dirty;
    
    public bool Explored { get; private set; }
    public bool Revealed { get; private set; }
    public bool Walkable { get; private set; }

    private bool _cacheExplored;
    private bool _cacheRevealed;
    private bool _cacheWalkable;

    private const int Themes = 2;
    private const int Variants = 2;
    private const float UnitW = 1f / 2 / Variants;
    private const float UnitH = 1f / Themes;
    private const float U16 = 1f / 16;
    private const float U18 = 1f / 18;

    private Vector3 Orientation => new Vector3(tileInner.x - tileOuter.x, 0, tileInner.y - tileOuter.y);

    private void Awake()
    {
        meshRenderer.enabled = false;
        locked = true;
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (dirty) Refresh();

        UpdateStaging();
    }

    private void Refresh()
    {
        meshFilter.mesh = GenerateRenderer();
        meshRenderer.enabled = Explored;
        var t = transform;
        var o = Orientation;
        t.localPosition = tileOuter.transform.position + 0.5f * o + tileOuter.altitude * U16 * Vector3.up;
        transform.LookAt(t.position + o);
        meshCollider.sharedMesh = GenerateCollider(tileInner.altitude * U16);
        
        gameObject.layer = transparent ? Scene.tilesLayer : Scene.blockerLayer;
        
        dirty = false;
    }

    private void UpdateStaging()
    {
        Explored = tileInner.explored || tileOuter.explored;
        Revealed = tileInner.revealed || tileOuter.revealed;
        Walkable = Revealed && !locked;

        if (_cacheExplored != Explored)
        {
            _cacheExplored = Explored;
            dirty = true;
        }

        if (_cacheRevealed != Revealed)
        {
            _cacheRevealed = Revealed;
            dirty = true;
        }

        if (!_cacheWalkable && Walkable)
        {
            _cacheWalkable = Walkable;
            var scene = tileInner.chunk.scene;
            tileOuter.explored = tileInner.explored = true;
            foreach (var tile in scene.AdjacentTiles(tileInner).Union(scene.AdjacentTiles(tileOuter)))
                tile.chunk.dirty = true;
        }
    }
    
    private Mesh GenerateRenderer()
    {
        var vertices = new Vector3[16];
        // inner
        vertices[ 0] = new Vector3(-0.5f + U16, 0, -U16);
        vertices[ 1] = new Vector3(-0.5f + U16, 1, -U16);
        vertices[ 2] = new Vector3(0.5f - U16, 0, -U16);
        vertices[ 3] = new Vector3(0.5f - U16, 1, -U16);
        // border
        vertices[ 4] = new Vector3(-0.5f + U16, 0, 0);
        vertices[ 5] = new Vector3(-0.5f, 0, 0);
        vertices[ 6] = new Vector3(-0.5f, 1, 0);
        vertices[ 7] = new Vector3(-0.5f + U16, 1, 0);
        vertices[ 8] = new Vector3(0.5f, 0, 0);
        vertices[ 9] = new Vector3(0.5f - U16, 0, 0);
        vertices[10] = new Vector3(0.5f - U16, 1, 0);
        vertices[11] = new Vector3(0.5f, 1, 0);
        // outer
        vertices[12] = new Vector3(-0.5f + U16, 0, U16);
        vertices[13] = new Vector3(-0.5f + U16, 1, U16);
        vertices[14] = new Vector3(0.5f - U16, 0, U16);
        vertices[15] = new Vector3(0.5f - U16, 1, U16);

        var initX = Revealed ? variant * UnitW : (variant + Variants) * UnitW;
        var initY = 1f - theme * UnitH;
        var uv = new Vector2[16];
        // inner  // outer
        uv[0] = uv[12] = new Vector2(initX + U16 * UnitW, initY - (1 - U18) * UnitH);
        uv[1] = uv[13] = new Vector2(initX + U16 * UnitW, initY - U18 * UnitH);
        uv[2] = uv[14] = new Vector2(initX + (1 - U16) * UnitW, initY - (1 - U18) * UnitH);
        uv[3] = uv[15] = new Vector2(initX + (1 - U16) * UnitW, initY - U18 * UnitH);
        // border
        uv[ 4] = new Vector2(initX + U16 * UnitW, initY - UnitH);
        uv[ 5] = new Vector2(initX, initY - (1 - U18) * UnitH);
        uv[ 6] = new Vector2(initX, initY - U18 * UnitH);
        uv[ 7] = new Vector2(initX + U16 * UnitW, initY);
        uv[ 8] = new Vector2(initX + UnitW, initY - (1 - U18) * UnitH);
        uv[ 9] = new Vector2(initX + (1 - U16) * UnitW, initY - UnitH);
        uv[10] = new Vector2(initX + (1 - U16) * UnitW, initY);
        uv[11] = new Vector2(initX + UnitW, initY - U18 * UnitH);
    
        var triangles = new int[84];
        triangles[ 0] = triangles[ 3] = triangles[ 6] = triangles[13] = triangles[16] = triangles[18] = 0;
        triangles[ 8] = triangles[ 9] = triangles[19] = triangles[22] = triangles[24] = 1;
        triangles[17] = triangles[20] = triangles[21] = triangles[31] = triangles[34] = 2;
        triangles[23] = triangles[26] = triangles[27] = triangles[35] = triangles[37] = triangles[40] = 3;
        triangles[ 1] = triangles[12] = triangles[44] = triangles[54] = 4;
        triangles[ 2] = triangles[ 4] = triangles[43] = triangles[47] = 5;
        triangles[ 5] = triangles[ 7] = triangles[10] = triangles[46] = triangles[50] = triangles[53] = 6;
        triangles[11] = triangles[25] = triangles[28] = triangles[52] = triangles[68] = triangles[71] = 7;
        triangles[32] = triangles[33] = triangles[36] = triangles[73] = triangles[75] = triangles[78] = 8;
        triangles[14] = triangles[15] = triangles[30] = triangles[55] = triangles[57] = triangles[72] = 9;
        triangles[29] = triangles[41] = triangles[70] = triangles[82] = 10; 
        triangles[38] = triangles[39] = triangles[79] = triangles[81] = 11;
        triangles[42] = triangles[45] = triangles[48] = triangles[56] = triangles[59] = triangles[60] = 12;
        triangles[49] = triangles[51] = triangles[62] = triangles[65] = triangles[66] = 13;
        triangles[58] = triangles[61] = triangles[63] = triangles[74] = triangles[77] = 14;
        triangles[64] = triangles[67] = triangles[69] = triangles[76] = triangles[80] = triangles[83] = 15;

        var mesh = new Mesh {vertices = vertices, uv = uv, triangles = triangles, name = World.NewId()};
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();
        return mesh;
    }
    
    private static Mesh GenerateCollider(float y)
    {
        y += 1;
        var vertices = new Vector3[16];
        // inner
        vertices[ 0] = new Vector3(-0.5f, -2, -U16);
        vertices[ 1] = new Vector3(-0.5f, y, -U16);
        vertices[ 2] = new Vector3(0.5f, -2, -U16);
        vertices[ 3] = new Vector3(0.5f, y, -U16);
        // border
        vertices[ 4] = new Vector3(-0.5f, -2, 0);
        vertices[ 5] = new Vector3(-0.5f, -2, 0);
        vertices[ 6] = new Vector3(-0.5f, y, 0);
        vertices[ 7] = new Vector3(-0.5f, y, 0);
        vertices[ 8] = new Vector3(0.5f, -2, 0);
        vertices[ 9] = new Vector3(0.5f, -2, 0);
        vertices[10] = new Vector3(0.5f, y, 0);
        vertices[11] = new Vector3(0.5f, y, 0);
        // outer
        vertices[12] = new Vector3(-0.5f, -2, U16);
        vertices[13] = new Vector3(-0.5f, y, U16);
        vertices[14] = new Vector3(0.5f, -2, U16);
        vertices[15] = new Vector3(0.5f, y, U16);
    
        var triangles = new int[84];
        triangles[ 0] = triangles[ 3] = triangles[ 6] = triangles[13] = triangles[16] = triangles[18] = 0;
        triangles[ 8] = triangles[ 9] = triangles[19] = triangles[22] = triangles[24] = 1;
        triangles[17] = triangles[20] = triangles[21] = triangles[31] = triangles[34] = 2;
        triangles[23] = triangles[26] = triangles[27] = triangles[35] = triangles[37] = triangles[40] = 3;
        triangles[ 1] = triangles[12] = triangles[44] = triangles[54] = 4;
        triangles[ 2] = triangles[ 4] = triangles[43] = triangles[47] = 5;
        triangles[ 5] = triangles[ 7] = triangles[10] = triangles[46] = triangles[50] = triangles[53] = 6;
        triangles[11] = triangles[25] = triangles[28] = triangles[52] = triangles[68] = triangles[71] = 7;
        triangles[32] = triangles[33] = triangles[36] = triangles[73] = triangles[75] = triangles[78] = 8;
        triangles[14] = triangles[15] = triangles[30] = triangles[55] = triangles[57] = triangles[72] = 9;
        triangles[29] = triangles[41] = triangles[70] = triangles[82] = 10; 
        triangles[38] = triangles[39] = triangles[79] = triangles[81] = 11;
        triangles[42] = triangles[45] = triangles[48] = triangles[56] = triangles[59] = triangles[60] = 12;
        triangles[49] = triangles[51] = triangles[62] = triangles[65] = triangles[66] = 13;
        triangles[58] = triangles[61] = triangles[63] = triangles[74] = triangles[77] = 14;
        triangles[64] = triangles[67] = triangles[69] = triangles[76] = triangles[80] = triangles[83] = 15;
        
        var mesh = new Mesh {vertices = vertices, triangles = triangles};
        mesh.Optimize ();
        mesh.RecalculateNormals ();
        return mesh;
    }
    
    #region Serialization
    
    [Serializable]
    public class Model
    {
        public int ix;
        public int iy;
        public int ox;
        public int oy;
        public int th;
        public int v;
        public int t;
        public int o;
        public int l;
        public int h;
    }
    
    public Model Serializable()
    {
        return new Model
        {
            ix = tileInner.x,
            iy = tileInner.y,
            ox = tileOuter.x, 
            oy = tileOuter.y,
            th = theme,
            v = variant,
            t =  transparent ? 1 : 0,
            o =  open ? 1 : 0,
            l =  locked ? 1 : 0,
            h =  leftHanded ? 1 : 0
        };
    }

    public void Deserialize(Model wall)
    {
        tileInner = World.instance.scene.Tile(wall.ix, wall.iy);
        tileOuter = World.instance.scene.Tile(wall.ox, wall.oy);
        theme = wall.th;
        variant = wall.v;
        transparent =  wall.t == 1;
        open =  wall.o == 1;
        locked =  wall.l == 1;
        leftHanded =  wall.h == 1;
        
        Refresh();
    }
    
    #endregion
}
