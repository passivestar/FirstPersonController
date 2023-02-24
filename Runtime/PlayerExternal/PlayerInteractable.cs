using UnityEngine;
using UnityEngine.Events;

public class PlayerInteractable : MonoBehaviour
{
    [SerializeField] UnityEvent<RaycastHit> onPlayerInteractLookAt;
    [SerializeField] UnityEvent onPlayerInteractLookAway;
    [SerializeField] UnityEvent<RaycastHit> onPlayerInteractStarted;
    [SerializeField] UnityEvent<RaycastHit> onPlayerInteractPerformed;
    [SerializeField] UnityEvent<RaycastHit, Vector2, Vector2> onPlayerInteractMove;

    void OnPlayerInteractLookAt(RaycastHit hit)
    {
        onPlayerInteractLookAt.Invoke(hit);
    }

    void OnPlayerInteractLookAway()
    {
        onPlayerInteractLookAway.Invoke();
    }

    void OnPlayerInteractStarted(RaycastHit hit)
    {
        onPlayerInteractStarted.Invoke(hit);
    }

    void OnPlayerInteractPerformed(RaycastHit hit)
    {
        onPlayerInteractPerformed.Invoke(hit);
    }

    void OnPlayerInteractMove((RaycastHit hit, Vector2 delta, Vector2 totalDelta) data)
    {
        onPlayerInteractMove.Invoke(data.hit, data.delta, data.totalDelta);
    }
}