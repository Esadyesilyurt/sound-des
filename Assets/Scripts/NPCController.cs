using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCController : MonoBehaviour
{
    [Header("NPC Ayarları")]
    public string npcName = "Köylü";
    public int day = 1; // Hangi gün konuşulacak (1 veya 2)
    public string[] dialogueLines;

    [Header("Görsel İpuçları")]
    public GameObject interactPrompt;
    public float interactDistance = 3f;

    public bool hasBeenTalkedTo = false;
    private bool isInteractable = true;
    private Transform player;
    private Collider npcCollider;

    public bool HasBeenTalkedTo => hasBeenTalkedTo;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        npcCollider = GetComponent<Collider>();
        
        // Collider yoksa ekle
        if (npcCollider == null)
        {
            npcCollider = gameObject.AddComponent<CapsuleCollider>();
            Debug.LogWarning($"NPC {npcName} için Collider eklendi. Lütfen Unity'de ayarlayın.");
        }
        
        // NPC collider'ını trigger yap (karakter içinden geçebilsin)
        if (npcCollider != null)
        {
            npcCollider.isTrigger = true;
        }
        
        // Gün kontrolü - sadece ilgili günde aktif
        if (GameManager.Instance != null && GameManager.Instance.currentDay != day)
        {
            SetInteractable(false);
        }

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!isInteractable || hasBeenTalkedTo || GameManager.Instance == null)
            return;

        // Oyuncu yakındaysa ipucu göster
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(distance <= interactDistance && 
                                        !GameManager.Instance.isInDialogue &&
                                        !GameManager.Instance.mustGoHome &&
                                        GameManager.Instance.currentDay == day);
            }
        }
    }

    public bool IsInteractable()
    {
        if (GameManager.Instance == null)
            return false;

        return isInteractable && 
               !hasBeenTalkedTo && 
               !GameManager.Instance.mustGoHome &&
               GameManager.Instance.currentDay == day;
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    public void Interact()
    {
        if (!IsInteractable())
        {
            Debug.Log($"NPC {npcName} etkileşilebilir değil!");
            return;
        }

        // Diyalog satırları kontrolü
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"NPC {npcName} için diyalog satırları boş! NPCController'da Dialogue Lines dizisini doldurun.");
            return;
        }

        hasBeenTalkedTo = true;
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        // Diyalog başlat
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartDialogue(npcName, dialogueLines);
        }
        else
        {
            Debug.LogError("GameManager.Instance null! Sahneye GameManager ekleyin.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}

