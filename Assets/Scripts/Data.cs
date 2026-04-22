using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BodyData
{
    public string prefabName;

    public Vector3 position;
    public Vector3 velocity;

    // ObjectProperties data
    public string id;
    public string objectName;
    public float mass;
    public float radius;
    public float speedMagnitude;
    public float distanceToEtoile;
    public float gravityMagnitude;
    public float temperatureMagnitude;
    public float periode;
    public float density;
    public string etoileParent;

    public float albedo;
    public float greenhouseEffect;
}

[Serializable]
public class SceneData
{
    public List<BodyData> bodies = new List<BodyData>();
}