using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Properties;
using UnityEngine;

public class SystemeSauvegarde : MonoBehaviour
{
    public static SystemeSauvegarde Instance;

    public List<GameObject> bodyPrefabs; // assign in inspector

    private Dictionary<string, GameObject> prefabLookup;


    /// <summary>
    /// Prepare la table de correspondance prefabID vers prefab.
    /// </summary>
    void Start()
    {
        prefabLookup = new Dictionary<string, GameObject>();

        foreach (var prefab in bodyPrefabs)
        {
            PrefabID pid = prefab.GetComponent<PrefabID>();

            if (pid == null || string.IsNullOrEmpty(pid.prefabID))
            {
                Debug.LogWarning($"Prefab {prefab.name} missing PrefabID!");
                continue;
            }

            prefabLookup[pid.prefabID] = prefab;
        }
    }


    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Ecoute le raccourci clavier pour creer une sauvegarde.
    /// </summary>
    void Update()
    {

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrl && Input.GetKeyDown(KeyCode.S))
        {
            SaveMenuUI menu = Object.FindFirstObjectByType<SaveMenuUI>();
            menu.CreateSave();
        }

    }

    /// <summary>
    /// Sauvegarde l'etat de la scene courante dans un fichier JSON.
    /// </summary>
    public void SaveScene(string saveName)
    {
        SceneData data = new SceneData();

        foreach (var body in GravityManager.Bodies)
        {
            if (body == null || body.rb == null) continue;

            ObjectProperties props = body.GetComponent<ObjectProperties>();

            if (props == null)
            {
                Debug.LogWarning($"Missing ObjectProperties on {body.name}");
                continue;
            }

            PlanetID pid = body.GetComponent<PlanetID>();
            if (pid == null)
            {
                pid = body.gameObject.AddComponent<PlanetID>();
            }

            string parentID = "";

            if (props.EtoileParent != null)
            {
                PlanetID parentPID = props.EtoileParent.GetComponent<PlanetID>();

                if (parentPID == null)
                    parentPID = props.EtoileParent.AddComponent<PlanetID>();

                parentID = parentPID.id;
            }

            PrefabID prefabIDComp = body.GetComponent<PrefabID>();

            if (prefabIDComp == null)
            {
                Debug.LogWarning($"Missing PrefabID on {body.name}");
                continue;
            }

            BodyData dataBody = new BodyData
            {
                id = pid.id,

                prefabID = prefabIDComp.prefabID,

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
                etoileParentID = parentID,

                albedo = props.albedo,
                greenhouseEffect = props.greenhouseEffect
            };

            data.bodies.Add(dataBody);
        }

        string json = JsonUtility.ToJson(data, true);
        string fullPath = Path.Combine(Application.persistentDataPath, saveName);

        if (!fullPath.EndsWith(".json"))
            fullPath += ".json";

        File.WriteAllText(fullPath, json);
        Debug.Log("Saved to: " + fullPath);
    }

    /// <summary>
    /// Charge une sauvegarde JSON et reconstruit la scene.
    /// </summary>
    public void LoadScene(string saveName)
    {
        string fullPath = Path.Combine(Application.persistentDataPath, saveName);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("No save file found!");
            return;
        }

        string json = File.ReadAllText(fullPath);

        SceneData data = JsonUtility.FromJson<SceneData>(json);

        ClearScene();

        Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();

        foreach (var dataBodies in data.bodies)
        {
            if (!prefabLookup.TryGetValue(dataBodies.prefabID, out GameObject prefab))
            {
                Debug.LogWarning("Missing prefab ID: " + dataBodies.prefabID);
                continue;
            }

            GameObject obj = Instantiate(prefab, dataBodies.position, Quaternion.identity);

            PlanetID pid = obj.GetComponent<PlanetID>();
            if (pid == null)
                pid = obj.AddComponent<PlanetID>();
            
            pid.id = dataBodies.id;
            spawned[pid.id] = obj;

            GravityBody gb = obj.GetComponent<GravityBody>();
            if (gb == null)
                gb = obj.AddComponent<GravityBody>();

            ObjectProperties props = obj.GetComponent<ObjectProperties>();
            if (props == null)
                props = obj.AddComponent<ObjectProperties>();

            // Restore physics
            gb.rb.mass = dataBodies.mass;

            // Delay velocity to avoid Unity override
            StartCoroutine(ApplyVelocityNextFrame(gb, dataBodies.velocity));

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
            if (string.IsNullOrEmpty(dataBodies.etoileParentID)) continue;

            if (spawned.TryGetValue(dataBodies.id, out GameObject obj) &&
                spawned.TryGetValue(dataBodies.etoileParentID, out GameObject parent))
            {
                obj.GetComponent<ObjectProperties>().EtoileParent = parent;
            }
        }

        Debug.Log("Scene loaded.");
    }

    /// <summary>
    /// Detruit tous les objets geres par le GravityManager.
    /// </summary>
    void ClearScene()
    {
        var bodies = new List<GravityBody>(GravityManager.Bodies);

        foreach (var body in bodies)
        {
            if (body != null)
                Destroy(body.gameObject);
        }
    }

    /// <summary>
    /// Applique la vitesse au prochain frame pour eviter l'ecrasement Unity.
    /// </summary>
    private IEnumerator ApplyVelocityNextFrame(GravityBody body, Vector3 vel)
    {
        yield return null;
        body.rb.linearVelocity = vel;
    }

}