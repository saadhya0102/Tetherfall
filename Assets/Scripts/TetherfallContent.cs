using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum WeaponDiscipline
    {
        Pathblade,
        Riftbow,
        Threadstaff
    }

    public enum CrossweaveAffinity
    {
        Anchor,
        Sight,
        Veil
    }

    public enum EquipmentSlot
    {
        Weapon,
        Coat,
        Charm,
        Tool
    }

    public enum GamePanel
    {
        None,
        FieldPack,
        Crafting,
        Vendor,
        QuestLog,
        Camp,
        Restoration,
        CinderResolution,
        CityBoard,
        Threadways,
        SkillTree,
        Settings
    }

    public enum DungeonState
    {
        Locked,
        Exploring,
        Swarm,
        Boss,
        Complete
    }

    public enum TrialState
    {
        Locked,
        AligningFlows,
        Surge,
        Boss,
        Complete
    }

    public enum BossActionKind
    {
        Sweep,
        ShardVolley,
        Threadquake,
        RailCharge,
        Cinderfall
    }

    public enum CityResidentService
    {
        Dialogue,
        NoticeBoard,
        Inn,
        Archive
    }

    public enum CityActivityKind
    {
        Delivery,
        Training,
        Rest,
        Archive
    }

    public enum BountyDifficulty
    {
        Notable,
        Dangerous,
        Apex
    }

    public enum ExpeditionDirective
    {
        Trail,
        Focus,
        Bulwark
    }

    public enum ThreadlinkSeal
    {
        None,
        Sever,
        Latch,
        Aegis
    }

    public enum MealEffect
    {
        Restore,
        Swift,
        Ward
    }

    [Serializable]
    public sealed class ResourceCost
    {
        public ResourceKind kind;
        public int amount;

        public ResourceCost(ResourceKind resourceKind, int resourceAmount)
        {
            kind = resourceKind;
            amount = resourceAmount;
        }
    }

    [Serializable]
    public sealed class EquipmentDefinition
    {
        public string id;
        public string displayName;
        public EquipmentSlot slot;
        public string description;
        public WeaponDiscipline discipline;
        public int damageBonus;
        public int maxHealthBonus;
        public float moveSpeedBonus;
        public int guardBonus;
        public float staminaBonus;
        public float staminaRegenerationBonus;
        public float windLiftVelocityBonus;
        public int favorPrice;
        public bool restorationRewardOnly;
        public List<ResourceCost> craftCost = new List<ResourceCost>();
    }

    [Serializable]
    public sealed class CrossweaveDefinition
    {
        public WeaponDiscipline weapon;
        public CrossweaveAffinity affinity;
        public string displayName;
        public string cue;
        public Color color;
    }

    [Serializable]
    public sealed class BossDefinition
    {
        public string id;
        public string displayName;
        public int maxHealth;
        public Color baseColor;
        public Color phaseColor;
        public Color labelColor;
        public float phaseThresholdOne;
        public float phaseThresholdTwo;
        public float phaseOneMoveSpeed;
        public float phaseTwoMoveSpeed;
        public string summonedEnemyId;
        public List<BossActionKind> phaseOneActions = new List<BossActionKind>();
        public List<BossActionKind> phaseTwoActions = new List<BossActionKind>();
    }

    [Serializable]
    public sealed class TrialDefinition
    {
        public string id;
        public string displayName;
        public Vector3 gatePosition;
        public Vector3 entrancePosition;
        public Vector3 returnPosition;
        public string bossId;
        public string rewardEquipmentId;
        public Color accentColor;
    }

    [Serializable]
    public sealed class CityDefinition
    {
        public string id;
        public string displayName;
        public string subtitle;
        public Vector3 center;
        public Color accentColor;
        public List<CityNpcDefinition> residents = new List<CityNpcDefinition>();
        public List<CityActivityDefinition> activities = new List<CityActivityDefinition>();
    }

    [Serializable]
    public sealed class CityNpcDefinition
    {
        public string id;
        public string displayName;
        public string role;
        public Vector3 position;
        public Color color;
        public CityResidentService service;
        public string dialogue;
        public float wanderRadius;
    }

    [Serializable]
    public sealed class CityActivityDefinition
    {
        public string id;
        public string title;
        public string description;
        public CityActivityKind kind;
        public List<ResourceCost> cost = new List<ResourceCost>();
        public string[] enemyIds = System.Array.Empty<string>();
        public Vector3 arenaPosition;
        public int favorReward;
        public int experienceReward;
        public float cooldownSeconds;
    }

    [Serializable]
    public sealed class BountyDefinition
    {
        public string id;
        public string title;
        public string description;
        public string sponsorCityId;
        public string targetEnemyId;
        public Vector3 position;
        public BountyDifficulty difficulty;
        public int favorReward;
        public int experienceReward;
        public List<ResourceCost> materialReward = new List<ResourceCost>();
        public float cooldownSeconds;
        public Color accentColor;
        public string phaseTitle;
        public string[] reinforcementEnemyIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ThreadwayDefinition
    {
        public string id;
        public string title;
        public string region;
        public string description;
        public Vector3 position;
        public Vector3 arrivalPosition;
        public int activationChimesaltCost;
        public Color accentColor;
    }

    [Serializable]
    public sealed class RestorationTierDefinition
    {
        public int tier;
        public string title;
        public string description;
        public List<ResourceCost> cost = new List<ResourceCost>();
        public bool isManual = true;
        public string requiredRouteEventId;
        public int experienceReward;
        public int favorReward;
        public string rewardEquipmentId;
    }

    [Serializable]
    public sealed class RestorationSiteDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public Vector3 position;
        public Color accentColor;
        public int maxTier;
        public List<RestorationTierDefinition> tiers = new List<RestorationTierDefinition>();
    }

    [Serializable]
    public sealed class MealDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public int minimumCampTier;
        public int recipeUnlockFavorPrice;
        public List<ResourceCost> ingredients = new List<ResourceCost>();
        public int vitalityRestore;
        public float staminaRestore;
        public MealEffect effect;
        public float effectDurationSeconds;
    }

    [Serializable]
    public sealed class VendorSupplyDefinition
    {
        public ResourceKind kind;
        public int amount;
        public int favorPrice;
    }

    [Serializable]
    public sealed class VendorProfileDefinition
    {
        public string id;
        public string displayName;
        public string serviceDescription;
        public List<string> equipmentIds = new List<string>();
        public List<string> recipeIds = new List<string>();
        public List<VendorSupplyDefinition> supplies = new List<VendorSupplyDefinition>();
    }

    [Serializable]
    public sealed class CampContractDefinition
    {
        public string id;
        public string title;
        public string description;
        public int minimumCampTier;
        public List<ResourceCost> delivery = new List<ResourceCost>();
        public int favorReward;
    }

    [Serializable]
    public sealed class QuestRecord
    {
        public string id;
        public string title;
        public string objective;
        public int current;
        public int required;
        public bool completed;
        public int favorReward;

        public bool IsComplete => current >= required;
    }

    public interface IExpeditionDamageable
    {
        bool IsDefeated { get; }
        void TakeDamage(int amount, string source);
    }
}
