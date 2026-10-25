using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CastleMarketTowns
{
    // Skill and attribute changes go through the hero developer, so they stay in the save as normal progression.
    internal static class CastleTraining
    {
        private const string MenuId = "castle_market_towns_training";

        internal static readonly Dictionary<string, float> LastSkillSession = new Dictionary<string, float>();
        internal static readonly Dictionary<string, float> LastAttributeSession = new Dictionary<string, float>();

        private static SkillObject[] MartialSkills
        {
            get
            {
                return new SkillObject[]
                {
                    DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm, DefaultSkills.Bow, DefaultSkills.Crossbow,
                    DefaultSkills.Throwing, DefaultSkills.Riding, DefaultSkills.Athletics, DefaultSkills.Scouting, DefaultSkills.Crafting
                };
            }
        }

        private static SkillObject[] AdministrativeSkills
        {
            get
            {
                return new SkillObject[]
                {
                    DefaultSkills.Leadership, DefaultSkills.Tactics, DefaultSkills.Steward, DefaultSkills.Trade,
                    DefaultSkills.Engineering, DefaultSkills.Charm, DefaultSkills.Roguery, DefaultSkills.Medicine
                };
            }
        }

        internal static void AddMenus(CampaignGameStarter starter, string keepMenuId, int index)
        {
            starter.AddGameMenuOption(keepMenuId, "castle_market_towns_trainers", "{=cmt_menu_visit_trainers}Visit the castle trainers", VisitCondition, delegate { GameMenu.SwitchToMenu(MenuId); }, false, index, false, null);
            starter.AddGameMenu(MenuId, "{=!}{TRAINING_TEXT}", MenuInit, GameMenu.MenuOverlayType.SettlementWithCharacters, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption(MenuId, "castle_market_towns_train_martial", "{=cmt_training_martial}Train martial skills ({TRAINING_GOLD}{GOLD_ICON}, {TRAINING_INFLUENCE} influence)", args => SkillCondition(args, MartialSkills, GameMenuOption.LeaveType.PracticeFight), delegate { ConfirmSkills(MartialSkills, new TextObject("{=cmt_training_martial_title}Train martial skills")); }, false, -1, false, null);
            starter.AddGameMenuOption(MenuId, "castle_market_towns_train_administrative", "{=cmt_training_administrative}Study administrative skills ({TRAINING_GOLD}{GOLD_ICON}, {TRAINING_INFLUENCE} influence)", args => SkillCondition(args, AdministrativeSkills, GameMenuOption.LeaveType.Conversation), delegate { ConfirmSkills(AdministrativeSkills, new TextObject("{=cmt_training_administrative_title}Study administrative skills")); }, false, -1, false, null);
            starter.AddGameMenuOption(MenuId, "castle_market_towns_train_attribute", "{=cmt_training_attribute}Train an attribute ({ATTRIBUTE_GOLD}{GOLD_ICON}, {ATTRIBUTE_INFLUENCE} influence)", AttributeCondition, delegate { ChooseAttributeHero(); }, false, -1, false, null);
            starter.AddGameMenuOption(MenuId, "castle_market_towns_training_back", "{=E1OwmQFb}Back", delegate(MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, delegate { GameMenu.SwitchToMenu(CastleMarketTownsBehavior.KeepAvailable(Settlement.CurrentSettlement) ? keepMenuId : "castle"); }, true, -1, false, null);
        }

        internal static void PruneSessions()
        {
            Prune(LastSkillSession, CastleMarketTownsSettings.MaxSkillCooldownDays);
            Prune(LastAttributeSession, CastleMarketTownsSettings.MaxAttributeCooldownDays);
        }

        private static void Prune(Dictionary<string, float> sessions, int longestCooldown)
        {
            float today = Today();
            List<string> stale = new List<string>();
            foreach (KeyValuePair<string, float> pair in sessions)
            {
                if (today - pair.Value >= longestCooldown)
                {
                    stale.Add(pair.Key);
                }
            }

            foreach (string heroId in stale)
            {
                sessions.Remove(heroId);
            }
        }

        private static CastleMarketTownsSettings Settings
        {
            get { return CastleMarketTownsSettings.Current; }
        }

        private static bool Open(Settlement settlement)
        {
            CastleMarketTownsSettings settings = Settings;
            return settings != null && settings.CastleTraining && CastleMarketTownsBehavior.KeepAvailable(settlement) && MobileParty.MainParty != null && Clan.PlayerClan != null;
        }

        private static bool VisitCondition(MenuCallbackArgs args)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            if (!Open(settlement))
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            if (settlement.IsUnderSiege)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=cmt_training_siege}No one trains while the castle is under siege.");
            }

            return true;
        }

        private static void MenuInit(MenuCallbackArgs args)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            args.MenuTitle = new TextObject("{=cmt_training_title}Training yard");
            string description = new TextObject("{=cmt_training_description}The castle's drillmaster and the steward's clerks will teach you, your companions and your adult family members, for a price.").ToString();
            MBTextManager.SetTextVariable("TRAINING_TEXT", description + "\n\n" + Status());
            if (settlement == null)
            {
                return;
            }

            args.MenuContext.SetBackgroundMeshName(settlement.SettlementComponent.WaitMeshName);
            Campaign.Current.GameMenuManager.MenuLocations.Clear();
        }

        private static string Status()
        {
            List<Hero> heroes = Trainees();
            if (heroes.Count == 0)
            {
                return new TextObject("{=cmt_training_no_trainees}No one in your party can train here. Check Train yourself and Train companions in the mod options.").ToString();
            }

            bool attributes = Settings.AttributeTraining;
            List<string> lines = new List<string>();
            foreach (Hero hero in heroes)
            {
                TextObject line = attributes
                    ? new TextObject("{=cmt_training_hero_status}{HERO}: skills {SKILLS}, attributes {ATTRIBUTES}")
                    : new TextObject("{=cmt_training_hero_status_skills}{HERO}: skills {SKILLS}");
                line.SetTextVariable("HERO", hero.Name);
                line.SetTextVariable("SKILLS", Readiness(DaysLeft(LastSkillSession, hero, Settings.SkillCooldownDays)));
                if (attributes)
                {
                    line.SetTextVariable("ATTRIBUTES", Readiness(DaysLeft(LastAttributeSession, hero, Settings.AttributeCooldownDays)));
                }

                lines.Add(line.ToString());
            }

            return string.Join("\n", lines.ToArray());
        }

        private static TextObject Readiness(int daysLeft)
        {
            if (daysLeft <= 0)
            {
                return new TextObject("{=cmt_training_ready}ready");
            }

            TextObject text = new TextObject("{=cmt_training_in_days}in {DAYS} {?(DAYS > 1)}days{?}day{\\?}");
            text.SetTextVariable("DAYS", daysLeft);
            return text;
        }

        // The player first, then adult heroes of the player's clan travelling in the player's party.
        private static List<Hero> Trainees()
        {
            List<Hero> heroes = new List<Hero>();
            CastleMarketTownsSettings settings = Settings;
            Hero player = Hero.MainHero;
            if (settings.TrainPlayer && player != null && player.IsAlive && !player.IsChild)
            {
                heroes.Add(player);
            }

            MobileParty party = MobileParty.MainParty;
            if (!settings.TrainCompanions || party == null || party.MemberRoster == null)
            {
                return heroes;
            }

            for (int i = 0; i < party.MemberRoster.Count; i++)
            {
                CharacterObject character = party.MemberRoster.GetCharacterAtIndex(i);
                Hero hero = character != null ? character.HeroObject : null;
                if (hero != null && hero != player && hero.IsAlive && !hero.IsChild && hero.Clan == Clan.PlayerClan)
                {
                    heroes.Add(hero);
                }
            }

            return heroes;
        }

        private static float Today()
        {
            return (float)CampaignTime.Now.ToDays;
        }

        private static int DaysLeft(Dictionary<string, float> sessions, Hero hero, int cooldown)
        {
            float last;
            if (!sessions.TryGetValue(hero.StringId, out last))
            {
                return 0;
            }

            return Math.Max(0, (int)Math.Ceiling(cooldown - (Today() - last)));
        }

        private static List<SkillObject> TrainableSkills(Hero hero, SkillObject[] skills)
        {
            List<SkillObject> trainable = new List<SkillObject>();
            foreach (SkillObject skill in skills)
            {
                if (skill != null && hero.GetSkillValue(skill) < Settings.MaxTrainableSkill)
                {
                    trainable.Add(skill);
                }
            }

            return trainable;
        }

        private static List<Hero> SkillTrainees(SkillObject[] skills, out TextObject reason)
        {
            reason = null;
            List<Hero> heroes = Trainees();
            if (heroes.Count == 0)
            {
                reason = new TextObject("{=cmt_training_no_trainees}No one in your party can train here. Check Train yourself and Train companions in the mod options.");
                return heroes;
            }

            List<Hero> ready = new List<Hero>();
            int soonest = int.MaxValue;
            bool rested = false;
            foreach (Hero hero in heroes)
            {
                int daysLeft = DaysLeft(LastSkillSession, hero, Settings.SkillCooldownDays);
                if (daysLeft > 0)
                {
                    soonest = Math.Min(soonest, daysLeft);
                    continue;
                }

                rested = true;
                if (TrainableSkills(hero, skills).Count > 0)
                {
                    ready.Add(hero);
                }
            }

            if (ready.Count > 0)
            {
                return ready;
            }

            if (rested)
            {
                reason = new TextObject("{=cmt_training_skills_maxed}Everyone ready to train already has these skills at {LEVEL} or higher.");
                reason.SetTextVariable("LEVEL", Settings.MaxTrainableSkill);
            }
            else
            {
                reason = new TextObject("{=cmt_training_cooldown}No one is ready to train yet. The next hero is ready {WHEN}.");
                reason.SetTextVariable("WHEN", Readiness(soonest));
            }

            return ready;
        }

        private static TextObject CostProblem(int gold, int influence)
        {
            if (Hero.MainHero.Gold < gold)
            {
                return new TextObject("{=d0kbtGYn}You don't have enough gold.");
            }

            if (Clan.PlayerClan.Influence < influence)
            {
                return new TextObject("{=cjZqTaHm}You don't have enough influence.");
            }

            return null;
        }

        private static void Pay(int gold, int influence)
        {
            if (gold > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, gold, false);
            }

            if (influence > 0)
            {
                ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -influence);
            }
        }

        private static string Names(List<Hero> heroes)
        {
            List<string> names = new List<string>();
            foreach (Hero hero in heroes)
            {
                names.Add(hero.Name.ToString());
            }

            return string.Join(", ", names.ToArray());
        }

        private static string SkillNames(SkillObject[] skills)
        {
            List<string> names = new List<string>();
            foreach (SkillObject skill in skills)
            {
                if (skill != null)
                {
                    names.Add(skill.Name.ToString());
                }
            }

            return string.Join(", ", names.ToArray());
        }

        private static void GainRange(out int low, out int high)
        {
            low = Math.Min(Settings.SkillGainMin, Settings.SkillGainMax);
            high = Math.Max(Settings.SkillGainMin, Settings.SkillGainMax);
        }

        private static bool SkillCondition(MenuCallbackArgs args, SkillObject[] skills, GameMenuOption.LeaveType leaveType)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            if (!Open(settlement))
            {
                return false;
            }

            CastleMarketTownsSettings settings = Settings;
            MBTextManager.SetTextVariable("TRAINING_GOLD", settings.TrainingGoldCost);
            MBTextManager.SetTextVariable("TRAINING_INFLUENCE", settings.TrainingInfluenceCost);
            args.optionLeaveType = leaveType;
            if (settlement.IsUnderSiege)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=cmt_training_siege}No one trains while the castle is under siege.");
                return true;
            }

            TextObject reason;
            List<Hero> ready = SkillTrainees(skills, out reason);
            if (ready.Count == 0)
            {
                args.IsEnabled = false;
                args.Tooltip = reason;
                return true;
            }

            TextObject cost = CostProblem(settings.TrainingGoldCost, settings.TrainingInfluenceCost);
            if (cost != null)
            {
                args.IsEnabled = false;
                args.Tooltip = cost;
                return true;
            }

            args.Tooltip = SkillOffer(skills, ready);
            return true;
        }

        private static TextObject SkillOffer(SkillObject[] skills, List<Hero> ready)
        {
            int low;
            int high;
            GainRange(out low, out high);
            TextObject offer = new TextObject("{=cmt_training_skill_offer}{HEROES} will each gain {LOW} to {HIGH} levels in up to {COUNT} random {?(COUNT > 1)}skills{?}skill{\\?} from: {SKILLS}. Only skills below {LEVEL} are trained, and none goes above it.");
            offer.SetTextVariable("HEROES", Names(ready));
            offer.SetTextVariable("LOW", low);
            offer.SetTextVariable("HIGH", high);
            offer.SetTextVariable("COUNT", Settings.SkillsPerSession);
            offer.SetTextVariable("SKILLS", SkillNames(skills));
            offer.SetTextVariable("LEVEL", Settings.MaxTrainableSkill);
            return offer;
        }

        private static void ConfirmSkills(SkillObject[] skills, TextObject title)
        {
            TextObject reason;
            List<Hero> ready = SkillTrainees(skills, out reason);
            if (ready.Count == 0)
            {
                return;
            }

            CastleMarketTownsSettings settings = Settings;
            TextObject text = new TextObject("{=cmt_training_skill_confirm}This costs {GOLD}{GOLD_ICON} and {INFLUENCE} influence. Each trained hero then waits {DAYS} days before training skills again.");
            text.SetTextVariable("GOLD", settings.TrainingGoldCost);
            text.SetTextVariable("INFLUENCE", settings.TrainingInfluenceCost);
            text.SetTextVariable("DAYS", settings.SkillCooldownDays);
            InformationManager.ShowInquiry(new InquiryData(title.ToString(), SkillOffer(skills, ready).ToString() + "\n\n" + text.ToString(), true, true, new TextObject("{=Y94H6XnK}Accept").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate { TrainSkills(skills, title); }, null), true);
        }

        private static void TrainSkills(SkillObject[] skills, TextObject title)
        {
            CastleMarketTownsSettings settings = Settings;
            TextObject reason;
            List<Hero> ready = SkillTrainees(skills, out reason);
            if (ready.Count == 0 || Closed())
            {
                RefreshMenu();
                return;
            }

            TextObject cost = CostProblem(settings.TrainingGoldCost, settings.TrainingInfluenceCost);
            if (cost != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(cost.ToString()));
                RefreshMenu();
                return;
            }

            Pay(settings.TrainingGoldCost, settings.TrainingInfluenceCost);
            int low;
            int high;
            GainRange(out low, out high);
            int cap = settings.MaxTrainableSkill;
            float today = Today();
            List<string> lines = new List<string>();
            foreach (Hero hero in ready)
            {
                List<SkillObject> trainable = TrainableSkills(hero, skills);
                List<string> gains = new List<string>();
                for (int picked = 0; picked < settings.SkillsPerSession && trainable.Count > 0; picked++)
                {
                    int index = MBRandom.RandomInt(trainable.Count);
                    SkillObject skill = trainable[index];
                    trainable.RemoveAt(index);
                    int before = hero.GetSkillValue(skill);
                    int target = Math.Min(before + MBRandom.RandomInt(low, high + 1), cap);
                    // The game multiplies the experience by a hero's XP multiplier, 1.2 for companions with Natural Leader, so
                    // asking for all levels at once can overshoot the cap. One level at a time never crosses more than one.
                    for (int step = before; step < target && hero.GetSkillValue(skill) < target; step++)
                    {
                        hero.HeroDeveloper.ChangeSkillLevel(skill, 1, false);
                    }

                    int after = hero.GetSkillValue(skill);
                    if (after <= before)
                    {
                        continue;
                    }

                    TextObject entry = new TextObject("{=cmt_training_skill_gain}{SKILL} +{GAIN} ({LEVEL})");
                    entry.SetTextVariable("SKILL", skill.Name);
                    entry.SetTextVariable("GAIN", after - before);
                    entry.SetTextVariable("LEVEL", after);
                    gains.Add(entry.ToString());
                }

                LastSkillSession[hero.StringId] = today;
                if (gains.Count > 0)
                {
                    TextObject line = new TextObject("{=cmt_training_result_line}{HERO}: {GAINS}");
                    line.SetTextVariable("HERO", hero.Name);
                    line.SetTextVariable("GAINS", string.Join(", ", gains.ToArray()));
                    lines.Add(line.ToString());
                }
            }

            ShowResult(title, string.Join("\n", lines.ToArray()));
        }

        private static bool Closed()
        {
            Settlement settlement = Settlement.CurrentSettlement;
            return !Open(settlement) || settlement.IsUnderSiege;
        }

        private static void ShowResult(TextObject title, string text)
        {
            InformationManager.ShowInquiry(new InquiryData(title.ToString(), text, true, false, new TextObject("{=WiNRdfsm}Done").ToString(), null, RefreshMenu, null), true);
        }

        private static void RefreshMenu()
        {
            if (Campaign.Current != null && Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu != null
                && Campaign.Current.CurrentMenuContext.GameMenu.StringId == MenuId)
            {
                GameMenu.SwitchToMenu(MenuId);
            }
        }

        private static int MaxAttribute
        {
            get { return Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute; }
        }

        private static bool CanRaiseAttribute(Hero hero)
        {
            foreach (CharacterAttribute attribute in Attributes.All)
            {
                if (hero.GetAttributeValue(attribute) < MaxAttribute)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<Hero> AttributeTrainees(out TextObject reason)
        {
            reason = null;
            List<Hero> heroes = Trainees();
            if (heroes.Count == 0)
            {
                reason = new TextObject("{=cmt_training_no_trainees}No one in your party can train here. Check Train yourself and Train companions in the mod options.");
                return heroes;
            }

            List<Hero> ready = new List<Hero>();
            int soonest = int.MaxValue;
            bool rested = false;
            foreach (Hero hero in heroes)
            {
                int daysLeft = DaysLeft(LastAttributeSession, hero, Settings.AttributeCooldownDays);
                if (daysLeft > 0)
                {
                    soonest = Math.Min(soonest, daysLeft);
                    continue;
                }

                rested = true;
                if (CanRaiseAttribute(hero))
                {
                    ready.Add(hero);
                }
            }

            if (ready.Count > 0)
            {
                return ready;
            }

            if (rested)
            {
                reason = new TextObject("{=cmt_training_attributes_maxed}Everyone ready to train already has every attribute at the maximum of {LEVEL}.");
                reason.SetTextVariable("LEVEL", MaxAttribute);
            }
            else
            {
                reason = new TextObject("{=cmt_training_cooldown}No one is ready to train yet. The next hero is ready {WHEN}.");
                reason.SetTextVariable("WHEN", Readiness(soonest));
            }

            return ready;
        }

        private static bool AttributeCondition(MenuCallbackArgs args)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            CastleMarketTownsSettings settings = Settings;
            if (!Open(settlement) || !settings.AttributeTraining)
            {
                return false;
            }

            MBTextManager.SetTextVariable("ATTRIBUTE_GOLD", settings.AttributeGoldCost);
            MBTextManager.SetTextVariable("ATTRIBUTE_INFLUENCE", settings.AttributeInfluenceCost);
            args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
            if (settlement.IsUnderSiege)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=cmt_training_siege}No one trains while the castle is under siege.");
                return true;
            }

            TextObject reason;
            if (AttributeTrainees(out reason).Count == 0)
            {
                args.IsEnabled = false;
                args.Tooltip = reason;
                return true;
            }

            TextObject cost = CostProblem(settings.AttributeGoldCost, settings.AttributeInfluenceCost);
            if (cost != null)
            {
                args.IsEnabled = false;
                args.Tooltip = cost;
                return true;
            }

            args.Tooltip = new TextObject("{=cmt_training_attribute_offer}Pick one hero and one attribute to raise by 1, up to the maximum of {LEVEL}.");
            args.Tooltip.SetTextVariable("LEVEL", MaxAttribute);
            return true;
        }

        private static void ChooseAttributeHero()
        {
            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (Hero hero in Trainees())
            {
                int daysLeft = DaysLeft(LastAttributeSession, hero, Settings.AttributeCooldownDays);
                bool maxed = !CanRaiseAttribute(hero);
                string hint = "";
                if (daysLeft > 0)
                {
                    TextObject wait = new TextObject("{=cmt_training_hero_waiting}Ready to train attributes {WHEN}.");
                    wait.SetTextVariable("WHEN", Readiness(daysLeft));
                    hint = wait.ToString();
                }
                else if (maxed)
                {
                    TextObject full = new TextObject("{=cmt_training_hero_maxed}Every attribute is at the maximum of {LEVEL}.");
                    full.SetTextVariable("LEVEL", MaxAttribute);
                    hint = full.ToString();
                }

                elements.Add(new InquiryElement(hero, hero.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(hero.CharacterObject)), daysLeft <= 0 && !maxed, hint));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                new TextObject("{=cmt_training_attribute_title}Train an attribute").ToString(),
                new TextObject("{=cmt_training_pick_hero}Who will train?").ToString(),
                elements, true, 1, 1, new TextObject("{=Y94H6XnK}Accept").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(),
                delegate(List<InquiryElement> chosen)
                {
                    Hero hero = chosen.Count > 0 ? chosen[0].Identifier as Hero : null;
                    if (hero != null)
                    {
                        ChooseAttribute(hero);
                    }
                }, null), true);
        }

        private static void ChooseAttribute(Hero hero)
        {
            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (CharacterAttribute attribute in Attributes.All)
            {
                int value = hero.GetAttributeValue(attribute);
                TextObject title = new TextObject("{=cmt_training_attribute_value}{ATTRIBUTE} ({VALUE})");
                title.SetTextVariable("ATTRIBUTE", attribute.Name);
                title.SetTextVariable("VALUE", value);
                string hint = attribute.Description != null ? attribute.Description.ToString() : "";
                elements.Add(new InquiryElement(attribute, title.ToString(), null, value < MaxAttribute, hint));
            }

            TextObject description = new TextObject("{=cmt_training_pick_attribute}Which attribute will {HERO} train?");
            description.SetTextVariable("HERO", hero.Name);
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                new TextObject("{=cmt_training_attribute_title}Train an attribute").ToString(), description.ToString(),
                elements, true, 1, 1, new TextObject("{=Y94H6XnK}Accept").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(),
                delegate(List<InquiryElement> chosen)
                {
                    CharacterAttribute attribute = chosen.Count > 0 ? chosen[0].Identifier as CharacterAttribute : null;
                    if (attribute != null)
                    {
                        ConfirmAttribute(hero, attribute);
                    }
                }, null), true);
        }

        private static void ConfirmAttribute(Hero hero, CharacterAttribute attribute)
        {
            CastleMarketTownsSettings settings = Settings;
            int value = hero.GetAttributeValue(attribute);
            TextObject text = new TextObject("{=cmt_training_attribute_confirm}Raise {HERO}'s {ATTRIBUTE} from {OLD} to {NEW} for {GOLD}{GOLD_ICON} and {INFLUENCE} influence? {HERO} then waits {DAYS} days before training an attribute again.");
            text.SetTextVariable("HERO", hero.Name);
            text.SetTextVariable("ATTRIBUTE", attribute.Name);
            text.SetTextVariable("OLD", value);
            text.SetTextVariable("NEW", value + 1);
            text.SetTextVariable("GOLD", settings.AttributeGoldCost);
            text.SetTextVariable("INFLUENCE", settings.AttributeInfluenceCost);
            text.SetTextVariable("DAYS", settings.AttributeCooldownDays);
            InformationManager.ShowInquiry(new InquiryData(new TextObject("{=cmt_training_attribute_title}Train an attribute").ToString(), text.ToString(), true, true, new TextObject("{=Y94H6XnK}Accept").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate { TrainAttribute(hero, attribute); }, null), true);
        }

        private static void TrainAttribute(Hero hero, CharacterAttribute attribute)
        {
            CastleMarketTownsSettings settings = Settings;
            TextObject reason;
            int before = hero.GetAttributeValue(attribute);
            if (Closed() || !settings.AttributeTraining || !AttributeTrainees(out reason).Contains(hero) || before >= MaxAttribute)
            {
                RefreshMenu();
                return;
            }

            TextObject cost = CostProblem(settings.AttributeGoldCost, settings.AttributeInfluenceCost);
            if (cost != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(cost.ToString()));
                RefreshMenu();
                return;
            }

            Pay(settings.AttributeGoldCost, settings.AttributeInfluenceCost);
            hero.HeroDeveloper.AddAttribute(attribute, 1, false);
            LastAttributeSession[hero.StringId] = Today();
            TextObject result = new TextObject("{=cmt_training_attribute_result}{HERO}'s {ATTRIBUTE} is now {VALUE}.");
            result.SetTextVariable("HERO", hero.Name);
            result.SetTextVariable("ATTRIBUTE", attribute.Name);
            result.SetTextVariable("VALUE", hero.GetAttributeValue(attribute));
            ShowResult(new TextObject("{=cmt_training_attribute_title}Train an attribute"), result.ToString());
        }
    }
}
