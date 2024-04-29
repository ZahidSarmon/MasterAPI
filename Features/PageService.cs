using Master.Data;
using Master.Domain;
using Master.Entities;
using Master.Features.DTOs;
using Master.Features.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Syncfusion.EJ2.Base;
using System.Data;
using System.Text;

namespace Master.Features;

public class PageService : IPageService
{
    private readonly AppDbContext _context;
    private const string _defaultExtendTable = "Multiselect";
    public PageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PageInputValueModel> GetPageInputValuesAsync(Guid pageId)
    {
        var page = await GetPageAsync(pageId);

        var pageInputsDeserialize = await GetPageInputsDeserializeAsync(pageId);

        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            var safedColumns = pageInputsDeserialize!.Where(i => !string.IsNullOrWhiteSpace(i.DatabaseName)).Select(i=>i.DatabaseName);

            var columns = $"Id,{string.Join(",", safedColumns)}";

            string query = $"select Id,{string.Join(",", safedColumns.Select(i => $"[{i}]"))} from {page!.DatabaseName}";

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

                        return new PageInputValueModel(columns, jsonArray);
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
        SqlTransaction transaction = null;

        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            try
            {
                Guid pageInputId = Guid.NewGuid();

                connection.Open();

                transaction = connection.BeginTransaction("PostPageInputValuesAsync");

                if (command.Id is not null && command.Id != Guid.Empty)
                {
                    await UpdatePageInputValueAsync(command, connection,transaction);
                }
                else
                {
                    await SavePageInputValueAsync(command, connection, transaction,pageInputId);
                }

                var multiselectQuery = BuildMultiselectPageInputValueQuery(command,pageInputId);

                if (multiselectQuery.Length>0)
                {
                    await ExecuteMultiselectPageInputValueAsync(multiselectQuery, connection, transaction);
                }

                transaction.Commit();

                return true;
            }
            catch
            {
                if (transaction != null) transaction.Rollback();
            }
            finally
            {
                connection.Close();
            }

            return false;
        }
    }

    private async Task UpdatePageInputValueAsync(PostPageInputCommand command,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        string buildQuery = @$"UPDATE {command.TableName}
                                    SET {string.Join(",", command.Columns.Select(item => $"{item}=@{item}"))}
                                    WHERE Id = @Id";

        using (var sqlCommand = new SqlCommand(buildQuery, connection, transaction))
        {
            foreach (var option in command.ColumnWithValues)
            {
                sqlCommand.Parameters.AddWithValue($"@{option.Key}", option.Value);
            }

            sqlCommand.Parameters.AddWithValue("@ModifiedOn", DateTime.UtcNow);
            sqlCommand.Parameters.AddWithValue("@ModifiedBy", command.User);

            await sqlCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task SavePageInputValueAsync(PostPageInputCommand command, 
        SqlConnection connection, 
        SqlTransaction transaction,
        Guid pageInputId)
    {
        var columns = command.Columns.Where(i => !string.IsNullOrWhiteSpace(i));

        string buildPageInputQuery = @$"INSERT INTO {command.TableName} (Id,{string.Join(",", columns.Select(i=>$"[{i}]"))},CreatedOn,CreatedBy) 
                                    VALUES(@Id,{string.Join(",", columns.Select(item => $"@{item}"))},@CreatedOn,@CreatedBy)";

        using (var sqlCommand = new SqlCommand(buildPageInputQuery, connection, transaction))
        {
            foreach (var option in command.ColumnWithValues)
            {
                sqlCommand.Parameters.AddWithValue($"@{option.Key}", option.Value);
            }

            sqlCommand.Parameters.AddWithValue("@Id", pageInputId);
            sqlCommand.Parameters.AddWithValue("@CreatedOn", DateTime.UtcNow);
            sqlCommand.Parameters.AddWithValue("@CreatedBy", command.User);

            await sqlCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task ExecuteMultiselectPageInputValueAsync(StringBuilder query,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        using (var sqlCommand = new SqlCommand(query.ToString(), connection, transaction))
        {
            await sqlCommand.ExecuteNonQueryAsync();
        }
    }

    private StringBuilder BuildMultiselectPageInputValueQuery(PostPageInputCommand command,Guid pageInputId)
    {
        var comboQueries = new StringBuilder();

        foreach (var comboInput in command.ComboInputs)
        {
            if (comboInput.Data.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(comboInput.TableName))
                {
                    string tableName = $"tb_{command.TableName}{comboInput.TableName}";

                    foreach (var data in comboInput.Data)
                    {
                        string buildQuery = @$"INSERT INTO {tableName} (Id,{command.TableName}Id,{comboInput.TableName}Id,CreatedOn,CreatedBy) 
                                    VALUES('{Guid.NewGuid()}','{pageInputId}','{data.Id}','{DateTime.UtcNow}','{command.User}')";

                        comboQueries.Append(buildQuery);
                    }
                }
                else
                {
                    string tableName = $"tb_{command.TableName}{_defaultExtendTable}";

                    foreach (var data in comboInput.Data)
                    {
                        string buildQuery = @$"INSERT INTO {tableName} (Id,{command.TableName}Id,Value,CreatedOn,CreatedBy) 
                                    VALUES('{Guid.NewGuid()}','{pageInputId}','{data.Id}','{DateTime.UtcNow}','{command.User}')";

                        comboQueries.Append(buildQuery);
                    }

                }
            }
        }

        return comboQueries;
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

                    await sqlCommand.ExecuteNonQueryAsync();
                }

                return true;
            }
            finally
            {
                connection.Close();
            }
        }
    }

    public async Task<bool> PostPageInputsAsync(PageCommand command)
    {
        var pageInputs = JsonConvert.DeserializeObject<IEnumerable<PageInputModel>>(command.Definition);

        var page = new Page
        {
            Id = command.Id,
            Name = command.Name,
            DatabaseName = command.DatabaseName,
            Definition = command.Definition
        };

        var pageExists = await _context.Set<Page>().FindAsync(page.Id);

        if (pageExists is null)
        {
            page.Id = Guid.NewGuid();

            await _context.Set<Page>().AddAsync(page);
        }
        else
        {
            pageExists.Name = page.Name;
            pageExists.DatabaseName = page.DatabaseName;
            pageExists.Definition = page.Definition;

            _context.Set<Page>().Update(pageExists);
        }

        await _context.SaveChangesAsync();

        var pageInputFields = new StringBuilder();

        foreach (var pageInput in pageInputs!)
        {
            if (pageInput.FieldType!.Equals(FieldType.MultiSelect.Name))
            {
                var extendFields = new StringBuilder();

                string baseTable = page.DatabaseName;
                string deriveTable = pageInput.ComboInput!.TableRef.TableName!;
                string extendTable = $"tb_{baseTable}{_defaultExtendTable}".ToLower();
                string baseId = $"{baseTable}Id";
                string derivedId = "Value";

                if (pageInput.ComboInput.IsDataBaseSource && !string.IsNullOrWhiteSpace(deriveTable))
                {
                    extendTable = $"tb_{baseTable}{deriveTable}".ToLower();
                    derivedId = $"{deriveTable}Id";
                    extendFields.Append($"[{derivedId}][{DatabaseDataType.Guid.Name}] NULL,");
                }
                else
                {
                    extendFields.Append($"[{derivedId}][{DatabaseDataType.Varchar.Name}](MAX) NULL,");
                }

                extendFields.Append($"[{baseId}][{DatabaseDataType.Guid.Name}] NULL,");

                await TableGenerateAsync(extendTable, extendFields);
            }
            else
            {
                if (
                pageInput.DataType!.Equals(DatabaseDataType.Binary.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Char.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Nchar.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Nvarchar.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Varbinary.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Varchar.Name)
                )
                {
                    pageInputFields.Append($"[{pageInput.DatabaseName!.Trim()}][{pageInput.DataType}]({pageInput.Size}) NULL,");
                }
                else if (
                    pageInput.DataType!.Equals(DatabaseDataType.DateTime2.Name)
                    || pageInput.DataType!.Equals(DatabaseDataType.DateTimeOffset.Name)
                    || pageInput.DataType!.Equals(DatabaseDataType.Time.Name)
                    )
                {
                    pageInputFields.Append($"[{pageInput.DatabaseName!.Trim()}][{pageInput.DataType}](7) NULL,");
                }
                else if (pageInput.DataType!.Equals(DatabaseDataType.Decimal.Name)
                    || pageInput.DataType!.Equals(DatabaseDataType.Numeric.Name))
                {
                    pageInputFields.Append($"[{pageInput.DatabaseName!.Trim()}][{pageInput.DataType}]({pageInput.DecimalPlace}) NULL,");
                }
                else
                {
                    pageInputFields.Append($"[{pageInput.DatabaseName!.Trim()}][{pageInput.DataType}] NULL,");
                }
            }
        }

        await TableGenerateAsync(page.DatabaseName, pageInputFields);

        return true;
    }

    public async Task<IEnumerable<PageInputModel>> GetPageInputsAsync(Guid pageId)
    {
        var pageInputs = await GetPageInputsDeserializeAsync(pageId);

        if(pageInputs is null) return Enumerable.Empty<PageInputModel>();

        foreach (var pageInput in pageInputs)
        {
            var comboData = pageInput.ComboInput!.Data;
            var radioData = pageInput.RadioInput!.Data;
            var checkboxData = pageInput.CheckBoxInput!.Data;

            if (pageInput.ComboInput!.IsDataBaseSource)
            {
               comboData = (await GetComboDataLookupAsync(pageInput.ComboInput.TableRef)).ToList();
            }

            if (pageInput.RadioInput!.IsDataBaseSource)
            {
                radioData = (await GetRadioLookupAsync(pageInput.RadioInput.TableRef)).ToList();
            }

            if (pageInput.CheckBoxInput!.IsDataBaseSource)
            {
                checkboxData = (await GetCheckboxDataLookupAsync(pageInput.CheckBoxInput.TableRef)).ToList();
            }

            pageInput.ComboInput.Data = comboData;
            pageInput.RadioInput.Data = radioData;
            pageInput.CheckBoxInput.Data = checkboxData;
        }

        return pageInputs;
    }

    public async Task<IEnumerable<PageLookupModel>> GetLookupPagesAsync()
    {
        return await _context.Set<Page>()
            .Select(i => new PageLookupModel(i.Id,i.Name,i.DatabaseName))
            .ToListAsync();
    }

    public async Task<Property<Entities.Page>> GetPagesAsync(PageDR pagination)
    {
        try
        {
            var dataSource = _context.Set<Entities.Page>()
                .AsNoTracking();

            DataOperations dataOperations = new DataOperations();

            if (pagination.Search != null)
            {
                dataSource = dataOperations.PerformSearching(dataSource, pagination.Search.MapFieldsToDbColumnNames());
            }
            if (pagination.Where != null && pagination.Where.Count > 0)
            {
                dataSource = dataOperations.PerformFiltering(dataSource, pagination.Where.MapFieldsToDbColumnNames(), pagination.Where[0].Operator);
            }

            int count = dataSource.Count();

            if (pagination.Skip != 0)
            {
                dataSource = dataOperations.PerformSkip(dataSource, pagination.Skip); //Paging
            }

            if (pagination.Take != 0)
            {
                dataSource = dataOperations.PerformTake(dataSource, pagination.Take);
            }

            if (pagination.Sorted is not null)
            {
                dataSource = dataOperations.PerformSorting(dataSource, pagination.Sorted);
            }

            var data = await dataSource.ToListAsync();

            return new Property<Entities.Page>(data, count);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> DeletePageAsync(Guid id)
    {
        var page = await _context.Set<Page>().FindAsync(id);

        if (page is null) return false;

        _context.Set<Page>().Remove(page);

        await TableDeleteAsync(page.DatabaseName);

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<Lookup<string>>> GetTableColumnsAsync(string schema,string table)
    {
        return await _context.Database.SqlQueryRaw<Lookup<string>>(
            @"SELECT DISTINCT COLUMN_NAME As Id,COLUMN_NAME As Name FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                ORDER BY COLUMN_NAME",
            [
                new SqlParameter("@schema", schema),
                new SqlParameter("@table", table)
            ]
            )
            .ToListAsync();
    }

    public async Task<IEnumerable<Lookup<string>>> GetTableNamesAsync(string schema)
    {
        return await _context.Database.SqlQueryRaw<Lookup<string>>(
            "SELECT DISTINCT TABLE_NAME As Id,TABLE_NAME As Name FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema ORDER BY TABLE_NAME",
            new SqlParameter("@schema", schema)
            )
            .ToListAsync();
    }

    public async Task<IEnumerable<Lookup<string>>> GetTableSchemasAsync()
    {
        return await _context.Database.SqlQueryRaw<Lookup<string>>(
            "SELECT DISTINCT TABLE_SCHEMA AS Id,TABLE_SCHEMA AS Name  FROM INFORMATION_SCHEMA.COLUMNS ORDER BY TABLE_SCHEMA"
            )
            .ToListAsync();
    }
     
    private async Task<IEnumerable<Lookup<string>>> GetComboDataLookupAsync(DatabaseTableRef tableRef)
    {
        return await _context.Database.SqlQueryRaw<Lookup<string>>(
            $"SELECT DISTINCT CONVERT(varchar(200),[{tableRef.IdColumn}]) as Id,[{tableRef.NameColumn}] as Name FROM [{tableRef.TableSchema}].[{tableRef.TableName}] ORDER BY Name"
            )
            .ToListAsync();
    }

    private async Task<IEnumerable<string>> GetRadioLookupAsync(DatabaseTableRef tableRef)
    {
        return await _context.Database.SqlQueryRaw<string>(
            $"SELECT DISTINCT CONVERT(varchar(200),[{tableRef.IdColumn}]) as Id FROM [{tableRef.TableSchema}].[{tableRef.TableName}] ORDER BY Id"
            )
            .ToListAsync();
    }

    private async Task<IEnumerable<Lookup<string>>> GetCheckboxDataLookupAsync(DatabaseTableRef tableRef)
    {
        return await _context.Database.SqlQueryRaw<Lookup<string>>(
            $"SELECT DISTINCT CONVERT(varchar(200),[{tableRef.IdColumn}]) as Id,[{tableRef.IdColumn}] as Name FROM [{tableRef.TableSchema}].[{tableRef.TableName}] ORDER BY Name"
            )
            .ToListAsync();
    }

    private async Task<IEnumerable<PageInputModel>?> GetPageInputsDeserializeAsync(Guid pageId)
    {
        var page = await GetPageAsync(pageId);

        if (page is null) return Enumerable.Empty<PageInputModel>();

        return JsonConvert.DeserializeObject<IEnumerable<PageInputModel>>(page.Definition!);
    }

    private async Task<Page?> GetPageAsync(Guid id)
    {
        return await _context.Set<Page>().FirstOrDefaultAsync(i => i.Id == id);
    }

    private async Task TableDeleteAsync(string tableName)
    {
        var query = $@"

                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{tableName}')
                    BEGIN
                        DROP TABLE [dbo].[{tableName}]
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

    private async Task TableGenerateAsync(string tableName,StringBuilder fields)
    {
        var query = $@" IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{tableName}')
                    BEGIN
                        DROP TABLE [dbo].[{tableName}]
                    END;
                    CREATE TABLE [dbo].[{tableName}](
                        [Id] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT NEWID(),
                        {fields}
	                    [CreatedOn] [datetime] NOT NULL,
	                    [ModifiedOn] [datetime] NULL,
	                    [CreatedBy] [varchar](150) NULL,
	                    [ModifiedBy] [varchar](150) NULL)";

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
