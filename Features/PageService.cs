using Master.Data;
using Master.Domain;
using Master.Entities;
using Master.Features.DTOs;
using Master.Features.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace Master.Features;

public class PageService : IPageService
{
    private readonly AppDbContext _context;
    public PageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Post(PageCommand command)
    {
        using (var transaction = _context.Database.BeginTransaction())
        {
            try
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
                    if (field.DataType!.Equals(DatabaseDataType.Int.Name) || field.DataType!.Equals(DatabaseDataType.Bigint.Name))
                    {
                        fields.Append($"[{field.DatabaseName!.Replace(" ", "")}][{field.DataType}] NULL,");
                    }
                    if (field.DataType!.Equals(DatabaseDataType.Varchar.Name))
                    {
                        fields.Append($"[{field.DatabaseName!.Replace(" ", "")}][{field.DataType}]({field.VarcharSize}) NULL,");
                    }
                }

                await ScriptGenerateAsync(page.DbName,fields);

                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                if (transaction != null) transaction.Rollback();
            }
            finally
            {
                transaction.Dispose();
            }
        }

        return false;
    }

    public async Task<IEnumerable<PageInputDTO>> GetPageInputs(Guid pageId)
    {
        var page = await _context.Set<Page>().FirstOrDefaultAsync(i=>i.Id==pageId);

        if (page is not null)
        {
            var pageInputs = JsonConvert.DeserializeObject<IEnumerable<PageInput>>(page.Definition!);

            return pageInputs!
                .Select(i => new PageInputDTO(i.Id!,i.Title!,i.DatabaseName!,i.FieldType!,i.PlaceHolder));
        }

        return Enumerable.Empty<PageInputDTO>();
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetPages()
    {
        return await _context.Set<Page>()
            .Select(i => new Lookup<Guid> { Id = i.Id, Name = i.Name })
            .ToListAsync();
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
