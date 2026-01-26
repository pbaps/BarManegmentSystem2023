namespace BarManegment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateLoanInstallmentSchema : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LoanApplications", "IsPaid", c => c.Boolean(nullable: false));
            AddColumn("dbo.LoanInstallments", "IsPaid", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LoanInstallments", "IsPaid");
            DropColumn("dbo.LoanApplications", "IsPaid");
        }
    }
}
