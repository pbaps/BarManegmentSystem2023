namespace BarManegment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixFinancialModels_Final : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.JournalEntry", "FiscalYearId");
            AddForeignKey("dbo.JournalEntry", "FiscalYearId", "dbo.FiscalYear", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.JournalEntry", "FiscalYearId", "dbo.FiscalYear");
            DropIndex("dbo.JournalEntry", new[] { "FiscalYearId" });
        }
    }
}
