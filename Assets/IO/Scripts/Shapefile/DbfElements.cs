// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;	//! TEMP

namespace Catfood.Shapefile
{
	public interface IElement
	{
		void Load(ref BinaryReader br);
		long GetLength();
	}

	public abstract class DBAttribute : IElement
	{
		public DBField Field { protected set; get; }
		public dynamic Value { protected set; get; }

		public abstract void Load(ref BinaryReader br);
		public long GetLength()
		{
			return Field.FieldLength;
		}
	}

	public class DBString : DBAttribute
	{
		public DBString(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			Value = new string(rawData);
		}
	}

	public class DBCurrency : DBAttribute
	{
		public DBCurrency(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			if (float.TryParse(new string(rawData), out float convertedVal))
				Value = convertedVal / 10000.0f;
        }
	}

	public class DBNumber : DBAttribute
	{
		public DBNumber(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			string valToConvert = new string(rawData);

			if (Field.DecimalCount == 0)
			{
				if (Field.FieldLength < 10)
				{
					if (int.TryParse(valToConvert, out int convertedVal))
						Value = convertedVal;
				}
				else
				{
					if (long.TryParse(valToConvert, out long convertedVal))
						Value = convertedVal;
				}
			}
			else
            {
				if (float.TryParse(valToConvert, out float convertedVal))
					Value = convertedVal;
			}
		}
	}

	public class DBFloat : DBAttribute
	{
		public DBFloat(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			if (float.TryParse(new string(rawData), out float convertedVal))
				Value = convertedVal;
		}
	}

	public class DBDate : DBAttribute
	{
		public DBDate(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			if (DateTime.TryParseExact(new string(rawData),
									   "yyyyMMdd",
									   System.Globalization.CultureInfo.InvariantCulture,
									   System.Globalization.DateTimeStyles.None,
									   out DateTime date))
			{
				Value = date;
			}
		}
	}

	public class DBDateTime : DBAttribute
	{
		public DBDateTime(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			if (DateTime.TryParseExact(new string(rawData),
									   "yyyyMMdd hh:mm:ss",
									   System.Globalization.CultureInfo.InvariantCulture,
									   System.Globalization.DateTimeStyles.None,
									   out DateTime dateTime))
			{
				Value = dateTime;
			}
		}
	}

	public class DBDouble : DBAttribute
	{
		public DBDouble(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			if (double.TryParse(new string(rawData), out double convertedVal))
				Value = convertedVal;
		}
	}

	public class DBInteger : DBAttribute
	{
		public DBInteger(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			if (int.TryParse(new string(rawData), out int convertedVal))
				Value = convertedVal;
		}
	}

	public class DBBoolean : DBAttribute
	{
		public DBBoolean(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			char[] rawData = br.ReadChars(Field.FieldLength);
			var value = rawData[0];
			if (value == 'Y' || value == 'y' || value == 'T' || value == 't')
				Value = true;
			else if (value == 'N' || value == 'n' || value == 'F' || value == 'f')
				Value = false;
			else
				Value = null;
		}
	}

	public class DBMemo : DBAttribute
	{
		public DBMemo(DBField field)
		{
			Field = field;
		}

		public override void Load(ref BinaryReader br)
		{
			Value = br.ReadChars(Field.FieldLength);
		}
	}

	public class DataFactory
	{
		public static readonly IDictionary<DBFFieldType, Func<DBField, DBAttribute>> Creators =
			new Dictionary<DBFFieldType, Func<DBField, DBAttribute>>()
			{
				{ DBFFieldType.Character, (fd) => new DBString(fd) },
				{ DBFFieldType.Currency, (fd) => new DBCurrency(fd) },
				{ DBFFieldType.Number, (fd) => new DBNumber(fd) },
				{ DBFFieldType.Float, (fd) => new DBFloat(fd) },
				{ DBFFieldType.Date, (fd) => new DBDate(fd) },
				{ DBFFieldType.DateTime, (fd) => new DBDateTime(fd) },
				{ DBFFieldType.Double, (fd) => new DBDouble(fd) },
				{ DBFFieldType.Integer, (fd) => new DBInteger(fd) },
				{ DBFFieldType.Boolean, (fd) => new DBBoolean(fd) },
				{ DBFFieldType.Memo, (fd) => new DBMemo(fd) },
				{ DBFFieldType.General, (fd) => new DBString(fd) },
			};

		public static DBAttribute CreateInstance(DBField fd)
		{
			if (!Creators.ContainsKey(fd.FieldType))
				throw new Exception($"{fd.FieldType} not registered in DataFactory");

			return Creators[fd.FieldType](fd);
		}
	}

	public class DBField : IElement
	{
		public string FieldName { private set; get; }
		public DBFFieldType FieldType { private set; get; }
		public int Reserved1 { private set; get; }
		public byte FieldLength { private set; get; }
		public byte DecimalCount { private set; get; }
		public byte[] WorkAreaID { private set; get; }
		public byte Example { private set; get; }
		public byte[] Reserved2 { private set; get; }
		public byte FieldFlag { private set; get; }

		public void Load(ref BinaryReader br)
		{
			FieldName = new string(br.ReadChars(11));
			FieldType = (DBFFieldType)br.ReadChar();
			Reserved1 = br.ReadInt32();
			FieldLength = br.ReadByte();
			DecimalCount = br.ReadByte();
			WorkAreaID = br.ReadBytes(2);
			Example = br.ReadByte();
			Reserved2 = br.ReadBytes(10);
			FieldFlag = br.ReadByte();
		}

		public long GetLength()
		{
			return 32;
		}
	}

	public class DBFeature : IElement
	{
		private List<DBField> Fields { set; get; }
		public char DeletionMarker { private set; get; }
		public List<DBAttribute> Attributes { private set; get; }

		public DBFeature(List<DBField> fields)
		{
			Fields = fields;
			Attributes = new List<DBAttribute>();
		}

		public void Load(ref BinaryReader br)
		{
			if (br.PeekChar() == -1)
				return;

			DeletionMarker = br.ReadChar();
			foreach (DBField fd in Fields)
			{
				DBAttribute attribute = DataFactory.CreateInstance(fd);
				attribute.Load(ref br);
				Attributes.Add(attribute);
			}
		}

		public long GetLength()
		{
			return sizeof(byte) + Fields.Sum(field => field.FieldLength);
		}
	}
}
