using UnityEngine;

public class PlaySFX : MonoBehaviour
{
    [SerializeField]public AudioSource audioSource;
    [SerializeField]public AudioClip DealSound;
    [SerializeField]public AudioClip HitSound;
    [SerializeField]public AudioClip DiscardSound;
   

    public void PlaySound( AudioClip currentSound)
    {
        if(currentSound==HitSound)
        {
            audioSource.volume=1.5f;
        }
        else
        {
            audioSource.volume=1f;
        }
        audioSource.PlayOneShot(currentSound);
    }
    public void Deal()
    {
        PlaySound(DealSound);
    }
    public void Discard()
    {
        PlaySound(DiscardSound);
    }
    public void Hit()
    {
        Invoke("HitDelay",.3f);
    }
    void HitDelay()
    {
        PlaySound(HitSound);
    }
}
