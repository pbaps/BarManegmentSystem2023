namespace BarManegment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddReasonToRequests : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SupervisorChangeRequest", "Reason", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SupervisorChangeRequest", "Reason");
        }
    }
}
