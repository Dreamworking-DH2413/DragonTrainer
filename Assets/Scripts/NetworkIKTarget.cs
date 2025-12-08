using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Ensures IK targets are properly synchronized across the network
/// Attach this to your IK target GameObjects (LeftWingTarget, RightWingTarget, hints, etc.)
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkIKTarget : NetworkBehaviour
{
    [Header("Sync Settings")]
    [Tooltip("How often to sync position/rotation (lower = more updates, higher bandwidth)")]
    public float syncInterval = 0.02f; // 50 times per second
    
    private float syncTimer = 0f;
    
    // Network variables for position and rotation
    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(
        Vector3.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
        
    private NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    
    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            // Not in network mode
            return;
        }
        
        if (IsServer)
        {
            // Host: Send position/rotation to network
            syncTimer += Time.deltaTime;
            if (syncTimer >= syncInterval)
            {
                syncTimer = 0f;
                netPosition.Value = transform.position;
                netRotation.Value = transform.rotation;
            }
        }
        else
        {
            // Clients: Apply networked position/rotation with smooth interpolation
            transform.position = Vector3.Lerp(transform.position, netPosition.Value, Time.deltaTime * 20f);
            transform.rotation = Quaternion.Slerp(transform.rotation, netRotation.Value, Time.deltaTime * 20f);
        }
    }
}
