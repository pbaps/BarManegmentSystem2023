namespace BarManegment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateLoanInstallmentSchema2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentVoucher", "IsPaid", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PaymentVoucher", "IsPaid");
        }
    }
}
