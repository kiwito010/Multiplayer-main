using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Va en la RAÍZ del prefab de cada fila de la lista de jugadores (tu prefab "PlayerEntry").
// Los tres campos de abajo son las partes de la fila: arrastralas desde el prefab.
// Si dejás alguno vacío, el script intenta encontrarlo solo.
public class LobbyPlayerEntry : MonoBehaviour
{
    [Header("Partes de la fila (arrastrar desde el prefab)")]
    public TMP_Text pingText;     // el texto del ping (el que dice "Xms")
    public TMP_Text nameText;     // el texto del nombre del rol (el que dice "Nombre")
    public Image avatarImage;     // la imagen donde va el avatar del rol (el cuadrado)

    private void Awake()
    {
        AutoBindMissing();
    }

    // Busca solo las partes que no estén asignadas.
    public void AutoBindMissing()
    {
        if (pingText == null || nameText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

            if (pingText == null)
            {
                foreach (TMP_Text text in texts)
                {
                    if (text == nameText) continue;

                    bool looksLikePing = text.name.ToLowerInvariant().Contains("ping")
                                         || text.text.Trim().ToLowerInvariant().EndsWith("ms");
                    if (looksLikePing)
                    {
                        pingText = text;
                        break;
                    }
                }
            }

            if (nameText == null)
            {
                foreach (TMP_Text text in texts)
                {
                    if (text != pingText)
                    {
                        nameText = text;
                        break;
                    }
                }
            }
        }

        if (avatarImage == null)
        {
            float smallestArea = float.MaxValue;

            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject == gameObject) continue; // el fondo de la fila no cuenta

                if (image.name.ToLowerInvariant().Contains("avatar"))
                {
                    avatarImage = image;
                    break;
                }

                float area = image.rectTransform.rect.width * image.rectTransform.rect.height;
                if (area < smallestArea)
                {
                    smallestArea = area;
                    avatarImage = image;
                }
            }
        }
    }

    public void SetPing(string text, Color color)
    {
        if (pingText == null) return;

        // Un degradé de vértices taparía el color del ping.
        pingText.enableVertexGradient = false;
        pingText.text = text;
        pingText.color = color;
    }

    public void SetName(string text)
    {
        if (nameText != null) nameText.text = text;
    }

    // Si no hay imagen para el rol, el avatar queda como está.
    public void SetAvatar(Sprite sprite)
    {
        if (avatarImage == null || sprite == null) return;

        avatarImage.sprite = sprite;
        avatarImage.color = Color.white;
        avatarImage.preserveAspect = true;
        avatarImage.enabled = true;
    }
}