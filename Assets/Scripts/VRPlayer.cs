using Unity.Netcode;
using UnityEngine;
using Valve.VR;

public class VRPlayer : NetworkBehaviour
{
    [Header("Avatar Body Parts")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;

    [Header("Visual Representations (for remote player)")]
    [SerializeField] private GameObject headVisual;
    [SerializeField] private GameObject leftHandVisual;
    [SerializeField] private GameObject rightHandVisual;

    // Network synced positions and rotations
    private NetworkVariable<Vector3> netHeadPos = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> netHeadRot = new NetworkVariable<Quaternion>();
    private NetworkVariable<Vector3> netLeftHandPos = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> netLeftHandRot = new NetworkVariable<Quaternion>();
    private NetworkVariable<Vector3> netRightHandPos = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> netRightHandRot = new NetworkVariable<Quaternion>();

    // Local SteamVR references (found at runtime)
    private Transform localCamera;
    private Transform localPlayerRoot; // The SteamVR player root object
    private SteamVR_Behaviour_Pose leftControllerPose;
    private SteamVR_Behaviour_Pose rightControllerPose;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            // This is YOUR avatar - get your local SteamVR tracking references
            localCamera = Camera.main?.transform;
            
            // Find the SteamVR player root (usually [CameraRig] or similar)
            if (localCamera != null)
            {
                // Walk up the hierarchy to find the root player object
                Transform current = localCamera;
                while (current.parent != null)
                {
                    current = current.parent;
                    // Look for common SteamVR root names
                    if (current.name.Contains("CameraRig") || current.name.Contains("Player") || current.name.Contains("SteamVR"))
                    {
                        localPlayerRoot = current;
                        break;
                    }
                }
                // If we didn't find a specific named root, just go to the top-most parent
                if (localPlayerRoot == null && localCamera.root != localCamera)
                {
                    localPlayerRoot = localCamera.root;
                }
            }
            
            // Find SteamVR controller poses in the scene
            var steamVRObjects = FindObjectsOfType<SteamVR_Behaviour_Pose>();
            foreach (var pose in steamVRObjects)
            {
                if (pose.inputSource == SteamVR_Input_Sources.LeftHand)
                    leftControllerPose = pose;
                else if (pose.inputSource == SteamVR_Input_Sources.RightHand)
                    rightControllerPose = pose;
            }
            
            // Hide your own avatar visuals (you see through the headset)
            if (headVisual != null) headVisual.SetActive(false);
            if (leftHandVisual != null) leftHandVisual.SetActive(false);
            if (rightHandVisual != null) rightHandVisual.SetActive(false);
            
            Debug.Log($"Local VR player spawned - tracking initialized. Player root: {(localPlayerRoot != null ? localPlayerRoot.name : "None")}");
        }
        else
        {
            // This is the OTHER player's avatar - just show their visuals
            Debug.Log("Remote player avatar spawned");
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            // Update the entire VRPlayer parent to follow the SteamVR player root
            if (localPlayerRoot != null)
            {
                transform.position = localPlayerRoot.position;
                transform.rotation = localPlayerRoot.rotation;
            }
            
            // Update local transforms and send to network
            UpdateLocalTracking();
        }
        else
        {
            // Apply network values to transforms for remote player
            ApplyNetworkTransforms();
        }
    }

    private void UpdateLocalTracking()
    {
        // Update head (relative to VRPlayer parent)
        if (localCamera != null && headTransform != null)
        {
            headTransform.position = localCamera.position;
            headTransform.rotation = localCamera.rotation;
            
            // Send to network
            UpdateHeadServerRpc(localCamera.position, localCamera.rotation);
        }

        // Update left hand
        if (leftControllerPose != null && leftHandTransform != null)
        {
            leftHandTransform.position = leftControllerPose.transform.position;
            leftHandTransform.rotation = leftControllerPose.transform.rotation;
            
            UpdateLeftHandServerRpc(leftControllerPose.transform.position, leftControllerPose.transform.rotation);
        }

        // Update right hand
        if (rightControllerPose != null && rightHandTransform != null)
        {
            rightHandTransform.position = rightControllerPose.transform.position;
            rightHandTransform.rotation = rightControllerPose.transform.rotation;
            
            UpdateRightHandServerRpc(rightControllerPose.transform.position, rightControllerPose.transform.rotation);
        }
    }

    private void ApplyNetworkTransforms()
    {
        // Apply synced values to transforms
        if (headTransform != null)
        {
            headTransform.position = Vector3.Lerp(headTransform.position, netHeadPos.Value, Time.deltaTime * 10f);
            headTransform.rotation = Quaternion.Lerp(headTransform.rotation, netHeadRot.Value, Time.deltaTime * 10f);
        }

        if (leftHandTransform != null)
        {
            leftHandTransform.position = Vector3.Lerp(leftHandTransform.position, netLeftHandPos.Value, Time.deltaTime * 10f);
            leftHandTransform.rotation = Quaternion.Lerp(leftHandTransform.rotation, netLeftHandRot.Value, Time.deltaTime * 10f);
        }

        if (rightHandTransform != null)
        {
            rightHandTransform.position = Vector3.Lerp(rightHandTransform.position, netRightHandPos.Value, Time.deltaTime * 10f);
            rightHandTransform.rotation = Quaternion.Lerp(rightHandTransform.rotation, netRightHandRot.Value, Time.deltaTime * 10f);
        }
    }

    [ServerRpc]
    private void UpdateHeadServerRpc(Vector3 pos, Quaternion rot)
    {
        netHeadPos.Value = pos;
        netHeadRot.Value = rot;
    }

    [ServerRpc]
    private void UpdateLeftHandServerRpc(Vector3 pos, Quaternion rot)
    {
        netLeftHandPos.Value = pos;
        netLeftHandRot.Value = rot;
    }

    [ServerRpc]
    private void UpdateRightHandServerRpc(Vector3 pos, Quaternion rot)
    {
        netRightHandPos.Value = pos;
        netRightHandRot.Value = rot;
    }
}