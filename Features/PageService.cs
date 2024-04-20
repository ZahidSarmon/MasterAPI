using Master.Data;
using Master.Entities;
using Master.Features.DTOs;
using Master.Features.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace Master.Features;

public class PageService : IPageService
{
    private readonly AppDbContext _context;
    public PageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PageInputValueDTO> GetPageInputValuesAsync(Guid pageId)
    {
        var page = await GetPageAsync(pageId);

        var pageInputsDeserialize = await GetPageInputsDeserializeAsync(pageId);

        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            var columns = $"Id,{string.Join(",", pageInputsDeserialize!.Select(i => i.DatabaseName))}";

            string query = $"select {columns} from {page!.Name}";

            using (var sqlCommand = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        DataTable dataTable = new DataTable();
                        dataTable.Load(reader);

                        dynamic[] dataArray = new dynamic[dataTable.Rows.Count];
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            DataRow row = dataTable.Rows[i];
                            dynamic rowData = new System.Dynamic.ExpandoObject();
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                ((IDictionary<String, Object>)rowData)[column.ColumnName] = row[column];
                            }
                            dataArray[i] = rowData;
                        }

                        string jsonArray = JsonConvert.SerializeObject(dataArray);

                        return new PageInputValueDTO(columns, jsonArray);
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }

    public async Task<bool> DeletePageInputValuesAsync(DeletePageCommand command)
    {
        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            try
            {
                string buildQuery = @$"DELETE {command.TableName} WHERE Id = @Id";

                using (var sqlCommand = new SqlCommand(buildQuery, connection))
                {
                    connection.Open();
                    sqlCommand.Parameters.AddWithValue("@Id", command.Id);
                    return await sqlCommand.ExecuteNonQueryAsync() > 0;
                }
            }
            finally
            {
                connection.Close();
            }
        }
    }

    public async Task<bool> PostPageInputValuesAsync(PostPageInputCommand command)
    {
        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            try
            {
                string buildQuery = @$"INSERT INTO {command.TableName} ({string.Join(",", command.Columns)},CreatedOn,CreatedBy) 
                                    VALUES({string.Join(",", command.Columns.Select(item => $"@{item}"))},@CreatedOn,@CreatedBy)";

                using (var sqlCommand = new SqlCommand(buildQuery, connection))
                {
                    connection.Open();
                    foreach (var option in command.ColumnWithValues)
                    {
                        sqlCommand.Parameters.AddWithValue($"@{option.Key}",option.Value);
                    }
                    sqlCommand.Parameters.AddWithValue("@CreatedOn",DateTime.UtcNow);
                    sqlCommand.Parameters.AddWithValue("@CreatedBy", command.CreatedBy);
                    return await sqlCommand.ExecuteNonQueryAsync()>0;
                }
            }
            finally
            {
                connection.Close();
            }
        }
    }

    public async Task<bool> PutPageInputValuesAsync(PutPageInputCommand command)
    {
        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            try
            {
                string buildQuery = @$"UPDATE {command.TableName}
                                    SET {string.Join(",",command.Columns.Select(item=>$"{item}=@{item}"))}
                                    WHERE Id = @Id";

                using (var sqlCommand = new SqlCommand(buildQuery, connection))
                {
                    connection.Open();

                    foreach (var option in command.ColumnWithValues)
                    {
                        sqlCommand.Parameters.AddWithValue($"@{option.Key}", option.Value);
                    }

                    sqlCommand.Parameters.AddWithValue("@ModifiedOn", DateTime.UtcNow);
                    sqlCommand.Parameters.AddWithValue("@ModifiedBy", command.ModifiedBy);

                    return await sqlCommand.ExecuteNonQueryAsync() > 0;
                }
            }
            finally
            {
                connection.Close();
            }
        }
    }

    public async Task<bool> PostAsync(PageCommand command)
    {
        var pageInputs = JsonConvert.DeserializeObject<IEnumerable<PageInput>>(command.Definition);

        var page = new Page
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            DbName = command.DatabaseName,
            Definition = command.Definition
        };

        await _context.Set<Page>().AddAsync(page);

        await _context.SaveChangesAsync();

        var fields = new StringBuilder();

        foreach (var field in pageInputs!)
        {
            if (
                field.DataType!.Equals(DatabaseDataType.Binary.Name)
                || field.DataType!.Equals(DatabaseDataType.Char.Name)
                || field.DataType!.Equals(DatabaseDataType.Nchar.Name)
                || field.DataType!.Equals(DatabaseDataType.Nvarchar.Name)
                || field.DataType!.Equals(DatabaseDataType.Varbinary.Name)
                || field.DataType!.Equals(DatabaseDataType.Varchar.Name)
                )
            {
                fields.Append($"[{field.DatabaseName!.Replace(" ", "")}][{field.DataType}]({field.Size}) NULL,");
            }
            else if (
                field.DataType!.Equals(DatabaseDataType.DateTime2.Name)
                || field.DataType!.Equals(DatabaseDataType.DateTimeOffset.Name)
                || field.DataType!.Equals(DatabaseDataType.Time.Name)
                )
            {
                fields.Append($"[{field.DatabaseName!.Replace(" ", "")}][{field.DataType}](7) NULL,");
            }
            else if (field.DataType!.Equals(DatabaseDataType.Decimal.Name)
                || field.DataType!.Equals(DatabaseDataType.Numeric.Name))
            {
                fields.Append($"[{field.DatabaseName!.Replace(" ", "")}][{field.DataType}]({field.DecimalPlace}) NULL,");
            }
            else
            {
                fields.Append($"[{field.DatabaseName!.Replace(" ", "")}][{field.DataType}] NULL,");
            }
        }

        await ScriptGenerateAsync(page.DbName, fields);

        return true;
    }

    public async Task<IEnumerable<PageInputDTO>> GetPageInputsAsync(Guid pageId)
    {
        var pageInputs = await GetPageInputsDeserializeAsync(pageId);

        if(pageInputs is null) return Enumerable.Empty<PageInputDTO>();

        return pageInputs!
            .Select(i => new PageInputDTO(i.Id!, i.Title!, i.DatabaseName!, i.FieldType!, i.PlaceHolder));
    }

    public async Task<IEnumerable<PageLookupDTO>> GetPagesAsync()
    {
        return await _context.Set<Page>()
            .Select(i => new PageLookupDTO(i.Id,i.Name,i.DbName))
            .ToListAsync();
    }

    private async Task<IEnumerable<PageInput>?> GetPageInputsDeserializeAsync(Guid pageId)
    {
        var page = await GetPageAsync(pageId);

        if (page is null) return Enumerable.Empty<PageInput>();

        return JsonConvert.DeserializeObject<IEnumerable<PageInput>>(page.Definition!);
    }

    private async Task<Page?> GetPageAsync(Guid id)
    {
        return await _context.Set<Page>().FirstOrDefaultAsync(i => i.Id == id);
    }

    private async Task ScriptGenerateAsync(string tableName,StringBuilder fields)
    {
        var query = $@"

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{tableName}')
                    BEGIN
                        CREATE TABLE [dbo].[{tableName}](
                            [Id] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT NEWID(),
                            {fields}
	                        [CreatedOn] [datetime] NOT NULL,
	                        [ModifiedOn] [datetime] NULL,
	                        [CreatedBy] [varchar](150) NULL,
	                        [ModifiedBy] [varchar](150) NULL
                        )
                    END;
                    ";

        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            try
            {
                using (var sqlCommand = new SqlCommand(query, connection))
                {
                    connection.Open();
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
