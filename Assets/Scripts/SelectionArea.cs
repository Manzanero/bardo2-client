using System;
using System.Linq;
using UnityEngine;

public class SelectionArea : MonoBehaviour
{
    public MeshRenderer meshRenderer;

    public Scene scene;

    private World _world;
    private Tile _startTile;
    private Tile _cachedTile;

    private void Start()
    {
        _world = World.instance;
        meshRenderer.enabled = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            meshRenderer.enabled = false;
        }

        _cachedTile = scene.mouseTile ? scene.mouseTile : _cachedTile;

        if (Input.GetMouseButtonDown(0) && scene.mouseOverTile && !World.MouseOverUi)
        {
            _startTile = _cachedTile;
            meshRenderer.enabled = true;

            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                scene.selectedTiles.Clear();
                scene.selectedTokens.Clear();
            }
        }
        
        if (!_startTile)
            return;
        
        var startPos = _startTile;
        var endPos = _cachedTile;
        var selectionPosX = (startPos.x + endPos.x) / 2f;
        var selectionPosY = (startPos.y + endPos.y) / 2f;
        var scaleX = Mathf.Abs(_cachedTile.x - startPos.x) + 0.875f;
        var scaleY = Mathf.Abs(_cachedTile.y - startPos.y) + 0.875f;
        var selfT = transform;
        selfT.position = new Vector3(selectionPosX, 0, selectionPosY);
        selfT.localScale = new Vector3(scaleX, 34 * World.U, scaleY);
        if (startPos == endPos) selfT.localScale = Vector3.zero;

        if (!Input.GetMouseButtonUp(0)) 
            return;

        _startTile = null;
        meshRenderer.enabled = false;
        var minX = Math.Min(startPos.x, endPos.x);
        var minY = Math.Min(startPos.y, endPos.y);
        var maxX = Math.Max(startPos.x, endPos.x);
        var maxY = Math.Max(startPos.y, endPos.y);
        for (var x = minX; x <= maxX; x += 1)
        for (var y = minY; y <= maxY; y += 1)
            scene.selectedTiles.Add(scene.tiles[x, y]);
        
        var tempSelect = scene.tokens.Where(token => scene.selectedTiles.Contains(token.tile));
        scene.selectedTokens.AddRange(tempSelect);
        // scene.selectedTokens = scene.selectedTokens.Distinct();
    }
}