using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
// using static Obeliskial_Essentials.Essentials;
using System;
using static DivinationLogger.Plugin;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// Make sure your namespace is the same everywhere
namespace DivinationLogger
{

    [HarmonyPatch] //DO NOT REMOVE/CHANGE

    public class DivinationLogger
    {
        // To create a patch, you need to declare either a prefix or a postfix. 
        // Prefixes are executed before the original code, postfixes are executed after
        // Then you need to tell Harmony which method to patch.
        // public static int divinationTier;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), nameof(AtOManager.GoToTown))]
        public static void GoToTownPostfix(AtOManager __instance)
        {
            LogDebug("GoToTownPostfix");
            // GetDivinationTier();
            LogDivinations(__instance);
        }

        public static int GetDivinationTier(int divinationIndex)
        {
            LogDebug("GetDivinationTier");
            AtOManager atOManager = AtOManager.Instance;
            if (atOManager == null)
            {
                LogDebug("AtOManager is null");
                return 0;
            }
            Dictionary<int, int> _DivinationTier = new Dictionary<int, int>();
            _DivinationTier.Add(0, 2);
            _DivinationTier.Add(1, 5);
            _DivinationTier.Add(2, 6);
            _DivinationTier.Add(3, 8);
            _DivinationTier.Add(4, 10);            
            return _DivinationTier.ContainsKey(divinationIndex) ? _DivinationTier[divinationIndex] : 0;
        }


        public static void LogDivinations(AtOManager atOManager)
        {
            LogDebug("LogDivinations");
            int ndivinations = atOManager.divinationsNumber;
            int tierNum = 0; // 0 for Fast, 1 for Basic, 2 for Advanced, 3 for Premium, 4 for Supreme

            Dictionary<int, string[]> cardsByOrder = GetDivinationDictForOneDivination(atOManager, tierNum, ndivinations);
            
            // Export cardsByOrder as a json
            string cardList = JsonUtility.ToJson(cardsByOrder);

            // save the json to a file
            System.IO.File.WriteAllText("DivinationResults.json", cardList);
            // LogDebug($"Divination Results: {card}");
        }

        public static Dictionary<int, string[]> GetDivinationDictForOneDivination(AtOManager atOManager, int tierNum, int ndivinations)
        {
            UnityEngine.Random.InitState((AtOManager.Instance.GetGameId() + "_" + AtOManager.Instance.mapVisitedNodes.Count.ToString() + "_" + AtOManager.Instance.currentMapNode + "_" + ndivinations).GetDeterministicHashCode());
            // ++ndivinations;
            // int tierNum = 0; // 0 for Fast, 1 for Basic, 2 for Advanced, 3 for Premium, 4 for Supreme
            TierRewardData townDivinationTier = Globals.Instance.GetTierRewardData(GetDivinationTier(tierNum));
            Dictionary<int, string[]> cardsByOrder = new Dictionary<int, string[]>();
            TierRewardData tierRewardBase;
            // int typeOfReward;
            // int dustQuantity;
            // int cardTierModFromCorruption;
            int numCardsReward = tierNum == 0 ? 3 : 4;
            TierRewardData tierRewardInf;
            TierRewardData tierReward;
            Hero[] theTeam = AtOManager.Instance.GetTeam();
            if ((UnityEngine.Object)townDivinationTier != (UnityEngine.Object)null)
            {
                tierRewardBase = townDivinationTier;
                // typeOfReward = 2;
            }
            else
            {
                LogDebug("No townDivinationTier");
                return null;
            }
            // dustQuantity = tierRewardBase.Dust;
            int num9 = tierRewardBase.TierNum;
            AtOManager.Instance.currentRewardTier = num9;
            // if ((UnityEngine.Object)thermometerData != (UnityEngine.Object)null)
            //     num9 += thermometerData.CardBonus + cardTierModFromCorruption;
            if (num9 < 0)
                num9 = 0;
            tierRewardBase = Globals.Instance.GetTierRewardData(num9);
            tierRewardInf = num9 <= 0 ? tierRewardBase : Globals.Instance.GetTierRewardData(num9 - 1);
            CardData _cardData = (CardData)null;
            for (int key = 0; key < theTeam.Length; ++key)
            {
                if (theTeam[key] == null || (UnityEngine.Object)theTeam[key].HeroData == (UnityEngine.Object)null)
                {
                    cardsByOrder[key] = new string[3]
                    {
                        "",
                        "",
                        ""
                    };
                }
                else
                {
                    Hero hero = theTeam[key];
                    Enums.CardClass result1 = Enums.CardClass.None;
                    Enum.TryParse<Enums.CardClass>(Enum.GetName(typeof(Enums.HeroClass), (object)hero.HeroData.HeroClass), out result1);
                    Enums.CardClass result2 = Enums.CardClass.None;
                    Enum.TryParse<Enums.CardClass>(Enum.GetName(typeof(Enums.HeroClass), (object)hero.HeroData.HeroSubClass.HeroClassSecondary), out result2);
                    int length = numCardsReward;
                    if (numCardsReward == 3 && result2 != Enums.CardClass.None)
                        length = 4;
                    string[] arr = new string[length];
                    List<string> stringList1 = Globals.Instance.CardListNotUpgradedByClass[result1];
                    List<string> stringList2 = result2 == Enums.CardClass.None ? new List<string>() : Globals.Instance.CardListNotUpgradedByClass[result2];
                    for (int index1 = 0; index1 < length; ++index1)
                    {
                        tierReward = index1 != 0 ? tierRewardInf : tierRewardBase;
                        int num10 = UnityEngine.Random.Range(0, 100);
                        bool flag2 = true;
                        while (flag2)
                        {
                            flag2 = false;
                            bool flag3 = false;
                            while (!flag3)
                            {
                                flag2 = false;
                                _cardData = Globals.Instance.GetCardData(index1 < 2 || result2 == Enums.CardClass.None ? stringList1[UnityEngine.Random.Range(0, stringList1.Count)] : stringList2[UnityEngine.Random.Range(0, stringList2.Count)], false);
                                if (!flag2)
                                {
                                    if (num10 < tierReward.Common)
                                    {
                                        if (_cardData.CardRarity == Enums.CardRarity.Common)
                                            flag3 = true;
                                    }
                                    else if (num10 < tierReward.Common + tierReward.Uncommon)
                                    {
                                        if (_cardData.CardRarity == Enums.CardRarity.Uncommon)
                                            flag3 = true;
                                    }
                                    else if (num10 < tierReward.Common + tierReward.Uncommon + tierReward.Rare)
                                    {
                                        if (_cardData.CardRarity == Enums.CardRarity.Rare)
                                            flag3 = true;
                                    }
                                    else if (num10 < tierReward.Common + tierReward.Uncommon + tierReward.Rare + tierReward.Epic)
                                    {
                                        if (_cardData.CardRarity == Enums.CardRarity.Epic)
                                            flag3 = true;
                                    }
                                    else if (_cardData.CardRarity == Enums.CardRarity.Mythic)
                                        flag3 = true;
                                }
                            }
                            int rarity = UnityEngine.Random.Range(0, 100);
                            string id = _cardData.Id;
                            _cardData = Globals.Instance.GetCardData(Functions.GetCardByRarity(rarity, _cardData), false);
                            if ((UnityEngine.Object)_cardData == (UnityEngine.Object)null)
                            {
                                flag2 = true;
                            }
                            else
                            {
                                for (int index2 = 0; index2 < arr.Length; ++index2)
                                {
                                    if (arr[index2] == _cardData.Id)
                                    {
                                        flag2 = true;
                                        break;
                                    }
                                }
                            }
                        }
                        arr[index1] = _cardData.Id;
                    }
                    cardsByOrder[key] = Functions.ShuffleArray<string>(arr);
                }
            }
            return cardsByOrder;
        }


    }
}