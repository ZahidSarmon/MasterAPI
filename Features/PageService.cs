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
    private const string _defaultExtendTableMultiselect = "Multiselect";
    private const string _defaultExtendTableCheckbox = "Checkbox";
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
            var safedColumns = pageInputsDeserialize!.Where(i => !string.IsNullOrWhiteSpace(i.DatabaseName)).Select(i=>i.DatabaseName).ToList();

            var columnHeader = new List<string>();

            columnHeader.AddRange(safedColumns!);

            var additionalQueries = new List<string>();

            foreach (var pageInput in pageInputsDeserialize!)
            {
                string columnTitle = pageInput.Title!.Replace(" ", "");

                if (pageInput.FieldType == FieldType.MultiSelect.Name)
                {
                    if (pageInput.ComboInput!.IsDataBaseSource)
                    {
                        string buildQuery = BuildMultiselectQueryDbSourceInputValues(page,pageInput);

                        additionalQueries.Add(buildQuery);

                        columnHeader.Add(columnTitle);
                    }
                    else
                    {
                        string buildQuery = BuildMultiselectQueryInputValues(page,pageInput);

                        additionalQueries.Add(buildQuery);

                        columnHeader.Add(columnTitle);
                    }
                }
            }

            string query = BuildQueryInputValues(page!.DatabaseName,safedColumns,additionalQueries);

            using (var sqlCommand = new SqlCommand(query, connection))
            {
                try
                {
                    if (connection!=null && connection.State == ConnectionState.Closed) connection.Open();

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

                        return new PageInputValueModel("Id,"+string.Join(",", columnHeader), jsonArray);
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }

    public async Task<IEnumerable<PageInputModel>> GetPageInputValueAsync(Guid pageId,Guid pageTableId)
    {
        var page = await GetPageAsync(pageId);

        var pageInputsDeserialize = (await GetPageInputsDeserializeAsync(pageId))!.ToList();

        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            foreach (var pageInput in pageInputsDeserialize)
            {
                string columnTitle = $"col_{pageInput.Title!.Replace(" ", "")}";

                if (pageInput.FieldType == FieldType.MultiSelect.Name)
                {
                    if (pageInput.ComboInput!.IsDataBaseSource)
                    {
                        string buildColumnQuery = BuildMultiselectQueryDbSourceInputValues(page, pageInput,pageTableId,true);

                        string columnValues = await GetMultiselectColumnValueAsync(connection, buildColumnQuery, columnTitle);

                        string buildLookupDataQuery = BuildLookupDataQueryForMultiselect(
                                pageInput.ComboInput.TableRef.TableSchema!,
                                pageInput.ComboInput.TableRef.TableName!,
                                pageInput.ComboInput.TableRef.IdColumn!,
                                pageInput.ComboInput.TableRef.NameColumn!,
                                columnValues
                                );

                        pageInput.ComboInput.Data = (await GetLookupDataFromQueryAsync(buildLookupDataQuery)).ToList();
                    }
                    else
                    {
                        string buildColumnQuery = BuildMultiselectQueryInputValues(page, pageInput,pageTableId,true);

                        string columnValues = await GetMultiselectColumnValueAsync(connection, buildColumnQuery, columnTitle);

                        if (!string.IsNullOrWhiteSpace(columnValues))
                        {
                            pageInput.ComboInput.Data = columnValues.Contains(",") ? 
                                    columnValues.Split(",").Select(i => new Lookup<string> { Id = i, Name = i }).ToList() : 
                                    new List<string>{ columnValues}.Select(i => new Lookup<string> { Id = i, Name = i }).ToList();
                        }
                    }
                }
            }
        }

        return pageInputsDeserialize;
    }

    public async Task<bool> DeletePageInputValueAsync(DeletePageCommand command)
    {
        SqlTransaction transaction = null;

        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            try
            {
                connection.Open();

                transaction = connection.BeginTransaction("DeletePageInputValueAsync");

                bool hasDelete = await DeletePageInputValueAsync(connection, transaction, command.TableName, command.Id);

                transaction.Commit();

                return await Task.FromResult(hasDelete);
            }
            catch (Exception)
            {
                if (transaction != null) transaction.Rollback();

                throw;
            }
            finally
            {
                if (transaction != null) transaction.Dispose();

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

                if (command.Id.HasValue)
                {
                    await DeletePageInputValueAsync(connection,transaction,command.TableName,command.Id.Value);
                }

                await SavePageInputValueAsync(command, connection, transaction, pageInputId);

                var multiselectQuery = BuildMultiselectPageInputValueQuery(command,pageInputId);

                if (multiselectQuery.Length>0)
                {
                    await ExecuteMultiselectPageInputValueAsync(multiselectQuery, connection, transaction);
                }

                transaction.Commit();

                return await Task.FromResult(true);
            }
            catch
            {
                if (transaction != null) transaction.Rollback();

                throw;
            }
            finally
            {
                connection.Close();
            }
        }
    }

    public async Task<bool> PostPageExcelInputValueAsync(PostPageExcelInputCommand command)
    {
        
        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
        {
            connection.Open();

            var queries = new StringBuilder();

            foreach (var value in command.Values)
            {
                var columns = command.Columns.Where(i => !string.IsNullOrWhiteSpace(i));

                string columnHeaders = string.Join(",", columns.Select(i => $"[{i}]"));

                string columnValues = string.Join(",",value.Select(i=>$"'{i.Replace("'"," ")}'"));

                string buildPageInputQuery = @$"INSERT INTO [dbo].[{command.TableName}] (Id,{columnHeaders},CreatedOn,CreatedBy) 
                                    VALUES('{Guid.NewGuid()}',{columnValues},'{DateTime.UtcNow}','{command.User}')";

                queries.Append(buildPageInputQuery);
            }

            using (var sqlCommand = new SqlCommand(queries.ToString(),connection))
            {
                return await sqlCommand.ExecuteNonQueryAsync()>0;
            }
        }
    }

    private async Task SavePageInputValueAsync(
        PostPageInputCommand command, 
        SqlConnection connection, 
        SqlTransaction transaction,
        Guid pageInputId)
    {
        var columns = command.Columns.Where(i => !string.IsNullOrWhiteSpace(i));

        string columnHeaders = columns.Any() ? ","+string.Join(",", columns.Select(i => $"[{i}]")) : string.Empty;

        string columnValue = columns.Any() ? ","+string.Join(",", columns.Select(item => $"@{item}")) : string.Empty;

        string buildPageInputQuery = @$"INSERT INTO [dbo].[{command.TableName}] (Id{columnHeaders},CreatedOn,CreatedBy) 
                                    VALUES(@Id{columnValue},@CreatedOn,@CreatedBy)";

        using (var sqlCommand = new SqlCommand(buildPageInputQuery, connection, transaction))
        {
            foreach (var option in command.ColumnWithValues)
            {
                if (option.Key.IsSafedKey())
                {
                    sqlCommand.Parameters.AddWithValue($"@{option.Key}", option.Value);
                }
            }

            sqlCommand.Parameters.AddWithValue("@Id", pageInputId);
            sqlCommand.Parameters.AddWithValue("@CreatedOn", DateTime.UtcNow);
            sqlCommand.Parameters.AddWithValue("@CreatedBy", command.User);

            await sqlCommand.ExecuteNonQueryAsync();
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
                string extendTable = $"tb_{baseTable}{_defaultExtendTableMultiselect}";
                string baseId = $"{baseTable}Id";
                string derivedId = "Value";
                string requiredText = "NULL";

                if (pageInput.ComboInput.IsDataBaseSource && !string.IsNullOrWhiteSpace(deriveTable))
                {
                    extendTable = $"tb_{baseTable}{deriveTable}";
                    derivedId = $"{deriveTable}Id";
                    extendFields.Append($"[{derivedId}][{DatabaseDataType.Guid.Name}] {requiredText},");
                }
                else
                {
                    extendFields.Append($"[{derivedId}][{DatabaseDataType.Varchar.Name}](MAX) {requiredText},");
                }

                extendFields.Append($"[{baseId}][{DatabaseDataType.Guid.Name}] {requiredText},");

                await TableGenerateAsync(extendTable, extendFields);
            }
            else
            {
                string requiredText = pageInput.IsRequired ? "NOT NULL" : "NULL";
                if (
                pageInput.DataType!.Equals(DatabaseDataType.Binary.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Char.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Nchar.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Nvarchar.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Varbinary.Name)
                || pageInput.DataType!.Equals(DatabaseDataType.Varchar.Name)
                )
                {
                    pageInputFields.Append($"[{pageInput.DatabaseName!.Trim()}][{pageInput.DataType}]({pageInput.Size}) {requiredText},");
                }
                else if (
                    pageInput.DataType!.Equals(DatabaseDataType.DateTime2.Name)
                    || pageInput.DataType!.Equals(DatabaseDataType.DateTimeOffset.Name)
                    || pageInput.DataType!.Equals(DatabaseDataType.Time.Name)
                    )
                {
                    pageInputFields.Append($"[{pageInput.DatabaseName!.Trim()}][{pageInput.DataType}](7) {requiredText},");
                }
                else if (pageInput.DataType!.Equals(DatabaseDataType.Decimal.Name)
                    || pageInput.DataType!.Equals(DatabaseDataType.Numeric.Name))
                {
                    pageInputFields.Append($"[{pageInput.DatabaseName!.Trim()}][{pageInput.DataType}](18,{pageInput.DecimalPlace}) {requiredText},");
                }
                else
                {
                    pageInputFields.Append($"[{pageInput.DatabaseName!.Trim()}][{pageInput.DataType}] {requiredText},");
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

    private async Task<bool> DeletePageInputValueAsync(SqlConnection connection,SqlTransaction transaction, string tableName, Guid id)
    {
        string buildQuery = @$"DELETE [dbo].[{tableName}] WHERE Id = @Id";

        using (var sqlCommand = new SqlCommand(buildQuery, connection, transaction))
        {
            sqlCommand.Parameters.AddWithValue("@Id", id);

            return await sqlCommand.ExecuteNonQueryAsync() > 0;
        }
    }

    private async Task<string> GetMultiselectColumnValueAsync(SqlConnection connection,string buildQuery,string columnTitle)
    {
        try
        {
            if (connection != null && connection.State == ConnectionState.Closed) connection.Open();

            using (var sqlCommand = new SqlCommand(buildQuery, connection))
            {
                var reader = await sqlCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return reader[columnTitle].ToString()!;
                }
            }
        }
        finally
        {
            connection.Close();
        }

        return string.Empty;
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

    private StringBuilder BuildMultiselectPageInputValueQuery(PostPageInputCommand command, Guid pageInputId)
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
                    string tableName = $"tb_{command.TableName}{_defaultExtendTableMultiselect}";

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

    private string BuildQueryInputValues(string databaseName,IList<string> columns,IList<string> additionalQueries)
    {
        string columnQuery = !columns.Any() ? "Id" : $"Id,{string.Join(",", columns)}";

        string additionalQuery = additionalQueries.Any() ? "," + string.Join(",", additionalQueries) : "";

        string query = @$"
                    SELECT 
                        {columnQuery} 
                        {additionalQuery}
                    FROM 
                        [dbo].[{databaseName}] baseTable";

        if (!columns.Any() && !additionalQueries.Any()) query = $"select Id from {databaseName}";

        return query;
    }

    private string BuildMultiselectQueryDbSourceInputValues(Page page,PageInputModel pageInput,Guid? pageTableId=null,bool isUpdate=false)
    {
        string columnTitle = $"col_{pageInput.Title!.Replace(" ", "")}";

        string table = $"[tb_{page!.DatabaseName}{pageInput.ComboInput.TableRef.TableName}]";

        string baseTableId = isUpdate ? $"'{pageTableId}'" : "baseTable.Id";

        string column = isUpdate ? pageInput.ComboInput.TableRef.IdColumn : pageInput.ComboInput.TableRef.NameColumn;

        string referance_table = $"[{pageInput.ComboInput.TableRef.TableSchema}].[{pageInput.ComboInput.TableRef.TableName}]";

        string query = $@"SELECT 
		                    STRING_AGG(CONVERT(varchar(max),{table}.[{column}]),',') AS {columnTitle}
	                    FROM 
		                    (
			                    SELECT {table}.[{page!.DatabaseName}Id] AS {page!.DatabaseName}Id,{referance_table}.[{column}] 
                                FROM {referance_table}  
			                    INNER JOIN {table}  
			                    ON {referance_table}.[Id] = {table}.[{pageInput.ComboInput.TableRef.TableName}Id]
		                    ) as  {table}
	                    WHERE {table}.[{page!.DatabaseName}Id] = {baseTableId}";

        if (isUpdate) return query;

        return @$"({query}) AS {columnTitle}";
    }

    private string BuildMultiselectQueryInputValues(Page page, PageInputModel pageInput,Guid? pageTableId=null,  bool isUpdate=false)
    {
        string columnTitle = $"col_{pageInput.Title!.Replace(" ", "")}";

        string table = $"[tb_{page!.DatabaseName}{_defaultExtendTableMultiselect}]";

        string baseTableId = isUpdate ? $"'{pageTableId}'" : "baseTable.Id";

        string query = @$"SELECT 
	                            STRING_AGG({table}.[Value], ',') AS {columnTitle}
                            FROM 
	                            {table}
                            WHERE
	                            {table}.{page!.DatabaseName}Id = {baseTableId}";

        if (isUpdate) return query;

        return @$"({query}) AS {columnTitle}";
    }

    private async Task<IEnumerable<Lookup<string>>> GetLookupDataFromQueryAsync(string query)
    {
        return await _context.Database.SqlQueryRaw<Lookup<string>>(query).ToListAsync();
    }

    private string BuildLookupDataQueryForMultiselect(string tableSchema,string tableName,string columnId,string columnName,string parameter)
    {
        string safeParameter = parameter.Contains(",")?string.Join(",", parameter.Split(',').Select(i => $"'{i}'")) : $"'{parameter}'";

        return $@"
            SELECT CONVERT(varchar(max),[{columnId}]) as Id,[{columnName}] FROM [{tableSchema}].[{tableName}] 
            WHERE [{columnId}] IN ({safeParameter})
            ";
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

    private async Task<IEnumerable<string>> GetCheckboxDataLookupAsync(DatabaseTableRef tableRef)
    {
        return await _context.Database.SqlQueryRaw<string>(
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
