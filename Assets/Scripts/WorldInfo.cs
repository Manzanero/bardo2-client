using System;
using System.Collections.Generic;

[Serializable]
public class WorldInfo
{
    public List<WorldProperty> properties;
    public List<SceneInfo> scenes;
    public List<PlayerInfo> players;

    [Serializable]
    public class WorldProperty
    {
        public string name;
        public List<string> values;
    }
}