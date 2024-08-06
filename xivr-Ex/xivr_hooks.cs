using System;
using System.IO;
using System.Drawing;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using Dalamud;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Dalamud.Hooking;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonNamePlate;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Resource;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.FFXIV.Client.UI;


using cCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using sCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;
using sCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using rCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

using xivr.Structures;
using xivr.StructuresEx;
using SettingsManager;


namespace xivr
{
    //----
    // Handles input from vr dll
    //----
    public delegate void HandleInputDelegate(InputAnalogActionData analog, InputDigitalActionData digital);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void UpdateControllerInput(ActionButtonLayout buttonId, InputAnalogActionData analog, InputDigitalActionData digital);

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class HandleInputAttribute : System.Attribute
    {
        public ActionButtonLayout inputId { get; private set; }
        public HandleInputAttribute(ActionButtonLayout buttonId) => inputId = buttonId;
    }

    //----
    // Internal logging from vr dll
    //----
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void InternalLogging(String value);

    public unsafe class xivr_hooks
    {
        protected Dictionary<ActionButtonLayout, HandleInputDelegate> inputList = new Dictionary<ActionButtonLayout, HandleInputDelegate>();

        byte[] GetThreadedDataASM =
            {
                0x55, // push rbp
                0x65, 0x48, 0x8B, 0x04, 0x25, 0x58, 0x00, 0x00, 0x00, // mov rax,gs:[00000058]
                0x5D, // pop rbp
                0xC3  // ret
            };

        public bool enableVR = true;
        private bool initalized = false;
        private bool hooksSet = false;
        private bool forceFloatingScreen = false;

        private bool isMounted = false;
        private bool dalamudMode = false;
        private bool isCharMake = false;
        private bool isCharSelect = false;
        private bool isHousing = false;

        private byte targetAddonAlpha = 0;
        private RenderModes curRenderMode = RenderModes.None;
        private int curEye = 0;
        private int[] nextEye = { 1, 0 };
        private int[] swapEyes = { 1, 0 };
        private float Deg2Rad = MathF.PI / 180.0f;
        private float Rad2Deg = 180.0f / MathF.PI;
        private float cameraZoom = 0.0f;
        private float leftBumperValue = 0.0f;
        private float BridgeBoneHeight = 0.0f;
        private float armLength = 1.0f;
        private float uiAngleOffset = 0.0f;
        private float default_minFOV = 0;
        private float default_curFOV = 0;
        private float default_maxFOV = 0;
        private float default_nearClip = 0;
        private ChangedTypeBool mouseoverUI = new ChangedTypeBool();
        private ChangedTypeBool mouseoverTarget = new ChangedTypeBool();
        private ChangedTypeBool inCutscene = new ChangedTypeBool();
        private Vector2 rotateAmount = new Vector2(0.0f, 0.0f);
        private Point virtualMouse = new Point(0, 0);
        private Point actualMouse = new Point(0, 0);
        private Dictionary<ActionButtonLayout, bool> inputState = new Dictionary<ActionButtonLayout, bool>();
        private Dictionary<ActionButtonLayout, ChangedType<bool>> inputStatus = new Dictionary<ActionButtonLayout, ChangedType<bool>>();
        private Dictionary<ConfigOption, int> SavedSettings = new Dictionary<ConfigOption, int>();
        private Stack<bool> overrideFromParent = new Stack<bool>();
        private bool frfCalculateViewMatrix = false; // frf first run this frame
        private int ScreenMode = 0;
        private UInt64 selectScreenMouseOver = 0;
        private UInt64 DisableSetCursorPosOrig = 0;
        private UInt64 DisableSetCursorPosOverride = 0x05C6909090909090;
        private UInt64 DisableSetCursorPosAddr = 0;

        private const int FLAG_INVIS = (1 << 1) | (1 << 11);
        private const byte NamePlateCount = 50;
        private UInt64 BaseAddress = 0;
        private UInt64 globalScaleAddress = 0;
        private GCHandle getThreadedDataHandle;
        private int[] runCount = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private UInt64 tls_index = 0;
        private UpdateControllerInput controllerCallback;
        private InternalLogging internalLogging;

        private Queue<stMultiIK>[] multiIK = { new Queue<stMultiIK>(), new Queue<stMultiIK>() };
        private MovingAverage neckOffsetAvg = new MovingAverage();

        private bool isSetProjection = false;
        private Matrix4x4 curProjection = Matrix4x4.Identity;
        private Matrix4x4 curViewMatrixWithoutHMD = Matrix4x4.Identity;
        private Matrix4x4 curViewMatrixWithoutHMDI = Matrix4x4.Identity;
        private Matrix4x4 hmdMatrix = Matrix4x4.Identity;
        private Matrix4x4 hmdMatrixI = Matrix4x4.Identity;
        private Matrix4x4 lhcMatrix = Matrix4x4.Identity;
        private Matrix4x4 lhcPalmMatrix = Matrix4x4.Identity;
        private Matrix4x4 lhcMatrixI = Matrix4x4.Identity;
        private Matrix4x4 rhcMatrix = Matrix4x4.Identity;
        private Matrix4x4 rhcPalmMatrix = Matrix4x4.Identity;
        private Matrix4x4 rhcMatrixI = Matrix4x4.Identity;
        private Matrix4x4 fixedProjection = Matrix4x4.Identity;
        private Matrix4x4 hmdOffsetFirstPerson = Matrix4x4.Identity;
        private Matrix4x4 hmdOffsetThirdPerson = Matrix4x4.Identity;
        private Matrix4x4 hmdOffsetMountedFirstPerson = Matrix4x4.Identity;
        private Matrix4x4 hmdWorldScale = Matrix4x4.CreateScale(1.0f);
        private Matrix4x4 handWatch = Matrix4x4.Identity;
        private Matrix4x4 handBoneRay = Matrix4x4.Identity;
        private Matrix4x4[] gameProjectionMatrix = {
                    Matrix4x4.Identity,
                    Matrix4x4.Identity
                };
        private Matrix4x4[] eyeOffsetMatrix = {
                    Matrix4x4.Identity,
                    Matrix4x4.Identity
                };

        private Matrix4x4 convertXZ = new Matrix4x4(0, 0, -1, 0,
                                                    0, -1, 0, 0,
                                                    -1, 0, 0, 0,
                                                    0, 0, 0, 1);
        private Vector3 avgHCPosition = new Vector3();
        private float avgHCRotation = 0.0f;

        private ExclusiveExtras exExtras = new ExclusiveExtras();
        private HookManager hookManager = new HookManager();
        private ConfigManager? cfgManager = null;
        private uint MouseOpeLimit = 0;

        private ResourceManager* resourceManager = null;
        private MovementManager* movementManager = null;
        private sCameraManager* scCameraManager = sCameraManager.Instance();
        private RenderTargetManager* renderTargetManager = RenderTargetManager.Instance();
        private Framework* frameworkInstance = Framework.Instance();
        private Device* dx11DeviceInstance = Device.Instance();
        private TargetSystem* targetSystem = TargetSystem.Instance();
        
        private AtkTextNode* vrTargetCursor = null;
        private CharSelectionCharList* charList = null;


        public static void PrintEcho(string message) => Plugin.ChatGui!.Print($"[xivr] {message}");
        public static void PrintError(string message) => Plugin.ChatGui!.PrintError($"[xivr] {message}");

        public void SetInputHandles()
        {
            //----
            // Gets a list of all the methods this class contains that are public and instanced (non static)
            // then looks for a specific attirbute attached to the class
            // Once found, create a delegate and add both the attribute and delegate to a dictionary
            //----
            inputList.Clear();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            foreach (MethodInfo method in this.GetType().GetMethods(flags))
            {
                foreach (System.Attribute attribute in method.GetCustomAttributes(typeof(HandleInputAttribute), false))
                {
                    ActionButtonLayout key = ((HandleInputAttribute)attribute).inputId;
                    HandleInputDelegate handle = (HandleInputDelegate)HandleInputDelegate.CreateDelegate(typeof(HandleInputDelegate), this, method);

                    if (!inputList.ContainsKey(key))
                    {
                        if (Plugin.cfg!.data.vLog)
                            Plugin.Log!.Info($"SetInputHandles Adding {key}");
                        inputList.Add(key, handle);
                        inputState.Add(key, false);
                        inputStatus.Add(key, new ChangedType<bool>());
                    }
                }
            }
        }

        public bool Initialize()
        {
            if (Plugin.cfg!.data.vLog)
                Plugin.Log!.Info($"Initialize A {initalized} {hooksSet}");

            if (initalized == false)
            {
                Plugin.Interop.InitializeFromAttributes(this);

                
                if (frameworkInstance == null)
                    frameworkInstance = Framework.Instance();

                if (dx11DeviceInstance == null)
                    dx11DeviceInstance = Device.Instance();

                if (renderTargetManager == null)
                    renderTargetManager = RenderTargetManager.Instance();

                BaseAddress = (UInt64)Process.GetCurrentProcess()?.MainModule?.BaseAddress;


                tls_index = (UInt64)Plugin.SigScanner!.GetStaticAddressFromSig(Signatures.g_tls_index);
                //globalScaleAddress = (UInt64)Plugin.SigScanner!.GetStaticAddressFromSig(Signatures.g_TextScale);
                //csCameraManager = (ControlSystemCameraManager*)Plugin.SigScanner!.GetStaticAddressFromSig(Signatures.g_ControlSystemCameraManager);
                //charList = (CharSelectionCharList*)Plugin.SigScanner!.GetStaticAddressFromSig(Signatures.g_SelectScreenCharacterList);
                selectScreenMouseOver = (UInt64)Plugin.SigScanner!.GetStaticAddressFromSig(Signatures.g_SelectScreenMouseOver);
                //movementManager = (MovementManager*)Plugin.SigScanner!.GetStaticAddressFromSig(Signatures.g_MovementManager);

                DisableSetCursorPosAddr = (UInt64)Plugin.SigScanner!.ScanText(Signatures.g_DisableSetCursorPosAddr);
                DisableSetCursorPosOrig = *(UInt64*)DisableSetCursorPosAddr;

                //----
                // Config settings
                //----
                cfgManager = new ConfigManager();

                List<string> cfgSearchStrings = new List<string>() {
                "MouseOpeLimit",
                "Gamma",
                "Fps",
                "MainAdapter",
                "FPSCameraInterpolationType"
                };

                cfgManager.AddToList(cfgSearchStrings);
                cfgManager.MapSettings();
                curRenderMode = RenderModes.None;
                
                SetInputHandles();

                controllerCallback = (buttonId, analog, digital) =>
                {
                    if (inputList.ContainsKey(buttonId))
                        inputList[buttonId](analog, digital);
                };

                internalLogging = (value) =>
                {
                    Plugin.Log!.Info($"xivr_main: {value}");
                };

                Imports.SetLogFunction(internalLogging);

                exExtras.Initalize();
                GetThreadedDataInit();
                hookManager.SetFunctionHandles(this, Plugin.cfg.data.vLog);
                initalized = true;
            }

            if (Plugin.cfg.data.vLog)
                Plugin.Log!.Info($"Initialize B {initalized} {hooksSet}");


            return initalized;
        }

        public void Update(IFramework framework)
        {
        }
        public void OnLogin()
        {
            if (hooksSet && enableVR)
            {
                SetRenderingMode();

                //if (DisableCameraCollisionAddr != 0)
                //    SafeMemory.Write<UInt64>((IntPtr)DisableCameraCollisionAddr, DisableCameraCollisionOverride);
            }
        }

        public void OnLogout()
        {
            //----
            // Sets the lengths of the TargetSystem to 0 as they keep their size
            // even though the data is reset
            //----
            targetSystem->ObjectFilterArray0.Length = 0;
            targetSystem->ObjectFilterArray1.Length = 0;
            targetSystem->ObjectFilterArray2.Length = 0;
            targetSystem->ObjectFilterArray3.Length = 0;

            if (hooksSet && enableVR)
            {
                //if (DisableCameraCollisionAddr != 0)
                //    SafeMemory.Write<UInt64>((IntPtr)DisableCameraCollisionAddr, DisableCameraCollisionOrig);
            }
        }

        public void Dispose()
        {
            if (Plugin.cfg!.data.vLog)
                Plugin.Log!.Info($"Dispose A {initalized} {hooksSet}");
            GetThreadedDataDestroy();
            cfgManager!.Dispose();
            exExtras.Dispose();
            hookManager.DisposeFunctionHandles(Plugin.cfg.data.vLog);
            initalized = false;
            if (Plugin.cfg!.data.vLog)
                Plugin.Log!.Info($"Dispose B {initalized} {hooksSet}");
        }


        public bool Start()
        {
            if (Plugin.cfg!.data.vLog)
            {
                Plugin.Log!.Info($"Settings:");
                Plugin.Log!.Info($"-- isEnabled = {Plugin.cfg.data.isEnabled}");
                Plugin.Log!.Info($"-- isAutoEnabled = {Plugin.cfg.data.isAutoEnabled}");
                Plugin.Log!.Info($"-- forceFloatingScreen = {Plugin.cfg.data.forceFloatingScreen}");
                Plugin.Log!.Info($"-- forceFloatingInCutscene = {Plugin.cfg.data.forceFloatingInCutscene}");
                Plugin.Log!.Info($"-- horizontalLock = {Plugin.cfg.data.horizontalLock}");
                Plugin.Log!.Info($"-- verticalLock = {Plugin.cfg.data.verticalLock}");
                Plugin.Log!.Info($"-- horizonLock = {Plugin.cfg.data.horizonLock}");
                Plugin.Log!.Info($"-- runRecenter = {Plugin.cfg.data.runRecenter}");
                Plugin.Log!.Info($"-- offsetAmountX = {Plugin.cfg.data.offsetAmountX}");
                Plugin.Log!.Info($"-- offsetAmountY = {Plugin.cfg.data.offsetAmountY}");
                Plugin.Log!.Info($"-- snapRotateAmountX = {Plugin.cfg.data.snapRotateAmountX}");
                Plugin.Log!.Info($"-- snapRotateAmountY = {Plugin.cfg.data.snapRotateAmountY}");
                Plugin.Log!.Info($"-- uiOffsetZ = {Plugin.cfg.data.uiOffsetZ}");
                Plugin.Log!.Info($"-- uiOffsetScale = {Plugin.cfg.data.uiOffsetScale}");
                Plugin.Log!.Info($"-- conloc = {Plugin.cfg.data.conloc}");
                Plugin.Log!.Info($"-- swapEyes = {Plugin.cfg.data.swapEyes}");
                Plugin.Log!.Info($"-- swapEyesUI = {Plugin.cfg.data.swapEyesUI}");
                Plugin.Log!.Info($"-- motioncontrol = {Plugin.cfg.data.motioncontrol}");
                Plugin.Log!.Info($"-- hmdWidth = {Plugin.cfg.data.hmdWidth}");
                Plugin.Log!.Info($"-- hmdHeight = {Plugin.cfg.data.hmdHeight}");
                Plugin.Log!.Info($"-- autoResize = {Plugin.cfg.data.autoResize}");
                Plugin.Log!.Info($"-- ipdOffset = {Plugin.cfg.data.ipdOffset}");
                Plugin.Log!.Info($"-- vLog = {Plugin.cfg.data.vLog}");
                Plugin.Log!.Info($"-- hmdloc = {Plugin.cfg.data.hmdloc}");
                Plugin.Log!.Info($"-- vertloc = {Plugin.cfg.data.vertloc}");
                Plugin.Log!.Info($"-- targetCursorSize = {Plugin.cfg.data.targetCursorSize}");
                Plugin.Log!.Info($"-- offsetAmountZ = {Plugin.cfg.data.offsetAmountZ}");
                Plugin.Log!.Info($"-- uiDepth = {Plugin.cfg.data.uiDepth}");
                Plugin.Log!.Info($"-- hmdPointing = {Plugin.cfg.data.hmdPointing}");
                Plugin.Log!.Info($"-- mode2d = {Plugin.cfg.data.mode2d}");
                Plugin.Log!.Info($"-- asymmetricProjection = {Plugin.cfg.data.asymmetricProjection}");
                Plugin.Log!.Info($"-- immersiveMovement = {Plugin.cfg.data.immersiveMovement}");
                Plugin.Log!.Info($"-- immersiveFull = {Plugin.cfg.data.immersiveFull}");
                Plugin.Log!.Info($"-- ultrawideshadows = {Plugin.cfg.data.ultrawideshadows}");
                Plugin.Log!.Info($"-- osk = {Plugin.cfg.data.osk}");
                Plugin.Log!.Info($"-- disableXboxShoulder = {Plugin.cfg.data.disableXboxShoulder}");
                Plugin.Log!.Info($"-- uiOffsetY = {Plugin.cfg.data.uiOffsetY}");
                Plugin.Log!.Info($"-- mouseMultiplyer = {Plugin.cfg.data.mouseMultiplyer}");
                Plugin.Log!.Info($"Start A {initalized} {hooksSet}");
            }

            if (frameworkInstance == null)
                frameworkInstance = Framework.Instance();

            if (dx11DeviceInstance == null)
                dx11DeviceInstance = Device.Instance();

            if (renderTargetManager == null)
                renderTargetManager = RenderTargetManager.Instance();

            if (scCameraManager == null)
                scCameraManager = sCameraManager.Instance();

            if (initalized && !hooksSet) // && Plugin.VR_IsHmdPresent())
            {
                if (Plugin.cfg.data.vLog)
                    Plugin.Log!.Info($"SetDX Dx: {(IntPtr)dx11DeviceInstance:x} | RndTrg:{(IntPtr)renderTargetManager:x}");

                //----
                // Gets the games textures
                //----
                UInt64 rtManagerAddr = ((UInt64)renderTargetManager) + 0x20;
                Texture* backText = dx11DeviceInstance->SwapChain->BackBuffer;
                Texture* rendText = *(Texture**)(rtManagerAddr + (ulong)(0x8 * 107));
                Texture* depthText = *(Texture**)(rtManagerAddr + (ulong)(0x8 * 10));

                if (!Imports.SetDX11((IntPtr)dx11DeviceInstance, backText, rendText, depthText))
                    return false;
                if(!Imports.CreateHandTextures(Plugin.PluginInterface!.AssemblyLocation.DirectoryName!))
                    return false;
                if (!Imports.renderStart((IntPtr)dx11DeviceInstance->D3D11Forwarder, (IntPtr)dx11DeviceInstance->D3D11DeviceContext))
                    return false;
                if (!Imports.CreateBuffers(backText, rendText, depthText))
                    return false;
                if (!Imports.CreateTextures(rendText, depthText))
                    return false;
                if (!Imports.vrStart())
                    return false;

                string filePath = Path.Join(Plugin.PluginInterface!.AssemblyLocation.DirectoryName, "config", "actions.json");
                if (Imports.SetActiveJSON(filePath, filePath.Length) == false)
                   Plugin.Log!.Info($"Error loading Json file : {filePath}");

                //----
                // Set the near clip
                //----
                RawGameCamera* renderCam = (RawGameCamera*)scCameraManager->CurrentCamera;
                if (renderCam != null)
                {
                    default_minFOV = renderCam->MinFoV;
                    default_curFOV = renderCam->CurrentFoV;
                    default_maxFOV = renderCam->MaxFoV;
                    default_nearClip = renderCam->BufferData->NearClip;

                    renderCam->BufferData->NearClip = 0.05f;
                }
                    

                //----
                // Sets some vr settings
                //----
                MouseOpeLimit = cfgManager!.GetSettingsValue("MouseOpeLimit", 0);
                if (cfgManager!.GetSettingsValue("Gamma", 0) == 50)
                    cfgManager!.SetSettingsValue("Gamma", 49);
                cfgManager!.SetSettingsValue("Fps", 0);
                cfgManager!.SetSettingsValue("MouseOpeLimit", 1);
                cfgManager!.SetSettingsValue("FPSCameraInterpolationType", 2);


                SetRenderingMode();

                if (DisableSetCursorPosAddr != 0)
                    SafeMemory.Write<UInt64>((IntPtr)DisableSetCursorPosAddr, DisableSetCursorPosOverride);

                neckOffsetAvg.Reset();

                //----
                // Loop though the bone enum list and convert it to a dict
                //----
                int j = 0;
                BoneOutput.boneNameToEnum.Clear();
                foreach (string i in Enum.GetNames(typeof(BoneList)))
                {
                    BoneOutput.boneNameToEnum.Add(i, (BoneList)j);
                    j++;
                }

                hookManager.EnableFunctionHandles(Plugin.cfg.data.vLog);
                hooksSet = true;
                if (Plugin.ClientState!.LocalPlayer != null)
                    OnLogin();
                PrintEcho("Starting VR.");
            }
            if (Plugin.cfg.data.vLog)
                Plugin.Log!.Info($"Start B {initalized} {hooksSet}");

            return hooksSet;
        }



        public void Stop()
        {
            if (Plugin.cfg!.data.vLog)
                Plugin.Log!.Info($"Stop A {initalized} {hooksSet}");
            if (hooksSet)
            {
                BoneOutput.boneNameToEnum.Clear();

                //----
                // Disable any input that might still be on
                //----
                InputAnalogActionData analog = new InputAnalogActionData();
                InputDigitalActionData digital = new InputDigitalActionData();
                analog.bActive = false;
                digital.bActive = false;
                foreach (KeyValuePair<ActionButtonLayout, HandleInputDelegate> input in inputList)
                    input.Value(analog, digital);

                gameProjectionMatrix[0] = Matrix4x4.Identity;
                gameProjectionMatrix[1] = Matrix4x4.Identity;
                eyeOffsetMatrix[0] = Matrix4x4.Identity;
                eyeOffsetMatrix[1] = Matrix4x4.Identity;
                curRenderMode = RenderModes.None;

                cfgManager!.SetSettingsValue("Gamma", 0);
                cfgManager!.SetSettingsValue("Fps", 0);
                cfgManager!.SetSettingsValue("MouseOpeLimit", 0);

                //----
                // Reset the FOV values and nearclip
                //----
                RawGameCamera* renderCam = (RawGameCamera*)scCameraManager->CurrentCamera;
                if (renderCam != null)
                {
                    renderCam->MinFoV = default_minFOV;
                    renderCam->CurrentFoV = default_curFOV;
                    renderCam->MaxFoV = default_maxFOV;
                    renderCam->BufferData->NearClip = default_nearClip;
                }
                    
                //----
                // Restores the target arrow alpha and remove the vr cursor
                //----
                fixed (AtkTextNode** pvrTargetCursor = &vrTargetCursor)
                    VRCursor.FreeVRTargetCursor(pvrTargetCursor);

                AtkUnitBase* targetAddon = (AtkUnitBase*)Plugin.GameGui!.GetAddonByName("_TargetCursor", 1);
                if (targetAddon != null)
                    targetAddon->Alpha = targetAddonAlpha;

                FirstToThirdPersonView();
                if (DisableSetCursorPosAddr != 0)
                    SafeMemory.Write<UInt64>((IntPtr)DisableSetCursorPosAddr, DisableSetCursorPosOrig);

                Imports.vrStop();
                Imports.DestroyTextures();
                Imports.DestroyBuffers();
                Imports.renderStop();
                Imports.DestroyHandTextures();
                Imports.UnsetDX11();
                OnLogout();

                hookManager.DisableFunctionHandles(Plugin.cfg.data.vLog);
                hooksSet = false;
                PrintEcho("Stopping VR.");
            }
            if (Plugin.cfg.data.vLog)
                Plugin.Log!.Info($"Stop B {initalized} {hooksSet}");
        }

        private void FirstToThirdPersonView()
        {
            Imports.Recenter();
            //----
            // Set the near clip
            //----
            RawGameCamera* renderCam = (RawGameCamera*)scCameraManager->CurrentCamera;
            if (renderCam != null)
                renderCam->BufferData->NearClip = 0.05f;
            neckOffsetAvg.Reset();

            if (Plugin.ClientState!.LocalPlayer != null)
            {
                GameObject* bonedObject = (GameObject*)Plugin.ClientState!.LocalPlayer.Address;
                Character* bonedCharacter = (Character*)Plugin.ClientState!.LocalPlayer.Address;

                if (bonedCharacter != null)
                {
                    //bonedCharacter->DrawData.Flags1 = HideHeadValue;

                    UInt64 equipOffset = (UInt64)(UInt64*)&bonedCharacter->DrawData;
                    fixed(CharEquipSlotData *ptr = &currentEquipmentSet.Head)
                        ChangeEquipmentHook!.Original(equipOffset, CharEquipSlots.Head, ptr);
                    fixed (CharEquipSlotData* ptr = &currentEquipmentSet.Ears)
                        ChangeEquipmentHook!.Original(equipOffset, CharEquipSlots.Ears, ptr);
                    fixed (CharEquipSlotData* ptr = &currentEquipmentSet.Neck)
                        ChangeEquipmentHook!.Original(equipOffset, CharEquipSlots.Neck, ptr);

                    //ChangeWeaponHook!.Original(equipOffset, CharWeaponSlots.MainHand, currentWeaponSet.MainHand, 0, 1, 0, 0);
                    //ChangeWeaponHook!.Original(equipOffset, CharWeaponSlots.OffHand, currentWeaponSet.OffHand, 0, 1, 0, 0);
                    //ChangeWeaponHook!.Original(equipOffset, CharWeaponSlots.uk3, currentWeaponSet.Uk3, 0, 1, 0, 0);

                    RefreshObject((GameObject*)Plugin.ClientState!.LocalPlayer.Address);
                }

                haveSavedEquipmentSet = false;
            }
        }

        private void ThirdToFirstPersonView()
        {
            Imports.Recenter();
            //----
            // Set the near clip
            //----
            RawGameCamera* renderCam = (RawGameCamera*)scCameraManager->CurrentCamera;
            if (renderCam != null)
                renderCam->BufferData->NearClip = 0.05f;
            neckOffsetAvg.Reset();

            if (Plugin.ClientState!.LocalPlayer != null)
            {
                GameObject* bonedObject = (GameObject*)Plugin.ClientState!.LocalPlayer.Address;
                Character* bonedCharacter = (Character*)Plugin.ClientState!.LocalPlayer.Address;

                if (bonedCharacter != null)
                {
                    if (haveSavedEquipmentSet == false)
                    {
                        currentEquipmentSet.Save(bonedCharacter);
                        haveSavedEquipmentSet = true;
                    }

                    UInt64 equipOffset = (UInt64)(UInt64*)&bonedCharacter->DrawData;
                    //----
                    // override the head neck and earing
                    //----
                    if (bonedCharacter->DrawData.EquipmentModelIds[(int)CharEquipSlots.Head].Variant != 99)
                    {
                        HideHeadValue = bonedCharacter->DrawData.Flags1;
                        bonedCharacter->DrawData.Flags1 = 0;

                        fixed (CharEquipSlotData* ptr = &hiddenEquipHead)
                            ChangeEquipmentHook!.Original(equipOffset, CharEquipSlots.Head, ptr);
                        fixed (CharEquipSlotData* ptr = &hiddenEquipNeck)
                            ChangeEquipmentHook!.Original(equipOffset, CharEquipSlots.Neck, ptr);
                        fixed (CharEquipSlotData* ptr = &hiddenEquipEars)
                            ChangeEquipmentHook!.Original(equipOffset, CharEquipSlots.Ears, ptr);

                        RefreshObject((GameObject*)Plugin.ClientState!.LocalPlayer.Address);
                    }
                }
            }
        }


        Dictionary<ushort, Bone> boneLayoutA = new Dictionary<ushort, Bone>();
        Dictionary<UInt64, Dictionary<BoneList, short>> boneLayout = new Dictionary<UInt64, Dictionary<BoneList, short>>();
        Dictionary<UInt64, Bone[]> rawBoneList = new Dictionary<UInt64, Bone[]>();
        Dictionary<BoneList, KeyValuePair<int, short>> BoneParentOverrideList = new Dictionary<BoneList, KeyValuePair<int, short>>();
        Dictionary<UInt64, List<KeyValuePair<Vector3, hkQsTransformf>>> boneLayoutT = new Dictionary<UInt64, List<KeyValuePair<Vector3, hkQsTransformf>>>();

        int timer = 100;
        CharEquipSlotData hiddenEquipHead = new CharEquipSlotData(6154, 99, 0, 0);
        CharEquipSlotData hiddenEquipEars = new CharEquipSlotData(0, 0, 0, 0);
        CharEquipSlotData hiddenEquipNeck = new CharEquipSlotData(0, 0, 0, 0);
        //CharWeaponSlotData hiddenEquipWeaponMainHand = new CharWeaponSlotData(0, 0, 0, 0);
        //CharWeaponSlotData hiddenEquipWeaponOffHand = new CharWeaponSlotData(0, 0, 0, 0);

        bool haveSavedEquipmentSet = false;
        CharEquipData currentEquipmentSet = new CharEquipData();
        //CharWeaponData currentWeaponSet = new CharWeaponData();

        //private Dictionary<UInt64, List<KeyValuePair<Vector3, hkQsTransformf>>> boneLayout = new Dictionary<UInt64, List<KeyValuePair<Vector3, hkQsTransformf>>>();
        Dictionary<hkaPose, Dictionary<string, int>> boneNames = new Dictionary<hkaPose, Dictionary<string, int>>();
        Matrix4x4 bridgeLocal = Matrix4x4.Identity;
        Vector3 neckPosition = new Vector3(0, 0, 0);



        Matrix4x4 plrSkeletonPosition = Matrix4x4.Identity;
        Matrix4x4 plrSkeletonPositionI = Matrix4x4.Identity;
        Matrix4x4 mntSkeletonPosition = Matrix4x4.Identity;
        Matrix4x4 mntSkeletonPositionI = Matrix4x4.Identity;
        //Matrix4x4[] headBoneMatrix = { Matrix4x4.Identity, Matrix4x4.Identity };
        Matrix4x4 headBoneMatrix = Matrix4x4.Identity;
        Matrix4x4 headBoneMatrixI = Matrix4x4.Identity;
        Vector3 eyeMidPoint = new Vector3(0, 0, 0);
        Vector3[] hmdOffsetPerEye = { new Vector3(0, 0, 0), new Vector3(0, 0, 0),
                                      new Vector3(0, 0, 0), new Vector3(0, 0, 0) };
        Matrix4x4 eyeMidPointM = Matrix4x4.Identity;
        byte HideHeadValue = 0;
        bool hideWeapons = true;

        public void RefreshObject(GameObject* obj2refresh)
        {
            obj2refresh->RenderFlags = 2;
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 500;
            timer.Elapsed += (sender, e) => { RefreshObjectTick(timer, obj2refresh); };
            timer.Enabled = true;
        }

        public void RefreshObjectTick(System.Timers.Timer timer, GameObject* obj2refresh)
        {
            obj2refresh->RenderFlags = 0;
            timer.Enabled = false;
        }

        class ChangedType<T>
        {
            private T old = default(T);
            public T Current
            {
                get => old;
                set
                {
                    Changed = false;
                    if (!EqualityComparer<T>.Default.Equals(value, old))
                    {
                        old = value;
                        Changed = true;
                    }
                }
            }
            public bool Changed { get; private set; }
            public ChangedType(T newVal = default(T))
            {
                old = newVal;
                Current = newVal;
                Changed = false;
            }
            public ChangedType<T> Set(T newVal)
            {
                Current = newVal;
                return this;
            }
        }

        class ChangedTypeBool
        {
            private bool old = false;
            public bool Current
            {
                get => old;
                set
                {
                    Changed = !(value == old);
                    old = value;
                }
            }
            public bool Changed { get; private set; }
            public ChangedTypeBool(bool newVal = false)
            {
                old = newVal;
                Current = newVal;
                Changed = false;
            }
            public ChangedTypeBool Set(bool newVal)
            {
                Current = newVal;
                return this;
            }
        }

        private ChangedType<CameraModes> gameMode = new ChangedType<CameraModes>(CameraModes.None);

        bool outputBonesOnce = false;
        GameObject* curMouseOverTarget = null;
        System.Timers.Timer RayOverFeedbackTimer = new System.Timers.Timer();
        public void RayOverFeedbackTick(System.Timers.Timer timer)
        {
            Imports.HapticFeedback(ActionButtonLayout.haptics_right, 0.1f, 1.0f, 0.25f);
            timer.Enabled = false;
        }

        private void SetVRMatrixSet()
        {
            if (hooksSet && enableVR)
            {
                Imports.UpdateController(controllerCallback);
                hmdMatrix = Imports.GetFramePose(poseType.hmdPosition, -1);// * hmdWorldScale;
                lhcMatrix = Imports.GetFramePose(poseType.LeftHand, -1);// * hmdWorldScale;
                lhcPalmMatrix = Imports.GetFramePose(poseType.LeftHandPalm, -1);// * hmdWorldScale;
                rhcMatrix = Imports.GetFramePose(poseType.RightHand, -1);// * hmdWorldScale;
                rhcPalmMatrix = Imports.GetFramePose(poseType.RightHandPalm, -1);// * hmdWorldScale;
                //hmdMatrix.Translation = headBoneMatrix.Translation;

                Matrix4x4.Invert(hmdMatrix, out hmdMatrixI);
                avgHCPosition = (lhcMatrix.Translation + rhcMatrix.Translation) / 2;

                handWatch = lhcMatrix * hmdWorldScale;
                handBoneRay = rhcMatrix * hmdWorldScale;
            }
        }
        
        public void RunUpdate()
        {
            if (hooksSet && enableVR)
            {
                if (!Plugin.cfg!.data.standingMode)
                {
                    RawGameCamera* renderCam = (RawGameCamera*)scCameraManager->CurrentCamera;
                    if (renderCam != null)
                        avgHCRotation = MathF.PI;
                }
                else
                    avgHCRotation = MathF.Atan2(avgHCPosition.X, avgHCPosition.Z);

                frfCalculateViewMatrix = false;

                Point currentMouse = new Point();
                Point halfScreen = new Point();
                if (dx11DeviceInstance != null && dx11DeviceInstance->SwapChain != null)
                {
                    halfScreen.X = ((int)dx11DeviceInstance->SwapChain->Width / 2);
                    halfScreen.Y = ((int)dx11DeviceInstance->SwapChain->Height / 2);
                }
                
                ScreenSettings* screenSettings = *(ScreenSettings**)((UInt64)frameworkInstance + 0x7A8);
                //Log!.Info($"{(int)dx11DeviceInstance->SwapChain->Height} {(int)dx11DeviceInstance->SwapChain->Width}");
                Imports.GetCursorPos(out currentMouse);
                Imports.ScreenToClient((IntPtr)screenSettings->hWnd, out currentMouse);

                int mouseMultiplyer = (int)(Plugin.cfg!.data.mouseMultiplyer + 1);

                //----
                // Changes anchor from top left corner to middle of screen
                //----
                virtualMouse.X = halfScreen.X + ((currentMouse.X - halfScreen.X) * mouseMultiplyer);
                virtualMouse.Y = halfScreen.Y + ((currentMouse.Y - halfScreen.Y) * mouseMultiplyer);
                
                if (gameMode.Current == CameraModes.ThirdPerson && gameMode.Changed == true)
                    FirstToThirdPersonView();
                else if (gameMode.Current == CameraModes.FirstPerson && gameMode.Changed == true)
                    ThirdToFirstPersonView();

                //----
                // Changes to 3rd person when a cutscene is triggered
                // and back when it ends
                //----
                if (inCutscene.Changed)
                    if (inCutscene.Current)
                        FirstToThirdPersonView();
                    else
                        if (gameMode.Current == CameraModes.FirstPerson)
                        ThirdToFirstPersonView();

                isMounted = false;
                //uiAngleOffset += 0.5f;

                if (Plugin.ClientState!.LocalPlayer != null)
                {
                    Character* bonedCharacter = (Character*)Plugin.ClientState!.LocalPlayer.Address;
                    isMounted = bonedCharacter->IsMounted();
                }

                exExtras.UpdateHandyHousing(xboxStatus, hmdMatrix, rhcMatrix, lhcMatrix);

                //----
                // Haptics if mouse over target changes
                //----
                mouseoverTarget.Current = (targetSystem->MouseOverTarget == curMouseOverTarget && targetSystem->MouseOverTarget != null);
                curMouseOverTarget = targetSystem->MouseOverTarget;
                if (mouseoverTarget.Current && mouseoverTarget.Changed)
                {
                    RayOverFeedbackTimer.Interval = 250;
                    RayOverFeedbackTimer.Elapsed += (sender, e) =>
                    {
                        RayOverFeedbackTick(RayOverFeedbackTimer);
                    };
                    RayOverFeedbackTimer.Enabled = true;
                }
                else if (!mouseoverTarget.Current && mouseoverTarget.Changed)
                {
                    RayOverFeedbackTimer.Enabled = false;
                }

                //----
                // Saves the target arrow alpha
                //----
                if (targetAddonAlpha == 0)
                {
                    AtkUnitBase* targetAddon = (AtkUnitBase*)Plugin.GameGui!.GetAddonByName("_TargetCursor", 1);
                    if (targetAddon != null)
                        targetAddonAlpha = targetAddon->Alpha;
                }
                
                isCharMake = (AtkUnitBase*)Plugin.GameGui!.GetAddonByName("_CharaMakeTitle", 1) != null;
                isCharSelect = (AtkUnitBase*)Plugin.GameGui!.GetAddonByName("_CharaSelectTitle", 1) != null;
                isHousing = (AtkUnitBase*)Plugin.GameGui!.GetAddonByName("HousingGoods", 1) != null;

                if (isCharSelect || isCharMake)
                    gameMode.Current = CameraModes.ThirdPerson;

                if (!isCharMake && !isCharSelect && Plugin.ClientState!.LocalPlayer == null)
                    timer = 100;

                if (timer > 0)
                {
                    forceFloatingScreen = true;
                    timer--;
                }
                else if (timer == 0)
                {
                    timer = -1;
                }
            }
        }

        public void toggleDalamudMode()
        {
            dalamudMode = !dalamudMode;
        }

        public void ForceFloatingScreen(bool forceFloating, bool isCutscene)
        {
            forceFloatingScreen = forceFloating;
            inCutscene.Current = isCutscene;
        }

        public void SetRotateAmount(float x, float y)
        {
            rotateAmount.X = (x * Deg2Rad);
            rotateAmount.Y = (y * Deg2Rad);
        }

        public Point GetWindowSize()
        {
            Rectangle rectangle = new Rectangle();
            ScreenSettings* screenSettings = *(ScreenSettings**)((UInt64)frameworkInstance + 0x7A8);
            Imports.GetClientRect((IntPtr)screenSettings->hWnd, out rectangle);
            return new Point(rectangle.Width, rectangle.Height);
        }

        public void WindowResize(int width, int height)
        {
            //----
            // Resizes the internal buffers
            //----
            if (dx11DeviceInstance == null)
                dx11DeviceInstance = Device.Instance();

            dx11DeviceInstance->NewWidth = (uint)width;
            dx11DeviceInstance->NewHeight = (uint)height;
            dx11DeviceInstance->RequestResolutionChange = 1;

            //----
            // Resizes the client window to match the internal buffers
            //----
            ScreenSettings* screenSettings = *(ScreenSettings**)((UInt64)frameworkInstance + 0x7A8);
            if(screenSettings != null && screenSettings->hWnd != 0)
                Imports.ResizeWindow((IntPtr)screenSettings->hWnd, width, height);
        }

        public void WindowMove(bool reset)
        {
            //----
            // should get the value from ingame, but value dosnt return a 0 - x adapter
            //----
            uint mainScreenAdapter = 0; // GetSettingsValue(MappedSettings["MainAdapter"], 0);
            ScreenSettings* screenSettings = *(ScreenSettings**)((UInt64)frameworkInstance + 0x7A8);
            Imports.MoveWindowPos((IntPtr)screenSettings->hWnd, (int)mainScreenAdapter, reset);
        }

        public void SetRenderingMode()
        {
            if (hooksSet && enableVR)
            {
                RenderModes rMode = curRenderMode;
                if (Plugin.cfg!.data.mode2d)
                    rMode = RenderModes.TwoD;
                else
                    rMode = RenderModes.AlternatEye;

                if (rMode != curRenderMode)
                {
                    curRenderMode = rMode;

                    if (curRenderMode == RenderModes.TwoD)
                    {
                        eyeOffsetMatrix[0] = Matrix4x4.Identity;
                        eyeOffsetMatrix[1] = Matrix4x4.Identity;
                    }
                    else
                    {
                        eyeOffsetMatrix[0] = Imports.GetFramePose(poseType.EyeOffset, 0);
                        eyeOffsetMatrix[1] = Imports.GetFramePose(poseType.EyeOffset, 1);
                    }
                }
                hmdWorldScale = Matrix4x4.Identity;
                if (gameMode.Current == CameraModes.FirstPerson)
                    hmdWorldScale = Matrix4x4.CreateScale((armLength / 0.5f) * (Plugin.cfg!.data.armMultiplier / 100.0f));

                gameProjectionMatrix[0] = Imports.GetFramePose(poseType.Projection, 0);
                gameProjectionMatrix[1] = Imports.GetFramePose(poseType.Projection, 1);

                hmdOffsetFirstPerson = Matrix4x4.CreateTranslation(0, (Plugin.cfg.data.offsetAmountYFPS / 100), (Plugin.cfg.data.offsetAmountZFPS / 100));
                //hmdOffsetFirstPerson *= hmdWorldScale;

                hmdOffsetThirdPerson = Matrix4x4.CreateTranslation((Plugin.cfg.data.offsetAmountX / 100), (Plugin.cfg.data.offsetAmountY / 100), (Plugin.cfg.data.offsetAmountZ / 100));
                //hmdOffsetThirdPerson *= hmdWorldScale;

                hmdOffsetMountedFirstPerson = Matrix4x4.CreateTranslation(0, (Plugin.cfg.data.offsetAmountYFPSMount / 100), (Plugin.cfg.data.offsetAmountZFPSMount / 100));
                //hmdOffsetMountedFirstPerson *= hmdWorldScale;
            }
        }

        /*
        //----
        // CascadeShadow_UpdateConstantBuffer
        //----
        private delegate void CSUpdateConstBufDg(UInt64 a, UInt64 b);
        [Signature(Signatures.CSUpdateConstBuf, DetourName = nameof(CSUpdateConstBufFn))]
        private Hook<CSUpdateConstBufDg>? CSUpdateConstBufHook = null;

        [HandleStatus("CSUpdateConstBuf")]
        public void CSUpdateConstBufStatus(bool status, bool dispose)
        {
            if (dispose)
                CSUpdateConstBufHook?.Dispose();
            else
                if (status)
                    CSUpdateConstBufHook?.Enable();
                else
                    CSUpdateConstBufHook?.Disable();
        }

        private void CSUpdateConstBufFn(UInt64 a, UInt64 b)
        {
            if (hooksSet && enableVR)
            {
                overrideFromParent.Push(true);
                CSUpdateConstBufHook!.Original(a, b);
                overrideFromParent.Pop();
            }
            else
                CSUpdateConstBufHook!.Original(a, b);
        }

        */
        

        //----
        // NamePlateDraw
        //----
        private delegate void NamePlateDrawDg(AddonNamePlate* a);
        [Signature(Signatures.NamePlateDraw, DetourName = nameof(NamePlateDrawFn))]
        private Hook<NamePlateDrawDg>? NamePlateDrawHook = null;

        [HandleStatus("NamePlateDraw")]
        public void NamePlateDrawStatus(bool status, bool dispose)
        {
            if (dispose)
                NamePlateDrawHook?.Dispose();
            else
                if (status)
                    NamePlateDrawHook?.Enable();
                else
                    NamePlateDrawHook?.Disable();
        }

        private void NamePlateDrawFn(AddonNamePlate* a)
        {
            if (hooksSet && enableVR)
            {
                //----
                // Disables the target arrow until it can be put in the world
                //----
                AtkUnitBase* targetAddon = (AtkUnitBase*)Plugin.GameGui!.GetAddonByName("_TargetCursor", 1);
                if (targetAddon != null)
                {
                    targetAddon->Alpha = 1;
                    targetAddon->Hide(true, false, 0);
                    //targetAddon->RootNode->SetUseDepthBasedPriority(true);
                }

                fixed (AtkTextNode** pvrTargetCursor = &vrTargetCursor)
                    VRCursor.SetupVRTargetCursor(pvrTargetCursor, Plugin.cfg!.data.targetCursorSize);

                for (byte i = 0; i < NamePlateCount; i++)
                {
                    NamePlateObject* npObj = &a->NamePlateObjectArray[i];
                    AtkComponentBase* npComponent = npObj->RootComponentNode->Component;

                    for (int j = 0; j < npComponent->UldManager.NodeListCount; j++)
                    {
                        AtkResNode* child = npComponent->UldManager.NodeList[j];
                        child->SetUseDepthBasedPriority(true);
                    }

                    npObj->RootComponentNode->Component->UldManager.UpdateDrawNodeList();
                }

                NamePlateObject* selectedNamePlate = null;
                var framework = Framework.Instance();
                UI3DModule* ui3DModule = framework->GetUIModule()->GetUI3DModule();

                IGameObject targObj = Plugin.TargetManager!.Target!;
                if (targObj != null)
                {
                    TargetSystem* targSys = (TargetSystem*)targObj.Address;
                    for (int i = 0; i < ui3DModule->NamePlateObjectInfoCount; i++)
                    {
                        UI3DModule.ObjectInfo* objectInfo = ui3DModule->NamePlateObjectInfoPointers[i];
                        if (objectInfo->GameObject == targSys->Target)
                        {
                            selectedNamePlate = &a->NamePlateObjectArray[objectInfo->NamePlateIndex];
                            break;
                        }
                    }
                }

                fixed (AtkTextNode** pvrTargetCursor = &vrTargetCursor)
                {
                    VRCursor.UpdateVRCursorSize(pvrTargetCursor, Plugin.cfg!.data.targetCursorSize);
                    VRCursor.SetVRCursor(pvrTargetCursor, selectedNamePlate);
                }
            }

            NamePlateDrawHook!.Original(a);
        }

        /*
        //----
        // LoadCharacter
        //----
        private delegate UInt64 LoadCharacterDg(UInt64 a, UInt64 b, UInt64 c, UInt64 d, UInt64 e, UInt64 f);
        [Signature(Signatures.LoadCharacter, DetourName = nameof(LoadCharacterFn))]
        private Hook<LoadCharacterDg>? LoadCharacterHook = null;

        [HandleStatus("LoadCharacter")]
        public void LoadCharacterStatus(bool status, bool dispose)
        {
            if (dispose)
                LoadCharacterHook?.Dispose();
            else
                if (status)
                    LoadCharacterHook?.Enable();
                else
                    LoadCharacterHook?.Disable();
        }

        private UInt64 LoadCharacterFn(UInt64 a, UInt64 b, UInt64 c, UInt64 d, UInt64 e, UInt64 f)
        {
            IPlayerCharacter player = Plugin.ClientState!.LocalPlayer!;
            if (player != null && (UInt64)player.Address == a)
            {
                CharCustData* cData = (CharCustData*)c;
                CharEquipData* eData = (CharEquipData*)d;
            }
            //Log!.Info($"LoadCharacterFn {a:X} {b:X} {c:X} {d:X} {e:X} {f:X}");
            return LoadCharacterHook!.Original(a, b, c, d, e, f);
        }


        

        //----
        // ChangeWeapon
        //----
        private delegate void ChangeWeaponDg(UInt64 address, CharWeaponSlots index, CharWeaponSlotData item, byte d, byte e, byte f, byte g);
        //[Signature(Signatures.ChangeWeapon, DetourName = nameof(ChangeWeaponFn))]
        private Hook<ChangeWeaponDg>? ChangeWeaponHook = null;

        //[HandleStatus("ChangeWeapon")]
        public void ChangeWeaponStatus(bool status, bool dispose)
        {
            if (dispose)
                ChangeWeaponHook?.Dispose();
            else
                if (status)
                    ChangeWeaponHook?.Enable();
                else
                    ChangeWeaponHook?.Disable();
        }

        private void ChangeWeaponFn(UInt64 address, CharWeaponSlots index, CharWeaponSlotData item, byte d, byte e, byte f, byte g)
        {
            if (hooksSet && enableVR)
            {
                IPlayerCharacter player = Plugin.ClientState!.LocalPlayer!;
                if (player != null)
                {
                    Character* bonedCharacter = (Character*)player.Address;
                    if (bonedCharacter != null)
                    {
                        UInt64 equipOffset = (UInt64)(UInt64*)&bonedCharacter->DrawData;
                        if (equipOffset == address)
                        {
                            haveSavedEquipmentSet = true;
                            //currentWeaponSet.Data[(int)index] = item.Data;
                        }
                    }
                }
            }
            //Log!.Info($"ChangeWeaponFn {address:X} {index} | {item.Type}, {item.Id}, {item.Variant}, {item.Dye} | {d}, {e}, {f}, {g}");
            ChangeWeaponHook!.Original(address, index, item, d, e, f, g);
        }


        //----
        // EquipGearsetInternal
        //----
        private delegate void EquipGearsetInternalDg(UInt64 address, int b, byte c);
        //[Signature(Signatures.EquipGearsetInternal, DetourName = nameof(EquipGearsetInternalFn))]
        private Hook<EquipGearsetInternalDg>? EquipGearsetInternalHook = null;

        //[HandleStatus("EquipGearsetInternal")]
        public void EquipGearsetInternalStatus(bool status, bool dispose)
        {
            if (dispose)
                EquipGearsetInternalHook?.Dispose();
            else
                if (status)
                    EquipGearsetInternalHook?.Enable();
                else
                    EquipGearsetInternalHook?.Disable();
        }

        private void EquipGearsetInternalFn(UInt64 address, int b, byte c)
        {
            //Log!.Info($"EquipGearsetInternalFn {address:X} {b} {c}");
            EquipGearsetInternalHook!.Original(address, b, c);
        }

        */

        //----
        // Input.GetAnalogueValue
        //----
        private delegate Int32 GetAnalogueValueDg(UInt64 a, UInt64 b);
        [Signature(Signatures.GetAnalogueValue, DetourName = nameof(GetAnalogueValueFn))]
        private Hook<GetAnalogueValueDg>? GetAnalogueValueHook = null;

        [HandleStatus("GetAnalogueValue")]
        public void GetAnalogueValueStatus(bool status, bool dispose)
        {
            if (dispose)
                GetAnalogueValueHook?.Dispose();
            else
            {
                if (status)
                    GetAnalogueValueHook?.Enable();
                else
                    GetAnalogueValueHook?.Disable();
            }
        }

        // 0 mouse left right
        // 1 mouse up down
        // 3 left | left right
        // 4 left | up down
        // 5 right | left right
        // 6 right | up down
        private Int32 GetAnalogueValueFn(UInt64 a, UInt64 b)
        {
            Int32 retVal = GetAnalogueValueHook!.Original(a, b);

            if (hooksSet && enableVR)
            {
                switch (b)
                {
                    case 0:
                    case 1:
                    case 2:
                        break;
                    case 3:
                        break;
                    case 4:
                        break;
                    case 5:
                        //Log!.Info($"GetAnalogueValueFn: {retVal} {leftBumperValue}");
                        if (MathF.Abs(retVal) >= 0 && MathF.Abs(retVal) < 15) rightHorizontalCenter = true;
                        if (Plugin.cfg!.data.horizontalLock && MathF.Abs(leftBumperValue) < 0.5)
                        {
                            if (MathF.Abs(retVal) > 75 && rightHorizontalCenter)
                            {
                                rightHorizontalCenter = false;
                                rotateAmount.X -= (Plugin.cfg!.data.snapRotateAmountX * Deg2Rad) * MathF.Sign(retVal);
                            }
                            retVal = 0;
                        }
                        break;
                    case 6:
                        //Log!.Info($"GetAnalogueValueFn: {retVal}");
                        if (MathF.Abs(retVal) >= 0 && MathF.Abs(retVal) < 15) rightVerticalCenter = true;
                        if (Plugin.cfg!.data.verticalLock && MathF.Abs(leftBumperValue) < 0.5)
                        {
                            if (MathF.Abs(retVal) > 75 && rightVerticalCenter && gameMode.Current == CameraModes.ThirdPerson)
                            {
                                rightVerticalCenter = false;
                                rotateAmount.Y -= (Plugin.cfg!.data.snapRotateAmountY * Deg2Rad) * MathF.Sign(retVal);
                            }
                            retVal = 0;
                        }
                        break;
                }
            }
            return retVal;
        }

        //----
        // Controller Input
        //----

        private delegate void ControllerInputDg(UInt64 a, UInt64 b, uint c);
        [Signature(Signatures.ControllerInput, DetourName = nameof(ControllerInputFn))]
        private Hook<ControllerInputDg>? ControllerInputHook = null;

        [HandleStatus("ControllerInput")]
        public void ControllerInputStatus(bool status, bool dispose)
        {
            if (dispose)
                ControllerInputHook?.Dispose();
            else
            {
                if (status)
                    ControllerInputHook?.Enable();
                else
                    ControllerInputHook?.Disable();
            }
        }

        float rightTriggerValue = 0;
        bool leftClickActive = false;
        bool rightClickActive = false;
        ChangedTypeBool rightBumperClick = new ChangedTypeBool();
        ChangedTypeBool rightTriggerClick = new ChangedTypeBool();
        float leftStickOrig = 0;
        Stopwatch leftStickTimer = new Stopwatch();
        ChangedTypeBool leftStickTimerHaptic = new ChangedTypeBool();
        float rightStickOrig = 0;
        Stopwatch rightStickTimer = new Stopwatch();
        ChangedTypeBool rightStickTimerHaptic = new ChangedTypeBool();

        public void ControllerInputFn(UInt64 a, UInt64 b, uint c)
        {
            UInt64 controllerBase = *(UInt64*)(a + 0x70);
            UInt64 controllerIndex = *(byte*)(a + 0x434);

            UInt64 controllerAddress = controllerBase + 0x30 + ((controllerIndex * 0x9C8) * 4);
            XBoxButtonOffsets* offsets = (XBoxButtonOffsets*)(controllerAddress);

            leftBumperValue = offsets->left_bumper;

            if (hooksSet && enableVR && Plugin.cfg!.data.motioncontrol)
            {
                if (xboxStatus.dpad_up.active)
                    offsets->dpad_up = xboxStatus.dpad_up.value;
                if (xboxStatus.dpad_down.active)
                    offsets->dpad_down = xboxStatus.dpad_down.value;
                if (xboxStatus.dpad_left.active)
                    offsets->dpad_left = xboxStatus.dpad_left.value;
                if (xboxStatus.dpad_right.active)
                    offsets->dpad_right = xboxStatus.dpad_right.value;
                if (xboxStatus.left_stick_down.active)
                    offsets->left_stick_down = xboxStatus.left_stick_down.value;
                if (xboxStatus.left_stick_up.active)
                    offsets->left_stick_up = xboxStatus.left_stick_up.value;
                if (xboxStatus.left_stick_left.active)
                    offsets->left_stick_left = xboxStatus.left_stick_left.value;
                if (xboxStatus.left_stick_right.active)
                    offsets->left_stick_right = xboxStatus.left_stick_right.value;
                if (xboxStatus.right_stick_down.active)
                    offsets->right_stick_down = xboxStatus.right_stick_down.value;
                if (xboxStatus.right_stick_up.active)
                    offsets->right_stick_up = xboxStatus.right_stick_up.value;
                if (xboxStatus.right_stick_left.active)
                    offsets->right_stick_left = xboxStatus.right_stick_left.value;
                if (xboxStatus.right_stick_right.active)
                    offsets->right_stick_right = xboxStatus.right_stick_right.value;
                if (xboxStatus.button_y.active)
                    offsets->button_y = xboxStatus.button_y.value;
                if (xboxStatus.button_b.active)
                    offsets->button_b = xboxStatus.button_b.value;
                if (xboxStatus.button_a.active)
                    offsets->button_a = xboxStatus.button_a.value;
                if (xboxStatus.button_x.active)
                    offsets->button_x = xboxStatus.button_x.value;
                if (xboxStatus.left_bumper.active)
                    offsets->left_bumper = xboxStatus.left_bumper.value;
                if (xboxStatus.left_trigger.active)
                    offsets->left_trigger = xboxStatus.left_trigger.value;
                if (xboxStatus.left_stick_click.active)
                    offsets->left_stick_click = xboxStatus.left_stick_click.value;
                if (xboxStatus.right_bumper.active)
                    offsets->right_bumper = xboxStatus.right_bumper.value;
                if (xboxStatus.right_trigger.active)
                    offsets->right_trigger = xboxStatus.right_trigger.value;
                if (xboxStatus.right_stick_click.active)
                    offsets->right_stick_click = xboxStatus.right_stick_click.value;
                if (xboxStatus.start.active)
                    offsets->start = xboxStatus.start.value;
                if (xboxStatus.select.active)
                    offsets->select = xboxStatus.select.value;
            }

            bool doLocomotion = false;
            Vector3 angles = new Vector3();
            if (Plugin.cfg!.data.conloc)
            {
                angles = GetAngles(lhcPalmMatrix);
                doLocomotion = true;
            }
            else if (Plugin.cfg!.data.hmdloc)
            {
                angles = GetAngles(hmdMatrix);
                doLocomotion = true;
            }

            //if (doLocomotion && Plugin.cfg!.data.vertloc)
            //    movementManager->Ground.AscendDecendPitch = MathF.Min(1.5f, MathF.Max(-1.5f, (angles.X * 1.5f))) + 0.5f;

            float up_down = (offsets->left_stick_up + -offsets->left_stick_down);
            float left_right = (-offsets->left_stick_left + offsets->left_stick_right);

            if (doLocomotion && gameMode.Current == CameraModes.ThirdPerson)
            {
                float stickAngle = MathF.Atan2(left_right, up_down);
                if (left_right == -1) stickAngle = -90 * Deg2Rad;
                else if (left_right == 1) stickAngle = 90 * Deg2Rad;
                stickAngle += angles.Y;

                Vector2 newValue = new Vector2(MathF.Sin(stickAngle), MathF.Cos(stickAngle));
                float hyp = MathF.Sqrt(up_down * up_down + left_right * left_right);
                newValue.X *= hyp;
                newValue.Y *= hyp;

                //Log!.Info($"{angles.Y * Rad2Deg} {newValue.Y} | {newValue.X} | {stickAngle * Rad2Deg}");
                if (newValue.Y > 0)
                {
                    offsets->left_stick_up = MathF.Abs(newValue.Y);
                    offsets->left_stick_down = 0;
                }
                else
                {
                    offsets->left_stick_up = 0;
                    offsets->left_stick_down = MathF.Abs(newValue.Y);
                }

                if (newValue.X > 0)
                {
                    offsets->left_stick_left = 0;
                    offsets->left_stick_right = MathF.Abs(newValue.X);
                }
                else
                {
                    offsets->left_stick_left = MathF.Abs(newValue.X);
                    offsets->left_stick_right = 0;
                }
                
            }
            if (hooksSet && enableVR && Plugin.cfg!.data.motioncontrol)
            {
                leftBumperValue = offsets->left_bumper;
                float curRightTriggerValue = offsets->right_trigger;
                float curRightBumperValue = offsets->right_bumper;

                rightTriggerClick.Current = (curRightTriggerValue > 0.75f);
                rightBumperClick.Current = (curRightBumperValue > 0.75f);

                InputAnalogActionData analog = new InputAnalogActionData();
                InputDigitalActionData digital = new InputDigitalActionData();

                //----
                // Right Click if trigger and bumper pressed
                //----
                if (leftClickActive == false && rightTriggerClick.Current == true && rightTriggerClick.Changed == true && rightBumperClick.Current == true)
                {
                    rightClickActive = true;
                    digital.bState = true;
                    inputRightClick(analog, digital);
                }
                else if (leftClickActive == false && rightTriggerClick.Current == false && rightTriggerClick.Changed == true && rightBumperClick.Current == true)
                {
                    rightClickActive = false;
                    digital.bState = false;
                    inputRightClick(analog, digital);
                }

                //----
                // Left Click if only trigger pressed
                //----
                if (rightClickActive == false && rightTriggerClick.Current == true && rightTriggerClick.Changed == true && rightBumperClick.Current == false)
                {
                    leftClickActive = true;
                    digital.bState = true;
                    inputLeftClick(analog, digital);
                }
                else if (rightClickActive == false && rightTriggerClick.Current == false && rightTriggerClick.Changed == true && rightBumperClick.Current == false)
                {
                    leftClickActive = false;
                    digital.bState = false;
                    inputLeftClick(analog, digital);
                }

                if (isHousing)
                    offsets->right_trigger = 0;

                //----
                // Left Stick Pressed
                //----
                bool updateLeftAfterInput = false;
                if (xboxStatus.left_stick_click.active == true && xboxStatus.left_stick_click.ChangedStatus == true)
                {
                    leftStickOrig = offsets->left_stick_click;
                    leftStickTimer = Stopwatch.StartNew();
                    leftStickTimerHaptic.Current = false;
                }
                //----
                // Left Stick Released
                //----
                else if (xboxStatus.left_stick_click.active == false && xboxStatus.left_stick_click.ChangedStatus == true)
                {
                    leftStickTimer.Stop();
                    if (leftStickTimer.ElapsedMilliseconds > 1000)
                        leftStickAltMode = ((leftStickAltMode) ? false : true);
                    else
                        offsets->left_stick_click = leftStickOrig;

                    updateLeftAfterInput = true;
                    leftStickOrig = 0;
                }

                if (leftStickTimer.IsRunning)
                {
                    leftStickTimerHaptic.Current = (leftStickTimer.ElapsedMilliseconds >= 1000);
                    if (leftStickTimerHaptic.Changed == true)
                        Imports.HapticFeedback(ActionButtonLayout.haptics_left, 0.1f, 100.0f, 50.0f);
                    offsets->left_stick_click = 0;
                }


                //----
                // Right Stick Pressed
                //----
                bool updateRightAfterInput = false;
                if (xboxStatus.right_stick_click.active == true && xboxStatus.right_stick_click.ChangedStatus == true)
                {
                    rightStickOrig = offsets->right_stick_click;
                    rightStickTimer = Stopwatch.StartNew();
                    rightStickTimerHaptic.Current = false;
                }
                //----
                // Right Stick Released
                //----
                else if (xboxStatus.right_stick_click.active == false && xboxStatus.right_stick_click.ChangedStatus == true)
                {
                    rightStickTimer.Stop();
                    if (rightStickTimer.ElapsedMilliseconds > 1000)
                        rightStickAltMode = ((rightStickAltMode) ? false : true);
                    else
                    {
                        offsets->right_stick_click = rightStickOrig;
                    }

                    updateRightAfterInput = true;
                    rightStickOrig = 0;
                }

                if (rightStickTimer.IsRunning)
                {
                    rightStickTimerHaptic.Current = (rightStickTimer.ElapsedMilliseconds >= 1000);
                    if (rightStickTimerHaptic.Changed == true)
                        Imports.HapticFeedback(ActionButtonLayout.haptics_right, 0.1f, 100.0f, 50.0f);
                    offsets->right_stick_click = 0;
                }

                if (isCharMake || Plugin.cfg!.data.disableXboxShoulder)
                {
                    offsets->left_trigger = 0;
                    offsets->right_trigger = 0;

                    //offsets->left_bumper = 0; //needed for zooming
                    offsets->right_bumper = 0;
                }

                ControllerInputHook!.Original(a, b, c);

                if (doLocomotion && gameMode.Current == CameraModes.FirstPerson)
                {
                    if (MathF.Abs(up_down) > 0 || MathF.Abs(left_right) > 0)
                    {
                        Character* bonedCharacter = GetCharacterOrMouseover(2);
                        RawGameCamera* gameCamera = (RawGameCamera*)scCameraManager->CurrentCamera;
                        if (bonedCharacter != null && gameCamera != null)
                            bonedCharacter->GameObject.SetRotation(gameCamera->CurrentHRotation - angles.Y);
                    }
                    else
                    {
                        Character* bonedCharacter = GetCharacterOrMouseover(2);
                        RawGameCamera* gameCamera = (RawGameCamera*)scCameraManager->CurrentCamera;
                        if (bonedCharacter != null && gameCamera != null)
                        {
                            Structures.Model* model = (Structures.Model*)bonedCharacter->GameObject.DrawObject;
                            if (model != null)
                            {
                                Structures.Model* modelMount = (Structures.Model*)model->mountedObject;
                                Structures.Model* playerMount = (Structures.Model*)bonedCharacter->Mount.MountObject;
                                if (modelMount != null && playerMount == null)
                                {
                                    Vector3 mountAngles = GetAngles(modelMount->basePosition.Rotation.Convert());
                                    gameCamera->CurrentHRotation = mountAngles.Y;
                                    //bonedCharacter->GameObject.Rotate(0);
                                    //Plugin.Log!.Info($"{mountAngles}");
                                }
                            }
                        }
                    }
                }

                if (updateLeftAfterInput)
                    offsets->left_stick_click = leftStickOrig;
                if (updateRightAfterInput)
                    offsets->right_stick_click = rightStickOrig;
            }
            else
            {
                ControllerInputHook!.Original(a, b, c);
            }
        }

        

        
        Dictionary<UInt64, stCommonSkelBoneList> commonBones = new Dictionary<UInt64, stCommonSkelBoneList>();

        //----
        // RenderSkeletonList
        //----
        private delegate void RenderSkeletonListDg(UInt64 RenderSkeletonLinkedList, float frameTiming);
        [Signature(Signatures.RenderSkeletonList, DetourName = nameof(RenderSkeletonListFn))]
        private Hook<RenderSkeletonListDg>? RenderSkeletonListHook = null;

        [HandleStatus("RenderSkeletonList")]
        public void RenderSkeletonListStatus(bool status, bool dispose)
        {
            if (dispose)
                RenderSkeletonListHook?.Dispose();
            else
                if (status)
                    RenderSkeletonListHook?.Enable();
                else
                    RenderSkeletonListHook?.Disable();
        }
        private unsafe void RenderSkeletonListFn(UInt64 RenderSkeletonLinkedList, float frameTiming)
        {
            //Log!.Info($"RenderSkeletonListFn {(UInt64)RenderSkeletonLinkedList:x} {curEye}");
            RenderSkeletonListHook!.Original(RenderSkeletonLinkedList, frameTiming);

            RawGameCamera* gameCamera = (RawGameCamera*)scCameraManager->CurrentCamera;
            if (gameCamera == null)
                return;

            Character* bonedCharacter = GetCharacterOrMouseover();
            if (bonedCharacter == null)
                return;

            Structures.Model* model = (Structures.Model*)bonedCharacter->GameObject.DrawObject;
            if (model == null)
                return;

            Skeleton* skeleton = model->skeleton;
            if(skeleton == null) 
                return;

            //UpdateBoneCamera();
            UpdateBoneScales();

            uiAngleOffset = 0;
            if (gameMode.Current == CameraModes.FirstPerson)
            {
                Structures.Model* modelMount = (Structures.Model*)model->mountedObject;
                Structures.Model* playerMount = (Structures.Model*)bonedCharacter->Mount.MountObject;
                if (modelMount != null && playerMount == null)
                {

                }
                else if (modelMount != null && playerMount != null)
                {
                    Vector3 mountAngle = GetAngles(modelMount->basePosition.Rotation.Convert());
                    Skeleton* mountSkeleton = modelMount->skeleton;
                    if (mountSkeleton != null)
                        mountSkeleton->Transform.Rotation = Quaternion.CreateFromYawPitchRoll(gameCamera->CurrentHRotation - (MathF.PI - avgHCRotation), 0, 0);
                }
                else
                {
                    skeleton->Transform.Rotation = Quaternion.CreateFromYawPitchRoll(gameCamera->CurrentHRotation - (MathF.PI - avgHCRotation), 0, 0);
                }

                uiAngleOffset = 0;// MathF.Floor(((avgHCRotation - 90 - 45) * Rad2Deg) / 72) * 72.0f;
            }

            while (multiIK[curEye].Count > 0)
            {
                //Log!.Info($"{(UInt64)skeleton:x} {(UInt64)itmSkeleton:x} {multiIK.Count}");
                stMultiIK ikElement = multiIK[curEye].Dequeue();
                RunIKElement(&ikElement);
            }
        }
        /*
        private float prevFrameTiming = 0;

        //----
        // RenderSkeletonListAnimation
        //----
        private delegate void RenderSkeletonListAnimationDg(UInt64 RenderSkeletonLinkedList, float frameTiming, UInt64 c);
        [Signature(Signatures.RenderSkeletonListAnimation, DetourName = nameof(RenderSkeletonListAnimationFn))]
        private Hook<RenderSkeletonListAnimationDg>? RenderSkeletonListAnimationHook = null;

        [HandleStatus("RenderSkeletonListAnimation")]
        public void RenderSkeletonListAnimationStatus(bool status, bool dispose)
        {
            if (dispose)
                RenderSkeletonListAnimationHook?.Dispose();
            else
                if (status)
                    RenderSkeletonListAnimationHook?.Enable();
                else
                    RenderSkeletonListAnimationHook?.Disable();
        }
        private unsafe void RenderSkeletonListAnimationFn(UInt64 RenderSkeletonLinkedList, float frameTiming, UInt64 c)
        {
            //_expectedFrameTime = (long)((1 / (Service.Settings.TargetFPS)) * TimeSpan.TicksPerSecond);
            frameTiming *= 0.615f;// (xivr_Ex.cfg!.data.offsetAmountZFPSMount / 100.0f);
            if (curEye == 0)
                prevFrameTiming = frameTiming;
            else
                frameTiming = prevFrameTiming;

            //Log!.Info($"RenderSkeletonListAnimationFn {(UInt64)RenderSkeletonLinkedList:x}");
            RenderSkeletonListAnimationHook!.Original(RenderSkeletonLinkedList, frameTiming, c);
        }
        */


        //----
        // GetBoneIndexFromName
        //----
        public delegate short GetBoneIndexFromNameDg(Skeleton* skeleton, String name);
        [Signature(Signatures.GetBoneIndexFromName, Fallibility = Fallibility.Fallible)]
        public static GetBoneIndexFromNameDg? GetBoneIndexFromNameFn = null;

        //----
        // twoBoneIK
        //----
        private delegate void twoBoneIKDg(byte* a1, hkIKSetup a2, hkaPose* pose);
        [Signature(Signatures.twoBoneIK, Fallibility = Fallibility.Fallible)]
        private twoBoneIKDg? twoBoneIKFn = null;

        private unsafe void RunIKElement(stMultiIK *ikElement)
        {
            //Log!.Info($"remove {curEye} {multiIKEye.Count} {(UInt64)curIK.objAddress:x}");
            if (ikElement->objCharacter == null)
                return;

            Structures.Model* model = (Structures.Model*)ikElement->objCharacter->GameObject.DrawObject;
            if (model == null)
                return;

            Skeleton* skeleton = model->skeleton;
            if (skeleton == null)
                return;

            SkeletonResourceHandle* srh = skeleton->SkeletonResourceHandles[0];
            if (srh == null)
                return;

            hkaSkeleton* hkaSkel = srh->HavokSkeleton;
            if (hkaSkel == null)
                return;

            if (!commonBones.ContainsKey((UInt64)hkaSkel))
                return;
            
            stCommonSkelBoneList csb = commonBones[(UInt64)hkaSkel];

            Matrix4x4 matrixHead = (Matrix4x4)ikElement->hmdMatrix;
            Matrix4x4 matrixLHC = (Matrix4x4)ikElement->lhcMatrix;
            Matrix4x4 matrixRHC = (Matrix4x4)ikElement->rhcMatrix;

            Matrix4x4 objSkeletonPosition = skeleton->Transform.ToMatrix();
            //Matrix4x4.Invert(objSkeletonPosition, out Matrix4x4 objSkeletonPositionI);

            Matrix4x4 objMountSkeletonPosition = Matrix4x4.Identity;
            Structures.Model* modelMount = (Structures.Model*)model->mountedObject;
            if (modelMount != null)
                objMountSkeletonPosition = modelMount->basePosition.ToMatrix();
            Matrix4x4.Invert(objMountSkeletonPosition, out Matrix4x4 objMountSkeletonPositionI);

            byte lockItem = 0;

            float armLength = csb.armLength * skeleton->Transform.Scale.Y;
            Matrix4x4 hmdLocalScale = Matrix4x4.CreateScale((armLength / 0.5f) * (ikElement->armMultiplier / 100.0f));
            Matrix4x4 hmdRotate = Matrix4x4.CreateFromYawPitchRoll(90 * Deg2Rad, 180 * Deg2Rad, 0 * Deg2Rad);
            Matrix4x4 hmdFlipScale = Matrix4x4.CreateScale(-1, 1, -1);
            Matrix4x4 avgController = Matrix4x4.CreateFromYawPitchRoll(MathF.PI - ikElement->avgControllerRotation, 0, 0);
            //Vector3 anglesSkel = GetAngles(model->Transform.Rotation);
            Vector3 anglesLHC = GetAngles(matrixLHC);
            Vector3 anglesRHC = GetAngles(matrixRHC);

            //Plugin.Log!.Info($"{avgHCRotation * Rad2Deg} {avgHCPosition}");
            
            hkIKSetup ikSetupHead = new hkIKSetup();
            bool runIKHead = false;
            if (csb.e_spine_b >= 0 && csb.e_spine_c >= 0 && csb.e_neck >= 0 && ikElement->doHandIK)
            {
                runIKHead = true;
                Matrix4x4 head = Matrix4x4.CreateFromYawPitchRoll(-90 * Deg2Rad, 0 * Deg2Rad, 90 * Deg2Rad) * matrixHead * hmdFlipScale;
                ikSetupHead.m_firstJointIdx = csb.e_spine_b;
                ikSetupHead.m_secondJointIdx = csb.e_spine_c;
                ikSetupHead.m_endBoneIdx = csb.e_neck;
                ikSetupHead.m_hingeAxisLS = new Vector4(0.0f, 0.0f, -1.0f, 0.0f);
                ikSetupHead.m_endTargetMS = new Vector4((head * hmdLocalScale).Translation, 0.0f);
                ikSetupHead.m_endTargetRotationMS = Quaternion.CreateFromRotationMatrix(head);
                ikSetupHead.m_enforceEndPosition = true;
                ikSetupHead.m_enforceEndRotation = true;
            }

            hkIKSetup ikSetupL = new hkIKSetup();
            bool runIKL = false;
            if (csb.e_arm_l >= 0 && csb.e_forearm_l >= 0 && csb.e_hand_l >= 0 && ikElement->doHandIK)
            {
                runIKL = true;
                Matrix4x4 palmL = hmdRotate * matrixLHC * avgController * hmdFlipScale;
                ikSetupL.m_firstJointIdx = csb.e_arm_l;
                ikSetupL.m_secondJointIdx = csb.e_forearm_l;
                ikSetupL.m_endBoneIdx = csb.e_hand_l;
                ikSetupL.m_hingeAxisLS = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
                ikSetupL.m_endTargetMS = new Vector4((palmL * hmdLocalScale).Translation, 0.0f);
                ikSetupL.m_endTargetRotationMS = Quaternion.CreateFromRotationMatrix(palmL);
                ikSetupL.m_enforceEndPosition = true;
                ikSetupL.m_enforceEndRotation = true;
            }

            hkIKSetup ikSetupR = new hkIKSetup();
            bool runIKR = false;
            if (csb.e_arm_r >= 0 && csb.e_forearm_r >= 0 && csb.e_hand_r >= 0 && ikElement->doHandIK)
            {
                runIKR = true;
                Matrix4x4 palmR = hmdRotate * matrixRHC * avgController * hmdFlipScale;
                ikSetupR.m_firstJointIdx = csb.e_arm_r;
                ikSetupR.m_secondJointIdx = csb.e_forearm_r;
                ikSetupR.m_endBoneIdx = csb.e_hand_r;
                ikSetupR.m_hingeAxisLS = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
                ikSetupR.m_endTargetMS = new Vector4((palmR * hmdLocalScale).Translation, 0.0f);
                ikSetupR.m_endTargetRotationMS = Quaternion.CreateFromRotationMatrix(palmR);
                ikSetupR.m_enforceEndPosition = true;
                ikSetupR.m_enforceEndRotation = true;
            }
            
            hkQsTransformf transform;
            for (ushort p = 0; p < skeleton->PartialSkeletonCount; p++)
            {
                hkaPose* objPose = skeleton->PartialSkeletons[p].GetHavokPose(0);
                if (objPose == null)
                    continue;

                if (p == 0 && csb.e_neck >= 0)
                {
                    float diffHeadNeck = MathF.Abs(objPose->ModelPose[csb.e_neck].Translation.Y - objPose->ModelPose[csb.e_head].Translation.Y);
                    //Log!.Info($"Neck Y {objPose->ModelPose[csb.e_neck].Translation.Y}");
                    neckOffsetAvg.AddNew(objPose->ModelPose[csb.e_neck].Translation.Y + diffHeadNeck);
                    //Log!.Info($"Neck {csb.e_neck} | {objPose->ModelPose[csb.e_neck].Translation.Y} Head {csb.e_head} | {objPose->ModelPose[csb.e_head].Translation.Y} = {diffHeadNeck}");

                    //----
                    // Debug crashes ik stuff and couldnt be bothered looking into why
                    // Works in release fine though
                    //----
#if DEBUG
#else
                    if (runIKHead)
                    {
                        //Plugin.Log!.Info($"ik Y {neckOffsetAvg.Average}");
                        ikSetupHead.m_endTargetMS.Y += neckOffsetAvg.Average + 0.25f;
                        twoBoneIKFn!(&lockItem, ikSetupHead, objPose);
                    }

                    if (runIKL)
                    {
                        ikSetupL.m_endTargetMS.Y += neckOffsetAvg.Average;
                        transform = objPose->LocalPose[csb.e_wrist_l];
                        transform.Rotation = Quaternion.CreateFromYawPitchRoll(0, (anglesLHC.Z / 2.0f), 0).Convert();
                        objPose->LocalPose[csb.e_wrist_l] = transform;
                        twoBoneIKFn!(&lockItem, ikSetupL, objPose);
                    }

                    if (runIKR)
                    {
                        ikSetupR.m_endTargetMS.Y += neckOffsetAvg.Average;
                        transform = objPose->LocalPose[csb.e_wrist_r];
                        transform.Rotation = Quaternion.CreateFromYawPitchRoll(0, (anglesRHC.Z / 2.0f), 0).Convert();
                        objPose->LocalPose[csb.e_wrist_r] = transform;
                        twoBoneIKFn!(&lockItem, ikSetupR, objPose);
                    }
#endif
                }
            }
        }

        private void UpdateBoneCamera()
        {
            if (inCutscene.Current || gameMode.Current == CameraModes.ThirdPerson)
                return;

            Character* bonedCharacter = GetCharacterOrMouseover();
            if (bonedCharacter == null)
                return;

            Structures.Model* model = (Structures.Model*)bonedCharacter->GameObject.DrawObject;
            if (model == null)
                return;

            Skeleton* skeleton = model->skeleton;
            if (skeleton == null)
                return;

            SkeletonResourceHandle* srh = skeleton->SkeletonResourceHandles[0];
            if (srh == null)
                return;

            hkaSkeleton* hkaSkel = srh->HavokSkeleton;
            if (hkaSkel == null)
                return;

            if (!commonBones.ContainsKey((UInt64)hkaSkel))
                return;

            plrSkeletonPosition = model->basePosition.ToMatrix();
            Matrix4x4.Invert(plrSkeletonPosition, out plrSkeletonPositionI);

            mntSkeletonPosition = Matrix4x4.Identity;
            Structures.Model* modelMount = (Structures.Model*)model->mountedObject;
            if (modelMount != null)
                mntSkeletonPosition = modelMount->basePosition.ToMatrix();
            Matrix4x4.Invert(mntSkeletonPosition, out mntSkeletonPositionI);

            stCommonSkelBoneList csb = commonBones[(UInt64)hkaSkel];
            if (skeleton->PartialSkeletonCount > 1)
            {
                hkaPose* objPose = skeleton->PartialSkeletons[0].GetHavokPose(0);
                if (objPose != null)
                {
                    float diffHeadNeck = MathF.Abs(objPose->ModelPose[csb.e_neck].Translation.Y - objPose->ModelPose[csb.e_head].Translation.Y);
                    headBoneMatrix = objPose->ModelPose[csb.e_neck].ToMatrix() * plrSkeletonPosition;
                    headBoneMatrix.M42 += diffHeadNeck;
                }
            }
        }

        private void UpdateBoneScales()
        {
            if (inCutscene.Current || gameMode.Current == CameraModes.ThirdPerson)
                return;

            Character* bonedCharacter = GetCharacterOrMouseover();
            if (bonedCharacter == null)
                return;

            Structures.Model* model = (Structures.Model*)bonedCharacter->GameObject.DrawObject;
            if (model == null)
                return;

            Skeleton* skeleton = model->skeleton;
            if (skeleton == null)
                return;

            SkeletonResourceHandle* srh = skeleton->SkeletonResourceHandles[0];
            if (srh == null)
                return;

            hkaSkeleton* hkaSkel = srh->HavokSkeleton;
            if (hkaSkel == null)
                return;

            if (!commonBones.ContainsKey((UInt64)hkaSkel))
                return;

            stCommonSkelBoneList csb = commonBones[(UInt64)hkaSkel];

            Transform transformS = skeleton->Transform;
            transformS.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), bonedCharacter->GameObject.Rotation);
            skeleton->Transform = transformS;

            float armLength = csb.armLength * skeleton->Transform.Scale.Y;
            hmdWorldScale = Matrix4x4.Identity;
            if (gameMode.Current == CameraModes.FirstPerson)
                hmdWorldScale = Matrix4x4.CreateScale((armLength / 0.5f) * (Plugin.cfg!.data.armMultiplier / 100.0f));
            hkQsTransformf transform;

            //----
            // Gets the rotation of the mount bone of the current mount
            //----
            Vector3 anglesMount = new Vector3(0, 0, 0);
            Structures.Model* modelMount = (Structures.Model*)model->mountedObject;
            if (modelMount != null && Plugin.cfg!.data.motioncontrol)
            {
                Skeleton* skeletonMount = modelMount->skeleton;
                if (skeletonMount != null)
                {
                    //----
                    // Keeps the mount the same rotation as the character so the hands are always correct
                    //----
                    transformS = skeletonMount->Transform;
                    transformS.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), bonedCharacter->GameObject.Rotation);
                    skeletonMount->Transform = transformS;

                    short mntMountId = GetBoneIndexFromNameFn!(skeletonMount, "n_mount");
                    short mntMountIdA = GetBoneIndexFromNameFn!(skeletonMount, "n_mount_a");
                    short mntMountIdB = GetBoneIndexFromNameFn!(skeletonMount, "n_mount_b");
                    short mntMountIdC = GetBoneIndexFromNameFn!(skeletonMount, "n_mount_c");
                    short mntAbdomenId = GetBoneIndexFromNameFn!(skeletonMount, "n_hara");
                    
                    if (mntAbdomenId > 0 && skeletonMount->PartialSkeletonCount == 1)
                    {
                        hkaPose* objPose = skeletonMount->PartialSkeletons[0].GetHavokPose(0);
                        if (objPose != null)
                        {
                            //anglesMount = GetAngles(objPose->ModelPose[mntAbdomenId].Rotation.Convert());

                            //----
                            // Keeps the mount bones the same as the character during animations
                            // so the hands are always correct
                            //----
                            transform = objPose->LocalPose[mntAbdomenId];
                            transform.Rotation = Quaternion.Identity.Convert();
                            objPose->LocalPose[mntAbdomenId] = transform;
                        }
                    }
                }
            }

            //----
            // Add the scabard and sheathes to the ToShrink list
            // as well as the weapon if its not shown
            //----
            List<KeyValuePair<short, Vector3>> scaleList = new List<KeyValuePair<short, Vector3>>();
            scaleList.Add(new KeyValuePair<short, Vector3>(csb.e_scabbard_l, new Vector3(0.0001f, 0.0001f, 0.0001f)));
            scaleList.Add(new KeyValuePair<short, Vector3>(csb.e_scabbard_r, new Vector3(0.0001f, 0.0001f, 0.0001f)));
            scaleList.Add(new KeyValuePair<short, Vector3>(csb.e_sheathe_l, new Vector3(0.0001f, 0.0001f, 0.0001f)));
            scaleList.Add(new KeyValuePair<short, Vector3>(csb.e_sheathe_r, new Vector3(0.0001f, 0.0001f, 0.0001f)));

            if (!Plugin.cfg!.data.showWeaponInHand)
            {
                scaleList.Add(new KeyValuePair<short, Vector3>(csb.e_weapon_l, new Vector3(0.0001f, 0.0001f, 0.0001f)));
                scaleList.Add(new KeyValuePair<short, Vector3>(csb.e_weapon_r, new Vector3(0.0001f, 0.0001f, 0.0001f)));
            }

            for (ushort p = 0; p < skeleton->PartialSkeletonCount; p++)
            {
                hkaPose* objPose = skeleton->PartialSkeletons[p].GetHavokPose(0);
                if (objPose == null)
                    continue;

                if (p == 0)
                {
                    //----
                    // Set the spine to the reverse of the abdomen to keep the upper torso stable
                    // while immersive mode is off
                    // Set the reference pose for anything above the waste
                    //----
                    if (csb.e_spine_a >= 0 && !Plugin.cfg!.data.immersiveMovement && !isMounted && Plugin.cfg!.data.motioncontrol)
                    {
                        Vector3 angles = GetAngles(objPose->ModelPose[csb.e_abdomen].Rotation.Convert());
                        //angles = anglesMount;
                        transform = objPose->LocalPose[csb.e_spine_a];
                        transform.Rotation = Quaternion.CreateFromYawPitchRoll(-angles.Y + (90 * Deg2Rad), 0 * Deg2Rad, 90 * Deg2Rad).Convert();
                        objPose->LocalPose[csb.e_spine_a] = transform; 

                        HashSet<short> children = csb.layout[csb.e_spine_a].Value;
                        foreach (short child in children)
                            objPose->LocalPose[child] = hkaSkel->ReferencePose[child];
                    } 
                    else if(isMounted && Plugin.cfg!.data.motioncontrol)
                    {
                        HashSet<short> children = csb.layout[csb.e_spine_c].Value;
                        foreach (short child in children)
                            objPose->LocalPose[child] = hkaSkel->ReferencePose[child];
                    }

                    //----
                    // Shrink the scabbards, sheathes and weapons if hidden
                    //----
                    foreach (KeyValuePair<short, Vector3> item in scaleList)
                    {
                        if (item.Key >= 0)
                        {
                            transform = objPose->LocalPose[item.Key];
                            transform.Scale = item.Value.Convert();
                            objPose->LocalPose[item.Key] = transform;
                        }
                    }

                    if (csb.e_neck >= 0)
                    {
                        //----
                        // Shrink the head and all child bones
                        //----
                        foreach (short id in csb.layout[csb.e_neck].Value)
                        {
                            transform = objPose->LocalPose[id];
                            transform.Translation = (transform.Translation.Convert() * -1).Convert();
                            transform.Scale = new Vector3(0.0001f, 0.0001f, 0.0001f).Convert();
                            objPose->LocalPose[id] = transform;
                        }

                        //----
                        // Rotate the neck to hide the head
                        //----
                        transform = objPose->LocalPose[csb.e_neck];
                        //transform.Rotation = Quaternion.CreateFromYawPitchRoll(0 * Deg2Rad, 0 * Deg2Rad, 180 * Deg2Rad).Convert();
                        transform.Scale = new Vector3(0.0001f, 0.0001f, 0.0001f).Convert();
                        objPose->LocalPose[csb.e_neck] = transform;
                    }
                }
                else
                {
                    //----
                    // shrink any other poses
                    //----
                    for (int i = 0; i < objPose->LocalPose.Length; i++)
                    {
                        transform = objPose->LocalPose[i];
                        //transform.Translation = (transform.Translation.Convert() * -1).Convert();
                        transform.Scale = new Vector3(0.0001f, 0.0001f, 0.0001f).Convert();
                        objPose->LocalPose[i] = transform;
                    }
                }
            }
        }




























        /*
private static class Signatures
{
    internal const string g_TextScale = "F3 0F 59 0D ?? ?? ?? ?? F3 0F 10 40 4C"; //
    internal const string g_SceneCameraManagerInstance = "48 8B 05 ?? ?? ?? ?? 48 8B 54 C8 58"; //
    internal const string g_ControlSystemCameraManager = "48 8D 0D ?? ?? ?? ?? F3 0F 10 4B ??"; //
    //internal const string g_SelectScreenCharacterList = "4C 8D 35 ?? ?? ?? ?? BF C8 00 00 00";
    internal const string g_ResourceManagerInstance = "48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? B0 01 48 83 C4 20"; //

    internal const string g_MovementManager = "48 8D 35 ?? ?? ?? ?? 84 C0 75";

    internal const string GameObjectGetPosition = "83 79 7C 00 75 09 F6 81 ?? ?? ?? ?? ?? 74 2A";
    //internal const string GetTargetFromRay = "E8 ?? ?? ?? ?? 84 C0 74 ?? 48 8B F3";
    
    //internal const string CSUpdateConstBuf = "4C 8B DC 49 89 5B 20 55 57 41 56 49 8D AB";
    //internal const string CutsceneViewMatrix = "E8 ?? ?? ?? ?? 80 BB 98 00 00 00 01 75 ??";
    //internal const string CSMakeProjectionMatrix = "E8 ?? ?? ?? ?? 0F 28 46 10 4C 8D 7E 10";
    //internal const string NamePlateDraw = "0F B7 81 ?? ?? ?? ?? 4C 8B C1 66 C1 E0 06";
    //internal const string RunBoneMath = "E8 ?? ?? ?? ?? 44 0F 28 58 10";
    //internal const string CalculateHeadAnimation = "48 89 6C 24 20 41 56 48 83 EC 30 48 8B EA";
    //internal const string LoadCharacter = "E8 ?? ?? ?? ?? 4D 85 F6 74 ?? 49 8B CE E8 ?? ?? ?? ?? 84 C0 75 ?? 4D 8B 46 20";
    ////internal const string ChangeWeapon = "E8 ?? ?? ?? ?? 80 7F 25 00";
    //internal const string EquipGearsetInternal = "E8 ?? ?? ?? ?? C7 87 08 01 00 00 00 00 00 00 C6 46 08 01 E9 ?? ?? ?? ?? 41 8B 4E 04";
    //internal const string PhysicsBoneUpdate = "E8 ?? ?? ?? ?? 48 8D 93 90 00 00 00 4C 8D 43 40";
    
    //internal const string syncModelSpace = "48 83 EC 18 80 79 38 00 0F 85 ?? ?? ?? ?? 48 8B 01";
    //internal const string threadedLookAtParent = "40 57 41 54 41 57 48 83 EC 30 4D 63 E0";
    //internal const string lookAtIK = "48 8B C4 48 89 58 08 48 89 70 10 F3 0F 11 58 ??";

    //internal const string RenderSkeletonList = "E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 8B 6C 24 ?? 48 8B 5C 24";
    //internal const string RenderSkeletonListSkeleton = "E8 ?? ?? ?? ?? 48 FF C3 48 83 C7 10 48 3B DE";
    //internal const string RenderSkeletonListAnimation = "E8 ?? ?? ?? ?? 44 39 64 24 28";
    //internal const string RenderSkeletonListPartialSkeleton = "E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 81 C3 C0 01 00 00";

}
*/

        private static class Signatures
        {
            internal const string g_tls_index = "8B 0D ?? ?? ?? ?? 45 33 E4 41";

            internal const string g_SelectScreenMouseOver = "48 89 05 ?? ?? ?? ?? 48 85 C0 74 ?? 48 8B 10";
            internal const string g_DisableSetCursorPosAddr = "FF ?? ?? ?? ?? 00 C6 05 ?? ?? ?? ?? 00 0F B6 43 38";

            internal const string AllocateQueueMemory = "E8 ?? ?? ?? ?? 48 8B F8 48 85 C0 0f 84 ?? ?? ?? ?? 45 33 C0 41 BA 05 00 00 00";
            internal const string ScreenPointToRay = "E8 ?? ?? ?? ?? 4C 8B E0 48 8B EB";
            internal const string ScreenPointToRay1 = "E8 ?? ?? ?? ?? F3 0F 10 45 A7 F3 0f 10 4D AB";
            internal const string MousePointScreenToClient = "E8 ?? ?? ?? ?? 48 8B 4B ?? 48 8D 54 24 ?? FF 15";
            internal const string DisableCinemaBars = "4C 8B DC 55 48 8B EC";

            internal const string ChangeEquipment = "E8 ?? ?? ?? ?? B1 ?? 41 FF C6";

            internal const string Pushback = "E8 ?? ?? ?? ?? 0F 28 B4 24 A0 01 00 00 48 8B 8C 24 90 01 00 00";
            internal const string PushbackUI = "E8 ?? ?? ?? ?? EB ?? E8 ?? ?? ?? ?? 4C 8D 5C 24 50";
            internal const string SetUIProj = "E8 ?? ?? ?? ?? 41 8B 44 24 08 4D 8D 4C 24 20";
            internal const string NamePlateDraw = "0F B7 81 ?? ?? ?? ?? 81 A1 2C 03 00 00 FF 1F 00 E0";

            internal const string CameraUpdateRender = "48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? F6 81 ?? ?? ?? ?? ?? 48 8B D9 48 89 B4 24";
            internal const string CalculateViewMatrix1 = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 89 AC 24 ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? F3 0F 10 B1 ?? ?? ?? ?? F3 0F 5C 71 ?? 0F 29 BC 24 ?? ?? ?? ?? F3 0F 10 B9";
            internal const string CalculateViewMatrix2 = "E8 ?? ?? ?? ?? 44 0F 28 54 24 ?? 44 0F 28 4C 24 ?? 44 0F 28 44 24 ?? 0F 28 BC 24 ?? ?? ?? ?? 0F 28 B4 24 ?? ?? ?? ?? 48 8B AC 24";
            internal const string CamManagerSetMatrix = "4C 8B DC 49 89 5B 10 49 89 73 18 49 89 7B 20 55 49 8D AB";
            internal const string SetMatrices = "E8 ?? ?? ?? ?? 0F 10 43 ?? C6 83";
            internal const string MakeProjectionMatrix2 = "E8 ?? ?? ?? ?? F3 44 0F 10 46";

            internal const string CameraUpdateMatrix = "E8 ?? ?? ?? ?? 45 33 FF 48 8D BE";
            internal const string CameraUpdateRotation = "E8 ?? ?? ?? ?? F3 0F 10 83 ?? ?? ?? ?? 41 0F 2E C1";

            internal const string SetRenderTarget = "E8 ?? ?? ?? ?? 4E 39 AC";
            internal const string DXGIPresent = "E8 ?? ?? ?? ?? C6 43 79 00";
            internal const string RenderThreadSetRenderTarget = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? F3 0F 10 5F 18";

            internal const string GetAnalogueValue = "E8 ?? ?? ?? ?? 66 44 0F 6E C3";
            internal const string ControllerInput = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 49 63 87 34 04 00 00";

            internal const string RunGameTasks = "E8 ?? ?? ?? ?? 48 8B 8B C0 35 00 00";
            internal const string FrameworkTick = "40 53 48 83 EC 20 FF 81 D0 16 00 00 48 8B D9 48 8D 4C 24 30";

            internal const string RenderSkeletonList = "E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 8B 6C 24 ?? 48 8B 5C 24";
            internal const string GetBoneIndexFromName = "E8 ?? ?? ?? ?? 66 23 C3";
            internal const string twoBoneIK = "E8 ?? ?? ?? ?? 0F 28 55 ?? 41 0F 28 D8";
        }

        //----
        // GetThreadedData
        //----
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate UInt64 GetThreadedDataDg();
        GetThreadedDataDg GetThreadedDataFn;

        public void GetThreadedDataInit()
        {
            //----
            // Used to access gs:[00000058] until i can do it in c#
            //----
            getThreadedDataHandle = GCHandle.Alloc(GetThreadedDataASM, GCHandleType.Pinned);
            if (!Imports.VirtualProtectEx(Process.GetCurrentProcess().Handle, getThreadedDataHandle.AddrOfPinnedObject(), (UIntPtr)GetThreadedDataASM.Length, 0x40 /* EXECUTE_READWRITE */, out uint _))
                return;
            else
                if (!Imports.FlushInstructionCache(Process.GetCurrentProcess().Handle, getThreadedDataHandle.AddrOfPinnedObject(), (UIntPtr)GetThreadedDataASM.Length))
                return;

            GetThreadedDataFn = Marshal.GetDelegateForFunctionPointer<GetThreadedDataDg>(getThreadedDataHandle.AddrOfPinnedObject());
        }

        private void GetThreadedDataDestroy()
        {
            if (getThreadedDataHandle.IsAllocated)
                getThreadedDataHandle.Free();
        }

        private UInt64 GetThreadedOffset()
        {
            UInt64 threadedData = GetThreadedDataFn();
            if (threadedData != 0)
            {
                threadedData = *(UInt64*)(threadedData + (UInt64)((*(int*)tls_index) * 8));
                threadedData = *(UInt64*)(threadedData + 0x250);
            }
            return threadedData;
        }

        private void AddcmdClear(Texture* rendTexture, Texture* depthTexture, bool depth = false, float r = 0, float g = 0, float b = 0, float a = 0)
        {
            UInt64 threadedOffset = GetThreadedOffset();
            if (threadedOffset != 0)
            {
                SetRenderTargetFn!(threadedOffset, 1, &rendTexture, depthTexture, 0, 0, 0, 0);
                AddcmdClear(depth, r, g, b, a);
            }
        }

        private void AddcmdClear(bool depth = false, float r = 0, float g = 0, float b = 0, float a = 0)
        {
            UInt64 threadedOffset = GetThreadedOffset();
            if (threadedOffset != 0)
            {
                UInt64 queueData = AllocateQueueMemmoryFn!(threadedOffset, 0x38);
                if (queueData != 0)
                {
                    Imports.ZeroMemory((byte*)queueData, 0x38);
                    cmdClearRenderDepth* cmd = (cmdClearRenderDepth*)queueData;
                    cmd->SwitchType = 4;
                    cmd->clearType = ((depth) ? 7 : 1);
                    cmd->colorR = r;
                    cmd->colorG = g;
                    cmd->colorB = b;
                    cmd->colorA = a;
                    cmd->clearDepth = 1;
                    cmd->clearStencil = 0;
                    cmd->clearCheck = 0;
                    PushbackFn!(threadedOffset, queueData);
                }
            }
        }

        private void AddcmdSetRendTargetFake(int curEye)
        {
            UInt64 threadedOffset = GetThreadedOffset();
            if (threadedOffset != 0)
            {
                UInt64 queueData = AllocateQueueMemmoryFn!(threadedOffset, 0x40);
                if (queueData != 0)
                {
                    Imports.ZeroMemory((byte*)queueData, 0x40);
                    cmdSetRendTarget* cmd = (cmdSetRendTarget*)queueData;
                    cmd->SwitchType = 0;
                    cmd->numRenderTargets = curEye;
                    cmd->RenderTarget0 = null;
                    PushbackFn!(threadedOffset, queueData);
                }
            }
        }

        //----
        // SetRenderTarget
        //----
        private delegate void SetRenderTargetDg(UInt64 a, int b, Texture** c, Texture* d, short e, short f, short g, short h);
        [Signature(Signatures.SetRenderTarget, Fallibility = Fallibility.Fallible)]
        private SetRenderTargetDg? SetRenderTargetFn = null;

        //----
        // AllocateQueueMemory
        //----
        private delegate UInt64 AllocateQueueMemoryDg(UInt64 a, UInt64 b);
        [Signature(Signatures.AllocateQueueMemory, Fallibility = Fallibility.Fallible)]
        private AllocateQueueMemoryDg? AllocateQueueMemmoryFn = null;

        //----
        // Pushback
        //----
        private delegate void PushbackDg(UInt64 a, UInt64 b);
        [Signature(Signatures.Pushback, Fallibility = Fallibility.Fallible)]
        private PushbackDg? PushbackFn = null;


        //----
        // ScreenPointToRay
        //----
        private delegate Ray* ScreenPointToRayDg(RawGameCamera* gameCamera, Ray* ray, int mousePosX, int mousePosY);
        [Signature(Signatures.ScreenPointToRay, DetourName = nameof(ScreenPointToRayFn))]
        private Hook<ScreenPointToRayDg> ScreenPointToRayHook = null;

        [HandleStatus("ScreenPointToRay")]
        public void ScreenPointToRayStatus(bool status, bool dispose)
        {
            if (dispose)
                ScreenPointToRayHook?.Dispose();
            else
            {
                if (status)
                    ScreenPointToRayHook?.Enable();
                else
                    ScreenPointToRayHook?.Disable();
            }
        }
        private Ray* ScreenPointToRayFn(RawGameCamera* gameCamera, Ray* ray, int mousePosX, int mousePosY)
        {
            if (hooksSet && enableVR)
            {
                if (Plugin.cfg!.data.motioncontrol)
                {
                    Matrix4x4 rayPos = handBoneRay * curViewMatrixWithoutHMDI;
                    Vector3 frwdFar = new Vector3(rayPos.M31, rayPos.M32, rayPos.M33) * -1;
                    ray->Origin = rayPos.Translation;
                    ray->Direction = Vector3.Normalize(frwdFar);
                }
                else //if (xivr_Ex.cfg.data.hmdPointing)
                {
                    Matrix4x4 rayPos = hmdMatrix * curViewMatrixWithoutHMDI;
                    Vector3 frwdFar = new Vector3(rayPos.M31, rayPos.M32, rayPos.M33) * -1;
                    ray->Origin = rayPos.Translation;
                    ray->Direction = Vector3.Normalize(frwdFar);
                }
            }
            else
                ScreenPointToRayHook!.Original(gameCamera, ray, mousePosX, mousePosY);
            return ray;
        }

        //----
        // ScreenPointToRay1
        //----
        private delegate void ScreenPointToRay1Dg(Ray* ray, float* mousePos);
        [Signature(Signatures.ScreenPointToRay1, DetourName = nameof(ScreenPointToRay1Fn))]
        private Hook<ScreenPointToRay1Dg> ScreenPointToRay1Hook = null;

        [HandleStatus("ScreenPointToRay1")]
        public void ScreenPointToRay1Status(bool status, bool dispose)
        {
            if (dispose)
                ScreenPointToRay1Hook?.Dispose();
            else
            {
                if (status)
                    ScreenPointToRay1Hook?.Enable();
                else
                    ScreenPointToRay1Hook?.Disable();
            }
        }
        private void ScreenPointToRay1Fn(Ray* ray, float* mousePos)
        {
            if (hooksSet && enableVR)
            {
                if (Plugin.cfg!.data.motioncontrol)
                {
                    Matrix4x4 rayPos = handBoneRay * curViewMatrixWithoutHMDI;
                    Vector3 frwdFar = new Vector3(rayPos.M31, rayPos.M32, rayPos.M33) * -1;
                    ray->Origin = rayPos.Translation;
                    ray->Direction = Vector3.Normalize(frwdFar);
                }
                else //if (xivr_Ex.cfg.data.hmdPointing)
                {
                    Matrix4x4 rayPos = hmdMatrix * curViewMatrixWithoutHMDI;
                    Vector3 frwdFar = new Vector3(rayPos.M31, rayPos.M32, rayPos.M33) * -1;
                    ray->Origin = rayPos.Translation;
                    ray->Direction = Vector3.Normalize(frwdFar);
                }
            }
            else
                ScreenPointToRay1Hook!.Original(ray, mousePos);
        }

        //----
        // MousePointScreenToClient
        //----
        private delegate void MousePointScreenToClientDg(UInt64 frameworkInstance, Point* mousePos);
        [Signature(Signatures.MousePointScreenToClient, DetourName = nameof(MousePointScreenToClientFn))]
        private Hook<MousePointScreenToClientDg> MousePointScreenToClientHook = null;

        [HandleStatus("MousePointScreenToClient")]
        public void MousePointScreenToClientStatus(bool status, bool dispose)
        {
            if (dispose)
                MousePointScreenToClientHook?.Dispose();
            else
            {
                if (status)
                    MousePointScreenToClientHook?.Enable();
                else
                    MousePointScreenToClientHook?.Disable();
            }
        }
        private void MousePointScreenToClientFn(UInt64 frameworkInstance, Point* mousePos)
        {
            if (hooksSet && enableVR)
                *mousePos = virtualMouse;
            else
                MousePointScreenToClientHook!.Original(frameworkInstance, mousePos);
        }

        //----
        // DisableCinemaBars
        //----
        private delegate void DisableCinemaBarsDg(UInt64 a1);
        [Signature(Signatures.DisableCinemaBars, DetourName = nameof(DisableCinemaBarsFn))]
        private Hook<DisableCinemaBarsDg> DisableCinemaBarsHook = null;

        [HandleStatus("DisableCinemaBars")]
        public void DisableCinemaBarsStatus(bool status, bool dispose)
        {
            if (dispose)
                DisableCinemaBarsHook?.Dispose();
            else
            {
                if (status)
                    DisableCinemaBarsHook?.Enable();
                else
                    DisableCinemaBarsHook?.Disable();
            }
        }
        private void DisableCinemaBarsFn(UInt64 a1)
        {
            return;
            //DisableCinemaBarsHook!.Original(a1);
        }

        //----
        // CameraUpdateRender
        //----
        private delegate void CameraUpdateRenderDg(RawGameCamera* rawGameCamera);
        [Signature(Signatures.CameraUpdateRender, DetourName = nameof(CameraUpdateRenderFn))]
        private Hook<CameraUpdateRenderDg>? CameraUpdateRenderHook = null;

        [HandleStatus("CameraUpdateRender")]
        public void CameraUpdateRenderStatus(bool status, bool dispose)
        {
            if (dispose)
                CameraUpdateRenderHook?.Dispose();
            else
            {
                if (status)
                    CameraUpdateRenderHook?.Enable();
                else
                    CameraUpdateRenderHook?.Disable();
            }
        }

        private void CameraUpdateRenderFn(RawGameCamera* rawGameCamera)
        {
            if (hooksSet && enableVR)
            {
                if (!inCutscene.Current)
                {
                    if (Plugin.cfg!.data.ultrawideshadows == true)
                        rawGameCamera->CurrentFoV = 2.54f; // ultra wide
                    else
                        rawGameCamera->CurrentFoV = 1.65f;
                    rawGameCamera->MinFoV = rawGameCamera->CurrentFoV;
                    rawGameCamera->MaxFoV = rawGameCamera->CurrentFoV;
                }

                if (inCutscene.Current || gameMode.Current == CameraModes.ThirdPerson || (!Plugin.cfg!.data.immersiveMovement && !isMounted))
                {
                    if (gameMode.Current == CameraModes.ThirdPerson)
                        neckOffsetAvg.AddNew(rawGameCamera->Position.Y);
                }
                else
                {
                    UpdateBoneCamera();
                    Vector3 frontBackDiff = rawGameCamera->LookAt - rawGameCamera->Position;
                    //rawGameCamera->Position = headBoneMatrix[curEye].Translation;
                    rawGameCamera->Position = headBoneMatrix.Translation;
                    rawGameCamera->LookAt = rawGameCamera->Position + frontBackDiff;
                }
                rawGameCamera->ViewMatrix = Matrix4x4.Identity;

                overrideFromParent.Push(true);
                CameraUpdateRenderHook!.Original(rawGameCamera);
                overrideFromParent.Pop();
            }
            else
                CameraUpdateRenderHook!.Original(rawGameCamera);
        }

        //----
        // Camera CalculateViewMatrix
        //----
        private delegate void CalculateViewMatrix2Dg(Matrix4x4* viewMatrix, Vector4 position, Vector4 lookAt, Vector4 d);
        [Signature(Signatures.CalculateViewMatrix2, DetourName = nameof(CalculateViewMatrix2Fn))]
        private Hook<CalculateViewMatrix2Dg>? CalculateViewMatrix2Hook = null;

        [HandleStatus("CalculateViewMatrix2")]
        public void CalculateViewMatrix2Status(bool status, bool dispose)
        {
            if (dispose)
                CalculateViewMatrix2Hook?.Dispose();
            else
            {
                if (status)
                    CalculateViewMatrix2Hook?.Enable();
                else
                    CalculateViewMatrix2Hook?.Disable();
            }
        }

        private void CalculateViewMatrix2Fn(Matrix4x4* viewMatrix, Vector4 position, Vector4 lookAt, Vector4 d)
        {
            if (hooksSet && enableVR && (!frfCalculateViewMatrix || inCutscene.Current))
            {
                CalculateViewMatrix2Hook!.Original(viewMatrix, position, lookAt, d);

                Matrix4x4 horizonLockMatrix = Matrix4x4.Identity;
                frfCalculateViewMatrix = true;

                if (!forceFloatingScreen)
                {
                    IPlayerCharacter player = Plugin.ClientState!.LocalPlayer!;
                    if (player != null)
                    {
                        if ((Plugin.cfg!.data.horizonLock || gameMode.Current == CameraModes.FirstPerson))
                        {
                            RawGameCamera* renderCam = (RawGameCamera*)scCameraManager->CurrentCamera;
                            horizonLockMatrix = Matrix4x4.CreateFromAxisAngle(new Vector3(1, 0, 0), renderCam->CurrentVRotation);
                            lookAt.Y = position.Y;
                        }

                        if (gameMode.Current == CameraModes.FirstPerson)
                            if (!isMounted)
                                horizonLockMatrix = hmdOffsetFirstPerson * horizonLockMatrix;
                            else
                                horizonLockMatrix = hmdOffsetMountedFirstPerson * horizonLockMatrix;
                        else
                            horizonLockMatrix = hmdOffsetThirdPerson * horizonLockMatrix;

                        //horizonLockMatrix.M42 -= camNeckDiffA;//.Average;
                    }

                    if (inCutscene.Current)
                        curViewMatrixWithoutHMD = *viewMatrix;
                    else
                        curViewMatrixWithoutHMD = *viewMatrix * horizonLockMatrix;
                    Matrix4x4.Invert(curViewMatrixWithoutHMD, out curViewMatrixWithoutHMDI);

                    if (Plugin.cfg!.data.swapEyes)
                        *viewMatrix = curViewMatrixWithoutHMD * hmdMatrixI * eyeOffsetMatrix[swapEyes[curEye]];
                    else
                        *viewMatrix = curViewMatrixWithoutHMD * hmdMatrixI * eyeOffsetMatrix[curEye];
                }
                else
                {
                    //curViewMatrixWithoutHMD = *viewMatrix;
                    //Matrix4x4.Invert(curViewMatrixWithoutHMD, out curViewMatrixWithoutHMDI);
                }
            }
            else
            {
                CalculateViewMatrix2Hook!.Original(viewMatrix, position, lookAt, d);
                //curViewMatrixWithoutHMD = *viewMatrix;
                //Matrix4x4.Invert(curViewMatrixWithoutHMD, out curViewMatrixWithoutHMDI);
            }
        }

        //----
        // CameraManager Setup??
        //----
        private delegate void CamManagerSetMatrixDg(sCameraManager* camMngrInstance);
        [Signature(Signatures.CamManagerSetMatrix, DetourName = nameof(CamManagerSetMatrixFn))]
        private Hook<CamManagerSetMatrixDg>? CamManagerSetMatrixHook = null;

        [HandleStatus("CamManagerSetMatrix")]
        public void CamManagerSetMatrixStatus(bool status, bool dispose)
        {
            if (dispose)
                CamManagerSetMatrixHook?.Dispose();
            else
            {
                if (status)
                    CamManagerSetMatrixHook?.Enable();
                else
                    CamManagerSetMatrixHook?.Disable();
            }
        }

        private void CamManagerSetMatrixFn(sCameraManager* camMngrInstance)
        {
            if (hooksSet && enableVR)
            {
                overrideFromParent.Push(true);
                CamManagerSetMatrixHook!.Original(camMngrInstance);
                overrideFromParent.Pop();
            }
            else
                CamManagerSetMatrixHook!.Original(camMngrInstance);
        }

        //----
        // MakeProjectionMatrix2
        //----
        private delegate void MakeProjectionMatrix2Dg(Matrix4x4* projMatrix, float b, float c, float d, float e);
        [Signature(Signatures.MakeProjectionMatrix2, DetourName = nameof(MakeProjectionMatrix2Fn))]
        private Hook<MakeProjectionMatrix2Dg>? MakeProjectionMatrix2Hook = null;

        [HandleStatus("MakeProjectionMatrix2")]
        public void MakeProjectionMatrix2Status(bool status, bool dispose)
        {
            if (dispose)
                MakeProjectionMatrix2Hook?.Dispose();
            else
            {
                if (status)
                    MakeProjectionMatrix2Hook?.Enable();
                else
                    MakeProjectionMatrix2Hook?.Disable();
            }
        }

        private void MakeProjectionMatrix2Fn(Matrix4x4* projMatrix, float b, float c, float d, float e)
        {
            bool overrideMatrix = (overrideFromParent.Count == 0) ? false : overrideFromParent.Peek();
            overrideMatrix |= inCutscene.Current;

            MakeProjectionMatrix2Hook!.Original(projMatrix, b, c, d, e);
            if (overrideMatrix)
                curProjection = *projMatrix;
            if (hooksSet && enableVR && overrideMatrix && !forceFloatingScreen)
            {
                if (Plugin.cfg!.data.swapEyes)
                {
                    gameProjectionMatrix[swapEyes[curEye]].M43 = projMatrix->M43;
                    gameProjectionMatrix[swapEyes[curEye]].M33 = projMatrix->M33;
                    *projMatrix = gameProjectionMatrix[swapEyes[curEye]];
                }
                else
                {
                    gameProjectionMatrix[curEye].M43 = projMatrix->M43;
                    gameProjectionMatrix[curEye].M33 = projMatrix->M33;
                    *projMatrix = gameProjectionMatrix[curEye];
                }
            }
        }

        //----
        // Camera UpdateRotation
        //----
        private delegate void UpdateRotationDg(GameCamera* gameCamera);
        [Signature(Signatures.CameraUpdateRotation, DetourName = nameof(UpdateRotationFn))]
        private Hook<UpdateRotationDg>? UpdateRotationHook = null;

        [HandleStatus("UpdateRotation")]
        public void UpdateRotationStatus(bool status, bool dispose)
        {
            if (dispose)
                UpdateRotationHook?.Dispose();
            else
            {
                if (status)
                    UpdateRotationHook?.Enable();
                else
                    UpdateRotationHook?.Disable();
            }
        }

        private void UpdateRotationFn(GameCamera* gameCamera)
        {
            if (hooksSet && enableVR && !forceFloatingScreen && !inCutscene.Current)
            {
                gameMode.Current = gameCamera->Camera.Mode;

                if (Plugin.cfg!.data.horizontalLock)
                    gameCamera->Camera.HRotationThisFrame2 = 0;
                if (Plugin.cfg!.data.verticalLock)
                    gameCamera->Camera.VRotationThisFrame2 = 0;

                if (gameMode.Current == CameraModes.FirstPerson)
                {
                    gameCamera->Camera.VRotationThisFrame1 = 0.0f;
                    gameCamera->Camera.VRotationThisFrame2 = 0.0f;
                }
                gameCamera->Camera.HRotationThisFrame2 += rotateAmount.X;
                gameCamera->Camera.VRotationThisFrame2 += rotateAmount.Y;

                rotateAmount.X = 0;
                rotateAmount.Y = 0;

                cameraZoom = gameCamera->Camera.CurrentZoom;
            }
            UpdateRotationHook!.Original(gameCamera);
        }

        //----
        // PushbackUI
        //----
        private delegate void PushbackUIDg(UInt64 a, UInt64 b);
        [Signature(Signatures.PushbackUI, DetourName = nameof(PushbackUIFn))]
        private Hook<PushbackUIDg>? PushbackUIHook = null;

        [HandleStatus("PushbackUI")]
        public void PushbackUIStatus(bool status, bool dispose)
        {
            if (dispose)
                PushbackUIHook?.Dispose();
            else
            {
                if (status)
                    PushbackUIHook?.Enable();
                else
                    PushbackUIHook?.Disable();
            }
        }

        private void PushbackUIFn(UInt64 a, UInt64 b)
        {
            if (hooksSet && enableVR)
            {
                Texture* texture = Imports.GetUIRenderTexture(curEye);
                UInt64 threadedOffset = GetThreadedOffset();
                SetRenderTargetFn!(threadedOffset, 1, &texture, null, 0, 0, 0, 0);
                AddcmdClear();

                overrideFromParent.Push(true);
                PushbackUIHook!.Original(a, b);
                overrideFromParent.Pop();
            }
            else
                PushbackUIHook!.Original(a, b);
        }


        //----
        // DXGIPresent
        //----
        private delegate void DXGIPresentDg(UInt64 a, UInt64 b);
        [Signature(Signatures.DXGIPresent, DetourName = nameof(DXGIPresentFn))]
        private Hook<DXGIPresentDg>? DXGIPresentHook = null;

        [HandleStatus("DXGIPresent")]
        public void DXGIPresentStatus(bool status, bool dispose)
        {
            if (dispose)
                DXGIPresentHook?.Dispose();
            else
            {
                if (status)
                    DXGIPresentHook?.Enable();
                else
                    DXGIPresentHook?.Disable();
            }
        }

        int threadedEye = 0;
        private unsafe void DXGIPresentFn(UInt64 a, UInt64 b)
        {
            if (hooksSet && enableVR)
            {
                if (threadedEye == 1 && dx11DeviceInstance != null)
                {
                    Imports.RenderUI(dx11DeviceInstance->SwapChain->BackBuffer);
                    DXGIPresentHook!.Original(a, b);
                    Imports.RenderVR(curProjection, curViewMatrixWithoutHMD, handBoneRay, handWatch, virtualMouse, uiAngleOffset, dalamudMode, forceFloatingScreen);
                }
            }
            else
                DXGIPresentHook!.Original(a, b);
        }

        //----
        // RenderThreadSetRenderTarget
        //----
        private delegate void RenderThreadSetRenderTargetDg(Device* deviceInstance, cmdSetRendTarget* command);
        [Signature(Signatures.RenderThreadSetRenderTarget, DetourName = nameof(RenderThreadSetRenderTargetFn))]
        private Hook<RenderThreadSetRenderTargetDg>? RenderThreadSetRenderTargetHook = null;

        [HandleStatus("RenderThreadSetRenderTarget")]
        public void RenderThreadSetRenderTargetStatus(bool status, bool dispose)
        {
            if (dispose)
                RenderThreadSetRenderTargetHook?.Dispose();
            else
            {
                if (status)
                    RenderThreadSetRenderTargetHook?.Enable();
                else
                    RenderThreadSetRenderTargetHook?.Disable();
            }
        }
        
        private void RenderThreadSetRenderTargetFn(Device* deviceInstance, cmdSetRendTarget* command)
        {
            if (hooksSet && enableVR && command->numRenderTargets >= 100)
            {
                threadedEye = command->numRenderTargets - 100;
                Imports.SetThreadedEye(threadedEye);
                command->numRenderTargets = 0;
            }
            else
                RenderThreadSetRenderTargetHook!.Original(deviceInstance, command);
        }

        //----
        // RunGameTasks
        //----
        delegate void RunGameTasksDg(TaskManager* taskManager, float* frameTiming);
        [Signature(Signatures.RunGameTasks, DetourName = nameof(RunGameTasksFn))]
        private Hook<RunGameTasksDg>? RunGameTasksHook = null;

        [HandleStatus("RunGameTasks")]
        public void RunGameTasksStatus(bool status, bool dispose)
        {
            if (dispose)
                RunGameTasksHook?.Dispose();
            else
            {
                if (status)
                    RunGameTasksHook?.Enable();
                else
                    RunGameTasksHook?.Disable();
            }
        }
        
        public void RunGameTasksFn(TaskManager* taskManager, float* frameTiming)
        {
            //*frameTiming = 0;
            //Log!.Info($"RunGameTasksFn Start {curEye} | {a:x} {*frameTiming}");
            //RunUpdate();
            RawGameCamera* renderCam = (RawGameCamera*)scCameraManager->CurrentCamera;
            if (hooksSet && enableVR && renderCam != null)
            {
                RunUpdate();
                for (int i = 0; i < taskManager->TaskCount; i++)
                {
                    if (i == 18)
                        CheckVisibility();

                    /*if (i == 23 && gameMode.Current == CameraModes.FirstPerson)
                    {
                        bool orgFRF = frfCalculateViewMatrix;
                        frfCalculateViewMatrix = false;
                        Task* task1 = (Task*)((UInt64)(18 * 0x78) + *(UInt64*)(a + 0x58));
                        task1->Execute(frameTiming);
                        frfCalculateViewMatrix = orgFRF;
                    }*/

                    //----
                    // Copies game render/depth targets
                    // before the ui is rendered on them
                    //----
                    if(i == taskManager->TaskCount - 1)
                        AddcmdSetRendTargetFake(100 + curEye);

                    Task* task = (Task*)((UInt64)(i * 0x78) + (UInt64)(taskManager->TaskList));
                    task->Execute(frameTiming);
                }
            }
            else
                RunGameTasksHook!.Original(taskManager, frameTiming);
        }


        //----
        // FrameworkTick
        //----
        private delegate UInt64 FrameworkTickDg(Framework* FrameworkInstance);
        [Signature(Signatures.FrameworkTick, DetourName = nameof(FrameworkTickFn))]
        private Hook<FrameworkTickDg>? FrameworkTickHook = null;

        [HandleStatus("FrameworkTick")]
        public void FrameworkTickStatus(bool status, bool dispose)
        {
            if (dispose)
                FrameworkTickHook?.Dispose();
            else
            {
                if (status)
                    FrameworkTickHook?.Enable();
                else
                    FrameworkTickHook?.Disable();
            }
        }

        public UInt64 FrameworkTickFn(Framework* FrameworkInstance)
        {
            if (hooksSet && enableVR)
            {
                //*(float*)(a + 0x16B8) = 0;
                //Log!.Info($"{(UInt64)FrameworkInstance:x} {((UInt64)FrameworkInstance + 0x16C0):x} {*(float*)(FrameworkInstance + 0x16C0)}");
                SetVRMatrixSet();
                GetMultiplayerIKData();
                //ShowBoneLayout();
                UInt64 retVal = 0;
                curEye = 0;
                if (enableVR) retVal = FrameworkTickHook!.Original(FrameworkInstance);
                curEye = 1;
                if (enableVR) retVal = FrameworkTickHook!.Original(FrameworkInstance);
                //ShowBoneLayout();
                return retVal;
            }
            else
                return FrameworkTickHook!.Original(FrameworkInstance);
        }







        //----
        // ChangeEquipment
        //----
        private delegate void ChangeEquipmentDg(UInt64 address, CharEquipSlots index, CharEquipSlotData* item);
        [Signature(Signatures.ChangeEquipment, DetourName = nameof(ChangeEquipmentFn))]
        private Hook<ChangeEquipmentDg>? ChangeEquipmentHook = null;

        [HandleStatus("ChangeEquipment")]
        public void ChangeEquipmentStatus(bool status, bool dispose)
        {
            if (dispose)
                ChangeEquipmentHook?.Dispose();
            else
            {
                if (status)
                    ChangeEquipmentHook?.Enable();
                else
                    ChangeEquipmentHook?.Disable();
            }
        }

        private void ChangeEquipmentFn(UInt64 address, CharEquipSlots index, CharEquipSlotData* item)
        {
            if (hooksSet && enableVR)
            {
                IPlayerCharacter player = Plugin.ClientState!.LocalPlayer!;
                if (player != null)
                {
                    Character* bonedCharacter = (Character*)player.Address;
                    if (bonedCharacter != null)
                    {
                        UInt64 equipOffset = (UInt64)(UInt64*)&bonedCharacter->DrawData;
                        if (equipOffset == address)
                        {
                            haveSavedEquipmentSet = true;
                            currentEquipmentSet.Data[(int)index] = item->Data;
                        }
                    }
                }
            }
            //Plugin.Log!.Info($"ChangeEquipmentFn {address:X} {index} {item->Id}, {item->Variant}, {item->Dye}");
            ChangeEquipmentHook!.Original(address, index, item);
        }
















        public static Vector3 GetAngles(Matrix4x4 source)
        {
            float thetaX, thetaY, thetaZ = 0.0f;
            thetaX = MathF.Asin(source.M32);

            if (thetaX < (Math.PI / 2))
            {
                if (thetaX > (-Math.PI / 2))
                {
                    thetaZ = MathF.Atan2(-source.M12, source.M22);
                    thetaY = MathF.Atan2(-source.M31, source.M33);
                }
                else
                {
                    thetaZ = -MathF.Atan2(-source.M13, source.M11);
                    thetaY = 0;
                }
            }
            else
            {
                thetaZ = MathF.Atan2(source.M13, source.M11);
                thetaY = 0;
            }
            Vector3 angles = new Vector3(thetaX, thetaY, thetaZ);
            return angles;
        }

        public static Vector3 GetAngles(Quaternion q)
        {
            float pitch, yaw, roll = 0.0f;
            float sqw = q.W * q.W;
            float sqx = q.X * q.X;
            float sqy = q.Y * q.Y;
            float sqz = q.Z * q.Z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = q.X * q.W - q.Y * q.Z;

            if (test > 0.49975f * unit)
            {   // singularity at north pole
                yaw = 2f * MathF.Atan2(q.Y, q.X);
                pitch = MathF.PI / 2f;
                roll = 0;
                return new Vector3(pitch, yaw, roll);
            }
            if (test < -0.49975f * unit)
            {   // singularity at south pole
                yaw = -2f * MathF.Atan2(q.Y, q.X);
                pitch = -MathF.PI / 2f;
                roll = 0;
                return new Vector3(pitch, yaw, roll);
            }

            Quaternion q1 = new Quaternion(q.W, q.Z, q.X, q.Y);
            yaw = 1 * MathF.Atan2(2f * (q1.X * q1.W + q1.Y * q1.Z), 1f - 2f * (q1.Z * q1.Z + q1.W * q1.W));   // Yaw
            pitch = 1 * MathF.Asin(2f * (q1.X * q1.Z - q1.W * q1.Y));                                         // Pitch
            roll = 1 * MathF.Atan2(2f * (q1.X * q1.Y + q1.Z * q1.W), 1f - 2f * (q1.Y * q1.Y + q1.Z * q1.Z));  // Roll
            return new Vector3(pitch, yaw, roll);
        }

        float AngleFromToVector(Vector3 from, Vector3 to)
        {
            float kEpsilonNormalSqrt = 1e-15F;
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            float denominator = (float)Math.Sqrt(from.LengthSquared() * to.LengthSquared());
            if (denominator < kEpsilonNormalSqrt)
                return 0F;

            float rawDot = Vector3.Dot(from, to);
            float dot = Math.Clamp(rawDot / denominator, -1f, 1f);
            float angle = ((float)Math.Acos(dot)) * Rad2Deg;
            float atanA = MathF.Atan2(from.X, from.Z);
            float atanb = MathF.Atan2(to.X, to.Z);

            Plugin.Log!.Info($"{atanb * Rad2Deg} {rawDot} {dot}");
            return angle * ((rawDot < 0) ? -1 : 1);
        }




        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, int cButtons, int dwExtraInfo);

        const int MOUSEEVENTF_LEFTDOWN = 0x02;
        const int MOUSEEVENTF_LEFTUP = 0x04;
        const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        const int MOUSEEVENTF_RIGHTUP = 0x10;
        const int MOUSEEVENTF_WHEEL = 0x0800;

        const int KEYEVENTF_KEYDOWN = 0x0000;
        const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        const int KEYEVENTF_KEYUP = 0x0002;

        const int VK_SHIFT = 0xA0;
        const int VK_ALT = 0xA4;
        const int VK_CONTROL = 0xA2;
        const int VK_ESCAPE = 0x1B;

        const int VK_F1 = 0x70;
        const int VK_F2 = 0x71;
        const int VK_F3 = 0x72;
        const int VK_F4 = 0x73;
        const int VK_F5 = 0x74;
        const int VK_F6 = 0x75;
        const int VK_F7 = 0x76;
        const int VK_F8 = 0x77;
        const int VK_F9 = 0x78;
        const int VK_F10 = 0x79;
        const int VK_F11 = 0x7A;
        const int VK_F12 = 0x7B;

        public XBoxStatus xboxStatus = new XBoxStatus();
        bool rightHorizontalCenter = false;
        bool rightVerticalCenter = false;
        bool leftStickAltMode = false;
        bool rightStickAltMode = false;

        //----
        // Movement
        //----
        [HandleInputAttribute(ActionButtonLayout.movement)]
        public void inputMovement(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            xboxStatus.left_stick_left.Set();
            xboxStatus.left_stick_right.Set();
            xboxStatus.left_stick_up.Set();
            xboxStatus.left_stick_down.Set();

            float deadzone = 0.5f;
            if (leftStickAltMode)
            {
                InputAnalogActionData analogRedirect = new InputAnalogActionData();
                InputDigitalActionData digitalRedirect = new InputDigitalActionData();

                if (analog.x > deadzone)
                {
                    digitalRedirect.bState = true;
                    inputXBoxPadRight(analogRedirect, digitalRedirect);
                }
                else if (analog.x < deadzone && analog.x >= 0)
                {
                    digitalRedirect.bState = false;
                    inputXBoxPadRight(analogRedirect, digitalRedirect);
                }

                if (analog.x < -deadzone)
                {
                    digitalRedirect.bState = true;
                    inputXBoxPadLeft(analogRedirect, digitalRedirect);
                }
                else if (analog.x > -deadzone && analog.x <= 0)
                {
                    digitalRedirect.bState = false;
                    inputXBoxPadLeft(analogRedirect, digitalRedirect);
                }

                if (analog.y > deadzone)
                {
                    digitalRedirect.bState = true;
                    inputXBoxPadUp(analogRedirect, digitalRedirect);
                }
                else if (analog.y < deadzone && analog.y >= 0)
                {
                    digitalRedirect.bState = false;
                    inputXBoxPadUp(analogRedirect, digitalRedirect);
                }

                if (analog.y < -deadzone)
                {
                    digitalRedirect.bState = true;
                    inputXBoxPadDown(analogRedirect, digitalRedirect);
                }
                else if (analog.y > -deadzone && analog.y <= 0)
                {
                    digitalRedirect.bState = false;
                    inputXBoxPadDown(analogRedirect, digitalRedirect);
                }
            }
            else
            {
                if (analog.x < 0)
                    xboxStatus.left_stick_left.Set(true, MathF.Abs(analog.x));
                else if (analog.x > 0)
                    xboxStatus.left_stick_right.Set(true, MathF.Abs(analog.x));

                if (analog.y > 0)
                    xboxStatus.left_stick_up.Set(true, MathF.Abs(analog.y));
                else if (analog.y < 0)
                    xboxStatus.left_stick_down.Set(true, MathF.Abs(analog.y));
            }
        }

        [HandleInputAttribute(ActionButtonLayout.rotation)]
        public void inputRotation(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            xboxStatus.right_stick_left.Set();
            xboxStatus.right_stick_right.Set();
            xboxStatus.right_stick_up.Set();
            xboxStatus.right_stick_down.Set();

            if (analog.x < 0)
                xboxStatus.right_stick_left.Set(true, MathF.Abs(analog.x));
            else if (analog.x > 0)
                xboxStatus.right_stick_right.Set(true, MathF.Abs(analog.x));

            if (rightStickAltMode)
            {
                if (analog.y > 0.75f)
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, 90, 0);
                else if (analog.y > 0.25f)
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, 30, 0);
                else if (analog.y < -0.75f)
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, -90, 0);
                else if (analog.y < -0.25f)
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, -30, 0);
            }
            else
            {
                if (analog.y > 0)
                    xboxStatus.right_stick_up.Set(true, MathF.Abs(analog.y));
                else if (analog.y < 0)
                    xboxStatus.right_stick_down.Set(true, MathF.Abs(analog.y));
            }
        }

        //----
        // Mouse
        //----

        [HandleInputAttribute(ActionButtonLayout.leftClick)]
        public void inputLeftClick(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            Plugin.Log!.Info($" Left Clicking ");
            if (digital.bState == true && inputState[ActionButtonLayout.leftClick] == false)
            {
                inputState[ActionButtonLayout.leftClick] = true;
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.leftClick] == true)
            {
                inputState[ActionButtonLayout.leftClick] = false;
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.rightClick)]
        public void inputRightClick(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.rightClick] == false)
            {
                inputState[ActionButtonLayout.rightClick] = true;
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.rightClick] == true)
            {
                inputState[ActionButtonLayout.rightClick] = false;
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            }
        }


        //----
        // Keys
        //----

        [HandleInputAttribute(ActionButtonLayout.recenter)]
        public void inputRecenter(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.recenter] == false)
            {
                inputState[ActionButtonLayout.recenter] = true;
                Imports.Recenter();
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.recenter] == true)
            {
                inputState[ActionButtonLayout.recenter] = false;
            }
        }

        [HandleInputAttribute(ActionButtonLayout.shift)]
        public void inputShift(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.shift] == false)
            {
                inputState[ActionButtonLayout.shift] = true;
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.shift] == true)
            {
                inputState[ActionButtonLayout.shift] = false;
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.alt)]
        public void inputAlt(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.alt] == false)
            {
                inputState[ActionButtonLayout.alt] = true;
                keybd_event(VK_ALT, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.alt] == true)
            {
                inputState[ActionButtonLayout.alt] = false;
                keybd_event(VK_ALT, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.control)]
        public void inputControl(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.control] == false)
            {
                inputState[ActionButtonLayout.control] = true;
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.control] == true)
            {
                inputState[ActionButtonLayout.control] = false;
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.escape)]
        public void inputEscape(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.escape] == false)
            {
                inputState[ActionButtonLayout.escape] = true;
                keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.escape] == true)
            {
                inputState[ActionButtonLayout.escape] = false;
                keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        //----
        // F Keys
        //----

        [HandleInputAttribute(ActionButtonLayout.button01)]
        public void inputButton01(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button01] == false)
            {
                inputState[ActionButtonLayout.button01] = true;
                keybd_event(VK_F1, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button01] == true)
            {
                inputState[ActionButtonLayout.button01] = false;
                keybd_event(VK_F1, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button02)]
        public void inputButton02(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button02] == false)
            {
                inputState[ActionButtonLayout.button02] = true;
                keybd_event(VK_F2, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button02] == true)
            {
                inputState[ActionButtonLayout.button02] = false;
                keybd_event(VK_F2, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button03)]
        public void inputButton03(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button03] == false)
            {
                inputState[ActionButtonLayout.button03] = true;
                keybd_event(VK_F3, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button03] == true)
            {
                inputState[ActionButtonLayout.button03] = false;
                keybd_event(VK_F3, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button04)]
        public void inputButton04(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button04] == false)
            {
                inputState[ActionButtonLayout.button04] = true;
                keybd_event(VK_F4, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button04] == true)
            {
                inputState[ActionButtonLayout.button04] = false;
                keybd_event(VK_F4, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button05)]
        public void inputButton05(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button05] == false)
            {
                inputState[ActionButtonLayout.button05] = true;
                keybd_event(VK_F5, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button05] == true)
            {
                inputState[ActionButtonLayout.button05] = false;
                keybd_event(VK_F5, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button06)]
        public void inputButton06(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button06] == false)
            {
                inputState[ActionButtonLayout.button06] = true;
                keybd_event(VK_F6, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button06] == true)
            {
                inputState[ActionButtonLayout.button06] = false;
                keybd_event(VK_F6, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button07)]
        public void inputButton07(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button07] == false)
            {
                inputState[ActionButtonLayout.button07] = true;
                keybd_event(VK_F7, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button07] == true)
            {
                inputState[ActionButtonLayout.button07] = false;
                keybd_event(VK_F7, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button08)]
        public void inputButton08(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button08] == false)
            {
                inputState[ActionButtonLayout.button08] = true;
                keybd_event(VK_F8, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button08] == true)
            {
                inputState[ActionButtonLayout.button08] = false;
                keybd_event(VK_F8, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button09)]
        public void inputButton09(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button09] == false)
            {
                inputState[ActionButtonLayout.button09] = true;
                keybd_event(VK_F9, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button09] == true)
            {
                inputState[ActionButtonLayout.button09] = false;
                keybd_event(VK_F9, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button10)]
        public void inputButton10(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button10] == false)
            {
                inputState[ActionButtonLayout.button10] = true;
                keybd_event(VK_F10, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button10] == true)
            {
                inputState[ActionButtonLayout.button10] = false;
                keybd_event(VK_F10, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button11)]
        public void inputButton11(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button11] == false)
            {
                inputState[ActionButtonLayout.button11] = true;
                keybd_event(VK_F11, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button11] == true)
            {
                inputState[ActionButtonLayout.button11] = false;
                keybd_event(VK_F11, 0, KEYEVENTF_KEYUP, 0);
            }
        }

        [HandleInputAttribute(ActionButtonLayout.button12)]
        public void inputButton12(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bState == true && inputState[ActionButtonLayout.button12] == false)
            {
                inputState[ActionButtonLayout.button12] = true;
                keybd_event(VK_F12, 0, KEYEVENTF_KEYDOWN, 0);
            }
            else if (digital.bState == false && inputState[ActionButtonLayout.button12] == true)
            {
                inputState[ActionButtonLayout.button12] = false;
                keybd_event(VK_F12, 0, KEYEVENTF_KEYUP, 0);
            }
        }


        //----
        // XBox Buttons
        //----
        bool showKeyboard = false;
        [HandleInputAttribute(ActionButtonLayout.xbox_button_y)]
        public void inputXBoxButtonY(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            if (rightStickAltMode)
                inputButton11(analog, digital);
            else
                xboxStatus.button_y.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_button_x)]
        public void inputXBoxButtonX(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.button_x.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_button_a)]
        public void inputXBoxButtonA(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.button_a.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_button_b)]
        public void inputXBoxButtonB(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            if (rightStickAltMode)
                inputButton12(analog, digital);
            else
                xboxStatus.button_b.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_left_trigger)]
        public void inputXBoxLeftTrigger(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            bool status = (analog.x > 0) ? true : false;
            xboxStatus.left_trigger.Set(status, analog.x);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_left_bumper)]
        public void inputXBoxLeftBumper(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            bool status = (analog.x > 0) ? true : false;
            xboxStatus.left_bumper.Set(status, analog.x);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_left_stick_click)]
        public void inputXBoxLeftStickClick(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.left_stick_click.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_right_trigger)]
        public void inputXBoxRightTrigger(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            bool status = (analog.x > 0) ? true : false;
            xboxStatus.right_trigger.Set(status, analog.x);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_right_bumper)]
        public void inputXBoxRightBumper(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            bool status = (analog.x > 0) ? true : false;
            xboxStatus.right_bumper.Set(status, analog.x);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_right_stick_click)]
        public void inputXBoxRightStickClick(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.right_stick_click.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_pad_up)]
        public void inputXBoxPadUp(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.dpad_up.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_pad_down)]
        public void inputXBoxPadDown(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.dpad_down.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_pad_left)]
        public void inputXBoxPadLeft(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.dpad_left.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_pad_right)]
        public void inputXBoxPadRight(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.dpad_right.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_start)]
        public void inputXBoxStart(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.start.Set(digital.bState, value);
        }

        [HandleInputAttribute(ActionButtonLayout.xbox_select)]
        public void inputXBoxSelect(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            float value = (digital.bState == true) ? 1.0f : 0.0f;
            xboxStatus.select.Set(digital.bState, value);
        }


        [HandleInputAttribute(ActionButtonLayout.thumbrest_left)]
        public void inputThumbrestLeft(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bChanged)
                rightStickAltMode = digital.bState;
        }

        [HandleInputAttribute(ActionButtonLayout.thumbrest_right)]
        public void inputThumbrestRight(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bChanged)
                leftStickAltMode = digital.bState;
        }

        //----
        // Watch Buttons
        //----

        [HandleInputAttribute(ActionButtonLayout.watch_audio)]
        public void inputWatchAudio(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bActive)
            {
                Plugin.CommandManager!.ProcessCommand("/bgm");
                Plugin.CommandManager!.ProcessCommand("/soundeffects");
                Plugin.CommandManager!.ProcessCommand("/voice");
                Plugin.CommandManager!.ProcessCommand("/ambientsounds");
                Plugin.CommandManager!.ProcessCommand("/performsounds");
            }
        }

        [HandleInputAttribute(ActionButtonLayout.watch_dalamud)]
        public void inputWatchDalamud(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bActive)
                Plugin.CommandManager!.ProcessCommand("/xlplugins");
        }

        [HandleInputAttribute(ActionButtonLayout.watch_ui)]
        public void inputWatchUI(InputAnalogActionData analog, InputDigitalActionData digital)
        {
        }

        [HandleInputAttribute(ActionButtonLayout.watch_keyboard)]
        public void inputWatchKeyboard(InputAnalogActionData analog, InputDigitalActionData digital)
        {
        }

        [HandleInputAttribute(ActionButtonLayout.watch_none)]
        public void inputWatchNone(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bActive)
                Plugin.CommandManager!.ProcessCommand("/chillframes toggle");
        }

        [HandleInputAttribute(ActionButtonLayout.watch_occlusion)]
        public void inputWatchOcclusion(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bActive)
                Plugin.CommandManager!.ProcessCommand("/xivr uidepth");
        }

        [HandleInputAttribute(ActionButtonLayout.watch_recenter)]
        public void inputWatchRecenter(InputAnalogActionData analog, InputDigitalActionData digital)
        {
        }

        [HandleInputAttribute(ActionButtonLayout.watch_weapon)]
        public void inputWatchWeapon(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bActive)
                Plugin.CommandManager!.ProcessCommand("/xivr weapon");
        }

        [HandleInputAttribute(ActionButtonLayout.watch_xivr)]
        public void inputWatchXIVR(InputAnalogActionData analog, InputDigitalActionData digital)
        {
            if (digital.bActive)
                Plugin.CommandManager!.ProcessCommand("/xivr");
        }


















        private Character* GetCharacterOrMouseover(byte charFrom = 3)
        {
            IPlayerCharacter player = Plugin.ClientState!.LocalPlayer!;
            UInt64 selectMouseOver = *(UInt64*)selectScreenMouseOver;

            if (player == null && selectMouseOver == 0)
                return null;

            if (selectMouseOver != 0 && (charFrom & 1) == 1)
                return (Character*)selectMouseOver;
            else if (player != null && (charFrom & 2) == 2)
                return (Character*)player!.Address;
            else
                return null;
        }

        private void CheckVisibilityInner(Character* character)
        {
            if (character == null)
                return;

            if ((ObjectKind)character->GameObject.ObjectKind == ObjectKind.Pc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.BattleNpc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.EventNpc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Mount ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Companion ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Retainer)
            {
                Structures.Model* model = (Structures.Model*)character->GameObject.DrawObject;
                if (model == null)
                    return;

                if (model->CullType == ModelCullTypes.InsideCamera && ((byte)character->GameObject.TargetableStatus & 2) == 2)
                    model->CullType = ModelCullTypes.Visible;

                DrawDataContainer* drawData = &character->DrawData;
                if (drawData != null && !drawData->IsWeaponHidden)
                {
                    Structures.Model* mhWeap = (Structures.Model*)drawData->Weapon(DrawDataContainer.WeaponSlot.MainHand).DrawObject;
                    if (mhWeap != null)
                        mhWeap->CullType = ModelCullTypes.Visible;

                    Structures.Model* ohWeap = (Structures.Model*)drawData->Weapon(DrawDataContainer.WeaponSlot.OffHand).DrawObject;
                    if (ohWeap != null)
                        ohWeap->CullType = ModelCullTypes.Visible;

                    Structures.Model* fWeap = (Structures.Model*)drawData->Weapon(DrawDataContainer.WeaponSlot.Unk).DrawObject;
                    if (fWeap != null)
                        fWeap->CullType = ModelCullTypes.Visible;
                }

                Structures.Model* mount = (Structures.Model*)model->mountedObject;
                if (mount != null)
                    mount->CullType = ModelCullTypes.Visible;

                OrnamentContainer* oCont = &character->OrnamentData;
                if (oCont != null)
                {
                    GameObject* bonedOrnament = (GameObject*)oCont->OrnamentObject;
                    if (bonedOrnament != null)
                    {
                        Structures.Model* ornament = (Structures.Model*)bonedOrnament->DrawObject;
                        if (ornament != null)
                            ornament->CullType = ModelCullTypes.Visible;
                    }
                }
            }
        }
        private void CheckVisibility()
        {
            if (inCutscene.Current)
                return;

            //----
            // Check the player
            //----
            Character* character = GetCharacterOrMouseover(2);
            if (character != null && character != targetSystem->ObjectFilterArray0[0])
                CheckVisibilityInner(character);

            for (int i = 0; i < Plugin.PartyList!.Length; i++)
            {
                Dalamud.Game.ClientState.Objects.Types.IGameObject partyMember = Plugin.PartyList[i]!.GameObject!;
                if (partyMember != null)
                {
                    Character* partyCharacter = (Character*)partyMember.Address;
                    if (character != null)
                        CheckVisibilityInner(partyCharacter);
                }
            }

            //----
            // Check anyone in sight
            //----
            for (int i = 0; i < targetSystem->ObjectFilterArray1.Length; i++)
                CheckVisibilityInner((Character*)targetSystem->ObjectFilterArray1[i]);
        }

        private void GetMultiplayerIKDataInner(bool isPlayer, Character* character, Matrix4x4 hmd, Matrix4x4 lhc, Matrix4x4 rhc)
        {
            if (character == null)
                return;

            if ((ObjectKind)character->GameObject.ObjectKind != ObjectKind.Pc)
                return;

            Structures.Model* model = (Structures.Model*)character->GameObject.DrawObject;
            if (model == null)
                return;

            Skeleton* skeleton = model->skeleton;
            if (skeleton == null)
                return;

            SkeletonResourceHandle* srh = skeleton->SkeletonResourceHandles[0];
            if (srh != null)
            {
                hkaSkeleton* hkaSkel = srh->HavokSkeleton;
                if (hkaSkel != null)
                    if (!commonBones.ContainsKey((UInt64)hkaSkel))
                    {
                        commonBones.Add((UInt64)hkaSkel, new stCommonSkelBoneList(skeleton));
                        Plugin.Log!.Info($"commonBoneCount {commonBones.Count} {commonBones[(UInt64)hkaSkel].armLength}");
                    }
            }

            float armMultiplier = 100.0f;
            if (gameMode.Current == CameraModes.FirstPerson)
                armMultiplier = Plugin.cfg!.data.armMultiplier;
            bool motionControls = Plugin.cfg!.data.motioncontrol;

            multiIK[0].Enqueue(new stMultiIK(
                character->CurrentWorld,
                character->GameObject.ObjectIndex,
                character,
                skeleton,
                isPlayer,
                hmd,
                lhc,
                rhc,
                motionControls,
                armMultiplier,
                avgHCRotation
                ));
            multiIK[1].Enqueue(new stMultiIK(
                character->CurrentWorld,
                character->GameObject.ObjectIndex,
                character,
                skeleton,
                isPlayer,
                hmd,
                lhc,
                rhc,
                motionControls,
                armMultiplier,
                avgHCRotation
                ));
        }
        private void GetMultiplayerIKData()
        {
            if (inCutscene.Current || gameMode.Current == CameraModes.ThirdPerson)
               return;

            Character* character = GetCharacterOrMouseover(2);
            GetMultiplayerIKDataInner(true, character, hmdMatrix, lhcMatrix, rhcMatrix);
            //if (character != null && character != targetSystem->ObjectFilterArray1[0])
            //    GetMultiplayerIKDataInner(true, character, hmdMatrix, lhcMatrix, rhcMatrix);
            //else
            //    for (int i = 0; i < 1; i++)
            //        GetMultiplayerIKDataInner((Character*)targetSystem->ObjectFilterArray0[i], hmdMatrix, lhcMatrix, rhcMatrix);
            //targetSystem->ObjectFilterArray0.Length
        }

        private unsafe void ShowBoneLayoutInner(Character* character)
        {
            if (character == null)
                return;

            if ((ObjectKind)character->GameObject.ObjectKind == ObjectKind.Pc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.BattleNpc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.EventNpc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Mount ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Companion ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Retainer)
            {
                Structures.Model* model = (Structures.Model*)character->GameObject.DrawObject;
                if (model == null)
                    return;

                BoneOutput.DrawBones(model->skeleton);
            }
        }
        private unsafe void ShowBoneLayout()
        {
            //----
            // Draws Skeletal overlay for all models
            // to get the full bone list
            //----
            //Character* character = GetCharacterOrMouseover();
            //if (character != null && character != targetSystem->ObjectFilterArray1[0])
            //    ShowBoneLayoutInner(character);

            for (int i = 0; i < targetSystem->ObjectFilterArray1.Length; i++)
                ShowBoneLayoutInner((Character*)targetSystem->ObjectFilterArray1[i]);
        }

        private void ShowHandBoneLayout(poseType tPose, Matrix4x4 controller, float heightOffset = 1.0f)
        {
            fingerHandLayout hand = Imports.GetSkeletalPose(tPose);
            Bone[] handArray = new Bone[31];

            handArray[0] = new Bone((BoneList)BoneListEn.e_root, 0, 0, null, new hkQsTransformf(), new hkQsTransformf());
            handArray[0].boneMatrix = hand.root.Convert().ToMatrix() * controller;// * Matrix4x4.CreateScale(-1, 1, -1);
            handArray[1] = new Bone((BoneList)BoneListEn.e_wrist_l, 1, handArray[0].id, handArray[0], new hkQsTransformf(), new hkQsTransformf());
            handArray[1].boneMatrix = hand.wrist.Convert().ToMatrix() * controller;
            handArray[2] = new Bone((BoneList)BoneListEn.e_thumb_a_l, 2, handArray[1].id, handArray[1], new hkQsTransformf(), new hkQsTransformf());
            handArray[2].boneMatrix = hand.thumb0Metacarpal.Convert().ToMatrix() * controller;
            handArray[3] = new Bone((BoneList)BoneListEn.e_thumb_a_l, 3, handArray[2].id, handArray[2], new hkQsTransformf(), new hkQsTransformf());
            handArray[3].boneMatrix = hand.thumb1Proximal.Convert().ToMatrix() * controller;
            handArray[4] = new Bone((BoneList)BoneListEn.e_thumb_a_l, 4, handArray[2].id, handArray[2], new hkQsTransformf(), new hkQsTransformf());
            handArray[4].boneMatrix = hand.thumb2Middle.Convert().ToMatrix() * controller;
            handArray[5] = new Bone((BoneList)BoneListEn.e_thumb_a_l, 5, handArray[3].id, handArray[3], new hkQsTransformf(), new hkQsTransformf());
            handArray[5].boneMatrix = hand.thumb3Distal.Convert().ToMatrix() * controller;
            handArray[6] = new Bone((BoneList)BoneListEn.e_finger_index_a_l, 6, handArray[1].id, handArray[1], new hkQsTransformf(), new hkQsTransformf());
            handArray[6].boneMatrix = hand.index0Metacarpal.Convert().ToMatrix() * controller;
            handArray[7] = new Bone((BoneList)BoneListEn.e_finger_index_a_l, 7, handArray[6].id, handArray[6], new hkQsTransformf(), new hkQsTransformf());
            handArray[7].boneMatrix = hand.index1Proximal.Convert().ToMatrix() * controller;
            handArray[8] = new Bone((BoneList)BoneListEn.e_finger_index_a_l, 8, handArray[7].id, handArray[7], new hkQsTransformf(), new hkQsTransformf());
            handArray[8].boneMatrix = hand.index2Middle.Convert().ToMatrix() * controller;
            handArray[9] = new Bone((BoneList)BoneListEn.e_finger_index_a_l, 9, handArray[8].id, handArray[8], new hkQsTransformf(), new hkQsTransformf());
            handArray[9].boneMatrix = hand.index3Distal.Convert().ToMatrix() * controller;
            handArray[10] = new Bone((BoneList)BoneListEn.e_finger_middle_a_l, 10, handArray[1].id, handArray[1], new hkQsTransformf(), new hkQsTransformf());
            handArray[10].boneMatrix = hand.middle0Metacarpal.Convert().ToMatrix() * controller;
            handArray[11] = new Bone((BoneList)BoneListEn.e_finger_middle_a_l, 11, handArray[10].id, handArray[10], new hkQsTransformf(), new hkQsTransformf());
            handArray[11].boneMatrix = hand.middle1Proximal.Convert().ToMatrix() * controller;
            handArray[12] = new Bone((BoneList)BoneListEn.e_finger_middle_a_l, 12, handArray[11].id, handArray[11], new hkQsTransformf(), new hkQsTransformf());
            handArray[12].boneMatrix = hand.middle2Middle.Convert().ToMatrix() * controller;
            handArray[13] = new Bone((BoneList)BoneListEn.e_finger_middle_a_l, 13, handArray[12].id, handArray[12], new hkQsTransformf(), new hkQsTransformf());
            handArray[13].boneMatrix = hand.middle3Distal.Convert().ToMatrix() * controller;
            handArray[14] = new Bone((BoneList)BoneListEn.e_finger_ring_a_l, 14, handArray[1].id, handArray[1], new hkQsTransformf(), new hkQsTransformf());
            handArray[14].boneMatrix = hand.ring0Metacarpal.Convert().ToMatrix() * controller;
            handArray[15] = new Bone((BoneList)BoneListEn.e_finger_ring_a_l, 15, handArray[14].id, handArray[14], new hkQsTransformf(), new hkQsTransformf());
            handArray[15].boneMatrix = hand.ring1Proximal.Convert().ToMatrix() * controller;
            handArray[16] = new Bone((BoneList)BoneListEn.e_finger_ring_a_l, 16, handArray[15].id, handArray[15], new hkQsTransformf(), new hkQsTransformf());
            handArray[16].boneMatrix = hand.ring2Middle.Convert().ToMatrix() * controller;
            handArray[17] = new Bone((BoneList)BoneListEn.e_finger_ring_a_l, 17, handArray[16].id, handArray[16], new hkQsTransformf(), new hkQsTransformf());
            handArray[17].boneMatrix = hand.ring3Distal.Convert().ToMatrix() * controller;
            handArray[18] = new Bone((BoneList)BoneListEn.e_finger_pinky_a_l, 18, handArray[1].id, handArray[1], new hkQsTransformf(), new hkQsTransformf());
            handArray[18].boneMatrix = hand.pinky0Metacarpal.Convert().ToMatrix() * controller;
            handArray[19] = new Bone((BoneList)BoneListEn.e_finger_pinky_a_l, 19, handArray[18].id, handArray[18], new hkQsTransformf(), new hkQsTransformf());
            handArray[19].boneMatrix = hand.pinky1Proximal.Convert().ToMatrix() * controller;
            handArray[20] = new Bone((BoneList)BoneListEn.e_finger_pinky_a_l, 20, handArray[19].id, handArray[19], new hkQsTransformf(), new hkQsTransformf());
            handArray[20].boneMatrix = hand.pinky2Middle.Convert().ToMatrix() * controller;
            handArray[21] = new Bone((BoneList)BoneListEn.e_finger_pinky_a_l, 21, handArray[20].id, handArray[20], new hkQsTransformf(), new hkQsTransformf());
            handArray[21].boneMatrix = hand.pinky3Distal.Convert().ToMatrix() * controller;
            handArray[0].SetWorldFromBoneMatrix(true);

            Matrix4x4 trnsOff = Matrix4x4.Identity;
            Vector3 p1 = Vector3.Zero;
            Vector3 p2 = Vector3.Zero;

            if (tPose == poseType.LeftHand)
                trnsOff = Matrix4x4.CreateTranslation(0.5f, heightOffset, 0);
            else if (tPose == poseType.RightHand)
                trnsOff = Matrix4x4.CreateTranslation(-0.5f, heightOffset, 0);

            for (int i = 0; i < 22; i++)
            {
                if (handArray[i].parent == null)
                    p1 = Vector3.Zero;
                else
                    p1 = Vector3.Transform(handArray[i].parent!.boneMatrix.Translation, trnsOff);
                p2 = Vector3.Transform(handArray[i].boneMatrix.Translation, trnsOff);
                Imports.SetRayCoordinate((float*)&p1, (float*)&p2);
            }
        }

    }
}

