using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Referansları")]
    public GameObject introUI;
    public TextMeshProUGUI introText;
    public GameObject warningUI;
    public TextMeshProUGUI warningText;
    public GameObject dialogueUI;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI dialogueSpeakerName;
    public GameObject timeUI;
    public TextMeshProUGUI timeText;
    public GameObject fadePanel; // Ekran kararma/aydınlanma için panel
    public GameObject endingUI; // Bitiş ekranı
    public TextMeshProUGUI endingText; // Bitiş metni

    [Header("Oyun Ayarları")]
    public float dayDuration = 300f; // Günün süresi (saniye)
    public float eveningTime = 0.7f; // Akşam olma zamanı (0.7 = günün %70'i)
    public int currentDay = 1;
    public int requiredConversationsDay1 = 2;
    public int requiredConversationsDay2 = 2;

    [Header("Oyun Durumu")]
    public bool isIntroActive = true;
    public bool canInteract = false;
    public bool mustGoHome = false;
    public bool isInDialogue = false;
    public bool isSleeping = false;
    public bool isGameEnded = false;

    [Header("Bitiş Ekranı Ayarları")]
    public Transform villageCenter; // Köyün merkez pozisyonu (kamera buraya bakacak)
    public float cameraHeight = 20f; // Tepeden bakış yüksekliği
    public float cameraDistance = 30f; // Kamera mesafesi

    private float currentTime = 0f; // 0-1 arası (0 = sabah, 1 = gece)
    private int conversationsCompleted = 0;
    private List<GameObject> day1NPCs = new List<GameObject>();
    private List<GameObject> day2NPCs = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ShowIntro();
        FindNPCs();
    }

    private void Update()
    {
        // Intro veya bitiş ekranı aktifken zaman ilerlemesin
        if (isIntroActive || isGameEnded)
        {
            return;
        }

        if (isSleeping || isInDialogue)
        {
            // Zaman UI'ı güncelle ama zaman ilerlemesin
            UpdateTimeUI();
            return;
        }

        // Zaman ilerlemesi
        currentTime += Time.deltaTime / dayDuration;
        
        // Zamanı 1'den fazla olmasın
        if (currentTime > 1f)
        {
            currentTime = 1f;
        }

        // Akşam kontrolü
        if (currentTime >= eveningTime && !mustGoHome)
        {
            mustGoHome = true;
            ShowWarning("Akşam oluyor! Eve gitmen gerekiyor.");
            DisableAllNPCs();
        }

        // Zaman UI güncelleme
        UpdateTimeUI();
    }

    private void ShowIntro()
    {
        isIntroActive = true;
        canInteract = false;
        introUI.SetActive(true);
        // Hikaye özeti buraya gelecek
        introText.text = "KöyDot... Kızgın kumların ortasında, zamanın durduğu yer. Ama bugün zaman durmadı, aksine çıldırdı. Köy meydanında beliren kırmızı, devasa, kemiksiz bir varlık, fizik kurallarına aykırı hareketler sergiliyor. Ne bir yüzü var ne de bir iskeleti... Sürekli eğilip bükülüyor, kolları gökyüzüne lanet okurcasına savruluyor ve asla yorulmuyor. Köylüler buna 'Kemiksiz Kıyamet' adını verdi. Rüzgarın oğlu olduğuna ve köyü kırbaçlamaya geldiğine inanıyorlar. Sen Adnan'sın. Bu dans eden dehşetin sırrını çözmek zorundasın.";
    }

    public void SkipIntro()
    {
        isIntroActive = false;
        canInteract = true;
        if (introUI != null)
            introUI.SetActive(false);
    }

    private void FindNPCs()
    {
        // NPC'leri tag veya isim ile bul
        GameObject[] allNPCs = GameObject.FindGameObjectsWithTag("NPC");
        foreach (GameObject npc in allNPCs)
        {
            NPCController npcController = npc.GetComponent<NPCController>();
            if (npcController != null)
            {
                if (npcController.day == 1)
                    day1NPCs.Add(npc);
                else if (npcController.day == 2)
                    day2NPCs.Add(npc);
            }
        }
    }

    public void StartDialogue(string speakerName, string[] dialogueLines)
    {
        // Null kontrolleri
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"NPC {speakerName} için diyalog satırları boş!");
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogError("DialogueUI referansı atanmamış! GameManager'da Dialogue UI'ı atayın.");
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager bulunamadı! Sahneye DialogueManager GameObject'i ekleyin.");
            return;
        }

        isInDialogue = true;
        dialogueUI.SetActive(true);
        
        if (dialogueSpeakerName != null)
        {
            dialogueSpeakerName.text = speakerName;
        }
        
        DialogueManager.Instance.StartDialogue(dialogueLines);
    }

    public void EndDialogue()
    {
        isInDialogue = false;
        dialogueUI.SetActive(false);
        conversationsCompleted++;

        // Gün 1 kontrolü
        if (currentDay == 1 && conversationsCompleted >= requiredConversationsDay1)
        {
            mustGoHome = true;
            ShowWarning("Akşam oluyor! Eve gitmen gerekiyor.");
            DisableAllNPCs();
        }
        // Gün 2 kontrolü
        else if (currentDay == 2 && conversationsCompleted >= requiredConversationsDay2)
        {
            // Oyun bitti
            EndGame();
        }
    }

    private void DisableAllNPCs()
    {
        List<GameObject> activeNPCs = currentDay == 1 ? day1NPCs : day2NPCs;
        foreach (GameObject npc in activeNPCs)
        {
            NPCController npcController = npc.GetComponent<NPCController>();
            if (npcController != null && !npcController.hasBeenTalkedTo)
            {
                npcController.SetInteractable(false);
            }
        }
    }

    private void ShowWarning(string message)
    {
        warningUI.SetActive(true);
        warningText.text = message;
    }

    public void HideWarning()
    {
        warningUI.SetActive(false);
    }

    public void Sleep()
    {
        Debug.Log("GameManager: Sleep() çağrıldı!");
        StartCoroutine(SleepSequence());
    }

    private IEnumerator SleepSequence()
    {
        Debug.Log("GameManager: SleepSequence başladı!");
        isSleeping = true;
        canInteract = false;

        // Ekranı karart
        if (fadePanel != null)
        {
            Image fadeImage = fadePanel.GetComponent<Image>();
            if (fadeImage == null)
                fadeImage = fadePanel.AddComponent<Image>();

            fadePanel.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);

            // Fade out (kararma)
            float fadeDuration = 1f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1);
        }

        // Gün geçişi
        yield return new WaitForSeconds(0.5f);
        
        currentTime = 0f;
        currentDay++;
        conversationsCompleted = 0;
        mustGoHome = false;

        // Gün 2 NPC'lerini aktif et ve Gün 1 NPC'lerini sıfırla
        if (currentDay == 2)
        {
            // Gün 1 NPC'lerini sıfırla
            foreach (GameObject npc in day1NPCs)
            {
                NPCController npcController = npc.GetComponent<NPCController>();
                if (npcController != null)
                {
                    npcController.hasBeenTalkedTo = false;
                    npcController.SetInteractable(false);
                }
            }

            // Gün 2 NPC'lerini aktif et
            foreach (GameObject npc in day2NPCs)
            {
                NPCController npcController = npc.GetComponent<NPCController>();
                if (npcController != null)
                {
                    npcController.hasBeenTalkedTo = false;
                    npcController.SetInteractable(true);
                }
            }
        }

        // Sabah oluyor - ekranı aydınlat
        yield return new WaitForSeconds(0.5f);

        if (fadePanel != null)
        {
            Image fadeImage = fadePanel.GetComponent<Image>();
            if (fadeImage != null)
            {
                // Fade in (aydınlanma)
                float fadeDuration = 1f;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                    fadeImage.color = new Color(0, 0, 0, alpha);
                    yield return null;
                }
                fadeImage.color = new Color(0, 0, 0, 0);
                fadePanel.SetActive(false);
            }
        }

        // Oyun devam ediyor
        isSleeping = false;
        canInteract = true;
        HideWarning();
    }

    private void UpdateTimeUI()
    {
        if (timeText == null)
            return;

        int hours = Mathf.FloorToInt(currentTime * 24f);
        if (hours >= 24) hours = 23;
        string timeString = $"{hours:00}:00";
        timeText.text = timeString;
    }

    private void EndGame()
    {
        canInteract = false;
        isGameEnded = true;
        
        // Player hareketini durdur
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Kamera pozisyonunu tepeden köye bakacak şekilde ayarla
        StartCoroutine(ShowEndingSequence());
    }

    private IEnumerator ShowEndingSequence()
    {
        // Kamera geçişi için kısa bir bekleme
        yield return new WaitForSeconds(0.5f);
        
        // Kamera pozisyonunu ayarla
        Camera mainCamera = Camera.main;
        if (mainCamera != null && villageCenter != null)
        {
            // Kamera tepeden köye bakacak
            Vector3 cameraPosition = villageCenter.position + Vector3.up * cameraHeight;
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(villageCenter.position);
        }
        else if (mainCamera != null)
        {
            // VillageCenter yoksa, köyün ortasını tahmin et veya sabit bir pozisyon kullan
            // Player'ın pozisyonunu merkez olarak kullan
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player != null)
            {
                Vector3 centerPosition = player.position;
                Vector3 cameraPosition = centerPosition + Vector3.up * cameraHeight;
                mainCamera.transform.position = cameraPosition;
                mainCamera.transform.LookAt(centerPosition);
            }
        }
        
        // Kısa bir bekleme
        yield return new WaitForSeconds(1f);
        
        // Bitiş UI'sını göster
        if (endingUI != null)
        {
            endingUI.SetActive(true);
            
            if (endingText != null)
            {
                // Bitiş metni buraya gelecek (kullanıcı Unity'de doldurmalı)
                if (string.IsNullOrEmpty(endingText.text))
                {
                    endingText.text = "Oyun Bitti!";
                }
            }
        }
        else
        {
            Debug.LogWarning("EndingUI referansı atanmamış! GameManager'da Ending UI'ı atayın.");
        }
    }

    public void SkipEnding()
    {
        // Bitiş ekranını kapat (opsiyonel - buton eklenebilir)
        if (endingUI != null)
        {
            endingUI.SetActive(false);
        }
    }
}

