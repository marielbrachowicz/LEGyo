using System;
using UnityEngine;
using com.rfilkov.kinect;
using ARMyo.Core;

// Ojo: el namespace NO se llama "ARMyo.Input" porque taparía a UnityEngine.Input
// dentro de los scripts del namespace ARMyo.
namespace ARMyo.Sensores
{
    /// <summary>
    /// Puente entre el KinectManager (paquete de rfilkov) y nuestros scripts.
    /// Versión pierna del KinectBridge del brazo: cada frame lee cadera, rodilla,
    /// tobillo y pie de la pierna elegida, más pelvis, pecho y la mano contraria,
    /// y los publica en un LegBodyFrame.
    ///
    /// Los demás scripts se suscriben a OnFrameUpdated o leen LastFrame;
    /// nunca hablan con KinectManager directamente.
    /// </summary>
    public class KinectBridgePierna : MonoBehaviour
    {
        public static KinectBridgePierna Instance { get; private set; }

        [Header("Configuración")]
        [Tooltip("Pierna del usuario que se mide. El modelo PIERNA.fbx es la derecha.")]
        public LadoPierna lado = LadoPierna.Derecha;

        [Range(0.1f, 2f)] public float userIdRefreshInterval = 0.5f;
        public bool logUserEvents = true;

        public event Action<LegBodyFrame> OnFrameUpdated;
        public event Action<ulong> OnUserDetected;
        public event Action<ulong> OnUserLost;

        public bool KinectListo => _kinectReady;
        public bool HayUsuario => _cachedUserId != 0;
        public bool IsReady => _kinectReady && _lastFrame.IsValid;
        public LegBodyFrame LastFrame => _lastFrame;

        /// <summary>Joint que falta (para mostrarlo en pantalla). Vacío si están todos.</summary>
        public string JointFaltante { get; private set; } = "";

        private LegBodyFrame _lastFrame = LegBodyFrame.Invalid;
        private ulong _cachedUserId;
        private ulong _previousUserId;
        private float _lastUserCheck = -999f;
        private bool _kinectReady;
        private KinectManager _km;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (_km == null) _km = KinectManager.Instance;
            _kinectReady = _km != null && _km.IsInitialized();

            if (!_kinectReady) { EmitInvalidIfNeeded(); return; }

            RefreshUserId();
            if (_cachedUserId == 0) { JointFaltante = ""; EmitInvalidIfNeeded(); return; }

            bool der = lado == LadoPierna.Derecha;
            var jCadera  = der ? KinectInterop.JointType.HipRight   : KinectInterop.JointType.HipLeft;
            var jRodilla = der ? KinectInterop.JointType.KneeRight  : KinectInterop.JointType.KneeLeft;
            var jTobillo = der ? KinectInterop.JointType.AnkleRight : KinectInterop.JointType.AnkleLeft;
            var jPie     = der ? KinectInterop.JointType.FootRight  : KinectInterop.JointType.FootLeft;
            var jMano    = der ? KinectInterop.JointType.HandLeft   : KinectInterop.JointType.HandRight;

            // Imprescindibles para los ángulos. El pie y la mano no se exigen:
            // el Kinect v2 los pierde a menudo y no queremos cortar la medición por eso.
            if (!Tracked(KinectInterop.JointType.Pelvis) ||
                !Tracked(KinectInterop.JointType.SpineChest) ||
                !Tracked(jCadera) || !Tracked(jRodilla) || !Tracked(jTobillo))
            {
                EmitInvalidIfNeeded();
                return;
            }
            JointFaltante = "";

            var frame = new LegBodyFrame(
                userId: _cachedUserId,
                lado: lado,
                cadera: Pos(jCadera),
                rodilla: Pos(jRodilla),
                tobillo: Pos(jTobillo),
                pie: Pos(jPie),
                pelvis: Pos(KinectInterop.JointType.Pelvis),
                pecho: Pos(KinectInterop.JointType.SpineChest),
                manoContraria: Pos(jMano),
                timestamp: Time.time,
                isValid: true);

            _lastFrame = frame;
            OnFrameUpdated?.Invoke(frame);
        }

        Vector3 Pos(KinectInterop.JointType j) => _km.GetJointPosition(_cachedUserId, j);

        bool Tracked(KinectInterop.JointType j)
        {
            if (_km.IsJointTracked(_cachedUserId, j)) return true;
            JointFaltante = j.ToString();
            return false;
        }

        void RefreshUserId()
        {
            if (Time.time - _lastUserCheck < userIdRefreshInterval) return;
            _lastUserCheck = Time.time;

            ulong newId = _km.GetPrimaryUserID();
            if (newId == _previousUserId) return;

            if (_previousUserId != 0)
            {
                if (logUserEvents) Debug.Log($"[KinectBridgePierna] Usuario perdido: {_previousUserId}");
                OnUserLost?.Invoke(_previousUserId);
                _lastFrame = LegBodyFrame.Invalid;
            }
            if (newId != 0)
            {
                if (logUserEvents) Debug.Log($"[KinectBridgePierna] Usuario detectado: {newId}");
                OnUserDetected?.Invoke(newId);
            }
            _cachedUserId = newId;
            _previousUserId = newId;
        }

        void EmitInvalidIfNeeded()
        {
            if (!_lastFrame.IsValid) return;
            _lastFrame = LegBodyFrame.Invalid;
            OnFrameUpdated?.Invoke(LegBodyFrame.Invalid);
        }
    }
}
