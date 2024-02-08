using BepInEx.Configuration;
using EcsRx.Entities;
using EcsRx.Extensions;
using EcsRx.Unity.Extensions;
using Gulong_plugin;
using HarmonyLib;
using Heluo;
using Heluo.Actor;
using Heluo.Battle;
using Heluo.Controller;
using Heluo.Data;
using Heluo.Features;
using Heluo.Flow;
using Heluo.FSM.Battle;
using Heluo.FSM.Player;
using Heluo.Manager;
using Heluo.Resource;
using Heluo.UI;
using Heluo.Utility;
using Ninject;
using Ninject.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace PathOfWuxia
{
    [System.ComponentModel.DisplayName("基础设置")]
    [System.ComponentModel.Description("基础设置")]
    public class HookGeneral : IHook
    {
        static ConfigEntry<bool> learnSkillNotEatBook;
        static ConfigEntry<bool> allMemberGetExp;
        static ConfigEntry<int> maxBattleAvatar;
        static ConfigEntry<bool> unlockCamera;
        static ConfigEntry<bool> showGiftFavorite;
        static ConfigEntry<string> playerSurname;
        static ConfigEntry<string> playerName;
        static ConfigEntry<int> addskillColumnNum;
        static ConfigEntry<float> gameSpeed;
        static ConfigEntry<KeyCode> speedKey;
        static ConfigEntry<float> playerMoveSpeed;
        static ConfigEntry<bool> ShowRepluseTick;
        static ConfigEntry<bool> onePunch;
        static ConfigEntry<bool> autoBattle;
        static ConfigEntry<int> saveNum;
        static ConfigEntry<bool> modSupport;
        static ConfigEntry<bool> unlimitRadarRange;
        static ConfigEntry<bool> showCoolDownTime;
        static ConfigEntry<bool> rightClickCloseUI;
        static EventHandler ReplacePlayerAvatarEventHander;

        static bool speedOn = false;
        public void OnRegister(GulongPlugin plugin)
        {
            learnSkillNotEatBook = plugin.Config.Bind("游戏设定", "学技能不吃书", false, "学技能后技能书不消失");
            allMemberGetExp = plugin.Config.Bind("战斗设定", "所有角色获得经验", false, "战斗后所有角色都可获得经验");
            maxBattleAvatar = plugin.Config.Bind("战斗设定", "最大上阵人数", 4, "修改最大上阵人数");
            onePunch = plugin.Config.Bind("战斗设定", "一击99999999", false, "不破锁血等，道具无效");
            gameSpeed = plugin.Config.Bind("游戏设定", "游戏速度", 2.0f, "修改游戏速度");
            speedKey = plugin.Config.Bind("游戏设定", "速度热键", KeyCode.F2, "开关速度调节");
            playerMoveSpeed = plugin.Config.Bind("游戏设定", "角色移动速度", 6f, "修改角色移动速度");
            unlimitRadarRange = plugin.Config.Bind("UI增强", "无限距追踪", false, "大地图上的npc无论多远都显示追踪图标，但不会自动消失。可能会引起卡顿.如果关闭后图标不消失可SL解决");
            rightClickCloseUI = plugin.Config.Bind("游戏设定", "右键关闭UI界面", false, "右键关闭UI界面");
            ShowRepluseTick = plugin.Config.Bind("UI增强", "显示时序数值", false, "战斗中显示时序数值");
            showGiftFavorite = plugin.Config.Bind("UI增强", "显示礼物喜爱度", false, "显示礼物喜爱度");
            playerSurname = plugin.Config.Bind("游戏设定", "主角姓", "辰", "修改主角姓");
            playerName = plugin.Config.Bind("游戏设定", "主角名", "雨", "修改主角名");
            addskillColumnNum = plugin.Config.Bind("游戏设定", "增加技能栏位数", 0, "每人最多6个栏位，多了无效");
            autoBattle = plugin.Config.Bind("战斗设定", "自动战斗", false, "注 意 智 障 A I");
            //modSupport = plugin.Config.Bind("mod支持", "mod支持", false, "mod支持");
            plugin.onUpdate += OnUpdate;

            ReplacePlayerAvatarEventHander += new EventHandler((o, e) =>
            {
                foreach (string id in playerIds)
                {
                    ReplacePlayerAvatarData(id);
                }
            });
            playerSurname.SettingChanged += ReplacePlayerAvatarEventHander;
            playerName.SettingChanged += ReplacePlayerAvatarEventHander;
        }

        public void OnUpdate()
        {
            if (Input.GetKeyDown(speedKey.Value))
            {
                speedOn = !speedOn;
                if (!speedOn)
                {
                    Time.timeScale = 1.0f;
                }
            }
            if (speedOn)
            {
                Time.timeScale = Math.Max(Time.timeScale, gameSpeed.Value);
            }
        }


        //修改中地图角色移动速度
        [HarmonyPrefix, HarmonyPatch(typeof(Move), "LateUpdate")]
        public static bool Move_LateUpdatePatch_changePlayerMoveSpeed(ref Move __instance)
        {
            //Console.WriteLine("Move_LateUpdatePatch_changePlayerMoveSpeed");
            PlayerStateMachine fsm = ResolutionExtensions.Get<PlayerStateMachine>(Game.Kernel, Array.Empty<IParameter>());
            fsm.forward1stSpeedMax = playerMoveSpeed.Value;
            return true;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(Move), "OnRightAxisMove")]
        public static bool Move_OnRightAxisMovePatch_changePlayerMoveSpeed(ref Move __instance)
        {
            //Console.WriteLine("Move_OnRightAxisMovePatch_changePlayerMoveSpeed");
            PlayerStateMachine fsm = ResolutionExtensions.Get<PlayerStateMachine>(Game.Kernel, Array.Empty<IParameter>());
            fsm.forward1stSpeedMax = playerMoveSpeed.Value;
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChangePlayerSpeedAction), "GetValue")]
        public static bool ChangePlayerSpeedAction_GetValuePatch_changePlayerMoveSpeed(ChangePlayerSpeedAction __instance)
        {
            //Console.WriteLine("ChangePlayerSpeedAction_GetValuePatch_changePlayerMoveSpeed");
            __instance.speed = playerMoveSpeed.Value;
            return true;
        }

        //修改大地图角色移动速度
        [HarmonyPrefix, HarmonyPatch(typeof(WorldMapPlayerMoveSystem), "Move")]
        public static bool WorldMapPlayerMoveSystem_MovePatch_changePlayerMoveSpeed(WorldMapPlayerMoveSystem __instance)
        {
            //Console.WriteLine("WorldMapPlayerMoveSystem_MovePatch_changePlayerMoveSpeed");
            Traverse.Create(__instance).Field("speed").SetValue((float)(playerMoveSpeed.Value * 3.33333));
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(WorldMapPlayerMoveSystem), "MoveTo", new Type[] { typeof(IEntity) })]
        public static bool WorldMapPlayerMoveSystem_MoveToPatch_changePlayerMoveSpeed(WorldMapPlayerMoveSystem __instance, ref IEntity target)
        {
            //Console.WriteLine("WorldMapPlayerMoveSystem_MoveToPatch_changePlayerMoveSpeed");
            PartyComponent partyComponent = null;
            NodeComponent nodeComponent = null;
            HarvestComponent harvestComponent;
            if (target.HasComponent(out harvestComponent))
            {
                Vector3 position = target.GetGameObject().transform.position;
                MistEffectSystem mistEffectSystem = (MistEffectSystem)Traverse.Create(__instance).Field("mistEffectSystem").GetValue();
                if (!mistEffectSystem.IsVisible(position))
                {
                    return false;
                }
                IEntity entity = (IEntity)Traverse.Create(__instance).Field("entity").GetValue();
                if (entity.HasComponent<DestinationComponent>())
                {
                    entity.RemoveComponent<DestinationComponent>();
                }
                DestinationComponent component = new DestinationComponent
                {
                    DestinationId = target.Id,
                    Destination = position,
                    Type = MoveType.NavMesh,
                    Speed = Convert.ToInt32(playerMoveSpeed.Value * 55.5)
                };
                entity.AddComponent(component);
                IObjectPool<GameObject> pool = (IObjectPool<GameObject>)Traverse.Create(__instance).Field("pool").GetValue();
                if (nodeComponent != null)
                {
                    GameObject indicator = pool.Load(GameConfig.WorldMap.MoveIndicatorEffectPath);
                    indicator.transform.position = nodeComponent.Position + Vector3.up * 2.5f;
                    Observable.Timer(TimeSpan.FromSeconds(2.0)).Subscribe(delegate (long x)
                    {
                        pool.Return(indicator);
                    });
                }
                IDisposable mistUpdateDisposable = (IDisposable)Traverse.Create(__instance).Field("mistUpdateDisposable").GetValue();
                IDisposable disposable = mistUpdateDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
                mistUpdateDisposable = Observable.EveryUpdate().Subscribe(delegate (long _)
                {
                    PartyComponent party = (PartyComponent)Traverse.Create(__instance).Field("party").GetValue();
                    mistEffectSystem.Update(party.Position);
                    CullingManager cullingManager = (CullingManager)Traverse.Create(__instance).Field("cullingManager").GetValue();
                    CullingComponent culling = (CullingComponent)Traverse.Create(__instance).Field("culling").GetValue();
                    cullingManager.Update(culling);
                });
                TimeProcessSystem timeSystem = (TimeProcessSystem)Traverse.Create(__instance).Field("timeSystem").GetValue();
                if (timeSystem.IsPaused)
                {
                    timeSystem.IsPaused = false;
                    return false;
                }
            }
            else if (target.HasComponent(out partyComponent) || target.HasComponent(out nodeComponent))
            {
                MistEffectSystem mistEffectSystem = (MistEffectSystem)Traverse.Create(__instance).Field("mistEffectSystem").GetValue();
                if (nodeComponent != null && !mistEffectSystem.IsVisible(nodeComponent.Position))
                {
                    return false;
                }
                IEntity entity = (IEntity)Traverse.Create(__instance).Field("entity").GetValue();
                if (entity.HasComponent<DestinationComponent>())
                {
                    entity.RemoveComponent<DestinationComponent>();
                }
                ValueTuple<MoveType, Vector3> valueTuple = (partyComponent == null) ? new ValueTuple<MoveType, Vector3>(MoveType.NavMesh, nodeComponent.Position) : new ValueTuple<MoveType, Vector3>(MoveType.Chase, Vector3.zero);
                MoveType item = valueTuple.Item1;
                Vector3 item2 = valueTuple.Item2;
                DestinationComponent component2 = new DestinationComponent
                {
                    DestinationId = target.Id,
                    Destination = item2,
                    Type = item,
                    Speed = Convert.ToInt32(playerMoveSpeed.Value * 55.5)
                };
                entity.AddComponent(component2);
                IObjectPool<GameObject> pool = (IObjectPool<GameObject>)Traverse.Create(__instance).Field("pool").GetValue();
                if (nodeComponent != null)
                {
                    GameObject indicator = pool.Load(GameConfig.WorldMap.MoveIndicatorEffectPath);
                    indicator.transform.position = nodeComponent.Position + Vector3.up * 2.5f;
                    Observable.Timer(TimeSpan.FromSeconds(2.0)).Subscribe(delegate (long x)
                    {
                        pool.Return(indicator);
                    });
                }
                IDisposable mistUpdateDisposable = (IDisposable)Traverse.Create(__instance).Field("mistUpdateDisposable").GetValue();
                IDisposable disposable2 = mistUpdateDisposable;
                if (disposable2 != null)
                {
                    disposable2.Dispose();
                }
                mistUpdateDisposable = Observable.EveryUpdate().Subscribe(delegate (long _)
                {
                    PartyComponent party = (PartyComponent)Traverse.Create(__instance).Field("party").GetValue();
                    mistEffectSystem.Update(party.Position);
                    CullingManager cullingManager = (CullingManager)Traverse.Create(__instance).Field("cullingManager").GetValue();
                    CullingComponent culling = (CullingComponent)Traverse.Create(__instance).Field("culling").GetValue();
                    cullingManager.Update(culling);
                });
                TimeProcessSystem timeSystem = (TimeProcessSystem)Traverse.Create(__instance).Field("timeSystem").GetValue();
                if (timeSystem.IsPaused)
                {
                    timeSystem.IsPaused = false;
                }
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(WorldMapPlayerMoveSystem), "MoveTo", new Type[] { typeof(Vector3) })]
        public static bool WorldMapPlayerMoveSystem_MoveToPatch_changePlayerMoveSpeed2(WorldMapPlayerMoveSystem __instance, ref Vector3 point)
        {
            //Console.WriteLine("WorldMapPlayerMoveSystem_MoveToPatch_changePlayerMoveSpeed2");

            NavMeshHit navMeshHit;
            if (!NavMesh.SamplePosition(point, out navMeshHit, 5f, -1))
            {
                return false;
            }
            ActorController actor = (ActorController)Traverse.Create(__instance).Field("actor").GetValue();
            NavMeshPath path = (NavMeshPath)Traverse.Create(__instance).Field("path").GetValue();
            if (!NavMesh.CalculatePath(actor.transform.position, navMeshHit.position, -1, path))
            {
                return false;
            }
            if (path.corners.Magnitude() == 0f)
            {
                return false;
            }
            MistEffectSystem mistEffectSystem = (MistEffectSystem)Traverse.Create(__instance).Field("mistEffectSystem").GetValue();
            if (!mistEffectSystem.IsVisible(point))
            {
                return false;
            }
            TimeProcessSystem timeSystem = (TimeProcessSystem)Traverse.Create(__instance).Field("timeSystem").GetValue();
            if (timeSystem.IsPaused)
            {
                timeSystem.IsPaused = false;
            }
            __instance.ResetCameraTarget();
            IEntity entity = (IEntity)Traverse.Create(__instance).Field("entity").GetValue();
            if (entity.HasComponent<DestinationComponent>())
            {
                entity.RemoveComponent<DestinationComponent>();
            }
            DestinationComponent component = new DestinationComponent
            {
                Destination = point,
                Type = MoveType.NavMesh,
                Speed = Convert.ToInt32(playerMoveSpeed.Value * 55.5)
            };
            entity.AddComponent(component);
            IObjectPool<GameObject> pool = (IObjectPool<GameObject>)Traverse.Create(__instance).Field("pool").GetValue();
            GameObject indicator = pool.Load(GameConfig.WorldMap.MoveIndicatorEffectPath);
            indicator.transform.position = point;
            indicator.transform.localScale = Vector3.one * GameConfig.WorldMap.PartyModelScale;
            Observable.Timer(TimeSpan.FromSeconds(2.0)).Subscribe(delegate (long x)
            {
                pool.Return(indicator);
            });
            IDisposable mistUpdateDisposable = (IDisposable)Traverse.Create(__instance).Field("mistUpdateDisposable").GetValue();
            IDisposable disposable = mistUpdateDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            mistUpdateDisposable = Observable.EveryUpdate().Subscribe(delegate (long _)
            {
                PartyComponent party = (PartyComponent)Traverse.Create(__instance).Field("party").GetValue();
                mistEffectSystem.Update(party.Position);
                CullingManager cullingManager = (CullingManager)Traverse.Create(__instance).Field("cullingManager").GetValue();
                CullingComponent culling = (CullingComponent)Traverse.Create(__instance).Field("culling").GetValue();
                cullingManager.Update(culling);
            });

            return false;
        }


        //学技能不吃书
        [HarmonyPrefix, HarmonyPatch(typeof(MovableItemUI), "UseBookConfirm")]
        public static bool MovableItemUI_UseBookConfirmPatch_UseBookNotDelete(MovableItemUI __instance)
        {
            if (learnSkillNotEatBook.Value)
            {
                Console.WriteLine("MovableItemUI_UseBookConfirmPatch_UseBookNotDelete");
                Entity useingEntity = (Entity)Traverse.Create(__instance).Field("useingEntity").GetValue();
                ILoopedGridItem<InventoryData> currentUseItem = (ILoopedGridItem<InventoryData>)Traverse.Create(__instance).Field("currentUseItem").GetValue();

                if (useingEntity == null || currentUseItem == null)
                {
                    return false;
                }
                NormalInventoryItem normalInventoryItem = Game.Data.Get<NormalInventoryItem>(currentUseItem.Value.Id);
                LearnedSkillComponent component = useingEntity.GetComponent<LearnedSkillComponent>();
                string id = useingEntity.GetComponent<NpcComponent>().Id;
                using (List<LearnedSkill>.Enumerator enumerator = component.Skills.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.Id == normalInventoryItem.Skill)
                        {
                            Heluo.Logger.LogError(id + " 已經有技能" + normalInventoryItem.Skill, "UseBookConfirm", "D:\\GuLong\\Assets\\Script\\UI\\View\\UIWarehouse.cs", 827);
                            return false;
                        }
                    }
                }
                component.Skills.Add(new LearnedSkill
                {
                    Id = normalInventoryItem.Skill
                });
                //(currentUseItem as WGLoopedGridItem<InventoryData>).GetComponentInParent<IMoveableInventory>().RemoveItem(currentUseItem.Value, 1);
                IReusableEventSystem EventSystem = (IReusableEventSystem)Traverse.Create(__instance).Property("EventSystem").GetValue();
                EventSystem.Publish<AudioEventArgs>(delegate (AudioEventArgs e)
                {
                    e.SetSound(GameConfig.SoundUIPath + "UISE_Party_SkillLearn01.wav", Game.UI.Canvas.gameObject, 0f, 1f, false);
                });
                EventSystem.Publish<HeroUseItemEventArgs>(new HeroUseItemEventArgs
                {
                    HeroId = id
                });
                useingEntity = null;
                currentUseItem = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MovableItemUI), "UseHeartFormulaConfirm")]
        private static bool MovableItemUI_UseHeartFormulaConfirmPatch_UseBookNotDelete(MovableItemUI __instance)
        {
            if (learnSkillNotEatBook.Value)
            {
                Console.WriteLine("MovableItemUI_UseHeartFormulaConfirmPatch_UseBookNotDelete");
                Entity useingEntity = (Entity)Traverse.Create(__instance).Field("useingEntity").GetValue();
                ILoopedGridItem<InventoryData> currentUseItem = (ILoopedGridItem<InventoryData>)Traverse.Create(__instance).Field("currentUseItem").GetValue();

                if (useingEntity == null || currentUseItem == null)
                {
                    return false;
                }
                NormalInventoryItem normalInventoryItem = Game.Data.Get<NormalInventoryItem>(currentUseItem.Value.Id);
                PerceptionComponent component = useingEntity.GetComponent<PerceptionComponent>();
                string id = useingEntity.GetComponent<NpcComponent>().Id;
                using (List<string>.Enumerator enumerator = component.ReleaseSpecialGroups.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == normalInventoryItem.Perception)
                        {
                            Heluo.Logger.LogError(id + " 已經有心法" + normalInventoryItem.Perception, "UseHeartFormulaConfirm", "D:\\GuLong\\Assets\\Script\\UI\\View\\UIWarehouse.cs", 919);
                            return false;
                        }
                    }
                }
                Game.World.GetSystem<PerceptionSystem>().AddReleaseSpecialGroup(id, new string[]
                {
                normalInventoryItem.Perception
                });
                //(currentUseItem as WGLoopedGridItem<InventoryData>).GetComponentInParent<IMoveableInventory>().RemoveItem(currentUseItem.Value, 1);
                IReusableEventSystem EventSystem = (IReusableEventSystem)Traverse.Create(__instance).Property("EventSystem").GetValue();
                EventSystem.Publish<AudioEventArgs>(delegate (AudioEventArgs e)
                {
                    e.SetSound(GameConfig.SoundUIPath + "UISE_Party_SkillLearn01.wav", Game.UI.Canvas.gameObject, 0f, 1f, false);
                });
                EventSystem.Publish<HeroUseItemEventArgs>(new HeroUseItemEventArgs
                {
                    HeroId = id
                });
                useingEntity = null;
                currentUseItem = null;

                return false;
            }
            else
            {
                return true;
            }
        }

        //所有角色获得经验
        [HarmonyPrefix, HarmonyPatch(typeof(UIBattleFinish), "Show")]
        private static bool UIBattleFinish_ShowPatch_AllPlayerGetExp(UIBattleFinish __instance)
        {
            if (allMemberGetExp.Value)
            {
                Console.WriteLine("UIBattleFinish_ShowPatch_AllPlayerGetExp");
                //Traverse.Create(__instance).Method("Show").GetValue();
                List<ValueTuple<string, int>> ids = (List<(string, int)>)Traverse.Create(__instance).Field("ids").GetValue();

                ids.Clear();

                BattleController Controller = (BattleController)Traverse.Create(__instance.fsm).Property("Controller").GetValue();
                DataComponentSystem<NpcComponent, NpcItem> system = Game.World.GetSystem<NpcDataSystem>();
                CharacterPropertyInfo characterPropertyInfo;
                foreach (string id in __instance.PartyCreationSystem.Player.Members)
                {
                    characterPropertyInfo = new CharacterPropertyInfo(system[id]);
                    characterPropertyInfo.Calculate(0);
                    ids.Add(new ValueTuple<string, int>(id, characterPropertyInfo.GetLevel()));
                }
                string text = __instance.PartyCreationSystem.Player.Members[0];
                characterPropertyInfo = new CharacterPropertyInfo(system[text]);
                characterPropertyInfo.Calculate(0);
                if (ids.Count == 0)
                {
                    ids.Add(new ValueTuple<string, int>(text, characterPropertyInfo.GetLevel()));
                }
                Console.WriteLine("ids.Count:" + ids.Count);


                int pageCount = (int)Traverse.Create(__instance).Field("pageCount").GetValue();
                int num = Mathf.Min(pageCount, ids.Count);
                for (int i = 0; i < num; i++)
                {
                    WgBattleFinishPortrait portraitPrefab = (WgBattleFinishPortrait)Traverse.Create(__instance).Field("portraitPrefab").GetValue();
                    Transform portraitViewport = (Transform)Traverse.Create(__instance).Field("portraitViewport").GetValue();
                    WgBattleFinishPortrait wgBattleFinishPortrait = UnityEngine.Object.Instantiate<WgBattleFinishPortrait>(portraitPrefab, portraitViewport);
                    wgBattleFinishPortrait.Initialize(ids[i].Item1);
                    wgBattleFinishPortrait.gameObject.SetActive(true);
                }

                int rolePage = (int)Traverse.Create(__instance).Field("rolePage").GetValue();
                rolePage++;
                Traverse.Create(__instance).Field("rolePage").SetValue(rolePage);

                return false;
            }
            else
            {
                return true;
            }
        }


        //最大上阵人数
        [HarmonyPrefix, HarmonyPatch(typeof(PlayerConfigure), "SetNumber", new Type[] { typeof(int) })]
        private static bool PlayerConfigure_SetNumberPatch_ChangeMaxBattleAvatarNum(PlayerConfigure __instance, ref int max)
        {
            Console.WriteLine("PlayerConfigure_SetNumberPatch_ChangeMaxBattleAvatarNum");
            max = maxBattleAvatar.Value;
            return true;
        }

        //一击99999999
        [HarmonyPrefix, HarmonyPatch(typeof(AttackProcessStrategy), "DamagePrint", new Type[] { typeof(DamageInfo), typeof(IBattleSkill), typeof(int) })]
        public static bool AttackProcessStrategy_DamagePrintPatch_OnePunch(AttackProcessStrategy __instance, ref DamageInfo info)
        {
            if (onePunch.Value)
            {
                Console.WriteLine("AttackProcessStrategy_DamagePrintPatch_OnePunch");
                IEntity attacker = info.source;
                if ((Faction)attacker.GetComponent<BattleUnit>().Party.Value == Faction.Player)
                {
                    using (List<Damage>.Enumerator enumerator = info.damage.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Damage damage = enumerator.Current;
                            damage.isDodge = false;
                            damage.value = 99999999;
                        }
                    }
                }
            }
            return true;
        }

        //无限距雷达
        [HarmonyPostfix, HarmonyPatch(typeof(PartyComponent), "CullInLevel", MethodType.Getter)]
        public static void PartyComponent_CullInLevelPatch_unlimitRadarRange(PartyComponent __instance, ref CullingLevel __result)
        {
            if (unlimitRadarRange.Value)
            {
                Console.WriteLine("PartyComponent_CullInLevelPatch_unlimitRadarRange");
                __result = CullingLevel.Outer;
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(PartyComponent), "CullOutLevel", MethodType.Getter)]
        public static void PartyComponent_CullOutLevelPatch_unlimitRadarRange(PartyComponent __instance, ref CullingLevel __result)
        {
            if (unlimitRadarRange.Value)
            {
                Console.WriteLine("PartyComponent_CullOutLevelPatch_unlimitRadarRange");
                __result = CullingLevel.Outer;
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(PartyComponent), "Radius", MethodType.Getter)]
        public static void PartyComponent_RadiusPatch_unlimitRadarRange(PartyComponent __instance, ref float __result)
        {
            if (unlimitRadarRange.Value)
            {
                //Console.WriteLine("PartyComponent_RadiusPatch_unlimitRadarRange");
                __result = 9999f;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PartyBillboardViewSystem), "Process")]
        public static void WgPartyBillboard_SetNamePatch_showNameAnywhere(WgPartyBillboard __instance, ref IEntity entity)
        {
            //Console.WriteLine("WgPartyBillboard_SetNamePatch_showNameAnywhere");
            EntityBillboardComponent component = entity.GetComponent<EntityBillboardComponent>();
            GameObject billboardObject = component.Object;
            Transform trans = billboardObject.transform;

            Vector4 border = Traverse.Create(__instance).Field("border").GetValue<Vector4>();

            Transform[] transforms = billboardObject.GetComponentsInChildren<Transform>();

            foreach (var child in transforms)
            {
                if (child.gameObject.name == "Name")
                {
                    if (trans.position.y > Screen.height - border.x - 40)
                    {
                        child.localPosition = new Vector3(child.localPosition.x, -40, 0);
                    }
                    else if (trans.position.y <= Screen.height - border.x - 40)
                    {
                        child.localPosition = new Vector3(child.localPosition.x, 40, 0);
                    }
                }
            }

        }


        //右键关闭UI
        [HarmonyPostfix, HarmonyPatch(typeof(UIControllableForm), "MouseUpEvent")]
        public static void UIControllableForm_MouseUpEventPatch_rightClickCloseUI(UIControllableForm __instance, ref PointerEventData.InputButton btn)
        {
            if (rightClickCloseUI.Value)
            {
                Console.WriteLine("UIControllableForm_MouseUpEventPatch_rightClickCloseUI");
                if (btn == PointerEventData.InputButton.Right)
                {
                    __instance.Close();
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UISkillEnhance), "MouseUpEvent")]
        public static void UISkillEnhance_MouseUpEventPatch_rightClickCloseUI(UIControllableForm __instance, ref PointerEventData.InputButton btn)
        {
            if (rightClickCloseUI.Value)
            {
                Console.WriteLine("UISkillEnhance_MouseUpEventPatch_rightClickCloseUI");
                if (btn == PointerEventData.InputButton.Right)
                {
                    UIMenuPage uiMenuPage = Game.UI.Get<UIMenuPage>();
                    uiMenuPage.Close();
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIWarehouse), "MouseUpEvent")]
        public static bool UIWarehouse_MouseUpEventPatch_rightClickCloseUI(UIWarehouse __instance, ref PointerEventData.InputButton btn)
        {
            Console.WriteLine("UIWarehouse_MouseUpEventPatch_rightClickCloseUI");
            if (rightClickCloseUI.Value)
            {
                if (btn != PointerEventData.InputButton.Right)
                {
                    return false;
                }
                InventoryData takenItem = Traverse.Create(__instance).Field("takenItem").GetValue<InventoryData>();
                if (btn == PointerEventData.InputButton.Right && takenItem != null)
                {
                    __instance.ReturnTakenItem();
                    return false;
                }
                GameObject benevolenceMakePrefab = Traverse.Create(__instance).Field("benevolenceMakePrefab").GetValue<GameObject>();
                if (benevolenceMakePrefab.activeInHierarchy)
                {
                    if (Traverse.Create(__instance).Method("IsTaken").GetValue<bool>())
                    {
                        return false;
                    }
                    __instance.Close();
                }
                else if (btn == PointerEventData.InputButton.Right)
                {
                    UIMenuPage uiMenuPage = Game.UI.Get<UIMenuPage>();
                    uiMenuPage.Close();
                }
                return false;
            }
            return true;
        }

        static Type[] canRightClickCloseForm = new Type[] {
            typeof(UIAttributes),
            typeof(UISkillEnhance),
            typeof(UIPerception) ,
            typeof(UIWarehouse),
            typeof(UIBlueprint),
            typeof(UIManorVisitor),
            typeof(UIMosaic),
            typeof(UIQuest),
            typeof(UIQuestBranch),
            typeof(UIQuestChat),
            typeof(UIHelp),
            typeof(UIBackground)
        };


        [HarmonyPostfix, HarmonyPatch(typeof(UIForm), "Show")]
        public static void UIForm_showPatch_rightClickCloseUI(UIForm __instance)
        {
            if (rightClickCloseUI.Value)
            {
                bool canRightClickClose = false;
                foreach (Type form in canRightClickCloseForm)
                {
                    if (__instance.GetType().IsAssignableFrom(form))
                    {
                        canRightClickClose = true;
                        break;
                    }
                }
                if (canRightClickClose)
                {
                    AddRightClickCloseUIEvent(__instance.gameObject);
                }
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(UIQuestBranch), "Show")]
        public static void UIQuestBranch_showPatch_rightClickCloseUI(UIQuestBranch __instance)
        {
            if (rightClickCloseUI.Value)
            {
                Console.WriteLine("UIQuestBranch_showPatch_rightClickCloseUI");
                AddRightClickCloseUIEvent(__instance.gameObject);
            }
        }

        public static void AddRightClickCloseUIEvent(GameObject gameObject)
        {
            Console.WriteLine("UIForm_showPatch_rightClickCloseUI");
            TouchController touchController = (gameObject.GetComponent<TouchController>() ?? gameObject.AddComponent<TouchController>());
            touchController.OnMouseUp = (Action<float, float, PointerEventData.InputButton>)Delegate.Combine(touchController.OnMouseUp, new Action<float, float, PointerEventData.InputButton>((x, y, btn) =>
            {
                if (rightClickCloseUI.Value)
                {
                    if (btn == PointerEventData.InputButton.Right)
                    {
                        Console.WriteLine(gameObject.name + ":right click");
                        UIMenuPage uiMenuPage = Game.UI.Get<UIMenuPage>();
                        uiMenuPage.Close();
                    }
                }
            }));
        }


        [HarmonyPostfix, HarmonyPatch(typeof(WGUnitInfo), "MouseDown")]
        public static void WGUnitInfo_MouseDownPatch_rightClickCloseUI(WGUnitInfo __instance)
        {
            if (rightClickCloseUI.Value)
            {
                Console.WriteLine("WGUnitInfo_MouseDownPatch_rightClickCloseUI");
                TouchController touchController = (__instance.gameObject.GetComponent<TouchController>() ?? __instance.gameObject.AddComponent<TouchController>());
                touchController.OnMouseUp = (Action<float, float, PointerEventData.InputButton>)Delegate.Combine(touchController.OnMouseUp, new Action<float, float, PointerEventData.InputButton>((x, y, btn) =>
                {
                    if (rightClickCloseUI.Value)
                    {
                        if (btn == PointerEventData.InputButton.Right)
                        {
                            Console.WriteLine(__instance.gameObject.name + ":right click");
                            WGSkillPrompt skillPrompt = Traverse.Create(__instance).Field("skillPrompt").GetValue<WGSkillPrompt>();
                            if (skillPrompt.isExtraShowing())
                            {
                                skillPrompt.HideExtra();
                                return;
                            }
                            __instance.Hide();
                        }
                    }
                }));
            }
        }

        //显示时序数值
        [HarmonyPostfix, HarmonyPatch(typeof(WGTimeline), "OnTimeEventAdd", new Type[] { typeof(ITimedEvent), typeof(Dictionary<ITimedEvent, WGTimelineIcon>), typeof(bool) })]
        public static void WGTimeline_OnTimeEventAddPatch_ShowRepluseTick(WGTimeline __instance)
        {
            Console.WriteLine("WGTimeline_OnTimeEventAddPatch_ShowRepluseTick");
            createRepluseTickText(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BattleSequencer), "Update")]
        public static void BattleSequencer_UpdatePatch_ShowRepluseTick(BattleSequencer __instance)
        {
            Console.WriteLine("BattleSequencer_UpdatePatch_ShowRepluseTick");
            UIBattle uIBattle = Game.UI.Get<UIBattle>();
            WGTimeline wGTimeline = uIBattle.GetComponentInChildren<WGTimeline>();
            createRepluseTickText(wGTimeline);
        }


        public static void createRepluseTickText(WGTimeline __instance)
        {
            List<ValueTuple<ITimedEvent, WGTimelineIcon>> TimelineQueue = Traverse.Create(__instance).Field("TimelineQueue").GetValue<List<ValueTuple<ITimedEvent, WGTimelineIcon>>>();
            foreach (ValueTuple<ITimedEvent, WGTimelineIcon> value in TimelineQueue)
            {
                Text RepluseTickText;
                UnityEngine.UI.Image portrait = Traverse.Create(value.Item2).Field("portrait").GetValue<UnityEngine.UI.Image>();
                if (portrait != null)
                {
                    var trans2 = portrait.transform.parent.parent.parent.Find("RepluseTickText");
                    if (trans2 == null)
                    {
                        GameObject gameObject2 = new GameObject("RepluseTickText");
                        gameObject2.transform.SetParent(portrait.transform.parent.parent.parent, false);
                        RepluseTickText = gameObject2.AddComponent<Text>();
                        RepluseTickText.text = (value.Item1.Tick / 100).ToString();

                        // 获得系统字体名称列表
                        string[] systemFontNames = Font.GetOSInstalledFontNames();
                        // 获得某种字体
                        int index = 0;
                        string systemFontName = systemFontNames[index];
                        Font font = Font.CreateDynamicFontFromOSFont(systemFontName, 36);

                        RepluseTickText.font = font;
                        RepluseTickText.fontSize = 18;
                        RepluseTickText.fontStyle = FontStyle.Bold;
                        RepluseTickText.alignment = TextAnchor.MiddleCenter;
                        RepluseTickText.transform.localPosition = new Vector3(0, 130, 0);
                    }
                    else
                    {
                        RepluseTickText = trans2.gameObject.GetComponent<Text>();
                        RepluseTickText.text = (value.Item1.Tick / 100).ToString();
                    }
                    if (ShowRepluseTick.Value)
                    {
                        RepluseTickText.gameObject.SetActive(true);
                    }
                    else
                    {
                        RepluseTickText.gameObject.SetActive(false);
                    }
                }


            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGGiftTip), "<SetItemInfo>g__ApplyManorGiftExposeBuff|5_1")]
        public static void WGGiftTip_ApplyManorGiftExposeBuffPatch_showGiftFavorite(WGGiftTip __instance, ref bool __result)
        {
            Console.WriteLine("WGGiftTip_ApplyManorGiftExposeBuffPatch_showGiftFavorite");
            if (showGiftFavorite.Value)
            {
                __result = true;
            }
        }

        //修改主角姓名
        static string[] playerIds = new string[] { "na0000", "na0000a", "na0000_boss", "nq0131" };

        [HarmonyPostfix, HarmonyPatch(typeof(NpcDataSystem), "Setup")]
        public static void NpcDataSystem_SetupPatch_ChangePlayerSurname(NpcDataSystem __instance, ref IEntity entity)
        {
            NpcComponent component = entity.GetComponent<NpcComponent>();

            if (playerIds.Contains(component.Id))
            {
                Console.WriteLine("NpcDataSystem_SetupPatch_ChangePlayerSurname");
                ReplacePlayerAvatarData(component.Id);
            }
        }

        public static void ReplacePlayerAvatarData(string Id)
        {
            Console.WriteLine("ReplacePlayerAvatarData");
            NpcDataSystem system = Game.World.GetSystem<NpcDataSystem>();
            NpcComponent component = system[Id].GetComponent<NpcComponent>();
            AvatarItem overrideAvatar = component.OverrideAvatar;
            if (overrideAvatar != null)
            {
                overrideAvatar.Surname = playerSurname.Value;
                overrideAvatar.Name = playerName.Value;
            }
            else
            {
                if (component.Item != null && component.Item.Avatar != null)
                {
                    component.Item.Avatar.Surname = playerSurname.Value;
                    component.Item.Avatar.Name = playerName.Value;
                }
            }

        }
        //增加技能栏位
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterPropertyInfo), "GetTotalInt")]
        public static void CharacterPropertyInfo_GetTotalIntPatch_addskillColumnNum(CharacterPropertyInfo __instance,ref CharacterProperty property, ref int __result)
        {
            PartyCreationSystem partyCreationSystem = Game.World.GetSystem<PartyCreationSystem>();
            IEntity playerEntity = partyCreationSystem.PlayerEntity;
            PartyComponent partyComponent = playerEntity.GetComponent<PartyComponent>();
            IList<string> members = partyComponent.Members;
            if(property == CharacterProperty.Memory)
            {
                Console.WriteLine("CharacterPropertyInfo_GetTotalIntPatch_addskillColumnNum");
                IEntity entity = __instance.Entity;
                if (members.Contains(entity.GetComponent<NpcComponent>().Id))
                {
                    __result += addskillColumnNum.Value;
                }
            }
        }

        //自动战斗
        [HarmonyPrefix, HarmonyPatch(typeof(BattleStateMachine), "IsControllable")]
        public static bool CharacterPropertyInfo_IsControllablePatch_autoBattle(BattleStateMachine __instance, ref bool __result)
        {
            //Console.WriteLine("CharacterPropertyInfo_IsControllablePatch_autoBattle");
            if (autoBattle.Value)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}