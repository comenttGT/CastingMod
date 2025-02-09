using UnityEngine;
using Photon.Pun;
using System;
using Photon.Realtime;
using System.Threading.Tasks;

namespace Utilities
{
    public class Callbacks : MonoBehaviourPunCallbacks
    {
        public static Callbacks instance { get; internal set; }

        public bool isInfection { get; internal set; }
        public string GameMode { get; internal set; }


        public event Action<Player> OnPlayerJoined;
        public event Action<Player> OnPlayerLeft;
        public event Action OnLocalLeftRoom;
        public event Action OnLocalJoinedRoom;
        //public event Action<Player, Player> OnPlayerTagged;


        public void Start()
        {
            if (instance == null)
                instance = this;
        }

        private async void RetryGettingGameMode()
        {
            await Task.Delay(500);
            if (!String.IsNullOrEmpty(GorillaTagManager.instance.GameModeName()))
                SetGamemode();
            else
                RetryGettingGameMode();
        }




        public override void OnPlayerEnteredRoom(Player newPlayer) => OnPlayerJoined(newPlayer);
        public override void OnPlayerLeftRoom(Player otherPlayer) => OnPlayerLeft(otherPlayer);
        public override void OnLeftRoom()
        {
            OnLocalLeftRoom();
            GameMode = "";
            isInfection = false;
        }
        public override void OnJoinedRoom()
        {
            OnLocalJoinedRoom();
            RetryGettingGameMode();
        }

        public bool isPlayerTagged(Player pylr)
        {
            VRRig temp = GorillaTagManager.instance.FindPlayerVRRig(pylr);
            return temp.currentMatIndex != 0;
        }

        private void SetGamemode()
        {
            string temp = GorillaTagManager.instance.GameModeName();
            GameMode = temp;
            if (temp == "INFECTION")
                isInfection = true;
            else
                isInfection = false;
        }
    }
}
