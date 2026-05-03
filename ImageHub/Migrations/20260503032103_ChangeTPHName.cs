using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageHub.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTPHName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Source"" 
                SET ""SourceType"" = CASE ""SourceType""
                    WHEN 0 THEN 'Unknown'
                    WHEN 1 THEN 'Pixiv'
                    WHEN 2 THEN 'Twitter'
                    WHEN 3 THEN 'Xiaohongshu'
                    WHEN 4 THEN 'Weibo'
                    ELSE 'Unknown'
                END;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""PublishTarget"" 
                SET ""PublishTargetType"" = CASE ""PublishTargetType""
                    WHEN 0 THEN 'Unknown'
                    WHEN 1 THEN 'TelegramGroup'
                    ELSE 'Unknown'
                END;
            ");



            migrationBuilder.AlterColumn<string>(
                name: "SourceType",
                table: "Source",
                type: "TEXT",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "PublishTargetType",
                table: "PublishTarget",
                type: "TEXT",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Source"" 
                SET ""SourceType"" = CASE ""SourceType""
                    WHEN 'Pixiv' THEN 1
                    WHEN 'Twitter' THEN 2
                    WHEN 'Xiaohongshu' THEN 3
                    WHEN 'Weibo' THEN 4
                    ELSE 0
                END;
            ");

                    // 回滚 PublishTarget 表：字符串转整数
                    migrationBuilder.Sql(@"
                UPDATE ""PublishTarget"" 
                SET ""PublishTargetType"" = CASE ""PublishTargetType""
                    WHEN 'TelegramGroup' THEN 1
                    ELSE 0
                END;
            ");


            migrationBuilder.AlterColumn<int>(
                name: "SourceType",
                table: "Source",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 13);

            migrationBuilder.AlterColumn<int>(
                name: "PublishTargetType",
                table: "PublishTarget",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 13);
        }
    }
}
