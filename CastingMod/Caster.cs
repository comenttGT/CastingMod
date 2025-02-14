using Photon.Realtime;
using Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Enums;
using System.Threading.Tasks;
using Photon.Pun;
using System.Linq;
using Utilities;
using GorillaNetworking;
using Cinemachine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

namespace CastingMod
{
    public class Caster : MonoBehaviour
    {
        public static Caster instance;

        private GameObject cameraFollower;
        private GameObject ThirdPersonObject;
        private Camera miniMapCam;
        private MainCam cam;

        private List<string> recordedTimes = new List<string>();
        private int team1Wins;
        private int team2Wins;
        private float latestTimeTeam1;
        private float latestTimeTeam2;
        private string team1Name = "Team1";
        private string team2Name = "Team2";
        private int currentPlayedTeam = 0;

        private string currentRoom = "Not Connected";
        private string roomToJoin;

        public SpectatedPlayer currentSpectatedPlayer { get; internal set; }

        public event Action<NetPlayer> OnSwitchedSpectatedPlayer;

        private bool isSpectating;
        public bool showNameTags { get; internal set; }
        private bool showRecordedTimes = true;
        private bool showCodeJoin = true;
        private bool showDayNightChanger = false;
        private bool showControls = false;
        private bool showTeamSetting = false;


        private NetPlayer[] allPlayers;
        private VRRig[] taggedRigs;
        public GorillaTagManager GTM;
        private BetterDayNightManager BDNM;

        private bool isFirstPerson = false;
        public MainCam FirstPersonCam { get; internal set; }
        private float FirstPerson_FOV = 120f;

        private bool FirstLaunchWithCaster = false;

        private Dictionary<GameObject, NetPlayer> nameTagPlayerObjects = new Dictionary<GameObject, NetPlayer>();
        private Texture2D RunnerTextureButton;
        public Texture2D TaggerTextureButton { get; internal set; }
        private Texture2D DefaultTextureButon;
        private RenderTexture MiniMap;




        public void Start()
        {
            if (instance == null)
                instance = this;
            ThirdPersonObject = GameObject.Find("Player Objects/Third Person Camera");
            cameraFollower = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/Main Camera/Camera Follower");
            GameObject tempCam = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/Main Camera");
            cam = new MainCam
            {
                Camera = tempCam.GetComponent<Camera>(),
                Listener = tempCam.GetComponent<AudioListener>(),
                Object = tempCam,
            };
            AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
            for (int i = 0; i < allListeners.Length; i++)
            {
                allListeners[i].enabled = false;
            }
            cameraFollower.AddComponent<AudioListener>();
            GameObject fpc = new GameObject
            {
                name = "FirstPersonCamSpectator"
            };
            Camera firstPersonTemp = fpc.AddComponent<Camera>();
            fpc.AddComponent<UniversalAdditionalCameraData>();
            CinemachineVirtualCamera vc = fpc.AddComponent<CinemachineVirtualCamera>();
            CinemachineVirtualCameraBase vcb = fpc.AddComponent<CinemachineVirtualCameraBase>();
            MiniMap = new RenderTexture(512, 512, 24)
            {
                filterMode = FilterMode.Point
            };
            GameObject mm = new GameObject
            {
                name = "MiniMapCam"
            };
            miniMapCam = mm.AddComponent<Camera>();
            mm.AddComponent<UniversalAdditionalCameraData>();
            mm.transform.eulerAngles = new Vector3(90, 0, 0);
            miniMapCam.targetTexture = MiniMap;
            miniMapCam.cameraType = CameraType.Preview;
            miniMapCam.forceIntoRenderTexture = true;
            FirstPersonCam = new MainCam
            {
                Camera = firstPersonTemp,
                Object = fpc,
                VirtualCam = vc
            };
            FirstPersonCam.Object.SetActive(false);
            vc.LookAt = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer/TurnParent").transform;
            if (!PlayerPrefs.HasKey("IsFirstTimeLaunchingCasterMod"))
            {
                PlayerPrefs.SetInt("IsFirstTimeLaunchingCasterMod", 69);
                FirstLaunchWithCaster = true;
                showCodeJoin = false;
            }
            showNameTags = true;
            DefaultTextureButon = LoadTexture("defaultButton.png", new Vector2Int(128, 32));
            RunnerTextureButton = LoadTexture("spectatedButton_runner.png", new Vector2Int(128, 32));
            TaggerTextureButton = LoadTexture("spectatedButton_taggers.png", new Vector2Int(128, 32));
            MiniMap.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            PhotonNetworkController.Instance.disableAFKKick = true;
            BDNM = FindAnyObjectByType<BetterDayNightManager>();
            OnSwitchedSpectatedPlayer += OnSwitchedSpectatedPlayerCallback;
            Callbacks.instance.OnLocalLeftRoom += OnLeftRoom;
            Callbacks.instance.OnLocalJoinedRoom += OnJoinedRoom;
            Callbacks.instance.OnPlayerJoined += OnPlayerJoined;
            Callbacks.instance.OnPlayerLeft += OnPlayerLeft;
        }


        
        public NetPlayer[] GetTaggedPlayers() => GTM.currentInfected.ToArray();
        public VRRig[] GetTaggedRigs()
        {
            NetPlayer[] temptagged = GetTaggedPlayers();
            List<VRRig> temprigs = new List<VRRig>();
            for (int i = 0; i < temptagged.Length; i++)
            {
                temprigs.Add(GTM.FindPlayerVRRig(temptagged[i]));
            }
            taggedRigs = temprigs.ToArray();
            return temprigs.ToArray();
        }
        public Color GetPlayerColor(NetPlayer plyr)
        {
            VRRig temp = GTM.FindPlayerVRRig(plyr);
            return temp.playerColor;
        }

        public bool isPlayerTagged(NetPlayer pylr)
        {
            VRRig temp = GTM.FindPlayerVRRig(pylr);
            return temp.setMatIndex == 2;
        }

        public float GetDistanceFromLava()
        {
            try
            {
                Transform target = null;
                float ClosestDistance = float.MaxValue;
                Vector3 currentPosition = currentSpectatedPlayer.Transform.position;

                for (int i = 0; i < taggedRigs.Length; i++)
                {
                    Vector3 DifferenceToTarget = taggedRigs[i].headMesh.transform.position - currentPosition;
                    float DistanceToTarget = DifferenceToTarget.sqrMagnitude;

                    if (DistanceToTarget < ClosestDistance)
                    {
                        ClosestDistance = DistanceToTarget;
                        target = taggedRigs[i].headMesh.transform;
                    }
                }

                return Vector3.Distance(target.position, currentSpectatedPlayer.Transform.position);
            }
            catch(Exception e)
            {
                print(e.Message);
                GetTaggedRigs();
                return -1f;
            }
        }

        public void ChangeSky(SkyTimeOptions sto)
        {
            switch (sto)
            {
                case SkyTimeOptions.Day:
                    {
                        BDNM.SetTimeOfDay(3);
                        break;
                    }
                case SkyTimeOptions.Night:
                    {
                        BDNM.SetTimeOfDay(0);
                        break;
                    }
                case SkyTimeOptions.Afernoon:
                    {
                        BDNM.SetTimeOfDay(2);
                        break;
                    }
                case SkyTimeOptions.Evening:
                    {
                        BDNM.SetTimeOfDay(6);
                        break;
                    }
                case SkyTimeOptions.Morning:
                    {
                        BDNM.SetTimeOfDay(5);
                        break;
                    }
            }
        }

        private void OnLeftRoom()
        {
            currentRoom = "Not Connected";
            DestroyAllNameTags();
            EndSpectating();
            UpdatePlayerLists();
        }
        private void OnJoinedRoom()
        {
            FixUISpectateGlitch();
            currentRoom = PhotonNetwork.CurrentRoom.Name;
            GetTaggedRigs();
            UpdateNameTags();
            UpdatePlayerLists();
        }

        private async void FixUISpectateGlitch()
        {
            await Task.Delay(500);
            if (isSpectating)
                ChangeToPlayer(currentSpectatedPlayer.Player);
            await Task.Delay(100);
            FixUISpectateGlitch();
        }

        public void OnGUI()
        {
            if (PhotonNetwork.InRoom)
            {
                if (isSpectating)
                {
                    GUI.Label(new Rect
                    {
                        size = new Vector2(512, 256),
                        position = new Vector2(32, 512),
                    }, $"<size=20><b>Player: {currentSpectatedPlayer.Name}</b></size>");
                    if (isPlayerTagged(currentSpectatedPlayer.Player))
                    {
                        GUI.Label(new Rect
                        {
                            size = new Vector2(512, 256),
                            position = new Vector2(32, 576),
                        }, $"<size=20><color=red><b>Tagger</b></color></size>");
                    }
                    else
                    {
                        float lavaDistance = GetDistanceFromLava();
                        GUI.Label(new Rect
                        {
                            size = new Vector2(512, 256),
                            position = new Vector2(32, 576),
                        }, $"<size=20><color=lightblue><b>Runner</b></color></size>");
                        GUI.Label(new Rect
                        {
                            position = new Vector2(875, 960),
                            size = new Vector2(256, 64)
                        }, $"<size=20><b>Distance from Lava: {((int)lavaDistance)}</b></size>");
                    }

                    if (!Timer.instance.timerActive)
                    {
                        GUI.Label(new Rect
                        {
                            position = new Vector2(865, 16),
                            size = new Vector2(256, 64)
                        }, "<i><b><size=20>Start/Stop Time [Space]</size></b></i>");
                    }
                    else if (Timer.instance.temptime < 0)
                    {
                        GUI.Label(new Rect
                        {
                            position = new Vector2(930, 16),
                            size = new Vector2(256, 64)
                        }, $"<i><b><size=20><color=red>{Timer.instance.ReturnedTime}</color></size></b></i>");
                    }
                    else if (Timer.instance.shouldStopTimer)
                    {
                        GUI.Label(new Rect
                        {
                            position = new Vector2(930, 16),
                            size = new Vector2(256, 64)
                        }, $"<i><b><size=20><color=cyan>{Timer.instance.ReturnedTime}</color></size></b></i>");
                    }
                    else
                    {
                        GUI.Label(new Rect
                        {
                            position = new Vector2(930, 16),
                            size = new Vector2(256, 64)
                        }, $"<i><b><size=20><color=lime>{Timer.instance.ReturnedTime}</color></size></b></i>");
                    }
                    if (showRecordedTimes)
                    {
                        for (int i = 0; i < recordedTimes.Count; i++)
                        {
                            GUI.Label(new Rect
                            {
                                position = new Vector2(10, 10) + new Vector2(0, i * 35f),
                                size = new Vector2(256, 128)
                            }, $"<i><b><size=15>{recordedTimes[i]}</size></b></i>");
                        }
                    }
                }
                for (int i = 0; i < allPlayers.Length; i++)
                {
                    if (GUI.Button(new Rect
                    {
                        position = new Vector2(1725, 512 + (i * 40f)),
                        size = new Vector2(128, 32)
                    }, ""))
                    {
                        ChangeToPlayer(allPlayers[i]);
                    };
                    if (isPlayerTagged(allPlayers[i]))
                    {
                        GUI.DrawTexture(new Rect
                        {
                            position = new Vector2(1725, 512 + (i * 40f)),
                            size = new Vector2(128, 32)
                        }, TaggerTextureButton);
                    }
                    else
                    {
                        GUI.DrawTexture(new Rect
                        {
                            position = new Vector2(1725, 512 + (i * 40f)),
                            size = new Vector2(128, 32)
                        }, RunnerTextureButton, ScaleMode.StretchToFill, true, 0, GetPlayerColor(allPlayers[i]), 128, 0);
                    }
                    GUI.Label(new Rect
                    {
                        position = new Vector2(1725, 512 + (i * 40f)),
                        size = new Vector2(128, 32)
                    }, $"<size=15><b>{allPlayers[i].NickName}</b></size>");
                    GUI.Label(new Rect
                    {
                        position = new Vector2(1860, 512 + (i * 40f)),
                        size = new Vector2(128, 32)
                    }, $"<size=15><b>{i}</b></size>");
                }


            }
            if (showDayNightChanger)
            {
                GUI.Label(new Rect
                {
                    position = new Vector2(1760, 280),
                    size = new Vector2(128, 32)
                }, "<size=15><b><i>Change Time</i></b></size>");
                if (GUI.Button(new Rect
                {
                    position = new Vector2(1745, 320),
                    size = new Vector2(128, 21)
                }, ""))
                {
                    ChangeSky(SkyTimeOptions.Morning);
                }
                GUI.DrawTexture(new Rect
                {
                    position = new Vector2(1745, 320),
                    size = new Vector2(128, 21)
                }, DefaultTextureButon);
                GUI.Label(new Rect
                {
                    position = new Vector2(1745, 320),
                    size = new Vector2(128, 21)
                }, "<size=10><b>Morning</b></size>");
                ////////////////////////////////////
                if (GUI.Button(new Rect
                {
                    position = new Vector2(1745, 350),
                    size = new Vector2(128, 21)
                }, ""))
                {
                    ChangeSky(SkyTimeOptions.Day);
                }
                GUI.DrawTexture(new Rect
                {
                    position = new Vector2(1745, 350),
                    size = new Vector2(128, 21)
                }, DefaultTextureButon);
                GUI.Label(new Rect
                {
                    position = new Vector2(1745, 350),
                    size = new Vector2(128, 21)
                }, "<size=10><b>Day</b></size>");
                ///////////////////////////////////
                if (GUI.Button(new Rect
                {
                    position = new Vector2(1745, 380),
                    size = new Vector2(128, 21)
                }, ""))
                {
                    ChangeSky(SkyTimeOptions.Afernoon);
                }
                GUI.DrawTexture(new Rect
                {
                    position = new Vector2(1745, 380),
                    size = new Vector2(128, 21)
                }, DefaultTextureButon);
                GUI.Label(new Rect
                {
                    position = new Vector2(1745, 380),
                    size = new Vector2(128, 21)
                }, "<size=10><b>Afternoon</b></size>");
                ////////////////////////////////////
                if (GUI.Button(new Rect
                {
                    position = new Vector2(1745, 410),
                    size = new Vector2(128, 21)
                }, ""))
                {
                    ChangeSky(SkyTimeOptions.Evening);
                }
                GUI.DrawTexture(new Rect
                {
                    position = new Vector2(1745, 410),
                    size = new Vector2(128, 21)
                }, DefaultTextureButon);
                GUI.Label(new Rect
                {
                    position = new Vector2(1745, 410),
                    size = new Vector2(128, 21)
                }, "<size=10><b>Evening</b></size>");
                ///////////////////////////////////////
                if (GUI.Button(new Rect
                {
                    position = new Vector2(1745, 440),
                    size = new Vector2(128, 21)
                }, ""))
                {
                    ChangeSky(SkyTimeOptions.Night);
                }
                GUI.DrawTexture(new Rect
                {
                    position = new Vector2(1745, 440),
                    size = new Vector2(128, 21)
                }, DefaultTextureButon);
                GUI.Label(new Rect
                {
                    position = new Vector2(1745, 440),
                    size = new Vector2(128, 21)
                }, "<size=10><b>Night</b></size>");
            }
            if (showCodeJoin)
            {
                roomToJoin = GUI.TextField(new Rect
                {
                    position = new Vector2(820, 540),
                    size = new Vector2(256, 24)
                }, $"{roomToJoin}");
                if (GUI.Button(new Rect
                {
                    position = new Vector2(820, 580),
                    size = new Vector2(256, 21)
                }, ""))
                {
                    PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomToJoin.ToUpper(), GorillaNetworking.JoinType.Solo);
                }
                GUI.DrawTexture(new Rect
                {
                    position = new Vector2(820, 580),
                    size = new Vector2(256, 21)
                }, DefaultTextureButon);
                GUI.Label(new Rect
                {
                    position = new Vector2(820, 580),
                    size = new Vector2(256, 21)
                }, "                        <b><i>Join Code</i></b>");
            }
            if (showTeamSetting)
            {
                team1Name = GUI.TextField(new Rect
                {
                    position = new Vector2(720, 540),
                    size = new Vector2(128, 24)
                }, $"{team1Name}").ToUpper();
                GUI.Label(new Rect
                {
                    position = new Vector2(630, 540),
                    size = new Vector2(128, 24)
                }, "<b><i>Team1 Name:</i></b>");
                team2Name = GUI.TextField(new Rect
                {
                    position = new Vector2(1040, 540),
                    size = new Vector2(128, 24)
                }, $"{team2Name}").ToUpper();
                GUI.Label(new Rect
                {
                    position = new Vector2(950, 540),
                    size = new Vector2(128, 24)
                }, "<b><i>Team2 Name:</i></b>");
            }
            GUI.DrawTexture(new Rect
            {
                position = new Vector2(1664, 0),
                size = new Vector2(256, 256)
            }, MiniMap);
            if (showControls)
            {
                GUI.Label(new Rect
                {
                    position = new Vector2(1400, 0),
                    size = new Vector2(512, 384)
                }, "<b>0-9 | Changes to the associated Player\r\nEnter | Changes to First/Third Person View\r\nUp Arrow | Make FOV Higher\r\nDown Arrow | Make FOV Lower\r\nTab | Toggles Nametags\r\nEscape | Ends Spectating\r\nSpace | Starts/Stops the Timer\r\nAlt | Toggles the Room Joiner\r\nX | Save Time\r\nC | Pause/Unpause Time\r\nY | Toggles Day & Night Changer\r\nCtrl | Toggles Recorded Times\r\nF1 | Removes Point from Team1\r\nF2 | Removes Point from Team2\r\nF3 | Removes Last Recorded Time\r\nT | Shows Control\r\nCapsLk | Shows Team Name Changer</b>");
            }
            if (FirstLaunchWithCaster)
            {
                GUI.Label(new Rect
                {
                    size = new Vector2(256, 128),
                    position = new Vector2(820, 460),
                    width = 256,
                    height = 128
                }, "<b><i>This is my first Casting Mod. If you find any Bugs, please report it in my Discord\n      <color=lightblue>( https://discord.gg/8ArfUrTrFG ).</color><size=15>\n\n                      Have Fun!</size></i></b>");
                if (GUI.Button(new Rect
                {
                    position = new Vector2(820, 580),
                    size = new Vector2(256, 21)
                }, ""))
                {
                    FirstLaunchWithCaster = false;
                    showCodeJoin = true;
                }
                GUI.DrawTexture(new Rect
                {
                    position = new Vector2(820, 580),
                    size = new Vector2(256, 21)
                }, DefaultTextureButon);
                GUI.Label(new Rect
                {
                    position = new Vector2(820, 580),
                    size = new Vector2(256, 21)
                }, "                           <b><i>Close</i></b>");
            }

            GUI.Label(new Rect
            {
                position = new Vector2(20, 940),
                size = new Vector2(512, 256)
            }, $"<color=cyan><size=20><b>{team1Name}: {team1Wins}</b></size></color>");
            GUI.Label(new Rect
            {
                position = new Vector2(20, 980),
                size = new Vector2(512, 256)
            }, $"<color=red><size=20><b>{team2Name}: {team2Wins}</b></size></color>");
            GUI.Label(new Rect
            {
                size = new Vector2(256, 21),
                position = new Vector2(20, 1050),
                width = 256,
                height = 128
            }, "<b>MADE BY COMENTTGT</b>");
            GUI.Label(new Rect
            {
                position = new Vector2(1730, 260),
                size = new Vector2(256, 24)
            }, "<b>Press 'T' for Controls</b>");
        }

        public void EndSpectating()
        {
            currentSpectatedPlayer = new SpectatedPlayer();
            isSpectating = false;
            GorillaCameraFollow temp_gcf = cameraFollower.GetComponent<GorillaCameraFollow>();
            cameraFollower.transform.SetParent(cam.Object.transform);
            temp_gcf.playerHead = cam.Object.transform;
            cameraFollower.transform.position = cam.Object.transform.position;
            cameraFollower.transform.rotation = cam.Object.transform.rotation;
            FirstPersonCam.Object.SetActive(false);
            ThirdPersonObject.SetActive(true);
        }

        public void ChangeToPlayer(NetPlayer player)
        {
            isSpectating = true;
            VRRig temp = GTM.FindPlayerVRRig(player);
            GorillaCameraFollow temp_gcf = cameraFollower.GetComponent<GorillaCameraFollow>();
            if (!isFirstPerson)
            {
                /*
                ThirdPersonObject.SetActive(true);
                FirstPersonCam.Object.SetActive(false);
                cameraFollower.transform.SetParent(temp.headMesh.transform);
                temp_gcf.playerHead = temp.headMesh.transform;
                cameraFollower.transform.position = temp.headMesh.transform.position;
                cameraFollower.transform.rotation = temp.headMesh.transform.rotation;
                OnSwitchedSpectatedPlayer(player);
                */
                FirstPersonCam.Camera.fieldOfView = FirstPerson_FOV;
                FirstPersonCam.Camera.nearClipPlane = 0.001f;
                FirstPersonCam.Camera.farClipPlane = 500f;
                FirstPersonCam.Object.transform.SetParent(temp.mainSkin.transform);
                FirstPersonCam.Object.transform.rotation = temp.mainSkin.transform.rotation;
                FirstPersonCam.Object.transform.position = temp.mainSkin.transform.position - (temp.mainSkin.transform.forward * 2f) + ((-temp.mainSkin.transform.up) + (temp.mainSkin.transform.up * 1.2f));
            }
            else
            {
                FirstPersonCam.Camera.fieldOfView = FirstPerson_FOV + 7.5f;
                FirstPersonCam.Camera.nearClipPlane = 0.025f;
                FirstPersonCam.Camera.farClipPlane = 500f;
                FirstPersonCam.Object.transform.SetParent(temp.headMesh.transform);
                FirstPersonCam.Object.transform.rotation = temp.headMesh.transform.rotation;
                FirstPersonCam.Object.transform.position = temp.headMesh.transform.position + (-temp.headMesh.transform.forward + (temp.headMesh.transform.forward * 1.1355f));
            }
            ThirdPersonObject.SetActive(false);
            cameraFollower.transform.SetParent(temp.headMesh.transform);
            cameraFollower.transform.position = temp.headMesh.transform.position;
            cameraFollower.transform.rotation = temp.headMesh.transform.rotation;
            FirstPersonCam.Object.SetActive(true);
            OnSwitchedSpectatedPlayer(player);
            UpdatePlayerLists();
        }
        private void OnSwitchedSpectatedPlayerCallback(NetPlayer player)
        {
            currentSpectatedPlayer = new SpectatedPlayer
            {
                Player = player,
                Name = player.NickName,
                IsTagged = isPlayerTagged(player),
                Team = "pipi",
                Transform = GTM.FindPlayerVRRig(player).headMesh.transform
            };
            print($"Changed Spectated Player to: {player.NickName}");
        }
        private async void UpdateNameTags()
        {
            await Task.Delay(850);
            VRRig[] allRigs = AllRigs();
            NetPlayer[] _allPlayers = allPlayers;
            for (int i = 0; i < allRigs.Length; i++)
            {
                if (allRigs[i].isLocal)
                    continue;
                else
                {
                    await Task.Delay(1);
                    if (nameTagPlayerObjects.ContainsValue(_allPlayers[i]))
                        continue;
                    else
                    {
                        GameObject nametag = new GameObject
                        {
                            name = "nameTag"
                        };
                        nameTagPlayerObjects.Add(nametag, _allPlayers[i]);
                        nametag.AddComponent<NameTag>().SetPlayer(allPlayers[i]);
                        nametag.transform.SetParent(allRigs[i].mainSkin.transform);
                        nametag.transform.position = allRigs[i].headMesh.transform.position + allRigs[i].headMesh.transform.up;
                    }
                }
            }
        }
        public void Update()
        {
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[0]);
            else if (Keyboard.current.digit1Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[1]);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[2]);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[3]);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[4]);
            else if (Keyboard.current.digit5Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[5]);
            else if (Keyboard.current.digit6Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[6]);
            else if (Keyboard.current.digit7Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[7]);
            else if (Keyboard.current.digit8Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[8]);
            else if (Keyboard.current.digit9Key.wasPressedThisFrame)
                ChangeToPlayer(allPlayers[9]);
            else if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                isFirstPerson = !isFirstPerson;
                ChangeToPlayer(currentSpectatedPlayer.Player);
            }
            else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                FirstPerson_FOV += 5;
                ChangeToPlayer(currentSpectatedPlayer.Player);
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                FirstPerson_FOV -= 5;
                ChangeToPlayer(currentSpectatedPlayer.Player);
            }
            else if (Keyboard.current.tabKey.wasPressedThisFrame)
                showNameTags = !showNameTags;
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                EndSpectating();
            else if (Keyboard.current.spaceKey.wasPressedThisFrame)
                Timer.instance.StartOrStopTimer();
            else if (Keyboard.current.altKey.wasPressedThisFrame)
                showCodeJoin = !showCodeJoin;
            else if (Keyboard.current.xKey.wasPressedThisFrame && Timer.instance.timerActive)
            {

                if (currentPlayedTeam == 0 || currentPlayedTeam == 2 || currentPlayedTeam == 4 || currentPlayedTeam == 6 || currentPlayedTeam == 8 || currentPlayedTeam == 10 || currentPlayedTeam == 12)
                {
                    recordedTimes.Add($"<color=cyan>{team1Name}: {Timer.instance.ReturnedTime}</color>");
                    latestTimeTeam1 = Timer.instance.ActualTime;
                    currentPlayedTeam++;
                }
                else if (currentPlayedTeam > 13)
                    print("Can't save a new Recorded Time!");
                else
                {
                    recordedTimes.Add($"<color=red>{team2Name}: {Timer.instance.ReturnedTime}</color>");
                    latestTimeTeam2 = Timer.instance.ActualTime;
                    if (latestTimeTeam1 > latestTimeTeam2 && Math.Abs(latestTimeTeam1 - latestTimeTeam2) > 2000)
                        team1Wins++;
                    else if (latestTimeTeam1 < latestTimeTeam2 && Math.Abs(latestTimeTeam1 - latestTimeTeam2) > 2000)
                        team2Wins++;
                    currentPlayedTeam++;

                }
                Timer.instance.ImediateStopTime();
            }
            else if (Keyboard.current.cKey.wasPressedThisFrame)
                Timer.instance.UnpauseOrPauseTime();
            else if (Keyboard.current.ctrlKey.wasPressedThisFrame)
                showRecordedTimes = !showRecordedTimes;
            else if (Keyboard.current.yKey.wasPressedThisFrame)
                showDayNightChanger = !showDayNightChanger;
            else if (Keyboard.current.f3Key.wasPressedThisFrame)
                recordedTimes.RemoveAt(recordedTimes.Count - 1);
            else if (Keyboard.current.f1Key.wasPressedThisFrame)
                team1Wins--;
            else if (Keyboard.current.f2Key.wasPressedThisFrame)
                team2Wins--;
            else if (Keyboard.current.tKey.wasPressedThisFrame)
                showControls = !showControls;
            else if(Keyboard.current.capsLockKey.wasPressedThisFrame)
                showTeamSetting = !showTeamSetting;

            if (isSpectating)
            {
                miniMapCam.transform.position = new Vector3(currentSpectatedPlayer.Transform.position.x, 30, currentSpectatedPlayer.Transform.position.z);
                miniMapCam.fieldOfView = 70 - ((miniMapCam.transform.position.y - currentSpectatedPlayer.Transform.position.y) * 2.25f);
            }
            else
            {
                miniMapCam.transform.position = new Vector3(cam.Object.transform.position.x, 30, cam.Object.transform.position.z);
                miniMapCam.fieldOfView = 70 - ((miniMapCam.transform.position.y - cam.Object.transform.position.y) * 2.25f);
            }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        private void UpdatePlayerLists() => allPlayers = NetworkSystem.Instance.AllNetPlayers;
        public VRRig[] AllRigs()
        {
            NetPlayer[] temptagged = NetworkSystem.Instance.AllNetPlayers;
            List<VRRig> temprigs = new List<VRRig>();
            for (int i = 0; i < temptagged.Length; i++)
            {
                temprigs.Add(GTM.FindPlayerVRRig(temptagged[i]));
            }
            return temprigs.ToArray();
        }

        private void OnPlayerJoined(Player player)
        {
            taggedRigs = GetTaggedRigs();
            FixUISpectateGlitch();
            DestroyPlayerNameTag(player);
            UpdateNameTags();
            UpdatePlayerLists();
        }

        private void OnPlayerLeft(Player player)
        {
            if (currentSpectatedPlayer.Player.UserId == player.UserId)
                EndSpectating();

            taggedRigs = GetTaggedRigs();
            DestroyPlayerNameTag(player);
            UpdateNameTags();
            UpdatePlayerLists();
        }
        private async void DestroyAllNameTags()
        {
            for (int i = 0; i < nameTagPlayerObjects.Count; i++)
            {
                await Task.Delay(1);
                DestroyImmediate(nameTagPlayerObjects.Keys.ToArray()[i]);
            }
            nameTagPlayerObjects = new Dictionary<GameObject, NetPlayer>();
            return;
        }

        private void DestroyPlayerNameTag(Player plyr)
        {
            GameObject[] nameTags = nameTagPlayerObjects.Keys.ToArray();
            for (int i = 0; i < nameTags.Length; i++)
            {
                NetPlayer temp;
                nameTagPlayerObjects.TryGetValue(nameTags[i], out temp);
                if (temp.UserId == plyr.UserId)
                {
                    nameTagPlayerObjects.Remove(nameTags[i]);
                    DestroyImmediate(nameTags[i]);
                    return;
                }
                else
                    continue;
            }

        }


        public Texture2D LoadTexture(string textureName, Vector2Int dimensionsizes)
        {
            Texture2D texture = new Texture2D(dimensionsizes.x, dimensionsizes.y);
            byte[] bytes;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"CastingMod.Embedded.Textures.{textureName}");
            using (BinaryReader reader = new BinaryReader(stream))
            {
                bytes = reader.ReadBytes((int)stream.Length);
            }
            texture.LoadImage(bytes);
            return texture;
        }

    }
}
