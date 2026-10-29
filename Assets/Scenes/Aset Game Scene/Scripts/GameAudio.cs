using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [SerializeField] private AudioClip lobbyBgm;
    [SerializeField] private AudioClip gameBgm;
    [SerializeField] private AudioClip walk;
    [SerializeField] private AudioClip jump;
    [SerializeField] private AudioClip death;
    [SerializeField] private AudioClip keyPickup;
    [SerializeField] private AudioClip gateOpen;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip bouncer;
    [SerializeField] private AudioClip buttonTap;

    private readonly List<Button> buttons = new();
    private AudioSource musicSource;
    private AudioSource effectsSource;
    private AudioSource walkSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        musicSource = CreateSource(true);
        effectsSource = CreateSource(false);
        walkSource = CreateSource(false);
        walkSource.loop = true;
        walkSource.clip = walk;
    }

    private void Start()
    {
        AudioClip music = SceneManager.GetActiveScene().name == "Lobby Scene"
            ? lobbyBgm
            : gameBgm;

        if (music != null)
        {
            musicSource.clip = music;
            musicSource.Play();
        }

        foreach (Button button in FindObjectsByType<Button>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            RegisterButton(button);
    }

    private void OnDestroy()
    {
        foreach (Button button in buttons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayButtonTap);
        }

        if (Instance == this)
            Instance = null;
    }

    public void SetWalking(bool walking)
    {
        if (walkSource == null || walk == null)
            return;

        if (walking && !walkSource.isPlaying)
            walkSource.Play();
        else if (!walking && walkSource.isPlaying)
            walkSource.Stop();
    }

    public void RegisterButton(Button button)
    {
        if (button == null || buttons.Contains(button))
            return;

        button.onClick.AddListener(PlayButtonTap);
        buttons.Add(button);
    }

    public void PlayJump() => PlayEffect(jump);
    public void PlayDeath() => PlayEffect(death);
    public void PlayKeyPickup() => PlayEffect(keyPickup);
    public void PlayGateOpen() => PlayEffect(gateOpen);
    public void PlayWin() => PlayEffect(win);
    public void PlayBouncer() => PlayEffect(bouncer);

    private void PlayButtonTap() => PlayEffect(buttonTap);

    private void PlayEffect(AudioClip clip)
    {
        if (clip != null && effectsSource != null)
            effectsSource.PlayOneShot(clip);
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }
}
