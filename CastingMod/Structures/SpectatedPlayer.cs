using Photon.Realtime;
using UnityEngine;

namespace Structures
{
    public struct SpectatedPlayer
    {
        public NetPlayer Player;
        public string Name;
        public bool IsTagged;
        public string Team;
        public Transform Transform;
    }
}
