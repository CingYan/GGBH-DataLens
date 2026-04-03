using System;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace MOD_b4qnSo
{
    public class ModMain
    {
        private const string VERSION = "datadump-v4";

        private static void Log(string msg)
        {
            MelonLogger.Msg("[DataDump " + VERSION + "] " + msg);
        }

        public void Init()
        {
            Log("=== Init start ===");
            g.timer.Frame(new Action(() => { RunAllDumps(); }), 300, false);
            Log("=== Init done (" + VERSION + ") ===");
        }

        public void Destroy() { Log("Destroy (" + VERSION + ")"); }

        private static string Esc(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n"))
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            return val;
        }

        private static string Tr(string key)
        {
            if (string.IsNullOrEmpty(key) || key == "0") return "";
            try
            {
                string t = ConfLocalText.GetText(key);
                if (!string.IsNullOrEmpty(t) && t != key) return t;
            }
            catch { }
            return "";
        }

        private static void Save(string file, StringBuilder sb, int count)
        {
            try
            {
                File.WriteAllText(file, sb.ToString(), new UTF8Encoding(true));
                Log("[" + file + "] " + count + " entries");
            }
            catch (Exception ex) { Log("[" + file + "] write error: " + ex.Message); }
        }

        // ========== Main ==========

        private void RunAllDumps()
        {
            Log("=== DATA DUMP START ===");

            int a = DumpLuck();
            int b = DumpItems();
            int c = DumpDrama();
            int d = DumpSkills();
            int e = DumpSchools();
            int f = DumpNpcs();

            Log("=== DATA DUMP COMPLETE ===");
            Log("Summary: luck=" + a + " item=" + b + " drama=" + c
                + " skill=" + d + " school=" + e + " npc=" + f
                + " total=" + (a + b + c + d + e + f));
        }

        // ========== 1. ALL Luck ==========

        private int DumpLuck()
        {
            Log("[LUCK] dumping ALL...");
            var sb = new StringBuilder();
            sb.AppendLine("id,key,display,type,level,isModExtend");
            int count = 0;
            try
            {
                var allConf = g.conf.roleCreateFeature.allConfList;
                if (allConf == null) { Log("[LUCK] null"); return 0; }
                int i = 0;
                while (true)
                {
                    try
                    {
                        var item = allConf[i];
                        if (item == null) break;
                        string d = Tr(item.name);
                        if (string.IsNullOrEmpty(d)) d = item.name;
                        sb.AppendLine(item.id + "," + Esc(item.name) + "," + Esc(d)
                            + "," + item.type + "," + item.level
                            + "," + (item.isModExtend ? "MOD" : "BASE"));
                        count++;
                        i++;
                    }
                    catch { break; }
                }
            }
            catch (Exception ex) { Log("[LUCK] " + ex.Message); }
            Save("dump_luck.csv", sb, count);
            return count;
        }

        // ========== 2. ALL Items ==========

        private int DumpItems()
        {
            Log("[ITEM] dumping ALL...");
            var sb = new StringBuilder();
            sb.AppendLine("id,name_key,display,type,className,level,worth,desc_display,isModExtend");
            int count = 0;
            try
            {
                var allConf = g.conf.itemProps.allConfList;
                if (allConf == null) { Log("[ITEM] null"); return 0; }
                int i = 0;
                while (true)
                {
                    try
                    {
                        var item = allConf[i];
                        if (item == null) break;
                        string nameD = Tr(item.name);
                        if (string.IsNullOrEmpty(nameD)) nameD = item.name;
                        string descD = Tr(item.desc);
                        if (string.IsNullOrEmpty(descD)) descD = item.desc;
                        sb.AppendLine(item.id + "," + Esc(item.name) + "," + Esc(nameD)
                            + "," + item.type + "," + item.className + "," + item.level
                            + "," + item.worth + "," + Esc(descD)
                            + "," + (item.isModExtend ? "MOD" : "BASE"));
                        count++;
                        i++;
                    }
                    catch { break; }
                }
            }
            catch (Exception ex) { Log("[ITEM] " + ex.Message); }
            Save("dump_item.csv", sb, count);
            return count;
        }

        // ========== 3. ALL Drama ==========

        private int DumpDrama()
        {
            Log("[DRAMA] dumping ALL...");
            var sb = new StringBuilder();
            sb.AppendLine("id,key,value,key_display,value_display,isModExtend");
            int count = 0;
            try
            {
                var allConf = g.conf.dramaInfo1.allConfList;
                if (allConf == null) { Log("[DRAMA] null"); return 0; }
                int i = 0;
                while (true)
                {
                    try
                    {
                        var item = allConf[i];
                        if (item == null) break;
                        string keyD = Tr(item.key);
                        if (string.IsNullOrEmpty(keyD)) keyD = "";
                        string valD = Tr(item.value);
                        if (string.IsNullOrEmpty(valD)) valD = "";
                        sb.AppendLine(item.id + "," + Esc(item.key) + "," + Esc(item.value)
                            + "," + Esc(keyD) + "," + Esc(valD)
                            + "," + (item.isModExtend ? "MOD" : "BASE"));
                        count++;
                        i++;
                    }
                    catch { break; }
                }
            }
            catch (Exception ex) { Log("[DRAMA] " + ex.Message); }
            Save("dump_drama.csv", sb, count);
            return count;
        }

        // ========== 4. ALL Skills ==========

        private int DumpSkills()
        {
            Log("[SKILL] dumping ALL...");
            var sb = new StringBuilder();
            sb.AppendLine("id,className,className_display,mainSkill,weaponType,magicType,isModExtend");
            int count = 0;
            try
            {
                var allConf = g.conf.battleSkillBase.allConfList;
                if (allConf == null) { Log("[SKILL] null"); return 0; }
                int i = 0;
                while (true)
                {
                    try
                    {
                        var item = allConf[i];
                        if (item == null) break;
                        string cnD = Tr(item.className);
                        if (string.IsNullOrEmpty(cnD)) cnD = "";
                        sb.AppendLine(item.id + "," + Esc(item.className) + "," + Esc(cnD)
                            + "," + item.mainSkill + "," + item.weaponType + "," + item.magicType
                            + "," + (item.isModExtend ? "MOD" : "BASE"));
                        count++;
                        i++;
                    }
                    catch { break; }
                }
            }
            catch (Exception ex) { Log("[SKILL] " + ex.Message); }
            Save("dump_skill.csv", sb, count);
            return count;
        }

        // ========== 5. ALL Schools ==========

        private int DumpSchools()
        {
            Log("[SCHOOL] dumping ALL...");
            var sb = new StringBuilder();
            sb.AppendLine("id,group,name1,name1_display,name2,name2_display,languageType,isModExtend");
            int count = 0;
            try
            {
                var allConf = g.conf.schoolName.allConfList;
                if (allConf == null) { Log("[SCHOOL] null"); return 0; }
                int i = 0;
                while (true)
                {
                    try
                    {
                        var item = allConf[i];
                        if (item == null) break;
                        string n1D = Tr(item.name1);
                        if (string.IsNullOrEmpty(n1D)) n1D = "";
                        string n2D = Tr(item.name2);
                        if (string.IsNullOrEmpty(n2D)) n2D = "";
                        sb.AppendLine(item.id + "," + item.group
                            + "," + Esc(item.name1) + "," + Esc(n1D)
                            + "," + Esc(item.name2) + "," + Esc(n2D)
                            + "," + Esc(item.languageType)
                            + "," + (item.isModExtend ? "MOD" : "BASE"));
                        count++;
                        i++;
                    }
                    catch { break; }
                }
            }
            catch (Exception ex) { Log("[SCHOOL] " + ex.Message); }
            Save("dump_school.csv", sb, count);
            return count;
        }

        // ========== 6. ALL NPCs ==========

        private int DumpNpcs()
        {
            Log("[NPC] dumping ALL...");
            var sb = new StringBuilder();
            sb.AppendLine("id,npcId,clothingName,clothingName_display,flag,model,luck,isModExtend");
            int count = 0;
            try
            {
                var allConf = g.conf.specificNpcCreate.allConfList;
                if (allConf == null) { Log("[NPC] null"); return 0; }
                int i = 0;
                while (true)
                {
                    try
                    {
                        var item = allConf[i];
                        if (item == null) break;
                        string cnD = Tr(item.clothingName);
                        if (string.IsNullOrEmpty(cnD)) cnD = "";
                        sb.AppendLine(item.id + "," + item.npcId
                            + "," + Esc(item.clothingName) + "," + Esc(cnD)
                            + "," + Esc(item.flag)
                            + "," + item.model + "," + item.luck
                            + "," + (item.isModExtend ? "MOD" : "BASE"));
                        count++;
                        i++;
                    }
                    catch { break; }
                }
            }
            catch (Exception ex) { Log("[NPC] " + ex.Message); }
            Save("dump_npc.csv", sb, count);
            return count;
        }
    }
}
