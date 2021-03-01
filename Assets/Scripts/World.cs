using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;


public class World : MonoBehaviour
{
    public static bool debugging = false;
    public const short ChunkSize = 16;
    public const float U = 1f / 16;

    public static bool loading = false;
    public static string label = "noname";
    public static string id = NewId();
    public static bool playerIsMaster = true;
    public static string playerName = "noname";
    public static bool sharingPlayerVision = false;
    
    public Scene scene;
    
    public List<PlayerInfo> players;
    public List<SceneInfo> scenes;
    public List<TokenProperty> tokenProperties;
    
    public static bool MouseOverUi { get; private set; }
    public static GameObject CurrentSelectedGameObject { get; private set; }
    
    public static World instance;
    
    private void Awake()
    {
        instance = this;
        tokenProperties.AddRange(new []
        {
            new TokenProperty {id = "showLabel", label = "Show Label", type = TokenPropertiesTypes.BOOLEAN},
            new TokenProperty {id = "label", label = "Label", type = TokenPropertiesTypes.TEXT, control = true},
            new TokenProperty {id = "initiative", label = "Initiative", type = TokenPropertiesTypes.NUMERIC, control = true},
            new TokenProperty {id = "isStatic", label = "Static", type = TokenPropertiesTypes.BOOLEAN},
            new TokenProperty {id = "hasBase", label = "Show Base", type = TokenPropertiesTypes.BOOLEAN},
            new TokenProperty {id = "baseSize", label = "Token Size", type = TokenPropertiesTypes.NUMERIC},
            new TokenProperty {id = "baseColor", label = "Token Color", type = TokenPropertiesTypes.COLOR},
            new TokenProperty {id = "baseAlfa", label = "Token Alfa", type = TokenPropertiesTypes.NUMERIC},
            new TokenProperty {id = "hasBody", label = "Show Body", type = TokenPropertiesTypes.BOOLEAN},
            new TokenProperty {id = "baseSize", label = "Base Size", type = TokenPropertiesTypes.NUMERIC},
            new TokenProperty {id = "bodyColor", label = "Body Color", type = TokenPropertiesTypes.COLOR},
            new TokenProperty {id = "bodyAlfa", label = "Body Alfa", type = TokenPropertiesTypes.NUMERIC},
            new TokenProperty {id = "bodyResource", label = "Body Res.", type = TokenPropertiesTypes.TEXT},
            new TokenProperty {id = "health", label = "Health", type = TokenPropertiesTypes.BAR, control = true},
            new TokenProperty {id = "stamina", label = "Stamina", type = TokenPropertiesTypes.BAR, control = true},
            new TokenProperty {id = "mana", label = "Mana", type = TokenPropertiesTypes.BAR, control = true},
            new TokenProperty {id = "vision", label = "Vision", type = TokenPropertiesTypes.BOOLEAN},
            new TokenProperty {id = "light", label = "Light", type = TokenPropertiesTypes.NUMERIC}
        });
    }

    private void Start()
    {
        // var file = GetResource<TextAsset>("Maps/test");
        var file = GetResource<TextAsset>("Maps/small");
        // var file = GetResource<TextAsset>("Maps/large");
        // var file = GetResource<TextAsset>("Maps/json/save");
        
        CreateSceneFromDonjon(file.text);
        // CreateSceneFromJson(file.text);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var chunk in scene.chunks) chunk.dirty = true;
            foreach (var wall in scene.walls) wall.dirty = true;
            foreach (var token in scene.tokens) token.dirty = true;
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            const string fileName = "C:/workspace/unity/bardo2/bardo2-client/Assets/Resources/Maps/json/save.txt";
            if (File.Exists(fileName)) File.Delete(fileName);
            var sr = File.CreateText(fileName);
            var data = JsonUtility.ToJson(scene.Serialize());
            data = data.Replace("\"a\":0,", "");
            data = data.Replace("\"w\":0,", "");
            data = data.Replace("\"t\":0", "");
            data = data.Replace(",}", "}");
            sr.WriteLine(data);
            sr.Close();
        }
        
        UpdateMouse();
    }
    
    private void UpdateMouse()
    {
        var current = EventSystem.current;
        MouseOverUi = current.IsPointerOverGameObject();
        CurrentSelectedGameObject = current.currentSelectedGameObject;
    }

    private void CreateSceneFromJson(string textMap)
    {
        var serializable = new Scene.Model();
        JsonUtility.FromJsonOverwrite(textMap, serializable);
        scene.Deserialize(serializable);
    }

    private void CreateSceneFromDonjon(string textMap)
        {
        // var a = DateTime.Now;
        
        var fileLines = textMap.Split('\n').Reverse().ToArray();
        var fileHeight = fileLines.Length;
        var fileWidth = fileLines[0].Split('\t').Length;
        var chunkWidth = 2 * fileWidth % ChunkSize == 0 ? 2 * fileWidth / ChunkSize : 2 * fileWidth / ChunkSize + 1;
        var chunkHeight = 2 * fileHeight % ChunkSize == 0 ? 2 * fileHeight / ChunkSize : 2 * fileHeight / ChunkSize + 1;

        scene.tiles = new Tile[2 * fileWidth, 2 * fileHeight];
        var chunks = scene.chunks = new Chunk[chunkWidth, chunkHeight];

        NewChunks(2 * fileWidth, 2 * fileHeight);

        for (var fy = 0; fy < fileHeight; fy++)
        {
            var line = fileLines[fy];
            var cols = line.Split('\t');
            for (var fx = 0; fx < fileWidth; fx++)
            {
                var c = cols[fx];
                scene.tiles[fx * 2, fy * 2] = NewTile(scene, fx * 2, fy * 2, c);
                scene.tiles[fx * 2 + 1, fy * 2] = NewTile(scene, fx * 2 + 1, fy * 2, c);
                scene.tiles[fx * 2, fy * 2 + 1] = NewTile(scene, fx * 2, fy * 2 + 1, c);
                scene.tiles[fx * 2 + 1, fy * 2 + 1] = NewTile(scene, fx * 2 + 1, fy * 2 + 1, c);

                if (c.StartsWith("D")) NewWalls(fx, fy, cols[fx - 1], c);
            }
        }
        
        // Debug.Log((DateTime.Now - a).TotalMilliseconds);
    }

    public void NewChunks(int width, int height)
    {
        var chunkWidth = width % ChunkSize == 0 ? width / ChunkSize : width / ChunkSize + 1;
        var chunkHeight = height % ChunkSize == 0 ? height / ChunkSize : height / ChunkSize + 1;
        scene.chunks = new Chunk[chunkWidth, chunkHeight];
        for (var y = 0; y < chunkHeight; y++)
        for (var x = 0; x < chunkWidth; x++)
        {
            var chunk = Instantiate(scene.chunkPrefab, scene.chunksParent).GetComponent<Chunk>();
            chunk.scene = scene;
            chunk.tileRangeXStart = x * ChunkSize;
            chunk.tileRangeXEnd = Mathf.Min((x + 1) * ChunkSize, scene.Width);
            chunk.tileRangeYStart = y * ChunkSize;
            chunk.tileRangeYEnd = Mathf.Min((y + 1) * ChunkSize, scene.Height);
            chunk.name = $"Chunk-{chunk.tileRangeXStart}-{chunk.tileRangeYStart}";
            scene.chunks[x, y] = chunk;
        }
    }
    
    private static Tile NewTile(Scene scene, int x, int y, string codename)
    {
        var tile = Instantiate(scene.tilePrefab, new Vector3(x, 0, y), Quaternion.identity, 
            scene.tilesParent).GetComponent<Tile>();
        tile.name = $"Tile-{x}-{y}";
        tile.chunk = scene.chunks[x / ChunkSize, y / ChunkSize];
        tile.x = x;
        tile.y = y;
        tile.theme = 0;
        tile.top = RandomVariant();
        tile.s = RandomVariant();
        tile.w = RandomVariant();
        tile.n = RandomVariant();
        tile.e = RandomVariant();
        
        switch (codename)
        {
            case "SDD":  // stairs down 2
                tile.theme = 1;
                tile.altitude = -16;
                foreach (var unused in scene.AdjacentTiles(tile).Where(t => t && t.altitude == -8))
                    tile.altitude = -12;
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "SD":  // stairs down 1
                tile.theme = 1;
                tile.altitude = -8;
                foreach (var unused in scene.AdjacentTiles(tile).Where(t => t && t.altitude == 0))
                    tile.altitude = -4;
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "F":   // floor
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "SU":   // stairs up 1
                tile.theme = 1;
                tile.altitude = 8;
                foreach (var unused in scene.AdjacentTiles(tile).Where(t => t && t.altitude == 0))
                    tile.altitude = 4;
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "SUU":   // stairs up 2
                tile.theme = 1;
                tile.altitude = 16;
                foreach (var unused in scene.AdjacentTiles(tile).Where(t => t && t.altitude == 8))
                    tile.altitude = 12;
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "DT":  // door oriented to north
            case "DB":  // door oriented to north trapped
            case "DL":  // door oriented to est
            case "DR":  // door oriented to est trapped
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "DPT":  // portcullis to north
            case "DPB":  // portcullis to north
            case "DPL":  // portcullis to est
            case "DPR":  // portcullis to est
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "DST":  // door oriented to north secret
            case "DSB":  // door oriented to north secret
            case "DSL":  // door oriented to est secret
            case "DSR":  // door oriented to est secret
                tile.altitude = 16;
                tile.walkable = true;
                break;
            case "":  // wall
                tile.altitude = 16;
                break;
            default:
                tile.theme = 3;
                tile.altitude = 0;
                break;
        }

        if (playerIsMaster)
        {
            tile.explored = true;
            tile.revealed = true;
        }

        tile.Refresh();
        return tile;
    }

    private void NewWalls(int fx, int fy, string prevCodename, string codename)
    {
        Wall wall1;
        Wall wall2;
        // portcullis
        if (new[] {"DPT", "DPB", "DPL", "DPR"}.Contains(codename))
        {
            if (prevCodename == "F")
            {
                wall1 = Instantiate(scene.wallPrefab, scene.wallsParent).GetComponent<Wall>();
                wall2 = Instantiate(scene.wallPrefab, scene.wallsParent).GetComponent<Wall>();
                wall1.name = $"Wall-{2 * fx}-{2 * fy}--{2 * fx + 1}-{2 * fy}";
                wall2.name = $"Wall-{2 * fx}-{2 * fy + 1}--{2 * fx + 1}-{2 * fy + 1}";
                wall1.tileInner = scene.Tile(2 * fx, 2 * fy);
                wall1.tileOuter = scene.Tile(2 * fx + 1, 2 * fy);
                wall2.tileInner = scene.Tile(2 * fx, 2 * fy + 1);
                wall2.tileOuter = scene.Tile(2 * fx + 1, 2 * fy + 1);
            }
            else
            {
                wall1 = Instantiate(scene.wallPrefab, scene.wallsParent).GetComponent<Wall>();
                wall2 = Instantiate(scene.wallPrefab, scene.wallsParent).GetComponent<Wall>();
                wall1.name = $"Wall-{2 * fx}-{2 * fy}--{2 * fx}-{2 * fy + 1}";
                wall2.name = $"Wall-{2 * fx + 1}-{2 * fy}--{2 * fx + 1}-{2 * fy + 1}";
                wall1.tileInner = scene.Tile(2 * fx, 2 * fy);
                wall1.tileOuter = scene.Tile(2 * fx, 2 * fy + 1);
                wall2.tileInner = scene.Tile(2 * fx + 1, 2 * fy);
                wall2.tileOuter = scene.Tile(2 * fx + 1, 2 * fy + 1);
            }
            wall1.theme = wall2.theme = 1;
            wall1.transparent = wall2.transparent = true;
        }
        else
        {
            if (prevCodename == "F")
            {
                wall1 = Instantiate(scene.wallPrefab, scene.wallsParent).GetComponent<Wall>();
                wall2 = Instantiate(scene.wallPrefab, scene.wallsParent).GetComponent<Wall>();
                wall1.name = $"Wall-{2 * fx}-{2 * fy}--{2 * fx + 1}-{2 * fy}";
                wall2.name = $"Wall-{2 * fx}-{2 * fy + 1}--{2 * fx + 1}-{2 * fy + 1}";
                wall1.tileInner = scene.Tile(2 * fx, 2 * fy);
                wall1.tileOuter = scene.Tile(2 * fx + 1, 2 * fy);
                wall2.tileInner = scene.Tile(2 * fx, 2 * fy + 1);
                wall2.tileOuter = scene.Tile(2 * fx + 1, 2 * fy + 1);
            }
            else
            {
                wall1 = Instantiate(scene.wallPrefab, scene.wallsParent).GetComponent<Wall>();
                wall2 = Instantiate(scene.wallPrefab, scene.wallsParent).GetComponent<Wall>();
                wall1.name = $"Wall-{2 * fx}-{2 * fy}--{2 * fx}-{2 * fy + 1}";
                wall2.name = $"Wall-{2 * fx + 1}-{2 * fy}--{2 * fx + 1}-{2 * fy + 1}";
                wall1.tileInner = scene.Tile(2 * fx, 2 * fy);
                wall1.tileOuter = scene.Tile(2 * fx, 2 * fy + 1);
                wall2.tileInner = scene.Tile(2 * fx + 1, 2 * fy);
                wall2.tileOuter = scene.Tile(2 * fx + 1, 2 * fy + 1);
            }
            wall1.theme = wall2.theme = 0;
        }
        wall1.name = NewId();
        wall2.name = NewId();
        scene.walls.Add(wall1);
        scene.walls.Add(wall2);
    }
    
    private static int RandomVariant()
    {
        var rand = Random.Range(0, 11);
        var variant = 0;
        switch (rand)
        {
            case 0:
                variant = 3;
                break;
            case 1:
            case 2:
                variant = 2;
                break;
            case 3:
            case 4:
            case 5:
                variant = 1;
                break;
        }

        return variant;
    }

    public static T GetResource<T>(string path)
    {
        var res = Resources.LoadAll(path, typeof(T)).Cast<T>().ToArray();
        if (!res.Any()) throw new Exception($"Resource not found: {path}");
        return res[0];
    }

    public static string NewId() => Guid.NewGuid().ToString().Substring(0, 8);

    public static string ToBase64(string data, bool reverse = false) => !reverse ? 
        Convert.ToBase64String(Encoding.UTF8.GetBytes(data)) :
        Encoding.UTF8.GetString(Convert.FromBase64String(data));

    public static string NowIsoDate()
    {
        var localTime = DateTime.Now;
        var localTimeAndOffset = new DateTimeOffset(localTime, TimeZoneInfo.Local.GetUtcOffset(localTime));
        var str = localTimeAndOffset.ToString("O");
        return str.Substring(0, 26) + str.Substring(27);
    }
}
