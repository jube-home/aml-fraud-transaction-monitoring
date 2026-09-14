// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Data;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static SqlDbType SqlSystemTypeToSqlDbType(this int @this)
        {
            switch (@this)
            {
                case 34:
                    return SqlDbType.Image;

                case 35:
                    return SqlDbType.Text;

                case 36:
                    return SqlDbType.UniqueIdentifier;

                case 40:
                    return SqlDbType.Date;

                case 41:
                    return SqlDbType.Time;

                case 42:
                    return SqlDbType.DateTime2;

                case 43:
                    return SqlDbType.DateTimeOffset;

                case 48:
                    return SqlDbType.TinyInt;

                case 52:
                    return SqlDbType.SmallInt;

                case 56:
                    return SqlDbType.Int;

                case 58:
                    return SqlDbType.SmallDateTime;

                case 59:
                    return SqlDbType.Real;

                case 60:
                    return SqlDbType.Money;

                case 61:
                    return SqlDbType.DateTime;

                case 62:
                    return SqlDbType.Float;

                case 98:
                    return SqlDbType.Variant;

                case 99:
                    return SqlDbType.NText;

                case 104:
                    return SqlDbType.Bit;

                case 106:
                    return SqlDbType.Decimal;

                case 108:
                    return SqlDbType.Decimal;

                case 122:
                    return SqlDbType.SmallMoney;

                case 127:
                    return SqlDbType.BigInt;

                case 165:
                    return SqlDbType.VarBinary;

                case 167:
                    return SqlDbType.VarChar;

                case 173:
                    return SqlDbType.Binary;

                case 175:
                    return SqlDbType.Char;

                case 189:
                    return SqlDbType.Timestamp;

                case 231:
                    return SqlDbType.NVarChar;

                case 239:
                    return SqlDbType.NChar;

                case 240:
                    return SqlDbType.Udt;

                case 241:
                    return SqlDbType.Xml;

                default:
                    throw new Exception(string.Format(
                        "Unsupported Type: {0}. Please let us know about this type and we will support it: sales@zzzprojects.com",
                        @this));
            }
        }
    }
}
