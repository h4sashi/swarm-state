// For any component that uses ScriptableObject configs
using UnityEngine;
public interface IConfigurable<T> where T : ScriptableObject
{
    T Config { get; set; }
    void ApplyConfig();
}