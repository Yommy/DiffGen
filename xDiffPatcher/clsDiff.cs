﻿using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace xDiffPatcher
{
    public enum ExeType
    {
        None = 0,
        Rag,
        Sak,
        RagRE,
        RagRE9
    }

    public enum DiffType
    {
        None = 0,
        Diff,
        xDiff
    }

    public enum PatchType
    {
        None = 0,
        UI,
        Fix,
        Data,
        Auto,
        Color
    }

    public enum ChangeType
    {
        None = 0,
        Byte,
        Word,
        Dword,
        String,
        Color
    }

    public struct SectionInfo
    {
        public UInt32 peHeader;        
        public UInt32 imgSize; 
        public UInt16 sectCount;
        public UInt32 xDiffStart;
        public byte[] sectionData;
        public UInt32 realSize;
    }

    public class DiffFile
    {
        private string m_exeBuildDate = "";
        private string m_exeName = "";
        private int m_exeCRC = 0;
        private int m_exeType = 0;

        private string m_name = "";
        private string m_author = "";
        private string m_version = "";
        private string m_releaseDate = "";

        private FileInfo m_fileInfo;

        private DiffType m_type = 0;

        private SectionInfo m_xDiffSection;

        private Dictionary<int, DiffPatchBase> m_xpatches; //for xDiff
        private Dictionary<string, DiffPatch> m_patches; //for diff

        public string ExeBuildDate
        {
            get { return m_exeBuildDate; }
            set { m_exeBuildDate = value; }
        }
        public string ExeName
        {
            get { return m_exeName; }
            set { m_exeName = value; }
        }
        public int ExeCRC
        {
            get { return m_exeCRC; }
            set { m_exeCRC = value; }
        }
        public int ExeType
        {
            get { return m_exeType; }
            set { m_exeType = value; }
        }

        public FileInfo FileInfo
        {
            get { return m_fileInfo; }
            set { m_fileInfo = value; }
        }
        public DiffType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        public string Author
        {
            get { return m_author; }
            set { m_author = value; }
        }
        public string Version
        {
            get { return m_version; }
            set { m_version = value; }
        }
        public string ReleaseDate
        {
            get { return m_releaseDate; }
            set { m_releaseDate = value; }
        }
        public Dictionary<string, DiffPatch> Patches
        {
            get { return m_patches; }
            set { m_patches = value; }
        }
        public Dictionary<int, DiffPatchBase> xPatches
        {
            get { return m_xpatches; }
            set { m_xpatches = value; }
        }

        public DiffFile()
        {
            m_xpatches = new Dictionary<int, DiffPatchBase>();
            m_patches = new Dictionary<string, DiffPatch>();
        }

        public DiffFile(string fileName, DiffType type)
        {
            m_xpatches = new Dictionary<int, DiffPatchBase>();
            m_patches = new Dictionary<string, DiffPatch>();

            this.Load(fileName, type);
        }

        public int PatchCount()
        {
            int count = 0;

            foreach (KeyValuePair<int, DiffPatchBase> p in this.xPatches)
            {
                if (p.Value is DiffPatch)
                    count++;
                else if (p.Value is DiffPatchGroup)
                    count += ((DiffPatchGroup)p.Value).Patches.Count;
            }

            return count;
        }

        public int Load(string fileName, DiffType type)
        {
            if (!File.Exists(fileName))
                return 1;

            m_fileInfo = new FileInfo(fileName);

            if (m_patches != null)
                m_patches.Clear();
            if (m_xpatches != null)
                m_xpatches.Clear();

            m_type = type;

            if (type == DiffType.xDiff)
            {
                XmlDocument XDoc = null;
                /*try
                {*/
                    XDoc = new XmlDocument();
                    XDoc.Load(fileName);

                    this.ExeBuildDate = XDoc.SelectSingleNode("//diff/exe/builddate").InnerText;
                    this.ExeName = XDoc.SelectSingleNode("//diff/exe/filename").InnerText;
                    this.ExeCRC = int.Parse(XDoc.SelectSingleNode("//diff/exe/crc").InnerText);
                    string xtype = XDoc.SelectSingleNode("//diff/exe/type").InnerText;
                    this.ExeType = 0;

                    this.Name = XDoc.SelectSingleNode("//diff/info/name").InnerText;
                    this.Author = XDoc.SelectSingleNode("//diff/info/author").InnerText;
                    this.Version = XDoc.SelectSingleNode("//diff/info/version").InnerText;
                    this.ReleaseDate = XDoc.SelectSingleNode("//diff/info/releasedate").InnerText;

                    //extra addition for adding .xdiff section
                    m_xDiffSection.peHeader = Convert.ToUInt32(XDoc.SelectSingleNode("//diff/override/peheader").InnerText, 10);
                    m_xDiffSection.imgSize = Convert.ToUInt32(XDoc.SelectSingleNode("//diff/override/imagesize").InnerText, 10);
                    m_xDiffSection.sectCount = Convert.ToUInt16(XDoc.SelectSingleNode("//diff/override/sectioncount").InnerText, 10);
                    m_xDiffSection.xDiffStart = Convert.ToUInt32(XDoc.SelectSingleNode("//diff/override/xdiffstart").InnerText, 10);

                    UInt32 vSize = Convert.ToUInt32(XDoc.SelectSingleNode("//diff/override/vsize").InnerText, 10);
                    UInt32 vOffset = Convert.ToUInt32(XDoc.SelectSingleNode("//diff/override/voffset").InnerText, 10);
                    UInt32 rSize = Convert.ToUInt32(XDoc.SelectSingleNode("//diff/override/rsize").InnerText, 10);
                    UInt32 rOffset = Convert.ToUInt32(XDoc.SelectSingleNode("//diff/override/roffset").InnerText, 10);
                    m_xDiffSection.realSize = rSize + rOffset;

                    //now create section data 
                    m_xDiffSection.sectionData = new byte[40];
                    UInt32 i = 0;

                    // first section name
                    foreach(byte b in Encoding.ASCII.GetBytes(".xdiff\x00\x00"))
                    {
                        m_xDiffSection.sectionData[i] = b;
                        i++;
                    }
                    
                    //next the offset and sizes
                    WriteDword(ref m_xDiffSection.sectionData, vSize, 8);
                    WriteDword(ref m_xDiffSection.sectionData, vOffset, 12);
                    WriteDword(ref m_xDiffSection.sectionData, rSize, 16);
                    WriteDword(ref m_xDiffSection.sectionData, rOffset, 20);
                    
                    //next the relocation and line numbers info - fill with 0
                    WriteDword(ref m_xDiffSection.sectionData, 0, 24);
                    WriteDword(ref m_xDiffSection.sectionData, 0, 28);
                    WriteDword(ref m_xDiffSection.sectionData, 0, 32);
                    
                    //Lastly Characteristics
                    WriteDword(ref m_xDiffSection.sectionData, 0xE0000060, 36);
    
                    XmlNode patches = XDoc.SelectSingleNode("//diff/patches");
                    foreach (XmlNode patch in patches.ChildNodes)
                    {
                        if (patch.Name == "patchgroup")
                        {
                            //XmlNode tmpNode = null;
                            DiffPatchGroup g = new DiffPatchGroup();

                            g.ID = int.Parse(patch.Attributes["id"].InnerText);
                            g.Name = patch.Attributes["name"].InnerText;

                            foreach (XmlNode node in patch.ChildNodes)
                            {
                                if (node.Name == "patch")
                                {
                                    DiffPatch p = new DiffPatch();
                                    p.LoadFromXML(node);
                                    this.xPatches.Add(p.ID, p);
                                    g.Patches.Add(p);
                                }
                            }

                            this.xPatches.Add(g.ID, g);
                        }
                        else if (patch.Name == "patch")
                        {
                            DiffPatch p = new DiffPatch();
                            p.LoadFromXML(patch);

                            this.xPatches.Add(p.ID, p);
                        }
                    }
                /*}
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to parse xDiff file: \n"+ex.ToString());
                    return 2;
                }*/
            } else if (type == DiffType.Diff)
            {
                bool hex = false;

                using (StreamReader r = new StreamReader(fileName))
                {
                    string line;
                    while (!r.EndOfStream && (line = r.ReadLine()) != null )
                    {
                        line = line.Trim();
                        if (line.Length < 5) continue;

                        if (line.StartsWith("OCRC:")) 
                        {
                            this.ExeCRC = int.Parse(line.Substring(5));
                        }
                        else if (line.StartsWith("BLURB:"))
                        {
                            this.Name = line.Substring(6);
                        }
                        else if (line.StartsWith("READHEX"))
                        {
                            hex = true;
                        }
                        else if (line.StartsWith("byte_"))
                        {
                            string pType, pName;
                            string pGroup;
                            DiffChange change = new DiffChange();
                            DiffPatch patch = new DiffPatch();
                            string[] split = line.Split(':');

                            Regex regex = new Regex("(.+)_\\[(.+)\\]_(.+)");
                            Match match = regex.Match(split[0]);

                            pName = "";
                            pType = "";
                            if (match.Success)
                            {
                                change.Type = ChangeType.Byte;
                                pType = match.Groups[1].Captures[0].Value;
                                pName = split[0].Substring(5); //match.Captures[2].Value.Replace('_', ' ');
                            } else 
                            {
                                regex = new Regex("(.+)_\\[(.+)\\]\\((.+)\\)_(.+)");
                                match = regex.Match(split[0]);

                                if (match.Success)
                                {
                                    change.Type = ChangeType.Byte;
                                    pType = match.Groups[1].Captures[0].Value;
                                    pGroup = match.Groups[3].Captures[0].Value;
                                    pName = split[0].Substring(5); //match.Groups[3].Captures[0].Value.Replace('_', ' ');
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            change.Offset = uint.Parse(split[1], System.Globalization.NumberStyles.HexNumber);
                            change.Old = (byte) ( (!hex) ? byte.Parse(split[2]) : byte.Parse(split[2], System.Globalization.NumberStyles.HexNumber) );
                            change.New_ = (byte) ( (!hex) ? byte.Parse(split[3]) : byte.Parse(split[3], System.Globalization.NumberStyles.HexNumber) );

                            if (m_patches.ContainsKey(pName))
                                m_patches[pName].Changes.Add(change);
                            else
                            {
                                patch.Changes.Add(change);
                                patch.Name = pName;
                                patch.Type = pType;
                                m_patches.Add(pName, patch);
                            }
                        }
                    }
                }
            }
            else
            {
                return 2;
            }

            return 0;
        }

        private int ApplyPatch(DiffPatch patch, ref byte[] buf)
        {
            int changed = 0;

            if (!patch.Apply)
                return -1;

            foreach (DiffInput i in patch.Inputs)
                if (!DiffInput.CheckInput(i.Value, i))
                    return -2;

            foreach (DiffChange c in patch.Changes)
            {
                switch (c.Type)
                {
                    case ChangeType.Byte:
                        {
                            byte old = buf[c.Offset];

                            if (old == (byte)c.Old)
                            {
                                buf[c.Offset] = (byte)c.GetNewValue(patch);
                                changed++;
                            }
                            else
                                MessageBox.Show(String.Format("Data mismatch at 0x{0:X} (0x{1:X} != 0x{2:X})!", c.Offset, old, (byte)c.Old));

                            break;
                        }

                    case ChangeType.Word:
                        {
                            UInt16 old = BitConverter.ToUInt16(buf, (int)c.Offset);

                            if (old == (UInt16)c.Old)
                            {
                                UInt16 val = (UInt16)c.GetNewValue(patch);
                                buf[c.Offset] = (byte)val;
                                buf[c.Offset + 1] = (byte)(val >> 8);
                                changed += 2;
                            }
                            else
                                MessageBox.Show(String.Format("Data mismatch at 0x{0:X} (0x{1:X} != 0x{2:X})!", c.Offset, old, (ushort)c.Old));

                            break;
                        }

                    case ChangeType.Dword:
                        {
                            UInt32 old = BitConverter.ToUInt32(buf, (int)c.Offset);

                            if (old == (UInt32)c.Old)
                            {
                                WriteDword(ref buf, (UInt32)c.GetNewValue(patch), c.Offset);
                                changed += 4;
                            }
                            else
                                MessageBox.Show(String.Format("Data mismatch at 0x{0:X} (0x{1:X} != 0x{2:X})!", c.Offset, old, (uint)c.Old));
                            break;
                        }

                    case ChangeType.String:// used only for displayable string
                        {
                            //currently not checking for old string - if client crashes your screwed
                            byte[] val = Encoding.ASCII.GetBytes((String)c.GetNewValue(patch) + "\x00");
                            
                            int i = 0;
                            foreach (byte b in val)
                                buf[c.Offset + i++] = b;
                            
                            changed += i;
                            break;
                        }
                }
            }

            //MessageBox.Show("Applied patch '" + patch.Name + "' (" + changed + " bytes)");

            return changed;
        }

        public int Patch(string inputFile, string targetFile)
        {
            if (!File.Exists(inputFile))
                return 1;

            if (this.Type == DiffType.None)
                return 0;
            
            int start = Environment.TickCount;
            Int64 l = new FileInfo(inputFile).Length;
            byte[] buf = new byte[m_xDiffSection.realSize];

            Array.Copy(File.ReadAllBytes(inputFile), 0, buf, 0, l);

            // Fill remaining space with null (if its an already patched file the loop would not execute)
            for (; l < buf.LongLength; l++)
                buf[l] = 0x00;

            // Modify SizeOfImage in Optional Header
            UInt32 offset = m_xDiffSection.peHeader + 0x50;
            UInt32 val = m_xDiffSection.imgSize;
            WriteDword(ref buf, val, offset);
            
            // Modify Section Count
            offset = m_xDiffSection.peHeader + 6;
            val = m_xDiffSection.sectCount;
            buf[offset] = (byte)val;
            buf[offset + 1] = (byte)(val >> 8);
            
            //Insert Section Data
            offset = m_xDiffSection.xDiffStart;
            foreach (byte b in m_xDiffSection.sectionData)
            {
                buf[offset] = b;
                offset++;
            }

            int changed = 0;
            if(this.Type == DiffType.xDiff)
            {   
                foreach (DiffPatchBase p in xPatches.Values)
                {
                    int ret;
                    if (p is DiffPatch) //patches inside group are already present in xPatches
                    {   
                        ret = ApplyPatch((DiffPatch)p, ref buf);
                        if (ret < 0 && ret == -2)
                            MessageBox.Show("Invalid input, could not apply patch '" + p.Name + "'!");
                        if (ret > 0)
                            changed += ret;
                    }
                    /*else if (p is DiffPatchGroup)
                        foreach (DiffPatch p2 in ((DiffPatchGroup)p).Patches)
                        {
                            ret = ApplyPatch(p2, ref buf);
                            if (ret < 0 && ret == -2)
                                MessageBox.Show("Invalid input, could not apply patch '" + p.Name + "'!");
                            if (ret > 0)
                                changed += ret;
                        }*/
                }
            }
            else if (this.Type == DiffType.Diff)
            {  
                foreach (DiffPatch p in Patches.Values)
                {
                    if (!p.Apply)
                        continue;

                    foreach (DiffChange c in p.Changes)
                    {
                        switch (c.Type)
                        {
                            case ChangeType.Byte:
                                {   
                                    byte old = buf[c.Offset];
                                    if (old == (byte)c.Old)
                                    {
                                        buf[c.Offset] = (byte)c.New_;
                                        changed++;
                                    }
                                    //else
                                    //{
                                    
                                    //}
                                    break;
                                }
                        }
                    }
                }
            }
            if (File.Exists(targetFile)) File.Delete(targetFile);
            File.WriteAllBytes(targetFile, buf);
            int stop = Environment.TickCount;            
            MessageBox.Show("Finished patching " + changed + "bytes in " + (stop - start) + "ms!");
            
            return 0;
        }

        private void WriteDword( ref byte[] buffer, UInt32 value, UInt32 offset = 0)
        {            
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);            
        }
    }

    public class DiffChange
    {
        ChangeType m_type;
        uint m_offset;
        object m_old;
        object m_new;

        public ChangeType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        public uint Offset
        {
            get { return m_offset; }
            set { m_offset = value; }
        }
        public object Old
        {
            get { return m_old; }
            set { m_old = value; }
        }
        public object New_
        {
            get { 
                return m_new; 
            }
            set { m_new = value; }
        }

        public object GetNewValue(DiffPatch p)
        {
            object val = null;
            if (this.New_ is string && ((string)this.New_).StartsWith("$"))
            {
                string str = ((string)this.New_);
                str = str.TrimStart('$');

                foreach (DiffInput i in p.Inputs)
                {
                    if (i.Name == str)
                    {
                        if (Type == ChangeType.Byte)
                            val = byte.Parse(i.Value);
                        else if (Type == ChangeType.Dword)
                        {
                            if (i.Type == ChangeType.Color && i.Value.Length >= 6)
                                val = UInt32.Parse(String.Format("00{2:X}{1:X}{0:X}", i.Value.Substring(0,2), i.Value.Substring(2,2), i.Value.Substring(4,2)), System.Globalization.NumberStyles.HexNumber);
                            else
                                val = UInt32.Parse(i.Value);
                        }
                        else if (Type == ChangeType.Word)
                            val = UInt16.Parse(i.Value);
                        else if (Type == ChangeType.String)
                            val = i.Value;
                        else
                            return null;

                        if (i.Operator != null && i.Operator.Length >= 2)
                        {
                            i.Operator = i.Operator.Trim();
                            char op = i.Operator[0];

                            if (op == '+')
                            {
                                string val2 = i.Operator.Substring(1).Trim();

                                if (Type == ChangeType.Byte)
                                    return (byte) (((byte)val) + byte.Parse(val2));
                                if (Type == ChangeType.Word)
                                    return (ushort) (((ushort)val) + ushort.Parse(val2));
                                if (Type == ChangeType.Dword)
                                    return (uint) (((uint)val) + uint.Parse(val2));
                            }
                            else if (op == '-')
                            {
                                string val2 = i.Operator.Substring(1).Trim();

                                if (Type == ChangeType.Byte)
                                    return (byte) (((byte)val) - byte.Parse(val2));
                                if (Type == ChangeType.Word)
                                    return (ushort) (((ushort)val) - ushort.Parse(val2));
                                if (Type == ChangeType.Dword)
                                    return (uint) (((uint)val) - uint.Parse(val2));
                            }

                        }
                        else
                        {
                            return val;
                        }
                    }
                }

                throw new Exception("Could not resolve input value '" + this.New_ + "'!");
            }

            return this.New_;
        }
    }

    public class DiffPatchBase
    {
        string m_name;
        int m_id = 0;

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public int ID
        {
            get { return m_id; }
            set { m_id = value; }
        }
    }

    public class DiffPatchGroup : DiffPatchBase
    {
        

        List<DiffPatch> m_patches = new List<DiffPatch>();

        public List<DiffPatch> Patches
        {
            get { return m_patches; }
            set { m_patches = value; }
        }

    }

    public class DiffInput
    {
        ChangeType m_type;
        String m_name;
        string m_operator;
        int m_min = int.MaxValue;
        int m_max = int.MaxValue;
        string m_value; // for diffpatcher only

        public string Value
        {
            get { return m_value; }
            set { m_value = value; }
        }

        public ChangeType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        public String Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        public String Operator
        {
            get { return m_operator; }
            set { m_operator = value; }
        }
        public int Min
        {
            get { return m_min; }
            set { m_min = value; }
        }
        public int Max
        {
            get { return m_max; }
            set { m_max = value; }
        }

        public void LoadFromXML(XmlNode node)
        {
            this.Min = int.MaxValue;
            this.Max = int.MaxValue;
            this.Name = null;
            this.Type = ChangeType.None;

            string type = null;

            System.Collections.IEnumerator e = node.Attributes.GetEnumerator();
            e.Reset();
            while (e.MoveNext())
            {
                XmlAttribute a = (XmlAttribute)e.Current;
                if (a.Name == "name")
                    this.Name = a.Value.TrimStart('$');
                else if (a.Name == "op")
                    this.Operator = a.Value;
                else if (a.Name == "max")
                    this.Max = int.Parse(a.Value);
                else if (a.Name == "min")
                    this.Min = int.Parse(a.Value);
                else if (a.Name == "type")
                    type = a.Value;
            }

            if (type == "byte")
                this.Type = ChangeType.Byte;
            else if (type == "word")
                this.Type = ChangeType.Word;
            else if (type == "dword")
                this.Type = ChangeType.Dword;
            else if (type == "string")
                this.Type = ChangeType.String;
            else if (type == "color")
                this.Type = ChangeType.Color;
            else
                this.Type = ChangeType.None;
        }

        public static bool CheckInput(string value, DiffInput input)
        {
            bool ok = true;

            if (input.Type == ChangeType.String)
            {
                if (input.Min != int.MaxValue && value.Length < input.Min)
                    ok = false;

                if (input.Max != int.MaxValue && value.Length > input.Max)
                    ok = false;
            }
            else if (input.Type == ChangeType.Byte)
            {
                byte val = 0;
                if (!byte.TryParse(value, out val))
                    ok = false;
                else if (input.Min != int.MaxValue && val < input.Min)
                    ok = false;
                else if (input.Max != int.MaxValue && val > input.Max)
                    ok = false;
            }
            else if (input.Type == ChangeType.Word)
            {
                UInt16 val = 0;
                if (!UInt16.TryParse(value, out val))
                    ok = false;
                else if (input.Min != int.MaxValue && val < input.Min)
                    ok = false;
                else if (input.Max != int.MaxValue && val > input.Max)
                    ok = false;
            }
            else if (input.Type == ChangeType.Byte)
            {
                UInt32 val = 0;
                if (!UInt32.TryParse(value, out val))
                    ok = false;
                else if (input.Min != int.MaxValue && val < input.Min)
                    ok = false;
                else if (input.Max != int.MaxValue && val > input.Max)
                    ok = false;
            }

            return ok;
        }
    }

    public class DiffPatch : DiffPatchBase
    {
        string m_type = "";
        bool m_recommended = false;
        string m_desc = "";
        int m_groupID = 0;
        bool m_apply = false; // for diffpatcher

        List<DiffInput> m_inputs = new List<DiffInput>();
        List<DiffChange> m_changes = new List<DiffChange>();

        public int GroupID
        {
            get { return m_groupID; }
            set { m_groupID = value; }
        }
        public string Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        public bool Recommended
        {
            get { return m_recommended; }
            set { m_recommended = value; }
        }
        public string Desc
        {
            get { return m_desc; }
            set { m_desc = value; }
        }
        public bool Apply
        {
            get { return m_apply; }
            set { m_apply = value; }
        }
        public List<DiffChange> Changes
        {
            get { return m_changes; }
            set { m_changes = value; }
        }
        public List<DiffInput> Inputs
        {
            get { return m_inputs; }
            set { m_inputs = value; }
        }

        public void LoadFromXML(XmlNode patch)
        {
            XmlNode tmpNode = null;
            //DiffPatch p = new DiffPatch();
            this.ID = int.Parse(patch.Attributes["id"].InnerText);
            this.Name = patch.Attributes["name"].InnerText;
            this.Type = patch.Attributes["type"].InnerText;

            tmpNode = patch.ParentNode;
            if (tmpNode != null && tmpNode.Name == "patchgroup")
                this.GroupID = int.Parse(tmpNode.Attributes["id"].InnerText);

            if (patch.Attributes["recommended"] != null)
                this.Recommended = true;

            tmpNode = patch.SelectSingleNode("desc");
            if (tmpNode != null)
                this.Desc = tmpNode.InnerText;

            foreach (XmlNode i in patch.SelectNodes("input"))
            {
                var input = new DiffInput();
                input.LoadFromXML(i);
                this.Inputs.Add(input); 
            }

            /*                        var input = new DiffInput();
                        input.LoadFromXML(change);
                        this.Inputs.Add(input);*/

            tmpNode = patch.SelectSingleNode("changes");
            if (tmpNode != null)
            {
                foreach (XmlNode change in tmpNode.ChildNodes)
                {
                    DiffChange c = new DiffChange();

                    if (change.Name == "byte")
                        c.Type = ChangeType.Byte;
                    else if (change.Name == "word")
                        c.Type = ChangeType.Word;
                    else if (change.Name == "dword")
                        c.Type = ChangeType.Dword;
                    else if (change.Name == "string")
                        c.Type = ChangeType.String;
                    else
                        c.Type = ChangeType.None;

                    if (change.Attributes["new"].InnerText.StartsWith("$"))
                    {
                        c.New_ = change.Attributes["new"].InnerText;
                    }
                    c.Offset = uint.Parse(change.Attributes["offset"].InnerText, System.Globalization.NumberStyles.HexNumber);
                    if (c.Type == ChangeType.String)
                    {
                        if (c.New_ == null)
                            c.New_ = change.Attributes["new"].InnerText;
                        c.Old = change.Attributes["old"].InnerText;
                    }
                    else if (c.Type == ChangeType.Byte)
                    {
                        if (c.New_ == null)
                            c.New_ = byte.Parse(change.Attributes["new"].InnerText, System.Globalization.NumberStyles.HexNumber);
                        c.Old = byte.Parse(change.Attributes["old"].InnerText, System.Globalization.NumberStyles.HexNumber);
                    }
                    else if (c.Type == ChangeType.Word)
                    {
                        if (c.New_ == null)
                            c.New_ = ushort.Parse(change.Attributes["new"].InnerText, System.Globalization.NumberStyles.HexNumber);
                        c.Old = ushort.Parse(change.Attributes["old"].InnerText, System.Globalization.NumberStyles.HexNumber);
                    } 
                    else if (c.Type == ChangeType.Dword)
                    {
                        if (c.New_ == null)
                            c.New_ = uint.Parse(change.Attributes["new"].InnerText, System.Globalization.NumberStyles.HexNumber);
                        c.Old = uint.Parse(change.Attributes["old"].InnerText, System.Globalization.NumberStyles.HexNumber);
                    } 

                    this.Changes.Add(c);
                }
            }
        }
    }
}
