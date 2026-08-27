using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class ReformatItemCodesByCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-number every item's code so each category has its own
            // running sequence, e.g. TUX-001, TUX-002, GOW-001, ACC-001...
            // Numbering follows creation order (ItemID) within each category.
            migrationBuilder.Sql(@"
                ;WITH Ranked AS (
                    SELECT
                        i.ItemID,
                        ROW_NUMBER() OVER (PARTITION BY i.CatID ORDER BY i.ItemID) AS Seq,
                        CASE
                            WHEN LOWER(c.CatName) = 'tuxedo'                              THEN 'TUX'
                            WHEN LOWER(c.CatName) = 'suit'                                THEN 'SUI'
                            WHEN LOWER(c.CatName) = 'gown'                                THEN 'GOW'
                            WHEN LOWER(c.CatName) IN ('accessory', 'accessories')         THEN 'ACC'
                            WHEN LOWER(c.CatName) = 'barong'                              THEN 'BAR'
                            WHEN LOWER(c.CatName) = 'dress'                               THEN 'DRS'
                            WHEN LEN(LOWER(REPLACE(c.CatName, ' ', ''))) >= 3
                                THEN UPPER(LEFT(REPLACE(c.CatName, ' ', ''), 3))
                            WHEN LEN(LOWER(REPLACE(c.CatName, ' ', ''))) > 0
                                THEN UPPER(RIGHT(REPLACE(c.CatName, ' ', '') + 'xxx', 3))
                            ELSE 'ITM'
                        END AS Prefix
                    FROM Items i
                    JOIN Categories c ON c.CatID = i.CatID
                )
                UPDATE Items
                SET ItemCode = Ranked.Prefix + '-' + RIGHT('000' + CAST(Ranked.Seq AS varchar(10)), 3)
                FROM Items
                INNER JOIN Ranked ON Ranked.ItemID = Items.ItemID;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only migration; the previous "ITM001"-style codes aren't
            // recoverable, so there's nothing meaningful to revert to.
        }
    }
}
