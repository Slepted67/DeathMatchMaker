using UnityEngine;

public class ZeroZOnAwake : MonoBehaviour
{
    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        foreach (var t in GetComponentsInChildren<Transform>(true))
            t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, 0f);
    }
}