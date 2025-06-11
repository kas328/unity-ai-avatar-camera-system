using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Mingle.Dev.KSK_Test._02.Scripts.AIAvatar
{
    public class CameraCapture : MonoBehaviour
    {
        #region SerializeField
        [Header("UI References")] 
        [SerializeField] private RawImage displayImage;
        [SerializeField] private Button switchCameraButton;
        #endregion

        private readonly Vector2 _referenceResolution = new Vector2(430, 932);
        private readonly Vector2 _designedViewerSize = new Vector2(430, 550);
        
        #region Constants & Private Fields
        private const float ScreenWidthRatio = 0.48f;
        private WebCamTexture _currentCamera;
        private bool _isCameraPermissionGranted;
        private bool _isInitialized;
        private bool _isFrontCamera = true; // 초기 카메라를 전면으로 설정
        private float _iosZoomLevel = 1.3f;
        
        // 타이머 관련
        private float _lastPermissionCheckTime = 0f;
        private const float PermissionCheckCooldown = 1f;
        #endregion

        #region Unity Lifecycle
        async void Start()
        {
            if (switchCameraButton != null)
            {
                switchCameraButton.onClick.AddListener(OnSwitchCameraButtonClick);
            }
            
            await RequestCameraPermission();
        }

        void OnDisable()
        {
            if (_currentCamera != null && _currentCamera.isPlaying)
            {
                _currentCamera.Stop();
            }
        }
        
        // 앱이 포그라운드로 돌아올 때 권한 확인
        private async void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && Time.time - _lastPermissionCheckTime > PermissionCheckCooldown)
            {
                _lastPermissionCheckTime = Time.time;
                
                await UniTask.Delay(1000);
                CheckCameraPermissionOnFocus().Forget();
            }
        }
        
        private async UniTaskVoid CheckCameraPermissionOnFocus()
        {
            bool hasPermission = MinglePermissionHandler.CheckPermission(PermissionType.Camera);
            
            // 권한 상태가 변경된 경우에만 처리
            if (hasPermission != _isCameraPermissionGranted)
            {
                Debug.Log("앱 포커스 획득 시 카메라 권한 상태 변경 감지: " + (hasPermission ? "허용됨" : "거부됨"));
                _isCameraPermissionGranted = hasPermission;
                
                if (hasPermission)
                {
                    // 권한이 새로 허용된 경우, 카메라 초기화
                    if (!_isInitialized || _currentCamera == null || !_currentCamera.isPlaying)
                    {
                        InitializeCamera();
                    }
                }
                else
                {
                    // 권한이 거부된 경우, 카메라 중지
                    if (_currentCamera != null && _currentCamera.isPlaying)
                    {
                        _currentCamera.Stop();
                        _isInitialized = false;
                        
                        // 텍스처 초기화 (검은 화면 표시)
                        if (displayImage != null)
                        {
                            displayImage.texture = null;
                        }
                    }
                }
            }
        }
        #endregion

        #region Camera Control
        private void OnSwitchCameraButtonClick()
        {
            CheckCameraPermissionAndSwitch().Forget();
        }
        
        private async UniTaskVoid CheckCameraPermissionAndSwitch()
        {
            bool hasPermission = MinglePermissionHandler.CheckPermission(PermissionType.Camera);
            
            if (hasPermission)
            {
                SwitchCamera();
            }
            else
            {
                await MinglePermissionHandler.CheckAskPermission(PermissionType.Camera, () => {
                    Debug.Log("카메라 권한이 거부되었습니다.");
                });
                
                if (MinglePermissionHandler.CheckPermission(PermissionType.Camera))
                {
                    SwitchCamera();
                }
            }
        }

        private void SwitchCamera()
        {
            // 기존 카메라 정지 및 해제
            if (_currentCamera != null)
            {
                _currentCamera.Stop();
                _isInitialized = false;
            }

            _isFrontCamera = !_isFrontCamera;
            
            InitializeCamera();
        }

        public void InitializeCamera()
        {
            if (_isInitialized) return;

            if (!_isCameraPermissionGranted) return;

            WebCamDevice[] devices = WebCamTexture.devices;
            WebCamDevice? selectedDevice = null;

            // 원하는 카메라(전면 또는 후면) 찾기
            foreach (var device in devices)
            {
                if (_isFrontCamera && device.isFrontFacing)
                {
                    selectedDevice = device;
                    break;
                }
                if (!_isFrontCamera && !device.isFrontFacing)
                {
                    selectedDevice = device;
                    break;
                }
            }

            // 원하는 카메라를 찾지 못했을 경우, 사용 가능한 첫 번째 카메라 사용
            if (selectedDevice == null && devices.Length > 0)
            {
                selectedDevice = devices[0];
                // 선택된 카메라가 전면인지 후면인지에 따라 상태 업데이트
                _isFrontCamera = selectedDevice.Value.isFrontFacing;
            }

            if (selectedDevice == null) return;

            _currentCamera = new WebCamTexture(selectedDevice.Value.name, 640, 480, 30);
            
            if (_currentCamera == null) return;

            displayImage.texture = _currentCamera;
            _currentCamera.Play();

            StartCoroutine(WaitForCamera());
        }

        private IEnumerator WaitForCamera()
        {
            while (_currentCamera.width <= 16 || _currentCamera.height <= 16)
            {
                yield return new WaitForEndOfFrame();
            }

            bool isRotated = false;

#if UNITY_IOS && !UNITY_EDITOR
            // iOS에서 카메라 회전 처리
            if (_isFrontCamera)
            {
                displayImage.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            }
            else
            {
                displayImage.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
                displayImage.rectTransform.localScale = new Vector3(1, 1, 1);
            }

            // apply zoom level
            float size = 1.0f / _iosZoomLevel;
            float offset = (1.0f - size) / 2.0f;
            displayImage.uvRect = new Rect(offset, offset, size, size);
            isRotated = true;
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Android에서 카메라 회전 처리
            if (_isFrontCamera)
            {
                displayImage.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
                // z축으로 90도 회전시킨 상태이기때문에 좌우 반전을 해결하기 위해서는 y축으로 반전시켜야 함.
                displayImage.rectTransform.localScale = new Vector3(1, -1, 1);
            }
            else
            {
                displayImage.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
                displayImage.rectTransform.localScale = new Vector3(1, 1, 1);
            }

            // apply zoom level
            float size = 1.0f / _iosZoomLevel;
            float offset = (1.0f - size) / 2.0f;
            displayImage.uvRect = new Rect(offset, offset, size, size);
            isRotated = true;
#endif
            
            Vector2 scaledCanvas = ImageUtilities.GetScaledCanvasSize(_referenceResolution, CanvasScaleMode.Expand);
            float cameraRatio = (float)_currentCamera.width / _currentCamera.height;
            AdjustCanvas(scaledCanvas, cameraRatio, isRotated);
            
            _isInitialized = true;
        }

        public void AdjustCanvas(Vector2 canvasResolution, float cameraRatio, bool isRotated)
        {
            float targetWidth = 0f;
            float targetHeight = 0f;
            float scaleFactor = 0f;
            float canvasYPos = 0f;
            
            float heightRatio = _designedViewerSize.y / _referenceResolution.y;
            if (isRotated)
            {
                targetWidth = canvasResolution.y * heightRatio;
                targetHeight = targetWidth / cameraRatio;
                scaleFactor = canvasResolution.x / Mathf.Min(targetHeight, canvasResolution.x);

                canvasYPos = (targetWidth * scaleFactor / 2f) - canvasResolution.y * heightRatio;
            }
            else
            {
                targetHeight = canvasResolution.y * heightRatio;
                targetWidth = targetHeight * cameraRatio;
                scaleFactor = canvasResolution.x / Mathf.Min(targetWidth, canvasResolution.x);

                canvasYPos = (targetHeight * scaleFactor / 2f) - canvasResolution.y * heightRatio;
                
            }
            
            displayImage.rectTransform.anchoredPosition = new Vector2(0, canvasYPos);
            displayImage.rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight) * scaleFactor;
        }
        #endregion

        #region Permission Management
        private async UniTask RequestCameraPermission()
        {
#if UNITY_IOS && !UNITY_EDITOR || UNITY_ANDROID && !UNITY_EDITOR
            _isCameraPermissionGranted = await MinglePermissionHandler.CheckAskNativePermission(PermissionType.Camera);
#else
            _isCameraPermissionGranted = true;
#endif

            if (_isCameraPermissionGranted)
            {
                InitializeCamera();
            }
            else
            {
                Debug.LogError("Camera permission denied");
            }
        }
        #endregion

        #region Photo Capture
        public async UniTask<byte[]> TakePhoto()
        {
            if (_currentCamera == null || !_currentCamera.isPlaying)
            {
                Debug.LogError("Camera is not initialized or not playing");
                return null;
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            Texture2D photo = new Texture2D(_currentCamera.width, _currentCamera.height);
            photo.SetPixels(_currentCamera.GetPixels());
            photo.Apply();

#if UNITY_IOS && !UNITY_EDITOR
            // iOS에서 사진 회전 처리
            photo = ImageUtilities.RotateTexture(photo, false);
#elif UNITY_ANDROID && !UNITY_EDITOR
            // 안드로이드는 전면과 후면의 회전값이 다름
            photo = ImageUtilities.RotateTexture(photo, _isFrontCamera);
#endif
            byte[] imageBytes = photo.EncodeToPNG();
            Destroy(photo);

            return imageBytes;
        }


        #endregion
    }
}