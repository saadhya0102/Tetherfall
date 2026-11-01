using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private const int MealWardGuardBonusValue = 5;
        private const float MealSwiftMoveSpeedBonusValue = 0.18f;
        private const float MealUseCooldownSeconds = 2.5f;

        private readonly Dictionary<string, int> packedMeals = new Dictionary<string, int>();
        private readonly HashSet<string> knownRecipeIds = new HashSet<string>();
        private readonly HashSet<string> completedCampContractIds = new HashSet<string>();
        private string activeMealId = ProvisioningCatalog.BrightbrothId;
        private MealEffect activeMealEffect;
        private long activeMealEffectEndsUtcTicks;

        public int MealGuardBonus =>
            IsMealEffectActive(MealEffect.Ward) ? MealWardGuardBonusValue : 0;
        public float MealMoveSpeedBonus =>
            IsMealEffectActive(MealEffect.Swift) ? MealSwiftMoveSpeedBonusValue : 0f;

        public string GetMealEffectStatus()
        {
            var remainingTicks = activeMealEffectEndsUtcTicks - DateTime.UtcNow.Ticks;
            if (!IsTimedMealEffect(activeMealEffect) || remainingTicks <= 0L)
            {
                return string.Empty;
            }

            var remaining = (int)Math.Min(
                int.MaxValue,
                Math.Max(1d, Math.Ceiling(TimeSpan.FromTicks(remainingTicks).TotalSeconds)));
            return activeMealEffect switch
            {
                MealEffect.Swift =>
                    $"Swift +{Mathf.RoundToInt(MealSwiftMoveSpeedBonusValue * 100f)}% speed {remaining}s",
                MealEffect.Ward => $"Ward +{MealWardGuardBonusValue} guard {remaining}s",
                _ => string.Empty
            };
        }

        private void InitializeProvisioning()
        {
            knownRecipeIds.Add(ProvisioningCatalog.BrightbrothId);
        }

        private bool IsMealEffectActive(MealEffect effect)
        {
            return IsTimedMealEffect(effect) &&
                   activeMealEffect == effect &&
                   activeMealEffectEndsUtcTicks > DateTime.UtcNow.Ticks;
        }

        private static bool IsTimedMealEffect(MealEffect effect)
        {
            return effect == MealEffect.Swift || effect == MealEffect.Ward;
        }

        private int GetMealAmount(string mealId)
        {
            return !string.IsNullOrEmpty(mealId) &&
                   packedMeals.TryGetValue(mealId, out var amount)
                ? Mathf.Max(0, amount)
                : 0;
        }

        private bool IsRecipeKnown(string mealId)
        {
            return !string.IsNullOrEmpty(mealId) && knownRecipeIds.Contains(mealId);
        }

        private MealDefinition FindFirstPackedMeal()
        {
            foreach (var meal in ProvisioningCatalog.Meals)
            {
                if (meal != null && GetMealAmount(meal.id) > 0)
                {
                    return meal;
                }
            }

            return null;
        }

        private void ReconcileActiveMealSelection()
        {
            var selectedMeal = ProvisioningCatalog.GetMeal(activeMealId);
            if (selectedMeal != null && GetMealAmount(selectedMeal.id) > 0)
            {
                return;
            }

            var replacement = FindFirstPackedMeal();
            activeMealId = replacement != null
                ? replacement.id
                : ProvisioningCatalog.BrightbrothId;
        }

        private void SelectMeal(string mealId)
        {
            var meal = ProvisioningCatalog.GetMeal(mealId);
            if (meal == null)
            {
                Notify("That provision is no longer available.");
                return;
            }

            if (!IsRecipeKnown(meal.id))
            {
                Notify($"{meal.displayName} is not recorded in the field notebook.");
                return;
            }

            var amount = GetMealAmount(meal.id);
            if (amount <= 0)
            {
                Notify($"No {meal.displayName} is packed. Prepare it at a camp kitchen first.");
                return;
            }

            if (activeMealId == meal.id)
            {
                Notify($"{meal.displayName} is already selected ({amount} packed).");
                return;
            }

            activeMealId = meal.id;
            Notify($"{meal.displayName} selected ({amount} packed). Press X in the field to use it.");
            RequestSave();
        }

        private void TryCookRecipe(string mealId)
        {
            var meal = ProvisioningCatalog.GetMeal(mealId);
            if (meal == null)
            {
                Notify("That provision recipe is unavailable.");
                return;
            }

            if (!IsRecipeKnown(meal.id))
            {
                Notify("That recipe is not in your field notebook.");
                return;
            }

            if (ActiveCamp == null)
            {
                Notify("Set up a portable camp before preparing provisions.");
                return;
            }

            if (campTier < meal.minimumCampTier)
            {
                Notify($"Camp level {meal.minimumCampTier + 1} is needed for {meal.displayName}.");
                return;
            }

            if (meal.ingredients == null || meal.ingredients.Count == 0)
            {
                Notify($"{meal.displayName} has no valid preparation list.");
                return;
            }

            if (!CanAfford(meal.ingredients))
            {
                Notify($"Need {DescribeMissingCost(meal.ingredients)} to prepare {meal.displayName}.");
                return;
            }

            var packedAmount = GetMealAmount(meal.id);
            if (packedAmount == int.MaxValue)
            {
                Notify($"The field pack cannot hold another {meal.displayName}.");
                return;
            }

            SpendCost(meal.ingredients);
            packedMeals[meal.id] = packedAmount + 1;
            activeMealId = meal.id;
            cookedMeals = GetMealAmount(ProvisioningCatalog.BrightbrothId);
            var experienceReward = GetCookingExperienceReward(meal);
            GainExperience(experienceReward);
            TetherfallWorldInteractionFeedback.PlayCooking(
                this,
                ActiveCamp.transform,
                meal.effect);
            Notify(
                $"{meal.displayName} packed and selected ({packedMeals[meal.id]} ready). " +
                $"+{experienceReward} XP. Press X in the field to use it.");
            RequestSave();
        }

        private void TryUseActiveMeal()
        {
            ReconcileActiveMealSelection();
            var meal = ProvisioningCatalog.GetMeal(activeMealId);
            if (meal == null || GetMealAmount(meal.id) <= 0)
            {
                Notify("No provisions are packed. Prepare one at a camp kitchen.");
                return;
            }

            if (Time.time < nextMealUseAt)
            {
                Notify($"Meal kit is ready in {Mathf.CeilToInt(nextMealUseAt - Time.time)} seconds.");
                return;
            }

            if (Health >= MaxHealth &&
                Stamina >= MaxStamina &&
                (meal.effect == MealEffect.Restore || IsMealEffectActive(meal.effect)))
            {
                Notify("Save that provision until its field benefit would matter.");
                return;
            }

            var remainingAmount = GetMealAmount(meal.id) - 1;
            if (remainingAmount > 0)
            {
                packedMeals[meal.id] = remainingAmount;
            }
            else
            {
                packedMeals.Remove(meal.id);
            }

            nextMealUseAt = Time.time + MealUseCooldownSeconds;
            HealPlayer(meal.vitalityRestore);
            RestoreStamina(meal.staminaRestore);
            if (IsTimedMealEffect(meal.effect) && meal.effectDurationSeconds > 0f)
            {
                activeMealEffect = meal.effect;
                activeMealEffectEndsUtcTicks = DateTime.UtcNow.AddSeconds(meal.effectDurationSeconds).Ticks;
            }

            cookedMeals = GetMealAmount(ProvisioningCatalog.BrightbrothId);
            ReconcileActiveMealSelection();
            var message = GetMealUseMessage(meal);
            if (remainingAmount > 0)
            {
                message += $" {remainingAmount} packed.";
            }
            else
            {
                var nextMeal = ProvisioningCatalog.GetMeal(activeMealId);
                message += nextMeal != null && GetMealAmount(nextMeal.id) > 0
                    ? $" {nextMeal.displayName} selected next ({GetMealAmount(nextMeal.id)} packed)."
                    : " The field pack is now out of provisions.";
            }

            Notify(message);
            RequestSave();
        }

        private static string GetMealUseMessage(MealDefinition meal)
        {
            return $"{meal.displayName} consumed: {DescribeMealBenefits(meal)}.";
        }

        private static string DescribeMealBenefits(MealDefinition meal)
        {
            if (meal == null)
            {
                return "no field benefit";
            }

            var parts = new List<string>();
            if (meal.vitalityRestore > 0)
            {
                parts.Add($"up to {meal.vitalityRestore} vitality");
            }

            if (meal.staminaRestore > 0f)
            {
                parts.Add($"up to {meal.staminaRestore:0.#} stamina");
            }

            if (meal.effect == MealEffect.Swift && meal.effectDurationSeconds > 0f)
            {
                parts.Add(
                    $"+{Mathf.RoundToInt(MealSwiftMoveSpeedBonusValue * 100f)}% route speed " +
                    $"for {Mathf.CeilToInt(meal.effectDurationSeconds)}s");
            }
            else if (meal.effect == MealEffect.Ward && meal.effectDurationSeconds > 0f)
            {
                parts.Add(
                    $"+{MealWardGuardBonusValue} guard " +
                    $"for {Mathf.CeilToInt(meal.effectDurationSeconds)}s");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "no field benefit";
        }

        private static int GetCookingExperienceReward(MealDefinition meal)
        {
            if (meal == null)
            {
                return 0;
            }

            var ingredientUnits = 0;
            if (meal.ingredients != null)
            {
                foreach (var ingredient in meal.ingredients)
                {
                    if (ingredient != null)
                    {
                        ingredientUnits += Mathf.Max(0, ingredient.amount);
                    }
                }
            }

            return Mathf.Clamp(
                10 + Mathf.Max(1, meal.minimumCampTier) * 4 + ingredientUnits * 2,
                12,
                40);
        }

        private void TryBuyRecipe(string mealId)
        {
            var meal = ProvisioningCatalog.GetMeal(mealId);
            var profile = activeVendor != null ? ProvisioningCatalog.GetVendor(activeVendor.VendorId) : null;
            if (meal == null ||
                profile == null ||
                profile.recipeIds == null ||
                !profile.recipeIds.Contains(mealId))
            {
                Notify("This vendor cannot teach that recipe.");
                return;
            }

            if (IsRecipeKnown(mealId))
            {
                Notify($"{meal.displayName} is already recorded in the field notebook.");
                return;
            }

            if (favors < meal.recipeUnlockFavorPrice)
            {
                Notify($"Need {meal.recipeUnlockFavorPrice - favors} more Favors.");
                return;
            }

            favors -= meal.recipeUnlockFavorPrice;
            knownRecipeIds.Add(mealId);
            Notify($"{meal.displayName} recipe added to the field notebook.");
            RequestSave();
        }

        private void TryBuyVendorSupply(VendorSupplyDefinition supply)
        {
            if (activeVendor == null || supply == null)
            {
                Notify("No vendor supply is selected.");
                return;
            }

            var profile = ProvisioningCatalog.GetVendor(activeVendor.VendorId);
            if (profile == null ||
                profile.supplies == null ||
                !profile.supplies.Contains(supply) ||
                supply.amount <= 0 ||
                supply.favorPrice < 0)
            {
                Notify("That supply is unavailable here.");
                return;
            }

            TryBuyResource(supply.kind, supply.amount, supply.favorPrice);
        }

        private void TryFulfillCampContract(CampContractDefinition contract)
        {
            if (contract == null)
            {
                Notify("No camp contract is selected.");
                return;
            }

            contract = FindCampContract(contract.id);
            if (contract == null)
            {
                Notify("That camp contract is no longer available.");
                return;
            }

            if (completedCampContractIds.Contains(contract.id))
            {
                Notify($"{contract.title} was already sent and its reward was claimed.");
                return;
            }

            if (ActiveCamp == null || campTier < contract.minimumCampTier)
            {
                Notify($"Camp level {contract.minimumCampTier + 1} is needed to fulfill this contract.");
                return;
            }

            if (contract.delivery == null || contract.delivery.Count == 0)
            {
                Notify($"{contract.title} has no valid delivery manifest.");
                return;
            }

            if (!CanAfford(contract.delivery))
            {
                Notify($"Contract needs {DescribeMissingCost(contract.delivery)}.");
                return;
            }

            if (!completedCampContractIds.Add(contract.id))
            {
                Notify($"{contract.title} was already sent and its reward was claimed.");
                return;
            }

            SpendCost(contract.delivery);
            AddFavors(contract.favorReward, contract.title);
            var experienceReward = GetContractExperienceReward(contract);
            GainExperience(experienceReward);
            TetherfallWorldInteractionFeedback.PlayCampContract(
                this,
                ActiveCamp.transform,
                PlayerTransform);
            Notify(
                $"{contract.title} dispatched. Reward claimed: " +
                $"{contract.favorReward} Favors and {experienceReward} XP.");
            RequestSave();
        }

        private static CampContractDefinition FindCampContract(string contractId)
        {
            if (string.IsNullOrEmpty(contractId))
            {
                return null;
            }

            foreach (var contract in ProvisioningCatalog.CampContracts)
            {
                if (contract != null && contract.id == contractId)
                {
                    return contract;
                }
            }

            return null;
        }

        private static int GetContractExperienceReward(CampContractDefinition contract)
        {
            return contract == null
                ? 0
                : Mathf.Clamp(20 + Mathf.Max(0, contract.favorReward), 35, 60);
        }

        private void WriteProvisioningSave(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            knownRecipeIds.Add(ProvisioningCatalog.BrightbrothId);
            ReconcileActiveMealSelection();
            ReconcileMealEffect(DateTime.UtcNow.Ticks);

            if (data.packedMeals == null)
            {
                data.packedMeals = new List<MealStackSaveData>();
            }
            else
            {
                data.packedMeals.Clear();
            }

            foreach (var meal in ProvisioningCatalog.Meals)
            {
                if (meal == null)
                {
                    continue;
                }

                var amount = GetMealAmount(meal.id);
                if (amount <= 0)
                {
                    continue;
                }

                data.packedMeals.Add(new MealStackSaveData
                {
                    mealId = meal.id,
                    amount = amount
                });
            }

            if (data.knownRecipeIds == null)
            {
                data.knownRecipeIds = new List<string>();
            }
            else
            {
                data.knownRecipeIds.Clear();
            }

            foreach (var meal in ProvisioningCatalog.Meals)
            {
                if (meal != null && IsRecipeKnown(meal.id))
                {
                    data.knownRecipeIds.Add(meal.id);
                }
            }

            if (data.completedCampContractIds == null)
            {
                data.completedCampContractIds = new List<string>();
            }
            else
            {
                data.completedCampContractIds.Clear();
            }

            foreach (var contract in ProvisioningCatalog.CampContracts)
            {
                if (contract != null && completedCampContractIds.Contains(contract.id))
                {
                    data.completedCampContractIds.Add(contract.id);
                }
            }

            data.activeMealId = activeMealId;
            data.activeMealEffect = activeMealEffect;
            data.activeMealEffectEndsUtcTicks = activeMealEffectEndsUtcTicks;
            data.cookedMeals = GetMealAmount(ProvisioningCatalog.BrightbrothId);
        }

        private void RestoreProvisioningState(TetherfallSaveData data)
        {
            packedMeals.Clear();
            if (data != null && data.packedMeals != null)
            {
                foreach (var stack in data.packedMeals)
                {
                    if (stack == null ||
                        ProvisioningCatalog.GetMeal(stack.mealId) == null ||
                        stack.amount <= 0)
                    {
                        continue;
                    }

                    var reconciledAmount = Math.Min(
                        int.MaxValue,
                        (long)GetMealAmount(stack.mealId) + stack.amount);
                    packedMeals[stack.mealId] = (int)reconciledAmount;
                }
            }

            if (data != null && data.cookedMeals > 0)
            {
                packedMeals[ProvisioningCatalog.BrightbrothId] = Mathf.Max(
                    GetMealAmount(ProvisioningCatalog.BrightbrothId),
                    data.cookedMeals);
            }

            knownRecipeIds.Clear();
            knownRecipeIds.Add(ProvisioningCatalog.BrightbrothId);
            if (data != null && data.knownRecipeIds != null)
            {
                foreach (var recipeId in data.knownRecipeIds)
                {
                    if (ProvisioningCatalog.GetMeal(recipeId) != null)
                    {
                        knownRecipeIds.Add(recipeId);
                    }
                }
            }

            foreach (var packedMealId in packedMeals.Keys)
            {
                knownRecipeIds.Add(packedMealId);
            }

            completedCampContractIds.Clear();
            if (data != null && data.completedCampContractIds != null)
            {
                foreach (var contractId in data.completedCampContractIds)
                {
                    var contract = FindCampContract(contractId);
                    if (contract != null)
                    {
                        completedCampContractIds.Add(contract.id);
                        campTier = Mathf.Max(
                            campTier,
                            Mathf.Clamp(contract.minimumCampTier, 0, 2));
                    }
                }
            }

            activeMealId = data != null && ProvisioningCatalog.GetMeal(data.activeMealId) != null
                ? data.activeMealId
                : ProvisioningCatalog.BrightbrothId;
            ReconcileActiveMealSelection();

            activeMealEffect = data != null ? data.activeMealEffect : MealEffect.Restore;
            activeMealEffectEndsUtcTicks = data != null
                ? Math.Max(0L, data.activeMealEffectEndsUtcTicks)
                : 0L;
            ReconcileMealEffect(DateTime.UtcNow.Ticks);
            cookedMeals = GetMealAmount(ProvisioningCatalog.BrightbrothId);
        }

        private void ReconcileMealEffect(long nowUtcTicks)
        {
            var maximumDurationSeconds = GetMaximumMealEffectDuration(activeMealEffect);
            if (maximumDurationSeconds <= 0f || activeMealEffectEndsUtcTicks <= nowUtcTicks)
            {
                activeMealEffect = MealEffect.Restore;
                activeMealEffectEndsUtcTicks = 0L;
                return;
            }

            var maximumEndTicks = nowUtcTicks + TimeSpan.FromSeconds(maximumDurationSeconds).Ticks;
            activeMealEffectEndsUtcTicks = Math.Min(activeMealEffectEndsUtcTicks, maximumEndTicks);
        }

        private static float GetMaximumMealEffectDuration(MealEffect effect)
        {
            if (!IsTimedMealEffect(effect))
            {
                return 0f;
            }

            var maximumDuration = 0f;
            foreach (var meal in ProvisioningCatalog.Meals)
            {
                if (meal != null && meal.effect == effect)
                {
                    maximumDuration = Mathf.Max(maximumDuration, meal.effectDurationSeconds);
                }
            }

            return maximumDuration;
        }

        private void DrawProvisioningVendor(Rect rect)
        {
            var profile = activeVendor != null ? ProvisioningCatalog.GetVendor(activeVendor.VendorId) : null;
            if (profile == null)
            {
                GUI.Label(new Rect(rect.x + 26f, rect.y + 70f, 680f, 30f), "This vendor's route stock is still being unpacked.", bodyStyle);
                return;
            }

            GUI.Label(new Rect(rect.x + 26f, rect.y + 70f, 650f, 38f), profile.serviceDescription, bodyStyle);
            GUI.Label(new Rect(rect.x + 610f, rect.y + 70f, 130f, 24f), $"Favors {favors}", headingStyle);
            var y = rect.y + 120f;

            foreach (var equipmentId in profile.equipmentIds)
            {
                var equipment = equipmentCatalog.Find(item => item.id == equipmentId);
                if (equipment == null)
                {
                    continue;
                }

                if (GUI.Button(
                        new Rect(rect.x + 26f, y, 132f, 34f),
                        unlockedEquipment.Contains(equipment.id) ? "Owned" : $"Buy {equipment.favorPrice}"))
                {
                    TryBuy(equipment);
                }

                GUI.Label(new Rect(rect.x + 176f, y, 510f, 38f), $"{equipment.displayName}: {equipment.description}", bodyStyle);
                y += 52f;
            }

            foreach (var recipeId in profile.recipeIds)
            {
                var meal = ProvisioningCatalog.GetMeal(recipeId);
                if (meal == null)
                {
                    continue;
                }

                if (GUI.Button(
                        new Rect(rect.x + 26f, y, 132f, 34f),
                        IsRecipeKnown(meal.id) ? "Learned" : $"Learn {meal.recipeUnlockFavorPrice}"))
                {
                    TryBuyRecipe(meal.id);
                }

                GUI.Label(
                    new Rect(rect.x + 176f, y, 510f, 42f),
                    $"{meal.displayName}: {meal.description}\n{DescribeMealBenefits(meal)}",
                    bodyStyle);
                y += 52f;
            }

            GUI.Label(new Rect(rect.x + 26f, rect.y + 318f, 400f, 24f), "Regional supplies", headingStyle);
            var supplyY = rect.y + 352f;
            for (var index = 0; index < profile.supplies.Count; index++)
            {
                var supply = profile.supplies[index];
                var x = rect.x + 26f + index * 215f;
                if (GUI.Button(
                        new Rect(x, supplyY, 198f, 34f),
                        $"{supply.amount} {ResourceLabel(supply.kind)}   {supply.favorPrice} Favors"))
                {
                    TryBuyVendorSupply(supply);
                }
            }

            GUI.Label(new Rect(rect.x + 26f, rect.y + 428f, 680f, 50f), activeVendor.Greeting, mutedStyle);
        }

        private void DrawProvisioningCamp(Rect rect)
        {
            var kitchenState = campTier >= 1
                ? "Kitchen ready"
                : "Kitchen unlocks at level 2";
            var openContracts = 0;
            foreach (var contract in ProvisioningCatalog.CampContracts)
            {
                if (contract != null && !completedCampContractIds.Contains(contract.id))
                {
                    openContracts++;
                }
            }

            var contractState = campTier < 2
                ? "contracts unlock at level 3"
                : openContracts > 0
                    ? $"{openContracts} contract{(openContracts == 1 ? string.Empty : "s")} ready"
                    : "all contracts sent";
            GUI.Label(
                new Rect(rect.x + 26f, rect.y + 70f, 670f, 30f),
                $"Camp level {campTier + 1}. {kitchenState}; {contractState}.",
                bodyStyle);
            if (GUI.Button(new Rect(rect.x + 26f, rect.y + 108f, 180f, 36f), "Rest until ready"))
            {
                ActiveCamp?.Rest(this);
            }

            GUI.Label(new Rect(rect.x + 224f, rect.y + 113f, 420f, 26f), "Restores vitality and field stamina.", mutedStyle);
            GUI.Label(new Rect(rect.x + 26f, rect.y + 162f, 300f, 24f), "Camp kitchen", headingStyle);
            var recipeY = rect.y + 194f;
            foreach (var meal in ProvisioningCatalog.Meals)
            {
                if (!IsRecipeKnown(meal.id))
                {
                    continue;
                }

                var buttonText = campTier < meal.minimumCampTier
                    ? $"Need camp {meal.minimumCampTier + 1}"
                    : $"Cook {meal.displayName}";
                if (GUI.Button(new Rect(rect.x + 26f, recipeY, 180f, 32f), buttonText))
                {
                    TryCookRecipe(meal.id);
                }

                GUI.Label(
                    new Rect(rect.x + 222f, recipeY, 500f, 38f),
                    $"{DescribeCost(meal.ingredients)} • {DescribeMealBenefits(meal)}\n" +
                    $"{GetMealAmount(meal.id)} packed • {GetCookingExperienceReward(meal)} XP per batch",
                    mutedStyle);
                recipeY += 42f;
            }

            if (GUI.Button(
                    new Rect(rect.x + 26f, rect.y + 335f, 180f, 36f),
                    campTier >= 2 ? "Camp fully upgraded" : "Upgrade camp"))
            {
                TryUpgradeCamp();
            }

            GUI.Label(new Rect(rect.x + 224f, rect.y + 340f, 470f, 28f), "Upgrades create permanent camp services.", mutedStyle);
            var contractY = rect.y + 390f;
            foreach (var contract in ProvisioningCatalog.CampContracts)
            {
                var completed = completedCampContractIds.Contains(contract.id);
                var label = completed
                    ? $"{contract.title}: sent"
                    : campTier < contract.minimumCampTier
                        ? $"Need camp {contract.minimumCampTier + 1}"
                        : contract.title;
                if (GUI.Button(new Rect(rect.x + 26f, contractY, 200f, 36f), label))
                {
                    TryFulfillCampContract(contract);
                }

                var experienceReward = GetContractExperienceReward(contract);
                var contractDetails = completed
                    ? $"Reward claimed: {contract.favorReward} Favors + {experienceReward} XP"
                    : campTier < contract.minimumCampTier
                        ? $"{contract.description}\nUnlocks at camp level {contract.minimumCampTier + 1}"
                        : !CanAfford(contract.delivery)
                            ? $"{contract.description}\nMissing {DescribeMissingCost(contract.delivery)} • Reward {contract.favorReward} Favors + {experienceReward} XP"
                            : $"{contract.description}\n{DescribeCost(contract.delivery)} → {contract.favorReward} Favors + {experienceReward} XP";
                GUI.Label(
                    new Rect(rect.x + 242f, contractY, 490f, 38f),
                    contractDetails,
                    mutedStyle);
                contractY += 40f;
            }
        }

        private void DrawPackedMealSelection(Rect rect)
        {
            var totalPacked = 0L;
            foreach (var amount in packedMeals.Values)
            {
                totalPacked = Math.Min(int.MaxValue, totalPacked + Mathf.Max(0, amount));
            }

            GUI.Label(
                new Rect(rect.x + 26f, rect.yMax - 72f, 180f, 22f),
                $"Packed provisions {totalPacked}",
                headingStyle);
            var x = rect.x + 190f;
            foreach (var meal in ProvisioningCatalog.Meals)
            {
                if (!IsRecipeKnown(meal.id))
                {
                    continue;
                }

                var amount = GetMealAmount(meal.id);
                var selected = activeMealId == meal.id && amount > 0;
                if (GUI.Button(
                        new Rect(x, rect.yMax - 77f, 155f, 30f),
                        $"{(selected ? "• " : string.Empty)}{meal.displayName} [{amount}]"))
                {
                    SelectMeal(meal.id);
                }

                x += 165f;
            }
        }
    }
}
