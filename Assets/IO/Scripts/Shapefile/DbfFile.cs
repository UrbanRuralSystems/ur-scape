// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections.Generic;
using System.IO;

namespace Catfood.Shapefile
{
    class DbfFile
    {
        private BinaryReader br;

        public DBFVersion Version { private set; get; }
        public byte UpdateYear { private set; get; }
        public byte UpdateMonth { private set; get; }
        public byte UpdateDay { private set; get; }
        public int UpdateDate { get { return UpdateYear * 10000 + UpdateMonth * 100 + UpdateDay; } }
        public int NumberOfRecords { private set; get; }
        public short HeaderLength { private set; get; }
        public short RecordLength { private set; get; }
        public byte[] Reserved { private set; get; }
        public List<DBField> Fields { private set; get; }
        public List<DBFeature> Features { private set; get; }
        public Dictionary<string, List<DBAttribute>> AttributeTable { private set; get; }

        public DbfFile(string path, out FileStream fs)
        {
            fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            br = new BinaryReader(fs);
        }

        public void Load()
        {
            Version = (DBFVersion)br.ReadByte();
            UpdateYear = br.ReadByte();
            UpdateMonth = br.ReadByte();
            UpdateDay = br.ReadByte();
            NumberOfRecords = br.ReadInt32();
            HeaderLength = br.ReadInt16();
            RecordLength = br.ReadInt16();
            Reserved = br.ReadBytes(20);

            // Read fields
            Fields = new List<DBField>();
            while (br.PeekChar() != 0x0d)
            {
                DBField field = new DBField();
                field.Load(ref br);
                Fields.Add(field);
            }

            br.BaseStream.Position = HeaderLength;

            // Read features and attributes
            int fieldCount = Fields.Count;
            Features = new List<DBFeature>();
            AttributeTable = new Dictionary<string, List<DBAttribute>>();
			for (int i = 0; i < NumberOfRecords; ++i)
            {
                DBFeature feature = new DBFeature(Fields);
                feature.Load(ref br);
                Features.Add(feature);

                for (int j = 0; j < fieldCount; ++j)
                {
                    string name = Fields[j].FieldName;
                    DBAttribute attribute = feature.Attributes[j];
                    if (!AttributeTable.ContainsKey(name))
                    {
                        AttributeTable.Add(name, new List<DBAttribute>());
                        AttributeTable[name].Add(attribute);
                    }
                    else
                        AttributeTable[name].Add(attribute);
                }
            }
        }
    }
}
