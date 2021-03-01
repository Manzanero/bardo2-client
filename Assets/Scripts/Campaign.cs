﻿#pragma warning disable 0649

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Campaign : MonoBehaviour
{
    public Transform mapsParent;

    public World world;
    
    public bool loaded;
    
    private World _world;
    private string _actionsFromDate;
    private readonly WaitForSecondsRealtime _updateActionsPeriod = new WaitForSecondsRealtime(1f);
    private bool _loadingMap;


    private void Start()
    {
        _world = World.instance;
        StartCoroutine(LoadCampaign());
        // StartCoroutine(UpdateActions());
    }
    
    public IEnumerator SaveCampaignProperty(string propertyName, string propertyValue)
    {
        var url = $"{Server.baseUrl}/world/campaign/{World.id}/property/{propertyName}/save";
        var request = Server.PostRequest(url, propertyValue);
        while (!request.isDone)
            yield return null;
        
        Server.GetResponse<Server.Response>(request);
    }
    
    public IEnumerator DefaultProperty(string propertyName, string propertyValue)
    {
        var url = $"{Server.baseUrl}/world/campaign/{World.id}/property/{propertyName}/default";
        var request = Server.PostRequest(url, propertyValue);
        while (!request.isDone)
            yield return null;
        
        Server.GetResponse<Server.Response>(request);
    }

    [Serializable] public class CampaignResponse : Server.Response
    {
        public CampaignData campaign;
        
        [Serializable] public class CampaignData
        {
            public List<CampaignProperty> properties;
            public List<SceneInfo> maps;
            public List<PlayerInfo> players;
            
            [Serializable] public class CampaignProperty
            {
                public string name;
                public string value;
            }
        }
    }

    private IEnumerator LoadCampaign()
    {
        while (!Server.serverReady)
            yield return null;
        
        var url = $"{Server.baseUrl}/world/campaign/{World.id}";
        var campaignRequest = Server.GetRequest(url);
        while (!campaignRequest.isDone) 
            yield return null;
        
        var campaignResponse = Server.GetResponse<CampaignResponse>(campaignRequest);
        var campaignInfo = campaignResponse.campaign; 
        var properties = campaignInfo.properties;
        
        // get players
        _world.players = campaignInfo.players;

        // get active map
        _world.scenes = campaignInfo.maps;
        var activeMapProperty = properties.FirstOrDefault(p => p.name == "ACTIVE_MAP");
        var activeMapName = activeMapProperty != null ? activeMapProperty.value : _world.scenes[0].id;
        var mapIds = _world.scenes.Select(x => x.id).ToList();
        if (!mapIds.Contains(activeMapName))
        {
            Debug.LogWarning($"Active map (name={activeMapName}) for user doesn't exist");
            activeMapName = campaignInfo.maps[0].id;
        }
        
        // // load active map
        // StartCoroutine(LoadMap(activeMapName));
        // while (Map(activeMapName) == null) 
        //     yield return null;
        //
        // world.scene = Map(activeMapName);
        // loaded = true;
    }

    // #region Maps
    //
    // public IEnumerator ChangeActiveMap(string mapId)
    // {
    //     World.Loading = true;
    //     var fromMap = activeMap;
    //     if (fromMap && fromMap.id.Equals(mapId))
    //         yield break;
    //     
    //     if (fromMap)
    //     {
    //         maps.Remove(fromMap);
    //         Destroy(fromMap.gameObject);
    //     }
    //     
    //     Debug.Log(mapId);
    //     StartCoroutine(LoadMap(mapId));
    //     while (!Map(mapId)) yield return null;
    //     
    //     StartCoroutine(SaveCampaignProperty("ACTIVE_MAP", mapId));
    //     
    //     activeMap = Map(mapId);
    //     activeMap.gameObject.SetActive(true);
    //     _gm.loading = false;
    // }
    //
    // [Serializable] private class MapResponse : Server.Response
    // {
    //     public Scene.SerializableMap scene;
    // }
    //
    // [Serializable] public class MapPropertiesResponse : Server.Response
    // {
    //     public List<Scene.MapProperty> properties;
    // }
    //
    // public IEnumerator LoadMap(string mapId)
    // {
    //     if (_loadingMap) yield return null;
    //     _loadingMap = true;
    //     
    //     var mapRequest = Server.GetRequest($"{Server.baseUrl}/world/campaign/{World.CampaignId}/map/{mapId}");
    //     while (!mapRequest.isDone) 
    //         yield return null;
    //     
    //     var mapResponse = Server.GetResponse<MapResponse>(mapRequest);
    //     var map = Instantiate(_gm.mapPrefab, mapsParent).GetComponent<Map>();
    //     map.Deserialize(mapResponse.map);     
    //     activeMap = map;
    //         
    //     // get map properties
    //     var request = Server.GetRequest($"{Server.baseUrl}/world/campaign/{World.CampaignId}/map/{mapId}/properties");
    //     while (!request.isDone) 
    //         yield return null;
    //     
    //     var response = Server.GetResponse<MapPropertiesResponse>(request);
    //     map.properties = response.properties;
    //     
    //     // get explored tiles
    //     var exploredProperty = map.properties.FirstOrDefault(p => p.name == "EXPLORED");
    //     if (exploredProperty != null)
    //     {
    //         var exploredChars = exploredProperty.value.ToCharArray();
    //         for (var y = 0; y < map.Height; y += 1) 
    //         for (var x = 0; x < map.Width; x += 1)
    //             map.tiles[x, y].Explored = exploredChars[x + y * map.Width] == '1';
    //     }
    //
    //     // get actions not saved
    //     var actionsRequest = Server.GetRequest($"{Server.baseUrl}/world/campaign/{World.CampaignId}/actions/from/map/{mapId}");
    //     while (!actionsRequest.isDone) 
    //         yield return null;
    //     
    //     var actionsResponse = Server.GetResponse<ActionsResponse>(actionsRequest);
    //     foreach (var action in actionsResponse.actions) _gm.ResolveAction(action);
    //     
    //     // get shared entity properties
    //     foreach (var entity in map.entities) entity.RefreshPermissions();
    //
    //     map.loaded = true;
    //     maps.Add(map);
    //     _loadingMap = false;
    //     // _actionsFromDate = actionsResponse.date;
    //     _actionsFromDate = World.NowIsoDate();
    // }
    //
    // #endregion
    //
    // #region Actions
    //
    // [Serializable] private class ActionsRequestBody
    // {
    //     public List<Action> actions;
    // }
    //
    // [Serializable] private class ActionsResponse : Server.Response
    // {
    //     public List<Action> actions;
    // }
    //
    // private IEnumerator UpdateActions()
    // {
    //     while (!loaded)
    //         yield return null;
    //     
    //     while (Server.serverReady)
    //     {
    //         yield return _updateActionsPeriod;
    //
    //         var data = new ActionsRequestBody {actions = _gm.actionsDone};
    //         var len = data.actions.Count;
    //         var url = $"{Server.baseUrl}/world/campaign/{World.CampaignId}/actions/from/date/{_actionsFromDate}";
    //         var request = Server.PostRequest(url, data);
    //         while (!request.isDone)
    //             yield return null;
    //
    //         var response = Server.GetResponse<ActionsResponse>(request, false);
    //         if (!response)
    //             continue;
    //         
    //         _gm.actionsDone.RemoveRange(0, len);
    //         _gm.actionsToDo.AddRange(response.actions);
    //         _actionsFromDate = response.date;
    //     }
    // }
    //
    // #endregion
}