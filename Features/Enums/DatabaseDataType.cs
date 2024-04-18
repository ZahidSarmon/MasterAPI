using Master.Domain;

namespace Master.Features.Enums;

public class DatabaseDataType : Enumeration
{
    public static readonly DatabaseDataType Int = new(1,"int");
    public static readonly DatabaseDataType Bigint = new(2,"bigint");
    public static readonly DatabaseDataType Varchar = new(3, "varchar");
    public DatabaseDataType(int id,string name):base(id,name)
    {
        
    }
}
