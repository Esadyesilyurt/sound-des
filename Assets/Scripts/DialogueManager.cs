using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private string[] currentDialogueLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;

    private TextMeshProUGUI dialogueText;
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Disable();
    }

    private void Start()
    {
        // GameManager'dan dialogueText'i al
        if (GameManager.Instance != null)
        {
            dialogueText = GameManager.Instance.dialogueText;
        }
        else
        {
            Debug.LogWarning("DialogueManager: GameManager.Instance bulunamadı!");
        }

        if (dialogueText == null)
        {
            Debug.LogError("DialogueManager: dialogueText referansı null! GameManager'da Dialogue Text'i atayın.");
        }
    }

    public void StartDialogue(string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: Boş diyalog satırları!");
            return;
        }

        if (dialogueText == null)
        {
            Debug.LogError("DialogueManager: dialogueText null! Diyalog gösterilemiyor.");
            return;
        }

        currentDialogueLines = lines;
        currentLineIndex = 0;
        DisplayNextLine();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (GameManager.Instance == null || !GameManager.Instance.isInDialogue)
            return;

        if (dialogueText == null)
            return;

        if (isTyping)
        {
            // Yazma animasyonunu atla
            StopAllCoroutines();
            if (currentLineIndex > 0 && currentLineIndex <= currentDialogueLines.Length)
            {
                dialogueText.text = currentDialogueLines[currentLineIndex - 1];
            }
            isTyping = false;
        }
        else
        {
            DisplayNextLine();
        }
    }

    private void DisplayNextLine()
    {
        if (currentDialogueLines == null || currentLineIndex >= currentDialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        if (dialogueText == null)
        {
            Debug.LogError("DialogueManager: dialogueText null!");
            EndDialogue();
            return;
        }

        StartCoroutine(TypeText(currentDialogueLines[currentLineIndex]));
        currentLineIndex++;
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.05f);
        }

        isTyping = false;
    }

    private void EndDialogue()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndDialogue();
        }
    }
}

