using CastingMod;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace Utilities
{
    public class NameTag : MonoBehaviour
    {
        public static NameTag latestNameTag;
        private TextMeshPro m_TextMeshPro;
        private NetPlayer associatedPlayer;
        private Color taggedColor => new Color(0.15f, 0, 0, 0.5f);

        public void Start()
        {
            latestNameTag = this;
            m_TextMeshPro = gameObject.AddComponent<TextMeshPro>();
            m_TextMeshPro.text = $"<size=2>{associatedPlayer.NickName}</size>";
            m_TextMeshPro.alignment = TextAlignmentOptions.Center;
            m_TextMeshPro.fontStyle = FontStyles.Bold;
            m_TextMeshPro.characterSpacing = 0.05f;
            m_TextMeshPro.canvas.renderMode = RenderMode.WorldSpace;
            m_TextMeshPro.canvas.worldCamera = Caster.instance.FirstPersonCam.Camera;
            //m_TextMeshPro.font = TMP_FontAsset.CreateFontAsset(GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/CodeOfConduct_Group/CodeOfConduct/COC Text").GetComponent<Text>().font);
        }

        public void LateUpdate()
        {
            m_TextMeshPro.enabled = true;
            transform.rotation = Caster.instance.FirstPersonCam.Object.transform.rotation;
            if (Caster.instance.isPlayerTagged(associatedPlayer))
                m_TextMeshPro.color = taggedColor;
            else
                m_TextMeshPro.color = Caster.instance.GetPlayerColor(associatedPlayer);

            m_TextMeshPro.enabled = Caster.instance.showNameTags;

        }
        public void SetPlayer(NetPlayer plyr) => associatedPlayer = plyr;

    }
}
