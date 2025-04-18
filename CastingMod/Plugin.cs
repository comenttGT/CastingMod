using BepInEx;
using Utilities;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
namespace CastingMod
{






    [BepInPlugin("Comentt.CM", "Casting Mod", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {

        public void Start() => GorillaTagger.OnPlayerSpawned(OnInit);

        private void OnInit()
        {
            Harmony h = new Harmony("ComenttsCastingMod");
            h.PatchAll(Assembly.GetExecutingAssembly());
            GameObject parent = new GameObject
            {
                name = "CastingMod"
            };
            GameObject cb = new GameObject
            {
                name = "Callbacks"
            };
            GameObject cast = new GameObject
            {
                name = "Caster"
            };
            GameObject t = new GameObject
            {
                name = "Timer"
            };
            cb.transform.SetParent(parent.transform);
            cast.transform.SetParent(parent.transform);
            t.transform.SetParent(parent.transform);
            cb.AddComponent<Callbacks>();
            t.AddComponent<Timer>();
            Caster spec_spec = cast.AddComponent<Caster>();
            spec_spec.GTM = GameObject.Find("Gorilla Tag Manager").GetComponent<GorillaTagManager>();
        }
    }
}
