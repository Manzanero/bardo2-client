using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BardoUI.Maps
{
    public class MapsWindow : MonoBehaviour
    {
    //     public Transform mapItemsParent;
    //     public GameObject mapItemPrefab;
    //
    //     public InputField currentMapInput;
    //     public Button saveButton;
    //     public Button saveAsButton;
    //     
    //     public Button newEmptyButton;
    //     public Button newDonjonButton;
    //     public GameObject donjonWindow;
    //     public InputField tsvInputField;
    //     public Button importDonjon;
    //     public Button newFromButton;
    //     
    //     public GameObject errorDialogue;
    //     public Text errorMessage;
    //
    //     private World _world;
    //     private int _cachedMapsCount;
    //     private bool _resetMapList;
    //     private string _cachedId;
    //
    //     private void Start()
    //     {
    //         _world = GameObject.Find("World").GetComponent<World>();
    //         
    //         saveButton.onClick.AddListener(SaveButton);
    //         saveAsButton.onClick.AddListener(SaveAsButton);
    //         newDonjonButton.onClick.AddListener(delegate { donjonWindow.SetActive(true); });
    //         importDonjon.onClick.AddListener(NewDonjonButton);
    //     
    //         // remove previous items
    //         foreach (Transform child in mapItemsParent)
    //             Destroy(child.gameObject);
    //         
    //         StartCoroutine(GetMapList());
    //     }
    //
    //     private IEnumerator GetMapList()
    //     {
    //         while (_world.scenes.Count == 0) yield return null;
    //         
    //         while (!_world.scene) yield return null;
    //
    //         _cachedId = _world.scene.id;
    //         _cachedMapsCount = _world.scenes.Count;
    //         
    //         while (true)
    //         {
    //             foreach (Transform child in mapItemsParent)
    //                 Destroy(child.gameObject);
    //             
    //             currentMapInput.text = _world.scene.label;
    //             foreach (var mapData in _world.scenes)
    //             {
    //                 var mapItem = Instantiate(mapItemPrefab, mapItemsParent).GetComponent<MapItem>();
    //                 mapItem.mapName.text = $"{mapData.name}";
    //                 mapItem.selected.SetActive(_world.scene.id == mapData.id);
    //                 mapItem.deleteMapButton.gameObject.SetActive(_world.scene.id != mapData.id);
    //                 mapItem.changeMapButton.gameObject.SetActive(_world.scene.id != mapData.id);
    //                 mapItem.deleteMapButton.onClick.AddListener(delegate { StartCoroutine(DeleteMap(mapData.id)); });
    //                 mapItem.changeMapButton.onClick.AddListener(delegate { StartCoroutine(_world.ChangeActiveMap(mapData.id)); });
    //                 mapItem.changeAllMapButton.onClick.AddListener(delegate { ChangeAllMapButton(mapData.id); });
    //             }
    //             
    //             while (_cachedMapsCount == _world.scenes.Count && 
    //                    _cachedId == _world.scene.id && 
    //                    !_resetMapList)
    //                 yield return null;
    //             
    //             _cachedId = _world.scene.id;
    //             _cachedMapsCount = _world.scenes.Count;
    //             _resetMapList = false;
    //         }
    //     }
    //
    //     public IEnumerator SaveActiveMap(string newName, bool asNew)
    //     {
    //         _world.loading = true;
    //         var scene = _world.scene;
    //         var mapId = asNew ? World.NewId() : scene.id;
    //         var data = scene.Serialize();
    //         data.name = newName;
    //         data.mapId = mapId;
    //         var url = $"{Server.baseUrl}/world/campaign/{Campaign.campaignId}/map/{mapId}/save";
    //         var request = Server.PostRequest(url, data);
    //         while (!request.isDone)
    //             yield return null;
    //         
    //         Server.GetResponse<Server.Response>(request);
    //         map.name = newName;
    //         map.id = mapId;
    //         campaign.mapsInfo.RemoveAll(x => x.id == mapId);
    //         campaign.mapsInfo.Add(new Campaign.MapInfo {name = newName, id = mapId});
    //         campaign.mapsInfo.Sort((x, y) => string.Compare(
    //             x.name, y.name, StringComparison.Ordinal));
    //     
    //         StartCoroutine(_campaign.SaveCampaignProperty("ACTIVE_MAP", mapId));
    //         world.loading = false;
    //         _resetMapList = true;
    //     }  
    //
    //     private void SaveButton()
    //     {
    //         var newName = currentMapInput.text;
    //         if (ExistingName(newName) && _campaign.activeMap.name != newName)
    //         {
    //             RaiseError($"There is already a map with name '{newName}'");
    //             return;
    //         }
    //         
    //         StartCoroutine(SaveActiveMap(newName, false));
    //     }
    //
    //     private void SaveAsButton()
    //     {
    //         var newName = currentMapInput.text;
    //         if(ExistingName(newName))
    //         {
    //             RaiseError($"There is already a map with name '{newName}'");
    //             return;
    //         }
    //         
    //         StartCoroutine(SaveActiveMap(newName, true));
    //     }
    //
    //     public IEnumerator DeleteMap(string mapId)
    //     {
    //         world.loading = true;
    //         var request = Server.DeleteRequest($"{Server.baseUrl}/world/campaign/{Campaign.campaignId}/map/{mapId}/delete");
    //         while (!request.isDone)
    //             yield return null;
    //     
    //         Server.GetResponse<Server.Response>(request);
    //         var map = _campaign.Map(mapId);
    //         if (map)
    //         {
    //             _campaign.maps.Remove(map);
    //             Destroy(map.gameObject);
    //         }
    //         _campaign.scenes.RemoveAll(x => x.id == mapId);
    //         world.loading = false;
    //         _resetMapList = true;
    //     }
    //
    //     private void ChangeAllMapButton(string mapId)
    //     {
    //         if (_campaign.activeMap.id != mapId) StartCoroutine(_campaign.ChangeActiveMap(mapId));
    //         
    //         world.RegisterAction(new Action
    //         {
    //             name = GameMaster.ActionNames.ChangeMap,
    //             strings = new List<string>{mapId}
    //         });
    //     }
    //
    //     public IEnumerator NewMapFromDonjon(string newName, string mapTsv)
    //     {
    //         world.loading = true;
    //         var campaign = world.campaign;
    //         var mapId = GameMaster.NewId();
    //         var map = MapGenerator.CreateMapFromDonjon(mapTsv);
    //         map.gameObject.SetActive(false);
    //         map.name = newName;
    //         map.id = mapId;
    //         var data = map.Serialize();
    //         var url = $"{Server.baseUrl}/world/campaign/{Campaign.campaignId}/map/{mapId}/save";
    //         var request = Server.PostRequest(url, data);
    //         while (!request.isDone)
    //             yield return null;
    //     
    //         Server.GetResponse<Server.Response>(request);
    //         campaign.maps.Add(map);
    //         StartCoroutine(_campaign.ChangeActiveMap(mapId));
    //         while (!map.gameObject.activeSelf)
    //             yield return null;
    //     
    //         campaign.mapsInfo.Add(new Campaign.MapInfo {name = newName, id = mapId});
    //         campaign.mapsInfo.Sort((x, y) => string.Compare(
    //             x.name, y.name, StringComparison.Ordinal));
    //         world.loading = false;
    //     }
    //     
    //     private void SpawnDonjonWindow()
    //     {
    //         donjonWindow.SetActive(true);
    //     }
    //     
    //     private void NewDonjonButton()
    //     {
    //         var newName = currentMapInput.text;
    //         if(ExistingName(newName))
    //         {
    //             RaiseError($"There is already a map with name '{newName}'");
    //             return;
    //         }
    //         
    //         var tsv =  tsvInputField.text;
    //         StartCoroutine(NewMapFromDonjon(newName, tsv));
    //     }
    //
    //     private void RaiseError(string msg)
    //     {
    //         errorDialogue.SetActive(true);
    //         errorMessage.text = msg;
    //     }
    //
    //     private bool ExistingName(string mapName)
    //     {
    //         var mapInfo = _campaign.scenes.FirstOrDefault(x => x.name == mapName);
    //         return mapInfo != null;
    //     }
    }
}