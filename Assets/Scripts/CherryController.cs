using System.Collections;
using UnityEngine;
public class CherryController : MonoBehaviour
{
    [Header("Cherry Setup")]
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private Vector2 levelDimensions = new Vector2(28f, 29f);
    [SerializeField] private float spawnOffset = 1.0f;
    [SerializeField] private int cherrySortingOrder = 100;

    [Header("Timing")]
    [SerializeField] private float spawnCooldown = 5.0f;

    [Header("Movement")]
    [SerializeField] private float travelSpeed = 4.0f;

    private Transform activeCherry;
    private Coroutine travelRoutine;
    private float nextSpawnTime;
    private Transform levelRoot;


    private void Start()
    {
        if (levelRoot == null)
        {
            GameObject generatedLevel = GameObject.Find("GeneratedLevel");
            if (generatedLevel != null)
            {
                levelRoot = generatedLevel.transform;
            }
        }

        nextSpawnTime = Time.time + spawnCooldown;
    }

    private void Update()
    {
        if (activeCherry == null && Time.time >= nextSpawnTime)
        {
            SpawnCherry();
        }
    }

    private void SpawnCherry()
    {
        if (cherryPrefab == null)
        {
            Debug.LogWarning("CherryController requires a cherry prefab reference.");
            ScheduleNextSpawn();
            return;
        }

        Vector3 center = levelRoot != null ? levelRoot.position : Vector3.zero;
        GetSpawnAndExitPositions(center, out Vector3 start, out Vector3 end);

        GameObject cherryInstance = Instantiate(cherryPrefab, start, Quaternion.identity);
        activeCherry = cherryInstance.transform;

        PrepareCherryForTraversal(cherryInstance);

        float travelDuration = CalculateTravelDuration(start, end);
        travelRoutine = StartCoroutine(TraversePath(activeCherry, start, end, travelDuration));
    }

    private void PrepareCherryForTraversal(GameObject cherryInstance)
    {
        var spriteRenderer = cherryInstance.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = cherrySortingOrder;
        }

        var collider = cherryInstance.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        var rigidbody = cherryInstance.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.velocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
            rigidbody.gravityScale = 0f;
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private float CalculateTravelDuration(Vector3 start, Vector3 end)
    {
        float pathLength = Vector3.Distance(start, end);
        if (travelSpeed <= 0.01f)
        {
            return pathLength;
        }

        return Mathf.Max(0.01f, pathLength / travelSpeed);
    }

    private IEnumerator TraversePath(Transform cherry, Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0f;
        while (cherry != null && elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            cherry.position = Vector3.Lerp(start, end, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (cherry != null)
        {
            cherry.position = end;
        }

        travelRoutine = null;
        DestroyCherryInstance();
    }

    private void GetSpawnAndExitPositions(Vector3 center, out Vector3 start, out Vector3 end)
    {
        Vector2 halfExtents = levelDimensions * 0.5f;
        int side = Random.Range(0, 4);

        Vector3 localStart = Vector3.zero;
        switch (side)
        {
            case 0: // Left
                localStart = new Vector3(-halfExtents.x - spawnOffset, Random.Range(-halfExtents.y, halfExtents.y), 0f);
                break;
            case 1: // Right
                localStart = new Vector3(halfExtents.x + spawnOffset, Random.Range(-halfExtents.y, halfExtents.y), 0f);
                break;
            case 2: // Top
                localStart = new Vector3(Random.Range(-halfExtents.x, halfExtents.x), halfExtents.y + spawnOffset, 0f);
                break;
            default: // Bottom
                localStart = new Vector3(Random.Range(-halfExtents.x, halfExtents.x), -halfExtents.y - spawnOffset, 0f);
                break;
        }

        start = center + localStart;
        Vector3 mirroredLocal = -localStart;
        end = center + mirroredLocal;
    }

    public void DestroyCherryInstance()
    {
        if (travelRoutine != null)
        {
            StopCoroutine(travelRoutine);
            travelRoutine = null;
        }

        if (activeCherry != null)
        {
            Destroy(activeCherry.gameObject);
            activeCherry = null;
        }

        ScheduleNextSpawn();
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + spawnCooldown;
    }
}
