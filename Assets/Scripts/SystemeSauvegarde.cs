using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Properties;
using UnityEngine;

public class SystemeSauvegarde : MonoBehaviour
{
    public static SystemeSauvegarde Instance;

    public List<GameObject> bodyPrefabs; // assign in inspector

    string SavePath(string name) => Application.persistentDataPath + "/" + name + ".json";

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrl && Input.GetKeyDown(KeyCode.D))
        {
            SaveScene("save");
        }

        if (ctrl && Input.GetKeyDown(KeyCode.L))
        {
            LoadScene("save");
        }
    }

    public void SaveScene(string saveName)
    {
        SceneData data = new SceneData();

        foreach (var body in GravityManager.Bodies)
        {
            if (body == null || body.rb == null) continue;

            ObjectProperties props = body.GetComponent<ObjectProperties>();

            BodyData dataBody = new BodyData
            {
                prefabName = body.gameObject.name,

                position = body.rb.position,
                velocity = body.rb.linearVelocity,

                objectName = props.objectName,
                mass = props.mass,
                radius = props.radius,
                speedMagnitude = props.speedMagnitude,
                distanceToEtoile = props.distanceToEtoile,
                gravityMagnitude = props.gravityMagnitude,
                temperatureMagnitude = props.temperatureMagnitude,
                periode = props.periode,
                density = props.density,
                etoileParent = props.EtoileParent != null ? props.EtoileParent.name : "",

                albedo = props.albedo,
                greenhouseEffect = props.greenhouseEffect
            };

            data.bodies.Add(dataBody);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveName, json);

        Debug.Log("Saved to: " + saveName);
    }

    public void LoadScene(string saveName)
    {
        if (!File.Exists(saveName))
        {
            Debug.LogWarning("No save file found!");
            return;
        }

        string json = File.ReadAllText(saveName);
        SceneData data = JsonUtility.FromJson<SceneData>(json);

        ClearScene();

        Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();

        foreach (var dataBodies in data.bodies)
        {
            GameObject prefab = bodyPrefabs.Find(p => p.name == dataBodies.prefabName);

            if (prefab == null)
            {
                Debug.LogWarning("Missing prefab: " + dataBodies.prefabName);
                continue;
            }

            GameObject obj = Instantiate(prefab, dataBodies.position, Quaternion.identity);

            GravityBody gb = obj.GetComponent<GravityBody>();
            if(gb == null)
                gb = obj.AddComponent<GravityBody>();

            ObjectProperties props = obj.GetComponent<ObjectProperties>();
            if(props == null)
                props = obj.AddComponent<ObjectProperties>();

            // Restore physics
            gb.rb.mass = dataBodies.mass;

            // Delay velocity to avoid Unity override
            StartCoroutine(ApplyVelocityNextFrame(gb, dataBodies.velocity));

            IEnumerator ApplyVelocityNextFrame(GravityBody body, Vector3 vel)
            {
                yield return null;
                body.rb.linearVelocity = vel;
            }

            // Restore properties
            props.objectName = dataBodies.objectName;
            props.mass = dataBodies.mass;
            props.radius = dataBodies.radius;
            props.speedMagnitude = dataBodies.speedMagnitude;
            props.distanceToEtoile = dataBodies.distanceToEtoile;
            props.gravityMagnitude = dataBodies.gravityMagnitude;
            props.temperatureMagnitude = dataBodies.temperatureMagnitude;
            props.periode = dataBodies.periode;
            props.density = dataBodies.density;

            props.albedo = dataBodies.albedo;
            props.greenhouseEffect = dataBodies.greenhouseEffect;
        }

        foreach (var dataBodies in data.bodies)
        {
            if (string.IsNullOrEmpty(dataBodies.etoileParent)) continue;

            if (spawned.TryGetValue(dataBodies.objectName, out GameObject obj) &&
                spawned.TryGetValue(dataBodies.etoileParent, out GameObject parent))
            {
                obj.GetComponent<ObjectProperties>().EtoileParent = parent;
            }
        }

        Debug.Log("Scene loaded.");
    }

    void ClearScene()
    {
        var bodies = new List<GravityBody>(GravityManager.Bodies);

        foreach (var body in bodies)
        {
            if (body != null)
                Destroy(body.gameObject);
        }
    }

}