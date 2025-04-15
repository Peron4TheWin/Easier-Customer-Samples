using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using MelonLoader;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Properties;
using Il2CppScheduleOne.UI.Handover;
using Il2CppSystem.Reflection;

[assembly: MelonInfo(typeof(EasierCustomerSamples.EntryPoint), "EasierCustomerSamples", "1.0.0", "_peron")]

namespace EasierCustomerSamples
{
    public class EntryPoint : MelonMod
    {
    }

    public class Config
    {
        public static bool AlwaysGuarantee = false;
        public static float BaseValue = 0.70f;
        public static float OneEffect = 0.10f;
        public static float TwoEffect = 0.20f;
        public static float ThreeEffect = 0.30f;

        public static void OnLoad()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string text = Path.Combine(Path.Combine(directoryName,"Mods"), "EasierCustomerSamples");
            bool flag = !Directory.Exists(text);
            if (flag)
            {
                Directory.CreateDirectory(text);
            }
            string path = Path.Combine(text, "config.ini");
            bool flag2 = !File.Exists(path);
            if (flag2)
            {
                string[] contents = new string[]
                {
                    "# Easier Customer samples mod configuration",
                    "#If this is on, the customer will always accept the sample, no matter how bad is it",
                    "AlwaysGuarantee=false",
                    "#This is the most configurable part, you can change the base acceptance rate for any drug you give to them, then how much accept rate a matched effect gives",
                    "#In this example if you give a drug with 1 effect the npc likes, you'll get a 80%, if you give one with two you'll get 90% and if you got three out of three matches you'll get a 100%",
                    "#You can customize this as you please :D",
                    "BaseValue=0.7",
                    "OneEffect=0.1",
                    "TwoEffect=0.2",
                    "ThreeEffect=0.3",
                };
                File.WriteAllLines(path, contents);
                MelonLogger.Msg("Config file created with default values.");
            }
            bool lastupdated = false;
            string[] array = File.ReadAllLines(path);
            foreach (string text2 in array)
            {
                bool flag3 = string.IsNullOrWhiteSpace(text2) || text2.TrimStart().StartsWith("#");
                if (!flag3)
                {
                    string[] array3 = text2.Split('=', StringSplitOptions.None);
                    bool flag4 = array3.Length < 2;
                    if (!flag4)
                    {
                        string text3 = array3[0].Trim();
                        string text4 = array3[1].Trim();
                        if (text3.Equals("AlwaysGuarantee", StringComparison.CurrentCultureIgnoreCase))
                        {
                            AlwaysGuarantee = bool.Parse(text4);
                        }
                        else if (text3.Equals("BaseValue", StringComparison.OrdinalIgnoreCase))
                        {
                            BaseValue = float.Parse(text4);
                        } else if (text3.Equals("OneEffect", StringComparison.OrdinalIgnoreCase))
                        {
                            OneEffect = float.Parse(text4);
                        }
                        else if (text3.Equals("TwoEffect", StringComparison.OrdinalIgnoreCase))
                        {
                            TwoEffect = float.Parse(text4);
                        }
                        else if (text3.Equals("ThreeEffect", StringComparison.OrdinalIgnoreCase))
                        {
                            ThreeEffect = float.Parse(text4);
                        }
                    }
                }
                
            }
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.PlayerScripts.Player))]
    [HarmonyPatch("PlayerLoaded")]
    class ConfigLoad
    {
        [HarmonyPostfix]
        static void PostfixPlayerLoaded(Il2CppScheduleOne.PlayerScripts.Player __instance)
        {
            Config.OnLoad();
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.Economy.Customer))]
    class Main
    {
        [HarmonyPatch("GetSampleSuccess")]
        [HarmonyPrefix]
        public static bool GetSampleSuccessPrefix(Il2CppScheduleOne.Economy.Customer __instance, ref float __result, List<ItemInstance> items, float price)
        {
            try
            {
                
                if (Config.AlwaysGuarantee)
                {
                    __result = 1f;
                    return false;
                }

                __result = Config.BaseValue;
                __instance.customerData.GuaranteeFirstSampleSuccess = false;
                ProductItemInstance productItemInstance;
                ProductDefinition product;
                ItemInstance itemInstance = null;
                try
                {
                    Il2CppReferenceArray<ItemSlot> test = Singleton<HandoverScreen>.Instance.CustomerSlots;
                    foreach (ItemSlot itemSlot in test)
                    {
                        if (itemSlot.Quantity != 0)
                        {
                            try
                            {
                                itemInstance = itemSlot.ItemInstance;
                            }
                            catch (Exception e)
                            {
                                return true;
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    //ignore
                }

                if (__instance.consumedSample != null) itemInstance = __instance.consumedSample;

                productItemInstance = itemInstance.TryCast<ProductItemInstance>();
                
                product = productItemInstance.Definition.TryCast<ProductDefinition>();
                int propertyMatchScore = 0;
                int totalProperties = __instance.customerData.PreferredProperties.Count;
                int matchedProperties = 0;
                for (int i = 0; i < totalProperties; i++)
                {
                    Property preferredProperty = __instance.customerData.PreferredProperties[i];
                    if (product.Properties.Contains(preferredProperty))
                    {
                        matchedProperties++;
                    }
                }
                if (totalProperties > 0)
                {
                    switch (matchedProperties)
                    {
                        case 1:
                            __result += Config.OneEffect;
                            break;
                        case 2:
                            __result += Config.TwoEffect;
                            break;
                        case 3:
                            __result += Config.ThreeEffect;
                            break;
                    }

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return true;
            }
        }
    }
}