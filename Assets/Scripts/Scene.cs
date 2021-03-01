using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class Scene : MonoBehaviour
{
    public Transform chunksParent;
    public Transform wallsParent;
    public Transform tilesParent;
    public Transform tokensParent;
    public Transform aurasParent;
    public Transform boundsParent;
    public GameObject wallPrefab;
    public GameObject chunkPrefab;
    public GameObject tilePrefab;
    public GameObject tokenPrefab;
    public GameObject auraPrefab;

    public World world;
    public string id;
    public string label;
    public Chunk[,] chunks;
    public Tile[,] tiles;
    public List<Wall> walls;
    public List<Token> tokens;
    public List<Aura> auras;
    
    public Vector3 mousePosition;
    public bool mouseOverTile;
    public Tile mouseTile;
    public Token mouseToken;
    public List<Tile> tilesRevealed = new List<Tile>();
    public readonly RangeObservableCollection<Tile> selectedTiles = new RangeObservableCollection<Tile>();
    public readonly RangeObservableCollection<Token> selectedTokens = new RangeObservableCollection<Token>();
    public List<Property> properties;

    [Serializable]
    public class Property
    {
        public string name; 
        public List<string> values;
    }

    public int Width => tiles.GetLength(0);
    public int Height => tiles.GetLength(1);

    public Tile Tile(int x, int y) => x >= Width || x < 0 || y >= Height || y < 0 ? null : tiles[x, y];

    public IEnumerable<Tile> AdjacentTiles(Tile tile) => new[] 
        {Tile(tile.x, tile.y - 1), Tile(tile.x - 1, tile.y), Tile(tile.x, tile.y + 1), Tile(tile.x + 1, tile.y)};

    public bool dirty;
    
    private Camera _mainCamera;
    private List<Tile> _allTilesInLight = new List<Tile>();
    private List<Tile> _allTilesInVision = new List<Tile>();
    private List<Transform> _cachedTilesTransform = new List<Transform>();
    private List<Tile> _cachedTiles = new List<Tile>();
    
    public static int tilesLayer;
    public static int blockerLayer;
    public static int mapLayerMask;

    private void Awake()
    {
        selectedTiles.AllowDuplicates = false;
        selectedTokens.AllowDuplicates = false;
        _mainCamera = Camera.main;
        tilesLayer = LayerMask.NameToLayer("Tiles");
        blockerLayer = LayerMask.NameToLayer("Blockers");
        mapLayerMask = LayerMask.GetMask("Tiles", "Blockers");
        Clear();
    }

    private void Clear()
    {
        if (chunks != null) Array.Clear(chunks, 0, chunks.Length);
        if (tiles != null) Array.Clear(tiles, 0, tiles.Length);
        walls?.Clear();
        tokens?.Clear();
        auras?.Clear();
        foreach (Transform child in chunksParent) Destroy(child.gameObject);
        foreach (Transform child in tilesParent) Destroy(child.gameObject);
        foreach (Transform child in wallsParent) Destroy(child.gameObject);
        foreach (Transform child in tokensParent) Destroy(child.gameObject);
        foreach (Transform child in aurasParent) Destroy(child.gameObject);
        foreach (Transform child in boundsParent) Destroy(child.gameObject);
    }
    
    public void RefreshVision()
    {
        dirty = true;
    }

    private void Start()
    {
        CreateBounds();
    }
    
    private void CreateBounds()
    {
        foreach (Transform b in boundsParent) Destroy(b.gameObject);
        var s = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        var w = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        var n = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        var e = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        s.position = new Vector3(Width / 2f - 0.5f, 0, -0.5f);
        w.position = new Vector3(-0.5f, 0, Height / 2f - 0.5f);
        n.position = new Vector3(Width / 2f - 0.5f, 0, Height - 0.5f);
        e.position = new Vector3(Width - 0.5f - 0.5f, 0, Height / 2f - 0.5f);
        s.localScale = new Vector3(Width, 2, 0);
        w.localScale = new Vector3(Height, 2, 0);
        n.localScale = new Vector3(Width, 2, 0);
        e.localScale = new Vector3(Height, 2, 0);
        w.localRotation = Quaternion.Euler(0, 90, 0);
        n.localRotation = Quaternion.Euler(0, 180, 0);
        e.localRotation = Quaternion.Euler(0, 270, 0);
        s.parent = w.parent = n.parent = e.parent = boundsParent;
    }

    private void Update()
    {
        if (dirty) Refresh();
        
        UpdateMouse();
    }

    private void Refresh()
    {
        UpdateVision();

        dirty = false;
    }

    private void UpdateMouse()
    {
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Tile tile;
        if (Physics.Raycast(ray, out var rayCastHit, 300f))
            tile = rayCastHit.transform.GetComponent<Tile>();
        else
        {
            mouseOverTile = false;
            return;
        }
        
        mouseOverTile = (bool) tile;
        
        if (mouseOverTile)
        {
            mousePosition = rayCastHit.point;
            mouseTile = tile;
        }
        else
        {
            if (!Physics.Raycast(ray, out rayCastHit, 300f, mapLayerMask))
                return;
        
            mousePosition = rayCastHit.point;
            mouseTile = rayCastHit.transform.GetComponent<Tile>();
        }
            
    }
    
    private void UpdateVision()
    {
        if (World.playerIsMaster && !World.sharingPlayerVision)
            return;
        
        _allTilesInLight.Clear();
        _allTilesInLight = tokens
            .Select(x => x.tilesInLight)
            .Aggregate(_allTilesInLight, (x, y) => x.Union(y).ToList());

        _allTilesInVision.Clear();
        _allTilesInVision = tokens
            .Where(x => x.sharedVision)
            .Select(x => x.tilesInVision)
            .Aggregate(_allTilesInVision, (x, y) => x.Union(y).ToList());
        
        var tilesToReveal = _allTilesInLight.Intersect(_allTilesInVision).ToList();

        foreach (var tile in tilesRevealed.Except(tilesToReveal))
        {
            tile.chunk.dirty = true;
            foreach (var extraTiles in AdjacentTiles(tile)) extraTiles.chunk.dirty = true;
            tile.revealed = false;
        }
        foreach (var tile in tilesToReveal.Except(tilesRevealed))
        {
            tile.chunk.dirty = true;
            tile.explored = true;
            tile.revealed = true;
        }
        tilesRevealed = tilesToReveal;
    }
    
    public IEnumerable<Tile> TilesInRange(Tile tile, float range)
    {
        // var a = System.DateTime.Now;
        
        const int rayCount = 180;
        const float angleIncrease = 360f / rayCount;
        var angle = 360f;
        var origin = new Vector3(tile.x, -1.05f, tile.y);
        var rayCastHits = new RaycastHit[(int) range * 2]; 

        _cachedTilesTransform.Clear();
        _cachedTiles.Clear();

        // get ground transforms in sight
        for (var i = 0; i <= rayCount; i++)
        {
            var hits = Physics.RaycastNonAlloc(origin, VectorFromAngle(angle), 
                rayCastHits, range, mapLayerMask);
            var obstacleDistance = range;
            
            for (var j = 0; j < hits; j++)
            {
                var rayCastHitDistance = rayCastHits[j].distance;
                if (rayCastHitDistance <= obstacleDistance &&
                    rayCastHits[j].transform.gameObject.layer == blockerLayer)
                    obstacleDistance = rayCastHitDistance;
            }
            for (var j = 0; j < hits; j++)
                if (rayCastHits[j].distance < obstacleDistance) 
                    _cachedTilesTransform.Add(rayCastHits[j].transform);
            
            angle -= angleIncrease;
        }
        
        _cachedTilesTransform = _cachedTilesTransform.Distinct().ToList();
        
        // add current tile transform
        _cachedTilesTransform.Add(tile.transform);
        
        // get tiles
        foreach (var t in _cachedTilesTransform
            .Select(tileTransform => tileTransform.GetComponent<Tile>())) 
            _cachedTiles.Add(t);
        
        // remove duplicates
        _cachedTiles = _cachedTiles.Distinct().ToList();
        _cachedTiles.Remove(null);
        
        // Debug.Log((System.DateTime.Now - a).TotalMilliseconds);

        return _cachedTiles;
    }
    
    private static Vector3 VectorFromAngle(float angle)
    {
        var angleRad = Mathf.Deg2Rad * angle;
        return new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
    }
    
    #region Serialization
    
    [Serializable]
    public class Model
    {
        public string id;
        public string label;
        public Vector2Int size;
        public List<Tile.Model> tiles;
        public List<Wall.Model> walls;
        public List<Token.Model> tokens;
    }
    
    public Model Serialize()
    {
        var serializableTiles = new List<Tile.Model>();
        for (var y = 0; y < Height; y += 1) 
        for (var x = 0; x < Width; x += 1)
            serializableTiles.Add(tiles[x, y].Serializable());

        return new Model
        {
            id = id,
            label = label,
            size = new Vector2Int(Width, Height),
            tiles = serializableTiles,
            walls = walls.Select(wall => wall.Serializable()).ToList(),
            tokens = tokens.Select(entity => entity.Serialize()).ToList()
        };
    }

    public void Deserialize(Model model)
    {
        Clear();
        id = model.id;
        label = model.label;
        var width = model.size.x;
        var height = model.size.y;
        tiles = new Tile[width, height];

        world.NewChunks(width, height);

        for (var i = 0; i < model.tiles.Count; i++)
        {
            var tileModel = model.tiles[i];
            var tile = Instantiate(tilePrefab, tilesParent).GetComponent<Tile>();
            tile.x = i % width;
            tile.y = i / width;
            tile.name = $"Tile-{tile.x}-{tile.y}";
            tile.chunk = chunks[tile.x / World.ChunkSize, tile.y / World.ChunkSize];
            tile.Deserialize(tileModel);
            tile.explored = tile.revealed = World.playerIsMaster;
            tiles[tile.x, tile.y] = tile;
        }

        foreach (var wallModel in model.walls)
        {
            var wall = Instantiate(wallPrefab, wallsParent).GetComponent<Wall>();
            wall.name = $"Wall-{wallModel.ix}-{wallModel.iy}--{wallModel.ox}-{wallModel.oy}";
            wall.Deserialize(wallModel);
            walls.Add(wall);
        }

        foreach (var tokenModel in model.tokens)
        {
            var token = Instantiate(tokenPrefab, tokensParent).GetComponent<Token>();
            token.Deserialize(tokenModel);
            token.name = token.label;
            tokens.Add(token);
        }
    }
    
    #endregion

}
