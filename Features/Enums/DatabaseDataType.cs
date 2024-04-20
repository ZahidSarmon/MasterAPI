using Master.Domain;

namespace Master.Features.Enums;

public class DatabaseDataType : Enumeration
{
    public static readonly DatabaseDataType Bigint = new(Random.Shared.Next(),"bigint");
    public static readonly DatabaseDataType Binary = new(Random.Shared.Next(),"binary");
    public static readonly DatabaseDataType Bit = new(Random.Shared.Next(),"bit");
    public static readonly DatabaseDataType Char = new(Random.Shared.Next(),"char");
    public static readonly DatabaseDataType Date = new(Random.Shared.Next(),"date");
    public static readonly DatabaseDataType DateTime = new(Random.Shared.Next(),"datetime");
    public static readonly DatabaseDataType DateTime2 = new(Random.Shared.Next(),"datetime2");
    public static readonly DatabaseDataType DateTimeOffset = new(Random.Shared.Next(),"datetimeoffset");
    public static readonly DatabaseDataType Decimal = new(Random.Shared.Next(),"decimal");
    public static readonly DatabaseDataType Float = new(Random.Shared.Next(),"float");
    public static readonly DatabaseDataType Geography = new(Random.Shared.Next(),"geography");
    public static readonly DatabaseDataType Geometry = new(Random.Shared.Next(),"geometry");
    public static readonly DatabaseDataType Guid = new(Random.Shared.Next(),"uniqueidentifier");
    public static readonly DatabaseDataType Hierarchyid = new(Random.Shared.Next(),"hierarchyid");
    public static readonly DatabaseDataType Image = new(Random.Shared.Next(),"image");
    public static readonly DatabaseDataType Int = new(Random.Shared.Next(),"int");
    public static readonly DatabaseDataType Money = new(Random.Shared.Next(),"money");
    public static readonly DatabaseDataType Nchar = new(Random.Shared.Next(),"nchar");
    public static readonly DatabaseDataType NText = new(Random.Shared.Next(),"ntext");
    public static readonly DatabaseDataType Numeric = new(Random.Shared.Next(),"numeric");
    public static readonly DatabaseDataType Nvarchar = new(Random.Shared.Next(),"nvarchar");
    public static readonly DatabaseDataType Real = new(Random.Shared.Next(),"real");
    public static readonly DatabaseDataType Smalldatetime = new(Random.Shared.Next(),"smalldatetime");
    public static readonly DatabaseDataType Smallint = new(Random.Shared.Next(),"smallint");
    public static readonly DatabaseDataType Smallmoney = new(Random.Shared.Next(),"smallmoney");
    public static readonly DatabaseDataType SqlVariant = new(Random.Shared.Next(),"sql_variant");
    public static readonly DatabaseDataType Text = new(Random.Shared.Next(),"text");
    public static readonly DatabaseDataType Time = new(Random.Shared.Next(),"time");
    public static readonly DatabaseDataType Timestamp = new(Random.Shared.Next(),"timestamp");
    public static readonly DatabaseDataType Tinyint = new(Random.Shared.Next(),"tinyint");
    public static readonly DatabaseDataType Varchar = new(Random.Shared.Next(),"varchar");
    public static readonly DatabaseDataType Varbinary = new(Random.Shared.Next(),"varbinary");
    public static readonly DatabaseDataType Xml = new(Random.Shared.Next(), "xml");
    public DatabaseDataType(int id,string name):base(id,name)
    {
        
    }
}
