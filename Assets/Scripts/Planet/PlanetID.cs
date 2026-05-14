using UnityEngine;
using System;

public class PlanetID : MonoBehaviour
{
    public string id;

    /// <summary>
    /// Genere un identifiant unique si aucun n'est defini.
    /// </summary>
    private void Awake()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }
    }
}