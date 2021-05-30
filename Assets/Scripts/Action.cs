using System;
using System.Collections.Generic;

[Serializable]
public class Action
{
    public string name;
    public string scene;
    public List<string> players;
    public List<string> strings;
    public List<Tile.Model> tiles;
    public List<Token.Model> tokens;
    public bool done;
}
