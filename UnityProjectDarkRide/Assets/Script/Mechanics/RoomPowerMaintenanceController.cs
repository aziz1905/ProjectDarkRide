using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kontroler Kelistrikan & Lampu Ruangan (1 Ruangan 1 Panel & Fuse).
/// 100% Pasangan 1-to-1 antara Spot Light dengan BolaPijar milik lampu itu sendiri.
/// </summary>
public class RoomPowerMaintenanceController : MonoBehaviour
{
    [Header("Room Power Components (Opsional)")]
    [Tooltip("Drag Sakelar Listrik ruangan ini ke sini (Boleh dikosongkan jika ruangan selalu nyala)")]
    [SerializeField] private PowerSwitchInteractable roomPowerSwitch;

    [Tooltip("Drag Kotak Sekring (FuseBox) ruangan ini ke sini (Boleh dikosongkan jika tidak ada fuse)")]
    [SerializeField] private FuseBoxController roomFuseBox;

    [Header("3D BolaPijar Glow Settings (Pijar Kaca Otomatis)")]
    [Tooltip("Warna pijar emisi kaca bola lampu 3D")]
    [SerializeField] private Color emissionColor = new Color(1f, 0.85f, 0.5f); // Kuning Warm

    [Tooltip("Kekuatan cahaya pijar kaca 3D")]
    [SerializeField] private float normalEmissionGlow = 2.5f;

    private class LampPair
    {
        public Light lightComponent;
        public Material bulbMaterial;
    }

    private List<LampPair> lampPairs = new List<LampPair>();
    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
    private bool isRoomPowerActive = false;

    private void Awake()
    {
        PairLampsAndBulbs();
    }

    private void Start()
    {
        if (roomPowerSwitch != null)
        {
            roomPowerSwitch.OnPowerStateChanged.AddListener(OnSwitchToggled);
        }

        UpdateRoomPowerState(true);
    }

    private void OnDestroy()
    {
        if (roomPowerSwitch != null)
        {
            roomPowerSwitch.OnPowerStateChanged.RemoveListener(OnSwitchToggled);
        }
    }

    /// <summary>
    /// Memasangkan setiap Spot Light dengan objek 3D 'BolaPijar' pasangannya secara presisi
    /// </summary>
    public void PairLampsAndBulbs()
    {
        lampPairs.Clear();

        Light[] allLights = GetComponentsInChildren<Light>(true);

        foreach (Light l in allLights)
        {
            if (l == null) continue;

            LampPair pair = new LampPair();
            pair.lightComponent = l;

            Transform bulbFolder = l.transform.parent != null ? l.transform.parent : l.transform;
            Renderer bulbRend = null;

            // 1. Cek langsung anak folder Bulb yang bernama BolaPijar
            Transform bpChild = bulbFolder.Find("BolaPijar");
            if (bpChild != null)
            {
                bulbRend = bpChild.GetComponent<Renderer>();
            }

            // 2. Jika tidak ada di Find langsung, cari di seluruh sub-objek Bulb
            if (bulbRend == null)
            {
                Renderer[] rends = bulbFolder.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    if (r != null && r.gameObject.name.ToLower().Contains("bolapijar"))
                    {
                        bulbRend = r;
                        break;
                    }
                }
            }

            if (bulbRend != null)
            {
                pair.bulbMaterial = bulbRend.material;
                pair.bulbMaterial.EnableKeyword("_EMISSION");
                pair.bulbMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            lampPairs.Add(pair);
        }
    }

    private void OnSwitchToggled(bool isSwitchOn)
    {
        UpdateRoomPowerState(false);
    }

    private void Update()
    {
        UpdateRoomPowerState(false);
    }

    private void UpdateRoomPowerState(bool forceUpdate)
    {
        bool switchOn = roomPowerSwitch != null ? roomPowerSwitch.IsPowerOn : true;
        bool fuseNormal = roomFuseBox != null ? !roomFuseBox.IsFuseBroken : true;

        bool shouldPowerBeOn = switchOn && fuseNormal;

        if (shouldPowerBeOn != isRoomPowerActive || forceUpdate)
        {
            isRoomPowerActive = shouldPowerBeOn;

            Color activeColor = (isRoomPowerActive && normalEmissionGlow > 0.01f)
                ? emissionColor * Mathf.LinearToGammaSpace(normalEmissionGlow)
                : Color.black;

            foreach (var pair in lampPairs)
            {
                if (pair == null) continue;

                if (pair.lightComponent != null)
                {
                    pair.lightComponent.enabled = isRoomPowerActive;
                }

                if (pair.bulbMaterial != null)
                {
                    pair.bulbMaterial.SetColor(EmissionColorProp, activeColor);
                }
            }
        }
    }

    public void SetRoomPower(bool enablePower)
    {
        if (roomPowerSwitch != null)
        {
            roomPowerSwitch.SetPowerState(enablePower);
        }
        UpdateRoomPowerState(true);
    }
}
