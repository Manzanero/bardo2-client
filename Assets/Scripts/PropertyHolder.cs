using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;


public class PropertyHolder
{
    private readonly Dictionary<string, Property> _properties = new Dictionary<string, Property>();

    public List<string> Keys => _properties.Keys.ToList();

    public bool Has(string name) => _properties.ContainsKey(name);
    
    public Property Get(string name) => _properties.TryGetValue(name, out var value) ? value : null;
    public string GetText(string name) => Get(name)?.ToText() ?? "";
    public float GetNumeric(string name) => Get(name)?.ToNumeric() ?? 0;
    public float[] GetBar(string name) => Get(name)?.ToBar() ?? new float[2];
    public bool GetBoolean(string name) => Get(name)?.ToBoolean() ?? false;
    public Color GetColor(string name) => Get(name)?.ToColor() ?? new Color();

    public void Set(string name, IEnumerable<string> values)
    {
        var property = Get(name);
        if (property == null)
            _properties.Add(name, new Property {name = name, values = values.ToList()});
        else
            property.values = values.ToList();
    }
    public void SetText(string name, string text) => Set(name, new[]
    {
        text
    });
    public void SetNumeric(string name, float numeric) => Set(name, new[]
    {
        numeric.ToString(CultureInfo.InvariantCulture)
    });
    public void SetBar(string name, float[] bar) => Set(name, new[]
    {
        bar[0].ToString(CultureInfo.InvariantCulture), bar[1].ToString(CultureInfo.InvariantCulture)
    });
    public void SetBoolean(string name, bool boolean) => Set(name, new[]
    {
        boolean ? "true" : "false"
    });
    public void SetColor(string name, Color color) => Set(name, new[]
    {
        Mathf.Round(color.r * 100).ToString(CultureInfo.InvariantCulture),
        Mathf.Round(color.g * 100).ToString(CultureInfo.InvariantCulture),
        Mathf.Round(color.b * 100).ToString(CultureInfo.InvariantCulture)
    });

    public void Remove(string name) => _properties.Remove(name);

    public static Property.Info GetInfo(Dictionary<string, Property.Info> propertiesInfo, string name) =>
        propertiesInfo.TryGetValue(name, out var value) ? value : null;

    public static bool HasInfo(Dictionary<string, Property.Info> propertiesInfo, string name) =>
        propertiesInfo.ContainsKey(name);

    [Serializable]
    public class Model
    {
        public List<Property> properties;
    }

    public Model Serializable()
    {
        return new Model {properties = _properties.Keys.Select(x => _properties[x]).ToList()};
    }

    public void Deserialize(Model model)
    {
        for (var i = 0; i < _properties.Count; i++) Set(model.properties[i].name, model.properties[i].values);
    }
}