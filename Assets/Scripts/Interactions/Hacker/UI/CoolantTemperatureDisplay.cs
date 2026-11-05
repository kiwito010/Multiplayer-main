using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoolantTemperatureDisplay : MonoBehaviour {

    // VARIABLES:

    [Header("Referencias")]
    [SerializeField] private NetworkComputer networkComputer;
    [SerializeField] private GameObject temperaturePanel;
    [SerializeField] private Slider temperatureBar;
    [SerializeField] private Image temperatureFill;
    [SerializeField] private TextMeshProUGUI temperatureText;

    // MÉTODOS:

    private void Update() {
        if (networkComputer == null ||
            networkComputer.Object == null ||
            !networkComputer.Object.IsValid) {

            if (temperaturePanel != null)
                temperaturePanel.SetActive(false);

            return;
        }

        bool showTemperature =
            networkComputer.IsHackRunning &&
            !networkComputer.IsCompleted;

        if (temperaturePanel != null) {
            temperaturePanel.SetActive(showTemperature);
        }

        if (!showTemperature)
            return;

        float temperature = Mathf.Clamp(
            networkComputer.Temperature,
            0f,
            100f
        );

        if (temperatureBar != null) {
            temperatureBar.SetValueWithoutNotify(temperature);
        }

        Color temperatureColor;

        if (temperature >= 80f) {
            temperatureColor = Color.red;
        } else if (temperature >= 50f) {
            temperatureColor = Color.yellow;
        } else {
            temperatureColor = Color.green;
        }

        if (temperatureFill != null) {
            temperatureFill.color = temperatureColor;
        }

        if (temperatureText != null) {
            temperatureText.color = temperatureColor;
            temperatureText.text =
                "TEMPERATURA " + Mathf.RoundToInt(temperature) + "%";
        }
    }
}
