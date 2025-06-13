using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mingle.Dev.Scripts.Shop.NewShop;
using Newtonsoft.Json;
using UnityEngine;

namespace Mingle.Dev.KSK_Test._02.Scripts.AIAvatar
{
    public class AvatarPartsHandler
    {
        #region Category Definitions
        // AI 분석 파츠 중 필수 장착 (빨간색)
        private readonly HashSet<string> _aiRequiredCategories = new HashSet<string>
        {
            Constants.AvatarBodySubCategory,
            Constants.AvatarHairSubCategory,
            Constants.AvatarEyebrowSubCategory,
            Constants.AvatarEyeSubCategory
        };

        // AI 분석 파츠 중 벗기기 가능 (파란색)
        private readonly HashSet<string> _aiOptionalCategories = new HashSet<string>
        {
            Constants.AvatarInnerSubCategory,
            Constants.AvatarLayeredSubCategory,
            Constants.AvatarInnerOnepieceSubCategory,
            Constants.AvatarOuterSubCategory,
            Constants.AvatarVestSubCategory,
            Constants.AvatarGlassesSubCategory,
            Constants.AvatarHeadPieceSubCategory
        };

        // 랜덤 생성 파츠 중 필수 장착 (빨간색)
        private readonly HashSet<string> _randomRequiredCategories = new HashSet<string>
        {
            Constants.AvatarHeadSubCategory,
            Constants.AvatarEarSubCategory,
            Constants.AvatarEyelashSubCategory,
            Constants.AvatarToothSubCategory,
            Constants.AvatarPantsSubCategory,
            Constants.AvatarSkirtSubCategory
        };

        // 랜덤 생성 파츠 중 벗기기 가능 (파란색)
        private readonly HashSet<string> _randomOptionalCategories = new HashSet<string>
        {
            Constants.AvatarSocksSubCategory,
            Constants.AvatarShoesSubCategory
        };
        
        // 하의 카테고리 grouping
        private readonly HashSet<string> _bottomCategories = new HashSet<string>
        {
            Constants.AvatarPantsSubCategory,
            Constants.AvatarSkirtSubCategory
        };

        // 제외 카테고리
        private readonly HashSet<string> _excludedCategories = new HashSet<string>
        {
            Constants.AvatarRingSubCategory,
            Constants.AvatarNecklaceSubCategory,
            Constants.AvatarBeardSubCategory,
            Constants.AvatarMustachSubCategory
        };
        #endregion

        #region Parts Processing
        public async UniTask<List<ChildPart>> ProcessPartsAsync(string imageUrl)
        {
            // 1. 모든 무료 파츠 가져오기
            var allFreePartsJson = await APIManager.GetAllFreeShopPartsAsync(APIManager.SpecifyToken());
            if (string.IsNullOrEmpty(allFreePartsJson)) return null;
            var allFreeParts = JsonConvert.DeserializeObject<List<ChildPart>>(allFreePartsJson);
            Debug.Log($"Free Parts Response: {allFreePartsJson}");
            
            // 2. AI 분석 파츠 가져오기
            var aiPartsJson = await APIManager.GetAIAvatarAsync(APIManager.SpecifyToken(), imageUrl);
            if (string.IsNullOrEmpty(aiPartsJson)) return null;
            Debug.Log($"AI Parts Response: {aiPartsJson}");

            var aiParts = JsonConvert.DeserializeObject<List<ChildPart>>(aiPartsJson);

            var combinedParts = new List<ChildPart>();
            var processedCategories = new HashSet<string>();

            var bodyColor = "";

            // 3. AI 분석 필수 파츠 처리 (빨간색)
            if (aiParts != null)
            {
                foreach (var aiPart in aiParts)
                {
                    var category = aiPart.SubCategory.ToLower();
                    if (_aiRequiredCategories.Contains(category))
                    {
                        if (category == Constants.AvatarHairSubCategory)
                        {
                            var hex = aiPart.Color;
                            aiPart.Color = $"{hex},{hex},{hex}";
                        }

                        if (category == Constants.AvatarBodySubCategory)
                        {
                            bodyColor = aiPart.Color;
                        }
                        
                        combinedParts.Add(aiPart);
                        processedCategories.Add(category);
                    }
                }
            }

            // 4. AI 분석 선택적 파츠 처리 (파란색)
            if (aiParts != null)
            {
                foreach (var category in _aiOptionalCategories)
                {
                    if (processedCategories.Contains(category)) continue;

                    var categoryParts = aiParts
                        .Where(p => p.SubCategory.ToLower() == category)
                        .ToList();
                    
                    if (!categoryParts.Any()) continue;

                    // 장착하지 않는 옵션을 포함하여 랜덤 선택
                    var index = Random.Range(0, categoryParts.Count + 0);
                    var isNull = index == categoryParts.Count;
                    
                    if (!isNull)
                    {
                        combinedParts.Add(categoryParts[index]);
                    }
                    processedCategories.Add(category);
                }
            }

            // 5. 랜덤 생성 필수 파츠 처리 (빨간색)
            foreach (var category in _randomRequiredCategories)
            {
                if (processedCategories.Contains(category)) continue;

                var categoryParts = allFreeParts
                    .Where(p => p.SubCategory.ToLower() == category)
                    .ToList();
                
                if (!categoryParts.Any()) continue;

                var randomIndex = Random.Range(0, categoryParts.Count);
                    
                if (category == Constants.AvatarHeadSubCategory)
                {
                    categoryParts[randomIndex].Color = bodyColor;
                }
                
                if (_bottomCategories.Contains(category))
                {
                    var bottomParts = allFreeParts
                        .Where(p => _bottomCategories.Contains(p.SubCategory.ToLower()))
                        .ToList();

                    if (bottomParts.Any())
                    {
                        var bottomIndex = Random.Range(0, bottomParts.Count);
                        combinedParts.Add(bottomParts[bottomIndex]);
                    }
                    processedCategories.Add(Constants.AvatarPantsSubCategory);
                    processedCategories.Add(Constants.AvatarSkirtSubCategory);
                    continue;
                }
                
                combinedParts.Add(categoryParts[randomIndex]);
                processedCategories.Add(category);
            }

            // 6. 랜덤 생성 선택적 파츠 처리 (파란색)
            foreach (var category in _randomOptionalCategories)
            {
                if (processedCategories.Contains(category)) continue;

                var categoryParts = allFreeParts
                    .Where(p => p.SubCategory.ToLower() == category)
                    .ToList();
                
                if (!categoryParts.Any()) continue;

                // 장착하지 않는 옵션을 포함하여 랜덤 선택
                var index = Random.Range(0, categoryParts.Count + 0);
                var isNull = index == categoryParts.Count;
                
                if (!isNull)
                {
                    combinedParts.Add(categoryParts[index]);
                }
                processedCategories.Add(category);
            }
            
            Debug.Log("@@@" + JsonConvert.SerializeObject(combinedParts));
            return combinedParts;
        }
        #endregion
    }
}