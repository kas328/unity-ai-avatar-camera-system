using System;
using System.Collections.Generic;
using System.IO;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EasyUI.Toast;
using Mingle.Dev.KSK_Test._02.Scripts.Utility;
using Mingle.Dev.Scripts.AvatarCustom;
using Mingle.Dev.Scripts.Shop.NewShop;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Random = UnityEngine.Random;

namespace Mingle.Dev.KSK_Test._02.Scripts.AIAvatar
{
    public class AIAvatarCustomization : MonoBehaviour
    {
        #region SerializeField
        [Header("References")] 
        [SerializeField] private GameObject capsule;
        [SerializeField] private GameObject randomPang;
        [SerializeField] private CameraCapture cameraCapture;
        [SerializeField] private CaptureButton captureButton;
        [SerializeField] private AILoadingScreen loadingScreen;
        #endregion

        #region Private Fields
        private Animator _animator;
        private static readonly int Blend = Animator.StringToHash("Blend");
        private const string RandomPangStateName = "RandomPangAnimation";
        private string _parts = "[]";
        private AvatarPartsHandler _avatarPartsHandler;
        private AvatarPartsController _avatarPartsController;

        // S3 관련 변수들
        private readonly string _bucketName = "[BUCKET_NAME]";
        private readonly string _folderName = "[FOLDER_NAME]";
        private IAmazonS3 _s3Client;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            // 씬 진입 시 카메라 권한 확인
            CheckInitialCameraPermission().Forget();
        }

        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            _animator = GetComponent<Animator>();
            _avatarPartsController = GetComponent<AvatarPartsController>();
            _avatarPartsHandler = new AvatarPartsHandler();

            // S3 클라이언트 초기화
            _s3Client = new AmazonS3Client(
                new CognitoAWSCredentials("[COGNITO_IDENTITY_POOL_ID]",
                    RegionEndpoint.APNortheast2), RegionEndpoint.APNortheast2);
        }
        #endregion

        #region Public Methods
        public void OnClickCaptureBtn()
        {
            CheckCameraPermissionAndCapture().Forget();
        }
        
        private async UniTaskVoid CheckCameraPermissionAndCapture()
        {
            bool hasCameraPermission = MinglePermissionHandler.CheckPermission(PermissionType.Camera);
            
            if (hasCameraPermission)
            {
                captureButton.DisableButton();
                await StartCustomization();
            }
            else
            {
                await MinglePermissionHandler.CheckAskPermission(PermissionType.Camera, () => {
                    Debug.Log("카메라 권한이 거부되었습니다.");
                });
            }
        }
        
        private async UniTaskVoid CheckInitialCameraPermission()
        {
            // 프레임 대기 (UI 초기화 완료 후 실행)
            await LocalizationSettings.InitializationOperation.Task;
            
            await UniTask.Delay(300);
            
            bool hasCameraPermission = MinglePermissionHandler.CheckPermission(PermissionType.Camera);
            
            if (!hasCameraPermission)
            {
                await MinglePermissionHandler.CheckAskPermission(PermissionType.Camera, () => {
                    Debug.Log("카메라 권한이 거부되었습니다.");
                });
            }
        }
        
        public async UniTask StartCustomizationWithGalleryImage(byte[] imageData)
        {
            try
            {
                // 로딩 화면 표시
                if (loadingScreen != null)
                {
                    loadingScreen.ShowLoadingScreen();
                    loadingScreen.SetProgress(0.2f);
                }
                else
                {
                    randomPang.SetActive(true);
                }
                
                if (imageData == null || imageData.Length == 0)
                {
                    Debug.LogError("Image data is null or empty");
                    return;
                }

                string fileName = $"{Information.UserId}_{System.DateTime.Now:yyyyMMddHHmmss}.png";
                string localPath = Path.Combine(Application.temporaryCachePath, fileName);
                await File.WriteAllBytesAsync(localPath, imageData);
                
                // 진행 상황 업데이트
                loadingScreen?.SetProgress(0.4f);

                string imageUrl = await UploadToS3(localPath, fileName);
                if (string.IsNullOrEmpty(imageUrl)) return;
                
                // 진행 상황 업데이트
                loadingScreen?.SetProgress(0.6f);

                var parts = await _avatarPartsHandler.ProcessPartsAsync(imageUrl);
                if (parts == null) return;
                
                // 진행 상황 업데이트
                loadingScreen?.SetProgress(0.9f);

                await SetPartsAndUpdateUI(parts);

                // 로딩 화면 숨기기
                if (loadingScreen != null)
                {
                    loadingScreen.HideLoadingScreen();
                }
                else 
                {
                    randomPang.SetActive(false);
                }

                await ShowCharacterWithAnimation();

                // 임시 파일 삭제
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }

                UniversalSceneManager.CompleteAIAvatar();
            }
            catch (Exception e)
            {
                // 오류 발생 시 로딩 화면 숨기기
                if (loadingScreen != null)
                {
                    loadingScreen.HideLoadingScreen();
                }
                else
                {
                    randomPang.SetActive(false);
                }
                
                captureButton.EnableButton();
                string retryMessage = LocalizationManager.GetLocalizedText(
                    "AIAvatarTable", 
                    "UI_AIAvatar_Retry", 
                    "가이드라인에 맞는 다른 사진으로 다시 시도해주세요"
                );

                Toast.Show(retryMessage, ToastColor.Black, ToastPosition.TopCenter);
                Debug.LogError($"Error during customization: {e.Message}");
            }
        }
        #endregion

        #region Customization Process
        private async UniTask StartCustomization()
        {
            try
            {
                loadingScreen.ShowLoadingScreen();
                loadingScreen.SetProgress(0.1f);
                
                byte[] photoData = await cameraCapture.TakePhoto();
                if (photoData == null || photoData.Length == 0) return;

                string fileName = $"{Information.UserId}_{DateTime.Now:yyyyMMddHHmmss}.png";
                string localPath = Path.Combine(Application.temporaryCachePath, fileName);
                await File.WriteAllBytesAsync(localPath, photoData);

                // 진행 상황 업데이트
                loadingScreen?.SetProgress(0.3f);

                string imageUrl = await UploadToS3(localPath, fileName);
                if (string.IsNullOrEmpty(imageUrl)) return;

                // 진행 상황 업데이트
                loadingScreen?.SetProgress(0.5f);

                var parts = await _avatarPartsHandler.ProcessPartsAsync(imageUrl);
                if (parts == null) return;

                // 진행 상황 업데이트
                loadingScreen?.SetProgress(0.8f);

                await SetPartsAndUpdateUI(parts);
                
                // 로딩 화면 숨기기
                if (loadingScreen != null)
                {
                    loadingScreen.HideLoadingScreen();
                }
                else 
                {
                    randomPang.SetActive(false);
                }
                
                await ShowCharacterWithAnimation();

                // 임시 파일 삭제
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
    
                UniversalSceneManager.CompleteAIAvatar();
            }
            catch (Exception e)
            {
                // 오류 발생 시 로딩 화면 숨기기
                if (loadingScreen != null)
                {
                    loadingScreen.HideLoadingScreen();
                }
                else
                {
                    randomPang.SetActive(false);
                }
                
                if(captureButton != null) captureButton.EnableButton();
                string retryMessage = LocalizationManager.GetLocalizedText(
                    "AIAvatarTable", 
                    "UI_AIAvatar_Retry", 
                    "가이드라인에 맞는 다른 사진으로 다시 시도해주세요"
                );

                Toast.Show(retryMessage, ToastColor.Black, ToastPosition.TopCenter);
                Debug.LogError($"Error during customization: {e.Message}");
            }
        }

        private async UniTask SetPartsAndUpdateUI(List<ChildPart> parts)
        {
            _parts = Information.ConvertPartsAsString(parts);
            if (UniversalSceneManager.SceneStack.Count > 0)
            {
                var previousScene = UniversalSceneManager.SceneStack.Peek().sceneName;
                if (previousScene == SceneName.LoginScene)
                {
                    await APIManager.SetEquippedPartsAsync(APIManager.SpecifyToken(), parts);
                }
            }
            _avatarPartsController.UpdateCustoms(_parts);
            Information.ChangeParts(_parts);
        }

        private async UniTask ShowCharacterWithAnimation()
        {
            transform.localScale = Vector3.zero;
            await transform.DOScale(250, 0.3f).SetEase(Ease.OutQuart).SetLink(gameObject).AsyncWaitForCompletion();
            
            PlayRandomAnimation();
        }
        #endregion

        #region Utility Methods
        private async UniTask<string> UploadToS3(string filePath, string objectName)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = $"{_bucketName}/{_folderName}",
                    Key = objectName,
                    FilePath = filePath,
                    CannedACL = S3CannedACL.PublicRead
                };

                var response = await _s3Client.PutObjectAsync(request);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    return $"https://[BUCKET_NAME].s3.ap-northeast-2.amazonaws.com/[FOLDER_NAME]/{objectName}";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error uploading to S3: {e.Message}");
            }

            return null;
        }

        private void PlayRandomAnimation()
        {
            _animator.SetFloat(Blend, Random.Range(0, 4));
            _animator.Play(RandomPangStateName);
        }
        #endregion
    }
}