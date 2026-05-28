using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace MOD_b4qnSo
{
    public class ModMain
    {
        private const string VERSION = "datalens-v1.1.0";
        private const int MAX_ROWS_PER_TABLE = 200000;
        private const int MAX_FAILS_AFTER_DATA = 25;

        private static void Log(string msg)
        {
            MelonLogger.Msg("[DataLens " + VERSION + "] " + msg);
        }

        public void Init()
        {
            Log("=== Init start ===");
            g.timer.Frame(new Action(() => { RunAllDumps(); }), 300, false);
            Log("=== Init done (" + VERSION + ") ===");
        }

        public void Destroy() { Log("Destroy (" + VERSION + ")"); }

        private static string Esc(object val)
        {
            string s = ObjToString(val);
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static string ObjToString(object val)
        {
            if (val == null) return "";
            try { return val.ToString(); } catch { return ""; }
        }

        private static string Tr(object key)
        {
            string s = ObjToString(key);
            if (string.IsNullOrEmpty(s) || s == "0") return "";
            try
            {
                string t = ConfLocalText.GetText(s);
                if (!string.IsNullOrEmpty(t) && t != s) return t;
            }
            catch { }
            return "";
        }

        private static string DisplayOrRaw(object key)
        {
            string t = Tr(key);
            if (!string.IsNullOrEmpty(t)) return t;
            return ObjToString(key);
        }

        private static void Save(string file, StringBuilder sb, int count)
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string path = string.IsNullOrEmpty(dir) ? file : Path.Combine(dir, file);
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
                Log("[" + file + "] " + count + " rows -> " + path);
            }
            catch (Exception ex) { Log("[" + file + "] write error: " + ex.Message); }
        }

        private static object GetMemberValue(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;
            Type t = obj.GetType();
            try
            {
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return f.GetValue(obj);
            }
            catch { }
            try
            {
                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(obj, null);
            }
            catch { }
            return null;
        }

        private static object GetIndexedValue(object list, int index)
        {
            if (list == null) return null;
            Type t = list.GetType();
            try
            {
                PropertyInfo itemProp = t.GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (itemProp != null) return itemProp.GetValue(list, new object[] { index });
            }
            catch { }
            try
            {
                MethodInfo getItem = t.GetMethod("get_Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (getItem != null) return getItem.Invoke(list, new object[] { index });
            }
            catch { }
            try
            {
                IList ilist = list as IList;
                if (ilist != null) return ilist[index];
            }
            catch { }
            try
            {
                dynamic d = list;
                return d[index];
            }
            catch { }
            return null;
        }

        private static int GetListCount(object list)
        {
            if (list == null) return -1;
            object count = GetMemberValue(list, "Count");
            if (count == null) count = GetMemberValue(list, "Length");
            try { return Convert.ToInt32(count); } catch { return -1; }
        }

        private static bool ContainsAny(string haystack, string[] needles)
        {
            if (string.IsNullOrEmpty(haystack)) return false;
            string h = haystack.ToLowerInvariant();
            for (int i = 0; i < needles.Length; i++)
            {
                string n = needles[i];
                if (!string.IsNullOrEmpty(n) && h.Contains(n.ToLowerInvariant())) return true;
            }
            return false;
        }

        private static bool LooksLikePrimitive(object value)
        {
            if (value == null) return true;
            Type t = value.GetType();
            return t.IsPrimitive || t.IsEnum || value is string || value is decimal;
        }

        private static void AppendPrimitiveFields(StringBuilder sb, string tableName, int rowIndex, object item, ref int count)
        {
            if (item == null) return;
            Type t = item.GetType();
            object itemId = GetMemberValue(item, "id");
            string mod = ObjToString(GetMemberValue(item, "isModExtend"));

            HashSet<string> done = new HashSet<string>();

            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (f.Name.Contains("k__BackingField")) continue;
                if (done.Contains(f.Name)) continue;
                try
                {
                    object val = f.GetValue(item);
                    if (!LooksLikePrimitive(val)) continue;
                    AppendLongRow(sb, tableName, rowIndex, itemId, f.Name, val, mod);
                    done.Add(f.Name);
                    count++;
                }
                catch { }
            }

            PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo p = props[i];
                if (p.GetIndexParameters().Length > 0) continue;
                if (done.Contains(p.Name)) continue;
                try
                {
                    object val = p.GetValue(item, null);
                    if (!LooksLikePrimitive(val)) continue;
                    AppendLongRow(sb, tableName, rowIndex, itemId, p.Name, val, mod);
                    done.Add(p.Name);
                    count++;
                }
                catch { }
            }
        }

        private static void AppendLongRow(StringBuilder sb, string tableName, int rowIndex, object itemId, string fieldName, object value, string mod)
        {
            string display = Tr(value);
            sb.AppendLine(Esc(tableName) + "," + rowIndex + "," + Esc(itemId) + "," + Esc(fieldName)
                + "," + Esc(value) + "," + Esc(display) + "," + Esc(mod));
        }

        private static int DumpConfigTables(string label, string file, string[] keywords)
        {
            Log("[" + label + "] dumping matching config tables...");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("table,row,id,field,value,value_display,isModExtend");
            int rows = 0;
            int tables = 0;

            try
            {
                object conf = g.conf;
                Type ct = conf.GetType();
                HashSet<string> visited = new HashSet<string>();

                MemberInfo[] members = ct.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int m = 0; m < members.Length; m++)
                {
                    MemberInfo mi = members[m];
                    if (!(mi is FieldInfo) && !(mi is PropertyInfo)) continue;
                    if (!ContainsAny(mi.Name, keywords)) continue;
                    if (visited.Contains(mi.Name)) continue;
                    visited.Add(mi.Name);

                    object confTable = null;
                    try
                    {
                        if (mi is FieldInfo) confTable = ((FieldInfo)mi).GetValue(conf);
                        else
                        {
                            PropertyInfo p = (PropertyInfo)mi;
                            if (p.GetIndexParameters().Length > 0) continue;
                            confTable = p.GetValue(conf, null);
                        }
                    }
                    catch { continue; }
                    if (confTable == null) continue;

                    object allConf = GetMemberValue(confTable, "allConfList");
                    if (allConf == null) allConf = confTable;
                    int added = DumpOneTable(sb, mi.Name, allConf, ref rows);
                    if (added > 0)
                    {
                        tables++;
                        Log("[" + label + "] " + mi.Name + " rows=" + added);
                    }
                }
            }
            catch (Exception ex) { Log("[" + label + "] " + ex.Message); }

            Save(file, sb, rows);
            Log("[" + label + "] tables=" + tables + " longRows=" + rows);
            return rows;
        }

        private static int DumpOneTable(StringBuilder sb, string tableName, object allConf, ref int longRows)
        {
            int before = longRows;
            int count = GetListCount(allConf);
            if (count >= 0)
            {
                for (int i = 0; i < count && i < MAX_ROWS_PER_TABLE; i++)
                {
                    object item = GetIndexedValue(allConf, i);
                    if (item == null) continue;
                    AppendPrimitiveFields(sb, tableName, i, item, ref longRows);
                }
            }
            else
            {
                int fails = 0;
                for (int i = 0; i < MAX_ROWS_PER_TABLE && fails < MAX_FAILS_AFTER_DATA; i++)
                {
                    object item = GetIndexedValue(allConf, i);
                    if (item == null)
                    {
                        fails++;
                        continue;
                    }
                    fails = 0;
                    AppendPrimitiveFields(sb, tableName, i, item, ref longRows);
                }
            }
            return longRows - before;
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
            int h = DumpMethods();

            Log("=== DATA DUMP COMPLETE ===");
            Log("Summary rows: luck=" + a + " item=" + b + " drama=" + c
                + " skill=" + d + " school=" + e + " npc=" + f + " method=" + h
                + " total=" + (a + b + c + d + e + f + h));
        }

        // ========== 1. Luck ==========

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
                        string d = DisplayOrRaw(item.name);
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

        // ========== 2. Items ==========

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
                        string nameD = DisplayOrRaw(item.name);
                        string descD = DisplayOrRaw(item.desc);
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

        // ========== Generic full dumps for tables that differ between game versions ==========

        private int DumpDrama()
        {
            return DumpConfigTables("DRAMA", "dump_drama.csv", new string[] { "drama", "dialog", "dialogue", "story", "plot" });
        }

        private int DumpSkills()
        {
            return DumpConfigTables("SKILL", "dump_skill.csv", new string[] { "skill", "ability", "martial", "magic", "gong", "schoolfight" });
        }

        private int DumpSchools()
        {
            return DumpConfigTables("SCHOOL", "dump_school.csv", new string[] { "school", "sect", "branch", "post" });
        }

        private int DumpNpcs()
        {
            return DumpConfigTables("NPC", "dump_npc.csv", new string[] { "npc", "unit", "role", "specific" });
        }

        private int DumpMethods()
        {
            return DumpConfigTables("METHOD", "dump_method.csv", new string[] { "method", "manual", "book", "formula", "ability", "martial", "skill", "magic", "gong" });
        }
    }
}
