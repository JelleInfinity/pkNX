using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using pkNX.Containers;
using pkNX.Game;
using pkNX.Randomization;
using pkNX.Structures;
using pkNX.Structures.FlatBuffers;
using pkNX.WinForms.Subforms;
using static pkNX.Structures.Species;

namespace pkNX.WinForms.Controls;

internal class EditorPLA : EditorBase
{
    private GameData8a Data => ((GameManagerPLA)ROM).Data;
    protected internal EditorPLA(GameManagerPLA rom) : base(rom) => CheckOodleDllPresence();

    private static void CheckOodleDllPresence()
    {
        const string file = $"{Oodle.OodleLibraryPath}.dll";
        var dir = Application.StartupPath;
        var path = Path.Combine(dir, file);
        if (!File.Exists(path))
            WinFormsUtil.Alert($"{file} not found in the executable folder", "Some decompression functions may cause errors.");
    }

    public void EditCommon()
    {
        var text = ROM.GetFilteredFolder(GameFile.GameText, z => Path.GetExtension(z) == ".dat");
        var config = new TextConfig(ROM.Game);
        var tc = new TextContainer(text, config);
        using var form = new TextEditor(tc, TextEditor.TextEditorMode.Common);
        form.ShowDialog();
        if (!form.Modified)
            text.CancelEdits();
    }

    public void EditScript()
    {
        var text = ROM.GetFilteredFolder(GameFile.StoryText, z => Path.GetExtension(z) == ".dat");
        var config = new TextConfig(ROM.Game);
        var tc = new TextContainer(text, config);
        using var form = new TextEditor(tc, TextEditor.TextEditorMode.Script);
        form.ShowDialog();
        if (!form.Modified)
            text.CancelEdits();
    }
    public void EditNPCModelSet()
    {
        var gfp = (GFPack)ROM.GetFile(GameFile.Resident);
        var index = gfp.GetIndexFull("bin/field/param/placement/common/npc_model_set.bin");

        var obj = FlatBufferConverter.DeserializeFrom<NPCModelSet8a>(gfp[index]);
        var result = PopFlat(obj.Table, "NPC Model Set Editor", z => z.NPCHash.ToString("X16"));
        if (!result)
            return;
        gfp[index] = FlatBufferConverter.SerializeFrom(obj);
    }

    public void EditTrainers()
    {
        var folder = ROM.GetFilteredFolder(GameFile.TrainerData);
        var cache = new DataCache<TrData8a>(folder)
        {
            Create = FlatBufferConverter.DeserializeFrom<TrData8a>,
            Write = FlatBufferConverter.SerializeFrom,
        };
        var names = folder.GetPaths().Select(Path.GetFileNameWithoutExtension).ToArray();
        using var form = new GenericEditor<TrData8a>(cache, names, "Trainers", Randomize, canSave: true);
        form.ShowDialog();

        void Randomize()
        {
            var settings = EditUtil.Settings.Species;
            var rand = new SpeciesRandomizer(ROM.Info, Data.PersonalData);
            rand.Initialize(settings, GetSpeciesBanlist());

            for (int i = 0; i < cache.Length; i++)
            {
                foreach (var t in cache[i].Team)
                {
                    t.Species = rand.GetRandomSpecies(t.Species);
                    t.Form = GetRandomForm(t.Species);
                    t.Gender = (int)FixedGender.Random;
                    t.Nature = NatureType8a.Random;
                    t.Move_01.Move = t.Move_02.Move = t.Move_03.Move = t.Move_04.Move = 0;
                    t.Move_01.Mastered = t.Move_02.Mastered = t.Move_03.Mastered = t.Move_04.Mastered = true;
                    t.Shiny = Randomization.Util.Random.Next(0, 100 + 1) < 3;
                    t.IsOybn = Randomization.Util.Random.Next(0, 100 + 1) < 3;
                }
            }
        }

        if (!form.Modified)
            cache.CancelEdits();
        else
            cache.Save();
    }

    public void EditMoveShop()
    {
        var names = ROM.GetStrings(TextName.MoveNames);
        var data = ROM.GetFile(GameFile.MoveShop);
        var obj = FlatBufferConverter.DeserializeFrom<MoveShopTable8a>(data[0]);
        var result = PopFlat(obj.Table, "Move Shop Editor", z => names[z.Move]);
        if (!result)
            return;
        data[0] = FlatBufferConverter.SerializeFrom(obj);
    }

    public void PopFlat<T1, T2>(GameFile file, string title, Func<T2, string> getName, Action? rand = null, bool canSave = true) where T1 : class, IFlatBufferArchive<T2> where T2 : class
    {
        var obj = ROM.GetFile(file);
        var data = obj[0];
        var root = FlatBufferConverter.DeserializeFrom<T1>(data);
        var arr = root.Table;
        if (!PopFlat(arr, title, getName, rand, canSave))
            return;
        obj[0] = FlatBufferConverter.SerializeFrom(root);
    }

    private static bool PopFlat<T2>(T2[] arr, string title, Func<T2, string> getName, Action? rand = null, bool canSave = true) where T2 : class
    {
        var names = arr.Select(getName).ToArray();
        var cache = new DataCache<T2>(arr);
        using var form = new GenericEditor<T2>(cache, names, title, randomize: rand, canSave: canSave);
        form.ShowDialog();
        return form.Modified;
    }


    public void EditThrow_Param()
    {
        PopFlat<ThrowParamTable8a, ThrowParam8a>(GameFile.ThrowParam, "Throw Param Editor", z => z.Hash.ToString("X16"));
    }

    public void EditThrow_PermissionSet_Param()
    {
        PopFlat<ThrowPermissionSetDictionary8a, ThrowPermissionSetEntry8a>(GameFile.ThrowPermissionSet, "Throw Permission Editor", z => z.Hash_00.ToString("X16"));
    }

    public void EditThrowableBaitParam_Dictionary()
    {
        PopFlat<ThrowableBaitParamDictionary8a, ThrowableBaitParamEntry8a>(GameFile.ThrowableBaitParam, "Throwable Bait Param Dictionary Editor", z => z.Hash_00.ToString("X16"));
    }
    public void EditThrowable_Param()
    {
        var itemNames = ROM.GetStrings(TextName.ItemNames);
        PopFlat<ThrowableParamTable8a, ThrowableParam8a>(GameFile.ThrowableParam, "Throwable Param Editor", z => itemNames[z.ItemID]);
    }
    public void EditThrow_Resource_Dictionary()
    {
        PopFlat<ThrowableResourceDictionary8a, ThrowableResourceEntry8a>(GameFile.ThrowableResource, "Throwable Resource Dictionary Editor", z => z.Hash_00.ToString("X16"));
    }
    public void EditThrow_ResourceSet_Dictionary()
    {
        PopFlat<ThrowableResourceSetDictionary8a, ThrowableResourceSetEntry8a>(GameFile.ThrowableResourceSet, "Throwable ResourceSet Dictionary Editor", z => z.Hash_00.ToString("X16"));
    }

    public void EditHa_Shop_Data()
    {
        var names = ROM.GetStrings(TextName.ItemNames);
        var data = ROM.GetFile(GameFile.HaShop);
        var obj = FlatBufferConverter.DeserializeFrom<HaShopTable8a>(data[0]);
        var result = PopFlat(obj.Table, "ha_shop_data Editor", z => names[z.ItemID], Randomize);
        if (!result)
            return;

        void Randomize()
        {
            foreach (var t in obj.Table)
            {
                if (Legal.Pouch_Recipe_LA.Contains((ushort)t.ItemID)) // preserve recipes
                    continue;
                t.ItemID = Legal.Pouch_Items_LA[Randomization.Util.Random.Next(Legal.Pouch_Items_LA.Length)];
            }
        }

        data[0] = FlatBufferConverter.SerializeFrom(obj);
    }

    public void EditApp_Config_List()
    {
        PopFlat<AppConfigList8a, AppconfigEntry8a>(GameFile.AppConfigList, "App Config List", z => z.OriginalPath);
    }

    public void EditArea_Weather()
    {
        var gfp = (GFPack)ROM.GetFile(GameFile.Resident);
        var data = gfp[2065];
        var obj = FlatBufferConverter.DeserializeFrom<AreaWeatherTable8a>(data);
        var result = PopFlat(obj.Table, "Area Weather Editor", z => z.Hash.ToString("X16"));
        if (!result)
            return;
        gfp[2065] = FlatBufferConverter.SerializeFrom(obj);
    }

    public void EditStatic()
    {
        var names = ROM.GetStrings(TextName.SpeciesNames);
        var obj = ROM.GetFile(GameFile.EncounterStatic);
        var data = obj[0];
        var root = FlatBufferConverter.DeserializeFrom<EventEncount8aArchive>(data);
        var entries = root.Table;
        var result = PopFlat(entries, "Static Encounter Editor", z => $"{z.EncounterName} ({GetDetail(z, names)})", () => Randomize(entries));
        if (result)
            obj[0] = FlatBufferConverter.SerializeFrom(root);

        static string GetDetail(EventEncount8a z, string[] names)
        {
            if (z.Table is not { Length: not 0 } x)
                return "No Entries";
            var s = x[0];
            return $"{names[s.Species]}{(s.Form == 0 ? "" : $"-{s.Form}")} @ Lv. {s.Level}";
        }

        void Randomize(IEnumerable<EventEncount8a> arr)
        {
            var settings = EditUtil.Settings.Species;
            var rand = new SpeciesRandomizer(ROM.Info, Data.PersonalData);
            settings.Legends = false;
            rand.Initialize(settings, GetSpeciesBanlist());
            foreach (var entry in arr)
            {
                if (entry.Table is not { Length: > 0 } x)
                    continue;
                foreach (var t in x)
                {
                    bool isBoss = t.Species is (int)Kleavor or (int)Lilligant or (int)Arcanine or (int)Electrode or (int)Avalugg or (int)Arceus;
                    if (isBoss) // don't randomize boss battles
                        continue;
                    if (Legal.Legendary_8a.Contains(t.Species)) // don't randomize legendaries
                        continue;

                    t.Species = rand.GetRandomSpecies(t.Species);
                    t.Form = (byte)GetRandomForm(t.Species);
                    t.Nature = (int)Nature.Random;
                    t.Gender = (int)FixedGender.Random;
                    t.ShinyLock = ShinyType8a.Random;
                    t.Move1 = t.Move2 = t.Move3 = t.Move4 = 0;
                    t.Mastered1 = t.Mastered2 = t.Mastered3 = t.Mastered4 = true;
                    t.IV_HP = t.IV_ATK = t.IV_DEF = t.IV_SPA = t.IV_SPD = t.IV_SPE = 31;
                    t.GV_HP = t.GV_ATK = t.GV_DEF = t.GV_SPA = t.GV_SPD = t.GV_SPE = 10;
                    t.Height = t.Weight = -1;
                }
            }
        }
    }

    public void EditGift()
    {
        var names = ROM.GetStrings(TextName.SpeciesNames);
        var obj = ROM.GetFile(GameFile.EncounterGift);
        var data = obj[0];
        var root = FlatBufferConverter.DeserializeFrom<PokeAdd8aArchive>(data);
        var entries = root.Table;
        var result = PopFlat(entries, "Gift Encounter Editor", z => $"{names[z.Species]}{(z.Form == 0 ? "" : $"-{z.Form}")} @ Lv. {z.Level}", () => Randomize(entries));
        if (result)
            obj[0] = FlatBufferConverter.SerializeFrom(root);

        void Randomize(IEnumerable<PokeAdd8a> arr)
        {
            var settings = EditUtil.Settings.Species;
            var rand = new SpeciesRandomizer(ROM.Info, Data.PersonalData);
            settings.Legends = false;
            rand.Initialize(settings, GetSpeciesBanlist());
            foreach (var t in arr)
            {
                t.Species = rand.GetRandomSpecies(t.Species);
                t.Form = (byte)GetRandomForm(t.Species);
                t.Nature = NatureType8a.Random;
                t.Gender = (int)FixedGender.Random;
                t.ShinyLock = ShinyType8a.Random;
                t.Ball = Randomization.Util.Random.Next(27, 37); // [Strange, Origin]
                t.Move1 = t.Move2 = t.Move3 = t.Move4 = 0;
                t.Height = t.Weight = -1;
            }
        }
    }

    public int[] GetSpeciesBanlist()
    {
        var pt = Data.PersonalData;
        var hasForm = new HashSet<int>();
        var banned = new HashSet<int>();
        foreach (var pi in pt.Table.Cast<IPersonalMisc_1>())
        {
            if (pi.IsPresentInGame)
            {
                banned.Remove(pi.ModelID);
                hasForm.Add(pi.ModelID);
            }
            else if (!hasForm.Contains(pi.ModelID))
            {
                banned.Add(pi.ModelID);
            }
        }
        return banned.ToArray();
    }

    public int GetRandomForm(int spec)
    {
        var pt = Data.PersonalData;
        var formRand = pt.Table.Cast<IPersonalMisc_1>()
            .Where(z => z.IsPresentInGame && !(Legal.BattleExclusiveForms.Contains(z.ModelID) || Legal.BattleFusions.Contains(z.ModelID)))
            .GroupBy(z => z.ModelID)
            .ToDictionary(z => z.Key, z => z.ToList());

        if (!formRand.TryGetValue((ushort)spec, out var entries))
            return 0;
        var count = entries.Count;

        return (Species)spec switch
        {
            Growlithe or Arcanine or Voltorb or Electrode or Typhlosion or Qwilfish or Samurott or Lilligant or Zorua or Zoroark or Braviary or Sliggoo or Goodra or Avalugg or Decidueye => 1,
            Basculin => 2,
            Kleavor => 0,
            _ => Randomization.Util.Random.Next(0, count),
        };
    }

    public void EditPersonal_Raw()
    {
        var names = ROM.GetStrings(TextName.SpeciesNames);
        PopFlat<PersonalTableLAfb, PersonalInfoLAfb>(GameFile.PersonalStats, "Personal Info Editor (Raw)", z => $"{names[z.Species]}{(z.Form == 0 ? "" : $"-{z.Form}")}");
    }

    public void EditLearnset_Raw()
    {
        var names = ROM.GetStrings(TextName.SpeciesNames);
        PopFlat<Learnset8a, Learnset8aMeta>(GameFile.Learnsets, "Learnset Editor (Raw)", z => $"{names[z.Species]}{(z.Form == 0 ? "" : $"-{z.Form}")}");
    }

    public void EditMiscSpeciesInfo()
    {
        var names = ROM.GetStrings(TextName.SpeciesNames);
        PopFlat<PokeMiscTable8a, PokeMisc8a>(GameFile.PokeMisc, "Misc Species Info Editor", z => $"{names[z.Species]}{(z.Form == 0 ? "" : $"-{z.Form}")} ~ {z.Value}");
    }

    public void EditMap_Viewer()
    {
        var resident = (GFPack)ROM.GetFile(GameFile.Resident);
        using var form = new MapViewer8a((GameManagerPLA)ROM, resident);
        form.ShowDialog();
    }

    public void EditAreas()
    {
        using var form = new AreaEditor8a((GameManagerPLA)ROM);
        form.ShowDialog();
    }

    public void EditMoves()
    {
        var obj = ROM[GameFile.MoveStats]; // folder
        var cache = new DataCache<Waza8a>(obj)
        {
            Create = FlatBufferConverter.DeserializeFrom<Waza8a>,
            Write = FlatBufferConverter.SerializeFrom,
        };
        using var form = new GenericEditor<Waza8a>(cache, ROM.GetStrings(TextName.MoveNames), "Move Editor");
        form.ShowDialog();
        if (!form.Modified)
        {
            cache.CancelEdits();
            return;
        }

        cache.Save();
        Data.MoveData.ClearAll(); // force reload if used again
    }

    public void EditItems()
    {
        var obj = ROM[GameFile.ItemStats]; // mini
        var data = obj[0];
        var items = Item8a.GetArray(data);
        var cache = new DataCache<Item8a>(items);
        using var form = new GenericEditor<Item8a>(cache, ROM.GetStrings(TextName.ItemNames), "Item Editor");
        form.ShowDialog();
        if (!form.Modified)
        {
            cache.CancelEdits();
            return;
        }

        obj[0] = Item8a.SetArray(items, data);
    }

    public void EditPokemon()
    {
        var editor = new PokeEditor8a
        {
            Personal = Data.PersonalData,
            PokeMisc = Data.PokeMiscData,
            Evolve = Data.EvolutionData,
            Learn = Data.LevelUpData,
            FieldDropTables = Data.FieldDrops,
            BattleDropTabels = Data.BattleDrops,
            DexResearch = Data.DexResearch
        };
        using var form = new PokeDataUI8a(editor, ROM, Data);
        form.ShowDialog();
        if (!form.Modified)
            editor.CancelEdits();
        else
            editor.Save();
    }


    public void PopFlatConfig(GameFile file, string title)
    {
        var obj = ROM.GetFile(file); // flatbuffer
        var data = obj[0];
        var root = FlatBufferConverter.DeserializeFrom<ConfigureTable8a>(data);
        var cache = new DataCache<Configure8aEntry>(root.Table);
        var names = root.Table.Select(z => z.Name).ToArray();
        using var form = new GenericEditor<Configure8aEntry>(cache, names, title);
        form.ShowDialog();
        if (!form.Modified)
            return;
        obj[0] = FlatBufferConverter.SerializeFrom(root);
    }


    public void EditAICommonConfig() => PopFlatConfig(GameFile.AICommonConfig, "AICommonConfig Editor");
    public void EditAIExcitingConfig() => PopFlatConfig(GameFile.AIExcitingConfig, "AIExcitingConfig Editor");
    public void Editai_field_waza_config() => PopFlatConfig(GameFile.ai_field_waza_config, "ai_field_waza_config Editor");
    public void Editai_semi_legend_config() => PopFlatConfig(GameFile.ai_semi_legend_config, "ai_semi_legend_config Editor");
    public void Editai_tiredness_config() => PopFlatConfig(GameFile.ai_tiredness_config, "ai_tiredness_config Editor");
    public void EditAppConfigList() => PopFlatConfig(GameFile.AppConfigList, "AppConfigList Editor");
    public void Editappli_hud_config() => PopFlatConfig(GameFile.appli_hud_config, "appli_hud_config Editor");
    public void Editappli_staffroll_config() => PopFlatConfig(GameFile.appli_staffroll_config, "appli_staffroll_config Editor");
    public void Editappli_tips_config() => PopFlatConfig(GameFile.appli_tips_config, "appli_tips_config Editor");
    public void Editbattle_common_config() => PopFlatConfig(GameFile.battle_common_config, "battle_common_config Editor");
    public void Editbattle_end_config() => PopFlatConfig(GameFile.battle_end_config, "battle_end_config Editor");
    public void Editbattle_in_config() => PopFlatConfig(GameFile.battle_in_config, "battle_in_config Editor");
    public void EditBattleLogicConfig() => PopFlatConfig(GameFile.BattleLogicConfig, "BattleLogicConfig Editor");
    public void Editbattle_start_config() => PopFlatConfig(GameFile.battle_start_config, "battle_start_config Editor");
    public void EditBattleViewConfig() => PopFlatConfig(GameFile.BattleViewConfig, "BattleViewConfig Editor");
    public void Editbattle_vsns_config() => PopFlatConfig(GameFile.battle_vsns_config, "battle_vsns_config Editor");
    public void Editbuddy_battle_config() => PopFlatConfig(GameFile.buddy_battle_config, "buddy_battle_config Editor");
    public void Editbuddy_config() => PopFlatConfig(GameFile.buddy_config, "buddy_config Editor");
    public void Editbuddy_direct_item_config() => PopFlatConfig(GameFile.buddy_direct_item_config, "buddy_direct_item_config Editor");
    public void Editbuddy_group_talk_config() => PopFlatConfig(GameFile.buddy_group_talk_config, "buddy_group_talk_config Editor");
    public void Editbuddy_landmark_config() => PopFlatConfig(GameFile.buddy_landmark_config, "buddy_landmark_config Editor");
    public void Editbuddy_npc_reaction_config() => PopFlatConfig(GameFile.buddy_npc_reaction_config, "buddy_npc_reaction_config Editor");
    public void Editbuddy_player_mode_config() => PopFlatConfig(GameFile.buddy_player_mode_config, "buddy_player_mode_config Editor");
    public void Editbuddy_warp_config() => PopFlatConfig(GameFile.buddy_warp_config, "buddy_warp_config Editor");
    public void Editcharacter_biped_ik_config() => PopFlatConfig(GameFile.character_biped_ik_config, "character_biped_ik_config Editor");
    public void Editcharacter_blink_config() => PopFlatConfig(GameFile.character_blink_config, "character_blink_config Editor");
    public void Editcharacter_controller_config() => PopFlatConfig(GameFile.character_controller_config, "character_controller_config Editor");
    public void Editcharacter_look_at_config() => PopFlatConfig(GameFile.character_look_at_config, "character_look_at_config Editor");
    public void EditCaptureConfig() => PopFlatConfig(GameFile.CaptureConfig, "CaptureConfig Editor");
    public void Editcommon_general_config() => PopFlatConfig(GameFile.common_general_config, "common_general_config Editor");
    public void Editcommon_item_config() => PopFlatConfig(GameFile.common_item_config, "common_item_config Editor");
    public void Editdemo_config() => PopFlatConfig(GameFile.demo_config, "demo_config Editor");
    public void Editenv_poke_voice_config() => PopFlatConfig(GameFile.env_poke_voice_config, "env_poke_voice_config Editor");
    public void Editevent_balloonrun_config() => PopFlatConfig(GameFile.event_balloonrun_config, "event_balloonrun_config Editor");
    public void Editevent_balloonthrow_config() => PopFlatConfig(GameFile.event_balloonthrow_config, "event_balloonthrow_config Editor");
    public void Editevent_bandit_config() => PopFlatConfig(GameFile.event_bandit_config, "event_bandit_config Editor");
    public void Editevent_culling_config() => PopFlatConfig(GameFile.event_culling_config, "event_culling_config Editor");
    public void Editevent_dither_config() => PopFlatConfig(GameFile.event_dither_config, "event_dither_config Editor");
    public void EditEventFarmConfig() => PopFlatConfig(GameFile.EventFarmConfig, "EventFarmConfig Editor");
    public void Editevent_game_over_config() => PopFlatConfig(GameFile.event_game_over_config, "event_game_over_config Editor");
    public void Editevent_item_config() => PopFlatConfig(GameFile.event_item_config, "event_item_config Editor");
    public void Editevent_mkrg_reward_config() => PopFlatConfig(GameFile.event_mkrg_reward_config, "event_mkrg_reward_config Editor");
    public void Editevent_quest_board_config() => PopFlatConfig(GameFile.event_quest_board_config, "event_quest_board_config Editor");
    public void Editevent_restriction_battle() => PopFlatConfig(GameFile.event_restriction_battle, "event_restriction_battle Editor");
    public void Editevent_work() => PopFlatConfig(GameFile.event_work, "event_work Editor");

    public void Edit_npc_ai_config() => PopFlatConfig(GameFile.npc_ai_config, "npc_ai_config");
    public void Edit_npc_controller_config() => PopFlatConfig(GameFile.npc_controller_config, "npc_controller_config");
    public void Edit_npc_creater_config() => PopFlatConfig(GameFile.npc_creater_config, "npc_creater_config");
    public void Edit_npc_pokemon_ai_config() => PopFlatConfig(GameFile.npc_pokemon_ai_config, "npc_pokemon_ai_config");
    public void Edit_npc_popup_config() => PopFlatConfig(GameFile.npc_popup_config, "npc_popup_config");
    public void Edit_npc_talk_table_config() => PopFlatConfig(GameFile.npc_talk_table_config, "npc_talk_table_config");
    public void Edit_player_camera_shake_config() => PopFlatConfig(GameFile.player_camera_shake_config, "player_camera_shake_config");
    public void Edit_player_collision_config() => PopFlatConfig(GameFile.player_collision_config, "player_collision_config");
    public void Edit_PlayerConfig() => PopFlatConfig(GameFile.PlayerConfig, "PlayerConfig");
    public void Edit_player_controller_config() => PopFlatConfig(GameFile.player_controller_config, "player_controller_config");
    public void Edit_player_face_config() => PopFlatConfig(GameFile.player_face_config, "player_face_config");
    public void Edit_pokemon_config() => PopFlatConfig(GameFile.pokemon_config, "pokemon_config");
    public void Edit_pokemon_controller_config() => PopFlatConfig(GameFile.pokemon_controller_config, "pokemon_controller_config");
    public void Edit_EvolutionConfig() => PopFlatConfig(GameFile.EvolutionConfig, "EvolutionConfig");
    public void Edit_pokemon_friendship_config() => PopFlatConfig(GameFile.pokemon_friendship_config, "pokemon_friendship_config");
    public void Edit_ShinyRolls() => PopFlatConfig(GameFile.ShinyRolls, "ShinyRolls");
    public void Edit_SizeScaleConfig() => PopFlatConfig(GameFile.SizeScaleConfig, "SizeScaleConfig");
    public void Edit_ride_basurao_collision_config() => PopFlatConfig(GameFile.ride_basurao_collision_config, "ride_basurao_collision_config");
    public void Edit_ride_basurao_config() => PopFlatConfig(GameFile.ride_basurao_config, "ride_basurao_config");
    public void Edit_ride_change_config() => PopFlatConfig(GameFile.ride_change_config, "ride_change_config");
    public void Edit_ride_common_config() => PopFlatConfig(GameFile.ride_common_config, "ride_common_config");
    public void Edit_ride_nyuura_collision_config() => PopFlatConfig(GameFile.ride_nyuura_collision_config, "ride_nyuura_collision_config");
    public void Edit_ride_nyuura_config() => PopFlatConfig(GameFile.ride_nyuura_config, "ride_nyuura_config");
    public void Edit_ride_nyuura_controller_config() => PopFlatConfig(GameFile.ride_nyuura_controller_config, "ride_nyuura_controller_config");
    public void Edit_ride_odoshishi_collision_config() => PopFlatConfig(GameFile.ride_odoshishi_collision_config, "ride_odoshishi_collision_config");
    public void Edit_ride_odoshishi_config() => PopFlatConfig(GameFile.ride_odoshishi_config, "ride_odoshishi_config");
    public void Edit_ride_ringuma_collision_config() => PopFlatConfig(GameFile.ride_ringuma_collision_config, "ride_ringuma_collision_config");
    public void Edit_ride_ringuma_config() => PopFlatConfig(GameFile.ride_ringuma_config, "ride_ringuma_config");
    public void Edit_ride_ringuma_controller_config() => PopFlatConfig(GameFile.ride_ringuma_controller_config, "ride_ringuma_controller_config");
    public void Edit_ride_whooguru_collision_config() => PopFlatConfig(GameFile.ride_whooguru_collision_config, "ride_whooguru_collision_config");
    public void Edit_ride_whooguru_config() => PopFlatConfig(GameFile.ride_whooguru_config, "ride_whooguru_config");
    public void Edit_ride_whooguru_controller_config() => PopFlatConfig(GameFile.ride_whooguru_controller_config, "ride_whooguru_controller_config");
    public void Edit_sound_config() => PopFlatConfig(GameFile.sound_config, "sound_config");
    public void Edit_water_motion() => PopFlatConfig(GameFile.water_motion, "water_motion");










    public void EditShinyRate() => PopFlatConfig(GameFile.ShinyRolls, "Shiny Rate Editor");
    public void EditWormholeRate() => PopFlatConfig(GameFile.WormholeConfig, "Wormhole Config Editor");
    public void EditCapture_Config() => PopFlatConfig(GameFile.CaptureConfig, "CaptureConfig Editor");
    public void EditBattle_Logic_Config() => PopFlatConfig(GameFile.BattleLogicConfig, "Battle Logic Config Editor");
    public void EditEvent_Farm_Config() => PopFlatConfig(GameFile.EventFarmConfig, "Event Farm Config Editor");
    public void EditPlayer_Config() => PopFlatConfig(GameFile.PlayerConfig, "Player Config Editor");



    public void EditField_anime_framerate_config() => PopFlatConfig(GameFile.field_anime_framerate_config, "field_anime_framerate_config");
    public void EditField_area_speed_config() => PopFlatConfig(GameFile.field_area_speed_config, "field_area_speed_config");
    public void EditField_camera_config() => PopFlatConfig(GameFile.field_camera_config, "field_camera_config");
    public void EditField_capture_director_config() => PopFlatConfig(GameFile.field_capture_director_config, "field_capture_director_config");
    public void EditField_chara_viewer_config() => PopFlatConfig(GameFile.field_chara_viewer_config, "field_chara_viewer_config");
    public void EditField_common_config() => PopFlatConfig(GameFile.field_common_config, "field_common_config");
    public void EditField_direct_item_config() => PopFlatConfig(GameFile.field_direct_item_config, "field_direct_item_config");
    public void EditField_env_config() => PopFlatConfig(GameFile.field_env_config, "field_env_config");
    public void EditField_item() => PopFlatConfig(GameFile.field_item, "field_item");
    public void EditField_item_respawn() => PopFlatConfig(GameFile.field_item_respawn, "field_item_respawn");
    public void EditField_landmark_incite_config() => PopFlatConfig(GameFile.field_landmark_incite_config, "field_landmark_incite_config");
    public void EditField_lockon_config() => PopFlatConfig(GameFile.field_lockon_config, "field_lockon_config");
    public void EditField_my_poke_ball_hit_none_target_config() => PopFlatConfig(GameFile.field_my_poke_ball_hit_none_target_config, "field_my_poke_ball_hit_none_target_config");
    public void EditField_obstruction_waza_config() => PopFlatConfig(GameFile.field_obstruction_waza_config, "field_obstruction_waza_config");
    public void EditField_pokemon_slope_config() => PopFlatConfig(GameFile.field_pokemon_slope_config, "field_pokemon_slope_config");
    public void EditField_quest_destination_config() => PopFlatConfig(GameFile.field_quest_destination_config, "field_quest_destination_config");
    public void EditField_shadow_config() => PopFlatConfig(GameFile.field_shadow_config, "field_shadow_config");
    public void EditField_throw_config() => PopFlatConfig(GameFile.field_throw_config, "field_throw_config");
    public void EditField_throwable_after_hit_config() => PopFlatConfig(GameFile.field_throwable_after_hit_config, "field_throwable_after_hit_config");
    public void EditField_vigilance_bgm_config() => PopFlatConfig(GameFile.field_vigilance_bgm_config, "field_vigilance_bgm_config");
    public void EditField_weathering_config() => PopFlatConfig(GameFile.field_weathering_config, "field_weathering_config");
    public void EditField_wild_pokemon_config() => PopFlatConfig(GameFile.field_wild_pokemon_config, "field_wild_pokemon_config");

    public void EditTestConfig() => PopFlatConfig(GameFile.TestConfig, "Test Config");

    public void EditField_Landmark_Config() => PopFlatConfig(GameFile.FieldLandmarkConfig, "Field Landmark Config Editor");
    public void EditBattle_View_Config() => PopFlatConfig(GameFile.BattleViewConfig, "Battle View Config Editor");
    public void EditAICommon_Config() => PopFlatConfig(GameFile.AICommonConfig, "AI Common Config Editor");
    public void EdiField_Spawner_Config() => PopFlatConfig(GameFile.FieldSpawnerConfig, "Field Spawner Config Editor");
    public void EditOutbreak_Config() => PopFlatConfig(GameFile.OutbreakConfig, "Outbreak Config Editor");
    public void EditEvolution_Config() => PopFlatConfig(GameFile.EvolutionConfig, "Evolution Config Editor");
    public void EditBall_Throw_Config() => PopFlatConfig(GameFile.BallThrowConfig, "Ball Throw Config Editor");
    public void EditSize_Scale_Config() => PopFlatConfig(GameFile.SizeScaleConfig, "Size Scale Config Editor");

    public void EditEvolutions()
    {
        var names = ROM.GetStrings(TextName.SpeciesNames);
        PopFlat<EvolutionTable8, EvolutionSet8a>(GameFile.Evolutions, "Evolution Editor",
            z => $"{names[z.Species]}{(z.Form != 0 ? $"-{z.Form}" : "")}");
    }

    public void EditOutbreakDetail()
        => PopFlat<MassOutbreakTable8a, MassOutbreak8a>(GameFile.Outbreak, "Outbreak Proc Editor", z => z.WorkValueName);

    public void EditNewOutbreak_Group()
        => PopFlat<NewHugeOutbreakGroupArchive8a, NewHugeOutbreakGroup8a>(GameFile.NewHugeGroup, "New Outbreak Group Editor", z => z.Group.ToString("X16"));

    public void EditNewOutbreak_GroupLottery()
        => PopFlat<NewHugeOutbreakGroupLotteryArchive8a, NewHugeOutbreakGroupLottery8a>(GameFile.NewHugeGroupLottery, "New Outbreak Group Lottery Editor", z => z.LotteryGroup.ToString("X16"));

    public void EditNewOutbreak_Lottery()
        => PopFlat<NewHugeOutbreakLotteryArchive8a, NewHugeOutbreakLottery8a>(GameFile.NewHugeLottery, "New Outbreak Lottery Editor", z => z.LotteryGroupString);

    public void EditNewOutbreak_TimeLimit()
        => PopFlat<NewHugeOutbreakTimeLimitArchive8a, NewHugeOutbreakTimeLimit8a>(GameFile.NewHugeTimeLimit, "New Outbreak Time Limit Editor", z => z.Duration.ToString());

    public void EditSymbolBehave()
    {
        var names = ROM.GetStrings(TextName.SpeciesNames);
        PopFlat<PokeAIArchive8a, PokeAI8a>(GameFile.SymbolBehave, "Symbol Behavior Editor", z => $"{names[z.Species]}{(z.Form != 0 ? $"-{z.Form}" : "")}");
    }

    public void EditMasterDump()
    {
        using var md = new DumperPLA((GameManagerPLA)ROM);
        md.ShowDialog();
    }
}
