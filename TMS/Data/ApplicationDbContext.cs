using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TMS.Models;

namespace TMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<AppUser> AppUsers { get; set; }//DbSet property used by all team members
        public DbSet<AppRole> AppRoles { get; set; }//DbSet property used by all team members

		
        public DbSet<LessonType> LessonTypes { get; set; }  //DbSet property which you need to use in the assignment
        public DbSet<CustomerAccount> CustomerAccounts { get; set; } //DbSet property which you need to use in the assignment
        //https://youtu.be/qqfvw6vN1n4
        public DbSet<AccountRate> AccountRates { get; set; }//DbSet property which you need to use in the assignment
        public DbSet<AccountTimeTable> AccountTimeTable { get; set; }//DbSet property which you need to use in the assignment
        public DbSet<CustomerAccountComment> CustomerAccountComments { get; set; }
        
        public DbSet<InstructorAccount> InstructorAccounts { get; set; }//Used by other team members in FYP who is in charge of coding assign instructor to customer
        
        public DbSet<TimeSheet> TimeSheets { get; set; }//Used by other team members in FYP who is in charge of Instructor role functionalities

        public DbSet<TimeSheetSchedule> TimeSheetSchedules { get; set; }//Used by other team members in FYP who is in charge of Instructor role functionalities
        public DbSet<TimeSheetScheduleSignature> TimeSheetScheduleSignature { get; set; }//Used by other team members in FYP who is in charge of Instructor role functionalities     
        public DbSet<AppNote> AppNotes { get; set; }
        public DbSet<AppNotePriorityLevel> AppNotePriorityLevels { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.Relational().TableName = entity.DisplayName();
            }


            modelBuilder.Entity<AppUser>()
              .HasOne(input => input.Role)
              .WithMany()
              .HasForeignKey(input => input.RoleId)
              .OnDelete(DeleteBehavior.Restrict)
              .IsRequired();


            //----------------------------------------------------------------------------
            //Relationship database modeling
            //----------------------------------------------------------------------------

            //-------------------------------------------------------------------------------------------
            //Note: InstructorAccount is a join table
            //--------------------------------------------------------------------------------------------
            //many-to-one relationship between InstructorAccount and CustomerAccount
            modelBuilder.Entity<InstructorAccount>()
              .HasOne(input => input.CustomerAccount)
              .WithMany(input => input.InstructorAccounts)
              .HasForeignKey(input => input.CustomerAccountId);

            //many-to-one relationship between InstructorAccount and AppUser
            modelBuilder.Entity<InstructorAccount>()
              .HasOne(input => input.Instructor)
              .WithMany(input => input.InstructorAccounts)
              .HasForeignKey(input => input.InstructorId);

            //many-to-one relationship between InstructorAccount and AppUser
            modelBuilder.Entity<InstructorAccount>()
              .HasOne(input => input.CreatedBy)
              .WithMany()
              .HasForeignKey(input => input.CreatedById);

            //Many-to-one relationship between TimeSheetDetail and TimeSheet
            //for the child-parent relationship
            modelBuilder.Entity<TimeSheetSchedule>()
              .HasOne(input => input.TimeSheet)
              .WithMany(input => input.TimeSheetSchedules)
              .HasForeignKey(input => input.TimeSheetId);

            //many-to-one relationship between TimeSheet and AppUser
            //for the createdby relationship
            //Usually this relationship links to the administrator user inside
            //that AppUser table.
            modelBuilder.Entity<TimeSheet>()
              .HasOne(input => input.CreatedBy)
                .WithMany()
                .HasForeignKey(input => input.CreatedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            //many-to-one relationship between TimeSheet and AppUser
            //for the approval relationship
            modelBuilder.Entity<TimeSheet>()
              .HasOne(input => input.ApprovedBy)
                .WithMany()
                .HasForeignKey(input => input.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            //many-to-one relationship between TimeSheet and AppUser
            modelBuilder.Entity<TimeSheet>()
              .HasOne(input => input.UpdatedBy)
                .WithMany()
                .HasForeignKey(input => input.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            //foreign key relationship for CustomerAccount with the AppUser table.
            modelBuilder.Entity<CustomerAccount>()
                 .HasOne(input => input.CreatedBy)
                 .WithMany()
                 .HasForeignKey(input => input.CreatedById)
                 .OnDelete(DeleteBehavior.SetNull);


            modelBuilder.Entity<CustomerAccount>()
                .HasOne(input => input.UpdatedBy)
                .WithMany()
                .HasForeignKey(input => input.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);

            //Foreign key relationship for LessonType with the AppUser table.
            modelBuilder.Entity<LessonType>()
                 .HasOne(input => input.CreatedBy)
                 .WithMany()
                 .HasForeignKey(input => input.CreatedById)
                 .OnDelete(DeleteBehavior.SetNull);


            //https://stackoverflow.com/questions/7573590/can-a-foreign-key-be-null-and-or-duplicate
            modelBuilder.Entity<LessonType>()
                .HasOne(input => input.UpdatedBy)
                .WithMany()
                .HasForeignKey(input => input.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);


            //----------------------------------------------------------------------------
            //Define one-to-one relationship between TimeSheetDetail and TimeSheetDetailSignaure
            //----------------------------------------------------------------------------
            //Reference: http://stackoverflow.com/questions/35506158/one-to-one-relationships-in-entity-framework-7-code-first
            modelBuilder.Entity<TimeSheetSchedule>()
            .HasOne(input => input.TimeSheetScheduleSignature)
            .WithOne(input => input.TimeSheetSchedule)
            .HasForeignKey<TimeSheetScheduleSignature>(input => input.TimeSheetScheduleId);

            //Define many-to-one relationship between TimeSheet and AppUser
            modelBuilder.Entity<TimeSheet>()
            .HasOne(input => input.Instructor)
            .WithMany(input => input.TimeSheets)
            .HasForeignKey(input => input.InstructorId);

            //Define many-to-one relationship between AccountRate and CustomerAccount
            //-- Missing relationship
            modelBuilder.Entity<AccountRate>()
            .HasOne(input => input.CustomerAccount)
            .WithMany(input => input.AccountRates)
            .HasForeignKey(input => input.CustomerAccountId);

            //Define many-to-one relationship between AccountTimeTable and AccountRate
            //-- Missing relationship
            modelBuilder.Entity<AccountTimeTable>()
            .HasOne(input => input.AccountRate)
            .WithMany(input => input.AccountTimeTables)
            .HasForeignKey(input => input.AccountRateId);

            //Define many-to-one relationship between CustomerAccountComment and CustomerAccount
            //-- Missing relationship            
            modelBuilder.Entity<CustomerAccountComment>()
             .HasOne(input => input.CustomerAccount)
             .WithMany(input => input.Comments)
             .HasForeignKey(input => input.CustomerAccountId);

            modelBuilder.Entity<TimeSheet>()
             .HasOne(input => input.Instructor)
             .WithMany(input => input.TimeSheets)
             .HasForeignKey(input => input.InstructorId);

            modelBuilder.Entity<AppNote>()
                .HasOne(input => input.CreatedBy)
                .WithMany()
                .HasForeignKey(input => input.CreatedById);

            modelBuilder.Entity<AppNote>()
                .HasOne(input => input.AppNotePriorityLevel)
                .WithMany(input => input.AppNotes)
                .HasForeignKey(input => input.AppNotePriorityLevelId);

            modelBuilder.Entity<CustomerAccountComment>()
             .HasOne(input => input.Parent)
             .WithMany(input => input.Child)
             .HasForeignKey(input => input.ParentId)
             .OnDelete(DeleteBehavior.Cascade);
        }//End of OnModelCreating

    }
}
