using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HouseDoor : MonoBehaviour
{
    [Header("Ev Ayarları")]
    public float interactDistance = 2f;

    [Header("Görsel İpuçları")]
    public GameObject interactPrompt;

    private Transform player;
    private bool canEnter = false;
    private Collider doorCollider;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        doorCollider = GetComponent<Collider>();
        
        // Collider yoksa ekle
        if (doorCollider == null)
        {
            doorCollider = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("HouseDoor için Collider eklendi. Lütfen Unity'de ayarlayın.");
        }
        
        // Collider'ı trigger yap (karakter içinden geçebilsin)
        if (doorCollider != null)
        {
            doorCollider.isTrigger = true;
        }
        
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (player == null || GameManager.Instance == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        canEnter = distance <= interactDistance && GameManager.Instance.mustGoHome;

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(canEnter && !GameManager.Instance.isInDialogue);
        }
    }

    public bool CanEnter()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("HouseDoor: GameManager.Instance null!");
            return false;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null)
            {
                Debug.LogWarning("HouseDoor: Player bulunamadı!");
                return false;
            }
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool canEnterNow = distance <= interactDistance && GameManager.Instance.mustGoHome;

        if (!canEnterNow)
        {
            Debug.Log($"HouseDoor: Giriş yapılamıyor - Distance: {distance}, mustGoHome: {GameManager.Instance.mustGoHome}");
        }

        return canEnterNow;
    }

    public void Enter()
    {
        Debug.Log("HouseDoor: Enter() çağrıldı!");
        
        if (!CanEnter())
        {
            Debug.LogWarning("HouseDoor: CanEnter() false döndü!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("HouseDoor: GameManager.Instance null!");
            return;
        }

        Debug.Log("HouseDoor: Uyuma işlemi başlatılıyor...");
        // Uyuma işlemi - ekran kararıp aydınlanacak
        GameManager.Instance.Sleep();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}

