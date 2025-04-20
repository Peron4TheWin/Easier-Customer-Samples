using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using MelonLoader;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Properties;
using Il2CppScheduleOne.UI.Handover;
using Il2CppSystem.Reflection;
using ModManagerPhoneApp;

[assembly: MelonInfo(typeof(EasierCustomerSamples.EntryPoint), "EasierCustomerSamples", "1.0.0", "_peron")]

namespace EasierCustomerSamples
{
    public class EntryPoint : MelonMod
    {
        public static MelonPreferences_Category category;
        public static MelonPreferences_Entry<bool> alwaysGuarantee;
        public static MelonPreferences_Entry<bool> allowUnpacked;
        public static MelonPreferences_Entry<float> baseValue;
        public static MelonPreferences_Entry<float> oneEffect;
        public static MelonPreferences_Entry<float> twoEffect;
        public static MelonPreferences_Entry<float> threeEffect;
        public override void OnInitializeMelon()
        {
            
            category = MelonPreferences.CreateCategory("EasierCustomerSamples_Settings", "Easier Customer Samples Settings");
            alwaysGuarantee = category.CreateEntry("AlwaysGuarantee", false, "Always guarantee sample success");
            allowUnpacked = category.CreateEntry("AllowUnpacked", false, "Allow unpacked items to be sampled");
            baseValue = category.CreateEntry("BaseValue", 0.7f, "Base sample success chance");
            oneEffect = category.CreateEntry("OneEffect", 0.1f, "Sample success chance for 1 property match");
            twoEffect = category.CreateEntry("TwoEffect", 0.2f, "Sample success chance for 2 property matches");
            threeEffect = category.CreateEntry("ThreeEffect", 0.3f, "Sample success chance for 3 property matches");
            category.SetFilePath("UserData/EasierCustomerSamples.cfg");
            MelonPreferences.Save();
            try
            {
                ModSettingsEvents.OnPreferencesSaved += ConfigLoads;
                LoggerInstance.Msg("Successfully subscribed to Mod Manager save event.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not subscribe to Mod Manager event (Mod Manager may not be installed/compatible): {ex.Message}");
            }
        }

        public static void ConfigLoads()
        {
            category.LoadFromFile();
        }

        [HarmonyPatch(typeof(Il2CppScheduleOne.PlayerScripts.Player))]
        [HarmonyPatch("PlayerLoaded")]
        class ConfigLoad
        {
            [HarmonyPostfix]
            static void PostfixPlayerLoaded(Il2CppScheduleOne.PlayerScripts.Player __instance)
            {
                ConfigLoads();
            }
        }


        [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Handover.HandoverScreen))]
        class AllowUnpacked
        {
            [HarmonyPatch(nameof(Il2CppScheduleOne.UI.Handover.HandoverScreen.GetError))]
            [HarmonyPrefix]
            public static bool GetErrorPrefix(Il2CppScheduleOne.UI.Handover.HandoverScreen __instance)
            {
                if ( !(bool)allowUnpacked.BoxedValue) return true;
                if ((__instance.Mode == HandoverScreen.EMode.Sample || __instance.Mode == HandoverScreen.EMode.Offer) && __instance.GetCustomerItemsCount(true) == 0)
                {
                    
                    foreach (ItemSlot itemSlot in __instance.CustomerSlots)
                    {
                        if (itemSlot.ItemInstance == null) return true;
                        ProductItemInstance? productItemInstance = itemSlot.ItemInstance.TryCast<ProductItemInstance>();
                        if (productItemInstance == null) return true;
                        if (productItemInstance.AppliedPackaging == null)
                        {
                            return false;
                        }
                    }
                }
                return true;
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
                    
                    if ((bool)alwaysGuarantee.BoxedValue)
                    {
                        __result = 1f;
                        return false;
                    }

                    __result = (float)baseValue.BoxedValue;
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
                                __result += (float)oneEffect.BoxedValue;
                                break;
                            case 2:
                                __result += (float)twoEffect.BoxedValue;
                                break;
                            case 3:
                                __result += (float)threeEffect.BoxedValue;
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
}