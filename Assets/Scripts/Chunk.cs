using System;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public MeshFilter meshFilter;

    public Scene scene;

    public int tileRangeXStart;
    public int tileRangeXEnd;
    public int tileRangeYStart;
    public int tileRangeYEnd;

    public bool dirty;
    
    private Vector3[] _vertices;
    private Vector2[] _uv;
    private int[] _triangles;
    private const int Variants = 4;
    private const int Themes = 32;
    private const float UnitW = 1f / 2 / Variants;
    private const float UnitH = 1f / 2 / Themes;

    private void Start()
    {
        _vertices = new Vector3[20 * (tileRangeXEnd - tileRangeXStart) * (tileRangeYEnd - tileRangeYStart)];
        _uv = new Vector2[20 * (tileRangeXEnd - tileRangeXStart) * (tileRangeYEnd - tileRangeYStart)];
        _triangles = new int[10 * 3 * (tileRangeXEnd - tileRangeXStart) * (tileRangeYEnd - tileRangeYStart)];
        Refresh();
    }

    private void Update()
    {
        if (dirty) Refresh();
    }

    private void Refresh()
    {
        RefreshMesh();
        
        dirty = false;
    }

    private void RefreshMesh()
    {
        for (var y = tileRangeYStart; y < tileRangeYEnd; y++)
        for (var x = tileRangeXStart; x < tileRangeXEnd; x++)
            RefreshTileMesh(x, y);
    
        var mesh = meshFilter.mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.uv = _uv;
        mesh.triangles = _triangles;

        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.Optimize();
    }

    private static Vector2[] GetUvPointsTop(int theme, int variant)
    {
        var initX = variant * UnitW;
        var initY = 1f - theme * 2 * UnitH;
        
        return new[]
        {
            new Vector2(initX, initY - UnitH),
            new Vector2(initX, initY),
            new Vector2(initX + UnitW, initY - UnitH),
            new Vector2(initX + UnitW, initY)
        };
    }

    private static Vector2[] GetUvPointsFace(int theme, float altitude, float fromAltitude, int variant)
    {
        var initX = variant * UnitW;
        var initY = 1f - theme * 2 * UnitH;
        
        if (altitude > 0)
        {
            return new[]
            {
                new Vector2(initX, initY - UnitH * (2 - fromAltitude)),
                new Vector2(initX, initY - UnitH * (2 - altitude)),
                new Vector2(initX + UnitW, initY - UnitH * (2 - fromAltitude)),
                new Vector2(initX + UnitW, initY - UnitH * (2 - altitude))
            };
        }

        if (altitude < 0)
        {
            return new[]
            {
                new Vector2(initX, initY - UnitH * (1 - fromAltitude)),
                new Vector2(initX, initY - UnitH * (1 - altitude)),
                new Vector2(initX + UnitW, initY - UnitH * (1 - fromAltitude)),
                new Vector2(initX + UnitW, initY - UnitH * (1 - altitude))
            };
        }

        return new Vector2[4];
    }

    private static Vector3[] GetPointsTop(int x, int y, float altitude)
    {
        return new[]
        {
            new Vector3(-0.5f + x, altitude, -0.5f + y),
            new Vector3(-0.5f + x, altitude, 0.5f + y),
            new Vector3(0.5f + x, altitude, -0.5f + y),
            new Vector3(0.5f + x, altitude, 0.5f + y)
        };
    }

    private static float GetBaseAltitude(float altitude, float fromAltitude)
    {
        return altitude >= 0 ? 
            Mathf.Clamp(altitude >= fromAltitude ? fromAltitude : altitude, 0, 1) : 
            Mathf.Clamp(altitude <= fromAltitude ? fromAltitude : altitude, -1, 0);
    }

    private static Vector3[] GetPointsFaceS(int x, int y, float altitude, float fromAltitude)
    {
        return new[]
        {
            new Vector3(-0.5f + x, fromAltitude, -0.5f + y),
            new Vector3(-0.5f + x, altitude, -0.5f + y),
            new Vector3(0.5f + x, fromAltitude, -0.5f + y),
            new Vector3(0.5f + x, altitude, -0.5f + y)
        };
    }

    private static Vector3[] GetPointsFaceW(int x, int y, float altitude, float fromAltitude)
    {
        return new[]
        {
            new Vector3(-0.5f + x, fromAltitude, 0.5f + y),
            new Vector3(-0.5f + x, altitude, 0.5f + y),
            new Vector3(-0.5f + x, fromAltitude, -0.5f + y),
            new Vector3(-0.5f + x, altitude, -0.5f + y)
        };
    }

    private static Vector3[] GetPointsFaceN(int x, int y, float altitude, float fromAltitude)
    {
        return new[]
        {
            new Vector3(0.5f + x, fromAltitude, 0.5f + y),
            new Vector3(0.5f + x, altitude, 0.5f + y),
            new Vector3(-0.5f + x, fromAltitude, 0.5f + y),
            new Vector3(-0.5f + x, altitude, 0.5f + y)
        };
    }

    private static Vector3[] GetPointsFaceE(int x, int y, float altitude, float fromAltitude)
    {
        return new[]
        {
            new Vector3(0.5f + x, fromAltitude, -0.5f + y),
            new Vector3(0.5f + x, altitude, -0.5f + y),
            new Vector3(0.5f + x, fromAltitude, 0.5f + y),
            new Vector3(0.5f + x, altitude, 0.5f + y)
        };
    }


    private void RefreshTileMesh(int x, int y)
    {
        var tiles = scene.tiles;
        var tile = tiles[x, y];
        // var altitude = tile.altitude / 16f;
        var altitude = tile.explored ? tile.altitude / 16f : 1;
        var tileS = scene.Tile(x, y - 1);
        var tileW = scene.Tile(x - 1, y);
        var tileN = scene.Tile(x, y + 1);
        var tileE = scene.Tile(x + 1, y);
        var tileSNotNull = tileS != null;
        var tileWNotNull = tileW != null;
        var tileNNotNull = tileN != null;
        var tileENotNull = tileE != null;
        var tileSAltitude = tileSNotNull ? tileS.altitude / 16f : altitude;
        var tileWAltitude = tileWNotNull ? tileW.altitude / 16f : altitude;
        var tileNAltitude = tileNNotNull ? tileN.altitude / 16f : altitude;
        var tileEAltitude = tileENotNull ? tileE.altitude / 16f : altitude;
        var tileSRevealed = tileSNotNull && tileS.revealed;
        var tileWRevealed = tileWNotNull && tileW.revealed;
        var tileNRevealed = tileNNotNull && tileN.revealed;
        var tileERevealed = tileENotNull && tileE.revealed;
        var tileSTransparent = tileSNotNull && tileS.transparent;
        var tileWTransparent = tileWNotNull && tileW.transparent;
        var tileNTransparent = tileNNotNull && tileN.transparent;
        var tileETransparent = tileENotNull && tileE.transparent;
        var offset = x - tileRangeXStart + (y - tileRangeYStart) * (tileRangeXEnd - tileRangeXStart);
        var verticesOffset = offset * 20;

        var pointsTop = GetPointsTop(x, y, altitude);
        _vertices[0 + verticesOffset] = pointsTop[0];
        _vertices[1 + verticesOffset] = pointsTop[1];
        _vertices[2 + verticesOffset] = pointsTop[2];
        _vertices[3 + verticesOffset] = pointsTop[3];
        var baseAltitudeS = GetBaseAltitude(altitude, tileSAltitude);
        var pointsS = GetPointsFaceS(x, y, altitude, baseAltitudeS);
        _vertices[4 + verticesOffset] = pointsS[0];
        _vertices[5 + verticesOffset] = pointsS[1];
        _vertices[6 + verticesOffset] = pointsS[2];
        _vertices[7 + verticesOffset] = pointsS[3];
        var baseAltitudeW = GetBaseAltitude(altitude, tileWAltitude);
        var pointsW = GetPointsFaceW(x, y, altitude, baseAltitudeW);
        _vertices[8 + verticesOffset] = pointsW[0];
        _vertices[9 + verticesOffset] = pointsW[1];
        _vertices[10 + verticesOffset] = pointsW[2];
        _vertices[11 + verticesOffset] = pointsW[3];
        var baseAltitudeN = GetBaseAltitude(altitude, tileNAltitude);
        var pointsN = GetPointsFaceN(x, y, altitude, baseAltitudeN);
        _vertices[12 + verticesOffset] = pointsN[0];
        _vertices[13 + verticesOffset] = pointsN[1];
        _vertices[14 + verticesOffset] = pointsN[2];
        _vertices[15 + verticesOffset] = pointsN[3];
        var baseAltitudeE = GetBaseAltitude(altitude, tileEAltitude);
        var pointsE = GetPointsFaceE(x, y, altitude, baseAltitudeE);
        _vertices[16 + verticesOffset] = pointsE[0];
        _vertices[17 + verticesOffset] = pointsE[1];
        _vertices[18 + verticesOffset] = pointsE[2];
        _vertices[19 + verticesOffset] = pointsE[3];

        var theme = tile.theme;
        var uvOffset = verticesOffset;

        var uvPointsTop = GetUvPointsTop(tile.transparent && tile.explored ? theme : 31, tile.revealed ? tile.top : tile.top + 4);
        _uv[0 + uvOffset] = uvPointsTop[0];
        _uv[1 + uvOffset] = uvPointsTop[1];
        _uv[2 + uvOffset] = uvPointsTop[2];
        _uv[3 + uvOffset] = uvPointsTop[3];
        var uvPointsFaceS = GetUvPointsFace(!tile.transparent ? theme : 31, altitude, baseAltitudeS, tileSRevealed ? tile.s : tile.s + 4);
        _uv[4 + uvOffset] = uvPointsFaceS[0];
        _uv[5 + uvOffset] = uvPointsFaceS[1];
        _uv[6 + uvOffset] = uvPointsFaceS[2];
        _uv[7 + uvOffset] = uvPointsFaceS[3];
        var uvPointsFaceW = GetUvPointsFace(!tile.transparent ? theme : 31, altitude, baseAltitudeW, tileWRevealed ? tile.w : tile.w + 4);
        _uv[8 + uvOffset] = uvPointsFaceW[0];
        _uv[9 + uvOffset] = uvPointsFaceW[1];
        _uv[10 + uvOffset] = uvPointsFaceW[2];
        _uv[11 + uvOffset] = uvPointsFaceW[3];
        var uvPointsFaceN = GetUvPointsFace(!tile.transparent ? theme : 31, altitude, baseAltitudeN, tileNRevealed ? tile.n : tile.n + 4);
        _uv[12 + uvOffset] = uvPointsFaceN[0];
        _uv[13 + uvOffset] = uvPointsFaceN[1];
        _uv[14 + uvOffset] = uvPointsFaceN[2];
        _uv[15 + uvOffset] = uvPointsFaceN[3];
        var uvPointsFaceE = GetUvPointsFace(!tile.transparent ? theme : 31, altitude, baseAltitudeE, tileERevealed ? tile.e : tile.e + 4);
        _uv[16 + uvOffset] = uvPointsFaceE[0];
        _uv[17 + uvOffset] = uvPointsFaceE[1];
        _uv[18 + uvOffset] = uvPointsFaceE[2];
        _uv[19 + uvOffset] = uvPointsFaceE[3];

        var trianglesOffset = offset * 30;

        // top
        _triangles[0 + trianglesOffset] = 0 + verticesOffset;
        _triangles[1 + trianglesOffset] = _triangles[4 + trianglesOffset] = 1 + verticesOffset;
        _triangles[2 + trianglesOffset] = _triangles[3 + trianglesOffset] = 2 + verticesOffset;
        _triangles[5 + trianglesOffset] = 3 + verticesOffset;
        // S
        _triangles[6 + trianglesOffset] = 4 + verticesOffset;
        _triangles[7 + trianglesOffset] = _triangles[10 + trianglesOffset] = 5 + verticesOffset;
        _triangles[8 + trianglesOffset] = _triangles[9 + trianglesOffset] = 6 + verticesOffset;
        _triangles[11 + trianglesOffset] = 7 + verticesOffset;
        // W
        _triangles[12 + trianglesOffset] = 8 + verticesOffset;
        _triangles[13 + trianglesOffset] = _triangles[16 + trianglesOffset] = 9 + verticesOffset;
        _triangles[14 + trianglesOffset] = _triangles[15 + trianglesOffset] = 10 + verticesOffset;
        _triangles[17 + trianglesOffset] = 11 + verticesOffset;
        // N
        _triangles[18 + trianglesOffset] = 12 + verticesOffset;
        _triangles[19 + trianglesOffset] = _triangles[22 + trianglesOffset] = 13 + verticesOffset;
        _triangles[20 + trianglesOffset] = _triangles[21 + trianglesOffset] = 14 + verticesOffset;
        _triangles[23 + trianglesOffset] = 15 + verticesOffset;
        // E
        _triangles[24 + trianglesOffset] = 16 + verticesOffset;
        _triangles[25 + trianglesOffset] = _triangles[28 + trianglesOffset] = 17 + verticesOffset;
        _triangles[26 + trianglesOffset] = _triangles[27 + trianglesOffset] = 18 + verticesOffset;
        _triangles[29 + trianglesOffset] = 19 + verticesOffset;
    }
}