using UnityEngine;

public class Sheep : MonoBehaviour
{
    private DissolveControl burnControl;
    private float dissolveAmount = 0f;
    public AudioSource audioSource;
    void Start()
    {
        burnControl = GetComponent<DissolveControl>();
        audioSource = GetComponent<AudioSource>();
    }

    public void BurnStep()
    {
        dissolveAmount += Time.deltaTime * 0.5f;
        Debug.Log(dissolveAmount);
        burnControl.SetDissolveBoth(dissolveAmount);

        if (dissolveAmount >= 0.75f)
            Die();
    }

    public void Die()
    {
        Destroy(gameObject);
    }
    
    public void PlayHitSound()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
