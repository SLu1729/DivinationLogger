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
// using OfficeOpenXml;
using System.IO;
using System.Text;
// using static Microsoft.IO.RecyclableMemoryStream;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;

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
            if (LogToLogOutput.Value) { LogDivinations(__instance); }
            WriteDivinationsToFile(__instance);
        }

        public static int GetDivinationTier(int divinationIndex)
        {
            LogDebug("Divination Tier: " + divinationIndex);
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
            int startingDivinationNum = atOManager.divinationsNumber;
            int tierNum = atOManager.GetTownTier() == 3 ? 2 : Math.Min(atOManager.GetTownTier(), 1);
            if (atOManager.GetTownTier() == 2)
            {
                tierNum -= 1;
            }
            int tiers = 3;
            int totalDivs = DivinationsToLog.Value;
            // 0 for Fast, 1 for Basic, 2 for Advanced, 3 for Premium, 4 for Supreme
            Dictionary<int, string> tierNames = new Dictionary<int, string>();
            tierNames.Add(0, "Fast");
            tierNames.Add(1, "Basic");
            tierNames.Add(2, "Advanced");
            tierNames.Add(3, "Premium");
            tierNames.Add(4, "Supreme");

            if (atOManager.GetTeam() == null)
            {
                LogDebug("Team is null");
                return;
            }
            Hero[] theTeam = atOManager.GetTeam();
            for (int j = 0; j < tiers; j++)
            {
                for (int i = 0; i < totalDivs; i++)
                {
                    int div = i + startingDivinationNum;
                    LogDebug($"Divination {div} {tierNames[tierNum + j]}");
                    Dictionary<int, string[]> cardsByOrder = GetDivinationDictForOneDivination(atOManager, tierNum + j, div);
                    if (cardsByOrder == null)
                    {
                        LogDebug("cardsByOrder is null");
                        return;
                    }
                    foreach (KeyValuePair<int, string[]> kvp in cardsByOrder)
                    {
                        if (kvp.Value == null || kvp.Value.Length == 0 || kvp.Key >= theTeam.Length)
                        {
                            LogDebug("Row is null");
                            return;
                        }
                        List<string> cards = new List<string>();
                        for (int k = 0; k < kvp.Value.Length; k++)
                        {
                            CardData cardData = Globals.Instance.GetCardData(kvp.Value[k], false);
                            if (cardData == null)
                            {
                                cards.Add("");
                            }
                            else
                            {
                                cards.Add(GetCardName(cardData));
                            }

                        }
                        string HeroName = theTeam[kvp.Key]?.SourceName?.ToString() ?? "Unknown";
                        LogDebug($"Hero {kvp.Key}, {HeroName}. Cards: {string.Join(", ", cards)}");
                    }

                }
            }
        }

        public static void WriteDivinationsToFile(AtOManager atOManager)
        {
            LogDebug("WriteDivinationsToFile");
            int startingDivinationNum = atOManager.divinationsNumber;
            int tierNum = Math.Min(Math.Max(atOManager.GetTownTier() - 1, 0), 2);
            int tiers = 3;
            int totalDivs = DivinationsToLog.Value;

            Dictionary<int, string> tierNames = new Dictionary<int, string>();
            tierNames.Add(0, "Fast");
            tierNames.Add(1, "Basic");
            tierNames.Add(2, "Advanced");
            tierNames.Add(3, "Premium");
            tierNames.Add(4, "Supreme");
            List<string> headerRow = ["Divination"];

            List<List<string>> divinationResults = new List<List<string>>();
            divinationResults.Add(headerRow);

            if (atOManager.GetTeam() == null)
            {
                LogDebug("Team is null");
                return;
            }
            Hero[] theTeam = atOManager.GetTeam();

            bool insertBlank = true;
            for (int j = 0; j < tiers; j++)
            {
                int nCards = 4;
                string tierName = tierNames[tierNum + j];
                
                for (int i = 0; i < nCards; i++)
                {
                    if(AddSpaces.Value && insertBlank)
                    {
                        headerRow.Add("");
                        insertBlank = false;
                    }
                    headerRow.Add($"{tierName}: Card {i + 1}");
                }
                insertBlank = true;
            }

            // Row = totalDivs_characterName
            // skipFirst = true;
            for (int divIteration = 0; divIteration < totalDivs; divIteration++)
            {
                // List<string> divinationRow = new List<string>();
                List<List<string>> divinationSetOfChars = new List<List<string>>();
                for (int i = 0; i < theTeam.Count(); i++)
                {
                    divinationSetOfChars.Add(new List<string>());
                }
                insertBlank = true;

                for (int j = 0; j < tiers; j++)
                {


                    int currentDiv = divIteration + startingDivinationNum;
                    LogDebug($"Divination {currentDiv} {tierNames[tierNum + j]}");
                    Dictionary<int, string[]> cardsByOrder = GetDivinationDictForOneDivination(atOManager, tierNum + j, currentDiv);

                    foreach (KeyValuePair<int, string[]> kvp in cardsByOrder)
                    {
                        int charIndex = kvp.Key;
                        // int headerIndex = charIndex + divIteration * theTeam.Count();
                        List<string> listOfCards = new List<string>();
                        for (int k = 0; k < 4; k++)
                        {
                            if(AddSpaces.Value && insertBlank)
                            {
                                listOfCards.Add("");
                                // listOfCards.Insert(0, charName);divinationSetOfChars[charIndex].Add("");
                                insertBlank = false;
                            }
                            if (k >= kvp.Value.Length)
                            {
                                listOfCards.Add("");
                                continue;
                            }
                            CardData cardData = Globals.Instance.GetCardData(kvp.Value[k], false);
                            if (cardData == null)
                            {
                                listOfCards.Add("");
                            }
                            else
                            {
                                listOfCards.Add(GetCardName(cardData));
                            }
                            
                        }
                        insertBlank = true;
                        
                        if (j == 0)
                        {
                            string charName = $"D{currentDiv + 1}: " + (theTeam[charIndex]?.SourceName?.ToString() ?? "Missing Hero");
                            listOfCards.Insert(0, charName);
                        }
                        
                        divinationSetOfChars[charIndex].AddRange(listOfCards);

                        // string HeroName = theTeam[kvp.Key]?.SourceName?.ToString() ?? "Unknown";
                        // LogDebug($"Hero {HeroName} {kvp.Key} cards: {string.Join(", ", kvp.Value)}");
                        // divinationRow.AddRange(kvp.Value);
                    }
                }
                int l = 0;
                foreach (List<string> row in divinationSetOfChars)
                {
                    divinationResults.Add(row);
                    l = row.Count();
                }
                if(AddSpaces.Value)
                {
                    List<string> blankRow = Enumerable.Repeat("", l).ToList();
                    divinationResults.Add(blankRow);
                }
            }

            SaveDivinationsToFile(atOManager, divinationResults);
        }


        public static void SaveDivinationsToFile(AtOManager atOManager, List<List<string>> divinationResults)
        {

            string filePath;
            if (AbsoluteFolderPath.Value == "" || AbsoluteFolderPath.Value == null)
            {
                string savePath = Path.GetDirectoryName(SaveManager.PathSaveGameTurn(0));
                filePath = SaveFolder.Value == "" || SaveFolder.Value == null ? Path.Combine(savePath, atOManager.GetGameId()) : Path.Combine(savePath, SaveFolder.Value);
            }
            else
            {
                filePath = AbsoluteFolderPath.Value;
            }
            string fileName = $"DivinationResults_Act_{atOManager.GetActNumberForText()}.csv";
            string fileNameE = $"DivinationResults_Act_{atOManager.GetActNumberForText()}.xlsx";
            if (SaveToCSV.Value)
            {
                WriteDataToCSV(divinationResults, Path.Combine(filePath, fileName));
            }

            if (SaveToExcel.Value)
            {
                // LogDebug("Excel is currently not working");
                // MiniExcel.ConvertCsvToXlsx(Path.Combine(filePath, fileName), Path.Combine(filePath, fileNameE));

                WriteDataToExcel(divinationResults, Path.Combine(filePath, fileNameE));
            }
        }


        public static Dictionary<int, string[]> GetDivinationDictForOneDivination(AtOManager atOManager, int tierNum, int ndivinations)
        {
            UnityEngine.Random.InitState((AtOManager.Instance.GetGameId() + "_" + AtOManager.Instance.mapVisitedNodes.Count.ToString() + "_" + AtOManager.Instance.currentMapNode + "_" + ndivinations).GetDeterministicHashCode());
            // ++ndivinations;
            // int tierNum = 0; // 0 for Fast, 1 for Basic, 2 for Advanced, 3 for Premium, 4 for Supreme
            TierRewardData townDivinationTier = Globals.Instance.GetTierRewardData(GetDivinationTier(tierNum));
            Dictionary<int, string[]> cardsByOrder = new Dictionary<int, string[]>();
            TierRewardData tierRewardBase;
            int numCardsReward = tierNum <= 1 ? 3 : 4;
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
            if (num9 < 0)
                num9 = 0;
            tierRewardBase = Globals.Instance.GetTierRewardData(num9);
            tierRewardInf = num9 <= 0 ? tierRewardBase : Globals.Instance.GetTierRewardData(num9 - 1);
            CardData _cardData = (CardData)null;
            int key = 0;
            for (; key < theTeam.Length; ++key)
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

        public static void WriteDataToCSV(List<List<string>> data, string filePath)
        {
            LogDebug("WriteListToCsv");
            try
            {
                StringBuilder csvContent = new StringBuilder();

                foreach (var row in data)
                {
                    csvContent.AppendLine(string.Join(",", row.Select(field =>
                        field.Contains(",") || field.Contains("\"")
                            ? $"\"{field.Replace("\"", "\"\"")}\""
                            : field)));
                }
                string outputDirectory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(filePath, csvContent.ToString());

                LogDebug($"CSV file successfully created at: {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"An error occurred: {ex.Message}");
            }
        }

        public static void WriteDataToExcel(List<List<string>> data, string filePath)
        {

            LogDebug("WriteListToExcel");

            List<string> headerRow = data.FirstOrDefault();
            if (headerRow == null)
            {
                LogError("headerRow is empty or null");
                return;
            }

            List<Dictionary<string, object>> values = [];

            for (int i = 1; i < data.Count; i++)
            {
                List<string> row = data[i];
                Dictionary<string, object> toAdd = [];
                for (int j = 0; j < headerRow.Count && j < row.Count; j++)
                {
                    toAdd.Add(headerRow[j], row[j]);
                }
                values.Add(toAdd);
            }
            if (AddFormatting.Value)
            {
                OpenXmlConfiguration configuration = new OpenXmlConfiguration()
                {
                    FastMode = true,
                    EnableAutoWidth = true,
                    FreezeColumnCount = 1,
                    FreezeRowCount = 1,    

                };
                // string filePathTest = Path.GetDirectoryName(filePath) + "test.xlsx";
                MiniExcel.SaveAs(filePath, values, configuration: configuration);
            }


            // MiniExcel.SaveAs(filePath, values);
            LogDebug($"Excel file created at: {filePath}");
        }



        public static string GetCardName(CardData cardData)
        {
            string output = cardData.CardName;
            if (cardData.CardUpgraded == Enums.CardUpgraded.A)
            {
                output += " (Blue)";
            }
            else if (cardData.CardUpgraded == Enums.CardUpgraded.B)
            {
                output += " (Yellow)";
            }
            else if (cardData.CardUpgraded == Enums.CardUpgraded.Rare)
            {
                output += " (Corrupted)";
            }
            return output;
        }


    }
}