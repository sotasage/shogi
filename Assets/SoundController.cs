using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    [SerializeField] List<AudioClip> bgm;
    [SerializeField] List<AudioClip> se;

    AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //BGMçƒê∂
    public void PlayBGM(int no)
    {
        audioSource.clip = bgm[no];
        audioSource.Play();
    }

    //SEçƒê∂
    public void PlaySE(int no)
    {
        audioSource.PlayOneShot(se[no]);
    }
}
