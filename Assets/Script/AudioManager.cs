using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Daftar Efek Suara")]
    public AudioClip brakeClip;
    public AudioClip crashClip;
    public AudioClip exitClip;
    public AudioClip honkClip;

    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Menambahkan komponen AudioSource secara otomatis via skrip
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Fungsi-fungsi publik untuk dipanggil oleh mobil
    public void PlayBrake() { if (brakeClip) sfxSource.PlayOneShot(brakeClip); }
    public void PlayCrash() { if (crashClip) sfxSource.PlayOneShot(crashClip); }
    public void PlayExit() { if (exitClip) sfxSource.PlayOneShot(exitClip); }
    public void PlayHonk() { if (honkClip) sfxSource.PlayOneShot(honkClip); }
}