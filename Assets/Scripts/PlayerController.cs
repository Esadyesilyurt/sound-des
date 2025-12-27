using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Kamera")]
    public Transform cameraTarget;
    public float cameraDistance = 5f;
    public float cameraHeight = 2f;

    [Header("Animasyon Ayarları")]
    [Tooltip("Idle animasyonunun tam ismi (örn: 'Idle', 'metarig|Idle')")]
    public string idleAnimationName = "Idle";
    [Tooltip("Yürüme animasyonunun tam ismi (örn: 'Walk', 'metarig|Walk')")]
    public string walkAnimationName = "Walk";

    private CharacterController characterController;
    private Animator animator;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Camera mainCamera;
    private InputSystem_Actions inputActions;
    private float currentSpeed = 0f;
    private bool isWalking = false;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        inputActions = new InputSystem_Actions();
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Disable();
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.isIntroActive || 
            GameManager.Instance.isInDialogue || 
            GameManager.Instance.isSleeping ||
            GameManager.Instance.isGameEnded)
        {
            return;
        }

        MovePlayer();
        UpdateCamera();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("GameManager.Instance is null!");
                return;
            }

            if (!GameManager.Instance.canInteract || GameManager.Instance.isInDialogue)
                return;

            TryInteract();
        }
    }

    private void MovePlayer()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Kamera yönüne göre hareket (sadece yatay düzlemde)
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        
        // Y eksenini sıfırla (sadece yatay hareket)
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        
        // Normalize et
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Hareket yönünü hesapla
        moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            // Karakteri hareket yönüne çevir (sadece Y ekseni etrafında)
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Vector3 eulerAngles = targetRotation.eulerAngles;
            eulerAngles.x = 0f; // X eksenini sıfırla
            eulerAngles.z = 0f; // Z eksenini sıfırla
            targetRotation = Quaternion.Euler(eulerAngles);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Hareket
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
            currentSpeed = moveSpeed;
            
            // Yürüme animasyonunu oynat
            if (!isWalking && animator != null)
            {
                PlayAnimation(walkAnimationName);
                isWalking = true;
            }
        }
        else
        {
            currentSpeed = 0f;
            
            // Idle animasyonunu oynat
            if (isWalking && animator != null)
            {
                PlayAnimation(idleAnimationName);
                isWalking = false;
            }
        }
    }

    private void UpdateCamera()
    {
        if (mainCamera == null)
            return;

        // Eğer cameraTarget kullanılıyorsa
        if (cameraTarget != null)
        {
            // Kamera karakterin arkasında ve yukarıda olmalı
            Vector3 targetPosition = transform.position - transform.forward * cameraDistance + Vector3.up * cameraHeight;
            cameraTarget.position = Vector3.Lerp(cameraTarget.position, targetPosition, Time.deltaTime * 5f);
            
            // Kamera karaktere bakmalı
            Vector3 lookAtPosition = transform.position + Vector3.up * 1.5f;
            cameraTarget.LookAt(lookAtPosition);
            
            // Main Camera'yı da güncelle
            if (mainCamera.transform != cameraTarget)
            {
                mainCamera.transform.position = cameraTarget.position;
                mainCamera.transform.rotation = cameraTarget.rotation;
            }
        }
        else
        {
            // CameraTarget yoksa direkt Main Camera'yı kontrol et
            Vector3 targetPosition = transform.position - transform.forward * cameraDistance + Vector3.up * cameraHeight;
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * 5f);
            
            Vector3 lookAtPosition = transform.position + Vector3.up * 1.5f;
            mainCamera.transform.LookAt(lookAtPosition);
        }
    }

    private void TryInteract()
    {
        // Etkileşim mesafesi
        float interactDistance = 3f;
        
        // Hem trigger hem de normal collider'ları kontrol et
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactDistance, Physics.AllLayers, QueryTriggerInteraction.Collide);

        NPCController closestNPC = null;
        HouseDoor closestDoor = null;
        float closestNPCDistance = float.MaxValue;
        float closestDoorDistance = float.MaxValue;

        foreach (Collider col in nearbyColliders)
        {
            // Player'ın kendi collider'ını atla
            if (col.transform == transform)
                continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);

            // NPC ile etkileşim
            NPCController npc = col.GetComponent<NPCController>();
            if (npc != null && npc.IsInteractable())
            {
                if (distance < closestNPCDistance)
                {
                    closestNPC = npc;
                    closestNPCDistance = distance;
                }
            }

            // Ev kapısı ile etkileşim
            HouseDoor houseDoor = col.GetComponent<HouseDoor>();
            if (houseDoor != null)
            {
                if (houseDoor.CanEnter() && distance < closestDoorDistance)
                {
                    closestDoor = houseDoor;
                    closestDoorDistance = distance;
                }
            }
        }

        // Önce NPC, sonra kapı
        if (closestNPC != null)
        {
            Debug.Log("PlayerController: NPC ile etkileşim başlatılıyor...");
            closestNPC.Interact();
            return;
        }

        if (closestDoor != null)
        {
            Debug.Log("PlayerController: Ev kapısı ile etkileşim başlatılıyor...");
            closestDoor.Enter();
            return;
        }
        
        Debug.Log("PlayerController: Yakında etkileşilebilir bir şey yok!");
    }

    private void PlayAnimation(string animationName)
    {
        if (animator == null || string.IsNullOrEmpty(animationName))
            return;

        // Animasyon ismini kontrol et ve oynat
        if (HasAnimation(animationName))
        {
            animator.Play(animationName);
        }
        else
        {
            // Farklı isimlerle dene
            string[] possibleNames = {
                animationName,
                "metarig|" + animationName,
                "Base Layer." + animationName,
                animationName.ToLower(),
                animationName.ToUpper()
            };

            foreach (string name in possibleNames)
            {
                if (HasAnimation(name))
                {
                    animator.Play(name);
                    return;
                }
            }

            Debug.LogWarning($"Animasyon bulunamadı: {animationName}. Lütfen Unity'de Animator Controller'ı kontrol edin.");
        }
    }

    private bool HasAnimation(string animationName)
    {
        if (animator == null || string.IsNullOrEmpty(animationName))
            return false;

        // Animator Controller'daki tüm animasyonları kontrol et
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName || clip.name.Contains(animationName))
            {
                return true;
            }
        }
        return false;
    }
}

