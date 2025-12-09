using System;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;

public class Sheep : NetworkBehaviour
{
    private NetworkVariable<bool> shouldBurn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private DissolveControl burnControl;
    private NetworkVariable<float> dissolveAmount = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public AudioSource audioSource;
    
    [System.NonSerialized]
    public ProceduralTerrain ownerTerrain; // Reference to the terrain chunk that owns this sheep
    void Start()
    {
        burnControl = GetComponent<DissolveControl>();
        audioSource = GetComponent<AudioSource>();
    }

    void FixedUpdate()
    {
        // Server checks if sheep fell below y=0 and kills it
        if (IsServer && transform.position.y < 0f)
        {
            Die();
            return;
        }
        
        // Server updates burning logic
        if (IsServer && shouldBurn.Value)
        {
            dissolveAmount.Value += Time.deltaTime * 0.5f;
            Debug.Log(dissolveAmount.Value);

            if (dissolveAmount.Value >= 0.75f)
                Die();
        }
        
        // All clients apply visual effects based on synced value
        if (burnControl != null)
        {
            burnControl.SetDissolveBoth(dissolveAmount.Value);
        }
    }
    
    public void Die()
    {
        if (IsServer)
        {
            // Server despawns the object which syncs to all clients
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
            Destroy(gameObject);
        }
    }
    
    // Method to start burning (should be called from server or via RPC)
    [Rpc(SendTo.Server)]
    public void StartBurningServerRpc()
    {
        shouldBurn.Value = true;
    }
    
    public void PlayHitSound()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
    
    // RPC to play hit sound on all clients
    [ClientRpc]
    public void PlayHitSoundClientRpc()
    {
        PlayHitSound();
    }
}
