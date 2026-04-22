using UnityEngine;
using System;

public class PlanetID : MonoBehaviour
{
    public string id;

    private void Awake()
    {
        // If no ID exists, generate one once
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }
    }
}