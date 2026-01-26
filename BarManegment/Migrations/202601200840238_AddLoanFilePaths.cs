namespace BarManegment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLoanFilePaths : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LoanApplications", "CouncilApprovalPath", c => c.String());
            AddColumn("dbo.LoanApplications", "MainPromissoryNotePath", c => c.String());
            AddColumn("dbo.LoanApplications", "DebtBondPath", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.LoanApplications", "DebtBondPath");
            DropColumn("dbo.LoanApplications", "MainPromissoryNotePath");
            DropColumn("dbo.LoanApplications", "CouncilApprovalPath");
        }
    }
}
