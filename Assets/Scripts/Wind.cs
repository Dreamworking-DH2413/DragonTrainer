using UnityEngine;

public class WindSounds : MonoBehaviour
{
    public Rigidbody rb;
    public AudioClip wind;

    private AudioSource windAudioSource;

    [Header("Wind Sound Settings")]
    public float minSpeed = 0f;
    public float maxSpeed = 50f;
    public float minVolume = 0.1f;
    public float maxVolume = 0.7f;
    public float minPitch = 0.8f;
    public float maxPitch = 1.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        windAudioSource = GetComponent<AudioSource>();
        if (windAudioSource == null)
            windAudioSource = gameObject.AddComponent<AudioSource>();

        windAudioSource.clip = wind;
        windAudioSource.loop = true;
        windAudioSource.volume = minVolume;
        windAudioSource.pitch = minPitch;
        windAudioSource.Play();
    }

    void Update()
    {
        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;

       

        float speedRatio = Mathf.Clamp01((speed - minSpeed) / (maxSpeed - minSpeed));

        windAudioSource.volume = Mathf.Lerp(minVolume, maxVolume, speedRatio);
        windAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);

         // Debug every 300 frames
        if (Time.frameCount % 300 == 0)
        {
            Debug.Log($"Wind Sound - Speed: {speed:F2}, Volume: {windAudioSource.volume:F2}, Pitch: {windAudioSource.pitch:F2}");
        }
    }
}
