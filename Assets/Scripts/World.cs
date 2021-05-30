using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BardoUI;
using BardoUI.Chat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


public class World : MonoBehaviour
{
    public GameObject loadingImage;

    public static bool debugging = true;
    public const short ChunkSize = 16;
    public const float U = 1f / 16;

    public static string label;
    public static string id;
    public static bool loaded;
    public static bool playerIsMaster;
    public static string playerName;
    public static bool sharingPlayerVision = false;

    public Scene scene;

    public List<PlayerInfo> players = new List<PlayerInfo>();
    public List<SceneInfo> scenes = new List<SceneInfo>();
    public readonly Dictionary<string, Property.Info> tokenPropertiesInfo = new Dictionary<string, Property.Info>();

    private readonly List<Action> _actionsToDo = new List<Action>();
    private readonly List<Action> _actionsDone = new List<Action>();
    private string _actionsFromDate;
    private readonly object _shareActionsFrequency = new WaitForSecondsRealtime(1f);

    public static World instance;

    private void Awake()
    {
        instance = this;
        tokenPropertiesInfo.Add("showLabel", new Property.Info {type = Property.Types.Boolean});
        tokenPropertiesInfo.Add("label", new Property.Info {type = Property.Types.Text, control = true});
        tokenPropertiesInfo.Add("initiative", new Property.Info {type = Property.Types.Numeric, control = true});
        tokenPropertiesInfo.Add("isStatic", new Property.Info {type = Property.Types.Boolean});
        tokenPropertiesInfo.Add("hasBase", new Property.Info {type = Property.Types.Boolean});
        tokenPropertiesInfo.Add("baseSize", new Property.Info {type = Property.Types.Numeric});
        tokenPropertiesInfo.Add("baseColor", new Property.Info {type = Property.Types.Color});
        tokenPropertiesInfo.Add("baseAlfa", new Property.Info {type = Property.Types.Percentage});
        tokenPropertiesInfo.Add("hasBody", new Property.Info {type = Property.Types.Boolean});
        tokenPropertiesInfo.Add("bodySize", new Property.Info {type = Property.Types.Numeric});
        tokenPropertiesInfo.Add("bodyColor", new Property.Info {type = Property.Types.Color});
        tokenPropertiesInfo.Add("bodyAlfa", new Property.Info {type = Property.Types.Percentage});
        tokenPropertiesInfo.Add("bodyResource", new Property.Info {type = Property.Types.Text});
        tokenPropertiesInfo.Add("health", new Property.Info {type = Property.Types.Bar, control = true});
        tokenPropertiesInfo.Add("stamina", new Property.Info {type = Property.Types.Bar, control = true});
        tokenPropertiesInfo.Add("mana", new Property.Info {type = Property.Types.Bar, control = true});
        tokenPropertiesInfo.Add("vision", new Property.Info {type = Property.Types.Boolean});
        tokenPropertiesInfo.Add("light", new Property.Info {type = Property.Types.Numeric});
    }

    private void Start()
    {
        // // var file = GetResource<TextAsset>("Maps/test");
        // // var file = GetResource<TextAsset>("Maps/small");
        // // var file = GetResource<TextAsset>("Maps/large");
        // var file = GetResource<TextAsset>("Maps/json/save");
        //
        // // CreateSceneFromDonjon(file.text);
        // CreateSceneFromJson(file.text);

        if (id == null)
        {
            id = "00000000";
            
            playerName = "admin";
            const string playerPassword = "admin";
            playerIsMaster = true;
            
            // playerName = "Alex";
            // const string playerPassword = "123456";
            // playerIsMaster = false;
            
            Server.SetCredentials(playerName, playerPassword);
        }
        
        NavigationPanel.instance.gameObject.SetActive(playerIsMaster);
        StartCoroutine(LoadWorld(id));
        StartCoroutine(ShareActions());
    }

    public static IEnumerator SaveWorldProperty(string propertyName, string value) =>
        SaveWorldProperty(propertyName, new[] {value});

    public static IEnumerator SaveWorldProperty(string propertyName, IEnumerable<string> values)
    {
        var deleteUrl = $"{Server.BaseUrl}/world/{id}/property/{propertyName}/delete";
        var deleteRequest = Server.DeleteRequest(deleteUrl);
        while (!deleteRequest.isDone)
            yield return null;

        Server.GetResponse<Server.Response>(deleteRequest);

        foreach (var value in values)
        {
            var url = $"{Server.BaseUrl}/world/{id}/property/{propertyName}/add";
            var request = Server.PostRequest(url, value);
            while (!request.isDone)
                yield return null;

            Server.GetResponse<Server.Response>(request);
        }
    }

    public static IEnumerator DefaultWorldProperty(string propertyName, string defaultValue)
    {
        var url = $"{Server.BaseUrl}/world/campaign/{id}/property/{propertyName}/default";
        var request = Server.PostRequest(url, defaultValue);
        while (!request.isDone)
            yield return null;

        Server.GetResponse<Server.Response>(request);
    }

    [Serializable]
    public class WorldResponse : Server.Response
    {
        public WorldInfo world;
    }

    private IEnumerator LoadWorld(string worldId)
    {
        while (!Server.serverReady)
            yield return null;

        var worldUrl = $"{Server.BaseUrl}/world/{worldId}";
        var worldRequest = Server.GetRequest(worldUrl);
        while (!worldRequest.isDone)
            yield return null;

        var worldResponse = Server.GetResponse<WorldResponse>(worldRequest);
        var worldInfo = worldResponse.world;
        var worldProperties = worldInfo.properties;

        // get players
        players = worldInfo.players;

        // get active scene
        scenes = worldInfo.scenes;
        var activeSceneProperty = worldProperties.FirstOrDefault(p => p.name == "ACTIVE_SCENE");
        var activeSceneId = activeSceneProperty != null ? activeSceneProperty.values[0] : scenes[0].id;
        var sceneIds = scenes.Select(x => x.id).ToList();
        if (!sceneIds.Contains(activeSceneId))
        {
            Debug.LogWarning($"Active map (id={activeSceneId}) for user doesn't exist");
            activeSceneId = worldInfo.scenes[0].id;
            StartCoroutine(SaveWorldProperty("ACTIVE_SCENE", activeSceneId));
        }

        // load active scene
        StartCoroutine(LoadScene(activeSceneId));
        while (!scene.loaded)
            yield return null;

        loaded = true;
    }

    #region Scene

    public IEnumerator ChangeScene(string sceneId)
    {
        if (scene.id.Equals(sceneId))
            yield break;

        loaded = false;
        Debug.Log(sceneId);
        StartCoroutine(SaveWorldProperty("ACTIVE_SCENE", sceneId));
        scene.loaded = false;
        StartCoroutine(LoadScene(sceneId));
        while (!scene.loaded)
            yield return null;

        loaded = false;
    }

    [Serializable]
    private class SceneResponse : Server.Response
    {
        public Scene.Model scene;
    }

    [Serializable]
    public class ScenePropertiesResponse : Server.Response
    {
        public List<Scene.Property> properties = new List<Scene.Property>();
    }

    public IEnumerator LoadScene(string sceneId)
    {
        var sceneRequest = Server.GetRequest($"{Server.BaseUrl}/world/{id}/scene/{sceneId}");
        while (!sceneRequest.isDone)
            yield return null;

        var sceneResponse = Server.GetResponse<SceneResponse>(sceneRequest);
        scene.Deserialize(sceneResponse.scene);

        // get scene properties
        var request = Server.GetRequest($"{Server.BaseUrl}/world/{id}/scene/{sceneId}/properties");
        while (!request.isDone)
            yield return null;

        var response = Server.GetResponse<ScenePropertiesResponse>(request);
        scene.properties = response.properties;

        // get explored tiles
        var exploredProperty = scene.properties.FirstOrDefault(p => p.name == "EXPLORED");
        if (exploredProperty != null)
        {
            var exploredChars = exploredProperty.values[0].ToCharArray();
            for (var y = 0; y < scene.Height; y += 1)
            for (var x = 0; x < scene.Width; x += 1)
                scene.tiles[x, y].explored = exploredChars[x + y * scene.Width] == '1';
        }

        // get actions not saved
        var actionsRequest = Server.GetRequest($"{Server.BaseUrl}/world/{id}/actions/from/scene/{sceneId}");
        while (!actionsRequest.isDone)
            yield return null;

        var actionsResponse = Server.GetResponse<ActionsResponse>(actionsRequest);
        foreach (var action in actionsResponse.actions) ResolveAction(action);

        // get shared entity properties
        foreach (var token in scene.tokens) token.dirty = true;

        scene.loaded = true;
        _actionsFromDate = NowIsoDate();
    }

    #endregion

    #region Update

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
            // data = data.Replace(",\"a\":0", "");
            // data = data.Replace(",\"w\":0", "");
            // data = data.Replace(",\"t\":0", "");
            // data = data.Replace(",}", "}");
            sr.WriteLine(data);
            sr.Close();
        }

        loadingImage.SetActive(!loaded);

        UpdateMouse();
        UpdateActions();
    }

    public static bool MouseOverUi { get; private set; }

    public static GameObject CurrentSelectedGameObject { get; private set; }

    private static void UpdateMouse()
    {
        var current = EventSystem.current;
        MouseOverUi = current.IsPointerOverGameObject();
        CurrentSelectedGameObject = current.currentSelectedGameObject;
    }

    private void UpdateActions()
    {
        foreach (var action in _actionsToDo)
            ResolveAction(action);
        
        var actionsToDelete = _actionsToDo.Where(a => a.done).ToList();
        if (actionsToDelete.Any())
            _actionsToDo.Remove(actionsToDelete[0]);
    }
    
    #endregion

    #region Actions

    public static class ActionNames
    {
        // world actions
        public const string AddMessage = "AddMessage";

        // scene actions
        public const string ChangeScene = "ChangeScene";
        public const string RefreshPermissions = "RefreshPermissions";

        // tiles actions
        public const string ChangeTiles = "ChangeTiles";
        public const string ShowTiles = "ShowTiles";
        public const string HideTiles = "HideTiles";

        // token actions
        public const string ChangeToken = "ChangeToken";
        public const string DeleteToken = "DeleteToken";
    }


    [Serializable]
    private class ActionsRequestBody
    {
        public List<Action> actions = new List<Action>();
    }

    [Serializable]
    private class ActionsResponse : Server.Response
    {
        public List<Action> actions = new List<Action>();
    }

    private IEnumerator ShareActions()
    {
        while (!loaded)
            yield return null;

        while (Server.serverReady)
        {
            yield return _shareActionsFrequency;

            var data = new ActionsRequestBody {actions = _actionsDone};
            // var len = data.actions.Count;
            var url = $"{Server.BaseUrl}/world/{id}/actions/from/date/{_actionsFromDate}";
            var request = Server.PostRequest(url, data);
            while (!request.isDone)
                yield return null;
            
            var response = Server.GetResponse<ActionsResponse>(request, false);
            if (!response)
                continue;
            
            // _actionsDone.RemoveRange(0, len);
            _actionsDone.Clear();
            _actionsToDo.AddRange(response.actions);
            _actionsFromDate = response.date;
        }
    }
    
    public void RegisterAction(Action action)
    {
        _actionsDone.Add(action);
    }

    public void ResolveAction(Action action)
    {
        if (action.players.Any())
        {
            var player = players.FirstOrDefault(x => x.name == playerName);
            if (player != null && !action.players.Contains(player.id))
                return;
        }

        if (action.scene.Any() && action.scene != scene.id)
            return;

        try
        {
            switch (action.name)
            {
                // campaign actions
                case ActionNames.AddMessage:
                    var strings = action.strings;
                    Chat.instance.NewMessage(strings[0], strings[1], strings[2], strings[3]);
                    break;

                // map actions
                case ActionNames.ChangeScene:
                    StartCoroutine(ChangeScene(action.strings[0]));
                    break;
                case ActionNames.RefreshPermissions:
                    StartCoroutine(RefreshPermissionsCoroutine());
                    break;

                // tiles actions
                case ActionNames.ChangeTiles:
                    ChangeTiles(action.tiles);
                    break;
                case ActionNames.ShowTiles:
                    if (!playerIsMaster) ShowTiles(action.strings[0]);
                    break;
                case ActionNames.HideTiles:
                    if (!playerIsMaster) HideTiles(action.strings[0]);
                    break;

                // entity actions
                case ActionNames.ChangeToken:
                    foreach (var entity in action.tokens) ChangeToken(entity);
                    break;
                case ActionNames.DeleteToken:
                    foreach (var entity in action.tokens) DeleteToken(entity);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Action (name={action.name}, map={action.scene}) throws error: {e}");
        }
        finally
        {
            action.done = true;
        }
    }

    private void ChangeTiles(IEnumerable<Tile.Model> serializableTiles)
    {
        foreach (var tile in serializableTiles) scene.tiles[tile.x, tile.y].Deserialize(tile);

        foreach (var t in scene.tokens.Where(x => x.sharedVision)) t.dirty = true;
    }

    private void ShowTiles(string tileArray)
    {
        var charArray = tileArray.ToCharArray();
        for (var y = 0; y < scene.Height; y += 1)
        for (var x = 0; x < scene.Width; x += 1)
            if (charArray[x + y * scene.Width] == '1')
                scene.tiles[x, y].explored = true;

        foreach (var t in scene.tokens.Where(x => x.sharedVision)) t.dirty = true;
        scene.RefreshVision();
    }

    private void HideTiles(string tileArray)
    {
        var charArray = tileArray.ToCharArray();
        for (var y = 0; y < scene.Height; y += 1)
        for (var x = 0; x < scene.Width; x += 1)
            if (charArray[x + y * scene.Width] == '1')
                scene.tiles[x, y].explored = false;

        foreach (var t in scene.tokens.Where(x => x.sharedVision)) t.dirty = true;
        scene.RefreshVision();
    }

    private void ChangeToken(Token.Model serializableEntity)
    {
        var token = scene.tokens.FirstOrDefault(x => x.id == serializableEntity.id);
        if (token == null)
        {
            token = Instantiate(scene.tokenPrefab, scene.tokensParent).GetComponent<Token>();
            scene.tokens.Add(token);
        }

        token.Deserialize(serializableEntity);

        foreach (var t in scene.tokens.Where(x => x.sharedVision)) t.dirty = true;
    }

    private void DeleteToken(Token.Model serializableEntity)
    {
        var token = scene.tokens.FirstOrDefault(x => x.id == serializableEntity.id);
        if (token == null)
            return;

        scene.tokens.Remove(token);
        scene.selectedTokens.Remove(token);
        Destroy(token.gameObject);
    }

    private IEnumerator RefreshPermissionsCoroutine()
    {
        var request = Server.GetRequest($"{Server.BaseUrl}/world/campaign/{id}/map/{scene.id}/properties");
        while (!request.isDone)
            yield return null;

        var response = Server.GetResponse<ScenePropertiesResponse>(request);
        scene.properties = response.properties;

        // get shared entity properties
        foreach (var token in scene.tokens) token.RefreshPermissions();
    }

    #endregion

    public void ReturnToStartScreen()
    {
        SceneManager.LoadScene(0);
    }

    #region Scene Generation

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
            case "SDD": // stairs down 2
                tile.theme = 1;
                tile.altitude = -16;
                foreach (var unused in scene.AdjacentTiles(tile).Where(t => t && t.altitude == -8))
                    tile.altitude = -12;
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "SD": // stairs down 1
                tile.theme = 1;
                tile.altitude = -8;
                foreach (var unused in scene.AdjacentTiles(tile).Where(t => t && t.altitude == 0))
                    tile.altitude = -4;
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "F": // floor
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "SU": // stairs up 1
                tile.theme = 1;
                tile.altitude = 8;
                foreach (var unused in scene.AdjacentTiles(tile).Where(t => t && t.altitude == 0))
                    tile.altitude = 4;
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "SUU": // stairs up 2
                tile.theme = 1;
                tile.altitude = 16;
                foreach (var unused in scene.AdjacentTiles(tile).Where(t => t && t.altitude == 8))
                    tile.altitude = 12;
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "DT": // door oriented to north
            case "DB": // door oriented to north trapped
            case "DL": // door oriented to est
            case "DR": // door oriented to est trapped
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "DPT": // portcullis to north
            case "DPB": // portcullis to north
            case "DPL": // portcullis to est
            case "DPR": // portcullis to est
                tile.walkable = true;
                tile.transparent = true;
                break;
            case "DST": // door oriented to north secret
            case "DSB": // door oriented to north secret
            case "DSL": // door oriented to est secret
            case "DSR": // door oriented to est secret
                tile.altitude = 16;
                tile.walkable = true;
                break;
            case "": // wall
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

    #endregion

    #region Utils

    public static T GetResource<T>(string path)
    {
        var res = Resources.LoadAll(path, typeof(T)).Cast<T>().ToArray();
        if (!res.Any()) throw new Exception($"Resource not found: {path}");
        return res[0];
    }

    public static string NewId() => Guid.NewGuid().ToString().Substring(0, 8);

    public static string ToBase64(string data, bool reverse = false) => !reverse
        ? Convert.ToBase64String(Encoding.UTF8.GetBytes(data))
        : Encoding.UTF8.GetString(Convert.FromBase64String(data));


    public static string Humanize(string text)
    {
        return new CultureInfo("en-US", false).TextInfo.ToTitleCase(
            Regex.Replace(Regex.Replace(text,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2").Replace('_', ' '));
    }

    public static string NowIsoDate()
    {
        var localTime = DateTime.Now;
        var localTimeAndOffset = new DateTimeOffset(localTime, TimeZoneInfo.Local.GetUtcOffset(localTime));
        var str = localTimeAndOffset.ToString("O");
        return str.Substring(0, 26) + str.Substring(27);
    }

    #endregion
}